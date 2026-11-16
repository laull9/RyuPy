/* ADR0220：编译模块静态 finder/loader。create_module 复用 Runtime 单点初始化，
 * exec_module 不执行正文；真实 CPython ModuleSpec 仍属于现有兼容 provider 执行域。 */
typedef struct {
    PyObject_HEAD
    PyObject *bootstrap, *loader_type;
} NativeImportHook;
static PyTypeObject *native_finder_type, *native_loader_type;
static PyObject *native_installed_finder;

static int import_hook_traverse(NativeImportHook *self, visitproc visit, void *arg) {
    Py_VISIT(Py_TYPE(self)); Py_VISIT(self->bootstrap); Py_VISIT(self->loader_type); return 0;
}
static int import_hook_clear(NativeImportHook *self) {
    Py_CLEAR(self->bootstrap); Py_CLEAR(self->loader_type); return 0;
}
static void import_hook_dealloc(NativeImportHook *self) {
    PyObject_GC_UnTrack(self); import_hook_clear(self);
    PyTypeObject *type = Py_TYPE(self); type->tp_free((PyObject *)self); Py_DECREF(type);
}
static PyObject *import_hook_new(PyTypeObject *type, PyObject *args, PyObject *kwargs) {
    if (PyTuple_GET_SIZE(args) || (kwargs && PyDict_Size(kwargs))) {
        PyErr_SetString(PyExc_TypeError, "native import hooks take no arguments"); return NULL;
    }
    return type->tp_alloc(type, 0);
}
static PyObject *native_loader_create(PyObject *self, PyObject *spec) {
    (void)self;
    PyObject *name = PyObject_GetAttrString(spec, "name");
    if (!name) return NULL;
    PyObject *module = dispatch(-1, 21, name, NULL, NULL, 0, NULL);
    Py_DECREF(name); return module;
}
static PyObject *native_loader_exec(PyObject *self, PyObject *module) {
    (void)self; (void)module;
    /* Runtime Import 已完成初始化；重复 exec 不重复求值，保留既有单点事务。 */
    Py_RETURN_NONE;
}
static PyMethodDef native_loader_methods[] = {
    {"create_module", native_loader_create, METH_O, NULL},
    {"exec_module", native_loader_exec, METH_O, NULL}, {NULL, NULL, 0, NULL}
};
static PyObject *native_finder_new(PyTypeObject *type, PyObject *args, PyObject *kwargs) {
    NativeImportHook *self = (NativeImportHook *)import_hook_new(type, args, kwargs);
    if (!self) return NULL;
    self->bootstrap = PyImport_ImportModule("_frozen_importlib");
    self->loader_type = PyObject_GetAttrString((PyObject *)type, "_loader_type");
    if (!self->bootstrap || !self->loader_type) { Py_DECREF(self); return NULL; }
    if (!PyType_Check(self->loader_type)
        || ((PyTypeObject *)self->loader_type)->tp_methods != native_loader_methods) {
        PyErr_SetString(PyExc_RuntimeError, "native finder loader type was changed"); Py_DECREF(self); return NULL;
    }
    return (PyObject *)self;
}
static PyObject *native_finder_spec(PyObject *object, PyObject *args, PyObject *kwargs) {
    NativeImportHook *self = (NativeImportHook *)object;
    PyObject *name, *path = Py_None, *target = Py_None;
    static char *keywords[] = {"fullname", "path", "target", NULL};
    if (!PyArg_ParseTupleAndKeywords(args, kwargs, "O|OO:find_spec", keywords, &name, &path, &target)) return NULL;
    (void)path; (void)target;
    if (!self->bootstrap || !self->loader_type) {
        PyErr_SetString(PyExc_RuntimeError, "native finder was cleared"); return NULL;
    }
    PyObject *known = dispatch(-1, 20, name, NULL, NULL, 0, NULL);
    if (!known) return NULL;
    int present = PyObject_IsTrue(known); Py_DECREF(known);
    if (present < 0) return NULL;
    if (!present) Py_RETURN_NONE;
    PyObject *package = NULL, *loader = NULL, *spec_type = NULL, *positional = NULL, *named = NULL, *result = NULL;
    package = dispatch(-1, 22, name, NULL, NULL, 0, NULL);
    if (!package) goto done;
    /* 每份规格持独立 loader；该引用也让 Finalize 前清缓存后的 atexit 导入安全。 */
    loader = PyObject_CallFunctionObjArgs(self->loader_type, NULL);
    if (!loader) goto done;
    spec_type = PyObject_GetAttrString(self->bootstrap, "ModuleSpec");
    if (!spec_type) goto done;
    positional = PyTuple_Pack(2, name, loader); named = PyDict_New();
    if (!positional || !named || PyDict_SetItemString(named, "is_package", package) < 0) goto done;
    result = PyObject_Call(spec_type, positional, named);
done:
    Py_XDECREF(package); Py_XDECREF(loader); Py_XDECREF(spec_type);
    Py_XDECREF(positional); Py_XDECREF(named); return result;
}
static PyMethodDef native_finder_methods[] = {
    {"find_spec", (PyCFunction)(void (*)(void))native_finder_spec, METH_VARARGS | METH_KEYWORDS, NULL},
    {NULL, NULL, 0, NULL}
};
static PyType_Slot native_loader_slots[] = {
    {Py_tp_new, import_hook_new}, {Py_tp_methods, native_loader_methods},
    {Py_tp_dealloc, import_hook_dealloc}, {Py_tp_traverse, import_hook_traverse},
    {Py_tp_clear, import_hook_clear}, {0, NULL}
};
static PyType_Slot native_finder_slots[] = {
    {Py_tp_new, native_finder_new}, {Py_tp_methods, native_finder_methods},
    {Py_tp_dealloc, import_hook_dealloc}, {Py_tp_traverse, import_hook_traverse},
    {Py_tp_clear, import_hook_clear}, {0, NULL}
};
static PyType_Spec native_loader_spec = {
    "ryupy.NativeLoader", sizeof(NativeImportHook), 0, Py_TPFLAGS_DEFAULT | Py_TPFLAGS_HAVE_GC, native_loader_slots
};
static PyType_Spec native_finder_spec_type = {
    "ryupy.NativeFinder", sizeof(NativeImportHook), 0, Py_TPFLAGS_DEFAULT | Py_TPFLAGS_HAVE_GC, native_finder_slots
};
API PyObject *rp_install_native_imports(void) {
    if (native_installed_finder) Py_RETURN_NONE;
    if (!native_loader_type) native_loader_type = (PyTypeObject *)PyType_FromSpec(&native_loader_spec);
    if (!native_loader_type) return NULL;
    if (!native_finder_type) native_finder_type = (PyTypeObject *)PyType_FromSpec(&native_finder_spec_type);
    if (!native_finder_type) return NULL;
    if (PyObject_SetAttrString((PyObject *)native_finder_type, "_loader_type", (PyObject *)native_loader_type) < 0) return NULL;
    PyObject *finder = PyObject_CallFunctionObjArgs((PyObject *)native_finder_type, NULL);
    if (!finder) return NULL;
    PyObject *meta_path = PySys_GetObject("meta_path");
    if (!meta_path || !PyList_Check(meta_path)) {
        PyErr_SetString(PyExc_TypeError, "sys.meta_path must be a list when installing native imports"); Py_DECREF(finder); return NULL;
    }
    PyObject *module = PyDict_GetItemString(PyImport_GetModuleDict(), "_ryupy_imports");
    if (!module || !PyModule_Check(module)) {
        PyErr_SetString(PyExc_RuntimeError, "native import bridge module was not initialized"); Py_DECREF(finder); return NULL;
    }
    if (PyObject_SetAttrString(module, "_finder", finder) < 0) { Py_DECREF(finder); return NULL; }
    if (PyList_Insert(meta_path, 0, finder) < 0) {
        PyObject *error = PyErr_GetRaisedException();
        (void)PyObject_DelAttrString(module, "_finder"); PyErr_Clear();
        PyErr_SetRaisedException(error); Py_DECREF(finder); return NULL;
    }
    native_installed_finder = finder; Py_RETURN_NONE;
}
/* 返回 owned 真实 C 类型；不从可变 builtins 字典重新解析初始化时冻结的描述符。 */
API PyObject *rp_descriptor_type(int kind) {
    switch (kind) {
        case 1: return Py_NewRef((PyObject *)&PyProperty_Type);
        case 2: return Py_NewRef((PyObject *)&PyStaticMethod_Type);
        case 3: return Py_NewRef((PyObject *)&PyClassMethod_Type);
        default: PyErr_SetString(PyExc_ValueError, "unknown native descriptor type"); return NULL;
    }
}
static void native_import_hooks_clear(void) {
    Py_CLEAR(native_installed_finder); Py_CLEAR(native_finder_type); Py_CLEAR(native_loader_type);
}
