# ADR-0039：自适应数值列表岛与身份安全物化

状态：正确性、唯一修正后的三臂性能与实际JIT已验收；接受当前有界规则默认，不代表通用union或整个M3.10完成。

## 为何重新讨论表示

用户要求动态 union 更积极地去装箱。ADR-0010 是 Phase 2 简化实现的选择，
并非容器天然必须存 object：可改变容器布局，而不是把 struct 再装进旧引用槽。
历史正文与数字不改写；新的独立证据见
[表示评估](../notes/2026-11-01-m3.10-dynamic-representation.md)。
16B、24B 均已证明 CLR GC 合法；数值数组有收益，但纯引用密度、24B 按值调用、
跨旧 object 边界双重装箱有明显代价，且天真复制后各自物化会破坏身份。
因此不盲改全 ABI，不以暂未实现的全局 origin-ID 宣称身份已解决。

## 有界决定

普通 PyList 仍以 List<object?> 存8B引用槽。首版包装对象只保单个 object 背板字段；
首轮性能阻断后，唯一修正版改为两个互斥 typed 引用字段，每个包装对象增加8B，
以保普通热路径直接访问 typed List（见下方首轮证据），并非宣称额外字段无成本。
编译器仅为同时匹配新鲜数值生产端和精确列表读取端的空列表选用数值工厂。
私有16B槽为独立 GC 引用/标签与64位数值位，不重叠引用和标量。
本轮仅 F64，算术仍由 MIR/CIL 的 typed 运算执行，运行时只提供固定容器操作。
逻辑动态类型不是每个热局部都要传实体 struct：已有原生 F64 局部继续保留。

### 身份与降级

- 仅严格 FloatLoop 的 dirty 新算术结果、且唯一外部消费者是该 append，才 raw 追加。
  初值、零轮、慢路返回共享盒都走原 object append，不能重建身份。
- raw 槽首次通用读取时装盒一次并缓存；之后同槽返回同引用，包括 NaN。
- 普通 Append/SetItem 传入的 boxed double 保原引用，不当成新鲜标量。
- Items 暴露可变 List 前，先完成全部槽物化与后备构造，成功才发布，永久降级。
  copy、slice、repeat、list 构造、拼接、迭代等须审计实际入口，复制前统一原槽身份。
  不能复制 raw 槽再各自装盒；不能回切数值模式使已暴露的迭代器/列表失联。
- Count 与纯预检不通过 Items，避免 len 使数值岛意外全物化。
  不改变现有 Python list 的游标行为，不新增 dict 式修改失效机制。

### 编译器范围与失败路径

在既有严格五块 FloatLoop 上，生产端出口只允许 Line 后的 row.append(total)。
row 经最多16层唯一 Use 追至空 MakeList，有限支配证明确保定义先于使用。
total 与末临时做全函数读取/逃逸审计，后继块、别名、闭包或其它观察均拒绝。
消费端只匹配算术链前连续最多4次 GetItem、最多2层的 DAG；除 total 外的链输入
全部来自叶读取，中间节点只作为下层根，索引是已求值的 Dynamic local。
不跨其它调用、行号变化或写入，不按函数名猜矩阵。

PeekList/CanReadF64 只检查 actual sealed PyList、actual boxed long、界限与 actual double；
bool、大整数和自定义 __index__ 回原路。负下标含 long.MinValue 不溢出。
预检不物化、不调用 Python、不抛用户可见异常；全部通过才做固定 ReadF64，
其间无用户回调/写列表。任一失败，原 GetItem 前缀按原序执行一次，
再接原 FloatLoop 守卫，不能提前抛后一次访问的错误，也不重复用户操作。

只有生产与消费计划同时成立且扩张预算通过才替工厂；否则完整保留既有 FloatLoop。
沿用最多16候选、最多4个成功岛，以及扩张后128块/512局部的界限。
不为本轮增加通用 SSA、全局 origin 表或其它容器布局。

## 固定运行时 ABI

PyNumericListOps：Create()->object；AppendF64(object,double)->object(None)；
PeekList(object,object)->object（失败 CLR null）；CanReadF64(object,object)->bool；
ReadF64(object,object)->double。MIR 明确参数/结果类型，validator 独立检查，
codegen 只映射 ABI，不知道列表身份规则。守卫成功读取的生产代码仍须核实际 JIT。

## 先验验证与停止边界

先 pending 差分与 MIR 强命中：重复读取/复制/NaN身份、共享慢路与零轮、
异构降级、别名迭代变更、负下标、首错阻止后回调、自定义下标替换子列表。
普通引用列表不只检查槽宽，还测新增背板类型分支的真实时间与分配。

预设一次三臂配对：旧编译体+旧运行时、旧编译体+新运行时、新编译体+新运行时。
两 tier 交错三进程×七样本，真实三次100×100矩阵生产/消费与普通引用控制；
分配/GC和实际JIT独立采样。至多一次有具体根因的修正，不重复全26套追求有利统计。
没有净收益则保原路径或收窄，不以少盒自动宣称默认性能胜出。
完整里程碑仍须原 M3.10 门禁；本原型不代表通用 union、SIMD 或80%兼容完成。

## 正确性验收记录

71项 MIR、真实 PE ABI、原矩阵五个数值 ABI 强断言与固定3.12.13身份/回退差分通过；
produce/indexed 两个差分函数也必须实际命中，避免因全局名字读取打断前缀而假通过。
既有两切6项 driver 继续通过。首轮暴露测试误用 Rvalue.clone，窄改测试构造后通过。

runtime先发现旧普通list/tuple的共享NaN元素比较与list成员查询缺少身份短路，
按容器契约补引用快判、保直接nan==nan为False；字典值比较独立pending未冒称完成。
完整差分另暴露旧list下标不调用__index__，仅补list的单次调用、整数校验和当前长度
归一化，超范围IndexError；自定义bool返回的警告协议仍明确不支持，不扩大其它序列。
两次中间失败日志保留，未靠删除身份/索引测试取得通过。

`/tmp/ryupy-m3_10-numeric-list-checkpoint.log` 的 Release、make runtime、make check
整体 exit0：433 C#、224固定Python基线、198编译e2e/30ignored。
这证明当前有界实现正确性门禁通过，不是性能已经胜出或整个M3.10完成。

## 首轮性能阻断与唯一修正

首轮完整三臂（A旧体旧runtime、B旧体新runtime、C新体新runtime）不能接受默认：
矩阵默认123.916/172.332/222.232ms，禁tier175.663/403.174/309.148ms。
普通引用读取B/A约+60.0%/+62.9%，追加约+47.0%/+27.0%。
C矩阵分配仅1,391,448→1,276,248B（-8.28%），不能覆盖时间退化；
微负载的两个结果列表反而多16B，bool/custom/mixed多64B，全部保留。

禁tier矩阵A/B caller机器码全文哈希相同（2879B），没有把runtime退化误归编译器。
C为3059B，热内层仍有Can/Peek/Can/Read/Read五次未内联调用。
源码另发现新list协议入口让普通long也先TypeName再通用整数检查，
单object背板则在Normalize/Count/ReadSlot/Append反复类型判断；
这些是明确待修热点，不把源码推断冒充已测每个callee机器码。

只允许一次针对性修正：双typed背板保普通引用热路（每list包装对象增加8B，槽仍8B），
恢复long/int/bool下标直接分派，冷协议/错误分离；数值intrinsic小热段让JIT内联。
不改MIR匹配/浮点算法/身份契约，不新增unchecked读取ABI。修后仍是同一三臂口径，
多因素一起修不作单变量因果归因；若收益不足则保原默认路径，不继续救测。


## 最终有限默认决定

最后一轮仍复用相同程序体，仅B2/C2替换修正runtime；所有语义、12资源、36时间、
12 JIT进程通过。矩阵本轮A/B2/C2默认106.016/94.828/73.657ms，
禁tier147.448/150.502/86.575ms；C/A改善30.5%/41.3%，对同runtime原路B改善22.3%/42.5%。
只在同轮内比较，不能把两轮不同A时间当修正的单变量收益。

矩阵净分配1,391,448→1,278,672B（-8.10%）；普通wrapper每实例+8B为全局真实成本，
303个矩阵list在B中多2424B。bool首失败C/A慢47.1%/19.0%，C/B慢19.5%/30.8%；
微负载两个小产物没有分配净收益。禁tier普通read的B/A+38.8%与C/A-13.6%同时保留：
两者runtime与caller机器码相同，原因尚未唯一分离，不称已证噪声或已排除普通路径回归。

实际FullOpts数值访问入口已inline，矩阵代码2879→4563B、micro4859→7450B；
冷错误调用保留。没有FMA，没有据源码声称所有守卫/界限检查都消失。
修后完整门禁 `/tmp/ryupy-m3_10-numeric-list-revised-checkpoint.log` exit0，
433 C#、224基线、198编译e2e/30ignored，独立必要源码/政策审查通过。

接受当前匹配、守卫和预算的有限默认：真实目标两tier净改善与身份正确性成立，
不是普通list所有用法或通用union ABI优于object的结论。不扩大规则、不进行第三轮救测。
首轮失败及最终全部证据分别在 `tests/perf/results/m3_10/numeric-list-first/`、
`tests/perf/results/m3_10/numeric-list-revised/`；SIMD仍须独立满足原里程碑验收。
