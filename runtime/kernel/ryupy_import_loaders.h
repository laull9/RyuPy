/* RyuPy 严格零字节码内核 C4-1 纯 C Loader 类型家族实现。
 *
 * 本头文件由锚点机制注入官方 CPython 3.12.13 的 `Python/ryupy_import_loaders.h`，
 * 由 `Python/import.c` 在 `import_find_and_load` 之前包含，提供三种纯 C Loader：
 * 1. `RpBuiltinLoader`（`importlib._bootstrap.BuiltinImporter`）：直接以类对象本身作为
 *    `__loader__`（匹配 CPython `METH_STATIC` / `METH_CLASS` 协议），服务 `PyImport_Inittab`。
 * 2. `RpExtensionLoader`（`importlib._bootstrap_external.ExtensionFileLoader`）：服务显式
 *    冻结 `.so/.pyd`，支持 `name`/`path`/`create_module`/`exec_module`/`is_package`/
 *    `get_filename`/`get_code`/`get_source`/`__eq__`/`__hash__`，未承诺长尾明确报错。
 * 3. `RpCilLoader`（`ryupy.CilModuleLoader`）：服务静态 CIL 回调，提供完整 PEP 451
 *    与比较/哈希协议，无字节码载荷；未承诺长尾明确拒绝。
 */
#ifndef Py_RYUPY_IMPORT_LOADERS_H
#define Py_RYUPY_IMPORT_LOADERS_H

/* --- 公共辅助：名称校验、类型检查与未承诺长尾明确拒绝 ------------------- */

static int
rp_loader_check_name(PyObject *expected_name, PyObject *fullname)
{
    if (!PyUnicode_Check(fullname)) {
        PyErr_Format(PyExc_TypeError, "fullname must be str, not %.200s",
                     Py_TYPE(fullname)->tp_name);
        return -1;
    }
    if (expected_name == NULL) {
        PyErr_SetString(PyExc_ImportError, "loader has been cleared");
        return -1;
    }
    int eq = PyUnicode_Compare(expected_name, fullname);
    if (eq == 0) {
        return 0;
    }
    if (PyErr_Occurred()) {
        return -1;
    }
    PyObject *msg = PyUnicode_FromFormat("loader for %R cannot handle %R",
                                         expected_name, fullname);
    if (msg != NULL) {
        (void)PyErr_SetImportError(msg, fullname, NULL);
        Py_DECREF(msg);
    }
    return -1;
}

static PyObject *
rp_loader_reject_data(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(arg))
{
    PyErr_SetString(PyExc_NotImplementedError,
                    "loader.get_data() is not supported in RyuPy minimal kernel");
    return NULL;
}

static PyObject *
rp_loader_reject_resource_reader(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(fullname))
{
    PyErr_SetString(PyExc_NotImplementedError,
                    "loader.get_resource_reader() requires importlib.readers (deferred beyond C4-1)");
    return NULL;
}

/* --- RpBuiltinLoader（类对象即 __loader__） ------------------------------ */

static PyObject *
rp_builtin_loader_create_module(PyObject *Py_UNUSED(ignored), PyObject *spec)
{
    PyThreadState *tstate = _PyThreadState_GET();
    PyObject *name = PyObject_GetAttr(spec, &_Py_ID(name));
    if (name == NULL) {
        return NULL;
    }
    if (!PyUnicode_Check(name)) {
        PyErr_Format(PyExc_TypeError, "spec.name must be str, not %.200s",
                     Py_TYPE(name)->tp_name);
        Py_DECREF(name);
        return NULL;
    }
    PyObject *mod = create_builtin(tstate, name, spec);
    Py_DECREF(name);
    if (mod == Py_None) {
        Py_DECREF(mod);
        PyObject *spec_name = PyObject_GetAttr(spec, &_Py_ID(name));
        if (spec_name != NULL) {
            PyObject *msg = PyUnicode_FromFormat("%R is not a built-in module", spec_name);
            if (msg != NULL) {
                (void)PyErr_SetImportError(msg, spec_name, NULL);
                Py_DECREF(msg);
            }
            Py_DECREF(spec_name);
        }
        return NULL;
    }
    return mod;
}

static PyObject *
rp_builtin_loader_exec_module(PyObject *Py_UNUSED(ignored), PyObject *mod)
{
    if (exec_builtin_or_dynamic(mod) < 0) {
        return NULL;
    }
    Py_RETURN_NONE;
}

static int
rp_require_builtin_arg(PyObject *fullname)
{
    if (!PyUnicode_Check(fullname)) {
        PyErr_Format(PyExc_TypeError, "fullname must be str, not %.200s",
                     Py_TYPE(fullname)->tp_name);
        return -1;
    }
    if (is_builtin(fullname) == 0) {
        PyObject *msg = PyUnicode_FromFormat("%R is not a built-in module", fullname);
        if (msg != NULL) {
            (void)PyErr_SetImportError(msg, fullname, NULL);
            Py_DECREF(msg);
        }
        return -1;
    }
    return 0;
}

static PyObject *
rp_builtin_loader_is_package(PyObject *Py_UNUSED(cls), PyObject *fullname)
{
    if (rp_require_builtin_arg(fullname) < 0) return NULL;
    Py_RETURN_FALSE;
}

static PyObject *
rp_builtin_loader_get_none(PyObject *Py_UNUSED(cls), PyObject *fullname)
{
    if (rp_require_builtin_arg(fullname) < 0) return NULL;
    Py_RETURN_NONE;
}

static PyMethodDef rp_builtin_loader_methods[] = {
    {"create_module", rp_builtin_loader_create_module, METH_O | METH_STATIC, NULL},
    {"exec_module", rp_builtin_loader_exec_module, METH_O | METH_STATIC, NULL},
    {"is_package", rp_builtin_loader_is_package, METH_O | METH_CLASS, NULL},
    {"get_code", rp_builtin_loader_get_none, METH_O | METH_CLASS, NULL},
    {"get_source", rp_builtin_loader_get_none, METH_O | METH_CLASS, NULL},
    {NULL, NULL, 0, NULL}
};

static PyType_Slot rp_builtin_loader_slots[] = {
    {Py_tp_methods, rp_builtin_loader_methods},
    {0, NULL}
};

static PyType_Spec rp_builtin_loader_type_spec = {
    "importlib._bootstrap.BuiltinImporter",
    sizeof(PyObject),
    0,
    Py_TPFLAGS_DEFAULT,
    rp_builtin_loader_slots,
};

/* --- RpExtensionLoader -------------------------------------------------- */

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
rp_ext_loader_create_module(PyObject *Py_UNUSED(self), PyObject *spec)
{
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
rp_ext_loader_exec_module(PyObject *Py_UNUSED(self), PyObject *mod)
{
    if (exec_builtin_or_dynamic(mod) < 0) {
        return NULL;
    }
    Py_RETURN_NONE;
}

static PyObject *
rp_ext_loader_is_package(RpExtensionLoaderObject *self, PyObject *fullname)
{
    if (rp_loader_check_name(self->name, fullname) < 0) return NULL;
    return PyBool_FromLong(self->is_package_flag);
}

static PyObject *
rp_ext_loader_get_filename(RpExtensionLoaderObject *self, PyObject *fullname)
{
    if (rp_loader_check_name(self->name, fullname) < 0) return NULL;
    return Py_NewRef(self->path ? self->path : Py_None);
}

static PyObject *
rp_ext_loader_get_none(RpExtensionLoaderObject *self, PyObject *fullname)
{
    if (rp_loader_check_name(self->name, fullname) < 0) return NULL;
    Py_RETURN_NONE;
}

static PyGetSetDef rp_ext_loader_getsets[] = {
    {"name", (getter)rp_ext_loader_get_name, NULL, NULL, NULL},
    {"path", (getter)rp_ext_loader_get_path, NULL, NULL, NULL},
    {NULL}
};

static PyMethodDef rp_ext_loader_methods[] = {
    {"create_module", rp_ext_loader_create_module, METH_O, NULL},
    {"exec_module", rp_ext_loader_exec_module, METH_O, NULL},
    {"is_package", (PyCFunction)rp_ext_loader_is_package, METH_O, NULL},
    {"get_filename", (PyCFunction)rp_ext_loader_get_filename, METH_O, NULL},
    {"get_code", (PyCFunction)rp_ext_loader_get_none, METH_O, NULL},
    {"get_source", (PyCFunction)rp_ext_loader_get_none, METH_O, NULL},
    {"get_data", rp_loader_reject_data, METH_O, NULL},
    {"get_resource_reader", rp_loader_reject_resource_reader, METH_O, NULL},
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

/* --- RpCilLoader -------------------------------------------------------- */

typedef struct {
    PyObject_HEAD
    PyObject *name;
    PyObject *origin;
    int has_location;
    int is_package_flag;
} RpCilLoaderObject;

static int
rp_cil_loader_traverse(RpCilLoaderObject *self, visitproc visit, void *arg)
{
    Py_VISIT(Py_TYPE(self));
    Py_VISIT(self->name);
    Py_VISIT(self->origin);
    return 0;
}

static int
rp_cil_loader_clear(RpCilLoaderObject *self)
{
    Py_CLEAR(self->name);
    Py_CLEAR(self->origin);
    return 0;
}

static void
rp_cil_loader_dealloc(RpCilLoaderObject *self)
{
    PyTypeObject *tp = Py_TYPE(self);
    PyObject_GC_UnTrack(self);
    (void)rp_cil_loader_clear(self);
    tp->tp_free((PyObject *)self);
    Py_DECREF(tp);
}

static PyObject *
rp_cil_loader_get_name(RpCilLoaderObject *self, void *Py_UNUSED(ctx))
{
    return Py_NewRef(self->name ? self->name : Py_None);
}

static PyObject *
rp_cil_loader_get_origin(RpCilLoaderObject *self, void *Py_UNUSED(ctx))
{
    return Py_NewRef(self->origin ? self->origin : Py_None);
}

static PyObject *
rp_cil_loader_repr(RpCilLoaderObject *self)
{
    return PyUnicode_FromFormat("<RyuPyCilLoader(%R)>", self->name ? self->name : Py_None);
}

static Py_hash_t
rp_cil_loader_hash(RpCilLoaderObject *self)
{
    Py_hash_t h1 = PyObject_Hash(self->name ? self->name : Py_None);
    if (h1 == -1) return -1;
    Py_hash_t h2 = PyObject_Hash(self->origin ? self->origin : Py_None);
    if (h2 == -1) return -1;
    Py_hash_t h = h1 ^ h2;
    return h == -1 ? -2 : h;
}

static PyObject *
rp_cil_loader_richcompare(RpCilLoaderObject *self, PyObject *other, int op)
{
    if ((op != Py_EQ && op != Py_NE) || !Py_IS_TYPE(other, Py_TYPE(self))) {
        Py_RETURN_NOTIMPLEMENTED;
    }
    RpCilLoaderObject *rhs = (RpCilLoaderObject *)other;
    PyObject *a1 = self->name ? self->name : Py_None;
    PyObject *b1 = rhs->name ? rhs->name : Py_None;
    PyObject *a2 = self->origin ? self->origin : Py_None;
    PyObject *b2 = rhs->origin ? rhs->origin : Py_None;
    int eq1 = PyObject_RichCompareBool(a1, b1, Py_EQ);
    if (eq1 < 0) return NULL;
    int eq2 = eq1 ? PyObject_RichCompareBool(a2, b2, Py_EQ) : 0;
    if (eq2 < 0) return NULL;
    int eq = eq1 && eq2 && (self->has_location == rhs->has_location) &&
             (self->is_package_flag == rhs->is_package_flag);
    return PyBool_FromLong(op == Py_EQ ? eq : !eq);
}

static PyObject *
rp_cil_loader_create_module(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(spec))
{
    /* 按 PEP 451：返回 None 表示由调用方采用标准模块创建。 */
    Py_RETURN_NONE;
}

static PyObject *
rp_cil_loader_exec_module(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(mod))
{
    PyErr_SetString(PyExc_NotImplementedError,
                    "RyuPyCilLoader.exec_module cannot be invoked outside kernel import transaction");
    return NULL;
}

static PyObject *
rp_cil_loader_is_package(RpCilLoaderObject *self, PyObject *fullname)
{
    if (rp_loader_check_name(self->name, fullname) < 0) return NULL;
    return PyBool_FromLong(self->is_package_flag);
}

static PyObject *
rp_cil_loader_get_filename(RpCilLoaderObject *self, PyObject *fullname)
{
    if (rp_loader_check_name(self->name, fullname) < 0) return NULL;
    if (self->has_location && self->origin != NULL && self->origin != Py_None) {
        return Py_NewRef(self->origin);
    }
    PyObject *msg = PyUnicode_FromFormat("CIL module %R has no file location", fullname);
    if (msg != NULL) {
        (void)PyErr_SetImportError(msg, fullname, NULL);
        Py_DECREF(msg);
    }
    return NULL;
}

static PyObject *
rp_cil_loader_get_none(RpCilLoaderObject *self, PyObject *fullname)
{
    if (rp_loader_check_name(self->name, fullname) < 0) return NULL;
    Py_RETURN_NONE;
}

static PyGetSetDef rp_cil_loader_getsets[] = {
    {"name", (getter)rp_cil_loader_get_name, NULL, NULL, NULL},
    {"origin", (getter)rp_cil_loader_get_origin, NULL, NULL, NULL},
    {NULL}
};

static PyMethodDef rp_cil_loader_methods[] = {
    {"create_module", rp_cil_loader_create_module, METH_O, NULL},
    {"exec_module", rp_cil_loader_exec_module, METH_O, NULL},
    {"is_package", (PyCFunction)rp_cil_loader_is_package, METH_O, NULL},
    {"get_filename", (PyCFunction)rp_cil_loader_get_filename, METH_O, NULL},
    {"get_code", (PyCFunction)rp_cil_loader_get_none, METH_O, NULL},
    {"get_source", (PyCFunction)rp_cil_loader_get_none, METH_O, NULL},
    {"get_data", rp_loader_reject_data, METH_O, NULL},
    {"get_resource_reader", rp_loader_reject_resource_reader, METH_O, NULL},
    {NULL, NULL, 0, NULL}
};

static PyType_Slot rp_cil_loader_slots[] = {
    {Py_tp_dealloc, rp_cil_loader_dealloc},
    {Py_tp_traverse, rp_cil_loader_traverse},
    {Py_tp_clear, rp_cil_loader_clear},
    {Py_tp_repr, rp_cil_loader_repr},
    {Py_tp_hash, rp_cil_loader_hash},
    {Py_tp_richcompare, rp_cil_loader_richcompare},
    {Py_tp_getset, rp_cil_loader_getsets},
    {Py_tp_methods, rp_cil_loader_methods},
    {0, NULL}
};

static PyType_Spec rp_cil_loader_type_spec = {
    "ryupy.CilModuleLoader",
    sizeof(RpCilLoaderObject),
    0,
    Py_TPFLAGS_DEFAULT | Py_TPFLAGS_HAVE_GC,
    rp_cil_loader_slots,
};

/* --- 类型初始化与实例构造辅助 ----------------------------------------- */

static int
rp_kernel_ensure_import_types(RpKernelImportState *state)
{
    if (state->spec_type == NULL) {
        state->spec_type = (PyTypeObject *)PyType_FromSpec(&rp_spec_type_spec);
        if (state->spec_type == NULL) return -1;
    }
    if (state->builtin_loader_type == NULL) {
        state->builtin_loader_type = (PyTypeObject *)PyType_FromSpec(&rp_builtin_loader_type_spec);
        if (state->builtin_loader_type == NULL) return -1;
    }
    if (state->extension_loader_type == NULL) {
        state->extension_loader_type = (PyTypeObject *)PyType_FromSpec(&rp_ext_loader_type_spec);
        if (state->extension_loader_type == NULL) return -1;
    }
    if (state->cil_loader_type == NULL) {
        state->cil_loader_type = (PyTypeObject *)PyType_FromSpec(&rp_cil_loader_type_spec);
        if (state->cil_loader_type == NULL) return -1;
    }
    return 0;
}

static PyObject *
rp_kernel_new_extension_loader(RpKernelImportState *state, PyObject *name,
                               PyObject *path, int is_package)
{
    RpExtensionLoaderObject *loader = PyObject_GC_New(
        RpExtensionLoaderObject, state->extension_loader_type);
    if (loader == NULL) return NULL;
    loader->name = Py_NewRef(name);
    loader->path = Py_NewRef(path);
    loader->is_package_flag = is_package ? 1 : 0;
    PyObject_GC_Track((PyObject *)loader);
    return (PyObject *)loader;
}

static PyObject *
rp_kernel_new_cil_loader(RpKernelImportState *state, PyObject *name,
                         PyObject *origin, int has_location, int is_package)
{
    RpCilLoaderObject *loader = PyObject_GC_New(RpCilLoaderObject, state->cil_loader_type);
    if (loader == NULL) return NULL;
    loader->name = Py_NewRef(name);
    loader->origin = Py_NewRef(origin ? origin : Py_None);
    loader->has_location = has_location ? 1 : 0;
    loader->is_package_flag = is_package ? 1 : 0;
    PyObject_GC_Track((PyObject *)loader);
    return (PyObject *)loader;
}

#endif /* Py_RYUPY_IMPORT_LOADERS_H */
