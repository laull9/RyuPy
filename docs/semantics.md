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
| 参数默认值（定义时求值、可变默认值共享） | ✅ M2.1 | 在 `def` 所在位置求值到模块静态数组；`FunctionDefaults` 语句保证引用此前的模块变量 |
| 一等函数（函数对象、作为实参 / 返回值、经变量调用） | ✅ M2.1 | 每个函数生成统一 thunk 与模块静态 `PyFunction`；`CallValue` 分派 |
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
| 其它 `import`、模块与包 | ❌ | Phase 2 M2.7 |
| `global` / `nonlocal` | ❌ | 分别为 M2.7 / M3.0 |
| 列表 / 字典 / 元组 / 集合 / frozenset / bytes / bytearray / slice | ✅ M2.3 | 容器表达式统一降级为 `Dynamic`，由运行时 `PyList` / `PyDict` / `PySet` / `PyFrozenSet` / `PyBytes` / `PyByteArray` / `PySlice` 承载 |
| `x[i]` / `x[i:j:k]` / `del x[i]` / `x[i] = v` | ✅ M2.3 | `RuntimeFn::GetItem` / `SetItem` / `DelItem`；切片对象 `PySlice`，负值索引与越界按 Python 规则处理 |
| `a in b` / `a not in b`、`a is b` / `a is not b` | ✅ M2.3 | `RuntimeFn::Contains` / `IsSame` |
| 普通 / 星号（`a, *b, c = x`）解包赋值、`for x in 容器` | ✅ M2.3 | `RuntimeFn::UnpackCheck` 长度校验；`IterStart` / `IterNextOk` / `IterValue` 迭代协议 |
| `del x` | ✅ M2.3 | `StmtKind::Delete` → `DelItem` |
| 闭包、lambda、生成器、推导式、装饰器 | ❌ | Phase 3 M3.0–M3.3 |
| `async` / `await`、完整 asyncio、模式匹配 | ❌ | 暂缓，按项目阻塞评估 |
| `raise exc` / `raise exc from cause` / 裸 `raise` | ✅ M2.2 | `Terminator::Raise` + `PyErrors.PrepareRaise` / `WithCause`；裸 `raise` 取运行时「当前异常」 |
| `assert cond[, msg]` | ✅ M2.2 | 去糖为 `if not cond: raise AssertionError(msg)` |
| `try` / `except` / `except (A, B)` / `else` / `finally` / `except … as e` | ✅ M2.2 | CIL 类型化 catch + `finally` 状态机；详见 §4.1 |
| `str(x)` / `type(x)` / 动态属性读取 | ✅ M2.2 | `PyRepr.StrValue` / `PyObject.TypeOf` / `PyObject.GetAttr`（当前覆盖异常与类型对象） |
| f-string（字面量、`!r` / `!s` / `!a`、格式说明符） | ✅ M2.4 | `PyOps.FormatValue`；嵌套格式说明符暂不支持 |
| 字符串方法 `split` / `join` / `strip` / `replace` / `find` 等 | ✅ M2.4 | `PyOps.CallMethod`；返回值下标按码点计算 |
| 内建 `len` / `range` / `enumerate` / `zip` / `reversed` / `sum` / `min` / `max` / `any` / `all` / `round` / `sorted` / `isinstance` / `id` / `getattr` / `hasattr` | ✅ M2.4 | `PyOps.CallBuiltin`；`sorted` / `list.sort` 支持 `key` 与 `reverse`，排序稳定 |
| `print` 的 `sep` / `end` | ✅ M2.4 | 静态路径直接写分隔符与结尾；`file` / `flush` 依赖 M3.4 |
| 普通 class、单继承、方法 | ❌ | Phase 2 M2.5 / M2.6 |
| `with` 与上下文协议 | ❌ | Phase 2 M2.8 |
| 常用标准库 | ❌ | Phase 3 逐 API 移植，当前只有 dataclass 特殊降级 |
| 多重继承、元类、任意描述符、动态代码与 import hook | ❌ | 暂缓，明确拒绝 |

## 2. 类型映射

| Python | Phase 1 `Ty` | MIR | CIL |
| --- | --- | --- | --- |
| `int` | `Int` | `I64` | `int64` |
| `float` | `Float` | `F64` | `float64` |
| `bool` | `Bool` | `Bool` | `bool` |
| `str` | `Str` | `Str` | `string` |
| `None` | `None` | `Unit` | `void` |
| `@dataclass` | `Class` | `Class` | `class` |
| `@dataclass(frozen=True)` | `Class` | `Class`（值类型） | `valuetype` |
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

仍未支持的动态操作：属性访问（需要普通 class，M2.5）、下标与容器（M2.3）、异常捕获（M2.2）、
`import`（M2.7）——这些在编译期报 `E0303` 等诊断，不静默降级成占位值。

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
| 17 | `frozen=True` dataclass 语义 | 对象仍有稳定身份，字段还能指向可变对象 | .NET valuetype 传参 / 赋值为值拷贝 | 当前禁止字段写入，但 frozen 不能证明身份不可观察；M2.5 按 ADR-0009 保留默认身份，值表示须证明或显式选择 |
| 18 | 回收与析构时机 | CPython 多数对象按 refcount 回收；Python 语言不保证即时析构 | .NET GC 延迟回收 | 明确不复现 refcount / 析构 / 弱引用回调的具体时机，见 D001；资源用 with / finally / close |
| 19 | 类型标注 | Python 不据函数 / 变量标注强制运行时类型 | 静态签名会限制实参类型 | M2.1 起改为「标注只作优化线索」：实参不符时被调函数降级为动态，不再报编译错误 |
| 20 | 字典键与顺序 | 相等的数字键保持一致哈希，dict 保持插入顺序 | .NET 集合默认行为不能直接等同 Python | M2.3 统一 Python equality / hash 与顺序，覆盖 bool / int / float 混合键 |
| 21 | 模块初始化 | 按首次导入执行，循环中可见部分初始化对象 | .NET 静态构造时机不等同 Python import | M2.7 独立模块对象、缓存与初始化入口，不依赖静态构造自动复现 |
| 22 | 方法与属性重绑定 | 后续查找须看到新绑定 | 缓存会保留旧目标 | M2.5–M2.9 携带类型 / shape / 模块版本，修改后失效并返回通用分派 |
| 23 | 生成器清理 | close / throw 与 finally / with 联动 | 简单 IEnumerator 不覆盖全部协议 | M3.2 保存异常和清理状态；析构触发关闭的时机仍受 D001 限制 |
| 24 | 整数与浮点相等 | `9007199254740993 == 9007199254740992.0` 为 `False` | 先转 `double` 后两者同值，得到 `True` | `PyHash` 的混合数值比较一律走 `PyInt.CompareWithDouble` 精确比较 |
| 25 | NaN 作为字典键 | `nan == nan` 为 `False`，但同一个 nan 对象能在字典里查到自己 | — | `PyHash.Equal` 不含身份兜底，`PyKeyComparer` 含（见 §4.3） |
| 26 | 实参求值顺序 | 全部实参先求值，再进入被调者 | 逐个下发 `Write*` 会边求值边输出 | M2.3 起先生成所有实参的临时量，再统一输出（见 §4.4） |
| 27 | 序列比较 | 只在同族内字典序，跨族抛 `TypeError` | — | tuple / list / bytes 系列分族；错误写的是元素类型而非容器类型（见 §4.3） |
| 28 | 静态 i64 上限（RyuPy 偏差） | 任意精度 | — | 静态特化路径的 `+ - * // % abs ** <<` 结果超出 i64 时抛 `OverflowError`，消息注明「RyuPy static integer limit」。动态路径无此限制（D003） |
| 29 | `int ** int` 的结果类型 | 指数为负时返回 `float`（`2 ** -1 == 0.5`） | — | 指数是确定的非负字面量时走静态快速幂；否则整表达式降级为动态，由 `PyInt.Power` 按真实指数返回 int / float |
| 30 | `for` 循环变量与 `range` 参数 | 循环变量可被循环体重写而不影响迭代；`range(...)` 参数只求值一次 | 用循环变量当计数器会破坏迭代 | 静态计数循环使用隐藏计数器：每轮开头才写用户变量，边界复制进局部变量 |
| 31 | 排序稳定性 | `sorted` / `list.sort` 稳定；`reverse=True` 仍保持相等元素的原始顺序 | `List.Sort` 不稳定，且「排完再反转」会颠倒相等元素 | `PySort` 自底向上归并排序；降序通过反转比较实现 |
| 32 | 空容器真值 | `[]`、`{}`、`""`、`set()` 为假 | 引用类型默认非空 | `PyOps.Truthy` 显式列出每种容器类型（含 dict 视图、bytes / bytearray） |
| 33 | 解包数量不符 | 少了报「not enough」，多了报「too many」 | — | `PyOps.UnpackCheck(length, required, hasStar)`：星号形态只检查下限，非星号形态必须精确相等 |
| 34 | 指数 / 移位量上限（RyuPy 偏差） | 任意大的指数会尝试构造巨大整数 | — | `PyInt.MaxBitShift = 1_000_000`：超过时报 `OverflowError`，避免一次运算耗尽内存；`2 ** 100`、`1 << 100` 等常用规模不受影响 |
| 35 | 参数绑定错误 | 运行时 `TypeError`：缺少 / 过多 / 重复绑定 / 未知关键字 / 位置-only 当关键字 | — | `PyBinder` 按 CPython 规则绑定并生成相同措辞的消息；编译期只判断能否走静态快路径，**不**把绑定错误变成编译错误 |
| 36 | 默认值求值时机 | 在 `def` 执行时于模块作用域求值、只求值一次；可变默认值跨调用共享 | — | 默认值在定义位置（`FunctionDefaults` 语句）写进模块静态数组；`def f(x=[])` 的所有调用共享同一个 list |
| 37 | f-string 格式说明符 | 支持嵌套插值（`f"{x:{width}}"`） | — | 当前只支持纯字面量规格；嵌套插值报 `E0100`。格式子集：宽度 / 对齐 / 补零 / 精度 / 进制 / 百分比 |
| 38 | `id(x)` | 返回对象地址（CPython 内部细节） | — | 返回运行时的稳定身份哈希；不伪造地址，登记为 D002 的内部观测差异 |

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

**当前限制**：`except` 的类型只支持内建异常类型名（用户自定义异常在 M2.6）；traceback 的
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

## 5. 模块与入口

- 顶层语句按顺序组成隐式 `main`，编译为 CIL `Main(string[])`。
- 顶层定义的变量是 `main` 的局部变量。Phase 1 中**函数体不能读取模块级变量**（只能引用顶层函数和类），否则报错。
- `from dataclasses import dataclass` 是 Phase 1 唯一允许的 import，由 HIR 特殊识别，不做真实导入。

Phase 2 M2.7 计划将模块变量改为模块命名空间成员，支持函数读取与 global 写入；各模块按首次 import 初始化，重复导入复用同一对象。项目源码和运行时模块通过同一注册边界解析，普通包与相对导入纳入首批范围。动态名称只加载构建时已登记模块，自定义导入器暂缓。失败与循环导入规则见 architecture.md §7。

## 6. 兼容范围与性能实现

Phase 2 优先动态核心、容器、普通类、异常与模块；Phase 3 增加闭包、生成器、推导式、装饰器和标准库。完整协程生态、元类、任意描述符、多重继承和解释器内部观测暂缓，Phase 5 汇总清单，不承诺最终全部补齐。

保留求值顺序、异常传播、可变别名、普通对象身份和容器行为的优化可默认采用。低影响偏差经 ADR 接受并公开登记后，也可选高性能默认实现。固定宽度整数、可观察的值拷贝、浮点重排等影响常用结果的扩展由用户显式选择；这些扩展目前尚未实现。refcount 与 GC 时机属于已接受架构边界，不代表其它差异也可省略。

原生、桥接和带偏差结果分别登记。当前通过的九个 Phase 1 e2e 用例只能证明其覆盖范围，不能证明常见项目或整个标准库已经可用。
