# OrderedDict candidate-003：普通exact dict轻量性能对照

2026-11-06，唯一默认JIT A-B-B-A窗口已完成。这里只报告六条完整Python工作负载，不是裸操作成本、语义验收、项目性能或无回退证明。

## 结果与必须保留的不利

- 共同PE下候选oracle与CPython原stdout逐字一致。A1/B1/B2/A2均exit0、stderr空，无超时、重试、补采或事后调参。120正式样本、48预热批次全部保留；先前旧runtime预备基线单列，未混入统计。
- 普通getitem的两配对为+15.279450%/+7.238870%，setitem为+5.628776%/+5.546016%。这是本窗口同方向不利，不能宣称零回退。contains第一对+29.625906%、第二对−0.381627%，不能只选第二对。
- equality第二对+36.812374%，construct第二对+5.058370%；它们与第一对方向相反。汇总的小幅下降不能隐藏这些不利或被称为稳定提速。
- iterate两配对下降，但旧A1/A2中位数1979.414002/757.054870 ns差异大；不能把汇总−69.056782%当作稳定普适收益。
- 正式批次6.158–144.608ms，多个路径存在明显样本内/进程间变化。两个预热批次不足证明tier/PGO稳定；未采JIT/调度事件，不能归因于某条新增分支、JIT切换或单纯噪声，也不能判定统计显著。仅每侧两个独立进程，不把10个样本当10个进程。
- 普通dict新增Compare、PyHash和consumer分支确有潜在影响，不能因存储文件未改就声称无成本。没有分配、GC、长期内存、OrderedDict自身吞吐、AOT或跨平台数据。S1/B2及所有历史性能风险仍开放。

## 全部进程中位数与范围

单位为ns/完整工作负载的一次循环。每进程仅5个正式样本，范围保留全部样本。迭代的一次循环是32-key整轮遍历；相等的一次循环是两次32项dict比较（相等/不等各一次）。其它路径同样包含Python循环、取模、算术、分支或checksum，详见源码与方法文件。

| 路径 | A1中位数 [min,max] | B1中位数 [min,max] | B2中位数 [min,max] | A2中位数 [min,max] |
|---|---|---|---|---|
| construct_8 | 743.234711 [498.596497, 1290.730118] | 680.230774 [645.348907, 780.208572] | 836.384262 [817.725982, 1066.465164] | 796.113876 [590.486145, 2206.539718] |
| getitem_32 | 26.529791 [26.399839, 28.156193] | 30.583397 [27.037298, 54.599451] | 49.269037 [36.918659, 53.887238] | 45.943264 [41.094223, 46.557974] |
| setitem_32 | 53.857580 [48.742064, 71.853798] | 56.889103 [38.259071, 70.827494] | 91.064123 [60.826395, 93.851191] | 86.279072 [57.376039, 90.489019] |
| contains_32 | 53.520233 [53.152172, 70.465906] | 69.376087 [30.445707, 73.788996] | 75.403969 [48.501740, 80.524471] | 75.692833 [48.761742, 77.013140] |
| iterate_32 | 1979.414002 [1118.705994, 2215.780334] | 456.454407 [375.836915, 549.370606] | 390.297119 [378.351135, 392.764832] | 757.054870 [737.027831, 797.714844] |
| equality_32 | 1427.687210 [941.819168, 1533.251358] | 1022.865891 [1004.878723, 1097.414597] | 1313.036988 [1089.119461, 1355.480743] | 959.735550 [917.759262, 1116.162033] |

## 全部配对与描述性汇总

配对1为B1/A1−1，配对2为B2/A2−1。描述性汇总为((B1中位数+B2中位数)/(A1中位数+A2中位数)−1)，不是汇合全部样本的中位数、不是置信区间。正数不利。

| 路径 | 配对1变化% | 配对2变化% | 两进程中位数均值变化% |
|---|---:|---:|---:|
| construct_8 | -8.476991 | +5.058370 | -1.476829 |
| getitem_32 | +15.279450 | +7.238870 | +10.182238 |
| setitem_32 | +5.628776 | +5.546016 | +5.577823 |
| contains_32 | +29.625906 | -0.381627 | +12.047535 |
| iterate_32 | -76.939922 | -48.445333 | -69.056782 |
| equality_32 | -28.355043 | +36.812374 | -2.157971 |

## 计时、预热、启动与协商窗口

- 每kernel先2个全工作量暖批次，随后固定5样本；六kernel固定次序、固定次数。time.perf_counter来自PyTimeModule的单调Stopwatch计数。只在两次API之间计完整fn；输入生成、输出和断言在外。
- 外部wall包括进程启动、JIT、输入、预热、正式样本和I/O，只作预算，不是吞吐或纯startup。没有以wall减批次时间伪造startup。rusage CPU是整个进程的user/system差，也不作单操作CPU成本。
- 窗口前父明确暂停父/实现/独审构建测试，仅文稿继续；原文见coordination-formal.json。完成后立即通知释放窗口。无全局进程可见性、无指定CPU affinity，不能排除共享宿主调度。
- 预备旧基线保留在logs/preliminary-a与PREPARATION.zh.md，仅作准备证据，不因正式值不同而删除。

| 进程 | 开始UTC | 结束UTC | 外wall秒 | user CPU秒 | system CPU秒 | 退出 |
|---|---|---|---:|---:|---:|---:|
| formal-a1 | 2026-11-06T10:50:16Z | 2026-11-06T10:50:19Z | 2.212041 | 2.293327 | 0.051174 | 0 |
| formal-b1 | 2026-11-06T10:50:19Z | 2026-11-06T10:50:21Z | 2.054316 | 2.126739 | 0.051287 | 0 |
| formal-b2 | 2026-11-06T10:50:22Z | 2026-11-06T10:50:24Z | 2.276957 | 2.414876 | 0.025638 | 0 |
| formal-a2 | 2026-11-06T10:50:24Z | 2026-11-06T10:50:26Z | 2.244180 | 2.358271 | 0.031902 | 0 |

## 输入、工具、环境和执行身份

- canonical基线HEAD：21748729ba978393bd4da46fe7a35ca4ca9c1835。该目录只读，未修改或push。最终候选来源为ryupy-ordereddict-implementation/candidate-003。
- Linux x64、AMD EPYC 9V74 80-Core Processor、.NET host 10.0.14。显式DOTNET_TieredCompilation=1、DOTNET_TieredPGO=1，无其它COMPlus性能覆写；完整子进程环境逐次保存，没有继承未记录的性能变量。PYTHONHASHSEED=0，CPython只作oracle，不作为速度基线。
- 源只用固定CLI编译一次。相同run/exact_dict.dll、runtimeconfig、dotnet/hostfxr/CoreCLR/JIT和工作目录，唯一替换run/RyuPy.Runtime.dll；源/PE/工具/host/runtime在每进程前后均核验。没有编Rust、SDK项目或独立Host。

| 内容 | SHA256 |
|---|---|
| Python源 | 261d3324509083bc92a32f0e0ff90c498b141668d0dbd1f24c02e8008ce853e8 |
| 共同PE | 149eb4a12dece149c6b73a8d08f4045223bd12a08bf0ed65d47cd30b96a26542 |
| runtimeconfig | ab20c6e50056e26fa0a23247870ac66dc23688ff3e4187399a8dfe436657fe0a |
| 旧runtime A | 3963737a656bfed430f1a10736d520b077fe6081db40bdf828b8f0f91457eab1 |
| 最终runtime B | fb5892bc74c102f24b7829c43695f64ca7258746513a9ba9f0d31a6a3c87c94e |
| CLI | 1757a3f128983be7a39f8a3efee698f1f8a8cf749d2c0ce1546076bca030f4db |
| CPython | 021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7 |
| dotnet host | 634efbf2414144d140d6f59d167342c8b3b53b4c34346ed786dd0470ddc9a3cb |
| libhostfxr.so | 3e39d88afa3670798842aafb156893cd49fa817f604949addfabffae2925367e |
| libcoreclr.so | 843b66a2b33a570537c779283b27f7c1f3ff5394e5a72436bf181c885d4cdfff |
| libclrjit.so | fd4e672a34b28fb38d2b5f43d7c50d8b1892aebca97d41cfaccc1c8be3ee40e9 |
| run.py | ef174e34f89109e5114c34411a2fad539001c05a04f02f0b38a04f4f7982801a |
| formal-summary.json | 9bec8de6e6d3320cad27875f57d9e86b50dc518a681cc2b36551e753e2356f04 |

正式执行命令（只执行过一次，runner拒绝覆盖已有结果）：

```sh
/workspace/shared/toolchains/data/uv/python/cpython-3.12.13-linux-x86_64-gnu/bin/python3.12 /workspace/scratch/e8be0fcf51ac/ryupy-ordereddict-performance/run.py compare /workspace/scratch/e8be0fcf51ac/ryupy-ordereddict-implementation/candidate-003/RyuPy.Runtime.dll fb5892bc74c102f24b7829c43695f64ca7258746513a9ba9f0d31a6a3c87c94e /workspace/scratch/e8be0fcf51ac/ryupy-ordereddict-performance/coordination-formal.json
```

## 证据索引

- FINDINGS.zh.md：本报告。METHODOLOGY.zh.md：事先冻结的方法与界限。PREPARATION.zh.md：原准备与预备基线记录。
- source/exact_dict.py：完整公开工作负载；run.py：完整执行和解析脚本；write_report.py：本报告生成脚本，不执行性能。
- inputs/frozen.json：工具/PE/host哈希；inputs/host.json与logs/dotnet-info/stdout：宿主信息；inputs/runtime-a和runtime-b：两个冻结DLL；inputs/common：唯一编译PE和runtimeconfig。
- logs/cp-oracle、old-oracle、candidate-oracle：正确性原流；logs/compile-once：唯一编译原流。
- logs/formal-a1、formal-b1、formal-b2、formal-a2：每次精确命令/环境/时戳、原stdout/stderr、退出/预算、全部样本JSON和前后输入核验。
- formal-all-samples.csv：168行完整正式窗口数据（120正式+48暖）。formal-summary.json与formal-progress.json：未截断原数值与汇总。
- preliminary-artifacts.sha256.json：第一阶段快照清单；final-artifacts.sha256.json：最终全证据清单，不包含其自身。

完成表示这个有界观测完整，不表示性能验收或回退已关闭。没有进一步运行或挑样。
