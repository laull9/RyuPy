# I64短Add链：固定同机baseline/candidate配对（已批准冻结，未运行）

## 候选与语义

2026-11-02父确认本固定范围/预算/阈值。baseline为`457886017cd4d32911a09b09307d07c327b87192`；candidate必须源码审/固定CP与MIR/PE门禁通过后才冻结。两个独立checkout、各自CLI/runtime，不给baseline换candidate runtime，不新增永久用户优化开关。现41407684705的81进程是历史背景，不能拿不同jobrunner时间作本轮baseline。

本轮原calls.py中的positional(a,b,c)只有两Add；本轮原MIR与ASM1738B实际确认。当前整数路径仍有中间盒，直接call而非binder。支持EH只Add方案：原F64优先不改，失败边exactboxedlong全输入guard→I64逐步WrappingAdd+overflow→最终BoxInt；任一失败唯一原DynBinary整段重放。

无溢出最终Box沿PyOps.BoxInt/PyInt.Box原smallcache；不得返回输入原盒替代新结果。溢出后BigInteger回落Normalize的盒身份必须由原慢路决定，不能只看最终数学值重新Box。bool/CLRint/BigInt/custom不当exactlong，输入只求值一次、保原帧/行/递归及回调次序。

## 固定矩阵与预算

1. 原四kernel+hello全部源hash、输入、3warm7sample不改：每版本45cold+36hot，两个版本共162被测进程。
2. 额外一个独立guard源码，含同一两Add函数的4个固定场景：全部exactdouble、bool/int混合、custom反向Add且日志/次数/身份校验、首步longmax+1后加负数回i64。每场景每调用100000次，3warm7sample，场景输入预先固定，不根据首结果降规模。custom最终对象身份与overflow最终值在计时后校验。
3. guard只hot，每版本三后端各3进程=18进程；每进程四场景，7条记录/场景。guard与原四kernel分开报告，不能把guard分母混入81基线。guard整进程分配含全部四场景，不能按标签分摊假精确。
4. 全组180被测进程（90cold、72原hot、18guardhot），1008条hot样本；六源码×两版本=12次普通ILbuild+12次AOTpublish，另两版本CLI/runtime初始构建属于基础准备，单列成本。
5. 总monotonic1800秒硬诊断预算，单build/publish180秒、测量进程120秒；不新增第二轮调参、扩大预热或剪掉失败场景。两checkout串行构建、同一普通CI runner；runtime测量必须所有build之后，期间无其它job内任务。

如果主认为成本过大，可在开始前拒该整组合同；一旦冻结执行，不因结果改变上述场景/次数。正常已观察单publish约10秒，12次约2分钟是估计，不保证；首次restore缓存/联网影响仍如实披露，不新增预热发布。

## 轮转与同CIL

每源码/version由公开build --aot取得本次DLL+native；该版本JIT默认/禁tier均执行此次AOT输入DLL。普通ILbuild只记录其hash/成本，不冒规范化等同。六臂命名B-J/B-T/B-A/C-J/C-T/C-A。

三重复在同kernel上依次：
- r0：B-J,C-J,C-T,B-T,B-A,C-A；
- r1：C-T,B-T,B-A,C-A,C-J,B-J；
- r2：B-A,C-A,C-J,B-J,B-T,C-T。

kernel顺序按repeat循环移位；同backend的baseline/candidate相邻，先后有反转，不先跑完所有baseline再candidate。保原fresh-process/OS页缓存已暖与固定预热非Tier1稳态口径。

## 逐条证据与源码强命中

复用已审阻塞wait4/独占PGID/watchdog/rawbytes/schema/逐checksum规则，缺stats立即失败。每个source基线固定CP3.12.13，全部原正期望保留。baseline与candidate各留types/MIR/CIL；driver强断当前calls候选真命中，I64成功只一次BoxInt、每步overflow检查；guard各场景也必须命中同候选，不允许因源码屏障被pass拒绝假验慢路。

仅静态分析本轮已经产生的native/.dbg：positional实际机器码大小、call/分配helper/guard。AOT整数链中间RhpNewFast应消失，最后Box/cache保；完整函数仍可能含F64/慢路RhpNewFast，不能只数整函数allocationcall数量判断快路失败。无新增timing/disasm进程干扰矩阵；若后续需要实际JIT asm，单独诊断无计时且不新增性能样本，须主指定函数。

## 事前准入阈值（已批准冻结）

- 正确性任一差异立即停止；不改原期望。
- calls整hot进程分配相对baseline至少减少20%（三后端分别核）。该指标是工作集总分配，非逐call精确模型；减少不够先判未证明目标，不改规模凑数。
- calls热kernel：AOT及禁tier两者各至少5%中位改善；default不允许三独立进程均慢且中位>5%。默认tier状态未量化，不能借“还没Tier1”忽略实测不利。
- 原matmul/dataclass/exceptions、四guard热场景：任一后端若三进程中位点均慢且综合中位>5%，默认准入阻断。数据重叠/单异常点仅如实列为不确定，不宣称无回归；不追加测量选点。
- 五场景cold wall几何比任后端>1.05必须审，不将热收益自动抵销冷启动成本。hello单列，不凭三点定显著性。
- actualpositional native代码相对baseline增长>25%或整个native文件增长>1%触发资源准入审查；阈值是预设工程预算，不是物理性能定律。构建时间完整列明但不把一次restore差当编译退化。
- 全满足才有默认候选资格；未满足可以拒本优化/保未合并草稿，不靠新增用户旗标把负收益藏起来。若有唯一机器码根因，主可另批准一次窄修与完整原矩阵复核，不能由worker自动重跑。

当前为已批准测量合同，尚无本轮构建或测量；须先通过候选语义与结构门禁。不得结果后修改阈值，不重开M3.9实验分支或修改原81基线。
