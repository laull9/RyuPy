# TODO

> **2026-11-02恢复与排期更新**：当前代码恢复自已推送M3.10（`a788d86`），其已归档项目能力为5/20。此前未推送M3.11源码随临时工作区丢失，历史10/20及725项C#测试不能当作当前checkout能力。用户已暂停16/20前置，优先原生AOT/JIT，并批准小步提交推送与独立WIP远端备份。详见[ADR0042](docs/adr/0042-aot-jit-recovery-priority.md)；原兼容目标未达成、不勾完成。

> **后续迭代顺序**：用户已再次确认，C ABI属于当前打包主线，继续绑定/部署与有界优化；打包能力闭环后沿TODO继续兼容性适配。16/20只是暂停前置，不是取消或已完成；当前不重新追逐全部项目长尾。

> 按里程碑组织，每个里程碑拆成多个贯穿所有层的「竖切」，以对应 e2e 用例转正为完成标志。
> 推进方式见 [docs/workflow.md](docs/workflow.md)；完成标准见其中的「里程碑验收」。
>
> 标记：`[ ]` 待做 · `[~]` 进行中 · `[x]` 完成
>
> 当前：Phase 1 已完成；Phase 2 的 M2.1–M2.12 全部完成（含模块、`with`、项目切片、PIC、动态热路径优化与基准体系），Phase 3 M3.0 闭包、lambda 与 nonlocal 已完成，M3.1 三类急切推导式与生成器表达式已实现，M3.2 普通 yield 状态机首切已实现，继续扩展生成器协议。目标为固定纯 Python 项目集至少 80% 原生通过，见 [兼容性清单](docs/compatibility.md)。下述里程碑包含多个竖切，每次按一个行为或 API 实施，不整阶段一次落地。
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

- [x] list / tuple：字面量、构造、嵌套容器、下标、负索引、切片、赋值 / 删除、append / extend / pop 与平坦普通 / 星号序列解包
      （`PyList` / `PyTuple`；直接无展开tuple/list字面量的星号解包仍由 `UnpackCheck` 校验长度后按下标与 `PySlice` 取值，中段为新 list；一般 iterator 星号有限验收见 ADR0068，专项及完整门禁通过；嵌套目标仍报 E0100）
- [x] dict：保持插入顺序，增删查改、get / items / keys / values / update；定义视图与迭代中修改行为
      （`PyDict` 用「键 → 槽位下标 + 平行键值数组 + 墓碑」，删除 O(1)；`Version` 只在增删时递增，覆盖已有键不递增；视图是「活的」，迭代器按槽位推进并校验版本）
- [x] set / frozenset：构造、成员判断、常用集合运算；统一可哈希判定与相等 / 哈希规则，覆盖 `True`、`1`、`1.0` 作键
      （`|` `&` `^` `-` 优先走集合语义，再回退到整数位运算；相等与哈希集中在 `PyHash`）
- [x] 可变容器保留共享引用；递归 repr / equality 的支持边界明确，循环引用不能导致无界运行
      （赋值只复制引用；`PyRepr` 用「正在处理」集合保护，自引用打印 `[[...]]`）
- [x] bytes / bytearray：字面量、索引、切片、常用转换与可变写入；为文件和编码 API 提供对象模型
      （`PyBytes` / `PyByteArray`；旧行误称 bytearray 切片赋值已支持，实际仅单元素赋值及切片删除；切片赋值有限候选见 ADR0098，不能据本行推定完整右值协议；bytes 按内容相等）
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
- [~] 公共 `format(value, spec='', /)` 分域实现：阶段2真实类型槽/对象返回身份及有限文本、整数规格已接线；新27项C#、4项源差分通过（异常链保首败并窄迁），正常完整门禁877C#/228CP/202e2e+30原ignored通过。显式浮点g/G随后新增22项C#，连原27项共49及1项297调用差分通过，正常完整门禁899C#/228CP/202e2e+30原ignored通过；f/e/%/无typeprecision、复杂合法规格、显式根方法及旧f-string统一仍未完成；见ADR0073/0074，不计项目通过。
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
- [x] 有本地写入证据或重复def的模块函数公开绑定有限本地验收：真实定义位置发布，callee先于参数冻结，旧alias/defaults独立；HIR4/ty2、新3应用及原star/capture两例通过，结构1项及正常完整门禁（818 C# / 228 CP / 202 e2e＋30原ignored）通过（ADR0069）。只选有限函数集合，保无碰撞热路；受影响模块槽Dynamic成本明确，不冒全LEGB或无本地证据的外部猴子补丁。
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
- [x] 既有序列解包数量校验同时检查「不足」与「过多」，星号形态按下限检查
      （`UnpackCheck(length, required, hasStar)`；`a, b = [1, 2, 3]` 抛 `ValueError`；本条不代表一般 iterator 解包已验收）
- [x] 非星号一般 iterator 解包有限修复验收
      （`UnpackSequence(source, required)` 最多读取 N+1 项，不调用源的 len / length_hint；普通实例仅无 `__iter__` 时允许整数 `__getitem__` 后备。本层值先冻结再按序写目标，保直接无展开 tuple/list 字面量与既有 tuple_scalar 结构；12 项运行时及4项driver（3 CP/JIT应用差分＋1 MIR结构）及正常完整门禁通过（776 C# / 228 CP / 202 e2e＋30原ignored；ADR-0064），不扩一般星号 iterator 或嵌套目标）
- [x] 通用星号 iterator 解包有限本地验收：真实双 iter、首 iterator 的 len/length_hint、getitem 后备、全槽冻结及暂停恢复已接线；16项runtime、2项ABI、4项driver（3应用+1结构）及正常完整门禁（818 C# / 228 CP / 202 e2e＋30原ignored）通过。保exact字面量及非星号结构；4096仅容量预留，资源失败时点差异见ADR0068，不扩嵌套目标。
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

### M2.12 基准体系与动态路径第二轮优化 ✅

> 把 `make bench` 从 4 个基准扩成按类别组织、可剖析、可对比的基准体系，再按剖析结果优化动态热路径。
> 设计、测量与边界见 [ADR-0014](docs/adr/0014-benchmark-suite-and-dynamic-hot-paths.md)。
> 用例：`m2_12_sequence_inplace.py`、`m2_12_identity_sort_cache.py`；xUnit：`HotPathTests`。

- [x] 基准体系：`tests/benchmarks/` 22 个可运行基准 + 4 个 Phase 3 pending 基准，文件头 `# bench:` 声明类别 /
      说明 / 变体 / pending；`tests/benchlib/` 负责发现、测量、报告、剖析
- [x] 测量口径：墙钟（含启动）与进程内执行耗时分开；RyuPy 记录托管分配、GC 次数 / 暂停、JIT 耗时 / 方法数
      （`RYUPY_STATS`，`PyStats.cs`），双方记录峰值内存与编译耗时；类别 / 总体几何平均
- [x] 入口：`make bench` / `bench-quick` / `bench-list` / `bench-save` / `bench-compare` / `bench-profile`；
      结果写 `<target>/bench/results/*.json`，`--compare` 逐项对比；`--profile` 用 dotnet-trace 输出热点函数表
- [x] 语义补齐（基准暴露）：序列 `+` / `*`、增量赋值原地语义、增量赋值目标只求值一次、
      `True` / 小整数的 `is`、排序只用 `<`
- [x] 优化：动态函数编译期可绑定时直调；i64 溢出检测去掉异常；比较 / 相等 / 二元运算数值快路径；
      `TryMethod` 不再为内建值分配实参数组；bool 单例与小整数装箱缓存；排序改为数组上的二分插入 + 归并
- [x] PIC：修复「同一站点总写同一格」导致的实际单态缓存，改为每站点 4 条轮转；属性写入不再因类上有
      其它 property 而整类放弃缓存；新增 property getter、类属性值、`super().method()` 缓存；实例槽位预分配
- [x] 运行时文件按职责拆分（`PyCompare` / `PySequenceOps` / `PyTextMethods` / `PyBytes` / `PyPicMethods` /
      `PyPicAttrs`），全部 ≤ 600 行；HIR 增量赋值降级拆到 `aug_assign.rs`
- [x] 验收：几何平均加速比（CPython / RyuPy）0.40x → 0.53x，RyuPy 自身 1.35x（明细见 ADR-0014）；
      e2e / 快照 / xUnit / `make check` 全绿

## Phase 3：闭包、生成器、推导式、装饰器与标准库移植

阶段目标：固定纯 Python 项目集至少 80% 原生通过。标准库按 [stdlib.md](docs/stdlib.md) 的 API 批次实施，C# 重写与适配按万行级工程安排；以下里程碑必须继续拆成单个 API / 行为的竖切。先解锁语言依赖，标准库基础 API 可穿插推进；桥接结果独立统计。

### 项目驱动基础内建补齐

- [x] `ord` / `chr` 直接调用：码点（含补充平面/孤立代理项）、单字节bytes/bytearray、`__index__`、范围与参数错误；wcwidth首阻断已从ord前移至map，项目仍未通过
- [ ] 修复独立代理码点拼接被UTF16合并的表示缺口（D009），`m3_builtins_surrogate_pair.py`保留pending；非已接受性能偏差
- [x] 一等内建函数值与惰性`map`：既有 M3.7 已实现 `PyMap` 与正式 `m3_7_map.py` 差分（逐项消费、多输入、错误恢复及重入）；本项仅纠正过时状态，不计作本轮新增能力或重新验证整个项目集

### 通用兼容欠账

以下问题是在M3.2逐切验证时额外发现并加入清单的通用容器/调用缺口，不属于初始M3.2协议与清理验收。
规划校正：从M3.2移到这里保留未完成状态、诊断和pending，不缩减原始生成器验收或固定20项目分母。
原用例文件名仍保留m3_2前缀以便追溯；绝不因迁移算作已支持。见本轮M3.2实现笔记及ADR-0020/21/23。

- [ ] 大字典用户键分块缓存hash合并：M3.7将超过17项动态纯标量键改为每pair真实值守卫；tuple/bytes/用户对象在hash/eq前NotImplementedError。`m3_2_dict_chunk_hash.py`继续pending，不以守卫路径冒称用户键兼容
- [ ] 补齐剩余表达式 yield 求值准备：双星号实参映射快照、dataclass专用构造及内建完整构造签名；当前未实现位置明确诊断；普通无暂停内建方法仍需逐项统一arity校验；list clear/pop/reverse/copy 已接线窄修（ADR0070），14项C#、3项真实应用及1项结构和完整门禁已通过，其余方法不借此勾完
- [~] list.pop 宽索引与一次 `__index__` 协议：先 long 判界后窄化，转换先于空表并观察回调后长度；三个真实应用、新7项C#及1项结构和完整门禁通过，见 ADR0071。警告型返回值/int子类仍有限边界。
- [ ] set/frozenset源含用户对象的星号展开需复用源缓存hash；当前显式NotImplementedError，不重跑用户hash（`m3_2_set_cached_hash.py` pending）
- [ ] 生成器方法零参super的owner/self上下文保存；当前仍E0100，普通方法支持不代表恢复委托可用
- [~] 普通类 hash 槽有限修复：创建时 own eq/缺 own hash 物化 None，继承/动态重绑、根 hash 显式绑定、空容器 None 准入及 dataclass 联动已接线，16项C#与4项真实差分及完整门禁通过；见 ADR0066。`collections.abc.Hashable` 仍不导出，`m3_6_hashable_pending.py` 保留；后续 ADR0072 已接线用户hash返回归约及必要整数/double等值闭包，新11项C#、3项真实应用及完整门禁通过；空表有效键副作用仍独立 pending，不冒完整协议。

### M3.0 闭包、lambda 与 nonlocal

> 依赖 Phase 2 函数对象 / 模块作用域。用例：`m3_0_closures.py`、`m3_0_scope_edges.py`、`m3_0_exception_cells.py`。

- [x] hir：嵌套函数、lambda、free / cell 分析与 nonlocal 解析；区分模块 global、外层 cell 与当前局部
- [x] mir / codegen：共享 cell 与环境数组，保持读写别名、late binding、闭包递归和函数逃逸（ADR-0015）
- [x] 运行时 callable：thunk 接收函数实例，从中读取默认参数与环境；签名共享，非捕获函数使用 null 环境
- [x] 验收：循环捕获、共享修改、参数捕获、默认值、互相递归、分支定义、外层退出后调用、空 cell、except 各退出路径清理与非法 nonlocal；逐层快照和诊断快照已补齐
- [x] `tests/benchmarks/closures.py` 转正；对比 M2.12 运行记录并保存 M3.0 基线，旧基线没有闭包通过项，不计算前后提升
- [x] 固定项目集重跑：仍 0/20 原生通过；项目首个阻塞与日期已更新，详见 compatibility.md

嵌套函数 / lambda 走运行时 `call_value`，静态可证明的顶层调用继续直调。分配与性能记录见
[实现笔记](docs/notes/2026-11-01-m3.0-closures.md)。

### M3.1 推导式与生成器表达式作用域

> 依赖 M3.0 / Phase 2 迭代协议。用例按 list / dict / set 推导式与生成器表达式分开，生成器表达式执行在 M3.2 完成。

- [x] list / dict / set 推导式，支持多层 for 与 if，独立作用域、闭包捕获和求值顺序
  - [x] list：多层 for / if、平坦解包、独立作用域、共享 cell、最左 iterable 立即取迭代器（ADR-0016）
  - [x] dict / set：重复键、key-before-value、哈希/异常顺序与集合去重分别验收
- [x] 生成器表达式最左迭代对象立即求值 / 取迭代器，其余元素、过滤与内层 iterable 惰性执行，复用 M3.2 状态机；独立作用域与共享 cell 差分通过
- [x] 急切推导式验收：变量不泄漏、嵌套条件、副作用、重复键和异常顺序通过；海象运算符明确拒绝（生成器表达式另有惰性差分）

### M3.2 生成器与暂停时清理

> 依赖 M3.0、M3.1 的作用域规则及 Phase 2 异常 / with。用例：`m3_2_generators.py`、`m3_2_generator_cleanup.py`。

原始协议/清理验收不变；逐切额外发现的容器/调用欠账见上方「通用兼容欠账」，相关pending与拒绝边界继续保留。

- [x] 普通 yield 首切：MIR 状态机、暂停 / 恢复、局部与共享 cell 保存、迭代 / 耗尽、重入防护及意外 StopIteration 转 RuntimeError（`m3_2_generator_basic.py`）；跨暂停异常清理在下列整里程碑验收
- [x] `return value` / StopIteration.value：只有首次完成的 next 可见终值；for / list / next(default) 消费后不泄漏，None 与 args / 对象身份差分通过
- [x] `send` 与表达式 yield：None启动、接收值、复合表达式跨暂停、lambda/default作用域、参数与重入 / 耗尽规则通过
- [x] 无星号暂停调用先绑定目标：函数 / 普通类 / 普通及内建方法、property、缺属性时机与非callable延迟检查通过；prepared内建方法先校验arity再执行副作用
- [x] 星号tuple/set保持最终类型；tuple展开后冻结，set前缀求值/流式hash插入与跨暂停差分通过，修复既有返回临时list问题
- [x] 增量赋值保存原目标对象与索引，支持属性/下标跨yield恢复；普通RHS重绑定亦不改变写回位置，覆盖切片身份与写回失败后的原地副作用
- [x] 字典字面量跨yield保存key/value并匹配固定CPython 3.12的hash时序：≤15对先全部求值后插入，16–17对逐对插入，更多项纯标量字面量免守卫，M3.7新增真实动态标量守卫；含边界/失败顺序差分，`**`字典合并仍待做
- [x] 暂停调用的`*`位置参数准备：混合位置项在各star处立即快照，唯一star位置源延至最终调用展开；复用ListExtend/StarArg，无新ABI，普通无暂停路径不变
- [x] 现代单参`throw`、`close` 和 GeneratorExit；新建/已闭合/重入/忽略关闭产出及异常状态差分与完整门禁通过；旧式throw(type,value,tb)明确拒绝并单列范围
- [x] `yield from`委托next/send/throw/close与返回值、自定义iterator终值及属性错误差分与完整门禁通过
- [x] yield 穿过 try / finally / with 时保留清理状态，关闭和异常展开不跳过退出；实现生成器内意外 StopIteration 的 Python 行为
- [x] 验收：部分消费、嵌套委托、关闭、异常与资源释放通过；即时析构触发的关闭时机仍受 D001 限制

### M3.3 装饰器与可调用包装

> 依赖 M3.0 / Phase 2 类与调用。M3.3完整验收通过，见ADR-0027及`m3_3_*`差分；88 e2e/18 pending、177 C#、102 CPython基线，固定项目仍0/20。

- [x] 函数与类装饰器表达式求值、逆序应用、带参数装饰器；仅在定义时执行并绑定返回结果
- [x] 接通 callable 对象、闭包包装、方法绑定；保留 __name__ / __doc__ / __annotations__ 等已声明元数据的可用范围
- [x] 连接现有 dataclass 特殊降级与运行时装饰器路径，避免重复应用；property 等内建设计保持一致
- [x] 验收：多层顺序、副作用、替换 callable、异常与装饰方法通过，functools.wraps 在 M3.6 接续

### M3.4 标准库：系统、路径与文件 ✅

> 依赖 Phase 2 模块 / bytes / with。每个模块再按 stdlib.md 列 API 开工。

- [x] `sys`：argv、标准流、exit、受控模块缓存视图，登记缺少的解释器内部接口
- [x] `io` 与 `open`：文本 / 二进制流、编码 / 换行、读写 / 定位 / 关闭，补权限与关闭后操作异常
- [x] `os` / `os.path`：环境变量、目录和常用路径函数；`pathlib`：常用 Path 构造、读写与遍历
- [x] 衔接 print(file=...)、模块定位与资源生命周期，分别验证 Windows / POSIX 承诺范围
- [x] 验收：逐 API 差分、临时目录中的文件副作用与 with 清理通过；更新标准库 API 表与相关项目入口结果

> 聚合门禁：209 C#、98 编译 e2e / 16 ignored、110 CPython 基线；逐 API / 平台边界见 docs/stdlib-system.md 与 ADR-0028。固定项目仍原生 0/20，binaryornot 首阻断从 open 前移至 locals。

### M3.5 标准库：数值、JSON 与正则 ✅

> 依赖 M3.4 与 Phase 2 容器 / Unicode / 异常；模块之间按实际依赖推进。

- [x] `math` / `time`：按 API 实现定义域、溢出、NaN 与计时行为，不直接透传 .NET 异常
- [x] `json`：loads / dumps / load / dump 与常用参数，大整数、转义、键顺序、非法输入与位置
- [x] `re`：先建立 Python 与 .NET 模式差异测试，再实现受支持语法、flags、分组与 match / search / findall / sub；不支持构造明确拒绝或显式桥接
- [x] `re` exact PyBytes 域：分阶段闭合现有匹配、捕获、替换及 escape API；公共链已接，40 项专项与四组真实应用通过，完整门禁通过（1059 C# / 228 CP / 202 e2e + 30 ignored）。可变 buffer、split/scanner 及旧复杂语法保留后续范围，不计项目通过（ADR-0080）。
- [x] 验收：每个 API 有边界、错误类型和项目调用测试；不得把部分 API 通过写成完整模块兼容

> 聚合门禁：241 C#、105 编译 e2e / 17 ignored、118 CPython 基线；stdlib-numeric.md 与 ADR-0029 逐 API 记录参数/语法/容量边界。固定项目仍原生 0/20，JSON高级选项与旧类绑定欠账保留独立pending。

### M3.6 标准库：容器工具、函数工具与类型元数据 ✅

> 依赖 M3.0–M3.3，contextmanager 依赖生成器清理。
> 完成原始验收与限定 API：297 C#、122 编译 e2e / 16 ignored、134 CPython 基线；构造守卫、死装箱消除与受守卫累加槽均经实际编译 A/B 和身份/回退差分。见 ADR-0030、三张 API 表及性能记录；固定项目仍 0/20，Hashable 等欠账保留。

- [x] `collections` / `collections.abc`：deque / defaultdict / Counter 与项目所需协议，支持范围独立登记
- [~] `collections.namedtuple` 有限真实类型：工厂、只读字段、tuple协议与helpers已接线，50项C#及5组真实CP/JIT首次通过；正常完整门禁首次通过（982项C#、228CP基线、202e2e+30原ignored）。默认module仅真实Dynamic/模块来源，Static未知NI；Unicode/NFKC、可变类/继承/copy/复杂反射及跨类名字intern身份等保边界。后续固定tabulate候选仍因缺html失败，不更新历史项目分子（ADR0077）。
- [x] `itertools`：chain / islice / repeat 起步，逐个补项目所需迭代工具并测试消费边界
- [x] `functools`：partial / reduce / wraps / lru_cache；验证包装元数据、参数绑定、强引用规则与有界缓存
- [x] `dataclasses`：字段默认值 / factory、repr / eq / frozen；保持身份规则与 Phase 1 回归，复杂继承参数另列
- [x] `typing` 常用运行时元数据 / 类型别名与 `contextlib.contextmanager`，不把完整 typing / 反射接口标为已支持
- [x] 验收：逐 API 差分、可变工厂、包装函数、缓存回收与生成器清理通过，更新项目阻塞

### M3.7 标准库：项目入口 API 与有界收尾

> 依赖相关基础模块；每项先盘点项目所需 API，不能用总模块名替代范围。
> 2026-11-01 用户明确批准重排，见 [ADR-0032](docs/adr/0032-bounded-stdlib-and-compatibility-gate.md)：标准库 API 交付与固定 16/20 兼容门拆分，后者完整保留为 M3.11，未完成、不降低分母。M3.7 当前只收尾 progress 的 mapping % 与 chardet 的 Flag 子集，若出现重大新依赖链则记录并排序，不无限追逐。
> 已完成用户批准后的有界范围：2026-11-01 最终 runtime/check/projects/upstream 命令链 exit0，422 C# / 214 基线 / 189 编译差分（29 ignored）；固定 5 native / 14 unsupported / 1 failed，原生 25%。三个指定上游集合 9/16/9 通过。progress已原生通过；chardet越过Flag后仍因bytes正则失败，准确登记后不扩新链。原16/20兼容门仍未完成。

- [x] `argparse` 常用解析、帮助、错误退出，`logging` 常用等级 / 格式 / handler，`unittest` 基础发现 / 断言
- [x] 按项目阻塞分别安排 `enum`、`copy`、`datetime`、`random`、`string` 的 API 清单、实现与上游测试移植
- [x] 记录深拷贝环、时区、随机序列和特殊类机制的准确边界，必要时显式桥接或保持失败，不静默换成不同算法
- [x] 每个模块选择 C# 重写、编译纯 Python 源码或桥接；保存源码版本、许可证、传递依赖与逐 API 状态
- [x] 完成当前已定义的 `enum.Flag` 与字符串 mapping `%` 子集，复测 chardet / progress；重大新链保留真实失败原因与后续排序，不在本轮无限扩展
- [x] 验收：已声明 API 的真实差分、选定上游范围及完整工程门禁通过；固定 20 项目全部复测并如实分类，不要求本里程碑达到 16/20；原兼容目标完整移交 M3.11，不能将标准库交付称为兼容门完成

### M3.8 标准库桥接的最小宿主

> 依赖 Phase 2 模块 / callable / 异常。为未原生移植的必要依赖提供显式路径，复用于 Phase 4。
> 排序说明（2026-11-01 用户JIT方向与批准重排）：本项保持全部未完成，暂后排到确有桥接依赖时；它不是M3.9/M3.10原生优化的技术前置，不因后排视作跳过或完成。

- [ ] 写 ADR：宿主隔离、P/Invoke 边界、Python 版本与搜索路径、初始化 / 退出、线程 / GIL；不让编译阶段 crate 链接解释器
- [ ] 标量 / 字符串 / 容器与 callable 转换，owned / borrowed 引用、包装身份与异常双向传播；解决跨堆引用环和回调释放
- [ ] 定义编译模块与 CPython 模块缓存的互操作边界，拒绝不支持的共享全局和动态导入混用；禁止隐式整项目回退
- [ ] 记录 libpython / 标准库 / 扩展部署清单，测试缺失依赖、版本不匹配、回调错误与长期运行引用释放
- [ ] 验收：指定标准库 API 在桥接路径通过，测转换与调用成本；单列桥接项目结果，不计入原生 80%

### M3.9 前瞻优化的首批验证

> 按 [optimization.md](docs/optimization.md) 逐项立项，已有正确通用路径后才做；不阻塞常用 API 移植。
> 首批五项已完成有界验证：tuple 标量替换与有限区间证明默认启用；直接累加槽和叶调用虽正确但通用/失败场景未达门槛，仅显式实验。完整26项、四核心预热kernel、实际JIT及分配/内存证据已归档；默认整套main耗时几何+2.8%，不能称整体提速。见 [验收记录](docs/notes/2026-11-01-m3.9-validation.md)。

- [x] 按 ADR-0002 评估迭代数据流或 SSA 视图；流敏感收窄 / 自动拆箱覆盖 bool 子类型、精确类型、值域与合流点
      （ADR-0036：有限闭区间/Top 工作队列、合流凸包与有界扩张，未知/可能溢出保原路；MIR结构与真实差分已过，专项与整套结果已归档；接受有证明的有限默认，不掩盖未改写对照的不利时间。）
- [x] 带守卫函数特化与有限变体，失败回通用路径；限制代码体积并验证标注不会新增类型拒绝
      （ADR-0035：纯叶 Add 单预测候选，私有原函数身份锚点、exact-long/溢出守卫与原调用回退；递归限额共享检查。
      两组宽候选及两组 exact 实参候选均有回退退化，决定默认关闭、仅显式实验；默认/显式/双实验组合真实测试通过，完整门禁423 C#、220基线、195 e2e/29 ignored通过。）
- [x] 逃逸分析与临时元组标量替换，验证身份、别名、副作用、异常与暂停点；逃逸时保留对象
      （ADR-0034：同块新鲜匿名tuple、≤8元素/函数总尝试≤32；构造点冻结，逃逸/别名/EH/暂停保留对象。
      两组真实配对消除64B/轮，返回与逃逸对象保留；422 C#、216基线、191 e2e/29 ignored完整门禁通过，不是一般对象逃逸分析。）
- [x] 研究已知迭代器融合、循环不变量外提、容器专门化，无法证明副作用安全时保留原路径
      （已有[真实 MIR 有界研究](docs/notes/2026-11-01-m3.9-loop-container-research.md)：小 tuple 迭代、纯位运算 LICM、exact-list 索引；均未实施，需结合 JIT 证据决定，不预报收益。）
- [x] 维护 Fibonacci / Mandelbrot / 矩阵乘法 / N-Body 与项目热点基准，分别记录冷启动、稳态、分配与内存；无收益的优化保持关闭
      （基础设施已在 M2.12 就绪：`make bench` 已含这些基准与分配 / GC / JIT / 内存列，`bench-profile` 定位热点；
      本次完整26项原规模、432个轮转进程、80个四核心kernel进程与16个实际JIT进程均语义通过；
      冷启动/含JIT的main/固定预热kernel分开，默认整套+2.8%、禁tier-5.3%，不利值和资源数据完整保留。）

### M3.10 面向 .NET JIT 的性能验收

> 2026-11-01 用户明确方向：尽量让生成的 CIL 命中 .NET JIT 优化，而不是在 C# 底座上再造解释器。以下原六项已按本节明确的有限范围实现并验收；不代表通用特化或全ABI已完成。经用户 2026-11-01 批准，原项目兼容性验收原文迁至 M3.11；性能完成不等于 80% 兼容完成。

> 用户 2026-11-01 追加要求更激进去装箱与动态 union：重新评估 ADR-0003/0010 的阶段性 object 取舍，比较 GC 合法16B sentinel+bits、24B tag+bits+ref 与选择性多表示/typed容器；原型需同时验证身份/物化、整数升级、调用、容器复制与JIT，不能仅数值快就替换生产ABI。独立16B/24B原型已完成布局、GC、调用和容器对照，收益与身份失败反例保留；ADR-0039自适应数值list岛已完成有界生产验收，保留普通对象ABI；通用union ABI尚未实现。

> 当前首切：[ADR-0037](docs/adr/0037-guarded-float-chain-scalarization.md) 的短 F64 算术链已通过差分/真实命中与预设一次配对JIT：矩阵约少72MB、N-Body约少264MB，双tier均加速；非浮点失败退化保留。首切聚合门禁已通过（423 C#、222基线、196 e2e/30 ignored），不计整个M3.10完成；严格内层浮点累加ADR-0038也已通过（423 C#、222基线、197 e2e/29 ignored），真实矩阵相对短链再少98.1%分配。有界数值list岛ADR-0039已完成（433 C#、224基线、198 e2e/30 ignored），相对FloatLoop基线矩阵双tier快30.5%/41.3%，同时保wrapper+8B、bool失败/代码增长代价。真packed SIMD按[ADR-0040](docs/adr/0040-guarded-two-lane-float-slp.md)完成有界实现、ISA回退/位模式与实际向量机器码验收，但相对同守卫scalar无净收益，两个变体均显式默认关闭；不是默认普遍向量化。主体聚合门禁433 C#、225基线、199 e2e/30 ignored已通过，最后补[ADR-0041](docs/adr/0041-proven-list-read-bounds.md)同pair证明下的重复逻辑范围检查消除，源码/实际JIT与最终门禁均已通过（434 C#、225基线、199 e2e/30 ignored），最终全26项两版本×两tier的104冷进程全部通过（单样本覆盖，不冒稳态）；结果与不利样本完整归档。通用union ABI未实现，整M3.10按上述有限范围验收完成。

- [x] 将已证明/受守卫的热点降为直接有类型 CIL 运算、调用、循环和分配；通用 object/PyOps 路径作为语义回退，不作为最终热点架构
- [x] 激进但有预算的类型特化：按实际类型/布局生成有限变体，内联与去虚拟化暴露给 JIT；溢出、类型变化、版本失效回正确通用路径，记录命中率与代码体积
- [x] 逃逸分析、标量替换与分配/装箱消除；观察身份、别名、异常、暂停/跨调用时按需物化且不重复副作用
- [x] 容器特化与循环优化：同质元素/索引路径、边界检查消除、循环不变量外提，变型/共享别名写入有正确升级或守卫回退
- [x] 自动 SIMD/向量化机会：产生 JIT 可识别的规则数值循环，必要时研究受 ISA 守卫的向量 CIL；逐目标验证机器码，不把重排浮点运算或丢失整数溢出语义当作默认提速
- [x] 性能验收同时保留 CIL、JIT 反汇编、装箱/分配量、特化/守卫命中率及冷启动/稳态吞吐证据；不能仅以 C# 通用 helper 更快宣布完成

### M3.11 Phase 3 独立项目兼容性门

> 后续候选影响面与成本见 [项目阻断排序](docs/project-backlog.md)，不是本轮自动新增scope。
> 由 M3.7 原无限补阻塞要求与 M3.10 原兼容验收拆出；用户批准的是排期与验收分离，不是减少项目、改变成功标准或宣布已完成。当前仍 5/20。M3.8 桥接仍未完成，M3.9 已独立完成首批验证；二者都不能替代本兼容门。

- [~] 普通单继承真实 `__new__` / `__init__`：本地门禁/三平台语义与有限fixture NativeAOT已验，已随打包批次进入main；保专用布局与罕见槽历史冷拒绝，不扩元类/多继承、不计项目通过（[ADR-0050](docs/adr/0050-ordinary-construction-protocol.md)）
- [~] 真实 `object()` 基础实例/身份与sentinel：本地完整门禁与三平台专项/组合CI通过，已随打包批次进入main；派生构造协议和任意返回富比较旧债独立，不计项目通过（[ADR-0049](docs/adr/0049-base-object-identity.md)）
- [~] 默认 site 的真实 `exit` / `quit` Quitter：参数/关闭stdin/异常上下文/一等身份与重绑专项及完整门禁通过，三平台专项及组合回归通过（旧测试目录碰撞已窄修并保失败历史），已随打包批次进入main，不计项目通过（[ADR-0048](docs/adr/0048-default-site-quitters.md)）
- [~] `int()` / 显式进制构造的真实参数绑定与求值顺序：671a48c最小差分证实第二参数及副作用被旧conversion遗漏；本地有限签名/严格文本解析、16项runtime、3项真实driver和完整门禁已通过，已进入main且Ubuntu/macOS普通CI通过，不计项目通过（[ADR-0053](docs/adr/0053-int-constructor-binding.md)）。
- [~] `abs` 完整实参求值与严格单参绑定：保原数值快路，修额外参数/星号被遗漏的现有错误，8项runtime、3项真实driver及完整门禁已通过，已进入main且Ubuntu/macOS普通CI通过（[ADR-0054](docs/adr/0054-abs-call-binding.md)）。
- [~] `float` 完整参数绑定与有限冷协议/文本转换：原额外参数及副作用被遗漏已修，保单数值快路，16项runtime、3项真实driver及完整门禁已通过，待远端验收（[ADR-0055](docs/adr/0055-float-constructor-binding.md)）。
- [~] `bool` 严格绑定与普通实例共享真值长度校验：不改数值/容器热分支，负长度和索引长度语义12项runtime/3项真实driver与完整门禁通过，待远端验收（[ADR-0056](docs/adr/0056-bool-binding-and-length-truth.md)）。
- [~] `len` 冷长度结果与bool共享校验：保exact容器/primitive热路，补结果对象一次`__index__`、负长度与上界错误、len自身绑定/缺槽现场；源审及新10＋旧bool12项runtime、新len3＋旧bool3项driver及首次完整门禁通过（786C#/228CP/202e2e＋30原ignored），未发布，不改变通用展开或项目分子（[ADR-0065](docs/adr/0065-len-result-protocol.md)）。
- [~] 显式 `--include-module` 与文件包 `importlib.resources.files(anchor)`：真实包身份/初始化及JIT源定位，AOT仅声明资源根；12项runtime、4项driver和loader/CLI/PE专项及完整门禁通过，待远端验收，不宣称pyfiglet或完整importlib通过（[ADR-0057](docs/adr/0057-explicit-package-resources.md)）。
- [~] `shutil.copyfileobj` 通用流复制：真实原生模块与当前 `COPY_BUFSIZE`，严格绑定、缓存方法和逐块协议；10项runtime与4项driver及完整门禁通过，待远端验收，不等于 `copy` 或整个模块支持（[ADR-0058](docs/adr/0058-shutil-copyfileobj.md)）。
- [~] `str` 注解的对象身份（C0先行阶段）：参数/返回/公开槽透传保对象，未知消费保守动态回退，独立数值/精确字符串结构保留；专项及完整门禁本地已验、未发布，不代表字符串子类布局或pyfiglet通过（[ADR-0062](docs/adr/0062-opaque-str-annotations.md)）。
- [~] `str` 子类有限布局/核心协议/常用方法：真实实例、类发布与公共转换身份，首专项 runtime 36/36、RuntimeBases 6/6、driver 4/4（1 MIR + 3 CP/JIT）本地通过；完整门禁本地通过、未发布，不计项目/AOT/性能通过。自定义new/未纳魔术槽/解码及UTF16排序旧债保明确边界与原CP pending（[ADR-0063](docs/adr/0063-string-subclass-layout.md)、[语义边界](docs/semantics.md#字符串子类有限布局与公共协议)）。
- [~] `optparse` 常用单值选项解析：真实OptionParser/Option/Values、六异常与默认帮助；47项专项C#与真实差分6/6、完整门禁本地已验，不冒完整模块或pyfiglet通过（[ADR-0060](docs/adr/0060-optparse-common-options.md)）。
- [~] 有限动态 `except`：真实模块/函数Name、属性链与tuple在异常后求值，身份匹配/清理及静态数值路径专项通过；原2/4应用失败修复后4/4、结构1/1，正常完整门禁1466C#/228CP/202e2e+30原ignored通过。Call/Subscript、动态生成器及删除旧边界明确保留，未发布、不冒tomli通过（[ADR0085](docs/adr/0085-dynamic-except-dispatch.md)）。
- [~] `types.MappingProxyType` 有限真实只读活代理：31项代理专项+3项旧Enum、四项CP/JIT应用及结构1项、首次完整门禁1497C#/228CP/202e2e+30原ignored已通过；保合法range/复杂比较及消费者边界，未发布、不冒tomli通过（[ADR0086](docs/adr/0086-mapping-proxy.md)）。
- [~] `typing.IO/BinaryIO/TextIO` 有限真实名义类：29运行时专项、四项CP/JIT应用分别通过；根空slots、普通派生、完整方法绑定和当前模块来源已接线，首次正常完整门禁已通过。泛型/完整反射与普通框架旧债保留，不冒tomli通过（[ADR0087](docs/adr/0087-typing-io-runtime.md)）。
- [~] `typing.NamedTuple` 有限真实函数/类语法：复用tuple布局、双遍字段与真实来源、默认值/注解/普通方法和property；160项相关runtime、5项CP/JIT应用＋1结构及HIR6项首次通过；旧Quitter期待窄迁后第二次正常完整门禁1607C#/228CP/202e2e＋30原ignored通过。泛型/生成类派生/复杂注解和descriptor等合法域保pending，未发布、不冒tomli通过（[ADR0088](docs/adr/0088-typing-namedtuple.md)）。
- [~] `getopt` 有限真实解析、GetoptError/error及函数局部稳定原生异常身份：21项C#、PE1、HIR1+4、5项源差分及正常完整门禁920C#/228CP/202e2e+30原ignored已验；一般协议/GNU/用户派生仍未支持，不冒tabulate通过（[ADR0075](docs/adr/0075-getopt-local-exception-imports.md)）。
- [~] `zipfile.is_zipfile` 尾记录识别与真实 `BadZipFile`：EOCD/ZIP64、流位置/所有权及异常协议有限切本地已验、未发布，不含ZipFile/解压，不计项目通过（[ADR-0059](docs/adr/0059-zipfile-tail-identification.md)）。
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

### M5.2 Native AOT 与交付（持续重大主线，承接旧 M1.6）

> 2026-11-02用户要求持续增强AOT性能与代码生成动态适配，目标接近Nuitka级打包体验，而非宣称现有兼容/性能相等。分阶段合同见[ADR0043](docs/adr/0043-aot-delivery-mainline.md)。16/20不再是本主线前置。

- [ ] N1：固定小矩阵覆盖裁剪后动态协议、BigInt、容器退化、ISA分支，严格原期望
- [ ] N2：固定规模JIT默认/禁tier/AOT冷/热配对，记录分配/GC/内存/体积/构建成本，再按证据优化代码生成
  - [x] 首轮5源码/三臂81进程固定基线，保留分配负结果及3独立进程边界（[证据](docs/validation/aot-jit-baseline/README.md)）
  - [x] [ADR0044](docs/adr/0044-guarded-integer-add-chain.md)：actual-I64短Add链有限默认；180进程同runner配对与合并后445/226/200+30门禁、AOT七配置通过，保留失败路径及冷启动负结果
  - [x] [ADR0045](docs/adr/0045-native-module-attribute-factories.md)：13项fresh属性工厂启动实验评估完成，但未达事前门槛，生产回退；保留120进程负结果，不扩大范围追收益
- [ ] N3：通用standalone目录清单、资源/原生依赖收集、许可/哈希/诊断和真实搬迁
  - [x] [ADR0046](docs/adr/0046-native-aot-artifact-manifest.md)：同次SDK/源码文本/实际CIL引用与发布文件hash清单、完整目录搬迁核验已通过三平台；不代表资源复制或动态依赖闭包
  - [x] [ADR0047](docs/adr/0047-explicit-aot-resources.md)：显式资源清单、JIT真实模块位置与AOT相对定位、ILC前数据快照及三平台 `_internal/resources/v1` 整树搬迁（有限验收；不等于自动依赖收集）
  - [x] 固定C ABI机制原型：真实PInvoke发射/读回、JIT共享库与AOT静态archive搬迁三平台通过（[证据](docs/validation/aot-fixed-cabi/README.md)）；未实现公共Python绑定/任意wheel
  - [ ] [固定C ABI Python入口](tests/aot/cabi_probe/PYTHON_BINDING.md)：真实生成模块/一等函数、严格参数/错误/重绑，显式JIT shared与AOT static部署；恢复版三平台固定验收已通过，仍 WIP 待主线准入；旧本地提交不可读，不继承旧验收（[合同](tests/aot/cabi_probe/PYTHON_BINDING_BUILD.md)、[本轮证据](docs/validation/aot-python-binding/README.md)）
- [ ] N4：逐真实OS/架构/ABI扩大平台CI，不把RID字符串当实机通过
  - [x] macOS arm64 / Windows x64 MSVC同宿主真实ILC、搬迁及args/exit/error通过（[证据](docs/validation/aot-host-platforms/README.md)）；SDK仍安装，干净目标机未勾完成
- [ ] N5：静态能力优先，必要外置依赖集中 `_internal`；目录交付成熟后独立评估onefile，不为形式强加自解包，资源寻址/生命周期/性能另验
  - [x] 无资源 smoke 产物的三平台仅exe复制/搬迁四模式验证（[证据](docs/validation/aot-single-executable/README.md)）；复用原ILC，Mac/Win仍同SDK宿主，不代表任意项目onefile
- [ ] B：桥接AOT仍独立验收libpython/扩展/stdlib部署，不因N项通过勾完成

- [x] driver 按 ADR-0005 方案 A 实现 `publish_aot`：Linux x64 glibc 有限首切已通过真实 CLI ILC 与无 dotnet/Python 容器搬迁（[证据](docs/validation/aot-driver-ci/README.md)）；Mac arm64/Windows x64 MSVC有限同宿主已扩验，未知平台、性能全矩阵与桥接不计完成，不直接调用 ilc
- [ ] `ryupy build --aot` 端到端可用，分平台记录工具链、构建时间、体积、启动与行为
- [ ] 原生产物在免装 Python / .NET 的目标环境验证；桥接产物另验 libpython / 标准库 / 扩展依赖，禁止声称零外部依赖
- [ ] 验证异常、模块注册和静态可达元数据在裁剪后仍正确；发布命令与实际 CLI 一致

## 暂缓能力

- [ ] `async/await`、完整 asyncio、线程调度语义、模式匹配：按项目阻塞重新评估，尚无必做里程碑
- [ ] 多重继承 / 完整 MRO、任意描述符、元类、动态代码执行、自定义 import hook、完整 frame / tracing API：保持显式不支持
- [ ] 直接调用 ilc、进程内 netcorehost、GPU / 完整 DLPack：已有交付路径证明必要后再排期

---

## 待定 / 技术债

- [ ] M3.5 JSON 自定义真值选项的惰性/短路副作用（当前bool/int/None以外明确拒绝）；m3_5_json_flag_objects.py保持pending

- [ ] M3.5 JSON bytes 的 surrogatepass 孤立代理码点编码（当前严格解码明确报 UnicodeDecodeError）；独立 pending：m3_5_json_surrogate_bytes.py，见 ADR-0029
- [ ] M2.6 模块中后定义 class 与前方同名 except-as 绑定顺序：m2_6_class_binding_order.py 保持 pending；M3.5 异常别名联调发现，不以改名测试冒充修复

- [x] io.UnsupportedOperation 双父异常层级已在 M3.7 接入有限原生身份与原位 catch 展开，验收见 docs/io-unsupported-operation.md
- [ ] M3.4 剩余长尾：文件描述符/完整编码策略、Windows 实机与退出整数宽度；逐项范围见 docs/stdlib-system.md 和 ADR-0028
- [x] 超过 u64::MAX 的字面量已接入有界正规化与常量池（4300有效十进制位）；四进制/默认值/容量差分及 M3.7 checkpoint3 聚合门禁通过（M3.4 错误退出测试发现），范围与代价见 docs/integer-literals.md

- [ ] `%` 字符串格式化的剩余 flags/宽度/precision/其它转换与 bytes 格式；M3.7 已支持无修饰标量和映射 `%s/%r/%a/%d/%i/%u/%%`，不再将整个能力标成未支持（docs/string-percent.md、semantics §4 #44）
- [x] 类体内引用同类刚定义的函数名：既有 raw namespace 路线经专用 CP/JIT 差分确认，`v = property(getv, setv)`、别名、定义时 defaults/decorator 与方法全局作用域通过；纠正旧 E0106 记录，不计新增生产能力（semantics §4 #45、ADR0067）。
- [ ] `MemoryError` 类型缺失：超大序列重复暂用 OverflowError 代替
- [ ] 动态路径剩余差距（ADR-0014「仍落后」）：异常抛捕（CLR 异常成本）、`str +=` 累加（不可变 string）、
      容器下标 / 迭代的类型分派、5 个以上实参的方法调用
- [x] M3.0 涉及的超长文件已按职责拆分：`codegen-cil/assembly.rs` / `stackify.rs`、`mir/build.rs` / `expr_builder.rs` / `mir.rs`、`hir/lower.rs` / `hir.rs`、`ty/check.rs`；保留介绍与注释
- [x] 既有超长 `mir/call_builder.rs`、`e2e/lib.rs` 已随对应功能拆分到 600 行内；后续扩展仍须先检查职责和行数
- [x] 函数对象的 `__name__` / `__doc__` / `__annotations__` 已接入声明范围，供 wraps 等使用
- [x] 函数 `__module__` 真实成员、定义时捕获与有限 Dynamic 调用来源：本地专项与完整门禁已通过，尚未公开发布；Static 未知来源明确拒绝；后续namedtuple消费者按ADR0077独立验收，不以此来源前置计标准库完成（ADR0076）。
- [ ] 函数对象的 `__defaults__` / `__closure__` 完整反射仍未接入
- [x] `Builtins` 改为带缓冲的 writer（64 KiB），入口包装器退出前 `Flush`；错误报告前先刷新 stdout
- [ ] `Name` 改为字符串驻留（等 profile 证明有必要）
- [ ] 错误码登记表 `docs/errors.md`（每个错误码一个最小复现 + 说明）
- [x] 深递归 `RecursionError` 守卫（M2.2），默认 1000 层，可用 `RYUPY_RECURSION_LIMIT` 调整
- [x] 浮点除零与 floor div / mod 数值边界统一抛 Python 异常（M2.2），并有差分用例
- [x] Phase 1 类型标注约束与 frozen dataclass 值拷贝已解除：M2.1 标注只作优化线索，M2.5 起 frozen dataclass 保留对象身份
- [ ] CLI 的 run 分支当前直接启动 dotnet 子进程；后续收回 driver，保持 architecture.md §2 的编排边界，本次只同步文档和测试

- [~] `html.escape` 有限真实协议：完整绑定、逐步动态replace与延后quote、exact字符串冷/热分路已接线，12项runtime与3应用CP/JIT通过，正常完整门禁994C#/228CP/202e2e+30原ignored已验；unescape/entities/parser及既有高阶协议边界保pending（ADR0078）。

- [~] `itertools.zip_longest` 真实惰性多槽状态、局部sequence/迭代协议及稳定type已接线；内部/公开与StopIteration必要ABI前置累计25项、3应用CP/JIT已过（首Z2失败保留），正常完整门禁1019C#/228CP/202e2e+30原ignored通过。非Stop错误整体终止、不物化输入，pickle/任意descriptor/派生保边界（ADR0079）。

- [~] `re` 条件组有限候选：闭合ASCII编号/名字、数字前向及两支解析已接；283运行时专项、3真实CP/JIT应用与正常完整门禁通过。开放capture等边界、后端差异与成本见ADR0082，不计项目增长。

- [~] `textwrap.TextWrapper` 普通可继承类与动态hook候选：74运行时专项通过，四应用分别通过（首轮2/4保留），完整门禁1411C#/228CP/202e2e+30原ignored通过；严格根快路保旧区间引擎，根高级算法及对象框架旧边界明确保留，见ADR0083。

- [~] `str.format` 通用模板候选：增量字段、命名/位置/自动编号、属性索引、转换、动态规格与对象身份已接内部引擎；累计47项通过，四应用分别通过（首轮2/4保留），正常门禁1458C#/228CP/202e2e+30原ignored通过。沿既有值格式域，不冒format_map/Formatter/f-string统一；见ADR0084。

- [~] `str.startswith/endswith` 窗口、tuple、类型级index与真实str子类提前方法捕获候选：新增23运行时及34旧回归通过，四应用与一结构及正常完整门禁1630C#/228CP/202e2e+30原ignored通过（首3/5保留）；保exact单参路径及明确capture/码点扫描成本，不冒tomli通过（ADR0089）。

- [~] 精确set.clear原地协议与有限迭代size/sticky/finished修正候选：源码已审，47运行时专项与3应用+1结构首次通过，正常完整门禁1647C#/228CP/202e2e+30原ignored通过；同size clear/refill明确NI+CP正例pending，不冒完整set或tomli通过（ADR0090）。

- [~] `codecs` BOM 通用常量族：十四名/五对象、端序与别名/重绑定已接线，首次四项运行时与两项CP/JIT差分通过，正常完整门禁1651C#/228CP/202e2e+30原ignored通过；编码器、registry、增量和流接口另切，不能以导入成功称完整模块或 chardet 通过（ADR0095）。

- [~] bytes正则可变subject阶段A：bytearray与现有连续B/c memoryview的eager匹配及Match活读已接候选，65运行时专项与3应用通过、正常完整门禁1666C#/228CP/202e2e+30原ignored通过；mutable sub/subn的384专项和3应用通过，正常完整门禁1702C#/228CP/202e2e+30原ignored通过；finditer已接候选且401专项/3应用通过、正常完整门禁1719C#/228CP/202e2e+30原ignored通过；replacement阶段F已接有限候选，429专项通过、应用首2/3后模板单独1/1通过（合计三unique），首次完整门禁1747C#/228CP/202e2e+30原ignored通过；view模板仍NI，exact扫描保原路径，共有对象/锁成本明确（ADR0096）。

- [~] bytes/bytearray八项ASCII分类候选：有限扫描/绑定/root与保存方法已接线，23项专项及三应用已通过，正常完整门禁1680C#/228CP/202e2e+30原ignored通过；不含派生布局/Unicode分类，保共享入口成本（ADR0097）。

- [~] bytearray 切片赋值有限候选：bytes/bytearray/现有连续 B/c view 与精确 list/tuple/range、普通与扩展写/空删除、一轮/两轮索引及原位尾段移动已接源码；首次24项C#通过，应用首2/3（无关slice构造器编译阻断）后基础窄迁单独1/1通过，原regex首败源逐字复验通过；首次正常完整门禁1771C#/228CP/202e2e+30原ignored通过。通用 iterable 与复杂 descriptor 保明确边界，不冒整个 API 或项目通过（ADR0098）。

- [~] 内建 slice 公开有限类型：修复ADR0098原构造器失败源；真实类型/构造/只读字段、任意精度indices、有限比较/hash已接候选，21项C#、三unique应用（有效首2/3后定向1/1）和MIR结构通过，首次正常完整门禁1792C#/228CP/202e2e+30原ignored通过。复杂比较/descriptor与旧容器内部切片边界不冒全兼容（ADR0099）。

- [~] builtin delattr 有限公开入口：复用slice与typing.IO两份原编译失败源；真实保存函数/严格绑定、普通属性与property及共享删除guard已接候选，最终23项C#、三unique公开应用和结构通过；首门禁旧IO守卫1项回归修后，第二正常完整门禁1815C#/228CP/202e2e+30原ignored通过。模块、custom hook、一般descriptor与完整元数据保NI；不继续静默绕过未知删除协议（ADR0100）。

- [~] 普通函数直接locals有限候选：依据binaryornot0.4.4当前原入口check.py:23真实E0100，保同调用共享dict/逐源码槽同步及未绑定删除；闭包、generator、间接/模块/类调用与复杂展开明确拒绝。专用字典结构重入guard和动态局部del已接，25C#、三unique应用、7边界及第三次正常完整门禁1840C#/228CP/202e2e+30原ignored通过；前两轮旧拒绝断言迁移保原证据，原binaryornot入口编译已过、JIT首错转为bytes.translate，未计项目通过（ADR0101）。

- [~] bytes/bytearray.translate有限buffer候选：通用256表/删除、bytes无变化self/bytearray fresh、保存/root/PIC与连续B/c view已接；220项独立CP观察、18C#专项及三unique应用通过。严格参数验证后None+空delete的bytes窄快路已接，最终真实CIL两轮72例及第三次正常完整门禁1858C#/228CP/202e2e+30原ignored通过，f3ba26b原binaryornot固定Linux JIT入口CP/compile/JIT各一次通过；重复**关键字旧公共展开差异保pending，用户buffer/宽格式/子类不冒全兼容（ADR0102）。

- [~] 精确list必要基础协议：依据177cd406的docopt原入口list/dict基类阻断，先独立修空切片、负步删除、index缺失、extend/+=流式与hint/失败前缀、耗尽游标；原CP三应用3/3、旧Ryu0/3、首修3/3，最终原源及转正各3/3，正常完整门禁1877C#/228CP/202e2e+30原ignored通过；same-CIL ABBA证实exact来源去全量快照，保小range约10.5%回退与大range噪声。真实派生应用只保CP草稿，不计docopt通过（ADR0103）。

- [~] 真实list单继承有限布局：独立wrapper/payload、构造/root描述符/默认协议与消费者已接，原CP3/3、旧compile0/3、最终六unique CP/JIT应用通过、原三转正复过；独立review四反例保留，三修复严格对齐，一项明确NI；全C#1927通过；真实CIL三轮ABBA去大热路回退，最终小操作仍约2–14%额外与共享CPU波动，分配不增；正常完整门禁1927C#/228CP/202e2e+30原ignored通过。custom核心magic、sort回调、宽slice等明确NI，79a04f7原docopt窗口已实测仅余dict基类E0100、未启动JIT，不计项目通过（ADR0104）。

- [~] 精确dict必要更新基础：原docopt真实dict派生前先拆四项已证缺陷，候选流式pairs/mapping优先/零一位置加kwargs；scalar新协议与旧raw自定义键边界分开，六应用CP/JIT与独立4源及1942C#通过；两轮真实CIL去大固定退化，但大pairs仍慢约9–21%、部分省分配，正常完整门禁1942C#/228CP/202e2e+30原ignored通过。真实dict布局与存储hash问题未开放，不计docopt通过（ADR0105）。

- [~] 真实dict单继承有限布局：依据docopt原dict基类阻断，独立wrapper/payload、根描述符/默认协议/活视图与主要消费者候选已接；scalar键与核心槽覆盖/同尺寸结构变异等明确边界。六主应用初轮4过2败，既有显式super与view-set范围已保原失败后拆pending，v3六主及转正CP/JIT均6/6通过；独立审查foreign view ne与bool/string碰撞已保原失败后修复，2004C#通过，递归保护也已核。两配置同CIL性能已核，32旧路径B/op不增，保默认JIT转换及固定配置iter-first约26%等不利样本；正常完整门禁2004C#/228CP/202e2e+30原ignored通过；c0786d1原docopt编译通过但JIT首败re.split缺属性，仍不计项目通过，见ADR0106。

- [~] 通用re.split：依据docopt新鲜首错，str/exact bytes及连续B/c buffer、双入口绑定、ssize_t/捕获None/零宽重试/确定释放已接；首三主1过2败保原流，旧view/GC边界拆pending后v3三主及转正通过，独审支持域与动态代理源通过。29新C#及正常完整门禁2033C#/234CP/205e2e通过，33ignored=原30+三新边界；同CIL两配置1080性能样本保全部不利值。稳定e066238矩阵默认9/20、字体选项另10/20已核；bd06269原docopt编译成功、JIT转为str.partition缺属性，仍不计项目通过，见ADR0107。

- [~] 通用str.partition/rpartition：原docopt缺属性首错驱动，局部绑定、首末Ordinal/代理对端点与receiver/sep身份已接；三主及独审30观察首轮/撤冗余后均严格通过，12新C#及正常完整门禁2045C#/240CP/208e2e通过，36ignored=原33+三新边界。3060真实性能样本保全部不利，21旧路径B/op不增，但rfind根/find预备等回退仍未排除；56aa6c6原docopt编译成功，JIT转为Argument缺__class__，仍不计项目通过，见ADR0108。


- [x] 通用默认__class__有限读取：真实普通实例/类及PyType元类固定点已接，保用户覆盖/字段优先级且PIC热命中不增分支；11新C#、三主CP/JIT及独立PIC开关探针通过，正常完整门禁2056C#/246CP/211e2e通过，39ignored=原36+三新边界。干净e0a9ac1原docopt0.6.2入口唯一CP/compile/JIT通过；不泛化完整库/全矩阵。300真实性能样本保全部不利，旧type/覆盖路径回退未排除；动态换类、一般反射及真实布局用户覆盖仍未实现，见ADR0109。
- [ ] 字符串公共旧路径性能风险复核：ADR0108保留两候选2304样本与单次ALC756样本；原rfind根/find预备等大回退无法可靠归因，同版本也明显波动。后续独立优化须保全部旧证据，不以正常门禁或新API分配不增代替风险关闭。

- [ ] 普通属性公共路径性能复核：ADR0109 的300样本中type两对约+7.8%/+14.1%，覆盖getter/method及循环有不利值；无法可靠归因，不以PIC热命中未变或分配不增代替无回退结论。

- [x] 真实TypeVar有限基础：真变量身份、构造次序、固定特殊槽及既有alias参数元数据已接；五主CP/JIT、16新C#及独审旁路通过，正常完整门禁2072C#/258CP/216e2e通过，46ignored=原39+七边界。280真实性能样本保全部不利与分配，旧路径时间回退未归因、新变量hash仍有包装成本。Generic保持下一切，不计Markdown通过，见ADR0110。

- [ ] TypeVar及原生特殊协议性能复核：ADR0110默认ABBA280样本保留全部不利，旧10路径分配中位数不变但多个时间回退未归因；新变量hash仍144/208B，需在不读instance extra且保反射/身份的前提下优化，不以功能门禁代替性能结论。

- [~] 真实Generic类与参数化基类：原Markdown3.5.2在TypeVar功能提交aa0c54e仍以Generic[_T]基类E0100编译失败，JIT未启动；同提交原docopt仍通过。TypeVar基础已单独通过。下一步保class/alias真实身份、orig_bases/parameters、类体副作用次序及失败，不擦为object，不以导入或单API成功计项目通过。

- [x] Generic根与TypeVar-only alias独立切：六主CP/JIT及12新C#、独审和正常完整门禁2084C#/273CP/222e2e通过，55ignored=原46+九边界；真类根空布局、参数tuple身份、专用LRU与导出消费次序已验。440性能样本保全部不利，旧构造默认约+6.2%风险未关闭，八旧路径分配不增。参数化基类尚未开放，不计Markdown通过，见ADR0111。

- [x] Generic参数化单基类有限切：真实alias解析、普通family布局、body后orig/parameters与相位守卫已接；五新主及G1原派生边界严格通过，14新C#和独审通过。正常完整门禁2098C#/284CP/228e2e通过，60ignored=旧55减一旧转正加六边界；480性能样本保全部不利，默认custom_new约+1.99%、旧G1风险未关闭，八旧路径分配不增。用户类再订阅/完整hook仍分切；干净a97c720原docopt入口仍通过，Markdown编译首次通过但JIT首错ClassVar导入缺失，仍不计Markdown通过，见ADR0112。

- [x] typing.ClassVar通用有限语义：真实SpecialForm根与有限参数alias、缓存及消费者相位已按CP3.12接入，完整门禁通过；项目与未知域、性能边界分开记录，见ADR0117。

- [x] ClassVar前置SpecialForm有限基础：Optional/Union共用raw key 128项LRU、真实冷Union导出消费、稳定alias args和根类型metadata候选已接；八主CP/compile/JIT及16新C#、独审通过，六条完整未知域/旧缺陷保pending。560样本全部保留，热Union/Optional省320/352B，冷Optional多848B且时间回退；正常完整门禁2114C#/298CP/236e2e通过、66ignored=旧60+六边界。干净0713ed8原docopt仍通过；Markdown CP/编译通过但JIT仍首错ClassVar缺失，不增加项目分子，见ADR0113。

- [ ] SpecialForm冷缓存与反射性能复核：ADR0113唯一ABBA保留Optional冷+35.77%/+87.34%、1232→2080B，以及get_args/type(Union)与旧路径不利值；需保真共享容量、原始键、helper相位与身份，避免以热路径获益掩盖冷成本。

- [x] ClassVar 前置普通 typing 原始缓存：十一普通根共用 raw receiver/parameter 128 LRU，冷建不再归一化驻留；八主 CP/compile/JIT、16新C#与162旧typing专项及独审通过。720样本保留全部不利，List/Dict热省152/128B，但冷与旧路径仍有回退；正常完整门禁2130C#/315CP/244e2e通过、75ignored保旧66加九边界。Tuple/Callable真实缓存留后切，C1未验收回归/WIP独立保留，干净b5e6279原docopt仍通过，Markdown CP/编译过而JIT仍ClassVar缺失；不计ClassVar或Markdown完成，见ADR0114。

- [ ] 普通 typing 缓存冷成本与旧路径性能复核：ADR0114保留默认/禁tier Dict冷+11.44%/+48.55%、默认listgetitem+77.16%和禁tier普通构造+17.26%等；不删离散负样本，不因热路径获益或分配不增宣称无回退。

- [x] typing.Tuple独立原始缓存：真实128项raw LRU、空cold与尾Ellipsis相位及多前缀已接，八主CP/compile/JIT、18新C#与178旧typing及独审通过；十二完整边界pending。880样本保全不利，热省152/128B、尾Ellipsis冷省1184B，但普通Tuple冷禁tier+26.27%及旧路径风险保留；正常完整门禁2148C#/335CP/252e2e通过、87ignored保旧75加十二边界。Callable仍下一切，ClassVar未恢复；干净2ee56fe原docopt仍过，Markdown CP/编译过而JIT仍ClassVar缺失，不计Markdown通过，见ADR0115。

- [ ] Tuple原始缓存冷成本与旧路径性能复核：ADR0115普通Tuple冷禁tier+26.27%、默认type(Union)+13.35%、禁tierTypeVar hash+15.98%等全部保留；不因热/尾Ellipsis冷收益关闭F0/F1a旧成本。

- [x] typing.Callable独立原始缓存：外层exact形状/list快照、inner128项raw LRU及result先检查/args仅转换已接；十一主、18新C#与198旧typing/metadata及独审通过，十五完整边界pending。1200性能样本保全部不利，热省8B但冷增120B、禁tier冷+18.14%、父关系issubclass回退；正常完整门禁2166C#/361CP/263e2e通过、102ignored保旧87加十五边界；干净68220e7原docopt仍过，Markdown CP/编译过而JIT仍ClassVar缺失，ClassVar未恢复、不增加项目分子，见ADR0116。

- [ ] Callable冷缓存与公开类型关系性能复核：ADR0116冷+120B、禁tier冷+18.14%、issubclass默认/禁tier+29.10%/+22.82%、默认args读取+33.70%及旧路径不利样本全部保留；需守住外层list快照、真实raw键与父关系，不以热收益关闭风险。

- [x] typing.ClassVar重新接入：真实根/alias及当前导出消费、共享原始LRU与各消费者相位已接；24完整CP/JIT、38目标C#及426旧回归、六独审源通过。1340性能样本已封存，TypedDict四字段冷建稳定+512B及时间回退明确保留；正常完整门禁2202C#/390CP/287e2e通过、107ignored；干净eed648d原docopt仍通过、Markdown首错前移itertools.count缺失但仍失败；不以功能门禁宣称性能无回退，见ADR0117。

- [ ] ClassVar消费者性能复核：ADR0117四字段TypedDict分配2912→3424B，默认/禁tier时间+18.20%/+78.57%；保全原始样本，在不跳过当前导出或用户比较语义的前提下另切优化，不关闭其它旧路径风险。

- [x] 通用itertools.count有限exact数值语义：真实类型/惰性状态、任意精度整数与逐次浮点相加已按CP接入并通过完整门禁；未知数值、派生、反射和性能边界另记ADR0118。

- [x] 通用itertools.count有限数值惰性候选：exact bool/int/float、任意精度与逐次浮点累加、稳定真实类型/保存根和局部绑定已接。首24C#/四主通过后独审发现纯kwargs超量错误字样，保真红灯后修复，B2的24C#/五主和独审通过；三个完整边界保pending。唯一440性能样本保全部不利，正常完整门禁2226C#/398CP/292e2e通过、110ignored；干净ed7bdb2原docopt仍过、Markdown转为typing.overload缺失仍失败，见ADR0118。

- [ ] 迭代器公开路径性能复核：ADR0118禁tier repeat +15.61%、chain +6.27%与单进程默认zip_longest/禁tier native type不利值全部保留；七共同路径分配不增不代表无回退，count新API仅绝对成本，TypedDict+512B旧风险仍开放。

- [x] 通用typing.overload有限三API：真实稳定函数/dummy、按当前module/qualname/真实首行的有序强引用registry及wrapper接线已实现；六完整公开源、23新C#与独审通过，正常门禁2258C#/426CP/305e2e通过、125ignored保旧115加十边界。唯一420性能样本保六项旧路径时间不利及首次Main新增3312/3056B；干净cf2e28e原docopt仍过、Markdown CP/编译过而JIT首错importlib.util缺失，仍不计Markdown通过，见ADR0120。

- [x] overload前置真实函数定义来源：共享签名冷登记真实词法名/首行，精确字符串__qualname__可改且不被普通字典遮蔽；七完整CP/JIT及独审、正常门禁2235C#/410CP/299e2e通过、115ignored。25份预期CIL逐字归一化/CP来源核后更新；全部失败保留。740性能样本保13项不利与冷分配，干净ab7c65d原docopt仍通过、Markdown首错仍缺overload；不计Markdown通过，见ADR0119。

- [ ] 函数定义来源冷成本与旧路径时间复核：ADR0119默认/禁tier首次限定名写约152/88B，冷初始化有新增成本；默认双规模104.44B/定义仅本组均值，禁tier受单样本额外98352B影响不可作微成本。13个共同路径时间不利全部保留，最高禁tier双compiler生成器创建+19.71%，不因分配不增而关闭风险。

- [ ] overload冷初始化与公共方法性能复核：ADR0120禁tier keyword +11.15%等六项不利、首次Main默认/禁tier +3312/+3056B，以及wrapper公共分派新增检查风险均保留；新API命中800B/miss688B仅为真实生成路径绝对成本，不以分配稳定或default负值关闭风险。

- [x] 真实有限 ModuleSpec 与普通源模块同槽元数据：真实 PyClass/PyInstance、parent/has_location/cached、有限 repr/eq/不可哈希及显式包标记已接；B3 的52项定向 C#、五份完整源同 PE 配新 runtime 的逐字 CP golden 验证通过，两项独审描述符/空format修复已闭环。B4 已用 RegisterSourceLocated 显式区分新槽能力、保旧十参 ABI；20 C#/8 codegen 定向及同旧 PE 配 B4 复验通过，B3 原错完整保留；B4正常完整门禁已过（2281 C#/790 Rust/433基线/433 cases selftest/310编译e2e、127 ignored）；B4真实CIL发现普通global意外Object化并阻断P3，B5类型层窄修定向29types/2真实PE已过，B5最终门禁2281 C#/791Rust/433基线/433cases/310e2e、127ignored已过，B5真实PE/CIL已复核恢复88原字段与全部原读写/前言；唯一16进程/336暖样本/16冷Main性能矩阵完成并保留全部不利；干净889ba9a7原docopt入口通过、Markdown仍缺importlib.util，不计Markdown通过，见 [ADR0121](docs/adr/0121-module-spec.md)。

- [ ] ModuleSpec 后续模块实例边界：旧失败模块对象共享静态槽的别名隔离、模块/全局 del 与删除后 builtins 回退、SourceFileLoader/native/__main__ 混合元数据及自定义 eq 非 bool 结果保持独立边界；两份完整边界源 pending 不删减，不以四字段快照替代完整实例全局。

- [x] 通用importlib.util.find_spec有限发现：真实稳定函数、当前缓存spec原身份、相对名、父包真实初始化和fresh源spec/loader已实现；不执行目标、不写缓存、不挂父属性。54项定向、八份完整公开源及2312C#/791Rust/450CP/318e2e门禁通过，136 ignored、九份完整边界保留。真实CIL独审和唯一12进程/264暖样本完成，原docopt继续通过、Markdown推进到native发现域NI，不冒称实例执行。详见[ADR0122](docs/adr/0122-importlib-util-find-spec.md)。

- [ ] find_spec性能后续：共同PE换runtime首次Main +2008B；禁tier dynamic_defaults +41.34%/+17.77%、plain_def_create +39.17%/+4.80%，default资源−26.01%/+48.44%及所有原样本保留。仅完成测量，尚未归因或证明无回退，见[完整性能报告](docs/importlib-util-performance.md)。

- [x] 真实模块实例基础B1：显式选择源码模块，唯一globals与显式执行context，普通失败import重试新实例；最终真实PE 28+18项、两完整正例及2337C#/817Rust/453CP/320e2e完整门禁通过，137ignored。默认PE仅时间戳不同，runtime97项变化已归因；唯一12进程/204暖样本完成，coldMain+344B及全部不利保留，尚未证明无回退。注解/环境原源pending、函数类与公开loader未开，不计Markdown通过，见[ADR0123](docs/adr/0123-module-instance-foundation.md)。

- [x] 真实模块实例归属B2：显式owner与六份完整正例、最终77+64项真实PE及正常完整门禁2355C#/836Rust/461CP/326e2e通过，139ignored；七MIR红测修复，急切推导式/直接build_class保pending。默认两对PE仅时间戳差异，runtime7改+16新正文全部归因；20进程384样本完成，明显默认不利未关闭，一次16进程同版本/JIT诊断未得可靠可修因果。未开放Python loader，不计Markdown通过，见[ADR0124](docs/adr/0124-module-definition-owners.md)。

- [ ] B2默认性能风险：method_calls +237.02%/+129.65%、module_bare +26.96%/+97.46%及全部不利保留；同版本对照明显漂移、插桩JIT层级不同，但不能反推原P3原因。一次有界诊断后停止，尚无可靠可修结构证据；不以暖分配/ABI/CIL相同或新实例收益关闭风险，见[完整报告](docs/module-owners-performance.md)。

- [ ] 模块实例性能后续：default静态调用、普通def/closure创建、资源路径两对不利，禁tier plain_def_create第二对+55.25%而第一对−9.25%；首次Main+344B。明确注册/异常布局与共享调用增量，但未做因果归因或关闭历史回退，见[完整报告](docs/module-instances-performance.md)。

- [ ] 原Markdown发现域前置：B2原入口仍在find_spec('html.parser')明确NI。已核native html只有escape，不存在真实html.parser；不添加假metadata/loader。固定 CP3.12.13 未改四文件窄闭包的旧 del re 首错经通用模块删除解除；6502e10 后单次 compile 新首错为 html/parser.py:21:25 的 re.compile 环境名字守卫，另有 super 等共 21 条 E0100。原 Markdown 同 PE 校正 JIT 仍是旧 find_spec NI，公开执行未到达，不计项目通过。

- [x] 公开编译模块实例执行首切：module_from_spec/create_module/exec_module接真实唯一globals、原spec/path和loader捕获代码，支持None非包与跨代码目标。6完整正例、2423 C#/900 Rust/533 CP/358 e2e通过，179 ignored；9新增完整边界保留，默认Markdown未过（ADR0129）。
- [ ] 公开loader长尾与成本：自定义spec/loader、namespace、ModuleType、静态目标/源码、资源clone/动态磁盘源与模块属性新分支/冷能力成本未完成；不把公开创建或通用importlib导入当全兼容。
- [x] html.parser有限lookbehind首切：通用无内部捕获的固定码点宽后顾，保完整原模式；2474 C#/900 Rust/549 CP/365 e2e通过，188 ignored。7新完整正例、9新完整pending，默认Markdown未过，见ADR0130。
- [ ] lookbehind长尾与成本：内部捕获/引用/条件/嵌套断言/显式边界、CP极大数精确异常、D009和parser冷成本未完成；不把有限后顾当完整re兼容。
- [x] sys.modules新模块别名首切：受限空闲顶层名挂真实模块，保唯一globals与原来源，不改Import ABI；2519 C#/900 Rust/578 CP/370 e2e通过，212 ignored，5新完整正例与24新pending，见ADR0131。
- [ ] 完整可写sys.modules及成本：替换/删除/None/任意值、生命周期收尾、父包/fromlist、真dict、动态spec协议与精确setter错误仍开放，新增冷路成本未采样。
- [x] XML Element有限原生对象首切：真实对象/字段/子节点/迭代、ElementTree/getroot、Comment/PI/QName，22完整场景9正13pending；2538 C#/900 Rust/600 CP/379 e2e通过、225 ignored，见ADR0132。
- [ ] XML后续与成本：真实TreeBuilder、序列化/解析、写删children、clear/extend/remove、完整协议与repr、warnings basename旧债；现有分配/keys扫描和历史性能风险未关闭。
- [x] 精确原生OrderedDict首切：独立双序载体、标量键、streaming更新、重排/游标/比较、dict只读根及公开序消费者；最终003隔离2577 C#与31完整合同22严格通过/9pending，原13异常setter债不改、独立19 raise-from通过。正常门禁2577 C#/900 Rust/631 CP/401 e2e通过、234 ignored；本切验收不等于远端发布，见[ADR0133](docs/adr/0133-ordered-dict-core.md)。
- [ ] OrderedDict普通路径性能风险：唯一默认ABBA的getitem两对+15.279%/+7.239%、setitem+5.629%/+5.546%；contains首对+29.626%，equality第二对+36.812%，反向与全部样本保留。无分配/GC/OD自身吞吐数据，未归因、不宣称无回退，不关闭S1/B2等历史风险，见[完整报告](docs/ordered-dict-performance.md)。
- [ ] OrderedDict长尾与下一项目首错：Python派生/属性、非标量键、dict根写入、union/GenericAlias、完整视图/复制等继续pending；原窄Markdown已越过OD至缺typing.NoReturn，仍未通过，默认find_spec NI独立。下一切先核NoReturn真实语义，不以导入占位或删除extra替代。

- [ ] ModuleSpec 性能后续：共同源首次Main Bold +7288B、Bnew +17648B冷分配；旧PE仅换runtime的default resources.files +28.89%/+25.01%尚未归因，Bnew module_external禁tier +12.62%/+1.31%及其它不利配对全部保留。按[完整性能报告](docs/module-spec-performance.md)另切定位，不以Bnew资源收益或新API成本抵消既有风险。

- [x] 通用模块裸名/global删除：typed绑定位、动态公开绑定与真实实例删键已接；8完整正例、最终82核心及正常2377C#/861Rust/476CP/334e2e门禁通过，146ignored。7完整边界保留；三对无del PE仅时间戳变化，真实四种typed删除CIL通过。唯一20进程288样本完成，默认大幅不利未关闭，见[ADR0125](docs/adr/0125-module-name-deletion.md)。

- [ ] 模块删除性能风险：default module_bare +4.002%/+185.153%、dynamic_defaults +23.124%/+2.126%、generator +22.518%/+14.675%及其它不利保留；规范CIL/分配相同不能解释时间差，按唯一有界窗口停止，未归因。首次新builtin fallback整段3,103,080B也单列，见[完整报告](docs/module-deletion-performance.md)。

- [~] 实例模块环境名字准确准入：compile/exec/eval 属性标识符已与真实裸名区分；4e0bf96 后未改四文件闭包19条属性误报消失，只剩 parser.py:128/141 两处super E0100。动态代码/其它环境协议仍未开放，继续真实class cell与super，不伪造loader；原21条旧失败仍保留。S1最终已清除余下两处super诊断，原窄闭包compile0、JIT下一阻塞为module_from_spec缺失；默认原Markdown仍find_spec NI。

- [x] 实例代码同名属性最小准入：compile/exec/eval 的属性标识符与环境裸名分开；17 项 HIR 专项、独立窄完整程序最终 PE/JIT 及正常2377 C#/866 Rust/479 CP/335 e2e通过，148 ignored。原两完整源仍因旧点调用顺序/缺失builtins模块失败并保留pending，runtime/MIR/CIL源码未改（ADR0126）。
- [ ] 通用动态点调用求值顺序：ordinary_attributes 完整源实际在 property 查找/实参顺序断言失败；后续精确 list 缺失属性顺序尚未执行。须兼顾提前捕获与现有 PIC 快路，不能靠扩大捕获后忽略分配/时间成本。
- [x] S1 限域真实实例零参 super：普通非生成器零参值调用使用真实类free cell与当前首参，shadow回原PIC；9新正例及旧S0super1例转正，16新边界保pending。最新2409 C#/900 Rust/518 CP/352 e2e及真实PE24通过；原窄闭包编译清除两处super阻塞、JIT到module_from_spec缺失，默认Markdown仍未过。见ADR0128。
- [ ] S1性能风险：一次14进程/144样本保留9条不利，default普通实例零参+6.071%、closure+6.971%，tieroff类body+10.579%、module_bare+10.136%；旧S0风险不关闭，未归因，不以相同分配或局部CIL修正声称零成本（lexical-super-performance.md）。

- [x] S0 实例词法类 cell：7份完整正例、真实PE32项及2381 C#/887 Rust/493 CP/342 e2e通过，155 ignored；7份边界保留pending。24步准备与唯一10进程/144样本观测完成；默认method_calls +310.450%等不利、冷反射额外CIL及未解析审计范围保留，不宣称无回退，不开super或假loader（ADR0127、class-cell-performance.md）。

- [~] P1普通动态模块环境隔离试点：HIR充分条件与一次编译计划复用InstanceGlobals，实际计划/发射原因可审计，完整CP/PE与旧显式边界持久回归；默认关闭feature。最终make check/完整20与性能证据独立记录，progress/AOT晋升条件未解除，见ADR0134。

## 2026-11-07：a485后的ForEach实验重建

- [~] 从持久恢复的a485重建默认关闭的严格ForEach局部holder；保持P1原模块族和旧运行时。
  新focused MIR/driver、独立CP与PE、完整make check、固定20同P1新flag off/on均须重新运行；
  历史f064计数不作为本轮结果。独立私有备份、完整Git bundle与最终验收记录待完成。

### 普通模块环境后续竖切

- [~] 显式普通实例相对from-import：结构化能力、运行期metadata与完整fromlist合同（ADR-0136）；自动集合不变，完整门禁待验。
- [ ] 普通非生成器Name except读取真实词法环境，独立验收。
- [ ] 前置通过后再显式迁移原wcwidth计算模块，包/表保旧族，不增加原项目分母成绩。
- [~] 显式普通实例Name/bare异常链：真实LEGB/捕获builtins、来源化MIR与as清理（ADR-0137）；显式local/cell/free del仍负例，完整门禁待验。
