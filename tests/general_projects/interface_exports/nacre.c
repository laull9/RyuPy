/* PEP489多阶段模块与PEP573关联heap type：独立模块状态和实例布局参与真实回调。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>

typedef struct { long factor; } ModuleState;
typedef struct { PyObject_HEAD long bias; PyObject *label; } Gauge;

static int gauge_init(Gauge *self, PyObject *args, PyObject *kwargs) {
    long bias = 2;
    const char *label = "seed";
    static char *names[] = {"", "label", NULL};
    if (!PyArg_ParseTupleAndKeywords(args, kwargs, "|l$s:Gauge", names, &bias, &label)) return -1;
    PyObject *text = PyUnicode_FromString(label);
    if (!text) return -1;
    self->bias = bias; Py_XSETREF(self->label, text);
    return 0;
}
static void gauge_dealloc(Gauge *self) {
    PyTypeObject *type = Py_TYPE(self);
    Py_XDECREF(self->label);
    type->tp_free((PyObject *)self);
    Py_DECREF(type);
}
static PyObject *sample(Gauge *self, PyObject *args) {
    PyObject *value, *callback;
    if (!PyArg_ParseTuple(args, "OO:sample", &value, &callback)) return NULL;
    ModuleState *state = (ModuleState *)PyType_GetModuleState(Py_TYPE(self));
    if (!state) return NULL;
    PyObject *offset = PyLong_FromLong(self->bias + state->factor);
    if (!offset) return NULL;
    PyObject *argument = PyNumber_Add(value, offset);
    Py_DECREF(offset);
    if (!argument) return NULL;
    PyObject *result = PyObject_CallOneArg(callback, argument);
    Py_DECREF(argument);
    return result;
}
static PyObject *echo(Gauge *self, PyObject *args) {
    PyObject *value, *callback;
    (void)self;
    if (!PyArg_ParseTuple(args, "OO:echo", &value, &callback)) return NULL;
    return PyObject_CallOneArg(callback, value);
}
static PyObject *label(Gauge *self, PyObject *unused) {
    (void)unused;
    return Py_NewRef(self->label);
}
static PyMethodDef gauge_methods[] = {
    {"sample", (PyCFunction)sample, METH_VARARGS, "使用实例偏移及模块状态后回调。"},
    {"echo", (PyCFunction)echo, METH_VARARGS, "保持原值身份的回调。"},
    {"label", (PyCFunction)label, METH_NOARGS, "返回实例拥有的标签。"},
    {NULL, NULL, 0, NULL}
};
static PyType_Slot gauge_slots[] = {
    {Py_tp_new, PyType_GenericNew}, {Py_tp_init, gauge_init},
    {Py_tp_dealloc, gauge_dealloc}, {Py_tp_methods, gauge_methods}, {0, NULL}
};
static PyType_Spec gauge_spec = {
    "violet_ridge.engine._nacre.Gauge", sizeof(Gauge), 0, Py_TPFLAGS_DEFAULT, gauge_slots
};
static int execute(PyObject *module) {
    ModuleState *state = (ModuleState *)PyModule_GetState(module);
    state->factor = 5;
    PyObject *type = PyType_FromModuleAndSpec(module, &gauge_spec, NULL);
    if (!type) return -1;
    if (PyModule_AddObject(module, "Gauge", type) < 0) { Py_DECREF(type); return -1; }
    return 0;
}
static PyModuleDef_Slot module_slots[] = {{Py_mod_exec, execute}, {0, NULL}};
static struct PyModuleDef definition = {
    PyModuleDef_HEAD_INIT, "_nacre", NULL, sizeof(ModuleState), NULL, module_slots, NULL, NULL, NULL
};
PyMODINIT_FUNC PyInit__nacre(void) { return PyModuleDef_Init(&definition); }
