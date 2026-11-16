/* C1 多版本原生小宿主：调用产品真实描述符并验证元数据、绑定、弱引用与GC。
 * 测试源码只存在手写fixture中，产品运行时没有新增Python求值辅助函数。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

typedef PyObject *(*managed_call)(intptr_t, int, PyObject *, PyObject *, PyObject *, int64_t, const char *);
extern int rp_start(const char *, const char **, int, const char **, int, managed_call);
extern int rp_stop(void);
extern int rp_gil(void);
extern void rp_gil_release(int);
extern const char *rp_init_error(void);
extern int rp_run_entry(const char *, const char *);
extern PyObject *rp_native_method(PyObject *, PyObject *, PyObject *);

static PyObject *callback(intptr_t id, int op, PyObject *a, PyObject *b, PyObject *c, int64_t n, const char *s) {
    (void)id; (void)op; (void)a; (void)b; (void)c; (void)n; (void)s;
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
    source[count] = 0; return source;
}
/* 同一原生可调用体必须能直接交给三种 CPython 描述符构造器，保持宿主版本的绑定。 */
static int install_method(PyObject *globals, const char *source, const char *key, int kind) {
    PyObject *name = PyUnicode_FromString(key), *qualname = PyUnicode_FromFormat("Subject.%s", key);
    if (!name || !qualname) { Py_XDECREF(name); Py_XDECREF(qualname); return -1; }
    PyObject *method = rp_native_method(PyDict_GetItemString(globals, source), name, qualname);
    Py_DECREF(name); Py_DECREF(qualname);
    if (!method) return -1;
    PyObject *decorated = method;
    if (kind == 1) decorated = PyClassMethod_New(method);
    else if (kind == 2) decorated = PyStaticMethod_New(method);
    else if (kind == 3) decorated = PyObject_CallFunctionObjArgs((PyObject *)&PyProperty_Type, method, NULL);
    if (kind) Py_DECREF(method);
    if (!decorated) return -1;
    int status = PyObject_SetAttrString(PyDict_GetItemString(globals, "Subject"), key, decorated);
    Py_DECREF(decorated); return status;
}
int main(int argc, char **argv) {
    if (argc != 6) return 2;
    const char *paths[] = {argv[2], argv[3], argv[4]};
    const char *python_argv[] = {"native-method-contract"};
    if (rp_start(argv[1], paths, 3, python_argv, 1, callback)) {
        fprintf(stderr, "%s\n", rp_init_error()); return 3;
    }
    int gil = rp_gil();
    char *source = read_source(argv[5]);
    if (!source) goto error;
    int result = rp_run_entry("native_method_fixture.py", source);
    free(source);
    if (result) goto error;
    PyObject *globals = PyModule_GetDict(PyImport_AddModule("__main__"));
    if (install_method(globals, "target", "sample", 0) || install_method(globals, "class_target", "kind", 1) ||
        install_method(globals, "static_target", "twice", 2) || install_method(globals, "property_target", "observed_offset", 3)) goto error;
    PyObject *args = PyTuple_Pack(1, Py_True);
    if (!args) goto error;
    PyObject *called = PyObject_CallObject(PyDict_GetItemString(globals, "test"), args);
    Py_DECREF(args);
    if (!called) goto error;
    Py_DECREF(called);
    rp_gil_release(gil);
    return rp_stop() ? 4 : 0;
error:
    PyErr_Print(); rp_gil_release(gil); rp_stop(); return 5;
}
