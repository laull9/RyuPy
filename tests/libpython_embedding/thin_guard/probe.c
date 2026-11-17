/* stock 薄函数守卫实证：初始化前原生 audit、真实胶水 code.replace 和弱身份登记。
 * 只用公开 C API；P 是原生身份回传叶子，替代未来静态 CIL seam，绝无业务实现。
 * 单主解释器、常规 GIL；登记链由 capsule 所有，hook 不持 F 强根。
 */
#include <Python.h>
#include <pthread.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef struct Link {
    PyObject *weak_f;
    PyObject *code; /* borrowed：F/code/P/capsule 链存活时使用，禁止增加强环。 */
    struct Link *next;
} Link;
static Link *links;
static long live, destroyed, calls, blocked;
static const char *capsule_name = "ryupy.thin_guard";

/* hook 在任何 Python 可执行代码运行前安装；比较精确弱身份，无 Python equality。 */
static int audit(const char *event, PyObject *args, void *unused) {
    (void)unused;
    if (strcmp(event, "object.__setattr__") || PyTuple_Size(args) != 3) return 0;
    PyObject *name = PyTuple_GetItem(args, 1);
    if (!PyUnicode_Check(name) || PyUnicode_CompareWithASCIIString(name, "__code__"))
        return 0;
    PyObject *f = PyTuple_GetItem(args, 0);
    for (Link *link = links; link; link = link->next) {
        if (link->weak_f && PyWeakref_GetObject(link->weak_f) == f) {
            blocked++;
            PyErr_SetString(PyExc_RuntimeError, "已登记薄函数禁止替换 __code__");
            return -1;
        }
    }
    return 0;
}

static void destroy(PyObject *capsule) {
    Link *link = PyCapsule_GetPointer(capsule, capsule_name);
    if (!link) { PyErr_WriteUnraisable(capsule); return; }
    Link **cursor = &links;
    while (*cursor && *cursor != link) cursor = &(*cursor)->next;
    if (*cursor) *cursor = link->next;
    Py_XDECREF(link->weak_f);
    free(link);
    live--;
    destroyed++;
}

static PyObject *dispatch(PyObject *capsule, PyObject *arg) {
    Link *link = PyCapsule_GetPointer(capsule, capsule_name);
    if (!link) return NULL;
    PyObject *f = link->weak_f ? PyWeakref_GetObject(link->weak_f) : Py_None;
    if (f == Py_None || PyFunction_GetCode(f) != link->code ||
        PyFunction_GetDefaults(f) || PyFunction_GetKwDefaults(f)) {
        PyErr_SetString(PyExc_RuntimeError, "薄函数已释放或形状失效");
        return NULL;
    }
    calls++;
    return Py_NewRef(arg);
}
static PyMethodDef dispatch_def = {"native_target", dispatch, METH_O, "原生身份叶子"};

/* 编译真实源码，提取 code，不运行 Python factory，避免 return trace 提前发布。 */
static PyObject *find_code(PyObject *code, const char *name) {
    PyObject *constants = PyObject_GetAttrString(code, "co_consts");
    if (!constants) return NULL;
    PyObject *result = NULL;
    for (Py_ssize_t i = 0; i < PyTuple_Size(constants) && !result; i++) {
        PyObject *item = PyTuple_GetItem(constants, i);
        if (!PyCode_Check(item)) continue;
        PyObject *code_name = PyObject_GetAttrString(item, "co_name");
        if (!code_name) break;
        if (!PyUnicode_CompareWithASCIIString(code_name, name)) result = Py_NewRef(item);
        Py_DECREF(code_name);
        if (!result) result = find_code(item, name);
        if (PyErr_Occurred()) break;
    }
    Py_DECREF(constants);
    return result;
}

static PyObject *replace_constants(PyObject *code, PyObject *p) {
    PyObject *old = PyObject_GetAttrString(code, "co_consts");
    if (!old) return NULL;
    PyObject *constants = PyTuple_New(PyTuple_Size(old));
    if (!constants) { Py_DECREF(old); return NULL; }
    int replaced = 0;
    for (Py_ssize_t i = 0; i < PyTuple_Size(old); i++) {
        PyObject *item = PyTuple_GetItem(old, i);
        if (item == Py_None) { item = p; replaced++; }
        if (PyTuple_SetItem(constants, i, Py_NewRef(item)) < 0) {
            Py_DECREF(constants); Py_DECREF(old); return NULL;
        }
    }
    Py_DECREF(old);
    if (!replaced) {
        Py_DECREF(constants);
        PyErr_SetString(PyExc_RuntimeError, "固定版本模板常量形状漂移");
        return NULL;
    }
    PyObject *method = PyObject_GetAttrString(code, "replace");
    PyObject *args = PyTuple_New(0);
    PyObject *kwargs = Py_BuildValue("{s:O}", "co_consts", constants);
    Py_DECREF(constants);
    PyObject *result = method && args && kwargs ? PyObject_Call(method, args, kwargs) : NULL;
    Py_XDECREF(method); Py_XDECREF(args); Py_XDECREF(kwargs);
    return result;
}

static PyObject *make(PyObject *self, PyObject *args) {
    (void)self;
    const char *mode;
    PyObject *globals;
    int fail = 0;
    if (!PyArg_ParseTuple(args, "sO!|i", &mode, &PyDict_Type, &globals, &fail)) return NULL;
    const char *source;
    if (!strcmp(mode, "A"))
        source = "def factory(p):\n def f(x):\n  return p(x)\n return f\n";
    else if (!strcmp(mode, "B"))
        source = "def f(x):\n p=(None,)[0]\n return p(x)\n";
    else if (!strcmp(mode, "C"))
        source = "def f(x):\n return (None,)[0](x)\n";
    else { PyErr_SetString(PyExc_ValueError, "未知模式"); return NULL; }
    char filename[128];
    snprintf(filename, sizeof(filename), "<ryupy-thin-guard-%s>", mode);
    PyObject *module_code = Py_CompileString(source, filename, Py_file_input);
    PyObject *code = module_code ? find_code(module_code, "f") : NULL;
    Py_XDECREF(module_code);
    if (!code) return NULL;
    Link *link = calloc(1, sizeof(Link));
    if (!link) { Py_DECREF(code); return PyErr_NoMemory(); }
    PyObject *capsule = PyCapsule_New(link, capsule_name, destroy);
    if (!capsule) { free(link); Py_DECREF(code); return NULL; }
    link->next = links; links = link; live++;
    PyObject *p = PyCFunction_New(&dispatch_def, capsule);
    Py_DECREF(capsule);
    PyObject *closure = NULL, *f = NULL;
    if (!p) goto done;
    if (fail == 1) goto injected;
    if (!strcmp(mode, "A")) {
        PyObject *cell = PyCell_New(p);
        closure = cell ? PyTuple_Pack(1, cell) : NULL;
        Py_XDECREF(cell);
        if (!closure) goto done;
    } else {
        PyObject *replacement = replace_constants(code, p);
        if (!replacement) goto done;
        Py_SETREF(code, replacement);
    }
    if (fail == 2) goto injected;
    f = PyFunction_New(code, globals);
    if (!f) goto done;
    /* GC 枚举不能看到 weak/code/closure 尚未就绪的半成品。 */
    PyObject_GC_UnTrack(f);
    if (closure && PyFunction_SetClosure(f, closure) < 0) goto unpublished;
    link->weak_f = PyWeakref_NewRef(f, NULL);
    if (!link->weak_f) goto unpublished;
    link->code = code;
    if (fail == 3) {
        PyErr_SetString(PyExc_RuntimeError, "注入创建失败");
        goto unpublished;
    }
    PyObject_GC_Track(f);
    goto done;
unpublished:
    Py_CLEAR(f);
    goto done;
injected:
    PyErr_SetString(PyExc_RuntimeError, "注入创建失败");
done:
    Py_XDECREF(closure); Py_XDECREF(p); Py_DECREF(code);
    return f;
}

static PyObject *stats(PyObject *self, PyObject *unused) {
    (void)self; (void)unused;
    return Py_BuildValue("(llll)", live, destroyed, calls, blocked);
}

/* 模拟扩展通过公开属性 setter 写 code，必须收到同一拒绝。 */
static PyObject *set_code(PyObject *self, PyObject *args) {
    (void)self;
    PyObject *f, *code;
    if (!PyArg_ParseTuple(args, "OO", &f, &code)) return NULL;
    if (PyObject_SetAttrString(f, "__code__", code) < 0) return NULL;
    Py_RETURN_NONE;
}

static PyObject *callback(PyObject *self, PyObject *args) {
    (void)self;
    PyObject *f, *arg;
    if (!PyArg_ParseTuple(args, "OO", &f, &arg)) return NULL;
    return PyObject_CallOneArg(f, arg);
}

typedef struct { PyObject *f, *arg, *result; int failed; } Worker;
static void *worker(void *data) {
    Worker *w = data;
    PyGILState_STATE state = PyGILState_Ensure();
    w->result = PyObject_CallOneArg(w->f, w->arg);
    if (!w->result) { PyErr_Clear(); w->failed = 1; }
    PyGILState_Release(state);
    return NULL;
}

static PyObject *thread_call(PyObject *self, PyObject *args) {
    (void)self;
    Worker w = {0};
    if (!PyArg_ParseTuple(args, "OO", &w.f, &w.arg)) return NULL;
    pthread_t thread;
    if (pthread_create(&thread, NULL, worker, &w)) {
        PyErr_SetString(PyExc_RuntimeError, "pthread_create 失败"); return NULL;
    }
    int joined;
    Py_BEGIN_ALLOW_THREADS
    joined = pthread_join(thread, NULL);
    Py_END_ALLOW_THREADS
    if (joined || w.failed) {
        PyErr_SetString(PyExc_RuntimeError, "原生线程回调失败"); return NULL;
    }
    return w.result;
}

static PyMethodDef methods[] = {
    {"make", make, METH_VARARGS, "创建并登记真实薄函数"},
    {"stats", stats, METH_NOARGS, "live/destroyed/calls/blocked"},
    {"set_code", set_code, METH_VARARGS, "原生属性替换反例"},
    {"callback", callback, METH_VARARGS, "原生回调"},
    {"thread_call", thread_call, METH_VARARGS, "原生线程回调"},
    {NULL, NULL, 0, NULL}
};
static struct PyModuleDef definition = {
    PyModuleDef_HEAD_INIT, "thin_native", "独立探针控制面", -1, methods,
    NULL, NULL, NULL, NULL
};
PyMODINIT_FUNC PyInit_thin_native(void) { return PyModule_Create(&definition); }

int main(int argc, char **argv) {
    if (argc != 3) return 2;
    if (PySys_AddAuditHook(audit, NULL) < 0 ||
        PyImport_AppendInittab("thin_native", PyInit_thin_native) < 0) return 3;
    PyConfig config;
    PyConfig_InitIsolatedConfig(&config);
    config.write_bytecode = 0;
    PyStatus status = PyConfig_SetBytesString(&config, &config.home, argv[1]);
    if (!PyStatus_Exception(status)) status = Py_InitializeFromConfig(&config);
    PyConfig_Clear(&config);
    if (PyStatus_Exception(status)) return 4;
    FILE *input = fopen(argv[2], "r");
    int failed = !input || PyRun_SimpleFileEx(input, argv[2], 1) != 0;
    int finalized = Py_FinalizeEx();
    printf("RESULT failed=%d finalize=%d live=%ld destroyed=%ld calls=%ld blocked=%ld\n",
           failed, finalized, live, destroyed, calls, blocked);
    return failed || finalized || live ? 1 : 0;
}
