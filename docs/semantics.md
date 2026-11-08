# 语言子集与语义规范

> 本文定义 RyuPy **当前支持**的 Python 子集，以及它与 Python 语义的对应方式。
> 常用行为以固定版本 CPython 为准，按项目与 API 验收。已接受偏差明确登记，不支持能力明确报错；运行时才能确定的非法操作抛 Python 语义异常。未记录的差异按缺陷处理。
>
> 80% 目标、项目清单与偏差见 [compatibility.md](compatibility.md)，标准库移植见 [stdlib.md](stdlib.md)。计划状态不能当成已实现。

状态图例：✅ 已实现 · 🚧 进行中 · 📋 计划中（括号内为里程碑） · ❌ Phase 1 不支持（编译期报错）

## 1. Phase 1 语法支持表

| 构造 | 状态 | 降级方式 |
| --- | --- | --- |
| 整数 / 浮点 / 布尔 / 字符串 / `None` 字面量 | ✅ M1.0 / M1.4 | `Literal` |
| `print(a, b, ...)` | ✅ M1.0 | 按类型逐个 `Write*` + `WriteSep` + `WriteNewline` |
| 带完整类型标注的 `def` | ✅ M1.1 | CIL 静态方法 |
| 不写标注的 `def` | ✅ M2.1 | 整函数降级为动态路径（参数 / 返回 / 局部都是 `object`） |
| 标注与实参不符的调用 | ✅ M2.1 | 被调函数降级，运行期按 Python 语义；不再报 `E0203` |
| 异构赋值（`x = 1; x = "a"`） | ✅ M2.1 | 整函数降级为动态路径 |
| 多返回类型（同一函数返回 int / str） | ✅ M2.1 | 返回类型降级为动态 |
| 局部变量（不带标注 / `x: int = …`） | ✅ M1.1 | CIL local，类型由推导决定 |
| `x += 1` 等增量赋值 | ✅ M1.1 | 去糖为 `x = x + 1` |
| 函数调用（含前向引用、递归） | ✅ M1.1 | `call` |
| 位置 / 关键字实参、`*args`、`**kwargs`、`/` 后仅位置、`*` 后仅关键字 | ✅ M2.1 | 静态直调按参数顺序生成；无法静态绑定时走 `PyOps.CallValue` + `PyBinder`（运行时抛 `TypeError`） |
| 参数默认值（定义时求值、可变默认值共享） | ✅ M2.1 | 在定义位置的外层作用域求值；顶层使用模块数组，嵌套 def / lambda 使用各自函数实例的默认值数组 |
| 一等函数（函数对象、作为实参 / 返回值、经变量调用） | ✅ M2.1 | 每个函数生成统一 thunk；顶层使用模块函数对象，嵌套函数在定义位置创建对象；`CallValue` 分派 |
| 算术 / 位运算 / 比较 / `not` | ✅ M1.2 | 见第 3 节 |
| `and` / `or`（短路） | ✅ M1.2 | 基本块分支 |
| `if` / `elif` / `else`、条件表达式 | ✅ M1.2 | 基本块分支 |
| `while`、`break`、`continue` | ✅ M1.2 | 基本块 + 循环目标栈 |
| `while … else` / `for … else` | ✅ M2.4 | 条件变假 / 迭代耗尽进入 else 块；`break` 直接跳到出口跳过 else |
| `for x in range(...)` | ✅ M1.3 | 原生计数循环，不产生迭代器 |
| 通用 `for`（容器 / `iter` / `enumerate` / `zip` / `reversed` / `range` 值）、元组循环目标 | ✅ M2.4 | `IPyIterator` 迭代协议；元组目标先写隐藏局部再解包 |
| `int(x)` / `float(x)` / `abs(x)` | ✅ M1.4 | `Cast` / 运行时函数 |
| `str + str` | ✅ M1.4 | `Builtins.StrConcat` |
| `from dataclasses import dataclass` + `@dataclass` | ✅ M1.5 / M3.6 | 当前默认 PyClass/PyInstance，生成字段方法与守卫构造 |
| `@dataclass(frozen=True)` | ✅ M1.5 / M3.6 | 引用身份不变，外部写入/删除抛 FrozenInstanceError |
| 链式比较 `a < b < c` | ✅ M1.2 | 中间项无副作用时展开，否则 ❌ |
| `import x` / `import x as y` / `from x import y` / 别名 | ✅ M2.7 | 编译期构建模块图，运行时走 `PyModules` 导入缓存 |
| 普通包 `__init__.py` 与包内相对导入 | ✅ M2.7 | 相对导入在 HIR 换算成绝对模块名 |
| 模块级变量、函数读写模块变量、`global` | ✅ M2.7 | 默认模块仍是静态槽位；显式B2实例模式的有限函数/类/闭包/生成器共享真实字典，范围见[合同](module-owners.md)，未选模式仍用静态槽 |
| `__name__` / `__package__` / `__file__`、`__main__` 入口 | ✅ M2.7 | 模块入口开头写入；入口模块名为 `__main__` |
| `from x import *` | ❌ | 明确报 `E0100`，要求显式列出名字 |
| 函数体内 `import` | ❌ | 明确报 `E0100`，当前只支持模块顶层导入 |
| `nonlocal` | ✅ M3.0 | 名字解析到最近外层函数的共享 cell；非法声明报 `E0103` |
| 列表 / 字典 / 元组 / 集合 / frozenset / bytes / bytearray / slice | ✅ M2.3 | 容器表达式统一降级为 `Dynamic`，由运行时 `PyList` / `PyDict` / `PySet` / `PyFrozenSet` / `PyBytes` / `PyByteArray` / `PySlice` 承载 |
| `x[i]` / `x[i:j:k]` / `del x[i]` / `x[i] = v` | ✅ M2.3 | `RuntimeFn::GetItem` / `SetItem` / `DelItem`；切片对象 `PySlice`，负值索引与越界按 Python 规则处理 |
| `a in b` / `a not in b`、`a is b` / `a is not b` | ✅ M2.3 | `RuntimeFn::Contains` / `IsSame` |
| 平坦序列解包赋值（含 `a, *b, c = x`）、`for x in 容器` | ✅ M2.3；一般非星号 iterator 有限修复通过 | 受控直接 tuple/list 字面量保 `UnpackCheck` 长度校验；一般非星号源新增 `UnpackSequence` 有界迭代准备。一般星号有限修复使用 UnpackEx 双iter/hint准备（ADR0068，专项及完整门禁通过），直接无展开字面量保旧结构；嵌套解包目标报 E0100。for 的迭代源沿用 `IterStart` / `IterNextOk` / `IterValue` |
| 模块顶层 `del name`、函数显式 `global` 删除 | ✅ 有限 | 默认 typed 绑定位/动态哨兵、显式实例真实字典删键；删除不查 builtin，读取才回退。默认元数据/自定义 builtins 等边界见[合同](module-deletion.md)；局部/cell/class namespace 不由此行扩域 |
| 嵌套 `def`、闭包、lambda | ✅ M3.0 | 定义位置创建 `PyFunction`；按 free 列表捕获共享 cell，保持 late binding 与函数逃逸 |
| list / dict / set 推导式（多层 for / if、解包、闭包） | ✅ M3.1 | 独立隐式函数作用域；最左 iterable 在外层取得迭代器 |
| 普通生成器 | 部分 | 语句 / 表达式 yield、send、if/for/while、return value、共享 cell、next/迭代/耗尽；完整协议待后续 |
| 生成器表达式 | ✅ | 最左 iterable 立即取迭代器，元素 / 过滤 / 内层 iterable 惰性；协议限制同普通生成器 |
| 函数/类装饰器 | ✅ Phase 3 M3.3 | M3.3 定义位置求值/逆序应用、公开替换绑定；见 ADR-0027 |
| `async` / `await`、完整 asyncio、模式匹配 | ❌ | 暂缓，按项目阻塞评估 |
| `raise exc` / `raise exc from cause` / 裸 `raise` | ✅ M2.2 | `Terminator::Raise` + `PyErrors.PrepareRaise` / `WithCause`；裸 `raise` 取运行时「当前异常」 |
| `assert cond[, msg]` | ✅ M2.2 | 去糖为 `if not cond: raise AssertionError(msg)` |
| `try` / `except` / `except (A, B)` / `else` / `finally` / `except … as e` | ✅ M2.2 | CIL 类型化 catch + `finally` 状态机；详见 §4.1 |
| `str(x)` / `type(x)` / 动态属性读取 | ✅ M2.2 | `PyRepr.StrValue` / `PyObject.TypeOf` / `PyObject.GetAttr`（当前覆盖异常与类型对象） |
| f-string（字面量、`!r` / `!s` / `!a`、格式说明符） | ✅ M2.4 | `PyOps.FormatValue`；嵌套格式说明符暂不支持 |
| 字符串方法 `split` / `join` / `strip` / `replace` / `find` 等 | ✅ M2.4 | `PyOps.CallMethod`；返回值下标按码点计算 |
| 内建 `len` / `range` / `enumerate` / `zip` / `reversed` / `sum` / `min` / `max` / `any` / `all` / `round` / `sorted` / `isinstance` / `id` / `getattr` / `hasattr` | ✅ M2.4 | `PyOps.CallBuiltin`；`sorted` / `list.sort` 支持 `key` 与 `reverse`，排序稳定 |
| 内建 `ord` / `chr` 直接调用 | ✅ Phase 3 项目驱动 | Unicode码点/单字节、chr整数与`__index__`，范围和参数错误；一等内建函数值已接共享身份与动态调用（M3.3） |
| `print` 的 `sep` / `end` | ✅ M2.4 | 静态路径直接写分隔符与结尾；`file` / `flush` 依赖 M3.4 |
| 普通 class、单继承、方法、绑定方法、`property` / `staticmethod` / `classmethod` | ✅ M2.5 / M2.6 | 运行时 `PyClass` / `PyInstance`；类体在 `class` 语句位置顺序执行 |
| 用户自定义异常（`class E(ValueError)`） | ✅ M2.6 | codegen 生成 CLR 主继承树；io.UnsupportedOperation 的额外父边由 MIR 原位展开 |
| `__repr__` / `__str__` / `__eq__` / `__hash__` / `__bool__` / `__len__` / `__iter__` / `__next__` / `__getitem__` 与常用运算槽 | ✅ M2.5 | `PyObject.TryMethod` 统一分派；未支持的魔术方法不伪造结果 |
| `with` / 多上下文 / `as` / 异常抑制 | ✅ M2.8 | 去糖为 `try` / `except BaseException` / `finally`；`__enter__` / `__exit__` 走普通属性与调用协议 |
| 常用标准库 | 🚧 M3.4–M3.6 | 按独立 API 表开放；不是整个模块兼容 |
| 多重继承、元类、任意描述符、动态代码与 import hook | ❌ | 暂缓，明确拒绝 |
| 继承内建容器（`class X(dict)`） | ❌ | 仍明确拒绝；`class X(object)` 使用普通类路径 |
| 单继承字符串（`class S(str)`） | ⚠️ 有限 | 真实载体/身份、默认值协议与现有常用方法；守卫与未支持域见下文 |
| `async with` | ❌ | 明确报 `E0100`，不退化成同步上下文 |

### bytearray 切片赋值有限候选

仅接精确 bytes/bytearray、现有连续 B/c memoryview、精确 list/tuple/range 整数序列右值。
slice 按 step/start/stop 转换；不同 bytearray 一轮，其余源在两轮之间转换/快照，第二轮
完成后按当前目标长度调整。空右值删除扩展选中项，非空扩展错长先 ValueError。
活动导出允许同长写，真实 resize 才 BufferError，所有别名仍见同一目标对象。
通用 iterable、原生子类、其它 view 布局、复杂 index descriptor 与警告型 index 返回
不计支持，明确 NI；单元素/其它容器旧索引边界不改变。首次24项专项、三unique应用
（首2/3后基础窄迁单独1/1）和正常完整门禁1771C#/228CP/202e2e+30原ignored通过；
算法/成本及首失败来源见 [ADR0098](adr/0098-bytearray-slice-assignment.md)。

## 2. 类型映射

| Python | Phase 1 `Ty` | MIR | CIL |
| --- | --- | --- | --- |
| `int` | `Int` | `I64` | `int64` |
| `float` | `Float` | `F64` | `float64` |
| `bool` | `Bool` | `Bool` | `bool` |
| 已证明的精确字符串（例如字面量） | `Str` | `Str` | `string` |
| `str` 注解的未知值 | `OpaqueStr` | `Dynamic` | `object`（保原对象身份） |
| `None` | `None` | `Unit` | `void` |
| `@dataclass` | `Class` | `Dynamic`（默认） | PyInstance 引用 |
| `@dataclass(frozen=True)` | `Class` | `Dynamic`（默认） | PyInstance 引用（ADR-0009：保留身份） |
| 无法静态定型 | `Dynamic` ✅ M2.1 | `Dynamic` | `object`（装箱 CLR 值） |

### 2.1 静态特化与动态降级（M2.1）

函数按**整体**选择执行路径，不混用：

| 情况 | 路径 |
| --- | --- |
| 标注齐全 + 所有调用点实参一致 + 体内可静态定型 | 静态（Phase 1 的 i64/f64 直算） |
| 缺任一标注、异构赋值、多返回类型、调用点实参不符、超出 i64 的字面量 | 动态（装箱 `object` + `PyOps`） |

降级是**单调**的：一旦无法证明就永久降级，用不动点迭代保证收敛。动态路径的运行时内核见
[ADR-0010](adr/0010-dynamic-value-representation.md)：`PyOps`（算术 / 比较 / 真值）、
`PyInt`（long → BigInteger 升级）、`PyRepr` / `PyNone`、`PyBaseException` 家族、`PyCast`
（流入静态槽位时的拆箱）。

标注只是优化线索：实参类型不符不再产生编译错误，而是让被调函数降级并最终在运行时抛 Python
`TypeError`。`str + int`、`"a" < 1` 等静态可见的错误也不再拒绝编译，交给 `PyOps` 给出与
CPython 一致的消息。

`str` 注解不是实际字符串类型证明。C0 阶段以 `OpaqueStr` 保存任意传入对象，参数、返回、
模块槽和字段不强制转为 CLR string。只复制或返回该值的静态函数仍可保留无关数值循环和
精确字符串局部；比较、真值、属性、调用等未证明安全的消费沿原整函数动态回退，包括结果
为 bool 的表达式。literal-only 的注解返回也使用 object，可能使调用者丢失精确事实；尚无
性能测量。此阶段不实现字符串子类布局，详见 [ADR-0062](adr/0062-opaque-str-annotations.md)。

### 字符串前后缀窗口（候选）

`str.startswith/endswith` 接受单个文本或tuple候选及可选start/end，None使用默认，
类型级 `__index__` 按start→end各一次；先固定原实参引用再进入回调，tuple按序短路。
窗口按码点而非UTF-16下标，超尾start不能夹成有效空窗，匹配不能切开正常代理对。
直接、保存、根描述符保各自绑定错误优先级；bytes既有实现未变。

真实字符串子类所有方法改为实参前捕获属性，getter失败阻止实参求值，返回非callable仍先
求完实参；用户异常不在新入口广catch。普通实例/精确文本保持原派发，但每次资格判定增加
类型检查；子类方法失去原PIC热命中并可能新建实参数组。单参不扫描全串，有界正码点位置
仍可能线性扫描，不加缓存，不宣性能收益。警告型index返回、复杂descriptor及D009保边界。

当前新增23运行时与34旧回归通过，四应用与一结构通过（首次3/5保留），正常完整门禁1630C#/228CP/202e2e+30原ignored通过。首失败是原生异常cause属性
赋值旧债，并非cause读取缺失；详情与后续验收见[ADR-0089](adr/0089-text-affix-window.md)。

### 字符串 partition/rpartition 的有限合同（ADR0108）

只接受一个位置sep，真实str/str子类可用，无隐式转换；空sep报ValueError。根入口self错误
优先，随后keyword、arity、sep类型、空sep。结果为三项tuple，命中中项保原sep身份，
未命中partition首项/rpartition末项保原receiver身份，含真实子类；切出的两侧为exact str。
首/末ordinal搜索拒绝正常代理对内部的端点，失败候选后继续搜索并保重叠；动态孤立代理
可作为输入，独立high+low拼接的来源区分仍属D009，不把UTF16守卫称为表示修复。

候选12项C#、三主CP/JIT与独立30观察通过；继承类对象的实参前属性捕获首项已证失败，
完整CP源单列pending，后续super等项未执行。候选002正常门禁2045C#/240CP/208e2e通过，
36ignored=原33+三新边界；3060性能样本保留，旧rfind根/find预备等回退风险未排除，见
[ADR0108](adr/0108-string-partition.md)。本API通过不意味着docopt原入口通过。

### 字符串子类：有限布局与公共协议

精确字符串仍为 CLR `string`；单继承 `str` 及其用户后代使用带不可变文本载体的真实实例，
保类身份和普通实例属性，不把子类对象悄悄展平成静态字符串。类体与校验、装饰器完成后才
发布候选类的公开绑定；类体或装饰器失败保留旧绑定。实际布局依据基类对象，不依据名称。

- 支持 `S()`、`S(object)`、`object=`、显式 `str.__new__(S, object)` 与有限 `__init__`；
  根 `str.__init__` 与 `object.__init__` 共享身份和参数规则。多继承/混合专用布局不开放。
- 默认长度、真值、迭代、下标、切片、包含、拼接、重复及已有 `%` 子集复用文本算法；
  长度/下标/切片/迭代按码点。默认比较和哈希兼容现精确字符串，用作 dict/set 键可互查。
- 方法范围为 `encode`、`upper/lower`、`strip/lstrip/rstrip`、`split/splitlines/join`、
  `replace`、`find/rfind/index/rindex/count`、`startswith/endswith`，沿各方法已有参数边界。
  支持普通方法覆盖、property、实例属性遮蔽、保存的绑定方法、根方法及 `super()` 调用；
  文本参数直接读取载体，不隐式调用用户 `__str__`。不代表任意自定义描述符已支持。
- 公共 `str()`/`repr()` 保用户协议合法返回的字符串子类原对象身份；普通根文本转换产物仍为
  精确字符串。`str` 仅已证明精确值的单普通位置参数保旧快路，其余完整求值并绑定，不漏多参
  或星号。`ord` 读原文本；`int`/`float` 保各自用户转换、`__index__`、原文本解析的有限优先级，
  `int(text, base)` 不以用户 `__int__` 替代原文本解析。

当前边界（不能据本节推断完整 `str` 子类兼容）：
- 创建期及后续修改明确拒自定义 `__new__`、布局/属性拦截槽、任意自定义描述符，以及覆盖
  默认 sequence、比较/hash 槽；`__reversed__/__format__/__round__/__length_hint__` 也未开放。
  `add/mul/mod/sub/truediv/floordiv/pow/and/or/xor/lshift/rshift` 的普通、反向和原地运算槽
  均受守卫限制；后九组含既有反向派发顺序债，并非声称这些运算本来都属于字符串协议。
  类 `__name__/__qualname__` 修改、类/实例 `__dict__` 与实例 `__class__` 修改仍明确拒绝。
- `encoding/errors` 解码构造尚未实现；合法解码域明确 `NotImplementedError`，不假作普通
  `str(object)`。路径、正则、JSON 等其它 consumer 没有随本切统一接入，不宣称接受子类载体。
- 排序统一沿现精确字符串的 UTF-16 ordinal；非 BMP 与 BMP 混合排序不等于 CPython 码点序。
  `tests/fixtures/str_subclass/unicode_order.py.pending` 与 `custom_new.py.pending` 保完整合法
  CP 期待，不计通过；不把旧排序差异改写成错误的 CP 期待。

C0 的 object 参数/返回/槽位及保守动态回退成本仍在；未知对象转换使用现完整动态 binder，
可能增加参数数组/调用开销，不宣称所有字符串快路或性能不变。本轮本地首专项为 runtime
36/36、既有 RuntimeBases 6/6、driver 4/4（1 MIR + 3 CP/JIT 源差分），含根初始化身份。
正常完整门禁本地通过（764 C#、228 CP基线、202 e2e+30原ignored），未发布；
不新增项目、NativeAOT 或性能运行，不宣称 pyfiglet 通过。详见
[ADR-0063](adr/0063-string-subclass-layout.md)。

动态属性访问、容器、异常捕获已在 M2.2–M2.5 接入，模块系统在 M2.7、`with` 在 M2.8 接入；
多重继承、元类、任意描述符等暂缓能力仍在编译期报诊断，不静默降级成占位值。

### 公共 format：独立对象协议与有限规格（候选）

`format(value, format_spec='', /)` 走现Dynamic内建、返回object；先拒关键字，再检查1..2个位置参数，
最后检查真实str/str子类规格。用户槽收到原规格对象；类链上的`__format__`一次绑定调用，
忽略实例同名值与`__getattr__`，返回真实str子类时保原对象。函数、staticmethod/classmethod、
property返回可调用对象及普通callable槽有专用绑定；任意Python自定义descriptor明确NI。
默认object空规格调用str并保结果身份，非空先TypeError而不先str。专门原生布局不乱套object协议。

文本按当前Python码点口径作宽度/精度和fill，支持Unicode15 Nd规格数字；无变换的非空文本
规格可返回原子类对象，空规格子类调用`__str__`。str子类自定义`__format__`守卫尚未开放。
整数支持省略/d/b/o/x/X、符号/对齐/前缀/补零；bool仅真正空规格返回True/False，其余按整数。
新format十进制输出按固定默认4300位，非十进制无该限制；配置API不随切实现。
分组、c/n、整数浮点presentation等合法未实现域明确NI，非法语法/接收类型组合仍ValueError。
浮点阶段2仅空规格；随后阶段3开放显式g/G（默认6、精度0按1），支持符号/宽度/对齐/补零；f/e/%、无typeprecision、z/#/grouping等仍明确NI。
NaN/Inf/±0短结果保符号规则，非零有限值复用固定.NET10显式有效精度；CP的INT_MAX
错误边界与后端实际999999999精度限制分开，后者不限制未调用后端的特殊数/零值。

真实CLR最大字符串容量之外明确NI，与CP资源异常域不同；不设任意一百万上限，
正常可分配请求照常，容量计算覆盖整个UTF16结果。OOM不捕获重试，不偷偷截断。
精确值不查询用户类槽，规格记录为局部值类型；二幂进制按字节线性转换。
公共调用仍有动态参数数组/binder成本，不冒零分配或实测性能提升。

旧f-string继续使用既有FormatValue和string ABI，并未跟此builtin统一；它的bool/浮点/Unicode
规格与单字段子类身份仍有差异。通用重复**键、部分混合*求值顺序、显式object/str/super根format
也未因本切修复。长尾CP正期待在`tests/fixtures/format_builtin/long_tail.py.pending`，不计通过。
当前候选新C#27/27及四项CP/JIT已过；异常链保首次旧nested-caught帧债，再以模块级cause窄迁通过，正常完整门禁877C#/228CP/202e2e+30原ignored通过。
详见[ADR-0073](adr/0073-builtin-format-protocol.md)，不声称tabulate、main、AOT或性能通过。
阶段3首次49项C#（新22+原27）和1项297调用CP/JIT差分通过，正常完整门禁899C#/228CP/202e2e+30原ignored通过；见[ADR-0074](adr/0074-format-float-general.md)。

## 3. 运算规则

### 3.1 算术（`T` 为结果类型）

| 左 \ 右 | `int` | `float` | `bool` |
| --- | --- | --- | --- |
| `int` | `int`（`/` 例外 → `float`） | `float`（左侧提升） | 视 `bool` 为 `int` |
| `float` | `float`（右侧提升） | `float` | 视 `bool` 为 `int` 后提升 |

- `**`：`int ** int` 在指数为非负**字面量**时结果为 `int`；指数为变量时 Phase 1 报错要求写成 float（因为 Python 在负指数时返回 `float`，静态无法确定）。
- 位运算只接受 `int` / `bool`。
- `str` 只支持 `+`（与 `str`）、`==` / `!=`。

### 3.2 比较与逻辑

- 比较运算两侧可以是 `int` / `float` 的任意组合（自动提升），结果为 `bool`。
- `if` / `while` 的条件：`bool` 直接使用；`int` / `float` 视为 `!= 0`；其它类型 Phase 1 报错。
- `a and b` / `a or b`：Python 返回**操作数本身**而非 `bool`。Phase 1 要求两侧类型相同，结果类型即该类型。

## 4. 语义陷阱清单

> 每一条都应该有对应的 e2e 用例。新发现的陷阱请追加到这里。

| # | 现象 | Python | CIL / .NET 原生 | RyuPy 处理 |
| --- | --- | --- | --- | --- |
| 1 | 负数整除 | `-7 // 2 == -4` | `div` 得 `-3`（向零截断） | `PyMath.FloorDivI64` |
| 2 | 负数取模 | `-7 % 3 == 2` | `rem` 得 `-1` | `PyMath.ModI64` |
| 3 | 真除法 | `7 / 2 == 3.5` | 整数 `div` 得 `3` | 先 `Cast` 为 f64 再 `div` |
| 4 | 整数溢出 | 任意精度，永不溢出 | `add` 静默回绕 | 动态路径按 ADR-0010 升级 BigInteger；静态特化仍是 i64：`add.ovf` 等触发时由入口包装器转成 Python `OverflowError`（带 traceback，不泄漏 .NET 异常与调用栈），`//`、`%`、`abs`、`**` 在运行时函数里显式转换。去掉标注即可走动态路径 |
| 5 | 浮点打印 | `1.0`、`1e+16`、`1e-05` | `1`、`1E+16`、`1E-05` | `PyFormat.FloatRepr` |
| 6 | 布尔打印 | `True` | `True` | 一致，但不要依赖 `bool.ToString()` 的区域设置 |
| 7 | `print` 换行 | 总是 `\n` | `WriteLine` 在 Windows 是 `\r\n` | 统一写 `'\n'` |
| 8 | `range` 负步长 | `range(10, 0, -3)` → 10 7 4 1 | — | 计数循环的比较方向随步长符号变化 |
| 9 | `range` 步长为 0 | `ValueError` | 死循环 | 字面量 0 编译期报错，变量运行时检查 |
| 10 | 先读后写的局部变量 | `UnboundLocalError` | CIL local 默认为 0 | 动态槽位初始化为「未赋值」哨兵，读取时检查并抛 `UnboundLocalError`；静态槽位（i64/f64）无法表示哨兵，仍编译期报错 |
| 11 | 深递归 | `RecursionError`（默认 1000 层） | 栈溢出直接崩溃进程 | M2.2 起在 `PyTraceback.PushFrame` 按帧数守卫，默认 1000 层、可用 `RYUPY_RECURSION_LIMIT` 调整；抛可捕获的 `RecursionError` |
| 12 | 除以零 | `ZeroDivisionError` | 整数抛 `DivideByZeroException`；浮点得 `inf` / NaN | 真除法与 `//` `%` 全部走运行时检查，抛 `ZeroDivisionError`；未捕获时按 Python 形式退出（M2.2） |
| 13 | `int(nan)` / `int(inf)` | `ValueError` / `OverflowError` | `conv.i8` 结果未定义 | 运行时函数检查 |
| 14 | 字符串长度 / 下标 / 切片 / 迭代 | 按码点 | `string.Length` 与下标按 UTF-16 code unit | M2.3 起统一走 `PyText`：`len("😀a") == 2`，`s[0] == "😀"`，切片与迭代按码点推进 |
| 15 | 浮点 NaN 比较 | `nan < 1` 为 `False`，`nan != nan` 为 `True` | `clt` 正确；`!(a < b)` 式的取反实现会出错 | 比较指令不得用「取反」实现 `>=` 等 |
| 16 | `-0.0` | 打印 `-0.0` | 打印 `-0` | `FloatRepr` 处理 |
| 17 | `frozen=True` dataclass 语义 | 对象仍有稳定身份，字段还能指向可变对象 | .NET valuetype 传参 / 赋值为值拷贝 | M2.5 起改为引用类型：禁止字段写入，但传参 / 赋值只复制引用，身份稳定（ADR-0009） |
| 18 | 回收与析构时机 | CPython 多数对象按 refcount 回收；Python 语言不保证即时析构 | .NET GC 延迟回收 | 明确不复现 refcount / 析构 / 弱引用回调的具体时机，见 D001；资源用 with / finally / close |
| 19 | 类型标注 | Python 不据函数 / 变量标注强制运行时类型 | 静态签名会限制实参类型 | M2.1 起改为「标注只作优化线索」：实参不符时被调函数降级为动态，不再报编译错误 |
| 20 | 字典键与顺序 | 相等的数字键保持一致哈希，dict 保持插入顺序 | .NET 集合默认行为不能直接等同 Python | M2.3 统一 Python equality / hash 与顺序，覆盖 bool / int / float 混合键 |
| 21 | 模块初始化 | 按首次导入执行，循环中可见部分初始化对象 | .NET 静态构造时机不等同 Python import | M2.7 独立模块对象、缓存与初始化入口，不依赖静态构造自动复现 |
| 22 | 方法与属性重绑定 | 后续查找须看到新绑定 | 缓存会保留旧目标 | M2.5 起类属性查找缓存携带全局修改版本，任意增删 / 替换立即失效；M2.7–M2.9 再扩展模块与 PIC |
| 23 | 生成器清理 | close / throw 与 finally / with 联动 | 简单 IEnumerator 不覆盖全部协议 | M3.2 保存异常和清理状态；析构触发关闭的时机仍受 D001 限制 |
| 24 | 整数与浮点相等 | `9007199254740993 == 9007199254740992.0` 为 `False` | 先转 `double` 后两者同值，得到 `True` | `PyHash` 的混合数值比较一律走 `PyInt.CompareWithDouble` 精确比较 |
| 25 | NaN 作为字典键 | `nan == nan` 为 `False`，但同一个 nan 对象能在字典里查到自己 | — | `PyHash.Equal` 不含身份兜底，`PyKeyComparer` 含（见 §4.3） |
| 26 | 实参求值顺序 | 全部实参先求值，再进入被调者 | 逐个下发 `Write*` 会边求值边输出 | M2.3 起先生成所有实参的临时量，再统一输出（见 §4.4） |
| 27 | 序列比较 | 只在同族内字典序，跨族抛 `TypeError` | — | tuple / list / bytes 系列分族；错误写的是元素类型而非容器类型（见 §4.3） |
| 28 | 静态 i64 上限（RyuPy 偏差） | 任意精度 | — | 静态特化路径的 `+ - * // % abs ** <<` 结果超出 i64 时抛 `OverflowError`，消息注明「RyuPy static integer limit」。动态路径无此限制（D003） |
| 29 | `int ** int` 的结果类型 | 指数为负时返回 `float`（`2 ** -1 == 0.5`） | — | 指数是确定的非负字面量时走静态快速幂；否则整表达式降级为动态，由 `PyInt.Power` 按真实指数返回 int / float |
| 30 | `for` 循环变量与 `range` 参数 | 循环变量可被循环体重写而不影响迭代；`range(...)` 参数只求值一次 | 用循环变量当计数器会破坏迭代 | 静态计数循环使用隐藏计数器：每轮开头才写用户变量，边界复制进局部变量 |
| 31 | 排序稳定性 | `sorted` / `list.sort` 稳定；`reverse=True` 仍保持相等元素的原始顺序 | `List.Sort` 不稳定，且「排完再反转」会颠倒相等元素 | `PySort` 二分插入 + 自底向上归并（只用 `<`，见 #43）；降序通过反转「先后」判断实现 |
| 32 | 空容器真值 | `[]`、`{}`、`""`、`set()` 为假 | 引用类型默认非空 | `PyOps.Truthy` 显式列出每种容器类型（含 dict 视图、bytes / bytearray） |
| 33 | 解包数量不符 | 少了报「not enough」，多了报「too many」 | — | 受控序列保 `UnpackCheck(length, required, hasStar)`；非星号一般源的修复为 `UnpackSequence(source, required)`：读取 N 项后仅多探测一次，不取 len / length_hint；普通实例仅缺少 `__iter__` 时可按整数 `__getitem__` 回退。先冻结本层值再写目标，失败不回滚已完成的目标写入。一般星号候选见ADR0068：两次iter、向首iterator取len/hint、耗尽第二iterator再冻结后缀，中段始终新list；嵌套目标未扩展。既有非星号12项运行时与4项driver（3应用差分＋1结构）及正常完整门禁本地通过（ADR-0064） |
| 34 | 指数 / 移位量上限（RyuPy 偏差） | 任意大的指数会尝试构造巨大整数 | — | `PyInt.MaxBitShift = 1_000_000`：超过时报 `OverflowError`，避免一次运算耗尽内存；`2 ** 100`、`1 << 100` 等常用规模不受影响 |
| 35 | 参数绑定错误 | 运行时 `TypeError`：缺少 / 过多 / 重复绑定 / 未知关键字 / 位置-only 当关键字 | — | `PyBinder` 按 CPython 规则绑定并生成相同措辞的消息；编译期只判断能否走静态快路径，**不**把绑定错误变成编译错误 |
| 36 | 默认值求值时机 | 在 `def` 执行时于模块作用域求值、只求值一次；可变默认值跨调用共享 | — | 默认值在定义位置（`FunctionDefaults` 语句）写进模块静态数组；`def f(x=[])` 的所有调用共享同一个 list |
| 37 | f-string 格式说明符 | 支持嵌套插值（`f"{x:{width}}"`） | — | 当前只支持纯字面量规格；嵌套插值报 `E0100`。格式子集：宽度 / 对齐 / 补零 / 精度 / 进制 / 百分比 |
| 38 | `id(x)` | 返回对象地址（CPython 内部细节） | — | 返回运行时的稳定身份哈希；不伪造地址，登记为 D002 的内部观测差异 |
| 39 | 序列拼接与重复 | `[1] + [2]`、`(1,) + (2,)`、`"ab" * 3`、`3 * [0]`；只允许同类拼接，次数 ≤ 0 得空序列 | — | M2.12：`PySequenceOps` 实现 list / tuple / str / bytes / bytearray 的 `+` / `*`，错误消息按 CPython（`can only concatenate list (not "tuple") to list`、`can't multiply sequence by non-int of type 'float'`） |
| 40 | 增量赋值的原地语义 | `a += b` 对 list 原地 `extend`，别名可见、`a is` 原对象；对 int / str / tuple 生成新对象 | 去糖成 `a = a + b` 会丢失别名 | M2.12：HIR `Binary { inplace }` → 操作码标记位 → `PyOps.InPlaceBinary`；覆盖 list / bytearray / set（`\|= &= -= ^=`）/ `__iadd__` 等，错误消息写 `+=` |
| 41 | 增量赋值目标只求值一次 | `a[f()] += 1`、`g().x += 1` 中 `f` / `g` 只调用一次 | 读、写各降级一次会执行两遍 | M2.12：`aug_assign.rs` 把除不可变字面量外的子表达式（含名字）先求值进隐藏局部，冻结原对象/索引并跨暂停保存 |
| 42 | `True` / `False` 与小整数的 `is` | `True` 是单例；[-5, 256] 的 int 被缓存，`x is 0` 为真 | 每次 `box` 都是新对象 | M2.12：bool 装箱复用单例、小整数装箱走缓存；`PyOps.Is` 对 bool 与 [-5, 256] 的 int 按值判定（不依赖装箱来源）。范围外的 int 仍按对象身份，与 CPython 一样不应依赖 |
| 43 | 排序只用 `<` | `sorted` / `list.sort` 只调用 `__lt__`，不调用 `__eq__` | — | M2.12：`PySort` 改为只用「小于」的二分插入 + 归并；此前每次比较先 `==` 再 `<`，会多调用用户 `__eq__` |
| 44 | `%` 字符串格式化（有限子集） | `"%d-%s" % (1, "a")`、`"%(name)s" % mapping` | — | M3.7 支持无修饰 `%s/%r/%a/%d/%i/%u/%%` 与映射键，逐项求值；flags/宽度/precision/其它转换及 bytes 仍未支持，详见 [string-percent.md](string-percent.md) |
| 45 | 类体内引用同类定义的函数名 | `v = property(getv, setv)` 读到类体刚定义的原始函数 | 不能提前绑定 self，也不能把类命名空间捕获进方法正文 | 既有 ClassScope/ClassNamespaceGet 路线经专用真实差分确认；别名、defaults/decorator、条件 fallback、方法与内部闭包的模块 late 名及 getter super 均有断言。旧 E0106 记录已过期，本轮仅补证，见 ADR0067；不外推任意 descriptor 或失败类发布 |

| 46 | 闭包 late binding 与默认值 | 闭包读调用时的变量；默认值保留定义时的结果 | 复制捕获值会丢失共享修改 | M3.0：一个绑定对应一个 cell，多闭包共享，默认值在 def / lambda 位置求值 |
| 47 | 空 cell 的异常 | 本地 cell 抛 `UnboundLocalError`，外层自由变量抛 `NameError` | null / 默认零值无法区分 | M3.0：cell 放未赋值哨兵；读取时按作用域选择异常，被捕获的 except 绑定在正常、返回、循环跳转和异常退出时都清空 cell |
| 48 | 推导式作用域 | 目标不泄漏，闭包共享该次推导式的循环绑定 | 普通内联循环会覆盖外层变量 | M3.1 三类急切推导式使用独立隐式函数与共享 cell；见 §4.7 |
| 49 | `__iter__` 的返回值 | 必须是迭代器，不再调用返回值的 `__iter__` | 递归获取会重复副作用、误接收仅可迭代对象 | M3.1 修复 `PyIter.CreateFromObject`，无 `__next__` 立即抛 `TypeError` |

### 4.1 未捕获异常与退出码（M2.2）

入口编译成两层：`Main(string[]) -> int` 是包装器，先 `PyTraceback.SetFile(...)`，再
`try { __main(); return 0; } catch (PySystemExit e) { return exitCode(e); } catch (PyBaseException e)
{ Report(e); return 1; } catch (Exception e) { ReportClr(e); return 1; }`。最后一层是兜底：
静态 i64 算术仍可能抛 CLR 的 `OverflowException`，`ReportClr` 把它映射成 Python 的
`OverflowError` 并按同样格式退出，**不会**打印 "Unhandled exception" 与 .NET 调用栈。
所有路径汇合后先 `Builtins.Flush()` 再返回退出码，保证带缓冲的 stdout 在 stderr 之前写全。
未捕获异常因此表现为：

```text
Traceback (most recent call last):
  File "x.py", line 3, in fail
  File "x.py", line 9, in <module>
ValueError: source

The above exception was the direct cause of the following exception:

Traceback (most recent call last):
  File "x.py", line 5, in <module>
RuntimeError: outer
```

stderr 上是 traceback，最后一行是 `Type: message`，退出码 1。`raise ... from ...` 用
「direct cause」分隔，处理期间再抛出的异常用「During handling of …」分隔。

**栈帧是怎么来的**：编译器在函数入口调 `PyTraceback.PushFrame`、每条语句前调 `SetLine`、
统一返回块调 `PopTo`；异常被捕获时处理入口再补一次 `PopTo`，把被展开的帧弹掉。
抛出前 `PyErrors.PrepareRaise` 把当前栈快照挂到异常对象上。

### 4.2 `try` 的编译方式（M2.2）

- **纯静态目标 `except`** 保留 CIL 的**类型化 catch**。CLR 按「类型及其基类」匹配；CLR 主继承树之外的已登记额外父边（目前仅 io.UnsupportedOperation → ValueError）由 MIR 在原 handler 位置展开独立 catch，见 io-unsupported-operation.md。用户异常内部按所属模块限定身份，不以显示名覆盖其它模块或内建异常。
  `except (A, B)` 为每种类型各生成一条记录；每条记录配一个**独立的两指令处理入口**
  （`stloc` 取下异常 + `leave` 跳出），因为 CLR 会把「try 与 handler 区间完全相同」的
  两条记录当成重复子句直接拒绝。
- **异常表顺序**必须内层（更早结束）在前，否则 JIT 判 `InvalidProgramException` 且不报原因。
- **`finally`** 不用 CIL 的 finally 处理器：CLR 的 finally 不能用 `leave` 离开、也无法抑制
  正在传播的异常，而 Python 的 `finally` 必须支持 `return` / `break` 与吞掉异常。
  实现改为「catch-all 异常通道 + 状态码 + 分派块」：所有离开路径（正常落下 / return /
  break / continue / 异常）先把状态码写进局部变量再跳进 finally 体，执行完由分派块决定
  下一步。`finally` 里 `return` 会覆盖待返回的值，与 CPython 一致。
- **裸 `raise`** 取运行时保存的「当前异常」，没有则抛 `RuntimeError`。
- **`except … as e`** 的分支结束后 `e` 被标记为未赋值，之后再读抛 `UnboundLocalError`。

**用户自定义异常**：M2.6 起 `class E(ValueError)` 也会生成对应的 CLR 子类，codegen 在模块入口
初始化异常工厂委托；`except E` / `except ValueError` 因此仍由类型化 catch 完成匹配。
traceback 的
帧来自编译器插桩，`__traceback__` 属性暂时是 `None`，完整 frame 对象与 `sys._getframe`
属于暂缓能力。

### 4.3 容器与相等性（M2.3）


容器表达式在类型层**统一降级为 `Dynamic`**：元素类型、键类型都不是编译期概念，`[1, "a"]`
也不再分两种 Python list 类型。这不禁止 MIR 在有证明/守卫的热点采用内部特化表示；
M3.10 的数值列表岛已按 ADR-0039 完成有界验收，不能将其冒称通用容器已经完成特化。

- **相等性与哈希**集中在 `PyHash`：`Equal` 负责 `==`，`Of` 负责 `hash`。字典与集合通过
  `PyKeyComparer` 把两者映射成 CLR 的 `IEqualityComparer<object>`。
- **`==` 与键查找的身份语义不同**（有意）：`nan == nan` 必须为 `False`，但同一个 `nan`
  对象作为字典键时必须能查到自己。前者对应 CPython 的 `PyObject_RichCompare`，后者对应带
  指针快判的 `PyObject_RichCompareBool`；因此 `Equal` 不含身份兜底而 `PyKeyComparer` 含，
  两边不能合并。
- list/tuple 元素相等以及 list 成员查询也需先认同一对象，再调用元素相等协议；
  共享 NaN 可在容器内认同自身，直接 `nan == nan` 仍为 False。既有字典**值**比较
  尚缺同一身份短路，独立欠账见 `m3_shared_nan_dict_pending.py`，未算本数值岛支持范围。
- list 整数下标支持一次 `__index__` 调用，并在回调完成后按当前长度处理负下标；
  超出宿主索引范围为 IndexError。自定义 `__index__` 返回 bool 的弃用警告协议仍明确
  NotImplementedError，不将原生 bool 下标与该返回值边界混淆；此窄修不代表其它序列已同步支持。
- **int / float 混合比较必须精确**：`9007199254740993 == 9007199254740992.0` 在 CPython 是
  `False`，而把整数先转成 `double` 再比较会静默得到 `True`。统一走 `PyInt.CompareWithDouble`。
- **数字键三元组**：`True`、`1`、`1.0` 互为相同键且哈希一致（即 `hash(True) == hash(1)`）。
- **序列比较**只在同族内成立：tuple / list / bytes 系列各自做字典序比较，跨族（如
  `list < tuple`）抛 `TypeError`；元素不可排序时诊断里写的是**元素**类型而非容器类型。
- **字典布局**：`PyDict` 用「键 → 槽位下标」的索引加平行的键 / 值数组，删除留墓碑，墓碑过多时
  压缩；删除是 O(1)，迭代按槽位顺序即插入顺序。`Version` 只在增删 / 清空时递增，**覆盖已有键
  不递增**（CPython 允许迭代中给已有键赋新值）。
- **迭代期间修改**：字典迭代器记下开始时的 `Version`，增删后再取下一个元素抛 `RuntimeError`；
  但 `dict.keys()` 视图是「活的」，新建一次遍历看到的是当时内容，不校验版本。可变集合候选仍保O(n)
  快照，但独立游标按初始size检查变化并粘滞报错，真正耗尽后不复活；非空clear后恢复同size明确NI，
  CP正例保pending。无clear的同size变异仍是快照旧债，不能把CP所有修改行为笼统称未定义（ADR0090）。
- **字符串按码点**：`PyText` 提供 `Length` / `At` / `Slice` / `Items`，下标、切片、迭代与 `len`
  都按码点而不是 UTF-16 code unit 计数。
- **排序稳定**：`sorted` / `list.sort` 使用 `PySort` 的稳定归并排序，`reverse=True` 通过反转
  比较实现，相等元素保持原始顺序。
- **循环引用**：`PyRepr` 用「正在处理」集合保护，自引用 list 打印 `[[...]]`、自引用 dict 打印
  `{'a': {...}}`，不会无界递归。

### 4.4 `print` 的实参求值顺序

`print(a, b)` 必须**先求值全部实参，再统一输出**。早期实现边求值边写（`Write*` 逐个下发），
使 `print(x, x.pop())` 写出的是调用前的内容，且实参里嵌套的输出会插到中间。现已改为先生成
全部实参的临时量，再按序 `WriteSep` / `Write*` / `WriteNewline`。

### 4.5 普通类与属性协议（M2.5 / M2.6）

普通类不生成 CLR 类型：类体是运行时语句，MIR 在 `class` 语句位置调用 `PyOps.MakeClass`，
然后按源码顺序执行属性赋值、方法定义与表达式副作用。因此类体顺序、方法默认值求值时机和
模块语句的相对顺序都与 CPython 一致。

- **查找优先级**：`property`（数据描述符）→ 实例字典 → 类链；方法与 `staticmethod` /
  `classmethod` 在读取时绑定。`del obj.attr` 只删除实例字典中的值，类属性继续可见。
- **绑定方法与super**：`PyBoundMethod` 记录定义类与接收者。默认直写零参仍用旧方法上下文，
  不把lookup owner/进入时self当作完整CP词法规则；显式实例普通函数改用真实类cell与当前首参，见S1。
- **缓存失效**：类属性查找带缓存，条目记录全局修改版本。任何类属性增删 / 替换都会让缓存
  失效（陷阱 #22）；条目超过 256 条整体清空，容量有界。
- **魔术方法**：`__repr__` / `__str__` / `__eq__` / `__hash__` / `__bool__` / `__len__` /
  `__iter__` / `__next__` / `__getitem__` / `__setitem__` / `__delitem__` / `__contains__`
  与常用一元 / 二元运算槽接入统一入口。默认对象身份仍在：没有 `__eq__` 时按引用相等，
  类链完全缺失 `__hash__` 槽时按身份哈希；None 槽拒绝，其他非可调用值报调用错误。普通类创建期抑制规则的有限修复已通过本地专项及完整门禁，见 ADR0066。
- **身份**：普通对象始终是引用；`frozen=True` dataclass 也改为 .NET 引用类型，装箱 / 传参
  只复制引用，满足 ADR-0009 的默认身份要求。
- **默认类读取**：普通实例没有用户类链同名项时，`__class__` 返回实际类引用并压过
  实例同名字段；存在用户同名项时保留 None、property、普通值及方法绑定优先级。
  普通类对象读取真实元类，不取自身字典同名值；PyType 载体补 type 固定点。
  PIC 根身份值不接受字段遮蔽，仍按全局类版本失效。详见 ADR0109；这不是动态换类、
  一般描述符/元类、全载体反射或真实 str/list/dict 派生用户覆盖的全面支持。
- **暂缓能力**：多重继承、元类、自定义 `__getattribute__` 在 HIR 报 `E0100`，
  不静默降级。

### 4.6 闭包作用域（M3.0）

赋值与参数创建当前函数绑定；被子函数引用的绑定变为 cell。`nonlocal` 跳过只转发环境的
中间函数，指向最近的外层函数绑定。`global` 指向模块，并截断子函数对该名字的向外查找。
模块变量不创建 cell。分支和循环里的 def 保留原来的执行位置，多个工厂调用各有独立 cell。

声明与参数冲突、同时 global / nonlocal、先使用或赋值后声明、没有外层绑定以及模块级
nonlocal 都报 `E0103`。嵌套 async def 报 `E0100`；嵌套 def 装饰器沿定义位置的外层作用域求值。
类体lambda的默认值读取定义点类命名空间，body仍读取外层而不捕获类命名空间。
函数内 class、`del` 局部名和 cell 的反射接口仍未支持，不能据闭包用例推定可用。
共享环境的容量和 GC 生命周期见 [ADR-0015](adr/0015-shared-closure-cells.md)。

### 4.7 list / dict / set 推导式（M3.1）

最左 iterable 在外层求值并只取一次迭代器；循环目标、后续 iterable、if 与元素表达式属于
独立隐式函数作用域。多层循环从左到右嵌套，每层条件按源码顺序短路，元素只在所有条件成立
时求值。循环目标支持单个名字及已有的平坦序列解包，不泄漏到外层，异常退出也不覆盖外层同名值。

推导式里的 lambda 共享该次推导式的循环 cell，保留 late binding；默认参数仍在创建 lambda
时求值。不同次推导式各有独立 cell。嵌套推导式可捕获外层推导式或函数绑定。
字典在每轮先求 key 再求 value，最后才进行哈希与插入；重复键保留首次位置但更新值。
字典插入用单次索引查找，避免重复调用自定义键的 `__hash__`；覆盖已有键仍保留位置和结构版本。
集合通过正常 add 协议去重，元素表达式仍逐次执行，不为去重跳过副作用。
类体推导式、async for、海象表达式明确拒绝。生成器表达式复用独立作用域与 M3.2 状态机：
最左 iterable 立即求值并取得迭代器，其余 iterable、条件和元素延迟到 next；没有急切结果容器。
生成器表达式内零参 super 暂拒绝；send 的值被其内部语句 yield 忽略，throw/close 等限制同普通生成器。
设计与分配代价见 [ADR-0016](adr/0016-comprehension-scopes.md)，不宣称性能提升。

此外，用户 `__iter__` 的返回值必须已具备迭代器协议，不再递归对其调用 `__iter__`；返回 list
等仅可迭代对象立即抛 `TypeError`。入口 Main 的真实 PE 方法头启用局部清零，保证成功退出 0，
不依赖平台上未初始化栈内存的偶然内容。

### 4.8 函数限定名与定义来源（ADR0119）

已支持的def/lambda保真实词法限定名，global定义重置、nonlocal保持词法路径，类体
改名不改变方法默认限定名。公开精确字符串__qualname__是真实可变成员，普通字典
同名项不能遮蔽；无效写入和删除按CP TypeError。运行时隐藏推导式函数不决定公开
限定名：按CP3.12，list/dict/set推导式中的lambda沿外层名，genexpr用<genexpr>。

首行来自首decorator expression、无装饰器def或lambda真实源行，随共享签名冷存，
只供内部消费者，不提供公开代码对象。__code__读写删、真实str子类限定名赋值及
未知native代码来源均明确NI；不假存代码替换、不用FuncId造行号或剥离str子类身份。
已有genexpr目标外部自由名捕获边界仍pending，不因只读来源扫描修复而声称解决。
完整函数反射仍未完成；overload三API有限注册另见下节与[ADR0120](adr/0120-typing-overload.md)，来源前置见[ADR0119](adr/0119-function-definition-metadata.md)。

### 4.9 typing.overload 有限注册与查询（ADR0120）

真实 overload/get_overloads/clear_overloads 与统一 dummy 保稳定身份；注册只解一次
__func__，按当前 module/qualname/真实定义首行将原参数强引用写入公开真实三层字典。
同首行覆盖保位置，查询返回新 list 快照，miss 返回新空 list；clear 清当前 registry
但不使旧快照失效。method/static/class wrapper 的只读 __func__ 保底层真实身份。

键接受 None、精确文本/bool/int/float；已知不可哈希容器报 TypeError，未知键协议、
自定义 mapping/factory/helper 与未知 native 来源在实际消费处明确 NI。已建前缀桶
遇错不回滚；clear 不 hash 旧键。真实公开 code、完整 wrapper 元数据、自定义属性
拦截仍未开放。API 改写 __qualname__ 后错误前缀仍为 canonical 名，partial 的 registry
repr 与 CP 不同，两个完整正例保持 pending，不能称完整兼容。性能、强引用增长与
门禁范围见 [ADR0120](adr/0120-typing-overload.md)。

## 5. 模块与入口

- 顶层语句按顺序组成隐式 `main`，入口模块名为 `__main__`；模块顶层代码编译成独立的初始化函数，由运行时导入机制触发，**编译期不执行副作用**。
- 顶层定义的变量是**模块级槽位**（M2.7）：模块顶层代码与函数体读写同一份存储；函数里的 `global` 声明不是必需，但声明后名字解析仍落到模块命名空间。
- 模块变量类型由顶层赋值提示保守推断；入口整体降级为动态执行时，槽位也统一装箱，避免 BigInteger 等值写回静态槽位。
- 模块搜索顺序：入口文件所在目录 → 命令行 `-I/--search-path` 给出的额外根（按给定顺序）。编译期只加载可达模块；未登记的模块名在运行时抛 `ModuleNotFoundError`。
- 普通包按 `__init__.py` 初始化；相对导入在 HIR 换算成绝对模块名。导入点分模块时先初始化父包（`a.b.c` → `a` → `a.b`），成功后把子模块挂到父包属性上。
- 导入前先登记「初始化中」的模块对象，循环导入能看到部分初始化状态；重复导入复用同一对象；初始化失败移除失败模块条目，已成功依赖保留。
- `from x import *` 与函数体内 `import` 明确报 `E0100`；自定义 import hook、namespace package 与热重载暂缓。

### 5.1 `with` 的展开

`with ctx as x: body` 在 HIR 去糖为：

```text
mgr = ctx
exit = mgr.__exit__          # CPython 同样在 __enter__ 之前取出
value = mgr.__enter__()
x = value
handled = False
try:
    body
except BaseException as exc:
    handled = True
    if not exit(type(exc), exc, None):
        raise
finally:
    if not handled:
        exit(None, None, None)
```

多上下文按源码顺序嵌套，外层 `try` 包住内层的 `__enter__`；`finally` 状态机保证
正常落下、`return`、`break`、`continue` 与异常路径都只调用一次 `__exit__`。
`__exit__` 返回真值抑制异常；清理时再次抛出会替换原异常（与 CPython 一致）。

已知边界：这里用 `mgr.__enter__` / `mgr.__exit__` 的普通属性查找（便于复用现有属性协议），
而 CPython 的 `with` 机制按类型槽查找。实例字典里显式遮蔽 `__enter__` / `__exit__` 属于冷门写法，
当前行为不同，登记为偏差 D008。

## 6. 兼容范围与性能实现

Phase 2 优先动态核心、容器、普通类、异常、模块与 `with`；Phase 3 增加闭包、生成器、推导式、装饰器和标准库。完整协程生态、元类、任意描述符、多重继承、内建容器继承和解释器内部观测暂缓，Phase 5 汇总清单，不承诺最终全部补齐。

保留求值顺序、异常传播、可变别名、普通对象身份和容器行为的优化可默认采用。低影响偏差经 ADR 接受并公开登记后，也可选高性能默认实现。固定宽度整数、可观察的值拷贝、浮点重排等影响常用结果的扩展由用户显式选择；这些扩展目前尚未实现。refcount 与 GC 时机属于已接受架构边界，不代表其它差异也可省略。

动态运算 / 方法调用使用**有界 PIC**（[ADR-0012](adr/0012-bounded-dynamic-pic.md)）：每个调用点最多四条形状，全局站点数有上限；未命中完整走通用路径。类属性替换、实例属性增删会让方法缓存失效，模块属性不缓存。`RYUPY_PIC=0` 可整体关闭，用于 A/B 对照。

M2.11 在不改变「动态值 = 装箱 object」的前提下消除了大部分固定分配（[ADR-0013](adr/0013-dynamic-hot-path-allocations.md)）：纯位置小参数调用复用调用点数组，thunk 对参数个数正确的调用直接取参，`long` 数值字面量装箱一次后进入静态字段，实例属性改为按类槽位存放并由属性 PIC 直接读写，方法不调用 `super()` 时跳过上下文维护。未命中仍然回到同一套通用路径。

原生、桥接和带偏差结果分别登记。单个 e2e 用例只能证明其覆盖范围，不能证明常见项目或整个标准库已经可用。

### 生成器当前原生范围（M3.2）

调用生成器函数只绑定参数并创建状态，不执行函数体；即使 yield 不可达也返回生成器。
每次 next 恢复局部、临时量、循环迭代器和共享 cell 引用，yield None 与正常耗尽分离。
完成或异常后永久耗尽并释放保存引用；重入抛 ValueError，意外逃逸的 StopIteration 转 RuntimeError 并保留原因。
每次恢复独立登记 / 清理 traceback，暂停不会把调用栈留在线程状态里。
支持语句 / 表达式yield、send、return value、现代单参throw、close、yield from及try/except/finally/with跨暂停清理；
生成器内零参super仍报E0100（尚未保存方法上下文），旧式throw(type,value,tb)明确TypeError。
表达式 yield 的结果为 send 实参，next / send(None) 接收 None；新建生成器拒绝非 None send 且不启动。
闭合生成器 send 任意值均耗尽，重入仍抛 ValueError；send 恰好接受一个位置参数。
lambda 中的 yield 也创建生成器；默认值在外层执行，复合表达式先前求值结果保存到帧。
推导式内部不允许 yield；最左 iterable 属于外层，允许外层生成器在此暂停。
完成值由触发首次结束的 next 以 StopIteration.value 暴露，args 保留非 None 终值作为单个原对象；
异常构造不提前调用终值 __str__，仅实际格式化时读取最新内容。
再次耗尽为 None / 空 args。for、list、next(default) 消费结束时不缓存终值，之后 next 不会再次领取。
此处不表示一般 StopIteration(...) 构造已完整支持；通用异常构造 ABI 的无参 / 显式 None / 多参保真仍待补齐。
现代throw接受异常实例或类型；新建throw不执行函数体，已关闭throw直接抛参数异常。close新建不执行body，
暂停后注入GeneratorExit并运行清理；若仍yield则RuntimeError但保留新暂停点。自动GC触发close不承诺（D001）。
yield from逐项转发next/send/throw/close，消费委托StopIteration.value；自定义迭代器终值不经MoveNext吞掉。
显式StopIteration单参也保留原对象和延迟str；无参/显式None及多参构造差异仍单列待做。
异常上下文用每生成器节点链保存，暂停后恢复调用者；普通运行时错误在Retry清栈前挂context并保存原traceback。
状态机首版保存全部局部，存在装箱与数组读写代价，尚无性能收益测量，见 ADR-0017。

属性 / 下标增量赋值现保存原对象、索引或切片，再读取旧值、求RHS、原地运算并写回；
RHS重绑定或暂停不改变写回位置，写回失败也不撤销已发生的原地修改。property setter本身仍未支持。
字典字面量现支持yield：按固定CPython 3.12基线，≤15对先按key/value顺序求全部表达式再插入，
16–17对逐对求值插入；故小字典后值异常前不hash，16–17对首hash异常则阻止后续求值。
超过17对的全标量字面量键继续直接插入；动态键在每对key/value各求值一次后，
由运行时精确检查真实int/bool/str/float/None值（含CLR BigInteger），再插入尚未公开的字典。
非标量键（包括tuple、bytes、用户对象）在hash/eq/repr之前明确NotImplementedError；
用户键的分块缓存hash合并仍未实现。原E0100范围收窄为运行时守卫，不为无用户回调的
标量键模拟CPython内部临时字典；不改变<=17项及字典`**`的既有边界。
暂停调用现支持`*`位置实参：多个位置项时逐个star立即展开快照，唯一位置项为star时
保留源至关键字准备结束后的最终调用才展开；位置组先于关键字组求值，无论源码关键字写在何处。
当前还对带`**`展开的调用实参内yield报E0100，映射快照/重复键时序留待独立竖切。
可以先把 yield 的接收值赋给局部，再进行这些操作；不把该限制冒称完整表达式兼容。

tuple / set 星号展开现支持暂停和普通路径：tuple从构建列表冻结为tuple；set首star前先求全部前缀元素再建set，后续逐元素插入。
普通iterable的set展开交错next与hash，不先物化列表。若源本身是set/frozenset且包含用户实例/用户异常
（含tuple/frozenset嵌套），缓存hash复制尚未实现，提前抛NotImplementedError而不重新运行用户hash。

支持的暂停调用现已在参数求值前绑定目标：函数/普通类对象保存到临时局部，方法立即取属性。
property和缺属性错误在参数yield前发生；非callable属性在参数求完后才报TypeError。
内建方法绑定固定接收者和类型静态入口，参数恢复后先校验支持的arity再执行副作用。
ADR0070 将 list.clear/pop/reverse/copy 的直接/PIC、保存和暂停路径统一为关键字优先的窄绑定：
实参完整求值后拒绝错误数量，失败不执行容器正文，仍保实参自身副作用。14项C#、3项真实应用和1项结构及完整门禁已通过；未测性能。
其它方法维持既有路径；ADR0070 当时保留的 pop 用户 __index__ 与宽整数索引旧债，
后由 ADR0071 窄切接线：绑定后只转换一次，转换先于空表、按回调后当前长度在 long 域判界；
失败不截断删除或物化数值背板，用户回调副作用保留。三个新应用、新7项C#及1项结构和完整门禁通过。
返回 bool 的弃用警告及真实 int 子类未闭，不宣称全部索引消费者完成。
旧静态dataclass暂停构造专用ABI仍报E0305；M3.6默认动态dataclass走普通类绑定/调用。int/float/str/type/异常及bool/bytes/bytearray的暂停构造
目前仅接受单位置参数，super暂停调用仍拒绝，未宣称完整内建构造签名支持。

### 用户 hash 返回与整数值 hash

ADR0072 候选严格区分：用户 __hash__ 返回实际整数，fits-i64保原值但-1→-2，超界才按整数hash归约；
整数自身hash始终按2^61−1有符号归约并规范-1，等值整数double复用同规则。常用long增边界/哨兵分支，
宽值才取模，不宣零成本；非整数double/随机化/空表有效键调用次数不扩。新11项C#、3项真实应用及完整门禁通过；没有性能测量。

### Unicode内建的表示边界

ord/chr支持普通码点、补充平面和单独代理项；但两个独立代理项拼接后当前UTF-16表示会合并为代理对，
`ord(chr(0xD800)+chr(0xDC00))`与CPython不一致。D009及`m3_builtins_surrogate_pair.py`记录为待修，
不将它算作完整代理字符串兼容，也不作为已接受性能取舍。

### 4.9 装饰器与元数据（M3.3）

装饰器表达式按源码顺序先求值，随后默认值/函数创建或基类/类体执行，最后逆序应用；
只有全部成功才替换公开绑定。跨模块读取同一最终结果，不能按原函数/类静态直达。
函数 __name__/__doc__/__annotations__ 和用户属性字典可读写；包装不自动复制元数据。
M3.6 注解保存定义时的真实对象，functools.wraps 已开放限定元数据复制；完整反射仍未完成。
M3.3 曾仅对混合装饰器 dataclass 使用动态 PyInstance；M3.6 默认统一为该引用表示，
字段、frozen 与身份遵守装饰顺序，并扩展默认值/factory 与限定字段对象（见 stdlib-metadata.md）。property的
getter/setter/deleter复制描述符；classmethod绑定实际访问类。详细边界见
[ADR-0027](adr/0027-definition-time-decorators.md)。

### 系统、流与路径（M3.4）

当前原生 API 与参数范围见 [stdlib-system.md](stdlib-system.md)，设计见 ADR-0028。
标准流可替换；print 捕获单次目标，flush 的真值转换先于任何写入；input 只去末尾 LF。
正常退出和 SystemExit 都尝试刷新当前 stdout/stderr；一个目标失败不跳过另一个，
失败退出 120，不自动 close 用户重绑定流，不保证 CPython finalization 的重复刷新次数。
文本 cookie、编码、换行和关闭后错误有差分；显式 with/close 清理不依赖 GC。
sys.path为受控只读视图；sys.modules仅准入ADR0131的受限新模块别名，不是可写dict。不支持动态import hook；环境与模块keys/items仍为快照。
Path.absolute 保留 ..，os.path.abspath 归一化；POSIX 实机验证与 Windows 限定入口测试分开记录。Windows os.path.dirname/join 支持普通盘符相对、根重置与 UNC 词法规则；设备命名空间、需要非 ASCII 盘符/UNC 大小写等价判断的 join 明确未支持。basename/split/splitext/normpath/abspath 仍保原 Windows 拒绝，不视为完整 ntpath。
Windows 实机文件行为与 C-long 退出边界未验收，不能宣称跨平台完整兼容。
io.UnsupportedOperation 已通过 CLR 主树、Python 额外父边及原位 typed catch 展开，支持
OSError/ValueError 两父捕获；无参/单参构造、既有流能力错误和内存流 fileno 的准确范围见
[io-unsupported-operation.md](io-unsupported-operation.md)，该增量修订 ADR-0028 的旧边界，见 ADR-0031。
完整文件描述符、编码错误策略、复杂 glob 等长尾仍未支持；StringIO 非零相对定位仍为普通 OSError。
文本逐码点实现优先语义，未声称性能提升。
M3.4 当时发现正整数字面量仅支持到 u64::MAX；M3.7 已扩展到下文有界 BigInt token 路径。int(十进制字符串) 也可创建更大整数，
不把后者的运行时 BigInteger 能力误写成任意大小源码整数字面量支持。

### 数值、JSON 与正则（M3.5）

原生声明范围见 [stdlib-numeric.md](stdlib-numeric.md) 与 ADR-0029，不代表完整模块。
math显式适配域/溢出/NaN和数值协议，宿主libm有限结果按容差验收；time单调时钟无固定锚点承诺。
JSON保留大整数/插入顺序/hook，dump按块读取当前write，编码失败保留已写前缀；
源码码点错误位置与真实ValueError子类分开于宿主UTF16位置。自定义cls/真值flag与bytes surrogatepass
仍有明确拒绝及独立pending；深度256与默认4300位十进制转换是公开容量边界。
re只接收经过Python子集解析的模式，Unicode分类固定3.12.13/Unicode15，不采用.NET默认分类/组编号。
空匹配后同位置非空重试、回调None空替换、达到count立即停止均有回归；
模式长度/译后体积/嵌套256/单次匹配2秒保护与64项缓存见API表，未支持语法明确拒绝。
不可变 bytes 域已接现有匹配、捕获、替换及 escape API，40专项与四应用通过、正常完整门禁通过；身份、ASCII、模板两阶段拼接及可变输入边界见 ADR-0080，不代表完整 re 或项目兼容。
条件组候选支持闭合ASCII编号/名字及数字前向，保持捕获参与/回溯、组号和既有消费者；开放捕获、text非ASCII条件token及极长数字条件仍明确NI。后端差异与编译期成本见ADR0082；283专项、3应用及正常完整门禁已验，不代表项目通过。
模块异常导出只读，实际用于typed handler的稳定import/from别名禁止跨模块改写；
本地同名源码及用户同名异常不会误认。普通模块/函数的有限动态except已接入Name、Name起点属性链和tuple表达式：目标在异常后轮到时读取，混合链全部动态分派，按真实类身份而非名字匹配。目标失败跳出本try，恢复context并清理选中的as绑定；纯static路径不变。动态函数在该异常链及共享后继可能多出LoadChecked，包含正常落下路径，不能声称零热成本。Call/Subscript等表达式、新动态生成器域及静态nested tuple旧偏差仍未闭；模块/普通local删除旧域不因此支持。8运行时专项、编译8项、结构1项和四应用复验已过，正常完整门禁1466C#/228CP/202e2e+30原ignored通过，见[ADR0085](adr/0085-dynamic-except-dispatch.md)。
repr补不可打印控制/分隔符与孤立代理项转义，不把日志中的控制字符当作可打印文本直接写出。

### 容器工具、函数工具与类型元数据（M3.6）

逐 API 范围见 [容器工具](stdlib-collections.md)、[函数工具](stdlib-functional.md)、
[数据类与元数据](stdlib-metadata.md)，设计及性能见 ADR-0030。
原生 dict 子类包装参与 ** 展开、JSON 与复制；可变容器不能作为 hash 键。
缓存保存强引用并按容量淘汰，partial 与方法描述符严格区分；contextmanager 的装饰器路径
同样保存异常处理上下文。显式 cause 不复制原异常，from None 仅抑制报告链而保留 context。
默认数据类统一保留 Python 类与实例身份，属性修改或 init 替换使构造计划重新检查/回退；
不会为保留旧静态速度而忽略生成 eq、factory、property 或 frozen 错误。
类型元数据不等于运行时类型检查；Protocol、TypeVar 与完整前向引用仍未实现；NamedTuple 有限真实范围见本文后节。
普通类隐式 hash 槽有限候选见 ADR0066；Hashable 的 ABC 结构判定/缓存仍独立 pending，不用内部容器预筛冒充 ABC 正确性。

### 源码模块之间的普通运行时基类

`from package import Base as Alias; class Child(Alias)` 与 `class Child(module.Base)`
在定义位置读取实际绑定，并由普通类守卫检查完整单继承祖先链，不传播跨模块
`ClassId`，也不以导入名猜测类型。别名/模块属性改绑为另一个普通用户类时继承新值；
`from` 旧绑定继续保留原类对象。类装饰器表达式、基类表达式、类体、逆序装饰应用按
既有定义顺序执行；仅全部成功后发布最终公开绑定，失败保留旧绑定及已发生的副作用。
本地继续派生运行时基类的类，也保留动态公开槽和真实基类检查。

首切仅开放已解析模块槽名字或属性基类表达式，普通类保持 `PyClass/PyInstance`
动态表示；直接工厂调用基类、多重继承和类关键字/自定义元类仍未实现。
运行时守卫不调用用户 `hash/eq/repr`，按引用身份检查祖先；非普通值、内建类型、
异常类、Enum、dataclass，以及需要专用入口的原生类均在类体前明确
`NotImplementedError`。已验证的原生 TestCase/Enum 和静态异常类入口保持原能力。
这不是模拟所有 CPython 元类失败时序：例如整数基对象可让 CPython 先执行类体、
最终构造再失败，该路径独立 pending，不冒称原生兼容。

成本是每个类定义一次 O(继承深度) 守卫与引用身份集合分配、以及运行时公开绑定；
没有给每次实例构造/字段读写增加守卫，不宣称比静态定义路径更快。
验收：`m3_7_runtime_bases.py` 与源码 fixtures、`runtime_bases` driver、
`PyRuntimeBasesTests`；特殊基类和非类元类时序各有独立 pending 正例。

### 函数内导入和局部注解目标

静态 import 可在函数/方法/生成器实际执行位置发生，局部、global、nonlocal 和捕获 cell
保留绑定身份；失败不覆盖旧值，from 多项按前缀逐项完成。依赖图发现不是初始化，
AOT 仍要求未调用函数的静态依赖源码可编译。动态导入和星号导入仍未开放；
函数内仅直属、先于handler且无重绑/cell的已证原生异常别名可静态catch，非任意动态异常表达式。完整边界见 [function-imports.md](function-imports.md)。
函数内属性/下标注解不求值，右值和目标求值/写入仍执行；注解中 yield 的符号判定
保留，未支持绑定语法不静默忽略。详见 [annotation-targets.md](annotation-targets.md)。

### 大整数字面量与旧异常别名

大整数 token 在 HIR 有界正规化，复用 MIR BigInt 常量池与运行时解析 ABI；全部进制
限制为 4300 个有效十进制位，超界明确 E0100，不声称 CPython 所有进制的无限容量。
不能证明参数默认值适合静态槽时仅降级所属函数，小整数安全路径保留；
详见 [integer-literals.md](integer-literals.md)。
IOError、EnvironmentError 规范化为同一个 OSError 类对象，直接构造、catch、
用户派生和生成器注入匹配复用原路径；局部绑定仍优先。一等 OSError 类对象调用
尚未实现，原新增验收保存在独立 `m3_7_oserror_firstclass_pending.py`，不冒称完成。


### TypedDict total-only 与 Ellipsis

TypedDict 类定义的有限专用入口仍求真实基值，不删 TYPE_CHECKING 分支；单父合并
字段与 total 只影响本类声明，运行时调用返回 dict 而非校验值类型。
Required/NotRequired、前向引用、非简单注解目标及复杂元类保持明确未支持，
详见 [TypedDict API](typing-typed-dict.md)。
Ellipsis 为真实单例，源码 `...` 不受用户重绑同名内建名字影响，
`typing.Tuple[T, ...]` 保参数身份；同名模块槽保 Dynamic 以支持未绑定时的窄单例回退。
其它内建模块槽回退仍未实现，不能据此宣称完整动态名称查找。


### 普通类体条件与已知字符串属性

普通类体 if/elif/else 在定义位置执行，可能绑定名字仅在 own 字典缺失时才求模块/内建
回退；方法仍不捕获类命名空间。所有分支的注解语法先触发空字典初始化，未执行声明
不写字段；普通含条件类的后置dataclass按实际注解dict工作，不用静态union字段。
特殊Enum/TypedDict/显式dataclass条件仍诊断，详见 [类体条件](class-body-conditions.md)。
已知Str的属性读取交已有动态协议，不再因静态接收者类型误拒split/upper等；当前
函数粒度策略会降所属函数，不改变独立数值函数，详见 [字符串属性](string-attributes.md)。


### 模块属性删除的既有边界

编译模块的属性写入支持现有公开槽与动态属性；`del module.name` 尚无对应
删除协议入口，当前抛 AttributeError。M3.9 叶调用特化验证用优化前保存 CLI
与新候选确认同一缺口；完整删除后旧函数别名仍应可调用的期望保留在
`tests/cases/optimization/leaf_identity_deletion`（pending），不计入特化正确性通过。


### object 基础实例与既有富比较边界

ADR0049历史首切只补真实canonical object及无字典基础实例、有限单继承根关系；当时尚未分派用户__new__，显式object.__new__(用户类)也未实现。后续ADR0050的有限普通构造已随打包批次进入main，当前行为以下段为准。全局比较ABI的bool返回
及!=取反旧债保留完整CP正期待为ignored，不因sentinel基础比较通过宣称任意富比较完成。
详见[ADR-0049](adr/0049-base-object-identity.md)，验收状态以该文档实际记录为准。


### 普通构造协议与槽历史限制

ADR0049的object基础实例已独立验收；后续[ADR0050](adr/0050-ordinary-construction-protocol.md)
已处理普通单继承new/init并进入main，不能把A历史拒绝当B最终行为。按实际返回类选择init，
foreign返回跳过；special布局customnew、finalized类own new删除/late根槽重设明确拒，
不模拟CP槽生命周期。完整合法CP历史正例留pending；全局富比较bool ABI旧债仍未修。


### int 构造签名与文本进制（本地候选）

`int()`、单参数数值转换、str/bytes/bytearray文本及显式base共用实际绑定和严格解析。
除固定单普通位置数值参数外，参数全部按普通调用求值/展开后检查，不丢base或错误调用的副作用。
支持base0或2至36、前缀/符号/严格下划线、Unicode十进制数字；bytes不作UTF8数字转换。
显式base的`__index__`先于读取文本，普通对象无base先`__int__`再`__index__`。
非2幂进制固定4300位，尚无sys阈值设置；2幂线性位打包，无额外人工bit上限。

弃用`__trunc__`、协议返回bool所需warning和无base memoryview解析仍明确NotImplementedError，
[原CP正例](../tests/fixtures/int_constructor/deprecated_protocols.py.pending)保留，不计通过。
直接bool及base=False不属于上述弃用路径。完整int子类/构造元数据、其它构造器签名不在本切。
静态float转I64宽度旧边界未扩大，详见[ADR0053](adr/0053-int-constructor-binding.md)。

错误链测试另暴露旧异常快照差异：函数内部抛出并就地捕获的异常可能提前带入尚未传播的外层caller帧。
[完整原CP期望](../tests/fixtures/int_constructor/nested_context_frames.py.pending)保留；本切不修改全局traceback。
int的模块级context→函数调用错误仍逐项核完整帧和链顺序，不能以新case通过冒称该旧差异已修。


### abs 调用形态（本地候选）

只有固定一个普通位置参数保原数值转换快路；零参、多参、关键字及星号完整求值/展开后绑定。
成功绑定才调用一次`__abs__`，返回任意对象身份不改；abs自身绑定及badoperand错误记录调用现场。
通用调用层未加EH或name检查，无效星号展开的既有现场边界保完整pending，不冒全调用栈修复。
详见[ADR0054](adr/0054-abs-call-binding.md)，不是新项目或性能结果。


### float 有限构造（本地候选）

只接受零或一个位置参数；额外参数、关键字和星号完整求值/展开后绑定。保单数值静态快路，冷对象按类型级`__float__`、缺失才`__index__`顺序转换，错误返回或抛错不后退。exact double动态转换与协议double结果保身份，文本先完整验证再CLR舍入，str固定Unicode15、bytes/bytearray仅ASCII语法，float不受int的4300位限制。原生数值子类/弃用警告和memoryview等buffer长尾仍明确未支持并保CP正期望。见[ADR0055](adr/0055-float-constructor-binding.md)，未宣称全静态身份、项目或性能验收。


### bool 与长度真值（隔离候选）

零或一个位置参数完整绑定后才求真值；普通实例先`__bool__`，缺失才`__len__`，后者可通过结果对象的`__index__`取得长度。负长度先ValueError，正向超过64位上界才OverflowError。共享冷路径影响条件/not/and/or等，primitive/container与数值静态条件不改；独立len()和其它native对象真值能力不据此宣称完成。索引弃用警告与通用展开帧保原pending。见[ADR0056](adr/0056-bool-binding-and-length-truth.md)。


### 显式文件包资源（本地候选）

`build/run --include-module NAME` 只把实际源码模块及父包纳入编译图，初始化仍由正常Import触发，包含不等于复制数据。新选项暂限ASCII点分标识符，旧源码Unicode导入不收窄。`importlib.resources.files(anchor)`首切仅实际登记的文件包：字符串先正常导入，模块对象按不可变注册位置返回现有Path，不由可改`__file__`或cwd猜目录。JIT使用源码根；AOT仅显式选中资源锚点可用，未选不回退源目录。`--resources`仍AOT-only。

非包锚点、默认caller、弃用package关键字warning及其它loader长尾明确未实现；显式spec/loader覆盖也拒绝，完整CP正期待保pending。目录/IO沿现有Path语义，不引入新的资源沙箱。见[ADR0057](adr/0057-explicit-package-resources.md)，不等于完整importlib、pyfiglet、AOT项目或性能验收。


### shutil 通用流复制（本地候选）

`copyfileobj(fsrc, fdst, length=0)` 绑定后求length真值一次，假值使用模块当前COPY_BUFSIZE；
先缓存read再缓存write，逐轮read/求真值/write原对象，忽略write返回，负length和短读不改变
循环规则。不自动flush/close/seek或回滚已写数据，用户回调异常沿既有协议传播。
尚不提供copy/copyfile/copymode等路径复制API，不能以导入成功代表完整shutil或pyfiglet通过。
本地专项与完整门禁通过，尚未发布，详见[ADR0058](adr/0058-shutil-copyfileobj.md)。


### zipfile 尾记录识别（本地候选）

`is_zipfile` 仅按固定CP3.12.13识别EOCD/可选ZIP64，不验证中央目录、成员、CRC或解压，
True不保证ZipFile可打开。借入流位置会改变且不关闭，自开路径流自行关闭；只吞指定
OSError/BadZipFile并恢复已处理回调现场。BadZipFile通过真实异常类型及既有静态catch接线，
不是名字伪装。完整任意buffer/位置算术及ZipFile等API未纳入，详见[ADR0059](adr/0059-zipfile-tail-identification.md)。
本地专项与完整门禁通过，尚未发布；不新增项目或性能通过声明。

### optparse 常用选项（本地开发候选）

真实Option/Values身份、live defaults、短选项簇和长缩写、交错参数及错误上下文按有限合同实现；
默认帮助保原空白、流调用与构造时COLUMNS快照，帮助字符串缓存跨调用保留。
不提供完整formatter/OptionGroup/callback/复杂nargs及任意内部状态猴补。
同parser递归帮助和未显式metavar的非ASCII dest帮助为显式未支持边界，正常解析不因此禁止。
当前47项专项C#、真实差分6/6与完整门禁本地已验；未发布，不声称pyfiglet已通过。
详见[ADR0060](adr/0060-optparse-common-options.md)。

### len 冷长度结果协议（有限本地验收）

len 的完整动态参数建立不变；exact 容器及字符串长度分支不变。用户长度结果与普通实例
真值共用返回 long 的冷校验：实际整数/直接bool合法，其它结果只调用一次类型级 __index__，
负长度先 ValueError、正超64位上界 OverflowError，不退到 __int__ 或递归转换。
直接 obj.__len__() 仍返回用户原对象。len 无槽报错与 bool 默认真、__bool__ 优先保各入口差别。
只准备本层新造错误，用户正文异常不 catch/reprepare；关键词优先于数量，len 的自身绑定
采用固定 CP 消息。__index__ 返回bool的弃用警告仍明确NI，真实整数子类相关域保正例pending；
复杂star/双星号共享债未改。源码必要审、新10＋旧bool12项runtime及新len3＋旧bool3项driver及首次正常完整门禁已通过，未发布，见
[ADR-0065](adr/0065-len-result-protocol.md)。

### 通用星号解包的有限本地验收（ADR0068）

一般源使用UnpackEx，完整执行len/hint回调及错误校验；hint查找/绑定与调用TypeError边界不同，
真实hint整数先ssize_t范围再判负。最多4096仅限制容量预留，实际元素数不设上限；
有效巨大hint的资源耗尽时点与CP可能不同，不承诺MemoryError等价。
非星号及直接无展开tuple/list字面量结构不改；未知descriptor明确NI，不静默跳getter。
新16项runtime、2项ABI及4项driver（3应用＋1结构）通过；正常完整门禁818 C# / 228 CP / 202 e2e＋30原ignored通过，未发布，不外推项目/AOT/性能。

### 模块函数公开绑定有限本地验收（ADR0069）

只对已有模块写入槽与直接顶层函数同名，或重复直接def的名字，使用真实公开动态槽；
排除class参与名字并保留旧动态类成功域。定义的默认值/元数据/装饰全部成功后才发布，
旧alias和defaults保独立；函数体late global与callee先于实参冻结均按该槽执行。
选中及既有运行时发布的顶层函数使用Dynamic参数/返回，注解不能成为运行时类型限制；
受影响main及模块槽整体装箱成本沿现规则，未碰撞数值函数结构另验，不声称零损失。
首次HIR4/ty2与新3应用、原star同名完整源及原capture身份例通过；结构1项及正常完整门禁818 C# / 228 CP / 202 e2e＋30原ignored通过，未发布。
无碰撞def提前读取、只有外部属性重绑的内部静态调用仍是旧债；未扩del/exec/eval或全LEGB。

### getopt 与局部稳定异常（本地候选）

真实 `getopt.getopt` 支持确切 list/tuple 参数和确切字符串声明，保短组合/参数、长精确与唯一缩写、重复项、停止规则及尾切片身份。
longopts先转换，候选完整扫描后才精确命中；长参数真值一次、短参数不测真值，用户异常原对象传播。
真实GetoptError/error同身份，构造固定两项args，msg/opt写入独立且删除回落空串；直接__str__保原msg，公共str验证并保真实字符串子类身份。
一般输入协议、gnu_getopt、异常用户派生/args重赋值未开放；首切不提供完整模块helper或反射。
局部异常证明仅编译期增加分析，不提前执行导入或污染模块保护名；源码同名模块优先，所有未证/重绑/捕获候选明确拒绝。
短cluster为码点游标，args真实slice最坏累计O(n²)，保回调修改隔离而非宣称优化收益。
当前21项C#、PE1、HIR1+4和5项CP/JIT差分已验，正常完整门禁920C#/228CP/202e2e+30原ignored通过；首失败与测试旧债见[ADR0075](adr/0075-getopt-local-exception-imports.md)。
不称tabulate、main、AOT或性能通过，普通内建异常__module__及函数内caught链正例保pending。


### 函数模块成员与有限调用来源（本地已验，未发布）

真实函数对象在定义时捕获所属模块当时的 `__name__`，成员可独立赋值/删除并保任意对象身份，
不与 `__dict__` 同名项混同。默认值与普通注解在创建前完成，失败定义不发布新函数。
最终类型分类为 Dynamic 的函数携实际 self，直调在实参前冻结函数引用，实参后从同一实例读取默认值；
别名和闭包经真实 thunk，生成器暂停/恢复保同一引用。模块每次执行创建独立来源。
Static 保旧参数/返回 ABI 与数值结构，来源未知时内部查询明确 NI，不借用祖先模块名。
这不是完整 frame API；当时尚无 namedtuple 消费者，后续有限工厂及其默认 module 使用见 ADR0077，不能据此前置推导全部消费者。
Dynamic 直调增加一次私有槽读取/空检查、引用实参与帧存取；生成器增加引用保存，不宣称零成本或性能收益。
来源/ABI/叶内联、PE与真实源码结构及三个 CP/JIT 应用专项已验；正常完整门禁通过：932 C#、228 CP 基线、202 e2e 与30原 ignored。CP 基线不代表 Ryu 支持数。
真正 `del __name__` 后创建函数的源码保留旧 E0100 pending，活跃 None 写入不等于键缺失。
完整边界及首次失败见 [ADR0076](adr/0076-function-module-origin.md)。


### namedtuple 有限真实类型（本地专项已验，未发布）

`collections.namedtuple` 创建独立真实类与不可变tuple载体，保存实际class及字段槽；普通tuple实例不增加类字段。支持位置/关键字/defaults、readonly字段、type/isinstance/tuple祖先、tuple值协议、跨生成类helper和真实方法预捕获。工厂普通iterable按fields逐项str、rename重复truth、defaults完整消费、最后module查询顺序运行；显式非None module原样保存。

默认module来自当前真实Dynamic函数/生成器或模块执行来源；Static未知来源NI，不猜当前globals。五应用只对Dynamic运行域做CP/JIT差分，Static另有分类/ABI结构门，不能冒Static调用结果也已验。

类/descriptor/helper修改、类__dict__、用户派生、copy/deepcopy、异cls显式构造、非生成类duck helper、复杂函数反射，以及Unicode/NFKC名称等首切明确NI。sequence-only __getitem__输入尚未接此API，不能把Unpack支持外推。继承tuple.index起止参数与部分错误文本保旧边界。

名字仅随schema保留，不使用CLR永久intern；跨独立类的等值typename/field名字is可能与CP不同，原CP正例保pending。同类metadata/defaults容器身份及元素共享保持。defaults/_make会调用校验hint但不按巨大hint预分配，资源耗尽时点不承诺与CP一致，不吞OOM重试。

本切unseal PyTuple、每PyClass布局引用及通用方法入口增加类型判断，有实际布局/JIT成本，未测性能，不称零回退。50项C#与5应用123标签/原流已验，正常完整门禁首次通过（982C#、228CP基线、202e2e+30原ignored）；该门禁阶段未新项目/AOT/性能验证；后续固定tabulate入口仍因缺html失败。详细取舍见 [ADR0077](adr/0077-namedtuple-layout.md)。


### html.escape 的逐步对象协议（本地验收已通过）

无入口str强转：三个replace之后才一次quote真值，再按需两次replace，返回最后对象。每步只对exact CLR string直接替换，str子类和普通对象保动态lookup/call及重绑时点。绑定错误单独准备caller/context，正文原异常不重包；完整用户错误链和高阶旧边界见[ADR0078](adr/0078-html-escape.md)。当前不声称unescape/parser或固定项目通过。


### zip_longest 的按槽耗尽状态（本地验收通过）

每次成功next立即冻结当前槽值，同一个iterator重复输入按真实次序消费；每槽Stop后不再调用该槽，非Stop错误则永久关停整个zip。每次推进独立row，保一次安全重入，不增加busy拒绝或修改已发布tuple。局部异常边界保原对象/已发生帧并恢复ambient，未测危险递归耗尽/error不冒完整兼容。公开验收与资源成本见[ADR0079](adr/0079-zip-longest.md)。

### TextWrapper 的初始化与动态hook（本地候选已验）

普通类初始化按序保存配置原对象，失败保已发生setter作用；wrap/fill及块算法真实动态调用，列表别名和hook重绑可观察。仅严格未变根实例可复用旧区间引擎，不跳过模块函数构造。默认高级算法、元数据旧债、复杂度与阶段证据见[ADR0083](adr/0083-textwrapper-class.md)，当前不声明项目通过。

### str.format 的字段执行与结果身份（本地候选已验）

模板按字段依次解析执行，路径逐步lookup，前字段副作用不会因后段语法失败消失。转换先于动态规格展开，最终调用真实类型级格式槽。终端单整对象可保str子类身份，已经物化的输出不因后续空字段回退。内部引擎与公共接线47专项、四应用分别通过，正常完整门禁已验；既有数值规格及高阶对象边界、成本与原参考见[ADR0084](adr/0084-string-format-template.md)，不把builtin format通过项算作模板应用通过。

### MappingProxyType 的只读活代理（有限候选）

`types.MappingProxyType` 保存底层对象，不复制、深冻结或展开嵌套代理。既有 dict 派生映射仍执行自己的
缺键策略；get 不改成 getitem 回退，普通方法每次重新查找并直返原结果。构造只检查载体/类槽，不先调用
用户 len/iter/keys；类级 getitem=None 可构造，实际读取才报不可调用。Enum 的成员代理使用同一类型并缓存，
Mapping/MutableMapping/dict 身份分开，JSON 拒绝 proxy。

公开 iter 保用户返回对象身份，内部游标只适配一次；整数 getitem 回退和 contains 早停不物化整个映射。
主动清理的局部迭代边界先保存同一异常的现场，正确的终止域才吞 IndexError/StopIteration；获取 iter 期间
的 StopIteration 原样传播。精确 dict 反向遍历保活游标，但原 dict 更严格结构版本检查的既有边界不因此宣称全修复。

显式比较槽保底层原结果，比较时右侧 proxy 不提前剥壳；union 对两侧 proxy 各解一层。
全局 Compare/Equal 的 bool ABI 没有改变，非 bool 比较表达式及严格子类型反射优先级不在已支持域。
property/任意 descriptor 迭代与运算槽、内建派生运算、未闭原生映射运算 fallback、GenericAlias 等保持明确边界。
range 是 CPython 合法输入而本切未支持，明确 NI，并保留 CP 正例，不改写为非法类型。

仅新增的 proxy 消费者与精确 dict union 叶子限制基本标量键，用户键在这些路径 NI；
不能据此说 proxy 下标或底层 copy 全部拒绝用户键。keys exact list 保原活引用，其它 keys 先物化后逐键读取。
函数 `**proxy` 有限合并保重复键先于对应 value 读取、非字符串键最终检查，跨实参表达式/展开的旧调度边界仍在；
字面量 `{**proxy}` 的源层 E0100 保持 pending，不通过编译器改写偷扩。

普通 dict 热路保持原结构，代理绑定/游标/keys 物化、新 kwargs 慢路检测和安全键扫描有实际开销，未测性能。
完整输入域、逐阶段实证与未支持项见 [ADR0086](adr/0086-mapping-proxy.md) 及
[逐 API 范围](stdlib-project-tools.md)。四项公共源应用逐项及结构1项已通过，首次完整门禁本地通过；不据此宣称完整 types 或 tomli 通过。

### typing IO 的名义类与根空布局（有限候选）

`typing.IO/BinaryIO/TextIO` 是稳定真实类，不是 `io` 文件类型别名或虚假 typing form。
固定 CP 源中的 28 个函数（含 8 个 property getter） 正文为 pass，合法绑定后返回 None；
不按注解转换参数，也不因 abstractmethod 标记拒绝零参构造。继承的方法保持原函数身份和声明方绑定错误，
BinaryIO/TextIO 各自覆盖的方法为独立对象。名义关系不凭名称或 read/write 鸭子协议猜测。

裸根无实例槽，写入在 property setter 优先之后拒绝；普通无 own slots 派生仍有真实字段。
根创建即禁止分配槽，真正 ApplyDataclass 首写前拒族，故不存在根的合法 PIC 写缓存/快构造计划；
普通注解声明仍可执行，不误当装饰器。未完成类不能从正常构造、object.__new__ 或作为下层基类提前使用。

类定义的模块来源只取最内真实模块执行标记及对应注册 getter 当前 __name__ 值并快照，
不以文件名、cwd、函数可写 module 或外层帧猜测；未知来源只限制需要模块元数据的观察，不影响普通构造和身份 hash。
本族完整 Generic/参数化 IO/完整反射、派生 slots/hooks 等合法域未实现，不返回伪祖先或占位参数。
普通框架已有动态 descriptor、函数内类等边界没有整体修复。with 异常 traceback 参数和 delattr 旧缺口保留 CP 正例 pending。

未给每次 SetSlot/PIC 命中新增分支；慢槽分配、类构造及冷反射仍有有限成本，没有性能测量。
29专项与四项应用分别通过，首次正常完整门禁已通过；详见 [ADR0087](adr/0087-typing-io-runtime.md)，不据此宣称完整 typing 或 tomli 通过。


### typing.NamedTuple 的真实有限类型

公开入口是真实函数，功能式严格绑定 typename/fields 位置参数与字段关键字，按 CP 两遍消费 fields，
不缓存重放一次性迭代器。函数式来源取真实调用者函数 Module，类语法取定义时真实模块 globals.__name__；
二者时点不混用，未知来源不猜 cwd/文件名。可变根与工厂在实际消费点检查，超出当前委托域明确 NI。

类语法保持字段顺序与尾默认值、注解的新字典及与 __new__ 的共享身份，生成对象仍是真实 tuple 布局。
普通方法、staticmethod/classmethod、property 读写删按真实绑定执行，字段只读且没有实例字典；
保存 bound 方法保 receiver/owner/function。失败的类终结或 decorator 不发布新绑定，不回滚已执行 body 副作用。
内部未求值 ForwardRef 保存真实元数据与 eq/hash，不导出虚假完整 ForwardRef；有限 ASCII 语法不执行表达式。

本切还修正全部实际 MakeClass 冷建类路径的初始 module/qualname 与外层首 docstring，包括普通无基类、
用户继承、字符串/异常/IO 派生，不限名为 Runtime 的 HIR 枚举。不改 Static 数值/函数 ABI，EnumCreate 与
TypedDictBegin 另走原协议。IO 定义期 qualname 允许真实覆盖及末期校验，late reserved 修改仍拒绝。
类体 del、自定义 builtins 环境及真正缺名 fallback 未闭，不能从 CP 参考外推源级支持；原 getter 异常原传。

完整 Generic/生成类再派生、魔术槽覆盖、晚类修改、未知可变 descriptor 常量、任意注解比较、复杂前向表达式等
合法域保明确边界与 CP 正例 pending。class module=None 的内部 typing 回退仍 NI，函数.__class__ 是旧反射缺口。
复用原 Special 冷分支封住未完成壳，不在普通 Invoke 加无条件门控；类准备/来源与typed属性入口有实际成本，未测性能。
160项相关runtime与5应用＋1结构首次通过，第二次正常完整门禁1607C#/228CP/202e2e＋30原ignored通过；未发布、未重跑tomli/AOT/性能，详见[ADR0088](adr/0088-typing-namedtuple.md)。

### set.clear（有限候选）

精确set的直接/保存/暂停方法共用零参数绑定，keyword先于arity拒绝；失败不清空，实参副作用不撤销。
成功原地返回None，保集合/背板身份且不调用键hash/eq。新独立set迭代游标修size错误粘性和真正耗尽，
非空clear后同size恢复保CP正例但明确NI；无clear同size快照旧债不扩大。每set新增long字段，迭代仍O(n)
快照并增加常数检查，未测性能。root descriptor/子类及通用**mapping旧边界未开放；47运行时专项与3应用+1结构首次通过，正常完整门禁1647C#/228CP/202e2e+30原ignored通过。

### codecs BOM 常量的有限范围

codecs 当前候选仅提供十四个 BOM 名称及五个不可变 bytes 对象，保 CP3.12.13 的别名身份、
实际运行端序和普通模块重绑定语义。BOM32_LE/BE 是 UTF16 别名，BOM64_LE/BE 才是 UTF32 别名。
未交付的 encode/decode/lookup 等成员沿原 AttributeError/ImportError，不导出假函数。
完整编解码、registry、错误处理器、增量与流接口另切；UTF8 encode/decode/lookup 合法正例保持 pending。仅注册冷路新增对象和映射，
不改变旧编解码热路；首次四项运行时与两项CP/JIT差分通过，正常完整门禁1651C#/228CP/202e2e+30原ignored通过；未新增项目、AOT 或性能结论，详见 ADR0095。

### bytes 正则可变 subject 阶段A候选

仅bytearray及现有连续B/c memoryview的match/search/fullmatch/findall与所得Match活读已通过65专项、3应用及正常完整门禁1666C#/228CP/202e2e+30原ignored。
Match.string保原身份，group按访问时读取当前捕获区间并裁至当前长度，span/endpos保匹配时标记；
未参与组不取buffer。扫描租约不随Match逃逸，view公开释放与无抛最终清理分开。阶段A当时的mutable finditer后由B接线，
replacement/escape在该阶段仍NI，后续F的有限扩展见下文；未验的宽格式/多维view不能称CP非法。保exact bytes扫描，但共有Match多一引用/分支，
每个memoryview新增锁对象与计数、release同步，未测性能。详见ADR0096；该API门禁本身不证明项目通过，固定候选入口结果另列。


### bytes/bytearray ASCII分类候选

八项isalpha/isalnum/isdigit/isspace/isascii/islower/isupper/istitle只按原始ASCII字节规则；高字节对大小写/title属于uncased，不是Unicode字母。空值仅isascii为真。保存bytearray方法活读原对象，不持导出或内容快照。

bound错误先kwargs后arity；有限root先self存在/类型再绑定，不隐式接受另一载体或buffer转换。共享入口新增类型分支、扫描无内容复制；23项专项/三应用及正常完整门禁1680C#/228CP/202e2e+30原ignored已通过，不能宣称零成本或项目通过。完整descriptor框架、派生布局和memoryview方法不在本切。详见ADR0097。


### bytes正则可变subject替换C候选

sub/subn现接bytearray和现有连续B/c view候选，count→模板→subject先序保留。prefix在callback前复制，callback续扫刷新完整窗口、捕获仍活读，尾部按完成时内容复制，扫描lease释放后才最终Join；callback失败也确定释放，不重试或吞异常。无callback一次编码，callback最坏O(N×M)复制，exact bytes原正文不迁移。内部20项、公共384专项/3应用及正常完整门禁1702C#/228CP/202e2e+30原ignored通过。replacement/template/escape在该阶段仍未支持，后续B/F范围见下文及ADR0096。


### bytes正则可变subject迭代B候选

finditer接同一bytearray/连续B/c view域。pos/end协议完成后立即租约，创建不扫描；每next刷新一次完整前缀，内部空匹配retry复用。最后yield仍锁，真正耗尽才释放并清source/Current/lease/pattern；保存的Match独立活读，不锁resize。遗弃沿现lease finalizer，不承诺即时析构。非Stop后端/分配故障保上次成功checkpoint和租约，下一次显式next才再试，是Ryu资源政策而非CP异常cursor等价。没有自动重试或新Python close API。

exact bytes/text执行原路保留，共同mutable分流有判断成本；可变M次产出及耗尽检查最坏O(N×(M+1))全文复制，marks-only不保M份宿主快照。401专项/3公共CP-first应用首次通过，正常完整门禁1719C#/228CP/202e2e+30原ignored通过。没有新的项目、AOT或性能验收；该阶段尚未支持的mutable replacement由后续F有限扩展。


### bytes正则可变replacement阶段F候选

callback返回bytearray或现有连续B/c view时保原引用至全部callback完成，随后按片段顺序获取租约；全部验证后一次输出分配/复制，finally释放。released空view也验证，错误索引包含前缀。bytearray作为非回调replacement时在count转换后读取：含反斜杠才快照并解析，无反斜杠保原对象供晚Join；Match.expand总是准备不可变模板快照。re.escape对bytearray及同域view短租约下两遍扫描、一次输出分配，不调用Python回调。

memoryview作为sub/expand模板仍NI：真实CP的可写hash错误、readonly缓存/释放及无效模板错误不能用统一快照冒充。跨全局空/单字节bytes身份旧差异保CP正例pending。exact bytes原拼接/escape正文不迁移；新冷Join O(parts+输出)及O(parts)引用/租约，escape O(n)，模板有必要快照成本，无性能测量结论。首次regex/memoryview专项429/429通过；首次三应用2/3，模板应用遇旧bytearray slice赋值缺口，原证据逐字pending保留，七处等价测试写法窄迁后单独1/1通过，三unique应用各通过（非首轮3/3），首次正常完整门禁1747C#/228CP/202e2e+30原ignored通过。详见ADR0096。

### slice 公共有限类型候选

slice 构造器与切片语法共享真实类型/载体，保存原字段身份，keyword先于arity报错；
字段只读、不可派生。公共indices按length→step→start→stop执行普通整数协议，全程任意精度，
不改变其它容器旧内部long切片算法。返回bool需warning或复杂index descriptor仍NI。
三字段比较支持精确原生标量和嵌套slice，逐字段身份快判且不预筛短路后项；非同一用户/容器
字段、混合用户富比较明确NI，不能经bool ABI吞掉合法任意对象返回。显式比较非slice返NotImplemented。
hash沿已有字段hash协议顺序执行、不缓存，等值slice可作dict/set键；不承诺CP具体hash值。
完整反射/descriptor重绑/reduce另切。构造复用原载体，比较/hash无临时tuple，indices和绑定对象
仍有分配，共享入口有类型判断成本。21项C#、三unique应用及结构通过，首次正常完整门禁1792C#/228CP/202e2e+30原ignored通过。
hash的tuple/frozenset/native字段和复杂callable槽在实际访问时NI，不预筛后项；见ADR0099。

### delattr 有限公开入口候选

两个位置参数、keyword先于arity再检查str名称，成功None；精确str及现已封闭的str子类名称支持，
不隐式__str__。任意str子类可有hash副作用，不能将现有禁止eq/hash覆写的有限布局外推。
复用普通实例/类属性、普通函数property deleter、已有函数metadata及只读/冻结对象。
公开与显式del共有guard拒绝custom __delattr__、一般数据descriptor、未知fdel形态和模块删除，
不偷偷删槽；原frozen顺序、property优先及非数据native包装遮蔽槽保留。未开放保留元数据NI。
删除路径增加类型链查询、property可能重复查找，不增加get/set热路或对象字段，不宣称无成本。
23项C#、三unique公开应用和结构通过，第二正常完整门禁1815C#/228CP/202e2e+30原ignored通过。
暖态1000次组合测量：共享set+delete为0字节，公开direct/saved各32,000字节；含调用器，
没有分配剖析归因或吞吐结论。动态property命名错误与未绑定builtin fallback原源保pending；见ADR0100。

### 普通函数locals有限候选

普通无cell/free的def/lambda真正内建直接调用，按CP3.12同调用共享dict、逐源码槽刷新、
未绑定删除及额外键保留；外部dict写不回fast locals，值对象身份不变。源码slot元数据排除
隐藏临时量，纯注解名无读写不入表。普通Dynamic局部bare del先检查绑定再清哨兵，
模块/global/nonlocal/cell/静态槽和复杂目标仍拒绝。非法调用不刷新，完整边界见[ADR0101](adr/0101-function-locals.md)。
module/class、generator/comp、间接调用、cell/free与复杂定义形状明确NI；混合star及非最终**
明确E0305，不用错误展开顺序冒充支持。locals字典key回调结构重入明确NI并保已发生副作用；
已有值改写允许，未绑定删除只压制KeyError。含非str额外键时不压缩墓碑，成本不隐瞒。

### bytes/bytearray.translate有限buffer候选

通用256字节映射与原字节删除，None仅作恒等table。精确bytes无实际变化返self，bytearray始终
fresh；表/删除接受精确bytes、bytearray与现连续B/c view，活读别名、released检查和错误次序
见[ADR0102](adr/0102-binary-translate.md)。无Python回调域用同步span/栈map/两遍精确输出，
None+已验证空delete的bytes直接返self。不把CP合法的用户buffer/宽格式/多维误称非法，
不扩真实子类或全局空bytes单例策略。重复**关键字仍有旧公共展开错误，六入口原差异保pending；
本次binder验收不代表完整调用展开副作用/错误合同。

### 精确list基础协议候选

空连续切片用count处理，负步删除按原下标降序；公共index缺项ValueError但内部查找哨兵不变。
extend/+=对精确list/tuple无全量快照，通用source按iter→原源len/hint→流式next/append，
保失败前缀、原异常链及副作用。list游标首次耗尽永久结束；用户游标一次Stop不强制永久终止。
长度提示完整验证但预分配至多4096额外槽，实际消费无截断；巨大合法hint的资源失败时点不与CP
逐分配等价。局部getitem fallback不扩全局迭代，复杂descriptor仍NI；该基础切未开放真实派生，
其后有限布局见下一节ADR0104。
完整范围、旧失败与最终验证见[ADR0103](adr/0103-list-primitives.md)。

### 真实list单继承有限候选

新增真实wrapper持固定PyList存储，默认分配空、init绑定后clear/流式消费；原地返回公开self，
新copy/slice/+/*为精确list，方法覆盖/保存/super与root绕过分开。根str共享object描述符，
比较保元素身份/strict-subclass顺序，foreign !=不误改eq。root sort仅无key回调原生numeric或
BMP无代理文本；非bool排序结果、复杂/宽slice、custom核心magic、copy/deepcopy等明确NI。
详细范围、四个已冻独立反例及最终验证见[ADR0104](adr/0104-list-subclass-layout.md)，
不将新根有限能力外推到所有精确list旧直接方法或全局字符串比较。

### 精确dict更新基础候选

update零或一位置来源、kwargs最后应用。outer pairs逐项消费并即时提交，inner pair按CP完整
物化后检查长度；mapping先keys选择，generic keys物化、exact list keys活读，再逐getitem。
keys getter AttributeError才回退；getitem失败先于键验证。inner TypeError重写保调用者ambient，
其它用户异常身份/链/帧保留。具体提示与资源边界见[ADR0105](adr/0105-dict-update-protocol.md)。
新generic域只scalar键，目标按结构版本重检，合法custom/tuple/bytes键暂NI；旧exact/native
raw行为不冒全hash/reentry修复。此基础不开放dict派生或root描述符，不等于完整dict API。

### 真实 dict 派生有限默认布局（ADR0106）

独立wrapper保身份和不可替换payload；根init更新不clear、root方法绕普通覆盖、只有缺键getitem查类missing，fromkeys实际cls()后插入。新布局只支持精确scalar键，custom核心槽/复杂键域/同尺寸结构变异迭代/结构重入比较/高级视图及复制算法明确NI，详见ADR0106；旧exact raw hash重用问题不因此算完成。消费者逐入口区分根存储与公开方法，JSON/items、Counter空与非空更新、mappingproxy委托不能互相替代。既有显式两参super及字面量dict展开缺口保独立pending，不改CP期望冒成功。

### re.split / Pattern.split 有限通用合同（ADR0107）

两个入口各自按CP3.12绑定，Compile先于maxsplit、ssize_t index一次先于subject消费；负数仍验subject而不扫描。每个匹配插入全部捕获，未参与为None，空匹配后允许同位非空重试；正限额达到即取尾片，不多扫描一次。exact整片保身份，bytearray/连续B/c view输出独立bytes且租约确定释放。动态孤立代理通过chr实证；源字面量的旧ruff替换、非连续view创建和临时父view依赖CP即时回收保持独立pending，不声称已修。旧Compile/flags、高阶index、宽/多维buffer、原生子类等范围及原失败见[ADR0107](adr/0107-regex-split.md)。

### TypeVar 与有限别名参数元数据（ADR0110）

TypeVar 为真原生类型的新身份对象，核心只读、约束原引用、module/extra 可写删。隐式
特殊槽不读取实例 extra；__parameters__ 沿已有 alias 参数树按身份保序去重并惰性缓存。
构造 flags/bound/constraints 次序按 CP3.12.13，来源使用当前帧函数的可写 __module__，
不每次重新读取 globals。未知 bound 比较、动态 helper 和超域 ForwardRef 字符串明确NI。
Generic 派生、alias 替换、完整反射和联合运算仍未实现，合法 global del/operator/exec
等旧缺口保独立 pending。范围、失败及性能限制见[ADR0110](adr/0110-typevar-foundation.md)。

### Generic 真实根与有限别名（ADR0111）

canonical 真类根、空实例布局及 TypeVar-only 专用128项LRU已接候选；args/parameters
为独立稳定tuple，原导出守卫在实际消费相位迁移。旧 cache 命中不抢先读已改绑导出，
IO关系未知分支NI而tuple先命中短路。完整基类/类体终结、用户alias替换与复杂反射另切；
根与alias的module显示规则分开，未知格式化回调消费时NI，不冒__str__等价。范围、
同名class绑定原失败及验证见[ADR0111](adr/0111-generic-root-alias.md)。

### Generic 参数化单基类（ADR0112）

最外Subscript或保存alias的单基类保一次求值、原alias身份与真实Generic父链；仅family
冷状态/metadata缓存，普通new/init/字段布局不变。body后orig覆写、set_name、父hook、
own参数推导按相位处理；未知合法hook/descriptor在真实消费NI。non-data可字段遮蔽，
data在字段前拒绝，class __module__仍直接返原对象。用户类再订阅、IO迁移及一般反射
不开放；原class-body raise/core反射失败完整保留，见[ADR0112](adr/0112-generic-single-base.md)。

### SpecialForm必要缓存与元数据基础（ADR0113）

Optional/Union共用128项原始receiver/parameter LRU，不再先归一化驻留；Optional冷
miss订阅当前Union，暖函数体不消费globals但key hash/eq仍消费自身helper。部分内层
存活可保身份，完全淘汰才新建；native alias不误读typing分类器。非Callable的args
和get_args共享稳定tuple，根类型metadata区分SpecialForm/Literal等，不假称完整Any
类/元类或私有helper协议。合法复杂回调/可写metadata/前向引用保pending，ClassVar
本体未导出。原三参type、孤代理端到端、私有helper读取旧缺口也完整保留，见ADR0113。

### 普通 typing 根原始缓存（ADR0114）

十一普通根共享独立128项 receiver/raw parameter LRU，冷构造不进入归一化 Intern；
暖命中绕过正文 helper，hash/eq仍消费自身依赖。冷失败不占容量，保存根不消费改绑
的公开导出。安全不可变参数树保惰性稳定metadata，未知合法采参/回调NI，不能冒充
TypeError；exact list/dict尚未实际执行unhashable回退。Tuple/Callable、ClassVar和
旧hash/origin限制另切，九完整边界保持pending，见[ADR0114](adr/0114-ordinary-typing-raw-cache.md)。

### typing.Tuple独立原始缓存（ADR0115）

Tuple单独128项raw receiver/parameter LRU，不进入旧Intern；保存根、None转换前键
和scalar/单tuple形状保身份。冷正文无arity/helper预检，空参数不读type-check或
sentinel但仍消费constructor/collector；尾Ellipsis在全部前缀检查后才消费，允许多前缀。
单/中Ellipsis与其它未知合法域完整pending，不冒TypeError；安全metadata复用不扩
checker，完整回调/fallback与ClassVar仍未完成，见[ADR0115](adr/0115-tuple-typing-raw-cache.md)。

### typing.Callable外层与独立原始缓存（ADR0116）

exact二元tuple形状验证和list单次快照位于inner缓存之外，含暖命中；list与同值tuple
可共用键，但scalar/tuple及None转换前形状不抹平。inner独立128项raw LRU，冷正文
先type-check result，再仅type-convert args，之后消费专用constructor/sentinel/collector。
暖命中绕正文但hash/eq仍消费自身helper；真实_CallableGenericAlias及_GenericAlias
直接父关系保身份与类型判断。完整动态helper/派生/ForwardRef/Ellipsis/未知采参保
pending，ClassVar尚未恢复，不以NI或窄源替代完整合法源，见[ADR0116](adr/0116-callable-typing-raw-cache.md)。

### ModuleSpec 有限元数据

真实公开规格与普通已编译源模块绑定见 [ModuleSpec合同](module-spec.md)。
已通过最终正常门禁的范围与保留失败见[ADR0121](adr/0121-module-spec.md)。
内部合成ModuleMetadata初值不使普通静态global整体降级，四元数据槽仍Dynamic；
用户真实读取与动态局部仍保原降级规则，不是一般用户表达式豁免。
本切不包含util执行链；旧失败模块静态槽别名、模块删除与原生/入口反射边界保pending，
未以普通类默认identity equality或可hash行为冒充CP规格对象协议。

### importlib.util.find_spec 有限发现

当前已验有限范围的精确绑定、相对解析、缓存spec原身份和父包初始化顺序见
[发现合同](importlib-util.md)。仅新ABI已定位源码登记可发现；查询不执行、缓存或挂载目标，
父正文已导入目标也不回头偷取缓存spec。未登记名称、native/legacy父与动态finder/path
明确NI，不冒充None或真实非包。自定义name/package协议在回调前拒绝，普通错误保
原异常相位、name/cause/context。loader有限字段比较保身份及同NaN，复杂不同字段
明确NI。实例执行、可写缓存和完整html.parser仍pending；最终验收见ADR0122。


### 显式模块实例基础B1

`--instance-module`只选择图内JIT源码模块，真实唯一dict与ModuleMain(context)共享名称存储，
未选模块静态语义不变；失败重试新旧对象隔离只在已选模式成立。重复执行保未覆盖键，
直接失败不回滚也不移除缓存；普通失败import只移除仍为本次对象的缓存项。
每次执行捕获真实builtins引用，外部重绑定影响下一次，就地变更可在本次观察；非dict
环境提前NI，不冒CP的module环境和延迟错误协议。缺/替换__import__明确失败，未知hook
不偷偷绕过。函数/类/生成器、注解、del、except、环境名字和AOT等边界前诊断。
动态真实异常类型调用保raw args与CLR身份，专有未接构造仍NI。详见[合同](module-instances.md)
及[ADR0123](adr/0123-module-instance-foundation.md)。公开loader、完整模块反射与html.parser
未完成；完整门禁和内部PE通过不等于Markdown项目通过。

### 显式模块实例定义归属B2

选中实例模式的函数保存真实globals与定义时builtins，Env仅保cell，实际self用于函数身份；
移动函数或改__module__不转移owner。参数/local/free先词法决议，global进实例字典。
旧frame保原builtins引用，原地变更可见；后续def重新从当前globals捕获，缺键继承caller
且不插键。genexpr在outer iterable前捕获，暂停生成器/生成器方法保帧中实际owner。

模块级有限普通class保局部真实对象，decorator表达式之后、bases之前guard/capture；
类metadata在bases之后、用户body之前初始化，方法def再次捕获。function.__module__
缺global为None，class.__module__可回退captured builtins。真实环境属性只读。

B2当轮的急切推导式、嵌套class、super/__class__、自定义build_class、metaclass/特殊base等
仍明确拒绝（后续__class__/super限域开放见S0/S1），非dict builtins仍提前NI；不冒CP全域。最终77+64项真实PE及完整门禁通过，
默认性能不利经过一次有界同版本/JIT诊断仍未关闭。见[合同](module-owners.md)、
[ADR0124](adr/0124-module-definition-owners.md)和[全部性能结果](module-owners-performance.md)。
公开loader、可写缓存、AOT、native发现及完整html.parser未因此完成。

### 显式实例类的词法 cell（S0）

实例普通类的裸__class__现在来自每次定义独立分配的真实cell，不是类namespace成员、
模块owner或属性查找TLS。正常终结后、类装饰器前填原类；类体/终结失败可逃逸空cell，
装饰器替换/失败不改变已填原类。旧方法、闭包和生成器跨移植/模块重执行保原cell，
真实globals与定义时builtins仍按B2规则分别处理。详见[ADR0127](adr/0127-instance-lexical-class-cell.md)。

创建__set_name__/父__init_subclass__协议尚未实现，含cell类在冷边界明确NI；这不模拟
CP调用这些hooks的时序、副作用或失败cell状态。B2-owned函数的__closure__读取NI，
写删为readonly，普通__dict__同名值不替代真实Env。S0当轮的super拒绝由下节S1限域推进；
显式__classcell__、nonlocal cell、嵌套class等仍保留边界，不因__class__前置通过就声称原html.parser/Markdown通过。


### 显式实例函数的词法零参super（S1）

普通非生成器函数读取super即请求隐式__class__，即使super被参数/local/global遮蔽；
__class__自身遮蔽仍生效。仅实际callee为canonical super才消费真实free cell及当前首位置
形参，否则回原CallValue0 PIC。先求值callee，再读当前首参；不从type(self)、属性owner、
__module__或TLS推断。无首参、缺/空类cell报RuntimeError，无关普通receiver报TypeError。
MIR验证Env槽与首参来源，CIL仅传site/callee/env/slot/arg0/kind，不提前读cell。

显式参数、star/kwargs、属性alias、native callback/partial、generator/genexpr alias、
class receiver、特殊布局/MRO、代理__class__回退、任意descriptor/复杂反射、普通outer
__class__ freecell、删除参数及None未绑定proxy保留边界。CP允许的partial/None行为在此NI
只表示未实现。代理__new__/__init__仍是正常基类属性，不和super类型自身的反射混淆。
默认直写零参保旧ABI/TLS；一等canonical无frame拒绝，旧非零位置参数NI、关键词优先
TypeError。默认super(*[])等展开为空仍可能走旧TLS，不宣称所有star形式已拒绝。

最新正常门禁、真实PE24、原入口及限定成本观察完成，完整范围见[ADR0128](adr/0128-instance-lexical-super.md)。
此S1验收时公开module_from_spec/exec_module与完整Markdown尚未完成；性能不利未关闭。公开loader的后续首切见下一节。


### 公开编译模块实例执行（首切）

module_from_spec对原生未改ModuleSpec和自有compiled loader创建真正独立模块，保原
spec/loader/path，不提前插builtins、执行或缓存。None loader非包也可创建空目标。
exec_module用loader捕获的代码和目标唯一globals，不按可写名字重新查注册、不选择目标
原默认代码；先检查目标当前name，再检查实例能力和builtins。None名字采用来源名检查但
不改目标元数据；失败保部分写入/原异常，重执行不清字典、不重置spec、不改导入缓存。

源码必须显式instance模式。自定义spec/loader、namespace、静态目标/来源执行、ModuleType
及动态磁盘源、资源clone仍未开放。已保存bound method不等同于后来新属性查找。
完整合同与语义边界见[ADR0129](adr/0129-public-module-from-spec.md)。


### 固定码点宽度后顾首切（ADR0130）

后顾正文限既有字符原子、非捕获组合与定宽重复/分支，不含内部捕获、引用、条件、嵌套
断言、显式anchor/boundary。宽度是Python码点/字节，UTF16可变长度不影响定宽判断。
变宽在受支持语法解析完成后按源码序报re.error，pattern/pos/lineno/colno均None。
单项计数与最终宽度超过int.MaxValue明确NI；正文累计零乘可归零，单项超大计数仍NI。
D009表示差异、复杂后顾、极大数精确错误与Match额外元数据不因此完成。

未知字母escape的text偏移修正至反斜线并局部准备错误；新宽度错误同样准备。有限min>max
在共同token处先于deferred宽度检查；不是所有旧text错误现场/消息的整体修复。
完整合同与保留失败见[ADR0130](adr/0130-fixed-width-lookbehind.md)。


### sys.modules 受限新模块别名（ADR0131）

精确非空无点str新键可挂真实模块原引用；已有缓存/注册/包前缀冲突与State1拒绝。
State0允许，不执行正文或补builtins，不改原name/spec/loader/globals和函数owner。
五注册写路逆向守卫，alias未知子域及缺属性回退冷NI。setter仅位置参数，关键字不写入。
完整替换/删除/None/任意对象、真dict、生命周期和动态spec._initializing副作用仍pending。
posonly精确错误消息仍有binder差异。见[完整合同](module-cache-alias-slice.md)。


### XML Element有限对象（ADR0132）

Element/SubElement保原值、构造attrib浅复制/赋值共享、重复children身份；keys/items为
list快照。append/insert、len、内建整数/读slice、活children及有限前序支持，没有公开
__iter__；环可建但遍历消费时NI，超过10000深度NI。QName仅精确文本，改变模块绑定
写前NI；域外repr/str不回占位字符串。bool真实发警告，旧filename basename差异与
完整警告例继续pending。TreeBuilder/序列化/解析及长尾均未完成，见[完整合同](xml-element-core-slice.md)。

### 精确原生OrderedDict有限首切（ADR0133）

独立native真类型及dict祖先，只开放精确标量键；raw PyDict保插入序，OD链保重排序。
覆盖不移动，re-init不清空，copy独立链共享值，fromkeys保同一default引用。活视图
及正反游标先判无next-key终止；真实结构修改仅首次RuntimeError后永久耗尽，覆盖
和端点no-op move不失效。dict只读根看raw，dict.copy及dict构造/update/**看公开序；
根写入与union先绑定再写前NI，不误称receiver非dict或静默改双存储。

update按mapping逐next/getitem/提交，pairs只N+1解包，不求hint；错误保前缀与原异常，
kwargs在位置源成功后写。OD比较先raw内容再链序，保plain dict反向子类型优先与兄弟
派生左侧方向；eq/ne独立，foreign非bool表达式、结构重入及完整派生/用户键域暂缓。

move先truth再查当前Count；空目标精确标量及四空builtin走已验缺键合同，未知key
在hash/eq/repr前NI，不算CP等价。仅move缺None的KeyError.args为空，未改全局异常。
原13异常cause setter完整失败保pending，独立19 raise-from链与09/病例22的None相位
完整通过，不用新例替换原失败。细节见[完整合同](ordered-dict-core-slice.md)。

普通dict比较/hash/消费者新增OD识别有真实成本，不能以PyDict存储未改宣称零回退；
唯一轻量窗口与全部不利见[性能记录](ordered-dict-performance.md)。正常完整门禁已过，
不等于远端已发布；最终DLL与具体交付边界见合同页。

### 普通动态模块隔离试点（ADR0134）

默认关闭的编译期feature允许一族普通非包非入口源码模块复用已有真实实例环境。全文通过
实例预检、无class、至少一个def且全部def无完整静态签名或含变长参数才选择；未选者保旧ABI。
普通import和同源compiled loader共享唯一dict及定义owner，不增加缓存。该候选不是已发布
默认，不包括AOT/typed优化/显式super恢复。`function.__defaults__`反射仍是完整保留边界。
具体选择、持久合同及不同门禁见[ADR0134](adr/0134-ordinary-dynamic-module-pilot.md)。

### 显式普通实例相对from-import候选

显式普通模块相对导入使用执行目标的真实package/spec，分完整fromlist预处理和逐项绑定两阶段。
旧自动P1不扩域。精确metadata、注册来源、未知finder/path/警告协议的支持与明确NI边界见
[ADR-0136](adr/0136-instance-relative-from-import.md)。目前是候选，完整门禁及真实项目迁移需分别验收。

### 显式普通实例Name/bare异常链候选

源码异常目标按原词法绑定延迟读取，真实globals及定义捕获builtins不被冻结成静态类型。
实例as清理删除真实dict键；局部/cell/free隐式清理保未绑定语义。显式local/cell/free del
仍预检拒绝，generator/类体源码handler未开放，合成with沿旧路径。见[ADR-0137](adr/0137-instance-name-exception-handlers.md)。

### 无资源AOT模块元数据候选

新显式来源注册但位置kind0时，spec/loader/cached初值为None，普通path未绑定、包path为空list；
它们仍是真实可变全局槽，不提供文件资源/finder来源能力。与CPython文件源码模块不同，
无来源finder仍NI；旧ABI不扩域。合同、边界与待执行门禁见[ADR0138](adr/0138-locationless-aot-module-metadata.md)。

### 显式二参super候选（待hosted验收）

普通PyClass与真实相关PyInstance的显式二参构造已接通共享候选入口；不得再忽略参数
或按TLS推断。普通PySuper属性目标必须先于实参捕获，含getter错误和ancestor方法变更。
旧PE IsModule是历史捕获判据alias，不是Python模块判断。源码super可能多分配bound
method并绕原super PIC，未测性能。合法未实现的未绑定/class/native/代理descriptor/MRO
形式保持NI，通用类重绑与复杂star顺序不随本切完成。测试仍pending，当前9/8/3基线
不得预先改写。详见[ADR0140](adr/0140-explicit-super-bound-instances.md)。

### Legacy生成器动态except（ADR0141候选）

HIR仅新增Legacy SourceDynamic的生成器准入，沿既有Name/属性链/无展开tuple目标；
目标求值、as及异常上下文由原动态CFG与生成器状态机组合。六组完整CP合同与普通observer
前置有效；产品编译/JIT/Native尚待hosted验收，暂不登记为已支持。Instance动态生成器、
except*与其它复杂目标继续拒绝，默认模块集合和Native自动Legacy不变。

### 显式 trace V1 产品候选（ADR0142，未验收）

`--trace-product-v1` 仅尝试单源码 Legacy 的封闭普通 next/property/标量域。所有能力检查在发射前完成；未知 callee、外逃、字段/构造器、Instance、源 bare、send/throw/close/yield-from、SystemExit/PEP479 整产物拒绝，不切局部旧实现。默认旧域保持。

异常单参构造若通过参数/返回/别名可能取得None，同样在产品HIR合同拒绝；现有无参/显式None的旧ABI歧义未作全面修复，零参及直接raise已准入类型仍单独保留。

内部 all-miss continuation 与源码显式 raise 分开，Retry 保持同 activation；异常 Frames 在 V1 中只能从唯一 owner 历史派生。原 A/B、原 C/D 及唯一同站点热输入的真实产品验收尚未执行，不能将结构/事务测试或离线门禁自测登记为语言能力。见 [ADR0142](adr/0142-generator-trace-product-v1.md)。
