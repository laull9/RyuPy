/* 原生构造code的身份来源：code常量中的capsule只持weakref，纯C借用链没有Python强根。
 * code复制/replace不继承原weakref目标；逃逸weak callback使用单调id查活登记，不能持失效C指针。 */
typedef struct RpCodeOrigin {
    struct RpCodeOrigin *previous, *next;
    uint64_t id;
    int kind, linked;
    PyObject *weak;
} RpCodeOrigin;
static RpCodeOrigin *rp_code_origins;
static uint64_t rp_code_origin_next;
static int64_t rp_code_origin_records, rp_code_origin_active;
static void rp_code_origin_unlink(RpCodeOrigin *entry) {
    if (!entry->linked) return;
    if (entry->previous) entry->previous->next = entry->next; else rp_code_origins = entry->next;
    if (entry->next) entry->next->previous = entry->previous;
    rp_code_origin_active--; entry->linked = 0; entry->next = entry->previous = NULL;
}
static int rp_code_origin_matches(PyObject *weak, PyObject *code) {
#if PY_VERSION_HEX >= 0x030d0000
    PyObject *target = NULL;
    int live = PyWeakref_GetRef(weak, &target);
    if (live < 0) return 0; /* 本文件只保存有效weakref，不执行用户协议。 */
    int matches = code ? live && target == code : !live;
    Py_XDECREF(target); return matches;
#else
    PyObject *target = PyWeakref_GetObject(weak);
    return code ? target == code : target == Py_None;
#endif
}
static PyObject *rp_code_origin_dead(PyObject *self, PyObject *weak) {
    uint64_t id = PyLong_AsUnsignedLongLong(self);
    if (PyErr_Occurred()) return NULL;
    for (RpCodeOrigin *entry = rp_code_origins; entry; entry = entry->next)
        if (entry->id == id) {
            /* 手工调用活weakref的callback无效；只接受真正清空的同一weakref。 */
            if (entry->weak == weak && rp_code_origin_matches(weak, NULL)) rp_code_origin_unlink(entry);
            break;
        }
    Py_RETURN_NONE;
}
static PyMethodDef rp_code_origin_callback = {"_ryupy_code_origin_dead", rp_code_origin_dead, METH_O, NULL};
static void rp_code_origin_destroy(PyObject *capsule) {
    PyObject *type, *value, *trace; PyErr_Fetch(&type, &value, &trace);
    RpCodeOrigin *entry = PyCapsule_GetPointer(capsule, "ryupy.code-origin.v1");
    if (entry) { rp_code_origin_unlink(entry); Py_XDECREF(entry->weak); PyMem_Free(entry); rp_code_origin_records--; }
    PyErr_Clear(); PyErr_Restore(type, value, trace);
}
static int rp_code_origin_kind(PyObject *code) {
    for (RpCodeOrigin *entry = rp_code_origins; entry; entry = entry->next)
        if (rp_code_origin_matches(entry->weak, code)) return entry->kind;
    return 0;
}
/* 仅用于本shim生成的固定code；过滤旧的末尾见证后追加新见证，字节码及其实际常量索引保持原值。 */
static PyObject *rp_code_origin_attach(PyObject *code, int kind) {
    RpCodeOrigin *entry = PyMem_Calloc(1, sizeof(*entry));
    if (!entry) return PyErr_NoMemory();
    if (rp_code_origin_next == UINT64_MAX) {
        PyMem_Free(entry); PyErr_SetString(PyExc_OverflowError, "code origin id exhausted"); return NULL;
    }
    entry->id = ++rp_code_origin_next; entry->kind = kind;
    PyObject *capsule = PyCapsule_New(entry, "ryupy.code-origin.v1", rp_code_origin_destroy);
    if (!capsule) { PyMem_Free(entry); return NULL; }
    rp_code_origin_records++;
    PyObject *original = PyObject_GetAttrString(code, "co_consts"), *constants = NULL, *method = NULL;
    PyObject *args = NULL, *kwargs = NULL, *result = NULL, *id = NULL, *callback = NULL;
    if (!original) goto done;
    Py_ssize_t count = PyTuple_GET_SIZE(original);
    while (count > 0 && PyCapsule_IsValid(PyTuple_GET_ITEM(original, count-1), "ryupy.code-origin.v1")) count--;
    constants = PyTuple_New(count+1);
    if (!constants) goto done;
    for (Py_ssize_t i=0; i<count; i++) PyTuple_SET_ITEM(constants, i, Py_NewRef(PyTuple_GET_ITEM(original, i)));
    PyTuple_SET_ITEM(constants, count, Py_NewRef(capsule));
    method = PyObject_GetAttrString(code, "replace"); args = PyTuple_New(0); kwargs = PyDict_New();
    if (!method || !args || !kwargs || PyDict_SetItemString(kwargs, "co_consts", constants) < 0) goto done;
    result = PyObject_Call(method, args, kwargs);
    if (!result) goto done;
    id = PyLong_FromUnsignedLongLong(entry->id);
    callback = id ? PyCFunction_New(&rp_code_origin_callback, id) : NULL;
    entry->weak = callback ? PyWeakref_NewRef(result, callback) : NULL;
    if (!entry->weak) { Py_CLEAR(result); goto done; }
    entry->next = rp_code_origins;
    if (rp_code_origins) rp_code_origins->previous = entry;
    rp_code_origins = entry; entry->linked = 1; rp_code_origin_active++;
done:
    Py_XDECREF(callback); Py_XDECREF(id); Py_XDECREF(kwargs); Py_XDECREF(args); Py_XDECREF(method);
    Py_XDECREF(constants); Py_XDECREF(original); Py_DECREF(capsule); return result;
}
API int rp_code_origin(PyObject *code) { return rp_code_origin_kind(code); }

API void rp_code_origin_stats(int64_t *records, int64_t *active) { *records = rp_code_origin_records; *active = rp_code_origin_active; }
