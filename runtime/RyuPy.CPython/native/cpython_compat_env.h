/* RyuPy CPython 3.9–3.15 公共环境与托管代理类型声明。
 * 统一封装跨版本宏兼容、PyConfig 错码缓冲、环境变量写入与 Managed 对象转发槽，
 * 由 compat 与 strict shim 共同包含，不含任何仅限严格内核的导出符号。 */
#ifndef RYUPY_CPYTHON_COMPAT_ENV_H
#define RYUPY_CPYTHON_COMPAT_ENV_H

#define PY_SSIZE_T_CLEAN
#include <Python.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#if defined(_WIN32)
#include <windows.h>
#define API __declspec(dllexport)
#else
#define API __attribute__((visibility("default")))
#endif

#if PY_VERSION_HEX < 0x03090000 || PY_VERSION_HEX >= 0x03100000
#error "RyuPy CPython shim requires CPython 3.9 or higher"
#endif

#if defined(Py_DEBUG)
#error "RyuPy requires a release CPython build"
#endif

#if defined(Py_GIL_DISABLED) && Py_GIL_DISABLED
#error "RyuPy currently requires standard GIL build of CPython; free-threaded CPython is not supported yet"
#endif

#if PY_VERSION_HEX < 0x030a0000
static inline PyObject *rp_compat_newref(PyObject *obj) {
    Py_XINCREF(obj);
    return obj;
}
#define Py_NewRef(obj) rp_compat_newref(obj)
#define Py_XNewRef(obj) rp_compat_newref(obj)
#define Py_IsNone(x) ((x) == Py_None)
#endif

#ifndef PySet_CheckExact
#define PySet_CheckExact(ob) Py_IS_TYPE(ob, &PySet_Type)
#endif

#ifndef Py_TPFLAGS_IMMUTABLETYPE
#define Py_TPFLAGS_IMMUTABLETYPE 0
#endif

#if PY_VERSION_HEX < 0x030c0000
static inline PyObject *rp_compat_exception_args(PyObject *ex) {
    PyObject *args = ((PyBaseExceptionObject *)ex)->args;
    return args ? Py_NewRef(args) : PyTuple_New(0);
}
#define PyException_GetArgs rp_compat_exception_args

static inline PyObject *rp_compat_get_raised_exception(void) {
    PyObject *type = NULL, *value = NULL, *tb = NULL;
    PyErr_Fetch(&type, &value, &tb);
    if (!type) return NULL;
    PyErr_NormalizeException(&type, &value, &tb);
    if (tb) {
        PyException_SetTraceback(value, tb);
        Py_DECREF(tb);
    }
    Py_XDECREF(type);
    return value;
}

static inline void rp_compat_set_raised_exception(PyObject *exc) {
    if (!exc) { PyErr_Clear(); return; }
    PyObject *tb = PyException_GetTraceback(exc);
    /* PyErr_SetRaisedException 接管传入的新引用，Restore 同样接管。 */
    Py_INCREF((PyObject *)Py_TYPE(exc));
    PyErr_Restore((PyObject *)Py_TYPE(exc), exc, tb);
}
#define PyErr_GetRaisedException rp_compat_get_raised_exception
#define PyErr_SetRaisedException rp_compat_set_raised_exception
#endif

#if PY_VERSION_HEX < 0x030b0000
/* 公开 ExcInfo API 保留线程异常栈语义，避免依赖私有结构布局。 */
static inline PyObject *rp_compat_get_handled_exception(void) {
    PyObject *type = NULL, *value = NULL, *tb = NULL;
    PyErr_GetExcInfo(&type, &value, &tb);
    Py_XDECREF(type); Py_XDECREF(tb);
    if (value == Py_None) { Py_DECREF(value); return NULL; }
    return value;
}
static inline void rp_compat_set_handled_exception(PyObject *exc) {
    PyObject *type = exc ? Py_NewRef((PyObject *)Py_TYPE(exc)) : NULL;
    PyObject *tb = exc ? PyException_GetTraceback(exc) : NULL;
    PyErr_SetExcInfo(type, Py_XNewRef(exc), tb);
}
#define PyErr_GetHandledException rp_compat_get_handled_exception
#define PyErr_SetHandledException rp_compat_set_handled_exception
#endif

/* 在解释器/Tcl 初始化前写入或清除原生进程环境变量，保证隔离生效。 */
API int rp_set_env(const char *name, const char *value) {
    if (!name || !*name) return -1;
#if defined(_WIN32)
    int name_len = MultiByteToWideChar(CP_UTF8, 0, name, -1, NULL, 0);
    if (name_len <= 0) return -1;
    wchar_t *wname = (wchar_t *)malloc((size_t)name_len * sizeof(wchar_t));
    if (!wname) return -1;
    MultiByteToWideChar(CP_UTF8, 0, name, -1, wname, name_len);
    int rc = -1;
    if (!value) {
        rc = _wputenv_s(wname, L"") == 0 ? 0 : -1;
    } else {
        int val_len = MultiByteToWideChar(CP_UTF8, 0, value, -1, NULL, 0);
        if (val_len > 0) {
            wchar_t *wval = (wchar_t *)malloc((size_t)val_len * sizeof(wchar_t));
            if (wval) {
                MultiByteToWideChar(CP_UTF8, 0, value, -1, wval, val_len);
                rc = _wputenv_s(wname, wval) == 0 ? 0 : -1;
                free(wval);
            }
        }
    }
    free(wname);
    return rc;
#else
    if (!value) return unsetenv(name);
    return setenv(name, value, 1);
#endif
}

typedef PyObject *(*managed_call)(intptr_t, int, PyObject *, PyObject *, PyObject *, int64_t, const char *);
typedef struct { PyObject_HEAD intptr_t identity; } Managed;
static managed_call dispatch;
static PyTypeObject *managed_type;
static PyThreadState *initial_thread;
static char init_error[4096];

static int fail_status(PyStatus status) {
    if (!PyStatus_Exception(status)) return 0;
    snprintf(init_error, sizeof(init_error), "%s: %s", status.func ? status.func : "initialization", status.err_msg ? status.err_msg : "requested exit");
    return -1;
}

#endif /* RYUPY_CPYTHON_COMPAT_ENV_H */
