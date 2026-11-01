# ADR-0010：动态值表示定为「装箱 object + 统一运算 ABI」

- 状态：已接受；取代 [ADR-0003](0003-pyvalue-layout.md) 的布局选择
- 日期：2026-10-31

> M3.10 更新（2026-11-01）：用户要求重新积极评估动态 union；历史选择与证据保留，
> 不是永久禁止值表示或数值容器。合法16B/24B布局已独立测量，下一有界生产试点见
> [ADR-0039](0039-adaptive-numeric-list-island.md)。通用 object ABI 当前仍保留，不能将试点冒称全ABI迁移。

## 背景

ADR-0003 比较了三种 `PyValue` 表示，倾向 16 字节 struct 哨兵方案（B），并要求 M2.1 用基准
数据定稿。Phase 2 的实际范围是动态核心 + 容器 + 普通对象 + 异常 + 模块：这些能力里几乎所有
动态值都会进入引用槽位——list 元素、dict 值、实例字段、异常 `args`、模块全局——而 MIR 的
`MirType::Dynamic` 同时要能承载 i64 快路径之外的一切。

先做了一次布局基准（`/tmp/pvbench`，.NET 10.0.403 Release，macOS arm64，2000 万次迭代，
预热一轮，`Stopwatch` 计时）：

| 场景 | object 装箱 | 24B struct（ADR-0003 方案 A） |
| --- | --- | --- |
| 算术累加（`acc = acc + 3`） | 41 ms | 4 ms |
| 容器读写（64 槽轮转） | 101 ms | 9 ms |

struct 在纯算术上快约 10 倍，容器访问快约 11 倍——差距来自每次 `long` 装箱的分配。

## 决定

M2.1 起 `MirType::Dynamic` 映射到 CIL `System.Object`，动态值就是「装箱的 CLR 值」：

| Python | 表示 |
| --- | --- |
| `int` | `long`，溢出后升级 `System.Numerics.BigInteger` |
| `float` | `double` |
| `bool` | `bool`（独立于 `long`，比较时显式当 0/1） |
| `str` | `string` |
| `None` | `PyNone.Instance` 单例（不用 `null`） |

运行时 ABI 收敛到三个静态类：

- `PyOps`：`Binary(opcode, a, b)`、`Compare(opcode, a, b)`、`Unary(opcode, a)`、`Truthy`、
  `ToIntValue` / `ToFloatValue` / `AbsValue`、`FromDecimalString`、`None()`；
- `PyCast`：动态值流入静态槽位时的显式拆箱（`ToInt64` / `ToFloat64` / `ToBool` / `ToStr`）；
- `PyBaseException` 家族：Python 异常类型名与消息，不泄漏 CLR 异常名。

操作码常量在 Rust（`mir::DynOp`）与 C#（`PyOps.Add` 等）两侧同步维护，修改必须成对出现。

## 理由

1. **对象模型本来就是引用**。容器、实例、模块全局都要求引用语义，struct 表示进入这些槽位仍要
   装箱，收益只在纯算术循环里成立。
2. **静态特化承担热点数值路径**。Phase 1 已把标注齐全、调用点一致的函数编译成 i64/f64 直算
   （fib 快 CPython 6.2 倍、Mandelbrot 6.1 倍），装箱成本只落在真正无法定型的代码上。
3. **ABI 面小**。三个静态类 + 一组操作码，比每种运算一个方法更容易与 codegen 引用表对齐，
   也比「值类型 + 引用类型」双表示少了大量转换规则。
4. **错误模型统一**。动态运算必须能抛出 Python 语义异常，引用类型的异常对象本来就是装箱世界
   的一部分；值类型表示会引入「异常如何引用值」的额外问题。

## 后果

- 动态算术比静态慢约 10 倍（上表）。这是可接受的：Phase 2 的目标是「能跑对」，不是性能；
  M2.9 的 PIC 与拆箱优化会在有数据支撑时收窄差距。
- 若后续项目数据显示热点确实落在动态算术上，再评估引入值类型表示（需要新的 ADR 与双表示
  转换规则），不在本阶段提前引入。
- 动态值身份：`long` / `double` / `bool` 装箱后每次都是新对象，`is` 与 `id()` 的语义必须在
  M2.5 按「值比较」而不是「引用比较」实现，并登记到 semantics.md。
- 容器与对象字段直接存 `object`，不需要额外句柄；Phase 4 的 `ref struct` 数组视图不能进容器，
  仍按原计划另设非 ref 句柄。
- 运行时测试（xUnit）覆盖算术、大整数升级、精确的 int/float 比较、NaN、真值与错误消息；
  这些测试是 ABI 的行为契约，改语义必须先改测试。
