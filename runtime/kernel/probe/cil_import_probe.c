/* RyuPy C4-1 冻结注册表负例、静态 CIL 导入回调槽与混合 C 包/atexit 探针。
 *
 * 用法：cil_import_probe <prefix> <ext_dir> <mode>
 * 模式：
 * 1. registry-negative  : 校验坏 ABI（version/struct_size/NULL/非法名/缺 origin/空 exec）、
 *                         重复注册、未冻结前禁止导入扩展、冻结后禁止增改、解释器归属与恢复。
 * 2. cil-mixed-callbacks: 验证静态 CIL 回调单一创建/初始化事务（kernel 与回调绝不重复执行）、
 *                         CIL 包 + .so 子模块、.so 反向导入 CIL、CIL 循环导入、CIL 失败回滚重试、
 *                         fromlist 跳过/穿透、atexit 合法导入后在 _PyImport_FiniCore 调用 cleanup。
 */
#include "probe_ext_common.h"

typedef struct {
    int create_calls;
    int exec_calls;
    int cleanup_calls;
    int retry_attempts;
    int pkg_execs;
    int helper_execs;
    int cyc_a_execs;
    int cyc_b_execs;
    int atexit_execs;
} CilMockState;

static PyObject *
mock_cil_create(PyInterpreterState *interp, const char *name, PyObject *spec,
                int is_package, void *user_data)
{
    (void)interp;
    (void)spec;
    (void)is_package;
    CilMockState *st = (CilMockState *)user_data;
    st->create_calls++;
    /* 验证 create_module 仅创建对象，不写入 sys.modules（由内核单一事务发布）。 */
    if (PyDict_GetItemString(PyImport_GetModuleDict(), name) != NULL) {
        PyErr_Format(PyExc_AssertionError,
                     "CIL create_module observed %s already in sys.modules", name);
        return NULL;
    }
    return PyModule_New(name);
}

static int
mock_cil_exec(PyInterpreterState *interp, PyObject *mod, PyObject *spec, void *user_data)
{
    (void)interp;
    CilMockState *st = (CilMockState *)user_data;
    st->exec_calls++;

    const char *name = PyModule_GetName(mod);
    if (name == NULL) return -1;

    /* 确认模块在 exec_module 前已被内核放入 sys.modules 且 _initializing == True。 */
    PyObject *in_sys = PyDict_GetItemString(PyImport_GetModuleDict(), name);
    if (in_sys != mod) {
        PyErr_SetString(PyExc_AssertionError, "CIL module must be in sys.modules before exec_module");
        return -1;
    }
    PyObject *init_flag = PyObject_GetAttrString(spec, "_initializing");
    if (init_flag == NULL || PyObject_IsTrue(init_flag) != 1) {
        Py_XDECREF(init_flag);
        PyErr_SetString(PyExc_AssertionError, "spec._initializing must be True during CIL exec");
        return -1;
    }
    Py_DECREF(init_flag);

    if (strcmp(name, "cil_pkg") == 0) {
        st->pkg_execs++;
        return PyModule_AddStringConstant(mod, "CIL_PKG", "yes");
    }
    if (strcmp(name, "cil_pkg.cil_helper") == 0) {
        st->helper_execs++;
        return PyModule_AddIntConstant(mod, "CIL_VAL", 700);
    }
    if (strcmp(name, "cil_retry") == 0) {
        st->retry_attempts++;
        if (PyModule_AddIntConstant(mod, "PARTIAL", 1) < 0) return -1;
        if (st->retry_attempts == 1) {
            PyErr_SetString(PyExc_RuntimeError, "cil_retry first attempt failed");
            return -1;
        }
        return PyModule_AddIntConstant(mod, "CIL_OK", 900);
    }
    if (strcmp(name, "cil_cyc_a") == 0) {
        st->cyc_a_execs++;
        if (PyModule_AddIntConstant(mod, "A1", 11) < 0) return -1;
        PyObject *b = PyImport_ImportModule("cil_cyc_b");
        if (b == NULL) return -1;
        Py_DECREF(b);
        return PyModule_AddIntConstant(mod, "A2", 22);
    }
    if (strcmp(name, "cil_cyc_b") == 0) {
        st->cyc_b_execs++;
        PyObject *a = PyImport_ImportModule("cil_cyc_a");
        if (a == NULL) return -1;
        PyObject *a1 = PyObject_GetAttrString(a, "A1");
        Py_DECREF(a);
        if (a1 == NULL || PyLong_AsLong(a1) != 11) {
            Py_XDECREF(a1);
            return -1;
        }
        Py_DECREF(a1);
        return PyModule_AddIntConstant(mod, "B1", 33);
    }
    if (strcmp(name, "cil_atexit") == 0) {
        st->atexit_execs++;
        return PyModule_AddIntConstant(mod, "CIL_ATEXIT", 999);
    }
    PyErr_Format(PyExc_ModuleNotFoundError, "unexpected CIL module %s", name);
    return -1;
}

static void
mock_cil_cleanup(void *user_data)
{
    CilMockState *st = (CilMockState *)user_data;
    st->cleanup_calls++;
}

typedef struct {
    PyInterpreterState *interp;
    int a_cleanups;
    int b_cleanups;
    int c_cleanups;
    int d_cleanups;
    int e_cleanups;
    int freeze_rc;
    int freeze_err_match;
    int replace_rc;
    int replace_err_match;
    int register_rc;
    int register_err_match;
} CleanupTxCtx;

static void
count_b_cleanup(void *ud) { ((CleanupTxCtx *)ud)->b_cleanups++; }
static void
count_d_cleanup(void *ud) { ((CleanupTxCtx *)ud)->d_cleanups++; }
static void
count_e_cleanup(void *ud) { ((CleanupTxCtx *)ud)->e_cleanups++; }

static void
reentrant_tx_cleanup(void *ud)
{
    CleanupTxCtx *ctx = (CleanupTxCtx *)ud;
    ctx->a_cleanups++;

    ctx->freeze_rc = rp_kernel_import_freeze(ctx->interp);
    ctx->freeze_err_match = PyErr_ExceptionMatches(PyExc_RuntimeError);
    PyErr_Clear();

    RpKernelCilImportCallbacks cb_b = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelCilImportCallbacks),
        mock_cil_create, mock_cil_exec, count_b_cleanup, ctx
    };
    ctx->replace_rc = rp_kernel_import_set_cil_callbacks(ctx->interp, &cb_b);
    ctx->replace_err_match = PyErr_ExceptionMatches(PyExc_RuntimeError);
    PyErr_Clear();

    RpKernelImportEntry late = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelImportEntry),
        RYUPY_IMPORT_KIND_CIL, 0, "tx_reentrant_entry", NULL, NULL
    };
    ctx->register_rc = rp_kernel_import_register(ctx->interp, &late);
    ctx->register_err_match = PyErr_ExceptionMatches(PyExc_RuntimeError);
    PyErr_Clear();
}

static void
raising_tx_cleanup(void *ud)
{
    CleanupTxCtx *ctx = (CleanupTxCtx *)ud;
    ctx->c_cleanups++;
    /* cleanup 留下未清除 Python 异常：外层 set_cil_callbacks 必须返回 -1 并保留异常与新回调所有权。 */
    PyErr_SetString(PyExc_ValueError, "old cleanup failed");
}

static int
verify_cil_callback_cleanup_reentrancy(PyInterpreterState *interp, CleanupTxCtx *ctx)
{
    ctx->interp = interp;
    RpKernelCilImportCallbacks cb_a = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelCilImportCallbacks),
        mock_cil_create, mock_cil_exec, reentrant_tx_cleanup, ctx
    };
    RpKernelCilImportCallbacks cb_c = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelCilImportCallbacks),
        mock_cil_create, mock_cil_exec, raising_tx_cleanup, ctx
    };
    RpKernelCilImportCallbacks cb_d = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelCilImportCallbacks),
        mock_cil_create, mock_cil_exec, count_d_cleanup, ctx
    };
    RpKernelCilImportCallbacks cb_e = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelCilImportCallbacks),
        mock_cil_create, mock_cil_exec, count_e_cleanup, ctx
    };

    PROBE_CHECK(rp_kernel_import_set_cil_callbacks(interp, &cb_a) == 0 && !PyErr_Occurred(),
                "install cb_a cleanly");
    PROBE_CHECK(rp_kernel_import_set_cil_callbacks(interp, &cb_c) == 0 && !PyErr_Occurred(),
                "replace cb_a with cb_c must return 0 without pending exc");
    PROBE_CHECK(ctx->a_cleanups == 1, "cb_a cleanup must run once");
    PROBE_CHECK(ctx->freeze_rc == -1 && ctx->freeze_err_match == 1,
                "re-entrant freeze inside cleanup must fail with RuntimeError");
    PROBE_CHECK(ctx->replace_rc == -1 && ctx->replace_err_match == 1,
                "re-entrant set_cil_callbacks inside cleanup must fail with RuntimeError");
    PROBE_CHECK(ctx->register_rc == -1 && ctx->register_err_match == 1,
                "re-entrant register inside cleanup must fail with RuntimeError");
    PROBE_CHECK(ctx->b_cleanups == 0 && rp_kernel_import_is_frozen(interp) == 0,
                "re-entrant freeze/replace must not mutate frozen state or steal cb_b ownership");

    /* 旧 cleanup 留下活跃 Python 异常时，外层安装 cb_d 必须返回 -1，保持异常且不接管 cb_d。 */
    PROBE_CHECK(rp_kernel_import_set_cil_callbacks(interp, &cb_d) == -1 &&
                    PyErr_Occurred() != NULL && PyErr_ExceptionMatches(PyExc_ValueError),
                "raising cleanup must abort set_cil_callbacks with preserved ValueError");
    PyErr_Clear();
    PROBE_CHECK(ctx->c_cleanups == 1 && ctx->d_cleanups == 0,
                "raising cleanup runs cb_c once and keeps cb_d ownership with caller");

    PROBE_CHECK(rp_kernel_import_set_cil_callbacks(interp, &cb_e) == 0 && !PyErr_Occurred(),
                "install cb_e cleanly after aborted replacement");
    return 0;
}

static int
mode_registry_negative(const char *ext_dir, CleanupTxCtx *tx_ctx)
{
    PyInterpreterState *interp = PyInterpreterState_Get();
    char so_path[1024];
    snprintf(so_path, sizeof(so_path), "%s/ext_sp_basic.so", ext_dir);

    /* 1. 坏 ABI 版本、错误 struct_size、NULL 指针、非法模块名、缺少 origin、非包填 package_dir */
    RpKernelImportEntry bad_ver = {
        99u, (uint32_t)sizeof(RpKernelImportEntry), RYUPY_IMPORT_KIND_EXTENSION, 0, "x", so_path, NULL
    };
    PROBE_CHECK(rp_kernel_import_register(interp, &bad_ver) == -1 &&
                    PyErr_ExceptionMatches(PyExc_ValueError),
                "bad abi_version must fail with ValueError");
    PyErr_Clear();

    RpKernelImportEntry bad_sz = {
        RYUPY_IMPORT_ABI_VERSION, 4u, RYUPY_IMPORT_KIND_EXTENSION, 0, "x", so_path, NULL
    };
    PROBE_CHECK(rp_kernel_import_register(interp, &bad_sz) == -1 &&
                    PyErr_ExceptionMatches(PyExc_ValueError),
                "bad struct_size must fail with ValueError");
    PyErr_Clear();

    PROBE_CHECK(rp_kernel_import_register(NULL, &bad_ver) == -1 &&
                    PyErr_ExceptionMatches(PyExc_ValueError),
                "NULL interp must fail");
    PyErr_Clear();

    const char *bad_names[] = {"", ".lead", "trail.", "a..b"};
    for (size_t i = 0; i < sizeof(bad_names) / sizeof(bad_names[0]); i++) {
        RpKernelImportEntry e = {
            RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelImportEntry),
            RYUPY_IMPORT_KIND_CIL, 0, bad_names[i], NULL, NULL
        };
        PROBE_CHECK(rp_kernel_import_register(interp, &e) == -1 &&
                        PyErr_ExceptionMatches(PyExc_ValueError),
                    "invalid module name must fail");
        PyErr_Clear();
    }

    RpKernelImportEntry nonpkg_dir = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelImportEntry),
        RYUPY_IMPORT_KIND_CIL, 0, "nonpkg_with_dir", NULL, "/tmp/res"
    };
    PROBE_CHECK(rp_kernel_import_register(interp, &nonpkg_dir) == -1 &&
                    PyErr_ExceptionMatches(PyExc_ValueError),
                "non-package with package_dir must fail");
    PyErr_Clear();

    RpKernelImportEntry no_origin = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelImportEntry),
        RYUPY_IMPORT_KIND_EXTENSION, 0, "no_origin_ext", NULL, NULL
    };
    PROBE_CHECK(rp_kernel_import_register(interp, &no_origin) == -1 &&
                    PyErr_ExceptionMatches(PyExc_ValueError),
                "extension without origin must fail");
    PyErr_Clear();

    RpKernelCilImportCallbacks bad_cb = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelCilImportCallbacks),
        NULL, NULL, NULL, NULL
    };
    PROBE_CHECK(rp_kernel_import_set_cil_callbacks(interp, &bad_cb) == -1 &&
                    PyErr_ExceptionMatches(PyExc_ValueError),
                "CIL callback without exec_module must fail");
    PyErr_Clear();

    if (verify_cil_callback_cleanup_reentrancy(interp, tx_ctx) != 0) {
        return 1;
    }

    /* 2. 正常注册后检测重复注册与未冻结前拒绝导入 */
    RpKernelImportEntry good = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelImportEntry),
        RYUPY_IMPORT_KIND_EXTENSION, 0, "ext_sp_basic", so_path, NULL
    };
    PROBE_CHECK(rp_kernel_import_register(interp, &good) == 0, "register ext_sp_basic");
    PROBE_CHECK(rp_kernel_import_register(interp, &good) == -1 &&
                    PyErr_ExceptionMatches(PyExc_ValueError),
                "duplicate registration must fail");
    PyErr_Clear();
    PROBE_CHECK(rp_kernel_import_is_frozen(interp) == 0, "must not be frozen yet");
    PROBE_CHECK(rp_kernel_import_entry_count(interp) == 1, "entry count must be 1");

    /* 未冻结前禁止加载注册扩展，但内建 math 仍可加载 */
    PyObject *unfrozen = PyImport_ImportModule("ext_sp_basic");
    PROBE_CHECK(unfrozen == NULL && PyErr_ExceptionMatches(PyExc_ImportError),
                "importing registered extension before freeze must fail");
    PyErr_Clear();
    PROBE_CHECK(PyDict_GetItemString(PyImport_GetModuleDict(), "ext_sp_basic") == NULL,
                "unfrozen import failure must not pollute sys.modules");
    PyObject *math_mod = PyImport_ImportModule("math");
    PROBE_CHECK(math_mod != NULL, "builtin math must work before freeze");
    Py_DECREF(math_mod);

    /* 3. 冻结后禁止继续注册或替换回调，已注册扩展正常加载 */
    PROBE_CHECK(rp_kernel_import_freeze(interp) == 0, "freeze failed");
    PROBE_CHECK(rp_kernel_import_is_frozen(interp) == 1, "is_frozen != 1");

    RpKernelImportEntry post_freeze = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelImportEntry),
        RYUPY_IMPORT_KIND_CIL, 0, "late_mod", NULL, NULL
    };
    PROBE_CHECK(rp_kernel_import_register(interp, &post_freeze) == -1 &&
                    PyErr_ExceptionMatches(PyExc_RuntimeError),
                "register after freeze must fail");
    PyErr_Clear();

    RpKernelCilImportCallbacks late_cb = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelCilImportCallbacks),
        mock_cil_create, mock_cil_exec, NULL, NULL
    };
    PROBE_CHECK(rp_kernel_import_set_cil_callbacks(interp, &late_cb) == -1 &&
                    PyErr_ExceptionMatches(PyExc_RuntimeError),
                "set_cil_callbacks after freeze must fail");
    PyErr_Clear();

    PyObject *sp = PyImport_ImportModule("ext_sp_basic");
    PROBE_CHECK(sp != NULL, "ext_sp_basic must load after freeze");
    Py_DECREF(sp);
    return 0;
}

static PyObject *
cil_atexit_cb(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    PyObject *m = PyImport_ImportModule("cil_atexit");
    if (m == NULL) return NULL;
    PyObject *v = PyObject_GetAttrString(m, "CIL_ATEXIT");
    Py_DECREF(m);
    if (v == NULL) return NULL;
    long val = PyLong_AsLong(v);
    Py_DECREF(v);
    if (val != 999) {
        PyErr_SetString(PyExc_AssertionError, "cil_atexit.CIL_ATEXIT != 999");
        return NULL;
    }
    Py_RETURN_NONE;
}

static PyMethodDef cil_atexit_def = {"cil_atexit_cb", cil_atexit_cb, METH_NOARGS, NULL};

static int
register_cil_entry_ex(PyInterpreterState *interp, const char *name, int is_pkg,
                      const char *package_dir)
{
    RpKernelImportEntry e = {
        RYUPY_IMPORT_ABI_VERSION,
        (uint32_t)sizeof(RpKernelImportEntry),
        RYUPY_IMPORT_KIND_CIL,
        is_pkg,
        name,
        NULL,
        package_dir,
    };
    return rp_kernel_import_register(interp, &e);
}

static int
register_cil_entry(PyInterpreterState *interp, const char *name, int is_pkg)
{
    return register_cil_entry_ex(interp, name, is_pkg, NULL);
}

static int
mode_cil_mixed_callbacks(const char *ext_dir, CilMockState *st)
{
    PyInterpreterState *interp = PyInterpreterState_Get();
    RpKernelCilImportCallbacks cb = {
        RYUPY_IMPORT_ABI_VERSION,
        (uint32_t)sizeof(RpKernelCilImportCallbacks),
        mock_cil_create,
        mock_cil_exec,
        mock_cil_cleanup,
        st,
    };
    PROBE_CHECK(rp_kernel_import_set_cil_callbacks(interp, &cb) == 0, "set cil callbacks");

    char cil_res_dir[1024];
    snprintf(cil_res_dir, sizeof(cil_res_dir), "%s/cil_pkg_res", ext_dir);
    PROBE_CHECK(register_cil_entry_ex(interp, "cil_pkg", 1, cil_res_dir) == 0, "reg cil_pkg");
    PROBE_CHECK(register_cil_entry(interp, "cil_pkg.cil_helper", 0) == 0, "reg cil_helper");
    PROBE_CHECK(register_cil_entry(interp, "cil_retry", 0) == 0, "reg cil_retry");
    PROBE_CHECK(register_cil_entry(interp, "cil_cyc_a", 0) == 0, "reg cil_cyc_a");
    PROBE_CHECK(register_cil_entry(interp, "cil_cyc_b", 0) == 0, "reg cil_cyc_b");
    PROBE_CHECK(register_cil_entry(interp, "cil_atexit", 0) == 0, "reg cil_atexit");

    char csub_so[1024], calls_cil_so[1024];
    snprintf(csub_so, sizeof(csub_so), "%s/cil_pkg/c_sub.so", ext_dir);
    snprintf(calls_cil_so, sizeof(calls_cil_so), "%s/ext_calls_cil.so", ext_dir);
    RpKernelImportEntry ext_csub = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelImportEntry),
        RYUPY_IMPORT_KIND_EXTENSION, 0, "cil_pkg.c_sub", csub_so, NULL
    };
    RpKernelImportEntry ext_rev = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelImportEntry),
        RYUPY_IMPORT_KIND_EXTENSION, 0, "ext_calls_cil", calls_cil_so, NULL
    };
    PROBE_CHECK(rp_kernel_import_register(interp, &ext_csub) == 0, "reg cil_pkg.c_sub");
    PROBE_CHECK(rp_kernel_import_register(interp, &ext_rev) == 0, "reg ext_calls_cil");
    PROBE_CHECK(rp_kernel_import_freeze(interp) == 0, "freeze cil mixed");

    /* 1. CIL 包 + package_dir 独立资源路径（不污染 __file__/has_location） + .so 子模块 */
    PyObject *pkg = probe_import_from("cil_pkg", "c_sub");
    PROBE_CHECK(pkg != NULL, "from cil_pkg import c_sub");
    PROBE_CHECK(!PyObject_HasAttrString(pkg, "__file__"), "CIL package must not set __file__");
    PyObject *pkg_path = PyObject_GetAttrString(pkg, "__path__");
    PROBE_CHECK(pkg_path != NULL && PyList_Check(pkg_path) && PyList_Size(pkg_path) == 1,
                "cil_pkg.__path__ must hold package_dir");
    const char *p0 = PyUnicode_AsUTF8(PyList_GetItem(pkg_path, 0));
    PROBE_CHECK(p0 != NULL && strcmp(p0, cil_res_dir) == 0, "cil_pkg.__path__[0] != cil_res_dir");
    Py_DECREF(pkg_path);
    PyObject *pkg_spec = PyObject_GetAttrString(pkg, "__spec__");
    PROBE_CHECK(pkg_spec != NULL, "cil_pkg.__spec__");
    PyObject *pkg_has_loc = PyObject_GetAttrString(pkg_spec, "has_location");
    PROBE_CHECK(pkg_has_loc == Py_False, "cil_pkg.__spec__.has_location must be False");
    Py_DECREF(pkg_has_loc);
    PyObject *pkg_loader = PyObject_GetAttrString(pkg_spec, "loader");
    PROBE_CHECK(pkg_loader != NULL &&
                    PyObject_CallMethod(pkg_loader, "get_resource_reader", "s", "cil_pkg") == NULL &&
                    PyErr_ExceptionMatches(PyExc_NotImplementedError),
                "CIL loader unpromised get_resource_reader must raise NotImplementedError");
    PyErr_Clear();
    Py_DECREF(pkg_loader);
    Py_DECREF(pkg_spec);
    PyObject *csub = PyObject_GetAttrString(pkg, "c_sub");
    PROBE_CHECK(csub != NULL, "cil_pkg.c_sub attached");
    PyObject *cval = PyObject_GetAttrString(csub, "C_SUB_VAL");
    PROBE_CHECK(cval != NULL && PyLong_AsLong(cval) == 505, "C_SUB_VAL != 505");
    Py_DECREF(cval);
    PROBE_CHECK(st->pkg_execs == 1, "cil_pkg exec count != 1");

    PyObject *helper_rel = PyObject_CallMethod(csub, "rel_import", "si", "cil_helper", 1);
    PROBE_CHECK(helper_rel != NULL, "c_sub relative import cil_helper");
    PROBE_CHECK(st->helper_execs == 1, "cil_helper exec count != 1");
    Py_DECREF(helper_rel);
    Py_DECREF(csub);

    /* 2. 独立 .so 扩展反向导入 CIL 回调模块，不触发二次执行 */
    PyObject *rev = PyImport_ImportModule("ext_calls_cil");
    PROBE_CHECK(rev != NULL, "import ext_calls_cil");
    PyObject *got_cil = PyObject_GetAttrString(rev, "GOT_CIL");
    PROBE_CHECK(got_cil != NULL && PyLong_AsLong(got_cil) == 701, "ext_calls_cil.GOT_CIL != 701");
    Py_DECREF(got_cil);
    PROBE_CHECK(st->helper_execs == 1, "cil_helper must stay single-exec");
    Py_DECREF(rev);

    /* 3. CIL 失败回滚与重试 */
    PyObject *r1 = PyImport_ImportModule("cil_retry");
    PROBE_CHECK(r1 == NULL && PyErr_ExceptionMatches(PyExc_RuntimeError), "cil_retry 1st fail");
    PyErr_Clear();
    PROBE_CHECK(PyDict_GetItemString(PyImport_GetModuleDict(), "cil_retry") == NULL,
                "failed CIL module must rollback sys.modules");
    PyObject *r2 = PyImport_ImportModule("cil_retry");
    PROBE_CHECK(r2 != NULL && st->retry_attempts == 2, "cil_retry 2nd succeed");
    Py_DECREF(r2);

    /* 4. CIL 同线程循环导入 */
    PyObject *ca = PyImport_ImportModule("cil_cyc_a");
    PROBE_CHECK(ca != NULL && st->cyc_a_execs == 1 && st->cyc_b_execs == 1,
                "CIL cycle must exec once each");
    Py_DECREF(ca);

    /* 5. CIL 包 fromlist 缺失跳过与 None 穿透 */
    PyObject *miss = probe_import_from("cil_pkg", "missing_cil_sub");
    PROBE_CHECK(miss == pkg, "CIL package fromlist missing ignored");
    Py_DECREF(miss);
    PROBE_CHECK(PyDict_SetItemString(PyImport_GetModuleDict(), "cil_pkg.blocked", Py_None) == 0,
                "set None block in cil_pkg");
    PROBE_CHECK(probe_import_from("cil_pkg", "blocked") == NULL &&
                    PyErr_ExceptionMatches(PyExc_ModuleNotFoundError),
                "CIL package fromlist None block must propagate");
    PyErr_Clear();
    Py_DECREF(pkg);

    /* 6. 注册 atexit 回调验证关机前表未被误清，cleanup 仅在 _PyImport_FiniCore 调用 */
    PyObject *atexit_mod = PyImport_ImportModule("atexit");
    PROBE_CHECK(atexit_mod != NULL, "import atexit");
    PyObject *fn = PyCFunction_NewEx(&cil_atexit_def, NULL, NULL);
    PyObject *reg_res = fn ? PyObject_CallMethod(atexit_mod, "register", "O", fn) : NULL;
    Py_XDECREF(fn);
    Py_DECREF(atexit_mod);
    PROBE_CHECK(reg_res != NULL, "register cil atexit callback");
    Py_DECREF(reg_res);
    PROBE_CHECK(st->cleanup_calls == 0, "cleanup must not run before finalize");
    return 0;
}

int
main(int argc, char **argv)
{
    probe_capture_diagnostics();
    if (argc != 4) {
        fprintf(stderr, "usage: cil_import_probe PREFIX EXT_DIR MODE\n");
        return 2;
    }
    const char *home = argv[1];
    const char *ext_dir = argv[2];
    const char *mode = argv[3];

    if (probe_begin_strict_kernel() != 0) return 1;
    /* 明确合同：Py_Initialize 前（preinit）调用 postinit 导入 ABI 必须纯 C 返回 -1 且不崩溃。 */
    RpKernelImportEntry pre_entry = {
        RYUPY_IMPORT_ABI_VERSION, (uint32_t)sizeof(RpKernelImportEntry),
        RYUPY_IMPORT_KIND_CIL, 0, "preinit_rejected", NULL, NULL
    };
    PROBE_CHECK(rp_kernel_import_register(NULL, &pre_entry) == -1, "preinit register must return -1");
    PROBE_CHECK(rp_kernel_import_freeze(NULL) == -1, "preinit freeze must return -1");
    PROBE_CHECK(rp_kernel_import_is_frozen(NULL) == -1, "preinit is_frozen must return -1");
    PROBE_CHECK(rp_kernel_import_entry_count(NULL) == -1, "preinit entry_count must return -1");

    PyStatus st;
    if (probe_init_python(home, 1, "utf-8", "surrogateescape", NULL, NULL, 0, 1, &st) != 0) {
        fprintf(stderr, "init failed: %s\n", st.err_msg ? st.err_msg : "unknown");
        return 1;
    }
    if (probe_enter_runtime() != 0) return 1;

    CilMockState cil_state;
    CleanupTxCtx tx_ctx;
    memset(&cil_state, 0, sizeof(cil_state));
    memset(&tx_ctx, 0, sizeof(tx_ctx));

    int rc = 2;
    if (strcmp(mode, "registry-negative") == 0) {
        rc = mode_registry_negative(ext_dir, &tx_ctx);
    }
    else if (strcmp(mode, "cil-mixed-callbacks") == 0) {
        rc = mode_cil_mixed_callbacks(ext_dir, &cil_state);
    }
    if (rc != 0) return rc;

    if (probe_enter_shutdown() != 0) return 1;
    PROBE_CHECK(Py_FinalizeEx() == 0, "Py_FinalizeEx failed");
    if (strcmp(mode, "registry-negative") == 0) {
        PROBE_CHECK(tx_ctx.d_cleanups == 0 && tx_ctx.e_cleanups == 1,
                    "aborted cb_d must never be cleaned; final cb_e cleans once");
    }
    else if (strcmp(mode, "cil-mixed-callbacks") == 0) {
        PROBE_CHECK(cil_state.atexit_execs == 1, "atexit CIL import did not run");
        PROBE_CHECK(cil_state.cleanup_calls == 1, "CIL cleanup must run once in _PyImport_FiniCore");
    }
    if (probe_assert_zero_bytecode(mode) != 0) return 1;
    printf("PASS %s\n", mode);
    return 0;
}
