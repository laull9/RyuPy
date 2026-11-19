# ADR0112：Generic 参数化单基类与普通后代

- 日期：2026-11-05
- 状态：有限域已验，正常完整门禁与原项目窗口完成

## 目标与范围

G1 根/TypeVar alias已验，但原Markdown3.5.2仍在Registry(Generic[_T])编译E0100。
本切开放真实有限单参数化基类，不按库名或Generic文本特判，不把alias擦成object。
G3用户泛型类订阅/替换、G4 IO祖先迁移仍分开；完整typing/PEP560/元类未完成。

HIR注册固定点与正式lower共用runtime_bases候选，开放最外Subscript，以及既有
Name/Attribute保存alias。接收者与索引照常求值且只一次；MIR原freeze、Validate、
MakeClass、body、Finalize、逆序decorator、最后发布骨架及ABI不变。

## 真实类布局与冷状态

Validate原样保alias，MakeClass仅一次解析mro_entries，保存原bases tuple与alias
身份，真实BaseClass为canonical Generic。root仍空布局，后代是普通类字段及new/init
链，不使用IsNativeRuntimeClass或把全family放入special构造。只有family冷分配
Pending/Ready/Failed状态及稳定bases/MRO tuple；mro()返回新list。

不重算用户类为CLR泛型，不生成特化IL。普通Child的own parameters为空，orig只继承
父tuple，不复制；真实type/isinstance和原origin身份保留。构造、foreign new返回、
普通字段/方法和实例getitem沿既有路径；family类Box[int]在误走实例getitem之前NI。
普通类既有反射/描述符/构造长尾不因这一布局整体修复或宣称全兼容。

## 类体与终结次序

类体开始不预注入最终orig/parameters；body失败不finally终结或替换旧公开绑定。
若alias解析改变bases，body结束后以原tuple覆盖own orig；未改变则保body最终own值。
随后按type创建时机处理set_name，再查实际父链init_subclass，再执行canonical
Generic参数推导；成功才ready并应用decorators。终结失败保持Failed，不可伪造可用类。

set_name前没有自动生成的own parameters，但body自写参数已可见，不能说该属性必然
不存在。父hook不调用super时CP不补own参数，所以未知hook不能静默skip后补参数。
定义父自己的hook本次不调用它；真正创建其后代时，已执行子body后才NI。未知descriptor
set_name也在body后、父hook前拒绝。CP descriptor原异常ValueError不应擅包RuntimeError。

裸Generic不是一律非法：按最终own orig和真实bases决定。exact tuple域内()、(int,)、
('bad',)成功为空参数；(T,)、(Generic[T],)、(List[T],)均保T原身份。plain Generic
成员、重复Generic alias、已证None/int非iterable按CP错误；混用已核旧alias采集后，
缺声明变量按CP检查。未知iterable/比较/参数协议及helper改绑仅在实际消费NI。
canonical导出、_collect_parameters、_GenericAlias等守卫保各自相位与短路。

## 晚改协议与未知描述符

根晚改new/init不在子类定义时提前消费；构造时按真实槽owner与GlobalVersion拒绝
尚未支持的继承根协议。实例读写、hash/eq/ne/call、显式super及PIC冷解析同源守卫，
普通PIC继续版本失效，不给全部普通类每次访问扫描基链。

普通函数的显式Box.__repr__(x)经已有类属性绑定可与CP一致，不能只因隐式路线NI而
统一禁止。独审反例证实未知descriptor先前会漏__get__、返回原对象或伪造TypeError；
第二候选在真正class/instance/super绑定处NI。non-data描述符仍由实例字段遮蔽，data
描述符在实例字段之前消费，不能因已有字段绕过。已知function/staticmethod/classmethod/
property原有绑定不变，不在字段赋值或class定义时无关预扫。

__module__是type专用元数据直接读，值对象即使有__get__也不消费它；新独立CP双跑
证明root与family都应返回原对象。构建前撤回误加的module口guard，保普通module写读，
不以合法但无descriptor绑定的读操作冒未知消费。原错误方案、修订与反例全部保留。

## 支持边界与原始失败

五主先固定CP3.12.13双跑，旧G1编译/运行均未过。首候选construction、definition_order、
cached_metadata、orig_entries四主严格通过；bare_failures唯一类体直接raise被旧HIR
E0100拒绝，JIT未运行。原源及oracle完整保unsupported_body_raise；主采用外部raising
函数调用，保body错误、前缀副作用、原绑定及优先级，CP重新冻结后五主均通过。

原core包含class-body locals和普通实例__dict__反射，完整保pending；新主独立设计，
不删原断言冒成功。family实例dict读取明确NI，不假称没有实例字典。完整hooks、
boundaries、用户class alias、未知descriptor原CP源也保pending，NI边界与CP真值分列，
不把显式拒绝算逐字兼容。IO、多基类/class keywords/metaclass、任意descriptor和
完整mappingproxy不扩；未知合法域不冒TypeError。

首候选9项新C#通过；描述符修订增加5项公开C#合同，第二候选14项全部通过。
五新主在第二候选严格CP/compile/JIT通过；G1原unsupported_generic_derivation完整
原源也严格通过，仅移除case pending标记，不改源或golden，作为一个旧边界真正关闭。
独立支持域、普通函数显式类读、module直接读与CP逐字一致；三个descriptor原反例
改在正确消费点NI，non-data遮蔽保留，data不再读错字段。原own_get在首次类读即NI，
不声称同次执行还验证了后续实例读；该部分由公开C#合同另验。
普通函数显式类访问继续支持，三个真实descriptor反例及所有原始流不覆盖。

## 性能与完整门禁安排

8旧公共热路径复用原源，同一新CLI生成CIL/host，在旧新runtime比较；8新Generic热
路径只报候选完整函数成本。两配置各唯一ABBA，共480样本上限，保不利值/分配及每
样本语义checksum，不追加重跑至变好。不测冷类定义，不能用启动总时间冒单指令成本。
G1普通构造默认约+6.2%及此前TypeVar/属性/partition风险均继续保留。

480/480正式样本完成，checksum及加载工具指纹前后均一致；八旧路径B/op不增。
默认custom_new约+1.99%，禁tier诊断type约+3.31%及listgetitem约+3.15%不利值保留。
默认大幅下降与禁tier明显不一致，不宣称普遍提速；旧G1约+6.2%风险不因此关闭。
新Generic默认热构造约527ns/128B、Child约494ns/128B、custom_new约1609ns/120B、
parameters约264ns/72B及MRO约99ns/48B，仅是完整热函数成本，不包含冷类定义。

第三次正常make check已全通过：2098C#/284CP/228e2e、60ignored。旧55ignored仅
G1原派生用例转正，其余54逐名保留；新增五主通过及六边界ignored，旧222通过无回退。
前两次分别构建ENOSPC、链接SIGBUS且磁盘归零，CP/C#通过但Rust/e2e未完成；原流
完整保留，不删除失败。核验清理81个可重建旧测试ELF释放8.89GiB后，恢复原默认
增量配置、jobs=1，第三轮空间监控未触限。源码10769项与冻结内容/大小/执行位一致。
干净功能提交a97c720上，原docopt0.6.2唯一CP/compile/JIT窗口严格通过，四条原
SyntaxWarning保留；原Markdown3.5.2的CP与golden一致、compile首次成功，JIT exit1
首错为ImportError: cannot import name 'ClassVar' from 'typing'，stdout空。
原20包根3187文件、原入口/golden、工具前后审计一致；未改包源、无重试或缩小断言。
仅计真实编译阻断推进，不计Markdown成功或合并不同提交/选项的项目通过率。下一切
依据ClassVar真首错处理通用语义，不以空导入桩或擦除运行时类型冒完成。
恢复继续标准tar.xz六件、严格低于32MiB，保全部旧新源/失败/性能流，独立实际恢复后
再更新备份；最终证明可为外部后置证书，不为元数据循环重打归档。
