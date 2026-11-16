# ADR0117：ClassVar 适配真实原始缓存与既有消费者

- 日期：2026-11-05
- 状态：有限域专项、运行独审、有界性能、正常完整门禁及原项目干净提交复测完成

## 来源及失败保全

Markdown3.5.2在Generic单基类支持后首错为ClassVar导入缺失。旧C1 WIP虽然有部分
专项通过，却在ClassVar=list后拒绝原List[key]暖命中，未通过最终验收；旧失败、旧
620性能样本及未实测static lambda修复都完整保存。此前拆出F0、F1a、F1b、F1c，先
接真实raw receiver/parameter缓存，再恢复本体，不能用归一化驻留表绕过错误次序。

本轮基线为干净d192d7d与实际4efa运行时；按九生产触点重新集成，不整包cherry-pick。
旧私有TypeArgument只有native公开入口和不可达未知form尾路，故保其原字节；native
PEP585不应新增ClassVar读取。Literal直接Normalize不调用该checker，原限制不扩。
保F1c Callable真实类型与父边、五个缓存算法及其有限域。新增ClassVar共用原SpecialForm
128项LRU，不另建池或无界旁表。319非修改运行时文件受保护。

## 根、alias及真冷检查

ClassVar有稳定SpecialForm根；参数化结果保存真实receiver origin、args/parameters及
已有惰性metadata，原始None/NoneType与tuple形状不抹平。ClassVar专用hash按origin与
args组合，不替换所有alias算法。根/alias的调用、基类、isinstance/issubclass右操作数
和已闭合再订阅按已证CP有限协议处理，保tuple类型判断短路。自由变量再订阅、完整
动态反射、ForwardRef及任意回调仍完整pending，不用NI当CP通过。

SpecialTypeArgument在真实冷相位读取当前ClassVar导出，并在分类typing alias后比较
其origin。ClassVar=list时List[int]作为新冷类型参数被拒绝，但原warm raw键仍能命中；
新List[(key,)]与旧List[key]不是同一raw键。List[(int,)]本来合法，不是该回归反例。
ClassVar=None/string时旧ClassVar alias和普通List alias可以合法通过，不能永久按
名字拒绝。裸ClassVar仍按SpecialForm准确拒绝。未知替换只在真正比较点NI，不做入口
canonical-identity预检。保存的ClassVar receiver不跟随公开导出换根。

Callable args只做type-convert，ClassVar根/alias可位于args；result才type-check，且
先于args转换。普通十一根、Tuple非空前缀、Optional/Union、TypeVar bound、NamedTuple
注解在各自原相位迁移absence guard，空Tuple不消费checker，TypeVar constraints不
突然加检查。不能借统一helper改写其它消费者的既有错误顺序或ForwardRef限制。

第九触点TypedDict来自新鲜CP双跑72场景：仅新own字段在已有冷验证点接ClassVar读取/
alias-origin检查，继承字段不重验。native alias的内部ClassVar合法，裸ClassVar非法；
类型注解不约束实例字典实际值。所有字段表达式在字段校验失败前已顺序求值，失败不
发布新绑定。未顺带实现Required/NotRequired、完整checker或任意字段对象协议。

## 专项与完整边界

原16份WIP源、原R1、新raw/四池/11根/消费者/TypedDict合同及已有4pending都保原字节。
首33物理源为32种源hash（Callable设计11与现有pending源相同），固定CP66进程全过；
安装28源均先pending。新09在函数内定义class，基线编译E0100，整源留pending，不扩
编译器或删除断言；另新增模块级完整12错误源，CP双跑输出与原golden逐字相等。
最终29新增case和4既有case的视图：24支持目标、9完整边界；旧09不算通过。

候选原23完整支持源CP/compile/JIT严格通过，新模块级源单独通过，共24。基线原32源
只有原R1严格通过，5编译失败、26JIT失败；新增模块级源CP/编译过而JIT失败。所有原
失败保留，不用新源掩盖完整边界。原9历史支持源本轮重新运行，不沿用旧DLL结论。

38项新/迁移C#首轮34过4败，4败均在新增TypedDict fixture复用generic_root.py导致
ModuleForFile歧义，未到ClassVar字段断言。只改该新测试类为唯一模块key/filename、
真实source frame和准确depth恢复，公共工厂、四facts及断言不变；单独重跑4项全过。
没有改生产绕测试，也不清全局模块表。54旧相关类排除已聚焦名字后实际426测试全过，
静态418方法清单不冒充实际数据行计数。当前生产仍首次编译的同一候选字节。

## 性能和门禁

唯一正式矩阵12进程、1340样本、51.719秒：32条共同路径分别default/禁tier ABBA，
另三条ClassVar仅候选绝对值。原30条源/CIL/迭代不改，新真实getter捕获和四字段
TypedDict均由完整公开函数测量；全部身份、MVID、checksum与初始化计数通过。
SDK首次因XDG目录只读在编译前失败，审计首次误认直接GetAttr而实际PyPic调用，两者
修正只涉及环境/证据判断，保原失败，不改源、不重跑已完成的A smoke或正式矩阵。

TypedDict四字段40个样本A20全2912、B20全3424 B/op，稳定多512B；默认代表时间
+18.20%，禁tier+78.57%。新增ClassVar消费保证真实错误次序，这一冷路径成本明确
保留待独立优化，当前不以功能正确替代性能通过。其余31条共同路径分配代表差为0；
默认10/32、禁tier22/32代表时间不利，全部报告，不作统计显著性结论。
真实根getitem捕获均504B，指定Union/Optional/cold_args没有重现旧+32/+64B症状；
不能由这次整体差异单独归因static lambda，也不能关闭历史旧路径风险。
ClassVar热/churn/args为536/1280/0 B/op，仅是候选绝对成本。F0/F1全部历史报告、
冷Optional+848B、Callable冷+120B等继续保留。正常make check唯一执行130.018秒exit0：2202C#、390固定CP基线、390CP-only自检、
287编译差分与775其它Rust通过；107e2e ignored和1其它Rust ignored保持明确边界。
相对F1c旧365逐名核验，只有三个明确pending转正；新增29中21支持、8完整pending。
CP-only另排除四个phase4外部依赖case，不冒Ryu支持；319未修改生产文件受保护。
门禁前冻结11114文件，实际runtime前后4cda字节相同，最低观测剩余空间大于1GiB，
未触发低容量中止。独审只读核对原流、逐名状态和源/产物身份，不另跑门禁。

## 项目及恢复

干净功能提交eed648db7e96495c2b05436829f395fe137dfebe上，两原固定入口各唯一
CP/compile/JIT尝试：docopt0.6.2三阶段exit0，stdout16B逐字等原golden，JIT stderr
空，CP四条SyntaxWarning共860B原样保留。Markdown3.5.2 CP/编译exit0，CP stdout77B
等原golden，JIT exit1、stdout空、stderr57B为ImportError: cannot import name
'count' from 'itertools'。没有重试、换入口或改项目；ClassVar缺失已越过，但Markdown
仍未通过。63项输入、两个3187文件源树、20个根次序与实际DLL/工具前后一致。
这是两个Linux JIT原固定入口窗口，不回写e066238同提交20项目矩阵，不声明AOT。
下一切按真实新首错研究通用itertools.count；TypedDict新增分配独立TODO保留，
其安全比较窄优化只有静态设计，没有实现或量测，不能冒称回退已修。

恢复沿固定F1a基包+单一最新累计增量，均小于32MiB；原失败、WIP、全部历史快照与
性能原样本仅由两包独立恢复。CURRENT/RESTORE侧录与固定Drive ID更新，不积累旧
增量链，不在新组合上传成功前删旧备份。
