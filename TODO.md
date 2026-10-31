# TODO

> 按里程碑组织。每个里程碑是一个「竖切」：贯穿所有层、以 e2e 用例转正为完成标志。
> 推进方式见 [docs/workflow.md](docs/workflow.md)；完成标准见其中的「里程碑验收」。
>
> 标记：`[ ]` 待做 · `[~]` 进行中 · `[x]` 完成

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

## Phase 1：静态强类型子集

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

### M1.6 Native AOT 与 Phase 1 收尾

- [ ] driver：按 ADR-0005 方案 A 实现 `publish_aot`
- [ ] `ryupy build --aot` 端到端可用；记录二进制大小
- [ ] 基准脚本：Fibonacci / Mandelbrot / 矩阵乘法 / N-Body，RyuPy vs CPython，结果写进 `docs/notes/`
- [ ] 补齐矩阵乘法、N-Body 的 e2e 用例（需要时可推迟到 Phase 2 的 list 支持之后）
- [ ] 回顾：更新 roadmap / architecture，把 ADR-0005 改为已接受

---

## Phase 2：渐进动态类型与 `PyValue`（粗粒度）

- [ ] 定稿 ADR-0003（`PyValue` 布局），BenchmarkDotNet 对比方案 A / B
- [ ] `Ty::Dynamic` 与 `MirType::Dynamic`；未标注函数参数默认 Dynamic
- [ ] 动静边界：`BoxToPyValue` / `UnboxFromPyValue` 守卫 + `TypeError`
- [ ] 静态内联缓存 `PyOps.Add(ref site, a, b)`
- [ ] `PyList` / `PyDict` / `PyTuple` / `PySet`；`len`、下标、迭代协议
- [ ] 普通 class（属性字典）

## Phase 3：中端优化与高级控制流（粗粒度）

- [ ] 决定数据流分析框架（迭代 gen/kill 或 SSA 视图，见 ADR-0002）；引入 `petgraph` 计算支配树
- [ ] 流敏感类型收窄（`isinstance`）与自动拆箱
- [ ] 逃逸分析与临时元组标量替换
- [ ] 闭包转换（DisplayClass）、lambda、`nonlocal`
- [ ] 生成器状态机、推导式、`async` / `await`

## Phase 4：NumPy 互操作（粗粒度）

- [ ] 进程内嵌 libpython，加载 C 扩展
- [ ] Buffer Protocol → `NdView`，下标访问编译为指针运算
- [ ] DLPack

## Phase 5：长尾语义与工具链（粗粒度）

- [ ] 溢出升级 `BigInteger`
- [ ] MRO、描述符、元类、版本号失效的内联缓存
- [ ] 异常体系映射与 `try` / `except` / `finally`
- [ ] 直接调用 `ilc`（ADR-0005 方案 B）

---

## 待定 / 技术债

- [ ] `Builtins` 当前每次写都经过 `Console.Out`（自动 flush），大量输出时很慢：改为带缓冲的 writer，进程退出前 flush
- [ ] `Name` 改为字符串驻留（等 profile 证明有必要）
- [ ] 错误码登记表 `docs/errors.md`（每个错误码一个最小复现 + 说明）
- [ ] 深递归 `RecursionError` 与 .NET 栈溢出的差异（陷阱 #11）
