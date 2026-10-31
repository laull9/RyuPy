# 架构决策记录（ADR）

每篇 ADR 记录一个**有取舍的决定**：当时的背景、考虑过的方案、最终选择及其代价。
ADR 一旦「已接受」就不再修改正文；推翻时新写一篇，并把旧的状态改为「已被 ADR-XXXX 取代」。

| 编号 | 标题 | 状态 |
| --- | --- | --- |
| [0001](0001-ruff-parser-isolation.md) | 使用 ruff 解析器，并隔离在 `ryupy-syntax` | 已接受 |
| [0002](0002-mir-non-ssa.md) | MIR 采用非 SSA 的「局部变量 + 基本块」形式 | 已接受 |
| [0003](0003-pyvalue-layout.md) | `PyValue` 的内存布局 | 已被 0010 取代 |
| [0004](0004-int-overflow.md) | `int` 溢出策略 | Phase 1 已接受；后续升级排期由 0009 修订为 M2.1 |
| [0005](0005-native-aot-packaging.md) | Native AOT 打包方式 | 提议（M5.2，承接旧 M1.6） |
| [0006](0006-dotnetdll-license.md) | dotnetdll 的 GPL 许可证影响 | 已接受 |
| [0007](0007-vendor-patch-dotnetdll-branch-and-us-heap.md) | Vendor Patch dotnetdll 分支偏移与 #US 堆缺陷 | 已接受 |
| [0008](0008-dataclass-lowering-and-value-types.md) | @dataclass 降级与 .NET 值类型映射 | Phase 1 已接受；后续值类型适用范围由 0009 限定 |
| [0009](0009-project-compatibility-first.md) | 按项目兼容性安排后续阶段 | 已接受 |
| [0010](0010-dynamic-value-representation.md) | 动态值表示：装箱 object + 统一运算 ABI | 已接受 |
| [0011](0011-release-package-layout.md) | 发行包布局与运行时定位 | 已接受 |
| [0012](0012-bounded-dynamic-pic.md) | 有界动态运算 / 调用 PIC | 已接受（性能数据由 0013 更新） |
| [0013](0013-dynamic-hot-path-allocations.md) | 动态热路径去分配，保留装箱表示 | 已接受 |

## 模板

```markdown
# ADR-XXXX：标题

- 状态：提议 / 已接受 / 已被 ADR-YYYY 取代
- 日期：YYYY-MM-DD

## 背景
要解决什么问题？有哪些约束？

## 方案
### A. ……
优点 / 缺点
### B. ……
优点 / 缺点

## 决定
选了哪个，为什么。

## 后果
带来的代价、需要后续跟进的事项、什么情况下应该重新审视。
```
