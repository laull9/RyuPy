/* 通用C API调用现场：固定模块code只调用co_consts中的原生thunk，真实D同时作为globals/locals。
 * 不给业务dict写临时名字；上下文在调用结束立即失效并释放CLR token，逃逸frame/code/thunk只能报错。
 * capsule自身可由CP持有至GC/退出；其dealloc只处理已清空的上下文，绝不回访失效CLR句柄。 */
typedef PyObject *(*rp_carrier_invoke_fn)(intptr_t);
typedef void (*rp_carrier_release_fn)(intptr_t);
typedef struct {
    intptr_t token;
    rp_carrier_invoke_fn invoke;
    rp_carrier_release_fn release;
    PyObject *target, *args, *kwargs;
    int state; /* 1未调用，2活动，3完成，0失效。 */
} RpCallCarrier;
static int64_t rp_carrier_live, rp_carrier_calls;
static void rp_carrier_close(RpCallCarrier *context) {
    context->state = 0;
    intptr_t token = context->token; context->token = 0;
    if (token) context->release(token);
    Py_CLEAR(context->target); Py_CLEAR(context->args); Py_CLEAR(context->kwargs);
}
static void rp_carrier_destroy(PyObject *capsule) {
    RpCallCarrier *context = PyCapsule_GetPointer(capsule, "ryupy.call-carrier.v1");
    if (!context) { PyErr_Clear(); return; }
    PyObject *type, *value, *trace; PyErr_Fetch(&type, &value, &trace);
    rp_carrier_close(context); PyMem_Free(context); rp_carrier_live--;
    PyErr_Restore(type, value, trace);
}
static PyObject *rp_carrier_dispatch(PyObject *capsule, PyObject *unused) {
    (void)unused;
    RpCallCarrier *context = PyCapsule_GetPointer(capsule, "ryupy.call-carrier.v1");
    if (!context) return NULL;
    if (context->state != 1) {
        PyErr_SetString(PyExc_RuntimeError, context->state == 0
            ? "compiled execution context expired" : "compiled execution context already used"); return NULL;
    }
    context->state = 2; rp_carrier_calls++;
    PyObject *result = context->target ? PyObject_Call(context->target, context->args, context->kwargs)
        : context->invoke(context->token);
    context->state = 3; return result;
}
static PyMethodDef rp_carrier_method = {"_ryupy_call_carrier", rp_carrier_dispatch, METH_NOARGS, NULL};
static PyObject *rp_carrier_run(RpCallCarrier *context, PyObject *globals, PyObject *filename) {
    PyObject *capsule = NULL, *thunk = NULL, *template = NULL, *constants = NULL;
    PyObject *replace = NULL, *empty = NULL, *keywords = NULL, *code = NULL, *result = NULL;
    if (!PyDict_CheckExact(globals) || !PyUnicode_Check(filename)) {
        PyErr_SetString(PyExc_TypeError, "call carrier requires exact globals dict and source filename"); goto done;
    }
    capsule = PyCapsule_New(context, "ryupy.call-carrier.v1", rp_carrier_destroy);
    if (!capsule) goto done;
    thunk = PyCFunction_New(&rp_carrier_method, capsule);
    if (!thunk) goto done;
    /* 公开code.replace只替换常量/来源；不手写版本相关bytecode，不执行业务源码。 */
    template = Py_CompileString("(0).__call__()\n", "<ryupy-call-carrier-v1>", Py_eval_input);
    if (!template) goto done;
    PyObject *original = PyObject_GetAttrString(template, "co_consts");
    if (!original) goto done;
    constants = PyTuple_New(PyTuple_GET_SIZE(original));
    if (!constants) { Py_DECREF(original); goto done; }
    int replaced = 0;
    for (Py_ssize_t i = 0; i < PyTuple_GET_SIZE(original); i++) {
        PyObject *item = PyTuple_GET_ITEM(original, i);
        if (PyLong_CheckExact(item) && PyLong_AsLong(item) == 0) {
            item = thunk; replaced++;
        }
        PyTuple_SET_ITEM(constants, i, Py_NewRef(item));
    }
    Py_DECREF(original);
    if (replaced != 1) { PyErr_SetString(PyExc_RuntimeError, "invalid call carrier template"); goto done; }
    replace = PyObject_GetAttrString(template, "replace"); empty = PyTuple_New(0); keywords = PyDict_New();
    if (!replace || !empty || !keywords || PyDict_SetItemString(keywords, "co_consts", constants) < 0
        || PyDict_SetItemString(keywords, "co_filename", filename) < 0) goto done;
    code = PyObject_Call(replace, empty, keywords);
    if (code) result = PyEval_EvalCode(code, globals, globals);
done:
    {
        PyObject *type, *value, *trace; PyErr_Fetch(&type, &value, &trace);
        /* 先失效再DECREF：析构/回调重入也不能重复执行或触及已free token。 */
        rp_carrier_close(context);
        Py_XDECREF(code); Py_XDECREF(keywords); Py_XDECREF(empty); Py_XDECREF(replace);
        Py_XDECREF(constants); Py_XDECREF(template); Py_XDECREF(thunk);
        if (capsule) Py_DECREF(capsule); else { PyMem_Free(context); rp_carrier_live--; }
        PyErr_Restore(type, value, trace);
    }
    return result;
}
/* 无论成功失败均消费token；托管端不在native释放后再次Free。 */
API PyObject *rp_execute_carrier(PyObject *globals, PyObject *filename, intptr_t token,
    rp_carrier_invoke_fn invoke, rp_carrier_release_fn release) {
    RpCallCarrier *context = PyMem_Calloc(1, sizeof(*context));
    if (!context) { release(token); return PyErr_NoMemory(); }
    rp_carrier_live++; context->state = 1;
    context->token = token; context->invoke = invoke; context->release = release;
    return rp_carrier_run(context, globals, filename);
}
/* 外部Host没有Python frame时复用真实__main__ D；现有Python调用现场始终保留。 */
static PyObject *rp_call_with_frame(PyObject *target, PyObject *args, PyObject *kwargs) {
    if (PyEval_GetFrame()) return PyObject_Call(target, args, kwargs);
    PyObject *module = PyImport_AddModule("__main__");
    if (!module) return NULL;
    if (!PyModule_Check(module)) {
        PyErr_SetString(PyExc_NotImplementedError, "native caller frame requires real module globals"); return NULL;
    }
    PyObject *globals = PyModule_GetDict(module);
    PyObject *filename = PyDict_GetItemString(globals, "__file__");
    filename = filename && PyUnicode_Check(filename) ? Py_NewRef(filename) : PyUnicode_FromString("<external CPython call>");
    if (!filename) return NULL;
    RpCallCarrier *context = PyMem_Calloc(1, sizeof(*context));
    if (!context) { Py_DECREF(filename); return PyErr_NoMemory(); }
    rp_carrier_live++; context->state = 1;
    context->target = Py_NewRef(target); context->args = Py_NewRef(args); context->kwargs = Py_XNewRef(kwargs);
    PyObject *result = rp_carrier_run(context, globals, filename); Py_DECREF(filename); return result;
}
API void rp_carrier_stats(int64_t *live, int64_t *calls) { *live = rp_carrier_live; *calls = rp_carrier_calls; }
