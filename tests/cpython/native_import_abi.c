/* 固定 C ABI 小宿主：只初始化选中共享 libpython，避免静态链接解释器与共享库的双份类型身份。
 * 核实真实描述符不读可变 builtins、旧源码导出保留、无消费者求值操作明确退场。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>
#include <stdint.h>
#include <stdio.h>

typedef PyObject *(*managed_call)(intptr_t, int, PyObject *, PyObject *, PyObject *, int64_t, const char *);
extern int rp_start(const char *, const char **, int, const char **, int, managed_call);
extern int rp_stop(void);
extern int rp_gil(void);
extern void rp_gil_release(int);
extern const char *rp_init_error(void);
extern PyObject *rp_descriptor_type(int);
extern PyObject *rp_install_imports(const char *);
extern PyObject *rp_op(int, PyObject *, PyObject *, PyObject *, int64_t, const char *);

static PyObject *callback(intptr_t id, int op, PyObject *a, PyObject *b, PyObject *c, int64_t n, const char *text) {
    (void)id; (void)op; (void)a; (void)b; (void)c; (void)n; (void)text;
    Py_INCREF(Py_None); return Py_None;
}
int main(int argc, char **argv) {
    if (argc != 4) return 2;
    const char *paths[] = {argv[2], argv[3]}, *arguments[] = {"native-import-abi"};
    if (rp_start(argv[1], paths, 2, arguments, 1, callback)) {
        fprintf(stderr, "%s\n", rp_init_error()); return 3;
    }
    int gil = rp_gil(), failed = 0;
    const char *names[] = {"property", "staticmethod", "classmethod"};
    PyObject *builtins = PyEval_GetBuiltins(), *saved[3] = {NULL, NULL, NULL};
    for (int i = 0; i < 3; i++) {
        saved[i] = PyDict_GetItemString(builtins, names[i]); Py_XINCREF(saved[i]);
        if (!saved[i] || PyDict_SetItemString(builtins, names[i], Py_None) < 0) { failed = 1; break; }
    }
    if (!failed) for (int i = 0; i < 3; i++) {
        PyObject *type = rp_descriptor_type(i + 1);
        if (!type || type != saved[i]) failed = 1;
        Py_XDECREF(type);
    }
    for (int i = 0; i < 3; i++) if (saved[i]) {
        if (PyDict_SetItemString(builtins, names[i], saved[i]) < 0) failed = 1;
        Py_DECREF(saved[i]);
    }
    if (!failed) {
        PyObject *result = rp_op(29, NULL, NULL, NULL, 0, "raise AssertionError('must not execute')");
        if (result || !PyErr_ExceptionMatches(PyExc_NotImplementedError)) failed = 1;
        Py_XDECREF(result); PyErr_Clear();
        result = rp_install_imports("legacy_abi_marker = 73\n");
        if (!result) failed = 1;
        Py_XDECREF(result);
        PyObject *module = PyDict_GetItemString(PyImport_GetModuleDict(), "_ryupy_imports");
        PyObject *marker = module ? PyObject_GetAttrString(module, "legacy_abi_marker") : NULL;
        if (!marker || PyLong_AsLong(marker) != 73) failed = 1;
        Py_XDECREF(marker);
    }
    if (failed && PyErr_Occurred()) PyErr_Print();
    rp_gil_release(gil);
    if (rp_stop()) failed = 1;
    if (failed) return 4;
    puts("native import ABI passed"); return 0;
}
