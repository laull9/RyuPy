# 数据类、注解与模块元数据：原生 API 边界

本文按 API 记录数据类、typing 与模块元数据的实现和验证边界，不代表完整库或反射兼容。
性能取舍与测量由本里程碑 ADR 单独记录；不能把此前缺少生成方法的静态表示速度当作语义等价基线。

## dataclasses

### 公开入口

| API | 支持行为 | 明确边界 |
| --- | --- | --- |
| `dataclass` | 类装饰器、直接传类或返回装饰器；生成初始化、repr、同类字段相等和冻结规则 | 当前类级选项只开放 `frozen`；`init/repr/eq/match_args` 非默认值以及 `order/unsafe_hash/kw_only/slots/weakref_slot` 非默认值拒绝 |
| 直接导入的 `@dataclass(...)` | 编译器保留装饰链准确位置，当前允许无参数或 `frozen=True/False` 字面量 | 该编译期标记不接受任意选项表达式、重复标记；存在可能改写导入别名的 `global` 写入时明确诊断，不静默忽略重绑定 |
| `field` | 仅关键字的 `default/default_factory/init/repr/compare` | 同时给默认值和工厂抛 `ValueError`；`hash/metadata/kw_only` 非默认值明确拒绝 |
| `MISSING` | 与 `None` 不同的稳定缺省标记 | 工厂参数使用另外的私有标记；用户显式传 `MISSING` 不会意外调用工厂 |
| `FrozenInstanceError` | 真实 `AttributeError` 子类；可捕获冻结赋值与删除错误 | 编译器不再提前拒绝这类写入，保留运行时 `try/except` 行为 |
| `is_dataclass` | 判断受支持数据类及其实例 | 不提供完整元类 / 继承判定协议 |
| `fields` | 返回按字段顺序排列的字段对象 tuple；接受类与实例 | 非数据类抛 `TypeError`；自定义替换字段映射不受支持 |
| `Field` 对象 | 可读 `name/type/default/default_factory/init/repr/compare` | 不是完整 CPython `Field` 反射接口；未开放属性不伪造占位元数据 |

数据类目前不支持继承、重复运行时应用 `dataclass`、`InitVar/ClassVar` 等特殊字段机制、
完整 `asdict/astuple/replace/make_dataclass`、任意描述符协议或动态元类。未注册入口保持缺失，
不以模块导入成功代表这些 API 已实现。类定义自身仍受当前 HIR 类体语法边界约束。

### 字段、默认值与工厂

- 类体按源码顺序执行。字段右值先求值并写类命名空间，再求值标注并写 `__annotations__`。
  普通默认对象只在类体求值一次，各实例保留共享引用；`None` 是真实默认值，不是缺省。
- `field(default_factory=...)` 在类定义时保存工厂对象，构造参数缺省时才调用；
  每次缺省调用各自运行，显式传参不运行。工厂异常直接传播，不转换成空值或宿主错误。
- `init=False` 字段不参与构造参数绑定；有工厂时每个实例仍调用工厂。
  `repr=False`、`compare=False` 分别从生成 repr 与比较字段集合中排除。
- 必填字段不能排在参与初始化的默认字段之后；错误在装饰应用时抛 `TypeError`。
  list/dict/set/bytearray 及受支持用户实例的不可哈希默认值在装饰时抛 `ValueError`，
  不通过调用用户 `__hash__` 来试探可变性。
- 装饰器作用于下层返回的当前类：从该类最终 `__annotations__` 和类属性读取字段、默认值，
  不借用原始类的字段布局。转换失败不替换此前公开绑定。

### 生成方法与身份

默认数据类是 `PyClass/PyInstance` 引用对象；赋值、传参和别名保留身份，包括 frozen 类。
原 MIR / CIL 静态字段布局与构造代码仍保留给将来的可证明特化，但不再仅凭字段标注选择它，
也不以丢失 repr、eq 或运行时异常来交换速度。

- 已有用户 `__init__/__repr__/__eq__` 保留，不被生成方法覆盖。
- 生成 repr 按字段顺序读取并格式化，递归引用有保护。
- 生成 eq 只比较同一实际类；先分别读取左右全部比较字段，再按 tuple 相等语义比较，
  字段 getter 与用户 `__eq__` 的副作用顺序不能由快路径改变。
- 普通可变数据类默认不可哈希；frozen 类的生成 hash 使用参与比较的字段 tuple，
  字段本身不可哈希时保留错误。显式用户 hash 保留。ADR0066 的有限候选在 field 占位处理后、生成 eq 前分类 own hash：None 与 own eq 同时存在按非显式处理；None 而无 own eq 仍保留。专项与完整门禁状态见该 ADR。
- frozen 阻止实例赋值与删除，包括新属性；生成初始化使用专用初始化入口，
  不把“冻结”误实现成值复制，也不冻结字段引用的容器内容。
- 普通方法替换必须立即生效；构造快路径必须在守卫失败后回到通用语义，不能绕过属性或用户方法。

## 注解是定义时的真实表达式

函数的运行时注解与静态类型线索分开保存。已知基础标注仍可提供优化线索；其它合法表达式
不强行变成存储布局，不因 `Optional` 或类型别名新增 Python 没有的实参类型限制。

- 函数装饰器表达式先求值，随后求默认参数，再按参数 / 返回值顺序求注解，最后逆序应用装饰器。
- 顶层函数、嵌套函数和方法的注解都在定义位置的外层作用域求值；方法可读取此前的类命名空间。
  嵌套注解引用的闭包变量和注解内 lambda 按外层作用域分析，不在调用函数时重新求注解。
- `__annotations__` 保存求值所得对象：后续重绑定类型别名不会追溯改变已定义函数的注解。
  注解可以有普通调用副作用；生成器定义嵌套函数时，注解内的 yield 仍走既有暂停状态机。
- 类有标注时先初始化 `__annotations__`，各字段依次写入当前映射；标注自身不创建实例字段值。
- 不提供 `from __future__ import annotations` 的延迟求值、任意前向引用解析或完整 `get_type_hints`。
  字符串标注作为字符串对象保存，不自动执行。普通非装饰类的既有类体自名早绑定边界仍需独立治理。

## 验证范围

- 原 `m3_6_dataclass_typing.py`：工厂隔离、repr/eq、frozen 可捕获写入与 Optional 元数据。
- `m3_6_dataclass_defaults.py`：定义 / 构造时机、共享默认、缺参工厂、错误默认值。
- `m3_6_dataclass_edges.py`：字段开关、post-init、显式 MISSING、工厂异常、冻结删除、
  替代类与失败绑定、字段元数据、eq 副作用和生成方法重绑定。
- `m3_6_annotations.py`：默认值 / 注解顺序、别名重绑定、外层闭包、类命名空间和跨暂停注解。
- C# `PyDataclassTests`、既有 `PyRuntimeDataclassTests`，及 M3.6 HIR / 类型 / MIR / CIL 快照。
- Phase 1 数据类与 Phase 3 装饰器的既有用例继续参与聚合门禁，不以新用例替换旧回归。

## typing 与类型别名

| API | 当前支持 | 明确边界 |
| --- | --- | --- |
| `Any`、`TYPE_CHECKING` | 稳定元数据对象；运行时 `TYPE_CHECKING=False` | 不执行静态检查器 |
| `List/Dict/Tuple/Set/FrozenSet` | 类型参数、`get_origin/get_args`、结构相等与哈希；Tuple原始缓存及有限尾Ellipsis见ADR0115 | 不允许通过这些 typing 别名构造容器；完整动态参数协议仍pending |
| `Optional/Union` | None 归一化、嵌套展平、重复项合并、Union 相等不依赖参数顺序 | 仅元数据；参数化别名用于 `isinstance/issubclass` 明确拒绝，包括尚未实现的 Union 特例 |
| `Callable[参数表达式, 返回类型]` | exact list/tuple及有限scalar参数转换、独立原始缓存与真实alias父关系见ADR0116；`get_args` 返回新参数列表与返回类型 | `Callable[..., T]`、ParamSpec、动态helper与完整派生协议未支持 |
| `Iterable/Iterator/Sequence/Mapping/MutableMapping/Collection/Container` | 参数元数据，origin 使用实际 `collections.abc` 对象 | 不借此增加泛型继承与 ABC 注册能力 |
| `get_origin/get_args/cast` | 未参数化形式无 args；cast 原样返回对象保留身份 | 不解析字符串前向引用，不执行类型转换 |
| `list[T]/dict[K,V]` 等原生容器别名 | 元数据与调用底层构造器 | 不验证所装元素类型；完整 GenericAlias 反射接口未实现 |

别名参数以数组快照保存，公开 args 以不可变 tuple 返回；Callable 的参数列表每次重建。
最多缓存 128 个别名，以先进先出淘汰限制强引用数量；缓存命中保留同一对象身份，
不承诺淘汰后的再次订阅身份不变。Union 的结构相等和哈希不依赖缓存命中。

`TypeVar/Protocol`、公开完整 `ForwardRef`、前向引用求值、完整泛型推导与反射仍未实现；
`NamedTuple` 的真实有限范围见下节，不用普通 object 或 dataclass 假装 tuple 类型。
参数注解不是运行时类型合同，错误类型的实参不会仅因为注解而新增拒绝。

## 本里程碑补齐的一等内建值

`pow` 可作为 partial 的目标，按 base/exp/mod 签名统一绑定位置及关键字参数。
二参复用动态幂协议；整数三参使用模幂，负指数用扩展欧几里得求逆，非互素或模零抛 ValueError。
浮点零负幂与有限溢出分别抛 ZeroDivisionError / OverflowError；需要 complex 的有限负底分数指数
明确 NotImplementedError，不返回错误 NaN。三参用户自定义数值协议尚未实现，明确拒绝。
容器类型作为一等构造器支持已有 list/tuple/dict/set 等入口，不因此开放任意内建类型继承。

## 窄死值装箱消除

M3.6 增加 MIR `DeadValues` pass，在生成器降级与控制流简化之后执行。
只删除整函数没有任何读取的局部 `Use/Box` 赋值，迭代到固定点；保留局部声明。
引用统计覆盖全部操作数、递归数组、闭包环境、默认参数、生成器帧、异常区局部与终结器。
调用、未赋值检查、拆箱、算术、属性访问和任何存储副作用都不能因此被删除。

这不是依据标注的整型推断，也不会把动态累加改成会溢出的固定宽度运算。
例如未使用的循环变量盒可被消除，但循环后读取、闭包捕获或帧保存仍保留。
`m3_6_dead_values.py` 与 MIR 结构 / 快照回归覆盖这些边界；新发现的动态 `if`
确定赋值缺口同时修正为分支独立状态及汇合交集，保留 `unused = unbound` 的异常。

## 受守卫的循环累加器

`NumericAccumulator` 在死值消除后仅匹配一个严格的三块自然循环：函数入口是无前驱的
唯一 preheader，循环体、回边、出口均无旁路；局部已有直接初始化，且循环内只在唯一
`total += rhs` 中读取和写回。闭包 cell、生成器、异常区、循环内其它观察 / 别名、
引用 total 的 rhs、被其它位置使用的中间结果，以及不能证明入口初始化的情形均保持原动态路径。

通过守卫后，一次函数调用创建一个内部 holder；不改变 Python 可见类或数值类型。
实际 long + long 且没有溢出时延迟装箱，溢出、bool / float / 用户对象等仍执行完整动态
原地加法协议。先求 rhs 再更新状态；动态回退所得对象原样保存。唯一循环出口物化结果，
零轮循环保留原 initial 身份，同一结果的多次观察复用相同对象。

这不是依据注解猜测固定整数宽度，也不支持任意循环不变量或任意数值协议特化。
每次匹配调用增加一个 holder 分配，短循环不保证更快；是否保留此优化由相同负载的热跑
时间 / 分配测量与完整门禁共同决定，不以独立 helper 微测替代端到端证据。
`m3_6_numeric_accumulator.py`、MIR 正反结构测试和 driver 快照覆盖动态类型转换、两向溢出、
用户 radd / iadd、属性副作用、零轮身份、循环内观察拒绝与异常区回退。

## typing IO 三类（有限已验范围）

| API/观察 | 本轮支持 | 边界 |
| --- | --- | --- |
| IO、BinaryIO、TextIO | 三个稳定真实类，名义类型关系、零参构造、普通无slots单继承后代 | 不是真实文件流，不补Generic/AnyStr框架 |
| 根方法与property | 28函数、8property；完整绑定、合法调用返回None、只读getter、普通覆盖/super | 不以“abstract”拒绝构造，不按参数注解做运行时转换 |
| 裸根布局 | 无实例字典/槽；property setter优先，普通后代字段可写 | 后代own slots、dataclass及未实现类hook明确边界 |
| 名称/模块 | typing根来源；普通模块级后代定义时读取真实当前__name__，公开重绑保身份 | 不猜未知Static来源；函数内类仍原E0100 |
| 有限元数据 | BinaryIO/TextIO及支持普通后代的真实直接bases、根函数绑定身份、未参数化get_origin/get_args | IO[...]、完整MRO/参数传播/函数反射等CP合法正例pending；BinaryIO/TextIO订阅按CP报TypeError |

29运行时专项和四项CP/JIT应用分别通过；首次正常完整门禁已通过，尚未发布。应用首次3/4的delattr编译旧缺口和公共专项首次28/29的宿主初始化遗漏均保留，未修改生产编译器绕过。范围、性能成本及证据见 [ADR0087](adr/0087-typing-io-runtime.md)。


## typing.NamedTuple（有限候选）

| 接口 | 已实现域 | 明确边界 |
|---|---|---|
| 功能式根 | 真实PyFunction、位置参数/字段kwargs、两遍fields、真实caller来源 | exact名字及有限pair/注解；替换collections工厂NI，不忽略替换 |
| 类语法 | 单父真实根、最终字段/尾默认、注解与new共享、成功后发布 | Generic、生成类派生、魔术覆盖及late类修改NI |
| tuple行为 | 复用真实tuple身份/索引/迭代/比较/hash及现helpers | 不扩全部tuple子类反射 |
| 普通成员 | 普通方法/static/classmethod、property读写删、首docstring | 字段只读、无实例dict；未知可变descriptor常量NI |
| 注解字符串 | 内部真实未求值ForwardRef，身份/元数据/eq/hash与有限ASCII识别 | 不公开完整构造/求值；复杂合法表达式、Unicode等NI，200层界限不冒CP同类异常 |

160项相关runtime（含既有namedtuple50/IO29）、5项完整原流CP/JIT应用＋1结构及HIR6项首次通过；旧Quitter期待窄迁后第二次正常完整门禁1607C#/228CP/202e2e＋30原ignored通过。
所有MakeClass路径的定义期module/qualname冷初始化为共有语义修正，不改Static数值ABI；IO晚修改仍拒绝。
来源未知、删除/自定义builtins、class module=None、复杂合法域及旧反射差异保[ADR0088](adr/0088-typing-namedtuple.md)与pending，未发布、不冒tomli或完整typing通过。

## typing.overload 三联 API（有限原生）

| API | 已验证的有限行为 | 明确边界 |
| --- | --- | --- |
| overload(func) | 真实函数、一次 __func__ 解包、当前 module/qualname/真实首行注册，保原参数强引用及统一 dummy | 公开 code、自定义属性/映射/hash/eq/helper 未支持，不猜来源 |
| get_overloads(func) | 当前 registry 的命中列表快照、miss 新空 list、bound method 及当前属性 | 未知合法动态域明确 NI；快照及元素身份不被 clear 改写 |
| clear_overloads() | 清当前公开 registry 原对象，不 hash 旧键、不消费工厂 | 不代表任意 mapping.clear 协议已支持 |

真实 registry 为两层 defaultdict 和最内 dict，同首行覆盖不移位；持续强引用函数，
无容量上限/自动逐出，删除/覆盖/clear 后按普通引用规则释放。弱键版本验证缓存不
延长旧桶寿命。六份完整公开源、23新C#及正常完整门禁通过；十份新增和两份复用旧
边界保持完整 pending。改写 API __qualname__ 后错误前缀、partial repr 已证差异
不算兼容。范围、完整失败和420性能样本见 [ADR0120](adr/0120-typing-overload.md)。


## importlib.machinery.ModuleSpec 与普通源模块元数据（有限已验范围）

| API/观察 | 已接入的有限行为 | 明确边界 |
| --- | --- | --- |
| ModuleSpec(name, loader, *, origin, loader_state, is_package) | 稳定真实 PyClass/PyInstance；先绑定再消费真值，字段保原引用，包列表独立 | 不是字典空桩；不因此开放 util 创建/执行、动态发现或任意 loader |
| parent / has_location / cached | 包按 locations 是否 None 判断；真值成功后赋值；惰性缓存保身份和描述符重复读取 | 缓存路径限固定 CP3.12、optimize=0、无 pycache_prefix；自定义 origin 协议在消费点明确 NI，跨平台未验 |
| repr / eq / hash | 逐属性消费；locations/动态类名走空 format；有限字段比较和 AttributeError 边界；不可哈希 | 直接字段自定义 eq 非 bool 结果与未知原生比较对象未接，sealed object() 仅保身份比较 |
| 普通已定位模块元数据 | resolver 显式包标记；四新动态槽入口初始化，裸名字/外部属性同槽；spec/loader/file/cached/path 初始引用关系真实 | 普通函数与全局仍沿既有静态 ABI；新 RegisterSourceLocated 显式声明新槽能力，旧十参入口保原合同，不从位置登记猜新槽 |
| CompiledModuleLoader / resources.files | 真来源描述类型；资源入口核本次 spec/loader 身份及 spec 来源字段 | 不伪装 SourceFileLoader，无 create_module/exec_module/get_source；不宣称验证 loader 任意字段改写 |

成功重复 import 复用模块/spec，不重跑元数据前言；失败移除缓存，重试新建 spec。
旧失败对象仍连共享静态槽，独立实例全局与旧别名隔离尚未接。模块/全局删除及
builtins 回退、SourceFileLoader/native/__main__ 混合元数据、可写 sys.modules 与
完整 html.parser 继续保边界，不能把 spec 存在当作完整 importlib 支持。

B3 阶段 52 项定向 C# 及五份完整源同 PE 配 B3 runtime 的实际 JIT/CP golden 比对
通过，两份完整边界分别保留 SourceFileLoader 缺失与全局 del 的 E0100，后者未运行。
B4 新登记入口20项 C#/8项codegen定向通过，原样旧PE配B4 runtime复验通过且保原错；
B4 正常完整门禁2281 C#/790 Rust/433基线/433 cases selftest/310编译e2e通过，127 ignored保留；
B4真实CIL审计发现普通global意外Object化，P3未放行；B5已作类型层窄修，
B5最终门禁2281C#/791Rust/433基线/433cases/310e2e、127ignored已过，
B5真实PE/CIL复核已关闭普通global退化，唯一性能矩阵已完成并保全部不利，详见[性能报告](module-spec-performance.md)；
干净889ba9a7原docopt入口通过，Markdown仍在JIT缺importlib.util，不计Markdown通过。
详细合同、阶段身份、失败保留及验收窗口见 [ADR0121](adr/0121-module-spec.md) 和
[ModuleSpec](module-spec.md)，历史性能风险不因此关闭。
