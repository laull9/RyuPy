# 语言子集与语义规范

> 本文定义 RyuPy **当前支持**的 Python 子集，以及它与 Python 语义的对应方式。
> 原则：**要么与 CPython 行为一致，要么在编译期报错。** 绝不允许「能编译，但结果不一样」。

状态图例：✅ 已实现 · 🚧 进行中 · 📋 计划中（括号内为里程碑） · ❌ Phase 1 不支持（编译期报错）

## 1. Phase 1 语法支持表

| 构造 | 状态 | 降级方式 |
| --- | --- | --- |
| 整数 / 浮点 / 布尔 / 字符串 / `None` 字面量 | ✅ M1.0 / M1.4 | `Literal` |
| `print(a, b, ...)` | ✅ M1.0 | 按类型逐个 `Write*` + `WriteSep` + `WriteNewline` |
| 带完整类型标注的 `def` | ✅ M1.1 | CIL 静态方法 |
| 局部变量（不带标注 / `x: int = …`） | ✅ M1.1 | CIL local，类型由推导决定 |
| `x += 1` 等增量赋值 | ✅ M1.1 | 去糖为 `x = x + 1` |
| 函数调用（含前向引用、递归） | ✅ M1.1 | `call` |
| 算术 / 位运算 / 比较 / `not` | ✅ M1.2 | 见第 3 节 |
| `and` / `or`（短路） | ✅ M1.2 | 基本块分支 |
| `if` / `elif` / `else`、条件表达式 | ✅ M1.2 | 基本块分支 |
| `while`、`break`、`continue` | ✅ M1.2 | 基本块 + 循环目标栈 |
| `for x in range(...)` | ✅ M1.3 | 原生计数循环，不产生迭代器 |
| `int(x)` / `float(x)` / `abs(x)` | ✅ M1.4 | `Cast` / 运行时函数 |
| `str + str` | ✅ M1.4 | `Builtins.StrConcat` |
| `from dataclasses import dataclass` + `@dataclass` | ✅ M1.5 | .NET class |
| `@dataclass(frozen=True)` | ✅ M1.5 | .NET struct |
| 链式比较 `a < b < c` | ✅ M1.2 | 中间项无副作用时展开，否则 ❌ |
| `while … else` / `for … else` | ❌ | — |
| 其它 `import` | ❌ | — |
| `global` / `nonlocal` | ❌ | Phase 3 |
| 列表 / 字典 / 元组 / 集合 | ❌ | Phase 2 |
| 闭包、lambda、生成器、`async` | ❌ | Phase 3 |
| 异常 `try` / `raise` | ❌ | Phase 5 |
| 普通 class、继承、方法 | ❌ | Phase 2+ |

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
| 4 | 整数溢出 | 任意精度，永不溢出 | `add` 静默回绕 | Phase 1 `add.ovf` 抛异常；Phase 5 升级 BigInteger（ADR-0004） |
| 5 | 浮点打印 | `1.0`、`1e+16`、`1e-05` | `1`、`1E+16`、`1E-05` | `PyFormat.FloatRepr` |
| 6 | 布尔打印 | `True` | `True` | 一致，但不要依赖 `bool.ToString()` 的区域设置 |
| 7 | `print` 换行 | 总是 `\n` | `WriteLine` 在 Windows 是 `\r\n` | 统一写 `'\n'` |
| 8 | `range` 负步长 | `range(10, 0, -3)` → 10 7 4 1 | — | 计数循环的比较方向随步长符号变化 |
| 9 | `range` 步长为 0 | `ValueError` | 死循环 | 字面量 0 编译期报错，变量运行时检查 |
| 10 | 先读后写的局部变量 | `UnboundLocalError` | CIL local 默认为 0 | 编译期做「确定赋值」分析报错 |
| 11 | 深递归 | `RecursionError`（默认 1000 层） | 栈溢出直接崩溃进程 | Phase 1 接受差异，记录在案 |
| 12 | 除以零 | `ZeroDivisionError` | 整数抛 `DivideByZeroException`；浮点得 `inf` | 浮点除法需显式检查（Phase 5 映射异常类型） |
| 13 | `int(nan)` / `int(inf)` | `ValueError` / `OverflowError` | `conv.i8` 结果未定义 | 运行时函数检查 |
| 14 | 字符串长度 / 下标 | 按码点 | 按 UTF-16 单元 | Phase 1 不支持 `len`/下标；Phase 2 处理 |
| 15 | 浮点 NaN 比较 | `nan < 1` 为 `False`，`nan != nan` 为 `True` | `clt` 正确；`!(a < b)` 式的取反实现会出错 | 比较指令不得用「取反」实现 `>=` 等 |
| 16 | `-0.0` | 打印 `-0.0` | 打印 `-0` | `FloatRepr` 处理 |
| 17 | `frozen=True` dataclass 语义 | Python 中所有对象变量均为引用语义 | .NET `valuetype` 传参/赋值是值拷贝，无 GC 堆开销但无引用别名 | 编译期禁止修改 `frozen=True` 属性（报 E0203）；对纯不可变数据两者在外部观测等价；记录于 ADR-0008 |

## 5. 模块与入口

- 顶层语句按顺序组成隐式 `main`，编译为 CIL `Main(string[])`。
- 顶层定义的变量是 `main` 的局部变量。Phase 1 中**函数体不能读取模块级变量**（只能引用顶层函数和类），否则报错。
- `from dataclasses import dataclass` 是 Phase 1 唯一允许的 import，由 HIR 特殊识别，不做真实导入。
