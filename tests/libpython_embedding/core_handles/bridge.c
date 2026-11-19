/* 独立双 GC 探针：公开 CPython API 管理真实薄函数、GC 可见 owner 与 capsule 生命周期。
 * owner context 仅强持 CLR core token；owner 的 F 强边由 CP GC 遍历，core 不持 CP 环境。
 * 所有导出函数及析构在主解释器 GIL 下调用；禁止关闭后调用。
 */
#include <Python.h>
#include <stdint.h>
#include <stdlib.h>
#include "../function_owner.h"

typedef int (*invoke_fn)(intptr_t, int64_t, int64_t, int64_t *);
typedef void (*release_fn)(intptr_t);
typedef struct { intptr_t token; RpFunctionOwner *owner; } CoreLink;
static invoke_fn invoke_core;
static release_fn release_core;
static long live_links, destroyed_links;
static PyObject *main_dict, *glue_code;

static void destroy_link(void *context) {
    CoreLink *link = context;
    release_core(link->token);
    free(link); live_links--; destroyed_links++;
}

static PyObject *dispatch(PyObject *owner, PyObject *arg) {
    CoreLink *link = rp_fo_context(owner);
    if (!link) return NULL;
    PyObject *f = rp_fo_borrow(link->owner); /* owner 强持真实 F，GIL 下借用。 */
    if (!f || f == Py_None) {
        PyErr_SetString(PyExc_RuntimeError, "执行 core 的函数环境已释放");
        return NULL;
    }
    PyObject *d = PyFunction_GetGlobals(f);
    PyObject *offset = PyDict_GetItemString(d, "offset");
    if (!offset) { PyErr_SetString(PyExc_NameError, "offset"); return NULL; }
    /* 参数转换可调用 __index__ 并重入修改 D；先持有 offset，禁止跨回调借用字典值。 */
    Py_INCREF(offset);
    long long x = PyLong_AsLongLong(arg);
    if (PyErr_Occurred()) { Py_DECREF(offset); return NULL; }
    long long y = PyLong_AsLongLong(offset);
    Py_DECREF(offset);
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
    PyObject *factory_code = PyObject_GetAttrString(PyDict_GetItemString(main_dict, "_factory"), "__code__");
    PyObject *constants = factory_code ? PyObject_GetAttrString(factory_code, "co_consts") : NULL;
    Py_XDECREF(factory_code);
    if (!constants) return -1;
    /* 只取得固定模板的嵌套 code，不执行 factory，避免 trace return 暴露半初始化 F。 */
    for (Py_ssize_t i = 0; i < PyTuple_Size(constants); i++) {
        PyObject *value = PyTuple_GetItem(constants, i);
        if (PyCode_Check(value)) { glue_code = value; Py_INCREF(glue_code); break; }
    }
    Py_DECREF(constants);
    if (!glue_code) { PyErr_SetString(PyExc_RuntimeError, "缺少固定胶水 code"); return -1; }
    return 0;
}
PyObject *ch_eval(const char *expression) {
    PyObject *result = PyRun_String(expression, Py_eval_input, main_dict, main_dict);
    if (!result) PyErr_Print();
    return result; /* owned */
}
static PyObject *make_function(PyObject *d, intptr_t token, int failure) {
    /* token 无论成功失败均被此函数消费，调用方不得二次释放。 */
    CoreLink *link = calloc(1, sizeof(CoreLink));
    if (!link) { release_core(token); return PyErr_NoMemory(); }
    link->token = token;
    RpFunctionOwner *owner = rp_fo_new(link, NULL, destroy_link);
    if (!owner) { free(link); release_core(token); return NULL; }
    live_links++;
    link->owner = owner;
    PyObject *p = PyCFunction_New(&dispatch_def, (PyObject *)owner);
    Py_DECREF(owner); /* 此后 p/closure 保活 owner；绑定前失败没有 F 环。 */
    if (!p) return NULL;
    if (failure == 1) {
        PyErr_SetString(PyExc_RuntimeError, "注入创建失败"); Py_DECREF(p); return NULL;
    }
    PyObject *cell = PyCell_New(p);
    PyObject *closure = cell ? PyTuple_Pack(1, cell) : NULL;
    Py_XDECREF(cell);
    Py_DECREF(p);
    if (!closure) return NULL;
    if (failure == 2) {
        PyErr_SetString(PyExc_RuntimeError, "注入创建失败"); Py_DECREF(closure); return NULL;
    }
    PyObject *f = PyFunction_New(glue_code, d);
    /* 初始化 owner 强环境前不向 GC 对象枚举发布 F；所有组装使用公开 C API。 */
    if (f) PyObject_GC_UnTrack(f);
    if (f && PyFunction_SetClosure(f, closure) < 0) {
        /* stock func_dealloc 无条件解除跟踪；销毁半成品前必须恢复 GC 跟踪。 */
        PyObject_GC_Track(f); Py_DECREF(f); f = NULL;
    }
    Py_DECREF(closure);
    if (!f) return NULL;
    if (failure == 3) {
        rp_fo_unbind(owner);
        PyErr_SetString(PyExc_RuntimeError, "注入创建失败");
        PyObject_GC_Track(f); Py_DECREF(f); return NULL;
    }
    rp_fo_bind(owner, f);
    PyObject_GC_Track(f);
    return f;
}
PyObject *ch_make(PyObject *d, intptr_t token) { return make_function(d, token, 0); }
/* 控制面验证三处已拥有 capsule 的失败回滚；消费 token 后保留析构计数而清除预期异常。 */
int ch_make_failure(PyObject *d, intptr_t token, int stage) {
    PyObject *f = make_function(d, token, stage);
    if (f) { Py_DECREF(f); return -1; }
    if (!PyErr_ExceptionMatches(PyExc_RuntimeError)) return -1;
    PyErr_Clear(); return 0;
}
intptr_t ch_token(PyObject *f) {
    /* 首证只接纳固定创建的无默认值、单 dispatch 捕获函数。 */
    if (!PyFunction_Check(f) || PyFunction_GetCode(f) != glue_code ||
        PyFunction_GetDefaults(f) || PyFunction_GetKwDefaults(f)) return 0;
    PyObject *closure = PyFunction_GetClosure(f);
    if (!closure || PyTuple_Size(closure) != 1) return 0;
    PyObject *p = PyCell_Get(PyTuple_GetItem(closure, 0));
    if (!p) return 0;
    intptr_t token = 0;
    if (PyCFunction_Check(p) && PyCFunction_GetFunction(p) == dispatch) {
        CoreLink *link = rp_fo_context(PyCFunction_GetSelf(p));
        if (link && rp_fo_borrow(link->owner) == f) token = link->token;
    }
    Py_DECREF(p);
    return token;
}
int ch_set(PyObject *d, PyObject *f) { return PyDict_SetItemString(d, "f", f); }
void ch_inc(PyObject *p) { Py_INCREF(p); }
void ch_dec(PyObject *p) { Py_DECREF(p); }
long ch_live(void) { return live_links; }
long ch_destroyed(void) { return destroyed_links; }
int ch_finish(void) {
    Py_CLEAR(glue_code);
    rp_fo_stop();
    return Py_FinalizeEx();
}
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
