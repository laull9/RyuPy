/* 中性Unicode码点桥：每码点两个uint16单元，保留孤立/连续代理码点，不执行Python协议。 */
API PyObject *rp_text_codepoints(const uint16_t *units, int64_t count) {
    if (count < 0 || (count & 1) || count / 2 > PY_SSIZE_T_MAX) {
        PyErr_SetString(PyExc_ValueError, "invalid Unicode codepoint units"); return NULL;
    }
    Py_UCS4 maximum = 0;
    for (int64_t i = 0; i < count; i += 2) {
        Py_UCS4 point = ((Py_UCS4)units[i] << 16) | units[i+1];
        if (point > 0x10ffff) { PyErr_SetString(PyExc_ValueError, "invalid Unicode codepoint"); return NULL; }
        if (point > maximum) maximum = point;
    }
    PyObject *result = PyUnicode_New((Py_ssize_t)(count / 2), maximum);
    if (!result) return NULL;
    for (int64_t i = 0; i < count; i += 2) {
        Py_UCS4 point = ((Py_UCS4)units[i] << 16) | units[i+1];
        if (PyUnicode_WriteChar(result, (Py_ssize_t)(i / 2), point) < 0) { Py_DECREF(result); return NULL; }
    }
    return result;
}
/* UTF8缓存无法表达代理码点时由调用方无损读取；其它异常保持原状态传播。 */
API int rp_unicode_utf8_fallback(void) {
    if (!PyErr_ExceptionMatches(PyExc_UnicodeEncodeError)) return 0;
    PyErr_Clear(); return 1;
}
API int64_t rp_unicode_codepoints(PyObject *value, uint16_t *output, int64_t capacity) {
    PyObject *exact = PyUnicode_FromObject(value);
    if (!exact) return -1;
    int64_t result = rp_unicode_units(exact, output, capacity, 1);
    Py_DECREF(exact); return result;
}
