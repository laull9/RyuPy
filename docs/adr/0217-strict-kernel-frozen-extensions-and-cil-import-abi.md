# ADR-0217：严格内核显式冻结扩展注册表、纯 C ModuleSpec 与 CIL 导入事务 ABI

- 日期：2026-11-16
- 状态：C4-1 源码与探针准备就绪（待构建窗口实测）；多版本仍限定 CPython 3.12.13，Windows/Linux 待验
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、[ADR-0216](0216-strict-kernel-native-stdio.md)

## 背景

ADR-0216 在 `_install_importlib=0` 严格内核上补齐了纯 C UTF-8 codec 与 `_io` 标准流，但导入系统仍仅支持 `sys.modules` 与 `PyImport_Inittab` 内建模块。C4 路线要求在零字节码执行（startup/runtime/shutdown 三阶段入口/执行/拒绝全为 0）前提下支持真实单/多阶段原生 `.so` 扩展、纯 C `ModuleSpec`/loader/模块 `repr`、包层级与 `fromlist`/相对导入、以及 CIL 静态导入回调。同时，现有兼容产物中的 `CPImports` 已收敛为 finder/loader 形态（两个 class helper 已移除），新内核 ABI 须严守 CPython 3.12.13 既有结构二进制兼容、避免过度全局锁死锁、避免关机前过早清空注册表破坏 `atexit` 导入，并定义单一创建/初始化事务防止重复执行。

## 决定

1. **不改既有结构布局的解释器纯 C 侧表与 Postinit ABI（`Include/ryupy_import_abi.h`）**：
   - 不改动 `PyInterpreterState` 与 `struct _import_state` 任何既有字段布局，在 `Python/ryupy_import_registry.h` 维护由短互斥锁 `rp_side_table_mutex`（支持 3.12 PEP 684 独立 GIL 子解释器）保护的纯 C 解释器侧表，锁内仅查/插/摘指针，不调用 Python API。
   - 现有 `rp_kernel_import_*` ABI 明确仅支持已初始化解释器及所属 GIL 线程；`Py_InitializeFromConfig` 前（preinit）或无活动线程时返回纯 C `-1` 不触碰 `PyErr_*`（preinit staging 留待需要时再单列）。
   - `RpKernelImportEntry` 与 `RpKernelCilImportCallbacks` 均带 `abi_version` / `struct_size` 校验；`origin`（代码文件位置，`has_location` / `__file__`）与 `package_dir`（包资源/搜索目录，`submodule_search_locations` / `__path__`）分离建模，并正确处理 `/` 与 Windows `X:\` 根目录边界。
   - 侧表槽位在 `_PyImport_FiniCore`（`_PyAtExit_Call` 与 `finalize_modules` 之后）先立 `finalizing` 墓碑再调用 `cleanup(user_data)`、广播并排空全部 `active_waiters`（等待者先于重获 GIL 退出，持 GIL 线程绝不等待必须重获 GIL 的线程），保证 `atexit` 合法导入并消除重入重建与 UAF。
2. **复用官方 3.12.13 原生加载机制**：
   - 磁盘 `.so` 扩展复用上游 `import_find_extension`、`_PyImport_LoadDynamicModuleWithSpec`（含内部 `_PyImport_SwapPackageContext`）、`PyModule_FromDefAndSpec` 与 `exec_builtin_or_dynamic`，不手写 `dlopen` 绕过缓存。
3. **纯 C `ModuleSpec`、Loader 与模块 `repr`**：
   - 实现纯 C `RpModuleSpec`（含 `cached` / `__cached__` 与比较完整支持）、`BuiltinImporter` 类对象、`ExtensionFileLoader` 与 `CilModuleLoader`（未承诺的 `get_data` / `get_resource_reader` 显式抛 `NotImplementedError`）。
   - `Objects/moduleobject.c` 的 `module_repr` 在 `IMPORTLIB(interp) == NULL` 时直连纯 C `_PyImport_RyuPyModuleRepr`，原 `import_guard_probe` 的 `repr` 负例契约随真实支持转正。
4. **每模块细粒度可重入锁、等待图死锁检测与异常/NUL 防护**：
   - 使用按模块名划分的 `RpModuleLockNode`（`has_owner` 显式标记，内嵌 NUL 字节拒绝，`blocked` 空槽及时释放），先放 `table_mutex` 再放 GIL 休眠；等待图 DFS 检测跨线程环路即刻抛 `RuntimeError`（`ensure_initialized` 按 `_lock_unlock_module` 语义接受部分初始化模块）。
5. **CIL 单一事务与 `fromlist`/相对导入**：
   - CIL 回调采用 `create_module` + `exec_module` 单一事务，由内核唯一写入 `sys.modules`、推动 `exec_module` 一次并统一回滚；子模块完成后按 `parent_name` 重取 `sys.modules` 当前父模块再挂载属性；`fromlist` 与相对导入完全对齐 CPython 3.12.13。

## 边界与后果

- 本切仅完成内核源码与探针准备，实际 `configure/make` 构建与 `--with-stock-diff` 差分验证待主代理通知构建窗口开放后执行。
- C4-2（host/C# 接入）为独立后续任务；多版本边界仍为 CPython 3.12.13，Windows 与 Linux 平台验证待办。
