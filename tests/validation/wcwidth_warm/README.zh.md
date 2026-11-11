# wcwidth 暖kernel有限可靠性门

精确标题 `validation: wcwidth-warm-kernel-001` 增加独立warm reusable job，并选择原metadata门。baseline、projects、双fullcheck、S1、Rust、S2、pyfiglet、来源保护原依赖与if不变；不让warm失败跳过它们。A1、其它未选择专项不冒通过。metadata完整Markdown AOT的既有红界不豁免，整轮可能仍失败。主workflow579行，不豁免600规则。

kernel.py从独立CP3.12.13已核的非计时合同派生：保留所有函数体，仅新增warm函数、sys.argv模式和准确介绍；EXPECTED.txt为原438B，SHA80cc7511e7997789f1ca183137f24887bbcb41f9561bcacf7e65bfdbe89f6ac6。没有新CP黄金/计时实测。原四调用点实际只用wcwidth、wcswidth两个API；100轮checksum(300,400,200,100)。三LRU上限1000/8/128，最后一层本负载没有调用，不称三层全热。

单候选只公开AOT构建一次，无pilot feature专用CLI。实际manifest绑定managed-input和Runtime，入口PE解码身份及真实Program源码核实后，dotnet运行同managed输入contract，再同ELF原位、搬移各一次。C3逐字438B且stderr空才M6。完整图、源码hash、SDK及工具、ELF、Runtime、managed依赖冻结。没有另造静态Host框架或默认能力变更。

M6每进程初始化一次，4个非计时暖batch+12计量batch，每batchN100000。计时仅两次既有perf_counter夹住真实kernel；checksum/cache_info/输出在外。每batch四调用点共400000次，wcwidth缓存访问800000次。checksum(300000,400000,200000,100000)，外层miss8、版本匹配3hit/1miss、数值版本层零，严格检查。4暖batch不保证稳定。

门：72点均有限且>=100ms；36对标签及实际second/first分别median[.95,1.05]、至少29对[.9,1.1]、严格同向最多27；每进程后6/前6 median[.95,1.05]；每进程median偏离总体median<=10%。工程门不是显著性检验。C每child30秒；M每child min(60,remaining)、全M600秒launch/child/结果检查；超时杀进程组并wait，不声称任意挂起I/O的OS硬终止保证。无补采、改N、丢异常点、跨后端比率或提速结论。

执行与归档使用同一明确允许环境，不传递或记录TOKEN/SECRET等任意继承键。原双流及退出状态先保存再判；失败RESULT仍passed=false。上传前再核环境键；如安全检查失败停止上传，不换路泄露。纯反例不代表产品执行。新kernel编译、cache API实际访问以及Native合同行为仍须首次hosted证明。

本候选在任何新产品/计量前由N20000改为100000，仅操作预算放大5倍降低过短风险；没有耗时实测依据、不保证通过，60秒child/600秒总预算不变，超时如实失败。始末和C前核HEAD/tree/clean；初始工具、C前及最终同hash。小输入清单不冒全仓字节冻结，完整dist含目录mode/文件集合、size/hash/mode实核等价。
