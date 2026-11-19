/* RyuPy 严格零字节码内核 C4-1 纯 C ModuleSpec、属性初始化与模块 repr 实现。
 *
 * 本头文件由锚点机制注入官方 CPython 3.12.13 的 `Python/ryupy_module_spec.h`，
 * 由 `Python/import.c` 在 `import_find_and_load` 之前包含，负责：
 * 1. 定义纯 C 堆类型 `RpModuleSpec`（`importlib._bootstrap.ModuleSpec`），完整提供
 *    `name`、`loader`、`origin`、`loader_state`、`submodule_search_locations`、
 *    `_uninitialized_submodules`、`_initializing`、`has_location`、`cached`、只读属性
 *    `parent`、`__repr__` 与 `__eq__`。
 * 2. 提供 `rp_kernel_init_module_attrs`，对齐 CPython `_bootstrap._init_module_attrs`
 *    补全 `__name__`、`__loader__`、`__package__`、`__spec__`、`__path__` 与 `__file__`。
 * 3. 提供 `_PyImport_RyuPyModuleRepr`，在 `_install_importlib=0` 时直接由纯 C 路径生成
 *    `<module 'name' (built-in)>`、`<module 'name' from '...'>`、`<module 'name'>` 等标准 repr。
 */
#ifndef Py_RYUPY_MODULE_SPEC_H
#define Py_RYUPY_MODULE_SPEC_H

typedef struct {
    PyObject_HEAD
    PyObject *name;
    PyObject *loader;
    PyObject *origin;
    PyObject *loader_state;
    PyObject *submodule_search_locations;
    PyObject *uninitialized_submodules;
    PyObject *cached;
    int initializing;
    int has_location;
} RpModuleSpecObject;

static int
rp_spec_traverse(RpModuleSpecObject *self, visitproc visit, void *arg)
{
    Py_VISIT(Py_TYPE(self));
    Py_VISIT(self->name);
    Py_VISIT(self->loader);
    Py_VISIT(self->origin);
    Py_VISIT(self->loader_state);
    Py_VISIT(self->submodule_search_locations);
    Py_VISIT(self->uninitialized_submodules);
    Py_VISIT(self->cached);
    return 0;
}

static int
rp_spec_clear(RpModuleSpecObject *self)
{
    Py_CLEAR(self->name);
    Py_CLEAR(self->loader);
    Py_CLEAR(self->origin);
    Py_CLEAR(self->loader_state);
    Py_CLEAR(self->submodule_search_locations);
    Py_CLEAR(self->uninitialized_submodules);
    Py_CLEAR(self->cached);
    return 0;
}

static void
rp_spec_dealloc(RpModuleSpecObject *self)
{
    PyTypeObject *tp = Py_TYPE(self);
    PyObject_GC_UnTrack(self);
    (void)rp_spec_clear(self);
    tp->tp_free((PyObject *)self);
    Py_DECREF(tp);
}

#define RP_SPEC_OBJ_GETSET(field)                                              \
    static PyObject *                                                          \
    rp_spec_get_##field(RpModuleSpecObject *self, void *Py_UNUSED(ctx))        \
    {                                                                          \
        return Py_NewRef(self->field ? self->field : Py_None);                 \
    }                                                                          \
    static int                                                                 \
    rp_spec_set_##field(RpModuleSpecObject *self, PyObject *val, void *Py_UNUSED(ctx)) \
    {                                                                          \
        if (val == NULL) {                                                     \
            PyErr_SetString(PyExc_AttributeError, "cannot delete attribute");  \
            return -1;                                                         \
        }                                                                      \
        Py_SETREF(self->field, Py_NewRef(val));                                \
        return 0;                                                              \
    }

RP_SPEC_OBJ_GETSET(name)
RP_SPEC_OBJ_GETSET(loader)
RP_SPEC_OBJ_GETSET(origin)
RP_SPEC_OBJ_GETSET(loader_state)
RP_SPEC_OBJ_GETSET(submodule_search_locations)
RP_SPEC_OBJ_GETSET(uninitialized_submodules)
RP_SPEC_OBJ_GETSET(cached)
#undef RP_SPEC_OBJ_GETSET

static PyObject *
rp_spec_get_has_location(RpModuleSpecObject *self, void *Py_UNUSED(ctx))
{
    return PyBool_FromLong(self->has_location);
}

static int
rp_spec_set_has_location(RpModuleSpecObject *self, PyObject *val, void *Py_UNUSED(ctx))
{
    if (val == NULL) {
        PyErr_SetString(PyExc_AttributeError, "cannot delete 'has_location'");
        return -1;
    }
    int flag = PyObject_IsTrue(val);
    if (flag < 0) {
        return -1;
    }
    self->has_location = flag;
    return 0;
}

static PyObject *
rp_spec_get_initializing(RpModuleSpecObject *self, void *Py_UNUSED(ctx))
{
    return PyBool_FromLong(self->initializing);
}

static int
rp_spec_set_initializing(RpModuleSpecObject *self, PyObject *val, void *Py_UNUSED(ctx))
{
    if (val == NULL) {
        PyErr_SetString(PyExc_AttributeError, "cannot delete '_initializing'");
        return -1;
    }
    int flag = PyObject_IsTrue(val);
    if (flag < 0) {
        return -1;
    }
    self->initializing = flag;
    return 0;
}

static PyObject *
rp_spec_get_parent(RpModuleSpecObject *self, void *Py_UNUSED(ctx))
{
    if (self->submodule_search_locations != NULL &&
        self->submodule_search_locations != Py_None)
    {
        return Py_NewRef(self->name ? self->name : Py_None);
    }
    if (self->name == NULL || !PyUnicode_Check(self->name)) {
        Py_RETURN_NONE;
    }
    Py_ssize_t len = PyUnicode_GET_LENGTH(self->name);
    Py_ssize_t dot = PyUnicode_FindChar(self->name, '.', 0, len, -1);
    if (dot == -2) {
        return NULL;
    }
    if (dot < 0) {
        return PyUnicode_FromString("");
    }
    return PyUnicode_Substring(self->name, 0, dot);
}

static PyObject *
rp_spec_repr(RpModuleSpecObject *self)
{
    PyObject *parts = PyList_New(0);
    if (parts == NULL) {
        return NULL;
    }
    PyObject *p0 = PyUnicode_FromFormat("name=%R", self->name ? self->name : Py_None);
    PyObject *p1 = PyUnicode_FromFormat("loader=%R", self->loader ? self->loader : Py_None);
    if (p0 == NULL || p1 == NULL ||
        PyList_Append(parts, p0) < 0 || PyList_Append(parts, p1) < 0)
    {
        Py_XDECREF(p0);
        Py_XDECREF(p1);
        Py_DECREF(parts);
        return NULL;
    }
    Py_DECREF(p0);
    Py_DECREF(p1);

    if (self->origin != NULL && self->origin != Py_None) {
        PyObject *p2 = PyUnicode_FromFormat("origin=%R", self->origin);
        if (p2 == NULL || PyList_Append(parts, p2) < 0) {
            Py_XDECREF(p2);
            Py_DECREF(parts);
            return NULL;
        }
        Py_DECREF(p2);
    }
    if (self->submodule_search_locations != NULL &&
        self->submodule_search_locations != Py_None)
    {
        PyObject *p3 = PyUnicode_FromFormat("submodule_search_locations=%S",
                                            self->submodule_search_locations);
        if (p3 == NULL || PyList_Append(parts, p3) < 0) {
            Py_XDECREF(p3);
            Py_DECREF(parts);
            return NULL;
        }
        Py_DECREF(p3);
    }
    PyObject *sep = PyUnicode_FromString(", ");
    PyObject *joined = sep ? PyUnicode_Join(sep, parts) : NULL;
    Py_XDECREF(sep);
    Py_DECREF(parts);
    if (joined == NULL) {
        return NULL;
    }
    PyObject *res = PyUnicode_FromFormat("ModuleSpec(%S)", joined);
    Py_DECREF(joined);
    return res;
}

static int
rp_eq_attr(PyObject *a, PyObject *other, const char *attr)
{
    PyObject *b = PyObject_GetAttrString(other, attr);
    if (b == NULL) {
        if (PyErr_ExceptionMatches(PyExc_AttributeError)) {
            PyErr_Clear();
            return -2; /* NotImplemented */
        }
        return -1;
    }
    int eq = PyObject_RichCompareBool(a ? a : Py_None, b, Py_EQ);
    Py_DECREF(b);
    return eq;
}

static PyObject *
rp_spec_richcompare(RpModuleSpecObject *self, PyObject *other, int op)
{
    if (op != Py_EQ && op != Py_NE) {
        Py_RETURN_NOTIMPLEMENTED;
    }
    PyObject *self_cached = rp_spec_get_cached(self, NULL);
    if (self_cached == NULL) {
        return NULL;
    }
    PyObject *self_has = PyBool_FromLong(self->has_location);
    if (self_has == NULL) {
        Py_DECREF(self_cached);
        return NULL;
    }
    PyObject *checks[6] = {
        self->name,
        self->loader,
        self->origin,
        self->submodule_search_locations,
        self_cached,
        self_has,
    };
    const char *names[6] = {
        "name", "loader", "origin", "submodule_search_locations", "cached", "has_location"
    };

    int all_eq = 1;
    for (int i = 0; i < 6; i++) {
        int r = rp_eq_attr(checks[i], other, names[i]);
        if (r == -1) {
            Py_DECREF(self_has);
            Py_DECREF(self_cached);
            return NULL;
        }
        if (r == -2) {
            Py_DECREF(self_has);
            Py_DECREF(self_cached);
            Py_RETURN_NOTIMPLEMENTED;
        }
        if (r == 0) {
            all_eq = 0;
            break;
        }
    }
    Py_DECREF(self_has);
    Py_DECREF(self_cached);
    return PyBool_FromLong(op == Py_EQ ? all_eq : !all_eq);
}

static PyGetSetDef rp_spec_getsets[] = {
    {"name", (getter)rp_spec_get_name, (setter)rp_spec_set_name, NULL, NULL},
    {"loader", (getter)rp_spec_get_loader, (setter)rp_spec_set_loader, NULL, NULL},
    {"origin", (getter)rp_spec_get_origin, (setter)rp_spec_set_origin, NULL, NULL},
    {"loader_state", (getter)rp_spec_get_loader_state, (setter)rp_spec_set_loader_state, NULL, NULL},
    {"submodule_search_locations", (getter)rp_spec_get_submodule_search_locations,
     (setter)rp_spec_set_submodule_search_locations, NULL, NULL},
    {"_uninitialized_submodules", (getter)rp_spec_get_uninitialized_submodules,
     (setter)rp_spec_set_uninitialized_submodules, NULL, NULL},
    {"_initializing", (getter)rp_spec_get_initializing, (setter)rp_spec_set_initializing, NULL, NULL},
    {"has_location", (getter)rp_spec_get_has_location, (setter)rp_spec_set_has_location, NULL, NULL},
    {"cached", (getter)rp_spec_get_cached, (setter)rp_spec_set_cached, NULL, NULL},
    {"parent", (getter)rp_spec_get_parent, NULL, NULL, NULL},
    {NULL}
};

static PyType_Slot rp_spec_slots[] = {
    {Py_tp_dealloc, rp_spec_dealloc},
    {Py_tp_traverse, rp_spec_traverse},
    {Py_tp_clear, rp_spec_clear},
    {Py_tp_repr, rp_spec_repr},
    {Py_tp_richcompare, rp_spec_richcompare},
    {Py_tp_getset, rp_spec_getsets},
    {0, NULL}
};

static PyType_Spec rp_spec_type_spec = {
    "importlib._bootstrap.ModuleSpec",
    sizeof(RpModuleSpecObject),
    0,
    Py_TPFLAGS_DEFAULT | Py_TPFLAGS_HAVE_GC,
    rp_spec_slots,
};

static PyObject *
rp_extract_parent_dir(PyObject *origin)
{
    Py_ssize_t len = PyUnicode_GET_LENGTH(origin);
    Py_ssize_t sep_pos = PyUnicode_FindChar(origin, '/', 0, len, -1);
#ifdef MS_WINDOWS
    Py_ssize_t bsep = PyUnicode_FindChar(origin, '\\', 0, len, -1);
    if (bsep > sep_pos) {
        sep_pos = bsep;
    }
#endif
    if (sep_pos < 0) {
        return Py_NewRef(Py_None);
    }
    if (sep_pos == 0) {
        /* 根目录 `/__init__.so`（或 `\\__init__.pyd`）保留根分隔符。 */
        return PyUnicode_Substring(origin, 0, 1);
    }
#ifdef MS_WINDOWS
    if (sep_pos == 2 && len >= 3) {
        Py_UCS4 c0 = PyUnicode_READ_CHAR(origin, 0);
        Py_UCS4 c1 = PyUnicode_READ_CHAR(origin, 1);
        if (((c0 >= 'A' && c0 <= 'Z') || (c0 >= 'a' && c0 <= 'z')) && c1 == ':') {
            /* Windows 盘符根目录 `C:\__init__.pyd` 保留 `C:\`。 */
            return PyUnicode_Substring(origin, 0, 3);
        }
    }
#endif
    return PyUnicode_Substring(origin, 0, sep_pos);
}

static PyObject *
rp_kernel_new_module_spec(RpKernelImportState *state, PyObject *name, PyObject *loader,
                          PyObject *origin, int has_location, int is_package,
                          PyObject *package_dir)
{
    if (state->spec_type == NULL) {
        PyErr_SetString(PyExc_RuntimeError, "ModuleSpec type not initialized");
        return NULL;
    }
    RpModuleSpecObject *spec = PyObject_GC_New(RpModuleSpecObject, state->spec_type);
    if (spec == NULL) {
        return NULL;
    }
    spec->name = Py_NewRef(name);
    spec->loader = Py_NewRef(loader ? loader : Py_None);
    spec->origin = Py_NewRef(origin ? origin : Py_None);
    spec->loader_state = Py_NewRef(Py_None);
    spec->cached = Py_NewRef(Py_None);
    spec->initializing = 0;
    spec->has_location = has_location ? 1 : 0;
    spec->submodule_search_locations = NULL;
    spec->uninitialized_submodules = PyList_New(0);
    if (spec->uninitialized_submodules == NULL) {
        Py_DECREF(spec);
        return NULL;
    }
    if (is_package) {
        PyObject *locs = PyList_New(0);
        if (locs == NULL) {
            Py_DECREF(spec);
            return NULL;
        }
        if (package_dir != NULL && package_dir != Py_None && PyUnicode_Check(package_dir) &&
            PyUnicode_GET_LENGTH(package_dir) > 0)
        {
            /* 显式包搜索/资源目录优先，绝不污染 origin / has_location / __file__。 */
            if (PyList_Append(locs, package_dir) < 0) {
                Py_DECREF(locs);
                Py_DECREF(spec);
                return NULL;
            }
        }
        else if (has_location && origin != NULL && PyUnicode_Check(origin)) {
            PyObject *dir = rp_extract_parent_dir(origin);
            if (dir == NULL || (dir != Py_None && PyList_Append(locs, dir) < 0)) {
                Py_XDECREF(dir);
                Py_DECREF(locs);
                Py_DECREF(spec);
                return NULL;
            }
            Py_DECREF(dir);
        }
        spec->submodule_search_locations = locs;
    }
    else {
        spec->submodule_search_locations = Py_NewRef(Py_None);
    }
    PyObject_GC_Track((PyObject *)spec);
    return (PyObject *)spec;
}

static int
rp_set_attr_ignore_attrerror(PyObject *obj, PyObject *attr, PyObject *value)
{
    if (PyObject_SetAttr(obj, attr, value) < 0) {
        if (PyErr_ExceptionMatches(PyExc_AttributeError)) {
            PyErr_Clear();
            return 0;
        }
        return -1;
    }
    return 0;
}

static int
rp_lookup_attr_str(PyObject *obj, const char *name, PyObject **out)
{
    PyObject *key = PyUnicode_FromString(name);
    if (key == NULL) return -1;
    int rc = _PyObject_LookupAttr(obj, key, out);
    Py_DECREF(key);
    return rc;
}

static int
rp_should_fill_attr(PyObject *obj, PyObject *attr)
{
    PyObject *cur = NULL;
    int rc = _PyObject_LookupAttr(obj, attr, &cur);
    if (rc < 0) return -1;
    if (cur == NULL) return 1;
    int is_none = (cur == Py_None);
    Py_DECREF(cur);
    return is_none ? 1 : 0;
}

/* 对齐 importlib._bootstrap._init_module_attrs(spec, module, override=False)。 */
static int
rp_kernel_init_module_attrs(PyObject *spec_obj, PyObject *module)
{
    RpModuleSpecObject *spec = (RpModuleSpecObject *)spec_obj;
    int r = rp_should_fill_attr(module, &_Py_ID(__name__));
    if (r < 0) return -1;
    if (r > 0 && rp_set_attr_ignore_attrerror(module, &_Py_ID(__name__), spec->name) < 0) {
        return -1;
    }

    r = rp_should_fill_attr(module, &_Py_ID(__loader__));
    if (r < 0) return -1;
    if (r > 0 && rp_set_attr_ignore_attrerror(module, &_Py_ID(__loader__), spec->loader) < 0) {
        return -1;
    }

    r = rp_should_fill_attr(module, &_Py_ID(__package__));
    if (r < 0) return -1;
    if (r > 0) {
        PyObject *parent = rp_spec_get_parent(spec, NULL);
        if (parent == NULL) return -1;
        int rc = rp_set_attr_ignore_attrerror(module, &_Py_ID(__package__), parent);
        Py_DECREF(parent);
        if (rc < 0) return -1;
    }

    if (rp_set_attr_ignore_attrerror(module, &_Py_ID(__spec__), spec_obj) < 0) {
        return -1;
    }

    if (spec->submodule_search_locations != NULL &&
        spec->submodule_search_locations != Py_None)
    {
        r = rp_should_fill_attr(module, &_Py_ID(__path__));
        if (r < 0) return -1;
        if (r > 0 &&
            rp_set_attr_ignore_attrerror(module, &_Py_ID(__path__),
                                         spec->submodule_search_locations) < 0)
        {
            return -1;
        }
    }

    if (spec->has_location && spec->origin != NULL && spec->origin != Py_None) {
        r = rp_should_fill_attr(module, &_Py_ID(__file__));
        if (r < 0) return -1;
        if (r > 0 && rp_set_attr_ignore_attrerror(module, &_Py_ID(__file__), spec->origin) < 0) {
            return -1;
        }
        PyObject *cached_val = rp_spec_get_cached(spec, NULL);
        if (cached_val == NULL) return -1;
        if (cached_val != Py_None) {
            PyObject *cached_key = PyUnicode_FromString("__cached__");
            int set_rc = 0;
            r = cached_key ? rp_should_fill_attr(module, cached_key) : -1;
            if (r > 0) set_rc = rp_set_attr_ignore_attrerror(module, cached_key, cached_val);
            Py_XDECREF(cached_key);
            if (r < 0 || set_rc < 0) {
                Py_DECREF(cached_val);
                return -1;
            }
        }
        Py_DECREF(cached_val);
    }
    return 0;
}

static PyObject *
rp_module_repr_from_spec(PyObject *spec)
{
    PyObject *name = NULL, *origin = NULL, *loader = NULL, *has_loc_obj = NULL;
    if (_PyObject_LookupAttr(spec, &_Py_ID(name), &name) < 0) goto error;
    if (name == NULL || name == Py_None) {
        Py_XDECREF(name);
        name = PyUnicode_FromString("?");
        if (name == NULL) goto error;
    }
    if (_PyObject_LookupAttr(spec, &_Py_ID(origin), &origin) < 0) goto error;
    if (origin == NULL || origin == Py_None) {
        if (rp_lookup_attr_str(spec, "loader", &loader) < 0) goto error;
        PyObject *res = (loader == NULL || loader == Py_None)
            ? PyUnicode_FromFormat("<module %R>", name)
            : PyUnicode_FromFormat("<module %R (%R)>", name, loader);
        Py_XDECREF(name);
        Py_XDECREF(origin);
        Py_XDECREF(loader);
        return res;
    }
    if (rp_lookup_attr_str(spec, "has_location", &has_loc_obj) < 0) goto error;
    int has_loc = 0;
    if (has_loc_obj != NULL) {
        has_loc = PyObject_IsTrue(has_loc_obj);
        if (has_loc < 0) goto error;
    }
    PyObject *res = has_loc
        ? PyUnicode_FromFormat("<module %R from %R>", name, origin)
        : PyUnicode_FromFormat("<module %R (%S)>", name, origin);
    Py_XDECREF(name);
    Py_XDECREF(origin);
    Py_XDECREF(has_loc_obj);
    return res;

error:
    Py_XDECREF(name);
    Py_XDECREF(origin);
    Py_XDECREF(loader);
    Py_XDECREF(has_loc_obj);
    return NULL;
}

/* 纯 C 实现 ModuleType.__repr__，完整覆盖有 __spec__ 与无 __spec__ 兜底分支。 */
PyObject *
_PyImport_RyuPyModuleRepr(PyObject *module)
{
    PyObject *spec = NULL;
    if (_PyObject_LookupAttr(module, &_Py_ID(__spec__), &spec) < 0) {
        return NULL;
    }
    if (spec != NULL && spec != Py_None) {
        PyObject *res = rp_module_repr_from_spec(spec);
        Py_DECREF(spec);
        return res;
    }
    Py_XDECREF(spec);

    PyObject *loader = NULL, *name = NULL, *filename = NULL;
    if (_PyObject_LookupAttr(module, &_Py_ID(__loader__), &loader) < 0) goto error;
    if (_PyObject_LookupAttr(module, &_Py_ID(__name__), &name) < 0) goto error;
    if (name == NULL) {
        name = PyUnicode_FromString("?");
        if (name == NULL) goto error;
    }
    if (_PyObject_LookupAttr(module, &_Py_ID(__file__), &filename) < 0) goto error;
    PyObject *res;
    if (filename != NULL) {
        res = PyUnicode_FromFormat("<module %R from %R>", name, filename);
    }
    else if (loader == NULL || loader == Py_None) {
        res = PyUnicode_FromFormat("<module %R>", name);
    }
    else {
        res = PyUnicode_FromFormat("<module %R (%R)>", name, loader);
    }
    Py_XDECREF(loader);
    Py_XDECREF(name);
    Py_XDECREF(filename);
    return res;

error:
    Py_XDECREF(loader);
    Py_XDECREF(name);
    Py_XDECREF(filename);
    return NULL;
}

#endif /* Py_RYUPY_MODULE_SPEC_H */
