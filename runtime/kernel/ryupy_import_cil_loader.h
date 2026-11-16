/* RyuPy 严格零字节码内核 C4-1 纯 C CilModuleLoader 实现。
 *
 * 本头文件由锚点机制注入官方 CPython 3.12.13 的 `Python/ryupy_import_cil_loader.h`，
 * 由 `Python/ryupy_import_loaders.h` 包含，提供 `ryupy.CilModuleLoader`：
 * 1. 作为内核本地静态 CIL 导入事务载体，全部核心与辅助方法统一使用
 *    `METH_VARARGS | METH_KEYWORDS` 实例绑定与 `PyArg_ParseTupleAndKeywords`，支持
 *    `create_module(spec)`、`exec_module(module)`、`is_package(fullname)`、`get_filename(fullname)`、
 *    `get_code(fullname)`、`get_source(fullname)`、`get_data(path)`、`get_resource_reader(fullname)`。
 * 2. 不在 Loader 上复制静态 CIL 回调执行体；直接调用 `exec_module` 仍显式报错，保证单事务不变量。
 */
#ifndef Py_RYUPY_IMPORT_CIL_LOADER_H
#define Py_RYUPY_IMPORT_CIL_LOADER_H

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
rp_cil_loader_create_module(PyObject *Py_UNUSED(self), PyObject *args, PyObject *kwds)
{
    if (rp_loader_parse_kw(args, kwds, "O:create_module", rp_kw_spec, 0, NULL) == NULL) {
        return NULL;
    }
    /* 按 PEP 451：返回 None 表示由调用方采用标准模块创建。 */
    Py_RETURN_NONE;
}

static PyObject *
rp_cil_loader_exec_module(PyObject *Py_UNUSED(self), PyObject *args, PyObject *kwds)
{
    if (rp_loader_parse_kw(args, kwds, "O:exec_module", rp_kw_module, 0, NULL) == NULL) {
        return NULL;
    }
    PyErr_SetString(PyExc_NotImplementedError,
                    "RyuPyCilLoader.exec_module cannot be invoked outside kernel import transaction");
    return NULL;
}

static PyObject *
rp_cil_loader_is_package(RpCilLoaderObject *self, PyObject *args, PyObject *kwds)
{
    PyObject *fullname = rp_loader_parse_kw(args, kwds, "O:is_package", rp_kw_fullname, 0, NULL);
    if (fullname == NULL || rp_loader_check_name(self->name, fullname) < 0) {
        return NULL;
    }
    return PyBool_FromLong(self->is_package_flag);
}

static PyObject *
rp_cil_loader_get_filename(RpCilLoaderObject *self, PyObject *args, PyObject *kwds)
{
    PyObject *fullname = rp_loader_parse_kw(args, kwds, "O:get_filename", rp_kw_fullname, 0, NULL);
    if (fullname == NULL || rp_loader_check_name(self->name, fullname) < 0) {
        return NULL;
    }
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
rp_cil_loader_return_none_checked(RpCilLoaderObject *self, PyObject *args, PyObject *kwds,
                                  const char *fmt)
{
    PyObject *fullname = rp_loader_parse_kw(args, kwds, fmt, rp_kw_fullname, 0, NULL);
    if (fullname == NULL || rp_loader_check_name(self->name, fullname) < 0) {
        return NULL;
    }
    Py_RETURN_NONE;
}

static PyObject *
rp_cil_loader_get_code(RpCilLoaderObject *self, PyObject *args, PyObject *kwds)
{
    return rp_cil_loader_return_none_checked(self, args, kwds, "O:get_code");
}

static PyObject *
rp_cil_loader_get_source(RpCilLoaderObject *self, PyObject *args, PyObject *kwds)
{
    return rp_cil_loader_return_none_checked(self, args, kwds, "O:get_source");
}

static PyObject *
rp_cil_loader_get_resource_reader(PyObject *Py_UNUSED(self), PyObject *args, PyObject *kwds)
{
    return rp_loader_reject_resource_reader_impl(args, kwds, "O:get_resource_reader", rp_kw_fullname, 0);
}

static PyGetSetDef rp_cil_loader_getsets[] = {
    {"name", (getter)rp_cil_loader_get_name, NULL, NULL, NULL},
    {"origin", (getter)rp_cil_loader_get_origin, NULL, NULL, NULL},
    {NULL}
};

static PyMethodDef rp_cil_loader_methods[] = {
    {"create_module", (PyCFunction)(void (*)(void))rp_cil_loader_create_module, METH_VARARGS | METH_KEYWORDS, NULL},
    {"exec_module", (PyCFunction)(void (*)(void))rp_cil_loader_exec_module, METH_VARARGS | METH_KEYWORDS, NULL},
    {"is_package", (PyCFunction)(void (*)(void))rp_cil_loader_is_package, METH_VARARGS | METH_KEYWORDS, NULL},
    {"get_filename", (PyCFunction)(void (*)(void))rp_cil_loader_get_filename, METH_VARARGS | METH_KEYWORDS, NULL},
    {"get_code", (PyCFunction)(void (*)(void))rp_cil_loader_get_code, METH_VARARGS | METH_KEYWORDS, NULL},
    {"get_source", (PyCFunction)(void (*)(void))rp_cil_loader_get_source, METH_VARARGS | METH_KEYWORDS, NULL},
    {"get_data", (PyCFunction)(void (*)(void))rp_loader_reject_data, METH_VARARGS | METH_KEYWORDS, NULL},
    {"get_resource_reader", (PyCFunction)(void (*)(void))rp_cil_loader_get_resource_reader, METH_VARARGS | METH_KEYWORDS, NULL},
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

#endif /* Py_RYUPY_IMPORT_CIL_LOADER_H */
