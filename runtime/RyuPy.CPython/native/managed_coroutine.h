/* 专用协程与 await 迭代器类型：静态 C 槽负责 CPython 协议，实际恢复始终进 CIL。 */
static PyTypeObject *managed_coroutine_type;
static PyTypeObject *managed_await_iterator_type;

static PyObject *managed_coroutine_await(PyObject *self) {
    return forward((Managed *)self, 31, NULL, NULL, NULL, 0, NULL);
}
static PyObject *managed_coroutine_send(PyObject *self, PyObject *value) {
    return forward((Managed *)self, 32, value, NULL, NULL, 0, NULL);
}
static PyObject *managed_coroutine_throw(PyObject *self, PyObject *args) {
    PyObject *type, *value = NULL, *traceback = NULL;
    if (!PyArg_UnpackTuple(args, "throw", 1, 3, &type, &value, &traceback)) return NULL;
    if (!value && PyExceptionInstance_Check(type))
        return forward((Managed *)self, 33, type, NULL, NULL, 0, NULL);
    if (traceback && !Py_IsNone(traceback) && !PyTraceBack_Check(traceback)) {
        PyErr_SetString(PyExc_TypeError, "throw() third argument must be a traceback object");
        return NULL;
    }
    /* 仍可收到旧式 (type, value, traceback)；在所属域规范化，同一个实例进入 CIL。 */
    if (PyExceptionInstance_Check(type)) {
        if (value && !Py_IsNone(value)) {
            PyErr_SetString(PyExc_TypeError, "instance exception may not have a separate value");
            return NULL;
        }
        value = Py_NewRef(type);
    } else if (PyExceptionClass_Check(type)) {
        PyErr_SetObject(type, value ? value : Py_None);
        value = PyErr_GetRaisedException();
    } else {
        PyErr_SetString(PyExc_TypeError, "exceptions must be classes or instances deriving from BaseException");
        return NULL;
    }
    if (!value) return NULL;
    if (traceback && !Py_IsNone(traceback) && PyException_SetTraceback(value, traceback) < 0) {
        Py_DECREF(value);
        return NULL;
    }
    PyObject *result = forward((Managed *)self, 33, value, NULL, NULL, 0, NULL);
    Py_DECREF(value);
    return result;
}
static PyObject *managed_coroutine_close(PyObject *self, PyObject *unused) {
    (void)unused;
    return forward((Managed *)self, 34, NULL, NULL, NULL, 0, NULL);
}
static PyObject *managed_await_next(PyObject *self) {
    return managed_coroutine_send(self, Py_None);
}
static PyObject *managed_coroutine_new(PyTypeObject *type, PyObject *args, PyObject *kwargs) {
    (void)type; (void)args; (void)kwargs;
    PyErr_SetString(PyExc_TypeError, "cannot create a compiled coroutine object manually");
    return NULL;
}
static PyMethodDef managed_coroutine_methods[] = {
    {"send", managed_coroutine_send, METH_O, NULL}, {"throw", managed_coroutine_throw, METH_VARARGS, NULL},
    {"close", managed_coroutine_close, METH_NOARGS, NULL}, {NULL, NULL, 0, NULL}
};
static PyType_Slot managed_coroutine_slots[] = {
    {Py_tp_dealloc, managed_dealloc}, {Py_tp_new, managed_coroutine_new},
    {Py_tp_methods, managed_coroutine_methods}, {Py_am_await, managed_coroutine_await}, {0, NULL}
};
static PyType_Slot managed_await_iterator_slots[] = {
    {Py_tp_dealloc, managed_dealloc}, {Py_tp_new, managed_coroutine_new},
    {Py_tp_methods, managed_coroutine_methods}, {Py_tp_iter, PyObject_SelfIter},
    {Py_tp_iternext, managed_await_next}, {0, NULL}
};
static PyType_Spec managed_coroutine_spec = {
    "ryupy.CilCoroutine", sizeof(Managed), 0, Py_TPFLAGS_DEFAULT, managed_coroutine_slots
};
static PyType_Spec managed_await_iterator_spec = {
    "ryupy.CilCoroutineIterator", sizeof(Managed), 0, Py_TPFLAGS_DEFAULT, managed_await_iterator_slots
};
API PyObject *rp_managed_coroutine(intptr_t identity, int iterator) {
    PyTypeObject **type = iterator ? &managed_await_iterator_type : &managed_coroutine_type;
    if (!*type)
        *type = (PyTypeObject *)PyType_FromSpec(iterator ? &managed_await_iterator_spec : &managed_coroutine_spec);
    if (!*type) return NULL;
    Managed *self = (Managed *)PyType_GenericAlloc(*type, 0);
    if (!self) return NULL;
    self->identity = identity;
    return (PyObject *)self;
}
/* 通用代理与专用代理共享相同的 managed root 标识，不改变对象往返身份。 */
API PyObject *rp_managed(intptr_t identity) {
    Managed *self = PyObject_New(Managed, managed_type);
    if (!self) return NULL;
    self->identity = identity; return (PyObject *)self;
}
API intptr_t rp_identity(PyObject *value) {
    return Py_IS_TYPE(value, managed_type) || is_managed_descriptor(value)
        || Py_IS_TYPE(value, managed_coroutine_type) || Py_IS_TYPE(value, managed_await_iterator_type)
        ? ((Managed *)value)->identity : 0;
}

/* 不从实例属性读取特殊方法，也不重复调用返回值的 __iter__。 */
API PyObject *rp_await_iterator(PyObject *source) {
    PyAsyncMethods *async = Py_TYPE(source)->tp_as_async;
    if (!async || !async->am_await) return NULL;
    PyObject *result = async->am_await(source);
    if (!result) return NULL;
    if (PyCoro_CheckExact(result) || Py_IS_TYPE(result, managed_coroutine_type)) {
        PyErr_SetString(PyExc_TypeError, "__await__() returned a coroutine");
    } else if (!PyIter_Check(result)) {
        PyErr_Format(PyExc_TypeError, "__await__() returned non-iterator of type '%.100s'", Py_TYPE(result)->tp_name);
    } else return result;
    Py_DECREF(result);
    return NULL;
}
API PyObject *rp_await_next(PyObject *iterator, int *done) {
    *done = 0;
#if PY_VERSION_HEX >= 0x030a0000
    PyObject *result = NULL;
    PySendResult status = PyIter_Send(iterator, Py_None, &result);
    *done = status == PYGEN_RETURN;
    return result;
#else
    PyObject *result = Py_TYPE(iterator)->tp_iternext(iterator);
    if (result || PyErr_Occurred()) return result;
    *done = 1;
    Py_RETURN_NONE;
#endif
}
