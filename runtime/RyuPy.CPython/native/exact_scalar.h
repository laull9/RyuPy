/* 精确Unicode布局只读：UTF-16直接保留孤立代理项；不经UTF-8编码，也不执行用户转换钩子。 */
API int64_t rp_unicode_units(PyObject *value, uint16_t *output, int64_t capacity, int codepoints) {
    if (!PyUnicode_CheckExact(value)) { PyErr_SetString(PyExc_TypeError, "exact Unicode required"); return -1; }
    Py_ssize_t length = PyUnicode_GetLength(value);
    if (length < 0) return -1;
    int64_t units = 0;
    for (Py_ssize_t index = 0; index < length; index++) {
        Py_UCS4 scalar = PyUnicode_ReadChar(value, index);
        if (scalar == (Py_UCS4)-1 && PyErr_Occurred()) return -1;
        units += codepoints || scalar > 0xffff ? 2 : 1;
    }
    if (!output) return units;
    if (capacity < units) { PyErr_SetString(PyExc_ValueError, "Unicode output capacity too small"); return -1; }
    int64_t offset = 0;
    for (Py_ssize_t index = 0; index < length; index++) {
        Py_UCS4 scalar = PyUnicode_ReadChar(value, index);
        if (codepoints) { output[offset++] = (uint16_t)(scalar >> 16); output[offset++] = (uint16_t)scalar; }
        else if (scalar <= 0xffff) output[offset++] = (uint16_t)scalar;
        else { scalar -= 0x10000; output[offset++] = (uint16_t)(0xd800 + (scalar >> 10)); output[offset++] = (uint16_t)(0xdc00 + (scalar & 0x3ff)); }
    }
    return units;
}
