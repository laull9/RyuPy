# AOT/JIT首组固定小配对（待正式测量）

目标：观察同源三种执行路径成本后选择一个有证据的热点，不预设AOT更快、不自动改默认优化。
仅Linux x64 glibc；5源码期望/哈希在perf_fixtures/expected.json，固定CPython3.12.13。

| 负载 | 固定规模 | 热模式 |
| --- | --- | --- |
| matmul | 64×64，每调用独立乘法 | 3预热+7计时 |
| positional calls | 200000次 | 同上 |
| dataclass construction | 50000次 | 同上 |
| exceptions | 10000次，含3334次ValueError | 同上 |
| hello | 原hello源码 | 无 |

原kernel体保留；前三运算kernel和异常kernel的原/壳后模式均Dynamic，不能称静态dataclass。
预审原kernel MIR排源码行后matmul/positional/parse相同，construction仅Record全局编号变化；
matmul的NumericListCreate/ReadCheckedF64/AppendF64路径仍存在。timer与checksum壳不是新优化。

三臂：默认JIT、同CIL禁tier、同次AOT发布native。每源一次普通IL build+一次AOT build，
正式JIT使用后者同一DLL。普通IL另留hash，不假称全部PE字节等同。无实验SLP/direct/leaf开关。
每轮按J/T/A循环轮转、kernel顺序轮转：5×3×3=45冷进程，4×3×3=36热进程，共81。
热每进程3warm+7sample（252条计时），按三个独立进程报告中位；不能当252独立采样。
不追加样本救测。cold指新进程，不清OS文件缓存；固定预热不证明Tier1稳态。

指标：wall/main、RSS、全进程分配/GC/pause、native及发布目录字节、构建时长。hot进程
分配含初始化/3warm/7sample/打印，不冒kernel隔离分配；heap不是峰值。AOT的JIT指标
汇总N/A而原stats保留，不把零JIT时间当100%加速。JIT依赖共享.NET、AOT依赖系统库，
不把各自随附产物大小叫整机部署占用。

每build180秒、每应用120秒、整组30分钟；外CI步骤32分钟留收尾余量。主线程阻塞wait4
计wall/rusage，独立watchdog超时终止本次组并回收；不得5ms轮询量化轻负载冷时间。
每进程退出码/每warm及sample checksum和编号/有限非负统计/schema均检查，缺坏stats立即失败，
raw JSON保原bytes/hash，不用{}吞错；首失败保全证据停本轮，修复后按原合同重新开始。
CI本job无并行构建/采样，不能保证共享宿主绝无外部噪声；结果仅此runner的观测。

成功81全部完成后才清自身host/bin,obj缓存，测量间不清理扰缓存。原始命令/环境/双流/stats/
IR/native仅target及Actions工件；Git只放脚本、固定输入、精简结果与CI链接，不放raw运行日志。

预检：4项runner/schema测试通过；首次lscpu因本地sysfs缺失退出，尚未采样，改读真实/proc/cpuinfo
后九CP冷/热协议+15types/MIR/CIL dump通过，零SDK发布、零性能进程。正式81采样仍待CI。
