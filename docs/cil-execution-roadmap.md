# 全 CIL 执行与 libpython 底层化路线

- 日期：2026-11-16
- 决策：[ADR-0208](adr/0208-cil-execution-libpython-kernel.md)，内核与胶水边界由 [ADR-0223](adr/0223-stock-libpython-bounded-glue.md) 修订
- 状态：C0 完成；C1–C3 已有定向验证子合同，C1–C5 整阶段完成门继续待办；本轮完整检查、六版本产品与性能复验通过
- 本次授权：在独立分支完整开发；Gemini 4 已停止使用，后续子代理改由 GPT-6.1 Sol 承担

2026-11-16 用户接受少量必要 Python 胶水。主线改为原版 libpython 正常初始化、静态 shim
与现有原生 finder/loader，保留必要上游 importlib/codec；自定义内核转为可选严格研究。
下列 C0–C5 按此执行，历史零字节码探针与已验子合同继续保留各自证据。

实施证据：[执行链与内核选型](cil-execution-inventory.md)、
[项目与性能回归合同](cil-execution-baseline.md)、
[严格生命周期门](../tests/cil_execution/README.md)、[首轮集成验收](cil-execution-first-slices-validation.md)、[第二轮集成验收](cil-execution-second-slices-validation.md)。
当前帧审计显式候选仅支持固定 3.12.13 私有 ABI，默认兼容产品继续维持 CPython 3.9–3.15；
严格内核原型与审计工具的版本范围分别登记，不能改变产品最低版本或替代已有多版本验收。

## 1. 最终目标与当前基线

应用、纯 Python 标准库和第三方 Python 包的函数体全部编译为 CIL，由 JIT 或
NativeAOT 执行；ADR-0223 清单内的必要启动、导入、编码与清理胶水可由原版 libpython 执行。
异步、对象模型和高级反射由生成的 CIL 与 RyuPy.Runtime 实现。libpython 承接
对象 ABI、内存/引用管理、异常、原生扩展及上述必要服务；原生扩展继续使用机器码，
C shim 封装静态 C API、槽位和回调边界。

当前实现仍包含源码委派、嵌入 Python 辅助函数、CPython 标准库执行及随包源码元数据。
2026-11-16 的兼容性成绩保留为迁移基线，详见[真实项目验收](../tests/general_projects/README.md)。
候选路径逐步收回执行归属，现有兼容模式维持独立回归，最终切换与删除旧代码分别验收。

最终产品的完成条件：

1. 应用与第三方业务函数体由 CIL 执行，依赖闭包、回调及异常路径均覆盖；必要胶水逐项登记。
2. 业务源码执行载荷退出已完成迁移的模块；原版发行中的必要胶水、资源与许可按真实闭包分发。
3. 产品启动、运行和关闭分别记录执行归属、胶水调用与性能，胶水新增须有具体用途和验收证据。
4. 本地导入、动态导入、原生扩展反向导入统一遵守冻结归属；复用上游 importlib 锁与协议。
5. 数据、许可证、Tcl/Tk 资源与原生依赖按实际闭包部署，维持删除输入后的独立运行。

编译机上的静态收集工具、CPython 即时 oracle 与测试驱动可继续使用 Python。
可选外置调试源码单独管理；默认最终产品只保留必要的签名、位置及调试元数据。
源码加密、改扩展名、压缩源码或改发 `.pyc` 均保留 Python 执行归属，按胶水或兼容委派实际记录。

## 2. 目标分层与职责

```text
driver 冻结完整依赖闭包、执行归属、能力需求和分发资源
    ↓
syntax → HIR → 类型旁表 → MIR → CIL
                       协程/异常/类语义在中端显式化
    ↓
RyuPy.Runtime：模块、函数、类、描述符、反射元数据、帧、协程和调度
    ↓ 中性协议与静态回调
RyuPy.CPython / C shim：对象 ABI、GIL、引用所有权、原生扩展加载与槽位
    ↓
libpython 底层能力及原生库
```

现有 crate 依赖规则继续有效。codegen 仅发射 MIR 已明确的操作，不扫描 Python 包、
选择 provider 或补做语言语义。PyObject、GIL、指针和引用计数保留在互操作组件/shim。
新增互操作使用声明式静态 P/Invoke，明确 UTF-8、bytes、引用所有权和关闭顺序。

Python 动态类与函数优先表示为运行时对象和元数据，操作由已编译的通用 CIL 方法完成。
仅在证明布局、身份、重绑定与缓存失效合同后，才选择专门化 CLR 类型或直接调用。
一次执行只有一个模块初始化权威和一套对象状态，镜像对象维护明确的身份映射。

## 3. 阶段顺序与完成门

2026-11-16 子合同进度与整阶段状态分列如下。首轮提交及性能结果仍保留原提交口径；
本轮 CreateSuper 公共 Object 返回 ABI 已恢复，`5d36db1e` 的 1843 项公共签名全部保留；
本轮最终检查为 3384 C# / 495 e2e，六版本产品与三形态审计通过；
26 项配对墙钟整体 -0.03%，单项残余成本见第二轮验收。

| 阶段 | 已验证子合同 | 当前整阶段状态 |
| --- | --- | --- |
| C0 | 执行入口、ABI 消费者、启动/关闭链、严格内核选型与基线冻结 | 完成 |
| C1 | object/super helper 移除；原生方法显式候选；两阶段类构造、prepared mapping、重入身份与失败清理；原生 finder/loader 与真实描述符类型 | 进行中；默认 wrapper 与旧源码安装 ABI 保留，完整 PEP451 两阶段模块事务待接入 |
| C2 | 实时默认值；通用 CIL 伴生核心；原生签名反射与三个真实框架消费者 JIT/AOT 搬迁 | 进行中；字符串/延迟注解、完整 code/frame、原复杂项目闭包及源码/资源根分离继续待办 |
| C3 | 默认关闭的 coroutine/await、async with、async for 与有限真实 traceback 视图 | 进行中；完整调度、IO、异步生成器及跨域 awaitable 继续待办 |
| C4 | 原版兼容产品与独立严格内核各有证据；严格原型 22 内核探针和 10 stock 对照通过 | 进行中；主线转向 stock 模块事务、胶水清单与完整业务闭包；严格 Host 源码候选未完成实跑验收 |
| C5 | 保留现有兼容默认与候选隔离 | 待办；默认晋升与旧执行路径退出分别验收 |

### C0：执行归属、启动链与回归基线盘点

- 登记所有源码/字节码执行入口、嵌入 Python 字符串、导入钩子和反向回调。
- 覆盖 `rp_run_entry`、`rp_install_imports`、`_rp_make_class`、`_rp_wrap_method`，
  以及通过标准库、第三方包或 C 扩展间接触发的解释执行。
- 单独核对 libpython 初始化和关闭的 frozen importlib、编码初始化等执行依赖。
  主线允许必要 frozen 启动胶水，记录具体归属与启动成本；零帧仍作为独立严格研究门。
- 评估最小初始化与可重建 libpython 内核发行方案，记录上游版本维护和许可成本。
- 冻结当前项目、API、默认开关、工具链、版本和每个平台已有成功/失败项。

完成门：交付可追踪的调用链、执行归属清单、底层 ABI 消费者和阶段验收方案；
选定严格产品的启动方案后再开始收缩发行依赖。仅扫描 API 名称不足以证明零字节码执行。

### C1：原生桥接与必要胶水收敛

- CIL 实现模块导入与初始化状态机、类构建及元类选择、函数绑定、参数转发和异常转换。
- 静态 C API/原生类型槽承接描述符与回调，替换 Python wrapper/helper 函数。
- 保留真实 CPython 类型/实例的 C 扩展布局、GIL、句柄与生命周期合同。
- 处理 C 扩展对精确函数类型、CodeType、栈帧和内部布局的检查；按真实消费者验收，
  需要的 ABI 适配单独实现，能力缺口返回明确诊断。

已验子合同：[ADR-0209](adr/0209-base-object-native-descriptors.md)、
[ADR-0211](adr/0211-native-method-descriptor-candidate.md)、
[ADR-0213](adr/0213-native-two-phase-class-construction.md)、
[ADR-0220](adr/0220-native-compiled-import-hooks.md)。当前默认内部源码仅有 1 个顶层 helper、
1 个转发闭包，共 2 个 def；finder/loader 两个 class、三个成员与三个别名均已静态替换，
原生方法候选安装源码为空。类构造定向门为三版本各两模式
30 项真实类桥、五个输入的 JIT/AOT 删除输入搬迁，重入输入含默认/候选两模式。
自动 set_name 使用精确专用代理，已有 opaque 身份与迟增能力严格拒绝；普通 MI AOT 保护门保留。
原生导入三版本各两模式 33 项类桥共 198 项通过，两模式 JIT/AOT 删除输入搬迁、包/循环/
失败重试/sys.modules 身份与关闭首次导入通过。完整 PEP451 两阶段事务留待 ADR-0221；
方法候选的精确 FunctionType/code/frame 缺口仍阻断默认晋升。

完成门：内部类构建与热点回调优先使用原生实现，保留胶水逐项说明用途及成本；现有跨域继承、
重入、异常、模块身份、关闭测试和原 SanBudget 编译路径通过。外部纯 Python 库此阶段仍可留在旧兼容域。

### C2：函数元数据、动态对象与反射

- 编译器生成签名、默认值、注解、限定名、闭包 cell、模块归属和源码位置元数据。
  按固定 Python 版本保留注解/默认值的求值时机与动态更新合同，避免只生成静态快照。
- Runtime 实现 `inspect.signature` 所需合同、装饰器解包、描述符、元类钩子和缓存失效。
- 分切实现 code/frame/traceback 的可观察字段、局部变量、异常链和协程帧。
  CIL 字节不能冒充 CPython 的 `co_code`；依赖精确 CPython 布局的消费者另立合同。
- 源码位置、`__file__`、资源根和用户可写数据根独立建模，替换 ADR-0207 的随包 `.py`
  锚点。保留文件相邻资源的可迁移语义，并明确 onefile 持久数据位置。
- `inspect.getsource` 原文查询在默认无源码产品中给出明确异常；外置调试源码可选，
  不以生成近似源码冒充原文。依赖原文或运行期生成代码的框架按实际用途另切适配。

已验子合同：[ADR-0212](adr/0212-live-function-defaults.md) 保留真实默认对象与定义身份，
[ADR-0214](adr/0214-generic-default-function-companions.md) 在冻结的多模块图中对准入 typed 定义
建立通用伴生体；Binder 成功并补齐缺省参数时调用，canonical、签名/defaults 与 def 求值仍唯一。
传递/递归调用、dataclass 普通构造和 range 通用降级已有三版本 oracle、JIT/AOT 搬迁及 CIL/PE
定向证据。完整供参的任意对象 typed 调用、单模块未知扩展入口、typed 特化恢复与完整 inspect
消费者继续另切，代码体积、冷登记成本和本轮最终性能待主代理冻结后复验。

完成门：反射/对象协议同次 CPython 差分、FastAPI 依赖注入和 Pydantic 模型场景通过；
JIT 与 NativeAOT 元数据完整，已有普通类和 typed 快路维持语义及性能保护。

### C3：原生协程状态机与异步运行时

- 以现有生成器挂起/恢复、字段驻留及异常清理为基础，显式加入 coroutine/awaitable 合同。
- 依次实现 `await`、`async with`、`async for`、异步生成器及必要的异步推导式。
- 实现任务、Future、事件循环及项目需要的 socket/定时器/IO 协作；.NET Task 提供底层
  等待能力，Python 可观察的调度、contextvars、取消与任务身份由 Runtime 维护。
- 逐切验证重入、跨暂停异常、嵌套 finally、超时取消、异步生成器关闭及事务/连接释放。
  GUI 主线程、事件循环线程亲和性与原生扩展 GIL 重入一并纳入协议测试。

已验子合同：[ADR-0210](adr/0210-native-coroutine-state-machine.md)、
[ADR-0215](adr/0215-native-async-with-traceback.md) 继续使用默认关闭的 `native-coroutines`。
async with 覆盖真实挂起、清理、取消注入与抑制；traceback 支持只读头、tb_frame/tb_lineno/tb_next，
frame 限 f_code/f_lineno，code 限 co_name/co_filename。六组新源码三版本 oracle 一致，
JIT、两份 AOT 删除输入搬迁及 TraceV1 七项 PE 保护已有定向证据；完整 frame/code、
跨暂停 super、跨域 awaitable、事件循环和真实服务生命周期继续待办。

[ADR-0218](adr/0218-native-async-for.md) 增加 async for 窄耗尽保护、逐轮特殊方法绑定及
真实行号，修复动态 while 出口/回边的绑定事实。显式候选 21 项、七组真实 NativeAOT 删除
输入搬迁通过，六个输入在 3.9.25/3.12.13/3.13.3 oracle 一致；异步生成器与调度继续待办。

**greenlet 阻断**：原生同步换栈跨越 CLR/PInvoke 活动托管帧不满足 GC 栈扫描与异常展开合同，
当前不能直接准入。后续须独立 resumable-sync ADR，把所需同步 Python 调用链降为显式可恢复的
CIL continuation/驻留状态，再验证 ThreadState/GIL、contextvars、重入、线程亲和性及真实
SQLAlchemy 事务/取消/关闭。符号扫描与现有 async 状态机证据各自登记，该桥仍未实现。

完成门：CPython 协议差分、真实 HTTP 请求/并发/取消与数据库清理、JIT/NativeAOT
和搬迁通过。现有 TestClient CRUD 继续保留，新增真实服务生命周期验证。

### C4：编译完整 Python 依赖闭包，libpython 收敛到底层

- 将应用、纯 Python 标准库及第三方包装层逐族编译；动态导入使用显式声明和清晰诊断。
- 按真实依赖闭包补齐所需语法/控制流，包括模式匹配、赋值表达式等项目阻塞项，使用通用语义实现。
- SanBudget 路径包括 `tkinter`/`ttk`/`sqlite3` 的 Python 层，底层保留 `_tkinter`、
  `_sqlite3` 和原生库。FastAPI 路径包括 asyncio/inspect、框架、模型及数据库依赖。
- NumPy Python 包装层与数值原生核心分别登记；真实计算、buffer、异常及回调一起验收。
- 拦截或拒绝原生扩展内部的动态 Python 求值与未登记导入，防止重新引入解释执行。
- 复用原版 libpython 启动/关闭链，冻结必要胶水清单与阶段归属；按实测瓶颈决定局部原生替换。
- 依赖采集继续保留 metadata、包数据、许可证、证书及平台原生加载闭包，逐项评估裁剪。

内核前置子合同见 [ADR-0216](adr/0216-strict-kernel-native-stdio.md) 与
[kernel README](../runtime/kernel/README.md)：主代理已在 macOS arm64 真实源码重建 3.12.13，十五独立模式
覆盖严格拒绝、UTF-8、标准流与关闭；五个 stdio/codecs 模式还与同次未改 CPython 对照。
扩展/router 已接入源码变换并通过完整内核构建；累计 22 项严格内核探针、30 个测试扩展及
10 项 stock CPython 对照通过，后续 Loader 关键字参数回归也已全量验证。真实 Host/CLI、
完整 CIL 闭包与其它版本/平台留在 C4，当前兼容 provider 的审计仍不完整。

完成门：原版本/原入口的固定二十项目及两真实应用逐项记录 CIL 覆盖与胶水清单；已通过项目
保持原行为。stock 产品三阶段成本、多版本原生扩展与搬迁通过；业务委派按未迁移闭包继续登记。

### C5：默认晋升与旧执行路径退出

- 主线默认晋升须覆盖既有 Python 3.9–3.15 原版发行、各 ABI 原生依赖及三平台分发验收；
  3.12.13 自定义严格原型留作独立研究，各版本主线继续复用匹配的上游发行。
- 先提供显式候选路径，逐模块验证；候选域内遇到缺口明确报错，禁止自动解释执行。
- 每个项目的完整 Python 闭包通过后登记全 CIL 成绩；混合路径仍按实际执行归属记录。
- 全部保护门和平台门通过后另行切换默认；旧兼容模式保留一个有明确退出条件的迁移阶段。
- 经过默认晋升稳定验收后，分别删除已迁移业务的源码委派与冗余载荷；必要胶水保留明确清单。

完成门：C0–C4 闭环、默认保留项目零新增失败、同提交平台验证及发布说明完成；
删除旧路径后重跑完整门禁与搬迁。默认切换和代码删除各用独立可回退提交。

## 4. 代码生成时间与动态能力边界

默认以构建期冻结能力并生成 CIL，随后走 JIT 或 NativeAOT。
NativeAOT 的动态类、反射和方法替换通过预编译操作函数与运行时数据实现。
任意运行期 `eval/exec`、新增函数体、动态加载新 CIL 的能力独立规划为 JIT 路径；
NativeAOT 对构建期无法确定的新代码给出诊断，禁止回落 CPython 或加入解释器绕过目标。
具体 CLI 与能力清单到对应实现切片再确定，本次规划不新增开关。

## 5. 防回退与实测要求

每个阶段遵守以下准入要求：

- 同次固定 CPython 差分：结果、身份、别名、副作用、求值顺序、错误、清理及退出。
- 原项目版本、入口和输入保留；完整固定二十项目与已有联合应用/NumPy门分别登记。
- JIT、standalone、onefile 及 macOS arm64/Linux x64/Windows x64 分开验收，包含删除
  构建输入、中文/空格搬迁、清洁环境及缺依赖/ABI错误等负例。
- 产物清单与真实执行审计共同记录业务 CIL、原生 ABI、必要胶水与兼容委派。
  审计观察求值入口/执行帧及回调全链，注明采样覆盖；静态存在与实际执行次数分别记录。
  原严格审计器继续执行零字节码合同，stock 胶水验收使用独立判据。
- 对公共分派、调用、容器、生成器和新增功能测端到端时间、冷启动、分配、稳态内存、
  体积与构建成本。固定当前性能工作负载与比较方法，保留所有不利样本。
- 每个切片先 pending、定向验证再完整 `make check`；代码单文件不超过600行。
  输出在仓库外并清理，oracle 当次生成，普通文件不增加摘要校验流程。

已通过项目出现新增失败、身份/异常/资源清理错误时，停止对应切片晋升，修复或撤回该切片。
收益不足或存在性能回退时保留显式候选状态，按证据决定后续优化或 ADR 取舍。

## 6. 当前实施位置

C0 盘点与基线冻结已完成。C1–C3 按上述有限子合同继续竖切；C4 主线转向原版库的标准模块
事务、必要胶水审计与完整业务闭包，已实现的严格内核研究代码保留。C5 独立验收默认晋升与退出。
当前 helper/ABI 实数与逐项证据见 [inventory](cil-execution-inventory.md)，任务见 [TODO](../TODO.md)。
首轮完整门与性能属于已提交历史记录；本轮最终公共 ABI、全门、Release 和性能尚待主代理验证，
已有不利样本与阶段剩余项继续保留。

## 依据

- [NativeAOT 的代码生成与加载限制](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/#limitations-of-native-aot-deployment)
- [Python inspect 合同](https://docs.python.org/3/library/inspect.html)
- [Python C API 类型与槽位](https://docs.python.org/3/c-api/type.html)
- [Tkinter 的 Python 与原生层结构](https://docs.python.org/3/library/tkinter.html)
