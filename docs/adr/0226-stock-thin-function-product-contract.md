# ADR-0226：显式 stock 普通函数的 core / handle 产品接线合同

- 日期：2026-11-17。
- 状态：A closure＋直接 GC owner 的内部 Host 候选两版通过，统一审查与完整检查已完成；
  Host NativeAOT 已验，编译器资格、产品搬迁、正式性能及默认晋升继续验收。候选默认关闭。
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、
  [ADR-0223](0223-stock-libpython-bounded-glue.md)、
  [ADR-0224](0224-cpython-thin-function-core-handles.md)、
  [ADR-0227](0227-stock-thin-function-metadata-probe.md)。

## 1. 有限 profile 与静态 ABI

首切只实现内部显式 Host factory：普通真实 `def f(x)`，单个位置或关键字 x、无默认值/
kwdefaults/业务 closure/装饰器/协程/生成器/类描述符。业务参数和结果为动态对象，保留
身份、别名和异常；Int64 运算只属于独立回收首证。FastAPI 原入口/版本要求保持。

```text
object? Body(IStockThinFunctionLease environment, object? argument)
```

core K 只接受单一无捕获静态 CIL 目标；lease 为隐藏环境，不出现在 Python 签名中。
编译器资格尚未接入；后续由 driver 冻结归属，HIR 明确诊断，MIR/codegen 发射静态目标。
当前真实 Host Runner 的静态 C# body 已验证 provider ABI，用户业务编译闭包仍待推进。

## 2. 所有权与真实元数据

```text
F -> globals D                   H -> 独立 owned F
F -> closure/cell -> P -> GC owner -> F
                              owner -> core K -> weak H
```

选择 A：只编译一次固定 factory/f 模板，提取嵌套 code，创建时公开函数/closure API 组装，
不执行 factory；实例 P 只在 closure 中。B/C 常量方案退出，原因是 stock GIL code 无
GC traversal；raw-F watcher 方案退出，原因是 weak callback 分配失败可缺席。见 ADR-0227。

owner 精确 traverse F/heap type；clear 幂等清 F；dealloc 在任何 DECREF 前摘借用登记链，
然后清字段、单点释放 core token，释放对象与 heap type。native context 仅有 token/
登记链，不强持 Python 值。K 不持 Host、F、D、owner、lease、参数、结果或公开 H 强引用。

H 私有 owner 持 owned F，终结器只入现有 Host 释放队列，GIL drain 真正 DECREF。
弱 H 缓存跳过 disposed H，重建 H 对应同一真实 F；不将 H 放进普通 Host Roots 强根表。
P、P.__self__ 或 cell 外逃按 CP 强引用延长 F/D/K 生命期，释放后循环 GC 收集整环。

F 是 name/qualname/module/annotations/属性字典/globals 的唯一权威；H 直接转发。
独立定义来源保存 entry ID、模块、路径/行号；真实 code/frame/closure 属于固定胶水，
禁止用业务 CodeType 冒充胶水。完整反射按真实消费者继续扩围。

## 3. 调用、名称查找与动态失效

公开 H 调用先转换实参，再验证当前 F/closure/P/owner/static target/默认值状态，随后走
真实 F 的 CP binder。参数转换可触发用户回调，故验证放在转换完成后；native dispatch
再次核对 owned caller frame 的 code/globals 与当前 F 环境。错误供参在 CIL body 前失败。

closure 验证先取当前 owned P 再检查静态目标及 self owner，禁止解引用旧 borrowed P。
默认值任意非 None（包括空 tuple/dict）使本有限合同失效，调用抛 NotImplementedError；
恢复 None 后可用。原生 audit 在赋值前拒绝已登记 F 的 code setter，普通函数对照放行；
audit 安装要用自定义事件验证实际安装，不能只检查 AddAuditHook 返回值。

真实 closure cell 可写。CP 直接 F 在 cell 改为 abs 后保留 Python 行为，原 native 调用数
不增；H 明确拒绝已失效的静态 core，恢复原 P 后可用。同 code/同 globals 的复制和高级
C API 消费者仍需独立合同，当前 frame 核验不作为全局执行封锁。

LoadGlobal 先查真实 D，再查 **F 定义时捕获的 __builtins__**；两处缺失才 NameError。
D 的 __builtins__ 重绑只影响后续 F，原 builtins 容器内修改仍即时可见；globals 遮蔽优先，
None 为真实值。查找/用户 mapping 回调的结果在进入 CLR 前转 owned。

活动 lease 独立保活 F，保存本次参数/globals 值的 CP owned 身份，结果为原值时返回原
CP 指针；H 返回原输入时保留 CLR 原盒，避免 1000L 等标量重新装盒丢身份。lease 数据仅
存在调用栈，不能流入 K。异常保持原对象，清理幂等，所有 reverse P/Invoke 边界捕获异常。

## 4. 失败、GC 与关闭

native make 在成功/失败所有路径消费 core token 一次。创建未发布 F 时取消 GC 跟踪，
组装 owner/closure 后恢复；失败先主动断开 owner/F，再恢复跟踪后 DECREF。
发布前 name/qualname 设置失败也调用 abort 断环，不等待退出清账。

Host 使用 stock 既有活动调用/GIL/owned 队列。关闭进入 Closing 后检查 native owner、
core roots、owned H；有外部根则明确拒绝，恢复 Open，已有 H 仍可调用/释放并重试。
不清 sys.modules 来制造回收零值；全根/队列已清后释放模板/type 并普通 finalize。
完整长进程、线程/atexit 新根、跨平台关闭继续单独验收。

Python __del__ 在 GC 内调用并保存 F、复活后首次 CP 调用/audit/H 回流已通过。
原生 DESTROY 在 clear 期复活可取得已清 globals/closure 的 F，同次 stock oracle 同样
为 `(code,globals,closure)=(1,0,0)`；该高级消费者的完整执行恢复未准入，测试仅观察不调用。

## 5. 源码与实际验证

- `StockThinFunctionContract.cs`：内部静态 core、弱 H 缓存、owner/lease 与 H 协议转发。
- `CPythonStockThinFunctions.cs`：显式启用/创建/回流、动态值身份租约及关闭拒绝。
- `native/stock_function_owner.h`、`stock_thin_function.h`：GC owner、固定 A glue 与静态 ABI。
- `StockThinProvider/`：真实程序集独立 Host Runner；不改变默认 Bootstrap 或编译资格。
- `tests/cases/stock_thin_function_ownership/`：两组源码输入保留 pending。

macOS arm64、stock 3.12.13/3.13.3 各使用自身头/库；最终各四个独立 JIT 进程通过：
普通矩阵（18 owner 析构、41 native calls、40 CIL body 计数）、audit 安装拒绝、外部八个
watcher 槽占满仍能启用、普通 finalizer 复活。每个回收门 roots/owned/weak core/queue 全零，
finalize=0、完整 stderr 为空。各六项托管协议测试通过。

普通矩阵包含 CP-only/CLR-only、disposed H 重建、真实 importlib 重导/失败外逃、动态对象
身份、默认值与 cell 失效/恢复、原异常/CLR 失败、三阶段创建和发布前失败回滚、P/owner/cell
保活、captured/live/rebound builtins、globals 遮蔽/None/NameError 与关闭重试。

普通兼容 shim 在 3.9.25/3.10.21/3.11.15/3.14.7 编译通过，保持 3.9+ 基线；新候选实际 Host
限上述两版。完整 make check 通过：3418 Runtime 测试、3 既有跳过、503 e2e、297 ignored，
Rust/fmt/clippy 与 Python 基线通过；297 包含本候选两组 pending。首轮门禁因仓内 TMPDIR
被源码外输出规则拒绝，环境修正后完整重跑，失败日志保留。

独立双 GC proof 两版各 JIT/NativeAOT 通过（52 owner 析构、172 CIL 调用）；产品 Host
NativeAOT 已独立验证。真实产品删除输入搬迁、正式性能与三平台扩围继续待办。
此前 attrs 入口首切的固定二十项目 60/60、FastAPI/SanBudget 六产品成绩归属已发布
ADR-0225，新的薄函数默认晋升与旧执行路径退出分别验收。


### Host NativeAOT 收口

独立 Runner 增加显式 `--aot`，用 `dotnet publish -r osx-arm64 -p:PublishAot=true`，
provider/Runner 与 ILC/ILLink 告警视为错误；Runtime 仅复制已验 DLL，不重建。
Main 在 Host.Start 前核对 `RuntimeFeature.IsDynamicCodeSupported == false`，发布后
直接运行原生程序。两版各 ordinary/blocked/watcher-full/resurrect 四进程退出零、
stderr 为空，普通矩阵均 18 析构/41 native calls/40 CIL body 计数，全部回收门归零。
默认 JIT 四门复验通过；附加 xUnit 仍明确属于 JIT。

驱动源码与命令见 [StockThinProvider](../../runtime/RyuPy.CPython.Tests/StockThinProvider/README.md)。
日志逐条保留命令/三流/退出码和实际模式，成功失败超时均清理仓外构建。
该 Runner 使用手写静态 C# body，原版 libpython/stdlib 使用固定发行路径；编译器实际
Python body 的静态接线及完整发布闭包继续是下一独立切片。
