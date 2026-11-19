/* 同一次持 GIL 的无回调 C 调用冻结精确字典的顺序与别名；每个槽交付一个 owned 引用。 */
API int rp_dict_snapshot(PyObject *dict, PyObject **items, Py_ssize_t count) {
    if (!PyDict_CheckExact(dict) || PyDict_Size(dict) != count || (count && !items)) {
        PyErr_SetString(PyExc_RuntimeError, "invalid exact dictionary snapshot");
        return -1;
    }
    Py_ssize_t cursor = 0;
    PyObject *key, *value;
    while (PyDict_Next(dict, &cursor, &key, &value)) {
        *items++ = Py_NewRef(key);
        *items++ = Py_NewRef(value);
    }
    return 0;
}
