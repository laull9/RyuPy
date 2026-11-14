# AOT 运行时、CPython 桥与交付性能候选方案

本文登记源码观察、待验证假设和实施门槛。2026-11-15 主代理复核后修订；尚无本轮成功的全量 AOT 性能基线。用户提供的 e8c07b85 全套运行在布局验证阶段失败，26 项均未取得 RyuPy 计时样本。

## 1. 目标与测量合同

目标为固定输入逐项运行速度不弱于固定 CPython、内存占用不高于 CPython；用户允许按实际情况调整，运行速度优先。保留速度和内存两列达标状态，对取舍给出证据及明确建议，不能擅自追加内存硬封顶要求。基准 CPython 固定 3.12.13。

纯 RyuPy NativeAOT、随包 CPython standalone、onefile 分开计量；真实 NumPy 与联合应用另列。保留原输入规模、身份、别名、副作用、异常及整数溢出语义。候选缓存必须说明容量、失效和释放。

基准代理串行构建和计时，其余代理仅做源码分析；先修复 hello 的布局识别并真正执行，再运行全套。记录每次构建完整错误，区分编译失败、发布识别失败与执行失败。

### 时间和内存口径

- 使用直接启动脚本的 CPython 墙钟作用户体验对照；带计时外壳的进程内耗时另外标注，避免导入外壳影响启动结论。
- 重复新进程不等于稳定运行的同一进程；若需要稳态吞吐，另设保留原业务工作量的进程内重复测量。
- 当前 onefile 每次解包，分别测首次启动与重复启动；未来引入持久缓存后才能增加缓存命中指标。
- 使用独占临时目录测应用解包冷态，不执行系统 purge/drop_caches；OS 页缓存状态如实登记，不声称磁盘冷态。
- 单子进程峰值使用 wait4(pid) 的 ru_maxrss，macOS 字节、Linux KiB，统一 MiB。累计 RUSAGE_CHILDREN 无法直接归属某一例，禁止用它替代逐进程统计。
- onefile 父子进程须分别采样并记录同时驻留的进程树开销。单进程高水位相加会高估同时峰值；共享页也会重复计数。进程树采样保留间隔及可能漏峰限制。
- RSS、私有内存、PSS 分列；macOS task_info 的 resident_size 不能称为排除共享页后的私有内存。
- 托管累计分配、托管存活量、CPython 分配块数属于不同指标，不能代替整进程 RSS；分配器钩子、分层计时作为独立诊断轮，不混入无插桩性能样本。
- 1 次热身、5 次计时，CPython/RyuPy 交替；报告中位数、波动、逐项输出差分和失败清单。不以总体几何平均掩盖逐项退化。

## 2. 已核对的源码观察

### 动态数值与对象分配

[PyOps.cs](../runtime/RyuPy.Runtime/PyOps.cs)、[PyInt.cs](../runtime/RyuPy.Runtime/PyInt.cs) 提供 bool 和小整数装箱复用。动态浮点结果可能装箱，但最终次数需要结合 MIR/CIL、实际快路和分配采样验证。matmul 原输入确为 100×100、重复 3 次；不能从 Python 循环次数推断实际堆分配次数。

扩大小整数缓存、缓存浮点对象会改变可观察身份与常驻内存，当前不选。优先验证消除非逃逸临时装箱及受守卫的数值运算。

### 参数绑定

[PyCallable.cs](../runtime/RyuPy.Runtime/PyCallable.cs) 的 PyBinder.Bind 已对 SimplePositional 且无关键字提供快路：满参返回原数组，默认值补齐分配一个结果数组。复杂绑定路径分配 bound、filled 和多个 List，并扫描参数种类，属于可验证的分配候选。

[callables.rs](../crates/ryupy-codegen-cil/src/callables.rs) 的 thunk 目前以位置实参数量恰好匹配且无关键字直调。默认参数 thunk 可进一步优化，但源码静态可解析的顶层调用可能已经绕过 thunk；必须先确认 calls.py 实际经过哪条路径。

### PIC 与缓存

[PyPic.cs](../runtime/RyuPy.Runtime/PyPic.cs)、[PyPicMethods.cs](../runtime/RyuPy.Runtime/PyPicMethods.cs) 存在 MaxSites=4096 的固定数组。评估按使用分页初始化的可能性，先确认类型初始化实际触发点，不能断言 hello 必定初始化所有数组。scratch 重入问题需要可复现调用路径才能登记为缺陷。

[PyRegex.cs](../runtime/RyuPy.Runtime/PyRegex.cs) 已在 Cache.Count>=64 时移除旧项，并提供 purge；本轮不登记无界正则缓存问题。

### 字典与字符串哈希

[PyDict.cs](../runtime/RyuPy.Runtime/PyDict.cs) 使用 Dictionary 索引及 keys/values 两个 List，空字典包含这些包装对象；内部数组的分配取决于实际插入。墓碑主要增加有序遍历及紧缩工作，普通键查找通过索引，不能称为每次查找线性扫描墓碑。

[PyHash.cs](../runtime/RyuPy.Runtime/PyHash.cs) 的 StringHash 对长度>=32 的字符串先 TryGetValue；缓存命中直接返回，未命中计算后创建 StrongBox。短字符串直接计算，没有短字符串弱表装箱问题。去除长字符串缓存可能增加重复 O(n) 扫描，必须分别测重复长键与一次性长键。

### 对象桥与生命周期

[PythonObject.cs](../runtime/RyuPy.CPython/PythonObject.cs) 位于 RyuPy.CPython，提供终结器与幂等 Dispose；Dispose 调用 GC.SuppressFinalize。终结器可延长未显式释放代理的存活时间，需测句柄、释放队列与 GC 后残留，不能直接去除必要的引用释放兜底。

[CPythonHost.cs](../runtime/RyuPy.CPython/CPythonHost.cs) 已有 ThreadStatic leaseDepth。Enter/Dispose 仍执行宿主状态检查、活动租约计数、GIL 配对及队列排空。leaseDepth>0 单独不足以证明回调到托管代码时线程仍持有 GIL：Python 扩展可能释放 GIL 后回调。优化必须先建立同宿主、当前线程、当前持锁状态及关闭协议的证明。

[PythonObject.cs](../runtime/RyuPy.CPython/PythonObject.cs) 的 Invoke 构造参数 tuple/关键字 dict，C shim 执行 PyObject_Call。Vectorcall 是减少中间参数容器的候选，需验证固定版本 shim ABI、借用引用存活、关键字顺序和异常清理。

rp_decref 可触发析构，rp_length/rp_truth 可执行 Python 协议和托管回调，禁止直接标注 SuppressGCTransition。当前不将该属性列为实施项。

### AOT 与 onefile

[aot.rs](../crates/ryupy-driver/src/aot.rs) 显式 PublishAot/Optimize；Speed 设置可以独立 A/B 验证。需先检查 SDK 实际属性名、有效默认值及 ILC 参数，未显式设置某属性无法证明该功能关闭。保留区域性功能，不能用 InvariantGlobalization 换取未经接受的行为变化。

[Program.cs](../runtime/RyuPy.OneFile/Program.cs) 每次创建私有临时根、解包、启动子进程、等待并清理。可以确定存在重复解包和等待父进程；具体载荷大小、启动毫秒数和父进程 RSS 尚待实测。

## 3. 候选竖切与准入顺序

本表顺序为调查顺序；实现顺序由基线热点决定。

| 候选 | 收益机制 | 实施边界与验证 |
| --- | --- | --- |
| AOT Speed 发布偏好 | 编译器倾向吞吐优化 | 独立调整有效发布配置；保留相同输入、运行时和 provider，测逐项速度/RSS/产物体积/构建耗时；回归正常门禁 |
| 参数绑定元数据与有限直调 | 减少重复签名扫描、列表及默认值数组 | 确认实际调用路径后，先优化复杂绑定中不必要的临时对象，再单列默认值 thunk；覆盖全部参数形态、可变默认值身份、错误消息及闭包/模块上下文 |
| PIC 按需存储 | 降低小程序固定数组占用 | 验证初始化触发与低站点程序内存；保持容量、版本失效、递归重入和禁用 PIC 行为；测多态热循环是否退化 |
| Vectorcall | 减少跨桥 tuple/dict 构造 | 独立于 GIL 协议变更；覆盖无参/位置/关键字、大参数数目、异常、托管回调及并发关闭；真实 NumPy 小调用与重计算分列 |
| onefile 缓存或执行替换 | 减少重复解包、父进程驻留 | 交付生命周期架构改变，先写 ADR 和异构评审；缓存与 Unix exec 分成两个竖切，Windows 单独验收 |
| 紧凑字典 | 减少多容器存储与访问间接层 | 先证明 dict_ops/真实应用热点，再写 ADR；合并 keys/values 后仍需索引和扩容，不能承诺两对象布局或固定节省比例 |

### 参数绑定实施细节

签名元数据预计算前核对现有公共元数据是否允许修改，避免缓存陈旧。优先栈上 bool 标记或按需列表；含托管引用的 object 槽不能直接 stackalloc。共享 scratch 必须在递归重入和并发时隔离，所有成功及异常出口清空引用。

覆盖 positional-only、keyword-only、*args/**kwargs、重复关键字、多传/少传、默认值定义时求值及身份、用户异常；CPython 同次生成预期。比较实际分配下降，累计分配指标会随调用次数增长，不能要求普通动态调用累计分配零增长。

### 桥接实施细节

先单独测 Vectorcall，保留现有 GIL/活动计数和关闭协议。参数指针数组在桥实现内部使用，引用所有权及 GC 根必须覆盖完整调用期间；不能把 PyObject/GIL 泄漏到 Rust IR。小数组栈空间和大数组释放分别验证。

重复跨域调用后检查活句柄、待释放引用、反向代理及跨堆环；区分显式 Dispose、GC/终结器处理和宿主关闭后的状态。受控 steady-state 测量记录回收周期，不能通过每次强制全量 GC 美化正常 RSS。

### onefile 交付实施细节

用户禁止普通文件 SHA/MD5 校验，缓存键采用构建期唯一发布标识，无需逐文件哈希流程。就绪标志不能独自证明目录可安全执行，需定义私有权限、路径/链接约束、并发发布、中断恢复、版本淘汰和磁盘容量。

必须明确应用写入资源与缓存复用是否冲突，保持相同程序并发实例的隔离。Unix exec 会消除原父进程负责的信号转发和退出清理，需要先解决缓存生命周期；测试 cwd、argv、环境、stdin/out/err、退出码、信号与解包中断。不能将这一组合称为低风险局部修改。

### 字典实施细节

覆盖插入顺序、增删后重插、活视图、迭代失效、NaN 身份键、True/1/1.0、用户 __hash__/__eq__ 的重入和异常、原生字典子类及模块 locals。内存对照包含空字典、小字典、单个大字典和反复增删，并测速度。字符串哈希缓存作为独立实验，避免混淆布局与算法收益。

## 4. 决策与交付

首先完成基准入口修复和实际 hello，随后取得完整固定输入基线、代表性 provider/onefile 与真实项目对照。先实现热点明确且语义边界有限的一项，再做同输入 A/B 和完整检查；没有收益的候选撤回。

调用 ABI、字典存储、GIL 生命周期、onefile 缓存与底层值表示变更应登记 ADR，编号以当时仓库空闲编号为准。重大决定由当前团队异构模型评审，普通修改由主代理自行审查。NativeAOT 证据采用实际 ILC 产物及适用的反汇编，JIT 反汇编另列。

基准样本和日志写仓库外临时目录，人工结论保留本文件；不提交原始报告、快照或普通文件散列。当前未实施本文件的任何产品优化，未形成收益数字或达标声明。
