/* RyuPy 严格零字节码内核专用：纯 C UTF-8 CodecInfo 与注册表入口。
 *
 * 本头文件由锚点变换注入官方 CPython 3.12.13 的 Python/ryupy_utf8_codec.h，并在
 * Python/codecs.c 中被 #include，用于替代 _install_importlib=0（无 frozen encodings 包）
 * 下的 Python 编解码查找路径：
 * 1. 包含 Python/ryupy_utf8_incremental.h，复用其中的纯 C 增量编解码器与错误处理器提取。
 * 2. 提供无状态 rp_utf8_encode / rp_utf8_decode 入口，返回 (结果, 消费长度) 二元组。
 * 3. 通过 PyStructSequence_NewType 构造符合 4 元组 + 属性协议的 codecs.CodecInfo。
 * 4. 仅匹配 UTF-8 及其标准别名（utf_8/utf8/u8/utf/utf8_ucs2/utf8_ucs4/cp65001），
 *    其余编码返回 None 交由 _PyCodec_Lookup 明确抛出 LookupError，严禁静默回落。
 */
#ifndef Py_RYUPY_UTF8_CODEC_H
#define Py_RYUPY_UTF8_CODEC_H

#include "ryupy_utf8_incremental.h"

/* 无状态 UTF-8 编码入口：(input: str, errors: str|None = 'strict') -> (bytes, consumed) */
static PyObject *
rp_utf8_encode(PyObject *Py_UNUSED(self), PyObject *args, PyObject *kwds)
{
    static char *kwlist[] = {"input", "errors", NULL};
    PyObject *str_obj = NULL;
    PyObject *errors_obj = NULL;
    if (!PyArg_ParseTupleAndKeywords(args, kwds, "O|O:encode", kwlist,
                                     &str_obj, &errors_obj)) {
        return NULL;
    }
    if (!PyUnicode_Check(str_obj)) {
        PyErr_Format(PyExc_TypeError,
                     "utf_8_encode() argument 1 must be str, not %.200s",
                     Py_TYPE(str_obj)->tp_name);
        return NULL;
    }
    const char *errors = NULL;
    if (rp_utf8_extract_errors(errors_obj, &errors) < 0) {
        return NULL;
    }
    PyObject *encoded = _PyUnicode_AsUTF8String(str_obj, errors);
    if (encoded == NULL) {
        return NULL;
    }
    return Py_BuildValue("Nn", encoded, PyUnicode_GET_LENGTH(str_obj));
}

/* 无状态 UTF-8 解码入口：(input: bytes-like, errors: str|None = 'strict') -> (str, consumed) */
static PyObject *
rp_utf8_decode(PyObject *Py_UNUSED(self), PyObject *args, PyObject *kwds)
{
    static char *kwlist[] = {"input", "errors", NULL};
    Py_buffer view;
    PyObject *errors_obj = NULL;
    if (!PyArg_ParseTupleAndKeywords(args, kwds, "y*|O:decode", kwlist,
                                     &view, &errors_obj)) {
        return NULL;
    }
    const char *errors = NULL;
    if (rp_utf8_extract_errors(errors_obj, &errors) < 0) {
        PyBuffer_Release(&view);
        return NULL;
    }
    Py_ssize_t input_len = view.len;
    PyObject *decoded = PyUnicode_DecodeUTF8Stateful(
        (const char *)view.buf, input_len, errors, NULL);
    PyBuffer_Release(&view);
    if (decoded == NULL) {
        return NULL;
    }
    return Py_BuildValue("Nn", decoded, input_len);
}

static PyMethodDef rp_utf8_encode_def = {
    "encode",
    (PyCFunction)(void (*)(void))rp_utf8_encode,
    METH_VARARGS | METH_KEYWORDS,
    PyDoc_STR("Encode str to UTF-8 bytes and return (bytes, chars_consumed).")
};

static PyMethodDef rp_utf8_decode_def = {
    "decode",
    (PyCFunction)(void (*)(void))rp_utf8_decode,
    METH_VARARGS | METH_KEYWORDS,
    PyDoc_STR("Decode bytes-like UTF-8 input and return (str, bytes_consumed).")
};

/* --- CodecInfo 描述与搜索函数注册 -------------------------------------- */

static PyStructSequence_Field rp_utf8_codecinfo_fields[] = {
    {"encode", NULL},
    {"decode", NULL},
    {"streamreader", NULL},
    {"streamwriter", NULL},
    {"incrementalencoder", NULL},
    {"incrementaldecoder", NULL},
    {"name", NULL},
    {"_is_text_encoding", NULL},
    {NULL}
};

static PyStructSequence_Desc rp_utf8_codecinfo_desc = {
    .name = "codecs.CodecInfo",
    .doc = "Pure-C UTF-8 CodecInfo for RyuPy strict no-bytecode kernel.",
    .fields = rp_utf8_codecinfo_fields,
    .n_in_sequence = 4,
};

static int
rp_is_utf8_normalized_name(const char *norm)
{
    return strcmp(norm, "utf_8") == 0
        || strcmp(norm, "utf8") == 0
        || strcmp(norm, "u8") == 0
        || strcmp(norm, "utf") == 0
        || strcmp(norm, "utf8_ucs2") == 0
        || strcmp(norm, "utf8_ucs4") == 0
        || strcmp(norm, "cp65001") == 0;
}

static PyObject *
rp_utf8_search_function(PyObject *codec_info, PyObject *encoding_obj)
{
    if (!PyUnicode_Check(encoding_obj)) {
        Py_RETURN_NONE;
    }
    const char *enc = PyUnicode_AsUTF8(encoding_obj);
    if (enc == NULL) {
        return NULL;
    }
    char norm[32];
    if (!_Py_normalize_encoding(enc, norm, sizeof(norm))) {
        Py_RETURN_NONE;
    }
    if (rp_is_utf8_normalized_name(norm)) {
        return Py_NewRef(codec_info);
    }
    Py_RETURN_NONE;
}

static PyMethodDef rp_utf8_search_def = {
    "rp_utf8_search_function",
    rp_utf8_search_function,
    METH_O,
    PyDoc_STR("Pure-C UTF-8 codec search function for RyuPy kernel.")
};

/* 向解释器 codec_search_path 注册纯 C UTF-8 CodecInfo；全部对象由解释器生命周期托管。 */
static int
rp_kernel_register_utf8_codec(PyInterpreterState *interp)
{
    PyTypeObject *incenc_type = NULL;
    PyTypeObject *incdec_type = NULL;
    PyTypeObject *ci_type = NULL;
    PyObject *encode_fn = NULL;
    PyObject *decode_fn = NULL;
    PyObject *utf8_name = NULL;
    PyObject *ci = NULL;
    PyObject *search_fn = NULL;

    incenc_type = (PyTypeObject *)PyType_FromSpec(&rp_utf8_incenc_spec);
    incdec_type = (PyTypeObject *)PyType_FromSpec(&rp_utf8_incdec_spec);
    ci_type = PyStructSequence_NewType(&rp_utf8_codecinfo_desc);
    encode_fn = PyCFunction_NewEx(&rp_utf8_encode_def, NULL, NULL);
    decode_fn = PyCFunction_NewEx(&rp_utf8_decode_def, NULL, NULL);
    utf8_name = PyUnicode_FromString("utf-8");

    if (incenc_type == NULL || incdec_type == NULL || ci_type == NULL ||
        encode_fn == NULL || decode_fn == NULL || utf8_name == NULL) {
        goto error;
    }

    ci = PyStructSequence_New(ci_type);
    if (ci == NULL) {
        goto error;
    }
    PyStructSequence_SET_ITEM(ci, 0, encode_fn);
    PyStructSequence_SET_ITEM(ci, 1, decode_fn);
    PyStructSequence_SET_ITEM(ci, 2, Py_NewRef(Py_None));
    PyStructSequence_SET_ITEM(ci, 3, Py_NewRef(Py_None));
    PyStructSequence_SET_ITEM(ci, 4, (PyObject *)incenc_type);
    PyStructSequence_SET_ITEM(ci, 5, (PyObject *)incdec_type);
    PyStructSequence_SET_ITEM(ci, 6, utf8_name);
    PyStructSequence_SET_ITEM(ci, 7, Py_NewRef(Py_True));
    /* 所有字段已就绪后纳入 GC 跟踪，匹配 structseq_new_impl 约定。 */
    PyObject_GC_Track(ci);
    encode_fn = decode_fn = utf8_name = NULL;
    incenc_type = incdec_type = NULL;

    search_fn = PyCFunction_NewEx(&rp_utf8_search_def, ci, NULL);
    Py_CLEAR(ci);
    if (search_fn == NULL) {
        goto error;
    }
    int rc = PyList_Append(interp->codec_search_path, search_fn);
    Py_DECREF(search_fn);
    Py_DECREF(ci_type);
    return rc;

error:
    Py_XDECREF(ci);
    Py_XDECREF(utf8_name);
    Py_XDECREF(decode_fn);
    Py_XDECREF(encode_fn);
    Py_XDECREF(ci_type);
    Py_XDECREF(incdec_type);
    Py_XDECREF(incenc_type);
    return -1;
}

#endif /* Py_RYUPY_UTF8_CODEC_H */
