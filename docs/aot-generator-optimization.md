# 生成器优化候选与主代理复核

2026-11-15。当前为测试候选；默认产品路径保持原全量保存/恢复。

## 已核对事实

- 生成器每实例独占一个 frame，恢复复用原数组，不是每个 yield 新建 frame。
- PC 的 I64 装箱使用 PyInt 小整数缓存，不能据此推算每步固定 24 字节分配。
- 当前保存全部局部会增加帧读写；其实际耗时、装箱贡献和保活对象需剖析。
- _running 是 bool，用于递归重入拒绝，不能据此宣布跨线程原子安全。

## 当前最窄候选

复用 MIR 完整读取器和有限 CFG 活跃性分析；不收敛或 EH/product 返回原路径。参数、命名槽、cell、非 Dynamic 槽强制保留，只对已证明不再读取的 Dynamic 编译器临时写 null。恢复端仍全量读取，动态槽不拆箱，静态槽始终保存。

主代理撤销了 RYUPY_OPT_GENERATOR_LIVENESS 环境变量接入，改为测试专用显式 lower_with_liveness_for_test。候选尚无产品配置或默认启用。

## 已执行与待执行

主代理已执行 cargo test -p ryupy-mir --lib liveness：5项通过；包含实际降级并运行 MIR 验证器。预算耗尽断言已随完整门禁通过，当前同工作区MIR为359通过、4忽略。

后续需验证 pending E2E 的 send/throw/close、yield from、回调观察、typed 临时和帧生命周期，再执行 NativeAOT 差分与 A/B。本轮未修改 C# 运行时，移除了重复且不覆盖真实编译降级的手写 C# 代理测试；实际裁剪由 MIR 降级结构测试和后续端到端差分验证。

该候选保留静态槽和命名变量，且清理操作仍需 ArraySet，不能预先承诺 generators 吞吐提升。没有应用级收益时应撤回实验，不为建立分析框架单独扩大架构。

## 原始负载的追加核实

主代理用正式Release CLI即时打印原始generators.py的Native MIR：`count_up` 的 `n`、`i` 为Dynamic，挂起时保存对象引用；不会每轮按typed I64参数重复装箱。撤回以这两个槽推算数十MiB冗余spill的归因。报告中三百万外部产出还包含yield from内部恢复，不能混用作统一resume调用次数。

已从运行时源码确认 `BuiltinSum` 通过 `ToSequence` 对迭代器调用 `DrainIterator`，先收集到List再复制数组；这是生成器表达式sum的全量物化热点，并改变迭代/加法副作用交错。下一竖切优先改为流式迭代，独立验证CPython事件顺序与真实AOT收益，再评估累加装箱优化。
