/* 手写原生扩展合同：共同归属的二进制必须在搬迁产品中经真实CPython ABI完成数值调用。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>

static PyObject *add(PyObject *self, PyObject *args) {
    long left, right;
    (void)self;
    if (!PyArg_ParseTuple(args, "ll", &left, &right)) return NULL;
    return PyLong_FromLong(left + right);
}
static PyMethodDef methods[] = {
    {"add", add, METH_VARARGS, "返回两个整数之和。"},
    {NULL, NULL, 0, NULL}
};
static struct PyModuleDef module = {PyModuleDef_HEAD_INIT, "_pulse", NULL, -1, methods};
PyMODINIT_FUNC PyInit__pulse(void) { return PyModule_Create(&module); }
