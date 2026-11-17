/* 产品程序集专项测试观察器：通过公开Py_AtExit读取产品shim纯C计数。
 * 不包含或改写产品shim；不持Python对象，不执行终态清理，低层回调不调用Python API。 */
#include <Python.h>
extern void rp_thin_stats(long long *, long long *, long long *);
static long long lowlevel_live = -1;
static void observe_exit(void) {
    long long destroyed, calls;
    rp_thin_stats(&lowlevel_live, &destroyed, &calls);
}
__attribute__((visibility("default"))) int program_install_observer(void) { return Py_AtExit(observe_exit); }
__attribute__((visibility("default"))) long long program_lowlevel_live(void) { return lowlevel_live; }

/* 真实frame的builtins为None，逐项调用产品API覆盖晚期受控失败；没有CP私有内存写入。 */
extern void rp_error_text(const char *, const char *);
extern PyObject *rp_make_error(const char *, PyObject *);
extern const char *rp_builtin_exception(PyObject *);
extern PyObject *rp_builtin(const char *);
static PyObject *missing_builtins(PyObject *self, PyObject *unused) {
    (void)self; (void)unused;
    rp_error_text("ValueError", "late");
    if (!PyErr_ExceptionMatches(PyExc_RuntimeError)) return NULL;
    PyErr_Clear();
    PyObject *args = PyTuple_New(0);
    if (!args) return NULL;
    PyObject *error = rp_make_error("ValueError", args); Py_DECREF(args);
    if (!error) return NULL;
    int valid = PyErr_GivenExceptionMatches(error, PyExc_RuntimeError)
        && !strcmp(rp_builtin_exception(error), "BaseException");
    Py_DECREF(error);
    PyObject *value = rp_builtin("len");
    if (value || !PyErr_ExceptionMatches(PyExc_TypeError) || !valid) {
        Py_XDECREF(value); PyErr_SetString(PyExc_AssertionError, "late builtins guard failed"); return NULL;
    }
    PyErr_Clear(); Py_RETURN_NONE;
}
__attribute__((visibility("default"))) int program_check_missing_builtins(void) {
    static PyMethodDef method = {"probe", missing_builtins, METH_NOARGS, NULL};
    PyObject *globals = PyDict_New(), *probe = PyCFunction_New(&method, NULL), *code = NULL, *result = NULL;
    if (!globals || !probe) goto done;
    if (PyDict_SetItemString(globals, "__builtins__", Py_None) < 0
        || PyDict_SetItemString(globals, "probe", probe) < 0) goto done;
    code = Py_CompileString("probe()", "<0228-late-builtins-control>", Py_file_input);
    if (code) result = PyEval_EvalCode(code, globals, globals);
done:
    Py_XDECREF(code); Py_XDECREF(probe); Py_XDECREF(globals);
    if (!result) return -1;
    Py_DECREF(result); return 0;
}
