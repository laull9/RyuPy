# TODO

> 按里程碑组织，每个里程碑拆成多个贯穿所有层的「竖切」，以对应 e2e 用例转正为完成标志。
> 推进方式见 [docs/workflow.md](docs/workflow.md)；完成标准见其中的「里程碑验收」。
>
> 标记：`[ ]` 待做 · `[~]` 进行中 · `[x]` 完成
>
> 当前：Phase 1 已完成；Phase 2 的 M2.1–M2.11 全部完成（含模块、`with`、项目切片、PIC 与动态热路径优化），下一步 Phase 3 M3.0 闭包、lambda 与 nonlocal。目标为固定纯 Python 项目集至少 80% 原生通过，见 [兼容性清单](docs/compatibility.md)。下述里程碑包含多个竖切，每次按一个行为或 API 实施，不整阶段一次落地。
>
> Phase 2–4 的 Python 验收样例已预置并标记 pending，另有多文件辅助包、错误退出 fixture 与 CPython 基线脚本，见 [tests/README.md](tests/README.md)。它们不计为 RyuPy 已实现能力，也不替代待选的真实项目集。

## Phase 0：脚手架 ✅

- [x] Cargo workspace 与 crate 分层（base / syntax / hir / ty / mir / codegen-cil / driver / cli / e2e）
- [x] `ryupy-base`：`SourceDb`、`Span`、`Diagnostic`、ariadne 渲染（字节偏移）
- [x] `ryupy-syntax`：封装 ruff 解析器，语法错误转诊断，含单元测试
- [x] HIR / Ty / MIR 数据结构定义；各阶段入口函数签名（实现为 `todo!()`）
- [x] `ryupy-driver`：流水线编排、产物布局、runtimeconfig、运行时 dll 定位、`dotnet` 子进程
- [x] `ryupy-cli`：`build` / `run` / `dump --emit`
- [x] `RyuPy.Runtime`（C#）：`Builtins`、`PyMath`、`PyFormat` 骨架，开启 AOT 分析
- [x] e2e 差分测试框架（CPython 为准，`# ryupy: pending` 标记）+ Phase 1 用例
- [x] Makefile、CI（GitHub Actions）、文档、ADR
- [x] 完成首次提交（仓库已 `git init`）

---

## Phase 1：静态强类型子集（M1.0–M1.5 已完成）

### M1.0 曳光弹：`print(1 + 2)` 🎯 ✅

> 用例：`tests/e2e/phase1/m1_0_print_int.py`。目标是**最窄**地打通全链路，其它一律报「暂不支持」。

- [x] hir：`lower_module` 只处理「表达式语句 / 调用 `print` / 整数字面量 / `+`」，其它语法报 `E0100` 并产出 `Missing`
- [x] hir：合成隐式 `main` 函数
- [x] ty：`check_module` 只处理 `Int + Int → Int`、`print(...) → None`
- [x] mir：`build_program` 生成 `main`：`_1 = Add(1, 2); WriteI64(_1); WriteNewline(); return`
- [x] mir：`validate` 实现「无 `Unreachable` 残留」「ID 越界」两项检查
- [x] codegen：参照 dotnetdll 的 `tests/common/mod.rs`，构造程序集、`System.Runtime` 与 `RyuPy.Runtime` 引用、`RyuPy.Generated.Module` 类、`Main` 入口
- [x] codegen：栈化 `Assign(Binary)` / `Eval(Call)` / `Return`，整数加法用 `add.ovf`
- [x] codegen：`dump_cil` 输出 ilasm 风格文本
- [x] 快照：m1_0 的 HIR / MIR / CIL 三份快照
- [x] 去掉 m1_0 的 `pending`，`make e2e` 通过 🎉
- [x] 笔记：`docs/notes/` 写第一篇（从源码到 .dll 的全过程）

### M1.1 函数与局部变量

> 用例：`m1_1_functions.py`

- [x] hir：`collect_assigned_names` 预扫描（函数级作用域 + 「赋值即局部」）
- [x] hir：顶层 `def` 收集（允许前向引用），参数与标注降级为 `Param` / `TypeRef`
- [x] hir：赋值、带标注赋值、增量赋值去糖、`return`
- [x] hir：`global` / `nonlocal` / 嵌套 `def` 报错；函数体内读取模块级变量报错
- [x] hir：`pretty::dump` 改为接近源码的缩进格式
- [x] ty：由标注计算 `FnSig`；缺标注报 `E0201`
- [x] ty：手写并查集 `find` / `unify` / `resolve`（含单元测试）
- [x] ty：类型不匹配报 `E0203`，用 secondary label 指出「期望类型来自哪里」
- [x] ty：`ModuleTypes::dump` 改为可读格式
- [x] mir：多函数、参数、`Callee::Fn` 调用、函数末尾隐式 `return`
- [x] mir：`pretty::dump` 改为 rustc MIR 风格
- [x] codegen：多个静态方法、`ldarg`、`call`、返回值
- [x] 确定赋值分析：先读后写的局部变量编译期报错（语义陷阱 #10）
- [x] 去掉 m1_1 的 `pending`，`make check` 全绿 🎉
- [x] 笔记：`docs/notes/` 记录 M1.1 实现总结（作用域、并查集与 CIL 方法调用）

### M1.2 运算与控制流

> 用例：`m1_2_arith.py`、`m1_2_control_flow.py`

- [x] hir：全部 `BinOp` / `UnaryOp` / `CmpOp`；链式比较展开；`and` / `or` 多元转二元；条件表达式
- [x] hir：`if` / `elif` / `else`、`while`、`break` / `continue`（在循环外报错）
- [x] ty：按 semantics.md §3 实现运算规则表（独立函数 + 表驱动单元测试）
- [x] ty：条件表达式中 `int` / `float` 的真值处理
- [x] mir：分支 / 循环基本块构建、循环目标栈、短路求值
- [x] mir：`//` `%` `**` 改写为 `RuntimeFn`
- [x] mir：`simplify_cfg` Pass（删不可达块、合并 Goto 链）+ 快照
- [x] mir：`validate` 补全类型检查项
- [x] codegen：分支指令与目标回填；比较指令序列（无 `cle`/`cge`，注意 NaN，见陷阱 #15）
- [x] runtime：`FloorDivI64` 除零处理；
- [x] 移位量越界处理（ADR-0004）
- [x] 去掉 m1_2_arith 与 m1_2_control_flow 的 `pending`，`make check` 全绿 🎉
- [x] 笔记：`docs/notes/` 记录 M1.2 实现总结（运算语义、短路求值、CFG 构建与分支回填）

### M1.3 `for range` 与斐波那契

> 用例：`m1_3_for_range.py`、`m1_3_fib.py`

- [x] hir：识别 `for x in range(...)`（1~3 个参数）降级为 `ForRange`；其它可迭代对象报错
- [x] mir：计数循环，步长正负两种比较方向；`continue` 跳到递增块
- [x] mir：步长为字面量 0 编译期报错，为变量时运行时检查（陷阱 #9）
- [x] e2e：`fib_iter(100)` 溢出用例，记录为已知差异（ADR-0004）
- [x] 基准：fib(30) 对比 CPython，结果写进笔记（比 CPython 快 6.2 倍）
- [x] 去掉 m1_3_for_range 与 m1_3_fib 的 `pending`，`make check` 全绿 🎉
- [x] 笔记：`docs/notes/` 记录 M1.3 实现总结（计数循环 CFG、步长正负方向、基础常量折叠与基准对比）

### M1.4 浮点、类型转换与字符串

> 用例：`m1_4_floats.py`、`m1_4_mandelbrot.py`

- [x] ty / mir：`int` → `float` 隐式提升插入 `Cast`；`/` 总是 float
- [x] hir / ty / mir：`int()` / `float()` / `abs()` 内建
- [x] runtime：`PyFormat.FloatRepr` 按 CPython 规则实现（陷阱 #5、#16）+ 单元测试覆盖
- [x] runtime：`int(nan)` / `int(inf)` 检查（陷阱 #13）
- [x] 字符串字面量、`str + str`、`print(str)`
- [x] 基准：Mandelbrot 对比 CPython（50 次渲染纯计算耗时 44ms vs 267ms，快 6.1 倍）
- [x] 去掉 m1_4_floats 与 m1_4_mandelbrot 的 `pending`，`make check` 全绿 🎉
- [x] 笔记：`docs/notes/` 记录 M1.4 实现总结（浮点 repr 规整、空字符串 US 堆踩坑、分支压缩根治与曼德博基准）

### M1.5 `@dataclass` 🎯 ✅

> 用例：`m1_5_dataclass.py`

- [x] hir：识别 `from dataclasses import dataclass`；解析 `@dataclass` / `@dataclass(frozen=True)` 类体字段
- [x] ty：`Ty::Class`、构造调用、字段读写类型检查；frozen 类字段写入报错 `E0203`
- [x] mir：`NewObject` / `GetField` / `SetField`
- [x] codegen：生成 class（字段 + 构造函数）与 valuetype（Sequential 布局）
- [x] 思考并记录：frozen → struct 的值语义与 Python 引用语义的差异（写入 semantics.md 陷阱 #17 与 ADR-0008）
- [x] 快照：`snapshot_m1_5_dataclass`
- [x] 去掉 m1_5_dataclass 的 `pending`，`make check` 全绿 🎉
- [x] 笔记：`docs/notes/` 记录 M1.5 实现总结（dataclass 引用与值类型降级、模块分层与 600 行拆分）

### 原 M1.6 的未完成工作

Native AOT 转入 M5.2，矩阵乘法 / N-Body 转入 M2.9，基准维护转入 M3.9。均保持待做。Phase 1 完成指静态子集闭环，不表示 AOT 已可用。

---

## Phase 2：动态核心、容器、普通 class、异常、模块系统

阶段目标：无标注的多文件程序能够用容器与对象组织数据，捕获异常并明确清理资源。默认保留常用 Python 行为；具体偏差见 compatibility.md。先完成 M2.0 / M2.1，随后按依赖推进；容器与异常可用后尽早开始模块系统，不等所有类协议完成。

### M2.0 项目基线与测试入口 ✅

> 用例与产物：保留现有单文件用例；新增多文件 fixture、异常结果和项目清单的测试设计。此项先完成 CPython 基线与失败记录，不要求项目已在 RyuPy 下通过。

- [x] 选择至少 20 个纯 Python 项目，覆盖至少四类用途，固定版本、来源、传递依赖、入口和上游测试范围；更新 compatibility.md 的分母（20 个，6 类，`tests/projects/manifest.tsv`）
- [x] 固定 CPython 3.12 的具体小版本与工具链；逐项目运行基线，登记语法、模块、标准库和平台阻塞，保留暂缓能力带来的失败（3.12.13 + .NET 10.0.403；20/20 基线已记录；M2.9 起逐项目记录第一项阻塞）
- [x] 扩展 e2e 框架：支持多文件目录、工作目录、参数、环境和输入，明确区分 fixture 辅助文件与测试入口（`tests/cases/**/case.txt`，见 tests/README.md）
- [x] 扩展错误路径比较：退出状态、异常类型、必要消息与副作用；traceback 只规范化运行时框架差异，保留用户源码位置（错误路径只比最后一行与 cause，用户位置留待 M2.2 traceback）
- [x] 建立项目级运行入口与机器可读结果，原生 / 桥接 / 带偏差 / pending 分开；不把编译或导入成功算成项目通过（`ryupy-e2e --bin projects`，JSONL）
- [x] 验收：单文件测试保持通过，多文件与异常框架自测通过，选集与 CPython 基线可复现；后续功能用例仍可 pending（`make cases-selftest` 51 项、20 项目基线可复现）

### M2.1 动态值、数值与普通调用

> 依赖 M2.0。建议用例：`m2_1_dynamic.py`、`m2_1_calls.py`、`m2_1_bigint.py`，分别作为独立竖切。

- [x] 比较 ADR-0003 的 GC 合法布局，基准覆盖算术、调用、容器访问和 GC；定稿布局、Tag、数值 / 引用所有权与 RuntimeFn ABI
      （ADR-0010：装箱 object + `PyOps` 操作码 ABI；算术 / 容器基准已测，调用与 GC 基准留到 M2.9 优化时补）
- [x] hir / ty：允许未标注函数、异构赋值与多种返回类型；引入 `Ty::Dynamic`；类型标注只提供优化线索，保留通用执行路径
      （`FuncMode::{Static, Dynamic}` 单调不动点降级；缺标注 / 异构赋值 / 多返回类型 / 调用点不符 / 超大整数字面量都会降级）
- [x] mir / codegen / runtime：`MirType::Dynamic`、静态值提升、带守卫拆箱与通用回退；真实运算不合法才产生 Python 类型错误
      （`Box` / `Unbox` / `Dyn{Bin,Compare,Unary,Truthy}`；`PyCast` 拆箱失败抛 `TypeError`；动态 `range` 保留计数循环）
- [x] runtime：动态算术、比较、真值与短路返回操作数；设计 Python 错误基类，为 M2.2 的捕获语法接续
      （`PyOps` / `PyInt` / `PyRepr` / `PyNone` / `PyBaseException` + 42 项 xUnit；`and`/`or` 由基本块短路并返回操作数本身）
- [x] Python `int` 全路径支持大整数字面量、溢出升级与混合比较；覆盖加减乘、幂、移位、除法边界、取负、abs、转换与打印，静态特化保留升级退路
      （HIR 字面量放宽到 i128，超出 i64 走 `PyOps.FromDecimalString`；`int("...")`、混合比较、幂/移位/除法边界都有差分用例；静态特化溢出退路 = 去标注走动态）
- [x] 函数对象、默认参数的定义期求值、位置 / 关键字参数、仅位置 / 仅关键字、`*args` / `**kwargs` 与重复绑定诊断；闭包捕获留到 M3.0
      （`PyFunction` + 统一 thunk：生成代码把实参与关键字打包，`PyBinder` 按 CPython 规则绑定并抛 `TypeError`；
      静态直调保留快路径；默认值在 `def` 位置求值，可变默认值跨调用共享）
- [x] 验收：标注与未标注调用、可变默认参数、多返回类型、bool / int 关系、大整数差分通过；原有溢出失败用例改为数值正确性测试
      （`m2_1_dynamic` / `m2_1_dynamic_flow` / `m2_1_bigint` / `m2_1_calls` / `m2_1_call_binding` / `m2_1_defaults_static` 已转正）

### M2.2 异常体系与结构化清理

> 依赖 M2.1。用例：`m2_2_exceptions.py`、`m2_2_finally.py`、`m2_2_recursion.py`、错误退出 fixture。用户自定义异常在 M2.6 接续。

- [x] runtime：建立 BaseException / Exception 与常用子类，明确 `args`、异常匹配、`StopIteration`、`SystemExit` 和 .NET 异常边界映射
      （CLR 继承树与 Python 继承树同构，`except` 才能直接映射成类型化 catch；`WithCause` 用 `MemberwiseClone` 保留运行时类型）
- [x] hir / mir：`raise`、`raise ... from ...`、裸重抛、`try/except/else/finally`、assert 与异常变量清理；定义异常区域、展开边与验证规则
      （`Body::regions` 按块下标区间描述受保护区；`finally` 用 catch-all + 状态码 + 分派块，不用 CIL finally）
- [x] codegen：生成合法 CIL 异常表与 `leave` 等控制流；覆盖 return / break / continue 穿过 finally，以及清理时再抛异常
      （每条 catch 记录独立两指令入口；异常表按「内层优先」排序，否则 JIT 判 `InvalidProgramException`）
- [x] 数值与内建错误统一映射：除零含 float、转换失败、索引 / 键 / 属性 / 名称错误；处理未赋值局部变量，避免 CLR 默认零值泄漏
      （上述全部已统一：索引 / 键错误由 M2.3 容器补上，`KeyError.ForKey` 让 `args[0]` 是键本身）
- [x] 保留 Python 异常链、用户文件与行号，建立基本 traceback
      （编译器插桩 `PushFrame` / `SetLine` / `PopTo`；`__cause__` 与 `__context__` 都按 CPython 措辞输出）
- [x] 评估受控递归深度守卫与可恢复 RecursionError，测试安全阈值；不可用时保留 D005，不以 .NET 栈溢出捕获作为方案
      （`PyTraceback.PushFrame` 按帧数守卫，默认 1000 层，可用 `RYUPY_RECURSION_LIMIT` 调整）
- [x] 验收：嵌套捕获、异常链、未捕获退出、finally 副作用与覆盖返回值通过；错误类型不再由 CLR 异常名代替
      （`m2_2_exceptions` / `m2_2_finally` / `m2_2_recursion` 与四个 `tests/cases/errors` 已转正；索引 / 键错误已随 M2.3 容器一并转正）

### M2.3 容器与可变别名

> 依赖 M2.1 / M2.2。用例按 list、tuple、dict、set、bytes 分开。

- [x] list / tuple：字面量、构造、嵌套、下标、负索引、切片、赋值 / 删除、append / extend / pop 与普通 / 星号解包
      （`PyList` / `PyTuple`；星号解包由 `UnpackCheck` 校验长度后按下标取值，其余部分用 `PySlice` 一次切出）
- [x] dict：保持插入顺序，增删查改、get / items / keys / values / update；定义视图与迭代中修改行为
      （`PyDict` 用「键 → 槽位下标 + 平行键值数组 + 墓碑」，删除 O(1)；`Version` 只在增删时递增，覆盖已有键不递增；视图是「活的」，迭代器按槽位推进并校验版本）
- [x] set / frozenset：构造、成员判断、常用集合运算；统一可哈希判定与相等 / 哈希规则，覆盖 `True`、`1`、`1.0` 作键
      （`|` `&` `^` `-` 优先走集合语义，再回退到整数位运算；相等与哈希集中在 `PyHash`）
- [x] 可变容器保留共享引用；递归 repr / equality 的支持边界明确，循环引用不能导致无界运行
      （赋值只复制引用；`PyRepr` 用「正在处理」集合保护，自引用打印 `[[...]]`）
- [x] bytes / bytearray：字面量、索引、切片、常用转换与可变写入；为文件和编码 API 提供对象模型
      （`PyBytes` / `PyByteArray`；bytearray 支持下标 / 切片赋值与删除，并与 bytes 按内容相等）
- [x] 验收：空 / 异构 / 嵌套容器、共享别名、切片边界、不可哈希键和迭代失效做 CPython 差分
      （`m2_3_containers` / `m2_3_slices` / `m2_3_bytes` / `m2_3_hashing` 已转正；顺带解锁 M2.2 遗留的索引 / 键错误映射）
- [x] 额外修正：`print` 实参改为先全部求值再统一输出；`PyHash` 的 int / float 比较改走精确比较

### M2.4 迭代、字符串与常用内建

> 依赖 M2.3。用例：`m2_4_iteration.py`、`m2_4_strings.py`、`m2_4_builtins.py`；用户对象协议由 M2.5 接续。

- [x] `iter` / `next` / StopIteration、通用 for、for / while 的 else、in / not in；range 保留可验证的计数快路径
      （`IPyIterator`；`enumerate` / `zip` / `reversed` / `range` 是运行时迭代器；元组循环目标先写隐藏局部再解包）
- [x] 字符串按 Python 码点处理 len / 下标 / 切片，覆盖非 BMP 与孤立代理值
      （`PyText`；`m2_10_dynamic_fixes` 覆盖 emoji 的长度、下标、切片与迭代）
- [x] 字符串常用 split / join / strip / lstrip / rstrip / replace / find / rfind / index / count / startswith / endswith 和编码转换
- [x] f-string 与常用格式化、repr / str / bool；`print` 的 `sep` / `end` 已接入，`file` / `flush` 依赖 M3.4 的文件流
      （`PyOps.FormatValue`：宽度 / 对齐 / 补零 / 精度 / 进制 / 百分比，未知规格抛 `ValueError`）
- [x] `len`、`range`、`enumerate`、`zip`、`reversed`、`sum`、`min/max`、`any/all`、round、稳定 `sorted` / list.sort，含 key 与 reverse
      （`PySort.SortBy`：key 只计算一次，降序仍稳定；`round` 按 double 的精确二进制值做银行家舍入）
- [x] `type` / `isinstance`、`is` / `is not`、id / hash、`getattr` / `setattr` / `hasattr` 等对象内建登记统一协议；`setattr` 的属性协议随 M2.5 普通 class 接入
- [x] 验收：惰性消费、提前退出、空迭代器、Unicode、key 函数副作用和排序稳定性通过，未支持参数明确报错
      （`m2_4_iteration` / `m2_4_iteration_extra` / `m2_4_strings` / `m2_4_builtins` 已转正）

### M2.5 普通 class 与属性协议

> 依赖 M2.2–M2.4。用例：`m2_5_classes.py`、`m2_5_identity.py`。

- [x] hir / ty / mir / codegen：普通类、类体顺序执行、`__init__`、实例创建、self、方法调用与绑定方法对象
      （类体降级为有序 `ClassItem`；`class` 语句在模块入口位置创建 `PyClass` 并顺序写属性 / 方法）
- [x] runtime：实例属性、类属性、动态增删属性、属性查找优先级、缺失属性错误；保留共享引用、实例身份与类体副作用
      （`PyClass` / `PyInstance`；数据描述符优先于实例字典，`AttributeError` 消息与 CPython 一致）
- [x] 按竖切接入 `__repr__` / `__str__`、`__eq__` / `__hash__`、`__bool__` / `__len__`、`__iter__` / `__next__`、`__getitem__` 与常用运算槽；未支持的魔术方法明确登记
      （`PyObject.TryMethod` 统一入口；迭代器适配 `PyInstanceIterator`；`__contains__` / `__setitem__` / `__delitem__` 与常用二元 / 一元运算槽）
- [x] 实现受支持的 `property` / `staticmethod` / `classmethod` 绑定规则，不扩大为任意描述符协议
      （`PyProperty` / `PyStaticMethod` / `PyClassMethod`；其他方法装饰器与 `__getattribute__` 报 `E0100`）
- [x] 设计 shape 与类型版本；属性增删、方法替换、类属性写入后缓存失效，超限回到通用查找
      （`PyClass` 按全局修改版本校验查找缓存，上限 256 条；任意类属性修改自动失效）
- [x] 落实 ADR-0009：普通对象默认有稳定身份；frozen dataclass 跨动态边界保留身份，不能每次装箱制造新对象；值表示须有证明或显式性能扩展
      （`frozen=True` 也改为 .NET 引用类型；装箱只复制引用，`identity(point) is point` 成立）
- [x] 验收：实例别名、同名类 / 实例属性、绑定方法、property 和缓存失效差分通过；保持 M1.5 回归
      （`m2_5_classes` / `m2_5_identity` / `m2_5_cache_invalidation` / `m2_5_protocols` 已转正）

### M2.6 单继承、super 与自定义异常

> 依赖 M2.5。用例：`m2_6_inheritance.py`、`m2_6_user_errors.py`。

- [x] 普通单继承、方法覆盖、零参数 `super()` 与基类初始化；建立受支持类链和子类判断
      （`PyClass.BaseClass` 单链；绑定方法记录定义类，`PyMethodContext` 提供零参数 `super()`）
- [x] 接通继承的属性 / 方法 / 运算协议、类修改版本传播与内建 `isinstance` / `issubclass`
      （属性查找沿类链；`PyExceptionHierarchy` 提供内建异常继承判断）
- [x] 允许继承 Exception 的用户类，接入 M2.2 的抛出、匹配、参数与异常链
      （codegen 为每个用户异常类生成 CLR 子类与工厂委托，类型化 catch 与 Python 继承树同构）
- [x] 多重继承、任意元类、自定义 `__getattribute__` 等未支持能力给出诊断；不静默降级成单继承或普通字段
      （HIR 报 `E0100`；诊断快照 `m2_6_multiple_inheritance`）
- [x] 验收：覆盖方法、super 链、基类变更后的缓存、自定义异常捕获与清理通过，暂缓构造有诊断测试
      （`m2_6_inheritance` / `m2_6_user_errors` / `m2_6_exception_chain` 已转正）

### M2.7 模块、包与模块全局 ✅

> 依赖 M2.1 / M2.2，多文件 fixture 复用 M2.0；可在容器完成后提前推进。用例目录：`m2_7_modules/`、`m2_7_cycles/`。

- [x] driver：文件解析、模块搜索路径、依赖图和源码注册；hir 接收已加载的模块身份与符号，不自行读文件
- [x] 支持 `import x`、`import x as y`、`from x import y`、别名、普通包 `__init__.py` 与包内相对导入；星号导入明确拒绝（`E0100`）
- [x] 模块命名空间、函数读写模块变量、`global`、`__name__` / `__file__` / `__package__`、`__main__` 入口，区分 import 与 from 的绑定行为
- [x] runtime：模块缓存，导入前登记部分初始化对象；重复导入只初始化一次，循环导入按访问时状态工作；失败移除失败模块条目且保留已成功依赖
- [x] 首批只加载已编译项目模块；未登记的模块名报 `ModuleNotFoundError`；自定义 import hook、namespace package、热重载暂缓
- [x] 后端生成各模块初始化入口与全局槽位，不在编译时执行模块副作用；模块属性读写走生成的 getter / setter
- [x] 验收：相对 / 重复 / 循环 / 缺失导入、失败重试、模块共享状态、入口保护与 from 绑定差分通过
      （`m2_7_modules` / `m2_7_cycles` / `m2_7_import_retry` / `harness/multifile` / `errors/missing_module` 已转正）

### M2.8 with 与上下文管理 ✅

> 依赖 M2.2 / M2.5。用例：`m2_8_with.py`，先用自定义上下文对象，真实文件在 M3.4 接入。

- [x] `with` / 多上下文 / `as`，按顺序进入、反序退出；进入中途失败仍清理已进入对象
- [x] 按 Python 规则调用 `__enter__` / `__exit__`，传递异常，处理异常抑制、return / break / continue 与清理时再次抛出
- [x] HIR 去糖为 `try` / `except BaseException` / `finally`，复用 MIR 异常区域与清理边；`handled` 标记避免重复退出
- [x] 验收：正常与异常离开、多上下文顺序、异常抑制、`return` / `break` / `continue` 清理与 `__exit__` 再次抛出差分通过
      （`m2_8_with` 已转正；诊断快照 `m2_8_async_with`）

### M2.9 Phase 2 项目切片与容量验收 ✅

> 依赖 M2.1–M2.8。不要求此时达到最终 80%，保留 Phase 3 标准库和高级语法阻塞。

- [x] 组合多文件数据处理 / 命令行样例，覆盖动态调用、容器、继承、模块状态、异常恢复与资源清理；去掉本阶段用例 pending
      （`cases/harness/pipeline`、`m2_9_matrix_nbody`、`m2_9_pic`、`m2_9_long_run` 已转正）
- [x] 补齐矩阵乘法与 N-Body e2e，复测已有 Fibonacci / Mandelbrot，记录输入与实际收益
      （`tests/run_benchmarks.py` + `make bench`；结果见 M2.8/M2.9 学习笔记）
- [x] 实现有上限的动态运算 / 调用 PIC，测试类型变化、方法替换、模块重绑定与超限通用回退，收益不足时保持关闭
      （ADR-0012；动态基准约快 3.3%，默认开启，`RYUPY_PIC=0` 可关闭）
- [x] 更新项目矩阵与默认限制报告，逐项目写下一项阻塞；所有已接受偏差均链接示例与测试
- [x] 检查容器扩容、shape / PIC 增长和模块缓存生命周期，执行可变对象与导入的长期运行测试
      （`m2_9_long_run`：2 万项列表、5000 键字典、5000 次集合插入、1000 次重复导入）
- [x] `make check` 及本阶段多文件 / 错误路径 / 项目入口通过；更新语义表、ADR 与学习笔记

### M2.10 语义修正与性能收尾 ✅

> 集中修复审查发现的正确性缺陷、静默错误与热路径开销；每项都有差分用例或单元测试。
> 用例：`m1_6_range_var_and_shift.py`、`m2_10_dynamic_fixes.py`、`tests/cases/errors/unhandled_unpack_too_many`。

- [x] 真值判定覆盖空 list / tuple / dict / set / frozenset / bytes / bytearray / dict 视图
      （`PyOps.Truthy` 显式列出每种容器；`if not items:` 不再走错分支）
- [x] 字符串按码点处理 `len` / 下标 / 切片 / 迭代，覆盖非 BMP 字符
      （`PyText` 统一换算码点偏移；迭代器保存字符偏移，避免 O(n²)）
- [x] 字典覆盖已有键不再误报「迭代期间修改」；删除降为 O(1)；迭代不再二次哈希
      （槽位 + 墓碑 + 紧凑化；视图迭代直接在槽位上走，不复制快照）
- [x] 静态 `for range` 改用隐藏计数器，`range` 参数只求值一次
      （循环体改写循环变量不会缩短循环；边界变量被改写不影响迭代次数）
- [x] 解包数量校验同时检查「不足」与「过多」，星号形态按下限检查
      （`UnpackCheck(length, required, hasStar)`；`a, b = [1, 2, 3]` 抛 `ValueError`）
- [x] 静态 i64 溢出统一转成 Python `OverflowError`
      （`PyMath` 显式转换 `//` / `%` / `abs` / `**` / `int(float)`；入口包装器兜底 catch CLR 异常，不打印 .NET 调用栈；ADR-0004 现状、compatibility D003）
- [x] 移位语义修正：`1 << 100` 不再被 CIL 取模成 `1 << 36`；`>>` 支持移位量 ≥ 64
      （`PyMath.ShiftLeftI64` / `ShiftRightI64`，静态路径改走运行时调用）
- [x] `int ** int` 负指数返回 float
      （指数为确定的非负字面量时保留静态快速幂；否则表达式动态化，由 `PyInt.Power` 处理）
- [x] 排序改为稳定归并：`sorted` / `list.sort`、`reverse=True` 都保持相等元素的原始顺序
      （`PySort`；不再用不稳定的 `List.Sort` + 反转）
- [x] codegen：`MaxStack` 按最大实参个数计算，不再写死 64；`Unit` 局部变量分配 i64 槽位
      （修复 `x = None` 触发的 `InvalidProgramException`）
- [x] Pass 产出的 MIR 校验失败改为返回错误，由 driver 报内部错误，不再 `panic!`
- [x] 热路径开销：`PyInt` 增加 i64 快路径再升级 BigInteger、字符串哈希缓存、stdout 缓冲写入、
      traceback 帧栈改平行数组、`SetLine` 仅在行号变化时写入、`NumericEqual` 的 i64 快路径
- [x] 验收：新增用例与既有 e2e、快照、xUnit 全部通过；`make check` 通过

### M2.11 动态热路径性能改造 ✅

> 在不改变动态值表示（装箱 `object`）的前提下，消除调用、属性与字面量的固定分配。
> 设计、测量与边界见 [ADR-0013](docs/adr/0013-dynamic-hot-path-allocations.md)。

- [x] MIR 新增纯位置小参数调用 `DynCallValue` / `DynCallMethod`；codegen 走 `PyPic.CallValueN` /
      `CallMethodN`，按调用点复用参数数组
- [x] thunk 快路径：参数个数正确且无关键字时直接取参，跳过 `PyBinder.Bind`；Bind 自身增加
      纯位置快路径（实参正好占满时复用调用方数组）
- [x] 数值字面量池：`Box` 常量在模块初始化时装箱一次，循环直接读静态字段
- [x] 实例属性槽位与属性 PIC：`PyClass` 属性名→槽位表、`PyInstance` 值数组；
      `DynGetAttr` / `SetAttr` 命中时直接读写槽位，类版本变化或未赋值时回退
- [x] 方法 PIC 扩展：缓存类版本与同名实例槽位；函数不调用 `super()` 时跳过方法上下文
- [x] `long` 数值快路径（`PyInt.*I64`）与 `FlattenArgs` 无 `*` / `**` 快路径；
      空关键字数组复用共享数组
- [x] 基准更新：动态基准 792 ms → 196 ms（CPython 162 ms），纯动态调用 67 ms 已快于 CPython 80 ms；
      `RYUPY_PIC=0` 为 386 ms，证明收益来自 PIC 与热路径而不是测量偏差
- [x] 语义不变：e2e / 快照 / xUnit / `make check` 全绿；新增 `containers` 基准登记容器与
      字符串拼接的剩余差距（字符串拼接受 .NET 不可变字符串限制）

## Phase 3：闭包、生成器、推导式、装饰器与标准库移植

阶段目标：固定纯 Python 项目集至少 80% 原生通过。标准库按 [stdlib.md](docs/stdlib.md) 的 API 批次实施，C# 重写与适配按万行级工程安排；以下里程碑必须继续拆成单个 API / 行为的竖切。先解锁语言依赖，标准库基础 API 可穿插推进；桥接结果独立统计。

### M3.0 闭包、lambda 与 nonlocal

> 依赖 Phase 2 函数对象 / 模块作用域。用例：`m3_0_closures.py`。

- [ ] hir：嵌套函数、lambda、free / cell 变量分析与 nonlocal 解析；区分模块 global、外层 cell 与当前局部
- [ ] mir / codegen：共享 cell 与环境类，保持读写别名、late binding、闭包递归和函数逃逸
- [ ] 运行时 callable 接通闭包环境、默认参数和必要元数据，非捕获函数保留轻量路径
- [ ] 验收：循环捕获、多个闭包共享修改、lambda / 默认参数差异、外层退出后继续调用和非法 nonlocal 通过

### M3.1 推导式与生成器表达式作用域

> 依赖 M3.0 / Phase 2 迭代协议。用例按 list / dict / set 推导式与生成器表达式分开，生成器表达式执行在 M3.2 完成。

- [ ] list / dict / set 推导式，支持多层 for 与 if，独立作用域、闭包捕获和求值顺序
- [ ] 生成器表达式先定义外层迭代对象的立即求值与后续惰性边界，衔接 M3.2 状态机
- [ ] 验收：变量不泄漏、嵌套条件、副作用、重复键和异常顺序通过；海象运算符未支持时明确拒绝

### M3.2 生成器与暂停时清理

> 依赖 M3.0、M3.1 的作用域规则及 Phase 2 异常 / with。用例：`m3_2_generators.py`、`m3_2_generator_cleanup.py`。

- [ ] 普通 yield 逐切实现暂停 / 恢复、局部与 cell 保存、状态机 class、迭代与耗尽
- [ ] `return value` / StopIteration.value、`send`、`throw`、`close` 和 GeneratorExit；明确关闭状态与重入错误
- [ ] `yield from` 委托传递 next / send / throw / close 与返回值；生成器表达式完成惰性执行
- [ ] yield 穿过 try / finally / with 时保留清理状态，关闭和异常展开不跳过退出；实现生成器内意外 StopIteration 的 Python 行为
- [ ] 验收：部分消费、嵌套委托、关闭、异常与资源释放通过；即时析构触发的关闭时机仍受 D001 限制

### M3.3 装饰器与可调用包装

> 依赖 M3.0 / Phase 2 类与调用。用例：`m3_3_decorators.py`。

- [ ] 函数与类装饰器表达式求值、逆序应用、带参数装饰器；仅在定义时执行并绑定返回结果
- [ ] 接通 callable 对象、闭包包装、方法绑定；保留 __name__ / __doc__ / __annotations__ 等已声明元数据的可用范围
- [ ] 连接现有 dataclass 特殊降级与运行时装饰器路径，避免重复应用；property 等内建设计保持一致
- [ ] 验收：多层顺序、副作用、替换 callable、异常与装饰方法通过，functools.wraps 在 M3.6 接续

### M3.4 标准库：系统、路径与文件

> 依赖 Phase 2 模块 / bytes / with。每个模块再按 stdlib.md 列 API 开工。

- [ ] `sys`：argv、标准流、exit、受控模块缓存视图，登记缺少的解释器内部接口
- [ ] `io` 与 `open`：文本 / 二进制流、编码 / 换行、读写 / 定位 / 关闭，补权限与关闭后操作异常
- [ ] `os` / `os.path`：环境变量、目录和常用路径函数；`pathlib`：常用 Path 构造、读写与遍历
- [ ] 衔接 print(file=...)、模块定位与资源生命周期，分别验证 Windows / POSIX 承诺范围
- [ ] 验收：逐 API 差分、临时目录中的文件副作用与 with 清理通过；更新标准库 API 表与相关项目入口结果

### M3.5 标准库：数值、JSON 与正则

> 依赖 M3.4 与 Phase 2 容器 / Unicode / 异常；模块之间按实际依赖推进。

- [ ] `math` / `time`：按 API 实现定义域、溢出、NaN 与计时行为，不直接透传 .NET 异常
- [ ] `json`：loads / dumps / load / dump 与常用参数，大整数、转义、键顺序、非法输入与位置
- [ ] `re`：先建立 Python 与 .NET 模式差异测试，再实现受支持语法、flags、分组与 match / search / findall / sub；不支持构造明确拒绝或显式桥接
- [ ] 验收：每个 API 有边界、错误类型和项目调用测试；不得把部分 API 通过写成完整模块兼容

### M3.6 标准库：容器工具、函数工具与类型元数据

> 依赖 M3.0–M3.3，contextmanager 依赖生成器清理。

- [ ] `collections` / `collections.abc`：deque / defaultdict / Counter 与项目所需协议，支持范围独立登记
- [ ] `itertools`：chain / islice / repeat 起步，逐个补项目所需迭代工具并测试消费边界
- [ ] `functools`：partial / reduce / wraps / lru_cache；验证包装元数据、参数绑定、强引用规则与有界缓存
- [ ] `dataclasses`：字段默认值 / factory、repr / eq / frozen；保持身份规则与 Phase 1 回归，复杂继承参数另列
- [ ] `typing` 常用运行时元数据 / 类型别名与 `contextlib.contextmanager`，不把完整 typing / 反射接口标为已支持
- [ ] 验收：逐 API 差分、可变工厂、包装函数、缓存回收与生成器清理通过，更新项目阻塞

### M3.7 标准库：项目入口与剩余阻塞

> 依赖相关基础模块；每项先盘点项目所需 API，不能用总模块名替代范围。

- [ ] `argparse` 常用解析、帮助、错误退出，`logging` 常用等级 / 格式 / handler，`unittest` 基础发现 / 断言
- [ ] 按项目阻塞分别安排 `enum`、`copy`、`datetime`、`random`、`string` 的 API 清单、实现与上游测试移植
- [ ] 记录深拷贝环、时区、随机序列和特殊类机制的准确边界，必要时显式桥接或保持失败，不静默换成不同算法
- [ ] 每个模块选择 C# 重写、编译纯 Python 源码或桥接；保存源码版本、许可证、传递依赖与逐 API 状态
- [ ] 验收：项目实际入口和固定上游测试通过；原生覆盖不足时继续补阻塞，不能靠减少分母达标

### M3.8 标准库桥接的最小宿主

> 依赖 Phase 2 模块 / callable / 异常。为未原生移植的必要依赖提供显式路径，复用于 Phase 4。

- [ ] 写 ADR：宿主隔离、P/Invoke 边界、Python 版本与搜索路径、初始化 / 退出、线程 / GIL；不让编译阶段 crate 链接解释器
- [ ] 标量 / 字符串 / 容器与 callable 转换，owned / borrowed 引用、包装身份与异常双向传播；解决跨堆引用环和回调释放
- [ ] 定义编译模块与 CPython 模块缓存的互操作边界，拒绝不支持的共享全局和动态导入混用；禁止隐式整项目回退
- [ ] 记录 libpython / 标准库 / 扩展部署清单，测试缺失依赖、版本不匹配、回调错误与长期运行引用释放
- [ ] 验收：指定标准库 API 在桥接路径通过，测转换与调用成本；单列桥接项目结果，不计入原生 80%

### M3.9 前瞻优化的首批验证

> 按 [optimization.md](docs/optimization.md) 逐项立项，已有正确通用路径后才做；不阻塞常用 API 移植。

- [ ] 按 ADR-0002 评估迭代数据流或 SSA 视图；流敏感收窄 / 自动拆箱覆盖 bool 子类型、精确类型、值域与合流点
- [ ] 带守卫函数特化与有限变体，失败回通用路径；限制代码体积并验证标注不会新增类型拒绝
- [ ] 逃逸分析与临时元组标量替换，验证身份、别名、副作用、异常与暂停点；逃逸时保留对象
- [ ] 研究已知迭代器融合、循环不变量外提、容器专门化，无法证明副作用安全时保留原路径
- [ ] 维护 Fibonacci / Mandelbrot / 矩阵乘法 / N-Body 与项目热点基准，分别记录冷启动、稳态、分配与内存；无收益的优化保持关闭

### M3.10 Phase 3 项目兼容性验收

- [ ] 冻结分母不变，至少 80% 项目原生通过预定入口与上游测试范围；若未达标，继续按失败阻塞排序实现
- [ ] 另列桥接、带偏差、部分通过与不支持结果，注明平台、Python / .NET 版本、传递依赖和验证日期
- [ ] 更新逐 API 标准库清单，所有未通过项有具体失败原因；不把 import 成功等同功能通过
- [ ] 高级语法 / 标准库 / 项目测试及 `make check` 通过，更新语义陷阱、ADR 和学习笔记

## Phase 4：以 NumPy 为首的非纯 Python 模块可引入性

阶段目标：从 RyuPy 代码实际 import 依赖原生扩展的模块，拿到模块对象并完成基本调用。优先 NumPy，再按项目阻塞扩大到其它 C / C++ / Rust 扩展及其 Python 包装层。复用 M3.8 的解释器桥；不承诺跨任意 ABI，也不以零拷贝或 SIMD 作为本阶段完成条件。

### M4.0 扩展依赖与加载边界

- [ ] 冻结 CPython / NumPy / wheel / OS / 架构组合，记录扩展与系统动态库依赖、搜索路径和 ABI 限制
- [ ] 区分原生运行时模块、编译的纯 Python 模块与桥接扩展模块；由统一模块系统路由，不隐式执行整个项目

### M4.1 NumPy 导入与基本调用

- [ ] 支持 `import numpy as np`、`from numpy import ...`、子模块和传递依赖；重复导入复用身份
- [ ] ndarray、dtype、标量和模块属性通过桥接对象访问；验证创建数组、索引 / 修改、基础运算及 linalg 基本调用
- [ ] 验证 Python / NumPy 异常传播和共享对象修改，不要求先重写 NumPy 算子或实现原始内存快路径

### M4.2 扩展通用性与失败诊断

- [ ] 用 sqlite3 / zlib 等独立扩展验证导入、基本调用、资源关闭和异常，新增第三方扩展按版本逐个登记
- [ ] 缺失模块、缺失系统库、ABI / 架构不匹配、初始化失败均明确报错并清理失败模块状态，禁止静默替代
- [ ] 对回调、GIL、owned / borrowed 引用和跨堆对象做重复导入 / 调用与释放测试

### M4.3 可引入性矩阵验收

- [ ] `tests/e2e/phase4/` 的 NumPy、sqlite3、zlib pending 用例转正；部署负例需在隔离环境补齐
- [ ] 记录每个模块的导入形式、基本 API、依赖、平台与版本结果；导入通过不宣称全部 API 兼容
- [ ] 发布桥接部署清单与失败复现，结果与原生 80% 分开；Buffer Protocol / 零拷贝 / SIMD 移入后续优化

## Phase 5：兼容性清单与显式记录的偏差

### M5.0 发布项目与 API 支持矩阵

- [ ] 发布项目版本、测试范围、平台、执行路径、依赖、结果、失败原因与复现命令；保留冻结项目集历史
- [ ] 汇总语法 / 对象协议 / 标准库逐 API 状态，区分原生 JIT、原生 AOT、桥接 JIT 与桥接 AOT

### M5.1 偏差与性能扩展验收

- [ ] 每条偏差有触发示例、影响项目、替代方式、测试与版本；明确 refcount / 析构 / 弱引用时机长期不复现
- [ ] 默认构建提供限制和依赖报告；低影响默认偏差经 ADR 登记，影响常用结果的性能扩展需显式选择并进入产物记录；未经证实的收益不写成承诺
- [ ] 复核暂缓能力的项目阻塞，只有实际收益支持时新增里程碑，不把本阶段变回长尾补全

### M5.2 Native AOT 与交付（承接旧 M1.6）

- [ ] driver 按 ADR-0005 方案 A 实现 `publish_aot`，验证后定稿 ADR；不直接调用 ilc
- [ ] `ryupy build --aot` 端到端可用，分平台记录工具链、构建时间、体积、启动与行为
- [ ] 原生产物在免装 Python / .NET 的目标环境验证；桥接产物另验 libpython / 标准库 / 扩展依赖，禁止声称零外部依赖
- [ ] 验证异常、模块注册和静态可达元数据在裁剪后仍正确；发布命令与实际 CLI 一致

## 暂缓能力

- [ ] `async/await`、完整 asyncio、线程调度语义、模式匹配：按项目阻塞重新评估，尚无必做里程碑
- [ ] 多重继承 / 完整 MRO、任意描述符、元类、动态代码执行、自定义 import hook、完整 frame / tracing API：保持显式不支持
- [ ] 直接调用 ilc、进程内 netcorehost、GPU / 完整 DLPack：已有交付路径证明必要后再排期

---

## 待定 / 技术债

- [x] `Builtins` 改为带缓冲的 writer（64 KiB），入口包装器退出前 `Flush`；错误报告前先刷新 stdout
- [ ] `Name` 改为字符串驻留（等 profile 证明有必要）
- [ ] 错误码登记表 `docs/errors.md`（每个错误码一个最小复现 + 说明）
- [x] 深递归 `RecursionError` 守卫（M2.2），默认 1000 层，可用 `RYUPY_RECURSION_LIMIT` 调整
- [x] 浮点除零与 floor div / mod 数值边界统一抛 Python 异常（M2.2），并有差分用例
- [x] Phase 1 类型标注约束与 frozen dataclass 值拷贝已解除：M2.1 标注只作优化线索，M2.5 起 frozen dataclass 保留对象身份
- [ ] CLI 的 run 分支当前直接启动 dotnet 子进程；后续收回 driver，保持 architecture.md §2 的编排边界，本次只同步文档和测试
