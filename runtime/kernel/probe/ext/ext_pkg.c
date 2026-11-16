/* RyuPy C4-1 父子包、fromlist 穿透与相对导入手写 C 扩展集合。
 *
 * 支持角色宏（必须定义其一）：
 * - ROLE_CPKG        : 包 cpkg（__init__.so），含 __all__=["csub"] 与 rel_import。
 * - ROLE_CPKG_CSUB   : 子模块 cpkg.csub（含 rel_import）。
 * - ROLE_CPKG_SUBPKG : 子包 cpkg.subpkg。
 * - ROLE_CPKG_LEAF   : 深层子模块 cpkg.subpkg.leaf（含 rel_import 测试 level=1/2/3）。
 * - ROLE_CPKG_SIBLING: 同级子模块 cpkg.subpkg.sibling。
 * - ROLE_CPKG_BROKEN : cpkg.broken_dep，在 Py_mod_exec 内导入不存在模块，验证 fromlist 穿透与回滚。
 * - ROLE_CIL_CSUB    : 挂在 CIL 包 cil_pkg 下的真实 .so 子模块 cil_pkg.c_sub。
 */
#define PY_SSIZE_T_CLEAN
#include <Python.h>

static PyObject *
common_rel_import(PyObject *self, PyObject *args)
{
    PyObject *name = NULL;
    int level = 0;
    PyObject *fromlist = Py_None;
    if (!PyArg_ParseTuple(args, "Oi|O:rel_import", &name, &level, &fromlist)) {
        return NULL;
    }
    PyObject *globals = PyModule_GetDict(self);
    if (globals == NULL) {
        return NULL;
    }
    return PyImport_ImportModuleLevelObject(name, globals, NULL, fromlist, level);
}

#if defined(ROLE_CPKG)

static PyMethodDef cpkg_methods[] = {
    {"rel_import", common_rel_import, METH_VARARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static int
cpkg_exec(PyObject *m)
{
    if (PyModule_AddStringConstant(m, "PKG_TAG", "cpkg") < 0) {
        return -1;
    }
    PyObject *all_list = Py_BuildValue("[s]", "csub");
    if (all_list == NULL) {
        return -1;
    }
    int rc = PyModule_AddObjectRef(m, "__all__", all_list);
    Py_DECREF(all_list);
    return rc;
}

static PyModuleDef_Slot cpkg_slots[] = {
    {Py_mod_exec, cpkg_exec},
    {0, NULL}
};

static struct PyModuleDef cpkg_def = {
    PyModuleDef_HEAD_INIT,
    "cpkg",
    NULL,
    0,
    cpkg_methods,
    cpkg_slots,
};

PyMODINIT_FUNC
PyInit_cpkg(void)
{
    return PyModuleDef_Init(&cpkg_def);
}

#elif defined(ROLE_CPKG_CSUB)

static PyMethodDef csub_methods[] = {
    {"rel_import", common_rel_import, METH_VARARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static int
csub_exec(PyObject *m)
{
    return PyModule_AddIntConstant(m, "SUB_VAL", 42);
}

static PyModuleDef_Slot csub_slots[] = {
    {Py_mod_exec, csub_exec},
    {0, NULL}
};

static struct PyModuleDef csub_def = {
    PyModuleDef_HEAD_INIT,
    "csub",
    NULL,
    0,
    csub_methods,
    csub_slots,
};

PyMODINIT_FUNC
PyInit_csub(void)
{
    return PyModuleDef_Init(&csub_def);
}

#elif defined(ROLE_CPKG_SUBPKG)

static int
subpkg_exec(PyObject *m)
{
    return PyModule_AddIntConstant(m, "SUBPKG", 1);
}

static PyModuleDef_Slot subpkg_slots[] = {
    {Py_mod_exec, subpkg_exec},
    {0, NULL}
};

static struct PyModuleDef subpkg_def = {
    PyModuleDef_HEAD_INIT,
    "subpkg",
    NULL,
    0,
    NULL,
    subpkg_slots,
};

PyMODINIT_FUNC
PyInit_subpkg(void)
{
    return PyModuleDef_Init(&subpkg_def);
}

#elif defined(ROLE_CPKG_LEAF)

static PyMethodDef leaf_methods[] = {
    {"rel_import", common_rel_import, METH_VARARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static int
leaf_exec(PyObject *m)
{
    return PyModule_AddIntConstant(m, "LEAF", 77);
}

static PyModuleDef_Slot leaf_slots[] = {
    {Py_mod_exec, leaf_exec},
    {0, NULL}
};

static struct PyModuleDef leaf_def = {
    PyModuleDef_HEAD_INIT,
    "leaf",
    NULL,
    0,
    leaf_methods,
    leaf_slots,
};

PyMODINIT_FUNC
PyInit_leaf(void)
{
    return PyModuleDef_Init(&leaf_def);
}

#elif defined(ROLE_CPKG_SIBLING)

static int
sibling_exec(PyObject *m)
{
    return PyModule_AddIntConstant(m, "SIBLING", 99);
}

static PyModuleDef_Slot sibling_slots[] = {
    {Py_mod_exec, sibling_exec},
    {0, NULL}
};

static struct PyModuleDef sibling_def = {
    PyModuleDef_HEAD_INIT,
    "sibling",
    NULL,
    0,
    NULL,
    sibling_slots,
};

PyMODINIT_FUNC
PyInit_sibling(void)
{
    return PyModuleDef_Init(&sibling_def);
}

#elif defined(ROLE_CPKG_BROKEN)

static int
broken_exec(PyObject *m)
{
    if (PyModule_AddIntConstant(m, "PARTIAL", 1) < 0) {
        return -1;
    }
    PyObject *missing = PyImport_ImportModule("nonexistent_inner_dep");
    if (missing == NULL) {
        return -1;
    }
    Py_DECREF(missing);
    return 0;
}

static PyModuleDef_Slot broken_slots[] = {
    {Py_mod_exec, broken_exec},
    {0, NULL}
};

static struct PyModuleDef broken_def = {
    PyModuleDef_HEAD_INIT,
    "broken_dep",
    NULL,
    0,
    NULL,
    broken_slots,
};

PyMODINIT_FUNC
PyInit_broken_dep(void)
{
    return PyModuleDef_Init(&broken_def);
}

#elif defined(ROLE_CIL_CSUB)

static int cil_csub_exec_count = 0;

static PyObject *
cil_csub_exec_count_fn(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    return PyLong_FromLong(cil_csub_exec_count);
}

static PyMethodDef cil_csub_methods[] = {
    {"rel_import", common_rel_import, METH_VARARGS, NULL},
    {"exec_count", cil_csub_exec_count_fn, METH_NOARGS, NULL},
    {NULL, NULL, 0, NULL}
};

static int
cil_csub_exec(PyObject *m)
{
    cil_csub_exec_count++;
    return PyModule_AddIntConstant(m, "C_SUB_VAL", 505);
}

static PyModuleDef_Slot cil_csub_slots[] = {
    {Py_mod_exec, cil_csub_exec},
    {0, NULL}
};

static struct PyModuleDef cil_csub_def = {
    PyModuleDef_HEAD_INIT,
    "c_sub",
    NULL,
    0,
    cil_csub_methods,
    cil_csub_slots,
};

PyMODINIT_FUNC
PyInit_c_sub(void)
{
    return PyModuleDef_Init(&cil_csub_def);
}

#else
#error "must define ROLE_CPKG* or ROLE_CIL_CSUB"
#endif
