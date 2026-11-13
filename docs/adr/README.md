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
| [0046](0046-native-aot-artifact-manifest.md) | 同次AOT发布清单与实际生成引用报告 | 有限三平台验收通过，资源与动态闭包未实现 |
| [0047](0047-explicit-aot-resources.md) | 显式随包数据与模块运行位置 | 三平台显式资源有限验收；自动依赖收集未实现 |
| [0048](0048-default-site-quitters.md) | 默认 site 的真实 exit / quit | 三平台专项与组合CI通过；WIP未main，非完整 site |
| [0049](0049-base-object-identity.md) | 真实 object 基础实例与身份 | 三平台有限专项通过；派生构造与富比较旧债独立保留 |
| [0050](0050-ordinary-construction-protocol.md) | 普通单继承的有限 new/init | 本地候选；专用布局与槽历史限制明确 |
| [0133](0133-ordered-dict-core.md) | 精确原生OrderedDict的双顺序与有限消费者 | 有限首切及完整门禁通过；性能风险开放，不等于远端发布 |

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
| [0142](0142-generator-trace-product-v1.md) | 显式生成器 trace 产品 V1：末端 lowering 与单一权威 | 未验收源码候选；默认关闭，完整产品/回归待执行 |

| [0145](0145-compiled-source-provenance.md) | 实际代码来源与同帧warnings/功能式TypedDict | 有限源码候选；纯Rust门完成，真实C#/JIT/Native与完整保护待验 |

| [0149](0149-string-search-windows.md) | 五个字符串搜索方法共享码点窗口 | 未编译源码候选；原槽/警告、同源JIT/AOT与项目保护待验 |
- [0155：有限JIT默认来源候选与分发环境](0155-default-source-candidates.md)（实现候选，完整门待验）
- [0158：显式普通实例直接globals冷路](0158-instance-direct-globals.md)（未验收；推导式保持拒绝）

- [0169：固定 C ABI 单库多导出](0169-cabi-multiple-exports.md)（已整合，macOS arm64 搬迁验收）
- [0170：文件系统 as_file 上下文](0170-filesystem-as-file-context.md)（已整合，CP/JIT/NativeAOT 验收）
- [0171：C ABI 与资源组合发布](0171-aot-binding-resources.md)（已整合，综合搬迁验收）
- [0172：AOT 与完整标准库 provider 路线](0172-aot-stdlib-provider-plan.md)（设计提案，待交叉评审）
- [0173：面向 NativeAOT 的声明式 C ABI 签名与只读借用](0173-declarative-cabi-signatures.md)（用户 dev 通用 C ABI v3；支持范围与验收见原 ADR）
- [0174：版本化原源 catalog 与可解释静态依赖报告](0174-versioned-source-catalog-probe.md)（用户 dev 的 catalog v2 与静态来源报告；验收范围见原 ADR）




- [0175：资源树与实际输出的早期相交诊断](0175-resource-output-preflight.md)（单一模块图内预检，保留合法兄弟资源与失败留证）

| [0177](0177-linux-native-symbol-delivery.md) | Linux Native调试符号与交付目录分离 | Linux有限验收通过；未合入dev/main |

- [0178：精确内建list的join去掉重复引用快照](0178-exact-list-join-allocation.md)（减分配候选，真实A/B两轮仅一轮过3%可靠门）

- [0179：显式NativeAOT体积/速度偏好](0179-native-aot-optimization-preference.md)（默认不变；真实SDK收据核对，优化收益按应用实测）

- [0180：普通源码 NativeAOT 直入口](0180-native-direct-source-entry.md)（独立整合分支三平台及catalog/通用ABI组合已验；未合dev）

- [0181：Enum auto 赋值期生成](0181-enum-auto-assignment.md)（有限图 JIT/Native 与完整回归已验证；不代表完整 Enum）
- [0182：固定非优化 __debug__](0182-debug-language-constant.md)（固定非优化正/负合同、有限 Native 与完整回归已验证）
- [0183：AOT standalone 同一部署计划](0183-aot-standalone-deployment-plan.md)（真实应用及独立 PE 合同已验证；原生发布/干净部署待验）
- [0184：有限标准输出 detach](0184-standard-output-detach.md)（CP/JIT/完整 C# 已验证；不扩一般 IO 或宿主句柄所有权）

- [0185：Windows extended 文件系统路径的有限准入](0185-windows-extended-filesystem-paths.md)（Windows真实宿主路径与三平台资源组合修复已验，保首轮失败）

- [0186：显式资源目录选择](0186-explicit-resource-trees.md)（Linux/macOS/Windows 两入口553资源已验；仅Linux有无运行时容器证明）

- [0187：Windows Path 有界共享词法模型](0187-windows-path-lexical-model.md)（有限词法与真实Windows路径/应用回归已验；不扩任意设备命名空间）
- [0188：离线校验已交付的AOT目录](0188-offline-distribution-verification.md)（实现及真实应用验收中；只证明内容自洽，不证明真实性/兼容性）
