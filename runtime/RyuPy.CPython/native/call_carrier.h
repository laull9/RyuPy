/* 通用C API调用现场：固定模块code只调用co_consts中的原生thunk，真实D同时作为globals/locals。
 * 不给业务dict写临时名字；上下文在调用结束立即失效并释放CLR token，逃逸frame/code/thunk只能报错。
 * capsule自身可由CP持有至GC/退出；其dealloc只处理已清空的上下文，绝不回访失效CLR句柄。 */
typedef PyObject *(*rp_carrier_invoke_fn)(intptr_t);
typedef void (*rp_carrier_release_fn)(intptr_t);
typedef struct RpCallCarrier {
    struct RpCallCarrier *previous, *next;
    int registered;
    PyObject *globals; /* 活动eval期间借用，由run的owned参数保活。 */
    intptr_t token;
    rp_carrier_invoke_fn invoke;
    rp_carrier_release_fn release;
    PyObject *target, *args, *kwargs, *captured_builtins;
    PyObject *code; /* 仅活动eval期间借用，栈上code强保活；失效时清空，不能被地址复用伪装。 */
    int state; /* 1未调用，2活动，3完成，0失效。 */
} RpCallCarrier;
static int64_t rp_carrier_live, rp_carrier_calls;
/* GIL保护的活动链；多线程释放GIL后可以交错返回，因此按节点unlink而非假设全局LIFO。 */
static RpCallCarrier *rp_carrier_active;
static void rp_carrier_register(RpCallCarrier *context, PyObject *code, PyObject *globals) {
    context->code = code; context->globals = globals; context->registered = 1;
    context->next = rp_carrier_active;
    if (rp_carrier_active) rp_carrier_active->previous = context;
    rp_carrier_active = context;
}
static void rp_carrier_unregister(RpCallCarrier *context) {
    if (!context->registered) return;
    if (context->previous) context->previous->next = context->next;
    else rp_carrier_active = context->next;
    if (context->next) context->next->previous = context->previous;
    context->previous = context->next = NULL; context->registered = 0;
    context->code = NULL; context->globals = NULL;
}
static void rp_carrier_close(RpCallCarrier *context) {
    rp_carrier_unregister(context); context->state = 0;
    intptr_t token = context->token; context->token = 0;
    if (token) context->release(token);
    Py_CLEAR(context->target); Py_CLEAR(context->args); Py_CLEAR(context->kwargs); Py_CLEAR(context->captured_builtins);
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
    PyObject *replace = NULL, *empty = NULL, *keywords = NULL, *code = NULL, *function = NULL, *result = NULL;
    int own_arguments = 0;
    if (!PyDict_CheckExact(globals) || !PyUnicode_Check(filename)) {
        PyErr_SetString(PyExc_TypeError, "call carrier requires exact globals dict and source filename"); goto done;
    }
    Py_INCREF(globals); Py_INCREF(filename); own_arguments = 1;
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
    if (code) { PyObject *tagged = rp_code_origin_attach(code, 2); Py_DECREF(code); code = tagged; }
    if (code) function = PyFunction_New(code, globals);
    if (function && context->captured_builtins) {
#if PY_VERSION_HEX >= 0x030a0000
        /* 完整目标CPython头的owned字段；新函数尚未执行/特化，定义点builtin视图不写回业务D。 */
        Py_SETREF(((PyFunctionObject *)function)->func_builtins, Py_NewRef(context->captured_builtins));
#else
        PyErr_SetString(PyExc_NotImplementedError, "captured builtins carrier requires CPython 3.10+");
        Py_CLEAR(function);
#endif
    }
    if (function) { rp_carrier_register(context, code, globals); result = PyObject_CallNoArgs(function); }
done:
    {
        PyObject *type, *value, *trace; PyErr_Fetch(&type, &value, &trace);
        /* 先失效再DECREF：析构/回调重入也不能重复执行或触及已free token。 */
        rp_carrier_close(context);
        Py_XDECREF(function); Py_XDECREF(code); Py_XDECREF(keywords); Py_XDECREF(empty); Py_XDECREF(replace);
        Py_XDECREF(constants); Py_XDECREF(template); Py_XDECREF(thunk);
        if (own_arguments) { Py_DECREF(globals); Py_DECREF(filename); }
        if (capsule) Py_DECREF(capsule); else { PyMem_Free(context); rp_carrier_live--; }
        PyErr_Restore(type, value, trace);
    }
    return result;
}
/* 无论成功失败均消费token；托管端不在native释放后再次Free。 */
API PyObject *rp_execute_carrier_builtins(PyObject *globals, PyObject *builtins, PyObject *filename, intptr_t token,
    rp_carrier_invoke_fn invoke, rp_carrier_release_fn release) {
    RpCallCarrier *context = PyMem_Calloc(1, sizeof(*context));
    if (!context) { release(token); return PyErr_NoMemory(); }
    rp_carrier_live++; context->state = 1;
    context->token = token; context->invoke = invoke; context->release = release;
    context->captured_builtins = Py_XNewRef(builtins);
    return rp_carrier_run(context, globals, filename);
}
/* 旧内部ABI继续按D创建时捕获builtins，新Runtime明确提供定义点视图。 */
API PyObject *rp_execute_carrier(PyObject *globals, PyObject *filename, intptr_t token,
    rp_carrier_invoke_fn invoke, rp_carrier_release_fn release) {
    return rp_execute_carrier_builtins(globals, NULL, filename, token, invoke, release);
}
/* 外部Host没有Python frame时复用真实__main__ D；现有Python调用现场始终保留。 */
API PyObject *rp_call_with_frame(PyObject *target, PyObject *args, PyObject *kwargs) {
    if (PyEval_GetFrame()) return PyObject_Call(target, args, kwargs);
    PyObject *module = PyImport_AddModule("__main__");
    if (!module) return NULL;
    if (!PyModule_Check(module)) {
        PyErr_SetString(PyExc_NotImplementedError, "native caller frame requires real module globals"); return NULL;
    }
    PyObject *globals = Py_NewRef(PyModule_GetDict(module));
    PyObject *filename = PyDict_GetItemString(globals, "__file__");
    filename = filename && PyUnicode_Check(filename) ? Py_NewRef(filename) : PyUnicode_FromString("<external CPython call>");
    if (!filename) { Py_DECREF(globals); return NULL; }
    RpCallCarrier *context = PyMem_Calloc(1, sizeof(*context));
    if (!context) { Py_DECREF(filename); Py_DECREF(globals); return PyErr_NoMemory(); }
    rp_carrier_live++; context->state = 1;
    context->target = Py_NewRef(target); context->args = Py_NewRef(args); context->kwargs = Py_XNewRef(kwargs);
    PyObject *result = rp_carrier_run(context, globals, filename); Py_DECREF(filename); Py_DECREF(globals); return result;
}
API void rp_carrier_stats(int64_t *live, int64_t *calls) { *live = rp_carrier_live; *calls = rp_carrier_calls; }

/* 返回0=无frame，1=真实Python frame，2=本桥活动carrier；两种活动现场均返回owned D/builtins。
 * 无活动carrier时不读code；活动时只查活动链，绝不遍历用户co_consts。 */
API int rp_carrier_frame_context_builtins(PyObject **globals, PyObject **builtins) {
    *globals = NULL; *builtins = NULL;
    PyFrameObject *frame = PyEval_GetFrame();
    if (!frame) return PyErr_Occurred() ? -1 : 0;
    RpCallCarrier *found = NULL;
    if (rp_carrier_active) {
        PyObject *code = (PyObject *)PyFrame_GetCode(frame);
        if (!code) return -1;
        for (RpCallCarrier *context = rp_carrier_active; context; context = context->next)
            if (context->code == code) { found = context; break; }
        Py_DECREF(code);
    }
#if PY_VERSION_HEX >= 0x030b0000
    *globals = PyFrame_GetGlobals(frame);
    if (*globals) *builtins = PyFrame_GetBuiltins(frame);
#else
    *globals = PyObject_GetAttrString((PyObject *)frame, "f_globals");
    if (*globals) *builtins = PyObject_GetAttrString((PyObject *)frame, "f_builtins");
#endif
    if (!*builtins) { Py_CLEAR(*globals); return -1; }
    return found && *globals == found->globals ? 2 : 1;
}
API int rp_carrier_frame_context(PyObject **globals) {
    PyObject *builtins = NULL; int result = rp_carrier_frame_context_builtins(globals, &builtins);
    Py_XDECREF(builtins); return result;
}
API PyObject *rp_execute_external_carrier(intptr_t token, rp_carrier_invoke_fn invoke, rp_carrier_release_fn release) {
    PyObject *module = PyDict_GetItemString(PyImport_GetModuleDict(), "__main__");
    int created = module == NULL;
    if (created) module = PyImport_AddModule("__main__");
    if (!module || !PyModule_Check(module)) {
        release(token);
        if (module) PyErr_SetString(PyExc_NotImplementedError, "native caller frame requires real module globals");
        return NULL;
    }
    PyObject *globals = Py_NewRef(PyModule_GetDict(module));
    /* 失败入口删除sys.modules后，标准新__main__初始化需安装canonical builtins。
     * 已有业务D的删键语义保留：available检查会选择旧无frame外部调用，不偷偷补回键。 */
    if (created && PyDict_SetItemString(globals, "__builtins__", PyEval_GetBuiltins()) < 0) {
        release(token); Py_DECREF(globals); return NULL;
    }
    PyObject *source = PyDict_GetItemString(globals, "__file__");
    source = source && PyUnicode_Check(source) ? Py_NewRef(source) : PyUnicode_FromString("<external CPython call>");
    if (!source) { release(token); Py_DECREF(globals); return NULL; }
    PyObject *result = rp_execute_carrier(globals, source, token, invoke, release);
    Py_DECREF(source); Py_DECREF(globals); return result;
}

/* 非module的旧sys.modules替换对象不被强转或新NI覆盖，旧调用保持原协议。 */
API int rp_external_carrier_available(void) {
    PyObject *module = PyDict_GetItemString(PyImport_GetModuleDict(), "__main__");
    return !module || (PyModule_Check(module)
        && PyDict_GetItemString(PyModule_GetDict(module), "__builtins__") != NULL);
}
