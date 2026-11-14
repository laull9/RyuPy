# ADR-0063：字符串子类的真实布局与有限协议

状态：有限布局与协议专项及正常完整门禁本地通过；未发布，不代表完整字符串子类或pyfiglet通过。

## 背景与范围

固定项目诊断已定位到真实str子类定义，不能只放行基类检查并返回无文本的普通实例。
C0先保str注解的对象身份；本切另需真实不可变文本布局、实际类/普通属性、默认构造、
值协议、继承字符串方法与公共str/repr的对象返回身份。exact CLRstring路径继续保留，
不把全部字符串包装，不引入rope，也不宣称未经测量的性能收益。

阶段A采用独立String布局标记而非异常BuiltinBase，真实载体保存readonly文本与实际类。
阶段B闭默认序列、混合字符串比较/hash/字典键和常用ord/int/float入口；阶段C闭已有字符串
方法、根descriptor、普通覆盖、saved绑定/PIC/super以及str/repr转换。所有阶段合闭后才能
登记有限支持。path/re/json、完整字符串算法/Unicode、元类/多继承与未纳槽覆盖不自动支持。

## 公开绑定与首轮进展

复用现Runtime基类的隐藏ClassId和独立公开模块槽：类体、finalize和decorator成功之后
才提交公开名字。类体副作用不回滚，失败保旧公开绑定；实际布局资格由运行时真实基值
决定，预扫描名字仅用于保守登记，不按项目或类名批准布局。

已独立审并接线三个HIR文件与四项结构测试，首轮4/4、HIR全库65/65。
覆盖直接str、别名、三级后代固定点、同名源码遮蔽/重定义及公开槽；不把结构验收冒充
真实构造、decorator失败或循环导入的运行结果。该阶段runtime载体与消费者仍在隔离实现。

## 语言参考与有限边界

除此前12组设计参考，另两次分别限30秒的固定CPython3.12.13参考完成：allocator五组
exit0/空stderr，以及ord/int/float四组原行为记录exit0/空stderr；未执行新项目、AOT或性能组。
它们确认S(object)/object=与显式str.__new__转换一次、分配后init接原args/kwargs；显式
rootnew不调用init。ord直接读文本，int/float先用户转换槽再默认文本，用户错误不能回退。
合法encoding/errors decode域保长尾，不把CP成功参考计为RyuPy已支持。

未实现值槽覆盖须在类发布前或late写删之前明确拒绝并保attrs/version原子性；普通业务
方法和非协议dunder数据不能泛禁。__str__/__repr__及普通方法覆盖是最终闭合必需，
不能通过raw读取绕过；内部打印与公共对象返回须分开，避免重复调用或剥去用户返回身份。
String域__dict__/复杂布局与元类等有限边界明确，普通类既有行为不在此切顺改。

## 验收约束

分阶段真实C#与结构测试，再合并有限CP/JIT原流差分、正常完整门禁和可恢复备份。
未审源码不批量接线，所有失败保留。以下分阶段记录不预称最终测试、项目通过数或性能收益；
不自动推送、启动大型原生矩阵或重跑历史固定36性能窗口。

## 阶段A内部首次编译

已接入独立String类标记、readonly payload载体、ready状态与构造/属性冷守卫。
12项C#首次通过，包含unsafe object分配、仅set/delete描述符与dataclass旁路拒绝。
当时是内部exact文本分配阶段，任意对象转换/rootnew及默认消费者仍待合并；不能当作公共
str子类已可用。公共str调用编译分流ty专项1/1，MIR首例因把saved alias的实际`dyn.call`
误期待为RuntimeFn::CallValue显示而失败，随后做测试表示窄修，生产未据此改动。

保已证明primitive/exact字符串单参数的旧转换路径，动态函数中的真实HIR字面量亦可
直接沿原helper；变量/Opaque/多参/keyword/star走完整object绑定，不让首参吞掉其余求值。
该阶段转换源码尚未通过真实应用验收，完整门禁待最终合并。

公共转换内部阶段已接线：新增12项C#与原A12项合计24/24首次通过，验证S(object)及
object=、真实rootnew内部入口、原args/init和StrObject/ReprObject返回身份；未把内部
直接调用当作公开source rootdescriptor已验。MIR测试仅改真实alias `dyn.call` 表示后1/1，
原typeobject/唯一调用/无静态字符串转换/primitive快路断言保留。首失败记录不删除。
当时B/C消费者、公开根descriptor及最终三源差分仍待合并；完整门禁尚未启动。

## B/C接线前的必要一致性检查

内部阶段运行时全库首次为751/752：唯一失败是旧RuntimeBases负例仍把canonical str当作
不支持基类。该断言仅迁为仍拒绝的list，保完整caller/context和消息；str正准入已有A专项。
当时修后全库尚待执行，不能把首失败写成全部通过。

比较尾审发现仅对子类改码点排序会与exact string既有UTF16 ordinal排序冲突，甚至破坏
等值替换和传递性。因此本切exact与新子类统一沿现ordinal，补表示一致性测试；正确的
CP E000/astral排序原正期望完整保在unicode_order.py.pending，不计当前通过。
这保留了旧Unicode排序偏差，未宣称修复全部Unicode比较，也未更改exact热路或测量性能。

B/C最终源码经独立审接线后，四类字符串C#专项首次36/36通过（布局12、对象转换12、
值协议/方法9、根init3）；旧RuntimeBases迁移后单独6/6通过。三源CP/JIT差分与MIR组成的
4项driver当时正在首次验证；不能把C#绿当作源级行为或项目验收。当时全库与正常完整门禁待后续。

同一候选Release运行时准备成功，driver首次4/4：1项MIR结构、3项完整原流CP/JIT差分。
包含真实根init、构造/公开绑定、普通方法与saved/PIC、numeric绑定及完整错误链；
不是4个应用，也不计两份pending通过。该阶段有限源码行为专项已闭，当时正常完整门禁尚待。

正常完整门禁首轮止于strict clippy：新类型测试使用filter_map(bool::then)，未进入完整
测试阶段。仅改为等价filter后map，筛选集合、顺序及原断言不变；不加allow、不改生产。
正常第二轮随后通过764项C#和228项CP基线，停在原CIL快照期待；首日志与状态保留。

## 快照迁移范围

一次枚举25个独立snapshot目标内70个实际快照函数，66通过、4个旧期待失败。
只对真实生成候选逐份归档old/new SHA并独立审查，不使用force pass或全量接受。
未知对象str从DynStr改为完整object binder，MIR保同source/dest；CIL每点新增参数数组
和完整绑定，严格映射branch/leave/switch及EH端点，未改变locals、签名与其余指令。
后续只复跑受影响函数按原顺序发现后级CIL，不重复整套70函数或用全门禁逐份挤出快照。
该改变有真实调用与分配成本，不称旧新ABI/性能等价。当时最终迁移和正常门禁终态待记录。

最终仅8份快照迁移：3 MIR、5 CIL，分四轮实际候选分别1/3/3/1，完整SHA均独立核验。
受影响四函数随后正常通过，未留下.snap.new。随后启动第三次正常完整门禁，运行期间未预标通过。

## 最终本地终态

第三次正常make check退出0，fmt、strict clippy、workspace测试、764项C#（无失败/跳过）、
228项CPython基线及202项e2e+30原ignored全部完成。原36项字符串、6项RuntimeBases与
4项driver仍在正常门禁中通过；8份快照仅对应已审有意调用变化。日志SHA256：
`60535a1fb544279e6460f8f044f56bdbcf3515d5d245cb5f1375daf61474097f`。

源码/测试/文档保持独立未发布批次。该门禁阶段未重跑pyfiglet固定入口，未增加项目通过数，未运行
NativeAOT或性能组；新对象载体、binder与注解object表示的成本仍待单独衡量。

## 固定项目入口后续验证

完成备份后，以未发布候选d231f5a执行一次原pyfiglet1.0.2固定入口（显式包含pyfiglet.fonts）。
官方归档及572源码逐成员hash不变，CLI一次准备成功；CP/compile/JIT各一次，退出0/0/1，
总0.895秒，无timeout，仍按总180秒及30/90/30秒阶段上限、首失败停，不是性能测量。
本次确已越过旧376行类定义阻断，进入loadFont；原错误链entry4→862→869→129→367，
内因“object of type 'map' has no len()”被包装为FontError。内因无内部帧，不能仅靠该日志
断言唯一内部表达式，也不能把进入字体解析当作完整渲染或项目通过。没有AOT运行。
独立核原6流、3编译产物、输入/工具hash及全部源码；result SHA256为
`b910c4c5bd8780d8ce6474aad1bc4260e634254c69c9b476e14209dd15063abb`。
