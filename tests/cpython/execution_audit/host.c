/* C0 独立原生宿主：复用产品 shim，具名帧通过直接 C API 和反向代理调用，
 * 在 Finalize 之后读取仅含 C 内存的快照，验证关闭观测没有重入 Python。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef PyObject *(*managed_call)(intptr_t, int, PyObject *, PyObject *, PyObject *, int64_t, const char *);
extern int rp_start(const char *, const char **, int, const char **, int, managed_call);
extern int rp_stop(void);
extern int rp_gil(void);
extern void rp_gil_release(int);
extern const char *rp_init_error(void);
extern PyObject *rp_install_imports(const char *);
extern int rp_run_entry(const char *, const char *);
extern PyObject *rp_managed(intptr_t);
extern int rp_execution_audit_start(void);
extern void rp_execution_audit_phase(int);
extern int64_t rp_execution_audit_count(int);
extern int rp_execution_audit_status(int);
extern int rp_execution_audit_row(int, int *, const char **, const char **, int64_t *);

static PyObject *callback(intptr_t identity, int op, PyObject *a, PyObject *b, PyObject *c, int64_t n, const char *s) {
    (void)identity; (void)b; (void)c; (void)n; (void)s;
    if (op == 0) return Py_NewRef(Py_None);
    if (op == 1) return PyObject_CallObject(PyTuple_GetItem(a, 0), NULL);
    PyErr_SetString(PyExc_RuntimeError, "unexpected audit callback operation");
    return NULL;
}

static char *read_source(const char *path) {
    FILE *stream = fopen(path, "rb");
    if (!stream) return NULL;
    if (fseek(stream, 0, SEEK_END)) { fclose(stream); return NULL; }
    long size = ftell(stream);
    if (size < 0 || fseek(stream, 0, SEEK_SET)) { fclose(stream); return NULL; }
    char *source = malloc((size_t)size + 1);
    if (!source) { fclose(stream); return NULL; }
    size_t count = fread(source, 1, (size_t)size, stream);
    fclose(stream);
    if (count != (size_t)size) { free(source); return NULL; }
    source[count] = 0;
    return source;
}

int main(int argc, char **argv) {
    if (argc != 6) return 2;
    int enabled = !strcmp(argv[5], "enabled");
    if (enabled && rp_execution_audit_start() != 0) {
        fprintf(stderr, "%s\n", rp_init_error()); return 3;
    }
    const char *paths[] = {argv[2], argv[3]};
    const char *python_argv[] = {"execution-audit"};
    if (rp_start(argv[1], paths, 2, python_argv, 1, callback)) {
        fprintf(stderr, "%s\n", rp_init_error()); return 4;
    }
    int gil = rp_gil();
    if (!strcmp(argv[5], "late-enable")) {
        if (rp_execution_audit_start() == 0) goto error;
        printf("negative %s\n", rp_init_error());
    }
    PyObject *setup = rp_install_imports("def bootstrap_helper():\n    return 1\nbootstrap_helper()\n");
    if (!setup) goto error;
    Py_DECREF(setup);
    rp_execution_audit_phase(1);
    char *source = read_source(argv[4]);
    if (!source) goto error;
    int result = rp_run_entry("execution_audit_fixture.py", source);
    free(source);
    if (result) goto error;
    PyObject *globals = PyModule_GetDict(PyImport_AddModule("__main__"));
    PyObject *proxy = rp_managed(1);
    if (!proxy) goto error;
    int assigned = PyDict_SetItemString(globals, "native_callback", proxy);
    Py_DECREF(proxy);
    if (assigned) goto error;
    PyObject *called = PyObject_CallObject(PyDict_GetItemString(globals, "run_calls"), NULL);
    if (!called) goto error;
    Py_DECREF(called);
    rp_gil_release(gil);
    if (rp_stop()) return 5;
    printf("totals %lld %lld %lld %d %d %d %d\n", (long long)rp_execution_audit_count(0),
           (long long)rp_execution_audit_count(1), (long long)rp_execution_audit_count(2),
           rp_execution_audit_status(0), rp_execution_audit_status(1),
           rp_execution_audit_status(2), rp_execution_audit_status(3));
    for (int i = 0; ; i++) {
        int phase; const char *file, *function; int64_t calls;
        if (!rp_execution_audit_row(i, &phase, &file, &function, &calls)) break;
        printf("frame %d %lld %s\t%s\n", phase, (long long)calls, function, file);
    }
    return 0;
error:
    PyErr_Print(); rp_gil_release(gil); rp_stop(); return 6;
}
