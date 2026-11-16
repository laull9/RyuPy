/* RyuPy 严格零字节码内核 C4-1 导入路由器公共内部辅助函数。
 *
 * 本头文件由锚点机制注入官方 CPython 3.12.13 的 `Python/ryupy_import_helpers.h`，
 * 由 `Python/import.c` 在 `Python/ryupy_import_loaders.h` 之后、`Python/ryupy_import_router.h`
 * 之前包含，提供：
 * 1. `rp_raise_module_not_found` / `rp_raise_not_found_fmt` / `rp_raise_halted_none`：
 *    构造带精确 `.name` 的 `ModuleNotFoundError`（对齐 `_bootstrap.py` 报错格式）。
 * 2. `rp_push_uninit_submodule` / `rp_pop_uninit_submodule`：维护父包 spec 的
 *    `_uninitialized_submodules` 列表，配合 `Objects/moduleobject.c` 精确报错。
 * 3. `rp_move_module_to_end`、`rp_attach_to_parent`、`rp_import_parent_if_needed`：
 *    处理父包预先加载、`__path__` 校验与子模块挂载。
 */
#ifndef Py_RYUPY_IMPORT_HELPERS_H
#define Py_RYUPY_IMPORT_HELPERS_H

static void
rp_raise_module_not_found(PyThreadState *tstate, PyObject *mod_name, PyObject *msg)
{
    if (msg != NULL) {
        (void)PyErr_SetImportErrorSubclass(PyExc_ModuleNotFoundError, msg, mod_name, NULL);
    }
    if (!_PyErr_Occurred(tstate)) {
        _PyErr_SetObject(tstate, PyExc_ModuleNotFoundError, mod_name ? mod_name : Py_None);
    }
}

static void
rp_raise_not_found_fmt(PyThreadState *tstate, PyObject *mod_name, const char *fmt,
                        PyObject *arg1, PyObject *arg2)
{
    PyObject *msg = arg2 != NULL
        ? PyUnicode_FromFormat(fmt, arg1, arg2)
        : PyUnicode_FromFormat(fmt, arg1);
    rp_raise_module_not_found(tstate, mod_name, msg);
    Py_XDECREF(msg);
}

static void
rp_raise_halted_none(PyThreadState *tstate, PyObject *mod_name)
{
    PyObject *msg = PyUnicode_FromFormat("import of %U halted; None in sys.modules", mod_name);
    rp_raise_module_not_found(tstate, mod_name, msg);
    Py_XDECREF(msg);
}

static int
rp_push_uninit_submodule(PyObject *parent_spec, PyObject *child_name)
{
    if (parent_spec == NULL || parent_spec == Py_None || child_name == NULL) {
        return 0;
    }
    PyObject *list = NULL;
    if (_PyObject_LookupAttr(parent_spec, &_Py_ID(_uninitialized_submodules), &list) < 0) {
        return -1;
    }
    int rc = 0;
    if (list != NULL && PyList_Check(list)) {
        rc = PyList_Append(list, child_name);
    }
    Py_XDECREF(list);
    return rc;
}

static void
rp_pop_uninit_submodule(PyThreadState *tstate, PyObject *parent_spec)
{
    if (parent_spec == NULL || parent_spec == Py_None) {
        return;
    }
    PyObject *exc = _PyErr_GetRaisedException(tstate);
    PyObject *list = NULL;
    if (_PyObject_LookupAttr(parent_spec, &_Py_ID(_uninitialized_submodules), &list) == 1 &&
        list != NULL && PyList_Check(list))
    {
        Py_ssize_t n = PyList_GET_SIZE(list);
        if (n > 0) {
            (void)PyList_SetSlice(list, n - 1, n, NULL);
        }
    }
    Py_XDECREF(list);
    if (_PyErr_Occurred(tstate)) {
        _PyErr_Clear(tstate);
    }
    _PyErr_SetRaisedException(tstate, exc);
}

static int
rp_move_module_to_end(PyThreadState *tstate, PyObject *abs_name, PyObject **io_mod)
{
    PyObject *modules = MODULES(tstate->interp);
    PyObject *popped = NULL;
    if (PyDict_CheckExact(modules)) {
        popped = _PyDict_Pop(modules, abs_name, NULL);
        if (popped == NULL && _PyErr_Occurred(tstate)) {
            return -1;
        }
    }
    else {
        popped = PyObject_GetItem(modules, abs_name);
        if (popped != NULL) {
            if (PyObject_DelItem(modules, abs_name) < 0) {
                Py_DECREF(popped);
                return -1;
            }
        }
        else if (_PyErr_ExceptionMatches(tstate, PyExc_KeyError)) {
            _PyErr_Clear(tstate);
        }
        else {
            return -1;
        }
    }
    if (popped == NULL) {
        _PyErr_SetObject(tstate, PyExc_KeyError, abs_name);
        return -1;
    }
    int rc = PyObject_SetItem(modules, abs_name, popped);
    if (rc < 0) {
        Py_DECREF(popped);
        return -1;
    }
    Py_SETREF(*io_mod, popped);
    return 0;
}

static int
rp_attach_to_parent(PyThreadState *tstate, PyObject *parent_mod, PyObject *parent_name,
                    PyObject *child_name, PyObject *child_mod)
{
    if (parent_mod == NULL || child_name == NULL) {
        return 0;
    }
    if (PyObject_SetAttr(parent_mod, child_name, child_mod) < 0) {
        if (_PyErr_ExceptionMatches(tstate, PyExc_AttributeError)) {
            _PyErr_Clear(tstate);
            PyObject *msg = PyUnicode_FromFormat(
                "Cannot set an attribute on %R for child module %R",
                parent_name, child_name);
            if (msg != NULL) {
                int w = PyErr_WarnExplicitObject(PyExc_ImportWarning, msg,
                                                 parent_name, 0, NULL, NULL);
                Py_DECREF(msg);
                if (w < 0) {
                    return -1;
                }
            }
            return 0;
        }
        return -1;
    }
    return 0;
}

static int
rp_import_parent_if_needed(PyThreadState *tstate, PyObject *abs_name,
                           PyObject **out_parent_name, PyObject **out_parent_spec,
                           PyObject **out_child_name)
{
    *out_parent_name = NULL;
    *out_parent_spec = NULL;
    *out_child_name = NULL;

    Py_ssize_t len = PyUnicode_GET_LENGTH(abs_name);
    Py_ssize_t dot = PyUnicode_FindChar(abs_name, '.', 0, len, -1);
    if (dot == -2) {
        return -1;
    }
    if (dot < 0) {
        return 0;
    }
    PyObject *parent_name = PyUnicode_Substring(abs_name, 0, dot);
    PyObject *child_name = PyUnicode_Substring(abs_name, dot + 1, len);
    if (parent_name == NULL || child_name == NULL) {
        Py_XDECREF(parent_name);
        Py_XDECREF(child_name);
        return -1;
    }

    PyObject *parent_mod = import_get_module(tstate, parent_name);
    if (parent_mod == NULL && _PyErr_Occurred(tstate)) {
        Py_DECREF(parent_name);
        Py_DECREF(child_name);
        return -1;
    }
    if (parent_mod == NULL) {
        PyObject *imp = IMPORT_FUNC(tstate->interp);
        PyObject *res = imp != NULL
            ? PyObject_CallOneArg(imp, parent_name)
            : PyImport_ImportModuleLevelObject(parent_name, NULL, NULL, NULL, 0);
        if (res == NULL) {
            Py_DECREF(parent_name);
            Py_DECREF(child_name);
            return -1;
        }
        Py_DECREF(res);
        parent_mod = import_get_module(tstate, parent_name);
    }
    if (parent_mod == NULL) {
        if (!_PyErr_Occurred(tstate)) {
            _PyErr_SetObject(tstate, PyExc_KeyError, parent_name);
        }
        Py_DECREF(parent_name);
        Py_DECREF(child_name);
        return -1;
    }
    if (parent_mod == Py_None) {
        /* 对齐 CPython _bootstrap.py:1314-1319：sys.modules[parent] is None 报 is not a package。 */
        Py_DECREF(parent_mod);
        rp_raise_not_found_fmt(tstate, abs_name,
                               "No module named %R; %R is not a package",
                               abs_name, parent_name);
        Py_DECREF(parent_name);
        Py_DECREF(child_name);
        return -1;
    }

    PyObject *parent_path = NULL;
    int has_path = _PyObject_LookupAttr(parent_mod, &_Py_ID(__path__), &parent_path);
    if (has_path < 0) {
        Py_DECREF(parent_mod);
        Py_DECREF(parent_name);
        Py_DECREF(child_name);
        return -1;
    }
    if (has_path == 0 || parent_path == NULL) {
        rp_raise_not_found_fmt(tstate, abs_name,
                               "No module named %R; %R is not a package",
                               abs_name, parent_name);
        Py_DECREF(parent_mod);
        Py_DECREF(parent_name);
        Py_DECREF(child_name);
        return -1;
    }
    Py_DECREF(parent_path);

    PyObject *parent_spec = NULL;
    int spec_rc = _PyObject_LookupAttr(parent_mod, &_Py_ID(__spec__), &parent_spec);
    Py_DECREF(parent_mod);
    if (spec_rc < 0) {
        Py_DECREF(parent_name);
        Py_DECREF(child_name);
        return -1;
    }
    *out_parent_name = parent_name;
    *out_parent_spec = parent_spec;
    *out_child_name = child_name;
    return 0;
}

#endif /* Py_RYUPY_IMPORT_HELPERS_H */
