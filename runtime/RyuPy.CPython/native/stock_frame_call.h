/* frame守卫对已转换的原始CP实参工作，避免重复CLR导出/容器协议。
 * exact stdlib partial使用公共func/args/keywords逐层合并；未知callable原样执行。
 * 所有临时引用只在GIL调用栈，规范type随F的GC owner存活至atexit结束。 */
static int rp_frame_partial(RpThinLink *link, PyObject *callable) {
    return Py_IS_TYPE(callable, (PyTypeObject *)PyTuple_GET_ITEM(link->owner->frame_guards, 6));
}
/* 只用于确定CLR调用实参的source位置；绑定过source的partial无需转换CLR源。 */
API int rp_thin_frame_target(PyObject *f, PyObject *callable, intptr_t *bound) {
    if (rp_thin_validate(f) < 0) return -2;
    RpThinLink *link = rp_thin_find(f);
    *bound = 0;
    PyObject *current = Py_NewRef(callable);
    while (rp_frame_partial(link, current)) {
        PyObject *args = PyObject_GetAttrString(current, "args");
        PyObject *next = PyObject_GetAttrString(current, "func");
        Py_DECREF(current);
        if (!args || !next) { Py_XDECREF(args); Py_XDECREF(next); return -2; }
        *bound += PyTuple_GET_SIZE(args); Py_DECREF(args); current = next;
    }
    int kind = rp_thin_frame_builtin(f, current); Py_DECREF(current); return kind;
}
/* unknown关键字/重复绑定由真实builtin binder报错，绝不以frame诊断覆盖参数错误。 */
static int rp_frame_keywords(PyObject *kwargs, const char *const *allowed, int count) {
    if (!kwargs) return 1;
    Py_ssize_t pos = 0; PyObject *key, *value;
    while (PyDict_Next(kwargs, &pos, &key, &value)) {
        if (!PyUnicode_CheckExact(key)) return 0;
        int found = 0;
        for (int i = 0; i < count; i++)
            if (PyUnicode_CompareWithASCIIString(key, allowed[i]) == 0) { found = 1; break; }
        if (!found) return 0;
    }
    return 1;
}
static int rp_frame_source_valid(PyObject *source) {
    if (PyUnicode_Check(source) || PyCode_Check(source)) return 1;
    if (!PyObject_CheckBuffer(source)) return 0;
    /* 不获取用户buffer：守卫拒绝先于业务执行，公开memoryview查询不触发exporter协议。 */
    if (PyMemoryView_Check(source)) {
        PyObject *released = PyObject_GetAttrString(source, "obj");
        if (!released) return -1;
        Py_DECREF(released);
    }
    return 1;
}
API int rp_thin_frame_call(PyObject *f, PyObject *callable, PyObject *args, PyObject *kwargs) {
    if (rp_thin_validate(f) < 0) return -1;
    intptr_t bound;
    int target = rp_thin_frame_target(f, callable, &bound);
    if (target == -2) return -1;
    if (target < 0) return 0;
    RpThinLink *link = rp_thin_find(f);
    PyObject *current = Py_NewRef(callable), *positional = Py_NewRef(args);
    PyObject *keywords = kwargs ? PyDict_Copy(kwargs) : PyDict_New();
    if (!keywords) { Py_DECREF(current); Py_DECREF(positional); return -1; }
    int result = 0;
    while (rp_frame_partial(link, current)) {
        PyObject *stored = PyObject_GetAttrString(current, "args");
        PyObject *original = PyObject_GetAttrString(current, "keywords");
        PyObject *defaults = original ? PyDict_Copy(original) : NULL; Py_XDECREF(original);
        PyObject *next = PyObject_GetAttrString(current, "func");
        if (!stored || !defaults || !next) {
            Py_XDECREF(stored); Py_XDECREF(defaults); Py_XDECREF(next); result = -1; goto done;
        }
        PyObject *merged = PySequence_Concat(stored, positional); Py_DECREF(stored);
        if (!merged || PyDict_Update(defaults, keywords) < 0) {
            Py_XDECREF(merged); Py_DECREF(defaults); Py_DECREF(next); result = -1; goto done;
        }
        Py_SETREF(positional, merged); Py_SETREF(keywords, defaults); Py_SETREF(current, next);
    }
    int kind = rp_thin_frame_builtin(f, current);
    Py_ssize_t size = PyTuple_GET_SIZE(positional), key_count = PyDict_Size(keywords);
    if (kind >= 0 && kind <= 2 && size == 0 && key_count == 0) { result = kind + 1; goto done; }
    if (kind == 3 || kind == 4) {
        static const char *const allowed[] = {"globals", "locals", "closure"};
        if (size < 1 || size > 3 || !rp_frame_keywords(keywords, allowed, kind == 4 ? 3 : 2)) goto done;
#if PY_VERSION_HEX < 0x030D0000
        if (PyDict_GetItemString(keywords, "globals") || PyDict_GetItemString(keywords, "locals")) goto done;
#endif
        if ((size >= 2 && PyDict_GetItemString(keywords, "globals")) ||
            (size >= 3 && PyDict_GetItemString(keywords, "locals"))) goto done;
        PyObject *globals = size >= 2 ? PyTuple_GET_ITEM(positional, 1) : PyDict_GetItemString(keywords, "globals");
        if (!globals || globals == Py_None) {
            int valid = rp_frame_source_valid(PyTuple_GET_ITEM(positional, 0));
            if (valid < 0) result = -1; else if (valid) result = kind + 1;
        }
    } else if (kind == 5) {
        static const char *const allowed[] = {"source", "filename", "mode", "flags", "dont_inherit", "optimize", "_feature_version"};
        if (size > 6 || !rp_frame_keywords(keywords, allowed, 7)) goto done;
        for (Py_ssize_t i = 0; i < size; i++) if (PyDict_GetItemString(keywords, allowed[i])) goto done;
        PyObject *required[3];
        for (int i = 0; i < 3; i++) {
            required[i] = size > i ? PyTuple_GET_ITEM(positional, i) : PyDict_GetItemString(keywords, allowed[i]);
            if (!required[i]) goto done;
        }
        if (!PyUnicode_Check(required[1]) || !PyUnicode_Check(required[2]) || PyCode_Check(required[0])) goto done;
        if (PyUnicode_CompareWithASCIIString(required[2], "eval") != 0 &&
            PyUnicode_CompareWithASCIIString(required[2], "exec") != 0 &&
            PyUnicode_CompareWithASCIIString(required[2], "single") != 0) goto done;
        PyObject *inherit = size >= 5 ? PyTuple_GET_ITEM(positional, 4) : PyDict_GetItemString(keywords, "dont_inherit");
        /* 已知true不需要caller flags，直接保留原binder及全部参数协议。 */
        if (inherit && (inherit == Py_True || (PyLong_CheckExact(inherit) && PyObject_IsTrue(inherit)))) goto done;
        PyObject *flags = size >= 4 ? PyTuple_GET_ITEM(positional, 3) : PyDict_GetItemString(keywords, "flags");
        PyObject *optimize = size >= 6 ? PyTuple_GET_ITEM(positional, 5) : PyDict_GetItemString(keywords, "optimize");
        /* 精确整数可只读核验；用户__index__/无效范围交真实binder，保留其顺序/异常。 */
        if (flags) {
            if (!PyLong_CheckExact(flags) && !PyBool_Check(flags)) {
                if (PyIndex_Check(flags)) result = 7;
                goto done;
            }
            int overflow = 0; long value = PyLong_AsLongAndOverflow(flags, &overflow);
            if (overflow) goto done;
            long mask = PyCF_MASK | PyCF_MASK_OBSOLETE | PyCF_ONLY_AST | PyCF_ALLOW_TOP_LEVEL_AWAIT | PyCF_TYPE_COMMENTS;
#ifdef PyCF_OPTIMIZED_AST
            mask |= PyCF_OPTIMIZED_AST;
#endif
            if (value < 0 || (value & ~mask)) goto done;
        }
        if (optimize) {
            if (!PyLong_CheckExact(optimize) && !PyBool_Check(optimize)) {
                if (PyIndex_Check(optimize)) result = 7;
                goto done;
            }
            int overflow = 0; long value = PyLong_AsLongAndOverflow(optimize, &overflow);
            if (overflow || value < -1 || value > 2) goto done;
        }

        if (inherit && inherit != Py_False && !PyLong_CheckExact(inherit)) { result = 7; goto done; }
        if (!inherit || inherit == Py_False || (PyLong_CheckExact(inherit) && PyObject_IsTrue(inherit) == 0)) {
            int valid = rp_frame_source_valid(required[0]);
            if (valid < 0) result = -1; else if (valid) result = 6;
        }
    }
done:
    Py_DECREF(current); Py_DECREF(positional); Py_DECREF(keywords); return result;
}
