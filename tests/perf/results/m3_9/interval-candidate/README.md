# M3.9 有限区间候选：已知合流改写获益，未知溢出对照不利数据保留

候选/tmp/ryupy-m3_9-interval-candidate-AznvQW，基线/tmp/ryupy-m3_9-interval-baseline-QAjoC0。
默认holder+TupleScalar、叶/直接累加实验均未开；candidate默认新增有限区间改写。runtime逐字节
一致，hashes.txt锁实际产物。source.patch含捕获时crates/runtime全部tracked/untracked源码差异，
manifest锁来源，排.pending/results，无二进制归档。旧CLI与当前runtime的组合来源见基线目录。

固定3.12.13及两真实编译体七场景语义/类型通过。choose的3/7合流与loop_diamond中的相同
运算实际变unbox_exact_i64→Add→box，不添加类型失败守卫；unknown参数仍dyn.Add。不是只把
名称叫interval就假称命中。loop形旧实现本来不匹配strict holder，call形有holder，两者分别与
自己的基线比较，不把两形直接当同成本循环。

## 两组交错正式结果

每组默认/禁tier各3进程、每进程3热身7样本、20000循环，B/C轮转；先每进程中位再跨进程中位，
wall单列。所有原始样本保留，absolute-times.md给出绝对ms和每轮ns。未隔离CPU或固定频率，
不挑好的一组，也不直接把未知路径回退叫噪声。

| 场景/模式 | 第1组变化 | 第2组变化 |
|---|---:|---:|
| call_true / 默认 | -15.5% | -38.0% |
| call_false / 默认 | -26.2% | -29.1% |
| loop_true / 默认 | -17.7% | -48.4% |
| loop_false / 默认 | -10.2% | -44.4% |
| call_true / 禁tier | -24.3% | -5.4% |
| call_false / 禁tier | -6.9% | -2.1% |
| loop_true / 禁tier | +8.1% | -29.8% |
| loop_false / 禁tier | -17.9% | -68.8% |
| unknown_overflow / 默认 | +23.6% | +9.8% |
| unknown_overflow / 禁tier | +32.8% | +54.6% |

未知int/bool全部值也保存在summary.json，不把对照删掉。已改写点在两组默认tier持续获益，
禁tier大多数获益但对照波动较大；未知overflow有重复不利数据，必须结合下面实际代码区别，
不能断言全负载无退化或全部归于系统噪声。

## 未改写函数的精确证据

unknown_loop完整原始CIL hash相同。choose_unknown原始CIL不同，**唯一**差异为IL_001c的
PyPic.Binary site常数3→1，因为前两处二元站点改写后不再分配PIC编号。原始文本/rawhash全部
保存；unknown-site-renumber.json仅对这一已核唯一指令做规范化，其余逐字完全相同，不泛化
抹除任意数字/诊断或其它差异。

实际JIT也分别检查，原始asm、hash和diff均保留：
- 禁tier choose_unknown的FullOpts全文在Diffable地址模式下hash完全相同，均690B。
- 默认Tier0/InstrumentedTier0只有上述site变化；但Tier1有**实质不同**的PGO/内联选择：
  B1585B、C1024B，fgCalledCount25964/10272，分支和inline报告不同。不能说默认机器码未变。
- 禁tier计时回退不能用默认Tier1的PGO差异来解释。没有硬件计数器、未作因果隔离，保留不确定性，
  不为使结果好看调生产或追加无穷复测。JIT诊断单独运行，不能把其具体版本直接等同每个正式
  计时进程当时的版本，但它揭示了默认层次选择确可能不同。

## 分配与已改写点机器码

独立harness20次预热、5批20000循环、两个tier各3进程，B/C所有批次相同：
call_true/false、unknown_int/bool 64 B；loop_true478776 B；loop_false479328 B；
unknown_overflow3360032 B。没有分配下降，未把同一small-int缓存收益冒充消除新分配。

FullOpts choose697→292B、loop_diamond1493→1099B；已知+2处多类型PIC路径被移除，unknown
保动态协议。默认全部分层版本也归档。不仅凭源码声称JIT变化，更不把本微基准外推项目吞吐。

## 重现与状态

基提交+source.patch构建候选CLI，默认编interval_diamond.py到artifact；以同runtime配旧baseline
产物。compare.py传baseline=目录、candidate=目录、--output新目录。harness及diagnostics.py在
另行进程采分配/JIT，不与正式计时并行或覆盖原数据。完整生产gate/最终默认政策由父决策，
此报告仅专项证据；未提交。所有测量已结束，不再追加特性或救测。

## 收尾只读核查：未知溢出风险仍未归因

overflow-sample-review.json保留24个进程各7样本的审查摘要。持续回归是两组聚合值事实，不能
自动抹成正负噪声；但也不是每个候选进程都慢：例如第二组禁tier候选进程2为4.559–6.313ms，
与基线约5ms相近，候选进程0/1中有10–18ms峰值，推动跨进程中位。样本中既有后半变慢也有
前半变慢，不支持简单“第一点JIT冷启动”解释，不能剔除峰值。

禁tier choose_unknown的690B实际FullOpts文字在Diffable地址模式下完全相同，因此其调用、栈
布局、指令序列均未发现差异；这是仅对该方法文本的证据，不等于整个进程地址布局/GC状态或
被调runtime方法都相同。unknown_loop的CIL相同，但本次未单独采其JIT，不冒称整个链机器码
都相同。默认层次的PGO差异已在上文明确，不能拿它解释禁tier观测。

正式性能进程未记录GC计数/停顿或JIT事件（独立allocation诊断只测分配字节并主动GC），因此
不能从现有证据判定是否前置已知场景加快后改变了末尾unknown_overflow的GC/层次化时间窗口。
所有进程按七场景固定顺序执行，unknown_overflow最后，故这一程序级时序效应是未排除的混杂
因素，而非已证原因。分配总字节相同不等于GC发生时刻相同。当前不追加救测/生产修改，最终
默认决定必须显式记录此对照风险；只凭已改写点收益不能盖掉它。

补充授权的唯一caller诊断（无新正式计时）：另开禁tier disasm进程采unknown_loop，两版完整
方法文本（Diffable地址模式）SHA均514d60059e8c41220cdf968e42d3a14936ba56ef1441c0ead4214e2cc2a52f7d，
均515B、stack40B，调用/累加指令全文相同。unknown-loop-jit-hash.json与原始asm/stdout/stderr
及空diff已保存。这补齐了先前caller缺证：在该禁tier采样中caller与choose_unknown都没有新增
机器指令/栈布局差异。共享runtime相同、父核Binary的site参数不查数值形状缓存；因此不能以
“site编号造成cache碰撞”解释回归。仍未采GC时间/系统调度/真实地址布局，不能把本结论扩大为
正式时间回退已得到唯一因果解释；原不利数据与风险声明保留。
