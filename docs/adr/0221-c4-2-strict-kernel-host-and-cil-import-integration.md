# ADR-0221：C4-2 严格无字节码内核接入宿主与产品导入事务设计

- 日期：2026-11-16
- 状态：设计精化冻结（仅设计，不改既有源码/不构建/不测试；C1/C2/C3 并行实施中仅维护本 ADR 与同路径 pending）
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、[ADR-0216](0216-strict-kernel-native-stdio.md)、[ADR-0217](0217-strict-kernel-frozen-extensions-and-cil-import-abi.md)
- 范围：以最小冷态改动将 C4-1 严格 CPython 3.12.13 无字节码内核接入 `RyuPy.CPython`、`RyuPy.Runtime`、`ryupy-codegen-cil` 与 `ryupy-driver`/CLI（不动普通 `PyDict` 布局，不扩大其它 Python 版本或实现 `CodeType`）。

## 1. 背景与源码核查结论

C4-1 已验收官方 CPython 3.12.13 外部构建下的纯 C UTF-8/`_io`、默认求值器三阶段计数器与严格拒绝门（`Include/ryupy_kernel.h`）及纯 C 导入事务 ABI（`Include/ryupy_import_abi.h`、`ryupy_import_router.h`）。对照现有 Runtime/CPython 源码核查，C4-2 设计必须正面解决五类硬约束：

1. **普通 `PyDict` 布局禁扩**：`PyDict`（`Runtime/PyDict.cs`）是 `sealed` 类且明确注释不给普通字典增加额外字段、CWT 查询或分派分支；而 `PyModuleInstanceState`、`PyModuleExecutionContext` 均属 `internal`，对外公开的 `LoadName`/`StoreName`/`CallDirectGlobals`/模块 `__dict__`/函数 `__globals__` 均返回 `object?`。因此无需动普通 `PyDict` 布局，更不应把真实 CPython `md_dict` 镜像成 `PyDict`。
2. **`TypeOf` 精确字典身份缺口**：`PythonObject.ForeignType`（`CPython/PythonObject.cs:22`）调用 `FromOwned(rp_op(13, Pointer))` 会把 `&PyDict_Type` 包装成 `PythonObject` 而非 `PyType.Get("dict")`，导致 `type(globals()) is dict` 为 `False`，需复用 `CPythonSignatureValues.cs:126-130` 模式精确归一。
3. **`__builtins__` 类型收窄缺口**：`PyModuleInstances.Execute` 与 `PyFunctionOwner.CaptureDefinitionContext` 目前要求 `__builtins__ is PyDict`，遇到真实 CPython `PyDictObject*` 会抛 `NotImplementedError`。
4. **Runtime 导入旁路**：除 `PyModules.Import` 外，`PyModuleRelativeImport`（`RelativePackage` 硬检 `PyModuleSpec.IsOriginal`、`RelativeFromList` 直查 `PyModuleDiscovery`）与 `PyModuleView`（`__delitem__` 抛未实现）仍走托管 `Cache`/`Registry`，strict 模式必须统一收口到 C4-1 内核唯一权威。
5. **进程级静态 P/Invoke 不可逆绑定**：`NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, ...)` 每进程仅可注册一次，且 `[DllImport(Native.Library)]` 永久缓存原生函数地址，同时 `libpython` 含进程全局解释器与静态类型状态；同进程绝不能通过改配置路径或重建 `CPythonHost` 在 `auto` 与 `strict-kernel` 间热切换。

## 2. 决定一：显式双通道分流与进程级 Profile 一次性锁定

### 2.1 兼容链路 vs 严格内核分流

| 维度 | 默认兼容模式（`auto` 及 3.9–3.15） | 严格无字节码模式（`strict-kernel`，首切 3.12.13） |
| --- | --- | --- |
| 启动配置 | `HostConfiguration` `schema=1`，`_install_importlib=1` | 显式 `strict-kernel`，`_install_importlib=0`，`isolated=1`，`use_environment=0` |
| 导入路由 | `rp_install_imports`（Python `_RyuPyFinder`/`_RyuPyLoader`） | 绕过 `rp_install_imports`，仅 C4-1 侧表 + `[UnmanagedCallersOnly]` 静态回调 |
| 方法与求值 | 按 `RYUPY_CPYTHON_NATIVE_METHODS`，含 `LegacyMethodWrapper`、`rp_op(29)` | 强制 `native-methods=true` 静态通道；移除 `LegacyMethodWrapper` 与 `rp_op(29)` 路径；未闭环语法编译期 `Diagnostic` |
| 审计证明 | `PySys_AddAuditHook`，固定 `complete=false` | `Py_InitializeFromConfig` 前置 `STARTUP` + `strict_reject=1`，全闭环输出 `complete=true` |
| 产物载荷 | 兼容配置与 PBS 目录 | **禁止 `.py`/`.pyc`**；必须包含 `LICENSE.Python.txt`、扩展依赖闭包与严格库 |

### 2.2 进程级一次绑定（`ProcessNativeBinding`）与顺序重建合同

- **禁止运行时动态卸载或函数指针反射**：不设计 `NativeLibrary.Free`/`dlclose(libpython)` 卸载重装，也不用 `Marshal.GetDelegateForFunctionPointer` 动态反射全表来绕过 `[DllImport]` 静态 ABI。
- **进程级一次性锁字段设计**：在 `CPythonHost` 所属层新增进程级静态守卫结构 `ProcessNativeBinding`（受进程级静态锁保护）：
  - 字段：`State`（`Unbound | Validated | BoundActive | BoundClosed`）、`Profile`（`Compat | StrictKernel`）、`LibraryPath`、`ShimPath`、`KernelVersion`、`ResolverInstalled`、`ShimHandle`、`PythonHandle`。
- **调用顺序与双负例门禁**：
  1. `CPythonHost.Start` **先于 `NativeLibrary.SetDllImportResolver` 及任何 `[DllImport]` 触发**进入 `ProcessNativeBinding.Acquire(requestedProfile, libraryPath, shimPath)`；
  2. 若当前处于 `BoundActive`/`BoundClosed` 且 `(Profile, LibraryPath, ShimPath)` 与已绑定值任一项不一致（例如先跑 `auto` 旧 shim 再起 `strict-kernel` 新 shim，或反向），**立即拒绝并抛出明确诊断异常**（负例 1：跨 profile 切换拒绝；负例 2：同 profile 切换另一 `libpython`/shim 路径拒绝）。CLI/JIT/Standalone/OneFile 产物每进程仅启动单一产品，不受同进程多 profile 测试约束影响；
  3. 仅在 `Unbound` 首次加载时先探测加载目标句柄并验证 capability/版本：strict 模式必须同时验证 `rp_kernel_version()`、`rp_kernel_import_register` 导出及 `Py_GetVersion` 为 `3.12.13`（负例 3：把普通 PBS 3.12.13 或 `3.13` 当作 strict-kernel 启动时在写锁与 `rp_start` 前即刻失败，避免半绑定污染）；校验通过后注册单次 `SetDllImportResolver` 并锁定身份。
- **同 profile 顺序 `CPythonHost` 重建独立验证**：仅当 `BoundClosed` 且三元组完全一致时允许在同一测试进程顺序重建 `CPythonHost`。重建必须独立验证：
  1. `SetDllImportResolver` 仅注册一次且复用既有 `ShimHandle`，不触发第二次注册异常；
  2. 原 `Host` 的 `Roots`、`objects`、`reverse`、`handles`、`imported`、`_mixinMirrors` 与 `_strictBuiltinsDict` 已随前次 `Dispose()` 清零，新 `Host` 不残留已释放 `PythonObject`（`Host != newHost`）；
  3. 原生 `rp_stop()` 返回时已清空 C 侧静态类型槽（`rp_managed` 等）、重置 `rp_kernel_reset_eval_stats()` 与 `rp_kernel_set_phase(STARTUP)`；`RyuPy.Runtime` 侧测试重置路径与 `PyProvider.Remove` 已脱离旧宿主。

## 3. 决定二：严格内核分发准备、许可证、`relocate` 与构建期 ABI 门禁

- **严格发行准备**：`ryupy-driver` 仅从显式 `strict-kernel` 前缀/构建源码生成或读取工件，绝不静默探测系统 Python；部署载荷含严格 `libpython`、strict shim、`LICENSE.Python.txt`、显式原生 `.so`/`.pyd` 及其非系统传递动态库闭包；排除全部 `Lib/*.py` 与 `__pycache__/*.pyc`。
- **构建期重定位与 ABI 校验**：扩展 `cpython_native::relocate` 处理全部 `.so`/`.pyd` 及其依赖动态库，保证 **删除源码输入目录与私有缓存 → 搬迁至中文/空格路径 → 清空 `PATH` 与污染 `PYTHONHOME`/`PYTHONPATH`** 自洽运行。收集扩展时核验 PEP 3149 ABI 标签（3.12.13 仅许 `.cpython-312-*` / `.abi3.*` 等合法目标，**拒绝 `cp313` 扩展**）与 `PyInit_<leaf>` 符号，失配即发编译期 `Diagnostic`；不做普通文件哈希校验。
- **冻结清单**：`ryupy-codegen-cil`（不依赖 HIR/`ty`）在严格模式下强制所有 CIL 模块使用 `ModuleExecutionKind::InstanceGlobals` 并输出冻结模块清单（`name`, `is_package`, `package_dir`）；未闭环静态槽或动态求值语法直接发射编译期 `Diagnostic`。

## 4. 决定三：最窄冷态 Namespace 方案——零改动 `PyDict` 与全协议核查

### 4.1 方案比较与选定（明确放弃修改 `PyDict`）

| 备选方案 | 数据布局与热路径影响 | 身份与语义后果 | 裁决 |
| --- | --- | --- | --- |
| **方案 A（否定）**：在 `PyDict` 加 `_backend` 字段或 `ConditionalWeakTable` | **污染全部普通字典**：`PyDict`（sealed）每个实例增加字段，或每次 `Get`/`TryGet`/`Set`/`Entries`/`PyIter` 增加 CWT 查询/空分支虚分派，违背既有无额外字段约束 | 若把真实 `md_dict` 再包一层 `PyDict`，将制造 CLR `PyDict` 与 CPython `PyDictObject*` 双身份分裂 | **拒绝** |
| **方案 B（否定）**：每次边界双向 `ContainerTransfer` 快照镜像 | 跨调用 Diff 开销大，仅支持标量/标量元组键 | C 扩展 `PyModule_GetDict` 与 CIL `globals()` 并发读写立刻失真，破坏实时身份 | **拒绝** |
| **方案 C（选定）**：仅重构冷态 `internal PyModuleInstanceState` / `PyModuleExecutionContext`，`globals()`/`__dict__`/`__globals__` 直接返回真实 CPython `dict` `PythonObject` | **普通 `PyDict` 数据布局与热路径 `0` 改动**；公开 ABI（`LoadName`/`StoreName`/`DeleteName`/`CallDirectGlobals`）本已返回 `object?`，签名不变；仅冷模块状态持有 `(PyDict? NativeGlobals, IPyModuleNamespace? ForeignGlobals)` 并在访问入口走 `NativeGlobals` 快路 | `globals() is mod.__dict__ is fn.__globals__` 同指一个真实 CPython `md_dict` `PythonObject`（零镜像复制，与 C 扩展 `PyModule_GetDict` 同源） | **采纳** |

### 4.2 内部冷重构点与中性模块合同

1. **中性接口（`RyuPy.Runtime` 不接触 CPython 指针/头文件）**：
   - `internal interface IPyModuleNamespace`：仅限模块命名空间/builtins 消费：`object ViewObject { get; }`、`bool TryGet(string name, out object? value)`、`void Set(string name, object? value)`、`bool DeleteIfPresent(string name)`、`bool IsExactBuiltinFastPathUnchanged()`（可选快路判定）。
   - `internal interface IPyModuleTransactionHost`（或 `PyProvider` 扩展能力接口，**不破坏 `PyProviderAbiContractTests.cs` 既锁公共签名**）：提供 `ImportStrict(string name, object? globals, object?[] fromlist, long level)`、`TryCachedStrict(string name, out PyModule? module)` 与系统视图。
2. **`RyuPy.Runtime` 内部消费点定向收窄调整**（全部在 `RyuPy.Runtime` 内部，零公开破坏）：
   - `PyModuleInstanceState`：`PyDict? NativeGlobals`（纯 Runtime / 兼容模式唯一赋值，热路径直接调用）+ `IPyModuleNamespace? ForeignGlobals`（strict 模式指向 `md_dict` 包装器）；提供内部 `GlobalsObject`、`TryGetGlobal`、`SetGlobal`、`DeleteGlobal`（`LoadName`/`StoreName`/`DeleteName`/`GetAttr("__dict__")`/`SetAttr`/`DeleteAttr` 统一经此，普通 `PyDict` 实例不经过它）。
   - `PyModuleExecutionContext`：从 `(PyModuleInstanceState State, PyDict Builtins)` 放宽为内部 `(PyModuleInstanceState State, object Builtins)`；`LoadName` 先检查 `if (context.Builtins is PyDict nativeBuiltins)` 走原快路，否则通过 `IPyModuleNamespace` 查询真实外部精确字典；`PyFunctionMetadata.GetMetadata("__globals__" | "__builtins__")` 分别返回 `GlobalsObject` 与 `Builtins`（均为 `object?`）。
   - `PyCallerSource` / `ModuleExecutionSource`（`PyCallerSource.cs`、`PyTracebackOrigin.cs`）：内部 `PyDict? Globals` 放宽为 `object? Globals`，`TryReadName` 支持 `PyDict` 与 `IPyModuleNamespace`（仍用 `ReferenceEquals(state.GlobalsObject, globals)` 校对一致性）。

### 4.3 `__builtins__` 真实字典捕获与外部 `dict` 协议核查表

| 核查项 | 现行源码路径（核查事实） | Strict 模式行为与 C4-2 必需调整 |
| --- | --- | --- |
| **`__builtins__` 初始化与捕获** | `PyModuleInstances.cs:98-105` 与 `PyFunctionOwner.cs:31-38` 强检 `is not PyDict` 并抛 NI | strict `Execute` 缺 `__builtins__` 时写入宿主级真实 `PyDictObject*`（`_strictBuiltinsDict`，启动时由 `PyModuleBuiltins.Default` 预填）；`Execute` 与 `CaptureDefinitionContext` 放行 `PyDict` 或 exact CPython `dict`（`rp_container_kind == 2`），仍拒绝非 dict 值。可结合 `_strictBuiltinsDict` 版本标记：未改写前 `LoadName` 内建查表零 P/Invoke 直读 `PyModuleBuiltins.Default`，一旦 Python/C 修改立刻回落 live 读取 `md_dict["__builtins__"]` |
| **迭代 & 视图** | `PyIter.Create`（`PyIter.cs:82`）先匹配 `IPyForeignObject.Iterate()`；`PyObject.GetAttr`（`PyObject.cs:89`）& `PyCollectionMethodCall.cs:49` 对 `PyNativeObject` 调 `rp_op(2)` | 实时调用 CPython `PyObject_GetIter` 及 `dict.keys`/`values`/`items`/`get`；迭代期增删键准确触发 CPython `RuntimeError` |
| **`in`（`Contains`）** | `PyCollectionOps.Contains`（`PyCollectionOps.cs:136`）调 `PyForeignClassBridge.Contains` -> `rp_contains` | 实时执行 CPython `PySequence_Contains(md_dict, key)`，不可哈希键准确抛 CPython `TypeError` |
| **下标赋值 / 删键 / 属性同步** | `PyIndexing.GetItem`、`PyCollectionOps.SetItem`/`DelItem` 调 `PythonObject.Item`/`WriteItem`（`rp_op(6/7/8)`）；`PyModuleInstanceState.SetAttr`/`DeleteAttr` | `mod.__dict__["x"] = 1`、`del globals()["x"]`、`mod.x = 1` 与 C 扩展 `PyDict_SetItemString(md_dict, ...)` 物理直达同一 `md_dict*` |
| **`ToPython` exact dict 身份** | `CPythonValues.ToPython` 对 `PythonObject` 直接 `rp_incref(Pointer)` | 把 `globals()`/`mod.__dict__`/`fn.__globals__`/`__builtins__` 传回 C 扩展直接交出原始 `PyDictObject*`，`PyDict_CheckExact` 与 C 侧 `is` 为 `True` |
| **`PyObject.TypeOf` exact dict 身份** | **现行缺陷**：`PythonObject.ForeignType` 调 `FromOwned(rp_op(13, Pointer))` 返回 `PythonObject(&PyDict_Type)`，令 `type(d) is dict`（对比 `PyType.Get("dict")`）为 `False`！ | **必需修复**：`CPythonHost` 启动缓存 canonical `&PyDict_Type` 等内建精确类型指针（参照 `CPythonSignatureValues.cs:126-130`）；`ForeignType` 仅当 `typePtr == BuiltinDictTypePtr` 时映射为 `PyType.Get("dict")`（不改变 `class D(dict)` 派生类型）。之后 `type(globals()) is dict`、`type(mod.__dict__) is dict`、`isinstance(..., dict)` 均为 `True` |

## 5. 决定四：Runtime 全导入收口、两阶段事务与详细生命周期

### 5.1 Runtime 导入路径全部收口至 C4-1 内核唯一权威

- **严禁托管侧分裂第二套 `sys.modules`**：strict 模式下，`PySystem` 初始化将真实 CPython `sys` 与 `PyImport_GetModuleDict()` 暴露为 `sys.modules`（因此 `del sys.modules[name]` 直接执行 CPython `PyDict_DelItem`，`"x" in sys.modules` 实时读取内核）。
- **全面重定向入口**：`PyModules.Import`、`PyModuleImport`（`builtins.__import__`）、`PyModuleInstances.RelativeImport`（bypass 旧 `PyModuleRelativeImport.RelativePackage` 对 CLR `PyModuleSpec.IsOriginal` 的强依赖与 `RelativeFromList` / `PyModuleDiscovery`）、`PyImportlibUtil` 与 `PyModules.TryCached` 在 strict 模式下均转发至 C4-1 路由器（`PyImport_ImportModuleLevelObject` / C4-1 `rp_kernel_import_find_and_load`）；彻底禁用 `PyClosedSourceFallback`。

### 5.2 两阶段事务、失败部分模块保留与删缓存重导新身份

严格按 `ryupy_import_router.h:209-310` 顺序执行：

1. **`CreateModuleCallback`（`rp_cil_create_module_fn`）— 只构造映射，绝不执行、绝不进 `sys.modules`**：
   - 验证待建名不在内核 `sys.modules`；调用 `PyModule_NewObject` 创建真实 `PyModuleObject* modPtr`，读取 `PyModule_GetDict(modPtr)` 为 `dictPtr`（验证 `rp_container_kind == 2`）；
   - 通过 `FromBorrowed(dictPtr)` 创建/取得 `PythonObject`（`foreignDictProxy`）；
   - 创建 `PyModule`（`managedPeer`），挂载 `ForeignObject = PythonObject(modPtr)` 及 `PyModuleInstanceState(code, foreignGlobals: new ForeignNamespace(host, foreignDictProxy))`；
   - 登记 **`host.objects[modPtr] = new WeakReference<object>(managedPeer)`**（同时要求 `ToPython(PyModule)` 命中 `ForeignObject` 直接 `rp_incref(modPtr)`，**绝不落入 `rp_managed` 二次泛化代理**）；
   - **严禁调用 `RunInit()` 或写入 `sys.modules`**；返回 owned `modPtr`。
2. **内核发布窗口（`ryupy_import_router.h:242-255`）**：
   - C4-1 执行 `rp_kernel_init_module_attrs(spec, modPtr)` 将 `__name__`/`__package__`/`__spec__`/`__loader__`/`__path__`/`__file__` 直接写入 `md_dict*`（`managedPeer` 通过 `foreignDictProxy` 立即可见）；
   - C4-1 置 `spec->initializing = 1` 并唯一调用 `_PyImport_SetModule(abs_name, modPtr)` 写入 `sys.modules`。
3. **`ExecModuleCallback`（`rp_cil_exec_module_fn`）— 仅执行一次**：
   - 直接由参数 `mod` 经 `host.objects[mod]` 精确找回 `managedPeer`（不按名字反查），检查/初始化 `md_dict["__builtins__"]` 构造 `PyModuleExecutionContext`，精确执行一次 `code.Execute(context)`；
   - 执行中嵌套 `import`/`from . import` 重入 C4-1 路由器（同线程重入每模块锁，循环导入直接从 `sys.modules` 见已发布部分模块；跨线程环形依赖由 C4-1 等待图 DFS 立刻抛 `RuntimeError`）；
   - 若托管代码抛 `PyBaseException`，在 `[UnmanagedCallersOnly]` 边界内持 GIL 转换为 `PyErr_*`（保留类型/消息/异常链）返回 `-1`，绝不跨原生帧泄漏托管异常。
4. **失败删除缓存、部分模块存活与 `del sys.modules` 重导新身份**：
   - 若 `exec_module` 返回 `-1`，C4-1 调用 `remove_module(tstate, abs_name)` 从 `sys.modules` 移除，clr 侧**不主动销毁/清空 `managedPeer` 或 `foreignDictProxy`**；
   - 因此：若失败前有外部/循环 sibling 捕获了 `partial = sys.modules[name]`，该 `partial` 及其已写入的 live `md_dict` 属性继续有效存活；一旦 `sys.modules[name]` 不存在（失败回滚或用户执行 `del sys.modules[name]`）并再次 `import`，C4-1 重新进入 `CreateModuleCallback`，生成**全新 `modPtr2`、`dictPtr2` 与 `managedPeer2`**（`new_mod is not old_mod`、`new_mod.__dict__ is not old_mod.__dict__`），旧持有人仍保留旧对象隔离。

### 5.3 跨堆 Refcycle、所有权边界、GIL、`Roots` 与关闭调用顺序

- **两类生命周期的明确区分（严禁将退出释放混同为运行期无泄漏）**：
  1. **跨堆双强环实证**：`PyModuleObject*(modPtr)` → `md_dict*` →（经 `StoreName` + `ToPython(PyFunction)` 生成 `rp_managed(id)`）→ `CPythonHost.Roots[id]` (`PyFunction`) → `PyFunctionOwner.Context.State` (`PyModuleInstanceState`) → `ForeignGlobals` (`PythonObject(md_dict*)`) → `CPythonHost.handles`（强 `Py_INCREF(md_dict*)`）。同理动态创建的引用模块全局命名空间的函数闭包，只要曾流入 `md_dict*` 或 `rp_managed`，同样参与该环。由于 CPython GC 不可追踪 CLR `PyFunction` 字段、CLR GC 不可回收被 `Roots[id]` 强持的 `PyFunction`，单侧 GC 均无法断开该环。
  2. **冻结模块宿主生命周期 `Roots`（C4-2 首切覆盖） vs 运行中跨堆环回收（C5 长期门待解）**：
     - **冻结模块 Host 生命周期 `Roots`**：正常应用内静态冻结模块随 `sys.modules` 伴随 Host 全程存活，进程退出经 `Dispose()` 执行 `Roots.Clear()` / `handles.Clear()` 统一归零。这**仅证明退出阶段资源释放闭环，绝不代表运行期反复卸载重导或动态闭包丢弃后无内存泄漏**。
     - **运行中跨堆环回收**：当程序在运行期内反复执行 `del sys.modules[name]` + 重导、初始化失败回滚丢弃或丢弃捕获模块全局变量的动态闭包时，若不引入跨堆环打破机制，已卸载的旧 `(modPtr, md_dict*, PyModuleInstanceState, PyFunction)` 会被 `Roots[id] <-> handles` 持续互相钉住。**本首切不扩展为通用 CLR/CPython 双 GC 协调器，而是把运行期长寿重导零泄漏显式登记为 C4-2 受限边界，交由后续 C5 以真实长期应用生命周期证据补齐。**
- **C4-2 首切可实现的所有权策略与受限范围**：
  - **策略**：(a) `PyModules.Cache`/`Registry` 不再对严格模块留存第二份 CLR 强引用，`host.objects` 保持 `WeakReference<object>`；(b) `exec_module` 失败或 `del sys.modules[name]` 仅从内核 `sys.modules` 移除模块键，不破坏用户已持有的部分模块/函数引用，且重导必分配全新对象身份；(c) 关机 `Dispose()` 作为兜底清零边界。
  - **受限范围**：C4-2 首切仅承诺**冻结注册表模块在单次或有限重导语义上的正确性与 `Dispose()` 退出归零**；不承诺高频 `del sys.modules` 循环或长运行动态闭包生产/丢弃场景下的 `O(1)` 运行期内存恒定（该能力作为 C5 准入门，需实测过以下持有量门后方可对外宣称运行期无泄漏）。
- **C5 长生命周期跨堆回收量化门（后续必验指标，不在 C4-2 虚报通过）**：
  1. **真实重复导入持有量门**：循环执行 `N` 次 `import m -> del sys.modules['m'] -> CP gc.collect() + CLR GC.Collect()/WaitForPendingFinalizers()`，断言 `CPythonHost.GetStatistics()` 的 `ManagedRoots`、`Handles`、`PendingReleases` 及 CPython 侧存活 `PyModuleObject`/`dict` 数量相对基线为 `O(1)` 有界，不随 `N` 线性增长；
  2. **旧引用释放门**：用户持有 `old = m; old_fn = m.fn` 时旧身份保持可用；执行 `del sys.modules['m']; old = None; old_fn = None` 并触发 CLR+CPython 双端 GC 后，确认旧模块与 `md_dict*` 真实释放、`Roots`/`Handles` 回落到导入前基线；
  3. **动态闭包丢弃门**：反复生成并挂载/传递引用 `md_dict` 的函数闭包后置空丢弃，双端 GC 后持有量计数归回基线。
- **完整生命周期字段与调用顺序**：
  1. `StrictHost.Start`：`ProcessNativeBinding.Acquire(StrictKernel, ...)` 校验 capability + `3.12.13` → `rp_kernel_set_phase(0)` + `rp_kernel_reset_eval_stats()` + `rp_kernel_set_strict_reject(1)` → `Py_InitializeFromConfig(_install_importlib=0)` → 预填充 `_strictBuiltinsDict` 并缓存 `BuiltinDictTypePtr` → `rp_kernel_import_set_cil_callbacks(interp, &callbacks)`（分配独立 `GCHandle` 存 `user_data`）→ 逐条 `rp_kernel_import_register` → `rp_kernel_import_freeze(interp)` → `rp_kernel_set_phase(1)`。
  2. `Runtime` 入口与重入：每次托管进 CPython 持 `Lease`（`PyGILState_Ensure`/`Release` 可重入）；`CreateModule`/`ExecModule` 由 CPython 持 GIL 回调 C#，`rp_side_table_mutex` 已预先释放，禁止释放 GIL 后操纵裸 `PyObject*`。
  3. `Bootstrap.Finish -> CPythonHost.Dispose -> rp_stop`：
     - `lock (gate)` 置 `state = 2/4`（`stopping`），**保持 `Roots`、`handles`、`objects`、`reverse`、`ForeignGlobals` 全部存活**；
     - `rp_stop()` 进 `Py_FinalizeEx()` **前**切换 `rp_kernel_set_phase(2)`（`SHUTDOWN`）；
     - `Py_FinalizeEx()` 依次执行：`_PyAtExit_Call`（CIL `atexit` 回调与 shutdown 合法静态导入均可用）→ `finalize_modules` → `_PyImport_FiniCore`（立 `finalizing` 墓碑，触发 `Cleanup(user_data)` 仅释放该次 Host 回调 `GCHandle`，禁止在 `Cleanup` 内重入 `register`/`set_cil_callbacks`/`freeze` 或留活跃异常，排空 `active_waiters`）；
     - `rp_stop()` 在 `Py_FinalizeEx()` 返回后读三阶段 `rp_kernel_eval_*` 计数，并复位 C shim 静态类型槽/eval 统计（保障同 profile 顺序 Host 重建）；
     - 返回 `Dispose()` 后再写出 `RYUPY_CPYTHON_REPORT` 审计 JSON，随后执行 `Roots.Clear()`、`imported.Clear()`、`objects.Clear()`、`transfers.Clear()`、`transferIndex.Clear()`、清空 `_strictBuiltinsDict` 引用、`lock (gate) { state = 3; handles.Clear(); }`（严禁 `state=3` 或 `Py_FinalizeEx` 之后再调 `rp_decref`！）、置 `Instance = null`、`ProcessNativeBinding.MarkHostClosed()`。
  4. **严格 `complete=true` 判决条件**：仅当 `strict-kernel` 生效、`strict_reject` 从 `Py_InitializeFromConfig` 前至 `Py_FinalizeEx` 结束未关闭、`dropped_frames == 0`、`coverage_failures == 0`、三阶段 `(entries, executed, rejected)` 全为 `(0,0,0)` 且 `state == "closed"` 时输出 `complete=true`、`startup_complete=true`，通过 `tests/cil_execution/check_audit.py`。

## 6. 职责落点、验收计划与复杂项目边界

- **职责划分（遵守 C1/C2/C3 并行边界与 `architecture.md §2`）**：`ryupy-cli`/`ryupy-driver` 负责显式配置、strict 工件/许可/`relocate`/ABI 标签校验及冻结清单；`ryupy-codegen-cil`（不碰 HIR/`ty`）负责严格 `InstanceGlobals` 强制、诊断拦截与元数据发射；`RyuPy.Runtime` 负责冷态 `PyModuleInstanceState`/`PyModuleExecutionContext`/`IPyModuleNamespace` 及全局导入收口（保持 `PyDict` 不变与 `PyProviderAbiContractTests` 兼容）；`RyuPy.CPython` & `native/` 负责 `ProcessNativeBinding`、strict shim 分离、`ForeignType` exact dict 归一、两阶段回调及零盲区审计输出。**不改动 C1 对象槽、C2 inspect/类构造/伴生、C3 async-for**。
- **首切窄验收与独立负例门（CPython 3.12.13）**：
  1. **正面闭环**：`math` + 多层 CIL 包（[tests/cases/c4_2_strict_kernel_import/](../../tests/cases/c4_2_strict_kernel_import/case.txt) 覆盖实时 `__dict__`/视图/迭代/`in`/删键/`type() is dict`/`__builtins__` 捕获/循环导入/失败部分模块存活/删 `sys.modules` 重导新身份/`atexit` 延迟导入）+ 独立原生 `.so` 扩展，通过 JIT / Standalone NativeAOT / OneFile 三形态与删除输入/中文空格路径搬迁门、`complete=true` 严格审计门及 `make runtime && make check`（macOS arm64 首切落地，Linux x64 / Windows x64 待各自严苛内核构建后补验）。
  2. **生命周期专项验证**：(a) 同进程先 `auto` 后 `strict-kernel` / 反向、切换路径及假冒内核版本早期拒绝负例；(b) 同 profile + 同路径顺序销毁重启 `CPythonHost` 的 resolver/缓存/`Roots`/对象映射纯净度独立测试。
- **复杂项目（FastAPI）与多版本保留**：严守 [tests/general_projects/README.md](../../tests/general_projects/README.md) 的 `Python >= 3.13.3` 约束及 `fastapi-venv`（`cp313` `pydantic_core` 与 5 个 SQLAlchemy `cyextension` 依赖 `PyFunction_Type`、`PyImport_Import`、`PyType_FromSpec`、`PyCode_NewEmpty`、`PyFrame_New`、`PyImport_ImportModuleLevelObject`、`PyType_FromMetaclass`、`PyUnstable_Code_NewWithPosOnlyArgs`、`PyThreadState_GetUnchecked`/`GetFrame`），绝不降级至 3.12.13 伪造兼容；未来单列 3.13.3 严格内核切片须把 Cython 元对象构造与 `_PyEval_EvalFrameDefault` 字节码执行分离审计。
