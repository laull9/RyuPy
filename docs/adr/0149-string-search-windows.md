# ADR-0149：五个字符串搜索方法共享码点窗口

- 状态：源码候选，未编译，未作产品验收
- 日期：2026-11-10

## 背景

Markdown 3.5.2 两显式入口已越过 expandtabs，在锁定 CPython 3.12 的
`_markupbase.ParserBase.updatepos` 遇到 `rawdata.count("\n", i, j)` 的旧单参限制。
该函数随后调用 `rindex`，只放宽 count 参数数会留下 UTF16 窗口与负下标错误。
默认 Markdown 的 find_spec 边界另属一条链，本决定不改变默认项目计数。

固定245例 CP oracle 已在外部单批采集，不重复执行；它是语义证据，不是产品通过。
该批证明 start 转换先于 needle 检查，但没有单测坏 needle 与自定义 end 的组合。
新 slots fixture 明确保留此补充合同；四个新fixture已各一次CP通过，仍不代表产品通过。

## 决定

一个内部 `PyTextSearch` 负责 count/find/rfind/index/rindex 的绑定、索引转换与搜索。
精确 string 的 CallMethod 与 PyBuiltinMethod.Invoke 在通用 keyword/arity 拒绝前进入它；
PreparedText、StringMethod 和 StringRootMethod 同样委托，删除旧 CountText/FindText。
PyInstance/PyStringInstance 仍先走实际属性，保类覆盖与实例遮蔽；根描述符显式调用保根行为。
list/tuple 的 count/index 签名与实现不变。不新增 RuntimeFn、CIL 引用、PIC 槽或全局 ABI。

先拒 keyword、再检查1至3个位置参数。在任何用户回调前冻结 needle/start/end 对象引用，
依次转换 start、end，之后检查 raw needle。不能提前取得 end 的类型槽，因为 start 可修改它。
None 使用省略值，直接 bool 是整数；BigInteger 饱和到有符号64位窗口界限而非 C-int 溢出。
调用 __index__ 返回 bool 沿真实 DeprecationWarning 路径，用户异常不捕获、不重包。

只从 expandtabs 抽出类型级冷槽取得与调用为 `PyIndexSlot`：普通/Python异常载体、native既有
特殊槽、raw __get__ 三参、actualClass、super上下文、缺槽与不可调用的区分均保留。
各 API 自己拥有 None、结果校验、警告、错误文案、溢出/饱和。expandtabs 仍是 C-int 合同。
这不是扩大普通对象协议或 int 派生布局，也不能仅因静态提取相同就宣布原能力已验收。

`PyTextWindow` 只提供纯 Offset、边界检查与计数。affix 只将原 Offset 委托给它；
affix 的 Index、tuple短路、旧 bool 返回明确拒绝边界完全不改。

## 算法与边界

窗口由码点映射到 UTF16，负下标从尾定位；正 start 越尾保无效，end 独立夹尾。
反向窗口不被改成有效空窗口。空 needle：有效窗口 count=窗口码点数+1，正向位置=start，
反向位置=end；无效窗口 count=0、find/rfind=-1、index/rindex 抛 substring not found。
非空搜索在有界 span 上 ordinal IndexOf/LastIndexOf，命中左右端点都不得切开有效代理对。
拒绝半pair候选后继续单调移动范围；count 命中后推进 needle.Length，保证非重叠。
返回位置只对最终命中计一次码点；不做 Substring、全串码点数组或候选数组。
不宣称底层搜索对所有 needle 都是线性复杂度，也不声称性能提升。

D009 原存储债保留：显式相邻高/低 surrogate 与真非 BMP 字符在 CLR string 中不可区分。
孤立 surrogate 与真 astral 的码点窗口仍有独立测试；没有把此实现宣传为全 Unicode 等价。
warning 真实源码来源/行号、完整 stderr、showwarning 替换的旧失败必须保留，不能放宽断言。

## 验收与非目标

新增 C#：PyTextSearchTests、PyTextSearchProtocolTests、PyTextSearchWarningTests。
其中真实 PyPic.CallMethod3 同站点重入检查外层 needle/end 保持和 end 槽晚查询。
新增 pending Python：tests/fixtures/text_search 下 core、slots、warning_contract、markupbase_updatepos。
最后一个仅导入锁定 stdlib，用 ParserBase 普通子类实际执行 reset/updatepos/getpos；
不能直接构造 ParserBase，也不能把该闭环视为 Markdown 通过。

本机无 dotnet/新CLI，当前只能进行静态检查。后续须运行新 C#、原 expandtabs 全18项、
原 expandtabs core/slots/完整 warning、affix/partition/NoReturn/容器保护，以及同源 JIT/AOT。
原 Markdown plain/full 完整入口与 golden 不改；即使推进到后续首错，也只报告该边界，
不自动继续修下一错。完整 check、默认固定20项与显式项目验收各自独立。
