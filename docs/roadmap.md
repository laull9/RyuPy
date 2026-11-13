# 愿景与路线图

## 当前主线：CPython provider 优先（2026-11-13）

项目目标调整为：**以随包 libpython 和通用对象桥获得良好 Python 生态兼容，保留
Python → CIL → NativeAOT 的编译链，长期对标 Nuitka 的 AOT 打包，并按实测热点优化性能。**
CP1–CP3在macOS arm64已通过18项JIT/NativeAOT搬迁、原联合应用及五项生命周期模式；
真实错版本ABI已通过，混合异常栈无陈旧帧严格门待验；完整门禁、Linux/Windows和干净机继续验收。决策见
[ADR-0181](adr/0181-cpython-provider-first.md)，架构合同见 [CPython provider](cpython-provider.md)。

### 短期顺序

| 顺序 | 目标 | 完成门 |
| --- | --- | --- |
| CP1 | 可随包 libpython 宿主和通用对象桥 | 固定发行和 ABI；身份/别名、异常、回调、GIL、生命周期；JIT/NativeAOT 与搬迁 |
| CP2 | 同一联合应用切换 CPython provider | 保留 HTTP → 编码解压 → 包资源 → SQLite 原程序；同次 CPython 即时比较；真实搬迁及干净机 |
| CP3 | 一个真实第三方 C 扩展 | 首选 NumPy，锁定版本/平台；扩展加载、计算、回调、异常、生命周期和原生依赖搬迁 |

主线不再扩张逐模块 C# 重写和逐库 C ABI 适配。已实现能力保留回归、维护和性能对照；
新缺口优先补通用宿主/对象桥，直接复用 CPython 标准库与扩展。
先在显式兼容配置完成 CP1–CP3，再晋升默认兼容配置；当前 CLI 默认选择保持不变。
纯 RyuPy 模式继续作为显式轻量与性能路径。

### 长期顺序

- **CP4 分发与覆盖**：扩大标准库真实应用验收，完善资源、metadata、动态导入声明和
  原生依赖闭包；逐项验收 macOS arm64、Linux x64、Windows x64 standalone，随后交付 onefile。
- **CP5 性能**：先测跨桥次数、GIL 等待、分配/复制及端到端瓶颈，再做批量调用、typed CIL、
  守卫特化、buffer 和必要的 C# 原生替换。每项优化同时交付正确性与应用级性能证据。
- **对标 Nuitka**：固定相同应用、版本、输入与部署条件，对照兼容性、分发体验、启动、
  吞吐、内存、体积和构建成本；收益按 workload 发布。

### 结构与计量

代码与测试按职责分层，保留稳定用例 ID、入口和必要手写输入；CPython 预期当次生成，
测试输出放仓库外临时目录。Git 仅保留必要源码、配置与文档，规则见
[ADR-0182](adr/0182-source-only-test-oracles.md)。迁移步骤见 [仓库结构规划](repository-layout-plan.md)。
代码和测试按一条发现链一个迁移竖切推进。全历史生成产物清理与强推已完成，
生成 golden、快照与 patch/diff 已清理；现有测试消费者继续迁移到即时比较。
固定 20 项继续作为可比较项目集，产品功能兼容率允许已验收的 CPython provider，
纯 RyuPy 执行率另列。短期先通过 CP1–CP3，之后产品目标至少 16/20；历史原生 16/20
转为长期优化指标。JIT、NativeAOT、搬迁、平台分别计量。

## 历史路线与已积累能力

以下保留旧排期及阶段编号，供追溯实现与测试；执行顺序统一采用本页 CP1–CP5。
旧条目中的“当前”“接下来”和“后续”均指当时决策，原验收状态保持原记录。

> **2026-11-02恢复与排期更新**：当前代码恢复自已推送M3.10（`a788d86`），其已归档项目能力为5/20。此前未推送M3.11源码随临时工作区丢失，历史10/20及725项C#测试不能当作当前checkout能力。用户已暂停16/20前置，优先原生AOT/JIT，并批准小步提交推送与独立WIP远端备份。详见[ADR0042](adr/0042-aot-jit-recovery-priority.md)；原兼容目标未达成、不勾完成。

> **历史迭代顺序，已由 ADR-0181 更新**：此前以 C ABI 绑定/部署与有界优化推进打包；现在优先 CPython 宿主和通用对象桥，既有 C ABI 保留维护。

修订日期：2026-11-02。本文描述后续计划；已实现能力见 [semantics.md](semantics.md)，执行任务见 [TODO.md](../TODO.md)。本次调整记录在 [ADR-0009](adr/0009-project-compatibility-first.md)。

## 2026-11-13 AOT 打包推进

当前优先交付可搬迁 standalone：源码模块、包数据和 C ABI 库共同进入发布目录。已整合单库多导出 v2 绑定与文件系统 `importlib.resources.as_file(Path)`，分别完成 macOS arm64 真实搬迁和差分测试。资源与绑定组合发布已完成 23 步真实搬迁验收；通用 C ABI v3 进一步支持 13 类标量、void 返回和 UTF-8/bytes 只读借用，已通过静态 AOT、`as_file()` 资源组合与受限环境搬迁（[ADR0173](adr/0173-declarative-cabi-signatures.md)）。接下来推进 SafeHandle 所有权、多库依赖闭包、更多源码模块准入及跨平台目标环境验收。

完整标准库继续按 API、底层依赖和平台登记。版本化 catalog v2 已接入五个纯 Python 原源模块，新增 colorsys 六 API 差分与真实 AOT 搬迁；`ryupy probe` 及普通构建报告列出 provider、来源、版本、依赖缺口与动态不确定性，默认发行已开启认证 catalog 静态准入，动态用户模块保留 pilot 策略门（[ADR0174](adr/0174-versioned-source-catalog-probe.md)）。C#/.NET 已整合 SQLite、HTTP/TLS、gzip/zlib、Base64 与摘要认证的常用 API，并通过跨模块应用搬迁；SQLite/TLS 复用成熟 provider 与系统实现。完整 provider 覆盖与 libpython 宿主继续实施。onefile 排在 standalone 分发闭环之后。完整接入路线见 [ADR0172](adr/0172-aot-stdlib-provider-plan.md)。

## 愿景

把常见 Python 项目编译成 .NET CIL，让用户保留现有代码，在可证明类型和对象行为的地方获得编译执行的性能。编译器用 Rust 编写，运行时用 C# 编写。日常开发通过 RyuJIT 运行；Native AOT现为持续性能、动态代码生成适配与独立交付的重大主线，见[ADR0043](adr/0043-aot-delivery-mainline.md)。

原约80%兼容目标按用户最新决定暂停作为前置，当前优先AOT/JIT；恢复兼容推进时，工作顺序再由项目遇到的阻塞决定：容器、异常、模块和标准库排在冷门语法之前。语言特性完成数和几个数值基准不足以说明项目能跑。

“80%”用固定项目集衡量：M2.0 选定至少 20 个、覆盖至少四类用途的纯 Python 项目，锁定版本、入口、依赖和验收测试；Phase 3 目标为其中至少 80% 原生通过。桥接项目另外统计。选集、计算方式和当前证据见 [compatibility.md](compatibility.md)。这个比例限定于该项目集，不能外推为 PyPI 的覆盖率。

## 取舍

- 常用行为按选定的 CPython 版本做差分测试，包括异常、求值顺序、别名和副作用。
- .NET 的 GC 不复现 CPython 的引用计数与即时析构时机。资源释放通过 `with`、`finally`、`close()` 明确表达；依赖 refcount 的项目记录为有偏差或不支持。
- 冷门元编程、运行时生成代码、自定义导入钩子和解释器内部接口暂缓。项目回归集证明它们阻塞常见用途后，再单独排期。
- 低影响行为可以采用更快的默认实现，每项先写 ADR，记录触发条件、收益、示例和适用项目。固定宽度整数、浮点重排等影响常用计算结果的扩展由用户显式选择；未记录的差异按缺陷处理。
- 类型标注提供优化线索。Phase 2 默认兼容路径不因标注而增加 Python 没有的参数类型限制；特化失败返回通用路径。Phase 1 的静态限制仍如实记录。
- 原生路径不依赖运行时生成 IL。libpython 桥接允许扩大生态覆盖，但会带来 Python 部署、对象生命周期和跨边界调用成本。

## 历史五阶段登记

| 阶段 | 范围 | 验收结果 |
| --- | --- | --- |
| Phase 1，已完成 M1.0–M1.5 | 静态函数、数值、控制流、字符串、dataclass | 已有九个 Phase 1 e2e 用例；性能与语义限制均有记录 |
| Phase 2 | 动态核心、容器、普通 class、异常、模块系统 | 无标注的多文件程序，含容器、对象、异常恢复和资源清理，端到端通过 |
| Phase 3 | 闭包、生成器、推导式、装饰器、标准库移植 | 固定纯 Python 项目集原生通过率达到 80%；标准库按 API 清单验收 |
| Phase 4 | 以 NumPy 为首的非纯 Python 模块可引入性 | 在明确的 Python / 扩展 / 平台组合上完成导入、基本调用与错误传播 |
| Phase 5 | 兼容性清单、显式记录的偏差、交付工具链 | 发布可运行项目清单、失败原因和部署依赖；独立验证 Native AOT |

旧 M1.6 的 Native AOT 已转入 M5.2；Linux x64 glibc及macOS arm64/Windows x64 MSVC有限同宿主CLI发布与搬迁已验收（[平台证据](validation/aot-host-platforms/README.md)）；Mac/Windows无SDK干净目标机、自动资源/原生依赖收集、更多平台/性能与桥接各子项仍继续；矩阵乘法和 N-Body 留到有容器后补齐。调整阶段归属不代表这些任务已经完成。

## Phase 2：动态核心、容器、普通 class、异常、模块系统

目标为能组织业务逻辑的多文件程序。实现细节与依赖顺序见 TODO 的 M2.0–M2.9。

| 工作 | 设计要求 |
| --- | --- |
| 动态值与调用 | 定稿 GC 合法的 `PyValue` 布局；未标注参数、异构赋值、默认参数、关键字参数、`*args` / `**kwargs`、函数对象和动静边界 |
| 数值与运算 | 动态算术、比较、真值、短路；i64 溢出升级 `BigInteger`，覆盖大整数字面量与所有整数运算，不把普通数值正确性留到 Phase 5 |
| 异常 | Python 异常继承树、`raise`、`try/except/else/finally`、裸重抛、异常链、用户异常、基本 traceback；映射实际运算产生的 .NET 异常 |
| 容器 | list / tuple / dict / set，索引、切片、解包、可变操作；dict 顺序、相等性与哈希一致性；bytes / bytearray 的常用路径 |
| 协议与内建 | 通用迭代、`for…else`、字符串码点操作、f-string、`len` / `iter` / `next` / `enumerate` / `zip` / `sorted` 等 |
| 普通 class | `__init__`、实例与类属性、绑定方法、单继承、`super()`、常用魔术方法、`property` / `staticmethod` / `classmethod`；保留对象身份和引用共享 |
| 模块与包 | 真实 `import` / `from` / 别名 / 相对导入，模块全局与 `global`、普通包、缓存、循环导入、初始化失败、入口元数据 |
| 资源管理 | `with` 和 `__enter__` / `__exit__`，异常抑制与 `finally` 的控制流；为 Phase 3 文件 API 提供清理基础 |

模块解析由 driver 读取源码并构建模块图；HIR 分配名称与模块身份；MIR 表达模块初始化、动态操作和异常区域；C# 运行时维护模块状态与 Python 对象。后端只消费 MIR，不引入 HIR / ty 依赖。详见 [architecture.md](architecture.md) §7。

属性和调用缓存从第一版就携带失效条件。普通方法替换、类属性写入、实例增删属性都必须让旧缓存失效。多重继承、任意描述符和元类暂缓；不能为了缓存速度静默冻结 Python 对象。

阶段验收使用多文件数据处理与命令行样例，覆盖共享可变数据、重复与循环导入、异常传播和资源清理。此时不宣称已支持任意无标注脚本。

## Phase 3：闭包、生成器、推导式、装饰器与标准库移植

当前M3.1生成器表达式已接入；M3.2已实现yield/return/send、现代单参throw/close、yield-from及跨try/finally/with清理，整里程碑聚合验收与只读复审已通过。M3.3已完成定义位置装饰器、可调用包装与声明范围元数据，纯/混合dataclass表示分别验证。M3.4已完成声明范围的系统/路径/文件API（逐API与平台边界见stdlib-system.md）；M3.5已完成声明范围的math/time/json/re，M3.6已完成声明范围的容器/函数工具与类型元数据，统一数据类表示配有守卫构造、死装箱消除与局部累加槽的实际性能证据；M3.7已完成用户批准的项目入口API与有界收尾。通用容器/调用欠账保留独立未完成项，M3.7最终固定项目入口为5/20（wcwidth、termcolor、colorama、texttable、progress；wcwidth/termcolor/texttable指定上游场景已验收），尚未达到独立M3.11的16/20目标。

### 经用户批准的收敛式排期（2026-11-01）

M3.7 的标准库逐 API 交付不再与追至 16/20 的开放式项目补缺绑在一起。
本轮保留已声明 API、许可证与上游证据，收尾当前 progress mapping `%`、chardet
`Flag` 子集并复测；若暴露重大新链，只登记准确结果并按影响项目数/成本排序。
M3.7 完成不能宣称项目兼容目标完成，固定分母、预定入口和成功标准均不改变。
原项目覆盖验收完整迁至 **M3.11 独立兼容性门**，仍须至少16/20原生通过；
Phase 3 整体目标仍受该门约束。**M3.10** 则独立验收面向 .NET JIT 的直接typed CIL、
特化、逃逸/装箱消除、容器与循环/SIMD证据，不以更快通用解释helper替代。
M3.8 桥接保持未完成，按用户JIT方向暂后排：它不是原生MIR/CIL优化的技术依赖。
M3.9首批有界优化与M3.10有界JIT交付均已完成验证；没有把后排桥接勾作完成。
M3.9两类有证据规则默认启用、两类实验默认关闭；整套默认main并未净提速，
原始不利样本与装箱热点保留，详见[验收记录](notes/2026-11-01-m3.9-validation.md)。
后续候选按 [影响面与成本](project-backlog.md) 登记，不保证解除首阻断即项目通过。
这是用户明确授权的验收/排期调整，原文与后果见 [ADR-0032](adr/0032-bounded-stdlib-and-compatibility-gate.md)。

高级语法服务于实际库代码。闭包使用共享 cell 保留 late binding 和 `nonlocal` 写入；推导式保留独立作用域与求值顺序；生成器使用可持有堆引用的状态机，覆盖 `send` / `throw` / `close`、`yield from` 和暂停期间的 `finally`；装饰器按 Python 顺序执行并允许返回新的可调用对象。

标准库移植单独按工程交付。C# 运行时重写和适配预计涉及万行级实现与测试，不能藏在一个“补标准库”条目里。语法支持允许后，也可编译经审查的纯 Python 标准库源码；其底层模块仍要由 C# 实现或 libpython 桥接。复用源码需要跟踪版本和许可证。

| 批次 | 优先模块与用途 | 实现路径 |
| --- | --- | --- |
| 基础系统与数据 | `sys`、`os` / `os.path`、`io`、`pathlib`、`math`、`time`、`json`、`re` | C# 语义适配；逐 API 测试，正则、路径与数值行为单列差异 |
| 常用程序结构 | `collections` / `collections.abc`、`itertools`、`functools`、`dataclasses`、`typing` 的运行时常用部分、`contextlib` | C# 原生实现，或编译已审核的纯 Python 实现；跟踪传递依赖 |
| 项目入口与测试 | `argparse`、`logging`、`unittest`、`enum`、`copy`、`datetime`、`random`、`string` | 由项目阻塞排序，按 API 分批交付 |
| 尚未移植的必要模块 | 固定项目明确依赖的模块 | Phase 3 建立最小 libpython 桥；显式声明环境，另列桥接通过率 |

标准库计划详见 [stdlib.md](stdlib.md)。`import json` 成功只说明加载成功；只有列出的 API 与差分测试通过，才算该部分可用。桥接到 CPython 执行整项测试，不计入 RyuPy 原生通过率。

`async/await`、完整 `asyncio`、线程调度、模式匹配与重型运行时反射退出当前必做路径。若它们阻塞选集，保持失败记录并重新评估优先级，不删项目凑比例。

## Phase 4：以 NumPy 为首的非纯 Python 模块可引入性

让 RyuPy 代码可以导入依赖 C / C++ / Rust 等原生实现的 Python 模块及其包装层。复用 Phase 3 的 libpython 宿主，先支持明确版本的 NumPy，再按项目依赖扩大范围。模块系统负责路由到桥接宿主，避免把整个项目交回 CPython 执行。

首批覆盖 `import numpy as np`、from 导入、子模块、传递依赖与重复导入；模块对象、ndarray、dtype 和标量能够访问，创建数组、索引修改、基础运算与 linalg 调用通过。再用 sqlite3 / zlib 等扩展验证加载机制能够复用，列出其它第三方扩展的独立结果。

每个组合记录 Python、模块与 wheel 版本、系统、架构、ABI 和系统动态库依赖。缺失依赖、初始化失败和 ABI 不匹配要明确报错；引用所有权、GIL、回调、异常与关闭必须通过测试。导入成功只证明可引入性，完整 API 兼容另按清单判断。部署问题参考 [NumPy 导入与 ABI 排错文档](https://numpy.org/doc/stable/user/troubleshooting-importerror.html)。

零拷贝、Buffer Protocol、SIMD、DLPack 和 GPU 属于可引入之后的优化，不作为本阶段门槛。后续借用数组内存仍需证明布局与生命周期，ref struct 视图不能入堆或跨 yield。

## Phase 5：兼容性清单与显式记录的偏差

清单从 M2.0 就开始维护，Phase 5 负责发布验收，避免最后才发现范围不清。

交付每个项目的版本、入口、测试集、依赖、原生或桥接路径、平台、通过结果和阻塞原因；每个标准库 API 的支持状态；每个偏差的最小示例、影响、替代写法和测试。refcount、析构时机、弱引用回调时机等差异长期保留。

本阶段不承担“补齐所有长尾”的承诺。元类、多重继承、自定义 import hook、`eval/exec`、完整 frame / tracing API 只有在项目证据支持时才新增独立里程碑。

Native AOT 工具链按 ADR-0005 的桩项目方案落地，分别验收原生与桥接产物。无 libpython 的产物可以免装 Python；桥接产物仍依赖对应解释器、标准库和扩展。AOT 构建需要 SDK 与目标平台工具链，不承诺任意平台交叉发布或固定二进制大小。

[ADR0046](adr/0046-native-aot-artifact-manifest.md)的同次SDK／生成引用／产物哈希清单与三平台完整目录搬迁已有限验收。[ADR0047](adr/0047-explicit-aot-resources.md)已完成显式资源选择、真实模块位置与三平台 `_internal/resources/v1` 整树搬迁。原生依赖绑定/自动收集仍是后续独立切，不由清单存在推断动态闭包已齐。遵用户静态优先方向，必要外置依赖集中 `_internal`，不为单文件形式强加自解包。已有无资源案例的[仅exe搬迁验证](validation/aot-single-executable/README.md)通过三平台；资源bundle、任意项目onefile与Mac/Windows无SDK干净机仍单列，不由此冒称完成。

## 前瞻优化

优化贯穿各阶段，排在正确性和项目阻塞之后。设计时留下快路径与退路，先测量再启用，详见 [optimization.md](optimization.md)。

| 时点 | 优化 | 正确性条件 |
| --- | --- | --- |
| Phase 2 | 标量拆箱、有限大小 PIC、对象 shape 与类型版本、模块槽位 | 缓存失效后回到通用分派；整数溢出升级；保留重绑定与对象身份 |
| Phase 3 | 流敏感收窄、带守卫的函数特化、临时元组标量替换、迭代器融合 | 别名、副作用、异常顺序、暂停点和可观察身份均受检查 |
| Phase 4 之后按需 | 批量桥接、Buffer Protocol、布局专门化、SIMD、边界检查合并 | 模块已能导入并调用；布局、所有权和边界证明成立，保留一般路径 |
| Phase 5 | 启动、构建、体积和长期运行内存优化 | 分开记录 JIT 冷启动、预热稳态与 AOT 数据；缓存有容量上限 |

性能数据必须记录硬件、工具链、输入、重复次数、内存与分配量。取消原规划中未经验证的固定倍数、固定体积和自动向量化承诺。


## M3.10 的 JIT 优先性能方向与有界交付（2026-11-01）

用户明确要求：生成的 CIL 应尽量直接暴露类型、运算、循环与分配给 .NET JIT，
而不是把 C# 通用动态执行层作为最终热点架构。M3.9 保留早期探索，M3.10 在原项目
兼容性验收之外，积极推进受守卫类型特化、逃逸/标量替换、容器特化与自动 SIMD 机会。
不因缺少函数标注或一个动态值就默认永久牺牲整函数的可优化局部区域。

通用运行时必须继续保留为正确性回退；类型变化、大整数溢出、类/容器布局失效、身份和
别名可观察、异常及暂停点均有明确恢复/物化路径。默认不擅自改变常用结果或浮点次序。
实现收益以生成 CIL 和目标 JIT 机器码、每次调用/迭代分配与装箱、特化命中率、代码体积、
冷启动和稳态吞吐共同验证。某种 JIT/ISA 未向量化时须如实报告，不把“写了循环”当作 SIMD。

上述原验收已按[有界实现报告](notes/2026-11-01-m3.10-validation.md)闭环：短F64链、
严格内层原生槽、身份安全16B数值list岛与同pair证明下重复逻辑检查消除有限默认启用。
真正两lane packed SLP已实现、验过ISA回退/位模式与实际机器码，但同guard scalar对照无净收益，
故仅保显式实验/诊断开关、默认关闭。不是通用union ABI、一般BCE/LICM或默认普遍SIMD完成。
最终门禁434 C#、225固定基线、199编译e2e/30 ignored，26项104冷进程覆盖全部通过；
单样本冷覆盖不等于稳态统计结论；25项冷main几何耗时退化9.54%/8.15%，
不宣称整体性能净赢。M3.11固定项目仍5/20，至少16/20入口及上游范围目标不减少。
