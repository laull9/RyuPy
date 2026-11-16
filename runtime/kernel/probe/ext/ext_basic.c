/* RyuPy C4-1 手写基础 C 扩展集合（通过编译宏角色切换产出不同 .so）。
 *
 * 支持角色宏（必须定义其一）：
 * - ROLE_SP_BASIC : 单阶段 m_size = -1，验证基础导入、init 计次与扩展缓存复用。
 * - ROLE_SP_REINIT: 单阶段 m_size = 0，验证可重复初始化语义。
 * - ROLE_SP_STATE : 单阶段 m_size > 0，验证 PyModule_GetState 模块状态隔离。
 * - ROLE_MP       : 多阶段 PyModuleDef_Init（Py_mod_create + Py_mod_exec）。
 * - ROLE_MP_CUSTOM: 多阶段 Py_mod_create 返回非 ModuleType 对象（dict）。
 * - ROLE_MP_RETRY : 多阶段 Py_mod_exec 首次失败并回滚、二次重试成功。
 * - ROLE_ATEXIT   : 多阶段扩展，专门验证 Py_FinalizeEx 期间 atexit 回调可合法导入。
 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>

#if defined(ROLE_SP_BASIC)

static int sp_basic_init_calls = 0;

static PyObject *
sp_basic_ping(PyObject *Py_UNUSED(self), PyObject *arg)
{
    long v = PyLong_AsLong(arg);
    if (v == -1 && PyErr_Occurred()) {
        return NULL;
    }
    return PyLong_FromLong(v + 10);
}

static PyObject *
sp_basic_init_count(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    return PyLong_FromLong(sp_basic_init_calls);
}

static PyMethodDef sp_basic_methods[] = {
    {"ping", sp_basic_ping, METH_O, NULL},
    {"init_count", sp_basic_init_count, METH_NOARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static struct PyModuleDef sp_basic_def = {
    PyModuleDef_HEAD_INIT,
    "ext_sp_basic",
    "single-phase basic extension",
    -1,
    sp_basic_methods,
    NULL, NULL, NULL, NULL,
};

PyMODINIT_FUNC
PyInit_ext_sp_basic(void)
{
    sp_basic_init_calls++;
    PyObject *m = PyModule_Create(&sp_basic_def);
    if (m == NULL) {
        return NULL;
    }
    if (PyModule_AddIntConstant(m, "CONSTANT", 100) < 0) {
        Py_DECREF(m);
        return NULL;
    }
    return m;
}

#elif defined(ROLE_SP_REINIT)

static int sp_reinit_calls = 0;

static PyObject *
sp_reinit_count(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    return PyLong_FromLong(sp_reinit_calls);
}

static PyMethodDef sp_reinit_methods[] = {
    {"init_count", sp_reinit_count, METH_NOARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static struct PyModuleDef sp_reinit_def = {
    PyModuleDef_HEAD_INIT, "ext_sp_reinit", NULL, 0, sp_reinit_methods, NULL, NULL, NULL, NULL,
};

PyMODINIT_FUNC
PyInit_ext_sp_reinit(void)
{
    sp_reinit_calls++;
    return PyModule_Create(&sp_reinit_def);
}

#elif defined(ROLE_SP_STATE)

typedef struct {
    int magic;
} SpState;

static int sp_state_calls = 0;

static PyObject *
sp_state_get_magic(PyObject *self, PyObject *Py_UNUSED(unused))
{
    SpState *st = (SpState *)PyModule_GetState(self);
    if (st == NULL) {
        PyErr_SetString(PyExc_RuntimeError, "missing single-phase state");
        return NULL;
    }
    return PyLong_FromLong(st->magic);
}

static PyObject *
sp_state_init_count(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    return PyLong_FromLong(sp_state_calls);
}

static PyMethodDef sp_state_methods[] = {
    {"get_magic", sp_state_get_magic, METH_NOARGS, NULL},
    {"init_count", sp_state_init_count, METH_NOARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static struct PyModuleDef sp_state_def = {
    PyModuleDef_HEAD_INIT, "ext_sp_state", NULL, sizeof(SpState), sp_state_methods, NULL, NULL, NULL, NULL,
};

PyMODINIT_FUNC
PyInit_ext_sp_state(void)
{
    sp_state_calls++;
    PyObject *m = PyModule_Create(&sp_state_def);
    if (m == NULL) {
        return NULL;
    }
    SpState *st = (SpState *)PyModule_GetState(m);
    st->magic = 777;
    return m;
}

#elif defined(ROLE_MP)

typedef struct {
    int value;
} MpState;

static int mp_create_calls = 0;
static int mp_exec_calls = 0;

static PyObject *
mp_get_counts(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    return Py_BuildValue("(ii)", mp_create_calls, mp_exec_calls);
}

static PyObject *
mp_get_state_val(PyObject *self, PyObject *Py_UNUSED(unused))
{
    MpState *st = (MpState *)PyModule_GetState(self);
    if (st == NULL) {
        PyErr_SetString(PyExc_RuntimeError, "missing multi-phase state");
        return NULL;
    }
    return PyLong_FromLong(st->value);
}

static PyObject *
mp_set_state_val(PyObject *self, PyObject *arg)
{
    long v = PyLong_AsLong(arg);
    if (v == -1 && PyErr_Occurred()) {
        return NULL;
    }
    MpState *st = (MpState *)PyModule_GetState(self);
    st->value = (int)v;
    Py_RETURN_NONE;
}

static PyObject *
mp_create(PyObject *spec, PyModuleDef *Py_UNUSED(def))
{
    mp_create_calls++;
    PyObject *name = PyObject_GetAttrString(spec, "name");
    if (name == NULL) {
        return NULL;
    }
    PyObject *m = PyModule_NewObject(name);
    Py_DECREF(name);
    return m;
}

static int
mp_exec(PyObject *m)
{
    mp_exec_calls++;
    MpState *st = (MpState *)PyModule_GetState(m);
    if (st == NULL) {
        PyErr_SetString(PyExc_SystemError, "md_state not allocated before Py_mod_exec");
        return -1;
    }
    st->value = 314;
    return PyModule_AddIntConstant(m, "MP_READY", 1);
}

static PyMethodDef mp_methods[] = {
    {"get_counts", mp_get_counts, METH_NOARGS, NULL},
    {"get_state_val", mp_get_state_val, METH_NOARGS, NULL},
    {"set_state_val", mp_set_state_val, METH_O, NULL},
    {NULL, NULL, 0, NULL}
};

static PyModuleDef_Slot mp_slots[] = {
    {Py_mod_create, mp_create},
    {Py_mod_exec, mp_exec},
    {0, NULL}
};

static struct PyModuleDef mp_def = {
    PyModuleDef_HEAD_INIT, "ext_mp", "multi-phase extension", sizeof(MpState), mp_methods, mp_slots, NULL, NULL, NULL,
};

PyMODINIT_FUNC
PyInit_ext_mp(void)
{
    return PyModuleDef_Init(&mp_def);
}

#elif defined(ROLE_MP_CUSTOM)

static PyObject *
mp_custom_create(PyObject *Py_UNUSED(spec), PyModuleDef *Py_UNUSED(def))
{
    PyObject *d = PyDict_New();
    if (d == NULL) {
        return NULL;
    }
    PyObject *val = PyLong_FromLong(555);
    if (val == NULL || PyDict_SetItemString(d, "custom_marker", val) < 0) {
        Py_XDECREF(val);
        Py_DECREF(d);
        return NULL;
    }
    Py_DECREF(val);
    return d;
}

static PyModuleDef_Slot mp_custom_slots[] = {
    {Py_mod_create, mp_custom_create},
    {0, NULL}
};

static struct PyModuleDef mp_custom_def = {
    PyModuleDef_HEAD_INIT, "ext_mp_custom", NULL, 0, NULL, mp_custom_slots, NULL, NULL, NULL,
};

PyMODINIT_FUNC
PyInit_ext_mp_custom(void)
{
    return PyModuleDef_Init(&mp_custom_def);
}

#elif defined(ROLE_MP_RETRY)

static int mp_retry_attempts = 0;

static PyObject *
mp_retry_attempts_fn(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    return PyLong_FromLong(mp_retry_attempts);
}

static int
mp_retry_exec(PyObject *m)
{
    mp_retry_attempts++;
    if (PyModule_AddIntConstant(m, "PARTIAL", 99) < 0) {
        return -1;
    }
    if (mp_retry_attempts == 1) {
        PyErr_SetString(PyExc_RuntimeError, "transient multi-phase init failure");
        return -1;
    }
    return PyModule_AddIntConstant(m, "OK", 200);
}

static PyMethodDef mp_retry_methods[] = {
    {"attempts", mp_retry_attempts_fn, METH_NOARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static PyModuleDef_Slot mp_retry_slots[] = {
    {Py_mod_exec, mp_retry_exec},
    {0, NULL}
};

static struct PyModuleDef mp_retry_def = {
    PyModuleDef_HEAD_INIT, "ext_mp_retry", NULL, 0, mp_retry_methods, mp_retry_slots, NULL, NULL, NULL,
};

PyMODINIT_FUNC
PyInit_ext_mp_retry(void)
{
    return PyModuleDef_Init(&mp_retry_def);
}

#elif defined(ROLE_ATEXIT)

static int
atexit_exec(PyObject *m)
{
    return PyModule_AddIntConstant(m, "ATEXIT_LOADED", 888);
}

static PyModuleDef_Slot atexit_slots[] = {
    {Py_mod_exec, atexit_exec},
    {0, NULL}
};

static struct PyModuleDef atexit_def = {
    PyModuleDef_HEAD_INIT, "ext_atexit", NULL, 0, NULL, atexit_slots, NULL, NULL, NULL,
};

PyMODINIT_FUNC
PyInit_ext_atexit(void)
{
    return PyModuleDef_Init(&atexit_def);
}

#else
#error "must define ROLE_*"
#endif
