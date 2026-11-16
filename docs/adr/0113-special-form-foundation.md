# ADR0113：ClassVar 前置 SpecialForm 缓存与反射基础

- 日期：2026-11-05
- 状态：有限域专项、正常完整门禁与有界性能通过；原项目待干净提交复测

## 目标与分切

G2干净功能提交a97c720的原Markdown3.5.2已编译成功，JIT首错为typing.ClassVar导入
缺失。独立CP3.12.13研究证明ClassVar与Optional/Union共用原始参数LRU，不能把现有
归一化FIFO直接复用或外套一层缓存。本F0先修必要基础，ClassVar本体仍为下一切，
不增加Markdown项目分子，不按库名或future注解作空导入桩。

## 原始键与共享缓存

Optional/Union移出旧Intern，进入以真实receiver和原始parameter为键的128项共享LRU。
None与NoneType不先归一化；等价不同Union原始参数不被旧驻留层错误合并。Union单值
仍返回原参数。Literal及容器/原生alias旧池不混入，Generic独立LRU不改。

hash桶与双向节点提供平均O(1)定位/触碰/淘汰，碰撞只扫描同hash桶，不扫描128项。
淘汰使用已存hash并解除两链/桶引用。热查找不新建key数组，但键长度仍有递归验证和
真正hash成本。entry、双链节点及桶有冷分配，不能因容量有界而声称零成本。

Optional冷miss先type-check，再真正订阅当前Union导出，canonical路线再进入同池；
内Union完成后才插入外Optional，通常占两项。只淘汰外项而内项仍存活时，可以返回
旧alias身份；两项均淘汰后才要求新建。保存的Union根自身仍用真实self作origin。
未知用户订阅回调在实际消费NI，不假称已支持任意重入；安全标准值按通用操作派发。

暖命中只绕过订阅函数体globals，键hash/eq仍真实消费自己的helpers。相同对象比较
短路与等价不同alias比较不能混同；native list[int]的eq不消费typing._GenericAlias。
首候选对此守卫过宽，已保留独立CP反例并修复。已知TypeError遵循_tp_cache未缓存
重试，未知用户hash/eq为NI，不冒无副作用TypeError。导出改绑不整体清空缓存。

## 类型与元数据

保留已驻留PyType身份，按现有root kind区分_SpecialForm、_LiteralSpecialForm、
_SpecialGenericAlias、_TupleType、_CallableType、_AnyMeta并提供typing模块显示。
不注册同名替代类型，不给普通实例属性热路新增祖先扫描。这里只修这些可观察元数据，
不宣称完整Any真实类/元类、内部类型构造/派生或所有原生GenericAlias反射已完成。

普通alias复用原_parameters单引用字段作惰性metadata holder，无反射的冷alias不
新增每对象字段或holder。首次args/parameters读才有holder/tuple成本；非Callable的
__args__及get_args返回同一稳定tuple。Callable公开get_args仍还原参数list。参数tuple
稳定、Generic专用alias不改。尚未实现的合法metadata/root mutation明确NI。

## 错误边界与原始失败

Optional tuple错误的repr只截前100个Python码点，复用PyText.OffsetOf，不按UTF16
单元或用Rune替换孤立代理。首候选emoji截断反例完整保留。通用str下标的无效键
TypeError文案也修正，不在Optional处特判；整数/slice成功路和其他容器错误不改，
不由此宣称完整__index__/大整数边界已修好。

旧基线六主CP/compile通过而JIT全部失败。首候选五主通过，saved_root只差旧str错误
文案，C#12/13。第二候选原六主及两份新支持域补证共八主严格CP/compile/JIT通过，
16项公开C#合同通过，独审两个真实阻断已复验解除。最初runner因golden尚未复制而
失败，未执行任何用例阶段，单独记录，不能记成RyuPy语义失败。

完整helper替换、alias mutation、ForwardRef/用户回调三个CP正例仍pending。
独审type三参数工厂探针暴露旧类型工厂缺陷，原65输入未真生成65类，不能误判LRU；
原完整源永久pending，独立TypeVar版已严格验证部分存活和完全淘汰。

新增完整error_message_boundaries保留Python孤代理输入/表示链的既有差异，尚未归因；
C#直接CLR字符串的硬编码CP预期通过不代表该端到端链通过。完整native_alias_helper
仍因未导出的私有helper读取失败；原源保留pending，独立不读取该私有导出的实际
暖比较源严格通过。这两份完整源不删断言、不改golden；支持补证为另建独立程序。
因此目标为八个支持用例、六个完整pending，不能把未知域NI算逐字CP成功。

## 性能与完整验收安排

同冻结CLI生成一份公开整函数CIL，两runtime共享字节相同host/CIL。六旧路径与八
typing路径覆盖冷热、160键轮转、metadata读及真实新alias首次args成本；两配置各
唯一ABBA，共560样本，保全部负值、分配、checksum和实际加载身份。默认与禁tier
分别报告，不以配置切换覆盖不利值，不声称预热保证tier稳定。

构建准备的清单glob误收host/obj目录，原失败保留；仅恢复普通文件清单，没有重建
CIL/host或修改断言。正式量测仅在双侧smoke、有限语义与候选身份断言全部通过后开始。
旧G1构造约+6.2%、G2不利值及更早性能风险继续保留，不能由本轮功能通过自动关闭。

唯一正式ABBA完成，8进程exit0、560/560 checksum及加载身份/输入前后核验通过。
默认/禁tier：Union热约-54.38%/-56.80%、920→600B，Optional热约-63.12%/-62.25%、
888→536B，args读约-56.67%/-58.21%、64→0B。不能把分配下降当全部性能改善。
Optional冷轮转约+35.77%/+87.34%、1232→2080B；Union冷约-6.57%/+14.73%、
1264→1288B；新alias首次args约-22.71%/+34.21%、1328→1384B。get_args虽64→0B，
仍约+44.09%/+2.37%；type(Union)约+58.11%/+20.13%。旧普通构造默认+5.14%、
旧type默认+30.58%、listgetitem默认+19.41%及禁tier Generic缓存+9.37%均保留。
默认部分进程离散明显，未取得稳定tier证据，不做普遍提速或无回退结论。冷缓存及
反射成本列后续有界优化项，不为归因无限等待，也不从新feature成功中删除负样本。

正常make check已exit0，2114C#/298CP/236e2e通过，66ignored。旧228通过与60ignored
逐名保留，准确新增八主通过和六条完整pending；CP基线/CP-only自检不计Ryu支持率。
非e2e Rust775通过、另1个旧baseobject ignored。独审10818源内容/size/mode无增删
或变化，七生产文件与固定runtime匹配，空间监控未触限。干净0713ed8功能提交原项目各唯一复测：docopt0.6.2的CP/编译/JIT
通过，CP保留四条860bytes原警告；Markdown3.5.2的CP/编译通过，JIT exit1首错仍为
ImportError: cannot import name 'ClassVar' from 'typing'。20根3187包源、原入口/golden
和工作树前后审计一致，未重试、不增加项目分子。六件仍须独立实际恢复后交付。标准tar.xz严格低于32MiB，保原始失败与全部旧证据。
