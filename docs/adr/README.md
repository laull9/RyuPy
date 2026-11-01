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
| [0014](0014-benchmark-suite-and-dynamic-hot-paths.md) | 基准体系与动态热路径第二轮优化 | 已接受 |
| [0015](0015-shared-closure-cells.md) | 共享 cell 与函数实例环境 | 已接受 |

| [0016](0016-comprehension-scopes.md) | 推导式先复用独立函数作用域 | 已接受（list 首个竖切） |

| [0017](0017-generator-frame-state-machine.md) | 普通生成器先使用全局部堆帧状态机 | 已接受 |

| [0018](0018-generator-return-lifetime.md) | 生成器终值由触发完成的操作立即消费 | 已接受 |

| [0019](0019-generator-send-input.md) | send 使用显式帧输入槽与恢复接收局部 | 已接受 |

| [0020](0020-prepared-suspending-calls.md) | 暂停调用先固定目标，保留普通调用热路径 | 已接受 |

| [0021](0021-starred-container-construction.md) | 星号容器保持最终类型与流式集合插入 | 已接受（缓存hash复制待做） |
| [0022](0022-augmented-target-freezing.md) | 增量赋值冻结目标与索引 | 已接受 |
| [0023](0023-dict-literal-evaluation-order.md) | 字典字面量求值与哈希分批 | 已接受 |
| [0024](0024-prepared-starred-arguments.md) | 暂停调用的位置星号准备 | 已接受 |
| [0025](0025-unicode-builtins.md) | 项目驱动Unicode基础内建 | 已接受（直接调用） |
| [0026](0026-generator-exception-resume.md) | 生成器逻辑异常恢复与委托清理 | 已接受 |

| [0027](0027-definition-time-decorators.md) | 定义位置装饰器与混合dataclass表示 | 已接受 |

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

| [0030](0030-tools-metadata-guarded-dataclasses.md) | 工具协议、元数据与守卫数据类优化 | 已接受 |
