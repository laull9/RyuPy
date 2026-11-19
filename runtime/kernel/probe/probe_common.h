/* RyuPy 严格零字节码内核 C 探针公共辅助头。
 *
 * 为 stdio_codecs_probe.c 与 stdio_edge_probe.c 提供统一断言宏、最小内核初始化包装、
 * 未修改官方 CPython 3.12.13 对照模式宏（RYUPY_STOCK_CPYTHON）及管道重定向辅助，
 * 确保关键行为同时在严格零字节码内核与同次官方 CPython 3.12.13 上执行一致断言。
 */
#ifndef RYUPY_KERNEL_PROBE_COMMON_H
#define RYUPY_KERNEL_PROBE_COMMON_H

#define PY_SSIZE_T_CLEAN
#include <Python.h>

#ifndef RYUPY_STOCK_CPYTHON
#include <ryupy_kernel.h>
#endif

#include <errno.h>
#include <fcntl.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/wait.h>
#include <unistd.h>

/* 断言诊断使用独立宿主 fd，标准流重定向后的失败也必须回到测试驱动。 */
static FILE *probe_diagnostics;
static inline void probe_capture_diagnostics(void)
{
    probe_diagnostics = fdopen(dup(STDERR_FILENO), "w");
    if (probe_diagnostics != NULL) setvbuf(probe_diagnostics, NULL, _IONBF, 0);
}

#define PROBE_CHECK(cond, msg)                                                 \
    do {                                                                       \
        if (!(cond)) {                                                         \
            fprintf(probe_diagnostics ? probe_diagnostics : stderr, "FAIL [%s:%d]: %s\n", __FILE__, __LINE__, (msg));  \
            if (Py_IsInitialized() && PyErr_Occurred()) {                      \
                PyErr_Print();                                                 \
            }                                                                  \
            return 1;                                                          \
        }                                                                      \
    } while (0)

/* 在严格内核模式下复位三阶段计数并开启 strict_reject=1；stock 对照模式下为空操作。 */
static inline int
probe_begin_strict_kernel(void)
{
#ifndef RYUPY_STOCK_CPYTHON
    rp_kernel_reset_eval_stats();
    if (rp_kernel_set_phase(RYUPY_KERNEL_PHASE_STARTUP) != 0) {
        fprintf(stderr, "FAIL: rp_kernel_set_phase(STARTUP) failed\n");
        return -1;
    }
    rp_kernel_set_strict_reject(1);
    if (rp_kernel_get_strict_reject() != 1) {
        fprintf(stderr, "FAIL: rp_kernel_get_strict_reject() != 1\n");
        return -1;
    }
#endif
    return 0;
}

static inline int
probe_enter_runtime(void)
{
#ifndef RYUPY_STOCK_CPYTHON
    if (rp_kernel_set_phase(RYUPY_KERNEL_PHASE_RUNTIME) != 0) {
        fprintf(stderr, "FAIL: rp_kernel_set_phase(RUNTIME) failed\n");
        return -1;
    }
#endif
    return 0;
}

static inline int
probe_enter_shutdown(void)
{
#ifndef RYUPY_STOCK_CPYTHON
    if (rp_kernel_set_phase(RYUPY_KERNEL_PHASE_SHUTDOWN) != 0) {
        fprintf(stderr, "FAIL: rp_kernel_set_phase(SHUTDOWN) failed\n");
        return -1;
    }
#endif
    return 0;
}

/* 断言启动/运行/关闭三阶段求值器入口、执行与拒绝计数全部严格为 0。 */
static inline int
probe_assert_zero_bytecode(const char *tag)
{
#ifndef RYUPY_STOCK_CPYTHON
    for (int p = 0; p < 3; p++) {
        int64_t ent = rp_kernel_eval_entry_count(p);
        int64_t exe = rp_kernel_eval_executed_count(p);
        int64_t rej = rp_kernel_eval_rejected_count(p);
        if (ent != 0 || exe != 0 || rej != 0) {
            fprintf(stderr,
                    "FAIL [%s]: non-zero eval stats at phase %d: "
                    "entries=%lld executed=%lld rejected=%lld\n",
                    tag, p, (long long)ent, (long long)exe, (long long)rej);
            return -1;
        }
    }
#else
    (void)tag;
#endif
    return 0;
}

/* 以给定隔离/编码参数初始化内核或对照 CPython；stock 模式下允许 _install_importlib=1。 */
static inline int
probe_init_python(const char *home, int utf8_mode, const char *stdio_enc,
                  const char *stdio_err, const char *fs_enc, const char *fs_err,
                  int dev_mode, int buffered_stdio, PyStatus *out_status)
{
    PyPreConfig pre;
    PyPreConfig_InitIsolatedConfig(&pre);
    pre.utf8_mode = utf8_mode;
    pre.dev_mode = dev_mode;
    PyStatus st = Py_PreInitialize(&pre);
    if (PyStatus_Exception(st)) {
        if (out_status) {
            *out_status = st;
        }
        return -1;
    }

    PyConfig cfg;
    PyConfig_InitIsolatedConfig(&cfg);
    cfg.site_import = 0;
    cfg.write_bytecode = 0;
    cfg.parse_argv = 0;
    cfg.install_signal_handlers = 0;
    cfg.dev_mode = dev_mode;
    cfg.buffered_stdio = buffered_stdio;
#ifndef RYUPY_STOCK_CPYTHON
    cfg.module_search_paths_set = 1;
    cfg._install_importlib = 0;
#else
    cfg._install_importlib = 1;
#endif

    st = PyConfig_SetBytesString(&cfg, &cfg.home, home);
    if (PyStatus_Exception(st)) {
        goto fail;
    }
    if (stdio_enc != NULL) {
        st = PyConfig_SetBytesString(&cfg, &cfg.stdio_encoding, stdio_enc);
        if (PyStatus_Exception(st)) {
            goto fail;
        }
    }
    if (stdio_err != NULL) {
        st = PyConfig_SetBytesString(&cfg, &cfg.stdio_errors, stdio_err);
        if (PyStatus_Exception(st)) {
            goto fail;
        }
    }
    if (fs_enc != NULL) {
        st = PyConfig_SetBytesString(&cfg, &cfg.filesystem_encoding, fs_enc);
        if (PyStatus_Exception(st)) {
            goto fail;
        }
    }
    if (fs_err != NULL) {
        st = PyConfig_SetBytesString(&cfg, &cfg.filesystem_errors, fs_err);
        if (PyStatus_Exception(st)) {
            goto fail;
        }
    }

    st = Py_InitializeFromConfig(&cfg);
fail:
    PyConfig_Clear(&cfg);
    if (out_status) {
        *out_status = st;
    }
    return PyStatus_Exception(st) ? -1 : 0;
}

static inline int
probe_read_nonblock(int fd, char *buf, size_t cap, ssize_t *out_len)
{
    int flags = fcntl(fd, F_GETFL);
    if (flags < 0) {
        return -1;
    }
    if (fcntl(fd, F_SETFL, flags | O_NONBLOCK) < 0) {
        return -1;
    }
    ssize_t n = read(fd, buf, cap);
    int saved_errno = errno;
    (void)fcntl(fd, F_SETFL, flags);
    if (n < 0) {
        if (saved_errno == EAGAIN || saved_errno == EWOULDBLOCK) {
            *out_len = 0;
            return 0;
        }
        return -1;
    }
    *out_len = n;
    return 0;
}

static inline int
probe_fd_is_open(int fd)
{
    return fcntl(fd, F_GETFD) >= 0;
}

#endif /* RYUPY_KERNEL_PROBE_COMMON_H */
