/* RyuPy C4-1 扩展导入探针公共辅助头（兼顾 RyuPy 严格内核与未修改 CPython 3.12.13 对照）。
 *
 * 提供两项公共能力：
 * 1. `probe_setup_extensions`：在 `RYUPY_STOCK_CPYTHON` 下将扩展目录注入 `sys.path`；
 *    在 RyuPy 严格内核下通过 `rp_kernel_import_register` 显式注册全部 `.so` 条目并冻结，
 *    绝不依赖目录扫描。
 * 2. `ProbeSyncCtx`：基于 `pthread_mutex_t` / `pthread_cond_t` 与 `sys._rp_sync_hook`
 *    PyCapsule 的轻量线程同步上下文，供慢速导入竞争、并行独立导入与跨线程死锁测试使用。
 */
#ifndef RYUPY_PROBE_EXT_COMMON_H
#define RYUPY_PROBE_EXT_COMMON_H

#include "probe_common.h"
#include <pthread.h>
#include <time.h>

typedef struct {
    const char *name;
    const char *rel_so;
    int is_package;
} ProbeExtSpec;

static const ProbeExtSpec PROBE_ALL_EXTS[] = {
    {"ext_sp_basic", "ext_sp_basic.so", 0},
    {"ext_sp_reinit", "ext_sp_reinit.so", 0},
    {"ext_sp_state", "ext_sp_state.so", 0},
    {"ext_mp", "ext_mp.so", 0},
    {"ext_mp_custom", "ext_mp_custom.so", 0},
    {"ext_mp_retry", "ext_mp_retry.so", 0},
    {"ext_atexit", "ext_atexit.so", 0},
    {"cpkg", "cpkg/__init__.so", 1},
    {"cpkg.csub", "cpkg/csub.so", 0},
    {"cpkg.subpkg", "cpkg/subpkg/__init__.so", 1},
    {"cpkg.subpkg.leaf", "cpkg/subpkg/leaf.so", 0},
    {"cpkg.subpkg.sibling", "cpkg/subpkg/sibling.so", 0},
    {"cpkg.broken_dep", "cpkg/broken_dep.so", 0},
    {"mp_cycle_a", "mp_cycle_a.so", 0},
    {"mp_cycle_b", "mp_cycle_b.so", 0},
    {"mp_slow", "mp_slow.so", 0},
    {"mp_coord_a", "mp_coord_a.so", 0},
    {"mp_coord_b", "mp_coord_b.so", 0},
    {"sp_dl_a", "sp_dl_a.so", 0},
    {"sp_dl_b", "sp_dl_b.so", 0},
    {"mp_dl_a", "mp_dl_a.so", 0},
    {"mp_dl_b", "mp_dl_b.so", 0},
    {"bad_nosym", "bad_nosym.so", 0},
    {"bad_null", "bad_null.so", 0},
    {"bad_unreported", "bad_unreported.so", 0},
    {"bad_type", "bad_type.so", 0},
    {"bad_uninit_def", "bad_uninit_def.so", 0},
    {"bad_neg_size", "bad_neg_size.so", 0},
    {"bad_missing", "does_not_exist.so", 0},
};

static inline int
probe_setup_extensions(const char *ext_dir, int freeze_now)
{
#ifdef RYUPY_STOCK_CPYTHON
    (void)freeze_now;
    PyObject *sys_path = PySys_GetObject("path");
    if (sys_path == NULL) {
        return -1;
    }
    PyObject *dir_obj = PyUnicode_DecodeFSDefault(ext_dir);
    if (dir_obj == NULL) {
        return -1;
    }
    int rc = PyList_Insert(sys_path, 0, dir_obj);
    Py_DECREF(dir_obj);
    return rc;
#else
    PyInterpreterState *interp = PyInterpreterState_Get();
    size_t count = sizeof(PROBE_ALL_EXTS) / sizeof(PROBE_ALL_EXTS[0]);
    char path_buf[1024];
    for (size_t i = 0; i < count; i++) {
        snprintf(path_buf, sizeof(path_buf), "%s/%s", ext_dir, PROBE_ALL_EXTS[i].rel_so);
        RpKernelImportEntry entry = {
            .abi_version = RYUPY_IMPORT_ABI_VERSION,
            .struct_size = (uint32_t)sizeof(RpKernelImportEntry),
            .kind = RYUPY_IMPORT_KIND_EXTENSION,
            .is_package = PROBE_ALL_EXTS[i].is_package,
            .name = PROBE_ALL_EXTS[i].name,
            .origin = path_buf,
            .package_dir = NULL,
        };
        if (rp_kernel_import_register(interp, &entry) < 0) {
            return -1;
        }
    }
    if (freeze_now && rp_kernel_import_freeze(interp) < 0) {
        return -1;
    }
    return 0;
#endif
}

typedef struct {
    pthread_mutex_t mu;
    pthread_cond_t cv;
    int slow_started;
    int slow_release;
    int coord_a_waiting;
    int coord_b_done;
    int sp_dl_arrived;
    int mp_dl_arrived;
} ProbeSyncCtx;

static inline void
probe_sync_init(ProbeSyncCtx *ctx)
{
    memset(ctx, 0, sizeof(*ctx));
    pthread_mutex_init(&ctx->mu, NULL);
    pthread_cond_init(&ctx->cv, NULL);
}

static inline void
probe_sync_destroy(ProbeSyncCtx *ctx)
{
    pthread_cond_destroy(&ctx->cv);
    pthread_mutex_destroy(&ctx->mu);
}

static inline int
probe_timed_wait(pthread_cond_t *cv, pthread_mutex_t *mu, int seconds)
{
    struct timespec ts;
    clock_gettime(CLOCK_REALTIME, &ts);
    ts.tv_sec += seconds;
    return pthread_cond_timedwait(cv, mu, &ts);
}

static inline void
probe_sync_callback(int event_id, void *raw_ctx)
{
    ProbeSyncCtx *ctx = (ProbeSyncCtx *)raw_ctx;
    pthread_mutex_lock(&ctx->mu);
    if (event_id == 1) {
        /* mp_slow: 通知主线程/等待线程已进入初始化，等待外部放行后再完成。 */
        ctx->slow_started = 1;
        pthread_cond_broadcast(&ctx->cv);
        while (!ctx->slow_release) {
            if (probe_timed_wait(&ctx->cv, &ctx->mu, 5) != 0) {
                break;
            }
        }
    }
    else if (event_id == 2) {
        /* mp_coord_a: 唤醒线程 2 去导入 mp_coord_b，并等待它完成。 */
        ctx->coord_a_waiting = 1;
        pthread_cond_broadcast(&ctx->cv);
        while (!ctx->coord_b_done) {
            if (probe_timed_wait(&ctx->cv, &ctx->mu, 5) != 0) {
                break;
            }
        }
    }
    else if (event_id == 3) {
        /* mp_coord_b: 通知 mp_coord_a 可以继续完成。 */
        ctx->coord_b_done = 1;
        pthread_cond_broadcast(&ctx->cv);
    }
    else if (event_id == 4) {
        /* sp_dl_a / sp_dl_b 两线程同时进入单阶段 PyInit_ 后再互相导入。 */
        ctx->sp_dl_arrived++;
        pthread_cond_broadcast(&ctx->cv);
        while (ctx->sp_dl_arrived < 2) {
            if (probe_timed_wait(&ctx->cv, &ctx->mu, 5) != 0) {
                break;
            }
        }
    }
    else if (event_id == 5) {
        /* mp_dl_a / mp_dl_b 两线程同时进入多阶段 Py_mod_exec 后再互相导入。 */
        ctx->mp_dl_arrived++;
        pthread_cond_broadcast(&ctx->cv);
        while (ctx->mp_dl_arrived < 2) {
            if (probe_timed_wait(&ctx->cv, &ctx->mu, 5) != 0) {
                break;
            }
        }
    }
    pthread_mutex_unlock(&ctx->mu);
}

static inline int
probe_install_sync_hook(ProbeSyncCtx *ctx)
{
    PyObject *sys_mod = PyImport_ImportModule("sys");
    if (sys_mod == NULL) return -1;
    PyObject *cap = PyCapsule_New((void *)probe_sync_callback, "rp_sync", NULL);
    if (cap == NULL || PyCapsule_SetContext(cap, ctx) < 0) {
        Py_XDECREF(cap);
        Py_DECREF(sys_mod);
        return -1;
    }
    int rc = PyObject_SetAttrString(sys_mod, "_rp_sync_hook", cap);
    Py_DECREF(cap);
    Py_DECREF(sys_mod);
    return rc;
}

static inline PyObject *
probe_import_from(const char *mod_name, const char *item1)
{
    PyObject *fromlist = Py_BuildValue("[s]", item1);
    if (fromlist == NULL) return NULL;
    PyObject *res = PyImport_ImportModuleLevel(mod_name, NULL, NULL, fromlist, 0);
    Py_DECREF(fromlist);
    return res;
}

#endif /* RYUPY_PROBE_EXT_COMMON_H */
