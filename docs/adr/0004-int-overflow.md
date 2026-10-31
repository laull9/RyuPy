# ADR-0004：`int` 溢出策略

- 状态：Phase 1 已接受；后续升级排期已被 [ADR-0009](0009-project-compatibility-first.md) 取代，提前至 M2.1；以下正文保留原决策，末尾「现状」记录实际落地方式
- 日期：2026-10-31（现状补充：2026-10-31）

## 背景

Python `int` 是任意精度的，永不溢出。Phase 1 把 `int` 映射为 `int64` 以获得原生性能。需要决定溢出时的行为。

## 方案

### A. 静默回绕（`add` / `mul`）
最快，但结果会**悄悄出错**，违反「要么一致、要么报错」的原则。

### B. 溢出即抛异常（`add.ovf` / `mul.ovf` / `sub.ovf`）
硬件标志位检查，开销通常很小（RyuJIT 会生成 `jo` 跳转）。结果要么正确，要么程序明确失败。

### C. 溢出时自动升级为 `BigInteger`
完全兼容 Python，但需要动态表示（Phase 2 的 `PyValue`），Phase 1 无法实现。

## 决定

Phase 1 选 B。Phase 5 在 B 的基础上实现 C：捕获溢出后转入慢路径。

## 后果

- `fib_iter(90)` 在 int64 范围内，可以通过；`fib_iter(100)` 会抛出异常——这是**预期行为**，应有一个 e2e 用例记录它（暂时作为「已知差异」用例）。
- 移位运算 `<<` 不受 `.ovf` 保护，需要单独处理（Phase 1 可先限制移位量 < 63，否则报错）。
- `PyMath.PowI64` 使用 C# `checked` 保持同一策略。

## 现状（M2.1 之后）

方案 C 只对**动态路径**生效：值被装箱成 `object` 时，`PyInt` 优先用 `long` 计算，溢出后升级为
`BigInteger`（见 ADR-0010）。带完整标注的静态特化路径仍是 `int64`，因为本项目的取舍是
「静态路径优先性能、动态路径优先兼容」。

静态路径的溢出处理从「抛 CLR 异常」改为「抛 Python 异常」：

| 运算 | 处理位置 | 越界时 |
| --- | --- | --- |
| `+` `-` `*` | CIL `add.ovf` / `sub.ovf` / `mul.ovf` | 入口包装器兜底 catch `System.OverflowException`，映射为 `OverflowError` |
| `//` `%` `abs` `**` | `PyMath.FloorDivI64` / `ModI64` / `AbsI64` / `PowI64` | 显式抛 `PyOverflowError` |
| `<<` `>>` | `PyMath.ShiftLeftI64` / `ShiftRightI64` | 左移越界抛 `PyOverflowError`；右移按 Python 语义处理移位量 ≥ 64 |
| `int(float)` | `PyMath.F64ToI64` | NaN → `ValueError`；Inf / 越界 → `OverflowError` |

错误消息统一注明 `RyuPy static integer limit`，明确这是实现上限而不是 Python 语义；该偏差登记为
compatibility.md 的 D003。入口包装器的兜底 catch 同时保证任何意外泄漏的 CLR 异常都不会打印
"Unhandled exception" 与 .NET 调用栈。

`PowI64` 只接受非负指数：`int ** int` 的负指数在 Python 里返回 `float`，类型层会让这类表达式
（除非指数是确定的非负字面量）降级到动态路径，由 `PyInt.Power` 处理。
