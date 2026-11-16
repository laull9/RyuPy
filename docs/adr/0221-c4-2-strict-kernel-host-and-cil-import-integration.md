# ADR-0221：C4-2 严格无字节码内核接入宿主、生命周期 Fence 与可执行分切计划

- 日期：2026-11-16
- 状态：按 ADR-0223 转为可选严格研究，暂停扩展；C4-1 Loader 关键字修复已合入 `676d02c8`
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、[ADR-0216](0216-strict-kernel-native-stdio.md)、[ADR-0217](0217-strict-kernel-frozen-extensions-and-cil-import-abi.md)、[ADR-0220](0220-native-compiled-import-hooks.md)
- 范围：保持普通 `PyDict` 数据布局与热路径零改动、保留 `auto`（3.9–3.15）兼容链路与 [tests/general_projects/README.md](../../tests/general_projects/README.md) `FastAPI >= 3.13.3` 原约束；在 CPython 3.12.13 严格内核上以可执行小切片打通 `RyuPy.CPython`、`RyuPy.Runtime`、`ryupy-codegen-cil` 与 `ryupy-driver`/CLI。

## 1. 源码核查结论与现状纠偏

用户已接受少量必要 Python 胶水，主线改用原版 libpython，见
[ADR-0223](0223-stock-libpython-bounded-glue.md)。下述严格内核专属 Fence、导入 ABI 和
三阶段零字节码合同留在研究 profile。Slice 2 中性冷 namespace 与模块事务可独立复用到
stock PEP451 loader；复用时另验身份、循环、失败回滚和生命周期，不能依赖 strict-only 符号。

对照当前实施分支（含 ADR-0220 与 C4-1 `676d02c8`），接入需解决以下时序与身份合同：

1. **兼容基线纠偏（ADR-0220 已落地）**：兼容通道（3.9–3.15）已由 `rp_install_native_imports` 提供原生 C finder/loader 并移除三个 Python 别名，`rp_op(29)` 已固定返回 `NotImplementedError`（`ryupy_cpython.c:363`）；兼容默认仅余 `LegacyMethodWrapper` 使用 `rp_install_imports` 源码（显式 `native-methods` 传 `""`）。strict 模式在此基础上彻底不调用 `rp_install_imports` 与 `rp_install_native_imports`，完全改由 C4-1 侧表驱动。
2. **入口时机与 `__main__` 跳过缺陷**：`emit_entry_wrapper`（`entry.rs:88-146`）现行在 `__init_functions` 之前调用 `Bootstrap.Start`，且 `__run_main`（`PyModules.RunMain`）通过 `Import("__main__")` 触发。在 strict 模式下：(a) 若 `Start` 提前冻结侧表，`__init_functions` 里的编译模块注册来不及进入冻结表；(b) CPython 初始化已预先创建 `sys.modules["__main__"]`，普通 `PyImport_ImportModule("__main__")` 会命中缓存而直接跳过编译入口。
3. **`ModuleFromOwned` 映射与旧 `fn` 回流缺口**：`CPythonImports.ModuleFromOwned`（`CPythonImports.cs:23-26`）要求 `objects[ptr]` 与 `reverse[cached]` 均存在且 `reference.Owner` 未释放；若 strict 仅设其一，回流必漏命中。此外 `del sys.modules[m]` 或回滚后若旧函数 `fn` 仍存活，再经 CPython 传回旧 `modPtr` 时应回到原 `PyModuleInstanceState` 而非新建割裂状态。
4. **程序集可见性与 `sys.modules` 哨兵失真**：`RyuPy.CPython` 与 `RyuPy.Runtime` 属不同程序集，`internal` 命名空间接口不可跨程序集实现；且 `PyModules.TryCached(name, out PyModule?)` 无法区分“缺键”、“`sys.modules[name] = None` 阻塞”与“任意非模块替换值”。
5. **其它硬缺口**：`PyInstanceGlobals`/`PyModuleImport`/`PyInstanceClassHook` 强依赖 canonical 托管函数引用身份；`PythonObject.ForeignType`（含 `__class__` 与 `PyObject.TypeOf`）未将精确 `&PyDict_Type` 归一为 `PyType.Get("dict")`；`ProcessBinding` 缺少不可逆 `Failed` 终态；现行 `Dispose()` 在 `Py_FinalizeEx` 前清除了类/方法缓存且 `handles.Clear()` 仅丢账本未做真实 owned `DECREF`。

## 2. 进程级单次绑定（`ProcessBinding`）与静态 P/Invoke 边界

```text
Unbound ──(locked CAS: 记录 canonical Profile + LibraryPath + ShimPath)──▶ Reserved
  │                                                                          │
  │ (任何 dlopen / NativeLibrary.Load / TryGetExport / capability P/Invoke    ├─▶ Active ──(Dispose)──▶ Closed
  │  / Py_GetVersion / rp_start 失败)                                        │                          │
  └──────────────────────────────────────────────────────────────────────────┴──▶ Failed (进程终态)   └─▶(仅同三元组可重入重建；跨 profile / 改 path / Failed 均立即拒绝)
```

- **不可逆与防混用规则**：在调用任何 `NativeLibrary.Load`/`dlopen` 之前先加进程锁抢占 `Reserved` 并锁定唯一 `(Profile, LibraryPath, ShimPath)` 三元组；`NativeLibrary.SetDllImportResolver` 仅注册一次并只捕获该进程已绑定 `ShimHandle`。任意加载/导出/版本/初始化失败转入 **`Failed` 终态（永不退回 `Unbound`）**，跨 profile（如同进程先 compat 后 strict 或反之）及切路径必须在子进程隔离运行、同进程即刻抛诊断拒绝。
- **能力核验原则**：`NativeLibrary.TryGetExport` 仅作存在性预检，真正的 `rp_kernel_version()`、capability 与版本校验走固定 static `[DllImport]` shim 入口，严禁动态函数指针反射表；compat 3.9–3.15 路径不触碰任何 strict-only 导出。

## 3. 严格启动、注册冻结与 `RunStrictMain` 序列

Compat（`auto` 3.9–3.15）保持既有序列不变；strict 模式按以下 **零用户代码前置注册 → 冻结 → 单次入口** 序列重排（由 `driver`/`HIR`/`MIR` 编期冻结资格与全部不支持语法 `Diagnostic`，`ryupy-codegen-cil` 只负责发射）：

1. **`PySystem.Initialize(args)`**：构建 CLR `sys` 宿主外壳（未执行任何用户 Python 帧）。
2. **`Bootstrap.StartStrictUnsealed(configPath, args)`**：获取 `ProcessBinding`（`StrictKernel`）；在 `Py_InitializeFromConfig` **前**调用 `rp_kernel_set_phase(0)`、`rp_kernel_reset_eval_stats()` 与 `rp_kernel_set_strict_reject(1)`；以 `_install_importlib=0, isolated=1, use_environment=0` 初始化内核；缓存 canonical `&PyDict_Type` 指针与宿主级 `_strictBuiltinsDict`；注册零字节码原生桥（不触发 Python `encodings`/importlib）。
3. **`__init_functions()` + `__init_class_factories()`**：冷注册全部已编译模块元数据、`InstanceCode` 及原生 `PyNativeModules`（含 `os.environ` 等必须接入无字节码注册通道的原生标准库模块；**严格零用户 Python 代码执行**）。
4. **`Bootstrap.SealStrict()`**：汇总结算冻结表（CIL 模块 + 原生扩展 `.so`/`.pyd` + 无字节码原生模块条目）；安装 `RpKernelCilImportCallbacks`（`create_module = NULL`）；逐条调用 `rp_kernel_import_register`；调用 `rp_kernel_import_freeze(interp)` 并切换 `rp_kernel_set_phase(1)`（`RUNTIME`）。
5. **`PyModules.RunStrictMain()`**：直接定位内核中已由 `Py_InitializeFromConfig` 创建的 `sys.modules["__main__"]`（不经普通 `Import("__main__")` 避免缓存命中跳过）；将编译 `__main__` 的 `PyModule` 绑定至该现存 `modPtr` 与其既有 `md_dict*`（保留 `__main__` 的 `builtins` 模块/模块入口语义及循环 `import __main__` 同对象），精确执行入口体一次。
6. **`Bootstrap.Finish()`**：进入 §6 `FiniCore` fence 关机序列与严格零帧审计（`complete=true` / `startup_complete=true`）。

## 4. `create_module = NULL`、公开中性冷接口与 `PyDict` 零改动

### 4.1 选定方案对比与跨程序集契约

- **维持普通 `PyDict`（`sealed`）零改动**：不给 `PyDict` 新增任何后端字段、`ConditionalWeakTable` 查询或分派分支；也不把 CPython `md_dict*` 镜像成 `PyDict`。
- **公共中性冷接口 vs 内部执行上下文**：
  - 在 `RyuPy.Runtime` 新增**`public` 中性冷接口**（遵守 `PyProviderAbiContractTests` 既有 `IPyForeignObject`/`IPyModuleProvider`/`PyProvider` 签名零删改规则）：
    - `public interface IPyModuleNamespace`：`object ViewObject { get; }`、`bool TryGet(string name, out object? value)`、`void Set(string name, object? value)`、`bool DeleteIfPresent(string name)`；
    - `public interface IPyStrictImportProvider`（或增补中性能力接口）：`bool TryGetSysModuleEntry(string name, out object? entry)`（精确区分 `false`=缺键、`true + PyNone.Instance`=`None` 阻塞、`true + object`=任意 `sys.modules` 替换值）、`object ImportLevel(string name, object? globals, object?[] fromlist, long level)`、`PyModule BindExistingMainModule(...)`。
  - 内部 **`PyModuleInstanceState`** 与 **`PyModuleExecutionContext`** 保持 **`internal`**：`State` 持 `PyDict? NativeGlobals`（纯 CLR 快路）与 `IPyModuleNamespace? ForeignGlobals`（strict 指向真实 `md_dict*` 代理）；`Context.Builtins` 放宽为 `object`（优先 `is PyDict` 快路，否则走 `IPyModuleNamespace`）。`globals()`、`mod.__dict__`、`fn.__globals__` 直接返回同一 `PythonObject(md_dict*)` foreign view；迭代、`in`、属性/`[]=`/删键及 `ToPython` 全直通 `md_dict*`。

### 4.2 优选 `create_module = NULL` 与 `Exec` 事务、旧 `fn` 回流

- **取舍选择：`create_module = NULL` 优于 C# 自建 `create_module`**：C4-1（`ryupy_import_router.h:236-255`）在 `create_module == NULL` 时直接调用 `PyModule_NewObject` 并执行 `rp_kernel_init_module_attrs(spec, mod)`、置 `initializing = 1` 且唯一写入 `sys.modules`，免除托管层复制属性初始化与早发风险。
- **`ExecModuleCallback(interp, modPtr, specPtr, userData)` 单次事务顺序**：
  1. 由 `spec.name` 查找不可变冻结 `PyCompiledModuleCode` 与 `md_dict* = PyModule_GetDict(modPtr)`；
  2. 为该 `modPtr` 建立或复用 `PyModuleInstanceState`（Host 维护 `modPtr -> WeakReference<PyModuleInstanceState>` / 存活表）：首次分配 `foreignDictProxy = FromBorrowed(md_dict*)`、构造 `managedPeer`（`ForeignObject = PythonObject(modPtr)`），并**同步写入 `host.objects[modPtr] = WeakReference(managedPeer)`、`host.reverse.Add(managedPeer, ManagedReference(0, modPtr, foreignModuleProxy))`**，保证后续任何 `ModuleFromOwned(modPtr)` 精确命中；
  3. 即便在 `exec_module` 中触发 GC 回收、`del sys.modules[m]` 或回滚后只要旧 `PyFunction`（通过 `PyFunctionOwner -> State`）或外部引用仍存活，若 CPython 侧再通过其它途径传回同一旧 `modPtr`，Host 凭 `modPtr` 表把新建 `PyModule` 视图回流到**同一个 `PyModuleInstanceState`**；而新重导由 C4-1 分配全新 `modPtr2`，天然隔离新旧身份；
  4. 精确执行 `code.Execute(context)` 一次；异常在 GIL 下转 `PyErr_*` 返回 `-1`，由 C4-1 摘除 `sys.modules[name]`，CLR 不破坏外部仍持有的部分模块/函数引用。
- **跨堆运行期回收明确界定为 C5 边界**：`md_dict* -> rp_managed -> Roots[id] -> PyFunction -> PyModuleInstanceState -> ForeignGlobals(PythonObject(md_dict*)) -> handles(Py_INCREF)` 构成真实跨堆双强环。本首切不引入通用双 GC 协调器，仅保证有限重导身份语义正确与关机释放；高频 `del sys.modules` / 丢弃动态闭包后的 `O(1)` 运行期持有量收敛作为 C5 长期生命周期门单列验收。

## 5. `__builtins__` Canonical 身份、`TypeOf`/`__class__` 与无字节码原生模块

- **避免 `__builtins__` 身份分裂**：Host 在 `StartStrictUnsealed` 构造单一宿主级 exact `PyDictObject*`（`_strictBuiltinsDict`），并将 RyuPy canonical 函数身份 **`PyInstanceGlobals.Function`**、**`PyModuleImport.Function`**、**`PyInstanceClassHook.Function`** 及内建 `PyType`（`dict`/`int`/`list`/`str`/`ValueError` 等）建立双向 `Roots`/`reverse` 与指针归一映射写入该字典；从而同时满足：
  - C 扩展读 `PyDict_GetItemString(md_dict, "__builtins__")` 获得合法 `PyDictObject*`；
  - CIL 执行路径的 `ImportGuard`、`ClassGuard`、`CallDirectGlobals`（全部依赖 `ReferenceEquals`）与 CPython 读回的同一函数/类型对象身份完全一致；
  - 模块函数 `def f()` 在定义点捕获当前 `md_dict["__builtins__"]` 字典，随后重绑 `globals()["__builtins__"] = other_dict` 仅影响新定义函数而不改旧 `f.__builtins__`（未重绑期间可凭 `_strictBuiltinsDict` 版本标记走 `PyModuleBuiltins.Default` 零 P/Invoke 快路）。
- **`TypeOf` 与 `__class__` 同源修复**：`PythonObject.ForeignType` 与 `__class__` 读取（`PyObject.cs:53,89` -> `FromOwned(rp_op(13 | 2))`）在命中宿主缓存精确 `&PyDict_Type` 时统一返回 `PyType.Get("dict")`（不改变自定义 `class Sub(dict)` 子类），保证 `type(globals()) is dict`、`type(mod.__dict__) is dict`、`mod.__dict__.__class__ is dict` 全部为 `True`；`__main__` 主入口与导入模块各自的 `__builtins__` 形态差异按 CP3.12.13 保持一致。
- **Runtime 原生模块无字节码通道**：`PyNativeModules.Register()` 中的模块（如 `os`/`os.environ`、`json`、`re` 等）在 strict 模式下通过 `SealStrict` 向内核侧表登记无 `.py` 字节码冻结条目并绑定对应 CLR 原生字典/对象，消除 `import os` 等的字节码与 `ImportError` 缺口。

## 6. `FiniCore` 真实释放 Fence（修复过早清理与假清零）

```text
[1. state=2] Dispose 进入 -> 切 phase=2(SHUTDOWN) -> Py_FinalizeEx 开始
   └─ _PyAtExit_Call 执行：Roots/handles/classBridge/methodCaches/CIL lazy import 全保持可用（atexit 可定义新类与延迟导入）
[2. state=4] _PyAtExit_Call 完毕 / finalize_modules + _PyImport_FiniCore 有效解释器 & GIL Fence
   └─ 关闭外部重入 -> Cleanup 释回调 GCHandle -> 释放 class/method 原生缓存 -> 排空 releases 并真实 Py_DECREF(handles) -> 记数真实 native 析构次数
[3. state=3] Py_FinalizeEx 返回之后（解释器与分配器已销毁）
   └─ 仅执行 CLR 账本 null/Clear（Roots.Clear / objects.Clear / handles.Clear），严禁再调用 rp_decref！-> 读三阶段 eval 计数写 complete=true 报告
```

- **真实析构证据**：关机验收除检查 `HostStatistics` 零残留及 `rp_kernel_eval_* == (0,0,0)` / `complete=true` 外，必须以 C 侧测试扩展/托管载体上的真实 `tp_dealloc` 析构计数递增证明 owned 引用被真实释放，杜绝 `handles.Clear()` 伪造零泄漏假象。

## 7. 验收清单与可执行分切计划

- **保留基线**：[tests/cases/c4_2_strict_kernel_import/entry.py](../../tests/cases/c4_2_strict_kernel_import/entry.py) 已由主代理修正 `.VERSION`、`flaky_failed` 与 `__main__` `__builtins__` 断言并通过 CP3.12.13 基线，不得覆盖；`FastAPI >= 3.13.3`（`cp313` `.so`）与 `auto`（3.9–3.15）独立保留，C4-2 首切严格限定 `3.12.13` macOS arm64（Linux/Windows 待跨平台内核构建后补验）。
- **必验清单（全部转正后方可在产品层移除 `# ryupy: pending`）**：
  1. 入口单次执行（`RunStrictMain` 直跑已有 `__main__` `md_dict`）与循环 `import __main__` 同对象；
  2. `exec_module` 执行期间触发 `GC.Collect` 后 `objects`/`reverse`/`ForeignObject` 与存活对象零损坏；
  3. `del sys.modules` 或回滚后旧 `fn` 存活场景下的 `ModuleFromOwned(modPtr)` 按 `ptr` 回流同一 `PyModuleInstanceState`；
  4. `__builtins__` 旧函数捕获与模块键重绑仅影响新 `def` 函数；
  5. `sys.modules[name] = None` 阻塞及任意非模块对象替换透传；
  6. `atexit` 回调内新类定义与延迟静态导入；
  7. `FiniCore` fence 真实 native `owned` 析构计数证明（不止账本清零）；
  8. 公共 ABI 无删除回归（`PyProviderAbiContractTests`）+ 独立子进程验证跨 profile/失败后 `ProcessBinding` 拒绝及同 profile 顺序重建 + JIT/Standalone NativeAOT/OneFile 删除输入与中文空格搬迁 + `complete=true`、`startup_complete=true`（`check_audit.py`）。

### 可执行分切路线（首个可独立实施切片：Slice 1）

| 切片 | 目标与可验证产出 | 依赖 |
| --- | --- | --- |
| **Slice 1（源码与宿主验证构建就绪）**：`ProcessBinding` + 严格加载/FiniCore Fence + 审计闭环 | 落定 `ProcessNativeBinding`（`Reserved`/`Failed` 不可逆终态、Compat 单次边界与 StrictKernel 同三元组顺序重建）、`ryupy_cpython_strict` 分离、独立 `rp_kernel_set_fini_fence` + 单次静态 `exec_module` trampoline（不与 `cil_callbacks.cleanup` 耦合）、单调 `disable_epoch`/`reset_epoch` 防篡改审计与真实 `FiniCore` 句柄 `DECREF`（允许 `op=0` 拒 `op!=0`/新租约）；12 项独立子进程测试待总控整跑 | 依赖 C4-1 `676d02c8` |
| **Slice 2**：公开中性冷接口与 `PyModuleInstanceState`/身份归一 | 落定 `IPyModuleNamespace`、`IPyStrictImportProvider`（`sys.modules` 三态）、`PyDict` 零改动冷态 `State`/`Context` 重构、`_strictBuiltinsDict` canonical 身份与 `&PyDict_Type` `TypeOf`/`__class__` 归一（含单元测试） | Slice 1 |
| **Slice 3**：`create_module = NULL` 两阶段回调、`RunStrictMain` 与 `SealStrict` | 接入 `Entry` 时序重排（`Initialize -> StartStrictUnsealed -> __init_functions -> SealStrict -> RunStrictMain -> Finish`）、`objects`/`reverse`/`ForeignObject` 同步与旧 `fn` `ptr` 回流、`PyNativeModules`（`os.environ`）接入与 `c4_2_strict_kernel_import` 转正 | Slice 2 |
| **Slice 4**：Driver 严苛分发 + `relocate` + 三形态与 ABI 门禁 | 冻结 `driver`/HIR 编期校验（codegen 只发射）、3.12.13 严格载荷（含 `LICENSE.Python.txt`、零 `.py/.pyc`、拒 `cp313` `.so`）、原生 `.so` 扩展交叉导入及 JIT/NativeAOT/OneFile 中文空格搬迁全门 | Slice 3 |
