# ADR-0041：证明支配下省去列表读取的重复逻辑范围检查

状态：最小实现、源码/实际机器码与最终完整聚合门禁均通过。

## 不能把内联冒充边界检查消除

ADR0039最终机器码虽内联CanRead/Read，却仍在读取时重复显式范围判断：
归档numeric-list-revised的matmul FullOpts中，IG32/33预检之后，IG62/66再做
normalized<count及List/数组检查；另一叶IG53/56→IG73/77同样重复。
原第4项明确包含边界检查消除，不能靠“不消除所有检查”的措辞冒称至少已消掉一个。

## 最小决定

增加仅供编译器受控区域发出的NumericListReadCheckedF64(Dynamic,Dynamic)->F64。
它接受已经证明actual sealed PyList、actual boxed long、规范化范围内、元素actual double
的同一局部对；只删除第二次显式normalized<count判断。
负下标仍按当前Count归一化，下层List/array安全检查保留；不用unsafe、Span或裸指针。
一般NumericListReadF64防御API不变，trusted入口不作为通用Python调用接口。

编译器在现纯成功区域替换读取，不改布局/算术/CFG/匹配预算，不引入Nullable/union新ABI。
独立must验证器确认：

- 同一list/index LocalId对的CanReadF64真边支配；值相等、别名或不同index局部不算证明。
- list/index/condition写入杀相关已成立事实和谓词来源；不传播Bool Use别名。
- 未知调用、潜在用户协议和写入杀全部事实与**尚未分支的谓词**，
  防止先CanRead、后callback改变列表、再按旧bool读取的漏洞。
- 只明确纯Peek/Can/Read/CheckedRead白名单保留事实；普通GetItem不算纯。
- 合流取交集，未访问/不可达不当证明，回边完整固定点；EH/暂停首切拒绝。
- ABI与F64目标独立校验；非法MIR在发射前给Diagnostic，不靠CLR异常兜底。

同一CanRead事实同时证明元素类型与边界；仅证明index范围不足以直接拆double。
正常Python入口不能绕过该MIR验证。原list身份、NaN、负索引与callback差分必须保留。

## 验收与有限测量

强负例含错receiver/index、改pair/cond、callback后旧bool、真边后写入、diamond缺一边、
回边失效与不可达；正例含多个只读探测/读取不误杀。
只读现有目标before与新actualJIT必须指出第二显式cmp/错误分支消失的具体位置，
不能以函数改名、源码删除if或无helper调用代替机器码证据。

只为此原验收缺项做一次既有matmul短配对（两tier、三进程×三样本），
资源/JIT独立；不重开numeric-list性能救测、不改其它优化。
然后完整代码门禁和一次预声明全26冷覆盖。结论最多是有限重复逻辑检查消除，
不是List/数组所有安全检查都已消失或通用循环边界证明已完成。

## 已取得的实际证据

独立must验证与负例、runtime16项、MIR93项、PE ABI及两个真实数值岛driver均通过。
另封闭CallWithArgs对可信ABI的绕过：Assign/Eval形态均直接拒绝，不能避开must证明。
最初focused恰读取并发补丁中间态的E0308日志保留，冻结后完整focused重跑通过。

证据目录为`tests/perf/results/m3_10/bounds-check/`。实际FullOpts before的IG62
`cmp r9, rdx; jae IG68`通向ReadError(2)；after对应IG73/81不再重复long范围比较，
仍有List大小和托管数组长度比较及CORINFO_HELP_RNGCHKFAIL。保留CLR类型检查，未使用unsafe。
只认这一层重复逻辑检查消除，不称所有bounds消失。

同一matmul体、同新runtime的预定20进程全部成功：默认73.4273→60.9257ms，
禁tier75.9249→69.9619ms；分配均1,278,672B，机器码4563→4693B（增加130B）。
小样本只支撑本切，不外推整套；before抓取时源码已并发编辑的快照限制在README明确，
旧matmul体另有完整准确来源。没有追加第三轮列表调优或重测其它负载。

最终`make runtime && make check && make projects`整体exit0：434个C#、225个固定Python基线、
199个编译e2e/30 ignored，项目5 native/14 unsupported/1 failed（固定20）。
日志`/tmp/ryupy-m3_10-final-bounds-checkpoint.log`；既定26项104冷进程覆盖也已全部通过，冷总体不利结果见最终验收报告。
