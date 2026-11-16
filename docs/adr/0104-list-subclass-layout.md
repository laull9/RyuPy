# ADR-0104：真实 list 单继承布局的有限默认协议

日期：2026-11-05。状态：有限支持域已验证，正常完整门禁通过；未远端发布。

## 依据与表示决定

177cd406的docopt0.6.2原固定入口只报TokenStream(list)与Dict(dict)两个真实基类E0100；
CP原golden一致且原四条SyntaxWarning保留。先完成ADR0103必要精确列表协议，275ff68完成
门禁及恢复后才接本切。不是对TokenStream按名字特判，不改原包、入口或golden，dict仍独立。

新增独立PyListInstance : PyInstance，持不可替换的PyList Payload引用。PyList保持sealed、
原两背板字段及数值岛ABI，普通PyInstance不新增payload。公开身份始终为wrapper，属性槽、
class和别名不随初始化/原地操作更换。相较精确list增加wrapper及payload间接读取，非零成本。

HIR沿既有Builtin::Dynamic("list")及runtime base路径识别canonical list与真实别名；
不是异常专用BuiltinBase，也不按名称猜布局。隐藏ClassId和公开module slot继续分离，
Validate→Make→Finalize→decorator→publish有独立HIR/MIR结构测试。失败不发布半成品类。
类增加冷layout/ready状态，late变更复用GlobalVersion，无反向子类图或每次元素读祖先遍历。
根描述符缓存只以有限支持名为键，不保存用户实例；绑定方法随持有者存活，固定self引用是
Python保存方法合同。游标仅持payload与索引，首次耗尽释放current；不建无界迭代快照缓存。

## 分配、初始化与普通属性

默认allocator先建空payload。custom init收到完整原args/kwargs，不先按list的一参数绑定；
默认init先绑定零或一位置、无keyword，再clear→消费source，失败保已提交前缀。
root list.__new__(cls,...)忽略额外参数只分配不init；object.__new__(list-subclass)拒不安全分配。
显式root init可重新初始化同payload，attrs/旧活游标的存储引用保留，self来源clear后为空。

property→实例属性→用户类链→list根fallback→__getattr__顺序保留；普通方法覆盖、保存绑定、
super、late写删及子类继承可用。方法/属性先于实参展开捕获，不让missing属性晚于star失败。
root描述符绕目标覆盖，绑定__self__是公开wrapper；root str沿object的repr语义，
list.__str__与object.__str__共享同一描述符，不把用户repr异常包进旧广catch重写上下文。

默认分配/初始化、__repr__/__str__可用。custom new、核心len/bool/iter/get/set/del/contains、
算术/比较/hash/反射覆盖、布局/元类/拦截槽与一般自定义descriptor在完成/late更改前明确NI。
__getattr__缺失fallback与普通业务dunder允许。__dict__/__mro__等未声明元数据不冒完整支持；
当前真实type/isinstance/issubclass、__class__/__base__/__bases__独立验证。
新list类repr只读自身module/qualname，字符串子类取原payload；builtins或非字符串module
回Name，空module保点号，不调用用户str/repr，不从父类继承元数据。

## 值协议与接线

长度/真值、整数索引、有限切片、contains、正向与反向活迭代、十一命名方法、拼接/重复、
原地操作、比较和默认不可hash接同一payload。+=/*=返回原wrapper，copy/slice/+/*产新精确list，
保元素引用、不复制attrs、不调派生init。self扩展固定原长；generic扩展复用ADR0103流式合同。
反向迭代只捕获初始尾下标，再读实时存储，越界/耗尽永久结束，不沿旧reversed全量快照。

root槽不注入全局PyObject.TryMethod，普通二元运算仍先用户/foreign槽，再默认list fallback，
显式root运算不反射。list/list比较不全量快照，元素相同身份跳eq，eq回调后重读长度；
元素strict-subclass富比较优先、NI槽不重复。元素eq依RichCompareBool对合法结果求真，
排序元素返回非bool则明确NI且不再调用其bool，避免现全局bool ABI静默吞结果。

新wrapper表达式Eq/Ne有冷局部分派：foreign显式ne不改写成eq；只有缺ne时才执行
object.__ne__的eq/Truthy取反，NI继续身份fallback。直接foreign Eq/Ne的非bool结果明确NI，
容器内部eq的Truthy合同不变；未借此全改普通对象比较ABI。

repr循环守卫按公开owner，嵌套元素仍调用其repr。空dict/set/frozenset的无comparer路径
也拒list派生键。ABC在真实内建资格外纳入根slot，例如新增__next__的list子类可结构识别Iterator。
Unpack/Ex、zip_longest、namedtuple与mappingproxy的独立消费入口接默认迭代，不只修改for。
JSON按list内容并保owner循环检测，不误走default或序列化attrs；bytearray来源/切片可迭代。
普通dataclass的真实派生可变默认值必须拒绝，default_factory可返回新派生实例。

## 明确支持边界

- root sort只接key=None、同原生numeric族（不含NaN）或无代理码元BMP字符串；回调key、
  自定义元素、混合text/numeric及非BMP/独立代理码元明确NI，执行排序前不改列表。
  reverse按固定CP3.12的truthiness转换，不凭其它版本记忆改成index规则。
- 容器slice仅既有原生窄整数域，用户index/宽整数/宽step明确NI；step=0先于RHS消费。
  RHS先源iter，再cursor iter/hint，完整物化后写入；self直接raw浅拷贝。仅getitem来源及
  RHS缩短目标导致扩展slice旧下标越界的域明确NI，不泄漏CLR错误或提交部分赋值。
- copy/deepcopy、textwrap内部算法特殊载体仍明确NI。没有完整pickle/weakref/GC/refcount承诺。
- 精确list原直接方法与全局range/字符串排序/比较不整体迁移；新根与派生默认支持域不能
  外推为全部旧直接方法错误已修。数值岛guard对新wrapper失败时不读payload或调用index。

## 验证演进与原失败

六份独立CP合同含92项硬编码断言，只是CP参考，不把合法暂缓域算已实现。
三原应用从备份逐hash恢复，CP3/3，275ff68原源编译0/3（E0100未知list），未启动JIT。
首Release6f696c原三应用3/3及首新增C#38/38；并行源码仍在完善，首产物已独立冻结，
不能将稍后采集源码hash冒称其构建来源。第二源码冻结/前后hash校验后Release2fb267再3/3。

第二相关C#68过/2败均为旧list能力拒绝断言：canonical list不准作基类、list根new缺属性。
保原文件与失败流，窄迁为新真实准入/严格绑定，同时补保dict仍拒绝，不删其它原断言。
首新增MIR测试误写不存在的Statement::Call导致编译失败，已改为实际Eval(Call)，原流保留；
独立HIR4项及MIR1项通过，不冒称是SDK/JIT验收。

独立审查对2fb267冻结四个真实反例：Unicode字符串sort按UTF16错序；普通dataclass静默接受
派生可变默认；foreign !=调用eq而非ne导致错结果；Iterator ABC遗漏继承根iter。
四个CP/compile/JIT原流分别保留。前者收敛BMP域并保CP合法源pending，后三者做新布局局部修复，
不更改全局比较ABI。reverse转换的审查假设已被固定CP实证否定，没有按错误猜测改生产。
第四冻结Release 96359badf2b353e0d80643b1967a8f9f8188f4ddbaf6b7f5cf06720ff28c4752：
原三应用、新三回归均CP/JIT各3/3；原三源仅pending行换空行后再3/3，golden与帧位不改。
独立原反例复验后三项严格stdout一致，Unicode只记明确NI，不能计CP成功。
全C#首1924/1926，另两条旧runtime-base list拒绝断言保原流后窄迁并补dict守卫；
最终1927/1927（基线1877，新50），零跳过。第六最终候选正常完整make check一次通过：1927 C#、228 CP基线、202 e2e，30原ignored；
fmt/clippy、全Rust与多文件自检全过，六unique driver应用与HIR/MIR结构均在该门禁重跑。

## 真实成本检查及热路修复

同一生成CIL/host、预绑定delegate、不包含每次reflection invoke；旧runtime2d268与第四
候选96359用安静ABBA，18项旧/34项候选，每例7样本和至少18960实际迭代，保所有原流。
发现精确append/pop由93.0–97.6ns退到183.5–206.4ns，extend也固定增加约45ns，
B/op不变。没有用其它快例平均遮盖，也没有把普通实例字段未变推成零CPU成本。
定位新增三处canonical list身份比较在非PyType上也调用PyType.Get字典查询；第五候选
只加PyType前置短路，保真实身份/捕获顺序/参数展开。修前原流、源码与产物hash全部保留；
第五仍有小append/pop约13–29%额外，故第六只隔离sealed精确PyList的冷布局检查，
不改wrapper行为。最终runtime fab08633d4b3283c9a2d5c82be372d3c790cef7ddb4aa0c40ae00797bebfeca7
同host ABBA四次exit0：n8 append/pop旧92.9/100.6ns、新97.0/99.5，落旧两run范围；
n256旧87.6/96.6、新100.0/98.6，仍约2–14%额外；extend256约2–7%额外。共享CPU、
无affinity，末次baseline多项上漂；保每run及每sample，不把小差异都说成已证明噪声。

ordinary属性0B/op、普通实例64B/op，所有exact案例B/op不变；PyInstance/PyList字段、
sealed/base不变，PyClass新增两bool。真实wrapper append/pop96B、extend64B、live312B
（exact248B）；wrapper构造n8/n256为192/2176B，exact另一路272/4240B，不能据此说
wrapper存储免费或全面更快。每例7样本，保存实际迭代/GC，不横比案例数量不同的run总GC。
三轮负面/修复证据全留，candidate-v2只准备未执行，不拿它作量测结论。
docopt尚未通过，未更改固定20项目分母。


## 提交后原项目窗口

功能提交79a04f7的未修改docopt0.6.2固定入口：CP一次exit0且原golden一致，四条
SyntaxWarning共732字节与177cd406原窗口逐字相同；compile一次exit1仅余Dict(dict)
在docopt.py:488的E0100，TokenStream(list)旧诊断已消失。未启动JIT，不计项目通过。
官方tar/28源码/入口/golden与前轮逐hash同，运行前后源码/输入/工具及干净HEAD/tree不变。
本轮只此项目窗口，无全矩阵/AOT/跨平台；结果文档提交不改变已门禁的生产源码。
