# M3.9 有限区间专项：无interval基线产物与诊断

产物根 `/tmp/ryupy-m3_9-interval-baseline-QAjoC0`。只使用保存的alias-baseline-HVOxHG旧CLI
（默认holder+TupleScalar，无interval/叶特化）编新负载，未cargo构建当前正在开发源码。
compiler-source.patch/manifest来自那个旧CLI真实捕获的完整crates/runtime源码，不用今天的
Rust工作树冒充旧CLI来源。本轮配当前Release runtime（含TraceCheckDepth），runtime-source.patch
单独记录当前runtime差异；hashes.txt固定实际CLI/程序集/runtime。归档不含二进制。

interval_diamond.py有七场景：函数调用choose(flag)与循环内diamond，均3/7合流后+2且参数
使函数保持Dynamic；unknown int/bool/overflow保未知路径，不删对照。固定CPython3.12.13与
真实DLL短验逐字一致。MIR显示常量box→分支合流→dyn.Add，不是本来就静态类型的假优化目标。

## 没有正式单边时间表

本阶段只固定产物、语义、分配/JIT。compare.py已准备，候选后再在同窗口交错配对，不先跑
两组单边时间。诊断harness运行入口时负载原打印的sample行只是初始化输出，**不作为正式性能
数据**；没有将这些插桩/分配/JIT进程的时间混入性能结论。

## 分配

独立harness复用实参数组，20次热身、5批20000循环，默认/禁tier各3新进程；所有批次一致：

| 场景 | 总字节/20000轮 | B/轮 |
|---|---:|---:|
| call_true/call_false | 64 | 0.0032 |
| loop_true | 478776 | 23.9388 |
| loop_false | 479328 | 23.9664 |
| unknown_int/unknown_bool | 64 | 0.0032 |
| unknown_overflow | 3360032 | 168.0016 |

重要：循环内diamond使旧严格holder匹配器不命中，原总和每轮有结果盒；call_diamond匹配
holder。这里保留真实现状，不冒称两形的holder状态完全相同；后续只能比较每形自己的基线/
候选，并以实际IR区分区间加法变化与其它变化。unknown溢出保持BigInteger语义。

## 实际机器码

默认/禁tier单独诊断进程采集choose/loop_diamond全部分层版本。FullOpts choose697B、
choose_unknown690B、loop_diamond1493B；真实可见PIC内联多类型路径与BinaryCore冷调用，
loop另有InPlaceBinary累加路径。不能因为CIL一个dyn.Add就称JIT未做任何类型快路。

## 重现

用保存CLI（或从compiler-source-manifest基提交+compiler-source.patch构建旧编译器）编
interval_diamond.py到artifact，配同hash当前runtime。harness/alloc.csproj引用../artifact里的
runtime，独立build后 `python3 diagnostics.py 产物根` 采分配/JIT。输出覆盖传入根中对应名字，
复现须用新目录，不能覆盖原记录。compare.py未来接baseline=目录、candidate=目录、--output新目录。
两个源码补丁对应不同阶段，不能把runtime最新patch重复套进旧patch的已改runtime；重建时使用
旧补丁编译器源码与最新runtime独立来源。实际二进制hash是最终配套证据。本轮未提交。
