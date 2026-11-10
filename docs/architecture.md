# RyuPy 架构设计

> 本文描述**当前代码的实际分层**以及每层的职责边界。愿景与阶段规划见 [roadmap.md](roadmap.md)；
> 关键取舍见 [adr/](adr/)；Python 语义细节见 [semantics.md](semantics.md)。

## 1. 总览

```text
            ┌──────────── 前端 ────────────┐ ┌──── 中端 ────┐ ┌──── 后端 ────┐
 .py ──► [syntax] ──► [hir] ──► [ty] ──────► [mir] ──passes──► [codegen-cil] ──► .dll ──► dotnet / ILC
          AST         HIR      HIR+类型旁表     MIR                CIL              │
                                                                                  ▼
                                                                    RyuPy.Runtime.dll (C#)
```

每一层只做一件事，并且**只向下一层输出数据结构**，不调用下一层的函数。把各层串起来的是 `ryupy-driver`。

## 2. Crate 分层与依赖规则

```text
ryupy-cli ─► ryupy-driver ─┬─► ryupy-codegen-cil ─► ryupy-mir ─┬─► ryupy-ty ─► ryupy-hir ─► ryupy-syntax
                           │                                    └──────────────►┘              │
                           └─► （各阶段）                                                       ▼
                                                                                    全部 ─► ryupy-base

ryupy-e2e（仅测试）─► ryupy-driver
```

**依赖规则（违反即视为架构错误）：**

1. 依赖只能**自上而下**，不允许循环，也不允许「跳层回调」。
2. `ryupy-base` 不依赖任何 `ryupy-*` crate。
3. **只有 `ryupy-syntax` 直接依赖 ruff 解析器**；其它 crate 通过 `ryupy_syntax::ast` 使用 AST 类型（ADR-0001）。
4. **`ryupy-codegen-cil` 只依赖 `ryupy-mir`**，不得引用 HIR / 类型 crate。后端不理解 Python。
5. **只有 `ryupy-codegen-cil` 依赖 `dotnetdll`**。
6. 只有 `ryupy-driver` 做文件 IO、启动子进程；各阶段 crate 是纯函数式的「数据进、数据出」。

| Crate | 输入 → 输出 | 负责 | 不负责 |
| --- | --- | --- | --- |
| `ryupy-base` | — | `SourceDb`、`Span`、`Diagnostic` 与渲染 | 任何 IR |
| `ryupy-syntax` | 源码 → `ParsedModule` + 语法诊断 | 封装 ruff，统一错误格式 | 语义检查 |
| `ryupy-hir` | AST → `hir::Module` | 名称解析、去糖、拒绝不支持的语法 | 类型 |
| `ryupy-ty` | HIR → `ModuleTypes`（旁表） | 函数签名、局部推导、类型错误 | 改写 HIR |
| `ryupy-mir` | HIR + 类型 → `mir::Program` | 控制流图、Python 语义改写、优化 Pass、合法性验证 | CIL 细节 |
| `ryupy-codegen-cil` | MIR → PE 字节 | 类型映射、栈化、程序集组装 | Python 语义 |
| `ryupy-driver` | 文件路径 → 产物 | 流水线编排、诊断汇总、产物布局、调用 dotnet | 编译逻辑 |
| `ryupy-cli` | 命令行参数 | 参数解析、打印 | 一切逻辑 |
| `ryupy-e2e` | `tests/e2e/**/*.py` | CPython 差分测试 | — |
| `RyuPy.Runtime` (C#) | — | 内建函数、Python 数值语义、动态值、容器、普通对象 / 类协议、异常、模块与标准库适配 | 编译期名称解析与类型推导 |

## 3. 各层 IR 设计要点

### 3.1 AST（ruff）

直接使用 `ruff_python_ast`。解析采用容错模式（`parse_unchecked_source`），即使有语法错误也返回 AST，便于一次报出多个错误。

### 3.2 HIR —— 「名字已解析的 Python」

- **rust-analyzer 风格 arena**：每个函数一个 `Body { locals, exprs, stmts }`，节点用 `Idx<T>` 互相引用，没有生命周期参数。
- 每个节点自带 `TextRange`，诊断随时可定位源码。
- `lower.rs` 编排降级，语句与函数体分别放在 `lower_stmts.rs` / `lower_fn.rs`：`+=`、`elif`、链式比较、`for … in range(…)`、多元 `and/or`。
- 顶层语句收拢为隐式函数 `Module::main`，最终成为 CIL 的 `Main`。
- 遇到不支持的语法：报诊断 + `ExprKind::Missing`，**绝不 panic**。

- M3.0 先由 `closures_walk.rs` 建立函数作用域树，再由 `closures.rs` 沿父链解析 free / cell。
  `closures_decl.rs` 检查声明冲突；默认值属于定义位置的外层作用域，函数体单独降级。
- M3.1 的 list / dict / set 推导式由 `comprehensions.rs` 去糖为独立嵌套函数，复用现有 HIR 节点与共享 cell；
  最左 iterable 在外层取得迭代器，以隐藏位置参数传入。`comprehension_ast.rs` 提供只读公共视图；
  dict 的 key 单独保存临时量保证先于 value 求值；生成器表达式生成 Yield 而非容器插入，
  标记隐式函数为生成器并复用 M3.2 状态机，最左 iterable 的立即求值边界不变。

- M3.2 由 `generator_scan.rs` 判定含不可达 yield 的当前函数，排除嵌套作用域；生成器强制 Dynamic。
  MIR `generators.rs` 将 Suspend 转工厂与恢复 CFG，显式保存全部局部到每实例 object[]；
  CIL 仅绑定静态恢复委托，`PyGenerator` 管理运行 / 终止、重入与每次恢复的 traceback。
  `generator_exceptions.rs` 将原Region变为逻辑handler表，恢复函数只有单outer catch；
  handler保存locals/异常/PC后返回内部Retry，正常yield/return以leave经过保护区外出口，不跳入CLR try中部。
  无异常区域的生成器不增加outer catch；有区域每块更新active_block，异常路径才额外spill与线性匹配。
  frame槽0为PC、1为send、2为注入异常、3为逻辑handler异常，locals从4开始；
  委托Step由生成器降级补frame参数，运行时处理协议不持frame，CIL不理解Python委托语义。
  PC -1 的恢复返回值是终值；
  普通迭代立即丢弃，仅首次完成的 next 将其放入 StopIteration，运行时不缓存终值。
  Suspend 携可选接收局部，
  恢复目标先取输入再清控制槽引用。表达式求值的已有临时量同样跨暂停保存。
  参数含暂停的无星号调用以 HIR PreparedCall 标记，MIR先固定callee / PrepareMethod结果再求args；
  运行时仅为这些调用创建内建方法绑定包装，普通CallMethod的PIC路径不额外分配。
  `container_literals.rs`独立构建容器：star tuple最终冻结，star set保留前缀MakeSet和后续SetAdd/SetExtend顺序；
  运行时SetExtend流式交错迭代与插入，不能用预物化list替代。

### 3.3 类型（旁表，不改 HIR）

- `ModuleTypes { sigs: FuncId → FnSig, bodies: FuncId → TypeckResults }`，`TypeckResults` 记录每个 `ExprId` / `LocalId` 的 `Ty`。
- Phase 1：签名必须完整标注；函数体内用并查集单态推导；`Ty::Error` 作为毒化类型抑制连锁报错。
- M2.1 起：每个函数有一个 `FuncMode`。静态检查能证明就保持 `Static`（i64/f64 直算），
  遇到缺标注、异构赋值、多返回类型、调用点实参不符或超出 i64 的字面量就**整函数**降级为
  `Dynamic`；降级单调、用不动点迭代收敛。标注只提供优化线索，不再新增 Python 没有的实参拒绝。

### 3.4 MIR —— 「定型的控制流图」

- 非 SSA 的「局部变量 + 基本块 + terminator」形式（ADR-0002），与 CIL 的局部变量槽模型几乎一一对应。
- 调用降级按职责拆分：`call_builder.rs` 处理用户调用绑定、方法与构造器；
  `call_builtins.rs` 处理内建分派和数值转换，二者仍只依赖 HIR / 类型与 MIR。
- 进入 MIR 后：所有局部变量类型确定；运算已区分 i64/f64；`//` `%` `**` `print` 等 Python 语义已改写为 `RuntimeFn` 调用；隐式类型提升已显式化为 `Cast`。
- 动态函数（M2.1）：局部变量与参数是 `MirType::Dynamic`；字面量先 `Box`，运算走
  `DynBinary` / `DynCompare` / `DynUnary` / `DynTruthy`；动态值进静态槽位时插 `Unbox`（`PyCast`）。
  `range` 在动态函数里仍是计数循环，边界拆箱成 i64。
- `validate.rs` 是 MIR 的「类型系统」：codegen 之前必跑，每个流水线 Pass 后也验证，非法 IR 返回诊断而不是 panic。
- 函数局部优化以 `MirPass` 为单位；跨函数叶摘要使用显式 `ProgramPass`，由 `PipelinePass` 区分调度。每个 Pass 独立、可单独快照测试，不在通用运行器中隐式追加变换。
- M3.9 默认流水线保留 holder 累加器，随后对新鲜非逃逸 tuple 做同块标量替换，再清理失用纯值。
  临时 tuple 最多 8 元素、每函数总尝试 32 次，元素在原构造点冻结；逃逸 / 身份观察 / EH / 暂停保留对象。
  `exact_types.rs` 提供有限前向精确盒类型事实，验证实验直接拆箱的守卫；不把 bool 当 boxed Int64。
  直接局部累加仅由显式编译会话 `--experimental-direct-accumulator` 选择，因配对吞吐未达默认门槛而关闭。
  Python 快慢路与溢出判断仍在 MIR，后端仅发 CLR 精确类型 / 拆箱 / 回绕加法原语（ADR-0033 / 0034）。


- 自动普通动态模块的ForEach累加实验由Session显式选择，默认关闭。新独立matcher在
  SimplifyCfg后、首次DeadValues前验证通用迭代四角色与唯一返回，复用既有holder ABI。
  build/dump共用实际automatic/InstanceGlobals计划守卫，不扩P1族、不猜range名称，
  不修改旧五块ForRange matcher；frame观察边界和重建验收隔离见[ADR-0135](adr/0135-foreach-local-accumulator.md)。

- M3.0 用 `MakeFunction` 显式携带环境 cell 与默认值；环境作为隐藏尾参数，Python 参数元数据
  不包含它。`CellNew` / `CellGet` / `CellGetFree` / `CellSet` 统一读写共享绑定。

### 3.5 CIL

- 生成的程序集：一个静态类 `RyuPy.Generated.Module` 承载所有模块的函数、模块级静态槽位、类工厂与异常工厂；`Main` 只负责初始化全局状态并按导入机制执行 `__main__`。用户异常类生成 CLR 类型（多模块合并时加模块前缀）；M3.6 默认 dataclass 与普通 class 均由运行时 `PyClass` / `PyInstance` 承载。旧静态 dataclass 代码路径保留但不作为兼容默认。
- 每个模块生成 `__mod_get_<i>` / `__mod_set_<i>` 与初始化入口，由 `PyModules` 注册；模块对象通过委托读写静态槽位，循环导入、缓存与失败重试由运行时处理。
- 引用的外部程序集：`System.Runtime`（基础类型）、`RyuPy.Runtime`（内建函数与语义辅助）。
- 整数算术用 `add.ovf` 等带溢出检查的指令（ADR-0004）；动态路径改用 `PyOps`，整数按需升级 BigInteger。
- `MirType::Dynamic` 映射到 `System.Object`；装箱用 `box`，拆箱走 `PyCast`（ADR-0010）。

闭包的 `PyCall` 委托首先接收函数对象自身；thunk 从中取默认值与环境。非捕获函数保持 null
环境，静态可证明的顶层调用继续直调。运行时引用、入口包装与字面量池分别放在 `refs.rs`、
`entry.rs`、`literals.rs`；操作数和右值栈化分别放在 `stackify_operands.rs`、`stackify_rvalue.rs`。

## 4. 诊断

- 各阶段返回 `Vec<Diagnostic>`，driver 汇总到 `Session`，**在阶段边界**检查是否有错误决定是否继续。
- 渲染统一走 `ryupy_base::diagnostic::render`（ariadne，按**字节**偏移）。测试中关闭颜色以便快照。
- 错误码分段：

| 范围 | 阶段 |
| --- | --- |
| `E00xx` | 语法（syntax） |
| `E01xx` | 名称解析 / 不支持的语法（hir） |
| `E02xx` | 类型（ty） |
| `E03xx` | MIR 构建期发现的语义错误 |
| 内部错误 | 不分配错误码，用 `anyhow` 报出并视为编译器 bug |

## 5. 产物布局

```text
<源目录>/__pycache__/<name>/  # JIT 默认输出目录（与 CPython 的 .pyc 同位）
├── <name>.dll                # 生成的程序集
├── <name>.runtimeconfig.json # 让 `dotnet <name>.dll` 找到共享框架
└── RyuPy.Runtime.dll         # 从 runtime/…/bin/Release/net10.0/ 复制

<源目录>/<name>.build/        # 高层 AOT 独占 attempt、托管输入与 SDK 中间产物
<源目录>/<name>.dist/         # AOT 交付：<name>（Windows .exe）、所需文件与清单

target/e2e/<case>/            # e2e 测试的产物，便于失败后手工检查

releases/                     # make build 的发行包（宿主平台，不进版本库）
├── ryupy-<版本>-<平台>/       # bin/ryupy + runtime/RyuPy.Runtime.dll
└── ryupy-<版本>-<平台>.tar.gz
```

AOT 的显式 `--out-dir ROOT` 为 build/dist 的共同根，JIT 显式输出不变。所有权、失败恢复及新基准后端边界见 [ADR-0052](adr/0052-aot-build-dist-and-bench.md)。

## 6. 测试策略

| 层次 | 工具 | 测什么 | 位置 |
| --- | --- | --- | --- |
| 单元测试 | `#[test]` | 单个函数的边界情况（并查集、运算规则表…） | 各 crate 的 `#[cfg(test)]` |
| 快照测试 | `insta` | 每层 IR 的文本转储、诊断文本 | 各 crate 的 `tests/` 或 `#[cfg(test)]` |
| 端到端差分 | `libtest-mimic` | 同一 `.py` 在 CPython 与 RyuPy 下 stdout 一致 | `tests/e2e/**/*.py`，由 `ryupy-e2e` 驱动 |
| 目录用例差分 | `libtest-mimic` + `ryupy-e2e` 库 | 入口 / 参数 / 环境 / stdio / 工作目录，以及退出码、异常类型、消息、cause | `tests/cases/**/case.txt` |
| 项目级验收 | `ryupy-e2e` 项目 runner | 编译并运行固定项目入口，对照 CPython 基线 | `tests/projects/`，结果写 JSONL |
| 运行时单测 | xUnit | `PyMath`、`PyFormat` 等 C# 语义函数 | `runtime/RyuPy.Runtime.Tests` |

常用语义以固定版本 CPython 为差分基准。成功用例比较 stdout 与成功退出；目录用例另外比较参数、
环境、stdio、工作目录与错误路径（退出码、异常类型、消息、cause），清单本身由 CPython 先行校验。
已接受偏差另外测试，不混进一致性通过率。

项目级验收固定项目版本、传递依赖、入口与上游测试范围，原生与桥接分别记录；标准库按 API 登记。
细节见 [compatibility.md](compatibility.md) 与 [stdlib.md](stdlib.md)。
测试框架的用例模型与解析在 `ryupy-e2e/src/lib.rs`，结果状态和 JSONL 序列化独立在
`results.rs`，由库根重导出既有公共接口，不改变统计口径。

## 7. Phase 2 / Phase 3 的设计边界（待实现）

以下冻结模块与桥接的职责边界；模块部分已在 M2.7 落地，桥接仍待实现。保持 §2 的依赖规则，不为模块和桥接增加反向依赖。

| 层 / 模块 | 新职责 | 边界 |
| --- | --- | --- |
| driver | 读取多文件、搜索与构建模块图、分配源码身份、组织产物和依赖报告 | 不在编译期执行 Python 模块副作用；嵌入解释器不进入前端 crate |
| HIR | 模块 / 全局 / 局部 / cell 名称分类，动态调用与异常语法降级 | 接收已加载模块，不读文件；不依赖 C# 运行时布局 |
| ty | 静态推导、Dynamic 合流、调用与容器类型事实 | 标注不自动变成运行时强制限制 |
| MIR | 动态边界、模块初始化、对象操作、异常区域与清理边、闭包和生成器状态机 | 语义操作与 RuntimeFn 完全显式化；验证处理异常展开和暂停后的状态 |
| codegen-cil | 发射 MIR 定义的类型、函数、模块槽位、异常表与运行时引用 | 不读 HIR / ty；不决定 Python 属性查找与导入语义 |
| C# 语言运行时 | PyValue、数值、容器、对象协议、异常、模块缓存、带版本的分派缓存 | 原生路径不依赖 Reflection.Emit；CLR 引用字段不得与数值字段重叠 |
| C# 标准库适配 | Python API 到 .NET 系统能力的语义包装 | 按 API 分模块，逐项记录异常、编码、路径、参数与平台差异 |
| 可选 libpython 宿主 | 解释器初始化、GIL、引用所有权、对象 / 异常 / 回调转换 | 与原生运行时隔离；跨堆引用有释放策略，桥接依赖进入部署清单 |

模块具有稳定身份与初始化状态：未初始化、初始化中、已完成；执行前登记模块对象，失败时清除失败模块的缓存条目，已成功导入依赖保留。依赖图用于发现和编译可达源码，运行时仍按 Python 求值顺序触发初始化，支持循环导入的部分可见状态。`import` / `from` 的绑定与缓存规则参考 [Python 导入系统](https://docs.python.org/3.12/reference/import.html)。

异常优先使用 CIL 原生异常区域。MIR 先定义处理区、展开目标与清理顺序，codegen 验证受保护区域之间的合法跳转；生成器将暂停中的处理区状态保存到状态机。完整 traceback 对象与解释器 frame 内部接口不属于首批范围。

`with` 在 HIR 去糖成已有的 `try` / `except BaseException` / `finally`（见 [semantics.md](semantics.md) §5.1），MIR 不再增加专用节点：异常区域、`return` / `break` / `continue` 的清理边与 `__exit__` 调用复用同一套 `finally` 状态机。`handled` 局部变量保证每次进入只调用一次 `__exit__`。

运行时 ABI 统一管理 RuntimeFn 签名、动态值布局、类型 / shape 版本、模块槽位和桥接句柄。新增 RuntimeFn 时同步修改 C# 实现与 codegen 引用表。PIC / 特化缓存有容量上限；属性、方法、继承链与模块绑定变化后旧条目失效，失败走通用路径。M2.9 的 PIC 由 codegen 分配调用点编号，`PyPic` 只缓存整数 / 浮点 / 字符串运算形状与普通类方法绑定；类全局版本、实例属性版本变化即失效，模块属性不缓存，见 [ADR-0012](adr/0012-bounded-dynamic-pic.md)。

普通对象与 frozen dataclass 均保留引用身份；M3.6 默认统一使用 PyInstance，旧 Phase 1 值类型降级不再作为当前实现。非逃逸且身份不可观察时可以消除分配；`ref struct` 数组视图不能入堆或跨 yield，动态数组值使用有所有权的堆句柄。

原生路径继续满足 AOT 分析约束，但真正发布与裁剪测试安排在 M5.2。动态导入通过构建时可达清单限定；桥接产物仍需 libpython 与扩展。Native AOT 对运行时程序集加载和代码生成的限制见 [Microsoft 文档](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)。

## 8. 保留的架构决定

保留实际九个 crate 的分层、非 SSA MIR（ADR-0002）、GC 合法的 PyValue 候选布局（ADR-0003）、子进程 `dotnet x.dll` 运行方式与按需引入依赖。Phase 3 有数据流分析需求后再判断是否增加 SSA 视图或图算法库，不照旧愿景提前引入完整优化框架。

## 实际代码来源候选（ADR0145）

MIR单一CodeSource表记录真实unit入口与私有signature归属，link按ID重映射，CIL冷关联不可变Runtime载体。真实源码driver使用required入口，旧手工MIR emit保兼容；fixed-binding豁免仅限实际已验证新增initializer。warnings环境与TypedDict函数Module能力分开，不按basename/注册名补来源。合同、有限前言门与热成本见[ADR0145](adr/0145-compiled-source-provenance.md)，当前产品未验收。

ADR0146候选只在driver完成硬图后作JIT可选空资源子包发现；AOT图不变。新Legacy冷source V1消费已有MIR关联证明授予词法归属，不新增owner或热调用ABI；旧host仍原合同。真实产品及完整门待验，见[ADR0146](adr/0146-empty-resource-package-discovery.md)。
