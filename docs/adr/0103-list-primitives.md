# ADR-0103：精确 list 的必要可变协议修复

日期：2026-11-05。状态：正常完整门禁通过；未发布。

## 实际项目依据与拆分

干净177cd406上分别对attrs23.2.0和docopt0.6.2执行一个原入口窗口，未改包、入口、golden。
attrs的CP/golden一致，编译首E0100在_make.py:67的enum.auto；其余真实诊断及静态动态类生成
依赖另存，未进入JIT。docopt的CP/golden一致，但CP有原有四条SyntaxWarning共732字节，
原stderr未过滤；编译只报TokenStream(list)与Dict(dict)两项基类E0100，未进入JIT。
这两份当前证据代替历史首错，不把本仓库同名attrs微基准当PyPI项目通过。

真实list派生需要布局、分配/初始化、根描述符、身份、魔术槽与独立消费入口一起闭合。
源审与CP合同发现其依赖的精确list已有常用错误，因此经确认先拆本切修必要基础协议，
不以普通PyInstance假装list，不开放HIR基类准入。三份未来list派生应用及六份CP合同
仅在外部恢复证据保存，未放入活动测试，不计已实现。docopt的list/dict仍未解除。

## 本切支持与算法

- 连续step=1的空反向区间按实际count=0处理：赋值在start插入，删除无动作，
  不把负stop-start交给CLR RemoveRange。扩展切片赋值原先长度一致检查不变。
- 非连续删除始终按原下标降序；负步本来已降序，不再反转，避免下标漂移。
  仍是多次RemoveAt，最坏O(k*n)搬移，未宣称线性压缩或整体容器优化。
- 公共list.index缺失转换为ValueError并保repr回调，内部IndexOf继续返回-1给contains等使用。
  不额外扩index的start/stop参数域。
- list.extend与+=共用窄局部helper。精确list/精确tuple快路无用户回调、无全量中间数组；
  object背板用AddRange，self有限复制原长。numeric分支逐原元素GetItem/Append，
  共享已物化盒，不复制raw数值槽后再次装盒，不新增PyList字段或改变数值岛ABI。
- 其它source依次获取iter、查询原source的len或length_hint、逐项next并立即Append。
  中途异常保已提交前缀和回调副作用；不能先全量物化再一次提交。
- 长度提示先完整验证整数/ssize_t/负数规则，只预留至多4096额外槽；实际元素不设上限。
  同ADR0068的有界估计，不承诺巨大合法hint的分配失败时点与CP一致，不捕获OOM重试。
- list游标用负cursor哨兵记录首次实际越过末尾，不增对象字段，此前仍观察实时长度；普通用户游标一次Stop只结束
  本次消费，不永久封死其后重新iter/next的可能性。全局list迭代的原数值物化合同不扩。

## 回调、错误与边界

原source的len中TypeError（查找/绑定/调用/整数协议）可回退hint；hint仅调用的TypeError
被忽略，getter错误、非整数返回、溢出与负值均传播，NotImplemented使用默认估计。
每个回调恢复帧深度和ambient状态；用户异常保原对象/cause/context，Attach缺失帧，
仅本层新造协议错误PrepareRaise。显式iter(user)的隐藏适配器重用真实user再次执行iter，
新返回的游标只验证next，不再调用游标iter或误查其hint。

仅extend本地支持普通实例__getitem__序列回退；从0按序访问，IndexError/StopIteration
结束。不借此更改全局for/list/map/unpack支持域。复杂自定义descriptor明确NI，
普通function/staticmethod/classmethod/property按当前支持绑定执行。
PyTuple并非sealed，所以快路必须真实精确类型；不能吞掉namedtuple或未来派生协议。

真实list/dict派生、builtin list构造绑定、反向活游标、sort重入/比较回调、复杂/任意精度
容器切片索引以及通用调用重复**旧差异均独立保留。本切不是完整list协议或容器兼容完成。

## CP-first与回归证据

固定CP3.12.13的三原应用先3/3通过；冻结旧Release d823383e后原源0/3，分别在负步删除、
extend事件顺序及耗尽游标复活处失败。原源/golden/双流/命令/退出码/hash全保，未改预期。
首修后同原源3/3通过：两个成功应用exit0/空stderr，错误链应用exit1且stdout与完整六帧
cause/context投影逐字匹配。另保隐藏用户游标重新iter及getitem回退的旧失败与修后对照。

首版独立C#16项通过；相关旧回归185过/1败为numeric迭代内部背板断言，身份断言未失败。
审查选择撤掉非必要的Current→GetItem优化，恢复原Items读取，保旧oracle不动；
不把表示策略变化靠删旧断言变绿。首版证据保留，最终候选需重做专项及完整门禁。
性能、最终计数和门禁在完成后补记，不用中间green替代最终代码。

独立审查与生产回归同时发现新helper直接读取旧range.Length会令合法少量极值range因中间
long算术溢出而泄漏CLR错误。原候选失败保留；本API局部改用BigInteger跨度/绝对步长求count，
再检查ssize_t上限，不修改全局range.Length或迭代器，不耗尽巨大范围。两种单元素极值、
四元素大跨度及正常/关闭StringIO均与CP原源对照，相关旧正常行为未退化。
最终独立专项19项加相关旧回归170项共189/189，零失败/跳过；旧numeric断言完全未改。
新增StringIO测试曾遗漏CP消息末尾句号，按已冻结CP精确补点；原失败日志仍保存。

## 最终原应用与真实性能

最终Release 2d268827原pending源3/3，pending仅换空行转正后再3/3；同一六帧及
cause/context、stdout、golden不变。前版824ffa通过不代替这个最终候选。

性能只比较旧/新均正确的正常路径，不拿旧错误输出作“更快”基准。同Python源由真实CLI
生成同一CIL DLL，预绑定CreateDelegate调用同一host；计时内无Reflection.Invoke。
每例预热200次，7个自适应样本；关闭TieredCompilation，输入/盒/target容量预建，
保内容、引用身份和源不变。静默ABBA四进程各16例通过，不是64个不同功能或项目通过。
8与65536元素分别测exact-list和range源extend/+=、list循环、index命中及两控制组。
每次target Clear+非空prefix复位计入，不扣控制组，不称纯helper时间。

exact来源extend/+=的8元素分配88→0托管B/op，65536元素524312→0，符合去除整数组快照。
65536元素extend完整调用中位160.2→22.4微秒，+=153.9→22.7微秒，与22.5微秒左右
bulk/reset控制接近；这些是此预留容量工作负载观察，不是所有列表/项目的倍率。
range分配496→232及3146152→1572904托管B/op；小range extend中位345.6→381.8ns，
约10.5%回退如实保留，加入长度提示与正确流式协议有成本；小range +=341.3→334.9ns。
大range自适应样本迭代数与GC次数差异明显，旧extend范围1.16–5.85ms、候选1.88–3.28ms，
不从合并1.05倍宣称稳定收益。loop72B与index24B均不变，哨兵没有增加游标字段/分配尺寸。

共享CPU未绑核，不能承诺无回退、统计显著、全应用/AOT/跨平台速度。B/op仅当前线程托管
分配，非RSS/栈/原生内存。完整逐样本、GC计数、控制漂移、命令和hash随恢复证据保留。
同generated DLL SHA256为1bd891b90c963f0424a98c02ea2942ea44de48cbdb12931d947698246295bcb2，
同host为d17232a60cb85def068eb2a590b8dc7fdbe8d47adca14e6da71f886afb9ec29f。

## 最终正常门禁

最终源码首次正常make check exit0：1877 C#、228 CP、202 e2e及30原ignored，
fmt/clippy/workspace全部通过。最终Release哈希未变。check.log SHA256：5164236c8494a02a5a2449a5725779e5f81b45b4bd1a81ac21bd9b2f12028808。
未为本切再跑docopt/attrs；两者真实基类/Enum阻断未解除。未push、未扩项目矩阵/AOT/跨平台。
