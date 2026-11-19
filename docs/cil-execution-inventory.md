# RyuPy C0 架构与执行链盘点（全 CIL 执行与 libpython 底层化）

- 日期：2026-11-16
- 阶段：C0（执行归属、启动链盘点与严格内核选型）
- 决策依据：[ADR-0208](adr/0208-cil-execution-libpython-kernel.md)、[全 CIL 实施路线](cil-execution-roadmap.md)、[架构设计](architecture.md)、[开发工作流](workflow.md)
- 回归基线配套文档：[C0 回归合同与可复现基线冻结](cil-execution-baseline.md)
- 证据标记约定：
  - **`[静态证据]`**：直接读取仓库源码、头文件、二进制符号表或构建清单所证实的事实。
  - **`[动态证据]`**：在本机固定 CPython 发行或真实工作流上实际运行并观测到的输出、帧计数、断点堆栈或错误信息；未运行项明确标注**未测 / 待验**，不臆测通过。

---

## 1. 历史盘点快照与当前源码状态

本页保留 `5d36db1e` 的 C0 历史取样；已提交首轮实现为 `cbb92535`，后续性能记录为
`21122fb6`。下表及第 2–8 节保留第二轮 `9ef7a363` 的执行盘点快照；第三轮变更见第 9.4 节。
第二轮完整检查已通过：
3384 项 C#、495 项 e2e；公共 ABI 的 1843 项基准签名全部保留。
冻结 Release 六版本产品、三形态审计和 26 项性能配对通过，详见[第二轮验收](cil-execution-second-slices-validation.md)。

| 维度 | 历史基准 `5d36db1e` | 已提交首轮 `cbb92535` | 第二轮源码 |
| :--- | :--- | :--- | :--- |
| C0 审计 | 尚无帧审计 | 固定 3.12.13 PEP 523 审计与严格拒绝门 | 保留；provider 报告 `complete=false`、`startup_complete=false` |
| C1 顶层 Python helper | 9 个 | 3 个 | 默认 1 个 `_rp_wrap_method`；显式方法候选 0 个 |
| 其它内部 Python 定义 | 2 个类、3 个成员方法、1 个方法转发闭包 | 同左 | 2 个类、3 个成员方法保留；默认另有 `_rp_method` 闭包，候选无该闭包 |
| shim 静态 `DllImport` | 67 项 | 75 项 | 85 项；另有动态取址的 `rp_build_version`，C 导出共 86 项 |
| C1 类构造 | Python 元类/构造 helper | 保留旧类构造 | 两阶段静态 shim、prepared namespace、重入身份与失败清理已定向验收，见 ADR-0213 |
| C2/C3 | 原有函数/生成器合同 | 实时默认值、显式 coroutine/await 首切 | 通用伴生核心、async with 与有限 traceback 子合同已定向验收，整阶段继续推进 |

C0 已完成执行归属、内核路线与基线盘点；C1–C5 整阶段完成门继续按第 10 节登记。

---

## 2. 源码、字节码执行入口与构建期边界盘点

### 2.1 产品运行期源码/字节码执行入口（必须归零或迁出）

| 编号 | 入口 / 通道 | 静态证据位置 | 触发条件与执行域 | 归属与处置阶段 |
| :--- | :--- | :--- | :--- | :--- |
| **E1** | `rp_run_entry` (`Py_CompileStringObject` + `PyEval_EvalCode`) | [ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c)、[Native.cs](../runtime/RyuPy.CPython/Native.cs)、[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs)、[Bootstrap.cs](../runtime/RyuPy.CPython/Bootstrap.cs)、[cpython_delegation.rs](../crates/ryupy-driver/src/cpython_delegation.rs) | 使用 `--cpython-entry` 时，将入口脚本复制到 `python/ryupy_entry/<name>`，运行时读源码文本并在 `__main__` 上下文中编译执行字节码 | **C3/C4 移除**：入口与所有应用模块编译为 CIL，取消 `entry_script` 与 `rp_run_entry` |
| **E2** | `rp_install_imports` (`PyRun_String`) | [ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c)、[Native.cs](../runtime/RyuPy.CPython/Native.cs)、[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs) | `CPythonHost.Start` 每次启动必然执行，通过 `PyRun_String` 编译并执行 `_ryupy_imports` 模块源码，导入 `sys` / `importlib.util` 并注册 finder/loader；默认还拼接方法包装 helper，`types` 导入已移除 | **C1 分切移除**：用静态 C 类型/C API/CIL 替代全部 Python helper 与 finder/loader，最终删除 `rp_install_imports` |
| **E3** | `rp_op(29, ...)` (`PyRun_String`) | [ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c) | `case 29: return PyRun_String(s, Py_file_input, PyEval_GetBuiltins(), PyEval_GetBuiltins());`；`[静态证据]` 全仓库检索 `rp_op(29` 在当前 `RyuPy.CPython` 与 `crates/` **0 处调用** | **C1 删除**：潜在任意源码求值后门，无调用者，应在 C1 直接移除该分派分支 |
| **E4** | `rp_op(1, s: name)` (`PyImport_ImportModule`) 导入 `.py` / `.pyc` | [ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c)、[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs)、[PyModules.cs](../runtime/RyuPy.Runtime/PyModules.cs)、[cpython_provider.rs](../crates/ryupy-driver/src/cpython_provider.rs) | 任何归属于 `CPythonHost.Owns(name)` 的模块（stdlib、site-packages、`--cpython-module`、`--cpython-dynamic-module`）通过 `PyModules.Import` 转发到 `PyImport_ImportModule`，执行其 `.py` / `.pyc` 字节码 | **C4 收敛**：纯 Python 标准库、第三方包装与本地模块全部编译为 CIL，仅保留原生扩展 `.so`/`.pyd` 与内建 C 模块经底层加载入口进入 |
| **E5** | `PyTextProviderMethods.Title`（内建方法间接导入执行） | [PyTextProviderMethods.cs](../runtime/RyuPy.Runtime/PyTextProviderMethods.cs)、[PyStringMethods.cs](../runtime/RyuPy.Runtime/PyStringMethods.cs)、[PyPreparedMethods.cs](../runtime/RyuPy.Runtime/PyPreparedMethods.cs) | 调用 `str.title()` 且存在 `PyProvider.Current` 时，经 `provider.Import("builtins")` 获取 `str.title` 转发执行 | **C4 移除依赖**：`str.title` 在 `RyuPy.Runtime` 原生实现 Unicode 规则，不再依赖 CPython `builtins.str` |
| **E6** | `compiled_sources` 随包 `.py` 载荷（数据锚点，非执行） | [cpython_delegation.rs](../crates/ryupy-driver/src/cpython_delegation.rs)、[cpython_delegation.rs](../crates/ryupy-driver/src/cpython_delegation.rs)、[linked_compile.rs](../crates/ryupy-driver/src/linked_compile.rs) | 将已编译源码复制到 `python/ryupy_compiled/` 并设置 `BundleRelative` 供 `__file__` / `inspect` 使用（不加入 `sys.path`） | **C2 替换**：按 [路线 §3 C2](cil-execution-roadmap.md) 独立建模源码位置、资源根与可写数据根，默认严格产品不打包 `.py` |

### 2.2 编译期/静态编译边界（不进入产品运行期）

- **构建期工具（保留在编译机）**：`[静态证据]` [cpython_discovery.rs](../crates/ryupy-driver/src/cpython_discovery.rs)、[cpython_portable.py](../crates/ryupy-driver/src/cpython_portable.py)、[cpython_dependencies.py](../crates/ryupy-driver/src/cpython_dependencies.py)、`cpython_build_tools.py` 仅在开发/构建机上用 `-I -X utf8 -S -B` 静态收集元数据（读 AST、`METADATA`、`RECORD`、`PYTHON.json`），不随产品发布，符合 [路线 §1](cil-execution-roadmap.md)。
- **动态代码内建（`eval` / `exec` / `compile`）**：`[静态证据]` [unresolved_globals.rs](../crates/ryupy-hir/src/unresolved_globals.rs) 与 [PyModuleBuiltins.cs](../runtime/RyuPy.Runtime/PyModuleBuiltins.cs) 将 `eval`、`exec`、`compile` 列为明确不支持的 builtin（`IsUnsupported` / 运行时 `NameError` 或 `NotImplementedError`），不会在原生模式或编译函数体中静默转交 CPython 解释执行。

---

## 3. 嵌入 Python helper 清单与源码实数

`[静态证据]` [CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs) 保留导入钩子源码，
默认再拼接 [CPythonNativeMethods.cs](../runtime/RyuPy.CPython/CPythonNativeMethods.cs) 的
`LegacyMethodWrapper`，统一经 `rp_install_imports` 写入 `_ryupy_imports`。
[CPythonClassBridge.cs](../runtime/RyuPy.CPython/CPythonClassBridge.cs) 缓存仍使用的真实对象指针。

| 名称 | `5d36db1e` 历史状态 | 当前源码与后续归属 |
| :--- | :--- | :--- |
| `_RyuPyLoader`：`create_module` / `exec_module` | Python 类与两个方法 | 保留；C1 下一切静态 loader。当前 create_module 回调 `PyModules.Import` 完成初始化，exec_module 空操作 |
| `_RyuPyFinder`：`find_spec` | Python 类与一个方法 | 保留；C1 下一切静态 finder，仍插入 `sys.meta_path[0]` |
| `_rp_calculate_meta` / `_rp_make_class` | 两个顶层 Python helper | 已删除；[class_construction.h](../runtime/RyuPy.CPython/native/class_construction.h) 选择元类、先 prepare、后调用真实元类，托管事务管理 namespace 与身份 |
| `_rp_wrap_method` 与内部 `_rp_method` | 顶层 factory 与一个闭包 | 默认保留；`RYUPY_CPYTHON_NATIVE_METHODS=1` 使用 NativeMethod/NativeStaticMethod，候选不安装这两个定义 |
| `_rp_object_new` / `_rp_object_init` / `_rp_object_setattr` / `_rp_object_delattr` | 四个顶层 Python helper | 已删除；直接缓存 `object` 原生描述符，见 ADR-0209 |
| `_rp_super_create` / `_rp_super_getattr` | 两个顶层 Python helper | 已删除；真实 super C API 与中性 PySuper canonical 身份，见 ADR-0211 |
| `_rp_staticmethod` / `_rp_classmethod` / `_rp_property` | 三个内建类型别名 | 保留；后续直接取得 builtin/type，和 finder/loader 分切替换 |

按嵌入字符串的 Python AST 定义计数，类体与闭包一并展开：

| 源码快照/模式 | 顶层 helper `def` | finder/loader 成员 `def` | 转发闭包 `def` | 全部 `def` | Python `class` | 描述符别名 |
| :--- | ---: | ---: | ---: | ---: | ---: | ---: |
| 历史 `5d36db1e` | 9 | 3 | 1 | 13 | 2 | 3 |
| 当前默认 | 1 | 3 | 1 | 5 | 2 | 3 |
| 当前原生方法候选 | 0 | 3 | 0 | 3 | 2 | 3 |

三个导入桥函数 `_rp_has` / `_rp_load` / `_rp_package` 是 `PyCFunction`，另列为 C 回调，
不进入 Python `def` 计数。两个导入钩子类仍由 `PyRun_String` 创建；原生方法候选仍保留
`rp_install_imports`、`sys` 与 `importlib.util` 执行链，C1 整阶段尚未完成。

---

## 4. 导入与反向导入执行链

### 4.1 编译域 → CPython 正向导入链

`[静态证据]`
1. 编译期 [modules.rs](../crates/ryupy-driver/src/modules.rs) 若 `cpython_provider::selected(sess, &name)` 为真，跳过 RyuPy 编译；若是本地委派（`is_local_delegated`），仅扫描其静态导入边使未委派的本地模块继续进入编译图。
2. 运行期入口调用 `PyModules.Import(name)`（[PyModules.cs](../runtime/RyuPy.Runtime/PyModules.cs)）或 `PyModules.ImportAttr(moduleName, name)`（[PyModules.cs](../runtime/RyuPy.Runtime/PyModules.cs)）：
   - 命中 `PyProvider.Current.Owns(name)`（[CPythonHost.cs](../runtime/RyuPy.CPython/CPythonHost.cs)：`entryScript != null && name == "__main__"`，或 `modules` 精确匹配/前缀子模块）→ 转入 `CPythonHost.Import(name)`（[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs)）。
   - 若为委派入口 `__main__`（首次）：调用 `Native.rp_run_entry(entryScript, source)` 编译执行入口源码；随后调用 `Native.rp_op(1, s: name)`（`PyImport_ImportModule(name)`），以 `ModuleFromOwned` 包装为同一权威 `PyModule` 视图（[PyProvider.Module](../runtime/RyuPy.Runtime/PyForeignObject.cs)）。

### 4.2 CPython → 编译域反向导入链与模块状态同步

`[静态证据]`
1. 编译模块注册时（`PyModules.Register` / `RegisterNativeLazy` / `PyModuleInstances` / `PyModuleLocations` / `PyModuleResourceLocations`）均通知 `PyProvider.Current?.Registered(name)` → `CPythonHost.Registered(name)` 将非 `Owns` 模块加入 `compiled` 集合（[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs)）。
2. 初始化前 `PyModules.InitializeRegisteredModule` 调用 `PyProvider.Current?.Initializing(module)` → `CPythonHost.Initializing`（[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs)）经 `rp_op(23, pointer, s: module.Name)` 将半初始化的 `ManagedObject` 代理直接写入 CPython `sys.modules[name]`；初始化抛错时 `Failed(name)` 经 `rp_op(30)` 清理 `sys.modules[name]`（[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs)）。
3. 当 CPython 代码执行 `import compiled_mod` 且该模块尚未初始化时，命中 `sys.meta_path[0]` 的 `_RyuPyFinder.find_spec` → 调用 C 回调 `_rp_has`（`Dispatch(-1, 20)` 检查 `compiled.Contains`）→ `_rp_package`（`Dispatch(-1, 22)`）→ `_rp_util.spec_from_loader(..., _RyuPyLoader(), ...)` → `_RyuPyLoader.create_module` → `_rp_load`（`Dispatch(-1, 21)`）→ 回调 `PyModules.Import(moduleName)`，回到 CLR 初始化并共享同一模块身份。

### 4.3 原生扩展内部触发的导入

`[静态证据]` + `[动态证据]`
- 实测内置扩展 `_sqlite3` 在 `PyModule_ExecDef` 阶段通过 `PyImport_ImportModule("functools")` 触发标准库 Python 模块导入（见第 7 节动态实验）。
- 实测 `NumPy 2.3.5`（19 个 `.so`）中 `_multiarray_umath` 与 Cython 生成的 `numpy/random/*.so` 均直接调用 `PyImport_ImportModule` / `PyImport_Import` / `PyImport_ImportModuleLevelObject`（见第 7 节符号清单）。

---

## 5. 跨域回调链与异常/生命周期边界

`[静态证据]`
1. **静态非托管回调入口**：`CPythonHost.Start` 将 `[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]` 方法 `Dispatch`（[CPythonCallbacks.cs](../runtime/RyuPy.CPython/CPythonCallbacks.cs)）传入 `rp_start`。
2. **`ryupy.ManagedObject` C 类型槽位**：[ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c) 定义 14 个槽位，通过 `forward(self, op, ...)` 转发至 `dispatch(self->identity, ...)`：
   - `op=0`: `managed_dealloc`（释放 `Roots[identity]` 与 `reverse` 缓存）
   - `op=1`: `managed_call_slot`（`PyObject_Call` → `PyOps.CallValue`）
   - `op=2/3/4`: `managed_getattr` / `managed_setattr`（set/del）
   - `op=5/6/7`: `managed_item` / `managed_setitem`（set/del）
   - `op=8/9/10/11/12/13/14/15`: `len`、`bool`、`repr`、`str`、`iter`、`next`、`hash`、`richcompare`。
3. **跨域方法与属性回调链**：
   - 编译类继承 CPython 类型时，`ConvertClassMemberOwned`（[CPythonClassBridge.cs](../runtime/RyuPy.CPython/CPythonClassBridge.cs)）将 `PyFunction` / `PyStaticMethod` / `PyClassMethod` / `PyProperty` 包装为 `ForeignMethodTarget`（实现 `IPyNativeCallable`，[CPythonClassBridge.cs](../runtime/RyuPy.CPython/CPythonClassBridge.cs)），**默认经过 Python helper `_rp_wrap_method`；显式原生方法候选使用静态 C 描述符**。
   - 当 Tk 事件循环、NumPy `frompyfunc` 或 CPython `super()` 调用该方法时：C 栈 → Python `_rp_method(*args, **kwargs)` 帧 → `managed_call_slot (op=1)` → `Dispatch` → `Enter(callback: true)` → `ManagedHandledScope` → 构造时先匹配 frozen 接收者 → `PullContainers()` → `ForeignMethodTarget.Invoke` → `PyObject.InvokeBound`（恢复 `PyMethodContext` 支持零参 `super()`）→ `PushContainers()` → `ToPython` / `RestoreCallbackError` → `PyTraceback.PopTo(callbackDepth + 1)` → 退出租约。
4. **自动 set_name 回调**：[managed_descriptor.h](../runtime/RyuPy.CPython/native/managed_descriptor.h)
   只为真实类型级 hook 签发专用代理，静态槽以同一 Rootid 调用 `Dispatch(op=24)`；
   owner 先按冻结 descriptor 与真实类型字典关联，再同步容器、转换参数并进入 CIL。
   普通 `ManagedObject` 的槽与非 data descriptor 身份保留。
5. **关闭钩子回调链**：`Py_FinalizeEx` 触发 `atexit` 注册的 `ManagedObject` 回调时，以 `state == 2 / 4` 的 `ownerThread` 租约重入 `Dispatch`（[CPythonHost.cs](../runtime/RyuPy.CPython/CPythonHost.cs)）。

---

## 6. libpython 启动 / 关闭 / Frozen 链与 C0 审计实证

### 6.1 启动链与关闭链全路径

`[静态证据]`
1. **启动链**：
   - 生成的 CIL `Main(string[] args)`（[entry.rs](../crates/ryupy-codegen-cil/src/entry.rs)）：`PySystem.Initialize` → `Bootstrap.Start("ryupy-cpython.json", args)`（[Bootstrap.cs](../runtime/RyuPy.CPython/Bootstrap.cs)）→ `CPythonHost.Start`（[CPythonHost.cs](../runtime/RyuPy.CPython/CPythonHost.cs)）。
   - `CPythonHost.Start`：读取并校验配置 → `dlopen(RTLD_NOW | RTLD_GLOBAL)` / `LoadLibraryExW(0x1100)` 载入固定 `libpython` 与 `shim` → 核对 `Py_GetVersion()` 与 `rp_build_version()` → `ConfigureExecutionAudit()`（C0 显式审计，环境变量 `RYUPY_CPYTHON_EXECUTION_AUDIT=1`）→ `TclTkEnvironment.Configure`（隔离 `TCL_LIBRARY` / `TK_LIBRARY`）→ 调用 `Native.rp_start(...)` → `InstallImports()` → 若审计开启则切换阶段 `rp_execution_audit_phase(1)`（runtime）。
   - `rp_start`（[ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c)）：`PyPreConfig_InitIsolatedConfig`（`utf8_mode = 1`）→ `Py_PreInitialize` → `PyConfig_InitIsolatedConfig`（`site_import = 0`, `write_bytecode = 0`, `parse_argv = 0`, `install_signal_handlers = 0`, `module_search_paths_set = 1`, `home`, `stdio_encoding="utf-8"`, `stdio_errors="surrogateescape"`, `module_search_paths`, `argv`）→ `Py_InitializeFromConfig(&config)` → `PyType_FromSpec(&managed_spec)` → `PyEval_SaveThread()`。
2. **关闭链**：
   - CIL `Main` 统一汇合点调用 `Bootstrap.Finish(code)`（[Bootstrap.cs](../runtime/RyuPy.CPython/Bootstrap.cs)）→ `Builtins.FinishExit(code)` → `Bootstrap.Stop()` → `CPythonHost.Dispose()`（[CPythonHost.cs](../runtime/RyuPy.CPython/CPythonHost.cs)）。
   - `CPythonHost.Dispose()`：校验 `leaseDepth == 0` 与 `ownerThread` → `state = 2` 等待 `active == 0` → `Enter()` 切 `rp_execution_audit_phase(2)`（审计开启时）、`CaptureModulePaths()`、`ReleaseClassBridgeResources()`、`Drain()` → `state = 4` → `Native.rp_stop()`（`PyEval_RestoreThread` + `rp_execution_audit_phase(2)` + `Py_FinalizeEx()` + `audit_finalized = audit_enabled`）→ 清理 `Roots` / `objects` / 恢复 `TCL_LIBRARY`/`TK_LIBRARY` → `WriteReport()`（当设置 `RYUPY_CPYTHON_REPORT` 时写出 JSON，包含 `execution_audit` 段）。

### 6.2 Frozen 与磁盘 Stdlib 启动执行实证（CPython 3.12.13）

C0 首轮历史取样使用仓库外 `/tmp` 只读探针与本机缓存 PBS `cpython-3.12.13`（`~/.cache/ryupy/cpython-auto/local-3.12.13-20260623-aarch64-apple-darwin`）动态观测到如下精确行为：

1. **`python3.12 -I -S -B -v` 与等效 C `rp_start` 配置动态观测 `[动态证据]`**：
   - `Py_InitializeFromConfig` 初始化后，`sys.modules` 已包含 **22** 个模块：`__main__`, `_abc`, `_codecs`, `_frozen_importlib`, `_frozen_importlib_external`, `_imp`, `_io`, `_thread`, `_warnings`, `_weakref`, `abc`, `builtins`, `codecs`, `encodings`, `encodings.aliases`, `encodings.utf_8`, `io`, `marshal`, `posix`, `sys`, `time`, `zipimport`。
   - 其中明确执行了 6 个 **frozen** Python 模块（`_frozen_importlib`, `_frozen_importlib_external`, `zipimport`, `codecs`, `abc`, `io`）以及 3 个 **磁盘 `.py/.pyc` 模块**（`encodings`, `encodings.aliases`, `encodings.utf_8`）。
   - **重要正向事实**：由于 `rp_start` 设置了 `module_search_paths_set = 1` 与 `home`，通过 `lldb` 对 `_Py_Get_Getpath_CodeObject` 下断点实测命中次数为 **0**，证明 frozen `getpath.py` 未被执行。
2. **`lldb` 对 `_PyEval_EvalFrameDefault` 断点计次 `[动态证据]`**：
   - 仅执行 `rp_start` 参数的 `Py_InitializeFromConfig` + `Py_FinalizeEx`（不含 `rp_install_imports`）：`_PyEval_EvalFrameDefault` C-to-Python 入口命中 **384** 次；其首帧栈为 `Py_InitializeFromConfig → pycore_interp_init → _PyImport_InitCore → PyImport_ImportFrozenModuleObject → exec_code_in_module → PyEval_EvalCode → _PyEval_EvalFrameDefault`。
   - 加上模拟 `rp_install_imports` 后再执行 `Py_FinalizeEx`：`_PyEval_EvalFrameDefault` 命中 **541** 次（`+157` 次）。
3. **`Py_FinalizeEx` 关闭期执行链 `[动态证据]`**：
   - 未注册 Python `atexit` 且未导入 `threading` 时，`shutdown` 期仅执行 `sys` / `builtins` / 模块 dict 清理（`# clear ...` / `# cleanup[2]` / `# cleanup[3]`），Python 帧数为 `0`；若用户或第三方库注册了 `atexit` 回调或导入了 `threading`（触发 `threading._shutdown`），则会在 `Py_FinalizeEx` 期间进入 Python 帧执行。

### 6.3 C0 原生 PEP 523 执行审计实证与严格门边界

`[静态证据]` + `[动态证据]`
- **实现机制**（[execution_audit.h](../runtime/RyuPy.CPython/native/execution_audit.h)、[CPythonExecutionAudit.cs](../runtime/RyuPy.CPython/CPythonExecutionAudit.cs)）：
  - 仅支持固定 CPython `3.12.13` release/GIL（读取 `<internal/pycore_frame.h>` 的 `_PyInterpreterFrame.f_code`，非 `3.12.13` 开启时 `rp_execution_audit_start()` 返回错误并保留固定诊断 `"execution audit requires exact CPython 3.12.13 release/GIL shim"`）。
  - 在 `Py_InitializeFromConfig` 之前调用 `PySys_AddAuditHook(audit_event, NULL)`；当最早具备非空 `_PyThreadState_UncheckedGet()` 的原生 `PySys_Audit` 事件到达时，通过 `_PyInterpreterState_SetEvalFrameFunc(interpreter, audit_eval)` 安装 PEP 523 帧求值 hook（并将 CPython 3.12 的帧内联调用切出默认快路径，从而真实统计每一个 Python-to-Python 与生成器恢复帧）。
- **C0 首轮历史取样 `[动态证据]`**（以下帧数与六项/六测数量保留原实验口径）：
  - 运行 `python3 -B tests/cpython/execution_audit.py --python .../python3.12 --unsupported-python .../python3.13` 通过（exit 0）：
    - 观测计数：**`startup = 839`、`runtime = 1544`、`shutdown = 6`**（覆盖直接调用、原生反向回调及异常、`resumed_generator` 4 次进入、`threading.Thread` 未来线程、`atexit`）；
    - 验证 `disabled` / `late-enable` 负例（均 0 帧且输出拒绝诊断）以及 `3.13.13` 开启审计返回退出码 3 拒绝、禁用审计默认宿主仍可通过。
  - 运行 `python3 -B -m unittest discover -s tests/cil_execution -p 'test_*.py' -v` 通过（6/6 tests OK）。
  - 主代理 C# 类桥 6 项集成测试（`CPythonClassBridgeTests`）在启用 `RYUPY_CPYTHON_EXECUTION_AUDIT=1` 下真跑报告：**`startup = 1299`、`runtime = 839`、`shutdown = 0`**；同时针对 C1 候选 [ADR-0209](adr/0209-base-object-native-descriptors.md) 验证，已移除的 4 个 `_rp_object_*` 转发函数帧计数为 **0**。
- **本轮 C1 产品子合同 `[动态证据]`**：[product_validation.py](../tests/cil_execution/product_validation.py)
  的 macOS arm64 standalone 搬迁探针通过：宿主关闭、三阶段均有真实帧，丢帧与 coverage failure 为零；
  八个已删除 object/super/类构造 helper 帧为零，默认 `_rp_wrap_method` 仍有执行帧。
  C1 五个手写输入及类桥证据见第 9 节；本轮最终全门与 Release 性能尚待主代理复验。
- **盲区与严格门拒绝结论 `[静态证据]`**：
  - 由于 PEP 523 钩子只能在首个带 `tstate` 的 `PySys_Audit` 事件之后安装，**初始化最前端（如 `_PyImport_InitCore` 加载 `_frozen_importlib` 等早期帧）存在启动盲区**；此外若外部 C 扩展或内部代码直接调用 `_PyEval_EvalFrameDefault` 或另建子解释器，也存在绕行风险（`audit_replaced` 仅在阶段切换或后续审计事件时检查 hook 是否被替换）。
  - 因此报告固定输出 **`complete = false`**、**`startup_complete = false`**，[tests/cil_execution/check_audit.py](../tests/cil_execution/check_audit.py) 严格门明确拒绝将当前或任何仅靠半程审计得到的报告晋升为零字节码执行。**现有兼容 provider 仍执行 frozen/磁盘 Python；独立严格内核原型按第 8 节单列。**

---

## 7. 底层 ABI 消费者盘点

### 7.1 内部托管互操作层消费者：`RyuPy.CPython`

`[静态证据]` 当前 [Native.cs](../runtime/RyuPy.CPython/Native.cs) 有 **85** 个
`DllImport(Library)` 静态声明。实际 shim 由主 C 文件 **70** 个与四个头文件 **16** 个
唯一 `API rp_*` 定义组成，共 **86** 个导出；差额 `rp_build_version` 由 `NativeLibrary.TryGetExport`
动态取址。此计数只覆盖 `RyuPy.CPython/native`，独立严格内核/router 不在该分母。

历史 `5d36db1e` 为 67 个静态声明，`cbb92535` 为 75 个。当前较 `5d36db1e` 增加 18 项：
审计 5、super 分类 2、普通/静态方法描述符 2、类构造及 callable 弱引用 7、专用 set_name 代理 2，
原静态声明保留。对应源码分布为
[class_construction.h](../runtime/RyuPy.CPython/native/class_construction.h) 7、
[method_descriptor.h](../runtime/RyuPy.CPython/native/method_descriptor.h) 2、
[execution_audit.h](../runtime/RyuPy.CPython/native/execution_audit.h) 5、
[managed_descriptor.h](../runtime/RyuPy.CPython/native/managed_descriptor.h) 2。
CPython C API 消费者分为以下五类：

1. **生命周期与配置**：`PyPreConfig_InitIsolatedConfig`、`Py_PreInitialize`、`PyConfig_InitIsolatedConfig`、`PyConfig_SetBytesString`、`Py_DecodeLocale`、`PyWideStringList_Append`、`Py_InitializeFromConfig`、`PyConfig_Clear`、`Py_IsInitialized`、`Py_FinalizeEx`、`Py_GetVersion`。
2. **GIL 与线程状态**：`PyGILState_Ensure`、`PyGILState_Release`、`PyEval_SaveThread`、`PyEval_RestoreThread`；以及审计专用的 `<internal/pycore_frame.h>`、`_PyThreadState_UncheckedGet`、`PyThreadState_GetInterpreter`、`_PyInterpreterState_GetEvalFrameFunc` / `SetEvalFrameFunc`、`PySys_AddAuditHook`。
3. **对象、数值、容器与缓冲**：`Py_NewRef` / `Py_XDECREF` / `Py_INCREF` / `Py_DECREF`、`PyType_FromSpec`、`PyObject_New`、`PyLong_*`（含 `PyLong_AsLongLongAndOverflow`、`PyNumber_ToBase`）、`PyFloat_*`、`PyBool_FromLong`、`PyUnicode_DecodeUTF8` / `PyUnicode_AsUTF8AndSize`、`PyBytes_FromStringAndSize` / `PyBytes_AsStringAndSize`、`PyTuple_*`、`PyList_*`、`PyDict_*`（含 `PyDict_GetItemWithError`、`PyDict_Next`）、`PyMapping_*`、`PyType_IsSubtype`、`PyMethod_New`、`PyWeakref_NewRef` / `PyWeakref_GetObject` / `PyWeakref_GetRef`（按版本 guard）、`PySlice_New`、`PySet_CheckExact` / `PyFrozenSet_CheckExact`。
4. **协议、模块与执行入口**：`PyObject_GetAttrString` / `SetAttrString` / `DelAttrString` / `Call` / `GetItem` / `SetItem` / `DelItem` / `GetIter` / `Repr` / `Str` / `Type` / `RichCompare` / `IsInstance` / `IsSubclass` / `IsTrue` / `Length` / `Hash` / `Dir`、`PyIter_Next`、`PySequence_Tuple` / `Contains`、`PyNumber_*`（含全部 InPlace 槽）、`PyEval_GetBuiltins`、`PyImport_ImportModule`、`PyImport_GetModuleDict`、`PyImport_AddModule`、`PyModule_New`、`PyModule_GetDict`、`PyCFunction_New`、`PyRun_String`（E2/E3）、`Py_CompileStringObject` + `PyEval_EvalCode`（E1）。
5. **异常与 Traceback 布局**：`PyErr_*`（`Occurred` / `Clear` / `GetRaisedException` / `SetRaisedException` / `GetHandledException` / `SetHandledException` / `SetString` / `GivenExceptionMatches` / `NewException`）、`PyException_*`（`GetArgs` / `GetCause` / `SetCause` / `GetContext` / `SetContext` / `GetTraceback`）、以及直接读写 `PyBaseExceptionObject.suppress_context` 与 `PyTracebackObject`（`tb_next` / `tb_frame` / `tb_lineno` / `tb_lasti`）+ `PyFrame_GetCode` + `PyCode_Addr2Line`。

### 7.2 标准库内建/动态扩展、第三方二进制扩展与 C ABI 消费者

| 消费者类别 | 实测对象（来源） | 关键依赖的 CPython ABI（静态符号 / 动态行为证据） | 对无字节码内核的约束 |
| :--- | :--- | :--- | :--- |
| **内建 C 模块（PBS `libpython`）** | `_sqlite3` 等（`PYTHON.json` 列出 106 项内建模块） | `[动态证据]` `_sqlite3` 在 `PyModule_ExecDef` 时调用 `PyImport_ImportModule("functools")` | 必须让 `PyImport_ImportModule` 路由到 RyuPy 已编译 CIL/Runtime 的 `functools`，而非走磁盘 `.py` |
| **`lib-dynload` 扩展** | `_tkinter.cpython-312-darwin.so`、`_dbm.cpython-312-darwin.so`、`SanBudget` 消费 | `[静态证据]` `nm -u`：`_tkinter` 依赖 `_PyType_FromSpec`、`_PyEval_SaveThread`/`RestoreThread`、`_PyErr_*`；`_dbm` 依赖 `_PyType_FromModuleAndSpec` | 保留 GIL 释放/重入、C `PyType_FromSpec` 与异常 C API；事件回调进入 `ManagedObject.tp_call` |
| **NumPy 2.3.5 核心与随机扩展** | `numpy/_core/_multiarray_umath.cpython-312-darwin.so`、9 个 `numpy/random/*.so` | `[静态证据]` `nm -u`：`_multiarray_umath` 导入 `_PyImport_ImportModule`、`_PyImport_Import`、`_PyEval_GetBuiltins`、`_PyBuffer_Release`、`_PyMemoryView_*`；**全部 9 个 Cython 生成的 `numpy/random/*.so` 均直接导入 `_PyCode_NewEmpty`、`_PyFrame_New`、`_PyImport_ImportModuleLevelObject`、`_PyType_FromMetaclass`** | **决定性约束**：内核**必须**保留 `PyCodeObject`/`PyFrameObject` C 结构 ABI（供 Cython traceback 构造）且**必须**让 `PyImport_ImportModuleLevelObject` 能在无 `importlib` 字节码时工作 |
| **FastAPI 技术栈扩展** | `pydantic-core==2.50.4`、`greenlet==3.5.6`、`SQLAlchemy==2.0.52` | `[静态证据]` macOS arm64 的 CPython 3.13.3 真实 wheel 已扫描：pydantic-core 单扩展有 161 个 Python 未定义符号，含 `PyEval_SaveThread/RestoreThread`；greenlet 生产扩展有 83 个，含 `PyContext_Type`、`PyThreadState_GetFrame`、`PyFrame_GetBack`；SQLAlchemy 五个扩展共 216 个，含 `PyCode_NewEmpty`、`PyFrame_New`、`PyUnstable_Code_NewWithPosOnlyArgs` | 保留实际版本对象/帧/线程 ABI；扫描登记静态消费者。greenlet 跨 CLR 换栈受 §7.3 阻断，C3/C4 继续逐库验证真实调度、布局与回调 |
| **RyuPy 直接 C ABI 绑定** | `native_binding.rs`（v1–v3 声明）与 [PyNativeBindings.cs](../runtime/RyuPy.Runtime/PyNativeBindings.cs) | `[静态证据]` JIT `DllImport` / AOT `DirectPInvoke`，经 `CheckedI32` + `ReadExactScalar` 仅允许精确整数标量入 C | 本身不依赖 `libpython`；仅当外域 `PythonObject` 传入时经 `IPyForeignObject.ReadScalar` 读标量 |


### 7.3 greenlet 跨 CLR 栈切换的准入阻断

真实 wheel 符号扫描登记了 greenlet 的帧、线程状态与 context ABI；其原生同步栈切换仍未准入。
在 CLR 托管调用/PInvoke 回调链中直接执行原生换栈，无法满足 CLR GC 栈扫描、托管异常展开及
活动托管帧的恢复合同，不能仅凭对象 ABI 相同、扩展可加载或 C3 coroutine 通过就运行该路径。
SQLAlchemy 等使用 greenlet 的同步到异步桥接继续受此阻断。

后续须独立制定 **resumable-sync ADR**：把所需同步 Python 调用链降为显式可暂停/恢复的 CIL
continuation/驻留状态，再设计受控调度桥。需逐项证明局部与 GC 根、异常/finally、重入、
ThreadState/GIL、contextvars、线程亲和性及取消/事务清理；真实 greenlet/SQLAlchemy 消费者和
JIT/NativeAOT 搬迁分别验收。本轮尚无该 ADR 的实现与准入结果。

---

## 8. 严格无字节码产品启动内核方案（明确选型与维护成本）

### 8.1 明确选型与路线区分

> **选型结论（唯一路线，非多选并列）**：严格零字节码交付产品（C4/C5 目标）选定**「基于上游 CPython 固定发布源码构建的可重建最小 `libpython` 内核（Rebuilt Minimal `libpython` Kernel）＋ `PyConfig._install_importlib = 0` 原生启动模式」**，首切固定 `CPython 3.12.13`（x64/arm64 常规 GIL）。

**选定路线 vs 现成产品的明确区分**：

1. **现成产品（当前与 C1–C3 过渡态）**：使用未修改的上游 `python-build-standalone`（`3.12.13+20260623` 与 auto 3.9–3.15）共享 `libpython` 与完整基础标准库。
   - **现状 `[动态证据]`**：`rp_start` 仍执行 frozen `_frozen_importlib`/`zipimport`/`codecs`/`abc`/`io` 与磁盘 `encodings`（C0 历史取样 `startup` 计帧 384–1299 次），且 `execution_audit` 为 `complete=false`、`startup_complete=false`。**当前不可宣称无 frozen 或零字节码**。
2. **为什么不能仅靠「现成未修改 PBS `libpython` + `_install_importlib = 0`」作为严格产品方案？**
   本轮在缓存的 `libpython3.12.dylib` 上通过临时 C 探针与 `lldb` **动态实证**了 `_install_importlib = 0` 的可行边界与三处硬阻断：
   - **已证实的正向基础 `[动态证据]`**：设置 `PyConfig_InitIsolatedConfig` + `module_search_paths_set = 1` + `home` + `_install_importlib = 0`，实测在 `CPython 3.12.13`、`3.13.13`、`3.15.2` 上均能成功完成 `Py_InitializeFromConfig` 与 `Py_FinalizeEx`；在 `3.12.13` 下 `lldb` 观测 `_PyEval_EvalFrameDefault` 与 `_Py_Get_Getpath_CodeObject` 命中次数均为 **0**；注册 C 版 `builtins.__import__` 后，内建多阶段模块 `math.sqrt(16.0) = 4.0` 在 **0 次** `_PyEval_EvalFrameDefault` 下成功执行，且 `_sqlite3` 初始化可被截获到 `c_import("functools")`。
   - **已证实的现成库硬阻断 `[动态证据]`**：
     1. **`PyImport_ImportModuleLevel("foo3", ...)` / `PyImport_ImportModuleLevelObject` 直接失败**：在 `_install_importlib = 0` 下绕过 `builtins.__import__` 直接读取 `interp->imports.importlib`（为 `NULL`），抛出 `SystemError: null argument to internal routine`。而上文已静态证实 **全部 9 个 `numpy/random/*.so` 均调用 `_PyImport_ImportModuleLevelObject`**，现成未改 `libpython` 无法承载 NumPy！
     2. **`sys.stdin` / `sys.stdout` 为 `NULL`**（仅 `sys.stderr` 为裸 `stdprinter`），无完整 UTF-8 C stdio 桥接；
     3. **二进制残余与审计盲区**：现成 `libpython` 仍内嵌 frozen 字节码数据，且不改 `libpython` 无法堵住初始化极早期帧与直接调用 `_PyEval_EvalFrameDefault` 的绕行风险（无法满足 `complete=true` 与 `startup_complete=true` 门禁）。

### 8.2 选定「可重建最小 `libpython` 内核」的四大构件与当前落点

以下是完整目标设计。当前 `runtime/kernel` 已接入严格求值拒绝、纯 C UTF-8/stdio、
冻结扩展注册表、原生 Spec/Loader、每模块锁和静态 CIL 导入单事务 ABI。macOS arm64
官方 3.12.13 源码构建的 22 项内核探针、30 个测试扩展及 10 项 stock 对照全部通过。
真实 C# host/CLI 与完整闭包编译继续待办，见
[kernel README](../runtime/kernel/README.md)、[ADR-0216](adr/0216-strict-kernel-native-stdio.md)
与 [ADR-0217](adr/0217-strict-kernel-frozen-extensions-and-cil-import-abi.md)。

1. **无字节码启动配置与空 Frozen 载荷**：
   - 启动参数固定 `PyConfig_InitIsolatedConfig` + `module_search_paths_set = 1` + `home` + `_install_importlib = 0` + `site_import = 0` + `write_bytecode = 0`。
   - 内核构建时将 `Python/frozen.c` 中的 `_PyImport_FrozenBootstrap` / `_PyImport_FrozenStdlib` / `_PyImport_FrozenTest` 以及 `Modules/getpath.c` 对应的 `_Py_M__getpath` **清空为空表**（仅保留 ABI 符号避免链接缺口）；严格产品目录中**不部署** `lib/python3.12/*.py`、`.pyc` 与 `encodings/` 目录。
2. **原生 C 导入路由器（修补 `Python/import.c`）**：
   - 在 `Py_InitializeFromConfig` 的 `_install_importlib == 0` 路径下，注册静态 C 函数指针作为内核默认导入路由（覆盖 `PyImport_ImportModule`、`PyImport_ImportModuleNoBlock` 与 `PyImport_ImportModuleLevelObject` 全部入口）。
   - 解析顺序严格冻结为：**① `sys.modules` 已缓存对象 → ② C 原生内建 `PyImport_Inittab`（`PyModule_FromDefAndSpec` + `PyModule_ExecDef`）→ ③ 随包冻结列表中的 C 扩展 `.so`/`.pyd`（`dlopen`/`LoadLibraryExW` + `PyInit_<mod>`）→ ④ 回调 `RyuPy.CPython` `Dispatch` 进入 CIL `PyModules.Import`（并写回同一 `sys.modules`）**。彻底禁止运行时磁盘扫描 `.py` / `.pyc`。
3. **进程 C Stdio 与 UTF-8 编解码桩**：
   - `sys.stdin`/`stdout`/`stderr` 在无 Python `io`/`encodings` 时由内核静态 C 流或 `RyuPy.Runtime` `PySystem` 统一接管，编码固定为 UTF-8（配合 `PyPreConfig.utf8_mode = 1`），不触发 `encodings.utf_8` / `aliases` 加载。
4. **不可绕过的全局零字节码求值审计与拒绝门（替代半程 PEP 523）**：
   - 在内核编译期/进程初始化最起点（先于任何子系统）将 `_PyEval_EvalFrameDefault` 与 `PyEval_EvalCode` 替换为全局审计桩：在严格零字节码构建配置下直接计次并拒绝/触发致命诊断；在过渡/诊断构建下实现 **`startup_complete = true`、`complete = true`** 的全生命周期无盲区覆盖，供 [tests/cil_execution/check_audit.py](../tests/cil_execution/check_audit.py) 验收。
   - 同时**完整保留** `PyCodeObject` / `_PyInterpreterFrame` / `PyThreadState` / `PyBaseExceptionObject` / `PyBuffer` 的 CPython 3.12.13 二进制布局，确保 `_PyCode_NewEmpty`、`_PyFrame_New` 等预编译第三方 wheel 符号兼容。

### 8.3 维护成本评估

| 成本项 | 具体内容与工程影响 | 控制策略 |
| :--- | :--- | :--- |
| **三平台可重复构建与产物校验** | 需维护 macOS arm64（`pgo+lto`）、Linux x64（`pgo+lto`，关联 glibc/OpenSSL/`libcrypt`）、Windows x64（MSVC `pgo` + DLL）三平台内核构建流水线（可基于 `python-build-standalone` 构建配方精简补丁集） | 首切锁定 **CPython 3.12.13** 单一内核，与现成 auto 3.9–3.15 兼容路径隔离，待 C4 闭环后再评估滚动升级 |
| **上游补丁维护面** | 当前覆盖 `import.c`、`frozen.c`、`ceval.c`、`pylifecycle.c`、`codecs.c`、`getpath.c`、`pycore_ceval.h`、`pycore_import.h`、`moduleobject.c`、`patchlevel.h` 与 `Makefile.pre.in`；另注入纯 C codec/增量类型及导入头 | 不改动对象模型、GC、字典/列表/数字布局，codec 与增量类型约六百余行 C，求值器/导入/启动锚点另列；版本扩展需逐处复验 |
| **C 扩展 ABI 布局兼容性** | 第三方 wheel（`numpy/random/*.so`、`pydantic-core`、`greenlet`）对 `PyTypeObject`、`_PyInterpreterFrame`、`PyThreadState` 有强结构假设 | 严格保持与上游 3.12.13 完全一致的编译宏（常规 GIL、release 非 `Py_DEBUG`）与头文件路径，每版按真实 wheel 门禁验收 |
| **开源许可与随包元数据** | 即使剥离 Python 标准库源码，内建 C 扩展（OpenSSL、SQLite、zlib-ng、expat、libffi、mpdecimal 等）仍留在 `libpython` 中；本机 `PYTHON.json` 显示需保留 **21** 份组件许可证 | 继续沿用 [cpython_deployment.rs](../crates/ryupy-driver/src/cpython_deployment.rs) 与 `PYTHON.json` + `licenses/` 校验链，无新增合规黑洞 |

---

## 9. 本轮已验证子合同与剩余边界

### 9.1 C1：基础槽、super、方法候选与两阶段类构造

- object 四个与 super 两个 helper 已由真实原生描述符/C API 接管，普通 super 保留两引用布局。
  真实 C3/Mixin 关系、遮蔽、反向返回的精确 super 与子类身份已有专项；普通 MI 的 AOT 保护门保留。
  公共 `CreateSuper(Object,Object)->Object` 的历史返回 ABI 已恢复，8 项定向测试通过；
  `5d36db1e` 的 1843 项公共签名全部保留，新增 45 项。
- 显式原生方法候选保留，默认 `_rp_wrap_method` 继续。FunctionType、CodeType、完整 frame、闭包布局
  与精确 C 扩展消费者仍有独立能力边界；默认晋升另验，见 [ADR-0211](adr/0211-native-method-descriptor-candidate.md)。
- `_rp_calculate_meta` / `_rp_make_class` 已删除。静态 shim 在 CIL 类体前完成元类选择与 prepare，
  mapping 单次读取后再回退 globals/builtins；类体前言按 namespace 优先读取 `__name__`，
  类型元数据由真实外部类型提供，函数 module 仍取其定义 globals。
- 构造回调按冻结 callable/descriptor、真实 MRO 来源和直接 bases 关联临时类型；覆盖自动及元类显式
  set_name、隐式 new、property getter/setter/deleter、继承 classmethod。既有 opaque 身份和 decoy
  不会被认领，异常路径撤销临时关联并释放 namespace，见 [ADR-0213](adr/0213-native-two-phase-class-construction.md)。
- `ManagedDescriptor` 只授予真实类型级 set_name；普通/实例同名字段无假槽。旧普通代理迟增能力时
  再导出精确拒绝，专用代理删除 hook 后按实时类型查询无动作；自定义数据描述符组合保持明确诊断。
  NativeStaticMethod 共用普通候选实例布局，按导出键固定 new 的静态绑定，mapping 不额外重写。
- 定向证据：3.9.25/3.12.13/3.13.3 各真实 shim、默认/候选各 30 项类桥，共 180 项通过；
  Runtime 类相关 69 项通过。五个类构造输入在 3.12.13 同次 oracle 下通过 JIT/AOT 删除输入搬迁；
  重入输入的默认/候选两模式均通过。最新 standalone 审计达到第 6.3 节的 helper 零帧合同。

剩余：两个 Python finder/loader、三个别名、默认 wrapper 与 `rp_install_imports`；死分派
`rp_op(29)` 仍在源码。当前 loader 的 create_module 执行完整 Runtime 初始化、exec_module 空操作；
原生替换须明确冻结该时序或同步抽出两阶段模块事务，覆盖包、循环导入、失败重试、单一 sys.modules
身份与关闭。上述子合同完成后才进入 C1 整阶段完成门。

### 9.2 C2：实时默认值之后的通用伴生核心

[ADR-0212](adr/0212-live-function-defaults.md) 的实时 defaults、kwdefaults、别名/可变对象与已绑定方法
继续保留。[ADR-0214](adr/0214-generic-default-function-companions.md) 在冻结的多模块图中，给准入
的 typed 顶层定义生成通用 CIL 伴生体；canonical 函数、签名、defaults、来源和 def 求值仍各一份。
Binder 成功且实际补齐缺省参数时进入通用体，普通完整供参和原 typed 核心保持原入口。

三版本同次 oracle、JIT/AOT 删除输入搬迁，以及真实 CIL/PE、别名/递归/传递调用、dataclass 普通构造
和 range 冻结求值/迭代结构已有定向证据。伴生体显式弱键登记，无二次 def 或解释执行；代码体积与
冷登记成本单列。完整供参的任意对象 typed 边界、单模块未知扩展调用、typed 特化恢复、
`inspect.signature` 完整消费者、code/frame、attrs 活字典和源码/资源/可写数据根继续待办。
最终全门、公共 ABI 与 Release 配对由主代理从最新源码冻结后验收。

### 9.3 C3：async with 与有限真实 traceback

`native-coroutines` 仍默认关闭。[ADR-0210](adr/0210-native-coroutine-state-machine.md) 的 coroutine/await
之上，[ADR-0215](adr/0215-native-async-with-traceback.md) 增加类型槽捕获、enter/exit 真实挂起、
多上下文、控制流退出、取消注入与异常抑制；同步 with 的 as 目标失败也进入清理保护区。
真实传播链支持 `__traceback__` 只读头、tb_frame/tb_lineno/tb_next（next 可写并拒绝循环）、
frame 的 f_code/f_lineno 和 code 的 co_name/co_filename；来源来自受信 CompiledCodeSource。

六组新源码三版本 oracle 一致，JIT 与 cleanup/traceback_protocol 的 AOT 删除输入搬迁通过；
TraceV1 七项 PE 保护和普通路径零分配断言保留。完整 frame/code、async for、异步生成器、
事件循环/Task/Future、真实 IO 生命周期、调度取消/contextvars、跨域 awaitable 与跨暂停 super
仍待后续；greenlet 原生切栈受第 7.3 节阻断。整阶段与最终性能门尚未完成。

---

### 9.4 第三轮执行盘点增量

[ADR-0220](adr/0220-native-compiled-import-hooks.md) 已移除 Python finder/loader 的两个类、
三个成员和三个描述符别名。默认只剩 `_rp_wrap_method` 与内部 `_rp_method`，共两个 def；
显式 native-methods 候选安装源码为空。`rp_install_imports` 旧 ABI 保留，默认只执行 wrapper
安装；`rp_op(29)` 固定返回 NotImplementedError。原生 finder 创建真实 ModuleSpec，loader
仍复用 Runtime 唯一初始化点，完整 PEP451 事务接入按 ADR-0221 继续。

C1 三版本两模式 198 项类桥通过；最终两模式 JIT/AOT 删除输入搬迁的已删内部帧均为零。
默认 startup/runtime/shutdown 为 826/2251/37，候选为 825/2249/37；默认 wrapper 两函数
各一帧，候选均零，完整审计标志继续为 false。

[ADR-0219](adr/0219-native-inspect-signature.md) 的原生签名与真实宿主值对象桥通过核心、
atexit/owner、attrs/Pydantic/FastAPI 共 11 组 JIT/AOT 删除输入搬迁；字符串求值、延迟注解、
精确 CodeType 和原复杂应用全闭包继续开放。[ADR-0218](adr/0218-native-async-for.md) 的
显式原生 async for 候选 21 项及七组真实 AOT 搬迁通过，完整调度和 IO 仍待办。
当前 shim 静态 DllImport 声明 102 项；Runtime 历史 1843 项公共签名全部保留，当前 1967 项。
本轮最终产品与性能记录见[第三轮验收](cil-execution-third-slices-validation.md)。

## 10. 阶段状态与剩余实现项

### 10.1 C0 完成记录与历史集成证据

C0 的执行入口/启动链/ABI 消费者盘点、严格内核路线选择和兼容/性能基线冻结已完成。
首轮 `cbb92535` 的完整 make check、固定项目与六次平衡性能配对记录在
[首轮集成验收](cil-execution-first-slices-validation.md)，包含全部不利样本。
这些历史结果保留原提交与计数；本轮 C1/C2/C3 后续子合同只按第 9 节定向证据登记，
本轮最终 make check 已通过（3384 项 C#、495 项 e2e），公共 ABI 已恢复；
冻结 Release 的六版本产品、三形态审计与 26 项性能通过，整体墙钟 -0.03%，单项成本保留。

严格内核独立原型的 macOS arm64 真实源码 22 项探针与 10 项 stock 对照通过；
原 CPython 结构布局保留。真实 C# host/CLI 接入仍待完成。
现成兼容 provider 仍 `complete=false`，严格内核产品接入与全生命周期零执行归 C4。

### 10.2 C1–C5 整阶段继续待办

- **C1**：完成默认 wrapper 退出、旧 `rp_install_imports` 源码 ABI 与精确函数布局消费者准入；
  finder/loader 和别名已静态替换，`rp_op(29)` 已固定拒绝源码执行。SanBudget 和生命周期门
  逐项保护；默认晋升与旧实现删除分开。
- **C2**：完成真实框架反射/inspect、动态对象与缓存失效、完整 code/frame/traceback 消费域、
  attrs 活字典，以及独立源码位置/资源根/可写根；移除 `compiled_sources` 默认 `.py` 锚点。
- **C3**：完成异步生成器、事件循环与真实 IO/任务取消/contextvars，明确跨域与线程/GIL
  合同；greenlet 需独立 resumable-sync ADR，完整 FastAPI 服务生命周期仍待原生准入。
- **C4**：编译应用/stdlib/框架 Python 闭包，接入严格内核扩展与 CIL 导入路由、产品生命周期及真实
  原生依赖；现有 22 项内核探针及 10 项 stock 对照独立登记，三阶段零执行仍需产品证据。
- **C5**：待 C0–C4、固定 20 项目、真实应用、3.9–3.15 和三平台 JIT/AOT/搬迁/性能门闭环，
  再分别验收默认切换与旧源码委派/胶水/过渡载荷退出。
