# RyuPy C0 架构与执行链盘点（全 CIL 执行与 libpython 底层化）

- 日期：2026-11-16
- 阶段：C0（执行归属、启动链盘点与严格内核选型）
- 决策依据：[ADR-0208](adr/0208-cil-execution-libpython-kernel.md)、[全 CIL 实施路线](cil-execution-roadmap.md)、[架构设计](architecture.md)、[开发工作流](workflow.md)
- 回归基线配套文档：[C0 回归合同与可复现基线冻结](cil-execution-baseline.md)
- 证据标记约定：
  - **`[静态证据]`**：直接读取仓库源码、头文件、二进制符号表或构建清单所证实的事实。
  - **`[动态证据]`**：在本机固定 CPython 发行或真实工作流上实际运行并观测到的输出、帧计数、断点堆栈或错误信息；未运行项明确标注**未测 / 待验**，不臆测通过。

---

## 1. Git 基准 `5d36db1e` 与工作树候选差异总览

本盘点严格区分**已提交基准**与**当前共享工作树中的未提交并行候选**：

| 维度 | 已提交基准（`5d36db1e`） | 当前共享工作树候选（未提交） |
| :--- | :--- | :--- |
| **Git 引用** | 分支 `feat/adr0208-cil-execution`，Commit `5d36db1e952f1db07aee6563d7a800cc1df94548` | 在 `5d36db1e` 之上的未提交工作区修改与未跟踪文件 |
| **C0 执行审计** | **无**（尚无 PEP 523 帧观测与审计门） | **新增固定 3.12.13 原生 PEP 523 审计**：[execution_audit.h](../runtime/RyuPy.CPython/native/execution_audit.h)、[CPythonExecutionAudit.cs](../runtime/RyuPy.CPython/CPythonExecutionAudit.cs)、[tests/cpython/execution_audit.py](../tests/cpython/execution_audit.py)、[tests/cil_execution/check_audit.py](../tests/cil_execution/check_audit.py) |
| **C1 Python 胶水** | `_ryupy_imports` 含 9 个 Python `def` + 2 个查找/加载类 + 3 个别名，含 `_rp_object_new` / `_rp_object_init` / `_rp_object_setattr` / `_rp_object_delattr` | **候选 [ADR-0209](adr/0209-base-object-native-descriptors.md)**：从 [CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs) 移除 4 个 `_rp_object_*` 转发函数；[CPythonClassBridge.cs](../runtime/RyuPy.CPython/CPythonClassBridge.cs#L519-L529) 直接获取并缓存 `object` 原生描述符；随后移除 2 个 super helper；默认剩余 3 个 Python `def` + 2 个类 + 3 个别名，原生方法描述符候选再移除方法包装 helper |
| **其余并行候选** | 无 | 另有并行未提交的 C0 基线文档（[cil-execution-baseline.md](cil-execution-baseline.md)）、`TODO.md`、`Makefile` 及 C2/C3 早期原型（`PyCoroutine*.cs`、`coroutines.rs`、`c2_function_defaults_dynamic.py`）；合流后按实际门禁登记验收 |

---

## 2. 源码、字节码执行入口与构建期边界盘点

### 2.1 产品运行期源码/字节码执行入口（必须归零或迁出）

| 编号 | 入口 / 通道 | 静态证据位置 | 触发条件与执行域 | 归属与处置阶段 |
| :--- | :--- | :--- | :--- | :--- |
| **E1** | `rp_run_entry` (`Py_CompileStringObject` + `PyEval_EvalCode`) | [ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c#L529-L556)、[Native.cs](../runtime/RyuPy.CPython/Native.cs#L74)、[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs#L11-L16)、[Bootstrap.cs](../runtime/RyuPy.CPython/Bootstrap.cs#L96-L106)、[cpython_delegation.rs](../crates/ryupy-driver/src/cpython_delegation.rs#L186-L210) | 使用 `--cpython-entry` 时，将入口脚本复制到 `python/ryupy_entry/<name>`，运行时读源码文本并在 `__main__` 上下文中编译执行字节码 | **C3/C4 移除**：入口与所有应用模块编译为 CIL，取消 `entry_script` 与 `rp_run_entry` |
| **E2** | `rp_install_imports` (`PyRun_String`) | [ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c#L254-L275)、[Native.cs](../runtime/RyuPy.CPython/Native.cs#L18)、[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs#L58-L113) | `CPythonHost.Start` 每次启动必然执行，通过 `PyRun_String` 编译并执行 `_ryupy_imports` 模块源码，导入 `sys` / `importlib.util` / `types` 并注册 finder/loader/helper | **C1 分切移除**：用静态 C 类型/C API/CIL 替代全部 Python helper 与 finder/loader，最终删除 `rp_install_imports` |
| **E3** | `rp_op(29, ...)` (`PyRun_String`) | [ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c#L350) | `case 29: return PyRun_String(s, Py_file_input, PyEval_GetBuiltins(), PyEval_GetBuiltins());`；`[静态证据]` 全仓库检索 `rp_op(29` 在当前 `RyuPy.CPython` 与 `crates/` **0 处调用** | **C1 删除**：潜在任意源码求值后门，无调用者，应在 C1 直接移除该分派分支 |
| **E4** | `rp_op(1, s: name)` (`PyImport_ImportModule`) 导入 `.py` / `.pyc` | [ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c#L322)、[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs#L17)、[PyModules.cs](../runtime/RyuPy.Runtime/PyModules.cs#L236)、[cpython_provider.rs](../crates/ryupy-driver/src/cpython_provider.rs#L282-L286) | 任何归属于 `CPythonHost.Owns(name)` 的模块（stdlib、site-packages、`--cpython-module`、`--cpython-dynamic-module`）通过 `PyModules.Import` 转发到 `PyImport_ImportModule`，执行其 `.py` / `.pyc` 字节码 | **C4 收敛**：纯 Python 标准库、第三方包装与本地模块全部编译为 CIL，仅保留原生扩展 `.so`/`.pyd` 与内建 C 模块经底层加载入口进入 |
| **E5** | `PyTextProviderMethods.Title`（内建方法间接导入执行） | [PyTextProviderMethods.cs](../runtime/RyuPy.Runtime/PyTextProviderMethods.cs#L6-L15)、[PyStringMethods.cs](../runtime/RyuPy.Runtime/PyStringMethods.cs#L6)、[PyPreparedMethods.cs](../runtime/RyuPy.Runtime/PyPreparedMethods.cs#L129) | 调用 `str.title()` 且存在 `PyProvider.Current` 时，经 `provider.Import("builtins")` 获取 `str.title` 转发执行 | **C4 移除依赖**：`str.title` 在 `RyuPy.Runtime` 原生实现 Unicode 规则，不再依赖 CPython `builtins.str` |
| **E6** | `compiled_sources` 随包 `.py` 载荷（数据锚点，非执行） | [cpython_delegation.rs](../crates/ryupy-driver/src/cpython_delegation.rs#L15-L16)、[cpython_delegation.rs](../crates/ryupy-driver/src/cpython_delegation.rs#L227-L236)、[linked_compile.rs](../crates/ryupy-driver/src/linked_compile.rs#L203-L221) | 将已编译源码复制到 `python/ryupy_compiled/` 并设置 `BundleRelative` 供 `__file__` / `inspect` 使用（不加入 `sys.path`） | **C2 替换**：按 [路线 §3 C2](cil-execution-roadmap.md#L87-L90) 独立建模源码位置、资源根与可写数据根，默认严格产品不打包 `.py` |

### 2.2 编译期/静态编译边界（不进入产品运行期）

- **构建期工具（保留在编译机）**：`[静态证据]` [cpython_discovery.rs](../crates/ryupy-driver/src/cpython_discovery.rs#L240-L270)、[cpython_portable.py](../crates/ryupy-driver/src/cpython_portable.py)、[cpython_dependencies.py](../crates/ryupy-driver/src/cpython_dependencies.py)、`cpython_build_tools.py` 仅在开发/构建机上用 `-I -X utf8 -S -B` 静态收集元数据（读 AST、`METADATA`、`RECORD`、`PYTHON.json`），不随产品发布，符合 [路线 §1](cil-execution-roadmap.md#L27)。
- **动态代码内建（`eval` / `exec` / `compile`）**：`[静态证据]` [unresolved_globals.rs](../crates/ryupy-hir/src/unresolved_globals.rs#L41-L49) 与 [PyModuleBuiltins.cs](../runtime/RyuPy.Runtime/PyModuleBuiltins.cs#L7-L9) 将 `eval`、`exec`、`compile` 列为明确不支持的 builtin（`IsUnsupported` / 运行时 `NameError` 或 `NotImplementedError`），不会在原生模式或编译函数体中静默转交 CPython 解释执行。

---

## 3. 嵌入 Python Helper 清单（基准 `5d36db1e` vs 工作树候选）

`[静态证据]` 所有内部 Python helper 均位于 [CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs#L58-L113) 的 `InstallImports()` 字符串中，通过 `rp_install_imports` 注入 `_ryupy_imports` 模块，并由 [CPythonClassBridge.cs](../runtime/RyuPy.CPython/CPythonClassBridge.cs#L508-L537) 懒加载缓存指针：

| Helper 名称 | 类型 | 基准 `5d36db1e` 状态 | 当前工作树候选（ADR-0209）状态 | C# 消费者位置 | 替代去向与阶段 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `_RyuPyLoader` | Python `class` (`create_module`, `exec_module`) | 存在 | **保留（未移除）** | `_RyuPyFinder.find_spec` | **C1 后续**：改为 C `PyType_Spec` loader 或由静态 C `__import__` 钩子接管 |
| `_RyuPyFinder` | Python `class` (`find_spec`) | 存在 | **保留（未移除）** | 插入 `sys.meta_path[0]` | **C1 后续**：改为静态 C finder 对象插入 `sys.meta_path[0]` |
| `_rp_calculate_meta` | Python `def` | 存在 | **保留（未移除）** | `_rp_make_class` | **C1 后续**：C 侧 `PyType_IsSubtype` / CLR 元类解析 |
| `_rp_make_class` | Python `def` | 存在 | **保留（未移除）** | `_makeClassFn` (`CreateDerivedClass`, `ResolveForeignTypePointer`) | **C1 后续**：C shim 调用 `types.resolve_bases` 协议 + `__prepare__` + `PyObject_Call(meta, ...)` |
| `_rp_wrap_method` | Python `def`（内含闭包 `_rp_method(*args, **kwargs)`） | 存在 | **默认保留，原生方法候选移除** | `_wrapMethodFn` (`WrapCallableOwned`)；每次方法回调产生 Python 帧 `_rp_method` | **C1 重点**：改为 C `PyType_Spec` 可调用描述符（携带 `ForeignMethodTarget` 与 `__name__`/`__qualname__`） |
| `_rp_object_new` | Python `def` | 存在（调用 `object.__new__`） | **已移除**（由 `_objectNewFn = object.__new__` 原生描述符替代） | `AllocateFromBaseObject` | **C1 候选已落地（ADR-0209）**：审计确认该转发帧为 0；待完整 `make check` 与搬迁验收 |
| `_rp_object_init` | Python `def` | 存在（调用 `object.__init__`） | **已移除**（由 `_objectInitFn = object.__init__` 原生描述符替代） | `InitializeFromBaseObject` | 同上 |
| `_rp_object_setattr` | Python `def` | 存在（调用 `object.__setattr__`） | **已移除**（由 `_objectSetAttrFn = object.__setattr__` 原生描述符替代） | `SetAttrFromBaseObject` | 同上 |
| `_rp_object_delattr` | Python `def` | 存在（调用 `object.__delattr__`） | **已移除**（由 `_objectDelAttrFn = object.__delattr__` 原生描述符替代） | `DelAttrFromBaseObject` | 同上 |
| `_rp_super_getattr` | Python `def` | 存在 | **已移除** | `CPythonHost.GetSuperAttribute` | 真实 super builtin 调用及原生属性查找 |
| `_rp_super_create` | Python `def` | 存在 | **已移除** | `CPythonHost.CreateSuper` | 中性 PySuper 与真实外部代理共享身份 |
| `_rp_staticmethod` | Python 全局别名（`= staticmethod`） | 存在 | **保留（未移除）** | `_staticMethodFn` | **C1 后续**：直接 `rp_builtin("staticmethod")` 或 `&PyStaticMethod_Type` |
| `_rp_classmethod` | Python 全局别名（`= classmethod`） | 存在 | **保留（未移除）** | `_classMethodFn` | **C1 后续**：直接 `rp_builtin("classmethod")` 或 `&PyClassMethod_Type` |
| `_rp_property` | Python 全局别名（`= property`） | 存在 | **保留（未移除）** | `_propertyFn` | **C1 后续**：直接 `rp_builtin("property")` 或 `&PyProperty_Type` |

> `[静态证据]` 基准 `5d36db1e` 中 `_ryupy_imports` 共包含 **9 个 Python `def` 转发/构建函数 + 2 个 Python 导入钩子类**；当前工作树移除了 4 个 `_rp_object_*` 与 2 个 `_rp_super_*` 函数，默认仍留存 **3 个 Python `def` + 2 个类 + 3 个别名**；`RYUPY_CPYTHON_NATIVE_METHODS=1` 候选再移除 `_rp_wrap_method`，且 `InstallImports()` 仍执行 `import sys, importlib.util, types` 与 `PyRun_String`。

---

## 4. 导入与反向导入执行链

### 4.1 编译域 → CPython 正向导入链

`[静态证据]`
1. 编译期 [modules.rs](../crates/ryupy-driver/src/modules.rs#L134-L153) 若 `cpython_provider::selected(sess, &name)` 为真，跳过 RyuPy 编译；若是本地委派（`is_local_delegated`），仅扫描其静态导入边使未委派的本地模块继续进入编译图。
2. 运行期入口调用 `PyModules.Import(name)`（[PyModules.cs](../runtime/RyuPy.Runtime/PyModules.cs#L236)）或 `PyModules.ImportAttr(moduleName, name)`（[PyModules.cs](../runtime/RyuPy.Runtime/PyModules.cs#L334)）：
   - 命中 `PyProvider.Current.Owns(name)`（[CPythonHost.cs](../runtime/RyuPy.CPython/CPythonHost.cs#L234-L237)：`entryScript != null && name == "__main__"`，或 `modules` 精确匹配/前缀子模块）→ 转入 `CPythonHost.Import(name)`（[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs#L9-L18)）。
   - 若为委派入口 `__main__`（首次）：调用 `Native.rp_run_entry(entryScript, source)` 编译执行入口源码；随后调用 `Native.rp_op(1, s: name)`（`PyImport_ImportModule(name)`），以 `ModuleFromOwned` 包装为同一权威 `PyModule` 视图（[PyProvider.Module](../runtime/RyuPy.Runtime/PyForeignObject.cs#L86-L88)）。

### 4.2 CPython → 编译域反向导入链与模块状态同步

`[静态证据]`
1. 编译模块注册时（`PyModules.Register` / `RegisterNativeLazy` / `PyModuleInstances` / `PyModuleLocations` / `PyModuleResourceLocations`）均通知 `PyProvider.Current?.Registered(name)` → `CPythonHost.Registered(name)` 将非 `Owns` 模块加入 `compiled` 集合（[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs#L47-L48)）。
2. 初始化前 `PyModules.InitializeRegisteredModule` 调用 `PyProvider.Current?.Initializing(module)` → `CPythonHost.Initializing`（[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs#L50-L55)）经 `rp_op(23, pointer, s: module.Name)` 将半初始化的 `ManagedObject` 代理直接写入 CPython `sys.modules[name]`；初始化抛错时 `Failed(name)` 经 `rp_op(30)` 清理 `sys.modules[name]`（[CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs#L57)）。
3. 当 CPython 代码执行 `import compiled_mod` 且该模块尚未初始化时，命中 `sys.meta_path[0]` 的 `_RyuPyFinder.find_spec` → 调用 C 回调 `_rp_has`（`Dispatch(-1, 20)` 检查 `compiled.Contains`）→ `_rp_package`（`Dispatch(-1, 22)`）→ `_rp_util.spec_from_loader(..., _RyuPyLoader(), ...)` → `_RyuPyLoader.create_module` → `_rp_load`（`Dispatch(-1, 21)`）→ 回调 `PyModules.Import(moduleName)`，回到 CLR 初始化并共享同一模块身份。

### 4.3 原生扩展内部触发的导入

`[静态证据]` + `[动态证据]`
- 实测内置扩展 `_sqlite3` 在 `PyModule_ExecDef` 阶段通过 `PyImport_ImportModule("functools")` 触发标准库 Python 模块导入（见第 7 节动态实验）。
- 实测 `NumPy 2.3.5`（19 个 `.so`）中 `_multiarray_umath` 与 Cython 生成的 `numpy/random/*.so` 均直接调用 `PyImport_ImportModule` / `PyImport_Import` / `PyImport_ImportModuleLevelObject`（见第 7 节符号清单）。

---

## 5. 跨域回调链与异常/生命周期边界

`[静态证据]`
1. **静态非托管回调入口**：`CPythonHost.Start` 将 `[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]` 方法 `Dispatch`（[CPythonCallbacks.cs](../runtime/RyuPy.CPython/CPythonCallbacks.cs#L10-L101)）传入 `rp_start`。
2. **`ryupy.ManagedObject` C 类型槽位**：[ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c#L130-L209) 定义 14 个槽位，通过 `forward(self, op, ...)` 转发至 `dispatch(self->identity, ...)`：
   - `op=0`: `managed_dealloc`（释放 `Roots[identity]` 与 `reverse` 缓存）
   - `op=1`: `managed_call_slot`（`PyObject_Call` → `PyOps.CallValue`）
   - `op=2/3/4`: `managed_getattr` / `managed_setattr`（set/del）
   - `op=5/6/7`: `managed_item` / `managed_setitem`（set/del）
   - `op=8/9/10/11/12/13/14/15`: `len`、`bool`、`repr`、`str`、`iter`、`next`、`hash`、`richcompare`。
3. **跨域方法与属性回调链**：
   - 编译类继承 CPython 类型时，`ConvertClassMemberOwned`（[CPythonClassBridge.cs](../runtime/RyuPy.CPython/CPythonClassBridge.cs#L355-L406)）将 `PyFunction` / `PyStaticMethod` / `PyClassMethod` / `PyProperty` 包装为 `ForeignMethodTarget`（实现 `IPyNativeCallable`，[CPythonClassBridge.cs](../runtime/RyuPy.CPython/CPythonClassBridge.cs#L577-L588)），**默认经过 Python helper `_rp_wrap_method`；显式原生方法候选使用静态 C 描述符**。
   - 当 Tk 事件循环、NumPy `frompyfunc` 或 CPython `super()` 调用该方法时：C 栈 → Python `_rp_method(*args, **kwargs)` 帧 → `managed_call_slot (op=1)` → `Dispatch` → `Enter(callback: true)` → `ManagedHandledScope` → `PullContainers()` → `ForeignMethodTarget.Invoke` → `PyObject.InvokeBound`（恢复 `PyMethodContext` 支持零参 `super()`）→ `PushContainers()` → `ToPython` / `RestoreCallbackError` → `PyTraceback.PopTo(callbackDepth + 1)` → 退出租约。
4. **关闭钩子回调链**：`Py_FinalizeEx` 触发 `atexit` 注册的 `ManagedObject` 回调时，以 `state == 2 / 4` 的 `ownerThread` 租约重入 `Dispatch`（[CPythonHost.cs](../runtime/RyuPy.CPython/CPythonHost.cs#L138)）。

---

## 6. libpython 启动 / 关闭 / Frozen 链与 C0 审计实证

### 6.1 启动链与关闭链全路径

`[静态证据]`
1. **启动链**：
   - 生成的 CIL `Main(string[] args)`（[entry.rs](../crates/ryupy-codegen-cil/src/entry.rs#L83-L147)）：`PySystem.Initialize` → `Bootstrap.Start("ryupy-cpython.json", args)`（[Bootstrap.cs](../runtime/RyuPy.CPython/Bootstrap.cs#L17-L25)）→ `CPythonHost.Start`（[CPythonHost.cs](../runtime/RyuPy.CPython/CPythonHost.cs#L75-L126)）。
   - `CPythonHost.Start`：读取并校验配置 → `dlopen(RTLD_NOW | RTLD_GLOBAL)` / `LoadLibraryExW(0x1100)` 载入固定 `libpython` 与 `shim` → 核对 `Py_GetVersion()` 与 `rp_build_version()` → `ConfigureExecutionAudit()`（工作树新增，环境变量 `RYUPY_CPYTHON_EXECUTION_AUDIT=1`）→ `TclTkEnvironment.Configure`（隔离 `TCL_LIBRARY` / `TK_LIBRARY`）→ 调用 `Native.rp_start(...)` → `InstallImports()` → 若审计开启则切换阶段 `rp_execution_audit_phase(1)`（runtime）。
   - `rp_start`（[ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c#L217-L251)）：`PyPreConfig_InitIsolatedConfig`（`utf8_mode = 1`）→ `Py_PreInitialize` → `PyConfig_InitIsolatedConfig`（`site_import = 0`, `write_bytecode = 0`, `parse_argv = 0`, `install_signal_handlers = 0`, `module_search_paths_set = 1`, `home`, `stdio_encoding="utf-8"`, `stdio_errors="surrogateescape"`, `module_search_paths`, `argv`）→ `Py_InitializeFromConfig(&config)` → `PyType_FromSpec(&managed_spec)` → `PyEval_SaveThread()`。
2. **关闭链**：
   - CIL `Main` 统一汇合点调用 `Bootstrap.Finish(code)`（[Bootstrap.cs](../runtime/RyuPy.CPython/Bootstrap.cs#L27-L38)）→ `Builtins.FinishExit(code)` → `Bootstrap.Stop()` → `CPythonHost.Dispose()`（[CPythonHost.cs](../runtime/RyuPy.CPython/CPythonHost.cs#L189-L223)）。
   - `CPythonHost.Dispose()`：校验 `leaseDepth == 0` 与 `ownerThread` → `state = 2` 等待 `active == 0` → `Enter()` 切 `rp_execution_audit_phase(2)`（工作树新增）、`CaptureModulePaths()`、`ReleaseClassBridgeResources()`、`Drain()` → `state = 4` → `Native.rp_stop()`（`PyEval_RestoreThread` + `rp_execution_audit_phase(2)` + `Py_FinalizeEx()` + `audit_finalized = audit_enabled`）→ 清理 `Roots` / `objects` / 恢复 `TCL_LIBRARY`/`TK_LIBRARY` → `WriteReport()`（当设置 `RYUPY_CPYTHON_REPORT` 时写出 JSON，工作树新增 `execution_audit` 段）。

### 6.2 Frozen 与磁盘 Stdlib 启动执行实证（CPython 3.12.13）

本轮使用仓库外 `/tmp` 只读探针与本机缓存 PBS `cpython-3.12.13`（`~/.cache/ryupy/cpython-auto/local-3.12.13-20260623-aarch64-apple-darwin`）动态观测到如下精确行为：

1. **`python3.12 -I -S -B -v` 与等效 C `rp_start` 配置动态观测 `[动态证据]`**：
   - `Py_InitializeFromConfig` 初始化后，`sys.modules` 已包含 **22** 个模块：`__main__`, `_abc`, `_codecs`, `_frozen_importlib`, `_frozen_importlib_external`, `_imp`, `_io`, `_thread`, `_warnings`, `_weakref`, `abc`, `builtins`, `codecs`, `encodings`, `encodings.aliases`, `encodings.utf_8`, `io`, `marshal`, `posix`, `sys`, `time`, `zipimport`。
   - 其中明确执行了 6 个 **frozen** Python 模块（`_frozen_importlib`, `_frozen_importlib_external`, `zipimport`, `codecs`, `abc`, `io`）以及 3 个 **磁盘 `.py/.pyc` 模块**（`encodings`, `encodings.aliases`, `encodings.utf_8`）。
   - **重要正向事实**：由于 `rp_start` 设置了 `module_search_paths_set = 1` 与 `home`，通过 `lldb` 对 `_Py_Get_Getpath_CodeObject` 下断点实测命中次数为 **0**，证明 frozen `getpath.py` 未被执行。
2. **`lldb` 对 `_PyEval_EvalFrameDefault` 断点计次 `[动态证据]`**：
   - 仅执行 `rp_start` 参数的 `Py_InitializeFromConfig` + `Py_FinalizeEx`（不含 `rp_install_imports`）：`_PyEval_EvalFrameDefault` C-to-Python 入口命中 **384** 次；其首帧栈为 `Py_InitializeFromConfig → pycore_interp_init → _PyImport_InitCore → PyImport_ImportFrozenModuleObject → exec_code_in_module → PyEval_EvalCode → _PyEval_EvalFrameDefault`。
   - 加上模拟 `rp_install_imports` 后再执行 `Py_FinalizeEx`：`_PyEval_EvalFrameDefault` 命中 **541** 次（`+157` 次）。
3. **`Py_FinalizeEx` 关闭期执行链 `[动态证据]`**：
   - 未注册 Python `atexit` 且未导入 `threading` 时，`shutdown` 期仅执行 `sys` / `builtins` / 模块 dict 清理（`# clear ...` / `# cleanup[2]` / `# cleanup[3]`），Python 帧数为 `0`；若用户或第三方库注册了 `atexit` 回调或导入了 `threading`（触发 `threading._shutdown`），则会在 `Py_FinalizeEx` 期间进入 Python 帧执行。

### 6.3 C0 原生 PEP 523 执行审计实证与严格门边界（工作树候选）

`[静态证据]` + `[动态证据]`
- **实现机制**（[execution_audit.h](../runtime/RyuPy.CPython/native/execution_audit.h#L1-L119)、[CPythonExecutionAudit.cs](../runtime/RyuPy.CPython/CPythonExecutionAudit.cs#L1-L55)）：
  - 仅支持固定 CPython `3.12.13` release/GIL（读取 `<internal/pycore_frame.h>` 的 `_PyInterpreterFrame.f_code`，非 `3.12.13` 开启时 `rp_execution_audit_start()` 返回错误并保留固定诊断 `"execution audit requires exact CPython 3.12.13 release/GIL shim"`）。
  - 在 `Py_InitializeFromConfig` 之前调用 `PySys_AddAuditHook(audit_event, NULL)`；当最早具备非空 `_PyThreadState_UncheckedGet()` 的原生 `PySys_Audit` 事件到达时，通过 `_PyInterpreterState_SetEvalFrameFunc(interpreter, audit_eval)` 安装 PEP 523 帧求值 hook（并将 CPython 3.12 的帧内联调用切出默认快路径，从而真实统计每一个 Python-to-Python 与生成器恢复帧）。
- **本轮实测数据 `[动态证据]`**：
  - 运行 `python3 -B tests/cpython/execution_audit.py --python .../python3.12 --unsupported-python .../python3.13` 通过（exit 0）：
    - 观测计数：**`startup = 839`、`runtime = 1544`、`shutdown = 6`**（覆盖直接调用、原生反向回调及异常、`resumed_generator` 4 次进入、`threading.Thread` 未来线程、`atexit`）；
    - 验证 `disabled` / `late-enable` 负例（均 0 帧且输出拒绝诊断）以及 `3.13.13` 开启审计返回退出码 3 拒绝、禁用审计默认宿主仍可通过。
  - 运行 `python3 -B -m unittest discover -s tests/cil_execution -p 'test_*.py' -v` 通过（6/6 tests OK）。
  - 主代理 C# 类桥 6 项集成测试（`CPythonClassBridgeTests`）在启用 `RYUPY_CPYTHON_EXECUTION_AUDIT=1` 下真跑报告：**`startup = 1299`、`runtime = 839`、`shutdown = 0`**；同时针对 C1 候选 [ADR-0209](adr/0209-base-object-native-descriptors.md) 验证，已移除的 4 个 `_rp_object_*` 转发函数帧计数为 **0**。
- **盲区与严格门拒绝结论 `[静态证据]`**：
  - 由于 PEP 523 钩子只能在首个带 `tstate` 的 `PySys_Audit` 事件之后安装，**初始化最前端（如 `_PyImport_InitCore` 加载 `_frozen_importlib` 等早期帧）存在启动盲区**；此外若外部 C 扩展或内部代码直接调用 `_PyEval_EvalFrameDefault` 或另建子解释器，也存在绕行风险（`audit_replaced` 仅在阶段切换或后续审计事件时检查 hook 是否被替换）。
  - 因此报告固定输出 **`complete = false`**、**`startup_complete = false`**，[tests/cil_execution/check_audit.py](../tests/cil_execution/check_audit.py#L18-L21) 严格门明确拒绝将当前或任何仅靠半程审计得到的报告晋升为零字节码执行。**当前任何产物均不得宣称无 frozen 或零字节码执行。**

---

## 7. 底层 ABI 消费者盘点

### 7.1 内部托管互操作层消费者：`RyuPy.CPython`

`[静态证据]` [Native.cs](../runtime/RyuPy.CPython/Native.cs#L8-L80) 声明了 **75** 个 `DllImport("ryupy_cpython")` 导出（基准 `5d36db1e` 为 **67** 个；新增审计 5 项、原生方法描述符 1 项、精确 super 分类 2 项），对应 [ryupy_cpython.c](../runtime/RyuPy.CPython/native/ryupy_cpython.c) 消费的 CPython C API 分为五类：

1. **生命周期与配置**：`PyPreConfig_InitIsolatedConfig`、`Py_PreInitialize`、`PyConfig_InitIsolatedConfig`、`PyConfig_SetBytesString`、`Py_DecodeLocale`、`PyWideStringList_Append`、`Py_InitializeFromConfig`、`PyConfig_Clear`、`Py_IsInitialized`、`Py_FinalizeEx`、`Py_GetVersion`。
2. **GIL 与线程状态**：`PyGILState_Ensure`、`PyGILState_Release`、`PyEval_SaveThread`、`PyEval_RestoreThread`；以及审计专用的 `<internal/pycore_frame.h>`、`_PyThreadState_UncheckedGet`、`PyThreadState_GetInterpreter`、`_PyInterpreterState_GetEvalFrameFunc` / `SetEvalFrameFunc`、`PySys_AddAuditHook`。
3. **对象、数值、容器与缓冲**：`Py_NewRef` / `Py_XDECREF` / `Py_INCREF` / `Py_DECREF`、`PyType_FromSpec`、`PyObject_New`、`PyLong_*`（含 `PyLong_AsLongLongAndOverflow`、`PyNumber_ToBase`）、`PyFloat_*`、`PyBool_FromLong`、`PyUnicode_DecodeUTF8` / `PyUnicode_AsUTF8AndSize`、`PyBytes_FromStringAndSize` / `PyBytes_AsStringAndSize`、`PyTuple_*`、`PyList_*`、`PyDict_*`（含 `PyDict_GetItemWithError`、`PyDict_Next`）、`PySlice_New`、`PySet_CheckExact` / `PyFrozenSet_CheckExact`。
4. **协议、模块与执行入口**：`PyObject_GetAttrString` / `SetAttrString` / `DelAttrString` / `Call` / `GetItem` / `SetItem` / `DelItem` / `GetIter` / `Repr` / `Str` / `Type` / `RichCompare` / `IsInstance` / `IsSubclass` / `IsTrue` / `Length` / `Hash` / `Dir`、`PyIter_Next`、`PySequence_Tuple` / `Contains`、`PyNumber_*`（含全部 InPlace 槽）、`PyEval_GetBuiltins`、`PyImport_ImportModule`、`PyImport_GetModuleDict`、`PyImport_AddModule`、`PyModule_New`、`PyModule_GetDict`、`PyCFunction_New`、`PyRun_String`（E2/E3）、`Py_CompileStringObject` + `PyEval_EvalCode`（E1）。
5. **异常与 Traceback 布局**：`PyErr_*`（`Occurred` / `Clear` / `GetRaisedException` / `SetRaisedException` / `GetHandledException` / `SetHandledException` / `SetString` / `GivenExceptionMatches` / `NewException`）、`PyException_*`（`GetArgs` / `GetCause` / `SetCause` / `GetContext` / `SetContext` / `GetTraceback`）、以及直接读写 `PyBaseExceptionObject.suppress_context` 与 `PyTracebackObject`（`tb_next` / `tb_frame` / `tb_lineno` / `tb_lasti`）+ `PyFrame_GetCode` + `PyCode_Addr2Line`。

### 7.2 标准库内建/动态扩展、第三方二进制扩展与 C ABI 消费者

| 消费者类别 | 实测对象（来源） | 关键依赖的 CPython ABI（静态符号 / 动态行为证据） | 对无字节码内核的约束 |
| :--- | :--- | :--- | :--- |
| **内建 C 模块（PBS `libpython`）** | `_sqlite3` 等（`PYTHON.json` 列出 106 项内建模块） | `[动态证据]` `_sqlite3` 在 `PyModule_ExecDef` 时调用 `PyImport_ImportModule("functools")` | 必须让 `PyImport_ImportModule` 路由到 RyuPy 已编译 CIL/Runtime 的 `functools`，而非走磁盘 `.py` |
| **`lib-dynload` 扩展** | `_tkinter.cpython-312-darwin.so`、`_dbm.cpython-312-darwin.so`、`SanBudget` 消费 | `[静态证据]` `nm -u`：`_tkinter` 依赖 `_PyType_FromSpec`、`_PyEval_SaveThread`/`RestoreThread`、`_PyErr_*`；`_dbm` 依赖 `_PyType_FromModuleAndSpec` | 保留 GIL 释放/重入、C `PyType_FromSpec` 与异常 C API；事件回调进入 `ManagedObject.tp_call` |
| **NumPy 2.3.5 核心与随机扩展** | `numpy/_core/_multiarray_umath.cpython-312-darwin.so`、9 个 `numpy/random/*.so` | `[静态证据]` `nm -u`：`_multiarray_umath` 导入 `_PyImport_ImportModule`、`_PyImport_Import`、`_PyEval_GetBuiltins`、`_PyBuffer_Release`、`_PyMemoryView_*`；**全部 9 个 Cython 生成的 `numpy/random/*.so` 均直接导入 `_PyCode_NewEmpty`、`_PyFrame_New`、`_PyImport_ImportModuleLevelObject`、`_PyType_FromMetaclass`** | **决定性约束**：内核**必须**保留 `PyCodeObject`/`PyFrameObject` C 结构 ABI（供 Cython traceback 构造）且**必须**让 `PyImport_ImportModuleLevelObject` 能在无 `importlib` 字节码时工作 |
| **FastAPI 技术栈扩展** | `pydantic-core==2.50.4`、`greenlet==3.5.6`、`SQLAlchemy==2.0.52` | `[静态证据]` macOS arm64 的 CPython 3.13.3 真实 wheel 已扫描：pydantic-core 单扩展有 161 个 Python 未定义符号，含 `PyEval_SaveThread/RestoreThread`；greenlet 生产扩展有 83 个，含 `PyContext_Type`、`PyThreadState_GetFrame`、`PyFrame_GetBack`；SQLAlchemy 五个扩展共 216 个，含 `PyCode_NewEmpty`、`PyFrame_New`、`PyUnstable_Code_NewWithPosOnlyArgs` | 保留实际版本对象/帧/线程 ABI。符号扫描用于登记静态消费者；C3/C4 继续逐库验证真实调度、布局与回调 |
| **RyuPy 直接 C ABI 绑定** | `native_binding.rs`（v1–v3 声明）与 [PyNativeBindings.cs](../runtime/RyuPy.Runtime/PyNativeBindings.cs) | `[静态证据]` JIT `DllImport` / AOT `DirectPInvoke`，经 `CheckedI32` + `ReadExactScalar` 仅允许精确整数标量入 C | 本身不依赖 `libpython`；仅当外域 `PythonObject` 传入时经 `IPyForeignObject.ReadScalar` 读标量 |

---

## 8. 严格无字节码产品启动内核方案（明确选型与维护成本）

### 8.1 明确选型与路线区分

> **选型结论（唯一路线，非多选并列）**：严格零字节码交付产品（C4/C5 目标）选定**「基于上游 CPython 固定发布源码构建的可重建最小 `libpython` 内核（Rebuilt Minimal `libpython` Kernel）＋ `PyConfig._install_importlib = 0` 原生启动模式」**，首切固定 `CPython 3.12.13`（x64/arm64 常规 GIL）。

**选定路线 vs 现成产品的明确区分**：

1. **现成产品（当前与 C1–C3 过渡态）**：使用未修改的上游 `python-build-standalone`（`3.12.13+20260623` 与 auto 3.9–3.15）共享 `libpython` 与完整基础标准库。
   - **现状 `[动态证据]`**：`rp_start` 仍执行 frozen `_frozen_importlib`/`zipimport`/`codecs`/`abc`/`io` 与磁盘 `encodings`（`startup` 计帧 384–1299 次），且 `execution_audit` 为 `complete=false`、`startup_complete=false`。**当前不可宣称无 frozen 或零字节码**。
2. **为什么不能仅靠「现成未修改 PBS `libpython` + `_install_importlib = 0`」作为严格产品方案？**
   本轮在缓存的 `libpython3.12.dylib` 上通过临时 C 探针与 `lldb` **动态实证**了 `_install_importlib = 0` 的可行边界与三处硬阻断：
   - **已证实的正向基础 `[动态证据]`**：设置 `PyConfig_InitIsolatedConfig` + `module_search_paths_set = 1` + `home` + `_install_importlib = 0`，实测在 `CPython 3.12.13`、`3.13.13`、`3.15.2` 上均能成功完成 `Py_InitializeFromConfig` 与 `Py_FinalizeEx`；在 `3.12.13` 下 `lldb` 观测 `_PyEval_EvalFrameDefault` 与 `_Py_Get_Getpath_CodeObject` 命中次数均为 **0**；注册 C 版 `builtins.__import__` 后，内建多阶段模块 `math.sqrt(16.0) = 4.0` 在 **0 次** `_PyEval_EvalFrameDefault` 下成功执行，且 `_sqlite3` 初始化可被截获到 `c_import("functools")`。
   - **已证实的现成库硬阻断 `[动态证据]`**：
     1. **`PyImport_ImportModuleLevel("foo3", ...)` / `PyImport_ImportModuleLevelObject` 直接失败**：在 `_install_importlib = 0` 下绕过 `builtins.__import__` 直接读取 `interp->imports.importlib`（为 `NULL`），抛出 `SystemError: null argument to internal routine`。而上文已静态证实 **全部 9 个 `numpy/random/*.so` 均调用 `_PyImport_ImportModuleLevelObject`**，现成未改 `libpython` 无法承载 NumPy！
     2. **`sys.stdin` / `sys.stdout` 为 `NULL`**（仅 `sys.stderr` 为裸 `stdprinter`），无完整 UTF-8 C stdio 桥接；
     3. **二进制残余与审计盲区**：现成 `libpython` 仍内嵌 frozen 字节码数据，且不改 `libpython` 无法堵住初始化极早期帧与直接调用 `_PyEval_EvalFrameDefault` 的绕行风险（无法满足 `complete=true` 与 `startup_complete=true` 门禁）。

### 8.2 选定「可重建最小 `libpython` 内核」的四大构件

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
| **上游补丁维护面** | 首轮实际覆盖 `import.c`、`frozen.c`、`ceval.c`、`pylifecycle.c`、`codecs.c`、`getpath.c`、`pycore_ceval.h` 与 `Makefile.pre.in`；另注入纯 C UTF-8 codec/增量类型头 | 不改动对象模型、GC、字典/列表/数字布局，codec 与增量类型约六百余行 C，求值器/导入/启动锚点另列；版本扩展需逐处复验 |
| **C 扩展 ABI 布局兼容性** | 第三方 wheel（`numpy/random/*.so`、`pydantic-core`、`greenlet`）对 `PyTypeObject`、`_PyInterpreterFrame`、`PyThreadState` 有强结构假设 | 严格保持与上游 3.12.13 完全一致的编译宏（常规 GIL、release 非 `Py_DEBUG`）与头文件路径，每版按真实 wheel 门禁验收 |
| **开源许可与随包元数据** | 即使剥离 Python 标准库源码，内建 C 扩展（OpenSSL、SQLite、zlib-ng、expat、libffi、mpdecimal 等）仍留在 `libpython` 中；本机 `PYTHON.json` 显示需保留 **21** 份组件许可证 | 继续沿用 [cpython_deployment.rs](../crates/ryupy-driver/src/cpython_deployment.rs) 与 `PYTHON.json` + `licenses/` 校验链，无新增合规黑洞 |

---

## 9. C1 首个最窄替换合同

### 9.1 边界澄清：已落地候选（ADR-0209）与 C1 下一首个最窄切片

1. **已落地工作树候选（[ADR-0209](adr/0209-base-object-native-descriptors.md) — `object` 四项基础槽）**：
   - **改动**：从 [CPythonImports.cs](../runtime/RyuPy.CPython/CPythonImports.cs) 移除 `_rp_object_new`、`_rp_object_init`、`_rp_object_setattr`、`_rp_object_delattr`；在 [CPythonClassBridge.cs](../runtime/RyuPy.CPython/CPythonClassBridge.cs#L519-L529) 直接获取 `object` 原生描述符并缓存。
   - **实测证据 `[动态证据]`**：主代理验证 4 个转发函数帧计数为 **0**，[BaseObjectDescriptors_PreserveAllocationIdentityAttributesAndErrors](../runtime/RyuPy.CPython.Tests/CPythonClassBridgeTests.cs#L146-L168) 定向通过。
   - **验收**：集成 `make check` 已通过；固定 CPython 同次差分与单继承 JIT/NativeAOT 删除输入搬迁已通过。

2. **C1-S1：super helper 已移除**
   - 缓存真实 `super` builtin，复用现有静态调用及属性 C API，未新增构造用原生 ABI。
   - Runtime 以中性 `PySuper` 统一本地/跨域、未绑定及外部反向返回对象的身份。
   - 真实外部 C3 承担 Mixin 顺序、属性遮蔽和晚改同步，普通 MI AOT 保护门保留。
   - 单继承 `native_super` 已过 CPython/JIT/NativeAOT/删除输入搬迁；`native_super_mro`
     仅按 JIT 专项验证。默认及原生方法候选的真实 C# 类桥各 14 项通过。

3. **C1-S2：静态方法描述符候选**
   - `RYUPY_CPYTHON_NATIVE_METHODS=1` 用 `PyType_Spec` 描述符替换 `_rp_wrap_method`。
   - `native_method_descriptors` 已过 CPython/JIT/NativeAOT/删除输入搬迁，精确 code/frame
     消费者保留明确诊断。候选与默认分别维护，见 [ADR-0211](adr/0211-native-method-descriptor-candidate.md)。

4. **后续 C1 切片**
   - 静态元类解析与类构造替换 `_rp_calculate_meta` / `_rp_make_class` 及三个描述符别名。
   - 静态 finder/loader 替换 `_RyuPyFinder` / `_RyuPyLoader`，最终删除内部 `rp_install_imports`。

---

## 10. 剩余实现项明确清单

### 10.1 C0 收口记录

- 完整 `make check` 已通过：3368 项 C#、487 项编译 e2e；固定二十项目为
  13 native / 6 unsupported / 1 failed，既有 11 项保护全部保留。
- 比较基准 `5d36db1e` 经 `git archive` 提取到仓库外并独立构建冻结 CLI，26 项
  NativeAOT、同次固定 CPython 与全部样本已采集。首轮候选也完成 26 项，发现
  缺省调用新增 Binder 成本；C2 已增加守卫快路，普通 super 已恢复两引用布局，
  最新候选的完整六次平衡配对已完成，26 项功能/搬迁通过；完整结果和未决单项见
  [首轮集成验收](cil-execution-first-slices-validation.md)，记录于 ADR-0212 / ADR-0211。
- FastAPI 的真实 CPython 3.13.3 wheel 静态 ABI 扫描已补齐，见 §7；两个真实项目
  的 JIT/standalone/onefile 删除输入搬迁在本轮候选通过，SanBudget 保留原业务 CIL 路径。
- `runtime/kernel` 已按 §8 路线源码重建，纯 C UTF-8 / `_io` 标准流及严格拒绝保护共十五模式通过；现成 provider 的帧审计继续
  保持 `complete=false`，严格内核产品接入及完整执行覆盖归 C4。

### 10.2 C1–C5 后续阶段剩余实现项

- **C1（内部零 Python 胶水）**：object/super helper 已移除，原生方法描述符保留显式候选；继续替换 `_rp_make_class`/`_rp_calculate_meta`、`_RyuPyFinder`/`_RyuPyLoader` 与 `rp_install_imports`，删除死分派 `rp_op(29)`；保障 `SanBudget --compiled-app`、21 项核心与 6 项生命周期探针不回退。
- **C2（元数据、动态对象与反射）**：生成函数签名/默认值/注解/闭包元数据；实现 `inspect.signature`、装饰器解包与 `attrs` 活字典 `__dict__` 合同；独立建模 `__file__` / 资源根 / 用户可写数据根，移除 `compiled_sources` 随包 `.py` 锚点并定义 `inspect.getsource` 明确诊断。
- **C3（原生协程状态机与异步运行时）**：MIR 显式协程/awaitable 状态机，依次覆盖 `await`、`async with`、`async for`、异步生成器与事件循环/任务取消/contextvars，打通 `FastAPI` 无 `--cpython-entry` 与无 `--cpython-module` 原生编译运行。
- **C4（完整闭包编译与底层 libpython 内核）**：编译应用、纯 Python 标准库（含 `tkinter`/`sqlite3`/`asyncio`/`inspect` 的 Python 层与 `PyTextProviderMethods.Title`）及第三方包装层；接入第 8 节选定的**可重建最小 `libpython` 内核**，消除 6 个 frozen 启动模块与磁盘 `encodings`，打通 `PyImport_ImportModuleLevelObject` 原生 C 路由，并使全生命周期审计达到 `complete=true, startup_complete=true, startup=0, runtime=0, shutdown=0`。
- **C5（默认晋升与旧路径退出）**：显式候选路径通过固定 20 项目、FastAPI/SanBudget、JIT/NativeAOT、三平台搬迁及性能保护门后，独立提交切换默认，再独立提交删除 `rp_run_entry`、源码委派及过渡载荷。
