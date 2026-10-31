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
| `RyuPy.Runtime` (C#) | — | 内建函数、Python 数值语义；计划增加动态值、容器、对象、异常、模块与标准库适配 | 编译期名称解析与类型推导 |

## 3. 各层 IR 设计要点

### 3.1 AST（ruff）

直接使用 `ruff_python_ast`。解析采用容错模式（`parse_unchecked_source`），即使有语法错误也返回 AST，便于一次报出多个错误。

### 3.2 HIR —— 「名字已解析的 Python」

- **rust-analyzer 风格 arena**：每个函数一个 `Body { locals, exprs, stmts }`，节点用 `Idx<T>` 互相引用，没有生命周期参数。
- 每个节点自带 `TextRange`，诊断随时可定位源码。
- 去糖规则集中在 `lower.rs`：`+=`、`elif`、链式比较、`for … in range(…)`、多元 `and/or`。
- 顶层语句收拢为隐式函数 `Module::main`，最终成为 CIL 的 `Main`。
- 遇到不支持的语法：报诊断 + `ExprKind::Missing`，**绝不 panic**。

### 3.3 类型（旁表，不改 HIR）

- `ModuleTypes { sigs: FuncId → FnSig, bodies: FuncId → TypeckResults }`，`TypeckResults` 记录每个 `ExprId` / `LocalId` 的 `Ty`。
- Phase 1：签名必须完整标注；函数体内用并查集单态推导；`Ty::Error` 作为毒化类型抑制连锁报错。
- Phase 2 计划：推不出来的落到 `Ty::Dynamic`（运行时 `PyValue`）；类型标注只提供优化线索，特化须有证明或守卫，失败返回通用路径，不新增 Python 没有的参数类型拒绝。

### 3.4 MIR —— 「定型的控制流图」

- 非 SSA 的「局部变量 + 基本块 + terminator」形式（ADR-0002），与 CIL 的局部变量槽模型几乎一一对应。
- 进入 MIR 后：所有局部变量类型确定；运算已区分 i64/f64；`//` `%` `**` `print` 等 Python 语义已改写为 `RuntimeFn` 调用；隐式类型提升已显式化为 `Cast`。
- `validate.rs` 是 MIR 的「类型系统」：codegen 之前必跑，Pass 之后 debug 构建下自动跑。
- 优化以 `MirPass` 为单位，每个 Pass 独立、可单独快照测试。

### 3.5 CIL

- 生成的程序集：一个静态类 `RyuPy.Generated.Module` 承载所有顶层函数（含 `Main`），每个 dataclass 一个类型。
- 引用的外部程序集：`System.Runtime`（基础类型）、`RyuPy.Runtime`（内建函数与语义辅助）。
- 整数算术用 `add.ovf` 等带溢出检查的指令（ADR-0004）。

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
target/ryupy/<name>/          # ryupy build 默认输出目录
├── <name>.dll                # 生成的程序集
├── <name>.runtimeconfig.json # 让 `dotnet <name>.dll` 找到共享框架
├── RyuPy.Runtime.dll         # 从 runtime/…/bin/Release/net10.0/ 复制
└── aot/                      # 计划于 M5.2 实现：桩项目与原生可执行文件（ADR-0005）

target/e2e/<case>/            # e2e 测试的产物，便于失败后手工检查
```

## 6. 测试策略

| 层次 | 工具 | 测什么 | 位置 |
| --- | --- | --- | --- |
| 单元测试 | `#[test]` | 单个函数的边界情况（并查集、运算规则表…） | 各 crate 的 `#[cfg(test)]` |
| 快照测试 | `insta` | 每层 IR 的文本转储、诊断文本 | 各 crate 的 `tests/` 或 `#[cfg(test)]` |
| 端到端差分 | `libtest-mimic` | 同一 `.py` 在 CPython 与 RyuPy 下 stdout 一致 | `tests/e2e/**/*.py`，由 `ryupy-e2e` 驱动 |
| 运行时单测 | xUnit | `PyMath`、`PyFormat` 等 C# 语义函数 | `runtime/RyuPy.Runtime.Tests` |

常用语义以固定版本 CPython 为差分基准。现有 e2e 比较成功用例的 stdout 与成功退出；M2.0 增加多文件、错误路径、异常类型和副作用的比较。已接受偏差另外测试，不混进一致性通过率。

项目级验收固定项目版本、传递依赖、入口与上游测试范围，原生与桥接分别记录；标准库按 API 登记。细节见 [compatibility.md](compatibility.md) 与 [stdlib.md](stdlib.md)。这些新增测试入口尚待实现。

## 7. Phase 2 / Phase 3 的设计边界（待实现）

以下描述计划接口的职责；实施里程碑时再冻结 Rust / C# 的具体数据结构。保持 §2 的依赖规则，不为模块和桥接增加反向依赖。

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

运行时 ABI 统一管理 RuntimeFn 签名、动态值布局、类型 / shape 版本、模块槽位和桥接句柄。新增 RuntimeFn 时同步修改 C# 实现与 codegen 引用表。PIC / 特化缓存有容量上限；属性、方法、继承链与模块绑定变化后旧条目失效，失败走通用路径。

普通对象保留引用身份，frozen dataclass 的 Phase 1 值类型降级受 ADR-0009 限定。非逃逸且身份不可观察时可以消除分配；`ref struct` 数组视图不能入堆或跨 yield，动态数组值使用有所有权的堆句柄。

原生路径继续满足 AOT 分析约束，但真正发布与裁剪测试安排在 M5.2。动态导入通过构建时可达清单限定；桥接产物仍需 libpython 与扩展。Native AOT 对运行时程序集加载和代码生成的限制见 [Microsoft 文档](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)。

## 8. 保留的架构决定

保留实际九个 crate 的分层、非 SSA MIR（ADR-0002）、GC 合法的 PyValue 候选布局（ADR-0003）、子进程 `dotnet x.dll` 运行方式与按需引入依赖。Phase 3 有数据流分析需求后再判断是否增加 SSA 视图或图算法库，不照旧愿景提前引入完整优化框架。
