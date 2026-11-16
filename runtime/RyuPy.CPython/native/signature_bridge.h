/* ADR0219：真实inspect身份和受信slot schema。只用公开C API分配/填写对象，
 * 不执行inspect.py构造器/绑定器，不复制Python源码，不读取CPython私有布局偏移。
 * 配置按解释器惰性初始化；schema不符给出明确错误，停止本次反射操作。 */
static PyObject *inspect_parameter, *inspect_signature, *inspect_bound, *inspect_empty, *inspect_kind;
static PyObject *inspect_kinds[5];
static PyObject *inspect_signature_api, *inspect_unwrap_api;
static PyObject *inspect_parameter_fields[4], *inspect_signature_fields[2], *inspect_bound_fields[2];
static int inspect_profile_captured;
static PyObject *inspect_module;

/* 私有模块状态拥有cache引用；全局变量只借用当前owner，atexit期间不提前清理。 */
#define INSPECT_CACHE_COUNT 21
typedef struct {
    PyObject *refs[INSPECT_CACHE_COUNT];
    PyObject *owner;
    uint64_t generation;
    int owned, cleared, freed;
} InspectProfileState;
static InspectProfileState *inspect_active_owner;
static uint64_t inspect_generation;
static int64_t inspect_owners_created, inspect_owners_cleared, inspect_owners_freed;
static PyObject **inspect_cache_slots[INSPECT_CACHE_COUNT] = {
    &inspect_parameter, &inspect_signature, &inspect_bound, &inspect_empty, &inspect_kind,
    &inspect_kinds[0], &inspect_kinds[1], &inspect_kinds[2], &inspect_kinds[3], &inspect_kinds[4],
    &inspect_signature_api, &inspect_unwrap_api,
    &inspect_parameter_fields[0], &inspect_parameter_fields[1], &inspect_parameter_fields[2], &inspect_parameter_fields[3],
    &inspect_signature_fields[0], &inspect_signature_fields[1], &inspect_bound_fields[0], &inspect_bound_fields[1], &inspect_module
};
static void inspect_aliases_null(void) {
    for (int i = 0; i < INSPECT_CACHE_COUNT; i++) *inspect_cache_slots[i] = NULL;
    inspect_active_owner = NULL;
}
static int inspect_owner_traverse(PyObject *module, visitproc visit, void *arg) {
    InspectProfileState *state = PyModule_GetState(module);
    if (state) for (int i = 0; i < INSPECT_CACHE_COUNT; i++) Py_VISIT(state->refs[i]);
    return 0;
}
static int inspect_owner_clear(PyObject *module) {
    InspectProfileState *state = PyModule_GetState(module);
    if (!state || !state->owned || state->cleared) return 0;
    state->cleared = 1; inspect_owners_cleared++;
    /* 旧owner可能在同profile重建后才析构，绝不能清掉新owner的借用别名。 */
    if (inspect_active_owner == state && state->owner == module && state->generation == inspect_generation)
        inspect_aliases_null();
    for (int i = 0; i < INSPECT_CACHE_COUNT; i++) Py_CLEAR(state->refs[i]);
    return 0;
}
static void inspect_owner_free(void *module) {
    InspectProfileState *state = PyModule_GetState((PyObject *)module);
    inspect_owner_clear((PyObject *)module);
    if (state && state->owned && !state->freed) { state->freed = 1; inspect_owners_freed++; }
}
static struct PyModuleDef inspect_owner_definition = {
    PyModuleDef_HEAD_INIT, "_ryupy_inspect_profile", NULL, sizeof(InspectProfileState),
    NULL, NULL, inspect_owner_traverse, inspect_owner_clear, inspect_owner_free
};
static int inspect_owner_publish(int clone) {
    InspectProfileState *previous = inspect_active_owner;
    PyObject *module = PyModule_Create(&inspect_owner_definition);
    if (!module) return -1;
    InspectProfileState *state = PyModule_GetState(module);
    if (!state) { Py_DECREF(module); return -1; }
    state->owner = module; state->generation = ++inspect_generation; state->owned = 1;
    inspect_owners_created++;
    for (int i = 0; i < INSPECT_CACHE_COUNT; i++) {
        state->refs[i] = *inspect_cache_slots[i];
        if (clone) Py_XINCREF(state->refs[i]);
    }
    inspect_active_owner = state;
    if (PyDict_SetItemString(PyImport_GetModuleDict(), "_ryupy_inspect_profile", module) < 0) {
        inspect_active_owner = previous;
        if (previous) {
            inspect_generation = previous->generation;
            for (int i = 0; i < INSPECT_CACHE_COUNT; i++) *inspect_cache_slots[i] = previous->refs[i];
        } else inspect_aliases_null();
        Py_DECREF(module); return -1;
    }
    Py_DECREF(module); return 0;
}
/* 仅初始化失败清理仍在本地拥有的引用；正常stop由模块状态在atexit之后释放。 */
static void inspect_bridge_clear(void) {
    if (inspect_active_owner) { inspect_owner_clear(inspect_active_owner->owner); return; }
    for (int i = 0; i < INSPECT_CACHE_COUNT; i++) Py_CLEAR(*inspect_cache_slots[i]);
}
/* Finalize之后只清借用指针和读取整数，绝不触及Python引用计数。 */
static void inspect_bridge_closed_aliases(void) { inspect_aliases_null(); }
API int64_t rp_inspect_lifecycle(int field) {
    if (field == 0) return inspect_active_owner != NULL;
    if (field == 1) return inspect_owners_created;
    if (field == 2) return inspect_owners_cleared;
    if (field == 3) return inspect_owners_freed;
    if (field == 4) {
        for (int i = 0; i < INSPECT_CACHE_COUNT; i++) if (*inspect_cache_slots[i]) return 0;
        return 1;
    }
    return -1;
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

/* API函数属性可被用户替换并原样往返；静态类型/empty/kind是冻结schema，变更必须拒绝。 */
static int inspect_profile_valid(void) {
    const char *names[] = {"Parameter", "Signature", "BoundArguments", "_empty", "_ParameterKind"};
    PyObject *values[] = {inspect_parameter, inspect_signature, inspect_bound, inspect_empty, inspect_kind};
    PyObject *dictionary = PyModule_GetDict(inspect_module);
    if (!dictionary) return -1;
    for (int i = 0; i < 5; i++) {
        if (PyDict_GetItemString(dictionary, names[i]) != values[i]) goto changed;
    }
    PyObject *parameter_empty = PyObject_GetAttrString(inspect_parameter, "empty");
    PyObject *signature_empty = PyObject_GetAttrString(inspect_signature, "empty");
    int valid = parameter_empty == inspect_empty && signature_empty == inspect_empty;
    Py_XDECREF(parameter_empty); Py_XDECREF(signature_empty);
    if (PyErr_Occurred()) return -1;
    if (!valid) goto changed;
    const char *kinds[] = {"POSITIONAL_ONLY", "POSITIONAL_OR_KEYWORD", "VAR_POSITIONAL", "KEYWORD_ONLY", "VAR_KEYWORD"};
    for (int i = 0; i < 5; i++) {
        PyObject *current = PyObject_GetAttrString(inspect_parameter, kinds[i]);
        if (!current) return -1;
        int same = current == inspect_kinds[i]; Py_DECREF(current);
        if (!same) goto changed;
    }
    return 0;
changed:
    PyErr_SetString(PyExc_NotImplementedError, "inspect trusted type/empty/kind schema was replaced"); return -1;
}
static int inspect_bridge_ready(void) {
    if (inspect_parameter) {
        if (inspect_profile_valid() < 0) return -1;
        PyObject *registered = PyDict_GetItemString(PyImport_GetModuleDict(), "_ryupy_inspect_profile");
        if (!inspect_active_owner || inspect_active_owner->cleared) {
            PyErr_SetString(PyExc_NotImplementedError, "inspect profile owner is unavailable"); return -1;
        }
        if (registered != inspect_active_owner->owner) {
            if (registered) { PyErr_SetString(PyExc_NotImplementedError, "inspect profile owner was replaced"); return -1; }
            /* 旧owner仍被调用者持有时，复制已冻结引用重建，不按模块名字重抓用户可变属性。 */
            return inspect_owner_publish(1);
        }
        return 0;
    }
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
    inspect_module = module;
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
    if (inspect_owner_publish(0) < 0) goto fail;
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
    int tag = 0;
    if (value == inspect_empty) tag = 1;
    for (int i = 0; i < 5; i++) if (value == inspect_kinds[i]) tag = i + 2;
    if (value == inspect_parameter) tag = 11;
    if (value == inspect_signature) tag = 12;
    if (value == inspect_bound) tag = 13;
    if (value == inspect_kind) tag = 14;
    if (value == inspect_signature_api) tag = 15;
    if (value == inspect_unwrap_api) tag = 16;
    if (Py_IS_TYPE(value, (PyTypeObject *)inspect_parameter)) tag = 20;
    if (Py_IS_TYPE(value, (PyTypeObject *)inspect_signature)) tag = 21;
    if (Py_IS_TYPE(value, (PyTypeObject *)inspect_bound)) tag = 22;
    return tag && inspect_bridge_ready() < 0 ? -1 : tag;
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
    if (tag < 0) return NULL;
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
