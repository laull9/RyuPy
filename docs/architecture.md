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
| `RyuPy.Runtime` (C#) | — | 内建函数、Python 数值语义、（Phase 2+）`PyValue`、容器、对象模型 | — |

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
- Phase 2：推不出来的落到 `Ty::Dynamic`（运行时 `PyValue`）。

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
└── aot/                      # 仅 --aot：桩项目与原生可执行文件（ADR-0005）

target/e2e/<case>/            # e2e 测试的产物，便于失败后手工检查
```

## 6. 测试策略

| 层次 | 工具 | 测什么 | 位置 |
| --- | --- | --- | --- |
| 单元测试 | `#[test]` | 单个函数的边界情况（并查集、运算规则表…） | 各 crate 的 `#[cfg(test)]` |
| 快照测试 | `insta` | 每层 IR 的文本转储、诊断文本 | 各 crate 的 `tests/` 或 `#[cfg(test)]` |
| 端到端差分 | `libtest-mimic` | 同一 `.py` 在 CPython 与 RyuPy 下 stdout 一致 | `tests/e2e/**/*.py`，由 `ryupy-e2e` 驱动 |
| 运行时单测 | （待建）xUnit | `PyMath`、`PyFormat` 等 C# 语义函数 | `runtime/RyuPy.Runtime.Tests` |

**以 CPython 为唯一真相**：e2e 不手写期望输出，而是现场运行 `python3`。这样语义差异会被自动发现，而不是被错误的期望值掩盖。

## 7. 相对 roadmap 的修订

roadmap 是愿景文档，落地时做了以下调整（详见对应 ADR）：

| roadmap 原设计 | 现设计 | 原因 |
| --- | --- | --- |
| 5 个编译器 crate | 增加 `ryupy-base`、`ryupy-syntax`、`ryupy-driver`、`ryupy-e2e` | 隔离 ruff 依赖；编排逻辑不该在 CLI；测试需要 python+dotnet，单独成 crate |
| MIR 为 SSA | 非 SSA 的 locals + blocks | 与 CIL 模型匹配，省掉 φ 消解（ADR-0002） |
| `PyValue` 用 `FieldOffset(8)` 重叠 `long` 与 `object` | 不可行，改为独立引用字段 | CLR 禁止引用字段与非引用字段重叠（ADR-0003） |
| 先用 `netcorehost` 进程内运行 | 先用子进程 `dotnet x.dll` | 简单可靠，够用再优化 |
| Day 1 引入全部依赖 | 按需引入 | `petgraph`、`egglog`、`pyo3-ffi` 等到对应阶段再加 |
