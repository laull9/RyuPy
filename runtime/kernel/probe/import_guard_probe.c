/* 无 importlib 时的真实 C API 负例与 PEP 523 严格门探针。
 * 每个模式独立进程；失败入口不得发布模块、执行字节码或把 NULL 送入对象协议。
 * 自定义求值器与生成器 throw 通过真实 Python C API 到达，而非直接调用计数函数。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>
#include <ryupy_kernel.h>
#include <stdio.h>
#include <string.h>

#define CHECK(c, m) do { if (!(c)) { fprintf(stderr, "FAIL: %s\n", m); return 1; } } while (0)

static int hook_calls;

static PyObject *
alternate_eval(PyThreadState *state, struct _PyInterpreterFrame *frame, int throwing)
{
    hook_calls++;
    return _PyEval_EvalFrameDefault(state, frame, throwing);
}

static int
initialize(const char *home)
{
    PyPreConfig pre;
    PyPreConfig_InitIsolatedConfig(&pre);
    pre.utf8_mode = 1;
    PyStatus status = Py_PreInitialize(&pre);
    CHECK(!PyStatus_Exception(status), "preinitialize");
    PyConfig config;
    PyConfig_InitIsolatedConfig(&config);
    config.site_import = 0;
    config.write_bytecode = 0;
    config.parse_argv = 0;
    config.install_signal_handlers = 0;
    config.module_search_paths_set = 1;
    config._install_importlib = 0;
    status = PyConfig_SetBytesString(&config, &config.home, home);
    if (!PyStatus_Exception(status))
        status = PyConfig_SetBytesString(&config, &config.stdio_encoding, "utf-8");
    if (!PyStatus_Exception(status))
        status = PyConfig_SetBytesString(&config, &config.stdio_errors, "surrogateescape");
    if (!PyStatus_Exception(status)) status = Py_InitializeFromConfig(&config);
    PyConfig_Clear(&config);
    CHECK(!PyStatus_Exception(status), status.err_msg ? status.err_msg : "initialize");
    return 0;
}

static PyObject *
evaluate(const char *source)
{
    PyObject *code = Py_CompileString(source, "<guard-probe>", Py_eval_input);
    if (code == NULL) return NULL;
    PyObject *globals = PyDict_New();
    if (globals == NULL) { Py_DECREF(code); return NULL; }
    if (PyDict_SetItemString(globals, "__builtins__", PyEval_GetBuiltins()) < 0) {
        Py_DECREF(code); Py_DECREF(globals); return NULL;
    }
    PyObject *result = PyEval_EvalCode(code, globals, globals);
    Py_DECREF(code); Py_DECREF(globals);
    return result;
}

static int
rejected_import(const char *mode)
{
    PyObject *code = (PyObject *)PyCode_NewEmpty("<guard-probe>", "guard", 1);
    PyObject *name = PyUnicode_FromString("guard_unpublished");
    PyObject *globals = PyDict_New();
    PyObject *result = NULL;
    CHECK(code && name && globals, "allocate probe objects");
    if (strcmp(mode, "magic") == 0) {
        CHECK(PyImport_GetMagicNumber() == -1, "magic must reject");
    }
    else if (strcmp(mode, "exec-paths") == 0)
        result = PyImport_ExecCodeModuleWithPathnames("guard_unpublished", code, NULL, "probe.pyc");
    else if (strcmp(mode, "exec-object") == 0)
        result = PyImport_ExecCodeModuleObject(name, code, NULL, NULL);
    else if (strcmp(mode, "repr") == 0)
        result = PyObject_Repr(PyImport_AddModule("guard_repr"));
    else CHECK(0, "unknown import probe mode");
    CHECK(result == NULL, "importlib consumer must fail");
    CHECK(PyErr_ExceptionMatches(PyExc_NotImplementedError), "explicit unsupported importlib API");
    PyErr_Clear();
    CHECK(PyDict_GetItemString(PyImport_GetModuleDict(), "guard_unpublished") == NULL,
          "failed code execution must not publish a module");
    CHECK(rp_kernel_eval_total_entries() == 0, "unsupported import must not evaluate");
    Py_DECREF(globals); Py_DECREF(name); Py_DECREF(code);
    return 0;
}

static int
custom_eval_guard(void)
{
    PyInterpreterState *interp = PyInterpreterState_Get();
    _PyFrameEvalFunction old = _PyInterpreterState_GetEvalFrameFunc(interp);
    _PyInterpreterState_SetEvalFrameFunc(interp, alternate_eval);
    PyObject *result = evaluate("40 + 2");
    _PyInterpreterState_SetEvalFrameFunc(interp, old);
    CHECK(result == NULL && PyErr_ExceptionMatches(PyExc_RuntimeError), "strict rejection");
    PyErr_Clear();
    CHECK(hook_calls == 0, "custom evaluator must not bypass strict dispatch");
    CHECK(rp_kernel_eval_total_entries() == 1 && rp_kernel_eval_total_rejected() == 1,
          "strict dispatch counted once");
    return 0;
}

static int
throw_context_guard(void)
{
    /* 诊断模式只用于创建真实 suspended generator；严格模式在恢复前重新开启。 */
    rp_kernel_set_strict_reject(0);
    PyObject *generator = evaluate("(value for value in (1, 2))");
    CHECK(generator != NULL, "create real generator");
    rp_kernel_set_strict_reject(1);
    rp_kernel_reset_eval_stats();
    PyObject *original = PyObject_CallFunction(PyExc_ValueError, "s", "injected");
    CHECK(original != NULL, "create throw exception");
    PyObject *result = PyObject_CallMethod(generator, "throw", "O", original);
    CHECK(result == NULL && PyErr_ExceptionMatches(PyExc_RuntimeError), "throw strict rejection");
    PyObject *rejection = PyErr_GetRaisedException();
    PyObject *context = PyException_GetContext(rejection);
    CHECK(context == original, "strict rejection preserves thrown exception as context");
    Py_XDECREF(context); Py_DECREF(rejection); Py_DECREF(original); Py_DECREF(generator);
    CHECK(rp_kernel_eval_total_entries() == 1 && rp_kernel_eval_total_rejected() == 1,
          "generator rejection counted once");
    return 0;
}

int
main(int argc, char **argv)
{
    CHECK(argc == 3, "usage: import_guard_probe PREFIX MODE");
    rp_kernel_reset_eval_stats();
    rp_kernel_set_phase(RYUPY_KERNEL_PHASE_STARTUP);
    rp_kernel_set_strict_reject(1);
    if (initialize(argv[1])) return 1;
    CHECK(rp_kernel_eval_total_entries() == 0, "startup must be bytecode-free");
    rp_kernel_set_phase(RYUPY_KERNEL_PHASE_RUNTIME);
    int failure = strcmp(argv[2], "custom-eval") == 0 ? custom_eval_guard()
        : strcmp(argv[2], "throw-context") == 0 ? throw_context_guard()
        : rejected_import(argv[2]);
    if (failure) return failure;
    rp_kernel_set_phase(RYUPY_KERNEL_PHASE_SHUTDOWN);
    CHECK(Py_FinalizeEx() == 0, "finalize");
    CHECK(rp_kernel_eval_entry_count(RYUPY_KERNEL_PHASE_SHUTDOWN) == 0,
          "shutdown must be bytecode-free");
    printf("PASS %s\n", argv[2]);
    return 0;
}
