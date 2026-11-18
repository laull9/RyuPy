/* stock 原版 libpython 的真实字典操作；module 路径直接读取 md_dict，
 * 不调用用户覆盖的 __dict__ 属性，不制造镜像，所有返回值遵守 owned 引用合同。 */
static PyObject *rp_initial_builtins_dict;
static PyObject *rp_initial_builtins_values[3];
static const char *rp_initial_builtins_names[] = {"globals", "__import__", "__build_class__"};
static void rp_namespace_stop(void) {
    for (int i = 0; i < 3; i++) Py_CLEAR(rp_initial_builtins_values[i]);
    Py_CLEAR(rp_initial_builtins_dict);
}
/* 只在隔离初始化完成、任何应用/第三方代码执行之前捕获，防后续替换污染规范身份。 */
static int rp_namespace_start(void) {
    PyObject *dictionary = PyEval_GetBuiltins();
    if (!dictionary || !PyDict_CheckExact(dictionary)) {
        PyErr_SetString(PyExc_RuntimeError, "initial builtins must be an exact dict"); return -1;
    }
    rp_initial_builtins_dict = Py_NewRef(dictionary);
    for (int i = 0; i < 3; i++) {
        PyObject *value = PyDict_GetItemString(dictionary, rp_initial_builtins_names[i]);
        if (!value || !PyCFunction_Check(value)) {
            rp_namespace_stop();
            PyErr_SetString(PyExc_RuntimeError, "initial canonical builtin is missing"); return -1;
        }
        rp_initial_builtins_values[i] = Py_NewRef(value);
    }
    return 0;
}
API PyObject *rp_initial_builtins(void) {
    if (rp_initial_builtins_dict) return Py_NewRef(rp_initial_builtins_dict);
    PyErr_SetString(PyExc_RuntimeError, "canonical builtins are unavailable"); return NULL;
}
API PyObject *rp_default_object_type(void) { return Py_NewRef((PyObject *)&PyBaseObject_Type); }
API PyObject *rp_initial_builtin(const char *name) {
    for (int i = 0; i < 3; i++)
        if (!strcmp(name, rp_initial_builtins_names[i]) && rp_initial_builtins_values[i])
            return Py_NewRef(rp_initial_builtins_values[i]);
    PyErr_SetString(PyExc_RuntimeError, "canonical builtin is unavailable"); return NULL;
}
/* 纯C状态，可在解释器停止后读取；不能以清计数替代上面的真实DECREF。 */
API int rp_namespace_snapshot_count(void) {
    int count = rp_initial_builtins_dict != NULL;
    for (int i = 0; i < 3; i++) count += rp_initial_builtins_values[i] != NULL;
    return count;
}
/* 普通builtin读取仍遵循当前frame；规范身份检查只使用上面的独立初始化见证。 */
API PyObject *rp_builtin(const char *name) {
    PyObject *value = rp_builtin_item(name);
    if (!value) { PyErr_SetString(PyExc_TypeError, "unknown builtin type"); return NULL; }
    return Py_NewRef(value);
}
API PyObject *rp_entry_module(void) {
    PyObject *module = PyImport_AddModule("__main__");
    return module ? Py_NewRef(module) : NULL;
}
/* 源码模块先拥有独立md_dict；sys.modules的发布、失败回滚交同一真实loader事务。 */
API PyObject *rp_compiled_module(PyObject *name) {
    if (!PyUnicode_CheckExact(name)) {
        PyErr_SetString(PyExc_TypeError, "compiled module name must be an exact string"); return NULL;
    }
    return PyModule_NewObject(name);
}
API PyObject *rp_module_namespace(PyObject *value) {
    if (PyDict_CheckExact(value)) return Py_NewRef(value);
    if (PyModule_Check(value)) return Py_XNewRef(PyModule_GetDict(value));
    PyErr_SetString(PyExc_TypeError, "module namespace requires an exact dict or module");
    return NULL;
}

/* 缺键为 0，命中为 1，异常为 -1；已绑定 None 与缺键严格分开。 */
API int rp_namespace_lookup(PyObject *dict, PyObject *key, PyObject **result) {
    *result = NULL;
    if (!PyDict_CheckExact(dict)) {
        PyErr_SetString(PyExc_TypeError, "module namespace must be an exact dict"); return -1;
    }
    PyObject *value = PyDict_GetItemWithError(dict, key);
    if (value) { *result = Py_NewRef(value); return 1; }
    return PyErr_Occurred() ? -1 : 0;
}

API int rp_namespace_delete(PyObject *dict, PyObject *key) {
    if (!PyDict_CheckExact(dict)) {
        PyErr_SetString(PyExc_TypeError, "module namespace must be an exact dict"); return -1;
    }
    if (PyDict_DelItem(dict, key) == 0) return 1;
    if (PyErr_ExceptionMatches(PyExc_KeyError)) { PyErr_Clear(); return 0; }
    return -1;
}

/* 模块对象的导入读取协议独立维护，复用本文件已有的真实字典/模块边界。 */
#include "import_protocol.h"
