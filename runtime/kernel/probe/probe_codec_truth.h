/* 编码器 final 参数无状态：CPython UTF-8 encoder 完全忽略它，不求真值。
 * 使用真实 C 类型的 __bool__ 反例，防止把 PyArg_Parse 的 p 格式引入可观察副作用。 */
#ifndef RYUPY_PROBE_CODEC_TRUTH_H
#define RYUPY_PROBE_CODEC_TRUTH_H
static int
probe_forbidden_truth(PyObject *Py_UNUSED(value))
{
    PyErr_SetString(PyExc_AssertionError, "encoder final must not call __bool__");
    return -1;
}

static int
probe_encoder_final_ignored(PyObject *encoder)
{
    PyType_Slot slots[] = {{Py_nb_bool, probe_forbidden_truth}, {0, NULL}};
    PyType_Spec spec = {"probe.FinalTrap", sizeof(PyObject), 0, Py_TPFLAGS_DEFAULT, slots};
    PyObject *type = PyType_FromSpec(&spec);
    PROBE_CHECK(type != NULL, "final trap type");
    PyObject *trap = PyObject_CallNoArgs(type);
    Py_DECREF(type);
    PROBE_CHECK(trap != NULL, "final trap instance");
    PyObject *result = PyObject_CallMethod(encoder, "encode", "sO", "ok", trap);
    Py_DECREF(trap);
    PROBE_CHECK(result != NULL && PyBytes_Check(result) && PyBytes_Size(result) == 2,
                "UTF-8 encoder must ignore final without evaluating it");
    Py_DECREF(result);
    return 0;
}
#endif
