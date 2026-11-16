/* RyuPy C4-1 循环导入、跨线程并发、死锁检测与反向导入 CIL 回调 C 扩展集合。
 *
 * 通过 sys._rp_sync_hook（由探针注入的 PyCapsule）与宿主线程协调阶段同步，无需跨 .so
 * 符号依赖，可在未修改官方 CPython 3.12.13 与 RyuPy 严格内核上运行同一套并发/循环行为：
 * - ROLE_CYCLE_A / ROLE_CYCLE_B: 同线程循环导入，验证单次执行与初始化中局部可见属性。
 * - ROLE_SLOW                  : 跨线程竞争同一模块，验证后到线程等待且仅初始化一次。
 * - ROLE_COORD_A / ROLE_COORD_B: 线程 1 导入 A 期间阻塞等线程 2 导入 B，验证无过度全局锁死锁。
 * - ROLE_SP_DL_A / ROLE_SP_DL_B: 单阶段未发布 sys.modules 前跨线程互等，验证死锁检测不无限挂起。
 * - ROLE_MP_DL_A / ROLE_MP_DL_B: 多阶段已发布 sys.modules 后跨线程互等，验证 _lock_unlock_module 语义。
 * - ROLE_CALLS_CIL             : C 扩展反向导入 CIL 回调模块，并验证双向/循环调用单次初始化。
 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>

typedef void (*rp_probe_sync_fn)(int event_id, void *ctx);

static int
call_probe_sync(int event_id)
{
    PyObject *sys_mod = PyImport_ImportModule("sys");
    if (sys_mod == NULL) {
        return -1;
    }
    PyObject *cap = PyObject_GetAttrString(sys_mod, "_rp_sync_hook");
    Py_DECREF(sys_mod);
    if (cap == NULL) {
        PyErr_Clear();
        return 0;
    }
    rp_probe_sync_fn fn = (rp_probe_sync_fn)PyCapsule_GetPointer(cap, "rp_sync");
    void *ctx = PyCapsule_GetContext(cap);
    Py_DECREF(cap);
    if (fn == NULL) {
        return -1;
    }
    /* 释放 GIL 让另一线程安全推进并到达同步点。 */
    Py_BEGIN_ALLOW_THREADS
    fn(event_id, ctx);
    Py_END_ALLOW_THREADS
    return 0;
}

#if defined(ROLE_CYCLE_A)

static int cycle_a_execs = 0;

static PyObject *
cycle_a_exec_count(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    return PyLong_FromLong(cycle_a_execs);
}

static int
cycle_a_exec(PyObject *m)
{
    cycle_a_execs++;
    if (PyModule_AddIntConstant(m, "BEFORE", 11) < 0) {
        return -1;
    }
    PyObject *b = PyImport_ImportModule("mp_cycle_b");
    if (b == NULL) {
        return -1;
    }
    PyObject *fb = PyObject_GetAttrString(b, "FROM_B");
    Py_DECREF(b);
    if (fb == NULL) {
        return -1;
    }
    long fb_val = PyLong_AsLong(fb);
    Py_DECREF(fb);
    if (fb_val != 22) {
        PyErr_SetString(PyExc_AssertionError, "mp_cycle_b.FROM_B != 22");
        return -1;
    }
    return PyModule_AddIntConstant(m, "AFTER", 33);
}

static PyMethodDef cycle_a_methods[] = {
    {"exec_count", cycle_a_exec_count, METH_NOARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static PyModuleDef_Slot cycle_a_slots[] = {
    {Py_mod_exec, cycle_a_exec},
    {0, NULL}
};

static struct PyModuleDef cycle_a_def = {
    PyModuleDef_HEAD_INIT,
    "mp_cycle_a",
    NULL,
    0,
    cycle_a_methods,
    cycle_a_slots,
};

PyMODINIT_FUNC
PyInit_mp_cycle_a(void)
{
    return PyModuleDef_Init(&cycle_a_def);
}

#elif defined(ROLE_CYCLE_B)

static int cycle_b_execs = 0;

static PyObject *
cycle_b_exec_count(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    return PyLong_FromLong(cycle_b_execs);
}

static int
cycle_b_exec(PyObject *m)
{
    cycle_b_execs++;
    PyObject *a = PyImport_ImportModule("mp_cycle_a");
    if (a == NULL) {
        return -1;
    }
    PyObject *spec = PyObject_GetAttrString(a, "__spec__");
    if (spec == NULL) {
        Py_DECREF(a);
        return -1;
    }
    PyObject *init_flag = PyObject_GetAttrString(spec, "_initializing");
    Py_DECREF(spec);
    if (init_flag == NULL || PyObject_IsTrue(init_flag) != 1) {
        Py_XDECREF(init_flag);
        Py_DECREF(a);
        PyErr_SetString(PyExc_AssertionError, "mp_cycle_a.__spec__._initializing must be True");
        return -1;
    }
    Py_DECREF(init_flag);

    PyObject *before = PyObject_GetAttrString(a, "BEFORE");
    if (before == NULL || PyLong_AsLong(before) != 11) {
        Py_XDECREF(before);
        Py_DECREF(a);
        return -1;
    }
    Py_DECREF(before);

    /* 验证访问尚未赋值属性时触发部分初始化模块 AttributeError。 */
    PyObject *after = PyObject_GetAttrString(a, "AFTER");
    if (after != NULL || !PyErr_ExceptionMatches(PyExc_AttributeError)) {
        Py_XDECREF(after);
        Py_DECREF(a);
        PyErr_SetString(PyExc_AssertionError, "expected AttributeError for uninitialized AFTER");
        return -1;
    }
    PyErr_Clear();
    Py_DECREF(a);
    return PyModule_AddIntConstant(m, "FROM_B", 22);
}

static PyMethodDef cycle_b_methods[] = {
    {"exec_count", cycle_b_exec_count, METH_NOARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static PyModuleDef_Slot cycle_b_slots[] = {
    {Py_mod_exec, cycle_b_exec},
    {0, NULL}
};

static struct PyModuleDef cycle_b_def = {
    PyModuleDef_HEAD_INIT,
    "mp_cycle_b",
    NULL,
    0,
    cycle_b_methods,
    cycle_b_slots,
};

PyMODINIT_FUNC
PyInit_mp_cycle_b(void)
{
    return PyModuleDef_Init(&cycle_b_def);
}

#elif defined(ROLE_SLOW)

static int slow_execs = 0;

static PyObject *
slow_exec_count(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    return PyLong_FromLong(slow_execs);
}

static int
slow_exec(PyObject *m)
{
    slow_execs++;
    if (call_probe_sync(1) < 0) {
        return -1;
    }
    return PyModule_AddIntConstant(m, "READY", 42);
}

static PyMethodDef slow_methods[] = {
    {"exec_count", slow_exec_count, METH_NOARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static PyModuleDef_Slot slow_slots[] = {
    {Py_mod_exec, slow_exec},
    {0, NULL}
};

static struct PyModuleDef slow_def = {
    PyModuleDef_HEAD_INIT,
    "mp_slow",
    NULL,
    0,
    slow_methods,
    slow_slots,
};

PyMODINIT_FUNC
PyInit_mp_slow(void)
{
    return PyModuleDef_Init(&slow_def);
}

#elif defined(ROLE_COORD_A)

static int
coord_a_exec(PyObject *m)
{
    if (call_probe_sync(2) < 0) {
        return -1;
    }
    return PyModule_AddIntConstant(m, "COORD_A", 1);
}

static PyModuleDef_Slot coord_a_slots[] = {
    {Py_mod_exec, coord_a_exec},
    {0, NULL}
};

static struct PyModuleDef coord_a_def = {
    PyModuleDef_HEAD_INIT,
    "mp_coord_a",
    NULL,
    0,
    NULL,
    coord_a_slots,
};

PyMODINIT_FUNC
PyInit_mp_coord_a(void)
{
    return PyModuleDef_Init(&coord_a_def);
}

#elif defined(ROLE_COORD_B)

static int
coord_b_exec(PyObject *m)
{
    if (PyModule_AddIntConstant(m, "COORD_B", 2) < 0) {
        return -1;
    }
    return call_probe_sync(3);
}

static PyModuleDef_Slot coord_b_slots[] = {
    {Py_mod_exec, coord_b_exec},
    {0, NULL}
};

static struct PyModuleDef coord_b_def = {
    PyModuleDef_HEAD_INIT,
    "mp_coord_b",
    NULL,
    0,
    NULL,
    coord_b_slots,
};

PyMODINIT_FUNC
PyInit_mp_coord_b(void)
{
    return PyModuleDef_Init(&coord_b_def);
}

#elif defined(ROLE_SP_DL_A)

static int sp_dl_a_entered = 0;

static struct PyModuleDef sp_dl_a_def = {
    PyModuleDef_HEAD_INIT,
    "sp_dl_a",
    NULL,
    -1,
    NULL,
};

PyMODINIT_FUNC
PyInit_sp_dl_a(void)
{
    if (!sp_dl_a_entered) {
        sp_dl_a_entered = 1;
        if (call_probe_sync(4) < 0) {
            return NULL;
        }
        PyObject *b = PyImport_ImportModule("sp_dl_b");
        if (b == NULL) {
            return NULL;
        }
        Py_DECREF(b);
    }
    return PyModule_Create(&sp_dl_a_def);
}

#elif defined(ROLE_SP_DL_B)

static int sp_dl_b_entered = 0;

static struct PyModuleDef sp_dl_b_def = {
    PyModuleDef_HEAD_INIT,
    "sp_dl_b",
    NULL,
    -1,
    NULL,
};

PyMODINIT_FUNC
PyInit_sp_dl_b(void)
{
    if (!sp_dl_b_entered) {
        sp_dl_b_entered = 1;
        if (call_probe_sync(4) < 0) {
            return NULL;
        }
        PyObject *a = PyImport_ImportModule("sp_dl_a");
        if (a == NULL) {
            return NULL;
        }
        Py_DECREF(a);
    }
    return PyModule_Create(&sp_dl_b_def);
}

#elif defined(ROLE_MP_DL_A)

static int
mp_dl_a_exec(PyObject *m)
{
    if (PyModule_AddIntConstant(m, "A_PART", 10) < 0) {
        return -1;
    }
    if (call_probe_sync(5) < 0) {
        return -1;
    }
    PyObject *b = PyImport_ImportModule("mp_dl_b");
    if (b == NULL) {
        return -1;
    }
    Py_DECREF(b);
    return PyModule_AddIntConstant(m, "A_DONE", 11);
}

static PyModuleDef_Slot mp_dl_a_slots[] = {
    {Py_mod_exec, mp_dl_a_exec},
    {0, NULL}
};

static struct PyModuleDef mp_dl_a_def = {
    PyModuleDef_HEAD_INIT,
    "mp_dl_a",
    NULL,
    0,
    NULL,
    mp_dl_a_slots,
};

PyMODINIT_FUNC
PyInit_mp_dl_a(void)
{
    return PyModuleDef_Init(&mp_dl_a_def);
}

#elif defined(ROLE_MP_DL_B)

static int
mp_dl_b_exec(PyObject *m)
{
    if (PyModule_AddIntConstant(m, "B_PART", 20) < 0) {
        return -1;
    }
    if (call_probe_sync(5) < 0) {
        return -1;
    }
    PyObject *a = PyImport_ImportModule("mp_dl_a");
    if (a == NULL) {
        return -1;
    }
    Py_DECREF(a);
    return PyModule_AddIntConstant(m, "B_DONE", 21);
}

static PyModuleDef_Slot mp_dl_b_slots[] = {
    {Py_mod_exec, mp_dl_b_exec},
    {0, NULL}
};

static struct PyModuleDef mp_dl_b_def = {
    PyModuleDef_HEAD_INIT,
    "mp_dl_b",
    NULL,
    0,
    NULL,
    mp_dl_b_slots,
};

PyMODINIT_FUNC
PyInit_mp_dl_b(void)
{
    return PyModuleDef_Init(&mp_dl_b_def);
}

#elif defined(ROLE_CALLS_CIL)

static int calls_cil_execs = 0;

static PyObject *
calls_cil_exec_count(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    return PyLong_FromLong(calls_cil_execs);
}

static int
calls_cil_exec(PyObject *m)
{
    calls_cil_execs++;
    if (PyModule_AddIntConstant(m, "BEFORE_CIL", 7) < 0) {
        return -1;
    }
    PyObject *cil = PyImport_ImportModule("cil_pkg.cil_helper");
    if (cil == NULL) {
        return -1;
    }
    PyObject *val = PyObject_GetAttrString(cil, "CIL_VAL");
    Py_DECREF(cil);
    if (val == NULL) {
        return -1;
    }
    long v = PyLong_AsLong(val);
    Py_DECREF(val);
    if (v == -1 && PyErr_Occurred()) {
        return -1;
    }
    return PyModule_AddIntConstant(m, "GOT_CIL", (int)(v + 1));
}

static PyMethodDef calls_cil_methods[] = {
    {"exec_count", calls_cil_exec_count, METH_NOARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static PyModuleDef_Slot calls_cil_slots[] = {
    {Py_mod_exec, calls_cil_exec},
    {0, NULL}
};

static struct PyModuleDef calls_cil_def = {
    PyModuleDef_HEAD_INIT,
    "ext_calls_cil",
    NULL,
    0,
    calls_cil_methods,
    calls_cil_slots,
};

PyMODINIT_FUNC
PyInit_ext_calls_cil(void)
{
    return PyModuleDef_Init(&calls_cil_def);
}

#else
#error "must define ROLE_*"
#endif
