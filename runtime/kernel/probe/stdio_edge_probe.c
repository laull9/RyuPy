/* RyuPy 严格零字节码内核手写 C 探针（closefd=False、坏 fd、非 UTF-8 与错误初始化边界）。
 *
 * 验证目标（同时支持与未修改官方 CPython 3.12.13 编译比对）：
 * 1. stdio-closefd：
 *    - 显式关闭 sys.stdin / sys.stdout / sys.stderr 后，Python 流对象 closed=True 且后续
 *      read/write/fileno 报 ValueError，但底层宿主 OS fd (0/1/2) 保持打开且可直接 C write；
 *      全部关闭后调用 Py_FinalizeEx() 仍返回 0。
 *    - buffered_stdio=0 时 sys.stdout.buffer 为 _io.FileIO、write_through=True，写入即刻落盘。
 * 2. stdio-bad-fd：
 *    - 在子进程中分别关闭 fd 0、fd 1、fd 2 后初始化，对应 sys.stdin/stdout/stderr 及其双下划线
 *      镜像均为 None；坏 stdout 时 print() 静默返回 None；input() 显式报 RuntimeError；
 *      Py_FinalizeEx() 返回 0，且严格内核三阶段计数均为 0。
 * 3. stdio-bad-config：
 *    - 在子进程中验证非 UTF-8 配置拒绝（latin-1 / ascii / isolated utf8_mode=0，严格内核边界）、
 *      未知编码、dev_mode=1 下未知 stdio_errors、stdin 为目录等错误初始化；
 *      release 下未知 filesystem_errors 在真实编码错误发生时才拒绝，与当次 CPython 一致；
 *      同时验证 release 模式下未知 stdio_errors 延迟到编码代理字符时抛 LookupError。
 */
#include "probe_common.h"

typedef int (*child_fn_t)(const char *home);

static int
run_in_child(const char *home, const char *label, child_fn_t fn)
{
    fflush(NULL);
    pid_t pid = fork();
    if (pid < 0) {
        fprintf(stderr, "FAIL: fork(%s) failed: %s\n", label, strerror(errno));
        return -1;
    }
    if (pid == 0) {
        int rc = fn(home);
        _exit(rc == 0 ? 0 : 1);
    }
    int status = 0;
    if (waitpid(pid, &status, 0) < 0) {
        fprintf(stderr, "FAIL: waitpid(%s) failed\n", label);
        return -1;
    }
    if (!WIFEXITED(status) || WEXITSTATUS(status) != 0) {
        fprintf(stderr, "FAIL: child [%s] exited abnormally (status=%d)\n", label, status);
        return -1;
    }
    return 0;
}

static int
child_unbuffered_stdio(const char *home)
{
    int p[2];
    PROBE_CHECK(pipe(p) == 0 && dup2(p[1], 1) >= 0, "pipe");
    close(p[1]);
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st2;
    PROBE_CHECK(probe_init_python(home, 1, "utf-8", "surrogateescape",
                                  "utf-8", "surrogateescape", 0, 0, &st2) == 0,
                "init unbuffered");
    PROBE_CHECK(probe_enter_runtime() == 0, "runtime");

    PyObject *sm = PyImport_ImportModule("sys");
    PyObject *im = PyImport_ImportModule("_io");
    PyObject *so = PyObject_GetAttrString(sm, "stdout");
    PyObject *wt = PyObject_GetAttrString(so, "write_through");
    PyObject *lb = PyObject_GetAttrString(so, "line_buffering");
    PyObject *bf = PyObject_GetAttrString(so, "buffer");
    PyObject *ft = PyObject_GetAttrString(im, "FileIO");
    int ok = (wt == Py_True && lb == Py_False && PyObject_TypeCheck(bf, (PyTypeObject *)ft));
    Py_DECREF(ft);
    Py_DECREF(bf);
    Py_DECREF(lb);
    Py_DECREF(wt);
    PROBE_CHECK(ok, "buffered_stdio=0 must use FileIO + write_through=True");

    PyObject *wr = PyObject_CallMethod(so, "write", "s", "UNBUF");
    PROBE_CHECK(wr != NULL, "unbuffered write");
    Py_DECREF(wr);
    char ub[16];
    ssize_t un = 0;
    PROBE_CHECK(probe_read_nonblock(p[0], ub, sizeof(ub), &un) == 0 && un == 5 &&
                    memcmp(ub, "UNBUF", 5) == 0,
                "unbuffered write must hit pipe immediately");
    Py_DECREF(so);
    Py_DECREF(im);
    Py_DECREF(sm);
    PROBE_CHECK(probe_enter_shutdown() == 0, "shutdown");
    PROBE_CHECK(Py_FinalizeEx() == 0, "finalize");
    close(p[0]);
    return probe_assert_zero_bytecode("unbuffered-stdio");
}

static int
run_stdio_closefd(const char *home)
{
    int in_pipe[2], out_pipe[2], err_pipe[2];
    PROBE_CHECK(pipe(in_pipe) == 0 && pipe(out_pipe) == 0 && pipe(err_pipe) == 0, "pipe");
    int saved0 = dup(0), saved1 = dup(1), saved2 = dup(2);
    PROBE_CHECK(saved0 >= 0 && saved1 >= 0 && saved2 >= 0, "dup");
    PROBE_CHECK(dup2(in_pipe[0], 0) >= 0 && dup2(out_pipe[1], 1) >= 0 && dup2(err_pipe[1], 2) >= 0,
                "dup2");
    close(in_pipe[0]);
    close(out_pipe[1]);
    close(err_pipe[1]);
    close(in_pipe[1]);

    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin strict");
    PyStatus st;
    PROBE_CHECK(probe_init_python(home, 1, "utf-8", "surrogateescape",
                                  "utf-8", "surrogateescape", 0, 1, &st) == 0,
                "init_python");
    PROBE_CHECK(probe_enter_runtime() == 0, "enter runtime");

    PyObject *sys_mod = PyImport_ImportModule("sys");
    const char *names[3] = {"stdin", "stdout", "stderr"};
    for (int i = 0; i < 3; i++) {
        PyObject *stream = PyObject_GetAttrString(sys_mod, names[i]);
        PROBE_CHECK(stream != NULL, "get stream");
        PyObject *cres = PyObject_CallMethod(stream, "close", NULL);
        PROBE_CHECK(cres == Py_None, "stream.close() failed");
        Py_DECREF(cres);

        PyObject *closed = PyObject_GetAttrString(stream, "closed");
        PROBE_CHECK(closed == Py_True, "stream.closed must be True after close()");
        Py_DECREF(closed);

        PyObject *fno = PyObject_CallMethod(stream, "fileno", NULL);
        PROBE_CHECK(fno == NULL && PyErr_ExceptionMatches(PyExc_ValueError),
                    "closed stream fileno() must raise ValueError");
        PyErr_Clear();

        if (i > 0) {
            PyObject *w = PyObject_CallMethod(stream, "write", "s", "after-close");
            PROBE_CHECK(w == NULL && PyErr_ExceptionMatches(PyExc_ValueError),
                        "closed stream write() must raise ValueError");
            PyErr_Clear();
        }
        else {
            PyObject *r = PyObject_CallMethod(stream, "read", "i", 1);
            PROBE_CHECK(r == NULL && PyErr_ExceptionMatches(PyExc_ValueError),
                        "closed stdin read() must raise ValueError");
            PyErr_Clear();
        }
        Py_DECREF(stream);
    }
    Py_DECREF(sys_mod);

    /* closefd=False 关键契约：宿主 OS fd 0/1/2 依然打开且可写入 */
    PROBE_CHECK(probe_fd_is_open(0) && probe_fd_is_open(1) && probe_fd_is_open(2),
                "host fds must remain open after Python close()");
    PROBE_CHECK(write(1, "HOST1", 5) == 5 && write(2, "HOST2", 5) == 5,
                "OS write to fd 1/2 after Python stream close failed");

    char buf[32];
    ssize_t n = 0;
    PROBE_CHECK(probe_read_nonblock(out_pipe[0], buf, sizeof(buf), &n) == 0 &&
                    n == 5 && memcmp(buf, "HOST1", 5) == 0,
                "pipe 1 host write mismatch");
    PROBE_CHECK(probe_read_nonblock(err_pipe[0], buf, sizeof(buf), &n) == 0 &&
                    n == 5 && memcmp(buf, "HOST2", 5) == 0,
                "pipe 2 host write mismatch");

    PROBE_CHECK(probe_enter_shutdown() == 0, "enter shutdown");
    PROBE_CHECK(Py_FinalizeEx() == 0, "Py_FinalizeEx after closed streams failed");
    PROBE_CHECK(probe_assert_zero_bytecode("stdio-closefd") == 0, "zero bytecode");

    dup2(saved0, 0);
    dup2(saved1, 1);
    dup2(saved2, 2);
    close(saved0);
    close(saved1);
    close(saved2);
    close(out_pipe[0]);
    close(err_pipe[0]);

    /* 补充验证 buffered_stdio=0 子进程：stdout.buffer 为 FileIO 且 write_through=True */
    PROBE_CHECK(run_in_child(home, "unbuffered-stdio", child_unbuffered_stdio) == 0,
                "unbuffered-stdio");
    printf("stdio-closefd PASS\n");
    return 0;
}

static int
child_bad_fd_0(const char *home)
{
    close(0);
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    PROBE_CHECK(probe_init_python(home, 1, "utf-8", "surrogateescape",
                                  "utf-8", "surrogateescape", 0, 1, &st) == 0,
                "init with closed fd 0 failed");
    PROBE_CHECK(probe_enter_runtime() == 0, "runtime");

    PyObject *sm = PyImport_ImportModule("sys");
    PyObject *bm = PyImport_ImportModule("builtins");
    PyObject *s_in = PyObject_GetAttrString(sm, "stdin");
    PyObject *d_in = PyObject_GetAttrString(sm, "__stdin__");
    PROBE_CHECK(s_in == Py_None && d_in == Py_None, "bad fd 0 must yield sys.stdin is None");
    Py_DECREF(s_in);
    Py_DECREF(d_in);

    PyObject *inp = PyObject_CallMethod(bm, "input", NULL);
    PROBE_CHECK(inp == NULL && PyErr_ExceptionMatches(PyExc_RuntimeError),
                "input with sys.stdin=None must raise RuntimeError");
    PyErr_Clear();

    Py_DECREF(bm);
    Py_DECREF(sm);
    PROBE_CHECK(probe_enter_shutdown() == 0, "shutdown");
    PROBE_CHECK(Py_FinalizeEx() == 0, "finalize");
    return probe_assert_zero_bytecode("bad-fd-0");
}

static int
child_bad_fd_1(const char *home)
{
    close(1);
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    PROBE_CHECK(probe_init_python(home, 1, "utf-8", "surrogateescape",
                                  "utf-8", "surrogateescape", 0, 1, &st) == 0,
                "init with closed fd 1 failed");
    PROBE_CHECK(probe_enter_runtime() == 0, "runtime");

    PyObject *sm = PyImport_ImportModule("sys");
    PyObject *bm = PyImport_ImportModule("builtins");
    PyObject *s_out = PyObject_GetAttrString(sm, "stdout");
    PyObject *d_out = PyObject_GetAttrString(sm, "__stdout__");
    PROBE_CHECK(s_out == Py_None && d_out == Py_None, "bad fd 1 must yield sys.stdout is None");
    Py_DECREF(s_out);
    Py_DECREF(d_out);

    /* CPython 语义：sys.stdout 为 None 时 print() 返回 None 而不报错 */
    PyObject *pr = PyObject_CallMethod(bm, "print", "s", "ignored");
    PROBE_CHECK(pr == Py_None, "print when sys.stdout is None must return None");
    Py_DECREF(pr);

    PyObject *inp = PyObject_CallMethod(bm, "input", NULL);
    PROBE_CHECK(inp == NULL && PyErr_ExceptionMatches(PyExc_RuntimeError),
                "input with sys.stdout=None must raise RuntimeError");
    PyErr_Clear();

    Py_DECREF(bm);
    Py_DECREF(sm);
    PROBE_CHECK(probe_enter_shutdown() == 0, "shutdown");
    PROBE_CHECK(Py_FinalizeEx() == 0, "finalize");
    return probe_assert_zero_bytecode("bad-fd-1");
}

static int
child_bad_fd_2(const char *home)
{
    close(2);
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    PROBE_CHECK(probe_init_python(home, 1, "utf-8", "surrogateescape",
                                  "utf-8", "surrogateescape", 0, 1, &st) == 0,
                "init with closed fd 2 failed");
    PROBE_CHECK(probe_enter_runtime() == 0, "runtime");

    PyObject *sm = PyImport_ImportModule("sys");
    PyObject *bm = PyImport_ImportModule("builtins");
    PyObject *s_err = PyObject_GetAttrString(sm, "stderr");
    PyObject *d_err = PyObject_GetAttrString(sm, "__stderr__");
    PROBE_CHECK(s_err == Py_None && d_err == Py_None, "bad fd 2 must yield sys.stderr is None");
    Py_DECREF(s_err);
    Py_DECREF(d_err);

    PyObject *inp = PyObject_CallMethod(bm, "input", NULL);
    PROBE_CHECK(inp == NULL && PyErr_ExceptionMatches(PyExc_RuntimeError),
                "input with sys.stderr=None must raise RuntimeError");
    PyErr_Clear();

    Py_DECREF(bm);
    Py_DECREF(sm);
    PROBE_CHECK(probe_enter_shutdown() == 0, "shutdown");
    PROBE_CHECK(Py_FinalizeEx() == 0, "finalize");
    return probe_assert_zero_bytecode("bad-fd-2");
}

static int
run_stdio_bad_fd(const char *home)
{
    PROBE_CHECK(run_in_child(home, "bad-fd-0", child_bad_fd_0) == 0, "bad-fd-0");
    PROBE_CHECK(run_in_child(home, "bad-fd-1", child_bad_fd_1) == 0, "bad-fd-1");
    PROBE_CHECK(run_in_child(home, "bad-fd-2", child_bad_fd_2) == 0, "bad-fd-2");
    printf("stdio-bad-fd PASS\n");
    return 0;
}

#ifndef RYUPY_STOCK_CPYTHON
static int
child_reject_latin1_stdio(const char *home)
{
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    int rc = probe_init_python(home, 1, "latin-1", "strict", "utf-8", "surrogateescape", 0, 1, &st);
    PROBE_CHECK(rc != 0 && PyStatus_Exception(st), "latin-1 stdio_encoding must fail");
    return probe_assert_zero_bytecode("reject-latin1-stdio");
}

static int
child_reject_ascii_stdio(const char *home)
{
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    int rc = probe_init_python(home, 1, "ascii", "strict", "utf-8", "surrogateescape", 0, 1, &st);
    PROBE_CHECK(rc != 0 && PyStatus_Exception(st), "ascii stdio_encoding must fail");
    return probe_assert_zero_bytecode("reject-ascii-stdio");
}

static int
child_reject_ascii_fs(const char *home)
{
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    int rc = probe_init_python(home, 1, "utf-8", "strict", "ascii", "surrogateescape", 0, 1, &st);
    PROBE_CHECK(rc != 0 && PyStatus_Exception(st), "ascii filesystem_encoding must fail");
    return probe_assert_zero_bytecode("reject-ascii-fs");
}

static int
child_reject_isolated_utf8_off(const char *home)
{
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    int rc = probe_init_python(home, 0, NULL, NULL, NULL, NULL, 0, 1, &st);
    PROBE_CHECK(rc != 0 && PyStatus_Exception(st), "isolated utf8_mode=0 must fail");
    return probe_assert_zero_bytecode("reject-utf8-off");
}
#endif

static int
child_reject_unknown_enc(const char *home)
{
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    int rc = probe_init_python(home, 1, "nonexistent_enc_xyz", "strict", "utf-8", "surrogateescape",
                               0, 1, &st);
    PROBE_CHECK(rc != 0 && PyStatus_Exception(st), "unknown stdio_encoding must fail");
    return probe_assert_zero_bytecode("reject-unknown-enc");
}

static int
child_reject_bad_fs_errors(const char *home)
{
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    int rc = probe_init_python(home, 1, "utf-8", "strict", "utf-8", "bogus_fs_err", 0, 1, &st);
    PROBE_CHECK(rc == 0, "release filesystem error handler validation is deferred");
    PROBE_CHECK(probe_enter_runtime() == 0, "runtime");
    Py_UCS2 lone_surrogate = 0xdc80;
    PyObject *text = PyUnicode_FromKindAndData(PyUnicode_2BYTE_KIND, &lone_surrogate, 1);
    PROBE_CHECK(text != NULL, "surrogate input");
    PyObject *encoded = PyUnicode_EncodeFSDefault(text);
    Py_DECREF(text);
    PROBE_CHECK(encoded == NULL && PyErr_ExceptionMatches(PyExc_LookupError),
                "unknown filesystem handler must fail when handling an encoding error");
    PyErr_Clear();
    PROBE_CHECK(probe_enter_shutdown() == 0 && Py_FinalizeEx() == 0, "finalize");
    return probe_assert_zero_bytecode("deferred-bad-fs-err");
}

static int
child_reject_dev_bad_stdio_errors(const char *home)
{
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    int rc = probe_init_python(home, 1, "utf-8", "bogus_stdio_err", "utf-8", "surrogateescape",
                               1, 1, &st);
    PROBE_CHECK(rc != 0 && PyStatus_Exception(st), "dev_mode=1 unknown stdio_errors must fail");
    return probe_assert_zero_bytecode("reject-dev-bad-stdio-err");
}

static int
child_reject_dir_stdin(const char *home)
{
    int dfd = open("/tmp", O_RDONLY);
    PROBE_CHECK(dfd >= 0 && dup2(dfd, 0) >= 0, "open /tmp as stdin");
    close(dfd);
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    int rc = probe_init_python(home, 1, "utf-8", "surrogateescape", "utf-8", "surrogateescape",
                               0, 1, &st);
    PROBE_CHECK(rc != 0 && PyStatus_Exception(st), "directory stdin must fail");
    return probe_assert_zero_bytecode("reject-dir-stdin");
}

static int
child_release_bogus_stdio_errors_runtime(const char *home)
{
    int p[2];
    PROBE_CHECK(pipe(p) == 0 && dup2(p[1], 1) >= 0, "pipe");
    close(p[1]);
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin");
    PyStatus st;
    PROBE_CHECK(probe_init_python(home, 1, "utf-8", "bogus_stdio_err", "utf-8", "surrogateescape",
                                  0, 1, &st) == 0,
                "release mode bogus stdio_errors init must match CPython");
    PROBE_CHECK(probe_enter_runtime() == 0, "runtime");

    PyObject *sm = PyImport_ImportModule("sys");
    PyObject *so = PyObject_GetAttrString(sm, "stdout");
    PyObject *surr = PyUnicode_FromKindAndData(PyUnicode_2BYTE_KIND, (const Py_UCS2[]){0xd800}, 1);
    PyObject *wr = PyObject_CallMethod(so, "write", "O", surr);
    PROBE_CHECK(wr == NULL && PyErr_ExceptionMatches(PyExc_LookupError),
                "writing surrogate with bogus stdio_errors must raise LookupError");
    PyErr_Clear();
    Py_DECREF(surr);
    Py_DECREF(so);
    Py_DECREF(sm);

    PROBE_CHECK(probe_enter_shutdown() == 0, "shutdown");
    PROBE_CHECK(Py_FinalizeEx() == 0, "finalize");
    close(p[0]);
    return probe_assert_zero_bytecode("release-bogus-stdio-errors");
}

static int
run_stdio_bad_config(const char *home)
{
#ifndef RYUPY_STOCK_CPYTHON
    PROBE_CHECK(run_in_child(home, "reject-latin1-stdio", child_reject_latin1_stdio) == 0, "latin1");
    PROBE_CHECK(run_in_child(home, "reject-ascii-stdio", child_reject_ascii_stdio) == 0, "ascii");
    PROBE_CHECK(run_in_child(home, "reject-ascii-fs", child_reject_ascii_fs) == 0, "ascii-fs");
    PROBE_CHECK(run_in_child(home, "reject-utf8-off", child_reject_isolated_utf8_off) == 0, "utf8-off");
#endif
    PROBE_CHECK(run_in_child(home, "reject-unknown-enc", child_reject_unknown_enc) == 0, "unknown-enc");
    PROBE_CHECK(run_in_child(home, "reject-bad-fs-err", child_reject_bad_fs_errors) == 0, "bad-fs-err");
    PROBE_CHECK(run_in_child(home, "reject-dev-bad-stdio-err", child_reject_dev_bad_stdio_errors) == 0,
                "dev-bad-stdio-err");
    PROBE_CHECK(run_in_child(home, "reject-dir-stdin", child_reject_dir_stdin) == 0, "dir-stdin");
    PROBE_CHECK(run_in_child(home, "release-bogus-stdio-err",
                             child_release_bogus_stdio_errors_runtime) == 0,
                "release-bogus-stdio-err");
    printf("stdio-bad-config PASS\n");
    return 0;
}

int
main(int argc, char **argv)
{
    probe_capture_diagnostics();
    if (argc != 3) {
        fprintf(stderr, "用法: %s <prefix> <stdio-closefd|stdio-bad-fd|stdio-bad-config>\n",
                argc > 0 ? argv[0] : "stdio_edge_probe");
        return 2;
    }
    if (strcmp(argv[2], "stdio-closefd") == 0) {
        return run_stdio_closefd(argv[1]);
    }
    if (strcmp(argv[2], "stdio-bad-fd") == 0) {
        return run_stdio_bad_fd(argv[1]);
    }
    if (strcmp(argv[2], "stdio-bad-config") == 0) {
        return run_stdio_bad_config(argv[1]);
    }
    fprintf(stderr, "未知模式: %s\n", argv[2]);
    return 2;
}
