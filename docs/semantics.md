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
| `from dataclasses import dataclass` + `@dataclass` | ✅ M1.5 | .NET class |
| `@dataclass(frozen=True)` | ✅ M1.5 | .NET struct |
| 链式比较 `a < b < c` | ✅ M1.2 | 中间项无副作用时展开，否则 ❌ |
| `import x` / `import x as y` / `from x import y` / 别名 | ✅ M2.7 | 编译期构建模块图，运行时走 `PyModules` 导入缓存 |
| 普通包 `__init__.py` 与包内相对导入 | ✅ M2.7 | 相对导入在 HIR 换算成绝对模块名 |
| 模块级变量、函数读写模块变量、`global` | ✅ M2.7 | 模块变量是静态槽位，模块对象通过生成的 getter / setter 暴露 |
| `__name__` / `__package__` / `__file__`、`__main__` 入口 | ✅ M2.7 | 模块入口开头写入；入口模块名为 `__main__` |
| `from x import *` | ❌ | 明确报 `E0100`，要求显式列出名字 |
| 函数体内 `import` | ❌ | 明确报 `E0100`，当前只支持模块顶层导入 |
| `nonlocal` | ✅ M3.0 | 名字解析到最近外层函数的共享 cell；非法声明报 `E0103` |
| 列表 / 字典 / 元组 / 集合 / frozenset / bytes / bytearray / slice | ✅ M2.3 | 容器表达式统一降级为 `Dynamic`，由运行时 `PyList` / `PyDict` / `PySet` / `PyFrozenSet` / `PyBytes` / `PyByteArray` / `PySlice` 承载 |
| `x[i]` / `x[i:j:k]` / `del x[i]` / `x[i] = v` | ✅ M2.3 | `RuntimeFn::GetItem` / `SetItem` / `DelItem`；切片对象 `PySlice`，负值索引与越界按 Python 规则处理 |
| `a in b` / `a not in b`、`a is b` / `a is not b` | ✅ M2.3 | `RuntimeFn::Contains` / `IsSame` |
| 普通 / 星号（`a, *b, c = x`）解包赋值、`for x in 容器` | ✅ M2.3 | `RuntimeFn::UnpackCheck` 长度校验；`IterStart` / `IterNextOk` / `IterValue` 迭代协议 |
| `del x` | ✅ M2.3 | `StmtKind::Delete` → `DelItem` |
| 嵌套 `def`、闭包、lambda | ✅ M3.0 | 定义位置创建 `PyFunction`；按 free 列表捕获共享 cell，保持 late binding 与函数逃逸 |
| list / dict / set 推导式（多层 for / if、解包、闭包） | ✅ M3.1 | 独立隐式函数作用域；最左 iterable 在外层取得迭代器 |
| 普通生成器 | 部分 | 语句 yield、if/for/while、return value、共享 cell、next/迭代/耗尽；完整协议待后续 |
| 生成器表达式 | ✅ | 最左 iterable 立即取迭代器，元素 / 过滤 / 内层 iterable 惰性；协议限制同普通生成器 |
| 普通函数装饰器 | ❌ | Phase 3 M3.3 后续竖切 |
| `async` / `await`、完整 asyncio、模式匹配 | ❌ | 暂缓，按项目阻塞评估 |
| `raise exc` / `raise exc from cause` / 裸 `raise` | ✅ M2.2 | `Terminator::Raise` + `PyErrors.PrepareRaise` / `WithCause`；裸 `raise` 取运行时「当前异常」 |
| `assert cond[, msg]` | ✅ M2.2 | 去糖为 `if not cond: raise AssertionError(msg)` |
| `try` / `except` / `except (A, B)` / `else` / `finally` / `except … as e` | ✅ M2.2 | CIL 类型化 catch + `finally` 状态机；详见 §4.1 |
| `str(x)` / `type(x)` / 动态属性读取 | ✅ M2.2 | `PyRepr.StrValue` / `PyObject.TypeOf` / `PyObject.GetAttr`（当前覆盖异常与类型对象） |
| f-string（字面量、`!r` / `!s` / `!a`、格式说明符） | ✅ M2.4 | `PyOps.FormatValue`；嵌套格式说明符暂不支持 |
| 字符串方法 `split` / `join` / `strip` / `replace` / `find` 等 | ✅ M2.4 | `PyOps.CallMethod`；返回值下标按码点计算 |
| 内建 `len` / `range` / `enumerate` / `zip` / `reversed` / `sum` / `min` / `max` / `any` / `all` / `round` / `sorted` / `isinstance` / `id` / `getattr` / `hasattr` | ✅ M2.4 | `PyOps.CallBuiltin`；`sorted` / `list.sort` 支持 `key` 与 `reverse`，排序稳定 |
| `print` 的 `sep` / `end` | ✅ M2.4 | 静态路径直接写分隔符与结尾；`file` / `flush` 依赖 M3.4 |
| 普通 class、单继承、方法、绑定方法、`property` / `staticmethod` / `classmethod` | ✅ M2.5 / M2.6 | 运行时 `PyClass` / `PyInstance`；类体在 `class` 语句位置顺序执行 |
| 用户自定义异常（`class E(ValueError)`） | ✅ M2.6 | codegen 生成 CLR 子类；类型化 catch 与 Python 继承树同构 |
| `__repr__` / `__str__` / `__eq__` / `__hash__` / `__bool__` / `__len__` / `__iter__` / `__next__` / `__getitem__` 与常用运算槽 | ✅ M2.5 | `PyObject.TryMethod` 统一分派；未支持的魔术方法不伪造结果 |
| `with` / 多上下文 / `as` / 异常抑制 | ✅ M2.8 | 去糖为 `try` / `except BaseException` / `finally`；`__enter__` / `__exit__` 走普通属性与调用协议 |
| 常用标准库 | ❌ | Phase 3 逐 API 移植，当前只有 dataclass 特殊降级 |
| 多重继承、元类、任意描述符、动态代码与 import hook | ❌ | 暂缓，明确拒绝 |
| 继承内建容器 / 字符串（`class X(dict)`） | ❌ | 明确报 `E0100`；`class X(object)` 等价于不写基类 |
| `async with` | ❌ | 明确报 `E0100`，不退化成同步上下文 |

## 2. 类型映射

| Python | Phase 1 `Ty` | MIR | CIL |
| --- | --- | --- | --- |
| `int` | `Int` | `I64` | `int64` |
| `float` | `Float` | `F64` | `float64` |
| `bool` | `Bool` | `Bool` | `bool` |
| `str` | `Str` | `Str` | `string` |
| `None` | `None` | `Unit` | `void` |
| `@dataclass` | `Class` | `Class` | `class` |
| `@dataclass(frozen=True)` | `Class` | `Class`（引用类型） | `class`（ADR-0009：保留身份） |
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

动态属性访问、容器、异常捕获已在 M2.2–M2.5 接入，模块系统在 M2.7、`with` 在 M2.8 接入；
多重继承、元类、任意描述符等暂缓能力仍在编译期报诊断，不静默降级成占位值。

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
| 33 | 解包数量不符 | 少了报「not enough」，多了报「too many」 | — | `PyOps.UnpackCheck(length, required, hasStar)`：星号形态只检查下限，非星号形态必须精确相等；M3.1 修复星号目标始终获得新 list（不保留 tuple 切片类型） |
| 34 | 指数 / 移位量上限（RyuPy 偏差） | 任意大的指数会尝试构造巨大整数 | — | `PyInt.MaxBitShift = 1_000_000`：超过时报 `OverflowError`，避免一次运算耗尽内存；`2 ** 100`、`1 << 100` 等常用规模不受影响 |
| 35 | 参数绑定错误 | 运行时 `TypeError`：缺少 / 过多 / 重复绑定 / 未知关键字 / 位置-only 当关键字 | — | `PyBinder` 按 CPython 规则绑定并生成相同措辞的消息；编译期只判断能否走静态快路径，**不**把绑定错误变成编译错误 |
| 36 | 默认值求值时机 | 在 `def` 执行时于模块作用域求值、只求值一次；可变默认值跨调用共享 | — | 默认值在定义位置（`FunctionDefaults` 语句）写进模块静态数组；`def f(x=[])` 的所有调用共享同一个 list |
| 37 | f-string 格式说明符 | 支持嵌套插值（`f"{x:{width}}"`） | — | 当前只支持纯字面量规格；嵌套插值报 `E0100`。格式子集：宽度 / 对齐 / 补零 / 精度 / 进制 / 百分比 |
| 38 | `id(x)` | 返回对象地址（CPython 内部细节） | — | 返回运行时的稳定身份哈希；不伪造地址，登记为 D002 的内部观测差异 |
| 39 | 序列拼接与重复 | `[1] + [2]`、`(1,) + (2,)`、`"ab" * 3`、`3 * [0]`；只允许同类拼接，次数 ≤ 0 得空序列 | — | M2.12：`PySequenceOps` 实现 list / tuple / str / bytes / bytearray 的 `+` / `*`，错误消息按 CPython（`can only concatenate list (not "tuple") to list`、`can't multiply sequence by non-int of type 'float'`） |
| 40 | 增量赋值的原地语义 | `a += b` 对 list 原地 `extend`，别名可见、`a is` 原对象；对 int / str / tuple 生成新对象 | 去糖成 `a = a + b` 会丢失别名 | M2.12：HIR `Binary { inplace }` → 操作码标记位 → `PyOps.InPlaceBinary`；覆盖 list / bytearray / set（`\|= &= -= ^=`）/ `__iadd__` 等，错误消息写 `+=` |
| 41 | 增量赋值目标只求值一次 | `a[f()] += 1`、`g().x += 1` 中 `f` / `g` 只调用一次 | 读、写各降级一次会执行两遍 | M2.12：`aug_assign.rs` 把非名字 / 字面量的子表达式先求值进隐藏局部 |
| 42 | `True` / `False` 与小整数的 `is` | `True` 是单例；[-5, 256] 的 int 被缓存，`x is 0` 为真 | 每次 `box` 都是新对象 | M2.12：bool 装箱复用单例、小整数装箱走缓存；`PyOps.Is` 对 bool 与 [-5, 256] 的 int 按值判定（不依赖装箱来源）。范围外的 int 仍按对象身份，与 CPython 一样不应依赖 |
| 43 | 排序只用 `<` | `sorted` / `list.sort` 只调用 `__lt__`，不调用 `__eq__` | — | M2.12：`PySort` 改为只用「小于」的二分插入 + 归并；此前每次比较先 `==` 再 `<`，会多调用用户 `__eq__` |
| 44 | `%` 字符串格式化（未支持） | `"%d-%s" % (1, "a")` | — | 当前抛 `TypeError: unsupported operand type(s) for %`；登记为待做（TODO 技术债），不静默生成近似结果 |
| 45 | 类体内引用同类定义的函数名（未支持） | `v = property(getv, setv)` 可以读到类体里刚定义的 `getv` | — | 当前报 `E0106` 未定义名字；类体作用域的名字解析待补（TODO 技术债） |

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

- **`except`** 编译成 CIL 的**类型化 catch**。CLR 按「类型及其基类」匹配，正好等于 Python 的
  `except` 语义，前提是 CLR 继承树与 Python 继承树同构（见运行时 `PyExceptionTypes`）。
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
也不再分两种 list。代价是放弃静态特化，收益是不必为每种容器组合发明新的静态类型——Python
自己也没有。

- **相等性与哈希**集中在 `PyHash`：`Equal` 负责 `==`，`Of` 负责 `hash`。字典与集合通过
  `PyKeyComparer` 把两者映射成 CLR 的 `IEqualityComparer<object>`。
- **`==` 与键查找的身份语义不同**（有意）：`nan == nan` 必须为 `False`，但同一个 `nan`
  对象作为字典键时必须能查到自己。前者对应 CPython 的 `PyObject_RichCompare`，后者对应带
  指针快判的 `PyObject_RichCompareBool`；因此 `Equal` 不含身份兜底而 `PyKeyComparer` 含，
  两边不能合并。
- **int / float 混合比较必须精确**：`9007199254740993 == 9007199254740992.0` 在 CPython 是
  `False`，而把整数先转成 `double` 再比较会静默得到 `True`。统一走 `PyInt.CompareWithDouble`。
- **数字键三元组**：`True`、`1`、`1.0` 互为相同键且哈希一致（即 `hash(True) == hash(1)`）。
- **序列比较**只在同族内成立：tuple / list / bytes 系列各自做字典序比较，跨族（如
  `list < tuple`）抛 `TypeError`；元素不可排序时诊断里写的是**元素**类型而非容器类型。
- **字典布局**：`PyDict` 用「键 → 槽位下标」的索引加平行的键 / 值数组，删除留墓碑，墓碑过多时
  压缩；删除是 O(1)，迭代按槽位顺序即插入顺序。`Version` 只在增删 / 清空时递增，**覆盖已有键
  不递增**（CPython 允许迭代中给已有键赋新值）。
- **迭代期间修改**：字典迭代器记下开始时的 `Version`，增删后再取下一个元素抛 `RuntimeError`；
  但 `dict.keys()` 视图是「活的」，新建一次遍历看到的是当时内容，不校验版本。集合迭代使用确定性
  快照（CPython 的集合迭代期修改是未定义行为）。
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
- **绑定方法**：`PyBoundMethod` 记录定义类与接收者；零参数 `super()` 通过线程局部的方法
  上下文取「定义类的下一个基类」，因此多级继承链上的 `super().__init__` 语义正确。
- **缓存失效**：类属性查找带缓存，条目记录全局修改版本。任何类属性增删 / 替换都会让缓存
  失效（陷阱 #22）；条目超过 256 条整体清空，容量有界。
- **魔术方法**：`__repr__` / `__str__` / `__eq__` / `__hash__` / `__bool__` / `__len__` /
  `__iter__` / `__next__` / `__getitem__` / `__setitem__` / `__delitem__` / `__contains__`
  与常用一元 / 二元运算槽接入统一入口。默认对象身份仍在：没有 `__eq__` 时按引用相等，
  没有 `__hash__` 时按身份哈希。
- **身份**：普通对象始终是引用；`frozen=True` dataclass 也改为 .NET 引用类型，装箱 / 传参
  只复制引用，满足 ADR-0009 的默认身份要求。
- **暂缓能力**：多重继承、元类、任意类装饰器、自定义 `__getattribute__` 在 HIR 报 `E0100`，
  不静默降级。

### 4.6 闭包作用域（M3.0）

赋值与参数创建当前函数绑定；被子函数引用的绑定变为 cell。`nonlocal` 跳过只转发环境的
中间函数，指向最近的外层函数绑定。`global` 指向模块，并截断子函数对该名字的向外查找。
模块变量不创建 cell。分支和循环里的 def 保留原来的执行位置，多个工厂调用各有独立 cell。

声明与参数冲突、同时 global / nonlocal、先使用或赋值后声明、没有外层绑定以及模块级
nonlocal 都报 `E0103`。嵌套 async def 与带装饰器的嵌套 def 报 `E0100`，按后续里程碑推进。
类体 lambda、函数内 class、`del` 局部名和 cell 的反射接口仍未支持，不能据闭包用例推定可用。
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
生成器表达式内零参 super 暂拒绝，send/throw/close 等限制同普通生成器。
设计与分配代价见 [ADR-0016](adr/0016-comprehension-scopes.md)，不宣称性能提升。

此外，用户 `__iter__` 的返回值必须已具备迭代器协议，不再递归对其调用 `__iter__`；返回 list
等仅可迭代对象立即抛 `TypeError`。入口 Main 的真实 PE 方法头启用局部清零，保证成功退出 0，
不依赖平台上未初始化栈内存的偶然内容。

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

### 普通生成器首切（M3.2）

调用生成器函数只绑定参数并创建状态，不执行函数体；即使 yield 不可达也返回生成器。
每次 next 恢复局部、临时量、循环迭代器和共享 cell 引用，yield None 与正常耗尽分离。
完成或异常后永久耗尽并释放保存引用；重入抛 ValueError，意外逃逸的 StopIteration 转 RuntimeError 并保留原因。
每次恢复独立登记 / 清理 traceback，暂停不会把调用栈留在线程状态里。
当前支持语句 yield、普通控制流和 return value；生成器内零参 super、try/with、表达式型 yield、yield from 报 E0100。
完成值由触发首次结束的 next 以 StopIteration.value 暴露，args 保留非 None 终值作为单个原对象；
异常构造不提前调用终值 __str__，仅实际格式化时读取最新内容。
再次耗尽为 None / 空 args。for、list、next(default) 消费结束时不缓存终值，之后 next 不会再次领取。
此处不表示一般 StopIteration(...) 构造已完整支持；通用异常构造 ABI 的无参 / 显式 None / 多参保真仍待补齐。
send/throw/close 入口目前显式抛 NotImplementedError，不宣称完整生成器协议或清理语义。
状态机首版保存全部局部，存在装箱与数组读写代价，尚无性能收益测量，见 ADR-0017。
