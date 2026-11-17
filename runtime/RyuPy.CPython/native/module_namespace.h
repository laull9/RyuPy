/* stock 原版 libpython 的真实字典操作；module 路径直接读取 md_dict，
 * 不调用用户覆盖的 __dict__ 属性，不制造镜像，所有返回值遵守 owned 引用合同。 */
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
