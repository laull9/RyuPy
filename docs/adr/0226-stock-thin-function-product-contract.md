# ADR-0226：显式 stock 普通函数的 core / handle 产品接线合同

- 日期：2026-11-17。
- 状态：候选设计与独立托管适配层；产品接线待办，默认关闭。
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、
  [ADR-0223](0223-stock-libpython-bounded-glue.md)、
  [ADR-0224](0224-cpython-thin-function-core-handles.md)。
- 并行边界：ADR-0225 的 attrs 入口由主代理独立验收；本轮只增加新文件。

## 1. 首切资格与静态 ABI

首个显式 profile 仅接纳编译器已冻结归属的 stock 模块内普通 `def f(x)`：单个名为 x 的
位置或关键字形参，无默认值、kwdefaults、业务 closure、装饰器、生成器、协程、类描述符。
模块 body 与函数业务 body 均须有已登记 CIL 目标；测试控制面按独立域登记。
业务参数与结果使用 Python 动态对象语义，保留身份/别名/异常；Int64 转换只属于 ADR-0224
首证。本轮不缩减 FastAPI 的版本或已有消费者要求；FastAPI 完整合同继续单独推进。

首切选择单槽 ABI，让真实胶水 `f(x): return p(x)` 自行完成 CP 参数绑定。keyword `x`、
缺参、重复参及额外参由真实 F 的 CP binder 处理。H 的内部 Invoke 收到已经绑定的一槽，
尚无公开通用 Call ABI；未来 H 通用调用仍先经真实 F 绑定。静态 CIL body 形状为：

```text
object? Body(IStockThinFunctionLease environment, object? argument)
```

Body 必须是单一静态目标。lease 是隐藏环境，不能出现在 Python 签名里；不得由旧
PyFunctionOwner/Context 生成闭包 thunk。codegen 只能从 MIR 显式操作发射固定调用；
driver 冻结资格，HIR 做明确不支持诊断。默认、kwargs、任意形参名按后续合同扩围。

## 2. 引用图与唯一权威

沿用 ADR-0224 的双端所有权，按 ADR-0227 主代理实证选择无 closure 的 B 候选：
`D -> F -> 固定 glue code/co_consts -> P -> capsule -> 强 core K`，
`capsule -> weak F`、`K -> weak H`、`H -> 独立 owned F`。
K 只保存不可变定义来源和静态目标，禁止持 Context、owned CP 值、owner、lease、绑定参数
或调用结果。H、瞬时调用栈及释放队列才可以持 owned F。禁止在 Host Roots/静态表中登记 H。

F 是公开元数据的唯一权威：name/qualname/module/annotations/属性字典/globals 均直接访问
F；H 不做镜像。`__module__` 修改不改变定义来源或旧 D。定义来源另保存 entry ID、原模块、
源码路径/行号，用于报告和编译归属。源码/code/frame/closure 如实显示固定胶水；业务来源
可以由独立 provenance API 提供，禁止构造冒充业务的 CodeType、cell 或 frame。
反射能力按真实消费者逐项准入，不声称首切已满足任意 inspect/source/traceback 使用。

native make 消费 core root token（成功失败都恰好释放一次）；组装 F/固定常量 P/weak F 时
不执行 Python factory。完整初始化后才恢复 GC 跟踪并发布，失败销毁前同样先恢复跟踪。
F 的真实 globals 和定义时捕获 builtins 是唯一环境；调用不能按模块名称重新查 sys.modules。
删除缓存后新导入建立新 D，旧 H 的 F 继续保留旧 D；失败事务使用 stock importlib 回滚。

## 3. 调用租约、回流与失效

provider 在 GIL 下完成 native F 身份验证，取得原 core 后再查询其弱 H 缓存；不能仅凭
CP 地址、公开模块名或可写属性签发 core 身份。命中活 H 返回原对象，失效才分配新 owned F。
跨解释器、外来函数、dispatch、默认值或 code 形状变化均拒绝该候选合同。

每次 H 调用及原生 dispatch 均核验：宿主仍 Open、真实 F 与 core 关联、固定 glue code、
唯一常量 dispatch/胶水形参结构、无 closure、defaults/kwdefaults 仍为空。
不能只在初次导出或回流时查。
__name__/qualname/module/annotations/属性字典的合法写入每次反射即时可见，不触发执行替身。
形状失效抛明确能力异常且本次不进入 CIL body；恢复原形状后允许重新验证成功。

租约顺序：登记活动调用 → 取得 GIL → 独立保活 F → 验证 → 调用 → 释放活动引用/GIL →
退出活动调用。任一步失败都回滚。对 D 中值读取后立即转 owned，再进入转换/用户回调；
严禁跨 `__index__`/协议调用借用字典值。lease Dispose 要幂等且无异常，释放过程保留正在
传播的异常身份。活动调用已开始后允许用户回调修改 F，影响后续调用；本次已选静态体完成。

首切公开 H 调用统一走真实 F，保留 trace/profile 的调用可见性。新文件中的 H.Invoke
仅是未来直达 seam，尚未连接公开调用器。直达晋升须证明 binder、元数据验证、trace/profile
切回、异常归属和所有权；任何失效都走明确拒绝。禁止调用原 Python 业务函数作为补偿。
不能凭跨调用版本号代替实时核验：stock 的所有公开变更并无统一可拦截版本计数。

**必须补上的 F 归属合同**：替换 `F.__code__` 后，调用可以不再进入 P；closure cell 也可写。
仅在 P 或 H 校验不足以证明该 F 的后续行为。显式 profile 启用前须验证支持的 Python 操作
和原生消费者合同，明确哪些元数据变更保语义、哪些在变更或执行前以能力异常拒绝。
公开 audit 的赋值前事件、不可写的固定胶水载体与调用校验分别实证，不能由其中一项推导
其它项覆盖。高级原生写接口按消费者另验；原生扩展仍须遵守上游 C API 的语义约束。

本门限定已登记 F、静态 CIL 目标及精确支持的 API，完整胶水清单与未知帧计量另验。
按 ADR-0223，不重新要求全解释器零帧或把自定义 libpython 作为主线前置依赖；现有观察器
仍如实报告 complete=false。profile 在上述元数据合同和所有权验证完成前保持关闭。
两版公开 API 元数据与原生回收实证见 [ADR-0227](0227-stock-thin-function-metadata-probe.md)；
原始 A 的可写 cell 对照促成 B 的选择，创建和内存成本仍需产品测量。

## 4. 双端 GC 与 stock 关闭

owner 的终结器只入队 owned F，宿主 Open/Closing 时由 GIL drain 真正 DECREF。
core/capsule 不持宿主强引用，释放回调通过静态 token 表删除唯一 core root；token 表
条目只含 K，禁止偷偷把 owner/Host 存进去。CP-only、CLR-only、回流重建、失败外逃、
新旧导入和双端释放均须在 Py_FinalizeEx 前通过真实原生析构和弱观察验收。

stock 尚缺完整主动关闭协议，采用显式阻塞关闭合同：Open → Closing 后禁止创建/回流/
新调用，等活动租约退出，撤销宿主持有的模块注册强根，只对仍匹配原 module 身份的候选
sys.modules 缓存解除登记，再反复 CLR GC/drain/CP GC 到稳态。外部 H 或其它 owned CP 值仍
活着时返回“关闭受阻”并保留解释器，允许释放已有引用与重试；不得 finalize 活跃 H。
只有 owned 根、待释放队列、活动调用、native core link 与 core root 全回基线才进入 Closed。
drain 必须计入 DECREF 导致的重入；Closing 期间的新函数构造和 atexit 新根明确拒绝。
这套状态机仍需产品实现；当前 proof 只证明全根先清零后的普通 finalize。

## 5. 本轮源码与验证层次

- `runtime/RyuPy.CPython/StockThinFunctionContract.cs`：内部中性 owner/lease、静态 core、
  弱 H 缓存、每调用验证及异常路径租约清理；只有 System 依赖，没有注册器或 P/Invoke。
  暂放 provider 内部以保留 Runtime 公共 ABI，接线窗口再决定接口的最终程序集边界。
- `runtime/RyuPy.CPython.Tests/StockThinFunctionContractTests.cs`：静态目标拒绝、缓存同身份、
  活字典读取 seam、已持 H 变更拒绝、异常同身份/清理、core 弱缓存重建。
- `tests/cases/stock_thin_function_ownership/`：两组手写 pending 正面输入及真实宿主矩阵。

托管测试里的 owner 是协议替身，没有 CP 引用，故产品根计数/原生析构/双 GC 状态仍待实测。
本轮不运行长编译、性能或原 proof，也不修改并行 ADR-0225。默认开关尚未提供；所有调用
入口均未接入本候选。JIT/AOT、删除输入搬迁、CP 3.12.13/3.13.3 及三平台继续分别验收。

## 6. 请求主代理的最小接线窗口（本轮未修改）

先完成 provider/shim、F 元数据合同与生命周期，再开放编译器资格。

| 阶段 | 最小既有文件 | 需要的修改 |
| --- | --- | --- |
| owned 身份桥 | `runtime/RyuPy.CPython/CPythonValues.cs` | FromOwned 在通用代理前识别已登记 F；ToPython 返回 H 所有的原 F；弱缓存复用 |
| 静态 native ABI | `runtime/RyuPy.CPython/native/ryupy_cpython.c`、`runtime/RyuPy.CPython/Native.cs` | include 新独立头与固定 make/acquire/validate/call/release ABI；业务不得落 C |
| 宿主生命周期 | `runtime/RyuPy.CPython/CPythonHost.cs` | 安装 owner/队列/活动调用状态、显式创建及 drain/关闭拒绝；辅助逻辑新增 partial 文件 |
| F 元数据合同 | 优先独立 native 头和公开 API；当前观察器保持原覆盖口径 | 按精确版本验证 code/closure/默认值变更与真实消费者，未验则保持 profile 关闭；不复用全局严格零帧门 |
| Runtime 中性调用 | `runtime/RyuPy.Runtime/PyFunctionOwner.cs`、`PyFunctionMetadata.cs` 及实际通用调用分派文件（窗口前精确盘点） | 新建独立 H 类型和 provider 合同；移出 owner 的强 context 边，反射直读 F；禁止复用旧 owner 导出 |
| 显式编译归属 | driver 归属计划、HIR MakeFunction 与 MIR/codegen 固定引用表（待前述 ABI 冻结再定清单） | 显式 stock 普通函数资格、静态体与中性环境操作，逐阶段诊断 |

第一窗口只需上表前三行的四个确定文件，加独立新增实现；涉及 Runtime 与 Rust
的窗口需主代理先确认实际分派位置和 ABI。不得修改 ADR-0225 的 attrs 资格、入口守卫或
既有 PyFunctionOwner 行为，也不得操作 `tests/fixtures/projects`。

最终晋升须专用宿主矩阵、原项目回归、四组合执行与回收、完整 stock 关闭、正式性能门及
make check 全部完成；默认晋升和旧路径退出分开验收。首个子代理交付未提交，后续由主代理
逐合同验收和提交；元数据原生实验独立进行。

## 7. 本轮实际验证

两组 pending 输入以固定 PBS CPython 3.12.13、3.13.3 分别运行，四次退出零且 stderr 为空。
live_reload 特别保留异常 traceback 时旧 F 仍活着，释放异常后 CP GC 才回收；避免忽略真实
frame 对 globals 的合法持有。产品输入仍为 pending。

仓外临时 net10.0 测试项目只链接上述两个新增 C# 文件，启用 warnings-as-errors 与 AOT
分析器，五项托管测试通过。机器只有 .NET 11.0.0 共享运行时，首次 testhost 因缺 net10
运行时退出；设置 DOTNET_ROLL_FORWARD=Major 后在 .NET 11 JIT 完成测试。未做 NativeAOT
发布、真实 CP owner/shim 测试或完整 make check。临时项目已清理，产品 obj/bin 未触碰。
