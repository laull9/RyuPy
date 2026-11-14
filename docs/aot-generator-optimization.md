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

优化前 `BuiltinSum` 通过 `ToSequence` / `DrainIterator` 全量物化。流式消费已随c9abdb5e提交，
原始generators配对约416.9→336.4ms、RSS83.6→21.9MiB；事件交错/早停、NativeAOT/搬迁及完整门禁通过。
该历史热点已消除，后续不得继续据此解释当前性能，见[sum专项](aot-sum-optimization.md)。


## 流式sum之后的候选复核

最新6ad1cce8的26项配对中，generators候选臂346.8ms、RSS21.9MiB，CP118.6ms/14.8MiB。
外部产出共3M；加上yield from内层，成功产出对应约4M次恢复，还需额外计入各生成器耗尽的最后恢复。

### 只读形参跳过挂起写回（尚未实现）

工厂确实将params写入初始frame；非参数局部是null/typed默认值，函数体首次运行后才有实际值。
因此可研究仅对已初始化且整个函数中无定义的参数跳过重复ArraySet，恢复读取仍保留。
这是引用写入优化，不能声称消除Dynamic参数反复装箱；实际吞吐须AOT配对测量。

分析必须穷尽Statement及Terminator定义，尤其Suspend.input在恢复时会赋值、异常计划插入的catch目标、
cell初始化/重绑定和trace product专用深度；只扫描Assign的伪代码不足以作为最终证明。
非参数常量局部不能据此省掉首次spill。send/throw/close、参数重绑定、捕获写入、locals观察、
yield from及EH/traceback均需独立结构与差分验证。

### 整数sum快路（尚未实现）

仅精确long可绕过用户魔法方法；溢出必须升级任意精度，异构/foreign/start回退仍维持交错和早停。
调研给出的 `total = checked(long + long)` 会使用CLR原始装箱，绕过现有PyInt.Box的小整数缓存；
未覆盖身份合同，不能直接采用。该写法也保留逐项堆分配，收益需要与真正原生累计状态的方案比较。
空输入必须返回原start引用；遇到需回调的值之前必须物化准确累加值，普通加法不能改成__iadd__。

### 调研论断修正

locals init存在时，按分支加载局部本身不会必然触发InvalidProgramException；
CLR验证器与Python/MIR的值有效性是不同要求。推迟restore仍需证明每个实际使用点与异常路径的定义，
不能仅以“definite assignment不允许”排除。生成器typed特化也需守卫/回退与跨挂起存储设计，
其工程成本高于本轮小竖切，但不是原则上不可能。
traceback压栈虽无稳态分配，仍有CPU成本；保留必要异常帧是合同，性能影响需测量。
