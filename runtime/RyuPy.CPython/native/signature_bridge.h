/* ADR0219：真实inspect身份和受信slot schema。只用公开C API分配/填写对象，
 * 不执行inspect.py构造器/绑定器，不复制Python源码，不读取CPython私有布局偏移。
 * 配置按解释器惰性初始化；schema不符给出明确错误，停止本次反射操作。 */
static PyObject *inspect_parameter, *inspect_signature, *inspect_bound, *inspect_empty, *inspect_kind;
static PyObject *inspect_kinds[5];
static PyObject *inspect_signature_api, *inspect_unwrap_api;
static PyObject *inspect_parameter_fields[4], *inspect_signature_fields[2], *inspect_bound_fields[2];
static int inspect_profile_captured;

static void inspect_bridge_clear(void) {
    Py_CLEAR(inspect_parameter); Py_CLEAR(inspect_signature); Py_CLEAR(inspect_bound);
    Py_CLEAR(inspect_empty); Py_CLEAR(inspect_kind);
    Py_CLEAR(inspect_signature_api); Py_CLEAR(inspect_unwrap_api);
    for (int i = 0; i < 5; i++) Py_CLEAR(inspect_kinds[i]);
    for (int i = 0; i < 4; i++) Py_CLEAR(inspect_parameter_fields[i]);
    for (int i = 0; i < 2; i++) { Py_CLEAR(inspect_signature_fields[i]); Py_CLEAR(inspect_bound_fields[i]); }
}

static int inspect_slot_schema(PyObject *type, const char **names, Py_ssize_t count) {
    if (!PyType_Check(type)) goto mismatch;
    PyObject *slots = PyObject_GetAttrString(type, "__slots__");
    if (!slots) return -1;
    int valid = PyTuple_CheckExact(slots) && PyTuple_GET_SIZE(slots) == count;
    for (Py_ssize_t i = 0; valid && i < count; i++) {
        PyObject *slot = PyTuple_GET_ITEM(slots, i);
        valid = PyUnicode_CheckExact(slot) && PyUnicode_CompareWithASCIIString(slot, names[i]) == 0;
    }
    Py_DECREF(slots);
    if (valid) return 0;
mismatch:
    PyErr_SetString(PyExc_NotImplementedError, "inspect bridge requires the trusted Parameter/Signature/BoundArguments slot schema");
    return -1;
}

static int inspect_bridge_ready(void) {
    if (inspect_parameter) return 0;
    if (inspect_profile_captured) {
        PyErr_SetString(PyExc_NotImplementedError, "inspect profile is closed; late reflection cannot recapture user module state"); return -1;
    }
    PyObject *module = PyImport_ImportModule("inspect");
    if (!module) return -1;
    inspect_parameter = PyObject_GetAttrString(module, "Parameter");
    inspect_signature = PyObject_GetAttrString(module, "Signature");
    inspect_bound = PyObject_GetAttrString(module, "BoundArguments");
    inspect_empty = PyObject_GetAttrString(module, "_empty");
    inspect_kind = PyObject_GetAttrString(module, "_ParameterKind");
    inspect_signature_api = PyObject_GetAttrString(module, "signature");
    inspect_unwrap_api = PyObject_GetAttrString(module, "unwrap");
    Py_DECREF(module);
    if (!inspect_parameter || !inspect_signature || !inspect_bound || !inspect_empty || !inspect_kind || !inspect_signature_api || !inspect_unwrap_api) goto fail;
    const char *parameter_slots[] = {"_name", "_kind", "_default", "_annotation"};
    const char *signature_slots[] = {"_return_annotation", "_parameters"};
    const char *bound_slots[] = {"arguments", "_signature", "__weakref__"};
    if (inspect_slot_schema(inspect_parameter, parameter_slots, 4) < 0 ||
        inspect_slot_schema(inspect_signature, signature_slots, 2) < 0 ||
        inspect_slot_schema(inspect_bound, bound_slots, 3) < 0) goto fail;
    const char *kind_names[] = {"POSITIONAL_ONLY", "POSITIONAL_OR_KEYWORD", "VAR_POSITIONAL", "KEYWORD_ONLY", "VAR_KEYWORD"};
    for (int i = 0; i < 5; i++) {
        inspect_kinds[i] = PyObject_GetAttrString(inspect_parameter, kind_names[i]);
        if (!inspect_kinds[i] || !Py_IS_TYPE(inspect_kinds[i], (PyTypeObject *)inspect_kind) ||
            !PyLong_Check(inspect_kinds[i]) || PyLong_AsLong(inspect_kinds[i]) != i) goto fail;
    }
    const char *field_names[] = {"_name", "_kind", "_default", "_annotation", "_return_annotation", "_parameters", "arguments", "_signature"};
    for (int i = 0; i < 8; i++) {
        PyObject *type = i < 4 ? inspect_parameter : i < 6 ? inspect_signature : inspect_bound;
        PyObject **field = i < 4 ? &inspect_parameter_fields[i] : i < 6 ? &inspect_signature_fields[i - 4] : &inspect_bound_fields[i - 6];
        *field = PyObject_GetAttrString(type, field_names[i]);
        if (!*field || !Py_IS_TYPE(*field, &PyMemberDescr_Type)) goto fail;
    }
    inspect_profile_captured = 1;
    return 0;
fail:
    if (!PyErr_Occurred()) PyErr_SetString(PyExc_NotImplementedError, "inspect bridge kind schema mismatch");
    inspect_bridge_clear(); return -1;
}


/* 用户替换slot descriptor时明确拒绝；不能借静态schema调用后来注入的Python属性正文。 */
static int inspect_fields_valid(PyObject *type, PyObject **fields, const char **names, int count) {
    for (int i = 0; i < count; i++) {
        PyObject *current = PyObject_GetAttrString(type, names[i]);
        if (!current) return -1;
        int same = current == fields[i]; Py_DECREF(current);
        if (!same) { PyErr_SetString(PyExc_NotImplementedError, "inspect trusted slot descriptor was replaced"); return -1; }
    }
    return 0;
}
static int inspect_object_schema(int tag) {
    const char *p[] = {"_name", "_kind", "_default", "_annotation"};
    const char *s[] = {"_return_annotation", "_parameters"};
    const char *b[] = {"arguments", "_signature"};
    if (tag == 20) return inspect_fields_valid(inspect_parameter, inspect_parameter_fields, p, 4);
    if (tag == 21) return inspect_fields_valid(inspect_signature, inspect_signature_fields, s, 2);
    return inspect_fields_valid(inspect_bound, inspect_bound_fields, b, 2);
}

/* 稳定身份表：1 empty，2..6 kind，11..14公开类型。所有返回指针为new reference。 */
API PyObject *rp_inspect_identity(int tag) {
    if (inspect_bridge_ready() < 0) return NULL;
    PyObject *value = NULL;
    if (tag == 1) value = inspect_empty;
    else if (tag >= 2 && tag <= 6) value = inspect_kinds[tag - 2];
    else if (tag == 11) value = inspect_parameter;
    else if (tag == 12) value = inspect_signature;
    else if (tag == 13) value = inspect_bound;
    else if (tag == 14) value = inspect_kind;
    else if (tag == 15) value = inspect_signature_api;
    else if (tag == 16) value = inspect_unwrap_api;
    if (!value) { PyErr_SetString(PyExc_ValueError, "invalid inspect identity tag"); return NULL; }
    return Py_NewRef(value);
}

/* 未观察inspect时仅一个NULL检查，不加载模块、不读用户属性；kind必须先于一般int转换。 */
API int rp_inspect_tag(PyObject *value) {
    if (!inspect_parameter) return 0;
    if (value == inspect_empty) return 1;
    for (int i = 0; i < 5; i++) if (value == inspect_kinds[i]) return i + 2;
    if (value == inspect_parameter) return 11;
    if (value == inspect_signature) return 12;
    if (value == inspect_bound) return 13;
    if (value == inspect_kind) return 14;
    if (value == inspect_signature_api) return 15;
    if (value == inspect_unwrap_api) return 16;
    if (Py_IS_TYPE(value, (PyTypeObject *)inspect_parameter)) return 20;
    if (Py_IS_TYPE(value, (PyTypeObject *)inspect_signature)) return 21;
    if (Py_IS_TYPE(value, (PyTypeObject *)inspect_bound)) return 22;
    return 0;
}

API PyObject *rp_inspect_parameter(PyObject *name, int kind, PyObject *default_value, PyObject *annotation) {
    if (inspect_bridge_ready() < 0) return NULL;
    if (inspect_object_schema(20) < 0) return NULL;
    if (!PyUnicode_CheckExact(name) || kind < 0 || kind > 4) {
        PyErr_SetString(PyExc_TypeError, "invalid native Parameter schema"); return NULL;
    }
    PyObject *result = PyType_GenericAlloc((PyTypeObject *)inspect_parameter, 0);
    if (!result) return NULL;
    if (PyObject_SetAttrString(result, "_name", name) < 0 ||
        PyObject_SetAttrString(result, "_kind", inspect_kinds[kind]) < 0 ||
        PyObject_SetAttrString(result, "_default", default_value) < 0 ||
        PyObject_SetAttrString(result, "_annotation", annotation) < 0) { Py_DECREF(result); return NULL; }
    return result;
}

API PyObject *rp_inspect_signature(PyObject *parameters, PyObject *annotation) {
    if (inspect_bridge_ready() < 0) return NULL;
    if (inspect_object_schema(21) < 0) return NULL;
    if (!PyDict_CheckExact(parameters)) { PyErr_SetString(PyExc_TypeError, "Signature schema requires exact dict"); return NULL; }
    PyObject *mapping = PyDictProxy_New(parameters);
    if (!mapping) return NULL;
    PyObject *result = PyType_GenericAlloc((PyTypeObject *)inspect_signature, 0);
    if (result && (PyObject_SetAttrString(result, "_parameters", mapping) < 0 ||
        PyObject_SetAttrString(result, "_return_annotation", annotation) < 0)) Py_CLEAR(result);
    Py_DECREF(mapping); return result;
}

API PyObject *rp_inspect_bound(PyObject *signature, PyObject *arguments) {
    if (inspect_bridge_ready() < 0) return NULL;
    if (inspect_object_schema(22) < 0) return NULL;
    if (!Py_IS_TYPE(signature, (PyTypeObject *)inspect_signature) || !PyDict_CheckExact(arguments)) {
        PyErr_SetString(PyExc_TypeError, "BoundArguments schema requires Signature and exact dict"); return NULL;
    }
    PyObject *result = PyType_GenericAlloc((PyTypeObject *)inspect_bound, 0);
    if (result && (PyObject_SetAttrString(result, "_signature", signature) < 0 ||
        PyObject_SetAttrString(result, "arguments", arguments) < 0)) Py_CLEAR(result);
    return result;
}

/* 显式反射入口可主动加载schema；普通FromOwned不承担探测用户类型的开销。 */
API int rp_inspect_prepare(void) { return inspect_bridge_ready(); }

/* 精确schema读取只走已验证的slot描述符；返回新引用组成的tuple。 */
API PyObject *rp_inspect_payload(PyObject *value) {
    int tag = rp_inspect_tag(value);
    if (tag >= 20 && tag <= 22 && inspect_object_schema(tag) < 0) return NULL;
    const char *names[4]; int count;
    if (tag == 20) { names[0] = "_name"; names[1] = "_kind"; names[2] = "_default"; names[3] = "_annotation"; count = 4; }
    else if (tag == 21) { names[0] = "_parameters"; names[1] = "_return_annotation"; count = 2; }
    else if (tag == 22) { names[0] = "_signature"; names[1] = "arguments"; count = 2; }
    else { PyErr_SetString(PyExc_TypeError, "inspect payload requires an exact supported object"); return NULL; }
    PyObject *result = PyTuple_New(count);
    if (!result) return NULL;
    for (int i = 0; i < count; i++) {
        PyObject *name = PyUnicode_FromString(names[i]);
        PyObject *item = name ? PyObject_GenericGetAttr(value, name) : NULL;
        Py_XDECREF(name);
        if (item && tag == 21 && i == 0) {
            PyObject *values = PyMapping_Values(item); Py_DECREF(item);
            item = values ? PySequence_Tuple(values) : NULL; Py_XDECREF(values);
        }
        if (!item) { Py_DECREF(result); return NULL; }
        PyTuple_SET_ITEM(result, i, item);
    }
    return result;
}

/* 真实CP函数/简单类/方法的code数据读取，不调用inspect.signature或任何Python正文。
 * 代码属性由解释器CodeType提供，参数默认对象保持原引用；原生descriptor长尾明确拒绝。 */
API PyObject *rp_inspect_function_schema(PyObject *callable) {
    PyObject *function = Py_NewRef(callable); int skip = 0;
    if (PyType_Check(function)) {
        PyObject *init = PyObject_GetAttrString(function, "__init__"); Py_DECREF(function);
        if (!init) return NULL;
        function = init; skip = 1;
    } else if (PyMethod_Check(function)) {
        PyObject *inner = Py_NewRef(PyMethod_Function(function)); Py_DECREF(function);
        function = inner; skip = 1;
    } else if (!PyFunction_Check(function)) {
        PyObject *call = PyObject_GetAttrString(function, "__call__"); Py_DECREF(function);
        if (!call) return NULL;
        function = call;
        if (PyMethod_Check(function)) {
            PyObject *inner = Py_NewRef(PyMethod_Function(function)); Py_DECREF(function);
            function = inner; skip = 1;
        }
    }
    if (!PyFunction_Check(function)) {
        Py_DECREF(function);
        PyErr_SetString(PyExc_ValueError, "external callable requires a real function code schema or explicit Signature"); return NULL;
    }
    PyObject *code = PyFunction_GetCode(function);
    if (!code) { Py_DECREF(function); return NULL; }
    PyObject *result = PyTuple_New(9);
    if (!result) { Py_DECREF(function); return NULL; }
    const char *names[] = {"co_varnames", "co_argcount", "co_posonlyargcount", "co_kwonlyargcount", "co_flags"};
    for (int i = 0; i < 5; i++) {
        PyObject *value = PyObject_GetAttrString(code, names[i]);
        if (!value) { Py_DECREF(function); Py_DECREF(result); return NULL; }
        PyTuple_SET_ITEM(result, i, value);
    }
    PyObject *defaults = PyFunction_GetDefaults(function), *keywords = PyFunction_GetKwDefaults(function);
#if PY_VERSION_HEX >= 0x030e0000
    PyObject *annotate = PyObject_GetAttrString(function, "__annotate__");
    if (!annotate) { Py_DECREF(function); Py_DECREF(result); return NULL; }
    int lazy = !Py_IsNone(annotate); Py_DECREF(annotate);
    if (lazy) { Py_DECREF(function); Py_DECREF(result);
        PyErr_SetString(PyExc_NotImplementedError, "external lazy annotations are not supported"); return NULL; }
#endif
    PyObject *annotations = PyFunction_GetAnnotations(function);
    if (!annotations && PyErr_Occurred()) { Py_DECREF(function); Py_DECREF(result); return NULL; }
    PyTuple_SET_ITEM(result, 5, Py_NewRef(defaults ? defaults : Py_None));
    PyTuple_SET_ITEM(result, 6, Py_NewRef(keywords ? keywords : Py_None));
    PyTuple_SET_ITEM(result, 7, Py_NewRef(annotations ? annotations : Py_None));
    PyObject *receiver = PyLong_FromLong(skip);
    if (!receiver) { Py_DECREF(function); Py_DECREF(result); return NULL; }
    PyTuple_SET_ITEM(result, 8, receiver); Py_DECREF(function); return result;
}
