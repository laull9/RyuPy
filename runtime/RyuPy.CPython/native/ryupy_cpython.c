/* 固定CPython 3.12.13的嵌入与对象桥；所有返回对象指针均拥有一个新引用。
 * PyConfig布局、宏、代理类型及借用提升只在本文件出现，托管端仅处理opaque地址。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#if defined(_WIN32)
#define API __declspec(dllexport)
#else
#define API __attribute__((visibility("default")))
#endif
#if PY_VERSION_HEX != 0x030c0df0
#error "RyuPy shim requires CPython 3.12.13 headers"
#endif

typedef PyObject *(*managed_call)(intptr_t, int, PyObject *, PyObject *, PyObject *, int64_t, const char *);
typedef struct { PyObject_HEAD intptr_t identity; } Managed;
static managed_call dispatch;
static PyTypeObject *managed_type;
static PyThreadState *initial_thread;
static char init_error[4096];

/* 回调失败必须已设置Python异常，绝不能让托管异常穿过C栈。 */
static PyObject *forward(Managed *self, int op, PyObject *a, PyObject *b, PyObject *c, int64_t n, const char *s) {
    return dispatch(self->identity, op, a, b, c, n, s);
}
static void managed_dealloc(Managed *self) {
    PyObject *saved = PyErr_GetRaisedException();
    PyObject *result = forward(self, 0, NULL, NULL, NULL, 0, NULL);
    Py_XDECREF(result); PyErr_Clear();
    PyErr_SetRaisedException(saved);
    PyTypeObject *type = Py_TYPE(self);
    type->tp_free((PyObject *)self);
    Py_DECREF(type);
}
static PyObject *managed_call_slot(PyObject *self, PyObject *args, PyObject *kwargs) {
    return forward((Managed *)self, 1, args, kwargs, NULL, 0, NULL);
}
static PyObject *managed_getattr(PyObject *self, PyObject *name) {
    const char *text = PyUnicode_AsUTF8(name);
    return text ? forward((Managed *)self, 2, NULL, NULL, NULL, 0, text) : NULL;
}
static int managed_setattr(PyObject *self, PyObject *name, PyObject *value) {
    const char *text = PyUnicode_AsUTF8(name);
    if (!text) return -1;
    PyObject *result = forward((Managed *)self, value ? 3 : 4, value, NULL, NULL, 0, text);
    if (!result) return -1;
    Py_DECREF(result); return 0;
}
static PyObject *managed_item(PyObject *self, PyObject *key) {
    return forward((Managed *)self, 5, key, NULL, NULL, 0, NULL);
}
static int managed_setitem(PyObject *self, PyObject *key, PyObject *value) {
    PyObject *result = forward((Managed *)self, value ? 6 : 7, key, value, NULL, 0, NULL);
    if (!result) return -1;
    Py_DECREF(result); return 0;
}
static Py_ssize_t managed_len(PyObject *self) {
    PyObject *result = forward((Managed *)self, 8, NULL, NULL, NULL, 0, NULL);
    if (!result) return -1;
    Py_ssize_t value = PyLong_AsSsize_t(result); Py_DECREF(result); return value;
}
static int managed_bool(PyObject *self) {
    PyObject *result = forward((Managed *)self, 9, NULL, NULL, NULL, 0, NULL);
    if (!result) return -1;
    int value = PyObject_IsTrue(result); Py_DECREF(result); return value;
}
static PyObject *managed_repr(PyObject *self) { return forward((Managed *)self, 10, NULL, NULL, NULL, 0, NULL); }
static PyObject *managed_str(PyObject *self) { return forward((Managed *)self, 11, NULL, NULL, NULL, 0, NULL); }
static PyObject *managed_iter(PyObject *self) { return forward((Managed *)self, 12, NULL, NULL, NULL, 0, NULL); }
static PyObject *managed_next(PyObject *self) { return forward((Managed *)self, 13, NULL, NULL, NULL, 0, NULL); }
static Py_hash_t managed_hash(PyObject *self) {
    PyObject *result = forward((Managed *)self, 14, NULL, NULL, NULL, 0, NULL);
    if (!result) return -1;
    Py_hash_t value = PyLong_AsSsize_t(result); Py_DECREF(result); return value == -1 ? -2 : value;
}
static PyObject *managed_compare(PyObject *self, PyObject *other, int op) {
    return forward((Managed *)self, 15, other, NULL, NULL, op, NULL);
}
static PyType_Slot managed_slots[] = {
    {Py_tp_dealloc, managed_dealloc}, {Py_tp_call, managed_call_slot},
    {Py_tp_getattro, managed_getattr}, {Py_tp_setattro, managed_setattr},
    {Py_mp_subscript, managed_item}, {Py_mp_ass_subscript, managed_setitem},
    {Py_mp_length, managed_len}, {Py_nb_bool, managed_bool},
    {Py_tp_repr, managed_repr}, {Py_tp_str, managed_str},
    {Py_tp_iter, managed_iter}, {Py_tp_iternext, managed_next},
    {Py_tp_hash, managed_hash}, {Py_tp_richcompare, managed_compare}, {0, NULL}
};
static PyObject *bridge_has(PyObject *self, PyObject *name) { (void)self; return dispatch(-1,20,name,NULL,NULL,0,NULL); }
static PyObject *bridge_load(PyObject *self, PyObject *name) { (void)self; return dispatch(-1,21,name,NULL,NULL,0,NULL); }
static PyObject *bridge_package(PyObject *self, PyObject *name) { (void)self; return dispatch(-1,22,name,NULL,NULL,0,NULL); }
static PyMethodDef bridge_methods[] = {
    {"_rp_has", bridge_has, METH_O, NULL}, {"_rp_load", bridge_load, METH_O, NULL},
    {"_rp_package", bridge_package, METH_O, NULL}, {NULL, NULL, 0, NULL}
};
static PyType_Spec managed_spec = {"ryupy.ManagedObject", sizeof(Managed), 0, Py_TPFLAGS_DEFAULT, managed_slots};

static int fail_status(PyStatus status) {
    if (!PyStatus_Exception(status)) return 0;
    snprintf(init_error, sizeof(init_error), "%s: %s", status.func ? status.func : "initialization", status.err_msg ? status.err_msg : "requested exit");
    return -1;
}
/* 使用UTF-8独立路径列表，设置所有搜索位置且拒绝环境、用户site和隐式cwd。 */
API int rp_start(const char *home, const char **paths, int path_count, const char **argv, int argc, managed_call callback) {
    if (strcmp(Py_GetVersion(), "3.12.13") < 0 || strncmp(Py_GetVersion(), "3.12.13 ", 8) != 0) {
        snprintf(init_error, sizeof(init_error), "ABI requires CPython 3.12.13, loaded %s", Py_GetVersion()); return -1;
    }
    if (Py_IsInitialized()) { strcpy(init_error, "CPython already initialized"); return -1; }
    PyConfig config; PyConfig_InitIsolatedConfig(&config);
    config.site_import = 0; config.write_bytecode = 0; config.parse_argv = 0;
    config.install_signal_handlers = 0; config.module_search_paths_set = 1;
    if (fail_status(PyConfig_SetBytesString(&config, &config.home, home))) goto error;
    if (fail_status(PyConfig_SetBytesString(&config, &config.stdio_encoding, "utf-8"))) goto error;
    if (fail_status(PyConfig_SetBytesString(&config, &config.stdio_errors, "surrogateescape"))) goto error;
    for (int list = 0; list < 2; list++) {
        int count = list ? argc : path_count;
        const char **items = list ? argv : paths;
        for (int index = 0; index < count; index++) {
            wchar_t *wide = Py_DecodeLocale(items[index], NULL);
            if (!wide) { strcpy(init_error, "invalid UTF-8 configuration"); goto error; }
            PyStatus status = PyWideStringList_Append(list ? &config.argv : &config.module_search_paths, wide);
            PyMem_RawFree(wide); if (fail_status(status)) goto error;
        }
    }
    if (fail_status(Py_InitializeFromConfig(&config))) goto error;
    PyConfig_Clear(&config); dispatch = callback;
    managed_type = (PyTypeObject *)PyType_FromSpec(&managed_spec);
    if (!managed_type) { strcpy(init_error, "managed proxy type creation failed"); PyErr_Clear(); Py_FinalizeEx(); return -1; }
    for (PyMethodDef *method = bridge_methods; method->ml_name; method++) {
        PyObject *function = PyCFunction_New(method, NULL);
        if (!function || PyDict_SetItemString(PyEval_GetBuiltins(), method->ml_name, function) < 0) {
            Py_XDECREF(function); strcpy(init_error, "import hook registration failed"); PyErr_Clear(); Py_FinalizeEx(); return -1;
        }
        Py_DECREF(function);
    }
    initial_thread = PyEval_SaveThread(); return 0;
error:
    PyConfig_Clear(&config); return -1;
}
API const char *rp_init_error(void) { return init_error; }
API int rp_gil(void) { return (int)PyGILState_Ensure(); }
API void rp_gil_release(int state) { PyGILState_Release((PyGILState_STATE)state); }
API int rp_stop(void) {
    PyEval_RestoreThread(initial_thread); initial_thread = NULL;
    int result = Py_FinalizeEx();
    managed_type = NULL;
    return result;
}
API void rp_decref(PyObject *value) { Py_XDECREF(value); }
API PyObject *rp_incref(PyObject *value) { return Py_NewRef(value); }
API PyObject *rp_managed(intptr_t identity) {
    Managed *self = PyObject_New(Managed, managed_type);
    if (!self) return NULL;
    self->identity = identity; return (PyObject *)self;
}
API intptr_t rp_identity(PyObject *value) {
    return Py_IS_TYPE(value, managed_type) ? ((Managed *)value)->identity : 0;
}
API PyObject *rp_none(void) { return Py_NewRef(Py_None); }
API int rp_is_none(PyObject *value) { return Py_IsNone(value); }
API PyObject *rp_int(int64_t value) { return PyLong_FromLongLong(value); }
API PyObject *rp_float(double value) { return PyFloat_FromDouble(value); }
API PyObject *rp_bool(int value) { return PyBool_FromLong(value); }
API PyObject *rp_text(const char *value, int64_t size) { return PyUnicode_DecodeUTF8(value, (Py_ssize_t)size, "surrogatepass"); }
API PyObject *rp_bytes(const char *value, int64_t size) { return PyBytes_FromStringAndSize(value, (Py_ssize_t)size); }
API const char *rp_utf8(PyObject *value, int64_t *length) {
    Py_ssize_t size = 0; *length = 0; const char *text = PyUnicode_AsUTF8AndSize(value, &size); if (text) *length = size; return text;
}
API int64_t rp_long(PyObject *value) { return PyLong_AsLongLong(value); }
API double rp_double(PyObject *value) { return PyFloat_AsDouble(value); }
API int rp_truth(PyObject *value) { return PyObject_IsTrue(value); }
API int64_t rp_length(PyObject *value) { return PyObject_Length(value); }
API int64_t rp_hash(PyObject *value) { return PyObject_Hash(value); }
API int rp_error(void) { return PyErr_Occurred() != NULL; }
API PyObject *rp_take_error(void) { return PyErr_GetRaisedException(); }
API void rp_restore_error(PyObject *value) { PyErr_SetRaisedException(Py_NewRef(value)); }
API void rp_error_text(const char *type, const char *message) {
    PyObject *builtins = PyEval_GetBuiltins(); PyObject *cls = PyDict_GetItemString(builtins, type);
    if (!cls || !PyExceptionClass_Check(cls)) cls = PyExc_RuntimeError;
    PyErr_SetString(cls, message);
}
API int rp_exception_matches(PyObject *error, PyObject *type) { return PyErr_GivenExceptionMatches(error, type); }
API int rp_is_exception_type(PyObject *type) { return PyExceptionClass_Check(type); }
/* 一个小而稳定的静态ABI汇集对象操作；错误保留在线程的原异常实例中。 */
API PyObject *rp_op(int op, PyObject *a, PyObject *b, PyObject *c, int64_t n, const char *s) {
    switch (op) {
        case 1: return PyImport_ImportModule(s);
        case 2: return PyObject_GetAttrString(a, s);
        case 3: if (PyObject_SetAttrString(a, s, b) < 0) return NULL; Py_RETURN_NONE;
        case 4: if (PyObject_DelAttrString(a, s) < 0) return NULL; Py_RETURN_NONE;
        case 5: return PyObject_Call(a, b, c);
        case 6: return PyObject_GetItem(a, b);
        case 7: if (PyObject_SetItem(a, b, c) < 0) return NULL; Py_RETURN_NONE;
        case 8: if (PyObject_DelItem(a, b) < 0) return NULL; Py_RETURN_NONE;
        case 9: return PyObject_GetIter(a);
        case 10: return PyIter_Next(a);
        case 11: return PyObject_Repr(a);
        case 12: return PyObject_Str(a);
        case 13: return PyObject_Type(a);
        case 14: return PyTuple_New((Py_ssize_t)n);
        case 15: if (PyTuple_SetItem(a, (Py_ssize_t)n, Py_NewRef(b)) < 0) return NULL; Py_RETURN_NONE;
        case 16: return PyDict_New();
        case 17: if (PyDict_SetItemString(a, s, b) < 0) return NULL; Py_RETURN_NONE;
        case 18: { PyObject *item = PyTuple_GetItem(a, (Py_ssize_t)n); return item ? Py_NewRef(item) : NULL; }
        case 19: return PyDict_Keys(a);
        case 20: return PyObject_RichCompare(a, b, (int)n);
        case 21: return PyLong_FromString((char *)s, NULL, 10);
        case 22: return PyObject_GetItem(PyImport_GetModuleDict(), a);
        case 23: if (PyDict_SetItemString(PyImport_GetModuleDict(), s, a) < 0) return NULL; Py_RETURN_NONE;
        case 24: return PyNumber_Long(a);
        case 25: return PyNumber_Float(a);
        case 26: return PySequence_Tuple(a);
        case 27: { int result = PyObject_IsInstance(a,b); return result < 0 ? NULL : PyBool_FromLong(result); }
        case 28: return PySlice_New(a, b, c);
        case 29: return PyRun_String(s, Py_file_input, PyEval_GetBuiltins(), PyEval_GetBuiltins());
        case 30: if (PyDict_DelItemString(PyImport_GetModuleDict(), s) < 0) { PyErr_Clear(); } Py_RETURN_NONE;
        case 31: return PyNumber_Index(a);
    }
    /* 动态数值操作码与Runtime一致，复合赋值交由真实Python inplace槽。 */
    if (op == 40 && (n & 0x100)) {
        switch (n & ~0x100) {
            case 1: return PyNumber_InPlaceAdd(a,b); case 2: return PyNumber_InPlaceSubtract(a,b);
            case 3: return PyNumber_InPlaceMultiply(a,b); case 4: return PyNumber_InPlaceTrueDivide(a,b);
            case 5: return PyNumber_InPlaceFloorDivide(a,b); case 6: return PyNumber_InPlaceRemainder(a,b);
            case 7: return PyNumber_InPlacePower(a,b,Py_None); case 8: return PyNumber_InPlaceAnd(a,b);
            case 9: return PyNumber_InPlaceOr(a,b); case 10: return PyNumber_InPlaceXor(a,b);
            case 11: return PyNumber_InPlaceLshift(a,b); case 12: return PyNumber_InPlaceRshift(a,b);
        }
    }
    if (op == 40) {
        switch (n) {
            case 1: return PyNumber_Add(a,b); case 2: return PyNumber_Subtract(a,b);
            case 3: return PyNumber_Multiply(a,b); case 4: return PyNumber_TrueDivide(a,b);
            case 5: return PyNumber_FloorDivide(a,b); case 6: return PyNumber_Remainder(a,b);
            case 7: return PyNumber_Power(a,b,Py_None); case 8: return PyNumber_And(a,b);
            case 9: return PyNumber_Or(a,b); case 10: return PyNumber_Xor(a,b);
            case 11: return PyNumber_Lshift(a,b); case 12: return PyNumber_Rshift(a,b);
        }
    }
    if (op == 41) {
        switch (n) {
            case 1: return PyNumber_Negative(a); case 2: return PyNumber_Positive(a);
            case 3: { int truth = PyObject_IsTrue(a); return truth < 0 ? NULL : PyBool_FromLong(!truth); }
            case 4: return PyNumber_Invert(a);
        }
    }
    PyErr_SetString(PyExc_NotImplementedError, "unknown RyuPy bridge operation"); return NULL;
}

/* 用真实异常类型与参数构造实例，回调首次出域后可复用同一实例。 */
API PyObject *rp_make_error(const char *type, PyObject *args) {
    PyObject *cls = PyDict_GetItemString(PyEval_GetBuiltins(), type);
    if (!cls || !PyExceptionClass_Check(cls)) cls = PyExc_RuntimeError;
    return PyObject_CallObject(cls, args);
}
/* exact标量识别用于协议键的显式读取；拒绝按可伪造的类型短名判断。 */
API int rp_scalar_kind(PyObject *value) {
    if (PyLong_CheckExact(value)) return 1;
    if (PyBool_Check(value)) return 2;
    if (PyFloat_CheckExact(value)) return 3;
    if (PyUnicode_CheckExact(value)) return 4;
    if (PyBytes_CheckExact(value)) return 5;
    return 0;
}
/* 沿真实MRO寻找内建异常祖先，避免同名用户类型冒充内建异常。 */
API const char *rp_builtin_exception(PyObject *value) {
    PyObject *mro = Py_TYPE(value)->tp_mro;
    PyObject *builtins = PyEval_GetBuiltins();
    if (!mro) return "BaseException";
    for (Py_ssize_t i = 0; i < PyTuple_GET_SIZE(mro); i++) {
        PyObject *type = PyTuple_GET_ITEM(mro, i);
        if (!PyType_Check(type)) continue;
        const char *name = ((PyTypeObject *)type)->tp_name;
        if (PyDict_GetItemString(builtins, name) == type) return name;
    }
    return "BaseException";
}
API PyObject *rp_builtin(const char *name) {
    PyObject *value = PyDict_GetItemString(PyEval_GetBuiltins(), name);
    if (!value) { PyErr_SetString(PyExc_TypeError, "unknown builtin type"); return NULL; }
    return Py_NewRef(value);
}
/* 精确内建容器同步原语，不执行包名相关逻辑。 */
API PyObject *rp_list(void) { return PyList_New(0); }
API int rp_list_append(PyObject *list, PyObject *item) { return PyList_Append(list,item); }
API int rp_list_replace(PyObject *list, PyObject *source) { return PyList_SetSlice(list,0,PyList_GET_SIZE(list),source); }
API int rp_dict_delete(PyObject *dict, PyObject *key) { return PyDict_DelItem(dict,key); }
API const char *rp_bytes_view(PyObject *value, int64_t *length) {
    char *buffer = NULL; Py_ssize_t size = 0; *length = 0;
    if (PyBytes_AsStringAndSize(value,&buffer,&size) < 0) return NULL;
    *length = size; return buffer;
}
API int rp_exact_tuple(PyObject *value) { return PyTuple_CheckExact(value); }

API PyObject *rp_notimplemented(void) { return Py_NewRef(Py_NotImplemented); }
/* 只观测已加载模块文件，不执行模块级__getattr__，不引入额外业务导入。 */
API PyObject *rp_loaded_modules(void) {
    PyObject *result = PyDict_New();
    if (!result) return NULL;
    PyObject *name, *module; Py_ssize_t position = 0;
    while (PyDict_Next(PyImport_GetModuleDict(), &position, &name, &module)) {
        if (!PyUnicode_Check(name) || !PyModule_Check(module)) continue;
        PyObject *file = PyDict_GetItemString(PyModule_GetDict(module), "__file__");
        if (file && PyUnicode_Check(file) && PyDict_SetItem(result, name, file) < 0) { Py_DECREF(result); return NULL; }
    }
    return result;
}
API int rp_exception_instance(PyObject *value) { return PyExceptionInstance_Check(value); }
