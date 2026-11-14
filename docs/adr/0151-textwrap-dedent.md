# ADR-0151：owner局部的有限textwrap.dedent（实施候选，待产品验收）

- 日期：2026-11-10
- 状态：生产与专项源码候选。没有可用本地.NET SDK，尚未编译C#、JIT、NativeAOT或完整门禁。

## 背景与决定

Markdown extra的依赖扫描发现缺dedent；它是通用文本API，不按项目名分支。
本切只实现该API的有限合同，仍有re.Scanner、XML TreeBuilder等后续依赖。
不改上游、固定入口/golden、固定20分母或通过数，不预告Markdown转绿。

保留public CreateAttributes、进程单例旧字典及唯一无参private Build行为。
TextWrapper/wrap/fill与其它旧属性仍只有该字典一个权威，旧闭包和FastPlan不迁移。
四个新增名字dedent/re/_whitespace_only_re/_leading_whitespace_re只归真实导入的owner.DynamicAttributes。
Python setter与getter按名字分域，不向旧字典镜像；旧宿主直接读取CreateAttributes不承诺看见新字段，
直接手写新保留键也不成为Python setter域的支持方式。无新增public CLR API或热ABI。

textwrap使用既有RegisterNativeLazy生命周期。首次Import时先真实Import re；失败原异常传播，
initializer不发布新字段，重试新owner，不回填逃逸旧owner，也不清除用户已写字段。
源re成功时旧wrap/fill继续可用，仅新dedent明确NotImplementedError。
源码必须已在编译图登记，旁置re.py不等于可导入；未扩编译器发现规则。
旧三导出提前可读的部分初始化行为仍是既有边界，不宣称完整CP初始化等价。

## 首次有限准入

在实际创建并登记re的那份字典时保存compile对象及Body/Signature/真实数组引用与内容。
首次textwrap初始化只接受native来源、同一compile对象、exact CLR long的MULTILINE=8；
Names/Kinds/HasDefault/Defaults数组必须保持登记时形状和值，不把readonly引用误说成内容不可变。
__name__/__module__/普通__defaults__假属性不作调用见证；不改变全局函数元数据协议。

已有native re若挂载_compile/_compiler/_parser/_cache/_cache2/_MAXCACHE/_MAXCACHE2/_compile_template，
新dedent明确NI，无论所挂值是什么，不调用/转换它；这些普通setter本身仍成功。
此有限缺席门不证明任意CP私有backend替换正确。导入后私有backend改写、内部突变仍在既有re域外，
不新增全模块扫描或动态backend追踪。

准入后实际用compile调用两条固定模式及显式flags=8，保存真实PyRegexPattern，
公开pattern使用与CP逐字相同的TAB/LF字符，不只是正则等价的转义文本。
这两次不是CP旧TextWrapper在内的五次compile初始化；不执行用户代理两次冒充全序列。
source/自定义compile/非规范flags/backend状态只令新API NI，不伪造两个private对象。
首次绑定完成后公开compile/MULTILINE再改不重编译或重新拒绝已保存Pattern。

## 每次消费顺序与结果

正常参数绑定先完成，支持text=与四类绑定错误。
准入检查后，先验证_whitespace_only_re身份，再读取exact str或str子类的raw payload，
不隐式调用__str__/strip/splitlines/expandtabs；bytes/bytearray/memoryview及其它对象TypeError。
只将按LF划分的全ASCII space/tab行清空，再验证_leading_whitespace_re身份并计算字符共同前缀。
因此坏subject优先于坏leading，而坏whitespace优先于subject错误；无margin也经过两private阶段。

无变化exact str返回原对象；str子类结果为base str。CR、NBSP、Unicode分隔符不当LF或缩进。
margin为空直接返回归一结果，不读取owner.re/sub。margin非空只支持同一初始re模块；
其它re对象普通赋值成功，但新dedent消费时NI。同一re每次读取当前sub并真实调用：
sub('(?m)^' + margin, '', normalized)，恰三个位置实参、零关键字。
任意返回对象、用户异常身份、noncallable TypeError保留；不预先删除margin或冻结sub。
保存dedent别名始终读原owner，公共dedent重绑不让旧别名转向新函数。

## 成本、边界与验证

两次扫描及归一分配总O(N)，不按行复制剩余全文；无变化归一不分配字符串。
只有dedent新路径和一次注册/首次导入有新增状态成本，不宣称实测性能无回退。
最终stock re仍受原pattern长度16384和译后主体200000字符两个既有预算限制。
纯空格margin16379虽未超原pattern限制，当前翻译会先超译后预算；16380先超原pattern限制。
正门仅选保守小margin，不宣称实际最大可接受值；两种大margin的CP成功正期待保pending。
不能在自定义sub调用前套这些stock限制。既有2秒是单次Regex.Match预算，不是整个dedent预算。

公开private Pattern替换、不同textwrap.re、完整sys.modules替换/native __dict__/__globals__反射、
完整CP初始化hook序列及其它未列textwrap API均不冒支持。
CP正期待的preimport五compile/private代理/source代理、大margin和完整__getattribute__ trap保pending，NI负门不替代这些正门。

验收分普通CP差分、NI负门、fresh宿主首次状态、CP pending与旧保护。
普通C#专项全经Import取得dedent；fresh专用Fact在普通发现中明确skip，
专用CI每场景单独进程、唯一实跑Passed无skip，并检查此前re/textwrap未缓存，不加产品reset。
真实源码re成功/失败重试另走实际Python编译图与JIT/Native；宿主Register测试不代签源码图。
本地CP自检首批四常规与source代理pending通过，preimport pending初稿字符串比较错误保原失败，
修正后唯一另轮自检通过，不合并冒充首批全过。
独审002更正普通fixture的HIR不支持__getattribute__与stock资源成功假设；
常规改用受支持的property sub零lookup，原完整trap及大margin的CP正期待独立保pending。
修订常规sub_dynamic及两个新增pending按新身份单批CP自检均exit0、stderr空；
产品阶段结果仍由独立同源CI原流证明。

测试清单与当前状态见[textwrap-dedent.md](../textwrap-dedent.md)。
