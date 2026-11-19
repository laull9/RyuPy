"""CPython 3.12.13 版本锁定、求值器审计/拒绝门与内建 C 导入路由锚点集合。

本模块定义针对官方 CPython 3.12.13 源码树的前半组结构变换：
1. `Include/patchlevel.h`：精确锁定 3.12.13 版本号，不接受其他版本静默套用。
2. `Include/ryupy_kernel.h`：声明三阶段默认求值器入口计数与严格拒绝开关 C ABI。
3. `Python/ceval.c`：注入原子三阶段状态并在 `_PyEval_EvalFrameDefault` 入口计次/拦截。
4. `Python/import.c`：在 `_install_importlib=0`（`IMPORTLIB(interp)==NULL`）下提供
   内建 C 模块路由 `rp_kernel_import_builtin_only`，并显式拒绝未支持消费者。
"""
from __future__ import annotations

from transform import AnchorEdit, NewHeaderFile


RYUPY_KERNEL_HEADER = NewHeaderFile(
    rel_path="Include/ryupy_kernel.h",
    content="""/* RyuPy 严格零字节码内核原型 C ABI 头文件（CPython 3.12.13 首切专用）。
 * 提供三阶段（startup/runtime/shutdown）默认求值器入口计数与严格拒绝执行开关。 */
#ifndef Py_RYUPY_KERNEL_H
#define Py_RYUPY_KERNEL_H

#include "Python.h"
#include <stdint.h>
#include "ryupy_import_abi.h"

#ifdef __cplusplus
extern "C" {
#endif

/* 与 RyuPy C0 审计及 tests/cil_execution/check_audit.py 对齐的生命周期三阶段。 */
#define RYUPY_KERNEL_PHASE_STARTUP  0
#define RYUPY_KERNEL_PHASE_RUNTIME  1
#define RYUPY_KERNEL_PHASE_SHUTDOWN 2

/* 查询内核原型版本标识；返回静态只读字符串，调用方不可释放。 */
PyAPI_FUNC(const char *) rp_kernel_version(void);

/* 设置或查询当前生命周期阶段（0=startup, 1=runtime, 2=shutdown）；非法阶段返回 -1。 */
PyAPI_FUNC(int) rp_kernel_set_phase(int phase);
PyAPI_FUNC(int) rp_kernel_get_phase(void);

/* 设置或查询严格拒绝字节码执行开关（非 0 开启：进入 _PyEval_EvalFrameDefault 直接报错拒绝）。 */
PyAPI_FUNC(void) rp_kernel_set_strict_reject(int enabled);
PyAPI_FUNC(int) rp_kernel_get_strict_reject(void);

/* 清零三阶段入口、实际执行与拒绝计数；不影响当前 phase 与 strict_reject 开关。 */
PyAPI_FUNC(void) rp_kernel_reset_eval_stats(void);

/* 读取指定阶段的默认求值器入口计数、实际进入字节码执行计数与被严格模式拒绝计数。 */
PyAPI_FUNC(int64_t) rp_kernel_eval_entry_count(int phase);
PyAPI_FUNC(int64_t) rp_kernel_eval_executed_count(int phase);
PyAPI_FUNC(int64_t) rp_kernel_eval_rejected_count(int phase);
PyAPI_FUNC(int64_t) rp_kernel_eval_total_entries(void);
PyAPI_FUNC(int64_t) rp_kernel_eval_total_rejected(void);

#ifdef __cplusplus
}
#endif
#endif /* Py_RYUPY_KERNEL_H */
""",
)


CEVAL_STATE_AND_API = """\
#include <ctype.h>
#include <stdbool.h>
#include <stdatomic.h>
#include "ryupy_kernel.h"

/* RyuPy 严格内核原型：三阶段默认求值器入口计数与严格拒绝开关（进程级原子状态）。 */
static _Atomic int rp_kernel_phase = RYUPY_KERNEL_PHASE_STARTUP;
static _Atomic int rp_kernel_strict_reject_flag = 0;
static _Atomic int64_t rp_kernel_eval_entries[3] = {0, 0, 0};
static _Atomic int64_t rp_kernel_eval_executed[3] = {0, 0, 0};
static _Atomic int64_t rp_kernel_eval_rejected[3] = {0, 0, 0};

static inline int
rp_kernel_active_phase_slot(void)
{
    int phase = atomic_load_explicit(&rp_kernel_phase, memory_order_acquire);
    return (phase >= RYUPY_KERNEL_PHASE_STARTUP && phase <= RYUPY_KERNEL_PHASE_SHUTDOWN)
        ? phase
        : RYUPY_KERNEL_PHASE_RUNTIME;
}

const char *
rp_kernel_version(void)
{
    return "ryupy-cpython-3.12.13-kernel-proto-v1";
}

int
rp_kernel_set_phase(int phase)
{
    if (phase < RYUPY_KERNEL_PHASE_STARTUP || phase > RYUPY_KERNEL_PHASE_SHUTDOWN) {
        return -1;
    }
    atomic_store_explicit(&rp_kernel_phase, phase, memory_order_release);
    return 0;
}

int
rp_kernel_get_phase(void)
{
    return atomic_load_explicit(&rp_kernel_phase, memory_order_acquire);
}

void
rp_kernel_set_strict_reject(int enabled)
{
    atomic_store_explicit(&rp_kernel_strict_reject_flag, enabled ? 1 : 0, memory_order_release);
}

int
rp_kernel_get_strict_reject(void)
{
    return atomic_load_explicit(&rp_kernel_strict_reject_flag, memory_order_acquire);
}

void
rp_kernel_reset_eval_stats(void)
{
    for (int i = 0; i < 3; i++) {
        atomic_store_explicit(&rp_kernel_eval_entries[i], 0, memory_order_release);
        atomic_store_explicit(&rp_kernel_eval_executed[i], 0, memory_order_release);
        atomic_store_explicit(&rp_kernel_eval_rejected[i], 0, memory_order_release);
    }
}

int64_t
rp_kernel_eval_entry_count(int phase)
{
    if (phase < RYUPY_KERNEL_PHASE_STARTUP || phase > RYUPY_KERNEL_PHASE_SHUTDOWN) {
        return -1;
    }
    return atomic_load_explicit(&rp_kernel_eval_entries[phase], memory_order_acquire);
}

int64_t
rp_kernel_eval_executed_count(int phase)
{
    if (phase < RYUPY_KERNEL_PHASE_STARTUP || phase > RYUPY_KERNEL_PHASE_SHUTDOWN) {
        return -1;
    }
    return atomic_load_explicit(&rp_kernel_eval_executed[phase], memory_order_acquire);
}

int64_t
rp_kernel_eval_rejected_count(int phase)
{
    if (phase < RYUPY_KERNEL_PHASE_STARTUP || phase > RYUPY_KERNEL_PHASE_SHUTDOWN) {
        return -1;
    }
    return atomic_load_explicit(&rp_kernel_eval_rejected[phase], memory_order_acquire);
}

int64_t
rp_kernel_eval_total_entries(void)
{
    int64_t total = 0;
    for (int i = 0; i < 3; i++) {
        total += atomic_load_explicit(&rp_kernel_eval_entries[i], memory_order_acquire);
    }
    return total;
}

int64_t
rp_kernel_eval_total_rejected(void)
{
    int64_t total = 0;
    for (int i = 0; i < 3; i++) {
        total += atomic_load_explicit(&rp_kernel_eval_rejected[i], memory_order_acquire);
    }
    return total;
}"""


IMPORT_BUILTIN_ROUTER = """\
/* _install_importlib=0 C4-1 完整导入实现：置于 import.c 内部 static 辅助函数之后，
 * 复用上游 create_builtin / exec_builtin_or_dynamic / import_find_extension /
 * _PyImport_LoadDynamicModuleWithSpec 与每模块锁。 */
#include "ryupy_import_loaders.h"
#include "ryupy_import_helpers.h"
#include "ryupy_import_router.h"
#include "ryupy_import_api_impl.h"

static PyObject *
import_find_and_load(PyThreadState *tstate, PyObject *abs_name)"""


def build_eval_import_anchors() -> list[AnchorEdit]:
    """返回版本锁定、ceval 计数/拦截门及 import C 内建路由所需的精确锚点。"""
    return [
        AnchorEdit(
            rel_path="Include/patchlevel.h",
            name="verify_cpython_3_12_13_version",
            old=(
                "#define PY_MAJOR_VERSION        3\n"
                "#define PY_MINOR_VERSION        12\n"
                "#define PY_MICRO_VERSION        13\n"
                "#define PY_RELEASE_LEVEL        PY_RELEASE_LEVEL_FINAL\n"
                "#define PY_RELEASE_SERIAL       0\n\n"
                "/* Version as a string */\n"
                '#define PY_VERSION              "3.12.13"'
            ),
            new=(
                "#define PY_MAJOR_VERSION        3\n"
                "#define PY_MINOR_VERSION        12\n"
                "#define PY_MICRO_VERSION        13\n"
                "#define PY_RELEASE_LEVEL        PY_RELEASE_LEVEL_FINAL\n"
                "#define PY_RELEASE_SERIAL       0\n\n"
                "/* Version as a string */\n"
                '#define PY_VERSION              "3.12.13"'
            ),
        ),
        AnchorEdit(
            rel_path="Python/ceval.c",
            name="ceval_inject_kernel_api",
            old="#include <ctype.h>\n#include <stdbool.h>",
            new=CEVAL_STATE_AND_API,
        ),
        AnchorEdit(
            rel_path="Python/ceval.c",
            name="ceval_default_entry_counter",
            old=(
                "PyObject* _Py_HOT_FUNCTION\n"
                "_PyEval_EvalFrameDefault(PyThreadState *tstate, _PyInterpreterFrame *frame, int throwflag)\n"
                "{\n"
                "    _Py_EnsureTstateNotNULL(tstate);\n"
                "    CALL_STAT_INC(pyeval_calls);"
            ),
            new=(
                "PyObject* _Py_HOT_FUNCTION\n"
                "_PyEval_EvalFrameDefault(PyThreadState *tstate, _PyInterpreterFrame *frame, int throwflag)\n"
                "{\n"
                "    _Py_EnsureTstateNotNULL(tstate);\n"
                "    int rp_phase = rp_kernel_active_phase_slot();\n"
                "    atomic_fetch_add_explicit(&rp_kernel_eval_entries[rp_phase], 1, memory_order_relaxed);\n"
                "    CALL_STAT_INC(pyeval_calls);"
            ),
        ),
        AnchorEdit(
            rel_path="Python/ceval.c",
            name="ceval_default_strict_reject_gate",
            old=(
                "    tstate->c_recursion_remaining -= (PY_EVAL_C_STACK_UNITS - 1);\n"
                '    if (_Py_EnterRecursiveCallTstate(tstate, "")) {\n'
                "        tstate->c_recursion_remaining--;\n"
                "        tstate->py_recursion_remaining--;\n"
                "        goto exit_unwind;\n"
                "    }"
            ),
            new=(
                "    tstate->c_recursion_remaining -= (PY_EVAL_C_STACK_UNITS - 1);\n"
                "    if (atomic_load_explicit(&rp_kernel_strict_reject_flag, memory_order_acquire)) {\n"
                "        atomic_fetch_add_explicit(&rp_kernel_eval_rejected[rp_phase], 1, memory_order_relaxed);\n"
                "        tstate->c_recursion_remaining--;\n"
                "        tstate->py_recursion_remaining--;\n"
                "        PyObject *rp_prior_error = _PyErr_GetRaisedException(tstate);\n"
                "        _PyErr_SetString(tstate, PyExc_RuntimeError,\n"
                '                         "CPython default bytecode evaluator is disabled in strict RyuPy kernel mode");\n'
                "        _PyErr_ChainExceptions1(rp_prior_error);\n"
                "        goto exit_unwind;\n"
                "    }\n"
                '    if (_Py_EnterRecursiveCallTstate(tstate, "")) {\n'
                "        tstate->c_recursion_remaining--;\n"
                "        tstate->py_recursion_remaining--;\n"
                "        goto exit_unwind;\n"
                "    }\n"
                "    atomic_fetch_add_explicit(&rp_kernel_eval_executed[rp_phase], 1, memory_order_relaxed);"
            ),
        ),
        AnchorEdit(
            rel_path="Python/import.c",
            name="import_ensure_initialized_guard",
            old=(
                "    if (busy) {\n"
                "        /* Wait until module is done importing. */\n"
                "        PyObject *value = _PyObject_CallMethodOneArg(\n"
                "            IMPORTLIB(interp), &_Py_ID(_lock_unlock_module), name);\n"
                "        if (value == NULL) {\n"
                "            return -1;\n"
                "        }\n"
                "        Py_DECREF(value);\n"
                "    }"
            ),
            new=(
                "    if (busy) {\n"
                "        if (IMPORTLIB(interp) == NULL) {\n"
                "            return rp_kernel_ensure_initialized(interp, mod, name);\n"
                "        }\n"
                "        /* Wait until module is done importing. */\n"
                "        PyObject *value = _PyObject_CallMethodOneArg(\n"
                "            IMPORTLIB(interp), &_Py_ID(_lock_unlock_module), name);\n"
                "        if (value == NULL) {\n"
                "            return -1;\n"
                "        }\n"
                "        Py_DECREF(value);\n"
                "    }"
            ),
        ),
        AnchorEdit(
            rel_path="Python/import.c",
            name="import_inject_builtin_router",
            old="static PyObject *\nimport_find_and_load(PyThreadState *tstate, PyObject *abs_name)",
            new=IMPORT_BUILTIN_ROUTER,
        ),
        AnchorEdit(
            rel_path="Python/import.c",
            name="import_find_and_load_route_null_importlib",
            old=(
                "    mod = PyObject_CallMethodObjArgs(IMPORTLIB(interp), &_Py_ID(_find_and_load),\n"
                "                                     abs_name, IMPORT_FUNC(interp), NULL);"
            ),
            new=(
                "    if (IMPORTLIB(interp) == NULL) {\n"
                "        mod = rp_kernel_import_find_and_load(tstate, abs_name);\n"
                "    }\n"
                "    else {\n"
                "        mod = PyObject_CallMethodObjArgs(IMPORTLIB(interp), &_Py_ID(_find_and_load),\n"
                "                                         abs_name, IMPORT_FUNC(interp), NULL);\n"
                "    }"
            ),
        ),
        AnchorEdit(
            rel_path="Python/import.c",
            name="import_level_object_guard_fromlist",
            old=(
                "        if (path) {\n"
                "            Py_DECREF(path);\n"
                "            final_mod = PyObject_CallMethodObjArgs(\n"
                "                        IMPORTLIB(interp), &_Py_ID(_handle_fromlist),\n"
                "                        mod, fromlist, IMPORT_FUNC(interp), NULL);\n"
                "        }"
            ),
            new=(
                "        if (path) {\n"
                "            Py_DECREF(path);\n"
                "            if (IMPORTLIB(interp) == NULL) {\n"
                "                final_mod = rp_kernel_handle_fromlist(\n"
                "                            tstate, mod, fromlist, IMPORT_FUNC(interp), 0);\n"
                "            }\n"
                "            else {\n"
                "                final_mod = PyObject_CallMethodObjArgs(\n"
                "                            IMPORTLIB(interp), &_Py_ID(_handle_fromlist),\n"
                "                            mod, fromlist, IMPORT_FUNC(interp), NULL);\n"
                "            }\n"
                "        }"
            ),
        ),
        AnchorEdit(
            rel_path="Python/import.c",
            name="import_reload_guard",
            old=(
                "    PyObject *importlib = PyImport_GetModule(&_Py_ID(importlib));\n"
                "    if (importlib == NULL) {"
            ),
            new=(
                "    if (IMPORTLIB(_PyInterpreterState_GET()) == NULL) {\n"
                "        PyErr_SetString(PyExc_NotImplementedError,\n"
                '                        "module reload is disabled in RyuPy no-bytecode kernel");\n'
                "        return NULL;\n"
                "    }\n"
                "    PyObject *importlib = PyImport_GetModule(&_Py_ID(importlib));\n"
                "    if (importlib == NULL) {"
            ),
        ),
        AnchorEdit(
            rel_path="Python/import.c",
            name="import_init_core_empty_meta_path",
            old=(
                "    if (importlib) {\n"
                "        /* This call sets up builtin and frozen import support */\n"
                "        if (init_importlib(tstate, sysmod) < 0) {\n"
                '            return _PyStatus_ERR("failed to initialize importlib");\n'
                "        }\n"
                "    }\n"
                "\n"
                "    return _PyStatus_OK();"
            ),
            new=(
                "    if (importlib) {\n"
                "        /* This call sets up builtin and frozen import support */\n"
                "        if (init_importlib(tstate, sysmod) < 0) {\n"
                '            return _PyStatus_ERR("failed to initialize importlib");\n'
                "        }\n"
                "    }\n"
                "    else {\n"
                "        PyObject *meta_path = PyList_New(0);\n"
                "        if (meta_path == NULL) {\n"
                "            return _PyStatus_NO_MEMORY();\n"
                "        }\n"
                '        int rc = PySys_SetObject("meta_path", meta_path);\n'
                "        Py_DECREF(meta_path);\n"
                "        if (rc < 0) {\n"
                '            return _PyStatus_ERR("failed to initialize empty sys.meta_path");\n'
                "        }\n"
                "    }\n"
                "\n"
                "    return _PyStatus_OK();"
            ),
        ),
    ]
