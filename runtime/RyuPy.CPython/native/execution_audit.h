/* C0 显式执行审计：在最早具有线程状态的原生 audit 事件装入 PEP 523 求值钩子。
 * 逐解释器覆盖当前与未来线程、Python 调用及原生扩展反向调用；仅复制帧元数据，
 * 不保留 Python 引用，不调用 Python helper。首次安装之前的启动区间仍有盲区，
 * 因而该首切始终禁止报告完整生命周期覆盖，也禁止据零计数晋升严格产品。
 * 私有帧布局只开放给固定 3.12.13 release/GIL 头文件；其他宿主开启时明确拒绝。 */
#if PY_MAJOR_VERSION == 3 && PY_MINOR_VERSION == 12 && PY_MICRO_VERSION == 13
#if defined(__has_include)
#if __has_include(<internal/pycore_frame.h>)
#define RP_EXECUTION_AUDIT_SUPPORTED 1
#include <internal/pycore_frame.h>
#endif
#endif
#endif
#ifndef RP_EXECUTION_AUDIT_SUPPORTED
#define RP_EXECUTION_AUDIT_SUPPORTED 0
#endif

typedef struct {
    int phase, origin;
    char *file, *function;
    int64_t calls;
} rp_audit_row;
static rp_audit_row audit_rows[4096];
static int audit_row_count, audit_enabled, audit_installed, audit_phase;
static int audit_dropped, audit_replaced, audit_finalized;
static int64_t audit_counts[3];

#if RP_EXECUTION_AUDIT_SUPPORTED
static _PyFrameEvalFunction audit_previous;
static PyInterpreterState *audit_interpreter;

/* 持 GIL 且只读真实 code 对象；转换失败时恢复原 raised exception，保护业务错误通道。 */
static void audit_record(PyCodeObject *code) {
    PyObject *saved = PyErr_GetRaisedException();
    const char *file = PyUnicode_AsUTF8(code->co_filename);
    const char *function = file ? PyUnicode_AsUTF8(code->co_name) : NULL;
    int origin = rp_code_origin_kind((PyObject *)code);
    audit_counts[audit_phase]++;
    if (!file || !function) { audit_dropped++; goto done; }
    for (int i = 0; i < audit_row_count; i++) {
        rp_audit_row *row = &audit_rows[i];
        if (row->phase == audit_phase && row->origin == origin && !strcmp(row->file, file) && !strcmp(row->function, function)) {
            row->calls++; goto done;
        }
    }
    if (audit_row_count == 4096) { audit_dropped++; goto done; }
    size_t file_size = strlen(file) + 1, function_size = strlen(function) + 1;
    char *file_copy = (char *)malloc(file_size), *function_copy = (char *)malloc(function_size);
    if (!file_copy || !function_copy) {
        free(file_copy); free(function_copy); audit_dropped++; goto done;
    }
    memcpy(file_copy, file, file_size); memcpy(function_copy, function, function_size);
    rp_audit_row *row = &audit_rows[audit_row_count++];
    row->phase = audit_phase; row->origin = origin; row->file = file_copy; row->function = function_copy; row->calls = 1;
done:
    PyErr_Clear(); PyErr_SetRaisedException(saved);
}

/* 非默认 eval hook 令 CPython 的普通 Python 调用也经过此入口；恢复生成器同样计次。 */
static PyObject *audit_eval(PyThreadState *thread, struct _PyInterpreterFrame *frame, int throwing) {
    audit_record(frame->f_code);
    return audit_previous(thread, frame, throwing);
}

static int audit_event(const char *event, PyObject *args, void *data) {
    (void)event; (void)args; (void)data;
    if (!audit_enabled) return 0;
    PyThreadState *thread = _PyThreadState_UncheckedGet();
    if (!thread) return 0;
    PyInterpreterState *interpreter = PyThreadState_GetInterpreter(thread);
    if (!audit_installed) {
        audit_interpreter = interpreter;
        audit_previous = _PyInterpreterState_GetEvalFrameFunc(interpreter);
        _PyInterpreterState_SetEvalFrameFunc(interpreter, audit_eval);
        audit_installed = 1;
    } else if (interpreter != audit_interpreter || _PyInterpreterState_GetEvalFrameFunc(interpreter) != audit_eval) {
        /* 其他解释器或 hook 替换会使观测缺口可见；不抢占第三方求值器。 */
        audit_replaced++;
    }
    return 0;
}
#endif

/* 开启必须早于 rp_start；失败保留固定诊断，不尝试把其他 CPython 布局当成 3.12。 */
API int rp_execution_audit_start(void) {
#if RP_EXECUTION_AUDIT_SUPPORTED
    if (Py_IsInitialized() || audit_enabled) {
        strcpy(init_error, "execution audit must be enabled once before initialization"); return -1;
    }
    audit_enabled = 1;
    if (PySys_AddAuditHook(audit_event, NULL) < 0) {
        strcpy(init_error, "failed to install native execution audit hook"); return -1;
    }
    return 0;
#else
    strcpy(init_error, "execution audit requires exact CPython 3.12.13 release/GIL shim with private frame headers"); return -1;
#endif
}

/* 阶段切换在拥有 GIL 时执行；关闭后只能读取原生快照，绝不重入 Python。 */
API void rp_execution_audit_phase(int phase) {
    if (!audit_enabled || phase < 0 || phase > 2) return;
#if RP_EXECUTION_AUDIT_SUPPORTED
    if (audit_installed && _PyInterpreterState_GetEvalFrameFunc(audit_interpreter) != audit_eval) audit_replaced++;
#endif
    audit_phase = phase;
}
API int64_t rp_execution_audit_count(int phase) {
    return phase >= 0 && phase < 3 ? audit_counts[phase] : 0;
}
API int rp_execution_audit_status(int field) {
    switch (field) {
        case 0: return audit_installed;
        case 1: return audit_finalized;
        case 2: return audit_dropped;
        case 3: return audit_replaced;
        default: return 0;
    }
}
/* 返回的 UTF-8 文本由 shim 持有到进程退出；不带 Python 引用，不允许调用方释放。 */
API int rp_execution_audit_row(int index, int *phase, const char **file, const char **function, int64_t *calls) {
    if (index < 0 || index >= audit_row_count) return 0;
    rp_audit_row *row = &audit_rows[index];
    *phase = row->phase; *file = row->file; *function = row->function; *calls = row->calls;
    return 1;
}

/* 来源只按原生注册code身份读取；0普通Python，1静态薄体胶水，2原生调用现场胶水。 */
API int rp_execution_audit_row_origin(int index) { return index >= 0 && index < audit_row_count ? audit_rows[index].origin : 0; }
