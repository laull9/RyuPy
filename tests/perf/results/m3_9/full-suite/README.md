# M3.9 整套基准与四核心同进程 kernel 验收

本目录原始执行位置记录在命令/manifest。保存 CLI 与程序集只留 `/tmp`，仓库归档只保文本证据。


## 结果摘要与默认判定边界

- 原顺序 4 组、随后逐 benchmark 轮转（432进程）全部26项输出通过；comprehensions/decorators/generators 三个旧pending均 ready，没有删除失败分母。
- 主要比较为 `paired-summary.md/json`：排除启动项hello、每项只计默认variant的25项 main 几何 C/B：default **1.0278（+2.8%）**、禁tier **0.9474（-5.3%）**。PIC关闭变体另外保留，不重复加权。没有证据支持“默认整套净赢”的声明。
- 默认不利结果包括 closures 385.636→634.878 ms（+64.6%）、dict_ops430.188→536.229（+24.6%）、sorting+15.4%，全部保留。原顺序组还存在同源CPython跨时段显著漂移，见`cpython-period-variation.json`；不以事后归一化删改这些数字，也不将整套百分比唯一归因于某一pass。
- 四core预热kernel全80进程语义通过，见 `kernel-summary.md/json`。default C/B时间变化 fib -1.2%、mandelbrot +5.0%、matmul +1.5%、nbody +0.1%；禁tier分别 -9.2%、-3.4%、-10.5%、+4.5%。不是“所有core都提升”。
- fib/mandel 核分配中位均0；matmul两方约144.67MB/3次100×100乘法，nbody两方679,309,624B/100000步。动态数值的消箱欠账仍真实存在；该测量中CPython的matmul/nbody kernel比RyuPy快，不隐去。
- interval的有限默认是独立proof/已改写专项证据支持的政策决定，不是本整套证据证明普遍净收益；此前unknown_overflow不利数据仍在`../interval-candidate/`。直接累加器和叶内联保实验开关，tuple有界默认取舍见各专项归档。
- 本次没有修改生产源码、没有新增优化或选择性改规模。固定输入/原始不利样本/后台tier边界均保留。

## 来源与口径

- 历史编译器：e1c1ee1 已保存的 Rust **debug** CLI，不重建历史源码。
- 当前编译器：已通过完整门禁的 dirty 源码，`cargo build --release -p ryupy-cli`；`source.patch` 同时包含 tracked 和未跟踪 crates/runtime 非 pending 源码，`base-commit.txt` 为基点。
- 两方运行统一 `binary-hashes.txt` 中 Release `RyuPy.Runtime.dll`，每个编译产物复制后校验哈希。编译产物差异属于编译器，而不是 runtime 混版。
- Rust CLI profile 不同，因此 `compile_ms` 仅诚实记录，不可据此比较编译器速度。Rust profile 不改变被发射 CIL 的 .NET JIT 优化等级。
- 固定 CPython 3.12.13，原 26 个输入规模不变，3 个旧 pending 同样尝试。不把只可导入当兼容，也不扩展为固定项目整体提速。

## 原 suite（冷墙钟与含 JIT 的 main）

`preparation/run_saved_suite.py` 复用原 bench 框架，只固定保存 CLI/独立输出/同 runtime 并额外保存每个进程完整 raw。每项 1 个独立热身进程、3 个正式进程，保原 PIC 关闭变体。default 清理外部 tier/JIT 开关；disabled 只设 `DOTNET_TieredCompilation=0`。

执行顺序：current-default、e1-default、e1-disabled、current-disabled。不是逐 benchmark 交错配对，因此不能将整套先后漂移直接归因某个 pass。每项仍按原框架在 CPython 与 RyuPy 之间逐轮交错。

- `wall_ms` 包含新进程启动；`main_ms` 仍包含该进程 JIT，不是同进程稳态。
- 原 suite 的 `alloc_bytes`/GC/JIT 是原运行时统计范围，不能与另表 kernel delta 混用。
- `raw/process-*.json` 保每次 wall/RSS/stats/env/command，stdout/stderr 分文件；原框架结果只保最后 stats，因此汇总脚本从完整 raw 重算正式三样本中位数。
- `suite-summary.json/md` 为辅助汇总，不取代原始日志。首次测量的日期文本遵原工具本机时区；`observed-utc.txt` 单独记录 UTC。

## 四核心 kernel（另表，非原 main）

真实编译方法由反射发现后绑定：fib/mandel 使用精确类型 delegate，matmul/nbody 少量顶层调用使用反射；不在每次算术内部走反射。加载原 main 直接输出原生标准流，外部 runner 将 stdout 的原 main 前缀与固定 CPython 逐字核验，再解析 kernel JSON。

每个进程先跑 3 次完整 kernel 热身，再 7 次正式 kernel；3 个独立进程，baseline/candidate 轮转，两 tier。每个样本输入恢复与校验排计时外。单独资源进程记录 allocation、GC、JIT delta，不混计时。热身次数是固定协议，不仅据次数断言所有方法均达到最终 JIT 层，实际 JIT 证据另存。

- fib：真实 fib_rec(30) 与 fib_iter(90)×200000，核两个整数结果。
- mandelbrot：真实 mandel 同 240×120、1000 上限；预分配网格，核完整输出；排 Python 原 main 字符串拼接/print，因此不能冒充原 main。
- matmul：每样本 make_matrix(100,1/2) 重建，计时连续 3 次真实 matmul，按原顺序核 checksum。
- nbody：每样本 make_bodies/offset_momentum 重置，计时真实 advance(0.01,100000)，核初末能量。

CPython kernel 为同源函数调用协议，但调用约定/循环宿主不同；它是解释器与已编译 kernel 的独立对照，不是四个原 main 的替代数据。

## 整套主要比较的追加时序校正

原顺序组完成后，另按预先固定规则执行 `run_paired_suite.py`：**不重编译**，每 benchmark/每 tier/每原有 variant，1 轮热身加 3 轮正式，每轮 B/C 交替，跨 benchmark 再反转起始顺序。每次仍逐字核对原固定 CPython 输出。完整 `paired-suite/` 是整套主要比较；`suite-summary` 的原顺序组保留，不抹掉不利数据、不事后按 CPython 漂移归一化。

## 诊断工具修正记录

首次 /tmp harness 使用 Console.SetOut 捕获，但运行时原生 stdout 使用自身缓冲writer，实际打印了正确答案而捕获串为空，触发工具校验错误。`core-validation-beforecapture`保留失败日志/原工具源码；后改为外部逐字校验原main输出前缀。两方四项真实方法的独立验证均通过后才开始正式kernel，不改生产或期望。

资源计数采用全进程 GC/JitInfo 在 kernel 前后差值，不是每方法归因。首版资源进程在样本间JSON输出，实际 fib/default 第三样本出现后台JIT增量而前两样本为0；该事实保留，不能仅因完成3轮预热便宣称默认tier整个进程不再编译。禁tier与真实方法JIT文本用于分别说明边界，正式时间不同时启用JitDisasm。

## 真实JIT终态

计划内16个独立JIT采样进程全部通过，`core-jit/comparison.json`保存完整Diffable文本hash、方法版本与代码字节。四core禁tier两方全文相同；默认matmul/nbody全文相同，fib候选多一个Tier1版本、mandel最后Tier1代码877/884B并非完全相同。它们说明实际采样的编译层次/原生代码边界，不能把时间波动全部归因新的算术算法，也不能反过来把不利样本删除。默认资源进程有后台JIT事实仍保留；最终只称同进程固定预热kernel，不保证整个进程完全不再晋级。没有启动JSON末尾版复测或其它追加实验。
