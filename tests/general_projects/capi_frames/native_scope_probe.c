/* 独立原生扩展一：直接读取公开C API的调用globals/locals/frame/code/source，不替调用者修补NULL。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>
static PyObject *facts(PyObject *self, PyObject *unused) {
    (void)self; (void)unused;
    PyObject *globals = PyEval_GetGlobals(), *locals = PyEval_GetLocals();
    PyFrameObject *frame = PyEval_GetFrame();
    PyObject *code = frame ? (PyObject *)PyFrame_GetCode(frame) : Py_NewRef(Py_None);
    if (!code) return NULL;
    PyObject *file = frame ? PyObject_GetAttrString(code, "co_filename") : Py_NewRef(Py_None);
    if (!file) { Py_DECREF(code); return NULL; }
    PyObject *result = PyTuple_Pack(5, globals ? globals : Py_None, locals ? locals : Py_None,
        file, code, frame ? (PyObject *)frame : Py_None);
    Py_DECREF(file); Py_DECREF(code); return result;
}
static PyObject *fail(PyObject *self, PyObject *unused) {
    (void)self; (void)unused;
    PyErr_SetString(PyExc_KeyError, "native-scope-probe"); return NULL;
}
static PyMethodDef methods[] = {
    {"facts", facts, METH_NOARGS, NULL}, {"fail", fail, METH_NOARGS, NULL}, {NULL,NULL,0,NULL}
};
static struct PyModuleDef module = {PyModuleDef_HEAD_INIT, "native_scope_probe", NULL, -1, methods,
    NULL, NULL, NULL, NULL};
PyMODINIT_FUNC PyInit_native_scope_probe(void) { return PyModule_Create(&module); }
