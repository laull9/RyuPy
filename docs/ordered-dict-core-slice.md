# OrderedDict 有限精确原生类型首切

日期：2026-11-06。参考固定CPython3.12.13默认加速OrderedDict。
冻结candidate-003及同源canonical正常make check均已完成有限验收。
这不代表完整collections、Markdown或20项目矩阵通过，也不等于远端已发布。

## 类型、布局与适用范围

真实导出`collections.OrderedDict`，稳定类型对象、dict/object祖先及Mapping/
MutableMapping关系，不是dict别名、函数工厂或项目专用替身。精确native载体组合
sealed PyDict保存raw插入序，OD专有双向链和键到节点索引保存公开顺序。普通PyDict
字段和存储算法、普通PyInstance布局不变；不实现IPyNativeMapping，也不扩大通用
PyDictLayout.TryGet，防止现有消费者错误剥raw。无Rust源码、RuntimeFn或CIL ABI改动。

键域为精确None/string/bool/int/long/BigInteger/double，双索引使用同一Python键比较，
保首次key身份、True/1/1.0合并及None正规化。tuple/bytes/用户hash键是CP合法暂缓域，
明确NI，不偷偷运行hash/eq，也不说成非法。值保存原对象引用，别名不复制值。

查询、覆盖、删除、重排和两端pop按有限键域均摊O(1)，遍历O(n)。这只是结构复杂度，
不是实际速度或无回退证明。PyDict加节点索引/链节点带来真实额外空间与分配。

## 核心API及两种顺序

支持空或一位置mapping/pairs及kwargs构造、重复init、update、下标读写删、contains、
len/truth、get/pop/setdefault/clear、move_to_end/popitem、copy/fromkeys和循环repr。

- 覆盖已有键不移动位置；删除再插入移到末尾。重复init更新同一对象而不清空。
- move_to_end的key/last可关键字，popitem支持last；last走真值协议而非index/仅bool。
  参数绑定先完成，再truth，再按回调后的容器状态查询或弹出；用户异常身份不改。
- 真正移动改变OD链序，no-op首尾移动不失效；底层raw插入序不变。
- 保存的bound method、已有view、容器别名仍指同一owner；浅copy独立顺序并共享值。
  fromkeys保重复键位置，所有键引用同一default。
- repr按CP3.12的`OrderedDict({'a': 1})`形式，空对象`OrderedDict()`，自环有保护。

公开`keys/values/items`为真实odict视图，正反向遍历均活读当前值，非快照。每个游标
独立；覆盖值与no-op move可继续。仍有next-key时实际重排/增删使首次next抛
`RuntimeError('OrderedDict mutated during iteration')`，随后永久耗尽。若已没有
next-key则优先StopIteration：初始空游标、最后键已yield后的修改均不能被版本检查
错误变成RuntimeError。raw dict根视图不因仅移动OD链而失效。

视图/游标的有限实例级及类级ABC身份已验证。values视图同一对象按身份比较，
不同对象的显式eq/ne返回NotImplemented，表达式保相应兜底；完整集合操作另切。

## 独立的流式更新与比较相位

OD.update不调用dict.update的算法正文。mapping先读keys属性，再调用keys，随后
逐次next→getitem→写入，不先物化全部keys。keys返回活list时，getter追加项可被
后续读取。只有keys属性读取的AttributeError回退pairs；keys调用失败不回退。

pairs逐项N+1解包，第三项足以拒绝过长pair，不继续读第四项，不求len/hint。长短
pair及不可迭代项使用OD对应错误，inner用户TypeError不替换成dict特定文案。已成功
前缀保留，位置源成功后才写kwargs；绑定错误不消费来源。用户回调可修改目标，
之后使用当前状态；原异常、cause/context不因更新器吞换。

OD对OD先沿raw插入序比较映射内容，成功后才比较公开键序，即使链序已不同也不
跳过value回调。同一value身份先短路，非bool值比较结果按容器合同求真。OD与精确
dict比较忽略顺序，plain dict在左仍保OD子类型优先与value方向；兄弟dict子类在左
则按该左侧的raw顺序，不强行套OD优先。显式dict根eq/ne仅看映射内容。

外部eq/ne同名槽独立分派，不用!eq代替显式ne。表达式结果需bool，任意对象返回值
受现有比较ABI限制而NI；结构比较重入亦NI。递归比较使用256层安全守卫，不声称
复刻CP递归阈值或sys.recursionlimit。与Counter/defaultdict等原生映射比较仍NI。

## 空move与异常边界

move先绑定、执行last真值，再读取当前Count。空目标只准精确标量和确定空的精确
list/dict/set/bytearray走缺键错误；tuple/bytes/用户键/非空容器在任何hash/eq/repr
前NI，不递归窥探未知容器。不把“没有调用用户代码”或声明NI算成CP等价。

缺None的move在空/非空目标都产生`KeyError.args == ()`，仅用Move专用叶子修正。
已存在None、truth清空/重新插入None及四空builtin相位已有完整差分；getitem/delete/
pop和全局KeyError未改。空popitem为`KeyError('dictionary is empty')`；空OD的
pop不可hash键仍TypeError，不能借用普通dict空pop的早返回。

独审07完整程序覆盖CP合法但本切暂缓的未知空key，003仍与CP不同、继续pending。
独审08首次CP因观察`exc.args[0]`自身IndexError，原源/双流保留；未修写它，而另写
完整09安全观察None空args。09对应病例22，最终003严格通过。

## dict根与消费者接线

| 入口 | 本切行为 |
| --- | --- |
| dict(od)、dict.update(od)、dict派生目标接收od | 按OD公开序复制；目标有限标量键守卫 |
| 调用**含OD | 按公开序，沿严格合并器检查与dict/proxy/显式关键字交叉重复 |
| dict.get、getitem/contains/len、keys/values/items、iter/reversed、repr | 有限只读根读取raw内容/插入序，不误报OD不是dict |
| dict.copy(od) | 产生精确dict，按OD公开序，而不是raw.Copy |
| 显式dict根eq/ne | 映射内容比较，不比较OD链序 |
| dict根写入、init、布局分配 | 先验证签名，合法暂缓操作写前NI，不制造raw/链不一致 |
| union及dict根or/ror/ior、dict派生原位union接OD | NI且目标未变，不把update复用成偷偷支持union |

OD receiver在现有dict根TryGet错误之前识别；没有把所有消费者统一剥payload。
不含OD/proxy的旧**路径未重写，但这不等于新资格判断完全没有CPU成本。

JSON在用户default回调之前NI；defaultdict/Counter消费、函数metadata、XML属性、
logging映射以及Partial/LruCache/Optparse实例字典替换等未接通入口明确NI。
mappingproxy、copy.copy/deepcopy沿现有native NI，不能自动降为dict。

Python派生、任意实例属性与槽shadow、非标量hash键、union、GenericAlias、复杂反射、
与set/普通dict_view的完整比较和集合运算、view.mapping、pickle、weakref等均未开放。
不以导入成功或空对象路径代表这些能力。

## 最终候选与31份完整合同

基线HEAD：`21748729ba978393bd4da46fe7a35ca4ca9c1835`。
最终003 Release DLL SHA256：
`fb5892bc74c102f24b7829c43695f64ca7258746513a9ba9f0d31a6a3c87c94e`。
完整runtime源归档SHA256：
`e2023154870ca2a8fab0a8bb60e5785a11f980dda70db0d9faf49132b1251f94`。
源manifest SHA256：`85456487b9b7c32aa576787ff5df8be43deeab7364d99b610ab0c3309b821bfb`。
36个新增/修改文件，605份runtime源；所有修改代码≤600行。没有new HEAD时不能用
文件hash伪造提交号，也不能把其它工作目录构建的不同DLL混称同一冻结DLL。

attempt-011的最终39/39专项及attempt-012同DLL的隔离全C#2577/2577均通过，
无skipped、stderr空、零warning，源码前后hash一致；这是隔离测试记录。
之后另在canonical完成正常make check，具体计数与DLL身份见下节，不混称同一运行。

31份完整CP3.12.13合同的最终真实PE实测为**22严格通过、9继续pending**，全部原源
前后hash不变。严格通过包括01–12、14–22及原完整reference03；文件名带pending的
reference03已整源通过，不能只按文件名计失败。INDEX原“待PE”分类是计划历史，
最终结果以本轮result为准。canonical已将22份转正、9份保pending，只改pending元数据/
首注释；31份执行AST及原golden均核同，不改执行断言。

9份完整未过源：13_update_failure_prefix、pending_dict_root_mutations、
pending_empty_move_user_key、pending_extended_protocols，以及reference01、02、04、
05、06。原六份参考源全保留，没有为了正例删去派生、extra或其它困难分支。

原13先在`error.__cause__ = cause`遇既有异常属性setter债，尚未进入相关OD更新路径。
旧XML runtime和候选均已独立复现；它仍pending，不删断言、不把异常框架混入本切。
独立19用真正raise-from覆盖用户异常身份、cause、真实context、失败前缀与kwargs
时点，完整通过；19不替代13。独审最终09/binary/roots/compare/sibling五份完整源
严格通过，独审07的未知key程序仅完成NI边界核验，不计CP成功。

## 首次失败与12次实施attempt

| attempt | 当次事实与后续处理 |
| --- | --- |
| 001 | env.sh隐式cd canonical，MSB3491只读缓存写失败，未编译/测试；后续恢复隔离cwd |
| 002 | NuGet审计刷新共享HTTP缓存受只读限制，NU1900；未禁audit/降warning，另复制已有小缓存 |
| 003 | 编译完成，29专项27过2败：type测试入口误用，以及OD **重复关键字真实漏检 |
| 004 | 修正测试入口与OD严格合并，29/29专项通过，零warning |
| 005 | 同candidate-001冻结候选全C#2567/2567通过，不覆盖前三次失败 |
| 006 | 002候选专项35/35，修复真实PE与独审反例 |
| 007 | 同002候选全C#2573/2573 |
| 008 | 空move限域中间源码38专项通过，尚无最终None叶子 |
| 009 | 同中间源码全C#2576通过，不能代表最终003 |
| 010 | 39专项通过，仍在最终测试补充前，不挂作最终003验收 |
| 011 | 最终003源码39/39专项通过 |
| 012 | 同最终003源码/DLL隔离全C#2577/2577通过 |

001曾把比较分支误插BinaryCore，Add/Eq与Sub/Ne编号重叠使OD加减返回bool；独审
保完整首次反例后002移除误分派。union根路径曾偷偷写入，后写前NI；view类级ABC
与values显式eq/ne亦保首次失败后修复。错误全限定名与空popitem引号是OD局部修复，
没有改全局异常。002的空move曾提前repr未知key，甚至替换KeyError；003改为域外
repr前NI，再只对Move缺None修空args。所有旧候选、原CP、原失败双流与退出都保留。

## 原项目窗口与支持率

隔离003及随后canonical最终Release同组窗口均复用已冻结的CP参考，没有重跑CP
或20项目全矩阵。canonical原docopt compile/
JIT均0，stdout16B对齐golden、stderr空。默认Markdown compile0、JIT1，stdout空、
375B旧find_spec source metadata NI逐字不变。四源窄Markdown compile0、JIT1，
stdout空，首错由缺OrderedDict推进为57B `ImportError: cannot import name 'NoReturn'
from 'typing'`。两段原入口、extra及所有项目源码未改。

这只证明该配置越过OD导入，**Markdown仍未通过**，不增加项目分子，不改历史同
提交矩阵，不声称原库全部OD分支或XML/Markdown serializer已执行完。

## 正常完整门禁与构建身份

canonical正常`make check` exit0，耗时254.160611秒；fmt/clippy通过，Release零warning。
5项manifest检查、631份固定CP基线、2577项C#、900项非e2e Rust、631份case框架
自检、401项真实编译e2e通过，234 ignored逐项保留。前后source清单完全一致，
窗口root最低可用1056272384B；未改正常命令或框架以绕过失败。

最终canonical Release DLL SHA256：
`448a3fec7c2c6189e51699903ea8be3dd1a26825240779f5e628228e1e6430e9`。
605份runtime源已逐份核同隔离003manifest；构建目录不同，两份DLL的hash也不同。
**ABBA实测对象是隔离003的fb5892…，不是canonical的448a3f…**；不能把同源证明
写成同一二进制，也不能把已完成性能测量换挂到未测DLL。canonical最终三项目使用
448a3f…，结果另留原双流与hash；有限31程序和隔离专项保各自原003证据。

本切验收不等于远端已发布。本文不预写未来完成，也不嵌入自身提交号；具体commit、
恢复包及核验结果由交付记录另列。

## 真实成本与开放风险

PyDict.cs保持sealed、字段及热存储算法原字节不变；普通PyInstance不增加payload。
但PyOps.Compare在精确数值/字符串早返后、旧dict比较前新增OD/OD-view判断，普通
**dict对dict相等也会经过**。PyHash.Equal在标量/身份早返后新增相同分支，Of/
IsHashable、update来源、**资格、根receiver、JSON/logging/metadata等入口亦增加
判断。不能说“普通dict热点都没改”“零CPU成本”或“无回退”。

OD专有PyDict、节点索引、链节点、绑定方法、view/iterator均有真实分配成本。实际
JIT分支、冷启动及旧路径影响由最终同PE/runtime测量决定，保留全部不利样本，不能
以静态复杂度、布局不变或局部收益关闭历史风险。没有新AOT/跨平台验收声明。

以下为已经发生的有界观测，不借用XML门禁或其它候选数据：

- **唯一轻量性能窗口已完成，风险未关闭**。同PE、host，仅换旧3963737与最终fb5892
  runtime的默认ABBA均exit0，120正式和48暖样本全保留。getitem两对+15.279%/+7.239%，
  setitem+5.629%/+5.546%；contains首对+29.626%，第二对−0.382%；equality第二对
  +36.812%不能被汇总−2.158%掩盖。迭代汇总−69.057%受旧A1/A2漂移限制，不称提速。
  未采分配/GC、OD自身吞吐或JIT事件，未归因、不加跑、不关闭S1/B2等历史风险。
  完整表、样本范围、输入hash与方法边界见[原样性能报告](ordered-dict-performance.md)。

## 证据索引

本轮完整证据分别保存在`ryupy-ordereddict-implementation`（REPORT、三候选、12次
attempt、base/new文件hash）、`ryupy-ordereddict-review`（REVIEW、独审原源及首次/
最终差分）、`ryupy-ordereddict-cases`（INDEX、完整entry/golden与首失败）、
`ryupy-ordereddict-validation/candidate-003-final-31/result.json`和
`ryupy-ordereddict-validation/projects-candidate-003/result.json`。正常门禁另保
`ryupy-ordereddict-validation/gate-001/`，canonical最终项目另保
`ryupy-ordereddict-validation/projects-final-001/`，不覆盖旧记录。固定CP为3.12.13，
CLI SHA256为`1757a3f128983be7a39f8a3efee698f1f8a8cf749d2c0ce1546076bca030f4db`。
这些记录按候选分别归档，不能以最终绿测覆盖或搬移原失败。决策见[ADR0133](adr/0133-ordered-dict-core.md)。
