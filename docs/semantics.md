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
| 41 | 增量赋值目标只求值一次 | `a[f()] += 1`、`g().x += 1` 中 `f` / `g` 只调用一次 | 读、写各降级一次会执行两遍 | M2.12：`aug_assign.rs` 把除不可变字面量外的子表达式（含名字）先求值进隐藏局部，冻结原对象/索引并跨暂停保存 |
| 42 | `True` / `False` 与小整数的 `is` | `True` 是单例；[-5, 256] 的 int 被缓存，`x is 0` 为真 | 每次 `box` 都是新对象 | M2.12：bool 装箱复用单例、小整数装箱走缓存；`PyOps.Is` 对 bool 与 [-5, 256] 的 int 按值判定（不依赖装箱来源）。范围外的 int 仍按对象身份，与 CPython 一样不应依赖 |
| 43 | 排序只用 `<` | `sorted` / `list.sort` 只调用 `__lt__`，不调用 `__eq__` | — | M2.12：`PySort` 改为只用「小于」的二分插入 + 归并；此前每次比较先 `==` 再 `<`，会多调用用户 `__eq__` |
| 44 | `%` 字符串格式化（有限子集） | `"%d-%s" % (1, "a")`、`"%(name)s" % mapping` | — | M3.7 支持无修饰 `%s/%r/%a/%d/%i/%u/%%` 与映射键，逐项求值；flags/宽度/precision/其它转换及 bytes 仍未支持，详见 [string-percent.md](string-percent.md) |
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
  `except` 语义，CLR 主继承树之外的已登记额外父边（目前仅 io.UnsupportedOperation → ValueError）由 MIR 在原 handler 位置展开独立 catch，见 io-unsupported-operation.md。用户异常内部按所属模块限定身份，不以显示名覆盖其它模块或内建异常。
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
内建方法绑定固定接收者和类型静态入口，参数恢复后先校验支持的arity再执行副作用；普通无暂停PIC路径不变。
旧静态dataclass暂停构造专用ABI仍报E0305；M3.6默认动态dataclass走普通类绑定/调用。int/float/str/type/异常及bool/bytes/bytearray的暂停构造
目前仅接受单位置参数，super暂停调用仍拒绝，未宣称完整内建构造签名支持。

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
sys.modules/path 为受控只读视图，不支持动态 import hook；环境与模块 keys/items 是快照。
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
模块异常导出只读，实际用于typed handler的稳定import/from别名禁止跨模块改写；
本地同名源码及用户同名异常不会误认。完整动态except表达式未完成。
repr补不可打印控制/分隔符与孤立代理项转义，不把日志中的控制字符当作可打印文本直接写出。

### 容器工具、函数工具与类型元数据（M3.6）

逐 API 范围见 [容器工具](stdlib-collections.md)、[函数工具](stdlib-functional.md)、
[数据类与元数据](stdlib-metadata.md)，设计及性能见 ADR-0030。
原生 dict 子类包装参与 ** 展开、JSON 与复制；可变容器不能作为 hash 键。
缓存保存强引用并按容量淘汰，partial 与方法描述符严格区分；contextmanager 的装饰器路径
同样保存异常处理上下文。显式 cause 不复制原异常，from None 仅抑制报告链而保留 context。
默认数据类统一保留 Python 类与实例身份，属性修改或 init 替换使构造计划重新检查/回退；
不会为保留旧静态速度而忽略生成 eq、factory、property 或 frozen 错误。
类型元数据不等于运行时类型检查；Protocol、NamedTuple、TypeVar 与完整前向引用仍未实现。
Hashable 的隐式类 hash 槽边界保留独立 pending，不用启发式关系冒充 ABC 正确性。

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
AOT 仍要求未调用函数的静态依赖源码可编译。动态导入、星号导入和函数内异常别名
catch 仍未开放，完整边界见 [function-imports.md](function-imports.md)。
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

本地候选只补真实canonical object及无字典基础实例、有限单继承根关系；不改变普通类
现有构造入口（尚未分派用户__new__）。显式object.__new__(用户类)仍明确不支持。全局比较ABI的bool返回
及!=取反旧债保留完整CP正期待为ignored，不因sentinel基础比较通过宣称任意富比较完成。
详见[ADR-0049](adr/0049-base-object-identity.md)，验收状态以该文档实际记录为准。
