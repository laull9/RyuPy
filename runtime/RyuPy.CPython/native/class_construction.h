/* C1 两阶段真实类型构造：元类选择与 __prepare__ 先于 CIL 类体，随后调用真实元类。
 * capsule 只持 owned 引用；所有布局均来自选中版本公开头文件，适配 CPython 3.9+。 */
typedef struct {
    PyObject *name, *bases, *meta, *namespace;
    int phase;
} ClassConstruction;
#define CLASS_CAPSULE "ryupy.class-construction"
static ClassConstruction *class_state(PyObject *session) {
    return (ClassConstruction *)PyCapsule_GetPointer(session, CLASS_CAPSULE);
}
static void class_destroy(PyObject *capsule) {
    PyObject *saved = PyErr_GetRaisedException();
    ClassConstruction *state = class_state(capsule);
    if (state) {
        Py_XDECREF(state->name); Py_XDECREF(state->bases);
        Py_XDECREF(state->meta); Py_XDECREF(state->namespace); free(state);
    }
    PyErr_Clear(); PyErr_SetRaisedException(saved);
}
API PyObject *rp_class_prepare(PyObject *name, PyObject *bases) {
    if (!PyUnicode_Check(name) || !PyTuple_Check(bases)) {
        PyErr_SetString(PyExc_TypeError, "class construction requires a string name and tuple bases"); return NULL;
    }
    PyTypeObject *winner = &PyType_Type;
    for (Py_ssize_t i = 0; i < PyTuple_GET_SIZE(bases); i++) {
        PyObject *base = PyTuple_GET_ITEM(bases, i);
        if (!PyType_Check(base)) {
            PyErr_SetString(PyExc_NotImplementedError, "class construction requires real type bases; MRO entries are not supported"); return NULL;
        }
        PyTypeObject *meta = Py_TYPE(base);
        if (PyType_IsSubtype(winner, meta)) continue;
        if (PyType_IsSubtype(meta, winner)) { winner = meta; continue; }
        PyErr_SetString(PyExc_TypeError, "metaclass conflict: the metaclass of a derived class must be a (non-strict) subclass of the metaclasses of all its bases"); return NULL;
    }
    PyObject *prepare = PyObject_GetAttrString((PyObject *)winner, "__prepare__");
    PyObject *namespace = NULL;
    if (!prepare) {
        if (!PyErr_ExceptionMatches(PyExc_AttributeError)) return NULL;
        PyErr_Clear(); namespace = PyDict_New();
    } else {
        /* 存在但值为 None 的 __prepare__ 仍须调用并报 TypeError。 */
        namespace = PyObject_CallFunctionObjArgs(prepare, name, bases, NULL); Py_DECREF(prepare);
    }
    if (!namespace) return NULL;
    if (!PyMapping_Check(namespace)) {
        PyErr_Format(PyExc_TypeError, "%.200s.__prepare__() must return a mapping, not %.200s", winner->tp_name, Py_TYPE(namespace)->tp_name);
        Py_DECREF(namespace); return NULL;
    }
    ClassConstruction *state = (ClassConstruction *)calloc(1, sizeof(ClassConstruction));
    if (!state) { Py_DECREF(namespace); return PyErr_NoMemory(); }
    state->name = Py_NewRef(name); state->bases = Py_NewRef(bases);
    state->meta = Py_NewRef((PyObject *)winner); state->namespace = namespace;
    PyObject *capsule = PyCapsule_New(state, CLASS_CAPSULE, class_destroy);
    if (!capsule) { Py_DECREF(state->name); Py_DECREF(state->bases); Py_DECREF(state->meta); Py_DECREF(namespace); free(state); }
    return capsule;
}
API int rp_class_lookup(PyObject *session, const char *name, PyObject **value) {
    *value = NULL;
    ClassConstruction *state = class_state(session);
    if (!state) return -1;
    *value = PyMapping_GetItemString(state->namespace, name);
    if (*value) return 1;
    if (PyErr_ExceptionMatches(PyExc_KeyError)) { PyErr_Clear(); return 0; }
    return -1;
}
API int rp_class_set(PyObject *session, const char *name, PyObject *value) {
    ClassConstruction *state = class_state(session);
    return state ? PyMapping_SetItemString(state->namespace, name, value) : -1;
}
API int rp_class_delete(PyObject *session, const char *name) {
    ClassConstruction *state = class_state(session);
    return state ? PyMapping_DelItemString(state->namespace, name) : -1;
}
API PyObject *rp_class_finish(PyObject *session) {
    ClassConstruction *state = class_state(session);
    if (!state) return NULL;
    if (state->phase) { PyErr_SetString(PyExc_RuntimeError, "class construction is no longer active"); return NULL; }
    state->phase = 1;
    PyObject *result = PyObject_CallFunctionObjArgs(state->meta, state->name, state->bases, state->namespace, NULL);
    state->phase = 2;
    if (result && !PyType_Check(result)) {
        Py_DECREF(result); PyErr_SetString(PyExc_NotImplementedError, "class metaclass must return a real derived type"); return NULL;
    }
    return result;
}
/* 只解析活弱引用；target 不反向强持 wrapper，避免跨堆环和地址复用。 */
API PyObject *rp_callable_reference(PyObject *callable) { return PyWeakref_NewRef(callable, NULL); }
static PyObject *class_callable(PyObject *reference) {
#if PY_VERSION_HEX >= 0x030d0000
    PyObject *value = NULL;
    return PyWeakref_GetRef(reference, &value) > 0 ? value : NULL;
#else
    PyObject *value = PyWeakref_GetObject(reference);
    return value && value != Py_None ? Py_NewRef(value) : NULL;
#endif
}
static int class_member_calls(PyObject *member, PyObject *callable) {
    if (member == callable) return 1;
    const char *fields[4] = {NULL, NULL, NULL, NULL};
    if (Py_IS_TYPE(member, &PyStaticMethod_Type) || Py_IS_TYPE(member, &PyClassMethod_Type)) fields[0] = "__func__";
    else if (Py_IS_TYPE(member, &PyProperty_Type)) { fields[0] = "fget"; fields[1] = "fset"; fields[2] = "fdel"; }
    /* 只读精确内建描述符字段，不执行用户自定义 __get__ 或元类属性协议。 */
    for (int i = 0; fields[i]; i++) {
        PyObject *inner = PyObject_GetAttrString(member, fields[i]);
        if (!inner) return -1;
        int same = inner == callable; Py_DECREF(inner);
        if (same) return 1;
    }
    return 0;
}
/* 真实首个 MRO 字典成员必须持有同一 frozen callable/descriptor，再核冻结直接 bases。
 * declaring 非空时必须恰为真实 MRO 命中的声明类型；kind=1 为 set_name 的 descriptor self。 */
API PyObject *rp_class_receiver(PyObject *session, PyObject *receiver, const char *name,
                               PyObject *expected, PyObject *declaring, int kind) {
    ClassConstruction *state = class_state(session);
    if (!state || state->phase != 1) return NULL;
    PyTypeObject *type = PyType_Check(receiver) ? (PyTypeObject *)receiver : Py_TYPE(receiver);
    if (!type->tp_mro || !type->tp_bases || PyTuple_GET_SIZE(type->tp_bases) != PyTuple_GET_SIZE(state->bases)) return NULL;
    for (Py_ssize_t i = 0; i < PyTuple_GET_SIZE(state->bases); i++)
        if (PyTuple_GET_ITEM(type->tp_bases, i) != PyTuple_GET_ITEM(state->bases, i)) return NULL;
    for (Py_ssize_t i = 0; i < PyTuple_GET_SIZE(type->tp_mro); i++) {
        PyTypeObject *source = (PyTypeObject *)PyTuple_GET_ITEM(type->tp_mro, i);
        PyObject *member = source->tp_dict ? PyDict_GetItemString(source->tp_dict, name) : NULL;
        if (!member) continue;
        if (declaring && declaring != (PyObject *)source) return NULL;
        if (kind == 1) return member == expected && source == type ? Py_NewRef((PyObject *)type) : NULL;
        PyObject *callable = class_callable(expected);
        if (!callable) return NULL;
        int matches = class_member_calls(member, callable); Py_DECREF(callable);
        return matches > 0 ? Py_NewRef((PyObject *)type) : NULL;
    }
    return NULL;
}
#undef CLASS_CAPSULE
