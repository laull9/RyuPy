# 仓库目录整理与随包 CPython 路线规划

日期：2026-11-13。性质：**目录与 provider 开发为规划；源码仓历史清理单独执行**。

本文以当前共享 checkout 为观察对象，区分已存在的文件、已有验收记录与拟议布局。
本任务按后续授权负责本文、`.gitignore` 与隔离镜像中的全历史产物清理；
没有开发产品、修改测试 runner 或搬移源码，没有提交共享工作区代码和文档。
共享工作区原有改动保留，历史发布和本地同步状态见 §7、§9。

## 1. 目标与决策边界

用户最新目标确定为：**随包 libpython 宿主 + 通用对象桥 → 现有联合应用切换 CPython
provider → 真实 NumPy 扩展 → 对标 Nuitka 的 AOT 分发体验 → 根据热点逐步以 C# 替换**。

据此作出以下布局决策：

1. 兼容覆盖优先通过统一 provider 和对象桥扩大。停止把逐个补写 C# 标准库 API 当成唯一主线；
   已有 C# 实现保留为对照路径和后续热点后端。
2. 编译器、运行时宿主、对象桥、依赖收集和验收各有独立边界。Rust 先在现有 crate 内
   建立职责目录；遵循 ADR-0181，CPython 互操作采用独立 C# 项目与最小 C shim。
   Runtime 保留中性合同，入口组合注册，避免反向依赖；不额外引入插件框架或合同程序集。
3. 先完成 standalone 闭包与搬迁，再推进 onefile。长期比较维度包含启动、体积、内存、
   构建成本、扩展兼容和干净目标机交付；每项使用固定程序与明确工具版本。
4. AOT 宿主、RyuPy 编译模块、CPython 执行模块和动态扩展分别登记。产品功能兼容率与
   纯 RyuPy 原生执行率分列，桥接结果归前者；保留固定项目 ID、偏差及历史原生口径。
5. 目录整理服务上述竖切，不能先重排全仓再做宿主。每次只迁一个职责域，保持可运行入口。

本文直接遵循 [ADR0181](adr/0181-cpython-provider-first.md)、
[CPython provider 方案](cpython-provider.md) 与
[ADR0182](adr/0182-source-only-test-oracles.md)。ADR0182 覆盖旧 golden、`.snap`、
patch/diff 保留规则。主代理已同步 AGENTS、TODO、架构与路线；本任务不修改这些文件。
目标目录与 CP1–CP5 仍是开发计划，历史清理属于本轮已授权的独立执行范围。

## 2. 观察依据与当前盘点

### 2.1 阅读范围

- `AGENTS.md`、`docs/architecture.md` §2、`docs/workflow.md`，以及 `TODO.md`
  的当前竖切、解释器桥和 Phase 4 条目。
- `docs/roadmap.md`、`docs/compatibility.md` 的路径与验收口径，
  `docs/sqlite3-implementation.md`、`tests/README.md`。
- 根及 crate 的 Cargo 清单、两个 C# 项目清单、`Makefile`、现有 `.gitignore`。
- `crates/ryupy-e2e/src/lib.rs`、`crates/ryupy-e2e/tests/e2e.rs`、
  `tests/run_python_baseline.py` 的发现实现，代表性测试和 CI 的路径引用。
- 对 `crates/`、`runtime/`、`tests/`、`scripts/`、`docs/validation/` 做定向路径盘点；
  Git 跟踪状态使用 `git ls-files`。另为完整备份枚举忽略文件，未散读构建缓存分析产品行为。

历史记忆只用于定位 fixture 约定；以下目录、数量、发现规则与实现状态均重新读取当前 checkout。
没有计算普通文件或产物的 SHA256 / MD5。

### 2.2 Rust 与 C# 布局

下表“直接源码”按当前磁盘相应 `src/` 或项目根目录的直接子文件计数，包含当前未提交新增文件。
“嵌套源码 / 集成测试”按 Git 已跟踪路径计数，二者口径刻意分开。

| 位置 | 直接源码 | 已跟踪嵌套 Rust 源码 | 已跟踪集成测试 Rust 文件 | 当前判断 |
| --- | ---: | ---: | ---: | --- |
| `crates/ryupy-base/src/` | 4 | 0 | 0 | 足够清楚，保持 |
| `crates/ryupy-syntax/src/` | 1 | 0 | 0 | 薄解析封装，保持 |
| `crates/ryupy-cli/src/` | 1 | 0 | 3 | 参数入口，暂不为目录数量拆分 |
| `crates/ryupy-hir/src/` | 94 | 0 | 0 | 降级、作用域、对象与测试平铺 |
| `crates/ryupy-ty/src/` | 28 | 0 | 0 | 检查、模式与测试平铺 |
| `crates/ryupy-mir/src/` | 165 | 38 | 0 | 已有 `passes/`，其余构建、验证、trace 与测试平铺 |
| `crates/ryupy-codegen-cil/src/` | 82 | 0 | 0 | 引用表、发射、部署元数据与测试平铺 |
| `crates/ryupy-driver/src/` | 78 | 0 | 183 | 编排、源码准入、AOT、资源和测试交错 |
| `crates/ryupy-e2e/src/` | 5 | 1 | 1 | 有 `bin/projects.rs`，发现与清单在库内 |
| `runtime/RyuPy.Runtime/` | 459 个 `.cs` | — | — | 对象、标准库、模块与部署支持平铺 |
| `runtime/RyuPy.Runtime.Tests/` | 325 个 `.cs` | — | — | xUnit 测试平铺 |

driver 的 183 个集成测试源码中，123 个是 `tests/` 根层 `.rs`，其余包含
`*_support/` 等辅助模块；不能按 183 个独立测试目标计算。清理前共享 checkout 有
151 个已跟踪 `.snap`，全历史发现 156 个快照路径，均进入产物删除清单。

问题根源是职责和所有权难找，而非文件数量本身。单代码文件 600 行是硬上限；
超限必须拆分，按状态所有者、输入输出、依赖方向和独立测试合同确定边界，
保留文件介绍及关键算法注释，禁止删注释、压行或机械切片凑数。

### 2.3 产品与证据的当前状态

- `tests/aot/stdlib_service/` 已存在 HTTP → URL/Base64 编码 → gzip 解压 → JSON →
  包资源 → SQLite 的同程序应用。其 README 登记了 macOS arm64 的 CPython、JIT、
  原位及搬迁 NativeAOT、缺 SQLite 原生库等验收；当前应用使用 C#/.NET 标准库适配。
- `docs/validation/stdlib-integrated/` 已存在摘要、JSON 和少量 stdout/stderr。
  该目录与 `tests/aot/stdlib_service/` 当前均有未跟踪内容。本轮只读取登记，没有重跑验收。
- `TODO.md` 中 Phase 4 的 NumPy、扩展 ABI、引用/GIL 和部署矩阵仍待实施。
  定向检查 driver / C# 运行时源码，未发现 `libpython`、`Py_Initialize`、
  `PyConfig_`、`PyGILState_`、`PyImport_ImportModule` 宿主入口。
  **随包 CPython 宿主和通用对象桥在本规划中明确列为新工作。**
- 已有 C ABI 绑定与 SQLite provider 应继续保留。新增 CPython 路径必须通过真实对象与
  扩展调用交付，单独导入模块或替换整个程序为外部 Python 子进程都不足以完成该目标。

### 2.4 测试、输入和结果混放

以下为清理前共享 checkout 的已跟踪数量。其 HEAD/index 尚未切换到清理历史，
因此这些数值用于解释旧布局；清理后镜像统计另见 §7：

| 位置 | 文件数 | 主要内容及处理结论 |
| --- | ---: | --- |
| `tests/e2e/` | 279 | 单文件程序及辅助包；维持现有发现入口 |
| `tests/cases/` | 1,725 | 清单入口、多文件源码和资源；按用例整体保留 |
| `tests/fixtures/` | 657 | 编译测试和错误路径输入；保留稳定输入归属 |
| `tests/aot/` | 299 | 应用、部署验收器、合同及说明；不是构建输出桶 |
| `tests/projects/` | 67 | 固定项目、入口、探针和 CPython 基线 |
| `tests/validation/` | 328 | 验收器、合同、输入、原型与部分记录混放 |
| `tests/perf/results/` | 7,809 | 原始运行流、报告、补丁及 C#/Python 原型混放 |
| `docs/validation/` | 107 | 验收摘要与部分运行证据 |

`tests/perf/results/` 包含 129 个 `.log`、2,685 个 `.stdout`、2,616 个 `.stderr`、
1,953 个 `.json`，同时还有 23 个 `.patch`、40 个 `.cs`、39 个 `.csproj` 和
149 个 `.py`。按内容职责分类后保留原型源码和人工总结；补丁退出所有历史，
必要实现确认有正式源码归属，原始恢复材料仅留仓库外持久备份。

`target/`、`releases/` 当前已跟踪文件数为 0，现有忽略规则覆盖二者，以及 C# 的
`bin/obj`、Python 缓存、`*.build/`、`*.dist/`、构建锁和若干指定日志/IR 转储。
抽查的联合应用 `__pycache__` 已被忽略。常见 `.dll/.so/.dylib/.exe/.pdb/.pyc`
跟踪路径查询为空。清理前 ignore 只覆盖部分性能输出；本轮规则已按 ADR0182 收紧，
全部 9,053 个删除路径均命中忽略规则，5,175 个保留路径均未被误忽略。

`tests/validation/markdown_closure_inputs.tar.gz` 属于本轮确认保留的独有输入归档；
`third_party/` 保留许可证和固定来源数据，`vendor/dotnetdll/` 是现有依赖源码。
这些路径按用途审查，禁止用扩展名规则一并清走。

## 3. 不改变的架构边界

遵守 `docs/architecture.md` §2：

- `ryupy-base` 保持基础诊断和源码位置；阶段依赖自上而下，无循环、无跳层回调。
- ruff 直接依赖只在 syntax；`dotnetdll` 只在 codegen-cil。
- codegen-cil 消费 MIR 和运行时 ABI，不引用 HIR / ty，也不决策 Python 模块 provider。
- 生产编译阶段的文件 IO、进程启动和依赖收集继续集中在 driver；
  C# 运行期宿主加载在运行时，测试框架的 IO 归测试基础设施。
- 通用桥调用由 MIR 描述为明确的运行时操作；新增 `RuntimeFn` 同步运行时实现、
  codegen 引用表、诊断及测试，不让 codegen 绕过 ABI 直接操作 CPython 对象。

目标职责链为：

```text
CLI → driver 的 provider 计划与分发闭包
        ├─ 编译流水线 → MIR → CIL → C# 原生实现
        └─ 随包 Python 依赖清单
运行中的 RyuPy 模块 → 统一 provider 路由 → 通用对象桥 → 随包 libpython → 标准库 / NumPy
```

provider 计划描述“哪个模块走哪条路径、部署什么依赖”；对象桥描述“如何跨边界持有与操作对象”。
HTTP、压缩和 SQLite 等模块使用同一桥接机制，不各写一套 FFI、缓存和异常转换。

## 4. Rust 路径映射

所有目标均为未来布局。先移动完整职责文件，再处理必要的文件内拆分；
crate 名、包依赖和外部 API 保持，`lib.rs` 用显式重导出维持既有调用路径。
下表列出实际已有文件的具体落点；通配组须在实施时展开成无重叠的逐文件清单。

| 当前路径 / 文件组 | 目标路径 / 目录 | 职责及注意事项 |
| --- | --- | --- |
| `crates/ryupy-base/src/*`、`ryupy-syntax/src/lib.rs`、`ryupy-cli/src/main.rs` | 原地 | 无此次主线需要的结构性拆分 |
| `crates/ryupy-driver/src/lib.rs` 中流水线主体、`linked_compile.rs`、`session.rs`、`dump.rs`、`stage_trace.rs` | `src/pipeline/{compile,linked,session,dump,trace}.rs` | lib 只做模块入口和公共 API；编排留在 driver |
| driver `modules.rs`、`module_resolver.rs`、`module_hints.rs`、`module_environments.rs`、`instance_modules.rs`、`ordinary_mi.rs` | `src/module_graph/{mod,resolver,hints,environments,instances,ordinary_mi}.rs` | 模块图与已有运行模式；避免与 provider 来源选择混为一个对象 |
| driver `source_catalog.rs`、`source_catalog_archive.rs`、`source_scan.rs`、`source_probe.rs`、`optional_sources.rs`、`binding_sources.rs` | `src/providers/{catalog,archive,scan,probe,optional_sources,binding_sources}.rs` | 当前来源准入；新增 `plan.rs` 统一 provider 决策 |
| driver `aot.rs`、`default_aot.rs`、`dotnet.rs`、`aot_platform.rs`、`aot_sdk_paths.rs`、`aot_work_paths.rs` | `src/toolchain/{aot,default_aot,dotnet,platform,sdk_paths,work_paths}.rs` | SDK / ILC 调用和独占构建工作目录 |
| driver `aot_layout.rs`、`aot_layout_files.rs`、`manifest.rs`、`manifest_files.rs`、`manifest_sdk.rs`、`runtime.rs`、`source_deployment.rs`、`distribution_metadata*.rs` | `src/distribution/`，保留原文件基名 | 产物布局、依赖闭包、runtime 定位、清单和来源记录 |
| driver `resources.rs`、`resources_paths.rs`、`resources_stage.rs`、`resource_discovery.rs` | `src/distribution/resources/{mod,paths,stage,discovery}.rs` | 资源发现、路径与复制；路径校验保留在所属步骤 |
| driver `native_binding.rs`、`native_binding_files.rs`、`native_binding_images.rs`、`native_binding_manifest.rs`、`native_binding_signature.rs`、`binding_build.rs` | `src/bindings/{mod,files,images,manifest,signature,build}.rs` | 已有声明式 C ABI，保持与 CPython 桥边界清楚 |
| driver `trace_product.rs`、`accumulator_options.rs` | `src/optimization/{trace_product,accumulator_options}.rs` | 有界优化选择；不进入宿主加载器 |
| `crates/ryupy-codegen-cil/src/assembly.rs`、`entry.rs`、`literals.rs`、`types.rs`、`callables.rs`、`modules.rs` | `src/emit/`，保留原文件基名 | PE/CIL 发射，按运行时合同工作 |
| codegen `refs.rs`、`assembly_refs.rs`、`stackify_refs.rs` 及其余生产 `*_refs.rs` | `src/references/{mod,assembly,stackify,...}.rs` | 引用身份、签名与方法引用；测试文件另行归属 |
| codegen `stackify.rs`、`stackify_operands.rs`、`stackify_runtime.rs`、`stackify_rvalue.rs` | `src/stackify/{mod,operands,runtime,rvalue}.rs` | 栈化算法独立维护 |
| codegen `runtimeconfig.rs`、`distribution_metadata.rs`、`candidate_sources.rs`、`source_provenance.rs`、`emission_report.rs` | `src/artifact_contract/`，保留原文件基名 | 输出合同与来源数据，复制和加载仍由 driver / runtime 完成 |
| codegen `native_abi_emit.rs`、`native_abi_types.rs`、`fixed_binding_emit.rs`、`fixed_bindings.rs`、`fixed_binding_deployment.rs` | `src/bindings/`，保留原文件基名 | C ABI 发射；`fixed_binding_refs.rs` 仍归 references |
| codegen `lexical_super.rs` | `src/emit/lexical_super.rs` | 对象调用发射 |
| `crates/ryupy-hir/src/hir.rs`、`class_hir.rs`、`operator.rs` | `src/ir/{mod,classes,operator}.rs` | HIR 数据模型 |
| HIR `lower.rs`、`lower_fn.rs`、`lower_stmts.rs`、`expr.rs`、`control_flow.rs`、`assign_target.rs`、`aug_assign.rs`、`chain_assign.rs`、`sequence_target.rs`、`with.rs` | `src/lowering/`，保留原文件基名 | 基础语法降级 |
| HIR `scope.rs`、`closures*.rs`、`direct_globals.rs`、`unresolved_globals.rs` | `src/scopes/`，保留原文件基名 | 名称、闭包与 cell；测试文件排除通配规则 |
| HIR `imports.rs`、`imports_lower.rs`、`module_*.rs`、`future_imports.rs` | `src/modules/`，保留原文件基名 | 模块定义和导入语义；排除对应测试 |
| HIR `class.rs`、`class_bases.rs`、`class_body.rs`、`class_scope.rs`、`lexical_class_*.rs`、`lexical_private.rs`、`ordinary_mi.rs`、`native_bases.rs`、`runtime_bases.rs` | `src/objects/`，保留原文件基名 | 类和布局语义；`class_hir.rs` 优先归 ir，测试另列 |
| `crates/ryupy-ty/src/check.rs`、`check_attrs.rs`、`check_stmts.rs`、`call.rs`、`infer.rs` | `src/checking/`，保留原文件基名 | 推导与检查 |
| ty `default_modes.rs`、`initial_mode.rs`、`runtime_function_modes.rs` | `src/modes/`，保留原文件基名 | 静态/动态边界选择 |
| ty `ty.rs`、`ops.rs`、`opaque.rs`、`module_metadata.rs`、`module_vars.rs` | `src/model/`，保留原文件基名 | 类型、运算规则与模块旁表 |
| `crates/ryupy-mir/src/mir.rs`、`operations.rs`、`exact_types.rs`、`native_abi.rs`、`runtime_fn.rs` | `src/ir/`，保留原文件基名 | MIR 和运行时操作合同，保留既有公共路径重导出 |
| MIR `build.rs`、`build_*.rs`、`expr_builder.rs`、`expr_ops.rs`、`expr_values.rs`、`call_builder.rs`、`call_builtins.rs`、`container_literals.rs`、`prepared_call.rs` | `src/lowering/`，保留原文件基名 | HIR→MIR；类/异常/模块子域按维护需要继续收拢 |
| MIR `validate.rs` 与生产 `validate_*.rs` | `src/validation/{mod,...}.rs` | CFG、区域、ABI 和对象约束；测试另列 |
| MIR `generator_product*.rs`、`trace_*.rs`、`build_product_trace.rs` | `src/optimization/{generator_product,trace}/` | 候选选择、证明与产品发射分层；`build_product_trace.rs` 优先归这里 |
| MIR `generators.rs`、`generator_exceptions.rs`、`yield_expr.rs` | `src/generators/`，保留原文件基名 | 普通生成器 lowering，与优化证明分离 |
| MIR `passes/` | 原地 | 已有职责域，无需重复迁移 |
| 各阶段 `pretty.rs`、HIR `pretty_class.rs` | 各 crate `src/pretty/{mod,classes}.rs` | 展示与诊断，不进入 IR 定义 |
| `crates/ryupy-e2e/src/lib.rs` 的清单/发现/执行逻辑，`results.rs`、`single_expectation.rs`、`project_manifest.rs` | `src/{cases,discovery,execution,reporting,projects}/` | 先抽纯清单模型和解析，再隔离进程执行；`bin/projects.rs` 留作薄入口 |

**规则优先级**：逐文件特例 → 测试归属 → 职责通配组。诸如
`build_product_trace.rs`、`class_hir.rs` 不重复分配；未列入映射的文件先原地保留，
由所属领域维护者逐项登记，禁止通配批量搬迁补齐表格。

私有测试迁入所属域的 `tests/` 模块，由 `#[cfg(test)] mod tests` 接入。
对访问多个同级私有模块的测试，先保留 crate 根测试入口及其可见性，再单独设计归属，
禁止为了移动测试把生产内部对象全部改成 `pub`。

## 5. 运行时、provider 与随包依赖的目标布局

### 5.1 已有文件迁移

| 当前路径 | 目标路径 | 保持的合同 |
| --- | --- | --- |
| `runtime/RyuPy.Runtime/PyObject.cs`、`PyClasses.cs`、`PyOps.cs`、`PyPreparedMethods.cs` | `Objects/` 下同名文件 | 现有运行时对象协议；按职责迁移，暂不改类型或命名空间 |
| `PyHttp.cs`、`PyHttpClient.cs`、`PyHttpErrors.cs`、`PyHttpHeaders.cs`、`PyHttpRequest.cs`、`PyHttpResponse.cs` | `Stdlib/Native/Http/` 下同名文件 | 已有 .NET HTTP 路径与错误合同 |
| `PyBase64.cs` | `Stdlib/Native/Encoding/PyBase64.cs` | 现有编解码行为 |
| `PySqlite.cs`、`PySqliteConnection.cs`、`PySqliteCursor.cs`、`PySqliteStatement.cs` | `Stdlib/Native/Sqlite/` 下同名文件 | 原生 provider 保留、资源与事务语义保留 |
| `PyImportlibResources.cs`、`PyModuleResourceLocations.cs`、`PyResourcePathContext.cs` | `Modules/Resources/` 下同名文件 | 包数据定位与搬迁路径合同 |
| `runtime/RyuPy.Runtime.Tests/PySqliteTests.cs`、`PyImportlibResourcesTests.cs`、`PyResourcesAsFileTests.cs` | `Stdlib/{Sqlite,Resources}/` 下同名文件 | 命名空间和测试标识暂保持 |
| `runtime/RyuPy.Runtime/RuntimeDependencies.props`、项目清单及许可证 | 第一阶段原地 | Makefile 的复制规则与 AOT 薄宿主依赖其现有位置 |

两个 C# 项目当前使用 SDK 风格默认源码包含规则。移动子目录须审查默认 glob、资源路径、
生成器和 `CallerFilePath` 等使用点；独立实验 `.cs` 不得落入生产项目的默认编译范围。
生产类型、程序集身份和运行时方法签名的改名，必须另开 ABI 变更，不能夹在目录移动里。

### 5.2 新能力的预留职责目录

下列目录当前是计划，**本轮没有创建**：

| 计划目录 | 所有权与边界 |
| --- | --- |
| `runtime/RyuPy.Runtime/Providers/` | 中性 provider 与对象操作合同、注册和调用记录；不引用 CPython 互操作项目，构建期选择仍归 driver |
| `runtime/RyuPy.CPython/Host/` | 独立 C# 项目中的初始化、随包路径、单解释器生命周期、线程附着和 GIL；子解释器另排 |
| `runtime/RyuPy.CPython/Abi/` | 受控的 C API 声明、版本/架构检查、AOT 可分析的调用及回调入口 |
| `runtime/RyuPy.CPython/Objects/` | owned/borrowed 引用、身份映射、跨堆可达关系、循环与释放队列 |
| `runtime/RyuPy.CPython/Operations/` | import、属性、调用、迭代、下标、基础转换和异常；保留别名、可变性与回调 |
| `runtime/RyuPy.CPython.Shim/` | 最小 C shim 源码与构建定义，隔离版本相关初始化结构、宏和 callable 代理；禁止逐库重写 |
| `crates/ryupy-driver/src/providers/cpython.rs` | 构建期 provider 计划与可解释诊断；不承载运行期 GIL 或引用管理 |
| `crates/ryupy-driver/src/distribution/python/` | 收集解释器、标准库、扩展、本地动态依赖、包资源与许可证，形成可搬迁闭包 |
| `runtime/RyuPy.CPython.Tests/` | 独立互操作合同测试项目，覆盖生命周期、线程、异常、别名、失败初始化和长期运行；Runtime.Tests 保留中性合同测试 |
| `third_party/python/` | 当前许可证/生成来源说明继续保留；未来增加随包发行物的来源与版本说明，区分两种用途 |

以上新项目属于 CP1 开发落点，后续需同步工程引用、构建、测试发现与部署命令；本轮只登记路径。
依赖为 `RyuPy.CPython → RyuPy.Runtime`，应用入口同时引用二者并注册 provider；
CPython 指针、GIL 与引用所有权仅在互操作组件和 shim 内出现。

桥接对象保留原始 Python 对象身份；容器跨边界默认代理，按明确合同转换基础值。
禁止靠 JSON 序列化重建整个对象图。跨堆引用需有容量、观测和释放策略，
避免永久强引用表；关闭宿主时必须处理仍存活句柄、回调和退出次序。

拟议 standalone 的逻辑闭包：应用可执行文件、RyuPy 运行时所需文件、应用资源、
`python/` 下的 libpython / 标准库 / 扩展 / 依赖，以及 `licenses/` 和部署清单。
不同平台可使用不同物理库布局，清单按目标 RID 表达真实依赖。
运行时搜索根锚定发行目录，禁止依赖 cwd、开发环境 Python、用户 site-packages 或构建缓存补库。
AOT 编译的宿主和模块、由 CPython 执行的部分应在产物清单中分别列出。

## 6. 测试、输入、即时预期和验收证据分层

本节以 ADR0182 为准：源码仓只保留必要代码、手写输入、配置、依赖锁、许可证与人工文档。
生成 golden、`.snap`、patch/diff、比较结果、机器报告全部禁入；改扩展名或嵌入大段 Markdown
也按产物处理。手写语义断言保留，禁止将旧输出机械转成源码常量。

### 6.1 当前路径到目标职责

| 类型 | 当前路径 → 计划位置 | 发现与验收合同 |
| --- | --- | --- |
| Rust 单元、IR、诊断 | `src/*_tests.rs` → 所属职责域 `tests/` | 显式 `cfg(test)`；快照删除后改结构、不变量、错误码、位置与必要消息断言 |
| driver 集成测试 | `tests/catalog_colorsys.rs` 等 → `tests/provider_catalog/`、`tests/distribution/` | 保留旧根文件薄入口和 `cargo test --test <旧名>`；辅助模块不独立注册 |
| C# 对象与标准库合同 | Runtime.Tests 平铺 → `Objects/`、`Modules/`、`Stdlib/`、`Providers/` | 保留 xUnit 标识；桥接合同进入独立 `RyuPy.CPython.Tests` |
| 单文件行为 | `tests/e2e/phaseN/`、`stdlib/` → 首轮原地 | 固定用例 ID、pending 和侧车合同，CPython 同次比较 |
| 多文件行为 | `tests/cases/<域>/<用例>/` → 原地 | `case.txt`、程序、手写资源整体保留；stdout 文件清除 |
| 编译及错误输入 | `tests/fixtures/<域>/` → 原地或领域 `fixtures/` | 仅保存必要输入；配置 JSON、包元数据和许可证按用途保留 |
| 联合应用 | `tests/aot/stdlib_service/{app.py,appdata/}` → `tests/fixtures/applications/stdlib_service/` | 整体是应用源码，禁止清理时整删；未来两种 provider 复用同程序 |
| 联合部署验收 | `tests/aot/stdlib_service/run.py` → `tests/acceptance/distribution/stdlib_service.py` | 路径迁移另批处理，保持入口、资源与业务合同 |
| 其他 AOT、专项验收 | `tests/aot/<主题>/`、`tests/validation/`、`scripts/*validation*.py` → `tests/acceptance/<职责>/` | 输入进入 fixtures，驱动与合同相邻；不按目录名整删 |
| 固定项目 | `tests/projects/{lock.tsv,manifest.tsv,entries,probes}` → 首轮原地 | 保持固定项目分母、版本和来源分类；`baselines/` 删除，runner 待改即时预期 |
| 性能输入与工具 | `tests/benchmarks/`、`benchlib/`、`run_benchmarks.py` → 首轮原地 | 每次配对生成正确性及性能数据，先过正确性门 |
| 实验源码 | results 内 `.py/.cs/.csproj/.sh` → `experiments/<主题>/src/` | 保留可复现原型、必要手写输入、人工结论；实验不自动进入主测试发现 |
| 人工验收摘要 | `docs/validation/<主题>/*.md` → 原地 | 保留独有结论、环境、版本和重跑命令；清理机器流/JSON/批量报告 |
| 测试与比较输出 | 旧结果树 → 仓库外每次临时目录 | 默认清理；仅显式失败调试保留仓库外路径，禁止写回源码树 |

`tests/validation/generator_product/goldens/` 中五个 `.py` 是输入源码，保留并在后续布局
迁移时改为职责名；其机器 manifest 清除。`markdown_closure_inputs.tar.gz` 是独有输入归档，
本轮保留；后续根据使用合同决定能否由测试现场生成，不能按压缩包扩展名直接删。

### 6.2 实际发现规则及迁移保护

1. Rust e2e 递归扫描整个 `tests/e2e/` 的 `.py`，跳过名为 `fixtures` 的目录，
   同名 `.case.txt` 为侧车。辅助脚本进入普通子目录会增加执行用例。
2. Rust 扫描 `tests/cases/` 时跳过 `fixtures`，遇 `case.txt` 即作为完整用例并停止向内发现。
3. Python baseline 默认只选 phase1–3；phase4 要显式选择，`stdlib/` 当前未纳入同一默认集合。
4. Python 目录发现只在 phase2 选择下递归匹配 `case.txt`，与 Rust 的停止规则存在差别。
   统一发现属于独立合同变更，先登记 ID、pending、排除原因，再逐项接线。
5. Cargo 根层集成测试自动发现；e2e 为 `harness = false`。直接移到深目录会丢失目标，
   因此先保留根层薄入口或显式注册，禁止用例数缩水。
6. 删除 insta 快照后改结构断言，不迁移 `.snap`、不建立旧格式兼容层、不开自动接受模式。
7. 核对 `include_str!`、`CARGO_MANIFEST_DIR`、`__file__`、相对导入、Makefile、CI、资源清单
   与项目 glob。目录改名不得改变测试语义、输入选择和忽略状态。

### 6.3 即时 CPython 比较合同（待 runner 实施）

- 每次测试先用固定 CPython 版本运行同份源码，再运行 RyuPy；参数、stdin、locale、时区、
  版本锁与必要环境一致。缺解释器、超时和意外失败直接报环境阻断或比较失败。
- 两臂使用全新独立 cwd、数据库和输出根，HTTP fixture 每臂重置；冻结显式随机种子与时钟输入。
- 只按具名字段规则归一化临时路径/端口；保留业务值、异常类型、退出码与副作用。
  数据库和资源副作用由独立源码断言核验，异常用例遵循手写异常合同。
- HIR/MIR/CIL 与 RyuPy Diagnostic 无 CPython 预期，使用结构、控制流不变量、错误码和位置断言。
  仅调试时导出 IR 文本，退出清理；禁止伪造 CPython 编译 IR 基线。
- 不读取旧 stdout/golden，不回退快照，不跳过缺预期用例，不降低断言求绿。

### 6.4 证据与临时存储

| 层 | 保存内容 | 生命周期 |
| --- | --- | --- |
| 源码合同 | 驱动、断言、case、手写输入、锁与许可证 | Git 长期维护 |
| 同次比较 | CPython/RyuPy stdout、stderr、退出码、数据库与资源检查 | 仓库外运行目录，退出默认清理 |
| 部署验收 | 原位/搬迁、平台、依赖缺失、provider 来源的执行结果 | 同一仓库外专属目录；只人工总结入文档 |
| 性能采样 | 匹配工具链、配对样本、资源与热点分析 | 当次生成，失败/调查显式保留仓库外 |
| 历史恢复 | 旧 refs、补丁、旧结果与未提交工作归档 | 仓库外持久备份，不再重新导入 Git |

`target/`、C# `bin/obj` 只作已忽略构建缓存，禁止用作测试档案区。
失败保留需显式选项、打印绝对路径和清理说明；成功默认删除，异常退出需有可靠清理逻辑。
忽略规则只防误加；后续提交门/CI 还需检查已跟踪文件用途，拦截强制添加的机器产物。

## 7. 本轮历史清理与保全

### 7.1 精确范围及备份

持久目录：`/Users/laull/Backups/RyuPy/history-cleanup-20261113-173023/`。

- `remote-original.git` 保留远端原始历史与归并的本地 refs；`local-all-refs.bundle`、
  `local-git-metadata.tar.gz` 保存本地历史、reflog 和 worktree 索引。
- 主工作区 tracked/untracked 文件、未暂存/暂存差异及各 worktree 开发差异均外部归档；
  发布前另补并发文档归档，忽略文件另行完整备份，恢复不依赖 `/tmp`。
- 最终镜像为 `rewrite-final-source-only.git`；前几轮镜像只是审计中间材料，不得发布。
- 精确删除清单为 `final-source-only-manifest.json`（路径与理由）和
  `final-source-only-remove-paths.txt`（literal 路径），保留清单为 `final-source-only-preserved-paths.txt`。
- 全历史 14,228 个路径中删除 9,053 个产物路径，保留 5,175 个路径；
  其中快照 156 个、patch/diff 29 个，六份 AOT 历史 stdout 也已纳入。
- 清除测试/性能输出、生成 CPython 基线、golden/trace 预期、机器 JSON/Markdown、IR/JIT
  转储及缓存标记。保留所有产品和测试源码、case、手写配置/输入、锁、许可证及人工摘要。

补丁审核记录在 `patch-source-audit.json` 和 `patch-review-conclusion.md`：
29 个补丁路径触及 96 个源码路径，77 个路径新增行在正式历史逐字可定位；
其余为未采纳实验、已重构签名/局部变量或被替换用例。必要实现已有正式源码归属，
原始补丁只保存在外部恢复材料；本轮没有借清理开发新实现。

### 7.2 验证、发布与本地同步

逐个直接比较 414 个提交，保留文件的 blob、模式、作者、消息与父序拓扑全部一致；
仅允许清单内删除，提交节点保留 414 个，其中 412 个身份因树或父链变化而重写。
157 个镜像 refs 全部核验，包括 48 个远端 refs 和 109 个 local-preserved refs。
注解 tag 保留原 tagger/消息并更新目标；tree 型 checkpoint 按同清单递归重建。
没有计算普通文件 SHA256/MD5。

| 口径（合并 refs 可达对象） | 清理前 | 清理后 |
| --- | ---: | ---: |
| Git 对象数 | 18,883 | 13,412 |
| blob 逻辑字节 | 121,606,629 | 75,884,755 |
| 等价 compact pack 全文件字节 | 15,269,605 | 10,969,876 |

完整验证见 `final-source-only-verification-report.json`、`final-source-only-all-ref-map.json`。
发布采用逐 ref 原 OID 租约与显式 39 heads + 9 tags，不使用 mirror push、prune 或普通强推；
推前复核远端集合和值，远端独有 dev 内容沿其自身历史清理，绝不用本地 dev 覆盖。
已完成 atomic dry-run 与实际租约强推，远端 39 个 heads、9 个 tags 共 48 个引用
逐项回读全部符合重写映射，远端引用集合无新增、无遗漏。109 个 local-preserved refs
仅留外部备份，未发布。逐 ref 的原值、目标值、回读值见外部 `push-receipt.json`。

共享 checkout 的 HEAD/index 与其余 worktrees 当前保持旧历史；必须与协调者约定停写窗口后
另做本地同步。旧工作区仍含旧已跟踪产物，不能直接继续向清理后远端推送旧分支。
本轮不会擅自 reset、checkout、stash 或清理这些开发工作区。

## 8. 迁移先后顺序与阶段门

历史清理先完成，runner 修复与目录搬迁分开；不要以“先全仓重排”阻塞 CPython 竖切。

| 阶段 | 顺序与动作 | 退出门 |
| --- | --- | --- |
| P0：同步窗口 | 确认所有者、停写窗口、恢复材料和 refs 映射，再同步本地旧历史 | 所有未提交工作保全，index 状态逐项核对，禁止重引入旧历史 |
| P1：测试基础设施 | 逐套件替换旧产物引用，外部临时目录和同次 CPython/结构断言 | 用例 ID/pending 不丢，禁回退/跳过；重跑受影响套件 |
| P2：最小宿主与桥 | CP1：独立 RyuPy.CPython、shim、Runtime 中性合同与入口注册 | 单向依赖；真实同进程对象调用、GIL/引用/异常/缺库负例 |
| P3：联合应用切换 | CP2：同一 stdlib_service 全链切 CPython provider | CPython、C# 对照、桥接 JIT/AOT、搬迁和数据库副作用逐臂一致 |
| P4：真实 NumPy | CP3：冻结解释器/NumPy/RID，验证 ndarray/dtype、读写别名和计算 | 真实扩展加载，干净机搬迁，缺依赖/ABI 错误负例，资源和 GIL 合同 |
| P5：分发闭环 | CP4：standalone 平台矩阵、许可证与依赖闭包，再 onefile | 无构建机路径/开发 Python 依赖；解包、清理、并发与失败恢复 |
| P6：热点替换 | CP5：采样后逐域换 C# 原生实现，顺带整理对应职责目录 | 结果、身份、别名、副作用与异常一致，配对性能有净收益 |
| P7：其余目录 | 按 §4/§5 逐职责迁 HIR/ty/MIR/codegen 和验收器 | 发现集合、测试入口、引用、文档闭合，600 行硬门通过 |

P2 前仅为实际需要建立最小目录；P7 非前置全仓工程。每次先移完整职责文件，再单独拆文件。
P3 所有指定调用必须走选中的 provider，禁止残留 C# 静默补齐；业务源码与输入保持同份。
搬迁验收只操作仓库外专属副本，检查不同 cwd、受限 PATH、中文/空格路径及缺随包库负例。
P4 先对象语义再 Buffer Protocol/零拷贝；macOS/Linux/Windows 分别登记。

## 9. 共享 checkout 安全与命令影响

driver 来源/AOT/模块环境、catalog 测试、MIR runtime_fn 和路线文档已有并发改动；
未跟踪联合应用、ADR 与摘要及旧 ADR 删除均由原所有者维护。本任务不暂存、不恢复它们。
主代理已完成 ADR0182 规则同步；当前协作风险是开发文件与旧 HEAD/index 的后续同步。

| 操作 | 影响与执行边界 |
| --- | --- |
| `git status`、`git ls-files`、`git diff`、`git ls-remote` | 本轮读取盘点；引用值用于并发检查 |
| bundle、tar、mirror clone | 本轮写仓库外持久备份，主工作区不切换 |
| `git filter-repo --invert-paths --paths-from-file <清单>` | 本轮只在隔离镜像执行，清单为精确 literal 路径，空提交与父拓扑保留 |
| `git diff-tree --raw -r --no-renames <旧> <新>` | 本轮逐提交直接核验保留源码与模式，只接受清单内 D |
| `git gc --prune=now` | 仅隔离重写镜像，禁止作用主 `.git` 或备份原始镜像 |
| `git push --atomic --force-with-lease=<ref>:<旧OID> <remote> <显式refs>` | 本轮授权发布历史；先 dry-run、再推送、再逐 ref 回读，不发布 local-preserved refs |
| 修改 `.gitignore` 和本文 | 本轮限定编辑，不提交主工作区，ignore 无法自动去除已跟踪文件 |
| `git mv -- <旧> <新>` | 未来目录迁移；同时改变 index，须协调文件所有者，禁止 `-f` 覆盖 |
| `git rm --cached -- <精确产物>` | 只影响 index，不能独立完成历史同步；主工作区本轮不执行 |
| `cargo test`、`dotnet test`、`make check` | 未来 runner 迁移后验证；构建缓存可留工具目录，测试输出必须仓库外 |
| `make projects-baseline`、`make bench-save`、`cargo insta review` | 旧落盘入口待移除/重设，不得作为有效验收门或自动修复手段 |
| `make build`、搬迁验收脚本 | 会生成或替换发行目录；只有专属所有权目录可清理，禁止碰并发源码 |

后续迁移账本逐文件记录旧/新路径、tracked/untracked/modified/deleted 状态、所有者及引用点。
保留既有测试目标、用例 ID、pending 与失败身份；先审移动再审接线，行为变更另开竖切。
禁止全仓 add/clean、硬 reset、自动 stash 或覆盖复制；回滚限本批路径，不覆盖他人的修改。
本轮历史强推已有明确授权，主工作区代码/文档提交与未经协调的 HEAD/index 同步未执行。

## 10. 清理后的测试状态与阻断

本轮只运行 Git 历史验证与规则检查，没有运行产品构建或测试，也没有修改测试 runner。
删除旧产物后，完整 `make check` 暂时阻断；保留驱动与用例，按下表逐套件迁移。

| 现存消费者 | 阻断与后续正确动作 |
| --- | --- |
| driver `tests/snapshots.rs` 及 insta 测试 | `.snap` 已删；改 IR/诊断结构断言，禁止自动创建/接受新快照 |
| `crates/ryupy-e2e/src/bin/projects.rs`、`tests/run_projects_baseline.py` | 仍读写项目 baselines；改固定 CPython 同次预期，保留项目 ID |
| Runtime.Tests `PyTextWrapTests.cs`、两个 `generate_*_baseline.py` | textwrap/random JSON 生成预期已删；改外部即时参考与独立输入 |
| driver `tests/dynamic_except_support/mod.rs` | 仍读 `.labels`；改显式源码结构/映射断言 |
| AOT decimal/tzinfo/ordinary_mi 等验收器 | 仍读 SOURCE/EXPECTED/stdout；删除 hash/旧格式兼容链，改同次比较 |
| source_provenance、generator、trace、metadata 专项脚本 | 仍有 frozen/reference/identity 结果消费者；保留手写合同，逐个迁移即时或结构断言 |
| perf full-suite 等脚本 | 仍复制 expected、写 source.patch/报告到 repo；改仓库外临时、当次配对、默认清理 |
| Makefile 的 baseline/bench/insta 相关入口 | 命令及结果出口待迁移；禁止为了入口绿而跳过失效套件 |

已有 e2e 的 `run_cpython` 提供现场比较起点，但发现域、输出根与副作用隔离仍需完成迁移。
定向引用清单另存外部备份的 `runner-migration-references.txt`，供下一竖切逐项核销。
后续每套件先完成真实消费者改造，再跑窄门、完整检查及搬迁矩阵；环境阻断和产品失败分别登记。

## 11. 下一次协调入口

先约定主工作区停写窗口与 refs 映射应用方式，保护当前路线文档和联合应用源码。
随后处理 P1 的旧 runner 依赖，再推进 P2 的最小宿主与对象桥。
Rust 平铺职责归类、C# 目录映射和剩余验收器迁移全部沿本文计划推进，尚未发生源码搬移。
