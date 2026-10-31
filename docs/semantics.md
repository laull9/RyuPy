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
| 其它 `import`、模块与包 | ❌ | Phase 2 M2.7 |
| `global` / `nonlocal` | ❌ | 分别为 M2.7 / M3.0 |
| 列表 / 字典 / 元组 / 集合 | ❌ | Phase 2 |
| 闭包、lambda、生成器、推导式、装饰器 | ❌ | Phase 3 M3.0–M3.3 |
| `async` / `await`、完整 asyncio、模式匹配 | ❌ | 暂缓，按项目阻塞评估 |
| 异常 `try` / `raise` / `finally` | ❌ | Phase 2 M2.2 |
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

Phase 2 计划增加 Dynamic 与 PyValue，普通 Python `int` 保留 i64 快路径并支持 BigInteger 升级，普通对象保留身份与共享引用。frozen dataclass 的默认对象身份策略按 ADR-0009 修订；上表只描述当前 Phase 1 实现。

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
| 4 | 整数溢出 | 任意精度，永不溢出 | `add` 静默回绕 | Phase 1 `add.ovf` 抛异常；M2.1 升级 BigInteger，排期由 ADR-0009 修订 |
| 5 | 浮点打印 | `1.0`、`1e+16`、`1e-05` | `1`、`1E+16`、`1E-05` | `PyFormat.FloatRepr` |
| 6 | 布尔打印 | `True` | `True` | 一致，但不要依赖 `bool.ToString()` 的区域设置 |
| 7 | `print` 换行 | 总是 `\n` | `WriteLine` 在 Windows 是 `\r\n` | 统一写 `'\n'` |
| 8 | `range` 负步长 | `range(10, 0, -3)` → 10 7 4 1 | — | 计数循环的比较方向随步长符号变化 |
| 9 | `range` 步长为 0 | `ValueError` | 死循环 | 字面量 0 编译期报错，变量运行时检查 |
| 10 | 先读后写的局部变量 | `UnboundLocalError` | CIL local 默认为 0 | 编译期做「确定赋值」分析报错 |
| 11 | 深递归 | `RecursionError`（默认 1000 层） | 栈溢出直接崩溃进程 | Phase 1 接受差异，记录在案 |
| 12 | 除以零 | `ZeroDivisionError` | 整数抛 `DivideByZeroException`；浮点得 `inf` / NaN | 当前差异；M2.2 补浮点检查并统一 Python 异常类型 |
| 13 | `int(nan)` / `int(inf)` | `ValueError` / `OverflowError` | `conv.i8` 结果未定义 | 运行时函数检查 |
| 14 | 字符串长度 / 下标 | 按码点 | 按 UTF-16 单元 | Phase 1 不支持 `len`/下标；Phase 2 处理 |
| 15 | 浮点 NaN 比较 | `nan < 1` 为 `False`，`nan != nan` 为 `True` | `clt` 正确；`!(a < b)` 式的取反实现会出错 | 比较指令不得用「取反」实现 `>=` 等 |
| 16 | `-0.0` | 打印 `-0.0` | 打印 `-0` | `FloatRepr` 处理 |
| 17 | `frozen=True` dataclass 语义 | 对象仍有稳定身份，字段还能指向可变对象 | .NET valuetype 传参 / 赋值为值拷贝 | 当前禁止字段写入，但 frozen 不能证明身份不可观察；M2.5 按 ADR-0009 保留默认身份，值表示须证明或显式选择 |
| 18 | 回收与析构时机 | CPython 多数对象按 refcount 回收；Python 语言不保证即时析构 | .NET GC 延迟回收 | 明确不复现 refcount / 析构 / 弱引用回调的具体时机，见 D001；资源用 with / finally / close |
| 19 | 类型标注 | Python 不据函数 / 变量标注强制运行时类型 | 静态签名会限制实参类型 | Phase 1 仍有限制；M2.1 默认兼容路径采用通用调用与守卫回退 |
| 20 | 字典键与顺序 | 相等的数字键保持一致哈希，dict 保持插入顺序 | .NET 集合默认行为不能直接等同 Python | M2.3 统一 Python equality / hash 与顺序，覆盖 bool / int / float 混合键 |
| 21 | 模块初始化 | 按首次导入执行，循环中可见部分初始化对象 | .NET 静态构造时机不等同 Python import | M2.7 独立模块对象、缓存与初始化入口，不依赖静态构造自动复现 |
| 22 | 方法与属性重绑定 | 后续查找须看到新绑定 | 缓存会保留旧目标 | M2.5–M2.9 携带类型 / shape / 模块版本，修改后失效并返回通用分派 |
| 23 | 生成器清理 | close / throw 与 finally / with 联动 | 简单 IEnumerator 不覆盖全部协议 | M3.2 保存异常和清理状态；析构触发关闭的时机仍受 D001 限制 |

## 5. 模块与入口

- 顶层语句按顺序组成隐式 `main`，编译为 CIL `Main(string[])`。
- 顶层定义的变量是 `main` 的局部变量。Phase 1 中**函数体不能读取模块级变量**（只能引用顶层函数和类），否则报错。
- `from dataclasses import dataclass` 是 Phase 1 唯一允许的 import，由 HIR 特殊识别，不做真实导入。

Phase 2 M2.7 计划将模块变量改为模块命名空间成员，支持函数读取与 global 写入；各模块按首次 import 初始化，重复导入复用同一对象。项目源码和运行时模块通过同一注册边界解析，普通包与相对导入纳入首批范围。动态名称只加载构建时已登记模块，自定义导入器暂缓。失败与循环导入规则见 architecture.md §7。

## 6. 兼容范围与性能实现

Phase 2 优先动态核心、容器、普通类、异常与模块；Phase 3 增加闭包、生成器、推导式、装饰器和标准库。完整协程生态、元类、任意描述符、多重继承和解释器内部观测暂缓，Phase 5 汇总清单，不承诺最终全部补齐。

保留求值顺序、异常传播、可变别名、普通对象身份和容器行为的优化可默认采用。低影响偏差经 ADR 接受并公开登记后，也可选高性能默认实现。固定宽度整数、可观察的值拷贝、浮点重排等影响常用结果的扩展由用户显式选择；这些扩展目前尚未实现。refcount 与 GC 时机属于已接受架构边界，不代表其它差异也可省略。

原生、桥接和带偏差结果分别登记。当前通过的九个 Phase 1 e2e 用例只能证明其覆盖范围，不能证明常见项目或整个标准库已经可用。
