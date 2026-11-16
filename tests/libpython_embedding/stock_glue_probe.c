/* 原版 libpython 胶水探针：对比产品原生 finder/loader 与窄 A2 手工交接。
 * dispatch 使用纯 C 模块代替 CIL 回调，只验证导入服务层和局部成本。
 * PEP523 逐帧观测与计时分进程运行；观测表不完整时明确失败。
 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>
#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#define API
static const char *phase = "startup";
static int failures;

/* 与产品静态 hook 共用回调形状；两个固定模块正文只在本探针用 C 模拟。 */
static PyObject *dispatch(long owner, int op, PyObject *name, PyObject *a,
                          PyObject *b, int n, void *context) {
    (void)owner;
    (void)a;
    (void)b;
    (void)n;
    (void)context;
    const char *text = PyUnicode_AsUTF8(name);
    if (!text) return NULL;
    int package = strcmp(text, "probe_pkg") == 0;
    int known = package || strcmp(text, "probe_pkg.child") == 0;
    if (op == 20) return PyBool_FromLong(known);
    if (op == 22) return PyBool_FromLong(package);
    if (op != 21 || !known) {
        PyErr_SetString(PyExc_ModuleNotFoundError, "探针只提供两个固定 C 模块");
        return NULL;
    }
    PyObject *module = PyModule_NewObject(name);
    if (!module) return NULL;
    if (PyModule_AddIntConstant(module, "value", 42) < 0) {
        Py_DECREF(module);
        return NULL;
    }
    return module;
}

/* 直接复用被测产品头；不复制、替换产品实现。 */
#include "import_hooks.h"

typedef struct {
    char stage[32];
    char file[256];
    char name[128];
    unsigned long count;
} FrameCount;

static FrameCount frames[128];
static size_t used;
static unsigned long frame_total, table_overflow, text_truncation, lookup_failure;
static _PyFrameEvalFunction original_eval;

/* 内部帧的 Unstable API 仅用于已验证的 3.12/3.13 观测，不进入产品。 */
static PyObject *observe(PyThreadState *state, struct _PyInterpreterFrame *frame,
                         int flag) {
    PyObject *error = PyErr_GetRaisedException();
    PyObject *code = PyUnstable_InterpreterFrame_GetCode(frame);
    PyObject *file = code ? PyObject_GetAttrString(code, "co_filename") : NULL;
    PyObject *name = code ? PyObject_GetAttrString(code, "co_name") : NULL;
    const char *filename = file ? PyUnicode_AsUTF8(file) : NULL;
    const char *function = name ? PyUnicode_AsUTF8(name) : NULL;
    frame_total++;
    if (!filename || !function) {
        lookup_failure++;
    } else if (strlen(phase) >= sizeof(frames[0].stage)
               || strlen(filename) >= sizeof(frames[0].file)
               || strlen(function) >= sizeof(frames[0].name)) {
        text_truncation++;
    } else {
        size_t index = 0;
        while (index < used) {
            if (strcmp(frames[index].stage, phase) == 0
                && strcmp(frames[index].file, filename) == 0
                && strcmp(frames[index].name, function) == 0) break;
            index++;
        }
        if (index == used && used == sizeof(frames) / sizeof(frames[0])) {
            table_overflow++;
        } else {
            if (index == used) {
                strcpy(frames[index].stage, phase);
                strcpy(frames[index].file, filename);
                strcpy(frames[index].name, function);
                used++;
            }
            frames[index].count++;
        }
    }
    Py_XDECREF(name);
    Py_XDECREF(file);
    Py_XDECREF(code);
    PyErr_Clear();
    PyErr_SetRaisedException(error);
    return original_eval(state, frame, flag);
}

static void checked(const char *label, PyObject *result) {
    if (result) {
        Py_DECREF(result);
        return;
    }
    fprintf(stderr, "FAIL %s\n", label);
    if (PyErr_Occurred()) PyErr_Print();
    failures++;
}

/* A2 对照只覆盖本探针固定包；它没有完整模块锁、回滚或任意 fromlist 合同。 */
static PyObject *a2_find(PyObject *self, PyObject *args) {
    (void)self;
    PyObject *name, *import_function;
    if (!PyArg_ParseTuple(args, "OO", &name, &import_function)) return NULL;
    PyObject *module = PyDict_GetItemWithError(PyImport_GetModuleDict(), name);
    if (module) return Py_NewRef(module);
    if (PyErr_Occurred()) return NULL;
    const char *text = PyUnicode_AsUTF8(name);
    if (!text) return NULL;
    int package = strcmp(text, "probe_pkg") == 0;
    if (strcmp(text, "probe_pkg.child") == 0) {
        PyObject *parent = PyImport_ImportModuleLevel("probe_pkg", NULL, NULL, NULL, 0);
        if (!parent) return NULL;
        Py_DECREF(parent);
    }
    module = dispatch(0, 21, name, NULL, NULL, 0, NULL);
    if (!module) return NULL;
    PyObject *bootstrap = PyDict_GetItemString(PyImport_GetModuleDict(), "_frozen_importlib");
    if (!bootstrap) {
        PyErr_SetString(PyExc_RuntimeError, "缺少原版 importlib");
        Py_DECREF(module);
        return NULL;
    }
    PyObject *spec = PyObject_CallMethod(bootstrap, "ModuleSpec", "OO", name, Py_None);
    if (!spec) {
        Py_DECREF(module);
        return NULL;
    }
    int result = PyObject_SetAttrString(module, "__spec__", spec);
    if (result == 0) result = PyObject_SetAttrString(spec, "_initializing", Py_False);
    Py_DECREF(spec);
    if (result == 0 && package) {
        PyObject *path = PyList_New(0);
        result = path ? PyObject_SetAttrString(module, "__path__", path) : -1;
        Py_XDECREF(path);
    }
    if (result == 0) result = PyDict_SetItem(PyImport_GetModuleDict(), name, module);
    if (result < 0) {
        Py_DECREF(module);
        return NULL;
    }
    return module;
}

static PyObject *a2_from(PyObject *self, PyObject *args) {
    (void)self;
    PyObject *module, *fromlist, *import_function;
    if (!PyArg_ParseTuple(args, "OOO", &module, &fromlist, &import_function)) return NULL;
    PyObject *child = PyObject_GetAttrString(module, "child");
    if (!child) {
        if (!PyErr_ExceptionMatches(PyExc_AttributeError)) return NULL;
        PyErr_Clear();
        child = PyImport_ImportModuleLevel("probe_pkg.child", NULL, NULL, Py_True, 0);
        if (!child) return NULL;
        if (PyObject_SetAttrString(module, "child", child) < 0) {
            Py_DECREF(child);
            return NULL;
        }
    }
    Py_DECREF(child);
    return Py_NewRef(module);
}

static PyObject *a2_lock(PyObject *self, PyObject *name) {
    (void)self;
    (void)name;
    Py_RETURN_NONE;
}

static PyMethodDef a2_methods[] = {
    {"_find_and_load", a2_find, METH_VARARGS, NULL},
    {"_handle_fromlist", a2_from, METH_VARARGS, NULL},
    {"_lock_unlock_module", a2_lock, METH_O, NULL}
};

static int install_a2(void) {
    PyObject *bootstrap = PyDict_GetItemString(PyImport_GetModuleDict(), "_frozen_importlib");
    if (!bootstrap) {
        PyErr_SetString(PyExc_RuntimeError, "缺少原版 importlib");
        return -1;
    }
    for (size_t index = 0; index < sizeof(a2_methods) / sizeof(a2_methods[0]); index++) {
        PyObject *function = PyCFunction_New(&a2_methods[index], NULL);
        if (!function) return -1;
        int result = PyObject_SetAttrString(bootstrap, a2_methods[index].ml_name, function);
        Py_DECREF(function);
        if (result < 0) return -1;
    }
    return 0;
}

static double now(void) {
    struct timespec value;
    if (clock_gettime(CLOCK_MONOTONIC, &value) != 0) {
        perror("clock_gettime");
        exit(2);
    }
    return value.tv_sec + value.tv_nsec / 1e9;
}

static void timed_imports(const char *label, const char *name, PyObject *fromlist,
                          long count) {
    double start = now();
    for (long index = 0; index < count; index++) {
        PyObject *result = PyImport_ImportModuleLevel(name, NULL, NULL, fromlist, 0);
        if (!result) {
            checked(label, NULL);
            return;
        }
        Py_DECREF(result);
    }
    printf("TIME %s n=%ld seconds=%.9f\n", label, count, now() - start);
}

static int exercise(long iterations) {
    phase = "cold_import";
    double start = now();
    PyObject *fromlist = Py_BuildValue("(s)", "child");
    if (!fromlist) return -1;
    checked("package_fromlist", PyImport_ImportModuleLevel("probe_pkg", NULL, NULL, fromlist, 0));
    PyObject *globals = Py_BuildValue("{s:s}", "__package__", "probe_pkg");
    if (!globals) {
        Py_DECREF(fromlist);
        return -1;
    }
    checked("relative", PyImport_ImportModuleLevel("child", globals, NULL, Py_True, 1));
    Py_DECREF(globals);
    PyObject *child = PyDict_GetItemString(PyImport_GetModuleDict(), "probe_pkg.child");
    PyObject *value = child ? PyObject_GetAttrString(child, "value") : NULL;
    if (!value || PyLong_AsLong(value) != 42 || PyErr_Occurred()) {
        Py_XDECREF(value);
        Py_DECREF(fromlist);
        if (!PyErr_Occurred()) PyErr_SetString(PyExc_AssertionError, "C 模块结果不是 42");
        return -1;
    }
    Py_DECREF(value);
    printf("TIME cold_import seconds=%.9f\n", now() - start);
    phase = "cached_plain";
    timed_imports("cached_plain", "probe_pkg.child", Py_True, iterations);
    phase = "cached_fromlist";
    timed_imports("cached_fromlist", "probe_pkg", fromlist, iterations);
    Py_DECREF(fromlist);
    phase = "codec";
    PyObject *decoder = PyCodec_IncrementalDecoder("utf-8", "strict");
    if (!decoder) return -1;
    PyObject *decoded = PyObject_CallMethod(decoder, "decode", "y#i", "汉", (Py_ssize_t)3, 1);
    Py_DECREF(decoder);
    if (!decoded) return -1;
    /* 用真实 Unicode 值断言 codec 结果，避免只检查调用成功。 */
    PyObject *expected = PyUnicode_FromString("汉");
    int equal = expected ? PyObject_RichCompareBool(decoded, expected, Py_EQ) : -1;
    Py_XDECREF(expected);
    Py_DECREF(decoded);
    if (equal != 1) {
        if (equal == 0) PyErr_SetString(PyExc_AssertionError, "UTF-8 解码结果错误");
        return -1;
    }
    phase = "stdio";
    PyObject *stream = PySys_GetObject("stdout");
    if (!stream) {
        PyErr_SetString(PyExc_RuntimeError, "缺少标准输出流");
        return -1;
    }
    checked("stdio", PyObject_CallMethod(stream, "write", "s", ""));
    checked("flush", PyObject_CallMethod(stream, "flush", NULL));
    return 0;
}

int main(int argc, char **argv) {
    if (argc != 5 || (strcmp(argv[2], "native") != 0 && strcmp(argv[2], "a2") != 0)) {
        fprintf(stderr, "usage: probe HOME native|a2 COUNT PROFILE\n");
        return 2;
    }
    char *end = NULL;
    errno = 0;
    long iterations = strtol(argv[3], &end, 10);
    if (errno || !end || end == argv[3] || *end || iterations <= 0) return 2;
    if (strcmp(argv[4], "0") != 0 && strcmp(argv[4], "1") != 0) return 2;
    int profile = strcmp(argv[4], "1") == 0;
    PyConfig config;
    PyConfig_InitIsolatedConfig(&config);
    config.site_import = 0;
    config.write_bytecode = 0;
    config.parse_argv = 0;
    PyStatus status = PyConfig_SetBytesString(&config, &config.home, argv[1]);
    double start = now();
    if (!PyStatus_Exception(status)) status = Py_InitializeFromConfig(&config);
    PyConfig_Clear(&config);
    if (PyStatus_Exception(status)) Py_ExitStatusException(status);
    printf("VERSION %s\nTIME startup seconds=%.9f\n", Py_GetVersion(), now() - start);
    if (profile) {
        original_eval = _PyInterpreterState_GetEvalFrameFunc(PyInterpreterState_Get());
        if (!original_eval) {
            fprintf(stderr, "无法取得原版求值函数\n");
            Py_FinalizeEx();
            return 2;
        }
        _PyInterpreterState_SetEvalFrameFunc(PyInterpreterState_Get(), observe);
    }
    phase = "install";
    if (strcmp(argv[2], "a2") == 0) {
        if (install_a2() < 0) checked("install_a2", NULL);
    } else {
        PyObject *bridge = PyImport_AddModule("_ryupy_imports");
        if (!bridge) checked("bridge", NULL);
        else checked("install_native", rp_install_native_imports());
    }
    if (!failures && exercise(iterations) < 0) checked("exercise", NULL);
    phase = "shutdown";
    native_import_hooks_clear();
    int result = Py_FinalizeEx();
    for (size_t index = 0; index < used; index++) {
        printf("FRAME %s %lu %s %s\n", frames[index].stage, frames[index].count,
               frames[index].file, frames[index].name);
    }
    printf("OBSERVATION total=%lu overflow=%lu truncation=%lu lookup_failure=%lu\n",
           frame_total, table_overflow, text_truncation, lookup_failure);
    printf("RESULT failures=%d finalize=%d profile=%d\n", failures, result, profile);
    return failures || result || table_overflow || text_truncation || lookup_failure ? 1 : 0;
}
