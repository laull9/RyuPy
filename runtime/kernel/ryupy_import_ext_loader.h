/* RyuPy 严格零字节码内核 C4-1 纯 C ExtensionFileLoader 实现。
 *
 * 本头文件由锚点机制注入官方 CPython 3.12.13 的 `Python/ryupy_import_ext_loader.h`，
 * 由 `Python/ryupy_import_loaders.h` 包含，提供 `importlib._bootstrap_external.ExtensionFileLoader`：
 * 1. 所有方法均使用 `METH_VARARGS | METH_KEYWORDS` 实例绑定与 `PyArg_ParseTupleAndKeywords`，
 *    与 CPython 3.12.13 官方签名完全对齐（`create_module(spec)`、`exec_module(module)`、
 *    `is_package(fullname)`、`get_code(fullname)`、`get_source(fullname)`、
 *    `get_filename(name=None)`、`get_data(path)`、`get_resource_reader(name=None)`）。
 * 2. `create_module` 复用上游 `import_find_extension` 与 `_PyImport_LoadDynamicModuleWithSpec`；
 *    `exec_module` 复用 `exec_builtin_or_dynamic`，保持唯一执行路径不复制。
 */
#ifndef Py_RYUPY_IMPORT_EXT_LOADER_H
#define Py_RYUPY_IMPORT_EXT_LOADER_H

typedef struct {
    PyObject_HEAD
    PyObject *name;
    PyObject *path;
    int is_package_flag;
} RpExtensionLoaderObject;

static int
rp_ext_loader_traverse(RpExtensionLoaderObject *self, visitproc visit, void *arg)
{
    Py_VISIT(Py_TYPE(self));
    Py_VISIT(self->name);
    Py_VISIT(self->path);
    return 0;
}

static int
rp_ext_loader_clear(RpExtensionLoaderObject *self)
{
    Py_CLEAR(self->name);
    Py_CLEAR(self->path);
    return 0;
}

static void
rp_ext_loader_dealloc(RpExtensionLoaderObject *self)
{
    PyTypeObject *tp = Py_TYPE(self);
    PyObject_GC_UnTrack(self);
    (void)rp_ext_loader_clear(self);
    tp->tp_free((PyObject *)self);
    Py_DECREF(tp);
}

static PyObject *
rp_ext_loader_get_name(RpExtensionLoaderObject *self, void *Py_UNUSED(ctx))
{
    return Py_NewRef(self->name ? self->name : Py_None);
}

static PyObject *
rp_ext_loader_get_path(RpExtensionLoaderObject *self, void *Py_UNUSED(ctx))
{
    return Py_NewRef(self->path ? self->path : Py_None);
}

static PyObject *
rp_ext_loader_repr(RpExtensionLoaderObject *self)
{
    return PyUnicode_FromFormat("<ExtensionFileLoader(name=%R, path=%R)>",
                                self->name ? self->name : Py_None,
                                self->path ? self->path : Py_None);
}

static Py_hash_t
rp_ext_loader_hash(RpExtensionLoaderObject *self)
{
    Py_hash_t h1 = PyObject_Hash(self->name ? self->name : Py_None);
    if (h1 == -1) return -1;
    Py_hash_t h2 = PyObject_Hash(self->path ? self->path : Py_None);
    if (h2 == -1) return -1;
    Py_hash_t h = h1 ^ h2;
    return h == -1 ? -2 : h;
}

static PyObject *
rp_ext_loader_richcompare(RpExtensionLoaderObject *self, PyObject *other, int op)
{
    if ((op != Py_EQ && op != Py_NE) || !Py_IS_TYPE(other, Py_TYPE(self))) {
        Py_RETURN_NOTIMPLEMENTED;
    }
    RpExtensionLoaderObject *rhs = (RpExtensionLoaderObject *)other;
    PyObject *a1 = self->name ? self->name : Py_None;
    PyObject *b1 = rhs->name ? rhs->name : Py_None;
    PyObject *a2 = self->path ? self->path : Py_None;
    PyObject *b2 = rhs->path ? rhs->path : Py_None;
    int eq1 = PyObject_RichCompareBool(a1, b1, Py_EQ);
    if (eq1 < 0) return NULL;
    int eq2 = eq1 ? PyObject_RichCompareBool(a2, b2, Py_EQ) : 0;
    if (eq2 < 0) return NULL;
    int eq = eq1 && eq2;
    return PyBool_FromLong(op == Py_EQ ? eq : !eq);
}

static PyObject *
rp_ext_loader_create_module(PyObject *Py_UNUSED(self), PyObject *args, PyObject *kwds)
{
    PyObject *spec = rp_loader_parse_kw(args, kwds, "O:create_module", rp_kw_spec, 0, NULL);
    if (spec == NULL) {
        return NULL;
    }
    PyObject *name = PyObject_GetAttr(spec, &_Py_ID(name));
    if (name == NULL) return NULL;
    PyObject *path = PyObject_GetAttr(spec, &_Py_ID(origin));
    if (path == NULL) {
        Py_DECREF(name);
        return NULL;
    }
    PyThreadState *tstate = _PyThreadState_GET();
    PyObject *mod = import_find_extension(tstate, name, path);
    Py_DECREF(name);
    Py_DECREF(path);
    if (mod != NULL || _PyErr_Occurred(tstate)) {
        return mod;
    }
#ifdef HAVE_DYNAMIC_LOADING
    return _PyImport_LoadDynamicModuleWithSpec(spec, NULL);
#else
    PyErr_SetString(PyExc_ImportError, "dynamic loading is disabled on this build");
    return NULL;
#endif
}

static PyObject *
rp_ext_loader_exec_module(PyObject *Py_UNUSED(self), PyObject *args, PyObject *kwds)
{
    PyObject *mod = rp_loader_parse_kw(args, kwds, "O:exec_module", rp_kw_module, 0, NULL);
    if (mod == NULL) {
        return NULL;
    }
    if (exec_builtin_or_dynamic(mod) < 0) {
        return NULL;
    }
    Py_RETURN_NONE;
}

static PyObject *
rp_ext_loader_is_package(RpExtensionLoaderObject *self, PyObject *args, PyObject *kwds)
{
    PyObject *fullname = rp_loader_parse_kw(args, kwds, "O:is_package", rp_kw_fullname, 0, NULL);
    if (fullname == NULL || rp_loader_check_name(self->name, fullname) < 0) {
        return NULL;
    }
    return PyBool_FromLong(self->is_package_flag);
}

/* 对齐 CPython 3.12.13 @_check_name 装饰器签名 `(self, name=None)`：省略或传 None 时取 `self->name`。 */
static PyObject *
rp_ext_loader_get_filename(RpExtensionLoaderObject *self, PyObject *args, PyObject *kwds)
{
    int has_name = 0;
    PyObject *name = rp_loader_parse_kw(args, kwds, "|O:get_filename", rp_kw_name, 1, &has_name);
    if (name == NULL) {
        return NULL;
    }
    if (has_name && name != Py_None && rp_loader_check_name(self->name, name) < 0) {
        return NULL;
    }
    return Py_NewRef(self->path ? self->path : Py_None);
}

static PyObject *
rp_ext_loader_return_none_checked(RpExtensionLoaderObject *self, PyObject *args, PyObject *kwds,
                                  const char *fmt)
{
    PyObject *fullname = rp_loader_parse_kw(args, kwds, fmt, rp_kw_fullname, 0, NULL);
    if (fullname == NULL || rp_loader_check_name(self->name, fullname) < 0) {
        return NULL;
    }
    Py_RETURN_NONE;
}

static PyObject *
rp_ext_loader_get_code(RpExtensionLoaderObject *self, PyObject *args, PyObject *kwds)
{
    return rp_ext_loader_return_none_checked(self, args, kwds, "O:get_code");
}

static PyObject *
rp_ext_loader_get_source(RpExtensionLoaderObject *self, PyObject *args, PyObject *kwds)
{
    return rp_ext_loader_return_none_checked(self, args, kwds, "O:get_source");
}

static PyObject *
rp_ext_loader_get_resource_reader(PyObject *Py_UNUSED(self), PyObject *args, PyObject *kwds)
{
    return rp_loader_reject_resource_reader_impl(args, kwds, "|O:get_resource_reader", rp_kw_name, 1);
}

static PyGetSetDef rp_ext_loader_getsets[] = {
    {"name", (getter)rp_ext_loader_get_name, NULL, NULL, NULL},
    {"path", (getter)rp_ext_loader_get_path, NULL, NULL, NULL},
    {NULL}
};

static PyMethodDef rp_ext_loader_methods[] = {
    {"create_module", (PyCFunction)(void (*)(void))rp_ext_loader_create_module, METH_VARARGS | METH_KEYWORDS, NULL},
    {"exec_module", (PyCFunction)(void (*)(void))rp_ext_loader_exec_module, METH_VARARGS | METH_KEYWORDS, NULL},
    {"is_package", (PyCFunction)(void (*)(void))rp_ext_loader_is_package, METH_VARARGS | METH_KEYWORDS, NULL},
    {"get_filename", (PyCFunction)(void (*)(void))rp_ext_loader_get_filename, METH_VARARGS | METH_KEYWORDS, NULL},
    {"get_code", (PyCFunction)(void (*)(void))rp_ext_loader_get_code, METH_VARARGS | METH_KEYWORDS, NULL},
    {"get_source", (PyCFunction)(void (*)(void))rp_ext_loader_get_source, METH_VARARGS | METH_KEYWORDS, NULL},
    {"get_data", (PyCFunction)(void (*)(void))rp_loader_reject_data, METH_VARARGS | METH_KEYWORDS, NULL},
    {"get_resource_reader", (PyCFunction)(void (*)(void))rp_ext_loader_get_resource_reader, METH_VARARGS | METH_KEYWORDS, NULL},
    {NULL, NULL, 0, NULL}
};

static PyType_Slot rp_ext_loader_slots[] = {
    {Py_tp_dealloc, rp_ext_loader_dealloc},
    {Py_tp_traverse, rp_ext_loader_traverse},
    {Py_tp_clear, rp_ext_loader_clear},
    {Py_tp_repr, rp_ext_loader_repr},
    {Py_tp_hash, rp_ext_loader_hash},
    {Py_tp_richcompare, rp_ext_loader_richcompare},
    {Py_tp_getset, rp_ext_loader_getsets},
    {Py_tp_methods, rp_ext_loader_methods},
    {0, NULL}
};

static PyType_Spec rp_ext_loader_type_spec = {
    "importlib._bootstrap_external.ExtensionFileLoader",
    sizeof(RpExtensionLoaderObject),
    0,
    Py_TPFLAGS_DEFAULT | Py_TPFLAGS_HAVE_GC,
    rp_ext_loader_slots,
};

#endif /* Py_RYUPY_IMPORT_EXT_LOADER_H */
