# ADR-0221：C4-2 严格无字节码内核接入宿主与产品导入事务设计

- 日期：2026-11-16
- 状态：设计冻结（待实施指令；本轮不修改既有源码、构建与目录索引）
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、[ADR-0216](0216-strict-kernel-native-stdio.md)、[ADR-0217](0217-strict-kernel-frozen-extensions-and-cil-import-abi.md)
- 范围：将 C4-1 严格 CPython 3.12.13 无字节码内核接入 `RyuPy.CPython`、`RyuPy.Runtime`、`ryupy-codegen-cil` 与 `ryupy-driver` / CLI，打通 JIT、Standalone NativeAOT 与 OneFile 产品闭环。

## 1. 背景与不可绕过的设计约束

C4-1 已在官方 CPython 3.12.13 外部源码构建上验收纯 C UTF-8/`_io`、默认求值器三阶段计数器与严格拒绝门（`Include/ryupy_kernel.h`），以及不改 `PyInterpreterState` 既有布局的扩展与 CIL 导入事务 C ABI（`Include/ryupy_import_abi.h`），但尚未接入产品运行时与工具链。

结合现有代码基线核查，C4-2 必须直接解决四项结构性缺口，禁止沿用兼容模式绕行：

1. **导入事务时序冲突**：现行 `CPythonImports.InstallImports` 通过 `rp_install_imports` 注入 Python 侧 `_RyuPyFinder`/`_RyuPyLoader`（与 `LegacyMethodWrapper`），并在 `create_module` 回调中经 `CPythonCallbacks.Dispatch(op=21)` 直接调用 `PyModules.Import` 完成 `RunInit()`，`exec_module` 实为空操作。而 C4-1 的 `rp_load_cil_entry` 严格区分两阶段：`create_module` 仅构造空模块并严禁写入 `sys.modules`，内核发布 `sys.modules[name]` 且置 `spec._initializing = 1` 后，方才在每模块可重入锁下精确调用一次 `exec_module`。若在 `create_module` 误调 `PyModules.Import`，将导致未发布即执行、破坏循环导入并引发重复初始化。
2. **`ExportModule` 缺失与容器快照失效**：现行 `CPythonValues.ToPython` 对未登记 `reverse` 的 CLR `PyModule` 只会生成 `ryupy.ManagedObject` 泛化代理（而非 CPython `PyModuleObject`），无法满足 C 扩展 `PyModule_Check`/`PyModule_GetDict`；传统静态字段槽（`global_fields`）与 `DynamicAttributes` 割裂；`CPythonContainers`（`ContainerTransfer`）仅在进出边界对标量/标量元组键做快照 diff，无法维持 C 扩展 `PyModule_GetDict` 与 CIL `globals()`/`f_globals` 的实时共享与对象身份。
3. **审计启动盲区**：现行 `execution_audit.h` 依赖 `PySys_AddAuditHook` 延迟挂载 PEP 523 求值钩子，`CPythonExecutionAudit.cs` 固定写入 `complete=false`、`startup_complete=false`，被 `tests/cil_execution/check_audit.py` 严格门拒绝。
4. **多版本与原复杂项目边界**：`tests/general_projects/README.md` 固定 FastAPI 规范项目与 venv 为 `Python >= 3.13.3`（`.so` 为 `cp313`），绝不可降级伪装在 3.12.13 上声称完成。

## 2. 决定一：显式双通道分流（默认兼容 vs 严格无字节码内核）

C4-2 不得破坏现有 `auto` provider（Python 3.9–3.15 PBS 分发与兼容基线）。两套路径以显式配置严格隔离，禁止自动静默互降。

| 维度 | 默认 / 兼容模式（`auto` 及 3.9–3.15） | 严格无字节码模式（`strict-kernel`，首切 3.12.13） |
| --- | --- | --- |
| 启动配置 | `HostConfiguration` `schema=1`，`_install_importlib=1` | 显式 `strict-kernel` 模式，`_install_importlib=0`，`isolated=1` |
| 导入路由 | `rp_install_imports`（Python `_RyuPyFinder`/`_RyuPyLoader`） | 仅 C4-1 纯 C 冻结侧表 + `[UnmanagedCallersOnly]` 静态 CIL 回调 |
| 方法包装 | 按 `RYUPY_CPYTHON_NATIVE_METHODS` 开关，默认含 `LegacyMethodWrapper` | 强制等效 `native-methods=true` 静态通道，彻底移除 `LegacyMethodWrapper` |
| 动态求值入口 | 保留兼容委托，C shim 存在 `rp_op(29)`（`PyRun_String`）历史实现 | **移除 `rp_op(29)` 可达路径**；编译期对未闭环动态语法报 `Diagnostic` |
| 审计证明 | `PySys_AddAuditHook`，固定 `complete=false`/`startup_complete=false` | `Py_InitializeFromConfig` 前开启 `_PyEval_EvalFrameDefault` 计数与拒绝，全闭环可产 `complete=true` |
| 产物载荷 | 允许兼容配置与 PBS 运行时目录 | **禁止包含 `.py`/`.pyc`**（可选无源码资源目录单独由 `package_dir` 表达） |

## 3. 决定二：严格内核分发准备、许可证、依赖闭包与构建期 ABI 校验

### 3.1 Driver 发行准备与零系统泄露

- 严格内核来源仅限显式指定路径或构建配方：已验证的严格内核构建前缀、外部官方 `Python-3.12.13` 源码包现场构建产物（使用仓库外临时目录，构建后清理）、或受控分发工件；严禁静默回落扫描系统 `python3`、全局 `venv` 或外部 `PYTHONHOME`/`PYTHONPATH`。
- 交付载荷（覆盖 JIT 输出目录、Standalone NativeAOT 与 OneFile 解包根目录）：
  1. 严格 `libpython` 动态库（如 `libpython3.12.dylib` / `libpython3.12.so` / `python312.dll`）与严格 C shim 动态库（建议独立目标库或显式模式分离，不混用旧 Python helper 注入路径）；
  2. 许可证文件：**必须携带 `LICENSE.Python.txt`** 及随包 C 扩展的第三方许可文本；
  3. 显式登记的原生扩展 `.so`/`.pyd` 及其非系统依赖动态库（按平台系统白名单排除系统基础库）；
  4. **载荷洁净门禁**：输出目录及 OneFile 嵌入载荷内禁止出现任何 `.py` / `.pyc`（严格排除标准库 `Lib/` 与 `__pycache__`）。

### 3.2 原生扩展重定位与构建期 ABI 门禁

- **依赖闭包与搬迁自洽**：沿用并扩展 `cpython_native::relocate` 对全部打包 `.so`/`.pyd` 及其传递动态库执行平台重定位（macOS `@loader_path`/`@rpath` 与签名、Linux `DT_NEEDED` 与 `$ORIGIN` rpath、Windows PE 导入表白名单校验），确保删除源输入目录及构建缓存、整体搬迁至**含中文与空格的路径**且清空 `PATH` / 注入失效 `PYTHONHOME` 后仍正常加载。
- **构建期 ABI 标签与目标版本检查**：在 `ryupy-driver` 收集并冻结原生扩展时，强制校验：
  1. 文件扩展后缀与二进制 ABI 标签必须匹配目标严格内核版本（3.12.13 首切仅接受 `.cpython-312-*`、`.abi3.*` 或合法平台无标签受控扩展，**发现 `cp313` 等跨大版本扩展立即发射编译期 `Diagnostic` 拒绝**）；
  2. 原生映像架构与 `PyInit_<leaf>` 导出符号存在性核验；
  3. 对依赖 CPython 未支持导出符号（或明确要求 `3.13+` 的包，参见 §7）在构建期截断报错，不延后至运行期失效。
- **不做普通文件哈希校验**：恪守仓库约定，不对 `.py`/`.so` 计算文件内容摘要或校验锁；`rp_kernel_import_register` 属于受信宿主对已部署路径的显式注册 ABI。

## 4. 决定三：冻结注册表、宿主生命周期与零盲区 `complete=true` 审计

### 4.1 构建期冻结导入元数据

- `ryupy-codegen-cil`（保持不依赖 HIR/`ty`）与 `ryupy-driver` 在严格模式下联合产出一份静态只读的冻结模块清单：
  - 每个 **CIL 模块/包**记录：`name`、`is_package`、`package_dir`（相对部署根目录的资源目录，非包为 `null`，不伪造 `__file__`）；
  - 每个 **原生扩展**记录：`name`、`is_package`、`origin`（相对部署根目录的 `.so`/`.pyd` 路径）、`package_dir`。
- 编译期强约束：**严格模式下所有 CIL 模块统一编译为 `ModuleExecutionKind::InstanceGlobals`**。若模块仍触发 legacy `global_fields` 静态槽模式，或源码包含未闭环的动态解释回退特性，Codegen/Compiler 必须直接发射 `Diagnostic` 失败。

### 4.2 启动 → 注册冻结 → 运行 → 关闭 精确时序

```text
[1. Preinit：Bootstrap.Start -> StrictHost.Start]
  ├─ 基于实际可执行/AppContext 基址解析全部相对 origin/package_dir（支持中文/空格）
  ├─ 装载严格 libpython 与严格 shim；校验 rp_kernel_version() 及 3.12.13 精确版本
  ├─ ★ 在 Py_InitializeFromConfig 之前：
  │    rp_kernel_set_phase(RYUPY_KERNEL_PHASE_STARTUP)   // 0
  │    rp_kernel_reset_eval_stats()
  │    rp_kernel_set_strict_reject(1)
  └─ 配置 PyConfig { _install_importlib=0, isolated=1, use_environment=0 } 并执行 Py_InitializeFromConfig
       └─ 启动期间纯 C UTF-8 + _io 初始化；任何意外进入 _PyEval_EvalFrameDefault 均被立时拦截计数

[2. Postinit 冻结：主解释器 GIL 已持有]
  ├─ 组装并安装静态 RpKernelCilImportCallbacks { abi_version=1, sizeof, create, exec, cleanup, user_data }
  │    -> rp_kernel_import_set_cil_callbacks(interp, &callbacks)
  ├─ 逐条调用 rp_kernel_import_register(interp, &entry) 注册全部 CIL 模块与扩展 .so/.pyd
  └─ 调用 rp_kernel_import_freeze(interp) 锁死注册表与回调槽

[3. Runtime 执行]
  ├─ rp_kernel_set_phase(RYUPY_KERNEL_PHASE_RUNTIME)     // 1
  ├─ 同步 Runtime 初始化状态（不早退执行模块体，不写入脱节假缓存）
  └─ 经由严格内核导入事务触发主模块及依赖执行（内建 math / 扩展 .so / CIL 包统一走 C4-1 锁与 sys.modules）

[4. Shutdown 与 atexit：Bootstrap.Finish -> Dispose -> rp_stop]
  ├─ ★ 保持 C# Roots GCHandle 活跃（atexit 仍需安全回调 CIL 并可合法导入冻结模块）
  ├─ 在 Py_FinalizeEx() 之前切换：rp_kernel_set_phase(RYUPY_KERNEL_PHASE_SHUTDOWN) // 2
  ├─ 调用 Py_FinalizeEx()：
  │    ├─ 1) _PyAtExit_Call 执行 CIL/C 退出回调及 shutdown 合法延迟导入
  │    ├─ 2) finalize_modules 清理模块对象
  │    └─ 3) _PyImport_FiniCore 置墓碑、调用 RpKernelCilImportCallbacks.cleanup(user_data) 并排空 waiters
  ├─ 读取三阶段计数器 rp_kernel_eval_{entry,executed,rejected}_count(0|1|2)
  └─ 释放 C# Roots，写出审计 JSON（RYUPY_CPYTHON_REPORT），宿主状态置 closed
```

### 4.3 审计完整覆盖依据（`complete=true` 与 `startup_complete=true`）

- 与旧 `PySys_AddAuditHook` 的根本差异在于：**严格内核的计数器与严格拒绝门直接内嵌于 `_PyEval_EvalFrameDefault` 本体，并在 `Py_InitializeFromConfig` 之前启用 `strict_reject=1` 与 `STARTUP` 阶段，直到 `Py_FinalizeEx` 结束后才读数。**
- 因而在 `Py_InitializeFromConfig` 启动前段到进程退出的整条链路上没有任何未观测盲区（即 `startup_complete` 在语义上真实成立）。
- 仅当严格内核标记有效、全生命周期 `strict_reject` 未被关闭、`dropped_frames == 0`、`coverage_failures == 0` 且三阶段 `(entries, executed, rejected)` 均为 `(0, 0, 0)`、`state == "closed"` 时，strict shim 才输出：
  `enabled=true`, `complete=true`, `startup_complete=true`, `hook_installed=true`, `finalized=true`, `startup_frames=0`, `runtime_frames=0`, `shutdown_frames=0`, `frames=[]` —— 直接通过 `tests/cil_execution/check_audit.py` 全项门禁。兼容模式（无内核直连计数器）维持 `complete=false` 不变。

## 5. 决定四：两阶段 CIL 事务、live `__dict__` 共享与失败回滚

### 5.1 为何严禁直接复用 `PyModules.Import` 与快照传输

1. **禁止 `create_module` 调 `PyModules.Import`**：`PyModules.Import` 会直接运行 `RunInit()`。若在 `create_module` 调用，会跳过 `sys.modules[name]` 预先发布与 `spec._initializing = 1` 状态窗口，破坏循环导入自引用并违反 C4-1 单一事务不变量。
2. **禁止 `ExportModule` 假代理或 `ContainerTransfer` 差量快照**：C 扩展通过 `PyModule_GetDict(mod)` 直接持有并原地增删改模块真实 `md_dict`；CIL 代码与 `f_globals` / `mod.__dict__` 亦高频读写。任何边界快照 diff 都无法在跨调用中间保持动态增删键、非标量键及精确对象身份一致，且现行通用 `ManagedObject` 代理会令 C 扩展 `PyModule_Check` 失败。

### 5.2 最小中性 `RyuPy.Runtime` 抽象合同

为维持分层干净（`RyuPy.Runtime` 绝不依赖 CPython C 指针或头文件，且不破坏 `PyProviderAbiContractTests.cs` 锁定的 `IPyForeignObject`/`IPyModuleProvider`/`PyProvider` 既有公开签名契约），在 `RyuPy.Runtime` 新增两组最小中性能力（优先 `internal`/显式能力接口，仅当测试或跨程序集必需暴露时同步登记至 `PyProviderAbiContractTests` 允许表）：

1. **`IPyDictStorage`（live 字典存储后端）**：
   - 抽象能力：`Count`、`ContainsKey(object? key)`、`TryGetValue(object? key, out object? value)`、`SetItem(object? key, object? value)`、`DeleteItem(object? key)`、`Clear()`、`CopyEntries()`（供遍历/`items()`/`keys()`）。
   - 用法：`PyDict` 可选挂载 `IPyDictStorage`。严格模式下，`RyuPy.CPython` 将 CPython `PyModule_GetDict(mod)` 返回的真实 `md_dict` 包装为 `IPyDictStorage` 实现（按键值双向 Marshal 实时直读直写 CPython `PyDict_*` C API），注入新建 `PyModule` 实例的 `state.Globals`。
   - 效果：`module.__dict__`、函数 `globals()` / `f_globals`、模块属性读写、`vars(mod)` 与 C 扩展 `PyModule_GetDict` 操作同一物理字典，零快照拷贝。
2. **`IPyModuleInstanceTransaction`（两阶段模块实例生命周期）**：
   - `CreatePendingModule(string name, PyDict liveGlobals, PyModuleMetadata meta)`：仅在 `PyModuleInstances` 模型中分配待初始化 CLR `PyModule` 并绑定 `liveGlobals`（及 `__name__`、`__package__`、`__path__`、`__file__`），**不写入唯一权威缓存前不执行初始化，不运行 `RunInit()`**；同步建立与 CPython `PyModuleObject` 的 1:1 `objects`/`reverse` 双向身份映射。
   - `ExecutePendingModule(string name)`：仅由 C4-1 的 `exec_module` 回调触发一次，在 `sys.modules[name]` 已由内核发布且 `_initializing = 1` 的前提下运行对应模块的编译初始化体 `RunInit()`。
   - `RollbackPendingModule(string name)`：当 `ExecutePendingModule` 抛出托管异常时，先将真实 `PyException`（含类型、消息与异常链）转换注入 `PyErr_*`，清除未完成的 CLR `PyModule` 实例与 `reverse` 映射，随后向 C4-1 返回 `-1` 由内核同步撤销 `sys.modules[name]`，保证失败重试从零洁净重启。

### 5.3 `sys.modules` 唯一权威与无回 fallback

- **唯一权威**：严格模式下，CPython 内核的 `PyImport_GetModuleDict()` 与 C4-1 每模块锁是 `sys.modules` 的唯一真实来源。`PyModuleView` 与 `PyModules.TryCached` 在严格模式下透传查询/受控对齐内核状态，绝不另起互不同步的第二张模块表。
- **循环导入**：`A.exec_module` 中调用 `import B`，`B` 反导 `A` 时，C4-1 锁与 `sys.modules` 直接返回处于初始化中的同一 `A` `PyModuleObject`（及同一个 CLR `PyModule` 身份），`A` 后续向 `liveGlobals` 写入的新属性对 `B` 实时可见。
- **无源码回退**：严格模式下禁用 `PyClosedSourceFallback` 的字节码/源码兜底通道，缺模块由纯 C 路由器确定性抛出 `ModuleNotFoundError`。
- **与既有内置模块并存规则**：C 内建模块（如 C4-1 静态编入内核的 `math`）优先归属 CPython 真实模块对象（避免用孤立 CLR 静态 dict 影子覆盖导致与 C 扩展观察不一致）；纯 CLR 原生标准库模块仅按已显式编译入图的冻结表策略配合存在，不注入源码胶水。

## 6. 决定五：模块职责落点（实施边界）

| 仓库组件 | C4-2 计划职责（待实施指令，本 ADR 不改动既有文件） | 架构红线（`architecture.md §2` & 分切边界） |
| --- | --- | --- |
| `ryupy-cli` / `ryupy-driver` | 新增显式 `strict-kernel` 模式；发行准备与 `LICENSE.Python.txt`；`.so` ABI tag 检查与平台 `relocate`；无 `.py/.pyc` 载荷生成与路径解析 | 保持默认 `auto` 兼容行为独立；无普通文件摘要校验 |
| `ryupy-codegen-cil` | 严格模式下强制 `InstanceGlobals`；静态冻结模块清单输出；未闭环语法编译期 `Diagnostic` | 不得依赖 HIR/`ty`；单文件不超 600 行 |
| `RyuPy.Runtime` | `IPyDictStorage` 与 `IPyModuleInstanceTransaction` 两阶段生命周期抽象；严格无源码回落禁用开关 | 不依赖 CPython 头文件/指针；恪守 `PyProviderAbiContractTests` 契约 |
| `RyuPy.CPython` & `native/` | 拆分/隔离严格 shim（移除 `rp_install_imports`/`LegacyMethodWrapper`/`rp_op(29)`）；对接 `ryupy_kernel.h`/`ryupy_import_abi.h`；实现 `create`/`exec`/`cleanup` 与 live `md_dict` 包装；输出精确三阶段审计报告 | 保留既有兼容路径独立；遵守 C1/C2/C3 已锁定语义不回退 |

## 7. 验收计划、版本矩阵与复杂项目（FastAPI）路线

### 7.1 C4-2 首切窄产品验收门（CPython 3.12.13 严格内核）

1. **端到端场景矩阵**：
   - `math` 真实内建模块运算与身份；
   - 多层 CIL 包（`fromlist`、相对导入、live `__dict__`/`globals()`/`sys.modules` 实时同步、循环导入、初始化失败回滚重试）；
   - 显式注册与搬迁后的独立原生 `.so` C 扩展（含依赖闭包重定位与与 CIL 相互导入）；
   - `atexit` 退出回调及 shutdown 阶段合法延迟导入。
2. **三交付形态与严酷搬迁门**：JIT、`Standalone NativeAOT`、`OneFile` 三种产物均须通过 **“构建 → 物理删除源目录与私有发行缓存 → 搬迁至含中文与空格路径 → 清空 `PATH` 与污染 `PYTHONHOME`/`PYTHONPATH`”** 实跑验收。
3. **统一判决标准**：
   - stdout/stderr/exit_code 与固定 CPython 3.12.13 同次 oracle 逐字节一致；
   - `tests/cil_execution/check_audit.py` 验证 `state=="closed"`、`complete==true`、`startup_complete==true`、`hook_installed==true`、`finalized==true`、`dropped_frames==0`、`coverage_failures==0` 且三阶段 `(startup, runtime, shutdown)` 帧数全为 `0`；
   - 产物目录与 OneFile 解包空间零 `.py`/`.pyc` 残留；`make runtime && make check` 通过。
4. **平台边界**：macOS arm64 为 C4-2 首切实现与实测平台；Linux x64 与 Windows x64 待各自平台 C4-1 严格内核构建完成后独立补验。

### 7.2 FastAPI 规范项目与 3.13.3 / 3.9–3.15 长期路线

- `tests/general_projects/README.md` 明确 FastAPI 规范项目要求 **`Python >= 3.13.3`**（规范 oracle 与 `fastapi-venv` 固定 `3.13.3`）。
- 对规范 `fastapi-venv` 实测原生 `.so` 符号依赖核查确认：
  - `pydantic_core`（`cp313` 标签）依赖：`PyFunction_Type`、`PyImport_Import`、`PyType_FromSpec`；
  - 5 个 SQLAlchemy `cyextension` 模块（`cp313` 标签：`collections`、`immutabledict`、`processors`、`resultproxy`、`util`）依赖：`PyCode_NewEmpty`、`PyFrame_New`、`PyImport_ImportModuleLevelObject`、`PyType_FromMetaclass`、`PyUnstable_Code_NewWithPosOnlyArgs`、`PyThreadState_GetUnchecked`（其中 `collections` 与 `resultproxy` 另依赖 `PyThreadState_GetFrame`）。
- **结论与不变量**：
  1. **严禁**为跑在 3.12.13 严格内核上而降级 FastAPI 规范 Python 版本，也禁止跳过 ABI 校验混装 `cp313` `.so`（构建期 ABI 门禁必须以 `Diagnostic` 明确阻断）。
  2. 原复杂 FastAPI 项目的全 CIL 执行必须等待后续单列的 **CPython 3.13.3 严格内核迁移与对象模型扩展切片**，同步打通 C 扩展所需的上述 C API 与元对象支持。
  3. **元对象创建与字节码执行分离**：后续 3.13.3 严格内核与执行审计必须把 Cython 用于 traceback/签名展示的 `PyCode_NewEmpty` / `PyFrame_New` / `PyUnstable_Code_NewWithPosOnlyArgs` **元对象构造**与 `_PyEval_EvalFrameDefault` **字节码帧求值**严格分离计数，既支持 Cython C 扩展合法生成元信息，又保证任何字节码解释帧执行被严格拒绝且计入审计。
  4. **3.9–3.15 兼容矩阵**：在各目标版本严格内核就位前，Python 3.9–3.15 `auto` 兼容路径按既有独立契约继续维护与回归，不在 3.12.13 上强行收口跨版本旧生态。

## 8. 配套待办输入（Pending Fixtures）

- 同步新增目录手写用例 `tests/cases/c4_2_strict_kernel_import/`（`case.txt` 标 `pending = true`，入口带 `# ryupy: pending`），覆盖 `math` + 编译期 CIL 包（相对导入/`fromlist`/live `__dict__`/循环导入/失败回滚/`atexit` 延迟导入），随 C4-2 实现落地后去标记并与 C 扩展产品门禁共同转正。
