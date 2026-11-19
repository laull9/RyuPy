/* RyuPy 严格零字节码内核 C4-1 纯 C Loader 类型家族汇总与内建 Importer 实现。
 *
 * 本头文件由锚点机制注入官方 CPython 3.12.13 的 `Python/ryupy_import_loaders.h`，
 * 由 `Python/import.c` 在 `import_find_and_load` 之前包含：
 * 1. 包含 `ryupy_import_loader_common.h`、`ryupy_import_ext_loader.h` 与 `ryupy_import_cil_loader.h`，
 *    按职责拆分子模块确保每文件精简且保留完整注释契约。
 * 2. 实现 `RpBuiltinLoader`（`importlib._bootstrap.BuiltinImporter`）：保持 `create_module`/`exec_module`
 *    的 `METH_STATIC` 绑定以及 `is_package`/`get_code`/`get_source` 的 `METH_CLASS` 绑定，
 *    并统一改为 `METH_VARARGS | METH_KEYWORDS` + `PyArg_ParseTupleAndKeywords`，准确支持
 *    `spec=` / `module=` / `fullname=` 关键字传参及缺失/重复/未知关键字 `TypeError` 拒绝。
 * 3. 提供三种 Loader 与 `ModuleSpec` 的统一类型延迟构造及实例工厂入口。
 */
#ifndef Py_RYUPY_IMPORT_LOADERS_H
#define Py_RYUPY_IMPORT_LOADERS_H

#include "ryupy_import_loader_common.h"

/* --- RpBuiltinLoader（类对象即 __loader__） ------------------------------ */

static PyObject *
rp_builtin_loader_create_module(PyObject *Py_UNUSED(ignored), PyObject *args, PyObject *kwds)
{
    PyObject *spec = rp_loader_parse_kw(args, kwds, "O:create_module", rp_kw_spec, 0, NULL);
    if (spec == NULL) {
        return NULL;
    }
    PyThreadState *tstate = _PyThreadState_GET();
    PyObject *name = PyObject_GetAttr(spec, &_Py_ID(name));
    if (name == NULL) {
        return NULL;
    }
    if (!PyUnicode_Check(name)) {
        PyErr_Format(PyExc_TypeError, "spec.name must be str, not %.200s",
                     Py_TYPE(name)->tp_name);
        Py_DECREF(name);
        return NULL;
    }
    PyObject *mod = create_builtin(tstate, name, spec);
    Py_DECREF(name);
    if (mod == Py_None) {
        Py_DECREF(mod);
        PyObject *spec_name = PyObject_GetAttr(spec, &_Py_ID(name));
        if (spec_name != NULL) {
            PyObject *msg = PyUnicode_FromFormat("%R is not a built-in module", spec_name);
            if (msg != NULL) {
                (void)PyErr_SetImportError(msg, spec_name, NULL);
                Py_DECREF(msg);
            }
            Py_DECREF(spec_name);
        }
        return NULL;
    }
    return mod;
}

static PyObject *
rp_builtin_loader_exec_module(PyObject *Py_UNUSED(ignored), PyObject *args, PyObject *kwds)
{
    PyObject *mod = rp_loader_parse_kw(args, kwds, "O:exec_module", rp_kw_module, 0, NULL);
    if (mod == NULL) {
        return NULL;
    }
    if (exec_builtin_or_dynamic(mod) < 0) {
        return NULL;
    }
    Py_RETURN_NONE;
}

static int
rp_require_builtin_arg(PyObject *fullname)
{
    if (!PyUnicode_Check(fullname)) {
        PyErr_Format(PyExc_TypeError, "fullname must be str, not %.200s",
                     Py_TYPE(fullname)->tp_name);
        return -1;
    }
    if (is_builtin(fullname) == 0) {
        PyObject *msg = PyUnicode_FromFormat("%R is not a built-in module", fullname);
        if (msg != NULL) {
            (void)PyErr_SetImportError(msg, fullname, NULL);
            Py_DECREF(msg);
        }
        return -1;
    }
    return 0;
}

static PyObject *
rp_builtin_loader_is_package(PyObject *Py_UNUSED(cls), PyObject *args, PyObject *kwds)
{
    PyObject *fullname = rp_loader_parse_kw(args, kwds, "O:is_package", rp_kw_fullname, 0, NULL);
    if (fullname == NULL || rp_require_builtin_arg(fullname) < 0) {
        return NULL;
    }
    Py_RETURN_FALSE;
}

static PyObject *
rp_builtin_loader_require_none(PyObject *args, PyObject *kwds, const char *fmt)
{
    PyObject *fullname = rp_loader_parse_kw(args, kwds, fmt, rp_kw_fullname, 0, NULL);
    if (fullname == NULL || rp_require_builtin_arg(fullname) < 0) {
        return NULL;
    }
    Py_RETURN_NONE;
}

static PyObject *
rp_builtin_loader_get_code(PyObject *Py_UNUSED(cls), PyObject *args, PyObject *kwds)
{
    return rp_builtin_loader_require_none(args, kwds, "O:get_code");
}

static PyObject *
rp_builtin_loader_get_source(PyObject *Py_UNUSED(cls), PyObject *args, PyObject *kwds)
{
    return rp_builtin_loader_require_none(args, kwds, "O:get_source");
}

static PyMethodDef rp_builtin_loader_methods[] = {
    {"create_module", (PyCFunction)(void (*)(void))rp_builtin_loader_create_module,
     METH_VARARGS | METH_KEYWORDS | METH_STATIC, NULL},
    {"exec_module", (PyCFunction)(void (*)(void))rp_builtin_loader_exec_module,
     METH_VARARGS | METH_KEYWORDS | METH_STATIC, NULL},
    {"is_package", (PyCFunction)(void (*)(void))rp_builtin_loader_is_package,
     METH_VARARGS | METH_KEYWORDS | METH_CLASS, NULL},
    {"get_code", (PyCFunction)(void (*)(void))rp_builtin_loader_get_code,
     METH_VARARGS | METH_KEYWORDS | METH_CLASS, NULL},
    {"get_source", (PyCFunction)(void (*)(void))rp_builtin_loader_get_source,
     METH_VARARGS | METH_KEYWORDS | METH_CLASS, NULL},
    {NULL, NULL, 0, NULL}
};

static PyType_Slot rp_builtin_loader_slots[] = {
    {Py_tp_methods, rp_builtin_loader_methods},
    {0, NULL}
};

static PyType_Spec rp_builtin_loader_type_spec = {
    "importlib._bootstrap.BuiltinImporter",
    sizeof(PyObject),
    0,
    Py_TPFLAGS_DEFAULT,
    rp_builtin_loader_slots,
};

#include "ryupy_import_ext_loader.h"
#include "ryupy_import_cil_loader.h"

/* --- 类型初始化与实例构造辅助 ----------------------------------------- */

static int
rp_kernel_ensure_import_types(RpKernelImportState *state)
{
    if (state->spec_type == NULL) {
        state->spec_type = (PyTypeObject *)PyType_FromSpec(&rp_spec_type_spec);
        if (state->spec_type == NULL) return -1;
    }
    if (state->builtin_loader_type == NULL) {
        state->builtin_loader_type = (PyTypeObject *)PyType_FromSpec(&rp_builtin_loader_type_spec);
        if (state->builtin_loader_type == NULL) return -1;
    }
    if (state->extension_loader_type == NULL) {
        state->extension_loader_type = (PyTypeObject *)PyType_FromSpec(&rp_ext_loader_type_spec);
        if (state->extension_loader_type == NULL) return -1;
    }
    if (state->cil_loader_type == NULL) {
        state->cil_loader_type = (PyTypeObject *)PyType_FromSpec(&rp_cil_loader_type_spec);
        if (state->cil_loader_type == NULL) return -1;
    }
    return 0;
}

static PyObject *
rp_kernel_new_extension_loader(RpKernelImportState *state, PyObject *name,
                               PyObject *path, int is_package)
{
    RpExtensionLoaderObject *loader = PyObject_GC_New(
        RpExtensionLoaderObject, state->extension_loader_type);
    if (loader == NULL) return NULL;
    loader->name = Py_NewRef(name);
    loader->path = Py_NewRef(path);
    loader->is_package_flag = is_package ? 1 : 0;
    PyObject_GC_Track((PyObject *)loader);
    return (PyObject *)loader;
}

static PyObject *
rp_kernel_new_cil_loader(RpKernelImportState *state, PyObject *name,
                         PyObject *origin, int has_location, int is_package)
{
    RpCilLoaderObject *loader = PyObject_GC_New(RpCilLoaderObject, state->cil_loader_type);
    if (loader == NULL) return NULL;
    loader->name = Py_NewRef(name);
    loader->origin = Py_NewRef(origin ? origin : Py_None);
    loader->has_location = has_location ? 1 : 0;
    loader->is_package_flag = is_package ? 1 : 0;
    PyObject_GC_Track((PyObject *)loader);
    return (PyObject *)loader;
}

#endif /* Py_RYUPY_IMPORT_LOADERS_H */
