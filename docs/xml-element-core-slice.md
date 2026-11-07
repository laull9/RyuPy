# XML Element 有限原生对象首切

日期：2026-11-06。参考固定CPython3.12.13默认加速Element；功能接受，完整XML与性能开放。

## 决定与范围

以固定 CPython 3.12.13 **默认 C 加速 Element** 为参考，新增有边界的 native 对象实现；不编译整个 ElementTree.py，不以字符串或单父 .NET XML DOM 冒充 Python 对象图。核心只服务通用 API，没有 Markdown 名字、输入或输出特判。

当前导出真实 `xml`、`xml.etree`、`xml.etree.ElementTree` 模块及：

- Element、SubElement；tag/text/tail/attrib；append、insert、get、set、keys、items、iter；len、整数索引、读切片与普通 children 迭代。
- ElementTree 的有限构造与 getroot。
- Comment、ProcessingInstruction、PI、有限 QName，以及共享可变 HTML_EMPTY 集合。

三种原生类型保持稳定身份，type/isinstance 使用既有真实类型协议；不因此开放完整类反射或用户派生。实现落在四个 XML 运行时文件，只给 PyNativeModules、PyIter、PyWarnings 添加必要接线；未新增 Rust 层语义、RuntimeFn 或 codegen ABI，也未扩张通用 binder/hash/type 框架。

## 对象、容器与输入合同

- Element 的 tag/text/tail 保存原对象引用，包括字符串子类；不在赋值时字符串化，也不缓存序列化结果。Element 以身份相等/哈希，可作字典键。
- 构造 attrib 浅复制：外层新字典，值仍共享；keyword 覆盖保顺序。直接给 attrib 赋受支持字典时保存该字典身份，外部别名和节点互见修改。当前仅接受精确 dict 与精确 str 键，字典派生、用户键等域在产生额外 hash/eq 副作用前明确 NI；外部字典后来出现未知键时，相关 API 消费也守门。
- keys/items 返回默认 C Element 的 **list 快照**，不是 live dict view。items 的 tuple 与返回 list 是新容器，条目中的原值引用保留。
- children 没有唯一 parent 指针。重复 child、两父共享同一 child 均保原引用；append/insert 不搬移旧父、不克隆。插入位置按有限整数域处理，负数相对当前长度并夹到端点；读切片返回新 list、元素仍为原节点。
- 整数与 slice 字段使用内建整数域，用户 __index__ 明确 NI。保越界、超大整数、slice step=0 等已测错误相位。写下标、写/删切片不在本切。
- Element/SubElement 按默认 C 入口的实参数与 attrib/额外属性规则绑定；不能按文档外观当普通 Python 关键字签名。ElementTree/QName 的 Python initializer 和 QName 的 Python eq 方法保 self 计数及关键字规则；继承 object 的 ne wrapper 使用独立位置参数绑定。
- ElementTree 保存传入 root 原引用，**不以 root 真值筛选**。file 参数只在真值分支进入未支持解析；getroot 返回原 root。并未实现 _setroot、tree.iter 或文件读写。

## 迭代、对象图与可见协议

普通 children 迭代器按当前索引活读：耗尽前追加可见，第一次耗尽后保持结束；迭代项是原节点。Element 本身**没有公开 __iter__**，通过 PyIter 的 XML 专用序列入口进入；不能因支持 for 就虚报 Iterable/Iterator/Sequence/Container ABC。独审已比较这些形状与 Sized。

Element.iter 支持 None、精确字符串及 `'*'`。前序遍历使用显式栈和当前活动路径集合，不预复制全树、不对共享 DAG 全局去重，同一节点从不同父路径可重复出现。环允许构造，但遍历实际遇到环时 NI；深度超过 10000 时 NI，避免 CLR 栈溢出。未知用户 tag 比较或非承诺 filter 明确拒绝，不假装支持任意过滤协议。

## 工厂与 QName 的有限身份合同

Comment/ProcessingInstruction 捕获创建时的真实 attributes 字典；保存旧函数后仍读取这份字典中的当前工厂绑定作为 tag，不按模块名重新 import 别的 globals。PI 初始与 ProcessingInstruction 同一函数，后者重绑不改变旧 PI 别名。若 Element 被替换成自定义工厂，在消费前 NI，不偷偷使用旧 canonical 类型；SubElement 的默认 C 路径仍用 canonical Element，不追随该公开名字重绑。

ProcessingInstruction 先构造节点并写 target，再消费 text 的真值；需要非精确字符串拼接的分支 NI。原完整自定义工厂回调例仍 pending，不能由普通工厂通过外推任意回调兼容。sys.modules 替换/删除与 ModuleType 等上切边界亦未扩展。

QName 是真类型与对象，只承诺精确字符串构造、text 读取/精确字符串修改、str、有限 eq/ne 与随当前 text 计算的 hash。未知用户比较（包括用户异常对象）、非字符串 text 赋值在对应消费/写入前 NI，不能用引用相同的捷径跳过用户比较。text 改写会改变 hash，不冻结或偷偷缓存旧哈希。

QName 模块绑定被替换会改变 CP Python 方法中的 isinstance 语义；本实现不声称该动态域兼容，而在 BeforeSet **写入前 NI**，保留旧绑定与实例 type。相同类型自赋允许。排序比较、动态属性、派生、__dict__/pickle 等反射仍未承诺。

## repr 与 bool 警告边界

Element、ElementTree 和两类迭代器的 repr/str/format，以及 QName repr 明确 NI，不返回看似成功的通用占位字符串。QName 受支持文本域的 str 正常。专项对 representation 拒绝作了核验，但 NI 是边界验收，不是 CP 成功等价。

bool(Element) 按 children 数量求真并接现有 warnings 发出 DeprecationWarning，已覆盖正常捕获与准确行号。然而原完整病例的 `filename == __file__` 仍失败：现有 warning/frame 路径产生 basename，而 CP 返回完整路径。独审用不含 XML 的旧 runtime 普通 warnings.warn 复现同一差异，因此是既有文件名债，不是新增 Element 帧错层。原完整警告源及断言保留 pending，不改 CP 基线，不据局部 warning 捕获宣称整个 warnings 合同已对齐。

## 未做与成本

TreeBuilder 未提供空构造壳；start/data/end/close 留待独立状态机切。tostring、XML/HTML 序列化、XMLParser/fromstring/parse、ElementPath/XPath、namespace、canonicalize、IO 编码均未完成。clear/remove/extend、写/删 children、itertext、copy/deepcopy、用户派生、复杂索引/映射/比较、完整反射仍明确 NI 或未导出并保 pending。

原 Markdown 核心使用自己的 serializer；本切只提供真实对象，不替换其转义算法。本次原入口尚未走到转换完成，不能把 XML 输出/转义或整个 Markdown 算作已验收。

成本包括模块和类型登记、每节点/属性字典、需要时分配的 children、绑定方法包装、keys/items 快照、children 游标及前序栈/活动路径集合。没有无界全局节点表或序列化缓存；前序辅助空间随活动深度增长，节点图随用户实际对象增长。**本切没有正式性能或 AOT 成本测量**，不宣称提速、零热路径成本或无回退；历史性能风险全部保留。

## 正常验收与原项目

正常make check通过：2538项C#、900项非e2e Rust、5项清单自检、600份CP基线与600份框架自检、379项编译端到端通过、225 ignored。门禁运行199.281秒，前后全部源hash/mode相同。没有改变正常命令、项目或测试框架以绕过失败。

最终canonical Release runtime SHA256：3963737a656bfed430f1a10736d520b077fe6081db40bdf828b8f0f91457eab1。冻结CLI仍为1757a3f128983be7a39f8a3efee698f1f8a8cf749d2c0ce1546076bca030f4db；Rust源与ABI未变。隔离候选004 SHA cf9c13dea9b518f64f282b14281333212dd1e4863ce600c433a223a8ce6c3700的全部源已逐字核同canonical，路径变化可能导致二进制hash不同，不混称同DLL。

新增22份完整CP合同，最新固定参考全部exit0、stderr空。9例逐字节真实PE通过转正：attribute_aliases, children_identity_indexing, core_errors_atomicity, factory_current_bindings, live_preorder_iteration, module_tree_identity, qname_callable_binding, sentinels_qname_html_empty, source_package_priority。13例继续pending：children_write_delete, clear_remove_aliases, element_index_mapping_callbacks, extend_input_side_effects, factory_module_cache_replacement, factory_rebinding_callbacks, qname_dynamic_values, qname_exception_comparison, qname_module_rebinding, representation_boundaries, serialization_separate_consumer, treebuilder_event_state, truthiness_warning_protocol。只移除通过例的pending元数据/首注释，执行AST、完整断言与CP golden不变。

完整representation边界例在hex内建尚未实现处E0100，尚未执行到repr；repr NI另由C#及独立探针确认，不把完整例冒称运行通过。完整警告例在filename断言失败，不删除它。模块替换例仍先受types.ModuleType/完整可写cache边界阻塞。

最终Release下原docopt16字节与原golden一致，stderr空；默认Markdown仍375字节旧find_spec source metadata NI。原四源窄配置从缺xml推进到缺collections.OrderedDict（65字节stderr），仍exit1、stdout空。所有入口/项目源前后hash相同，复用历史CP参考，不是新20项目矩阵、完整Markdown或XML serializer验收。AOT/跨平台及新性能采样未运行。

## 首失败、审查与成本补记

CP环境首捕获误假定内建_elementtree有__file__而失败，后按实际built-in origin固定解释器/ElementTree源身份。主case历史三次失败分别是默认C TreeBuilder attrib共享、空close返回值和Element tag关键字假设；原源/流均保留，修正的是CP事实误解，不是放宽实现期待。

候选期间独审抓到QName用户异常比较被False吞回调、eq(other=)误作posonly、模块QName重绑被静默忽略；最后改为正确Python绑定及域外消费/写入前NI，不扩通用比较。公开Element无__iter__，最终从PyIter有限序列入口接入，保ABC形状；原失败与旧候选均保留。QName/Tree构造过量位置参数的可选范围只在XML局部修正，完整绑定例逐字通过。

父首次004复验误传相对runtime路径，12例编译均找不到DLL，没有执行；修为绝对路径另建attempt后六核心通过，最后完整22例验证九正十三pending。首封包patch混合空文件git header与unified diff失败，原patch/流保留；只修封包格式后二次check/apply及全部new SHA核同，未修改源。隔离C#历次日志与冻结源均保留，最终2538过不能抹去先前发现。

get/set等为守住外部attrib别名新增非文本键而扫描keys，属性多时逐项set可能二次增长；节点/字典、方法包装和参数数组、快照、前序O(depth)栈/集合及启动注册均有真实成本。非XML普通容器、通用hash/type/binder源保持原实现，PyIter只在已进入native分支增加载体判断。未以此宣称零开销，S1/B2及所有旧不利样本继续开放。
