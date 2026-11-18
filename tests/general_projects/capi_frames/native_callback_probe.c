/* 独立原生扩展二：PEP489模块状态保存真实frame/code/thunk；traverse/clear让逃逸对象正常GC退账。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>
typedef struct { PyObject *frame, *code, *thunk; } State;
static State *state(PyObject *module) { return (State *)PyModule_GetState(module); }
static PyObject *remember(PyObject *module, PyObject *unused) {
    (void)unused; State *s = state(module);
    PyFrameObject *frame = PyEval_GetFrame();
    if (!frame) { PyErr_SetString(PyExc_RuntimeError, "no native caller frame"); return NULL; }
    PyObject *code = (PyObject *)PyFrame_GetCode(frame);
    if (!code) return NULL;
    PyObject *constants = PyObject_GetAttrString(code, "co_consts");
    if (!constants) { Py_DECREF(code); return NULL; }
    PyObject *thunk = NULL;
    for (Py_ssize_t i=0; i<PyTuple_GET_SIZE(constants); i++) {
        PyObject *item = PyTuple_GET_ITEM(constants, i);
        if (PyCFunction_Check(item)) { thunk = Py_NewRef(item); break; }
    }
    Py_DECREF(constants);
    Py_XSETREF(s->frame, Py_NewRef((PyObject *)frame)); Py_XSETREF(s->code, code); Py_XSETREF(s->thunk, thunk);
    return PyBool_FromLong(thunk != NULL);
}
static PyObject *replay(PyObject *module, PyObject *unused) {
    (void)unused; State *s = state(module);
    if (!s->thunk) { PyErr_SetString(PyExc_RuntimeError, "no escaped native thunk"); return NULL; }
    return PyObject_CallNoArgs(s->thunk);
}
static PyObject *call(PyObject *module, PyObject *target) {
    (void)module;
    PyObject *before = PyEval_GetGlobals();
    if (!before) { PyErr_SetString(PyExc_RuntimeError, "no native callback globals"); return NULL; }
    Py_INCREF(before);
    PyObject *result = PyObject_CallNoArgs(target);
    if (!result) { Py_DECREF(before); return NULL; }
    PyObject *answer = PyTuple_Pack(3, before, result, PyEval_GetGlobals() == before ? Py_True : Py_False);
    Py_DECREF(before); Py_DECREF(result); return answer;
}
static int clear(PyObject *module) {
    State *s = state(module); Py_CLEAR(s->frame); Py_CLEAR(s->code); Py_CLEAR(s->thunk); return 0;
}
static PyObject *forget(PyObject *module, PyObject *unused) { (void)unused; clear(module); Py_RETURN_NONE; }
static int traverse(PyObject *module, visitproc visit, void *arg) {
    State *s = state(module); Py_VISIT(s->frame); Py_VISIT(s->code); Py_VISIT(s->thunk); return 0;
}
static PyMethodDef methods[] = {{"remember",remember,METH_NOARGS,NULL},{"replay",replay,METH_NOARGS,NULL},
    {"call",call,METH_O,NULL},{"forget",forget,METH_NOARGS,NULL},{NULL,NULL,0,NULL}};
static int exec_module(PyObject *module) { (void)module; return 0; }
static PyModuleDef_Slot slots[] = {{Py_mod_exec, exec_module},{0,NULL}};
static struct PyModuleDef definition = {PyModuleDef_HEAD_INIT,"native_callback_probe",NULL,sizeof(State),methods,
    slots,traverse,clear,NULL};
PyMODINIT_FUNC PyInit_native_callback_probe(void) { return PyModuleDef_Init(&definition); }
