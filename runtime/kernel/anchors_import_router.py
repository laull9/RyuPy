"""CPython 3.12.13 C4-1 显式冻结扩展注册表、纯 C ModuleSpec/Loader 与 CIL 回调路由锚点。

本模块定义 C4-1 导入子系统所需的新增头文件集合与精确源码锚点（与
`anchors_eval_import.py` 协同接入 `transform.py`）：
1. 注入 `Include/ryupy_import_abi.h` 与
   `Python/ryupy_import_{registry,loader_common,loaders,ext_loader,cil_loader,helpers,router,api_impl}.h`、
   `Python/ryupy_module_spec.h`。
2. `Include/internal/pycore_import.h`：保持 `struct _import_state` 与 `PyInterpreterState`
   既有布局零改动，仅声明 `_PyImport_RyuPyModuleRepr`。
3. `Objects/moduleobject.c`：`module_repr` 在 `interp->imports.importlib == NULL` 时改走
   `_PyImport_RyuPyModuleRepr`，提供纯 C 模块 repr。
4. `Python/import.c`：顶部包含 `condvar.h` 与核心声明/结构头；在 `_PyImport_Init` / `_PyImport_Fini`
   初始化和清理纯 C 侧表互斥锁；在 `_PyImport_FiniCore`（`atexit` 与 `finalize_modules`
   之后）调用 `rp_kernel_import_fini_core(interp)` 安全释放状态。
"""
from __future__ import annotations

from pathlib import Path

from transform import AnchorEdit, NewHeaderFile


def _load_kernel_header(filename: str) -> str:
    return Path(__file__).with_name(filename).read_text(encoding="utf-8")


RYUPY_IMPORT_ABI_HEADER = NewHeaderFile(
    rel_path="Include/ryupy_import_abi.h",
    content=_load_kernel_header("ryupy_import_abi.h"),
)
RYUPY_IMPORT_REGISTRY_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_import_registry.h",
    content=_load_kernel_header("ryupy_import_registry.h"),
)
RYUPY_MODULE_LOCKS_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_module_locks.h",
    content=_load_kernel_header("ryupy_module_locks.h"),
)
RYUPY_MODULE_SPEC_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_module_spec.h",
    content=_load_kernel_header("ryupy_module_spec.h"),
)
RYUPY_IMPORT_LOADER_COMMON_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_import_loader_common.h",
    content=_load_kernel_header("ryupy_import_loader_common.h"),
)
RYUPY_IMPORT_LOADERS_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_import_loaders.h",
    content=_load_kernel_header("ryupy_import_loaders.h"),
)
RYUPY_IMPORT_EXT_LOADER_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_import_ext_loader.h",
    content=_load_kernel_header("ryupy_import_ext_loader.h"),
)
RYUPY_IMPORT_CIL_LOADER_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_import_cil_loader.h",
    content=_load_kernel_header("ryupy_import_cil_loader.h"),
)
RYUPY_IMPORT_HELPERS_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_import_helpers.h",
    content=_load_kernel_header("ryupy_import_helpers.h"),
)
RYUPY_IMPORT_ROUTER_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_import_router.h",
    content=_load_kernel_header("ryupy_import_router.h"),
)
RYUPY_IMPORT_API_IMPL_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_import_api_impl.h",
    content=_load_kernel_header("ryupy_import_api_impl.h"),
)


def build_import_router_headers() -> list[NewHeaderFile]:
    """返回 C4-1 导入路由器需要注入源码树的全部新增头文件。"""
    return [
        RYUPY_IMPORT_ABI_HEADER,
        RYUPY_IMPORT_REGISTRY_HEADER,
        RYUPY_MODULE_LOCKS_HEADER,
        RYUPY_MODULE_SPEC_HEADER,
        RYUPY_IMPORT_LOADER_COMMON_HEADER,
        RYUPY_IMPORT_LOADERS_HEADER,
        RYUPY_IMPORT_EXT_LOADER_HEADER,
        RYUPY_IMPORT_CIL_LOADER_HEADER,
        RYUPY_IMPORT_HELPERS_HEADER,
        RYUPY_IMPORT_ROUTER_HEADER,
        RYUPY_IMPORT_API_IMPL_HEADER,
    ]


def build_import_router_anchors() -> list[AnchorEdit]:
    """返回 C4-1 所需的 pycore_import 声明、moduleobject 与 import.c 生命周期锚点。"""
    return [
        AnchorEdit(
            rel_path="Include/internal/pycore_import.h",
            name="pycore_import_declare_ryupy_module_repr",
            old=(
                "extern PyObject * _PyImport_ImportlibModuleRepr(\n"
                "        PyInterpreterState *interp,\n"
                "        PyObject *module);"
            ),
            new=(
                "extern PyObject * _PyImport_ImportlibModuleRepr(\n"
                "        PyInterpreterState *interp,\n"
                "        PyObject *module);\n"
                "extern PyObject * _PyImport_RyuPyModuleRepr(PyObject *module);"
            ),
        ),
        AnchorEdit(
            rel_path="Objects/moduleobject.c",
            name="moduleobject_pure_c_repr_without_importlib",
            old=(
                "static PyObject *\n"
                "module_repr(PyModuleObject *m)\n"
                "{\n"
                "    PyInterpreterState *interp = _PyInterpreterState_GET();\n"
                "    return _PyImport_ImportlibModuleRepr(interp, (PyObject *)m);\n"
                "}"
            ),
            new=(
                "static PyObject *\n"
                "module_repr(PyModuleObject *m)\n"
                "{\n"
                "    PyInterpreterState *interp = _PyInterpreterState_GET();\n"
                "    if (interp->imports.importlib == NULL) {\n"
                "        return _PyImport_RyuPyModuleRepr((PyObject *)m);\n"
                "    }\n"
                "    return _PyImport_ImportlibModuleRepr(interp, (PyObject *)m);\n"
                "}"
            ),
        ),
        AnchorEdit(
            rel_path="Python/import.c",
            name="import_include_condvar_and_abi",
            old='#include "pydtrace.h"             // PyDTrace_IMPORT_FIND_LOAD_START_ENABLED()',
            new=(
                '#include "pydtrace.h"             // PyDTrace_IMPORT_FIND_LOAD_START_ENABLED()\n'
                '#include "condvar.h"\n'
                '#include "ryupy_import_abi.h"\n'
                '#include "ryupy_import_registry.h"\n'
                '#include "ryupy_module_locks.h"\n'
                '#include "ryupy_module_spec.h"'
            ),
        ),
        AnchorEdit(
            rel_path="Python/import.c",
            name="import_init_side_table_mutex",
            old=(
                "    if (INITTAB != NULL) {\n"
                '        return _PyStatus_ERR("global import state already initialized");\n'
                "    }"
            ),
            new=(
                "    if (INITTAB != NULL) {\n"
                '        return _PyStatus_ERR("global import state already initialized");\n'
                "    }\n"
                "    rp_kernel_side_table_init();"
            ),
        ),
        AnchorEdit(
            rel_path="Python/import.c",
            name="import_fini_side_table_mutex",
            old=(
                "    /* Free memory allocated by _PyImport_Init() */\n"
                "    fini_builtin_modules_table();\n\n"
                "    PyMem_SetAllocator(PYMEM_DOMAIN_RAW, &old_alloc);"
            ),
            new=(
                "    /* Free memory allocated by _PyImport_Init() */\n"
                "    fini_builtin_modules_table();\n"
                "    rp_kernel_side_table_fini();\n\n"
                "    PyMem_SetAllocator(PYMEM_DOMAIN_RAW, &old_alloc);"
            ),
        ),
        AnchorEdit(
            rel_path="Python/import.c",
            name="import_fini_core_cleanup_ryupy_state",
            old=(
                "    if (IMPORT_LOCK(interp) != NULL) {\n"
                "        PyThread_free_lock(IMPORT_LOCK(interp));\n"
                "        IMPORT_LOCK(interp) = NULL;\n"
                "    }\n"
                "\n"
                "    _PyImport_ClearCore(interp);\n"
                "}"
            ),
            new=(
                "    if (IMPORT_LOCK(interp) != NULL) {\n"
                "        PyThread_free_lock(IMPORT_LOCK(interp));\n"
                "        IMPORT_LOCK(interp) = NULL;\n"
                "    }\n"
                "\n"
                "    rp_kernel_import_fini_core(interp);\n"
                "    _PyImport_ClearCore(interp);\n"
                "}"
            ),
        ),
    ]
