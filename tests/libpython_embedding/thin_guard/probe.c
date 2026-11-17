/* stock 薄函数守卫实证：初始化前原生 audit、真实 closure 胶水和 GC 可见强 owner。
 * 只用公开 C API；P 是原生身份回传叶子，替代未来静态 CIL seam，绝无业务实现。
 * 单主解释器、常规 GIL；登记链由 GC owner 所有，hook 不持额外 F 强根。
 */
#include <Python.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "../function_owner.h"

typedef struct Link {
    RpFunctionOwner *owner; /* 仅借用：登记链在 owner dealloc 的任何 DECREF 前摘除。 */
    PyObject *code; /* borrowed：F/closure/P/owner 链存活时使用，禁止增加强环。 */
    struct Link *next;
} Link;
static Link *links;
static long live, destroyed, calls, blocked;
static PyObject *resurrect_target, *resurrect_saved;
static int resurrection_watcher = -1;

/* 独立消费者 watcher：只在一次 DESTROY 取强引用，验证 stock 的合法复活路径。 */
static int consumer_watcher(PyFunction_WatchEvent event, PyFunctionObject *function, PyObject *value) {
    (void)value;
    if (event == PyFunction_EVENT_DESTROY && (PyObject *)function == resurrect_target) {
        resurrect_target = NULL;
        resurrect_saved = Py_NewRef((PyObject *)function);
    }
    return 0;
}
static PyObject *arm_resurrection(PyObject *self, PyObject *function) {
    (void)self;
    if (!PyFunction_Check(function) || resurrect_target || resurrect_saved) {
        PyErr_SetString(PyExc_RuntimeError, "复活控制面状态无效"); return NULL;
    }
    if (resurrection_watcher < 0) {
        resurrection_watcher = PyFunction_AddWatcher(consumer_watcher);
        if (resurrection_watcher < 0) return NULL;
    }
    resurrect_target = function; /* DESTROY 通知之前本体有效；此处不持强引用。 */
    Py_RETURN_NONE;
}
static PyObject *take_resurrection(PyObject *self, PyObject *unused) {
    (void)self; (void)unused;
    if (!resurrect_saved) { PyErr_SetString(PyExc_RuntimeError, "未发生 watcher 复活"); return NULL; }
    PyObject *result = resurrect_saved; resurrect_saved = NULL;
    return result;
}

/* stock GC clear 内 DESTROY 复活可能留下已清理字段，先检查公开 getter，禁止盲目调用。 */
static PyObject *resurrection_state(PyObject *self, PyObject *function) {
    (void)self;
    if (!PyFunction_Check(function)) { PyErr_SetString(PyExc_TypeError, "要求函数"); return NULL; }
    return Py_BuildValue("(iii)", PyFunction_GetCode(function) != NULL,
                         PyFunction_GetGlobals(function) != NULL,
                         PyFunction_GetClosure(function) != NULL);
}

/* hook 在任何 Python 可执行代码运行前安装；比较 owner 持有的精确函数身份，无 Python equality。 */
static int audit(const char *event, PyObject *args, void *unused) {
    (void)unused;
    if (strcmp(event, "object.__setattr__") || PyTuple_Size(args) != 3) return 0;
    PyObject *name = PyTuple_GetItem(args, 1);
    if (!PyUnicode_Check(name) || PyUnicode_CompareWithASCIIString(name, "__code__"))
        return 0;
    PyObject *f = PyTuple_GetItem(args, 0);
    for (Link *link = links; link; link = link->next) {
        if (rp_fo_borrow(link->owner) == f) {
            blocked++;
            PyErr_SetString(PyExc_RuntimeError, "已登记薄函数禁止替换 __code__");
            return -1;
        }
    }
    return 0;
}

static void detach(void *context) {
    Link *link = context;
    Link **cursor = &links;
    while (*cursor && *cursor != link) cursor = &(*cursor)->next;
    if (*cursor) *cursor = link->next;
}
static void destroy(void *context) {
    free(context); live--; destroyed++;
}

static PyObject *dispatch(PyObject *owner, PyObject *arg) {
    Link *link = rp_fo_context(owner);
    if (!link) return NULL;
    PyObject *f = rp_fo_borrow(link->owner);
    if (f == Py_None || PyFunction_GetCode(f) != link->code ||
        PyFunction_GetDefaults(f) || PyFunction_GetKwDefaults(f)) {
        PyErr_SetString(PyExc_RuntimeError, "薄函数已释放或形状失效");
        return NULL;
    }
    PyFrameObject *frame = PyEval_GetFrame();
    PyCodeObject *caller = frame ? PyFrame_GetCode(frame) : NULL;
    int matches = caller && (PyObject *)caller == link->code;
    Py_XDECREF(caller);
    if (!matches) { PyErr_SetString(PyExc_RuntimeError, "薄函数 dispatch 需要原胶水调用帧"); return NULL; }
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

static PyObject *make(PyObject *self, PyObject *args) {
    (void)self;
    const char *mode;
    PyObject *globals;
    int fail = 0;
    if (!PyArg_ParseTuple(args, "sO!|i", &mode, &PyDict_Type, &globals, &fail)) return NULL;
    if (strcmp(mode, "A")) { PyErr_SetString(PyExc_ValueError, "仅支持 GC 可见 closure 模式 A"); return NULL; }
    const char *source = "def factory(p):\n def f(x):\n  return p(x)\n return f\n";
    char filename[128];
    snprintf(filename, sizeof(filename), "<ryupy-thin-guard-%s>", mode);
    PyObject *module_code = Py_CompileString(source, filename, Py_file_input);
    PyObject *code = module_code ? find_code(module_code, "f") : NULL;
    Py_XDECREF(module_code);
    if (!code) {
        if (!PyErr_Occurred())
            PyErr_SetString(PyExc_RuntimeError, "固定胶水模板缺少 f code");
        return NULL;
    }
    Link *link = calloc(1, sizeof(Link));
    if (!link) { Py_DECREF(code); return PyErr_NoMemory(); }
    RpFunctionOwner *owner = rp_fo_new(link, detach, destroy);
    if (!owner) { free(link); Py_DECREF(code); return NULL; }
    link->owner = owner; link->next = links; links = link; live++;
    PyObject *p = PyCFunction_New(&dispatch_def, (PyObject *)owner);
    /* 直到创建结束都独立保活 owner，覆盖失败路径主动断环。 */
    PyObject *closure = NULL, *f = NULL;
    if (!p) goto done;
    if (fail == 1) goto injected;
    PyObject *cell = PyCell_New(p);
    closure = cell ? PyTuple_Pack(1, cell) : NULL;
    Py_XDECREF(cell);
    if (!closure) goto done;
    if (fail == 2) goto injected;
    f = PyFunction_New(code, globals);
    if (!f) goto done;
    /* GC 枚举不能看到 owner/code/closure 尚未就绪的半成品。 */
    PyObject_GC_UnTrack(f);
    if (closure && PyFunction_SetClosure(f, closure) < 0) goto unpublished;
    rp_fo_bind(owner, f);
    link->code = code;
    if (fail == 3) {
        PyErr_SetString(PyExc_RuntimeError, "注入创建失败");
        goto unpublished;
    }
    PyObject_GC_Track(f);
    goto done;
unpublished:
    rp_fo_unbind(owner);
    /* stock func_dealloc 内部无条件解除 GC 跟踪，失败销毁前必须先恢复。 */
    PyObject_GC_Track(f);
    Py_CLEAR(f);
    goto done;
injected:
    PyErr_SetString(PyExc_RuntimeError, "注入创建失败");
done:
    Py_XDECREF(closure); Py_XDECREF(p); Py_DECREF(owner); Py_DECREF(code);
    return f;
}

static PyObject *stats(PyObject *self, PyObject *unused) {
    (void)self; (void)unused;
    return Py_BuildValue("(llll)", live, destroyed, calls, blocked);
}

/* 公开属性 setter 对照：记录并拒绝已登记 F 的 code 赋值。 */
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

static PyMethodDef methods[] = {
    {"make", make, METH_VARARGS, "创建并登记真实薄函数"},
    {"stats", stats, METH_NOARGS, "live/destroyed/calls/blocked"},
    {"set_code", set_code, METH_VARARGS, "原生属性赋值对照"},
    {"callback", callback, METH_VARARGS, "原生回调"},
    {"arm_resurrection", arm_resurrection, METH_O, "独立 watcher 合法保活函数"},
    {"take_resurrection", take_resurrection, METH_NOARGS, "移交 watcher 持有的函数"},
    {"resurrection_state", resurrection_state, METH_O, "公开 getter 观察 clear 后字段"},
    {NULL, NULL, 0, NULL}
};
static struct PyModuleDef definition = {
    PyModuleDef_HEAD_INIT, "thin_native", "独立探针控制面", -1, methods,
    NULL, NULL, NULL, NULL
};
PyMODINIT_FUNC PyInit_thin_native(void) { return PyModule_Create(&definition); }

int main(int argc, char **argv) {
    setvbuf(stdout, NULL, _IONBF, 0);
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
    if (resurrection_watcher >= 0) (void)PyFunction_ClearWatcher(resurrection_watcher);
    resurrect_target = NULL; Py_CLEAR(resurrect_saved);
    rp_fo_stop();
    int finalized = Py_FinalizeEx();
    printf("RESULT failed=%d finalize=%d live=%ld destroyed=%ld calls=%ld blocked=%ld\n",
           failed, finalized, live, destroyed, calls, blocked);
    return failed || finalized || live ? 1 : 0;
}
