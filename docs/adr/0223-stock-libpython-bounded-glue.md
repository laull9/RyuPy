# ADR-0223：原版 libpython、受控必要胶水与性能验收

- 日期：2026-11-16。
- 状态：方向已接受；本 ADR 仅修订设计与验收合同，产品实施、清单冻结和性能验收待办。
- 决策来源：用户最新授权“为了如报告里说明的，可以略微放宽无python胶水的限制，可以有少量
  必要的胶水，并且保证性能等问题”。
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、[ADR-0220](0220-native-compiled-import-hooks.md)、
  [ADR-0221](0221-c4-2-strict-kernel-host-and-cil-import-integration.md)。
- 研究与原始来源：[原版 libpython 评估](../libpython-noninvasive-evaluation.md)。

## 1. 决定

主线选择**原版 PBS 正常初始化 + 现有原生 finder/loader + 少量受控 Python 胶水**（研究路线 A1）。
复用上游 bootstrap、importlib、codec 和确有需要的冷导入/清理回调，不重建完整 importer、
codec 或自定义 CPython。应用、第三方业务函数体和非胶水依赖继续迁入 CIL，原生扩展继续运行
机器码并回调已编译函数。保留 driver 冻结执行归属、Runtime 实现语义、shim 封装 C API 的分层。

必要胶水不限于启动：冷导入、编码器回调和关闭可保留登记的 Python 执行。
胶水必须按第 3 节逐函数/来源/版本/阶段登记，按第 5 节接受性能约束。
未知应用/第三方模块、任意 eval/exec、委派整个依赖闭包继续走明确能力诊断；已有兼容 provider
委派仍可作为兼容基线运行，但成绩单独归属，不能登记为 CIL 主线完成。

ADR-0208 中“应用和依赖函数体进入 CIL”的方向保留，必要胶水成为明确例外。
其全阶段零字节码、清除全部 frozen 执行载荷和“产品不能含任何 Python 胶水”的主线完成门由本
ADR 修订为“CIL 归属完整 + 胶水范围可审计 + 语义与性能达标”。严格全阶段零执行保留可选研究
profile 和原有拒绝/审计门，停止扩展为默认产品前置依赖。
主线报告必须显示实际胶水执行次数，不能输出零字节码成功标记。

## 2. 为什么选 A1

现有 ADR-0220 已用静态 C finder/loader 接入标准导入协议，复用真实 ModuleSpec、缓存、包、
None/替换值和 CPython 导入算法。它的 Runtime 初始化时序与完整 PEP451 两阶段事务仍按既有
边界逐切迁移；本决策不自动宣告这些语义完成。

stock 正常 bootstrap 后改写真实 `_frozen_importlib` 的私有算法属性（A2）已在 macOS arm64
PBS 3.12.13/3.13.3 窄探针验证接线；无需修改 interpreter 私有字段或 libpython 二进制。
但探针 fromlist 只返回包、锁函数只返回 None，没有完整事务、锁等待和并发循环语义。
后续 stock 增量 decoder 调用两版仍新增 2 次默认求值器命中。继续追求零帧会把整个 importer、
codec 和清理链重新变成 RyuPy 的维护责任。

持久化 [A1 探针](../../tests/libpython_embedding/README.md) 直接 include 产品原生 hook，
在 stock 3.12.13/3.13.3 验证了包/child/fromlist/相对导入与 codec/关闭，dispatch 为两个纯 C
模块 stub。初始化后 PEP 523 观测首次导入 56/50 帧，全为 frozen importlib；缓存普通导入零帧，
缓存包 fromlist 每次一帧，UTF-8 decoder 三帧来自 frozen codecs。真实 CIL 产品闭包另验。
前置 6 轮初稿计时缺少独立全机负载监控，且 A2 缺完整事务；正式功能验收仅 10 次/2 轮，
执行期间有其它验证负载。这些计时均不进入第 5 节产品性能准入。

现在允许必要胶水，A1 在兼容性、多版本/平台成本和可审查范围上更好。
A2 仅作为**实测热点驱动的局部优化**：必须先证明具体导入算法是实际瓶颈，再比较稳定性能收益、
完整语义和维护增量。减少 Python 帧数本身不是晋升理由。私有结构 adapter、符号劫持与源码
fork 均不进入本轮主线实施。

两版无 importlib 的实验还暴露直接 C 导入的 NULL 内部 importer 崩溃，以及 3.13 codec 注册表
初始化前置与未导出私有符号。复用 stock 正常初始化可直接避开这类自建内核责任。
这些是特定版本、平台和窄 C 探针证据；真实产品兼容和性能继续按后续门验收。

## 3. 胶水范围与审计清单

“必要”指承担 CPython 嵌入初始化、导入调度、编码/流适配、有限对象桥和关闭衔接，且保留它比
重写服务更利于兼容和维护。业务计算、请求处理、模型验证、数据转换主体与第三方业务实现
仍按 CIL 目标推进。桥转调必须以已登记 CIL callable 为目标，不能把原业务 Python callable
包一层后改称胶水。

允许族是建清单的入口，**不是执行白名单通配符**：

| 族 | 可登记内容 | 必须排除的扩张 |
| --- | --- | --- |
| bootstrap / getpath | 固定发行正常启动确需的 frozen 初始化函数与路径计算入口 | 预先解释执行应用模块来降低运行期计数 |
| importlib | 已观察到的发现/锁/规格/loader/缓存与失败清理函数；优先保留上游实现 | 放行整个 importlib 模块树或所有磁盘 SourceFileLoader 执行 |
| codec / stdio | 实际所需编码的查找、增量类型和错误/流协议回调 | 通配全部 encodings、任意用户可注入 Python codec |
| 桥接 / 反射 | 固定版本的必要方法包装/元数据适配；真实签名与对象身份约束登记 | 整个 inspect、装饰器框架或第三方业务调用链 |
| 关闭 | 实际需要的已登记 atexit/threading/weakref/流刷新衔接函数 | 通配整个清理模块树或关闭时执行未编译业务体 |

每个条目至少保存：稳定条目 ID、来源发行及精确 Python micro、模块/qualname/源码位置，
frozen/磁盘/内嵌源码类型，依赖的具体函数集合，保留理由，调用方/静态 CIL 目标，允许阶段，
准入项目/输入，预期触发条件，以及当次 startup/runtime/shutdown 调用数和耗时归因。
递归/闭包/类成员逐个列明，新增依赖不自动继承资格。固定 CPython 版本间清单分别冻结。
普通文件无需摘要；用发行标识、原始路径、源码位置与显式清单追踪来源。

旧 `_rp_wrap_method` / `_rp_method` 可作为兼容候选条目评估，先保留精确 FunctionType、
描述符及回调语义；不能仅因 native-methods 减少帧就晋升。若包装在热路径逐次执行，必须按
单次开销与代表项目总成本计量，选择原生替换或批量桥接时仍需保持身份/异常/别名/副作用。

胶水清单与业务 CIL 清单由构建期归属计划关联。未登记帧、来源漂移、运行期新增源码或漏掉的
第三方函数体进入失败/待归类清单，不能自动转为允许胶水。保留完整基础发行以维持启动、资源
和原生依赖，只控制准入执行集合；文件存在与获准执行分别登记。

## 4. 观察与边界

执行报告分列：应用/第三方/标准库的 CIL 归属；原生扩展能力；登记胶水的三阶段调用和成本；
未知解释执行；当前覆盖缺口。兼容率、CIL 覆盖率、胶水占比与性能分别计量。
清单冻结和逐函数分类仍待产品实现，本 ADR 未宣告已有机器报告覆盖全部启动函数。

`compile/exec/import` audit 事件用作补充。两版实验在 exec=0 时直接 EvalCode 仍成功，
正常 bootstrap 的 EvalCode 与 exec 事件计数也不同；不能从事件零推导解释执行零。
PEP 523/帧观察需注明精确 ABI、安装时机、子解释器、hook 替换和关闭盲区；现有半程报告继续
保持 `complete=false` / `startup_complete=false`，胶水许可不改变覆盖能力字段含义。

初始化前安装的外部 LLDB 默认求值器断点可作为研究证据，属于独立观察工具，不能冒充产品内
不可绕行拒绝。默认求值器入口命中数也可能与 Python 函数体数不同，统计字段必须区分。
完整启动清单在支持的精确版本上取得额外观察证据；日常报告无法覆盖的窗口显式列出，严格
profile 仍由原全阶段门拒绝不完整覆盖。

常规 GIL、release、主解释器与同进程单一版本/profile 继续保留。所有路线仍须验证原生线程
回调、GIL 重入、owned 引用真实释放、atexit 首次导入和跨堆根稳态。
stock 没有 ADR-0221 的内部 FiniCore fence 公共等价物；实施应先设计并验证 stock 主动关停/
持根策略，不得把 strict-only 关闭钩子直接套用。修改清理方案须保留真实 native 析构证据。

## 5. 性能预算与测量合同

**以下数字是待验证的设计准入目标，当前没有测量结果证明达标。** 基线与候选须使用同一 SDK、
同一提交的两份冻结配置、同一精确 CPython/PBS/目标/GIL、同一工具链和输入；在同次运行中
生成固定 CPython oracle，先核结果/身份/别名/异常/副作用/退出，再测性能。
产品形态按 JIT、standalone NativeAOT、onefile 分开，纯原生模式同时作为保护组。
百分比统一按候选/基线的耗时或资源比计算；吞吐量取倒数转换为成本比，避免方向混淆。

| 指标 | 初始设计预算（候选对同合同基线的回退） | 分开采集 |
| --- | --- | --- |
| 稳态 | 配对几何均值上限 +1%；单项中位数上限 +3%；单项 p95 上限 +5% | 热循环、代表项目吞吐/请求、跨桥回调、codec；不能用总体均值掩盖单项 |
| 启动/首次使用/关闭 | 各自中位数及 p95 上限为 +5% 或 +5ms，取较宽绝对窗口 | 进程启动、bootstrap、首次导入、首业务请求、清理；onefile 冷解包与缓存命中另列 |
| 内存 | 峰值 RSS、稳定 RSS 和重复回调后持有量分别不超过 +5% 或 +4MiB，取较宽窗口 | 跨堆根增长需收敛；持续增长直接阻断，不能用窗口容忍泄漏 |
| 体积 | 最终分发增量不超过 +3% 或 +1MiB，取较宽窗口 | 下载、展开、最终产品、CIL/符号/源码与许可证各类体积 |

先按相同闭包比较，避免删功能或减少资源取得表面收益。完整 PBS 相对严格内核的体积差另作
架构选型账，不能混入同合同的优化回退预算。需要突破预算时先给出实际消费者收益和代价，
独立 ADR 调整目标，禁止测完后悄悄抬阈值。

隔离采样窗口，禁止与构建或其它性能任务并发；短基准重复实际操作到可稳定计时尺度。
至少 12 个独立配对块，AB/BA 次序各半；多个候选使用平衡排列，保留全部有效样本。
尾延迟至少 60 次独立观测并报告置信区间、样本数与调度/GC/解包状态。冷/热缓存、预热、启动
与稳态严格分开，测量脚本、输入和排除规则在采样前冻结。

报告配对比值的 95% 置信区间及每项中位数/p95；区间上界在预算内才通过。
区间跨准入线判为待定，先调查系统干扰，再按预先约定
整批增加配对轮次；保留旧批，不能挑快样本或无限重跑直到过线。确认的单项或长尾回退阻断
晋升，先定位胶水调用次数、跨桥、GC、元数据可达图或代码布局，再做最窄修复并重跑受影响门。
未解决时保留原实现或局部关闭新候选；总体加速不能抵消实际场景的回退超标。

这套预算约束后续实施，本轮不构建、不采样，不报告任何百分比或毫秒收益已实现。

## 6. 迁移和验收

1. 冻结胶水分类模式与每版本清单，基于 A1 保留已有 finder/loader、对象 ABI 和兼容产品。
   清单先从真实观察生成候选，再人工核准有限函数集合，未知帧保持阻塞。
2. 逐闭包迁移应用、第三方业务与非胶水标准库到 CIL；必要 wrapper、importlib、codec 和清理
   保持原语义，不为归零重写基础设施。CIL 回调和原生扩展间接 import 都进入同一归属计划。
3. 先固定 3.12.13 和真实 3.13.3 消费者复验，再按既有 3.9–3.15 普通 GIL 与三平台发布矩阵
   扩验。cp39…cp315/abi3、Linux libc、macOS 部署版本、Windows DLL/CRT 分别记录。
4. 完成结果差分、胶水清单/调用数、生命周期、性能四组门；JIT/AOT/onefile 删除输入后搬迁，
   无系统 Python/网络依赖、资源和许可证完整。动态 import、包/fromlist/循环/失败重试、
   sys.modules 身份、真实原生回调和关闭首次导入必须覆盖。
5. A2 仅在第 5 节证据定位到具体瓶颈后另切，完整锁/回滚/缓存语义先验；不自动接替 A1。
   默认晋升与旧路径退出独立验收；严格内核研究资产保留，不用未验收候选替换主线。

本次文档修订完成决策和 stock 研究证据整理；文档作者未修改产品代码或重跑构建/平台测试，未完成
胶水清单产品化、固定项目全 CIL 闭包、stock 生命周期改造、多平台搬迁和第 5 节性能采样。

## 7. 原始资料

2026-11-16–17 主代理集成检查：`651d9d5e` 加候选工作树的 `make check` 退出零，3418 Runtime C# /
503 编译 e2e 通过，3 个既有 C# skip、295 个 e2e ignored。额外 compat 类桥 34 项通过。
严格研究测试源码显式启用编译通过，十二项 Host 实跑仍待验；常规门不会隐式重建该内核。
本目录探针经主代理复核后，对零退出但 stderr 非空、帧明细总数不符增加失败判据；两版功能
复验均通过并清理仓外产物。这些检查不含第 5 节的隔离产品性能采样。
未验收的 Host、Runtime 冷 namespace 与启动裁图候选保存在远端
`wip/adr0208-cil-execution-backup`，与本次路线/探针提交分开。

- [CPython 公开导入协议](https://docs.python.org/3.12/reference/import.html#the-meta-path) 与
  [C 导入 API](https://docs.python.org/3.12/c-api/import.html)：标准 finder/loader 与内部消费者。
- [3.12.13 import.c](https://github.com/python/cpython/blob/v3.12.13/Python/import.c) 与
  [3.13.3 codecs.c](https://github.com/python/cpython/blob/v3.13.3/Python/codecs.c)：私有交接和初始化约束。
- [PBS 维护者仓库](https://github.com/astral-sh/python-build-standalone)、
  [发行格式](https://gregoryszorc.com/docs/python-build-standalone/main/distributions.html) 与
  [许可说明](https://gregoryszorc.com/docs/python-build-standalone/main/running.html#licensing)。
- 持久复现优先使用 [stock 胶水探针说明与两版命令](../../tests/libpython_embedding/README.md)、
  [C 源码](../../tests/libpython_embedding/stock_glue_probe.c) 与
  [runner](../../tests/libpython_embedding/run_probe.py)：macOS arm64 的 3.12.13/3.13.3 功能与逐帧归属。
- 早期研究者报告 `/tmp/ryupy-stock-sol.spVbVH/REPORT.md` 的失败边界持久化摘要见
  [评估第 4 节](../libpython-noninvasive-evaluation.md#真实-stock-判别实验已读取报告未由本文作者重跑)。
