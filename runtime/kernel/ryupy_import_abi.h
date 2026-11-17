/* RyuPy 严格零字节码内核 C4-1 扩展与 CIL 导入 C ABI 头文件。
 *
 * 本文件定义宿主/嵌入端向严格内核注册冻结扩展、静态 CIL 导入回调及查询冻结状态
 * 所需的公开 C ABI：
 * 1. 所有条目与回调均带显式 `abi_version` 与 `struct_size` 校验，坏 ABI 明确拒绝。
 * 2. 所有注册、回调安装与冻结均要求解释器已初始化（`Py_IsInitialized() != 0`）且当前
 *    线程持有归属于目标 `PyInterpreterState *` 的 GIL；若在 `Py_InitializeFromConfig`
 *    之前（preinit）或无活动解释器状态下调用，返回纯 C 错误码 -1（不触碰 `PyErr_*`）。
 * 3. 状态存放于不改动任何 CPython 既有结构体布局的 per-interpreter 纯 C 侧表，
 *    并在 `_PyImport_FiniCore` 阶段释放，确保 `atexit` 阶段仍可执行合法导入。
 * 4. 本头文件由锚点机制注入 `Include/ryupy_import_abi.h`，并由 `Include/ryupy_kernel.h`
 *    统一包含导出。
 */
#ifndef Py_RYUPY_IMPORT_ABI_H
#define Py_RYUPY_IMPORT_ABI_H

#include "Python.h"
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#define RYUPY_IMPORT_ABI_VERSION 1u

/* 冻结导入注册表条目类型：磁盘原生扩展（.so/.pyd）或静态 CIL 编译模块/包。 */
#define RYUPY_IMPORT_KIND_EXTENSION 1
#define RYUPY_IMPORT_KIND_CIL       2

/* 单个显式冻结导入条目描述符；所有字符串由内核复制持有，调用方调用后可释放。
 * 注意：`origin` 表示模块代码文件位置（影响 `has_location` 与 `__file__`）；
 * `package_dir` 表示包的搜索/资源目录（仅影响 `submodule_search_locations` / `__path__`，
 * 不赋予 `__file__`），使无 `.py` CIL 包也能携带独立资源目录。 */
typedef struct {
    uint32_t abi_version;   /* 必须等于 RYUPY_IMPORT_ABI_VERSION */
    uint32_t struct_size;   /* 必须等于 sizeof(RpKernelImportEntry) */
    int kind;               /* RYUPY_IMPORT_KIND_EXTENSION 或 RYUPY_IMPORT_KIND_CIL */
    int is_package;         /* 非 0 表示包（赋予 __path__ 与包级 __package__） */
    const char *name;       /* 完整点分模块名（UTF-8，非空、无 NUL、段名合法） */
    const char *origin;     /* 显式扩展文件位置（EXTENSION 必填；CIL 通常为 NULL） */
    const char *package_dir;/* 可选包搜索/资源目录（仅 is_package != 0 时允许非空） */
} RpKernelImportEntry;

/* CIL 模块创建钩子（可选）：仅负责构造模块对象并返回值，禁止写入 sys.modules 或执行模块体。
 * 返回 Py_None 或未提供该钩子时，由内核调用 PyModule_NewObject 创建标准模块。 */
typedef PyObject *(*rp_cil_create_module_fn)(
    PyInterpreterState *interp,
    const char *name,
    PyObject *spec,
    int is_package,
    void *user_data);

/* CIL 模块初始化执行钩子（必填）：在内核已将模块发布至 sys.modules 并置位 _initializing=True
 * 后调用，且同一加载事务内仅调用一次；返回 0 表示成功，返回 -1 表示失败（内核自动回滚）。 */
typedef int (*rp_cil_exec_module_fn)(
    PyInterpreterState *interp,
    PyObject *module,
    PyObject *spec,
    void *user_data);

/* CIL 回调上下文释放钩子（可选）：随解释器销毁在 _PyImport_FiniCore 阶段调用（atexit 之后）。 */
typedef void (*rp_cil_callback_cleanup_fn)(void *user_data);

/* 静态 CIL 导入回调槽配置：定义单一模块创建/初始化事务，避免内核与托管端重复执行。 */
typedef struct {
    uint32_t abi_version;   /* 必须等于 RYUPY_IMPORT_ABI_VERSION */
    uint32_t struct_size;   /* 必须等于 sizeof(RpKernelCilImportCallbacks) */
    rp_cil_create_module_fn create_module;
    rp_cil_exec_module_fn exec_module;
    rp_cil_callback_cleanup_fn cleanup;
    void *user_data;
} RpKernelCilImportCallbacks;

/* 向已初始化目标解释器的未冻结注册表追加一条扩展或 CIL 模块条目；失败返回 -1。 */
PyAPI_FUNC(int) rp_kernel_import_register(
    PyInterpreterState *interp,
    const RpKernelImportEntry *entry);

/* 为已初始化目标解释器安装静态 CIL 导入回调槽（仅冻结前允许；替换前在事务保护下先调用旧 cleanup）。
 * cleanup 执行期间禁止重入 register / set_cil_callbacks / freeze；若旧 cleanup 留下活跃 Python
 * 异常，本次安装中止并返回 -1（保留异常且新 callbacks 所有权仍归调用方）。 */
PyAPI_FUNC(int) rp_kernel_import_set_cil_callbacks(
    PyInterpreterState *interp,
    const RpKernelCilImportCallbacks *callbacks);

/* 冻结已初始化目标解释器的导入注册表与 CIL 回调槽；冻结后禁止再注册，只允许按冻结表路由。 */
PyAPI_FUNC(int) rp_kernel_import_freeze(PyInterpreterState *interp);

/* 查询目标解释器导入表状态：已冻结返回 1，未冻结返回 0，未初始化/归属错误返回 -1。 */
PyAPI_FUNC(int) rp_kernel_import_is_frozen(PyInterpreterState *interp);

/* 查询目标解释器已注册的冻结导入条目总数；未初始化/归属错误返回 -1。 */
PyAPI_FUNC(Py_ssize_t) rp_kernel_import_entry_count(PyInterpreterState *interp);

#ifdef __cplusplus
}
#endif
#endif /* Py_RYUPY_IMPORT_ABI_H */
