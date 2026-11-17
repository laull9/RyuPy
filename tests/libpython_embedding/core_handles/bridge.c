/* 独立双 GC 探针：公开 CPython API 管理真实薄函数、弱回流与 capsule 生命周期。
 * capsule 仅强持 CLR core token；弱 F 不能形成通向 globals 的强回边。
 * 所有导出函数及析构在主解释器 GIL 下调用；禁止关闭后调用。
 */
#include <Python.h>
#include <stdint.h>
#include <stdlib.h>

typedef int (*invoke_fn)(intptr_t, int64_t, int64_t, int64_t *);
typedef void (*release_fn)(intptr_t);
typedef struct { intptr_t token; PyObject *weak_f; } CoreLink;
static invoke_fn invoke_core;
static release_fn release_core;
static long live_links, destroyed_links;
static PyObject *main_dict, *factory_code;
static const char *capsule_name = "ryupy.proof.core";

static void destroy_link(PyObject *capsule) {
    CoreLink *link = PyCapsule_GetPointer(capsule, capsule_name);
    if (!link) { PyErr_WriteUnraisable(capsule); return; }
    Py_XDECREF(link->weak_f);
    release_core(link->token);
    free(link);
    live_links--;
    destroyed_links++;
}

static PyObject *dispatch(PyObject *capsule, PyObject *arg) {
    CoreLink *link = PyCapsule_GetPointer(capsule, capsule_name);
    if (!link) return NULL;
    PyObject *f = PyWeakref_GetObject(link->weak_f); /* GIL 保护 borrowed F。 */
    if (!f || f == Py_None) {
        PyErr_SetString(PyExc_RuntimeError, "执行 core 的函数环境已释放");
        return NULL;
    }
    PyObject *d = PyFunction_GetGlobals(f);
    PyObject *offset = PyDict_GetItemString(d, "offset");
    if (!offset) { PyErr_SetString(PyExc_NameError, "offset"); return NULL; }
    long long x = PyLong_AsLongLong(arg), y = PyLong_AsLongLong(offset);
    if (PyErr_Occurred()) return NULL;
    int64_t result;
    if (invoke_core(link->token, x, y, &result)) {
        PyErr_SetString(PyExc_OverflowError, "CIL 叶子运算失败");
        return NULL;
    }
    return PyLong_FromLongLong(result);
}
static PyMethodDef dispatch_def = {"cil_dispatch", dispatch, METH_O, "固定静态 CIL 入口"};

int ch_run(const char *source) {
    PyObject *result = PyRun_String(source, Py_file_input, main_dict, main_dict);
    if (!result) { PyErr_Print(); return -1; }
    Py_DECREF(result);
    return 0;
}
int ch_init(const char *home, invoke_fn invoke, release_fn release) {
    invoke_core = invoke; release_core = release;
    PyConfig config;
    PyConfig_InitIsolatedConfig(&config);
    config.write_bytecode = 0;
    wchar_t *wide = Py_DecodeLocale(home, NULL);
    if (!wide) return -1;
    PyStatus status = PyConfig_SetString(&config, &config.home, wide);
    PyMem_RawFree(wide);
    if (!PyStatus_Exception(status)) status = Py_InitializeFromConfig(&config);
    PyConfig_Clear(&config);
    if (PyStatus_Exception(status)) return -1;
    main_dict = PyModule_GetDict(PyImport_AddModule("__main__"));
    /* 固定胶水只有转调，无业务计算。code/frame 如实属于该源码。 */
    if (ch_run("def _factory(p):\n def f(x):\n  return p(x)\n return f\n")) return -1;
    factory_code = PyObject_GetAttrString(PyDict_GetItemString(main_dict, "_factory"), "__code__");
    return factory_code ? 0 : -1;
}
PyObject *ch_eval(const char *expression) {
    PyObject *result = PyRun_String(expression, Py_eval_input, main_dict, main_dict);
    if (!result) PyErr_Print();
    return result; /* owned */
}
PyObject *ch_make(PyObject *d, intptr_t token) {
    /* token 无论成功失败均被此函数消费，调用方不得二次释放。 */
    CoreLink *link = calloc(1, sizeof(CoreLink));
    if (!link) { release_core(token); return PyErr_NoMemory(); }
    link->token = token;
    PyObject *capsule = PyCapsule_New(link, capsule_name, destroy_link);
    if (!capsule) { free(link); release_core(token); return NULL; }
    live_links++;
    PyObject *p = PyCFunction_New(&dispatch_def, capsule);
    Py_DECREF(capsule);
    if (!p) return NULL;
    PyObject *factory = PyFunction_New(factory_code, d);
    PyObject *f = factory ? PyObject_CallFunctionObjArgs(factory, p, NULL) : NULL;
    Py_XDECREF(factory);
    Py_DECREF(p);
    if (!f) return NULL;
    link->weak_f = PyWeakref_NewRef(f, NULL);
    if (!link->weak_f) { Py_DECREF(f); return NULL; }
    return f;
}
intptr_t ch_token(PyObject *f) {
    /* 首证只接纳固定创建的无默认值、单 dispatch 捕获函数。 */
    if (!PyFunction_Check(f) || PyFunction_GetDefaults(f) || PyFunction_GetKwDefaults(f)) return 0;
    PyObject *closure = PyFunction_GetClosure(f);
    if (!closure || PyTuple_Size(closure) != 1) return 0;
    PyObject *p = PyCell_Get(PyTuple_GetItem(closure, 0));
    if (!p) return 0;
    intptr_t token = 0;
    if (PyCFunction_Check(p) && PyCFunction_GetFunction(p) == dispatch) {
        CoreLink *link = PyCapsule_GetPointer(PyCFunction_GetSelf(p), capsule_name);
        if (link && PyWeakref_GetObject(link->weak_f) == f) token = link->token;
    }
    Py_DECREF(p);
    return token;
}
int ch_set(PyObject *d, PyObject *f) { return PyDict_SetItemString(d, "f", f); }
void ch_inc(PyObject *p) { Py_INCREF(p); }
void ch_dec(PyObject *p) { Py_DECREF(p); }
long ch_live(void) { return live_links; }
long ch_destroyed(void) { return destroyed_links; }
int ch_finish(void) { Py_CLEAR(factory_code); return Py_FinalizeEx(); }
int ch_call(PyObject *f, int64_t x, int64_t *result) {
    PyObject *arg = PyLong_FromLongLong(x);
    PyObject *value = arg ? PyObject_CallFunctionObjArgs(f, arg, NULL) : NULL;
    Py_XDECREF(arg);
    if (!value) { PyErr_Print(); return -1; }
    *result = PyLong_AsLongLong(value);
    Py_DECREF(value);
    if (PyErr_Occurred()) { PyErr_Print(); return -1; }
    return 0;
}
