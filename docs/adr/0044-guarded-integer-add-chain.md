# ADR0044：有界actual-I64短Add链消除中间盒

- 日期：2026-11-02。
- 状态：WIP实现已通过语义/结构与完整门禁；同runner性能尚待验收，不预批准main默认开启。

## 证据与选择

[首次固定AOT/JIT配对](../validation/aot-jit-baseline/README.md)的calls负载在三臂
整个热进程均分配约147.48MB，AOT更快不等于消除了装箱。真实native的positional
函数为1738B，调用者直接调用该函数而非参数数组binder；两次Add之间仍有中间盒。
因此先研究短表达式的不可观察中间值，不先改固定arity或全Dynamic union ABI。

首切只Add，不引入Sub/Mul或新的runtime ABI。以既有FloatChain的候选及逃逸检查
为基础，不把浮点无溢出的性质套到整数，也不重结合整数计算。

## 语义合同

- 连续2–4个Add、最多5个不同Dynamic Local输入，每函数最多4个I64附加臂；
  保原16候选扫描预算，不重扫生成的fallback。FloatLoop/SLP原路径不扩。
- 中间dest匿名、唯一def且只在链内读；拒cell/env/EH/generator，拒跨Call、Line、
  store或其它观察屏障。finaldest只在整个快路成功后写入。
- 复用原输入引用快照，现F64守卫与快路仍先执行；F64失败后才尝试I64。
  所有输入必须先通过IsBoxedI64，再进行任何拆箱/算术；bool、Int32、BigInteger、
  double、用户对象均不能凭注解或转换进入整数臂。
- 每步WrappingAdd后检查`((a ^ sum) & (b ^ sum)) < 0`。任一步溢出直接回唯一
  原DynBinary整段，从原输入按原顺序重放；不能仅检查最终值或传回绕回后的sum。
- 完成后仅最终结果经既有BoxInt→PyInt.Box/cache物化。保大整数新结果与输入的
  外部身份、原inplace标志、用户协议及异常行/函数帧；不改已有输入对象。

## 工程预算与代价

设N输入、S步骤。I64准入前用当前整个函数实际数量作checked预算：原F64新增
`3N+S`局部、`N+3`块、`3N+S+1`语句；I64额外`2N+5S`局部、`N+S+2`块、
`2N+5S+1`语句。合计不能超过512局部、128块、4096语句。原动态S语句移到
fallback仍存在，不扣除。预算失败仅不加I64臂，原F64-only策略保持；此处不声称
修复原F64-only所有预算策略。

成本包括额外类型守卫、逐步溢出分支、CFG/机器码膨胀及失败路径开销。只减少不可
观察中间盒，不承诺输入/最终结果/累加器已去盒；不能据CIL结构宣称实际分配收益。

## 验收与默认政策

必须锁当前calls源码真实结构命中，而非未优化函数语义通过：逐步overflow、单最终
Box、完整原fallback及exact-unbox支配验证均保。固定CPython3.12.13差分覆盖
正负首/中/末溢出后回落、BigInt/bool/浮点/用户协议、单次求值、别名与异常栈。

语义和完整门禁通过后仍先WIP。性能以同runner固定baseline与候选配对，预先冻结
源码、进程数、顺序、deadline和判断标准；保存calls分配变化、失败/overflow成本、
native函数大小及其它负载负结果。历史81进程仅背景，不拿跨job噪声冒优化因果。
未证明净收益不默认启用，不能循环调参/增加样本直到有利。此前dataclass的AOT分配
不利结果继续保留。本ADR不等于整AOT优化或跨平台交付完成。

## 当前实际验收记录

2026-11-02：5项MIR、3项真实driver差分/异常栈、1项真实PE物化测试通过；
配对工具4项既有和3项新增轻测试通过。完整`make check`退出0：434 C#、226固定
CP基线、200 e2e通过/30 ignored，fmt/clippy/Rust门禁通过，无快照更新。
源码与测试保持原期望；新e2e依据实际driver差分转正。性能180进程尚未运行，
候选仅保存在WIP，不将语义通过写成性能获益或默认采用。
