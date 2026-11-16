/* RyuPy 严格零字节码内核专用：纯 C UTF-8 增量编解码器实现。
 *
 * 本头文件由锚点变换注入官方 CPython 3.12.13 的 Python/ryupy_utf8_incremental.h，
 * 被 Python/ryupy_utf8_codec.h 包含，实现完整遵循 Python codecs 协议的纯 C 堆类型：
 * 1. rp_utf8_extract_errors：将 Python errors 属性（str / None）校验并转换为 C 字符串，
 *    复用 Python/codecs.c 已注册的 8 种内建 C 错误处理器及自定义 PyCodec_RegisterError。
 * 2. RpUtf8IncEncoder（encodings.utf_8.IncrementalEncoder）：
 *    支持构造/读写 errors 属性、encode(input, final=False)、reset()、getstate()、setstate(state)。
 * 3. RpUtf8IncDecoder（encodings.utf_8.IncrementalDecoder）：
 *    内部维护未完成多字节 UTF-8 尾部的 bytes 缓冲，通过 PyUnicode_DecodeUTF8Stateful
 *    支持跨 chunk 任意字节切分（3 字节汉字、4 字节 emoji 等）、getstate() -> (bytes, 0)、
 *    setstate((bytes, int))、reset() 以及 final=True 时按 errors 协议收敛末尾截断序列。
 */
#ifndef Py_RYUPY_UTF8_INCREMENTAL_H
#define Py_RYUPY_UTF8_INCREMENTAL_H

#include <stddef.h>
#include <string.h>

static int
rp_utf8_extract_errors(PyObject *errors_obj, const char **out_errors)
{
    if (errors_obj == NULL || errors_obj == Py_None) {
        *out_errors = NULL;
        return 0;
    }
    if (!PyUnicode_Check(errors_obj)) {
        PyErr_Format(PyExc_TypeError,
                     "errors must be str or None, not %.200s",
                     Py_TYPE(errors_obj)->tp_name);
        return -1;
    }
    Py_ssize_t len = 0;
    const char *s = PyUnicode_AsUTF8AndSize(errors_obj, &len);
    if (s == NULL) {
        return -1;
    }
    if (strlen(s) != (size_t)len) {
        PyErr_SetString(PyExc_ValueError, "embedded null character");
        return -1;
    }
    *out_errors = s;
    return 0;
}

/* --- 纯 C UTF-8 IncrementalEncoder -------------------------------------- */

typedef struct {
    PyObject_HEAD
    PyObject *errors;
} RpUtf8IncEncoder;

static PyObject *
rp_utf8_incenc_new(PyTypeObject *type, PyObject *Py_UNUSED(args), PyObject *Py_UNUSED(kwds))
{
    RpUtf8IncEncoder *self = (RpUtf8IncEncoder *)type->tp_alloc(type, 0);
    if (self == NULL) {
        return NULL;
    }
    self->errors = PyUnicode_FromString("strict");
    if (self->errors == NULL) {
        Py_DECREF(self);
        return NULL;
    }
    return (PyObject *)self;
}

static int
rp_utf8_incenc_init(RpUtf8IncEncoder *self, PyObject *args, PyObject *kwds)
{
    static char *kwlist[] = {"errors", NULL};
    PyObject *errors = NULL;
    if (!PyArg_ParseTupleAndKeywords(args, kwds, "|O:__init__", kwlist, &errors)) {
        return -1;
    }
    PyObject *new_err = (errors != NULL) ? Py_NewRef(errors) : PyUnicode_FromString("strict");
    if (new_err == NULL) {
        return -1;
    }
    Py_SETREF(self->errors, new_err);
    return 0;
}

static int
rp_utf8_incenc_traverse(RpUtf8IncEncoder *self, visitproc visit, void *arg)
{
    Py_VISIT(Py_TYPE(self));
    Py_VISIT(self->errors);
    return 0;
}

static int
rp_utf8_incenc_clear(RpUtf8IncEncoder *self)
{
    Py_CLEAR(self->errors);
    return 0;
}

static void
rp_utf8_incenc_dealloc(RpUtf8IncEncoder *self)
{
    PyTypeObject *tp = Py_TYPE(self);
    PyObject_GC_UnTrack(self);
    Py_CLEAR(self->errors);
    tp->tp_free((PyObject *)self);
    Py_DECREF(tp);
}

static PyObject *
rp_utf8_incenc_get_errors(RpUtf8IncEncoder *self, void *Py_UNUSED(ctx))
{
    return Py_NewRef(self->errors != NULL ? self->errors : Py_None);
}

static int
rp_utf8_incenc_set_errors(RpUtf8IncEncoder *self, PyObject *val, void *Py_UNUSED(ctx))
{
    if (val == NULL) {
        PyErr_SetString(PyExc_AttributeError, "cannot delete 'errors' attribute");
        return -1;
    }
    Py_XSETREF(self->errors, Py_NewRef(val));
    return 0;
}

static PyObject *
rp_utf8_incenc_encode(RpUtf8IncEncoder *self, PyObject *args, PyObject *kwds)
{
    static char *kwlist[] = {"input", "final", NULL};
    PyObject *input = NULL;
    PyObject *final = NULL;
    if (!PyArg_ParseTupleAndKeywords(args, kwds, "O|O:encode", kwlist, &input, &final)) {
        return NULL;
    }
    (void)final;  /* UTF-8 encoder 无状态；Python 原实现也不读取 final 的真值。 */
    if (!PyUnicode_Check(input)) {
        PyErr_Format(PyExc_TypeError,
                     "utf_8_encode() argument 1 must be str, not %.200s",
                     Py_TYPE(input)->tp_name);
        return NULL;
    }
    const char *errors = NULL;
    if (rp_utf8_extract_errors(self->errors, &errors) < 0) {
        return NULL;
    }
    return _PyUnicode_AsUTF8String(input, errors);
}

static PyObject *
rp_utf8_incenc_reset(RpUtf8IncEncoder *Py_UNUSED(self), PyObject *Py_UNUSED(ignored))
{
    Py_RETURN_NONE;
}

static PyObject *
rp_utf8_incenc_getstate(RpUtf8IncEncoder *Py_UNUSED(self), PyObject *Py_UNUSED(ignored))
{
    return PyLong_FromLong(0);
}

static PyObject *
rp_utf8_incenc_setstate(RpUtf8IncEncoder *Py_UNUSED(self), PyObject *Py_UNUSED(state))
{
    Py_RETURN_NONE;
}

static PyMethodDef rp_utf8_incenc_methods[] = {
    {"encode", (PyCFunction)(void (*)(void))rp_utf8_incenc_encode,
     METH_VARARGS | METH_KEYWORDS, NULL},
    {"reset", (PyCFunction)rp_utf8_incenc_reset, METH_NOARGS, NULL},
    {"getstate", (PyCFunction)rp_utf8_incenc_getstate, METH_NOARGS, NULL},
    {"setstate", (PyCFunction)rp_utf8_incenc_setstate, METH_O, NULL},
    {NULL, NULL, 0, NULL}
};

static PyGetSetDef rp_utf8_incenc_getset[] = {
    {"errors", (getter)rp_utf8_incenc_get_errors, (setter)rp_utf8_incenc_set_errors, NULL, NULL},
    {NULL, NULL, NULL, NULL, NULL}
};

static PyType_Slot rp_utf8_incenc_slots[] = {
    {Py_tp_new, rp_utf8_incenc_new},
    {Py_tp_init, rp_utf8_incenc_init},
    {Py_tp_traverse, rp_utf8_incenc_traverse},
    {Py_tp_clear, rp_utf8_incenc_clear},
    {Py_tp_dealloc, rp_utf8_incenc_dealloc},
    {Py_tp_methods, rp_utf8_incenc_methods},
    {Py_tp_getset, rp_utf8_incenc_getset},
    {0, NULL}
};

static PyType_Spec rp_utf8_incenc_spec = {
    .name = "encodings.utf_8.IncrementalEncoder",
    .basicsize = sizeof(RpUtf8IncEncoder),
    .flags = Py_TPFLAGS_DEFAULT | Py_TPFLAGS_BASETYPE | Py_TPFLAGS_HAVE_GC,
    .slots = rp_utf8_incenc_slots,
};

/* --- 纯 C UTF-8 IncrementalDecoder（支持跨 chunk 不完整序列暂存） --------- */

typedef struct {
    PyObject_HEAD
    PyObject *errors;
    PyObject *buffer;
} RpUtf8IncDecoder;

static PyObject *
rp_utf8_incdec_new(PyTypeObject *type, PyObject *Py_UNUSED(args), PyObject *Py_UNUSED(kwds))
{
    RpUtf8IncDecoder *self = (RpUtf8IncDecoder *)type->tp_alloc(type, 0);
    if (self == NULL) {
        return NULL;
    }
    self->errors = PyUnicode_FromString("strict");
    self->buffer = PyBytes_FromStringAndSize("", 0);
    if (self->errors == NULL || self->buffer == NULL) {
        Py_DECREF(self);
        return NULL;
    }
    return (PyObject *)self;
}

static int
rp_utf8_incdec_init(RpUtf8IncDecoder *self, PyObject *args, PyObject *kwds)
{
    static char *kwlist[] = {"errors", NULL};
    PyObject *errors = NULL;
    if (!PyArg_ParseTupleAndKeywords(args, kwds, "|O:__init__", kwlist, &errors)) {
        return -1;
    }
    PyObject *new_err = (errors != NULL) ? Py_NewRef(errors) : PyUnicode_FromString("strict");
    PyObject *empty_buf = PyBytes_FromStringAndSize("", 0);
    if (new_err == NULL || empty_buf == NULL) {
        Py_XDECREF(new_err);
        Py_XDECREF(empty_buf);
        return -1;
    }
    Py_SETREF(self->errors, new_err);
    Py_SETREF(self->buffer, empty_buf);
    return 0;
}

static int
rp_utf8_incdec_traverse(RpUtf8IncDecoder *self, visitproc visit, void *arg)
{
    Py_VISIT(Py_TYPE(self));
    Py_VISIT(self->errors);
    Py_VISIT(self->buffer);
    return 0;
}

static int
rp_utf8_incdec_clear(RpUtf8IncDecoder *self)
{
    Py_CLEAR(self->errors);
    Py_CLEAR(self->buffer);
    return 0;
}

static void
rp_utf8_incdec_dealloc(RpUtf8IncDecoder *self)
{
    PyTypeObject *tp = Py_TYPE(self);
    PyObject_GC_UnTrack(self);
    Py_CLEAR(self->errors);
    Py_CLEAR(self->buffer);
    tp->tp_free((PyObject *)self);
    Py_DECREF(tp);
}

static PyObject *
rp_utf8_incdec_get_errors(RpUtf8IncDecoder *self, void *Py_UNUSED(ctx))
{
    return Py_NewRef(self->errors != NULL ? self->errors : Py_None);
}

static int
rp_utf8_incdec_set_errors(RpUtf8IncDecoder *self, PyObject *val, void *Py_UNUSED(ctx))
{
    if (val == NULL) {
        PyErr_SetString(PyExc_AttributeError, "cannot delete 'errors' attribute");
        return -1;
    }
    Py_XSETREF(self->errors, Py_NewRef(val));
    return 0;
}

static PyObject *
rp_utf8_incdec_decode(RpUtf8IncDecoder *self, PyObject *args, PyObject *kwds)
{
    static char *kwlist[] = {"input", "final", NULL};
    Py_buffer view;
    int final = 0;
    if (!PyArg_ParseTupleAndKeywords(args, kwds, "y*|p:decode", kwlist, &view, &final)) {
        return NULL;
    }
    const char *errors = NULL;
    if (rp_utf8_extract_errors(self->errors, &errors) < 0) {
        PyBuffer_Release(&view);
        return NULL;
    }

    Py_ssize_t prev_len = self->buffer ? PyBytes_GET_SIZE(self->buffer) : 0;
    PyObject *joined = NULL;
    const char *data_ptr = (const char *)view.buf;
    Py_ssize_t data_len = view.len;

    if (prev_len > 0) {
        if (view.len > PY_SSIZE_T_MAX - prev_len) {
            PyBuffer_Release(&view);
            return PyErr_NoMemory();
        }
        data_len = prev_len + view.len;
        joined = PyBytes_FromStringAndSize(NULL, data_len);
        if (joined == NULL) {
            PyBuffer_Release(&view);
            return NULL;
        }
        char *dst = PyBytes_AS_STRING(joined);
        memcpy(dst, PyBytes_AS_STRING(self->buffer), (size_t)prev_len);
        if (view.len > 0) {
            memcpy(dst + prev_len, view.buf, (size_t)view.len);
        }
        data_ptr = dst;
    }

    Py_ssize_t consumed = data_len;
    PyObject *decoded = PyUnicode_DecodeUTF8Stateful(
        data_ptr, data_len, errors, final ? NULL : &consumed);
    if (decoded == NULL) {
        /* 保持 self->buffer 不变，对齐 BufferedIncrementalDecoder 异常安全语义 */
        Py_XDECREF(joined);
        PyBuffer_Release(&view);
        return NULL;
    }

    Py_ssize_t remaining = final ? 0 : (data_len - consumed);
    if (remaining > 0) {
        PyObject *new_buf = PyBytes_FromStringAndSize(data_ptr + consumed, remaining);
        if (new_buf == NULL) {
            Py_DECREF(decoded);
            Py_XDECREF(joined);
            PyBuffer_Release(&view);
            return NULL;
        }
        Py_SETREF(self->buffer, new_buf);
    }
    else if (prev_len > 0) {
        PyObject *empty_buf = PyBytes_FromStringAndSize("", 0);
        if (empty_buf == NULL) {
            Py_DECREF(decoded);
            Py_XDECREF(joined);
            PyBuffer_Release(&view);
            return NULL;
        }
        Py_SETREF(self->buffer, empty_buf);
    }

    Py_XDECREF(joined);
    PyBuffer_Release(&view);
    return decoded;
}

static PyObject *
rp_utf8_incdec_reset(RpUtf8IncDecoder *self, PyObject *Py_UNUSED(ignored))
{
    PyObject *empty_buf = PyBytes_FromStringAndSize("", 0);
    if (empty_buf == NULL) {
        return NULL;
    }
    Py_SETREF(self->buffer, empty_buf);
    Py_RETURN_NONE;
}

static PyObject *
rp_utf8_incdec_getstate(RpUtf8IncDecoder *self, PyObject *Py_UNUSED(ignored))
{
    PyObject *buf = self->buffer ? self->buffer : Py_None;
    return Py_BuildValue("(Oi)", buf, 0);
}

static PyObject *
rp_utf8_incdec_setstate(RpUtf8IncDecoder *self, PyObject *state)
{
    if (!PyTuple_Check(state) || PyTuple_GET_SIZE(state) != 2) {
        PyErr_SetString(PyExc_TypeError, "state must be a 2-tuple (buffered_input, info)");
        return NULL;
    }
    Py_buffer view;
    if (PyObject_GetBuffer(PyTuple_GET_ITEM(state, 0), &view, PyBUF_SIMPLE) < 0) {
        return NULL;
    }
    PyObject *new_buf = PyBytes_FromStringAndSize((const char *)view.buf, view.len);
    PyBuffer_Release(&view);
    if (new_buf == NULL) {
        return NULL;
    }
    Py_SETREF(self->buffer, new_buf);
    Py_RETURN_NONE;
}

static PyMethodDef rp_utf8_incdec_methods[] = {
    {"decode", (PyCFunction)(void (*)(void))rp_utf8_incdec_decode,
     METH_VARARGS | METH_KEYWORDS, NULL},
    {"reset", (PyCFunction)rp_utf8_incdec_reset, METH_NOARGS, NULL},
    {"getstate", (PyCFunction)rp_utf8_incdec_getstate, METH_NOARGS, NULL},
    {"setstate", (PyCFunction)rp_utf8_incdec_setstate, METH_O, NULL},
    {NULL, NULL, 0, NULL}
};

static PyGetSetDef rp_utf8_incdec_getset[] = {
    {"errors", (getter)rp_utf8_incdec_get_errors, (setter)rp_utf8_incdec_set_errors, NULL, NULL},
    {NULL, NULL, NULL, NULL, NULL}
};

static PyType_Slot rp_utf8_incdec_slots[] = {
    {Py_tp_new, rp_utf8_incdec_new},
    {Py_tp_init, rp_utf8_incdec_init},
    {Py_tp_traverse, rp_utf8_incdec_traverse},
    {Py_tp_clear, rp_utf8_incdec_clear},
    {Py_tp_dealloc, rp_utf8_incdec_dealloc},
    {Py_tp_methods, rp_utf8_incdec_methods},
    {Py_tp_getset, rp_utf8_incdec_getset},
    {0, NULL}
};

static PyType_Spec rp_utf8_incdec_spec = {
    .name = "encodings.utf_8.IncrementalDecoder",
    .basicsize = sizeof(RpUtf8IncDecoder),
    .flags = Py_TPFLAGS_DEFAULT | Py_TPFLAGS_BASETYPE | Py_TPFLAGS_HAVE_GC,
    .slots = rp_utf8_incdec_slots,
};

#endif /* Py_RYUPY_UTF8_INCREMENTAL_H */
