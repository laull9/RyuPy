# ADR0119：overload 前置真实函数限定名与定义来源

- 日期：2026-11-05
- 状态：有限域语义、独审、有界性能、正常完整门禁及干净原项目复测完成

## 来源与范围

count 后干净 ed7bdb2 的原 Markdown 3.5.2 入口缺 typing.overload。CP 的真实注册键
依赖当前 module、当前 qualname 和代码首行，不能拿 FuncId、名字哈希或假行号代替。
本切只完成必要来源前置，不导出 overload，不提供空装饰器，不增加项目通过数。
公开代码对象、代码替换和完整函数反射另切，保持完整 CP 正例 pending。

## 设计与语义

HIR 对原 AST 做只读扫描，以真实范围匹配已有函数。无装饰器取 def 行，有装饰器取
首个 decorator expression 的首行，而非 @ token；lambda 取表达式行。同一行的不同
lambda 保不同定义身份。扫描不改默认值、标注、装饰器和函数体的实际求值顺序。
缺来源或范围碰撞报 Diagnostic，不猜名字，不让 codegen 依赖 HIR 或 ruff。

限定名是词法来源：类体改 __qualname__ 不改方法的源默认值；函数内加 <locals>，
global 声明定义重置名字，nonlocal 仍按定义词法路径。CPython 3.12 的 list/dict/set
推导式不另加作用域名，genexpr 子作用域用 <genexpr>，最左 iterable 仍在外层。
target 内的 lambda 也在 genexpr 子作用域登记；隐式生成器函数不伪造用户来源。

MIR 仅搬运自己的纯数据，link 不给 Python 限定名加后端前缀。生成器转换保留用户
factory 来源，不登记 resume/step。codegen 在 __init_functions 每个共享 PySignature
初始化后调用一次 FunctionDefinition(object, string, long)，不在每次 def 或调用查表。
源行必须正数、来源须有独立签名，重复登记明确失败。

运行时用按共享签名索引的 ConditionalWeakTable 保存不可变来源。精确字符串的
__qualname__ 是真实可变成员，与 __name__、__module__ 及公开 __dict__ 分离；同名字典
项不能遮蔽。限定名非字符串写入/删除按 CP TypeError。首次实际改写才创建冷覆盖
表和记录，命中直接改值，不在重复写创建捕获 callback。普通 PyFunction 实例字段、
Invoke、binder、thunk 与现有 module 捕获顺序不变，但冷登记有真实成本，须量测。

真实 str 子类限定名赋值是 CP 合法操作；本切明确 NotImplementedError，不假报非
字符串 TypeError，也不复制成基类字符串破坏身份。未知 native 来源读限定名或内部
首行明确 NI；显式已知限定名可独立保存，但不能因此伪造代码首行。既有已登记原生
元数据守卫保持。公开 __code__ 读写删均明确 NI，不能让普通属性 fallback 假装替换
后仍用旧内部来源；__dict__ 里的同名条目也不能绕过此边界。

## 原始证据与独审修正

五主源和三完整边界先固定 CP3.12.13 双跑，16/16 通过；旧稳定版本八源全部编译过，
运行均真实失败。候选001五主和独立词法表达式综合源通过。独审另证 genexpr target
内 lambda 漏来源导致 E0101，以及合法 str 子类被错报 TypeError，原源与失败全保留。

只补只读 target 扫描及 str 子类 NI 分支，没有顺带修旧 target 自由名捕获问题。最初
完整 target 源在旧稳定版本已是 UnboundLocalError box，仍完整 pending；另建无自由名
的独立完整源验证本切，不删原源断言冒称它通过。候选002七完整支持源 CP/JIT严格
一致；五反射、派生或旧自由名边界保持 pending。独审还用真实 CIL 对原 source 映射
核跨行 decorator、generator factory、两级 lambda 的源行，所有登记只在冷初始化。

新增白盒 HIR5、MIR4、CIL1、C#9；专项包含一项相关旧 HIR 和19项相关旧C#，共11
Rust及28C#通过。首次 cargo check 的 Name 查询类型错误和首次 xUnit 纯格式警告均
留原流；局部修正后检查/专项通过。四处旧合成 C# fixture 的已知限定名显式赋值，
未知来源旧缺特性断言改 NI，wraps 真实复制断言改为新已支持值；未伪造任何行号。
首次完整门禁 CP410 过、C#2234过/1失败；遗漏的contextmanager合成fixture已按同样
原则补明确限定名，原异常抑制断言保持。首次门禁和单例详细诊断全留，不算门禁通过。
第二门禁C#2235全过，进入driver后因旧CIL快照尚未包含新冷登记而失败。25份预期
更新逐份保旧/新原件：仅__init_functions共61组四指令新增及其直线标签重编号，
剥离后整文件SHA回到旧值，每组真实名字/首行另与固定CP code对象核对。收集时
一次未沿用串行配置的额外Cargo链接耗尽空间，进程组已停、原失败保留；只清本次
可重建重复产物后，改用完整门禁已构建的精确二进制顺序采集，不据该中断称通过。

## 性能与仍开放风险

唯一预注册矩阵24进程、740稳态样本、16次真实首次Main及4次用户函数首写，34.157秒，
无重跑/补样。默认tier及禁tier各三组ABBA：同旧PE换runtime、同完整源双compiler
配各自runtime、再多128真实定义的双compiler冷初始化。小/大各25/153真实来源登记，
59/315生成方法仅__init_functions规范CIL不同；实例字段、构造、Invoke、Signature、
Binder规范CIL相同。生成PE的MVID为零，身份改用实际PE SHA与规范方法CIL，不伪报相同。

八条旧共同路径分配中位数差均0，但32个路径/配置/组组合有13项时间不利，全部保留：
同旧PE/runtime组，默认闭包创建+0.81%、生成器创建+17.81%，禁tier动态默认参数
+2.87%、keyword+3.54%；双compiler组默认静态调用+2.57%、动态默认参数+7.94%、
keyword+13.54%、普通def创建+6.67%，禁tier静态+6.28%、动态默认参数+1.54%、
bound默认参数+11.12%、普通def创建+7.24%、生成器创建+19.71%。两进程各五样本的
有界结果不证明统计显著，正负差均不得当普遍改善或无回退；不继续跑以挑好值。

默认/覆盖限定名读取和重复写，本组每个样本当前线程0B；预建新函数池首次写为
约152.0081/88.0081B每写（默认/禁tier），完整32768写批次成本与计时外建池范围均留。
真实Main后覆盖表已被既有TypedDict/NamedTuple原生初始化写入建立，因此只称用户
函数首写，不能伪称全表冷建；准备时错误空表假设的smoke失败保留，没有清表绕过。

默认首次Main的小/大规模B−A分别+2288/+15656B，双规模差分为104.4375B/新增定义，
仅是本组平均。禁tier小规模A第二进程多98352B，使双规模差分膨胀为488.625B/定义；
原值未删除、未归因，不能当独立来源记录大小。各组冷时间B−A为负，但包含冷JIT、
系统初始化、表扩容及模块正文且每side/规模仅两值，不称来源登记提速或免费。
保留全部原样本、checksum/身份审计和13项不利。既有TypedDict+512B、count及F0/F1
风险继续开放；本组也没有覆盖所有原生元数据消费者的长期存活/RSS成本。

## 完整门禁与后续

check003正常make check133.066秒exit0：2235C#、410固定CP基线、410CP-only自检、
299编译差分通过、115ignored；其它Rust785通过+1ignored。旧402个e2e逐名保留状态，
新增12项为7支持和5完整边界；旧775项其它Rust逐名保留、恰加10白盒。前两门禁
失败与额外快照收集磁盘失败均不混为成功，完整原证据保存。源门禁11188文件，实际
Release runtime始终5c1bbbe8；单进程原配置，空间监控未触发低容量中止。

## 原项目复测与恢复

干净功能提交ab7c65def6c86f43b24b419af29cfd98db0702f5上，两原入口各唯一CP/compile/JIT
尝试：docopt0.6.2三阶段exit0、stdout16B逐字等golden，CP四条SyntaxWarning原样保留，
JIT stderr空；Markdown3.5.2 CP/compile exit0、CP stdout77B等golden，JIT exit1且stdout
空，stderr57B仍为ImportError: cannot import name 'overload' from 'typing'。63个输入、
两个3187文件源树及20根原次序前后不变。只报这两项固定Linux JIT入口，不改旧同提交
20项目矩阵、不泛化其它平台/AOT或完整库兼容。

下一切再接真实overload/get_overloads/clear_overloads及有序强引用注册，按消费相位
读取当前成员并保留部分副作用。Markdown尚未通过，不能把来源前置计作模块完成。
恢复保持固定基包＋最新累计包；两原始count收尾日志可按显式无损规则逐字重建，
每件严格小于32MiB，最终完整两包独立恢复后才发布；旧证据和负样本保留，不推送远端。
