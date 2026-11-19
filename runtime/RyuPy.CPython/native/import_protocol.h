/* stock IMPORT_FROM原语：公共属性→权威缓存→真实ImportError字段；不加载额外模块，不执行名字配方。
 * name_from使用本次固定Python头文件的ImportError布局，3.12/3.13真实构建与异常差分共同验收。 */
static PyObject *rp_import_attribute_impl(PyObject *unused, PyObject *arguments) {
    (void)unused;
    PyObject *receiver, *member;
    if (!PyArg_ParseTuple(arguments, "OO", &receiver, &member)) return NULL;
    PyObject *value = PyObject_GetAttr(receiver, member);
    if (value || !PyErr_ExceptionMatches(PyExc_AttributeError)) return value;
    PyErr_Clear();
    PyObject *package = PyObject_GetAttrString(receiver, "__name__");
    if (package && PyUnicode_Check(package)) {
        PyObject *qualified = PyUnicode_FromFormat("%U.%U", package, member);
        if (!qualified) { Py_DECREF(package); return NULL; }
        value = PyImport_GetModule(qualified);
        Py_DECREF(qualified);
        if (value || PyErr_Occurred()) { Py_DECREF(package); return value; }
    } else {
#if PY_VERSION_HEX >= 0x030D0000
        if (!package && !PyErr_ExceptionMatches(PyExc_AttributeError)) return NULL;
#endif
        Py_CLEAR(package); PyErr_Clear();
    }
    PyObject *filename = PyModule_GetFilenameObject(receiver);
    PyObject *display = package ? Py_NewRef(package) : PyUnicode_FromString("<unknown module name>");
    if (!display) { Py_XDECREF(filename); return NULL; }
    PyObject *message;
    if (!filename || !PyUnicode_Check(filename)) {
        Py_CLEAR(filename); PyErr_Clear();
#if PY_VERSION_HEX >= 0x030D0000
        PyObject *spec = PyObject_GetAttrString(receiver, "__spec__");
        PyObject *initializing = spec ? PyObject_GetAttrString(spec, "_initializing") : NULL;
        int partial = initializing ? PyObject_IsTrue(initializing) : 0;
        Py_XDECREF(initializing); Py_XDECREF(spec); PyErr_Clear();
        message = partial > 0
            ? PyUnicode_FromFormat("cannot import name %R from partially initialized module %R (most likely due to a circular import)", member, display)
            : PyUnicode_FromFormat("cannot import name %R from %R (unknown location)", member, display);
#else
        message = PyUnicode_FromFormat("cannot import name %R from %R (unknown location)", member, display);
#endif
    } else {
        PyObject *spec = PyObject_GetAttrString(receiver, "__spec__");
        PyObject *initializing = spec ? PyObject_GetAttrString(spec, "_initializing") : NULL;
        int partial = initializing ? PyObject_IsTrue(initializing) : 0;
        Py_XDECREF(initializing); Py_XDECREF(spec); PyErr_Clear();
        if (partial < 0) partial = 0;
        message = partial
            ? PyUnicode_FromFormat("cannot import name %R from partially initialized module %R (most likely due to a circular import) (%S)", member, display, filename)
            : PyUnicode_FromFormat("cannot import name %R from %R (%S)", member, display, filename);
    }
    Py_DECREF(display);
    PyErr_SetImportError(message, package, filename);
    Py_XDECREF(message); Py_XDECREF(package); Py_XDECREF(filename);
#if PY_VERSION_HEX >= 0x030c0000
    PyObject *error = PyErr_GetRaisedException();
    if (error && PyErr_GivenExceptionMatches(error, PyExc_ImportError))
        Py_XSETREF(((PyImportErrorObject *)error)->name_from, Py_NewRef(member));
    PyErr_SetRaisedException(error);
#endif
    return NULL;
}
static PyMethodDef rp_import_attribute_definition = {
    "__ryupy_import_attribute_protocol__", rp_import_attribute_impl, METH_VARARGS,
    "执行通用IMPORT_FROM属性、缓存和异常协议。"
};
API PyObject *rp_import_attribute_callable(void) {
    return PyCFunction_New(&rp_import_attribute_definition, NULL);
}
