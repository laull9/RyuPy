/* C1 显式候选方法描述符：静态 C 调用槽转发至既有托管目标，原生 __get__ 绑定。
 * 对象由 CPython GC 遍历，元数据独立于编译函数保存；不能提供 CPython CodeType 的
 * 帧/闭包消费者得到明确错误，调用方必须显式选择此候选，不伪装成 PyFunction。 */
#include <stddef.h>

typedef struct {
    PyObject_HEAD
    PyObject *target, *name, *qualname, *module, *doc;
    PyObject *annotations, *defaults, *kwdefaults, *dictionary;
    PyObject *weakrefs;
} NativeMethod;
static PyTypeObject *native_method_type, *native_static_method_type;

static int native_method_traverse(NativeMethod *self, visitproc visit, void *arg) {
    Py_VISIT(Py_TYPE(self));
    Py_VISIT(self->target); Py_VISIT(self->name); Py_VISIT(self->qualname);
    Py_VISIT(self->module); Py_VISIT(self->doc); Py_VISIT(self->annotations);
    Py_VISIT(self->defaults); Py_VISIT(self->kwdefaults); Py_VISIT(self->dictionary);
    return 0;
}
static int native_method_clear(NativeMethod *self) {
    Py_CLEAR(self->target); Py_CLEAR(self->name); Py_CLEAR(self->qualname);
    Py_CLEAR(self->module); Py_CLEAR(self->doc); Py_CLEAR(self->annotations);
    Py_CLEAR(self->defaults); Py_CLEAR(self->kwdefaults); Py_CLEAR(self->dictionary);
    return 0;
}
static void native_method_dealloc(NativeMethod *self) {
    PyObject_GC_UnTrack(self);
    if (self->weakrefs) PyObject_ClearWeakRefs((PyObject *)self);
    native_method_clear(self);
    PyTypeObject *type = Py_TYPE(self);
    type->tp_free((PyObject *)self); Py_DECREF(type);
}
static PyObject *native_method_new(PyTypeObject *type, PyObject *args, PyObject *kwargs) {
    (void)type; (void)args; (void)kwargs;
    PyErr_SetString(PyExc_TypeError, "RyuPy native method descriptors are created by the class bridge");
    return NULL;
}
static PyObject *native_method_call(PyObject *object, PyObject *args, PyObject *kwargs) {
    NativeMethod *self = (NativeMethod *)object;
    if (!self->target) { PyErr_SetString(PyExc_RuntimeError, "RyuPy native method was cleared"); return NULL; }
    return PyObject_Call(self->target, args, kwargs);
}
static PyObject *native_method_get(PyObject *self, PyObject *instance, PyObject *owner) {
    (void)owner;
    if (!instance || Py_IsNone(instance)) return Py_NewRef(self);
    return PyMethod_New(self, instance);
}

/* 隐式 __new__ 按导出位置选择静态绑定类型；后续改 __name__ 不会改变绑定行为。
 * 与普通方法复用同一对象布局，不为所有方法实例增加标志字段。 */
static PyObject *native_static_method_get(PyObject *self, PyObject *instance, PyObject *owner) {
    (void)instance; (void)owner; return Py_NewRef(self);
}

/* 名字与限定名保留真实可写字段；已建立的 PyMethod 在后续读取时看见当前值。 */
static PyObject *native_method_text_get(NativeMethod *self, void *closure) {
    PyObject **field = (PyObject **)((char *)self + (size_t)closure);
    return *field ? Py_NewRef(*field) : Py_NewRef(Py_None);
}
static int native_method_text_set(NativeMethod *self, PyObject *value, void *closure) {
    if (!value || !PyUnicode_Check(value)) {
        PyErr_SetString(PyExc_TypeError, "method name and qualname must be string objects"); return -1;
    }
    PyObject **field = (PyObject **)((char *)self + (size_t)closure);
    Py_XSETREF(*field, Py_NewRef(value)); return 0;
}
static PyObject *native_method_value_get(NativeMethod *self, void *closure) {
    PyObject **field = (PyObject **)((char *)self + (size_t)closure);
    return *field ? Py_NewRef(*field) : Py_NewRef(Py_None);
}
static int native_method_value_set(NativeMethod *self, PyObject *value, void *closure) {
    PyObject **field = (PyObject **)((char *)self + (size_t)closure);
    Py_XSETREF(*field, Py_XNewRef(value)); return 0;
}
static PyObject *native_method_annotations_get(NativeMethod *self, void *closure) {
    (void)closure;
    if (!self->annotations) self->annotations = PyDict_New();
    return self->annotations ? Py_NewRef(self->annotations) : NULL;
}
static int native_method_mapping_set(NativeMethod *self, PyObject *value, void *closure) {
    if (value && !Py_IsNone(value) && !PyDict_Check(value)) {
        PyErr_SetString(PyExc_TypeError, "method mapping metadata requires a dict or None"); return -1;
    }
    return native_method_value_set(self, value && !Py_IsNone(value) ? value : NULL, closure);
}
static int native_method_defaults_set(NativeMethod *self, PyObject *value, void *closure) {
    if (value && !Py_IsNone(value) && !PyTuple_Check(value)) {
        PyErr_SetString(PyExc_TypeError, "method defaults requires a tuple or None"); return -1;
    }
    return native_method_value_set(self, value && !Py_IsNone(value) ? value : NULL, closure);
}
static PyObject *native_method_unsupported_get(NativeMethod *self, void *closure) {
    (void)self; (void)closure;
    PyErr_SetString(PyExc_NotImplementedError, "RyuPy native method descriptor has no CPython CodeType, globals or closure layout");
    return NULL;
}
static int native_method_unsupported_set(NativeMethod *self, PyObject *value, void *closure) {
    (void)value;
    Py_XDECREF(native_method_unsupported_get(self, closure)); return -1;
}
/* 反射只在显式读取时进入 target；wrapper 自有 signature override 仍属于其原字典。
 * 不转发 name/defaults/annotations 等现有独立元数据，避免改变候选既有绑定合同。 */
static PyObject *native_method_canonical_get(NativeMethod *self, void *closure) {
    const char *name = (const char *)closure;
    if (strcmp(name, "__signature__") == 0 && self->dictionary) {
        PyObject *override = PyDict_GetItemString(self->dictionary, name);
        if (override) return Py_NewRef(override);
    }
    if (!self->target) { PyErr_SetString(PyExc_RuntimeError, "RyuPy native method was cleared"); return NULL; }
    PyObject *value = PyObject_GetAttrString(self->target, name);
    if (!value && strcmp(name, "__globals__") == 0 && PyErr_ExceptionMatches(PyExc_NotImplementedError)) {
        /* 无受信 globals 的宿主目标仍保留既有 code/layout 边界诊断。 */
        PyErr_Clear(); return native_method_unsupported_get(self, NULL);
    }
    return value;
}
static int native_method_signature_set(NativeMethod *self, PyObject *value, void *closure) {
    (void)closure;
    if (!value) {
        if (self->dictionary && PyDict_DelItemString(self->dictionary, "__signature__") == 0) return 0;
        PyErr_Clear(); PyErr_SetString(PyExc_AttributeError, "native method has no own __signature__ override"); return -1;
    }
    PyObject *dictionary = PyObject_GenericGetDict((PyObject *)self, NULL);
    if (!dictionary) return -1;
    int result = PyDict_SetItemString(dictionary, "__signature__", value); Py_DECREF(dictionary); return result;
}

#define METHOD_OFFSET(field) ((void *)offsetof(NativeMethod, field))
static PyGetSetDef native_method_getsets[] = {
    {"__name__", (getter)native_method_text_get, (setter)native_method_text_set, NULL, METHOD_OFFSET(name)},
    {"__qualname__", (getter)native_method_text_get, (setter)native_method_text_set, NULL, METHOD_OFFSET(qualname)},
    {"__module__", (getter)native_method_value_get, (setter)native_method_value_set, NULL, METHOD_OFFSET(module)},
    {"__doc__", (getter)native_method_value_get, (setter)native_method_value_set, NULL, METHOD_OFFSET(doc)},
    {"__annotations__", (getter)native_method_annotations_get, (setter)native_method_mapping_set, NULL, METHOD_OFFSET(annotations)},
    {"__defaults__", (getter)native_method_value_get, (setter)native_method_defaults_set, NULL, METHOD_OFFSET(defaults)},
    {"__kwdefaults__", (getter)native_method_value_get, (setter)native_method_mapping_set, NULL, METHOD_OFFSET(kwdefaults)},
    {"__dict__", PyObject_GenericGetDict, PyObject_GenericSetDict, NULL, NULL},
    {"__code__", (getter)native_method_unsupported_get, (setter)native_method_unsupported_set, NULL, NULL},
    {"__signature__", (getter)native_method_canonical_get, (setter)native_method_signature_set, NULL, "__signature__"},
    {"__globals__", (getter)native_method_canonical_get, (setter)native_method_unsupported_set, NULL, "__globals__"},
    {"__closure__", (getter)native_method_unsupported_get, (setter)native_method_unsupported_set, NULL, NULL},
    {NULL, NULL, NULL, NULL, NULL}
};
#undef METHOD_OFFSET
static PyType_Slot native_method_slots[] = {
    {Py_tp_new, native_method_new}, {Py_tp_call, native_method_call},
    {Py_tp_descr_get, native_method_get}, {Py_tp_dealloc, native_method_dealloc},
    {Py_tp_traverse, native_method_traverse}, {Py_tp_clear, native_method_clear},
    {Py_tp_getset, native_method_getsets}, {0, NULL}
};
static PyType_Spec native_method_spec = {
    "ryupy.NativeMethod", sizeof(NativeMethod), 0, Py_TPFLAGS_DEFAULT | Py_TPFLAGS_HAVE_GC, native_method_slots
};

/* 三个输入均借用，返回新引用；类型缓存只在持 GIL 区域创建并在 Finalize 前释放。 */
static PyObject *native_method_create(PyObject *target, PyObject *name, PyObject *qualname, int static_binding) {
    if (!PyCallable_Check(target) || !PyUnicode_Check(name) || !PyUnicode_Check(qualname)) {
        PyErr_SetString(PyExc_TypeError, "native method requires callable target and string names"); return NULL;
    }
    PyTypeObject **cache = static_binding ? &native_static_method_type : &native_method_type;
    if (!*cache) {
        PyType_Slot slots[sizeof(native_method_slots) / sizeof(native_method_slots[0])];
        memcpy(slots, native_method_slots, sizeof(slots));
        for (size_t i = 0; slots[i].slot; i++)
            if (static_binding && slots[i].slot == Py_tp_descr_get) slots[i].pfunc = native_static_method_get;
        PyType_Spec spec = native_method_spec;
        spec.slots = slots;
        if (static_binding) spec.name = "ryupy.NativeStaticMethod";
        *cache = (PyTypeObject *)PyType_FromSpec(&spec);
        if (!*cache) return NULL;
        /* 正式版头文件控制布局，托管端只持 opaque 指针；支持普通属性字典与弱引用。 */
        (*cache)->tp_dictoffset = offsetof(NativeMethod, dictionary);
        (*cache)->tp_weaklistoffset = offsetof(NativeMethod, weakrefs);
        PyType_Modified(*cache);
    }
    NativeMethod *self = (NativeMethod *)(*cache)->tp_alloc(*cache, 0);
    if (!self) return NULL;
    self->target = Py_NewRef(target); self->name = Py_NewRef(name); self->qualname = Py_NewRef(qualname);
    self->module = PyUnicode_FromString("_ryupy_imports");
    if (!self->module) { Py_DECREF(self); return NULL; }
    return (PyObject *)self;
}

API PyObject *rp_native_method(PyObject *target, PyObject *name, PyObject *qualname) {
    return native_method_create(target, name, qualname, 0);
}
API PyObject *rp_native_static_method(PyObject *target, PyObject *name, PyObject *qualname) {
    return native_method_create(target, name, qualname, 1);
}
