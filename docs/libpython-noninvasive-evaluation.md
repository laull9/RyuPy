# 原版 libpython 非侵入式嵌入评估

- 日期：2026-11-16。
- 性质：架构与维护成本研究；最新正式决定见 [ADR-0223](adr/0223-stock-libpython-bounded-glue.md)。
- 范围：CPython 3.9–3.15、常规 GIL、Linux x64 / macOS arm64 / Windows x64。
- 本页作者阅读现有代码、官方资料和另一位研究者的 stock 3.12.13/3.13.3 实验报告，未亲自重跑
  原型、编译源码或执行平台测试。实验来源与实际范围在第 4 节单列。
- 本轮未读取或修改用户项目测试输入；写入范围仅本页与新增 ADR-0223。

## 1. 推荐决定

用户最新授权：“为了如报告里说明的，可以略微放宽无python胶水的限制，可以有少量必要的胶水，
并且保证性能等问题”。据此，**主线选 A1：原版 PBS 正常初始化 + 现有原生 finder/loader +
少量登记的 bootstrap/importlib/codec 及桥接胶水**。继续把应用与第三方业务函数体迁入 CIL。
无需为全阶段零帧重造 importlib、codec 或维护 CPython fork。

主线采用“CIL 执行归属 + 受控胶水 + 实测性能”合同。启动、运行、关闭允许登记的必要胶水帧，
原 ADR-0208 的全阶段零字节码要求移为可选严格研究 profile。既有零帧门原样保留在该 profile；
主线不再以通过它为晋升前提，也不能输出零字节码成功标记。

**A2 私有 importlib 属性交接降为测量驱动的可选优化。** 两版窄探针证明这条接线可行，但
尚未实现完整 fromlist、事务与并发锁；stock codec 仍会执行 Python 回调。
在已经允许必要胶水的前提下，全面接管 importer 带来的兼容维护成本高，当前没有性能收益证据。
先在 A1 上定位真正热点；只有局部替换取得稳定收益、保留既有语义并通过版本/平台门才采用。

**公开 API 纯 C 最小启动路线应淘汰为完整产品方案。** CPython 没有公开的“关闭所有字节码、
保留全部扩展 ABI 与导入服务”的初始化配置，也没有覆盖内部导入器和全部求值入口的公开替换槽。
`_install_importlib=0` 属私有配置。把它归入“公开 API 方案”会错误估计版本维护成本。

stock 私有结构 adapter 与现有源码 fork 仅保留研究和历史证据，停止主动扩展为主线前置依赖。
以后显式恢复严格 profile 时，再按全入口拒绝、全生命周期覆盖和完整产品门决定载体。
这次修订改变了产品验收合同，应用/依赖 CIL 迁移、语义兼容、原生 ABI 和独立分发要求继续保留。

## 2. 用词、目标与判定表

本文中“原版/stock PBS”指直接复用 PBS 维护者发布的二进制，RyuPy 不再修改或重编 libpython。
PBS 自身包含发行工程补丁；“stock PBS”与“python.org 无补丁源码构建”须分别登记。
侵入程度分三层：公开协议嵌入；公开对象 API 操作私有属性协议；私有结构/代码接线。
A2 属第二层，保持库二进制和解释器结构布局原样，但依赖私有算法合同；不能称为“全公开稳定接口”。
“无源码 fork”只说明不维护 CPython 源码分支，运行时写私有结构仍属于第三层内部侵入。

| 路线 | 发行与启动方式 | 应用/依赖可全部 CIL | 全阶段零字节码 | 是否非侵入式 | 主要判断 |
| --- | --- | --- | --- | --- | --- |
| A1 | stock PBS，公开正常初始化，原生 finder/loader | 应用/第三方业务体持续迁移；必要胶水显式列外 | 主线已允许登记的内部执行 | 公开导入协议为主；现有 ModuleSpec 来源等窄依赖登记 | 已选择的主线 |
| A2 | stock 正常启动，随后公开对象 API 交接私有 importlib 算法属性 | 两版窄原型可行，完整路由/关闭待验 | 启动仍有执行，codec 回调仍有执行 | 库二进制与结构布局不改；私有属性协议依赖 | 热点实测后局部评估 |
| B | stock，公开 API 最小初始化与纯 C 服务 | 有限 C 对象服务可行 | 公开合同缺失，完整产品路线不成立 | 真正全公开约束下成立的范围很窄 | 淘汰为完整内核方案 |
| C | stock，私有最小启动与内部状态 adapter | 研究上可行，须实测全部消费者 | 尚无可强制覆盖全部入口的证据 | 否；可做到无源码 fork | 暂停扩展，保留研究 |
| D | 现有源码变换严格内核 | 仍需完整闭包与 host 接入 | 设计可覆盖；当前独立原型有局部门证据 | 否；承担源码 fork | 可选严格研究与成本对照 |

必须同时记录两条轴：Python 函数体的执行归属、CPython 内部字节码执行次数。A1 可以收回全部
业务与依赖函数体，内部导入算法作为登记胶水执行 frozen Python。A2 可进一步收回部分导入算法，
已有 Python 执行计数照常保留。`write_bytecode=0` 只控制缓存写入；
`site_import=0`、隔离搜索路径与禁用用户环境也没有关闭求值器。

依据：[CPython 初始化配置](https://docs.python.org/3.12/c-api/init_config.html)、
[PBS 维护者仓库](https://github.com/astral-sh/python-build-standalone)。

### 受控胶水的边界

可准入的是固定来源的 bootstrap/getpath、实际需要的 importlib 发现/锁/规格/loader、编码及流
回调、有限桥接与清理函数。每个条目登记来源发行、精确 micro、模块/qualname/源码位置、
具体依赖函数、理由、CIL 目标、允许阶段和当次三阶段调用数；新增闭包和成员不自动放行。
不允许整个模块树的通配许可，也不允许把第三方业务体、完整 inspect/编码包或任意动态源码
委派归为胶水。用户回调目标保持 CIL；现有兼容委派成绩分开登记。

胶水可以在冷导入、codec 和清理阶段执行，不能只登记 startup 后隐去后续执行。
旧方法 wrapper 等必要桥接先保语义并测热点成本，逐帧列出来源；未知帧与覆盖窗口进入待归类
清单。具体清单、审计字段与准入预算由 [ADR-0223 §3–5](adr/0223-stock-libpython-bounded-glue.md#3-胶水范围与审计清单)
定义，清单产品化和完整消费者分类仍待实施。

## 3. 官方源码核查：公开扩展点覆盖到哪里

### 3.1 最小初始化与 `_install_importlib`

CPython 3.12.13 的 `Include/cpython/initconfig.h` 将 `_install_importlib` 放在
`Private fields` 中，注释用途是 `freeze_importlib`。3.9 与 3.15 维护分支同样如此。
它出现在已安装头文件中，仍然是私有合同；跨版本不可复制 `PyConfig` 布局。
公开 `Py_InitializeFromConfig` 并未把它引用的全部字段变成公开配置。
官方文档列出的 `_init_main` / `_Py_InitializeMain` 也明确属于私有临时 API。

在 3.12.13 `pylifecycle.c` 中，core 阶段按 `_install_importlib` 决定导入器初始化；main 阶段
发现该值为零后直接返回。该模式原用于冻结工具，未完成正常 stdio、路径、编码与扩展导入服务。
只设 `_init_main=0` 也不能推出“core 阶段无 frozen 执行”；实际发布源码的 core 调用链必须核对。

原始依据：[3.12.13 配置头文件](https://github.com/python/cpython/blob/v3.12.13/Include/cpython/initconfig.h#L194-L203)、
[3.9 配置头文件](https://github.com/python/cpython/blob/3.9/Include/cpython/initconfig.h)、
[3.15 配置头文件](https://github.com/python/cpython/blob/3.15/Include/cpython/initconfig.h)、
[3.12.13 生命周期源码](https://github.com/python/cpython/blob/v3.12.13/Python/pylifecycle.c#L1041-L1050)、
[私有多阶段初始化说明](https://docs.python.org/3.12/c-api/init_config.html#multi-phase-initialization-private-provisional-api)。
3.9/3.15 分支链接是浮动证据，后续实现须固定实际 micro tag。

### 3.2 导入钩子与内部 importer 是两层

公开 `sys.meta_path` / finder / loader 协议允许用原生类型实现 CIL 模块发现与执行，并沿用
CPython 模块缓存、导入锁、包及失败回滚。`PyImport_Import` 使用当前 builtins 的
`__import__`；因此替换该 callable 可以承接这一类调用。

但 `PyImport_ImportModuleLevelObject` 是独立 C 入口。3.12.13 直接通过
`interp->imports.importlib` 调用 `_find_and_load`、`_handle_fromlist`、`_lock_unlock_module` 等。
`PyImport_GetMagicNumber`、模块 `repr`、ExecCode 路径也有内部 importlib 消费者。
替换 builtins 的 `__import__` 或插入 C finder，均未替换这套内部状态；正常初始化下，
导入调度仍可执行 frozen Python。关闭内部 importlib 后，必须补齐这些消费者依赖。

公开 API 可创建模块、操作 `sys.modules`、注册内建初始化函数、执行多阶段模块定义；没有公开
setter 能把 `PyInterpreterState` 的内部 importlib 权威切换成宿主 C 实现。手动预填所有模块
只能覆盖已缓存成功路径，循环/部分初始化、删缓存重导、包 fromlist 和失败路径仍需完整导入合同。

另一条重要路径是**保留解释器已经引用的真实 importlib 模块对象，只替换其算法属性**。
正常初始化后，`PyObject_SetAttrString(real_importlib, "_find_and_load", native_callable)`
可以让上述 C 入口对原模块的下一次属性查找取得 C callable；同理替换 `_handle_fromlist` 与
`_lock_unlock_module`。该路径无需公开 interpreter.importlib setter，也无需写私有字段。
“没有公开内部 setter”因此不能推出“stock 运行期导入算法必须保持 Python”。
它依赖的是私有属性名称、参数、调用时序和跨版本缓存行为；详细方案见 A2。

原始依据：[导入协议](https://docs.python.org/3.12/reference/import.html#the-meta-path)、
[公开 C 导入 API](https://docs.python.org/3.12/c-api/import.html)、
[3.12.13 import.c](https://github.com/python/cpython/blob/v3.12.13/Python/import.c#L2594-L2769)、
[内部 import 状态](https://github.com/python/cpython/blob/v3.12.13/Include/internal/pycore_import.h#L31-L63)。
属性赋值 API 依据：[PyObject_SetAttrString](https://docs.python.org/3.12/c-api/object.html#c.PyObject_SetAttrString)。

### 3.3 stock 能否强制禁止全部字节码

结论：**公开配置、audit hook、导入钩子和 PEP 523 组合仍缺少全入口强制禁止能力。**

- `PySys_AddAuditHook` 可以在初始化前注册并覆盖所有解释器的已发出事件；audit 事件不是逐帧入口。
  3.12.13 `PyEval_EvalCode` 本体直接构造函数并调用 `_PyEval_Vector`，没有普遍的 `exec` 审计门。
  监控 `compile/exec/import` 事件无法证明普通函数、生成器及原生反向调用无解释执行。
- PEP 523 是每解释器求值函数替换点，涉及私有 API/帧布局。分派 helper 检查 `eval_frame`，
  默认求值器本体直接执行指令。原生扩展若直接调用 `_PyEval_EvalFrameDefault`，就跳过该分派。
  设置一个拒绝 hook 也须覆盖首次安装之前、新解释器、关闭阶段及 hook 被替换的窗口。
- `PyImport_ImportFrozenModule` 仍可从 stock 库内载荷取得执行代码；删除磁盘 `.py/.pyc`
  没有删除内嵌 frozen/deepfreeze 载荷。任意原生符号扫描也遗漏 `dlsym` / `GetProcAddress`
  及对象 callable 的间接求值；闭包审计必须包含行为负例。
- ELF interposition、DYLD 注入、IAT 修改或函数机器码改写均属于侵入式二进制接管，受内部绑定、
  优化和平台加载规则影响，不推荐作为三平台产品的隐式严格门。

原始依据：[PEP 523](https://peps.python.org/pep-0523/)、
[3.12.13 求值分派](https://github.com/python/cpython/blob/v3.12.13/Include/internal/pycore_ceval.h#L76-L84)、
[3.12.13 PyEval_EvalCode 与默认求值器](https://github.com/python/cpython/blob/v3.12.13/Python/ceval.c#L531-L626)、
[原生审计 API](https://docs.python.org/3.12/c-api/sys.html#c.PySys_AddAuditHook)、
[官方审计边界](https://docs.python.org/3.12/library/sys.html#sys.addaudithook)。
上述结论针对正常 ABI 下直接求值等工程绕行路径；可选严格 profile 保留统一拒绝目标。
主线按执行归属和胶水清单验收，不能用 audit exec=0 声称未执行 Python。

## 4. 各路线的具体工程代价

### A1：stock 正常启动 + 原生 shim/导入钩子

复用 PBS 动态库、标准库、扩展和依赖，不改 CPython 内部结构；RyuPy 的 CIL 模块由原生
finder/loader 进入唯一模块事务。当前已有 [原生导入桥](../runtime/RyuPy.CPython/native/import_hooks.h)
与 [ADR-0220](adr/0220-native-compiled-import-hooks.md)，可以继续收回模块初始化权威。
依赖闭包中的纯 Python 模块逐族编译，CPython bootstrap 名单显式列为内部执行基础设施。

它省掉自建 interpreter 与 codec/importlib 内核的发布工作，保留全部 C API 生态入口。
原生扩展内部 import 将进入正常 importer；扩展调用应用/业务 callable 的目标必须映射为 CIL 回调。
未编译闭包、运行时生成代码和解释器私有消费者仍需能力诊断，不能靠磁盘源码回退完成迁移。
普通 importlib 的执行应在审计报告中单列，任何“全阶段零字节码=true”的声明必须拒绝。

### A2：stock 正常 bootstrap 后，通过公开对象 API 交接私有导入协议

这是 A1 与私有结构 adapter 之间的可选路线，**当前保留研究，不作为主线实施前置**。
它保留正常初始化创建的真实 `interp.importlib` 对象与编码/stdio 状态，随后用静态 C callable
替换原对象的 `_find_and_load` / `_handle_fromlist` / `_lock_unlock_module` 等已核消费者。
调用侧仍持有原模块，因而直接 `PyImport_ImportModuleLevelObject` 也可能自然进入新路由。
它使用公开 C 对象 API，算法属性协议属于 CPython 私有实现；维护范围比解释器结构接线窄，
但相较 A1 仍要接管原本可以复用的导入算法与锁语义。

必须先取得并保留**原模块对象本身**，只在 `sys.modules` 换一个同名新模块会留下内部旧引用。
3.12.13 `import.c` 的当前属性查找支持这项判断；其它版本若缓存 callable 或改用不同入口，
必须逐版核对。原生导入仍须维持包、锁、回滚、单/多阶段扩展与 CIL 模块单一事务。
从旧 frozen 函数已有别名、闭包或缓存直接调用，仍会进入 Python；交接清单须覆盖消费者和可达旧引用。

交接时序应冻结为：正常 bootstrap → 明确范围的编码/标准流准备 → 记录启动执行清单与计数 →
单线程持 GIL 安装 C/CIL 算法与封闭模块清单 → 切运行阶段 → 执行业务 → 单独审计关闭。
不能通过提前导入全部业务模块来掩盖其初始化归属；预热仅限明确允许的内部基础设施。

codec 搜索函数、增量编解码器以及 `atexit`、threading、weakref/析构等关闭消费者可能仍保留
Python callable；正常 bootstrap 没有让这些路径自动变为 C。需逐消费者编译/替换或明确登记
剩余内部 Python 执行。只替换三个导入算法的窄探针应记录为“导入交接成功”，运行/关闭零帧另验。

A2 不移除 stock 默认求值器，原生直接 EvalCode/默认求值器仍可执行。启动/预热已有 Python
执行继续保留，运行期和关闭分别计量；ADR-0223 已接受受控必要胶水。今后只有性能归因指出
局部导入算法是实际瓶颈，才比较 A2 的净收益与语义维护成本，禁止以帧数下降替代性能证据。

原始依据：[3.12.13 导入调用属性](https://github.com/python/cpython/blob/v3.12.13/Python/import.c#L2556-L2614)、
[公开对象赋值 API](https://docs.python.org/3.12/c-api/object.html#c.PyObject_SetAttrString)、
[关闭阶段仍可调用 Python 的源码说明](https://github.com/python/cpython/blob/v3.12.13/Python/pylifecycle.c#L1755-L1788)。

### B：stock 最小初始化 + 公开 API 纯 C 服务

公开对象 API 可服务数值、容器、buffer、异常与 C callable；实际可行范围是受控的有限对象宿主。
完整兼容扩展所需的初始化、导入、编解码与生命周期服务超出该范围。
若加入 `_install_importlib=0`，路线已经进入 C；若先正常初始化再关闭 Python 执行，则启动期已有
字节码；若改发自定义 frozen Python，则继续含 CPython 执行。三者都不能满足 B 的原定义。

尤其 `PyCodec_Register` 的官方合同包含尝试导入 `encodings` 的副作用。原生 UTF-8 编解码函数
可以独立调用，但 TextIOWrapper、增量 codec、错误处理器注册和编码查找构成另一套初始化链。
不能从“UTF-8 primitive 是 C”推出“完整 stdio 启动全为 C”。
依据：[公开 codec 注册合同](https://docs.python.org/3.12/c-api/codec.html)、
[3.12.13 codecs.c](https://github.com/python/cpython/blob/v3.12.13/Python/codecs.c#L19-L45)。

### C：stock + 窄私有 ABI adapter

把所有私有依赖集中在按精确 CPython/PBS 发行构建的 C adapter，CLR 继续使用静态、中性的 shim ABI。
可研究私有最小启动、纯 C importer 对象与 `interp->imports` 接线、纯 C UTF-8 codec/stdio，
并复用现有 CIL 模块事务、身份映射及引用所有权实现。

成本从“重建 CPython”转为“维护内部状态协议”，包括：字段偏移与初始化顺序、import_func 与缓存、
loader 协议、单/多阶段扩展缓存、包/相对导入、codec 私有状态、finalize 时机。
读取私有头文件并不保证所需私有符号被 PBS 动态库导出；Windows 尤须逐一核对。
禁止在 C# 复制解释器结构、按猜测偏移写内存或在失败后自动回落正常启动。

adapter 若只创建 C 代理并接好内部 importer，可能保持应用闭包零解释执行；
stock 的默认求值器和 frozen 载荷仍存在。全生命周期实测零次与全入口可拒绝必须分开报告。
后续若重启严格研究，须主动测试直接默认求值器调用，缺门即判定严格 profile 未准入。

### D：当前源码 fork 严格内核

现有 [kernel README](../runtime/kernel/README.md) 记录 3.12.13 macOS arm64 的独立探针证据；
host/CLI、完整闭包、多版本与其它平台仍待验。本文没有重跑该证据。
四组额外维护面如下，实际修改跨越多个上游文件：

| fork 维护组 | 与 stock 对比的新增责任 |
| --- | --- |
| 求值入口与分派守卫 | 维护 ceval / pycore_ceval 等源码锚点、直接调用拒绝、异常及生成器退栈 |
| frozen/deepfreeze/getpath 与构建规则 | 清除内嵌执行载荷、绕开 Python 路径计算、维护生成文件依赖与平台构建规则 |
| import 路由与扩展加载 | 补齐内部 importer 消费者、模块锁/等待图/回滚、原生 Spec/Loader、静态 CIL 回调 |
| 启动、codec、stdio 与关闭 fence | 补齐纯 C UTF-8/增量类型/流协议，维护真实解释器有效期内的 owned 释放时机 |

因此“只有四处补丁”会低估成本；这是四组功能和多处精确版本源码锚点。
PBS 对优化、依赖和跨平台构建的投入可作为配方参考，但 RyuPy 改过内核后，现成 PBS 二进制
无法直接替代自己的构建产物。沿用 PBS 构建配方也须承担安全更新、工具链、发布、签名与搬迁门。

### 真实 stock 判别实验（已读取报告，未由本文作者重跑）

2026-11-16，另一位研究者在 macOS arm64 对已安装的原版 PBS 3.12.13 与 3.13.3 做了独立 C
探针。3.12.13 使用 Clang 22.1.3 构建的发行，3.13.3 使用 Clang 20.1.4 构建的发行；探针以
`clang -g -O0` 编译，每个模式独立子进程。外部 LLDB 在进程启动前设置默认求值器、getpath
与 EvalCode 断点，正向 bootstrap/负向 EvalCode 都有命中以核实断点有效。它是观察实验，
未执行 RyuPy 产品、真实 NumPy 全初始化或性能比较。

| 实验 | 3.12.13 | 3.13.3 | 可得结论 |
| --- | --- | --- | --- |
| 私有 `_install_importlib=0` 与 C import hook | 初始化成功；高层 ImportModule 可入 C hook；缺失模块/fromlist 直接 C API 失败 | 相同 | 高层 hook 不能覆盖全部 C 导入消费者；两版 stdin/stdout 都为空 |
| 正在初始化的缓存模块，直接 ImportModuleLevelObject | 子进程退出 -11；LLDB 栈进入内部 lock 方法查找 | 子进程退出 -11 | 同名 sys.modules 桩未修复内部 importlib 权威 |
| 纯 C UTF-8 codec + stock `_io`，无私有结构写入 | C encodings 桩 + PyCodec_Register 可完成编解码、增量、TextIOWrapper、print/input；三阶段默认求值器命中 0/0/0 | PyCodec_Register 内部访问空 search_path 崩溃，退出 -11；所需私有初始化符号本机未导出 | 私有最小启动不能按公开 API 名称推定跨 minor 初始化合同 |
| 正常 bootstrap 后替换三个私有算法属性 | 新导入、包/fromlist、缓存相对、initializing、首次 math 和 consumer.so 直接 C API 探针通过；默认求值器命中 387/0/0 | 同组探针通过；命中 345/0/0 | A2 接线可行；这些命中数按 startup/runtime/shutdown 分段 |
| A2 后通过 stock 增量 decoder 构造并 decode | 运行期新增 2 次默认求值器命中 | 同样新增 2 次 | codec 回调仍有 Python 执行，exec audit 均为零 |
| audit hook 拒绝 exec 后，直接 CompileString + EvalCode | 成功写入 marker=42；exec=0、compile=1 | 同样成功 | exec 事件不足以审计全部求值 |
| 初始化后安装 PEP 523 拒绝器，再 EvalCode | 被拒绝，marker 缺失，pep_denied=1 | 同样被拒绝；LLDB EvalCode=1、默认求值器=0 | 仅覆盖安装后的相应分派，独立默认求值器导出仍存在 |

A2 探针中的 `_handle_fromlist` 只返回包，`_lock_unlock_module` 只返回 None；未实现完整
fromlist 加载、模块事务、锁等待、并发循环和死锁处理。不能据此决定用 A2 替换成熟 importer。
正常 bootstrap 两版 `PyEval_EvalCode` 各 10 次，exec 审计各 8 次，进一步确认事件与求值调用非一一对应。
表中数字是被测入口断点命中，不能当作完整 Python 函数体调用总量，CPython 可能内联执行子帧。

3.13 无 importlib 关闭 math 另出现 `m_clear` 的 dict watcher 错误，进程退出 0，作为真实失败
边界保留。NumPy 仅静态确认一个 cp313 random 扩展依赖 ImportModuleLevelObject/Code/Frame，
未运行其业务。Linux、Windows、其它 CPython 版本、真实产品和性能继续待验。

以下持久化探针是 A1 的主要复现入口；上述失败边界另保留本节摘要，原始研究报告位于
`/tmp/ryupy-stock-sol.spVbVH/REPORT.md`，已清理早期探针二进制。本文未复制原始日志或生成产物。
源码复核：[3.13.3 codec 初始化前置](https://github.com/python/cpython/blob/v3.13.3/Python/codecs.c#L20-L35)、
[3.13.3 导入实现](https://github.com/python/cpython/blob/v3.13.3/Python/import.c)。

### 可复现的 A1 原生导入桥与胶水归属

持久入口为 [探针说明与两版命令](../tests/libpython_embedding/README.md)、
[C 探针](../tests/libpython_embedding/stock_glue_probe.c) 与
[仓库外构建/清理 runner](../tests/libpython_embedding/run_probe.py)。2026-11-16，研究者已对
macOS arm64 stock PBS 3.12.13/3.13.3 各运行 `--iterations 10 --repeats 2`，包/child/fromlist/
相对导入返回 42，UTF-8 decoder 返回汉字，stdout 空写/flush 与 Finalize 成功，归属表完整性
错误均零，临时目录清理完成。本文作者只读取说明和结果，未重跑命令。

该探针直接 include 当前产品 `native/import_hooks.h`，dispatch 使用两个纯 C 模块 stub。
它验证 A1 导入服务接线，真实 CIL 函数体、完整应用闭包、第三方扩展和搬迁仍须走产品门。
profile 在初始化后安装 PEP 523 观察器逐函数记录来源，随后委托 stock 求值器；计时进程不装
观察器。128 项归属表溢出、字符串超长、code/属性读取失败都会令进程失败。

| A1 观察阶段 | 3.12.13 函数帧 | 3.13.3 函数帧 | 观察来源 |
| --- | ---: | ---: | --- |
| 安装原生 finder/loader | 0 | 0 | C API 与缓存模块 |
| 首次导入固定包及 child，含 fromlist/相对调用 | 56 | 50 | frozen importlib 的锁、ModuleSpec、模块事务、fromlist |
| 缓存普通模块直接导入一次 | 0 | 0 | C 缓存命中 |
| 缓存包 fromlist 一次 | 1 | 1 | frozen importlib `_handle_fromlist` |
| UTF-8 增量 decoder 构造及 decode | 3 | 3 | frozen codecs 两个 `__init__` 与 `decode` |
| stdout 空 write/flush、关闭 | 0 | 0 | 本探针触发的 C 流路径 |

此表是观察器覆盖期间的函数帧数，前表 387/345 是 LLDB 默认求值器入口次数；二者口径不同。
本探针不统计启动函数帧，观察到的关闭零帧仅覆盖该输入，完整生命周期仍有独立覆盖门。
有限源码族支持逐函数胶水清单，而不是整个模块树通配。正式 A2 对照仍用空锁和有限 fromlist
桩；API 检查会引入额外属性帧，不能以其窄帧数替代完整 importer 合同。

前置仓外初稿曾做 6 轮平衡次序、每轮 100 万次缓存导入：普通导入 native/A2 中位数为
3.12 的 48.924/48.166ms、3.13 的 46.218/45.126ms；缓存包 fromlist 为
103.746/106.497ms 与 105.018/109.607ms。两模块冷导入 native 约 17.5/18.5µs、A2 约 6µs。
采样未独立监控全机负载，初稿与持久源码的 API 检查也不同；A2 省去的工作含其未实现的事务。
这些数据只支持研究排序，无法建立 A2 的完整产品收益。正式功能验收的极小计时次数运行时
还有其它验证负载，全部计时排除在性能结论外。ADR-0223 的产品预算仍待正式隔离采样。

## 5. 多版本、平台、wheel 与 ABI

当前 shim 已按选定 Python 的头文件编译，封装 PyConfig/PyStatus 和对象协议，导出
`rp_build_version` / `rp_python_version`；[driver shim 构建](../crates/ryupy-driver/src/cpython_shim.rs)
在仓库外临时目录调用平台工具链，成功后随计划释放。stock 路线仍保留这笔每版本/平台成本。
完整 `PyConfig`、异常/traceback 等非 Limited API 消费者使现有 shim 无法直接变成单一 abi3 二进制。

| 维度 | 必须保留的合同 |
| --- | --- |
| 3.9–3.15 | 七个 minor 分别选择匹配头文件、普通 GIL/release 构建、目标 ABI 与实际 micro；私有 adapter/fork 对 micro 也精确锁定。3.9/3.15 存在同名私有字段只提供调查入口，不能复用结构布局 |
| cp39…cp315 wheel | 按 interpreter、ABI、OS、CPU、libc/最低部署版本选择；cp312 wheel 不能装到 cp313。每版真实消费者分别验收 |
| abi3 wheel | 受 Limited API 下限与平台约束；Windows 通常依赖 python3.dll。abi3 只承诺 ABI 稳定性，内部 import、回调与动态求值仍需逐行为准入 |
| C API 消费者 | 精确 PyCode/PyFrame/PyType/ThreadState、异常、GIL 和 buffer 约束保留；对象布局兼容与函数实际语义兼容分别验收 |
| Linux x64 | 固定 glibc 或 musl 变体、动态加载与全局符号可见性、OpenSSL 等依赖和目标最低系统；不能混装 libc wheel |
| macOS arm64 | 固定 CPU/最低部署版本、install_name/rpath、扩展依赖及签名后搬迁；x64 或 universal2 切片另验 |
| Windows x64 | 固定 python3XY.dll/python3.dll、.pyd 后缀、导出表、MSVC/CRT 与依赖 DLL 搜索；私有符号在 ELF 可用不能推出 DLL 可用 |

版本探测先锁定规范化发行根、libpython 与 shim 路径，再核对 shim 头文件版本、已加载库版本、
平台/指针宽度、GIL/debug 配置、ABI 标记与扩展后缀。私有 adapter 加精确发行标识和能力版本。
`Py_GetVersion` 是必要信息，完整构建宏与布局验证还需发行元数据；同进程保持单一 CPython/profile，
加载失败不可换库重试。符号存在性与一次 import 成功均不足以放行全部 ABI 消费者。

依据：[CPython C API 稳定性](https://docs.python.org/3.12/c-api/stable.html)、
[PyPA wheel 标签规范](https://packaging.python.org/en/latest/specifications/platform-compatibility-tags/)。
CPython 明确指出私有 API 可在补丁版本变动；Stable ABI 也依赖平台与编译选项。
本页采用现有常规 GIL 范围，free-threaded 与 debug 发行另立合同。

## 6. 原生扩展、标准流、GIL 与关闭

原生扩展的依赖闭包包括 C 内部 import、Cython traceback、静态全局缓存、运行期 getattr、
回调和直接 EvalCode/RunString。当前盘点已记录 `_sqlite3 -> functools`、NumPy/random 的
code/frame 消费者及 FastAPI 栈 ABI；这些是已有文档证据，本研究未重新扫描真实 wheel。
应以 [C0 ABI 消费者盘点](cil-execution-inventory.md#7-底层-abi-消费者盘点) 为初始清单，
在选定版本上运行真实功能、异常、重入和关闭场景。greenlet 跨 CLR 原生换栈的既有阻断继续有效。

A 沿用官方 stdio/codec，保留启动所需包与内部模块；C/D 承担纯 C stdio 实现与完整差分。
macOS/Linux 的 fd、TTY/pipe、关闭后宿主 fd 身份和 Windows 控制台/重定向/Unicode 路径均单列。
支持 UTF-8 的原型应明确拒绝其它编码，之后按真实项目需要补齐 CIL codec 闭包。

所有路线均保留 GIL、线程状态、引用所有权、原生线程回调和托管异常边界。
`PyGILState_Ensure` 便利接口不能直接推广到任意子解释器；本轮维持主解释器合同，拒绝未登记
子解释器。关闭前阻止新的外部线程进入，等待活动回调退出，再按有效解释器/GIL 顺序释放句柄。

ADR-0221 要求 `atexit` 新建类/首次导入仍可用，之后在 FiniCore fence 内真实 DECREF，最终返回后
只清 CLR 账本。fork 有明确接线位置；stock 公开 API 没有等价的“内部模块清理完毕、解释器仍有效”
回调。`Py_AtExit` 位于 Python 内部 finalization 完成之后，不能用它执行这批 Python C API 释放。
A 可采用经验证的宿主主动关停/根持有策略；C 需证明私有 fence 的生命周期。
若要改变现有释放顺序，必须另做生命周期设计，不能直接挪用 strict 回调名字。

依据：[官方初始化/关闭/线程合同](https://docs.python.org/3.12/c-api/init.html)、
[Py_AtExit 时机](https://docs.python.org/3.12/c-api/sys.html#c.Py_AtExit)。
官方明确记录扩展未卸载、重复初始化与部分资源释放的限制，进程重启隔离仍是版本/profile 切换边界。

## 7. 成本账：已有成本与 fork 增量分开

| 成本项 | stock A1/A2 | stock 私有 adapter C | fork D |
| --- | --- | --- | --- |
| 分发 libpython、原生扩展及其依赖 | 既有成本 | 既有成本 | 既有成本，外加自建产物 |
| 3.9–3.15 目标 shim | 既有成本；A2 加私有属性协议与缓存调查 | 加精确版本私有接线 | 加 strict ABI/profile |
| CIL 对象/导入/反射/异步语义 | 所有路线都必须做 | 同左 | 同左 |
| CPython/PBS 安全与版本更新 | 消费上游发行并回归 | 加私有布局/导出/时序回归 | 加锚点迁移、全库重建与发布 |
| UTF-8/stdio/importer 内核重写 | A1 复用；A2 加原生 importer，运行期 codec 收权另验 | 需要 | 需要 |
| 三平台 interpreter 工具链与优化 | 复用 PBS 发行成果 | 复用 PBS 发行成果 | 自建并复验；fork 新增核心成本 |
| 执行审计 | 基础设施与 CIL 归属分列；A2 运行/关闭单列 | 全生命周期覆盖待证明 | 源码拒绝门覆盖仍须产品验证 |

PBS full archive 带 `PYTHON.json`、构建产物与自包含安装树；install-only 不带 full archive 的
顶层构建元数据，stripped 版本再去除调试符号。应复用现有完整发行缓存和许可清单，不为省下载
盲选缺元数据的包。共享库嵌入优先于重链接 PBS LTO 对象，后者还有工具链兼容成本。
依据：[PBS archive 格式](https://gregoryszorc.com/docs/python-build-standalone/main/distributions.html)、
[重用构建产物](https://gregoryszorc.com/docs/python-build-standalone/main/running.html#reconsuming-build-artifacts)。

许可证仍覆盖 CPython 与实际保留的 OpenSSL/SQLite/zlib/libffi 等组件。移除标准库源码或使用
自建内核都不会自动消除依赖许可。继续从发行元数据和许可证文本生成随包清单；核对所选发行
的组件清单和来源。依据：[PBS 许可说明](https://gregoryszorc.com/docs/python-build-standalone/main/running.html#licensing)。

**本研究没有产品性能收益数字。** stock A1/A2 保留 bootstrap/编码载荷；C 仍在库中保留 frozen 字节数组；
D 可删除数组与无用文件，但 CIL 闭包、调试元数据和替代服务也会增加体积。
保留 GIL/引用计数后，主要执行收益来自减少解释执行和跨桥调用；更换构建路线本身不能推出性能增益。

性能预算采用 [ADR-0223 §5](adr/0223-stock-libpython-bounded-glue.md#5-性能预算与测量合同) 的待验证
设计目标，不能声称已达标。同 SDK、同提交的冻结对照配置、同次固定 CPython oracle，平衡 AB/BA
采样；稳态总体和单项/长尾分别限额，启动、首导入、首次业务调用、关闭、RSS 与体积分列。
置信区间跨线判待定，确认的单项/长尾回退阻断晋升；保留全部有效样本并定位后做最窄修复。
JIT/AOT、onefile 冷解包与缓存命中、纯原生保护组分别运行，搬迁后重新核结果。
stock 缓存按发行版本/目标/GIL 复用；shim 可另缓存，但当前 driver 临时构建路径仍需具体实现。
私有 adapter 加精确 micro/发行标识；fork 缓存再加内核变换版本、工具链和构建选项，失效面最大。
普通文件按用户约定不加入 sha256/md5 流程。

## 8. 建议迁移步骤与验收门

1. **冻结 A1 胶水清单**：按 ADR-0223 逐函数登记来源/版本/阶段和必要理由，关联 CIL 归属，
   显式显示未知帧与覆盖缺口。全阶段零字节码门移至可选严格研究 profile，保留原统计含义。
2. **完成最窄 stock 产品竖切**：保留现有原生 finder/loader 与上游 importlib/codec，逐闭包
   迁移业务与非胶水依赖到 CIL，验证原生扩展直接 ImportModuleLevelObject 反向导入和 CIL 回调。
   不为减少少量胶水帧而重写整个导入器。完整 PEP451 事务缺口按真实项目另切。
3. **生命周期先验**：设计 stock 关停/持根/真实释放方案，覆盖 atexit 首次导入、异常、外部线程和
   跨堆根稳态；禁止直接套用 strict-only 内部 fence 或只清账本。
4. **性能与平台晋升**：按固定 SDK/提交/oracle、平衡轮次分别测启动/稳态/内存/体积与搬迁。
   先固定 3.12.13 与真实 3.13.3 消费者，再扩既有 3.9–3.15 普通 GIL 和三平台门。
5. **按证据局部优化**：A2 仅在实测 importer 热点成立后另切；私有结构 adapter/fork 暂停扩展，
   保留研究资产。基础发行、wheel、许可/资源/native 依赖共用 driver；默认晋升与旧路径退出分开。

| 门 | 必须观察的结果 |
| --- | --- |
| 归属门 | 应用/标准库/第三方函数体明确 CIL、原生、登记胶水或兼容委派；业务体无未登记源码回退 |
| 胶水门 | 来源/版本/函数集合/依赖/阶段/调用数完整；无模块树通配；未知执行与覆盖缺口显式失败/待归类 |
| 导入门 | 包/fromlist/相对/循环/并发/失败重试、sys.modules=None/替换值、删缓存后旧函数身份、原生扩展首次反导入 |
| ABI 门 | cp39…cp315 与准入 abi3 按实际版本分别测；原生数值/buffer/异常/回调/帧消费者，缺口明确拒绝 |
| 生命周期门 | 原生线程重入、atexit 首次导入/定义类、真实 tp_dealloc/owned 释放、跨堆根稳态、失败后进程绑定终态 |
| 可选严格拒绝门 | 严格 profile 另验普通调用、EvalCode/RunString、生成器、直接默认求值器、PEP 523 替换、原生间接 import/eval 和子解释器 |
| 可选全阶段零执行门 | 严格 profile 另验初始化前至 finalize 返回后的完整覆盖、不可重置纪元、成功执行全零和拒绝计次 |
| 产品门 | 原入口真实 JIT/standalone/onefile，删除构建输入和系统 Python 后搬迁；三个平台分别验证 |
| 性能门 | ADR-0223 待验证预算；同SDK/提交/oracle、平衡轮次、单项与尾延迟限额，体积/RSS/启动/稳态/退出分列 |

stock A1/A2 必须如实报告内部 Python 执行名单；主线按受控胶水合同验收，另设的严格审计门继续拒绝 A1/A2。
PEP 523 半程报告继续保持 `complete=false` / `startup_complete=false`。
C 只有实验覆盖的版本、平台与消费者可以登记；D 保留历史独立 C 探针证据，产品严格 profile 尚未验收。

## 9. 本研究的验证边界

已做：阅读指定 ADR/路线/盘点/kernel 说明和现有 shim/构建源码；在线核对 CPython 官方文档、
固定 3.12.13/3.13.3 源码、3.9/3.15 维护分支及 PBS 维护者资料；读取另一研究者 macOS arm64
stock 3.12.13/3.13.3 报告和仓库探针说明，把方法、成功/失败/崩溃、入口/函数帧两类计数、
来源分布和窄桩限制收入本文，前置研究计时与正式功能验收分开登记。

本文作者未做：stock 原型重跑、fork 源码重建、真实 wheel 动态探测、Windows/Linux/macOS 新运行
验证、冷启动/大小/缓存收益测量；未核定 PBS 每个旧版本是否仍有新发布 artifact。
后续必须按实际固定发行查询可用性，不能假定 3.9–3.15 都有相同发布节奏或全部平台的新构建。

仍待：A1 产品胶水清单/逐函数调用数、完整 CIL 业务闭包、真实原生消费者、stock 生命周期、
同提交多版本三平台搬迁与性能预算验收。本轮仅更新本评估和新增 ADR-0223，产品状态保持原登记。
