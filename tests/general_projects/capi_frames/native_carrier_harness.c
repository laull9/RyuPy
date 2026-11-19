/* 机械层测试接头直接复用产品header，真实消费两独立原生扩展；不修改业务globals。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>
#include <stdint.h>
#define API
#include "code_origin.h"
#include "call_carrier.h"
static PyObject *run(PyObject *self, PyObject *args) {
    (void)self; PyObject *globals, *filename, *target, *builtins = NULL;
    if (!PyArg_ParseTuple(args,"OOO|O",&globals,&filename,&target,&builtins)) return NULL;
    RpCallCarrier *context = PyMem_Calloc(1,sizeof(*context));
    if (!context) return PyErr_NoMemory();
    rp_carrier_live++; context->state=1; context->captured_builtins=Py_XNewRef(builtins);
    context->target=Py_NewRef(target); context->args=PyTuple_New(0);
    if (!context->args) { rp_carrier_close(context); PyMem_Free(context); rp_carrier_live--; return NULL; }
    return rp_carrier_run(context,globals,filename);
}
static PyObject *call_existing(PyObject *self, PyObject *target) {
    (void)self; PyObject *args=PyTuple_New(0);
    if (!args) return NULL;
    PyObject *result=rp_call_with_frame(target,args,NULL); Py_DECREF(args); return result;
}
static PyObject *stats(PyObject *self, PyObject *unused) {
    (void)self; (void)unused; return Py_BuildValue("LL",(long long)rp_carrier_live,(long long)rp_carrier_calls);
}
static PyObject *origin(PyObject *self, PyObject *code) { (void)self; return PyLong_FromLong(rp_code_origin(code)); }
static PyObject *origin_stats(PyObject *self, PyObject *unused) {
    (void)self; (void)unused; int64_t records,active; rp_code_origin_stats(&records,&active);
    return Py_BuildValue("LL",(long long)records,(long long)active);
}
static PyObject *kind(PyObject *self, PyObject *unused) {
    (void)self; (void)unused; PyObject *globals=NULL;
    int value=rp_carrier_frame_context(&globals); Py_XDECREF(globals);
    return value<0 ? NULL : PyLong_FromLong(value);
}
static PyObject *repeat(PyObject *self, PyObject *args) {
    (void)self; PyObject *target; Py_ssize_t count;
    if (!PyArg_ParseTuple(args,"On",&target,&count)) return NULL;
    for (Py_ssize_t i=0;i<count;i++) { PyObject *value=PyObject_CallNoArgs(target); if (!value) return NULL; Py_DECREF(value); }
    Py_RETURN_NONE;
}
static PyMethodDef methods[]={{"origin",origin,METH_O,NULL},{"origin_stats",origin_stats,METH_NOARGS,NULL},
    {"kind",kind,METH_NOARGS,NULL},{"repeat",repeat,METH_VARARGS,NULL},{"run",run,METH_VARARGS,NULL},{"call_existing",call_existing,METH_O,NULL},
    {"stats",stats,METH_NOARGS,NULL},{NULL,NULL,0,NULL}};
static struct PyModuleDef definition={PyModuleDef_HEAD_INIT,"native_carrier_harness",NULL,-1,methods,NULL,NULL,NULL,NULL};
PyMODINIT_FUNC PyInit_native_carrier_harness(void) { return PyModule_Create(&definition); }
