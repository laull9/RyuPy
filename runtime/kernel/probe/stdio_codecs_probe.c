/* RyuPy 严格零字节码内核手写 C 探针（纯 C UTF-8 CodecInfo 与 _io 标准流）。
 *
 * 验证目标（同时支持与未修改官方 CPython 3.12.13 编译比对）：
 * 1. codecs-utf8：
 *    - PyCodec_KnownEncoding / _codecs.lookup 对 utf-8 及别名（UTF-8/utf8/u8/utf/cp65001）
 *      返回真实 4 元组 CodecInfo，含 name="utf-8"、_is_text_encoding=True、encode/decode、
 *      incrementalencoder/incrementaldecoder。
 *    - 复用内建 C 错误处理器（strict/ignore/replace/surrogateescape/surrogatepass/
 *      backslashreplace/xmlcharrefreplace）与 PyCodec_RegisterError 自定义 C 处理器。
 *    - IncrementalEncoder / IncrementalDecoder 的跨 chunk（3 字节汉字、4 字节 emoji 拆分）、
 *      getstate/setstate/reset/errors 读写及 final=True 截断错误处理、memoryview 输入、
 *      以及与 _io.TextIOWrapper(newline=None) 的联动。
 * 2. stdio-streams：
 *    - sys.stdin/stdout/stderr 与 sys.__stdin__/__stdout__/__stderr__ 六个标准流身份、
 *      底层 _io.TextIOWrapper / BufferedReader / BufferedWriter / FileIO(closefd=False)、
 *      encoding="utf-8"、errors、mode、name 及 builtins.open。
 *    - print / input / EOFError / buffer / fileno / isatty / 未 flush 前缓冲与 flush 后落盘 /
 *      Py_FinalizeEx 自动 flush / close() 后报 ValueError 但宿主 OS fd 仍保持打开。
 *    - 严格零字节码内核模式下全程 strict_reject=1，三阶段计数均严格为 (0,0,0)。
 */
#include "probe_common.h"
#include "probe_codec_truth.h"

/* 自定义 C 错误处理器：在遇到编码错误时替换为 "[C_ERR]" 并跳过出错区间。 */
static PyObject *
custom_c_err_handler(PyObject *Py_UNUSED(self), PyObject *exc)
{
    if (PyObject_TypeCheck(exc, (PyTypeObject *)PyExc_UnicodeEncodeError)) {
        Py_ssize_t end = 0;
        if (PyUnicodeEncodeError_GetEnd(exc, &end) < 0) {
            return NULL;
        }
        return Py_BuildValue("(sn)", "[C_ERR]", end);
    }
    PyErr_SetString(PyExc_TypeError, "unexpected exception in custom_c_err_handler");
    return NULL;
}

static PyMethodDef custom_err_def = {
    "custom_c_err",
    custom_c_err_handler,
    METH_O,
    NULL
};

static int
verify_codec_stateless_and_handlers(PyObject *ci)
{
    PyObject *name_attr = PyObject_GetAttrString(ci, "name");
    PROBE_CHECK(name_attr != NULL && PyUnicode_Check(name_attr), "CodecInfo.name missing");
    PROBE_CHECK(strcmp(PyUnicode_AsUTF8(name_attr), "utf-8") == 0, "CodecInfo.name != utf-8");
    Py_DECREF(name_attr);

    PyObject *is_text = PyObject_GetAttrString(ci, "_is_text_encoding");
    PROBE_CHECK(is_text != NULL && PyObject_IsTrue(is_text) == 1, "_is_text_encoding != True");
    Py_DECREF(is_text);

    PyObject *enc_fn = PyObject_GetAttrString(ci, "encode");
    PyObject *dec_fn = PyObject_GetAttrString(ci, "decode");
    PROBE_CHECK(enc_fn != NULL && PyCallable_Check(enc_fn), "CodecInfo.encode not callable");
    PROBE_CHECK(dec_fn != NULL && PyCallable_Check(dec_fn), "CodecInfo.decode not callable");
    PROBE_CHECK(PyTuple_GetItem(ci, 0) == enc_fn, "ci[0] != ci.encode");
    PROBE_CHECK(PyTuple_GetItem(ci, 1) == dec_fn, "ci[1] != ci.decode");

    /* 无状态 encode / decode 校验（含中文与 consumed 长度） */
    PyObject *eres = PyObject_CallFunction(enc_fn, "s", "hi\xe4\xb8\xad");
    PROBE_CHECK(eres != NULL && PyTuple_Check(eres) && PyTuple_Size(eres) == 2, "encode tuple");
    PyObject *ebytes = PyTuple_GetItem(eres, 0);
    Py_ssize_t econsumed = PyLong_AsSsize_t(PyTuple_GetItem(eres, 1));
    PROBE_CHECK(PyBytes_Check(ebytes) && econsumed == 3, "encode result/consumed mismatch");
    PROBE_CHECK(PyBytes_Size(ebytes) == 5 &&
                    memcmp(PyBytes_AsString(ebytes), "hi\xe4\xb8\xad", 5) == 0,
                "encode UTF-8 bytes mismatch");

    PyObject *dres = PyObject_CallFunction(dec_fn, "O", ebytes);
    PROBE_CHECK(dres != NULL && PyTuple_Check(dres) && PyTuple_Size(dres) == 2, "decode tuple");
    PyObject *dstr = PyTuple_GetItem(dres, 0);
    Py_ssize_t dconsumed = PyLong_AsSsize_t(PyTuple_GetItem(dres, 1));
    PROBE_CHECK(PyUnicode_Check(dstr) && dconsumed == 5, "decode result/consumed mismatch");
    PROBE_CHECK(strcmp(PyUnicode_AsUTF8(dstr), "hi\xe4\xb8\xad") == 0, "decode UTF-8 str mismatch");
    Py_DECREF(dres);
    Py_DECREF(eres);

    /* strict 错误检查（孤立代理与非法字节） */
    PyObject *lone_surr = PyUnicode_FromKindAndData(PyUnicode_2BYTE_KIND, (const Py_UCS2[]){0xd800}, 1);
    PROBE_CHECK(lone_surr != NULL, "alloc surrogate");
    PyObject *bad_enc = PyObject_CallFunction(enc_fn, "Os", lone_surr, "strict");
    PROBE_CHECK(bad_enc == NULL && PyErr_ExceptionMatches(PyExc_UnicodeEncodeError),
                "strict encode surrogate must raise UnicodeEncodeError");
    PyErr_Clear();

    /* 复用 C 错误处理器：surrogatepass / backslashreplace / replace / ignore */
    PyObject *sp = PyObject_CallFunction(enc_fn, "Os", lone_surr, "surrogatepass");
    PROBE_CHECK(sp != NULL, "surrogatepass encode");
    PyObject *sp_b = PyTuple_GetItem(sp, 0);
    PROBE_CHECK(PyBytes_Size(sp_b) == 3 &&
                    memcmp(PyBytes_AsString(sp_b), "\xed\xa0\x80", 3) == 0,
                "surrogatepass bytes mismatch");
    Py_DECREF(sp);

    PyObject *br = PyObject_CallFunction(enc_fn, "Os", lone_surr, "backslashreplace");
    PROBE_CHECK(br != NULL, "backslashreplace encode");
    PyObject *br_b = PyTuple_GetItem(br, 0);
    PROBE_CHECK(strcmp(PyBytes_AsString(br_b), "\\ud800") == 0, "backslashreplace mismatch");
    Py_DECREF(br);

    /* 自定义 PyCodec_RegisterError + PyCodec_Encode */
    PyObject *cfunc = PyCFunction_NewEx(&custom_err_def, NULL, NULL);
    PROBE_CHECK(cfunc != NULL && PyCodec_RegisterError("ryupy_custom", cfunc) == 0,
                "PyCodec_RegisterError failed");
    Py_DECREF(cfunc);
    PyObject *cust = PyCodec_Encode(lone_surr, "utf-8", "ryupy_custom");
    PROBE_CHECK(cust != NULL && PyBytes_Check(cust) &&
                    strcmp(PyBytes_AsString(cust), "[C_ERR]") == 0,
                "custom C error handler mismatch");
    Py_DECREF(cust);
    Py_DECREF(lone_surr);

    PyObject *raw_bad = PyBytes_FromStringAndSize("a\xff\xfe", 3);
    PyObject *bad_dec = PyObject_CallFunction(dec_fn, "Os", raw_bad, "strict");
    PROBE_CHECK(bad_dec == NULL && PyErr_ExceptionMatches(PyExc_UnicodeDecodeError),
                "strict decode invalid bytes must raise UnicodeDecodeError");
    PyErr_Clear();

    PyObject *rep_dec = PyObject_CallFunction(dec_fn, "Os", raw_bad, "replace");
    PROBE_CHECK(rep_dec != NULL &&
                    strcmp(PyUnicode_AsUTF8(PyTuple_GetItem(rep_dec, 0)),
                           "a\xef\xbf\xbd\xef\xbf\xbd") == 0,
                "replace decode mismatch");
    Py_DECREF(rep_dec);
    Py_DECREF(raw_bad);

    Py_DECREF(enc_fn);
    Py_DECREF(dec_fn);
    return 0;
}

static int
verify_incremental_encoder_decoder(PyObject *ci)
{
    PyObject *inc_enc_cls = PyObject_GetAttrString(ci, "incrementalencoder");
    PyObject *inc_dec_cls = PyObject_GetAttrString(ci, "incrementaldecoder");
    PROBE_CHECK(inc_enc_cls != NULL && inc_dec_cls != NULL, "incremental factories missing");

    /* IncrementalEncoder：默认 errors / errors 变更 / getstate / setstate / reset */
    PyObject *enc = PyObject_CallNoArgs(inc_enc_cls);
    PROBE_CHECK(enc != NULL, "IncrementalEncoder() failed");
    PROBE_CHECK(probe_encoder_final_ignored(enc) == 0, "encoder final protocol");
    PyObject *err_attr = PyObject_GetAttrString(enc, "errors");
    PROBE_CHECK(err_attr != NULL && strcmp(PyUnicode_AsUTF8(err_attr), "strict") == 0,
                "IncrementalEncoder default errors != strict");
    Py_DECREF(err_attr);

    PyObject *e1 = PyObject_CallMethod(enc, "encode", "s", "hello");
    PyObject *e2 = PyObject_CallMethod(enc, "encode", "sO", "\xe4\xb8\xad", Py_True);
    PROBE_CHECK(e1 != NULL && strcmp(PyBytes_AsString(e1), "hello") == 0, "incenc chunk1");
    PROBE_CHECK(e2 != NULL && strcmp(PyBytes_AsString(e2), "\xe4\xb8\xad") == 0, "incenc chunk2");
    Py_DECREF(e1);
    Py_DECREF(e2);

    PyObject *est = PyObject_CallMethod(enc, "getstate", NULL);
    PROBE_CHECK(est != NULL && PyLong_AsLong(est) == 0, "incenc getstate != 0");
    Py_DECREF(est);
    PyObject *es1 = PyObject_CallMethod(enc, "setstate", "i", 0);
    PyObject *er1 = PyObject_CallMethod(enc, "reset", NULL);
    PROBE_CHECK(es1 == Py_None && er1 == Py_None, "incenc setstate/reset");
    Py_DECREF(es1);
    Py_DECREF(er1);

    PyObject *surr_dc80 = PyUnicode_FromKindAndData(PyUnicode_2BYTE_KIND, (const Py_UCS2[]){0xdc80}, 1);
    PyObject *se_str = PyUnicode_FromString("surrogateescape");
    PROBE_CHECK(PyObject_SetAttrString(enc, "errors", se_str) == 0, "mutate enc.errors");
    Py_DECREF(se_str);
    PyObject *e3 = PyObject_CallMethod(enc, "encode", "O", surr_dc80);
    PROBE_CHECK(e3 != NULL && PyBytes_Size(e3) == 1 && (unsigned char)PyBytes_AsString(e3)[0] == 0x80,
                "incenc mutated surrogateescape failed");
    Py_DECREF(e3);
    Py_DECREF(surr_dc80);
    Py_DECREF(enc);

    /* IncrementalDecoder：跨 chunk 3字节汉字 + 4字节 emoji + getstate/setstate/reset + memoryview */
    PyObject *dec = PyObject_CallNoArgs(inc_dec_cls);
    PROBE_CHECK(dec != NULL, "IncrementalDecoder() failed");

    PyObject *c1 = PyBytes_FromStringAndSize("\xe4", 1);
    PyObject *c2 = PyBytes_FromStringAndSize("\xb8", 1);
    PyObject *c3 = PyBytes_FromStringAndSize("\xad", 1);
    PyObject *r1 = PyObject_CallMethod(dec, "decode", "O", c1);
    PyObject *r2 = PyObject_CallMethod(dec, "decode", "O", c2);
    PyObject *st2 = PyObject_CallMethod(dec, "getstate", NULL);
    PROBE_CHECK(r1 != NULL && PyUnicode_GetLength(r1) == 0, "3-byte chunk1 must wait");
    PROBE_CHECK(r2 != NULL && PyUnicode_GetLength(r2) == 0, "3-byte chunk2 must wait");
    PROBE_CHECK(st2 != NULL && PyTuple_Check(st2) && PyTuple_Size(st2) == 2, "getstate 2-tuple");
    PROBE_CHECK(PyBytes_Size(PyTuple_GetItem(st2, 0)) == 2 &&
                    memcmp(PyBytes_AsString(PyTuple_GetItem(st2, 0)), "\xe4\xb8", 2) == 0 &&
                    PyLong_AsLong(PyTuple_GetItem(st2, 1)) == 0,
                "getstate buffered bytes mismatch");
    Py_DECREF(r1);
    Py_DECREF(r2);

    PyObject *r3 = PyObject_CallMethod(dec, "decode", "O", c3);
    PROBE_CHECK(r3 != NULL && strcmp(PyUnicode_AsUTF8(r3), "\xe4\xb8\xad") == 0,
                "3-byte chunk3 decoded mismatch");
    Py_DECREF(r3);
    Py_DECREF(c1);
    Py_DECREF(c2);
    Py_DECREF(c3);

    /* 4字节 emoji F0 9F 98 80 跨 chunk 并用 setstate/reset 恢复 */
    PyObject *e_head = PyBytes_FromStringAndSize("A\xf0\x9f", 3);
    PyObject *e_tail = PyBytes_FromStringAndSize("\x98\x80Z", 3);
    PyObject *mv_tail = PyMemoryView_FromObject(e_tail);
    PyObject *re1 = PyObject_CallMethod(dec, "decode", "O", e_head);
    PROBE_CHECK(re1 != NULL && strcmp(PyUnicode_AsUTF8(re1), "A") == 0, "emoji head");
    Py_DECREF(re1);
    PyObject *saved_st = PyObject_CallMethod(dec, "getstate", NULL);
    PROBE_CHECK(saved_st != NULL &&
                    PyBytes_Size(PyTuple_GetItem(saved_st, 0)) == 2,
                "emoji saved state len");

    PyObject *rst = PyObject_CallMethod(dec, "reset", NULL);
    PROBE_CHECK(rst == Py_None, "dec.reset()");
    Py_DECREF(rst);
    PyObject *after_rst = PyObject_CallMethod(dec, "getstate", NULL);
    PROBE_CHECK(after_rst != NULL && PyBytes_Size(PyTuple_GetItem(after_rst, 0)) == 0 &&
                    PyLong_AsLong(PyTuple_GetItem(after_rst, 1)) == 0,
                "after reset getstate != (b'', 0)");
    Py_DECREF(after_rst);

    PyObject *dec2 = PyObject_CallNoArgs(inc_dec_cls);
    PyObject *set_rc = PyObject_CallMethod(dec2, "setstate", "(O)", saved_st);
    PROBE_CHECK(set_rc == Py_None, "dec2.setstate()");
    Py_DECREF(set_rc);
    PyObject *restored_state = PyObject_CallMethod(dec2, "getstate", NULL);
    PROBE_CHECK(restored_state && PyTuple_GetItem(restored_state, 0) == PyTuple_GetItem(saved_st, 0),
                "restored immutable bytes state must preserve identity");
    Py_DECREF(restored_state);
    PyObject *re2 = PyObject_CallMethod(dec2, "decode", "OO", mv_tail, Py_True);
    PROBE_CHECK(re2 != NULL && strcmp(PyUnicode_AsUTF8(re2), "\xf0\x9f\x98\x80Z") == 0,
                "emoji restored decode mismatch");
    Py_DECREF(re2);
    Py_DECREF(dec2);
    Py_DECREF(saved_st);
    Py_DECREF(st2);
    Py_DECREF(mv_tail);
    Py_DECREF(e_head);
    Py_DECREF(e_tail);

    /* final=True 对末尾不完整序列的处理：strict 报错，replace 产出 U+FFFD */
    PyObject *inc_b = PyBytes_FromStringAndSize("\xe4\xb8", 2);
    PyObject *bad_fin = PyObject_CallMethod(dec, "decode", "OO", inc_b, Py_True);
    PROBE_CHECK(bad_fin == NULL && PyErr_ExceptionMatches(PyExc_UnicodeDecodeError),
                "strict final=True incomplete must raise UnicodeDecodeError");
    PyErr_Clear();
    Py_DECREF(dec);

    PyObject *dec_rep = PyObject_CallFunction(inc_dec_cls, "s", "replace");
    PyObject *rep_fin = PyObject_CallMethod(dec_rep, "decode", "OO", inc_b, Py_True);
    PROBE_CHECK(rep_fin != NULL && strcmp(PyUnicode_AsUTF8(rep_fin), "\xef\xbf\xbd") == 0,
                "replace final=True incomplete != U+FFFD");
    Py_DECREF(rep_fin);
    Py_DECREF(dec_rep);
    Py_DECREF(inc_b);

    /* 验证 _io.TextIOWrapper(BytesIO, encoding='utf-8', newline=None) 联动 IncrementalNewlineDecoder */
    PyObject *io_mod = PyImport_ImportModule("_io");
    PyObject *bio_cls = PyObject_GetAttrString(io_mod, "BytesIO");
    PyObject *tw_cls = PyObject_GetAttrString(io_mod, "TextIOWrapper");
    PyObject *bio = PyObject_CallFunction(bio_cls, "y#", "a\r\n\xe4\xb8\xad\r\n", 8);
    PyObject *tw = PyObject_CallFunction(tw_cls, "Os", bio, "utf-8");
    PROBE_CHECK(tw != NULL, "TextIOWrapper(BytesIO, 'utf-8') failed");
    PyObject *line1 = PyObject_CallMethod(tw, "readline", NULL);
    PyObject *line2 = PyObject_CallMethod(tw, "readline", NULL);
    PROBE_CHECK(line1 != NULL && strcmp(PyUnicode_AsUTF8(line1), "a\n") == 0, "TextIOWrapper nl1");
    PROBE_CHECK(line2 != NULL && strcmp(PyUnicode_AsUTF8(line2), "\xe4\xb8\xad\n") == 0,
                "TextIOWrapper nl2");
    Py_DECREF(line1);
    Py_DECREF(line2);
    Py_DECREF(tw);
    Py_DECREF(bio);
    Py_DECREF(tw_cls);
    Py_DECREF(bio_cls);
    Py_DECREF(io_mod);

    Py_DECREF(inc_enc_cls);
    Py_DECREF(inc_dec_cls);
    return 0;
}

static int
run_codecs_utf8(const char *home)
{
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin strict");
    PyStatus st;
    PROBE_CHECK(probe_init_python(home, 1, "utf-8", "surrogateescape",
                                  "utf-8", "surrogateescape", 0, 1, &st) == 0,
                "init_python failed");
    PROBE_CHECK(probe_enter_runtime() == 0, "enter runtime");

    PROBE_CHECK(PyCodec_KnownEncoding("utf-8") == 1, "utf-8 known");
    PROBE_CHECK(PyCodec_KnownEncoding("UTF-8") == 1, "UTF-8 known");
    PROBE_CHECK(PyCodec_KnownEncoding("utf8") == 1, "utf8 known");
    PROBE_CHECK(PyCodec_KnownEncoding("u8") == 1, "u8 known");
    PROBE_CHECK(PyCodec_KnownEncoding("utf") == 1, "utf known");
    PROBE_CHECK(PyCodec_KnownEncoding("cp65001") == 1, "cp65001 known");
#ifndef RYUPY_STOCK_CPYTHON
    /* 内核边界：仅内置纯 C UTF-8 编解码，显式拒绝 latin-1/ascii/utf-8-sig */
    PROBE_CHECK(PyCodec_KnownEncoding("latin-1") == 0, "latin-1 must be unknown in kernel");
    PROBE_CHECK(PyCodec_KnownEncoding("ascii") == 0, "ascii must be unknown in kernel");
    PROBE_CHECK(PyCodec_KnownEncoding("utf-8-sig") == 0, "utf-8-sig must be unknown in kernel");
#endif

    PyObject *codecs_mod = PyImport_ImportModule("_codecs");
    PROBE_CHECK(codecs_mod != NULL, "_codecs import failed");
    PyObject *ci = PyObject_CallMethod(codecs_mod, "lookup", "s", "utf-8");
    PROBE_CHECK(ci != NULL, "_codecs.lookup('utf-8') failed");

    if (verify_codec_stateless_and_handlers(ci) != 0) {
        Py_DECREF(ci);
        Py_DECREF(codecs_mod);
        return 1;
    }
    if (verify_incremental_encoder_decoder(ci) != 0) {
        Py_DECREF(ci);
        Py_DECREF(codecs_mod);
        return 1;
    }

#ifndef RYUPY_STOCK_CPYTHON
    PyObject *bad = PyObject_CallMethod(codecs_mod, "lookup", "s", "latin-1");
    PROBE_CHECK(bad == NULL && PyErr_ExceptionMatches(PyExc_LookupError),
                "_codecs.lookup('latin-1') must raise LookupError");
    PyErr_Clear();
#endif

    Py_DECREF(ci);
    Py_DECREF(codecs_mod);

    PROBE_CHECK(probe_enter_shutdown() == 0, "enter shutdown");
    PROBE_CHECK(Py_FinalizeEx() == 0, "Py_FinalizeEx failed");
    PROBE_CHECK(probe_assert_zero_bytecode("codecs-utf8") == 0, "zero bytecode");
    printf("codecs-utf8 PASS\n");
    return 0;
}

static int
verify_stream_identity(PyObject *sys_mod, PyObject *io_mod, const char *attr,
                       const char *dunder_attr, const char *expected_name,
                       const char *expected_mode, const char *expected_errors,
                       const char *expected_buf_type, int expected_fd)
{
    PyObject *s = PyObject_GetAttrString(sys_mod, attr);
    PyObject *ds = PyObject_GetAttrString(sys_mod, dunder_attr);
    PROBE_CHECK(s != NULL && ds != NULL && s == ds, "stream identity mismatch");

    PyObject *tw_type = PyObject_GetAttrString(io_mod, "TextIOWrapper");
    PyObject *buf_type = PyObject_GetAttrString(io_mod, expected_buf_type);
    PyObject *fio_type = PyObject_GetAttrString(io_mod, "FileIO");
    PROBE_CHECK(PyObject_TypeCheck(s, (PyTypeObject *)tw_type), "stream not TextIOWrapper");

    PyObject *buf = PyObject_GetAttrString(s, "buffer");
    PROBE_CHECK(buf != NULL && PyObject_TypeCheck(buf, (PyTypeObject *)buf_type),
                "buffer type mismatch");
    PyObject *raw = PyObject_GetAttrString(buf, "raw");
    PROBE_CHECK(raw != NULL && PyObject_TypeCheck(raw, (PyTypeObject *)fio_type),
                "raw not FileIO");

    PyObject *closefd = PyObject_GetAttrString(raw, "closefd");
    PROBE_CHECK(closefd == Py_False, "stdio raw closefd must be False");
    Py_DECREF(closefd);

    PyObject *enc = PyObject_GetAttrString(s, "encoding");
    PyObject *errs = PyObject_GetAttrString(s, "errors");
    PyObject *mode = PyObject_GetAttrString(s, "mode");
    PyObject *name = PyObject_GetAttrString(s, "name");
    PROBE_CHECK(strcmp(PyUnicode_AsUTF8(enc), "utf-8") == 0, "stream encoding != utf-8");
    PROBE_CHECK(strcmp(PyUnicode_AsUTF8(errs), expected_errors) == 0, "stream errors mismatch");
    PROBE_CHECK(strcmp(PyUnicode_AsUTF8(mode), expected_mode) == 0, "stream mode mismatch");
    PROBE_CHECK(strcmp(PyUnicode_AsUTF8(name), expected_name) == 0, "stream name mismatch");
    Py_DECREF(enc);
    Py_DECREF(errs);
    Py_DECREF(mode);
    Py_DECREF(name);

    PyObject *fno = PyObject_CallMethod(s, "fileno", NULL);
    PyObject *tty = PyObject_CallMethod(s, "isatty", NULL);
    PROBE_CHECK(fno != NULL && PyLong_AsLong(fno) == expected_fd, "fileno mismatch");
    PROBE_CHECK(tty == Py_False, "redirected pipe isatty must be False");
    Py_DECREF(fno);
    Py_DECREF(tty);

    Py_DECREF(raw);
    Py_DECREF(buf);
    Py_DECREF(fio_type);
    Py_DECREF(buf_type);
    Py_DECREF(tw_type);
    Py_DECREF(s);
    Py_DECREF(ds);
    return 0;
}

static int
run_stdio_streams(const char *home)
{
    int in_pipe[2], out_pipe[2], err_pipe[2];
    PROBE_CHECK(pipe(in_pipe) == 0 && pipe(out_pipe) == 0 && pipe(err_pipe) == 0, "pipe failed");
    int saved0 = dup(0), saved1 = dup(1), saved2 = dup(2);
    PROBE_CHECK(saved0 >= 0 && saved1 >= 0 && saved2 >= 0, "dup failed");
    PROBE_CHECK(dup2(in_pipe[0], 0) >= 0 && dup2(out_pipe[1], 1) >= 0 && dup2(err_pipe[1], 2) >= 0,
                "dup2 failed");
    close(in_pipe[0]);
    close(out_pipe[1]);
    close(err_pipe[1]);

    /* 先只写两行文本；原始字节在文本读取完成后再写，避免 TextIOWrapper 预读。 */
    const char *in_payload = "hello \xe4\xb8\x96\xe7\x95\x8c\n\n";
    PROBE_CHECK(write(in_pipe[1], in_payload, strlen(in_payload)) == (ssize_t)strlen(in_payload),
                "write stdin pipe failed");
    PROBE_CHECK(probe_begin_strict_kernel() == 0, "begin strict");
    PyStatus st;
    PROBE_CHECK(probe_init_python(home, 1, "utf-8", "surrogateescape",
                                  "utf-8", "surrogateescape", 0, 1, &st) == 0,
                "init_python failed");
    PROBE_CHECK(probe_enter_runtime() == 0, "enter runtime");

    PyObject *sys_mod = PyImport_ImportModule("sys");
    PyObject *io_mod = PyImport_ImportModule("_io");
    PyObject *builtins_mod = PyImport_ImportModule("builtins");
    PROBE_CHECK(sys_mod && io_mod && builtins_mod, "core builtins/_io/sys missing");

    /* builtins.open 指向 _io.open */
    PyObject *b_open = PyObject_GetAttrString(builtins_mod, "open");
    PyObject *io_open = PyObject_GetAttrString(io_mod, "open");
    PROBE_CHECK(b_open == io_open, "builtins.open != _io.open");
    Py_DECREF(b_open);
    Py_DECREF(io_open);

    /* 验证六个标准流身份与属性 */
    if (verify_stream_identity(sys_mod, io_mod, "stdin", "__stdin__", "<stdin>",
                               "r", "surrogateescape", "BufferedReader", 0) != 0 ||
        verify_stream_identity(sys_mod, io_mod, "stdout", "__stdout__", "<stdout>",
                               "w", "surrogateescape", "BufferedWriter", 1) != 0 ||
        verify_stream_identity(sys_mod, io_mod, "stderr", "__stderr__", "<stderr>",
                               "w", "backslashreplace", "BufferedWriter", 2) != 0) {
        return 1;
    }

    PyObject *stdout_obj = PyObject_GetAttrString(sys_mod, "stdout");
    PyObject *stderr_obj = PyObject_GetAttrString(sys_mod, "stderr");
    PyObject *stdin_obj = PyObject_GetAttrString(sys_mod, "stdin");

    /* 管道非 tty 下 stdout.line_buffering == False，先写不带换行再 flush */
    char rbuf[512];
    ssize_t nread = 0;
    PyObject *w1 = PyObject_CallMethod(stdout_obj, "write", "s", "BUF1");
    PROBE_CHECK(w1 != NULL && PyLong_AsLong(w1) == 4, "stdout.write('BUF1')");
    Py_DECREF(w1);
    PROBE_CHECK(probe_read_nonblock(out_pipe[0], rbuf, sizeof(rbuf), &nread) == 0 && nread == 0,
                "unflushed buffered stdout must not reach pipe yet");
    PyObject *fl1 = PyObject_CallMethod(stdout_obj, "flush", NULL);
    PROBE_CHECK(fl1 == Py_None, "stdout.flush()");
    Py_DECREF(fl1);
    PROBE_CHECK(probe_read_nonblock(out_pipe[0], rbuf, sizeof(rbuf), &nread) == 0 && nread == 4 &&
                    memcmp(rbuf, "BUF1", 4) == 0,
                "flushed stdout mismatch");

    /* builtins.print 与 stderr backslashreplace */
    PyObject *print_fn = PyObject_GetAttrString(builtins_mod, "print");
    PROBE_CHECK(print_fn != NULL, "builtins.print missing");
    PyObject *kw = Py_BuildValue("{s:s,s:s,s:O}", "sep", "-", "end", "!\n", "flush", Py_True);
    PyObject *pargs = Py_BuildValue("(ss)", "hi", "\xe4\xb8\x96\xe7\x95\x8c");
    PyObject *pres = PyObject_Call(print_fn, pargs, kw);
    PROBE_CHECK(pres == Py_None, "builtins.print failed");
    Py_DECREF(pres);
    Py_DECREF(pargs);
    Py_DECREF(kw);
    PROBE_CHECK(probe_read_nonblock(out_pipe[0], rbuf, sizeof(rbuf), &nread) == 0 &&
                    nread == 11 && memcmp(rbuf, "hi-\xe4\xb8\x96\xe7\x95\x8c!\n", 11) == 0,
                "print UTF-8 output mismatch");

    PyObject *surr_d800 = PyUnicode_FromKindAndData(PyUnicode_2BYTE_KIND, (const Py_UCS2[]){0xd800}, 1);
    PyObject *ekw = Py_BuildValue("{s:O}", "file", stderr_obj);
    PyObject *eargs = Py_BuildValue("(O)", surr_d800);
    PyObject *epres = PyObject_Call(print_fn, eargs, ekw);
    PROBE_CHECK(epres == Py_None, "print to stderr failed");
    Py_DECREF(epres);
    Py_DECREF(eargs);
    Py_DECREF(ekw);
    Py_DECREF(surr_d800);
    Py_DECREF(print_fn);
    PROBE_CHECK(probe_read_nonblock(err_pipe[0], rbuf, sizeof(rbuf), &nread) == 0 &&
                    nread == 7 && memcmp(rbuf, "\\ud800\n", 7) == 0,
                "stderr backslashreplace mismatch");

    /* builtins.input 与 buffer 读写 */
    PyObject *inp1 = PyObject_CallMethod(builtins_mod, "input", "s", "P>");
    PROBE_CHECK(inp1 != NULL && strcmp(PyUnicode_AsUTF8(inp1), "hello \xe4\xb8\x96\xe7\x95\x8c") == 0,
                "input line1 mismatch");
    Py_DECREF(inp1);
    PROBE_CHECK(probe_read_nonblock(out_pipe[0], rbuf, sizeof(rbuf), &nread) == 0 &&
                    nread == 2 && memcmp(rbuf, "P>", 2) == 0,
                "input prompt not flushed to stdout");

    PyObject *inp2 = PyObject_CallMethod(builtins_mod, "input", "s", "");
    PROBE_CHECK(inp2 != NULL && PyUnicode_GetLength(inp2) == 0, "input empty line");
    Py_DECREF(inp2);

    PROBE_CHECK(write(in_pipe[1], "RAWBYTES", 8) == 8, "write raw bytes after text consumption");
    close(in_pipe[1]);
    PyObject *in_buf = PyObject_GetAttrString(stdin_obj, "buffer");
    PyObject *raw_in = PyObject_CallMethod(in_buf, "read", NULL);
    PROBE_CHECK(raw_in != NULL && PyBytes_Size(raw_in) == 8 &&
                    memcmp(PyBytes_AsString(raw_in), "RAWBYTES", 8) == 0,
                "stdin.buffer.read mismatch");
    Py_DECREF(raw_in);
    Py_DECREF(in_buf);

    PyObject *inp_eof = PyObject_CallMethod(builtins_mod, "input", NULL);
    PROBE_CHECK(inp_eof == NULL && PyErr_ExceptionMatches(PyExc_EOFError),
                "input at EOF must raise EOFError");
    PyErr_Clear();

    /* stdout.buffer.write 直接写入字节 */
    PyObject *out_buf = PyObject_GetAttrString(stdout_obj, "buffer");
    PyObject *bw = PyObject_CallMethod(out_buf, "write", "y#", "RAW\xe4\xb8\xad", (Py_ssize_t)6);
    PROBE_CHECK(bw != NULL && PyLong_AsLong(bw) == 6, "stdout.buffer.write");
    Py_DECREF(bw);
    Py_DECREF(out_buf);

    /* 留一段未 flush 的文本待 Py_FinalizeEx 自动 flush */
    PyObject *w_fin = PyObject_CallMethod(stdout_obj, "write", "s", ":FIN");
    PROBE_CHECK(w_fin != NULL, "stdout.write(':FIN')");
    Py_DECREF(w_fin);

    Py_DECREF(stdin_obj);
    Py_DECREF(stdout_obj);
    Py_DECREF(stderr_obj);
    Py_DECREF(builtins_mod);
    Py_DECREF(io_mod);
    Py_DECREF(sys_mod);

    PROBE_CHECK(probe_enter_shutdown() == 0, "enter shutdown");
    PROBE_CHECK(Py_FinalizeEx() == 0, "Py_FinalizeEx failed");

    PROBE_CHECK(probe_read_nonblock(out_pipe[0], rbuf, sizeof(rbuf), &nread) == 0 &&
                    nread == 10 && memcmp(rbuf, "RAW\xe4\xb8\xad:FIN", 10) == 0,
                "Py_FinalizeEx did not flush stdout");

    /* 宿主的 0/1/2 fd 在 Py_FinalizeEx 后仍保持打开（closefd=False） */
    PROBE_CHECK(probe_fd_is_open(0) && probe_fd_is_open(1) && probe_fd_is_open(2),
                "host stdio fds must stay open after Py_FinalizeEx");

    /* 恢复原宿主 fd */
    dup2(saved0, 0);
    dup2(saved1, 1);
    dup2(saved2, 2);
    close(saved0);
    close(saved1);
    close(saved2);
    close(out_pipe[0]);
    close(err_pipe[0]);

    PROBE_CHECK(probe_assert_zero_bytecode("stdio-streams") == 0, "zero bytecode");
    printf("stdio-streams PASS\n");
    return 0;
}

int
main(int argc, char **argv)
{
    probe_capture_diagnostics();
    if (argc != 3) {
        fprintf(stderr, "用法: %s <prefix> <codecs-utf8|stdio-streams>\n",
                argc > 0 ? argv[0] : "stdio_codecs_probe");
        return 2;
    }
    if (strcmp(argv[2], "codecs-utf8") == 0) {
        return run_codecs_utf8(argv[1]);
    }
    if (strcmp(argv[2], "stdio-streams") == 0) {
        return run_stdio_streams(argv[1]);
    }
    fprintf(stderr, "未知模式: %s\n", argv[2]);
    return 2;
}
