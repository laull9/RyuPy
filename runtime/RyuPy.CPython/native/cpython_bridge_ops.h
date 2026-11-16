/* RyuPy CPython 纯对象桥接原语：托管对象转发槽、标量/容器/异常/类操作。
 * 供 compat（3.9+）与 strict（3.12.13 内核）shim 复用，所有返回对象指针均拥有新引用。 */
#ifndef RYUPY_CPYTHON_BRIDGE_OPS_H
#define RYUPY_CPYTHON_BRIDGE_OPS_H

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
static PyType_Spec managed_spec = {"ryupy.ManagedObject", sizeof(Managed), 0, Py_TPFLAGS_DEFAULT, managed_slots};

#include "managed_descriptor.h"

API const char *rp_init_error(void) { return init_error; }
API int rp_gil(void) { return (int)PyGILState_Ensure(); }
API void rp_gil_release(int state) { PyGILState_Release((PyGILState_STATE)state); }
API void rp_decref(PyObject *value) { Py_XDECREF(value); }
API PyObject *rp_incref(PyObject *value) { return Py_NewRef(value); }
API PyObject *rp_managed(intptr_t identity) {
    Managed *self = PyObject_New(Managed, managed_type);
    if (!self) return NULL;
    self->identity = identity; return (PyObject *)self;
}
API intptr_t rp_identity(PyObject *value) {
    return Py_IS_TYPE(value, managed_type) || is_managed_descriptor(value) ? ((Managed *)value)->identity : 0;
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
    PyObject *builtins = PyEval_GetBuiltins();
    PyObject *cls = (builtins && PyDict_Check(builtins)) ? PyDict_GetItemString(builtins, type) : NULL;
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
        case 29: PyErr_SetString(PyExc_NotImplementedError, "rp_op(29) source evaluation is retired"); return NULL;
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

/* 默认代理容器比较只读取live槽；输出元素引用归调用者持有。 */
API int rp_container_kind(PyObject *value) {
    if (PyList_CheckExact(value)) return 1;
    if (PyDict_CheckExact(value)) return 2;
    if (PySet_CheckExact(value)) return 3;
    if (PyFrozenSet_CheckExact(value)) return 4;
    return 0;
}
API PyObject *rp_list_item(PyObject *value, int64_t index) {
    PyObject *item = PyList_GetItem(value, (Py_ssize_t)index); return item ? Py_NewRef(item) : NULL;
}
API int rp_dict_next(PyObject *value, int64_t *position, PyObject **key, PyObject **item) {
    Py_ssize_t cursor = (Py_ssize_t)*position; *key = NULL; *item = NULL;
    int found = PyDict_Next(value, &cursor, key, item); *position = cursor;
    if (found) { Py_INCREF(*key); Py_INCREF(*item); }
    return found;
}
API int rp_dict_lookup(PyObject *value, PyObject *key, PyObject **item) {
    *item = PyDict_GetItemWithError(value, key);
    if (*item) { Py_INCREF(*item); return 1; }
    return PyErr_Occurred() ? -1 : 0;
}
API PyObject *rp_exception_type(const char *name, PyObject *parent, PyObject *metadata) {
    if (!PyExceptionClass_Check(parent)) {
        PyErr_SetString(PyExc_TypeError, "RyuPy exception base must derive from BaseException"); return NULL;
    }
    PyObject *type = PyErr_NewException(name, parent, metadata);
    if (type) ((PyTypeObject *)type)->tp_flags |= Py_TPFLAGS_IMMUTABLETYPE;
    return type;
}
API PyObject *rp_make_error(const char *type, PyObject *args) {
    PyObject *builtins = PyEval_GetBuiltins();
    PyObject *cls = (builtins && PyDict_Check(builtins)) ? PyDict_GetItemString(builtins, type) : NULL;
    if (!cls || !PyExceptionClass_Check(cls)) cls = PyExc_RuntimeError;
    return PyObject_CallObject(cls, args);
}
API int rp_scalar_kind(PyObject *value) {
    if (PyLong_CheckExact(value)) return 1;
    if (PyBool_Check(value)) return 2;
    if (PyFloat_CheckExact(value)) return 3;
    if (PyUnicode_CheckExact(value)) return 4;
    if (PyBytes_CheckExact(value)) return 5;
    return 0;
}
API PyObject *rp_integer_hex(PyObject *value) { return PyNumber_ToBase(value, 16); }
API const char *rp_builtin_exception(PyObject *value) {
    PyObject *mro = Py_TYPE(value)->tp_mro;
    PyObject *builtins = PyEval_GetBuiltins();
    if (!mro || !builtins || !PyDict_Check(builtins)) return "BaseException";
    for (Py_ssize_t i = 0; i < PyTuple_GET_SIZE(mro); i++) {
        PyObject *type = PyTuple_GET_ITEM(mro, i);
        if (!PyType_Check(type)) continue;
        const char *name = ((PyTypeObject *)type)->tp_name;
        if (PyDict_GetItemString(builtins, name) == type) return name;
    }
    return "BaseException";
}
API PyObject *rp_builtin(const char *name) {
    PyObject *builtins = PyEval_GetBuiltins();
    PyObject *value = (builtins && PyDict_Check(builtins)) ? PyDict_GetItemString(builtins, name) : NULL;
    if (!value) { PyErr_SetString(PyExc_TypeError, "unknown builtin type"); return NULL; }
    return Py_NewRef(value);
}
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
API int rp_is_module(PyObject *value) { return PyModule_Check(value); }
API int rp_small_integer(PyObject *value, int64_t *result) {
    if (!PyLong_CheckExact(value)) return 0;
    int overflow = 0; long long number = PyLong_AsLongLongAndOverflow(value, &overflow);
    if (overflow || number < -5 || number > 256) return 0;
    *result = number; return 1;
}
API PyObject *rp_exception_args(PyObject *value) { return PyException_GetArgs(value); }
API PyObject *rp_exception_link(PyObject *value, int cause) {
    PyObject *link = cause ? PyException_GetCause(value) : PyException_GetContext(value);
    return link ? link : Py_NewRef(Py_None);
}
API const char *rp_exception_name(PyObject *value) { return Py_TYPE(value)->tp_name; }
API int rp_exception_suppress(PyObject *value) { return ((PyBaseExceptionObject *)value)->suppress_context; }
API PyObject *rp_get_handled(void) { return PyErr_GetHandledException(); }
API void rp_set_handled(PyObject *value) {
    PyObject *raised = PyErr_GetRaisedException();
    PyErr_SetHandledException(value);
    PyErr_SetRaisedException(raised);
}
API void rp_exception_set_chain(PyObject *value, PyObject *cause, PyObject *context, int suppress) {
    PyException_SetCause(value, Py_XNewRef(cause));
    PyException_SetContext(value, Py_XNewRef(context));
    ((PyBaseExceptionObject *)value)->suppress_context = suppress != 0;
}
API int rp_traceback_frame(PyObject *error, int index, const char **file, const char **function, int *line) {
    *file = NULL; *function = NULL; *line = 0;
    if (!PyExceptionInstance_Check(error)) return 0;
    PyObject *root = PyException_GetTraceback(error);
    if (!root) return PyErr_Occurred() ? -1 : 0;
    PyTracebackObject *trace = (PyTracebackObject *)root;
    while (index-- > 0 && trace) trace = trace->tb_next;
    if (!trace) { Py_DECREF(root); return 0; }
    PyCodeObject *code = PyFrame_GetCode(trace->tb_frame);
    if (!code) { Py_DECREF(root); return -1; }
    *file = PyUnicode_AsUTF8(code->co_filename);
    *function = PyUnicode_AsUTF8(code->co_name);
    *line = trace->tb_lineno >= 0 ? trace->tb_lineno : PyCode_Addr2Line(code, trace->tb_lasti);
    int result = *file && *function ? 1 : -1;
    Py_DECREF(code); Py_DECREF(root); return result;
}
API int rp_is_type(PyObject *value) { return value && PyType_Check(value) ? 1 : 0; }
API int rp_super_kind(PyObject *value) { return value == (PyObject *)&PySuper_Type ? 2 : Py_IS_TYPE(value, &PySuper_Type) ? 1 : 0; }
API PyObject *rp_super_type(void) { return Py_NewRef((PyObject *)&PySuper_Type); }
API int rp_is_plain_type(PyObject *value) { return value == (PyObject *)&PyType_Type ? 1 : 0; }
API int rp_is_subclass(PyObject *cls, PyObject *base) { return PyObject_IsSubclass(cls, base); }
API int rp_contains(PyObject *container, PyObject *item) { return PySequence_Contains(container, item); }
API PyObject *rp_dir(PyObject *value) { return PyObject_Dir(value); }
API PyObject *rp_borrowed_type(PyObject *value) { return value ? (PyObject *)Py_TYPE(value) : NULL; }

#endif /* RYUPY_CPYTHON_BRIDGE_OPS_H */
