# M3.10 最终整26：一次104冷进程覆盖

**52次编译、104次真实进程全部通过，输出与固定CPython3.12.13一致。** 这是单冷样本覆盖，不是稳态或统计显著性验证。整套冷时间没有证明总体净赢，负值全部保留；不追加重跑挑样本。

## 两版本与准确范围

A为最初M3.9已保存Release CLI beeda4c6ebb8da1735c77d52e237b0e8afa117873e261f7705d018e6032e9a9a，原runtime d9678b3d13d756843e8fef814275c7ba2cc4888fd2ed48ab17579f03bda17169。
C为最终M3.10默认Release CLI d9447ed733f8743a53bfff1e2b4b5d1b1f7a3c918751bb29d221ad0054fcca34，最终runtime fc4e746cd24ca868dfa0cf31665d97578c9a86828a97c8e594ed01458bd925b7。

A来源9023f21与已保存M3.9 full-suite准确来源；baseline-frozen-manifest.json是最初冻结时证据，里面“尚未计时”只描述当时，不是本次状态。C source.patch包含基点9023f21后全部tracked+untracked crates/runtime源码，未遗漏新文件。编译时不使用任何SLP实验/诊断flag，packed和scalar-SLP默认均关闭。旧direct/leaf实验也不开。

本表比较整个M3.10变化，含runtime布局，不是统一新runtime后的纯编译器隔离；此前NumericList三臂已独立隔离普通runtime成本。二者用途不同，不混口径。

## 唯一预设运行

原tests/benchmarks下26项、原规模，全部包含comprehensions/decorators/generators。每项每tier每版本1进程、0独立warmup：26×2×2=104，逐项交替A/C且tier反转顺序，非先跑一整块再另一块。没有PICoff、额外core、SLP计时或追加样本。

23项源码与上一固定13基线逐字hash匹配，复用其stdout；3项去pending后字节hash变化，按预案重新固定13取输出。source-hashes.json逐源记录当前/以前hash和复用决定，expected/保实际期望。全部样本逐字校验，任何首失败本应停止；实际无失败。

wall包括启动/JIT/main；main_ms仍含进程内JIT与初始化执行，不叫充分预热steady。分配/GC/JIT计数由同一基准stats提供，是整main而非热kernel。maxrss原始统一字节，compile_ms每程序一次单列，A/C都是Release CLI但仍不能用单次约毫秒级编译测量宣称编译速度差异。

52次编译总墙钟约255ms；104进程wall总和98.672s，仅本次实际耗时，不是未来保证。

## 诚实汇总（仅描述）

排startup hello的25项几何C/A：

|指标|default|disabled|
|---|---:|---:|
|main_ms|1.0954（慢9.54%）|1.0815（慢8.15%）|
|cold wall|1.0883|1.0838|
|allocated bytes|0.8384|0.8384|

单样本没有统计稳定性，不据此判每项因果，也不能忽略总体负值或宣称全项目加速。完整26表见summary.md，无删项。

不利示例：默认exceptions+181.2%、strings+73.6%、closures+49.1%、records+44.8%、sorting+39.5%；禁tierexceptions+115.2%、dict_ops+60.3%、sieve+58.5%、mandelbrot+52.1%。不自动把这些叫噪声；本轮无法分离编译、runtime、JIT与进程阶段影响，按已批准有限范围不新增救测。

数值目标同样保两tier差异：

|项目|default A→C main ms|disabled A→C main ms|main分配大致变化|
|---|---|---|---|
|matmul|371.677→183.643|328.842→171.422|151.24MB→8.17MB|
|nbody|1022.869→802.380|1249.559→1314.560|682.68MB→418.68MB|
|fib|235.207→260.698|244.136→269.176|约398KB基本相同|
|mandelbrot|318.237→414.320|378.802→576.091|约8.27MB基本相同|

nbody禁tier本次冷main慢约5.2%，不能用默认收益遮盖。矩阵本表分配含make_matrix/main开销，所以与专项kernel的1.28MB不是矛盾，禁止混作同分母。

## 与热专项的边界

此前float-chain、float-loop、numeric-list-first/revised、bounds-check、slp-first保有预设3进程重复的compiled kernel/micro、资源与实际机器码证据。那些结果支持有界形状与机器码判断，不代表本整26已“稳态净赢”。SLP因B/C对照无packed净益默认关闭；本轮不再测该实验。

## 复现与归档

run_final_suite.py复用benchlib.measure.run_once，52次编译后逐项轮转；输出含每次命令/源hash/CLI及runtime/DLLhash/原始stdout stderr/stats/wall/rss，summary脚本只做完整描述性表。

原始目录/tmp/ryupy-m3_10-final-suite-GPrNc2，运行log run.log。程序二进制留/tmp；仓库归档只纯文本，不含CLI/DLL/bin/obj。相同源码/命令可重现测试流程，不能保证单次时间相同。

2026-11-01 19:06:28 UTC session7007真实exit0，随即通知父释放唯一性能窗口；后续仅聚合/归档，无新构建或样本。提交/最终默认声明由父统筹。
