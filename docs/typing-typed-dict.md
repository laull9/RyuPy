# TypedDict 有限运行时类型元数据

本页记录 M3.7 的 **total-only 子集**，不是完整 `typing` 或静态类型检查器。
完整 `Required` / `NotRequired` 验收 `m3_7_typed_dict.py` 仍为 pending。

## 已实现的定义与 API

- `typing.TypedDict` 是真实函数 factory，不伪装为普通类。
  功能式调用支持 `TypedDict(name, fields, *, total=True)`；前两项为位置参数，
  字段表目前要求真实 dict，`total` 要求 bool。
- 类定义支持单个 TypedDict 父、字面量布尔 `total`、无默认值的简单字段注解、
  `pass`、首条 docstring 和普通类装饰器。
- 子类复制父字段元数据后按源码顺序合并；覆盖字段保留原插入位置。
  子类 `total` 只影响本次新声明或覆盖的字段，不反向改变父字段规则。
- `__annotations__` 是可修改的 dict；`__required_keys__` / `__optional_keys__`
  是创建时的 frozenset 快照，之后修改注解表不会重新计算这些集合。
- 支持 `__name__`、`__qualname__`、`__module__`、`__doc__`、`__total__`、
  `__bases__` 与 `__class__` 元数据；类语句模块名使用编译入口的模块身份。
  不模拟用户在定义前重绑 `__name__` 后的自定义模块元数据。
- 产物是独立 `_TypedDictMeta` 元数据对象，调用返回真实 dict。
  不检查必填键、额外键或值类型；关键字、位置映射等复用现有 dict 构造协议。
- `typing.is_typeddict` 只认可真实产物；`get_origin` 为 None，`get_args` 为 ()。
  `issubclass(T, dict)` 为真，但 TypedDict 作为 `isinstance` / `issubclass`
  的右操作数抛 TypeError，不当作运行时字段校验器。

## 真实基值与求值顺序

HIR 只在 driver 已确认原生 `typing`（未被本地源码遮蔽）时，登记导入名/别名与
已定义 TypedDict 父的**语法候选**。候选并不证明导入已经执行或绑定不可改。
条件导入仍按实际分支执行，`TYPE_CHECKING` 保持可改绑，不折叠死分支。

定义时先求装饰器表达式，再读取真实基值；`TypedDictBegin` 仅接受真实根 factory
或真实 TypedDict 父。随后在私有命名空间求值注解，`TypedDictFinish` 合并元数据，
再逆序应用装饰器，全部成功后才覆盖公开绑定。异常保留旧公开对象。

未执行导入保留 NameError；候选名改绑为不支持的基值在类体前抛
NotImplementedError。这是明确能力守卫，**不声称支持任意元类路径的 CPython
报错时点**。普通本地 `typing.py` 不会因相同属性拼写获得专用身份。

## 明确未实现

- Required / NotRequired、ForwardRef / 字符串注解（包括 future annotations）、
  Generic、多重继承、方法、字段默认值、dataclass 混合定义。
- 非简单字段目标，如 `(x): T`，报 E0100；不错误登记为字段，也不声称已支持其副作用。
- 非字面量 total 类关键字、任意其它类关键字、通过普通赋值创建的未知父别名。
- TypedDict 元数据属性重绑或删除目前明确拒绝；注解 dict 的内容修改独立支持。
- 接受的字段类型值限已有真实类型与 typing 别名子集；不将任意普通对象伪装成类型。
- 工厂的旧式关键字字段声明、完整类型反射与静态类型检查不在此范围。

## 表示与成本

定义时分配私有 PyClass 命名空间壳，冷路径 ConditionalWeakTable 关联构建参数；
完成后产出独立 PyTypedDictType，复制父注解和 required/optional 集合。
表不会以全局强引用保留失败或已完成的壳；不向普通 PyClass 实例/PIC 热路径加判断。
这是一项兼容性实现，不宣称比旧拒绝路径更快。

## 验收

- `m3_7_typed_dict_total.py`：父字段覆盖、total 规则、构造、元数据、装饰失败绑定。
- `m3_7_type_checking.py`：模块属性/from 别名重绑、真实条件定义，不靠分支删除通过。
- HIR `typed_dict_tests`：候选身份、源码遮蔽与明确语法拒绝。
- driver `typed_dict`：HIR/types/MIR 快照、改绑根及未执行导入的真实运行时检查。
- `PyTypedDictTests`、`PyTypeMetadataRoutingTests`：独立元数据和共享类型协议。

以上正例与固定 CPython 3.12.13 比较；完整包装器矩阵仍 pending，不计作通过。

## 配套 Ellipsis 单例

`...` 与未遮蔽的内建 `Ellipsis` 指向同一真实单例，repr/str 为 `Ellipsis`，
类型名为 `ellipsis`。实例不可调用，`type(Ellipsis)()` 无参构造返回同一实例；
布尔值为真，哈希使用稳定实例身份。模块后来给 `Ellipsis` 赋值时，未绑定期间仍回退
内建值；该名字的模块槽保守使用 Dynamic，避免整数/字符串槽无法容纳真实单例。
普通其它整数变量不因这一规则降级。显式用户值（包括 None）优先于内建回退。

`typing.Tuple[T, ...]` 的参数元数据保留 Ellipsis 身份，显示为 `typing.Tuple[T, ...]`；
不因此开放 `Callable[..., T]` 等尚未实现形式。源码 literal 不读取用户同名绑定。
验收为 `m3_7_ellipsis.py`、`m3_7_ellipsis_integer_shadow.py`、对应 runtime 与 ty 专测。

## 功能式调用来源候选（未验收）

ADR0145候选只改变功能式调用来源：真实Main入口快照或Dynamic函数当前__module__，不是调用时globals或basename猜测。raw对象保身份，true原样/false main；真值AttributeError及子类才读同帧活globals，非None结果不再次求真值。二级None元类默认填充、非str直接/typing alias显示仍明确NI，并受旧正确路径不能回退的验收硬门。Static保旧ModuleForFile消费者，不能据signature声称支持可变真实Module。类语句的原模块名合同不在此切扩大。详见[ADR0145](adr/0145-compiled-source-provenance.md)；真实产品门未完成。
