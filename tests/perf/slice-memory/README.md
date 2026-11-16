# 真子集切片：两层长期存活内存门（仅诊断）

产品固定base2128d6bc、candidate917f27b2；产品源码无修改，CI头不作为产品构建来源。
此前短切片四轮wall获益约32–34%，但近全与累计观察后仍可保留16B槽及全部盒，暂不合dev。

四轮AB/BA/BA/AB独立构建；规模32768/131072，每进程四份源；near(n−1)、cumulative每256项
覆盖全源、full控制，held与released分别独立进程。每层96进程，总192；每cell四独立进程。
每进程READY之后五次原始smaps_rollup采样，保留RSS/PSS/Private及maps/status/stat/page环境；
ACK之后真实checksum和逐元素身份校验强制读取活根。released先校验再释放根，独立进程记录，
不把其分配史/高水位与held相减当纯因果；Python层不强制GC，不能把驻留量叫托管活堆。

第二层为同精确Runtime的独立C# NativeAOT驱动，不是Python编译器路径，也不是JIT宿主。
Collect/Wait/Collect后GetTotalMemory(false)及GCMemoryInfo读实际托管活堆，根在采样后
Verify及GC.KeepAlive；GC指标采集后再输出，避免把JSON序列化分配算入活堆读数。
Runtime层的采样时点和GC模式与Python层不同，必须分表。

原始全部样本、8个Python产品SDK10.0.403清单、16个NativeAOT可执行hash、8个产品tree、
源码、独立Runtime发布日志保留。固定CP3.12.13同源握手/checksum/身份作oracle附录；
probe真实数值producer MIR来自各冻结CLI。仅诊断，不重复宣称默认check/搬迁验收。

smaps受共享runner/页驻留/GC未归还内存等影响，峰值不能代替采样驻留；独立Runtime活堆
也不是Python产品真实堆。进程启动基座、映射、缓存及规模差异保留分析，不挑有利点。
只有不同规模/轮次及full控制一致时才讨论近全/累计风险，不能以槽数估算替代实际数据。

Python `released`只是“源码显式清根阶段”：变量赋None已执行，但没有强制GC，编译器临时根、
运行时栈等是否仍持有对象未经堆快照证明，不能称对象已被GC释放。五次同PID采样不是五个
独立重复，每cell的独立重复数为四轮四进程。C#层有显式完整GC协议，仍须与Python分列。
