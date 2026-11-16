# ADR0120：typing.overload 三联 API 的有限真实注册

- 日期：2026-11-05
- 状态：有限域语义、独审、唯一性能、正常完整门禁及干净原项目复测完成

## 来源与范围

干净 count 提交的原 Markdown 3.5.2 首错为缺少 typing.overload。ADR0119 已提供真实
函数词法限定名和定义首行，本切据这些来源接 overload/get_overloads/clear_overloads，
不以空装饰器、猜造行号或只供 Markdown 使用的桩替代协议。Rust、通用 binder、
PyFunction 实例字段及 Invoke 不变；完整公开代码反射与未知动态域继续分切。

## 设计与可观察语义

三 API 与统一 dummy 为稳定真实 PyFunction，保存的函数与公开同名模块槽分离。
本组零/一参绑定先处理未知或重复关键字，再处理过多位置或缺参，不改共享 binder。
overload 只解一次 __func__，注册时保存原参数强引用，而非解包后的函数；统一 dummy
调用抛固定 NotImplementedError。普通函数 的普通 __func__ 成员与真实
bound/classmethod/staticmethod 的底层函数身份分别处理，不被同名字典假属性遮蔽。

公开 registry 是真实 defaultdict(partial(defaultdict, dict))，内层是真实 PyDict，
键按当前 module、当前 qualname、真实定义首行分层。同 line 替换保留原位置，查询
返回新 list 快照并保持元素身份；miss 返回新空 list，clear 清当前公开 registry，
不替换对象，不使已经返回的快照失去原函数。没有容量上限或自动逐出，强引用一直
持续到替换、删除或 clear；仅用于键域验证的 ConditionalWeakTable 不延长旧桶寿命。

helper 与 registry 在 CP 消费相位读取：注册已建的前缀桶遇到后续错误不回滚；异常
出现后才检查当前 AttributeError helper，返回时才取 dummy；get miss 不消费全局
list helper。更换为未知 getattr/list/异常/helper、未知 mapping 或工厂时在实际消费
点明确 NI，不提前扫描未消费层。clear 不 hash 旧键、不消费 default_factory。

有限键域为 None、精确 str/bool/int/float 与任意精度整数。已知不可哈希容器按
TypeError，未知 tuple/bytes/派生及用户 hash/eq 明确 NI，不把合法未知域误判为
不可哈希。外部桶结构版本变化才重新扫描真实 key；内部已验证 scalar 插入和 clear
更新该弱键版本缓存。失败扫描不记成功版本，不给通用 PyDict 增字段或影子存储。

真实 method/wrapper __func__ 读取另接 PyObject；直接或含暂停的 wrapper.__func__
调用须将两种 wrapper 纳入既有 CallMethod/PrepareMethod 属性捕获分支。直接调用
先 GetAttr，再 FlattenArgs，缺属性不能被后续星号错误抢先覆盖；prepared 保存原
函数且不提前调用。两处共同分派新增类型检查具有成本风险，不据改动窄就称免费。

## 验证、完整边界与失败保留

固定 CP3.12.13 完整正例和 golden 先独立双跑。C# 红灯19项均因 triad 缺失失败；
候选001为19/19，候选002引入真实字典版本弱键缓存后22/22，候选003必要 wrapper
接线后23/23。B3 runtime SHA256 为
96ca01f0a24ec3afcfb5da64725ea77e7450b3fcd894459ada318ec45aaa4138。

B3 原17份完整源各唯一 compile/JIT：6份严格通过、1份编译失败、10份运行失败。
随后 API 元数据改写完整源也保持 pending。六份转正只替换首行 pending 注释且仍
占一行，第2行到末尾与全部位置属性 AST 不变，golden 未改。原 descriptor_factories
直接 __func__ 失败保留，由共享分派修复后原整源通过，不删原调用规避失败。

十份新增完整 pending 与两份复用旧边界继续保留，包括公开代码反射 NI、自定义
__getattribute__ E0100、native/wrapper 元数据缺口、helper 重绑 NI，以及下列已证
差异：registry repr 中 partial 显示为 <partial object> 而非 CP 的 ...；三 API 改写
__qualname__ 后13条 TypeError 仍用 canonical 名称前缀，其余合法调用/身份/恢复
输出一致。完整源未改 golden 或吞掉异常转正，不宣称这些边界兼容。

正常 make check 唯一门禁 exit0，143.068秒：2258 C#、426固定CP基线、426 CP-only、
305正常e2e通过、125 ignored，主Rust785通过+原1 ignored，e2e框架另7项通过。
旧414个索引用例逐名状态不变，新增16个为6通过和10 ignored；旧Rust具名状态不变。
独审复核原日志、源身份、行号、golden 和真实 CIL，不以CP自检计RyuPy支持。

## 唯一性能矩阵与冷成本

唯一12进程、420稳态样本、12次真实首次Main，62.959秒，无正式重试、补样或校准。
同冻结编译器只编common/API两个PE各一次，common同字节配A/B runtime，API仅B；
同一个A接口host用于三个部署。default和禁tier各为common ABBA再API B1/B2，每
路径每进程5样本，只列两个进程中位数的均值与范围，不证明显著或default tier稳定。

真实CIL审计证实普通静态直调、PyPic调用/真实thunk与binder及三个工厂；新增API实际
经IsModule/CaptureModuleTarget/CallCapturedMethod0或1到PyOps.CallValue、Invoke和
真实回调，先绑定再消费registry。最初离线断言误要求直接CallValue，原失败保留，
只按已保存token校正审计，不重编PE或重smoke。三部署无计时smoke的语义、身份、
registry强引用已通过。PyFunction字段/ctor/Invoke、Signature/Binder/PyPic规范CIL
不变；允许的唯一共同方法变化是PrepareMethod/CallMethod两处wrapper分派。

八条旧路径每完整函数的分配中位数差均0，但禁tier六项时间不利全部保留：静态
调用+4.21%、keyword+11.15%、bound默认参数+4.12%、普通def创建+4.73%、闭包创建
+7.30%、闭包调用+3.03%。default八条本组时间为负，不据此称普遍加速。旧函数来源
13项不利、count repeat/chain及TypedDict四字段+512B等历史风险全部仍开放。
本矩阵不覆盖全部普通方法入口，不能把局部CIL不变或分配不增当成无性能回退。

新API每完整函数128个item，default/禁tier每item的描述值：双注册5257.6/5465.3ns、
1296B；两元素命中查询2784.2/3024.4ns、800B；module miss查询2406.3/2478.5ns、
688B；clear加两次重建注册7602.3/8443.5ns、约2920.29B；预绑定method查询
495.4/656.6ns、208B。双注册和clear重建均为混合周期，不冒称一次纯API调用；
重建桶和验证缓存成本留在计时内，预绑定成本在计时外。新API绝对成本不抵消旧回退。
双注册周期除以2后，default/禁tier每次注册均摊2628.807/2732.636ns、各648B；
这只是完整周期归一化，不是孤立API基元成本。

普通首次Main在default的A原值为67.844/60.765ms、各3502568B，B为70.688/58.761ms、
各3505880B，均值差+0.420ms/+3312B。禁tier A为133.903/141.341ms、各3506920B，
B为148.269/141.609ms、各3509976B，均值差+7.317ms/+3056B。API源仅B：default
61.469/70.296ms、各3507704B；禁tier161.983/162.350ms、各3511672B，不能与common
交叉相减。真实普通原生启动即预加载typing，不显式import也可能承担新初始化成本。
Main含JIT、PySystem、typing、函数来源与模块正文，不能把全部差值拆称registry本身
的独立成本；当前线程GC分配也不是RSS或长期存活内存。全部420原样本与原流保留。

## 项目状态与恢复

本次功能提交前未重跑此前已完成make check；重启后56个生产/测试候选路径逐个与
门禁冻结SHA一致。后续只有文档更新，未改生产、测试或golden。

干净功能提交cf2e28e242b08eca921546e2fc71ebf54728fc0a上，原docopt0.6.2和Markdown3.5.2
各唯一CP/compile/JIT：docopt三阶段exit0，CP/JIT stdout16B逐字等golden，CP四条
原SyntaxWarning保留，JIT stderr空；Markdown CP/compile exit0，CP stdout77B等
golden，JIT exit1、stdout空、stderr54B，首错为
ModuleNotFoundError: No module named 'importlib.util'。所有阶段原流、退出与PE留存。
63输入、两份各3187文件原包源树及20搜索根原次序前后不变，入口/golden未改。
这只说明两项固定Linux JIT原入口的新状态，不增加Markdown分子，不替代旧同提交
20项目矩阵，不泛化其它平台或NativeAOT。下一切从真实消费点设计通用importlib.util，
不能为单项目特判或只导出空模块。

上一个111ec1d恢复包已从固定双包独立读回113683逻辑证据、70份源码快照并通过Git
及最小恢复工具smoke；四份早期快照mode限制保持明示。本切新增性能/项目/提交证据
按同一固定恢复契约追加，完成独立恢复前不冒称新备份已发布。未推送远端。
