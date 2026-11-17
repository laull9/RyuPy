/* 机械层测试接头直接复用产品header，真实消费两独立原生扩展；不修改业务globals。 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>
#include <stdint.h>
#define API
#include "code_origin.h"
#include "call_carrier.h"
static PyObject *run(PyObject *self, PyObject *args) {
    (void)self; PyObject *globals, *filename, *target;
    if (!PyArg_ParseTuple(args,"OOO",&globals,&filename,&target)) return NULL;
    RpCallCarrier *context = PyMem_Calloc(1,sizeof(*context));
    if (!context) return PyErr_NoMemory();
    rp_carrier_live++; context->state=1;
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
static PyMethodDef methods[]={{"run",run,METH_VARARGS,NULL},{"call_existing",call_existing,METH_O,NULL},
    {"stats",stats,METH_NOARGS,NULL},{NULL,NULL,0,NULL}};
static struct PyModuleDef definition={PyModuleDef_HEAD_INIT,"native_carrier_harness",NULL,-1,methods,NULL,NULL,NULL,NULL};
PyMODINIT_FUNC PyInit_native_carrier_harness(void) { return PyModule_Create(&definition); }
