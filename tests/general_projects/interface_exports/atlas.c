/* 传统单阶段扩展：普通对象回调保身份，带关键字偏移的数值调用及原生异常。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>

static PyObject *relay(PyObject *self, PyObject *args, PyObject *kwargs) {
    PyObject *value, *callback;
    long offset = 0;
    static char *names[] = {"", "", "offset", NULL};
    (void)self;
    if (!PyArg_ParseTupleAndKeywords(args, kwargs, "OO|$l:relay", names,
                                    &value, &callback, &offset)) return NULL;
    PyObject *argument = Py_NewRef(value);
    if (offset) {
        PyObject *step = PyLong_FromLong(offset);
        if (!step) { Py_DECREF(argument); return NULL; }
        PyObject *sum = PyNumber_Add(argument, step);
        Py_DECREF(step); Py_DECREF(argument); argument = sum;
        if (!argument) return NULL;
    }
    PyObject *result = PyObject_CallOneArg(callback, argument);
    Py_DECREF(argument);
    return result;
}
static PyObject *reject(PyObject *self, PyObject *code) {
    (void)self;
    long value = PyLong_AsLong(code);
    if (value == -1 && PyErr_Occurred()) return NULL;
    PyErr_Format(PyExc_ValueError, "atlas rejected %ld", value);
    return NULL;
}
static PyMethodDef methods[] = {
    {"relay", (PyCFunction)(void (*)(void))relay, METH_VARARGS | METH_KEYWORDS, "调用实际回调；offset为仅关键字参数。"},
    {"reject", reject, METH_O, "抛出带原始数值的ValueError。"},
    {NULL, NULL, 0, NULL}
};
static struct PyModuleDef definition = {PyModuleDef_HEAD_INIT, "_atlas", NULL, -1, methods};
PyMODINIT_FUNC PyInit__atlas(void) { return PyModule_Create(&definition); }
