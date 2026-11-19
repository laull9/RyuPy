/* RyuPy 严格零字节码内核 C4-1 Loader 公共参数校验、关键字解析与拒绝契约头。
 *
 * 本头文件由锚点机制注入官方 CPython 3.12.13 的 `Python/ryupy_import_loader_common.h`，
 * 被 `Python/ryupy_import_loaders.h`、`ryupy_import_ext_loader.h` 与 `ryupy_import_cil_loader.h`
 * 共享引用，避免在各 Loader 之间重复实现名称检查、关键字解析与未承诺 API 拒绝逻辑：
 * 1. `rp_loader_parse_kw`：统一通过 `PyArg_ParseTupleAndKeywords` 解析单参数调用，完整支持
 *    位置/关键字传参，并在缺失参数、重复传参或未知关键字时由 CPython 原生抛出 `TypeError`。
 * 2. `rp_loader_check_name`：校对请求模块名与 Loader 绑定名称是否一致，失配抛 `ImportError`。
 * 3. `rp_loader_reject_data` / `rp_loader_reject_resource_reader`：在先完成签名参数检验的前提下，
 *    对未承诺的 `get_data` / `get_resource_reader` 显式抛出 `NotImplementedError`。
 */
#ifndef Py_RYUPY_IMPORT_LOADER_COMMON_H
#define Py_RYUPY_IMPORT_LOADER_COMMON_H

/* 共享单关键字数组，避免各 Loader 重复定义并消除 const/discard 告警。 */
static char *rp_kw_spec[] = {"spec", NULL};
static char *rp_kw_module[] = {"module", NULL};
static char *rp_kw_fullname[] = {"fullname", NULL};
static char *rp_kw_name[] = {"name", NULL};
static char *rp_kw_path[] = {"path", NULL};

/* 解析单个位置或关键字参数；支持 "O:fn"（必选）与 "|O:fn"（可选）。
 * 失败（缺失必选参数、重复关键字、多余位置参数或未知关键字）返回 NULL 并设置 TypeError。 */
static PyObject *
rp_loader_parse_kw(PyObject *args, PyObject *kwds, const char *fmt, char *const kwlist[],
                   int optional, int *has_value)
{
    PyObject *value = NULL;
    if (!PyArg_ParseTupleAndKeywords(args, kwds, fmt, (char **)kwlist, &value)) {
        return NULL;
    }
    if (has_value != NULL) {
        *has_value = (value != NULL);
    }
    if (!optional && value == NULL) {
        PyErr_SetString(PyExc_TypeError, "missing required loader argument");
        return NULL;
    }
    return value != NULL ? value : Py_None;
}

/* 校验请求的 fullname 与当前 Loader 绑定模块名是否一致。 */
static int
rp_loader_check_name(PyObject *expected_name, PyObject *fullname)
{
    if (!PyUnicode_Check(fullname)) {
        PyErr_Format(PyExc_TypeError, "fullname must be str, not %.200s",
                     Py_TYPE(fullname)->tp_name);
        return -1;
    }
    if (expected_name == NULL) {
        PyErr_SetString(PyExc_ImportError, "loader has been cleared");
        return -1;
    }
    int eq = PyUnicode_Compare(expected_name, fullname);
    if (eq == 0) {
        return 0;
    }
    if (PyErr_Occurred()) {
        return -1;
    }
    PyObject *msg = PyUnicode_FromFormat("loader for %R cannot handle %R",
                                         expected_name, fullname);
    if (msg != NULL) {
        (void)PyErr_SetImportError(msg, fullname, NULL);
        Py_DECREF(msg);
    }
    return -1;
}

/* 对显式带 `path` 参数的 `get_data(path)` 校验签名后拒绝。 */
static PyObject *
rp_loader_reject_data(PyObject *Py_UNUSED(self), PyObject *args, PyObject *kwds)
{
    if (rp_loader_parse_kw(args, kwds, "O:get_data", rp_kw_path, 0, NULL) == NULL) {
        return NULL;
    }
    PyErr_SetString(PyExc_NotImplementedError,
                    "loader.get_data() is not supported in RyuPy minimal kernel");
    return NULL;
}

/* 对 `get_resource_reader` 按目标契约（Extension 可选 `name=None` / CIL 必选 `fullname`）校验参数后拒绝。 */
static PyObject *
rp_loader_reject_resource_reader_impl(PyObject *args, PyObject *kwds,
                                      const char *fmt, char *const kwlist[], int optional)
{
    if (rp_loader_parse_kw(args, kwds, fmt, kwlist, optional, NULL) == NULL) {
        return NULL;
    }
    PyErr_SetString(PyExc_NotImplementedError,
                    "loader.get_resource_reader() requires importlib.readers (deferred beyond C4-1)");
    return NULL;
}

#endif /* Py_RYUPY_IMPORT_LOADER_COMMON_H */
