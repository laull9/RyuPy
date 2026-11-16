/* RyuPy 严格零字节码内核原型手写 C 探针（直接链接源码构建的 libpython3.12）。
 *
 * 验证目标：
 * 1. strict-clean：_install_importlib=0 + strict_reject=1 下启动、导入与运算真实内建 math、
 *    验证默认纯 C 标准流 print 零字节码工作，显式拒绝未支持消费者（非内建模块、冻结模块、
 *    相对导入、reload、删除 sys.stdout 后的 print）并关闭，
 *    全三阶段（startup/runtime/shutdown）默认求值器入口/执行/拒绝计数均为 0。
 * 2. strict-reject：开启 strict_reject=1 后尝试调用 PyEval_EvalCode 必须被硬拒绝并抛出
 *    RuntimeError，对应阶段 entry=1, rejected=1, executed=0。
 * 3. phase-counters：关闭 strict_reject=0 后在 startup/runtime/shutdown 分别求值，验证三阶段
 *    计数器精确归属（1, 2, 1）且零字节码初始化本身计数为 0。
 * 4. reject-importlib：尝试设置 _install_importlib=1 必须明确失败，不可静默执行冻结字节码。
 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>
#include <ryupy_kernel.h>

#include <math.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define CHECK(cond, msg)                                                       \
    do {                                                                       \
        if (!(cond)) {                                                         \
            fprintf(stderr, "FAIL [%s:%d]: %s\n", __FILE__, __LINE__, (msg));  \
            if (Py_IsInitialized() && PyErr_Occurred()) {                      \
                PyErr_Print();                                                 \
            }                                                                  \
            return 1;                                                          \
        }                                                                      \
    } while (0)

static int
init_minimal_kernel(const char *home, int install_importlib, PyStatus *out_status)
{
    PyPreConfig pre;
    PyPreConfig_InitIsolatedConfig(&pre);
    pre.utf8_mode = 1;
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
    cfg.module_search_paths_set = 1;
    cfg._install_importlib = install_importlib;

    st = PyConfig_SetBytesString(&cfg, &cfg.home, home);
    if (PyStatus_Exception(st)) {
        PyConfig_Clear(&cfg);
        if (out_status) {
            *out_status = st;
        }
        return -1;
    }
    st = PyConfig_SetBytesString(&cfg, &cfg.stdio_encoding, "utf-8");
    if (PyStatus_Exception(st)) {
        PyConfig_Clear(&cfg);
        if (out_status) {
            *out_status = st;
        }
        return -1;
    }
    st = PyConfig_SetBytesString(&cfg, &cfg.stdio_errors, "surrogateescape");
    if (PyStatus_Exception(st)) {
        PyConfig_Clear(&cfg);
        if (out_status) {
            *out_status = st;
        }
        return -1;
    }

    st = Py_InitializeFromConfig(&cfg);
    PyConfig_Clear(&cfg);
    if (out_status) {
        *out_status = st;
    }
    return PyStatus_Exception(st) ? -1 : 0;
}

static int
eval_simple_int(const char *expr, long *out_val)
{
    PyObject *code = Py_CompileString(expr, "<kernel_probe>", Py_eval_input);
    if (!code) {
        return -1;
    }
    PyObject *main_mod = PyImport_AddModule("__main__");
    if (!main_mod) {
        Py_DECREF(code);
        return -1;
    }
    PyObject *globals = PyModule_GetDict(main_mod);
    if (!PyDict_GetItemString(globals, "__builtins__")) {
        if (PyDict_SetItemString(globals, "__builtins__", PyEval_GetBuiltins()) < 0) {
            Py_DECREF(code);
            return -1;
        }
    }
    PyObject *res = PyEval_EvalCode(code, globals, globals);
    Py_DECREF(code);
    if (!res) {
        return -1;
    }
    long val = PyLong_AsLong(res);
    Py_DECREF(res);
    if (val == -1 && PyErr_Occurred()) {
        return -1;
    }
    *out_val = val;
    return 0;
}

static int
verify_math_operations(PyObject *math_mod)
{
    PyObject *sqrt_res = PyObject_CallMethod(math_mod, "sqrt", "d", 16.0);
    CHECK(sqrt_res != NULL, "math.sqrt(16.0) failed");
    double sqrt_val = PyFloat_AsDouble(sqrt_res);
    Py_DECREF(sqrt_res);
    CHECK(!PyErr_Occurred() && fabs(sqrt_val - 4.0) < 1e-12, "math.sqrt(16.0) != 4.0");

    PyObject *fact_res = PyObject_CallMethod(math_mod, "factorial", "l", 6L);
    CHECK(fact_res != NULL, "math.factorial(6) failed");
    long fact_val = PyLong_AsLong(fact_res);
    Py_DECREF(fact_res);
    CHECK(!PyErr_Occurred() && fact_val == 720, "math.factorial(6) != 720");

    PyObject *fabs_res = PyObject_CallMethod(math_mod, "fabs", "d", -3.5);
    CHECK(fabs_res != NULL, "math.fabs(-3.5) failed");
    double fabs_val = PyFloat_AsDouble(fabs_res);
    Py_DECREF(fabs_res);
    CHECK(!PyErr_Occurred() && fabs(fabs_val - 3.5) < 1e-12, "math.fabs(-3.5) != 3.5");

    PyObject *pi_obj = PyObject_GetAttrString(math_mod, "pi");
    CHECK(pi_obj != NULL, "math.pi missing");
    double pi_val = PyFloat_AsDouble(pi_obj);
    Py_DECREF(pi_obj);
    PyObject *sin_res = PyObject_CallMethod(math_mod, "sin", "d", pi_val / 2.0);
    CHECK(sin_res != NULL, "math.sin(pi/2) failed");
    double sin_val = PyFloat_AsDouble(sin_res);
    Py_DECREF(sin_res);
    CHECK(!PyErr_Occurred() && fabs(sin_val - 1.0) < 1e-12, "math.sin(pi/2) != 1.0");

    PyObject *gcd_res = PyObject_CallMethod(math_mod, "gcd", "ll", 48L, 18L);
    CHECK(gcd_res != NULL, "math.gcd(48, 18) failed");
    long gcd_val = PyLong_AsLong(gcd_res);
    Py_DECREF(gcd_res);
    CHECK(!PyErr_Occurred() && gcd_val == 6, "math.gcd(48, 18) != 6");

    printf("math_ops sqrt(16)=%.1f factorial(6)=%ld fabs(-3.5)=%.1f sin(pi/2)=%.6f gcd(48,18)=%ld\n",
           sqrt_val, fact_val, fabs_val, sin_val, gcd_val);
    return 0;
}

static int
run_strict_clean(const char *home)
{
    CHECK(strncmp(Py_GetVersion(), "3.12.13", 7) == 0, "Py_GetVersion must be 3.12.13");
    CHECK(strcmp(rp_kernel_version(), "ryupy-cpython-3.12.13-kernel-proto-v1") == 0,
          "unexpected rp_kernel_version");

    rp_kernel_reset_eval_stats();
    CHECK(rp_kernel_set_phase(RYUPY_KERNEL_PHASE_STARTUP) == 0, "set startup phase");
    rp_kernel_set_strict_reject(1);
    CHECK(rp_kernel_get_strict_reject() == 1, "strict reject must be 1");

    PyStatus st;
    CHECK(init_minimal_kernel(home, 0, &st) == 0, "init_minimal_kernel(_install_importlib=0) failed");
    CHECK(rp_kernel_set_phase(RYUPY_KERNEL_PHASE_RUNTIME) == 0, "set runtime phase");

    /* 核对无 frozen importlib / encodings 进入 sys.modules */
    PyObject *modules = PyImport_GetModuleDict();
    CHECK(modules != NULL, "sys.modules missing");
    CHECK(PyDict_GetItemString(modules, "sys") != NULL, "sys missing in sys.modules");
    CHECK(PyDict_GetItemString(modules, "builtins") != NULL, "builtins missing in sys.modules");
    CHECK(PyDict_GetItemString(modules, "_frozen_importlib") == NULL, "_frozen_importlib must not exist");
    CHECK(PyDict_GetItemString(modules, "encodings") == NULL, "encodings must not exist");

    /* 同时覆盖 PyImport_ImportModule 与 PyImport_ImportModuleLevel 两条内建路由入口 */
    PyObject *math_mod = PyImport_ImportModule("math");
    CHECK(math_mod != NULL, "PyImport_ImportModule(\"math\") failed");
    PyObject *math_lvl = PyImport_ImportModuleLevel("math", NULL, NULL, NULL, 0);
    CHECK(math_lvl != NULL, "PyImport_ImportModuleLevel(\"math\") failed");
    CHECK(math_mod == math_lvl, "math module identity mismatch");

    PyObject *spec = PyObject_GetAttrString(math_mod, "__spec__");
    CHECK(spec != NULL && spec != Py_None, "math.__spec__ missing");
    PyObject *origin = PyObject_GetAttrString(spec, "origin");
    CHECK(origin != NULL && PyUnicode_Check(origin), "math.__spec__.origin missing");
    CHECK(strcmp(PyUnicode_AsUTF8(origin), "built-in") == 0, "math.__spec__.origin != built-in");
    Py_DECREF(origin);
    Py_DECREF(spec);

    if (verify_math_operations(math_mod) != 0) {
        Py_DECREF(math_lvl);
        Py_DECREF(math_mod);
        return 1;
    }

    /* 显式拒绝未支持消费者，绝不返回 SystemError 或静默执行字节码 */
    PyObject *bad_json = PyImport_ImportModule("json");
    CHECK(bad_json == NULL && PyErr_ExceptionMatches(PyExc_ModuleNotFoundError),
          "non-builtin module json must fail with ModuleNotFoundError");
    PyErr_Clear();

    PyObject *bad_enc = PyImport_ImportModule("encodings");
    CHECK(bad_enc == NULL && PyErr_ExceptionMatches(PyExc_ModuleNotFoundError),
          "encodings must fail with ModuleNotFoundError");
    PyErr_Clear();

    CHECK(PyImport_ImportFrozenModule("_frozen_importlib") == 0,
          "frozen _frozen_importlib must be absent");

    PyObject *bad_rel = PyImport_ImportModuleLevel("relmod", NULL, NULL, NULL, 1);
    CHECK(bad_rel == NULL && PyErr_Occurred() != NULL, "relative import without package must fail");
    PyErr_Clear();

    PyObject *bad_reload = PyImport_ReloadModule(math_mod);
    CHECK(bad_reload == NULL && PyErr_ExceptionMatches(PyExc_NotImplementedError),
          "reload must fail with NotImplementedError");
    PyErr_Clear();

    /* 新合同：_install_importlib=0 已接入 _io + 纯 C UTF-8 标准流，builtins.print 可零字节码工作；
     * 仅当显式删除 sys.stdout 时才应抛出 RuntimeError("lost sys.stdout")。 */
    PyObject *builtins_mod = PyImport_ImportModule("builtins");
    CHECK(builtins_mod != NULL, "builtins import failed");
    PyObject *ok_print = PyObject_CallMethod(builtins_mod, "print", "s", "ryupy-kernel-stdout-ok");
    CHECK(ok_print == Py_None, "print with C stdio must succeed and return None");
    Py_DECREF(ok_print);

    PyObject *sys_mod = PyImport_ImportModule("sys");
    CHECK(sys_mod != NULL, "sys import failed");
    PyObject *saved_stdout = PyObject_GetAttrString(sys_mod, "stdout");
    CHECK(saved_stdout != NULL && saved_stdout != Py_None, "sys.stdout must be initialized");
    CHECK(PyObject_DelAttrString(sys_mod, "stdout") == 0, "del sys.stdout failed");
    PyObject *bad_print = PyObject_CallMethod(builtins_mod, "print", "s", "should-reject-lost-stdout");
    CHECK(bad_print == NULL && PyErr_ExceptionMatches(PyExc_RuntimeError),
          "print after deleting sys.stdout must fail with RuntimeError");
    PyErr_Clear();
    CHECK(PyObject_SetAttrString(sys_mod, "stdout", saved_stdout) == 0, "restore sys.stdout failed");
    Py_DECREF(saved_stdout);
    Py_DECREF(sys_mod);
    Py_DECREF(builtins_mod);

    Py_DECREF(math_lvl);
    Py_DECREF(math_mod);

    CHECK(rp_kernel_set_phase(RYUPY_KERNEL_PHASE_SHUTDOWN) == 0, "set shutdown phase");
    CHECK(Py_FinalizeEx() == 0, "Py_FinalizeEx failed");

    for (int p = 0; p < 3; p++) {
        CHECK(rp_kernel_eval_entry_count(p) == 0, "strict-clean entry count must be 0");
        CHECK(rp_kernel_eval_executed_count(p) == 0, "strict-clean executed count must be 0");
        CHECK(rp_kernel_eval_rejected_count(p) == 0, "strict-clean rejected count must be 0");
    }
    printf("strict-clean PASS entries=(0,0,0) executed=(0,0,0) rejected=(0,0,0)\n");
    return 0;
}

static int
run_strict_reject(const char *home)
{
    rp_kernel_reset_eval_stats();
    CHECK(rp_kernel_set_phase(RYUPY_KERNEL_PHASE_STARTUP) == 0, "set startup phase");
    rp_kernel_set_strict_reject(1);

    PyStatus st;
    CHECK(init_minimal_kernel(home, 0, &st) == 0, "init_minimal_kernel failed");
    CHECK(rp_kernel_set_phase(RYUPY_KERNEL_PHASE_RUNTIME) == 0, "set runtime phase");

    long val = 0;
    CHECK(eval_simple_int("1 + 2", &val) != 0, "PyEval_EvalCode must fail under strict_reject=1");
    CHECK(PyErr_ExceptionMatches(PyExc_RuntimeError), "strict reject must raise RuntimeError");
    PyErr_Clear();

    CHECK(rp_kernel_set_phase(RYUPY_KERNEL_PHASE_SHUTDOWN) == 0, "set shutdown phase");
    CHECK(Py_FinalizeEx() == 0, "Py_FinalizeEx failed");

    CHECK(rp_kernel_eval_entry_count(0) == 0 && rp_kernel_eval_rejected_count(0) == 0,
          "startup counts must remain 0");
    CHECK(rp_kernel_eval_entry_count(1) == 1 && rp_kernel_eval_rejected_count(1) == 1 &&
              rp_kernel_eval_executed_count(1) == 0,
          "runtime must record 1 entry, 1 rejected, 0 executed");
    CHECK(rp_kernel_eval_entry_count(2) == 0 && rp_kernel_eval_rejected_count(2) == 0,
          "shutdown counts must remain 0");
    printf("strict-reject PASS entries=(0,1,0) executed=(0,0,0) rejected=(0,1,0)\n");
    return 0;
}

static int
run_phase_counters(const char *home)
{
    rp_kernel_reset_eval_stats();
    CHECK(rp_kernel_set_phase(RYUPY_KERNEL_PHASE_STARTUP) == 0, "set startup phase");
    rp_kernel_set_strict_reject(0);
    CHECK(rp_kernel_set_phase(99) == -1, "invalid phase must be rejected");

    PyStatus st;
    CHECK(init_minimal_kernel(home, 0, &st) == 0, "init_minimal_kernel failed");
    CHECK(rp_kernel_eval_entry_count(0) == 0, "startup init itself must enter 0 frames");

    long v = 0;
    CHECK(eval_simple_int("10 + 20", &v) == 0 && v == 30, "startup eval 10+20");

    CHECK(rp_kernel_set_phase(RYUPY_KERNEL_PHASE_RUNTIME) == 0, "set runtime phase");
    CHECK(eval_simple_int("1 + 2", &v) == 0 && v == 3, "runtime eval 1+2");
    CHECK(eval_simple_int("6 * 7", &v) == 0 && v == 42, "runtime eval 6*7");

    CHECK(rp_kernel_set_phase(RYUPY_KERNEL_PHASE_SHUTDOWN) == 0, "set shutdown phase");
    CHECK(eval_simple_int("5 + 6", &v) == 0 && v == 11, "shutdown eval 5+6");
    CHECK(Py_FinalizeEx() == 0, "Py_FinalizeEx failed");

    CHECK(rp_kernel_eval_entry_count(0) == 1 && rp_kernel_eval_executed_count(0) == 1 &&
              rp_kernel_eval_rejected_count(0) == 0,
          "startup counts mismatch");
    CHECK(rp_kernel_eval_entry_count(1) == 2 && rp_kernel_eval_executed_count(1) == 2 &&
              rp_kernel_eval_rejected_count(1) == 0,
          "runtime counts mismatch");
    CHECK(rp_kernel_eval_entry_count(2) == 1 && rp_kernel_eval_executed_count(2) == 1 &&
              rp_kernel_eval_rejected_count(2) == 0,
          "shutdown counts mismatch");
    CHECK(rp_kernel_eval_total_entries() == 4 && rp_kernel_eval_total_rejected() == 0,
          "total counters mismatch");
    printf("phase-counters PASS entries=(1,2,1) executed=(1,2,1) rejected=(0,0,0)\n");
    return 0;
}

static int
run_reject_importlib(const char *home)
{
    rp_kernel_reset_eval_stats();
    rp_kernel_set_phase(RYUPY_KERNEL_PHASE_STARTUP);
    rp_kernel_set_strict_reject(1);

    PyStatus st;
    int rc = init_minimal_kernel(home, 1, &st);
    CHECK(rc != 0 && PyStatus_Exception(st), "_install_importlib=1 must fail in stripped kernel");
    CHECK(rp_kernel_eval_total_entries() == 0, "failed importlib init must execute 0 frames");
    printf("reject-importlib PASS err=%s\n", st.err_msg ? st.err_msg : "(null)");
    return 0;
}

int
main(int argc, char **argv)
{
    if (argc != 3) {
        fprintf(stderr, "用法: %s <kernel_prefix> <strict-clean|strict-reject|phase-counters|reject-importlib>\n",
                argc > 0 ? argv[0] : "kernel_probe");
        return 2;
    }
    const char *home = argv[1];
    const char *mode = argv[2];
    if (strcmp(mode, "strict-clean") == 0) {
        return run_strict_clean(home);
    }
    if (strcmp(mode, "strict-reject") == 0) {
        return run_strict_reject(home);
    }
    if (strcmp(mode, "phase-counters") == 0) {
        return run_phase_counters(home);
    }
    if (strcmp(mode, "reject-importlib") == 0) {
        return run_reject_importlib(home);
    }
    fprintf(stderr, "未知探针模式: %s\n", mode);
    return 2;
}
