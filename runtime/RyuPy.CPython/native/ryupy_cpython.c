/* CPython 3.9–3.15 兼容通道 shim：保留既有导入钩子、PEP 523 观测、委派源码入口与三版本类桥；
 * 绝不包含或链接任何严格零字节码内核专属导出符号（rp_kernel_*）。 */
#include "cpython_compat_env.h"

/* 宿主在调用任何布局相关函数之前通过静态 shim P/Invoke 核对头文件与已加载库版本。 */
API const char *rp_build_version(void) { return PY_VERSION; }
API const char *rp_python_version(void) { return Py_GetVersion(); }

#include "execution_audit.h"
#include "method_descriptor.h"
#include "class_construction.h"
#include "import_hooks.h"
#include "signature_bridge.h"
#include "cpython_bridge_ops.h"
#include "module_namespace.h"

static PyObject *bridge_has(PyObject *self, PyObject *name) { (void)self; return dispatch(-1, 20, name, NULL, NULL, 0, NULL); }
static PyObject *bridge_load(PyObject *self, PyObject *name) { (void)self; return dispatch(-1, 21, name, NULL, NULL, 0, NULL); }
static PyObject *bridge_package(PyObject *self, PyObject *name) { (void)self; return dispatch(-1, 22, name, NULL, NULL, 0, NULL); }
static PyMethodDef bridge_methods[] = {
    {"_rp_has", bridge_has, METH_O, NULL}, {"_rp_load", bridge_load, METH_O, NULL},
    {"_rp_package", bridge_package, METH_O, NULL}, {NULL, NULL, 0, NULL}
};

API int rp_start(const char *home, const char **paths, int path_count, const char **argv, int argc, managed_call callback) {
    init_error[0] = '\0';
    PyPreConfig preconfig; PyPreConfig_InitIsolatedConfig(&preconfig);
    preconfig.utf8_mode = 1;
    if (fail_status(Py_PreInitialize(&preconfig))) return -1;
    PyConfig config; PyConfig_InitIsolatedConfig(&config);
    config.site_import = 0; config.write_bytecode = 0; config.parse_argv = 0;
    config.install_signal_handlers = 0; config.module_search_paths_set = 1;
    if (fail_status(PyConfig_SetBytesString(&config, &config.home, home))) goto error;
    if (fail_status(PyConfig_SetBytesString(&config, &config.stdio_encoding, "utf-8"))) goto error;
    if (fail_status(PyConfig_SetBytesString(&config, &config.stdio_errors, "surrogateescape"))) goto error;
    for (int list = 0; list < 2; list++) {
        int count = list ? argc : path_count;
        const char **items = list ? argv : paths;
        for (int index = 0; index < count; index++) {
            wchar_t *wide = Py_DecodeLocale(items[index], NULL);
            if (!wide) { strcpy(init_error, "invalid UTF-8 configuration"); goto error; }
            PyStatus status = PyWideStringList_Append(list ? &config.argv : &config.module_search_paths, wide);
            PyMem_RawFree(wide); if (fail_status(status)) goto error;
        }
    }
    if (fail_status(Py_InitializeFromConfig(&config))) goto error;
    PyConfig_Clear(&config); dispatch = callback;
    managed_type = (PyTypeObject *)PyType_FromSpec(&managed_spec);
    if (!managed_type) { strcpy(init_error, "managed proxy type creation failed"); PyErr_Clear(); Py_FinalizeEx(); return -1; }
    initial_thread = PyEval_SaveThread(); return 0;
error:
    /* 初始化失败可能已经建立部分解释器；只在真实已初始化时同线程清理。 */
    PyConfig_Clear(&config); if (Py_IsInitialized()) Py_FinalizeEx(); return -1;
}

/* 查找器全局只属于内部模块，用户builtins不出现桥函数、sys别名或loader类。 */
API PyObject *rp_install_imports(const char *source) {
    PyObject *module = PyModule_New("_ryupy_imports");
    if (!module) return NULL;
    PyObject *globals = PyModule_GetDict(module);
    if (PyDict_SetItemString(globals, "__builtins__", PyEval_GetBuiltins()) < 0) goto error;
    for (PyMethodDef *method = bridge_methods; method->ml_name; method++) {
        PyObject *function = PyCFunction_New(method, NULL);
        if (!function) goto error;
        int status = PyDict_SetItemString(globals, method->ml_name, function);
        Py_DECREF(function);
        if (status < 0) goto error;
    }
    if (source && *source) {
        PyObject *result = PyRun_String(source, Py_file_input, globals, globals);
        if (!result) goto error;
        Py_DECREF(result);
    }
    if (PyDict_SetItemString(PyImport_GetModuleDict(), "_ryupy_imports", module) < 0) goto error;
    Py_DECREF(module);
    Py_RETURN_NONE;
error:
    Py_DECREF(module);
    return NULL;
}

API int rp_stop(void) {
    PyEval_RestoreThread(initial_thread); initial_thread = NULL;
    rp_execution_audit_phase(2);
    native_import_hooks_clear();
    Py_CLEAR(native_method_type); Py_CLEAR(native_static_method_type); Py_CLEAR(managed_descriptor_type);
    int result = Py_FinalizeEx();
    inspect_bridge_closed_aliases();
    audit_finalized = audit_enabled;
    managed_type = NULL;
    return result;
}

/* 以 __main__ 模块上下文编译并执行显式委派入口源码；异常保留在 PyErr 供宿主接管。 */
API int rp_run_entry(const char *path, const char *source) {
    PyObject *main_module = PyImport_AddModule("__main__");
    if (!main_module) return -1;
    PyObject *globals = PyModule_GetDict(main_module);
    if (!globals) return -1;
    PyObject *filename = PyUnicode_DecodeUTF8(path, (Py_ssize_t)strlen(path), "surrogatepass");
    if (!filename) return -1;
    if (PyDict_SetItemString(globals, "__file__", filename) < 0 ||
        PyDict_SetItemString(globals, "__cached__", Py_None) < 0 ||
        PyDict_SetItemString(globals, "__package__", Py_None) < 0) {
        Py_DECREF(filename);
        return -1;
    }
    if (!PyDict_GetItemString(globals, "__builtins__")) {
        if (PyDict_SetItemString(globals, "__builtins__", PyEval_GetBuiltins()) < 0) {
            Py_DECREF(filename);
            return -1;
        }
    }
    PyObject *code = Py_CompileStringObject(source, filename, Py_file_input, NULL, -1);
    Py_DECREF(filename);
    if (!code) return -1;
    PyObject *result = PyEval_EvalCode(code, globals, globals);
    Py_DECREF(code);
    if (!result) return -1;
    Py_DECREF(result);
    return 0;
}
