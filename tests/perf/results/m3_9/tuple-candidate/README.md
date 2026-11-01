# M3.9 tuple标量替换：默认holder上的成对结果

基线/tmp/ryupy-m3_9-tuple-baseline-KCoa1e，候选/tmp/ryupy-m3_9-tuple-candidate-TwzqCI。
本轮两者均默认holder，候选新增TupleScalar，无C#修改，runtime逐字节一致。source.patch是候选
捕获时crates/runtime完整tracked+untracked源码差异（排.pending/results），source-manifest.json
锁基提交、UTC和各文件hash；不重复历史直接累加器漏存新增源码的问题。hashes.txt记录实际CLI/
产物/runtime，归档不含二进制。基线源码、环境和固定13输出见../tuple-baseline。

## 配对测量

默认/禁tier分开，每变体3新进程、每进程3次热身7样本、20001轮；两变体轮转，执行两组。
全部保留在paired、paired-repeat，summary.json按每进程中位再跨进程中位汇总；wall时间单列。
输入、逃逸列表clear、语义验证在计时外，逃逸构造和保存仍计时。候选零/偶数/奇数轮、tuple新
身份及元素身份断言均通过，未删逃逸对照。未隔离CPU/固定频率，不将微基准倍数外推项目。

| 模式 | 负载 | 第1组 B→C(ms) | 第2组 B→C(ms) |
|---|---|---:|---:|
| 默认 | swap | 4.813786→0.043029 | 3.616688→0.043493 |
| 默认 | read | 2.332536→0.042595 | 1.807700→0.045726 |
| 默认 | escape | 5.061762→4.726146 | 4.058471→4.671469 |
| 禁tier | swap | 1.962887→0.045791 | 1.386932→0.045881 |
| 禁tier | read | 0.804725→0.039031 | 0.799081→0.039364 |
| 禁tier | escape | 1.677801→1.443653 | 1.645715→1.677705 |

无逃逸swap/read两组持续大幅获益。escape第二组默认+15.1%、禁tier+1.9%不能隐去；不过两版
完整escape方法CIL的SHA256完全相同（escape-cil-hash.json），FullOpts同为369B，分配也相同，
第一组反向更快。这里记录控制波动，不声称escape得到性能优化或已证明精确零时间差。

## 实际分配

独立harness/Program.cs复用输入/反射实参数组，20次预热后5批20001轮，两个tier各3进程。
候选数据在allocation.json；基线本轮另跑到baseline-diagnostics，旧记录保持不动。
所有批次B/C字节数一致：

| 方法 | B 每20001轮 | C 每20001轮 | 净消除 |
|---|---:|---:|---:|
| swap | 1280128 B | 64 B | 64 B/轮 |
| read | 1280064 B | 0 B | 64 B/轮 |
| escape | 1280088 B | 1280088 B | 0 B |

swap末尾返回tuple及元素数组的64B仍保留；read的0B仅是此真实方法/反射复用harness测得，不代表
整个Python进程零分配。escape列表在预热后保容量，所有20001个tuple仍真实逃逸，不能清除。

## IR与实际JIT，不把消除对象误说成更快构造对象

MIR原loop MakeTuple→Len→UnpackCheck→GetItem替换为冻结引用局部和Use；swap返回MakeTuple、
escape的MakeTuple仍在。CIL对应变化保留完整。FullOpts大小swap451→291B、read341→204B，
escape369→369B。

确认循环并未被整体消去：
- 禁tier swap IG03调用SetLine(9)，递增/比较index；IG04交换寄存器引用并回IG03。最终仅构造
  返回tuple。read IG03仍SetLine(16)、引用赋值、index递增和比较回边。
- 默认Tier1-OSR保留循环，SetLine内联成线程帧行号数组检查/更新；摘取swap-tier1.asm和
  read-tier1.asm，完整版本见jit-default.asm。不是依赖JIT删除全部无效循环捏造“每次tuple变快”。

正确结论是：这个20001轮微负载中，证明无逃逸中间tuple可被消除后，消除了逐轮数组/tuple分配
及运行时解包/索引成本；不是每轮仍构造真实tuple却提速83倍，更不是项目整体性能数字。

## 重现与状态

在独立基提交应用source.patch，cargo build -p ryupy-cli，默认CLI编workload.py至artifact，
再用本目录compare.py传baseline=基线artifact、candidate=候选artifact和新--output目录。
独立 `dotnet build harness/alloc.csproj -c Release` 后 `python3 allocation.py 新产物根` 可测分配/JIT，
须不与正式计时并行，且不覆盖本次数据。harness引用../artifact/RyuPy.Runtime.dll。
本专项测量支持该有界规则的收益，最终默认启用仍以父完整make check/语义审阅为准。未提交。
