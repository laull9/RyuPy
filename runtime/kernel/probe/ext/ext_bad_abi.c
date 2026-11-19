/* RyuPy C4-1 坏 ABI / 坏导出符号 C 扩展负例集合（复用官方 importdl 校验语义）。
 *
 * 支持模式宏（必须定义其一）：
 * - BAD_NOSYM     : 不导出 PyInit_bad_nosym（ImportError）。
 * - BAD_NULL      : PyInit_bad_null 返回 NULL 且不设异常（SystemError）。
 * - BAD_UNREPORTED: PyInit_bad_unreported 返回模块同时残留未报告异常（SystemError）。
 * - BAD_TYPE      : PyInit_bad_type 返回非 Module / 非 PyModuleDef 对象（SystemError）。
 * - BAD_UNINIT_DEF: PyInit_bad_uninit_def 返回未调用 PyModuleDef_Init 的 PyModuleDef（SystemError）。
 * - BAD_NEG_SIZE  : PyInit_bad_neg_size 对多阶段 m_slots 使用 m_size=-1（SystemError）。
 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>

#if defined(BAD_NOSYM)

PyMODINIT_FUNC
PyInit_wrong_symbol_name(void)
{
    Py_RETURN_NONE;
}

#elif defined(BAD_NULL)

PyMODINIT_FUNC
PyInit_bad_null(void)
{
    return NULL;
}

#elif defined(BAD_UNREPORTED)

static struct PyModuleDef bad_unreported_def = {
    PyModuleDef_HEAD_INIT,
    "bad_unreported",
    NULL,
    -1,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
};

PyMODINIT_FUNC
PyInit_bad_unreported(void)
{
    PyObject *m = PyModule_Create(&bad_unreported_def);
    PyErr_SetString(PyExc_ValueError, "leaked exception from init");
    return m;
}

#elif defined(BAD_TYPE)

PyMODINIT_FUNC
PyInit_bad_type(void)
{
    return PyLong_FromLong(123);
}

#elif defined(BAD_UNINIT_DEF)

static struct PyModuleDef bad_uninit_def = {
    PyModuleDef_HEAD_INIT,
    "bad_uninit_def",
    NULL,
    0,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
};

PyMODINIT_FUNC
PyInit_bad_uninit_def(void)
{
    /* 刻意跳过 PyModuleDef_Init(&bad_uninit_def) 以触发类型未初始化检查。 */
    return (PyObject *)&bad_uninit_def;
}

#elif defined(BAD_NEG_SIZE)

static int
bad_neg_exec(PyObject *Py_UNUSED(m))
{
    return 0;
}

static PyModuleDef_Slot bad_neg_slots[] = {
    {Py_mod_exec, bad_neg_exec},
    {0, NULL}
};

static struct PyModuleDef bad_neg_def = {
    PyModuleDef_HEAD_INIT,
    "bad_neg_size",
    NULL,
    -1,
    NULL,
    bad_neg_slots,
    NULL,
    NULL,
    NULL,
};

PyMODINIT_FUNC
PyInit_bad_neg_size(void)
{
    return PyModuleDef_Init(&bad_neg_def);
}

#else
#error "must define BAD_*"
#endif
