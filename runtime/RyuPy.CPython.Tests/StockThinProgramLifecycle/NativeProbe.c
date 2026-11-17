/* 独立生命周期探针 shim：包含原产品实现，仅增加测试导出和终态模板所有权移交。
 * 不读写 CPython 私有内存；保留的模板/type 借用地址由正在调用的 F/owner 强引用见证。
 * 此文件与产品 shim 分开构建，终态之后只读取 C 计数，不调用解释器 API。
 */
#define rp_decref program_original_decref
#define rp_thin_make program_original_make
#include "ryupy_cpython.c"
#undef rp_decref
#undef rp_thin_make
static int terminal_prepared, terminal_done;
static long terminal_decrefs, post_finalize_decrefs;
static long lowlevel_live = -1;
static void program_lowlevel_exit(void) { lowlevel_live = rp_thin_live; }
API int program_install_observer(void) { return Py_AtExit(program_lowlevel_exit); }
API long long program_lowlevel_live(void) { return lowlevel_live; }
API PyObject *rp_thin_make(PyObject *d, intptr_t token, int failure) {
    if (terminal_prepared) {
        /* 拒绝新定义时仍消费调用方交付的 core token，沿用 make 的单一所有权合同。 */
        rp_thin_release(token);
        PyErr_SetString(PyExc_RuntimeError, "程序终态禁止创建新薄函数");
        return NULL;
    }
    return program_original_make(d, token, failure);
}
API int program_canonical_builtins(PyObject *module) {
    /* 原 Runtime metadata 前言会写 managed 默认 builtins；本冷路在固定 Entry 创建 F 前恢复原 CP dict。
     * 只使用公开 module/dict API；替换旧代理触发真实 DECREF/dealloc，业务函数尚未创建。 */
    return PyDict_SetItemString(PyModule_GetDict(module), "__builtins__", PyEval_GetBuiltins());
}
API int program_prepare(void) {
    if (terminal_prepared || !rp_thin_live || !rp_thin_template || !rp_fo_type) return -1;
    terminal_prepared = 1;
    /* 只交还 shim 自己的两份 owned 引用；F 保活 code，owner 保活 heap type。
     * 不能 Py_CLEAR：现有 shape/dispatch 在终态回调中仍比较这两个借用别名。 */
    Py_DECREF(rp_thin_template); Py_DECREF(rp_fo_type);
    return 0;
}
API void rp_decref(PyObject *p) {
    if (terminal_done) { post_finalize_decrefs++; return; }
    terminal_decrefs++; Py_XDECREF(p);
}
API int program_stop(void) {
    int result = rp_stop();
    terminal_done = 1;
    if (terminal_prepared) {
        /* 这些全局已经转为借用；清别名没有 DECREF。 */
        rp_thin_template = NULL; rp_fo_type = NULL;
    }
    return result;
}
API void program_counts(long long *live, long long *destroyed, long long *calls,
                       long long *decrefs, long long *post) {
    rp_thin_stats(live, destroyed, calls);
    *decrefs = terminal_decrefs; *post = post_finalize_decrefs;
}
