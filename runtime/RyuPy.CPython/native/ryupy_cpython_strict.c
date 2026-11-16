/* RyuPy CPython 3.12.13 严格零字节码内核专属 shim（libryupy_cpython_strict）。
 * 仅编译于包含 ryupy_kernel.h 的严格内核前缀：
 * 1. 初始化前强制置位 strict_reject=1、phase=STARTUP 与计数归零，以 _install_importlib=0 启动；
 * 2. 彻底省去 compat 导入 Python 胶水、import_hooks 与 PEP 523 盲区追踪；
 * 3. 挂载 C4-1 FiniCore cleanup 回调，在 _PyAtExit_Call 与 finalize_modules 之后、解释器销毁前
 *    回调宿主 FiniCore Fence 真实 Py_DECREF 清理并清空原生类型引用。 */
#include "cpython_compat_env.h"
#include <ryupy_kernel.h>

#if PY_VERSION_HEX != 0x030c0df0
#error "RyuPy strict shim requires CPython 3.12.13 strict kernel"
#endif

API const char *rp_strict_build_version(void) { return PY_VERSION; }
API const char *rp_strict_python_version(void) { return Py_GetVersion(); }
API const char *rp_strict_kernel_version(void) { return rp_kernel_version(); }

API int rp_strict_has_import_abi(void) {
    /* 未初始化下调用 is_frozen(NULL) 应返回 -1 且不设置异常，由此验证 C4-1 侧表 ABI 就绪。 */
    if (RYUPY_IMPORT_ABI_VERSION != 1u) return 0;
    (void)rp_kernel_import_is_frozen(NULL);
    return 1;
}

#include "method_descriptor.h"
#include "class_construction.h"
#include "signature_bridge.h"
#include "cpython_bridge_ops.h"

typedef void (*rp_fini_fence_fn)(void);
typedef int (*rp_strict_exec_fn)(PyInterpreterState *interp, PyObject *module, PyObject *spec);
static int strict_fini_core_calls;
static int strict_reject_dropped;
static int strict_audit_finalized;
static int64_t strict_baseline_disable_epoch;
static int64_t strict_baseline_reset_epoch;
/* 单次安装静态 Exec trampoline：Slice 3 Seal 只注册条目并置位 Host 已封口与执行目标，
 * 绝不二次调用 rp_kernel_import_set_cil_callbacks，避免替换槽提前触发 cleanup/FiniCore Fence。 */
static rp_strict_exec_fn strict_cil_exec_trampoline;

static int rp_strict_cil_exec_dispatcher(
    PyInterpreterState *interp,
    PyObject *module,
    PyObject *spec,
    void *user_data) {
    (void)user_data;
    if (strict_cil_exec_trampoline) {
        return strict_cil_exec_trampoline(interp, module, spec);
    }
    PyErr_SetString(PyExc_NotImplementedError, "CIL module execution is not sealed/installed");
    return -1;
}

/* 在 _PyImport_FiniCore（atexit 结束、解释器与 GIL 仍有效）阶段触发托管 Release Fence。 */
static void rp_strict_fini_core_cleanup(void *user_data) {
    strict_fini_core_calls++;
    strict_cil_exec_trampoline = NULL;
    if (rp_kernel_get_strict_reject() == 0) strict_reject_dropped = 1;
    rp_fini_fence_fn fence_cb = (rp_fini_fence_fn)user_data;
    if (fence_cb) fence_cb();
    Py_CLEAR(native_method_type);
    Py_CLEAR(native_static_method_type);
    Py_CLEAR(managed_descriptor_type);
    Py_CLEAR(managed_type);
}

API int rp_strict_start(
    const char *home,
    const char **paths,
    int path_count,
    const char **argv,
    int argc,
    managed_call callback,
    rp_fini_fence_fn fini_fence_cb) {
    init_error[0] = '\0';
    strict_fini_core_calls = 0;
    strict_reject_dropped = 0;
    strict_audit_finalized = 0;
    strict_cil_exec_trampoline = NULL;

    if (rp_kernel_set_phase(0) != 0) {
        strcpy(init_error, "rp_kernel_set_phase(STARTUP) failed");
        return -1;
    }
    rp_kernel_reset_eval_stats();
    rp_kernel_set_strict_reject(1);
    strict_baseline_disable_epoch = rp_kernel_strict_disable_epoch();
    strict_baseline_reset_epoch = rp_kernel_eval_reset_epoch();
    if (rp_kernel_get_strict_reject() == 0) strict_reject_dropped = 1;

    PyPreConfig preconfig; PyPreConfig_InitIsolatedConfig(&preconfig);
    preconfig.utf8_mode = 1;
    if (fail_status(Py_PreInitialize(&preconfig))) return -1;

    PyConfig config; PyConfig_InitIsolatedConfig(&config);
    config.site_import = 0; config.write_bytecode = 0; config.parse_argv = 0;
    config.install_signal_handlers = 0; config.module_search_paths_set = 1;
    config._install_importlib = 0;
    if (fail_status(PyConfig_SetBytesString(&config, &config.home, home))) goto error;
    if (fail_status(PyConfig_SetBytesString(&config, &config.stdio_encoding, "utf-8"))) goto error;
    if (fail_status(PyConfig_SetBytesString(&config, &config.stdio_errors, "surrogateescape"))) goto error;
    for (int list = 0; list < 2; list++) {
        int count = list ? argc : path_count;
        const char **items = list ? argv : paths;
        for (int index = 0; index < count; index++) {
            wchar_t *wide = Py_DecodeLocale(items[index], NULL);
            if (!wide) { strcpy(init_error, "invalid UTF-8 configuration"); goto error; }
            PyStatus status = PyWideStringList_Append(list ? &config.argv : &config.module_search_paths, wide);
            PyMem_RawFree(wide); if (fail_status(status)) goto error;
        }
    }
    if (fail_status(Py_InitializeFromConfig(&config))) goto error;
    PyConfig_Clear(&config);
    if (rp_kernel_get_strict_reject() == 0) strict_reject_dropped = 1;

    dispatch = callback;
    managed_type = (PyTypeObject *)PyType_FromSpec(&managed_spec);
    if (!managed_type) {
        strcpy(init_error, "managed proxy type creation failed");
        PyErr_Clear(); Py_FinalizeEx(); return -1;
    }

    RpKernelCilImportCallbacks callbacks = {0};
    callbacks.abi_version = RYUPY_IMPORT_ABI_VERSION;
    callbacks.struct_size = (uint32_t)sizeof(RpKernelCilImportCallbacks);
    callbacks.create_module = NULL;
    callbacks.exec_module = rp_strict_cil_exec_dispatcher;
    /* cleanup 只属 CIL 槽自身，绝不承载宿主 FiniCore Fence，避免后续合法替换槽提前关闭宿主。 */
    callbacks.cleanup = NULL;
    callbacks.user_data = NULL;
    if (rp_kernel_import_set_cil_callbacks(PyInterpreterState_Get(), &callbacks) < 0) {
        strcpy(init_error, "rp_kernel_import_set_cil_callbacks failed");
        PyErr_Clear();
        Py_CLEAR(managed_type);
        Py_FinalizeEx();
        return -1;
    }

    if (rp_kernel_set_fini_fence(PyInterpreterState_Get(), rp_strict_fini_core_cleanup, (void *)fini_fence_cb) < 0
        || rp_kernel_set_fini_fence(PyInterpreterState_Get(), rp_strict_fini_core_cleanup, (void *)fini_fence_cb) < 0) {
        strcpy(init_error, "rp_kernel_set_fini_fence failed");
        PyErr_Clear();
        Py_CLEAR(managed_type);
        Py_FinalizeEx();
        return -1;
    }

    initial_thread = PyEval_SaveThread();
    return 0;
error:
    PyConfig_Clear(&config);
    if (Py_IsInitialized()) Py_FinalizeEx();
    return -1;
}

API int rp_strict_stop(void) {
    PyEval_RestoreThread(initial_thread); initial_thread = NULL;
    if (rp_kernel_get_strict_reject() == 0) strict_reject_dropped = 1;
    if (rp_kernel_set_phase(2) != 0) strict_reject_dropped = 1;
    int result = Py_FinalizeEx();
    if (rp_kernel_get_strict_reject() == 0) strict_reject_dropped = 1;
    inspect_bridge_closed_aliases();
    strict_audit_finalized = 1;
    managed_type = NULL;
    return result;
}

/* 返回严格启动阶段缓冲区只读借用 UTF-8 指针；由本 shim 静态存储拥有，下次 rp_strict_start 才覆写。 */
API const char *rp_strict_init_error(void) { return init_error; }
API int rp_strict_set_phase(int phase) { return rp_kernel_set_phase(phase); }
API int rp_strict_get_phase(void) { return rp_kernel_get_phase(); }
API int rp_strict_get_strict_reject(void) { return rp_kernel_get_strict_reject(); }
API int rp_strict_reject_never_disabled(void) {
    return strict_reject_dropped == 0
        && rp_kernel_get_strict_reject() != 0
        && rp_kernel_strict_disable_epoch() == strict_baseline_disable_epoch ? 1 : 0;
}
API int rp_strict_stats_never_reset(void) {
    return rp_kernel_eval_reset_epoch() == strict_baseline_reset_epoch ? 1 : 0;
}
API int rp_strict_fini_core_calls(void) { return strict_fini_core_calls; }
API int rp_strict_is_finalized(void) { return strict_audit_finalized; }
API int64_t rp_strict_eval_entry_count(int phase) { return rp_kernel_eval_entry_count(phase); }
API int64_t rp_strict_eval_executed_count(int phase) { return rp_kernel_eval_executed_count(phase); }
API int64_t rp_strict_eval_rejected_count(int phase) { return rp_kernel_eval_rejected_count(phase); }
API int64_t rp_strict_eval_total_entries(void) { return rp_kernel_eval_total_entries(); }
API int64_t rp_strict_eval_total_executed(void) { return rp_kernel_eval_total_executed(); }
API int64_t rp_strict_eval_total_rejected(void) { return rp_kernel_eval_total_rejected(); }
