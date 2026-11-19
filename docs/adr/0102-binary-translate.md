# ADR-0102：bytes/bytearray.translate的有限buffer实现

日期：2026-11-05。状态：最终正常完整门禁通过，原项目窗口另记；未发布。

## 依据与范围

上一干净功能候选c1ed015在binaryornot0.4.4原固定入口解除locals编译阻塞后，JIT首错为
bytes缺translate属性。官方helpers.py:71/83确实使用translate(None, delete)，但本切实现
通用256字节表/删除语义，不按项目名、特定delete值或调用点特判，不改原包/入口/golden。

支持精确sealed bytes/bytearray接收者；table必填位置参数，可为None或精确bytes、bytearray、
现有一维连续B/c memoryview；delete可位置或keyword传入，必须为同类buffer，None不合法。
表按原始字节长度严格256；c视图不是按迭代返回的bytes元素处理，offset视图只读有效窗口。
删除匹配原始输入字节，然后翻译存活字节；NUL/0xFF、重复删除值与全部删除均有效。
bytes没有实际变化时返回原对象；bytearray即使无变化或为空也返回独立新对象/存储。
保存方法保持接收者身份、调用时活读内容，源/table/delete相互别名不修改原对象。

## 绑定与分层

共享运行时binder和正文服务直接、保存bound、root descriptor、保存root与PrepareMethod。
root描述符稳定身份，先验证位置self及真实载体；之后按CP先总参数上限、必填位置table、
非法keyword、table buffer/长度、delete buffer顺序检查。空源仍验证全部参数。
省略delete有独立sentinel，不能把显式None当作省略。自造错误保调用者帧与原ambient context。
root通过offset跳过self而不复制位置数组，不引入RuntimeFn/MIR/CIL或新的PIC特化。

## 算法、存储与生命周期

stackalloc ushort[256]共512字节，0..255为映射，256为删除标记。delete只扫描一次。
第一遍源扫描计最终长度并检查实际变化，第二遍填精确输出；不变bytes只扫描一遍。
复杂度O(256+d+2n)，不是普遍单遍。没有按对象身份/Count/ResizeEpoch缓存可变表或删除集，
同长内容变化也会下一调用立即生效。不为每个源字节线性查delete，不使用Python dict/hash。
变化bytes直接分配最终byte[]；bytearray新建List并EnsureCapacity/SetCount填最终backing存储，
不先建临时byte[]再复制、不为删除结果再次缩容。wrapper/List与公共调用成本并非零。

table=None且delete验证后为空的bytes路径在Invoke绑定之后直接返回原对象，省256映射及源扫描。
必须先验证delete，不能绕过released空view或显式None错误；bytearray和显式恒等table不走此路径。
该分支与实际项目非空delete调用不同，不能用它的速度代表binaryornot工作负载。
None加非空delete的bytes会多一次O(1)内建buffer活性读取，核心仍只扫描delete一次；明确保其成本。

memoryview新增窄内部同步span读取：先检查Owner/released，再取当前偏移窗口。
合法buffer域中从取span至输出无Python回调、await/yield或用户转换，局部托管byref保数组存活，
不新增临时导出租约/锁/输入快照。调用者已有view仍持原租约，完成或失败后不多留导出。
此证明只适用于现同步无回调域，不保证CLR宿主并发修改公开存储安全，不可直接用于未来buffer回调。

## 明确边界与审查发现

CP3.12.13允许宽格式/多维连续buffer及用户__buffer__/__release_buffer__，这些不是CP非法输入。
本切沿现view构造限制；普通PyInstance含类型级__buffer__时明确NI，不执行回调。
一般__bytes__/__iter__/__index__/__len__/__getitem__不能替代buffer，不调用这些协议。
真实内建子类布局、descriptor.__get__不借此扩展。删除到空的全局bytes单例仍有旧身份偏差，
不局部加入空/单字节intern，也不宣称所有CP身份策略一致。

独立审查实证旧通用FlattenArgs的重复**关键字缺口：直接/保存bound/root各两载体共六入口，
CP报multiple values for keyword argument而Ryu当前报arity。原CP/JIT源/双流/diff失败完整保存，
原源另保tests/fixtures/binary_translate/pending/duplicate_keyword.py.pending，不计通过。
本切严格绑定指普通直接参数及既有无重复展开后的binder，不是完整调用展开合同。
仅在translate正文查重会晚于本应截断的展开副作用，不能用局部补丁伪装修复；通用展开另切。

## 验证记录

固定CP3.12.13独立220项观察及核心断言，含范围外合同，不把全部观察计入实现通过。
三unique公开应用覆盖全值域/16种buffer组合、别名/暂停前接收者捕获、精确错误/完整cause-context链。
原CP-first、首次原源CP/JIT及转正后均3/3通过；去pending仅换空行保持全部错误帧行号。
首草稿literal-is产生SyntaxWarning，原稿/原stderr保存，改为变量后CP/JIT无额外stderr。
运行时首17项通过，增加None/empty-view快路专项后18项；完整门禁、真实CIL成本及项目结果后记。

首正常make check在旧PyNotImplementedTests.BooleanConversionUsesWarningPolicy停止：1857过、1败，
fmt/clippy/Release/CP228已过，后续workspace/e2e尚未运行。首日志保留，其安静输出没有具体
异常消息。原代码单类3项及全量诊断1858项均通过，未重现；不能据此断言translate正文回归。
审查确认该测试读取全局警告/帧状态却未加入已有隔离组，现仅将整个类加入与warnings相同的
禁并行集合，保全部原断言，不改运行时语义；第二次正常门禁结果另记。

第二次正常make check exit0：1858C#、228CP、202e2e及30原ignored，fmt/clippy/workspace全过。
之后真实性能发现新快路初稿将恒等/表初始化写为循环内三元分支，64字节非空转换出现固定成本
回退。基线→候选→基线复跑与JIT汇编均支持该判断：FullOpts确实在256次循环内重复判断。
保快路但恢复循环外分支/两个初始化循环，原候选性能和第二次门禁原日志不覆盖；最终代码须再跑
同源性能、专项与正常完整门禁，不能用先前green代替后改代码验收。

循环外分支版在静默对照中仍有小buffer回退：64字节ASCII初版214.7ns、两轮候选316.6/325.3ns；
FullOpts表初始化循环主体由5条变7条，增加两次寄存器复制，与局部span活区变化相符，不归因于
已经外提的bounds check。再将窄guard上移Invoke，Translate核心逐字恢复初版，后续同源结果另记。
不保留这个不利候选作最终性能方案，也不删其测试/汇编/原流证据。

最终Invoke guard的两轮真实生成CIL验证各72例、5组共享buffer全部通过；每例200次预热、
7个样本，关闭分层编译，预绑定生成方法委托，不在计时内Reflection.Invoke。覆盖64B/4KiB/1MiB、
两种载体、真实映射/删除/恒等/view/keyword；CP结果、身份与输入不变逐例核验。
所有72例分配与初版相同；最终Translate源码逐字相同，FullOpts代码大小同为1011字节，
初始化恢复同一5指令循环。64B bytes xor两轮213.1/245.8ns，ASCII223.3/228.8ns；
部分复跑样本显著飘高，例如xor+删除238.0/432.4ns，不把相同源码/汇编称为全面性能无回退。

1MiB bytes None省略delete两轮42.1/46.2ns，显式空delete53.1/62.6ns，均0托管B/op。
这是无需扫描的窄恒等分支，不报告名义内存带宽。真实None+删除空白两轮1.693/2.034ms，
819848B/op；xor映射1.586/2.101ms，1048624B/op。正长位置参数结果bytes观察为输出长+48B，
bytearray为输出长+96B；关键字另128B调用开销。包含生成调用路径、分派和结果，不是单helper
分配剖析，也不含栈/原生/RSS。独占CPU/绑核未做，部分早期测量与门禁并行，最终轮门禁暂停；
这些样本不证明统计显著、跨语言倍率、应用整体/AOT/跨平台提速。详细两轮范围见验证summary。

共享buffer额外测试首工厂丢弃memoryview.cast的中间view，错误假定子view释放就能resize，
因既有GC租约而失败；原失败完整保留，显式释放工厂中间view后所有版本5/5通过。不是生产回归。

最终Invoke guard代码第三次正常make check exit0：1858C#、228CP、202e2e及30原ignored，
fmt/clippy/workspace全部通过。最终日志SHA256：743b88fa07500813c9d9cfd6d815fa1cd03a281919f0e62f12e35dc5389c59cc。
首失败与中间green日志分别保留，未以中间green代替后改代码验收，未改项目分母或CP期待。

## 原项目唯一后续窗口

干净功能提交f3ba26bcd72629f63199a535efbc6a18c3aa597c（tree2d613947a3d762de099dda2cd324aa7d1abcb4b4）
对binaryornot0.4.4原固定Linux JIT入口执行唯一CP/compile/JIT窗口：各exit0，无超时，
CP与JIT原stdout都为False True换行且与原golden一致，JIT stderr空。原sdist、76个包文件、
501个chardet5.2.0依赖文件、入口和golden哈希与前轮一致，产物runtime与已验Release同hash。
没有改包、shim、重试或为了结果变更入口。CP/编译/JIT约0.215/1.619/0.566秒仅为预算记录，
不是项目速度基准。result.json SHA256：383e55be75ea2687b6ffa06d1d6d7f70d9bcef04c2ff2abf0a44d76aa1ea4ab6。
这只证明该候选单入口通过，不代表完整库、main、当前20项目矩阵、AOT或跨平台。
