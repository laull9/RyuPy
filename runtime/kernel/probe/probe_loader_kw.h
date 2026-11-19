/* RyuPy C4-1 探针共享 Loader 关键字调用与正负例校验辅助头。
 *
 * 供 `ext_import_probe.c`（兼容 strict 内核与 `--with-stock-diff` 未修改 CPython 3.12.13）
 * 与 `cil_import_probe.c` 共享使用，避免两处探针各自重复实现单关键字调用、缺失/重复/未知
 * 关键字 `TypeError` 断言以及 Loader 族关键字合约检查：
 * 1. `probe_call_noargs` / `probe_call_kw1`：发起 `obj.method()` 与 `obj.method(kwname=arg)`。
 * 2. `probe_expect_kw_negatives`：验证缺失必选参数、位置+重复关键字、未知关键字均抛 `TypeError`。
 * 3. `probe_verify_builtin_loader_kw` / `probe_verify_ext_loader_kw` / `probe_verify_cil_loader_kw`：
 *    分别校验 BuiltinImporter、ExtensionFileLoader 与 CilModuleLoader 的全方法关键字正/负例。
 */
#ifndef RYUPY_PROBE_LOADER_KW_H
#define RYUPY_PROBE_LOADER_KW_H

static inline PyObject *
probe_call_noargs(PyObject *obj, const char *method)
{
    PyObject *mname = PyUnicode_FromString(method);
    if (mname == NULL) return NULL;
    PyObject *res = PyObject_CallMethodNoArgs(obj, mname);
    Py_DECREF(mname);
    return res;
}

static inline PyObject *
probe_call_kw1(PyObject *obj, const char *method, const char *kwname, PyObject *arg)
{
    PyObject *fn = PyObject_GetAttrString(obj, method);
    if (fn == NULL) return NULL;
    PyObject *empty = PyTuple_New(0);
    PyObject *kw = PyDict_New();
    PyObject *res = NULL;
    if (empty != NULL && kw != NULL && PyDict_SetItemString(kw, kwname, arg) == 0) {
        res = PyObject_Call(fn, empty, kw);
    }
    Py_XDECREF(kw);
    Py_XDECREF(empty);
    Py_DECREF(fn);
    return res;
}

static inline int
probe_expect_kw_negatives(PyObject *obj, const char *method, const char *good_kw,
                          PyObject *good_arg, int require_missing_fail)
{
    PyObject *fn = PyObject_GetAttrString(obj, method);
    if (fn == NULL) return -1;

    if (require_missing_fail) {
        PyObject *r_miss = PyObject_CallNoArgs(fn);
        if (r_miss != NULL || !PyErr_ExceptionMatches(PyExc_TypeError)) {
            Py_XDECREF(r_miss);
            Py_DECREF(fn);
            fprintf(stderr, "PROBE_FAIL: %s() missing arg did not raise TypeError\n", method);
            return -1;
        }
        PyErr_Clear();
    }

    PyObject *pos = PyTuple_Pack(1, good_arg);
    PyObject *empty = PyTuple_New(0);
    PyObject *kw_dup = PyDict_New();
    PyObject *kw_bad = PyDict_New();
    int rc = -1;
    if (pos != NULL && empty != NULL && kw_dup != NULL && kw_bad != NULL &&
        PyDict_SetItemString(kw_dup, good_kw, good_arg) == 0 &&
        PyDict_SetItemString(kw_bad, "__unexpected_kw__", good_arg) == 0) {
        PyObject *r_dup = PyObject_Call(fn, pos, kw_dup);
        if (r_dup != NULL || !PyErr_ExceptionMatches(PyExc_TypeError)) {
            Py_XDECREF(r_dup);
            fprintf(stderr, "PROBE_FAIL: %s(pos, %s=...) duplicate kw did not raise TypeError\n",
                    method, good_kw);
            goto cleanup;
        }
        PyErr_Clear();

        PyObject *r_bad = PyObject_Call(fn, empty, kw_bad);
        if (r_bad != NULL || !PyErr_ExceptionMatches(PyExc_TypeError)) {
            Py_XDECREF(r_bad);
            fprintf(stderr, "PROBE_FAIL: %s(__unexpected_kw__=...) did not raise TypeError\n",
                    method);
            goto cleanup;
        }
        PyErr_Clear();
        rc = 0;
    }

cleanup:
    Py_XDECREF(kw_bad);
    Py_XDECREF(kw_dup);
    Py_XDECREF(empty);
    Py_XDECREF(pos);
    Py_DECREF(fn);
    return rc;
}

/* 校对 BuiltinImporter 全核心方法关键字正/负例（覆盖 staticmethod 与 classmethod 绑定）。 */
static inline int
probe_verify_builtin_loader_kw(PyObject *loader, PyObject *spec, PyObject *mod, const char *name_cstr)
{
    PyObject *name = PyUnicode_FromString(name_cstr);
    PROBE_CHECK(name != NULL, "alloc builtin name");
    PyObject *r_pkg = probe_call_kw1(loader, "is_package", "fullname", name);
    PROBE_CHECK(r_pkg == Py_False, "BuiltinImporter.is_package(fullname=...) != False");
    Py_XDECREF(r_pkg);
    PyObject *r_code = probe_call_kw1(loader, "get_code", "fullname", name);
    PROBE_CHECK(r_code == Py_None, "BuiltinImporter.get_code(fullname=...) != None");
    Py_XDECREF(r_code);
    PyObject *r_src = probe_call_kw1(loader, "get_source", "fullname", name);
    PROBE_CHECK(r_src == Py_None, "BuiltinImporter.get_source(fullname=...) != None");
    Py_XDECREF(r_src);
    PyObject *created = probe_call_kw1(loader, "create_module", "spec", spec);
    PROBE_CHECK(created != NULL && PyModule_Check(created), "BuiltinImporter.create_module(spec=...)");
    PyObject *exec_res = probe_call_kw1(loader, "exec_module", "module", created);
    PROBE_CHECK(exec_res == Py_None, "BuiltinImporter.exec_module(module=...) != None");
    Py_XDECREF(exec_res);
    Py_DECREF(created);

    PROBE_CHECK(probe_expect_kw_negatives(loader, "create_module", "spec", spec, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "exec_module", "module", mod, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "is_package", "fullname", name, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "get_code", "fullname", name, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "get_source", "fullname", name, 1) == 0,
                "BuiltinImporter kw negatives");
    Py_DECREF(name);
    return 0;
}

/* 校对 ExtensionFileLoader 全方法关键字正/负例（兼容 strict 内核与 stock 3.12.13）。 */
static inline int
probe_verify_ext_loader_kw(PyObject *loader, PyObject *spec, PyObject *mod,
                           const char *name_cstr, const char *expected_origin)
{
    PyObject *name = PyUnicode_FromString(name_cstr);
    PROBE_CHECK(name != NULL, "alloc ext name");
    PyObject *r_pkg = probe_call_kw1(loader, "is_package", "fullname", name);
    PROBE_CHECK(r_pkg == Py_False, "ExtensionFileLoader.is_package(fullname=...) != False");
    Py_XDECREF(r_pkg);
    PyObject *r_code = probe_call_kw1(loader, "get_code", "fullname", name);
    PROBE_CHECK(r_code == Py_None, "ExtensionFileLoader.get_code(fullname=...) != None");
    Py_XDECREF(r_code);
    PyObject *r_src = probe_call_kw1(loader, "get_source", "fullname", name);
    PROBE_CHECK(r_src == Py_None, "ExtensionFileLoader.get_source(fullname=...) != None");
    Py_XDECREF(r_src);
    PyObject *fn0 = probe_call_noargs(loader, "get_filename");
    PyObject *fn_kw = probe_call_kw1(loader, "get_filename", "name", name);
    PROBE_CHECK(fn0 != NULL && strcmp(PyUnicode_AsUTF8(fn0), expected_origin) == 0 &&
                    fn_kw != NULL && strcmp(PyUnicode_AsUTF8(fn_kw), expected_origin) == 0,
                "ExtensionFileLoader.get_filename() / (name=...) mismatch");
    Py_XDECREF(fn0);
    Py_XDECREF(fn_kw);
    PyObject *cm = probe_call_kw1(loader, "create_module", "spec", spec);
    PROBE_CHECK(cm != NULL && PyModule_Check(cm), "ExtensionFileLoader.create_module(spec=...)");
    PyObject *em = probe_call_kw1(loader, "exec_module", "module", cm);
    PROBE_CHECK(em == Py_None, "ExtensionFileLoader.exec_module(module=...) != None");
    Py_XDECREF(em);
    Py_DECREF(cm);

    PROBE_CHECK(probe_expect_kw_negatives(loader, "create_module", "spec", spec, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "exec_module", "module", mod, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "is_package", "fullname", name, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "get_code", "fullname", name, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "get_source", "fullname", name, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "get_filename", "name", name, 0) == 0,
                "ExtensionFileLoader kw negatives");
    Py_DECREF(name);
    return 0;
}

/* 校对 CilModuleLoader 本地 API 合同全方法关键字正/负例。 */
static inline int
probe_verify_cil_loader_kw(PyObject *loader, PyObject *spec, PyObject *mod, const char *name_cstr)
{
    PyObject *name = PyUnicode_FromString(name_cstr);
    PyObject *path_arg = PyUnicode_FromString("unused_path");
    PROBE_CHECK(name != NULL && path_arg != NULL, "alloc cil loader kw args");

    PyObject *r_pkg = probe_call_kw1(loader, "is_package", "fullname", name);
    PROBE_CHECK(r_pkg == Py_True, "CilModuleLoader.is_package(fullname=...) != True");
    Py_XDECREF(r_pkg);
    PyObject *r_code = probe_call_kw1(loader, "get_code", "fullname", name);
    PROBE_CHECK(r_code == Py_None, "CilModuleLoader.get_code(fullname=...) != None");
    Py_XDECREF(r_code);
    PyObject *r_src = probe_call_kw1(loader, "get_source", "fullname", name);
    PROBE_CHECK(r_src == Py_None, "CilModuleLoader.get_source(fullname=...) != None");
    Py_XDECREF(r_src);
    PyObject *r_cm = probe_call_kw1(loader, "create_module", "spec", spec);
    PROBE_CHECK(r_cm == Py_None, "CilModuleLoader.create_module(spec=...) != None");
    Py_XDECREF(r_cm);

    PROBE_CHECK(probe_call_kw1(loader, "exec_module", "module", mod) == NULL &&
                    PyErr_ExceptionMatches(PyExc_NotImplementedError),
                "CilModuleLoader.exec_module(module=...) must raise NotImplementedError");
    PyErr_Clear();
    PROBE_CHECK(probe_call_kw1(loader, "get_filename", "fullname", name) == NULL &&
                    PyErr_ExceptionMatches(PyExc_ImportError),
                "CilModuleLoader.get_filename(fullname=...) without location must raise ImportError");
    PyErr_Clear();
    PROBE_CHECK(probe_call_kw1(loader, "get_resource_reader", "fullname", name) == NULL &&
                    PyErr_ExceptionMatches(PyExc_NotImplementedError),
                "CilModuleLoader.get_resource_reader(fullname=...) must raise NotImplementedError");
    PyErr_Clear();
    PROBE_CHECK(probe_call_kw1(loader, "get_data", "path", path_arg) == NULL &&
                    PyErr_ExceptionMatches(PyExc_NotImplementedError),
                "CilModuleLoader.get_data(path=...) must raise NotImplementedError");
    PyErr_Clear();

    PROBE_CHECK(probe_expect_kw_negatives(loader, "create_module", "spec", spec, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "exec_module", "module", mod, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "is_package", "fullname", name, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "get_filename", "fullname", name, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "get_code", "fullname", name, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "get_source", "fullname", name, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "get_data", "path", path_arg, 1) == 0 &&
                    probe_expect_kw_negatives(loader, "get_resource_reader", "fullname", name, 1) == 0,
                "CilModuleLoader kw negatives");
    Py_DECREF(path_arg);
    Py_DECREF(name);
    return 0;
}

#endif /* RYUPY_PROBE_LOADER_KW_H */
