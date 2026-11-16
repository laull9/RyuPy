/* 只有真实拥有 __set_name__ 的托管描述符实例采用此类型；普通代理不暴露假特殊方法。
 * 不提供 __get__/__set__/__delete__ 数据描述符槽，完整自定义描述符仍有独立准入边界。 */
static PyTypeObject *managed_descriptor_type;
static PyObject *managed_set_name(PyObject *self, PyObject *args) {
    PyObject *owner, *name;
    if (!PyArg_UnpackTuple(args, "__set_name__", 2, 2, &owner, &name)) return NULL;
    return forward((Managed *)self, 24, owner, name, NULL, 0, NULL);
}
static PyMethodDef managed_descriptor_methods[] = {
    {"__set_name__", managed_set_name, METH_VARARGS, NULL}, {NULL, NULL, 0, NULL}
};
/* Finalize 前会释放缓存；活实例仍持其类型，此静态槽签名不依赖可能失效的缓存地址。 */
static int is_managed_descriptor(PyObject *value) {
    PyTypeObject *type = Py_TYPE(value);
    return type->tp_dealloc == (destructor)managed_dealloc
        && type->tp_methods == managed_descriptor_methods && type->tp_basicsize == sizeof(Managed);
}
API int rp_is_managed_descriptor(PyObject *value) { return is_managed_descriptor(value); }
API PyObject *rp_managed_descriptor(intptr_t identity) {
    if (!managed_descriptor_type) {
        size_t count = sizeof(managed_slots) / sizeof(managed_slots[0]);
        PyType_Slot slots[sizeof(managed_slots) / sizeof(managed_slots[0]) + 1];
        memcpy(slots, managed_slots, sizeof(managed_slots));
        slots[count - 1] = (PyType_Slot){Py_tp_methods, managed_descriptor_methods};
        slots[count] = (PyType_Slot){0, NULL};
        PyType_Spec spec = {"ryupy.ManagedDescriptor", sizeof(Managed), 0, Py_TPFLAGS_DEFAULT, slots};
        managed_descriptor_type = (PyTypeObject *)PyType_FromSpec(&spec);
        if (!managed_descriptor_type) return NULL;
    }
    Managed *self = (Managed *)managed_descriptor_type->tp_alloc(managed_descriptor_type, 0);
    if (!self) return NULL;
    self->identity = identity; return (PyObject *)self;
}
