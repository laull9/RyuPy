# M3.9 最后一次有界叶调用候选：仅已证明i64实参

候选保存/tmp/ryupy-m3_9-leaf-narrow-8wLQS8（准确根以comparison.json的variants和hashes.txt为准）。
基线为alias-baseline-HVOxHG旧编译体，复制到新baseline-artifact并换同当前runtime；不是wide候选。
源码source.patch含捕获时crates/runtime tracked+untracked，manifest锁UTC/基提交/hash；排pending
与results，归档不含二进制。当前CLI默认临时开启收窄pass，最终政策由父决定。

## 命中与语义

原alias(index)的实际MIR仍WrappingAdd/私有函数anchor。未知动态参数repeat则没有特化：完整
方法CIL与基线SHA256逐字节相同（unknown-cil-hash.json），不将未命中描述为guard加速。未知
负载保留源码/完整CIL作核查，不重复测量没有变化的方法。

新增known_i64.py包含同一repeat_index调用点以range index输入、同一repeat_constant调用点以
常量7输入；每个调用点分别将模块alias绑定twice与other。两方法都实际命中WrappingAdd，重绑
场景仍要通过同一身份失败回退，不能用未优化的另一个函数替代失败路径。固定CPython3.12.13
和两个编译体结果/类型均通过，正式20000轮返回39998/20000/14/8。

## 两组交错结果

默认/禁tier各3新进程，每场景3热身7样本，基线/候选轮转；完整做两组，不再追加调参。统计
先每进程中位再跨进程中位，wall单列。summary.json和absolute-times.md给出所有ms/ns和百分比，
ns是整个循环除20000而非纯callee耗时。环境未隔离CPU/固定频率，保留对照波动。

| 场景/模式 | 第1组 B→C ns/调用 | 第2组 B→C ns/调用 |
|---|---:|---:|
| 原alias / 默认 | 92.41→29.47 | 108.51→29.43 |
| 原alias / 禁tier | 65.37→46.94 | 88.53→30.23 |
| index_fast / 默认 | 76.51→37.78 | 81.16→24.17 |
| index_fast / 禁tier | 59.19→32.62 | 98.00→32.85 |
| index_rebind / 默认 | 68.68→85.78 | 73.17→84.79 |
| index_rebind / 禁tier | 40.74→49.24 | 44.12→47.51 |
| constant_fast / 默认 | 51.41→4.91 | 51.79→4.98 |
| constant_fast / 禁tier | 28.02→5.52 | 29.17→5.69 |
| constant_rebind / 默认 | 73.92→67.85 | 60.26→67.10 |
| constant_rebind / 禁tier | 27.55→32.12 | 29.73→31.93 |

原direct对照也保留：默认+9.8%/+2.7%，禁tier+1.9%/-7.7%。成功路径净收益稳；但index重绑默认
两组+24.9%/+15.9%，禁tier+20.9%/+7.7%。constant重绑禁tier+16.6%/+7.4%。其绝对成本约
2–17 ns/调用，是否接受需要明确默认政策，不能宣称“失败路径无回归”。未知动态参数完全跳过
特化确实消除了wide版本相应额外守卫，但没有消除已知输入下的函数身份失败成本。

## 实际分配与机器码

原alias/direct同harness、known四场景另用known-harness（反射在计时外设置真实模块alias到
导出的twice/other）。20热身后5批20000调用，两个tier各3进程，所有批次B/C相同：

| 场景 | B/C总字节 | B/C每调用 |
|---|---:|---:|
| 原alias/direct | 950800 B | 47.54 B |
| index_fast | 950736 B | 47.5368 B |
| index_rebind | 947688 B | 47.3844 B |
| constant_fast/rebind | 0 B | 0 B |

没有分配优化收益；PIC参数槽原已复用，精确arity原已跳过binder，不误称新pass消除逐次参数数组
或binder。已知index的装箱/结果盒仍在，常量小整数命中缓存时两版都无分配。

真实FullOpts known repeat_index代码448→611B（stack24B不变），repeat_constant378→537B
（stack24→40B）；实际身份失败仍回CallValueFast，默认JIT部分PIC内联但无wide累加器那样巨量
多类型算术片段。完整分层asm保留。成功路径跳过PIC/delegate/thunk与叶trace push/pop，仍有深度
检查；无需凭源代码属性猜JIT行为。时间回退可能受守卫/布局/活跃值影响，无硬件计数器不作
单因素定量归因。

## 重现与结论

在manifest基提交独立工作树应用source.patch，cargo build -p ryupy-cli；用相同当前runtime分别
配旧保存CLI产物和当前候选产物。compare.py测alias_leaf，compare-known.py测known_i64，传
baseline=目录、candidate=目录、--output新目录。diagnostics.py及known-diagnostics.py是独立
分配/JIT流程，两harness引用../artifact/runtime，不能与正式计时并行或覆盖本次原始文件。

这是最后一次有据收窄实验：成功收益明确、未知类型调用无代码变化、已知输入重绑成本仍重复
出现。推荐由父选择明确接受有界权衡或保显式实验，不继续救微基准；未宣称整工程gate通过，未提交。
