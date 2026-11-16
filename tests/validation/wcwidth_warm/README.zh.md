# wcwidth 暖kernel有限可靠性门

精确标题 `validation: wcwidth-warm-kernel-001` 增加独立warm reusable job，并选择原metadata门。baseline、projects、双fullcheck、S1、Rust、S2、pyfiglet、来源保护原依赖与if不变；不让warm失败跳过它们。A1、其它未选择专项不冒通过。metadata完整Markdown AOT的既有红界不豁免，整轮可能仍失败。主workflow579行，不豁免600规则。

kernel.py从独立CP3.12.13已核的非计时合同派生：保留所有函数体，仅新增warm函数、sys.argv模式和准确介绍；EXPECTED.txt为原438B，SHA80cc7511e7997789f1ca183137f24887bbcb41f9561bcacf7e65bfdbe89f6ac6。没有新CP黄金/计时实测。原四调用点实际只用wcwidth、wcswidth两个API；100轮checksum(300,400,200,100)。三LRU上限1000/8/128，最后一层本负载没有调用，不称三层全热。

单候选只公开AOT构建一次，无pilot feature专用CLI。实际manifest绑定managed-input和Runtime，入口PE解码身份及真实Program源码核实后，dotnet运行同managed输入contract，再同ELF原位、搬移各一次。C3逐字438B且stderr空才M6。完整图、源码hash、SDK及工具、ELF、Runtime、managed依赖冻结。没有另造静态Host框架或默认能力变更。

M6每进程初始化一次，4个非计时暖batch+12计量batch，每batchN100000。计时仅两次既有perf_counter夹住真实kernel；checksum/cache_info/输出在外。每batch四调用点共400000次，wcwidth缓存访问800000次。checksum(300000,400000,200000,100000)，外层miss8、版本匹配3hit/1miss、数值版本层零，严格检查。4暖batch不保证稳定。

门：72点均有限且>=100ms；36对标签及实际second/first分别median[.95,1.05]、至少29对[.9,1.1]、严格同向最多27；每进程后6/前6 median[.95,1.05]；每进程median偏离总体median<=10%。工程门不是显著性检验。C每child30秒；M每child min(60,remaining)、全M600秒launch/child/结果检查；超时杀进程组并wait，不声称任意挂起I/O的OS硬终止保证。无补采、改N、丢异常点、跨后端比率或提速结论。

执行与归档使用同一明确允许环境，不传递或记录TOKEN/SECRET等任意继承键。原双流及退出状态先保存再判；失败RESULT仍passed=false。上传前再核环境键；如安全检查失败停止上传，不换路泄露。纯反例不代表产品执行。新kernel编译、cache API实际访问以及Native合同行为仍须首次hosted证明。

本候选在任何新产品/计量前由N20000改为100000，仅操作预算放大5倍降低过短风险；没有耗时实测依据、不保证通过，60秒child/600秒总预算不变，超时如实失败。始末和C前核HEAD/tree/clean；初始工具、C前及最终同hash。小输入清单不冒全仓字节冻结，完整dist含目录mode/文件集合、size/hash/mode实核等价。


## LRU双字符串分配对照（新增门，尚待hosted实测）

`allocation_runner.py` 由同一 reusable workflow 的独立 job 执行，不能阻止原 warm 或项目保护调度。
固定旧公开完整 commit `6923573d78f7b504dcbe6d6067dd145cd1a7bd65` / tree
`c27b271f3223e215a3c4fcc1618664ad3acf7968`；只准三个Runtime内部文件差分。
同一官方 SDK、Release、TraceAdapterTest=false；旧/新 Runtime 独立构建，十新Fact和原LRU/functools通过后，
以旧Runtime编译一次公开ABI Host，字节相同的Host在旧/新隔离目录执行。实际程序集位置/版本/SHA与磁盘相核。

预构造参数与稳定返回，capacity128，两短CLR字符串、typed=false目标与typed=true控制；各先填一次、预热20000次。
ABBA四进程，各域三批100000次，24样本。区间只有真实Invoke/最后返回与线程分配计数；
精确hits/misses/返回identity在外检查，不强制GC、不调tiering/亲和性。
6个同版本同域字节计数必须相同、typed控制旧新相同且目标严格减少，才报告此微核B/op；
首批不丢、失败不重试/补采，不能外推kernel、Native分配或速度。本轮无时间A/B。
job35分钟硬限，各构建/测试300秒、Host120秒、单进程60秒、测量总300秒；
新增两Runtime构建/一个Host构建、四计量进程，无额外CLI或ILC。最坏所有子上限接近job硬限，
准备/上传超额将如实形成不完整门，不延长或偷偷重跑。

原warm的C3/M6统计输入、阈值和计数不变。C3三合同全成功后、M6 deadline及六进程之前，
以同普通CLI/Runtime一次编译、执行仍标pending的
`tests/e2e/stdlib/functools_lru_string_pair.py`，对已冻结CP19B和空stderr；保存真实PE及Runtime，
编译域不支持也是红，不删标记、不改为单独CP通过；pending失败则M6未运行，原样记录。
此顺序是功能先行，避免M6噪声阻挡功能验收；不拿旧暖时间做新A/B。不为该fixture扩大AOT类域。
metadata消费者现在接受既有warm精确标题，并经真实check_context反例验证；后继既有产品红界不豁免。
