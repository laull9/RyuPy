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
| [0028](0028-system-streams-paths.md) | 系统、流与路径 API 的有限原生实现 | 已接受 |
| [0029](0029-numeric-json-regex.md) | 数值、JSON 与正则 API 的有限原生实现 | 已接受 |
| [0030](0030-tools-metadata-guarded-dataclasses.md) | 工具协议、元数据与守卫数据类优化 | 已接受 |
| [0031](0031-project-tools.md) | 项目入口工具与协议的有限原生实现 | 已接受 |
| [0032](0032-bounded-stdlib-and-compatibility-gate.md) | 有界标准库交付与独立项目兼容性门（用户批准重排） | 已接受 |
| [0033](0033-direct-cil-guarded-accumulator.md) | 受守卫直接 CIL 累加槽 | 已验证；默认关闭、显式实验 |
| [0034](0034-fresh-tuple-scalar-replacement.md) | 新鲜临时 tuple 的有界标量替换 | 已接受，限定规则默认启用 |
| [0035](0035-guarded-leaf-call-specialization.md) | 有限纯叶调用的守卫特化 | 已验证，默认关闭、显式实验 |
| [0036](0036-bounded-interval-unboxing.md) | 非 SSA 有限值域证明与自动拆箱 | 有界默认、验收通过 |
| [0037](0037-guarded-float-chain-scalarization.md) | 受守卫短浮点算术链的中间盒消除 | 有限默认，聚合门禁通过 |
| [0038](0038-inner-float-loop-materialization.md) | 严格内层浮点累加与出口物化 | 有限默认，聚合门禁通过 |
| [0039](0039-adaptive-numeric-list-island.md) | 自适应数值列表岛与身份安全物化 | 有限默认，三臂及门禁验收通过 |
| [0040](0040-guarded-two-lane-float-slp.md) | 受守卫两lane独立浮点SLP | 已验证，无净收益，显式实验默认关 |
| [0041](0041-proven-list-read-bounds.md) | 支配证明下省去重复列表逻辑范围检查 | 源码、实际JIT与聚合门禁通过 |
| [0042](0042-aot-jit-recovery-priority.md) | 恢复后优先AOT/JIT与小步远端保存 | 用户已批准方向 |
| [0043](0043-aot-delivery-mainline.md) | AOT持续性能与交付主线 | 分阶段实施中 |
| [0044](0044-guarded-integer-add-chain.md) | actual-I64短Add链消除中间盒 | 有限默认，配对及组合门禁通过 |
| [0045](0045-native-module-attribute-factories.md) | 13项原生模块属性工厂延迟构造 | 固定配对未达采用门槛，生产已回退 |
| [0046](0046-native-aot-artifact-manifest.md) | 同次AOT发布清单与实际生成引用报告 | 实现接线，待完整门禁与三平台验收 |

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
