/* 显式 stock A 薄函数：真实 closure/cell -> P -> GC owner -> F 的可见强环。
 * owner 内的 context 仅有纯 core token 与借用登记链；K 没有 CP/Host/owner 强回边。
 * 模板编译一次、不执行 factory；code 常量不含实例 P，无 weak/watcher/capsule 依赖。
 */
#include "stock_function_owner.h"

typedef PyObject *(*rp_thin_invoke_fn)(intptr_t, PyObject *, PyObject *);
typedef void (*rp_thin_release_fn)(intptr_t);
typedef struct RpThinLink {
    intptr_t token;
    RpFunctionOwner *owner; /* 仅借用：owner 的 dealloc 在任何 DECREF 前摘链。 */
    struct RpThinLink *next;
} RpThinLink;
static RpThinLink *rp_thin_links;
static PyObject *rp_thin_template, *rp_thin_frame_guards;
static const char *rp_thin_frame_names[] = {"globals", "locals", "vars", "eval", "exec", "compile"};
static rp_thin_invoke_fn rp_thin_invoke;
static rp_thin_release_fn rp_thin_release;
static long rp_thin_live, rp_thin_destroyed, rp_thin_calls;
static int rp_thin_audit_seen, rp_thin_enabled, rp_thin_program_terminal;
static PyObject *rp_thin_dispatch(PyObject *self, PyObject *argument);

static RpThinLink *rp_thin_find(PyObject *f) {
    if (!PyFunction_Check(f)) return NULL;
    for (RpThinLink *link = rp_thin_links; link; link = link->next)
        if (rp_fo_borrow(link->owner) == f) return link;
    return NULL;
}
static int rp_thin_audit(const char *event, PyObject *args, void *unused) {
    (void)unused;
    if (!strcmp(event, "ryupy.stock_thin.audit_probe")) { rp_thin_audit_seen = 1; return 0; }
    if (strcmp(event, "object.__setattr__") || PyTuple_Size(args) != 3) return 0;
    PyObject *name = PyTuple_GetItem(args, 1);
    if (PyUnicode_Check(name) && !PyUnicode_CompareWithASCIIString(name, "__code__") &&
        rp_thin_find(PyTuple_GetItem(args, 0))) {
        PyErr_SetString(PyExc_NotImplementedError, "已登记薄函数禁止替换 __code__"); return -1;
    }
    return 0;
}
static void rp_thin_detach(void *context) {
    RpThinLink *link = context;
    RpThinLink **cursor = &rp_thin_links;
    while (*cursor && *cursor != link) cursor = &(*cursor)->next;
    if (*cursor) *cursor = link->next;
}
static void rp_thin_dispose(void *context) {
    RpThinLink *link = context;
    /* 只有 owner 最终析构在这里交还 token；tp_clear/解绑/摘链均不重复释放。 */
    rp_thin_release(link->token);
    free(link); rp_thin_live--; rp_thin_destroyed++;
    /* core释放仍纯托管；独立终态通知在GIL内、最后CIL入口已析构后退余下弱owned池。 */
    if (rp_thin_program_terminal && !rp_thin_live) {
        PyObject *saved = PyErr_GetRaisedException();
        PyObject *result = dispatch(-2, 30, NULL, NULL, NULL, 0, NULL);
        Py_XDECREF(result); PyErr_Clear(); PyErr_SetRaisedException(saved);
    }
}
static int rp_thin_shape(RpThinLink *link, PyObject *f) {
    if (!PyFunction_Check(f) || PyFunction_GetCode(f) != link->owner->expected_code ||
        PyFunction_GetDefaults(f) || PyFunction_GetKwDefaults(f)) return 0;
    PyObject *closure = PyFunction_GetClosure(f);
    if (!closure || PyTuple_Size(closure) != 1) return 0;
    PyObject *cell = PyTuple_GetItem(closure, 0);
    if (!PyCell_Check(cell)) return 0;
    PyObject *p = PyCell_Get(cell); /* owned 当前 cell 值，绝不解引用旧借用 P 地址。 */
    if (!p) { PyErr_Clear(); return 0; }
    int valid = PyCFunction_Check(p) && PyCFunction_GetFunction(p) == rp_thin_dispatch &&
        PyCFunction_GetSelf(p) == (PyObject *)link->owner;
    Py_DECREF(p);
    return valid;
}
API int rp_thin_validate(PyObject *f) {
    RpThinLink *link = rp_thin_find(f);
    if (link && rp_thin_shape(link, f)) return 0;
    PyErr_SetString(PyExc_NotImplementedError, "薄函数已释放或形状失效"); return -1;
}
static PyObject *rp_thin_dispatch(PyObject *self, PyObject *argument) {
    if (!rp_fo_type || !Py_IS_TYPE(self, (PyTypeObject *)rp_fo_type)) {
        PyErr_SetString(PyExc_NotImplementedError, "未知薄函数 owner"); return NULL;
    }
    RpThinLink *link = rp_fo_context(self);
    if (!link) { PyErr_SetString(PyExc_NotImplementedError, "薄函数环境已清理"); return NULL; }
    PyObject *f = rp_fo_borrow(link->owner);
    if (f == Py_None || !rp_thin_shape(link, f)) {
        PyErr_SetString(PyExc_NotImplementedError, "薄函数已释放或形状失效"); return NULL;
    }
    /* owner 正式拥有 F；跨 frame 获取、托管转换和任意重入再独立保活。 */
    Py_INCREF(f);
    PyFrameObject *current = PyEval_GetFrame();
    PyObject *frame = current ? Py_NewRef((PyObject *)current) : NULL;
    PyObject *code = frame ? (PyObject *)PyFrame_GetCode((PyFrameObject *)frame) : NULL;
#if PY_VERSION_HEX >= 0x030b0000
    PyObject *globals = frame ? PyFrame_GetGlobals((PyFrameObject *)frame) : NULL;
#else
    PyObject *globals = frame ? PyObject_GetAttrString(frame, "f_globals") : NULL;
#endif
    int valid = code == link->owner->expected_code && globals == PyFunction_GetGlobals(f);
    Py_XDECREF(code); Py_XDECREF(globals); Py_XDECREF(frame);
    if (!valid) {
        Py_DECREF(f);
        if (!PyErr_Occurred()) PyErr_SetString(PyExc_NotImplementedError, "拒绝薄函数复制或不同 globals 调用");
        return NULL;
    }
    rp_thin_calls++;
    PyObject *result = rp_thin_invoke(link->token, f, argument);
    Py_DECREF(f);
    return result;
}
static PyMethodDef rp_thin_dispatch_def = {
    "cil_dispatch", rp_thin_dispatch, METH_O, "静态 CIL 薄函数入口"
};
/* 提取内嵌 f 的真实 code；没有 factory 调用或创建期 return trace。 */
static PyObject *rp_thin_find_code(PyObject *code) {
    PyObject *constants = PyObject_GetAttrString(code, "co_consts");
    if (!constants) return NULL;
    PyObject *result = NULL;
    for (Py_ssize_t i = 0; i < PyTuple_Size(constants) && !result; i++) {
        PyObject *item = PyTuple_GetItem(constants, i);
        if (!PyCode_Check(item)) continue;
        PyObject *name = PyObject_GetAttrString(item, "co_name");
        if (!name) break;
        if (!PyUnicode_CompareWithASCIIString(name, "f")) result = Py_NewRef(item);
        Py_DECREF(name);
        if (!result) result = rp_thin_find_code(item);
        if (PyErr_Occurred()) break;
    }
    Py_DECREF(constants);
    return result;
}
API int rp_thin_enable(rp_thin_invoke_fn invoke, rp_thin_release_fn release) {
    if (rp_thin_enabled) return 0;
    if (!invoke || !release) { PyErr_SetString(PyExc_ValueError, "缺少静态回调"); return -1; }
#if PY_VERSION_HEX < 0x030c0000 || PY_VERSION_HEX >= 0x030e0000
    PyErr_SetString(PyExc_NotImplementedError, "stock 薄函数目前仅验收 3.12/3.13 GIL"); return -1;
#endif
    /* AddAuditHook 可被已有 hook 无异常地拒绝；必须用实际事件核对安装成功。 */
    rp_thin_audit_seen = 0;
    if (PySys_AddAuditHook(rp_thin_audit, NULL) < 0) return -1;
    if (PySys_Audit("ryupy.stock_thin.audit_probe", NULL) < 0) return -1;
    if (!rp_thin_audit_seen) {
        PyErr_SetString(PyExc_NotImplementedError, "薄函数 audit hook 安装被拒绝"); return -1;
    }
    PyObject *module = Py_CompileString("def factory(p):\n def f(x):\n  return p(x)\n return f\n",
                                        "<ryupy-stock-thin-A>", Py_file_input);
    if (!module) return -1;
    PyObject *code = rp_thin_find_code(module);
    Py_DECREF(module);
    if (!code) {
        if (!PyErr_Occurred()) PyErr_SetString(PyExc_RuntimeError, "缺少 A 胶水模板");
        return -1;
    }
    if (rp_fo_start() < 0) { Py_DECREF(code); return -1; }
    PyObject *guards = PyTuple_New(8);
    if (!guards) { Py_DECREF(code); rp_fo_stop(); return -1; }
    PyObject *builtins = PyEval_GetBuiltins();
    for (int i = 0; i < 6; i++) {
        PyObject *value = PyDict_GetItemString(builtins, rp_thin_frame_names[i]);
        if (!value) {
            Py_DECREF(guards); Py_DECREF(code); rp_fo_stop();
            PyErr_SetString(PyExc_RuntimeError, "规范frame builtin缺失"); return -1;
        }
        PyTuple_SET_ITEM(guards, i, Py_NewRef(value));
    }
    /* 标准库partial以真实原生type身份冻结；公共字段读取，无私有结构布局。 */
    PyObject *functools = PyImport_ImportModule("_functools");
    PyObject *partial = functools ? PyObject_GetAttrString(functools, "partial") : NULL;
    Py_XDECREF(functools);
    if (!partial) { Py_DECREF(guards); Py_DECREF(code); rp_fo_stop(); return -1; }
    PyTuple_SET_ITEM(guards, 6, partial);
    PyObject *ast_module = PyImport_ImportModule("_ast");
    PyObject *ast_type = ast_module ? PyObject_GetAttrString(ast_module, "AST") : NULL;
    Py_XDECREF(ast_module);
    if (!ast_type) { Py_DECREF(guards); Py_DECREF(code); rp_fo_stop(); return -1; }
    PyTuple_SET_ITEM(guards, 7, ast_type);
    rp_thin_frame_guards = guards;
    rp_thin_template = rp_code_origin_attach(code, 1); Py_DECREF(code);
    if (!rp_thin_template) { Py_CLEAR(rp_thin_frame_guards); rp_fo_stop(); return -1; }
    rp_thin_invoke = invoke; rp_thin_release = release; rp_thin_enabled = 1;
    return 0;
}
/* make 成功失败都消费 token；三个失败注入覆盖 P/closure/F-owner 组装阶段。 */
static PyObject *rp_thin_make_code(PyObject *globals, intptr_t token, int failure, PyObject *code) {
    if (rp_thin_program_terminal) {
        rp_thin_release(token);
        PyErr_SetString(PyExc_RuntimeError, "程序终态禁止创建薄函数"); return NULL;
    }
    if (!rp_thin_enabled || !PyDict_CheckExact(globals)) {
        rp_thin_release(token); PyErr_SetString(PyExc_TypeError, "薄函数需要已启用 profile 和精确 globals dict"); return NULL;
    }
    RpThinLink *link = calloc(1, sizeof(RpThinLink));
    if (!link) { rp_thin_release(token); return PyErr_NoMemory(); }
    link->token = token;
    RpFunctionOwner *owner = rp_fo_new(link, rp_thin_detach, rp_thin_dispose);
    if (!owner) { free(link); rp_thin_release(token); return NULL; }
    owner->frame_guards = Py_NewRef(rp_thin_frame_guards);
    owner->expected_code = Py_NewRef(code);
    link->owner = owner; link->next = rp_thin_links; rp_thin_links = link; rp_thin_live++;
    PyObject *p = PyCFunction_New(&rp_thin_dispatch_def, (PyObject *)owner);
    PyObject *cell = NULL, *closure = NULL, *f = NULL;
    if (!p) goto done;
    if (failure == 1) goto injected;
    cell = PyCell_New(p);
    closure = cell ? PyTuple_Pack(1, cell) : NULL;
    if (!closure) goto done;
    if (failure == 2) goto injected;
    f = PyFunction_New(code, globals);
    if (!f) goto done;
    PyObject_GC_UnTrack(f);
    if (PyFunction_SetClosure(f, closure) < 0) goto unpublished;
    rp_fo_bind(owner, f);
    if (failure == 3) {
        PyErr_SetString(PyExc_RuntimeError, "注入创建失败"); goto unpublished;
    }
    PyObject_GC_Track(f);
    goto done;
unpublished:
    /* 先断新环；stock func_dealloc 内部无条件 UnTrack，恢复后才可 DECREF。 */
    rp_fo_unbind(owner);
    PyObject_GC_Track(f); Py_CLEAR(f);
    goto done;
injected:
    PyErr_SetString(PyExc_RuntimeError, "注入创建失败");
done:
    Py_XDECREF(closure); Py_XDECREF(cell); Py_XDECREF(p); Py_DECREF(owner);
    return f;
}
/* 仅已持 owned F 的内部创建者在发布失败时使用；断环后 finally 归还 F。 */
API void rp_thin_abort(PyObject *f) {
    RpThinLink *link = rp_thin_find(f);
    if (link) rp_fo_unbind(link->owner);
}
/* 旧测试seam保持原模板来源；正式CIL定义使用下面的冷来源定制。 */
API PyObject *rp_thin_make(PyObject *globals, intptr_t token, int failure) {
    return rp_thin_make_code(globals, token, failure, rp_thin_template);
}
API PyObject *rp_thin_make_source(PyObject *globals, intptr_t token, int failure,
    PyObject *filename, int64_t line, PyObject *name) {
    if (!rp_thin_enabled || rp_thin_program_terminal) {
        rp_thin_release(token); PyErr_SetString(PyExc_RuntimeError, "薄函数profile不可创建新定义"); return NULL;
    }
    PyObject *method = PyObject_GetAttrString(rp_thin_template, "replace");
    PyObject *args = PyTuple_New(0), *kwargs = PyDict_New(), *first = PyLong_FromLongLong(line), *code = NULL;
    if (method && args && kwargs && first
        && PyDict_SetItemString(kwargs, "co_filename", filename) == 0
        && PyDict_SetItemString(kwargs, "co_firstlineno", first) == 0
        && PyDict_SetItemString(kwargs, "co_name", name) == 0
#if PY_VERSION_HEX >= 0x030b0000
        && PyDict_SetItemString(kwargs, "co_qualname", name) == 0
#endif
    ) code = PyObject_Call(method, args, kwargs);
    Py_XDECREF(first); Py_XDECREF(kwargs); Py_XDECREF(args); Py_XDECREF(method);
    if (!code) { rp_thin_release(token); return NULL; }
    PyObject *tagged = rp_code_origin_attach(code, 1); Py_DECREF(code); code = tagged;
    if (!code) { rp_thin_release(token); return NULL; }
    PyObject *function = rp_thin_make_code(globals, token, failure, code);
    Py_DECREF(code); return function;
}
API intptr_t rp_thin_token(PyObject *f) {
    RpThinLink *link = rp_thin_find(f);
    return link ? link->token : 0;
}
/* 当前owned F的GC owner保活规范身份；提交终态后也不使用已释放CLR包装或悬空别名。 */
API int rp_thin_frame_builtin(PyObject *f, PyObject *value) {
    if (rp_thin_validate(f) < 0) return -2;
    RpThinLink *link = rp_thin_find(f);
    if (!link || !link->owner->frame_guards) return -1;
    for (int i = 0; i < 6; i++) if (PyTuple_GET_ITEM(link->owner->frame_guards, i) == value) return i;
    return -1;
}
/* 只核查可供eval/exec/compile接收的公开对象族；不读取buffer、不求值用户源码。 */
API int rp_frame_source(PyObject *value) {
    return PyUnicode_Check(value) || PyCode_Check(value) || PyObject_CheckBuffer(value);
}
API PyObject *rp_thin_global(PyObject *f, const char *name) {
    if (rp_thin_validate(f) < 0) return NULL;
    PyObject *key = PyUnicode_FromString(name);
    if (!key) return NULL;
    PyObject *value = PyDict_GetItemWithError(PyFunction_GetGlobals(f), key);
    if (value) { Py_INCREF(value); Py_DECREF(key); return value; }
    if (PyErr_Occurred()) { Py_DECREF(key); return NULL; }
    /* LOAD_GLOBAL 的第二层是定义时捕获的 F.__builtins__，并非当前 D 的同名键。
     * 属性返回 owned；映射查找/用户 __getitem__ 的结果也在进入 CLR 前变为 owned。 */
    PyObject *builtins = PyObject_GetAttrString(f, "__builtins__");
    if (!builtins) { Py_DECREF(key); return NULL; }
    if (PyDict_CheckExact(builtins)) {
        value = PyDict_GetItemWithError(builtins, key);
        Py_XINCREF(value);
    } else {
        value = PyObject_GetItem(builtins, key);
        if (!value && PyErr_ExceptionMatches(PyExc_KeyError)) PyErr_Clear();
    }
    Py_DECREF(builtins);
    if (!value && !PyErr_Occurred()) {
        /* 与stock LOAD_GLOBAL一样保存原查询名；只创建本次缺失异常，映射原异常原样传播。
         * 全部使用公开API；任一分配/属性失败保留该失败，SetRaisedException消费owned异常。 */
        PyObject *message = PyUnicode_FromFormat("name '%U' is not defined", key);
        PyObject *error = message ? PyObject_CallFunctionObjArgs(PyExc_NameError, message, NULL) : NULL;
        Py_XDECREF(message);
        if (error) {
            if (PyObject_SetAttrString(error, "name", key) < 0) Py_DECREF(error);
            else PyErr_SetRaisedException(error);
        }
    }
    Py_DECREF(key);
    return value;
}
API void rp_thin_stats(long long *live, long long *destroyed, long long *calls) {
    *live = rp_thin_live; *destroyed = rp_thin_destroyed; *calls = rp_thin_calls;
}
API int rp_thin_clear(void) {
    if (rp_thin_live) { PyErr_SetString(PyExc_RuntimeError, "薄函数仍有 native owner，禁止关闭"); return -1; }
    Py_CLEAR(rp_thin_template); Py_CLEAR(rp_thin_frame_guards); rp_fo_stop(); rp_thin_enabled = 0;
    return 0;
}

/* 专用程序终态：check无副作用，允许零实例；prepare只移交shim自己的两份owned。
 * 现有F/owner保活比较别名；没有实例时直接清空。禁止新make，finalize后只撤销借用。 */
API int rp_thin_program_check(void) {
    if (!rp_thin_enabled || rp_thin_program_terminal) {
        PyErr_SetString(PyExc_RuntimeError, "stock程序缺少能力或已经提交终态"); return -1;
    }
    return 0;
}
API int rp_thin_program_prepare(void) {
    rp_thin_program_terminal = 1;
    Py_CLEAR(rp_thin_frame_guards);
    if (rp_thin_live) { Py_DECREF(rp_thin_template); Py_DECREF(rp_fo_type); }
    else { Py_CLEAR(rp_thin_template); Py_CLEAR(rp_fo_type); }
    return 0;
}
static void rp_thin_program_closed(void) {
    if (rp_thin_program_terminal) { rp_thin_template = NULL; rp_fo_type = NULL; }
}

#include "stock_frame_call.h"
