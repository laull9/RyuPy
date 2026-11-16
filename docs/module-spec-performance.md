# B5唯一正式性能矩阵结果

## 结论与证据边界

2026-11-05正式矩阵完成：16个顺序新dotnet进程、336个暖样本、16次首次Main，全部成功。无校准、无自动重试、无补样或择优删样。B4只完成P1/P2，因真实CIL归因发现ordinary globals退化而停止；正式样本只有修复后的B5这一轮。

B5新编译的真实resources.files路径降低本轮观测到的时间与分配，其他6项公共暖路径分配保持相同；但是共同源首次Main分配稳定增加17,648字节，禁tier下首次Main与完整启动两次均较A慢。只换runtime的Bold资源时间在default两次均更慢。不能声称全路径提速、冷成本消失或统计显著。

A=旧CLI PE+旧runtime；Bold=同旧PE+新runtime；Bnew=新CLI PE+新runtime。B5修复后88个既有字段保持原类型，仅新增12个metadata槽；完整CIL归因见[ADR0121](adr/0121-module-spec.md)，原审查为08-performance-b5-cil.zh.md。普通global类型已恢复，热方法原指令只按明确字段编号映射，不是靠删除指令归一化。

## 身份及固定执行

- A_cli：875561561084da9fc63477336e4df255649b233fe588fdda6eeb9fbecf253784；/workspace/shared/ryupy-typing-overload-validation/baseline/ryupy
- A_runtime：96ca01f0a24ec3afcfb5da64725ea77e7450b3fcd894459ada318ec45aaa4138；/workspace/shared/ryupy-typing-overload-validation/candidate-003/RyuPy.Runtime.dll
- B_cli：f6956679307d0f88483916e5b442d94505d55c3d480e1450e4e973875764ba92；/workspace/shared/ryupy-module-spec-validation/compiler-candidate-005/ryupy
- B_runtime：c2fb6b3f3b423631ae13eedb246bbc3125b00fba05b15051dccf619b772ca7f8；/workspace/shared/ryupy-module-spec-validation/candidate-release-004/RyuPy.Runtime.dll
- B_source_manifest：f56e50346ea2c795bdabe08d02e2cc9c652d581913a62228be34b9ada2f1fbef；/workspace/shared/ryupy-module-spec-validation/frozen-source-005/manifest.json
- python：021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7；/workspace/shared/toolchains/data/uv/python/cpython-3.12.13-linux-x86_64-gnu/bin/python3.12
- dotnet：634efbf2414144d140d6f59d167342c8b3b53b4c34346ed786dd0470ddc9a3cb；/workspace/shared/toolchains/dotnet/dotnet
- P3输入清单SHA：71f49d90795c5c479a677cfc4da035291a8436c7fd26055184a11277bf182c7a。
- 原始聚合JSON SHA：a0762a05a7b624e5c1f030d3fb163436e7aa6465eb301a617efac643f9572972。
- 原始文件：performance-b5/formal/每进程.json、.command.json、.result.json、.stdout、.stderr。所有336样本仍在原文件；formal-summary.json保留每进程三样本中位数和两个镜像配对，不合并为一个最好结果。
- 阶段结果：performance-b5/p3-complete.json passed=true。B5增量P2只有2编译+2审计+2smoke；4CP oracle、同字节host、A PE、host/A/B runtime审计及A/Bold smoke由performance原证据复用，102项SHA清单反复复核。

## 公共暖路径绝对值

每格为第1/第2个独立进程各自3样本的中位数；时间单位ns/item。短函数item为完整函数一次，其余函数每次64 item。外部元数据item包含三属性读取及比较；bare额外含函数调用、tuple与解包；files为真实资源路径返回和守卫，磁盘读取仅在计时外检查器。所有host delegate调用开销保留。

### default

| 项目 | A ns/item | Bold ns/item | Bnew ns/item | A/Bold/Bnew B/item |
|---|---:|---:|---:|---:|
| static_calls | 272.88 / 295.26 | 269.32 / 277.70 | 299.17 / 296.02 | 33 / 33; 33 / 33; 33 / 33 |
| dynamic_defaults | 123.17 / 126.60 | 121.94 / 144.62 | 123.63 / 117.49 | 33 / 33; 33 / 33; 33 / 33 |
| plain_def_create | 93.59 / 141.27 | 83.09 / 92.49 | 95.13 / 83.76 | 192 / 192; 192 / 192; 192 / 192 |
| closure_create | 124.11 / 118.28 | 107.70 / 94.98 | 92.18 / 90.67 | 256 / 256; 256 / 256; 256 / 256 |
| module_external | 658.36 / 709.58 | 651.79 / 655.05 | 723.94 / 664.88 | 24.625 / 24.625; 24.625 / 24.625; 24.625 / 24.625 |
| module_bare | 906.35 / 817.95 | 877.80 / 847.40 | 907.26 / 966.24 | 96.625 / 96.625; 96.625 / 96.625; 96.625 / 96.625 |
| resource_files | 6090.23 / 5452.89 | 7849.73 / 6816.58 | 2716.52 / 2574.21 | 2760 / 2760; 2760 / 2760; 1296 / 1296 |

| 项目 | Bold相对A时间变化：配对1/2 | Bnew相对A时间变化：配对1/2 |
|---|---:|---:|
| static_calls | -1.30% / -5.95% | +9.64% / +0.26% |
| dynamic_defaults | -0.99% / +14.24% | +0.37% / -7.19% |
| plain_def_create | -11.22% / -34.53% | +1.65% / -40.71% |
| closure_create | -13.23% / -19.70% | -25.73% / -23.34% |
| module_external | -1.00% / -7.68% | +9.96% / -6.30% |
| module_bare | -3.15% / +3.60% | +0.10% / +18.13% |
| resource_files | +28.89% / +25.01% | -55.40% / -52.79% |

### tieroff

| 项目 | A ns/item | Bold ns/item | Bnew ns/item | A/Bold/Bnew B/item |
|---|---:|---:|---:|---:|
| static_calls | 77.32 / 68.36 | 75.36 / 75.45 | 71.15 / 76.84 | 33 / 33; 33 / 33; 33 / 33 |
| dynamic_defaults | 44.13 / 44.87 | 39.34 / 40.28 | 38.10 / 42.61 | 33 / 33; 33 / 33; 33 / 33 |
| plain_def_create | 63.28 / 47.34 | 51.32 / 45.22 | 42.20 / 43.35 | 192 / 192; 192 / 192; 192 / 192 |
| closure_create | 57.74 / 68.46 | 73.68 / 70.50 | 69.30 / 60.77 | 256 / 256; 256 / 256; 256 / 256 |
| module_external | 155.10 / 157.35 | 167.10 / 173.14 | 174.69 / 159.42 | 24.625 / 24.625; 24.625 / 24.625; 24.625 / 24.625 |
| module_bare | 261.89 / 215.70 | 246.81 / 226.02 | 235.42 / 252.97 | 96.625 / 96.625; 96.625 / 96.625; 96.625 / 96.625 |
| resource_files | 6132.19 / 6451.55 | 5736.94 / 6176.06 | 1071.47 / 1163.62 | 2760 / 2760; 2760 / 2760; 1296 / 1296 |

| 项目 | Bold相对A时间变化：配对1/2 | Bnew相对A时间变化：配对1/2 |
|---|---:|---:|
| static_calls | -2.54% / +10.37% | -7.99% / +12.40% |
| dynamic_defaults | -10.85% / -10.25% | -13.66% / -5.06% |
| plain_def_create | -18.90% / -4.47% | -33.31% / -8.43% |
| closure_create | +27.61% / +2.97% | +20.03% / -11.24% |
| module_external | +7.73% / +10.03% | +12.62% / +1.31% |
| module_bare | -5.76% / +4.79% | -10.10% / +17.28% |
| resource_files | -6.45% / -4.27% | -82.53% / -81.96% |

分配结果在两重复及两tier配置保持：static_calls 33 B/item、dynamic_defaults 33、plain_def_create 192、closure_create 256、module_external 24.625、module_bare 96.625。files：A/Bold均2760，Bnew1296，减少1464 B/item（53.04%）；每完整64-item函数减少93,696 B。

default的Bold files时间两配对分别更慢，Bnew的static_calls和module_bare也有不利配对；tieroff的module_external两配对都更慢，closure_create存在不利配对。表格保留全部方向。暖时间只有两独立重复且tier优化状态有变化，不宣布这些幅度统计显著，更不能拿default与tieroff互比当实现收益。

## 冷Main与完整启动

完整启动从外层Popen之前到收到首次Main标记，包含dotnet启动、加载、配置/反射准备、真实Main/import/spec/loader/前言和管道传输；不减IPC。窄Main另测wall、当前线程分配、进程CPU和GC，不冒称整个原生启动总分配。

| 配置 | 臂/重复 | 完整启动ms | 窄Main ms | 窄Main分配B | 窄Main CPU ms | GC0/1/2 |
|---|---|---:|---:|---:|---:|---|
| default | common-A1 | 106.413 | 55.223 | 440760 | 55.365 | 0/0/0 |
| default | common-Bold1 | 98.821 | 54.087 | 448048 | 54.248 | 0/0/0 |
| default | common-Bnew1 | 110.095 | 67.427 | 458408 | 67.515 | 0/0/0 |
| default | common-Bnew2 | 109.341 | 60.909 | 458408 | 61.020 | 0/0/0 |
| default | common-Bold2 | 97.856 | 55.007 | 448048 | 55.142 | 0/0/0 |
| default | common-A2 | 118.111 | 73.221 | 440760 | 73.365 | 0/0/0 |
| default | api-Bnew1 | 113.003 | 69.424 | 3513792 | 69.602 | 0/0/0 |
| default | api-Bnew2 | 106.128 | 61.412 | 3513792 | 61.558 | 0/0/0 |
| tieroff | common-A1 | 212.639 | 148.606 | 444856 | 148.624 | 0/0/0 |
| tieroff | common-Bold1 | 202.284 | 141.043 | 452144 | 141.353 | 0/0/0 |
| tieroff | common-Bnew1 | 214.488 | 158.358 | 462504 | 158.452 | 0/0/0 |
| tieroff | common-Bnew2 | 213.405 | 157.324 | 462504 | 157.406 | 0/0/0 |
| tieroff | common-Bold2 | 203.752 | 142.336 | 452144 | 142.686 | 0/0/0 |
| tieroff | common-A2 | 189.494 | 130.307 | 444856 | 130.413 | 0/0/0 |
| tieroff | api-Bnew1 | 236.563 | 172.756 | 3517888 | 172.853 | 0/0/0 |
| tieroff | api-Bnew2 | 205.788 | 151.687 | 3517888 | 151.779 | 0/0/0 |

| 配置 | 相对A臂 | 启动差ms：配对1/2 | Main差ms：配对1/2 | Main分配差B：配对1/2 |
|---|---|---:|---:|---:|
| default | Bold | -7.592 / -20.255 | -1.135 / -18.214 | +7288 / +7288 |
| default | Bnew | +3.682 / -8.770 | +12.205 / -12.313 | +17648 / +17648 |
| tieroff | Bold | -10.355 / +14.258 | -7.563 / +12.029 | +7288 / +7288 |
| tieroff | Bnew | +1.849 / +23.912 | +9.752 / +27.017 | +17648 / +17648 |

两种tier配置的共同源冷分配差完全相同：Bold相对A +7,288 B，Bnew相对A +17,648 B，Bnew相对Bold再加10,360 B。差值为完整组合路径，不武断拆成单个metadata对象成本。API首次Main自身约3.51MB，不与共同源作相减归因。

## 新API绝对暖成本

仅Bnew，无A兼容基线。每item为完整Python函数，构造/关键字调用/赋槽/首次cached等组合成本不能称单getter成本；eq仅限协议中两个字符串/None/空locations且热缓存相等的对象。

| API | default ns/item：重复1/2 | default B/item：重复1/2 | tieroff ns/item：重复1/2 | tieroff B/item：重复1/2 |
|---|---:|---:|---:|---:|
| construct_module | 2913.71 / 2570.03 | 592 / 592 | 977.68 / 938.07 | 592 / 592 |
| construct_package | 2637.55 / 2789.69 | 792 / 792 | 916.08 / 988.68 | 792 / 792 |
| cached_cold | 4472.89 / 4552.83 | 1224 / 1224 | 1583.74 / 1623.75 | 1224 / 1224 |
| cached_hot | 700.52 / 413.73 | 0 / 0 | 128.94 / 167.62 | 0 / 0 |
| parent | 1635.61 / 787.98 | 192 / 192 | 220.12 / 235.28 | 192 / 192 |
| eq | 7044.52 / 3953.24 | 272 / 272 | 1203.22 / 1217.38 | 224 / 224 |
| spec_repr | 4002.21 / 2632.86 | 920 / 920 | 849.65 / 831.26 | 920 / 920 |

热cached两配置均0 B/item；有限eq为default272 B/item、tieroff224 B/item，不能遗漏此配置差异。其余API分配两配置一致。default API时间重复间有明显变化，保留全部中位数，不取最快一次。

## 校验、CPU/GC与尚未关闭的风险

- 16进程均host/runtime/generated SHA及oracle/iterations SHA匹配，Main计数均1，全部checksum/公开语义通过；不代表逐iteration全部返回对象已审计。功能检查器在计时外，warmup固定；显式GC仅在暖样本前。
- 每个暖样本原始记录仍含ns/function、B/function、ns/item、B/item、CPU秒、GC0/1/2和checksum；进程CPU分辨率有限，小样本可能记录0，不能据0推断没有CPU消耗。原始记录保留所有样本，报告表格只是按既定中位数规则展示。
- 初始化仅证明host真实Main调用一次、marker及anchor/语义稳定，不宣称观察到所有模块正文历史执行次数。
- 此轮为单机Linux .NET JIT，有固定default/tieroff两配置、两次独立重复，无统计显著性、跨机器、多核或AOT结论。没有扩大总体benchmarks或固定项目覆盖。
- 资源files仅真实合格文件包/固定data.txt检查，不涵盖磁盘吞吐、metadata篡改失败路径、所有模块形态、循环导入和失败重试性能。
- ModuleSpec只覆盖固定构造/缓存/parent/有限eq/repr；更多keyword/bound/generator、TypedDict/count、descriptor/format定制协议及历史13项来源风险仍未覆盖。
- 不能声称此次收益抵消旧overload的+3312/+3056 B冷开销或禁tier时间不利。B5公共路径新冷成本仍明确存在，未优化掉。
- 本报告不把旧PE单换runtime的Bold结果等同新编译成本；新metadata前言真实保留在Bnew Main内。

## 六项公共暖路径全部时间不利配对

下面逐条列出相对A的所有正向时间差，包含小幅不利观察，不因其它重复更快而删除。两配对分别由各进程3样本中位数比较；这不是统计显著性检验。

| 配置 | 臂 | 项目 | 配对 | A ns/item | B ns/item | 增加ns/item | 增加百分比 |
|---|---|---|---:|---:|---:|---:|---:|
| default | Bold | dynamic_defaults | 2 | 126.60 | 144.62 | +18.03 | +14.24% |
| default | Bold | module_bare | 2 | 817.95 | 847.40 | +29.44 | +3.60% |
| default | Bnew | static_calls | 1 | 272.88 | 299.17 | +26.29 | +9.64% |
| default | Bnew | dynamic_defaults | 1 | 123.17 | 123.63 | +0.46 | +0.37% |
| default | Bnew | plain_def_create | 1 | 93.59 | 95.13 | +1.54 | +1.65% |
| default | Bnew | module_external | 1 | 658.36 | 723.94 | +65.58 | +9.96% |
| default | Bnew | module_bare | 1 | 906.35 | 907.26 | +0.91 | +0.10% |
| default | Bnew | static_calls | 2 | 295.26 | 296.02 | +0.75 | +0.26% |
| default | Bnew | module_bare | 2 | 817.95 | 966.24 | +148.29 | +18.13% |
| tieroff | Bold | closure_create | 1 | 57.74 | 73.68 | +15.94 | +27.61% |
| tieroff | Bold | module_external | 1 | 155.10 | 167.10 | +11.99 | +7.73% |
| tieroff | Bold | static_calls | 2 | 68.36 | 75.45 | +7.09 | +10.37% |
| tieroff | Bold | closure_create | 2 | 68.46 | 70.50 | +2.04 | +2.97% |
| tieroff | Bold | module_external | 2 | 157.35 | 173.14 | +15.78 | +10.03% |
| tieroff | Bold | module_bare | 2 | 215.70 | 226.02 | +10.33 | +4.79% |
| tieroff | Bnew | closure_create | 1 | 57.74 | 69.30 | +11.56 | +20.03% |
| tieroff | Bnew | module_external | 1 | 155.10 | 174.69 | +19.58 | +12.62% |
| tieroff | Bnew | static_calls | 2 | 68.36 | 76.84 | +8.48 | +12.40% |
| tieroff | Bnew | module_external | 2 | 157.35 | 159.42 | +2.06 | +1.31% |
| tieroff | Bnew | module_bare | 2 | 215.70 | 252.97 | +37.27 | +17.28% |

共20个不利配对观察。其余方向与所有绝对值见前表。旧PE的Bold资源路径另须单独保留风险：default +28.89%/+25.01%，tieroff -6.45%/-4.27%。Bnew的收益不能抵消或代替这条旧PE风险。

## 必需语义与后续优化假设（本轮未验证）

- 新导入语义需要每次导入尝试拥有正确身份的spec/loader、包locations列表、cached值及真实模块槽，并保留别名、副作用和失败重试边界。这些新增可观察对象不能靠省掉初始化、跨模块共用身份或移出Main来制造低冷成本。
- 当前PyModuleMetadata同时持有初值字典、来源字段、spec/loader及缓存值；完整冷Main差+17,648 B与runtime-only差+7,288 B并未分项测量，不能把全部新增字节标记为不可优化的必要对象成本。字典表示、初始化派发、来源守卫与延迟类初始化等均可能有内部优化空间，须独立候选逐项证明保持语义，再测量。
- 一个尚未验证的热路假设：PyModule.GetAttr中冷metadata分支的try/catch可能妨碍普通属性热路内联。可考虑隔离到专用冷helper，但此轮没有JIT归因证据，不能据此解释所有不利配对或宣布风险已关闭。
- 尤其Bold default资源路径和Bnew tieroff module_external两配对不利应独立追踪。没有将本轮矩阵重跑来挑选更有利结果；后续优化应建立新候选和新证据，不替换本报告。
