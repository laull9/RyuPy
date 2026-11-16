/* RyuPy C4-1 真实 .so 扩展导入差分探针（支持 RyuPy 严格内核与未修改 CPython 3.12.13）。
 *
 * 用法：ext_import_probe <prefix> <ext_dir> <mode>
 * 五个模式均可在严格内核（strict_reject=1，三阶段计数全 0）与 `-DRYUPY_STOCK_CPYTHON` 下同次验证：
 * 1. ext-basic-spec      : 单/多阶段 .so、ModuleSpec/loader/repr、sys.modules 单一性与 None 阻塞、
 *                          自定义 Py_mod_create 与多阶段失败回滚重试。
 * 2. ext-packages-fromlist: 父子包、__path__/__package__、fromlist 缺失跳过与内部缺依赖/None 穿透、
 *                          相对导入 level=1/2/3 与无父包越界报错。
 * 3. ext-cycles-threads  : 同线程循环、跨线程同模块等待单次初始化、跨线程独立并发无全局锁死锁、
 *                          跨线程环路死锁检测不无限挂起。
 * 4. ext-errors-bad-abi  : 未知模块、缺 .so 文件、缺 PyInit_、返回 NULL/未清异常/坏类型/未初始化 def/负 m_size。
 * 5. ext-atexit          : Py_FinalizeEx 期间在 atexit 回调内合法导入未加载过的 .so 扩展。
 */
#include "probe_ext_common.h"
#include <unistd.h>

static int
check_str_attr(PyObject *obj, const char *attr, const char *expected)
{
    PyObject *v = PyObject_GetAttrString(obj, attr);
    PROBE_CHECK(v != NULL && PyUnicode_Check(v), "expected str attribute");
    const char *s = PyUnicode_AsUTF8(v);
    int eq = (s != NULL && strcmp(s, expected) == 0);
    Py_DECREF(v);
    PROBE_CHECK(eq, "str attribute value mismatch");
    return 0;
}

static int
check_bool_attr(PyObject *obj, const char *attr, int expected)
{
    PyObject *v = PyObject_GetAttrString(obj, attr);
    PROBE_CHECK(v != NULL, "missing bool attribute");
    int is_true = PyObject_IsTrue(v);
    Py_DECREF(v);
    PROBE_CHECK(is_true == expected, "bool attribute mismatch");
    return 0;
}

static int
check_repr_equals(PyObject *obj, const char *expected)
{
    PyObject *r = PyObject_Repr(obj);
    PROBE_CHECK(r != NULL && PyUnicode_Check(r), "PyObject_Repr failed");
    const char *s = PyUnicode_AsUTF8(r);
    int eq = (s != NULL && strcmp(s, expected) == 0);
    if (!eq) {
        fprintf(stderr, "repr mismatch: got=%s expected=%s\n", s ? s : "(null)", expected);
    }
    Py_DECREF(r);
    PROBE_CHECK(eq, "module repr mismatch");
    return 0;
}

static int
mode_ext_basic_spec(const char *ext_dir)
{
    /* 1. 内建 itertools 的 ModuleSpec 与 repr（stock 3.12 与严格内核同为 PyImport_Inittab 内建） */
    PyObject *builtin_mod = PyImport_ImportModule("itertools");
    PROBE_CHECK(builtin_mod != NULL, "import itertools");
    PyObject *builtin_spec = PyObject_GetAttrString(builtin_mod, "__spec__");
    PROBE_CHECK(builtin_spec != NULL && builtin_spec != Py_None, "itertools.__spec__");
    if (check_str_attr(builtin_spec, "name", "itertools")) return 1;
    if (check_str_attr(builtin_spec, "origin", "built-in")) return 1;
    if (check_str_attr(builtin_spec, "parent", "")) return 1;
    if (check_bool_attr(builtin_spec, "has_location", 0)) return 1;
    if (check_bool_attr(builtin_spec, "_initializing", 0)) return 1;
    if (check_repr_equals(builtin_mod, "<module 'itertools' (built-in)>")) return 1;
    PyObject *builtin_loader = PyObject_GetAttrString(builtin_spec, "loader");
    PROBE_CHECK(builtin_loader != NULL && PyType_Check(builtin_loader),
                "BuiltinImporter __loader__ must be a type/class object");
    PyObject *builtin_is_pkg = PyObject_CallMethod(builtin_loader, "is_package", "s", "itertools");
    PROBE_CHECK(builtin_is_pkg == Py_False, "BuiltinImporter.is_package('itertools') != False");
    Py_XDECREF(builtin_is_pkg);
    Py_DECREF(builtin_loader);
    Py_DECREF(builtin_spec);
    Py_DECREF(builtin_mod);

    /* 2. 无 spec 普通模块 repr 契约 */
    PyObject *plain = PyImport_AddModule("plain_repr");
    PROBE_CHECK(plain != NULL, "add plain_repr");
    if (check_repr_equals(plain, "<module 'plain_repr'>")) return 1;

    /* 3. 单阶段 m_size=-1 + 缓存复用 + None 阻塞 */
    PyObject *sp = PyImport_ImportModule("ext_sp_basic");
    PyObject *sp2 = PyImport_ImportModule("ext_sp_basic");
    PROBE_CHECK(sp != NULL && sp == sp2, "ext_sp_basic single instance");
    Py_DECREF(sp2);

    PyObject *ping = PyObject_CallMethod(sp, "ping", "l", 5L);
    PROBE_CHECK(ping != NULL && PyLong_AsLong(ping) == 15, "ext_sp_basic.ping(5) != 15");
    Py_DECREF(ping);

    char expected_origin[1024];
    char expected_repr[1100];
    snprintf(expected_origin, sizeof(expected_origin), "%s/ext_sp_basic.so", ext_dir);
    snprintf(expected_repr, sizeof(expected_repr), "<module 'ext_sp_basic' from '%s'>", expected_origin);
    if (check_repr_equals(sp, expected_repr)) return 1;
    if (check_str_attr(sp, "__file__", expected_origin)) return 1;
    if (check_str_attr(sp, "__package__", "")) return 1;

    PyObject *sp_spec = PyObject_GetAttrString(sp, "__spec__");
    PROBE_CHECK(sp_spec != NULL, "ext_sp_basic.__spec__");
    if (check_str_attr(sp_spec, "origin", expected_origin)) return 1;
    if (check_bool_attr(sp_spec, "has_location", 1)) return 1;
    if (check_str_attr(sp_spec, "parent", "")) return 1;
    PyObject *sp_loader = PyObject_GetAttrString(sp_spec, "loader");
    PROBE_CHECK(sp_loader != NULL && sp_loader != Py_None, "sp_loader");
    if (check_str_attr(sp_loader, "name", "ext_sp_basic")) return 1;
    if (check_str_attr(sp_loader, "path", expected_origin)) return 1;
    Py_DECREF(sp_loader);
    Py_DECREF(sp_spec);

    /* pop sys.modules 后重新导入，应命中上游 extension 缓存而不重复执行 PyInit_ */
    PyObject *modules = PyImport_GetModuleDict();
    PROBE_CHECK(PyDict_DelItemString(modules, "ext_sp_basic") == 0, "del sys.modules[ext_sp_basic]");
    PyObject *sp_cached = PyImport_ImportModule("ext_sp_basic");
    PROBE_CHECK(sp_cached != NULL, "re-import ext_sp_basic from extension cache");
    PyObject *icnt = PyObject_CallMethod(sp_cached, "init_count", NULL);
    PROBE_CHECK(icnt != NULL && PyLong_AsLong(icnt) == 1, "m_size=-1 must reuse extension cache");
    Py_DECREF(icnt);
    Py_DECREF(sp_cached);
    Py_DECREF(sp);

    /* sys.modules[name] = None 阻塞（覆盖扩展与内建模块） */
    PROBE_CHECK(PyDict_SetItemString(modules, "ext_sp_basic", Py_None) == 0, "block ext_sp_basic");
    PyObject *blocked = PyImport_ImportModule("ext_sp_basic");
    PROBE_CHECK(blocked == NULL && PyErr_ExceptionMatches(PyExc_ModuleNotFoundError),
                "None in sys.modules must raise ModuleNotFoundError");
    PyErr_Clear();
    PROBE_CHECK(PyDict_GetItemString(modules, "ext_sp_basic") == Py_None, "None entry preserved");

    PROBE_CHECK(PyDict_SetItemString(modules, "itertools", Py_None) == 0, "block itertools");
    PROBE_CHECK(PyImport_ImportModule("itertools") == NULL && PyErr_ExceptionMatches(PyExc_ModuleNotFoundError),
                "None in sys.modules must block builtin itertools too");
    PyErr_Clear();

    /* 4. 单阶段 m_size=0 与 m_size>0 */
    PyObject *reinit = PyImport_ImportModule("ext_sp_reinit");
    PROBE_CHECK(reinit != NULL, "import ext_sp_reinit");
    Py_DECREF(reinit);
    PyObject *stateful = PyImport_ImportModule("ext_sp_state");
    PROBE_CHECK(stateful != NULL, "import ext_sp_state");
    PyObject *magic = PyObject_CallMethod(stateful, "get_magic", NULL);
    PROBE_CHECK(magic != NULL && PyLong_AsLong(magic) == 777, "ext_sp_state magic != 777");
    Py_DECREF(magic);
    Py_DECREF(stateful);

    /* 5. 多阶段 Py_mod_create + Py_mod_exec 与自定义非模块返回 */
    PyObject *mp = PyImport_ImportModule("ext_mp");
    PROBE_CHECK(mp != NULL, "import ext_mp");
    PyObject *counts = PyObject_CallMethod(mp, "get_counts", NULL);
    PROBE_CHECK(counts != NULL && PyTuple_Size(counts) == 2 &&
                    PyLong_AsLong(PyTuple_GetItem(counts, 0)) == 1 &&
                    PyLong_AsLong(PyTuple_GetItem(counts, 1)) == 1,
                "ext_mp create/exec counts != (1, 1)");
    Py_DECREF(counts);
    PyObject *sv = PyObject_CallMethod(mp, "get_state_val", NULL);
    PROBE_CHECK(sv != NULL && PyLong_AsLong(sv) == 314, "ext_mp state != 314");
    Py_DECREF(sv);
    Py_DECREF(mp);

    PyObject *custom = PyImport_ImportModule("ext_mp_custom");
    PROBE_CHECK(custom != NULL && PyDict_Check(custom), "ext_mp_custom must return dict");
    PyObject *marker = PyDict_GetItemString(custom, "custom_marker");
    PROBE_CHECK(marker != NULL && PyLong_AsLong(marker) == 555, "custom_marker != 555");
    Py_DECREF(custom);

    /* 6. 多阶段失败回滚与二次重试成功 */
    PyObject *retry1 = PyImport_ImportModule("ext_mp_retry");
    PROBE_CHECK(retry1 == NULL && PyErr_ExceptionMatches(PyExc_RuntimeError),
                "ext_mp_retry 1st attempt must fail");
    PyErr_Clear();
    PROBE_CHECK(PyDict_GetItemString(modules, "ext_mp_retry") == NULL,
                "failed multi-phase import must rollback sys.modules");
    PyObject *retry2 = PyImport_ImportModule("ext_mp_retry");
    PROBE_CHECK(retry2 != NULL, "ext_mp_retry 2nd attempt must succeed");
    PyObject *ok_val = PyObject_GetAttrString(retry2, "OK");
    PROBE_CHECK(ok_val != NULL && PyLong_AsLong(ok_val) == 200, "ext_mp_retry.OK != 200");
    Py_DECREF(ok_val);
    Py_DECREF(retry2);
    return 0;
}

static int
mode_ext_packages_fromlist(void)
{
    PyObject *modules = PyImport_GetModuleDict();

    /* 1. 深层点分导入返回顶层包并挂载子属性 */
    PyObject *top = PyImport_ImportModuleLevel("cpkg.subpkg.leaf", NULL, NULL, NULL, 0);
    PROBE_CHECK(top != NULL, "import cpkg.subpkg.leaf");
    if (check_str_attr(top, "PKG_TAG", "cpkg")) return 1;
    if (check_str_attr(top, "__package__", "cpkg")) return 1;
    PyObject *subpkg = PyObject_GetAttrString(top, "subpkg");
    PROBE_CHECK(subpkg != NULL, "cpkg.subpkg attached");
    PyObject *leaf = PyObject_GetAttrString(subpkg, "leaf");
    PROBE_CHECK(leaf != NULL, "cpkg.subpkg.leaf attached");
    if (check_str_attr(leaf, "__package__", "cpkg.subpkg")) return 1;

    /* 2. fromlist 正常子模块 + 缺失子模块跳过 + * (__all__) */
    PyObject *from_ok = probe_import_from("cpkg", "csub");
    PROBE_CHECK(from_ok == top && PyObject_HasAttrString(top, "csub"), "from cpkg import csub");
    Py_DECREF(from_ok);

    PyObject *from_missing = probe_import_from("cpkg", "no_such_sub");
    PROBE_CHECK(from_missing == top, "fromlist missing submodule must be ignored");
    Py_DECREF(from_missing);

    PyObject *from_star = probe_import_from("cpkg", "*");
    PROBE_CHECK(from_star == top, "from cpkg import *");
    Py_DECREF(from_star);

    /* 非字符串 fromlist 项报 TypeError */
    PyObject *bad_fl = Py_BuildValue("[i]", 123);
    PyObject *bad_res = PyImport_ImportModuleLevel("cpkg", NULL, NULL, bad_fl, 0);
    Py_DECREF(bad_fl);
    PROBE_CHECK(bad_res == NULL && PyErr_ExceptionMatches(PyExc_TypeError),
                "non-str fromlist item must raise TypeError");
    PyErr_Clear();

    /* 3. fromlist 内部缺依赖穿透 + 回滚 */
    PyObject *broken = probe_import_from("cpkg", "broken_dep");
    PROBE_CHECK(broken == NULL && PyErr_ExceptionMatches(PyExc_ModuleNotFoundError),
                "fromlist inner missing dep must propagate ModuleNotFoundError");
    PyObject *exc = PyErr_GetRaisedException();
    PyObject *exc_name = PyObject_GetAttrString(exc, "name");
    const char *exc_name_s = exc_name ? PyUnicode_AsUTF8(exc_name) : NULL;
    PROBE_CHECK(exc_name_s != NULL && strcmp(exc_name_s, "nonexistent_inner_dep") == 0,
                "penetrated ModuleNotFoundError.name mismatch");
    Py_DECREF(exc_name);
    Py_DECREF(exc);
    PROBE_CHECK(PyDict_GetItemString(modules, "cpkg.broken_dep") == NULL,
                "broken_dep must be rolled back from sys.modules");

    /* 4. fromlist None 阻塞穿透 */
    PROBE_CHECK(PyDict_SetItemString(modules, "cpkg.blocked_sub", Py_None) == 0, "set None block");
    PyObject *blk = probe_import_from("cpkg", "blocked_sub");
    PROBE_CHECK(blk == NULL && PyErr_ExceptionMatches(PyExc_ModuleNotFoundError),
                "fromlist None-blocked submodule must propagate ModuleNotFoundError");
    PyErr_Clear();

    /* 5. 相对导入（level=1, level=2, 越界 level=3, 无父包 level=1） */
    PyObject *rel1 = PyObject_CallMethod(leaf, "rel_import", "si", "sibling", 1);
    PROBE_CHECK(rel1 != NULL, "relative level=1 sibling");
    PyObject *sib_val = PyObject_GetAttrString(rel1, "SIBLING");
    PROBE_CHECK(sib_val != NULL && PyLong_AsLong(sib_val) == 99, "sibling.SIBLING != 99");
    Py_DECREF(sib_val);
    Py_DECREF(rel1);

    PyObject *rel2 = PyObject_CallMethod(leaf, "rel_import", "si", "csub", 2);
    PROBE_CHECK(rel2 != NULL, "relative level=2 csub");
    Py_DECREF(rel2);

    PyObject *rel_oob = PyObject_CallMethod(leaf, "rel_import", "si", "x", 3);
    PROBE_CHECK(rel_oob == NULL && PyErr_ExceptionMatches(PyExc_ImportError),
                "relative import beyond top-level must raise ImportError");
    PyErr_Clear();

    PyObject *sp = PyImport_ImportModule("ext_sp_basic");
    PROBE_CHECK(sp != NULL, "import ext_sp_basic for no-parent relative test");
    PyObject *sp_globals = PyModule_GetDict(sp);
    PyObject *rel_noparent = PyImport_ImportModuleLevel("x", sp_globals, NULL, NULL, 1);
    PROBE_CHECK(rel_noparent == NULL && PyErr_ExceptionMatches(PyExc_ImportError),
                "relative import without parent package must raise ImportError");
    PyErr_Clear();
    Py_DECREF(sp);

    Py_DECREF(leaf);
    Py_DECREF(subpkg);
    Py_DECREF(top);
    return 0;
}

typedef struct {
    const char *mod_name;
    PyObject *result_mod;
    int had_error;
    int was_runtime_error;
} ThreadImportArgs;

static void *
thread_import_worker(void *raw)
{
    ThreadImportArgs *args = (ThreadImportArgs *)raw;
    PyGILState_STATE gstate = PyGILState_Ensure();
    args->result_mod = PyImport_ImportModule(args->mod_name);
    if (args->result_mod == NULL) {
        args->had_error = 1;
        args->was_runtime_error = PyErr_ExceptionMatches(PyExc_RuntimeError);
        PyErr_Clear();
    }
    PyGILState_Release(gstate);
    return NULL;
}

static int
mode_ext_cycles_threads(void)
{
    ProbeSyncCtx ctx;
    probe_sync_init(&ctx);
    PROBE_CHECK(probe_install_sync_hook(&ctx) == 0, "install sync hook");

    /* 1. 同线程循环导入 */
    PyObject *ca = PyImport_ImportModule("mp_cycle_a");
    PROBE_CHECK(ca != NULL, "same-thread cycle import");
    PyObject *ca_after = PyObject_GetAttrString(ca, "AFTER");
    PROBE_CHECK(ca_after != NULL && PyLong_AsLong(ca_after) == 33, "mp_cycle_a.AFTER != 33");
    Py_DECREF(ca_after);
    PyObject *ca_cnt = PyObject_CallMethod(ca, "exec_count", NULL);
    PROBE_CHECK(ca_cnt != NULL && PyLong_AsLong(ca_cnt) == 1, "mp_cycle_a exec_count != 1");
    Py_DECREF(ca_cnt);
    Py_DECREF(ca);

    /* 2. 跨线程同模块竞争：验证单次初始化与同一模块身份 */
    ThreadImportArgs slow1 = {"mp_slow", NULL, 0, 0};
    ThreadImportArgs slow2 = {"mp_slow", NULL, 0, 0};
    pthread_t t1, t2;
    PROBE_CHECK(pthread_create(&t1, NULL, thread_import_worker, &slow1) == 0, "spawn slow1");
    Py_BEGIN_ALLOW_THREADS
    pthread_mutex_lock(&ctx.mu);
    while (!ctx.slow_started) {
        probe_timed_wait(&ctx.cv, &ctx.mu, 5);
    }
    pthread_mutex_unlock(&ctx.mu);
    Py_END_ALLOW_THREADS

    PROBE_CHECK(pthread_create(&t2, NULL, thread_import_worker, &slow2) == 0, "spawn slow2");
    Py_BEGIN_ALLOW_THREADS
    usleep(50000);
    pthread_mutex_lock(&ctx.mu);
    ctx.slow_release = 1;
    pthread_cond_broadcast(&ctx.cv);
    pthread_mutex_unlock(&ctx.mu);
    pthread_join(t1, NULL);
    pthread_join(t2, NULL);
    Py_END_ALLOW_THREADS

    PROBE_CHECK(!slow1.had_error && !slow2.had_error, "concurrent mp_slow import failed");
    PROBE_CHECK(slow1.result_mod != NULL && slow1.result_mod == slow2.result_mod,
                "concurrent mp_slow must return identical module");
    PyObject *scnt = PyObject_CallMethod(slow1.result_mod, "exec_count", NULL);
    PROBE_CHECK(scnt != NULL && PyLong_AsLong(scnt) == 1, "mp_slow must exec once");
    Py_DECREF(scnt);
    Py_DECREF(slow1.result_mod);
    Py_DECREF(slow2.result_mod);

    /* 3. 跨线程独立模块并发：线程 1 导入 A 等线程 2 导入 B，绝不因过度全局锁死锁 */
    ThreadImportArgs ca_arg = {"mp_coord_a", NULL, 0, 0};
    ThreadImportArgs cb_arg = {"mp_coord_b", NULL, 0, 0};
    PROBE_CHECK(pthread_create(&t1, NULL, thread_import_worker, &ca_arg) == 0, "spawn coord_a");
    Py_BEGIN_ALLOW_THREADS
    pthread_mutex_lock(&ctx.mu);
    while (!ctx.coord_a_waiting) {
        probe_timed_wait(&ctx.cv, &ctx.mu, 5);
    }
    pthread_mutex_unlock(&ctx.mu);
    Py_END_ALLOW_THREADS
    PROBE_CHECK(pthread_create(&t2, NULL, thread_import_worker, &cb_arg) == 0, "spawn coord_b");
    Py_BEGIN_ALLOW_THREADS
    pthread_join(t1, NULL);
    pthread_join(t2, NULL);
    Py_END_ALLOW_THREADS
    PROBE_CHECK(!ca_arg.had_error && !cb_arg.had_error, "parallel independent import deadlocked");
    Py_XDECREF(ca_arg.result_mod);
    Py_XDECREF(cb_arg.result_mod);

    /* 4. 跨线程单阶段死锁检测：不无限阻塞，其中一线程收到 RuntimeError 死锁提示 */
    ThreadImportArgs sdl1 = {"sp_dl_a", NULL, 0, 0};
    ThreadImportArgs sdl2 = {"sp_dl_b", NULL, 0, 0};
    PROBE_CHECK(pthread_create(&t1, NULL, thread_import_worker, &sdl1) == 0, "spawn sp_dl_a");
    PROBE_CHECK(pthread_create(&t2, NULL, thread_import_worker, &sdl2) == 0, "spawn sp_dl_b");
    Py_BEGIN_ALLOW_THREADS
    pthread_join(t1, NULL);
    pthread_join(t2, NULL);
    Py_END_ALLOW_THREADS
    PROBE_CHECK((sdl1.had_error && sdl1.was_runtime_error && !sdl2.had_error) ||
                    (sdl2.had_error && sdl2.was_runtime_error && !sdl1.had_error),
                "cross-thread single-phase cycle must detect deadlock with RuntimeError");
    Py_XDECREF(sdl1.result_mod);
    Py_XDECREF(sdl2.result_mod);

    /* 5. 跨线程多阶段已发布循环：不无限阻塞，双方按 _lock_unlock_module 语义收敛 */
    ThreadImportArgs mdl1 = {"mp_dl_a", NULL, 0, 0};
    ThreadImportArgs mdl2 = {"mp_dl_b", NULL, 0, 0};
    PROBE_CHECK(pthread_create(&t1, NULL, thread_import_worker, &mdl1) == 0, "spawn mp_dl_a");
    PROBE_CHECK(pthread_create(&t2, NULL, thread_import_worker, &mdl2) == 0, "spawn mp_dl_b");
    Py_BEGIN_ALLOW_THREADS
    pthread_join(t1, NULL);
    pthread_join(t2, NULL);
    Py_END_ALLOW_THREADS
    PROBE_CHECK(!mdl1.had_error && !mdl2.had_error && mdl1.result_mod && mdl2.result_mod,
                "cross-thread multi-phase published cycle must finish cleanly");
    Py_DECREF(mdl1.result_mod);
    Py_DECREF(mdl2.result_mod);

    probe_sync_destroy(&ctx);
    return 0;
}

static int
mode_ext_errors_bad_abi(void)
{
    PyObject *unk = PyImport_ImportModule("unknown_xyz");
    PROBE_CHECK(unk == NULL && PyErr_ExceptionMatches(PyExc_ModuleNotFoundError),
                "unknown module must raise ModuleNotFoundError");
    PyErr_Clear();

    PyObject *miss = PyImport_ImportModule("bad_missing");
    PROBE_CHECK(miss == NULL && PyErr_ExceptionMatches(PyExc_ImportError),
                "missing .so file must raise ImportError");
    PyErr_Clear();

    PyObject *nosym = PyImport_ImportModule("bad_nosym");
    PROBE_CHECK(nosym == NULL && PyErr_ExceptionMatches(PyExc_ImportError),
                "missing PyInit_ symbol must raise ImportError");
    PyErr_Clear();

    /* 内嵌 NUL 模块名禁止截断碰撞已注册的 ext_sp_basic，必须明确报错。 */
    PyObject *nul_name = PyUnicode_FromStringAndSize("ext_sp_basic\0extra", 18);
    PROBE_CHECK(nul_name != NULL, "alloc embedded NUL name");
    PyObject *nul_mod = PyImport_ImportModuleLevelObject(nul_name, NULL, NULL, NULL, 0);
    Py_DECREF(nul_name);
    PROBE_CHECK(nul_mod == NULL && PyErr_Occurred() != NULL,
                "embedded NUL module name must fail");
    PyErr_Clear();

    const char *syserr_mods[] = {
        "bad_null", "bad_unreported", "bad_type", "bad_uninit_def", "bad_neg_size"
    };
    for (size_t i = 0; i < sizeof(syserr_mods) / sizeof(syserr_mods[0]); i++) {
        PyObject *bad = PyImport_ImportModule(syserr_mods[i]);
        PROBE_CHECK(bad == NULL && PyErr_ExceptionMatches(PyExc_SystemError),
                    "bad ABI extension must raise SystemError");
        PyErr_Clear();
        PROBE_CHECK(PyDict_GetItemString(PyImport_GetModuleDict(), syserr_mods[i]) == NULL,
                    "bad ABI extension must not remain in sys.modules");
    }
    return 0;
}

static int atexit_callback_ran = 0;

static PyObject *
probe_atexit_handler(PyObject *Py_UNUSED(self), PyObject *Py_UNUSED(unused))
{
    PyObject *mod = PyImport_ImportModule("ext_atexit");
    if (mod == NULL) {
        return NULL;
    }
    PyObject *val = PyObject_GetAttrString(mod, "ATEXIT_LOADED");
    Py_DECREF(mod);
    if (val == NULL) {
        return NULL;
    }
    long v = PyLong_AsLong(val);
    Py_DECREF(val);
    if (v != 888) {
        PyErr_SetString(PyExc_AssertionError, "ext_atexit.ATEXIT_LOADED != 888");
        return NULL;
    }
    atexit_callback_ran = 1;
    Py_RETURN_NONE;
}

static PyMethodDef atexit_cb_def = {"probe_atexit_handler", probe_atexit_handler, METH_NOARGS, NULL};

static int
mode_ext_atexit(void)
{
    PyObject *atexit_mod = PyImport_ImportModule("atexit");
    PROBE_CHECK(atexit_mod != NULL, "import atexit");
    PyObject *fn = PyCFunction_NewEx(&atexit_cb_def, NULL, NULL);
    PROBE_CHECK(fn != NULL, "create atexit C function");
    PyObject *res = PyObject_CallMethod(atexit_mod, "register", "O", fn);
    Py_DECREF(fn);
    Py_DECREF(atexit_mod);
    PROBE_CHECK(res != NULL, "atexit.register failed");
    Py_DECREF(res);
    return 0;
}

int
main(int argc, char **argv)
{
    probe_capture_diagnostics();
    if (argc != 4) {
        fprintf(stderr, "usage: ext_import_probe PREFIX EXT_DIR MODE\n");
        return 2;
    }
    const char *home = argv[1];
    const char *ext_dir = argv[2];
    const char *mode = argv[3];

    if (probe_begin_strict_kernel() != 0) return 1;
    PyStatus st;
    if (probe_init_python(home, 1, "utf-8", "surrogateescape", NULL, NULL, 0, 1, &st) != 0) {
        fprintf(stderr, "init failed: %s\n", st.err_msg ? st.err_msg : "unknown");
        return 1;
    }
    if (probe_setup_extensions(ext_dir, 1) != 0) {
        if (PyErr_Occurred()) PyErr_Print();
        return 1;
    }
    if (probe_enter_runtime() != 0) return 1;

    int rc = 2;
    if (strcmp(mode, "ext-basic-spec") == 0) rc = mode_ext_basic_spec(ext_dir);
    else if (strcmp(mode, "ext-packages-fromlist") == 0) rc = mode_ext_packages_fromlist();
    else if (strcmp(mode, "ext-cycles-threads") == 0) rc = mode_ext_cycles_threads();
    else if (strcmp(mode, "ext-errors-bad-abi") == 0) rc = mode_ext_errors_bad_abi();
    else if (strcmp(mode, "ext-atexit") == 0) rc = mode_ext_atexit();
    if (rc != 0) return rc;

    if (probe_enter_shutdown() != 0) return 1;
    PROBE_CHECK(Py_FinalizeEx() == 0, "Py_FinalizeEx failed");
    if (strcmp(mode, "ext-atexit") == 0) {
        PROBE_CHECK(atexit_callback_ran == 1, "atexit import callback did not succeed");
    }
    if (probe_assert_zero_bytecode(mode) != 0) return 1;
    printf("PASS %s\n", mode);
    return 0;
}
