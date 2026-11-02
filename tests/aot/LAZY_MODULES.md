# 原生模块属性延迟构造：冻结测量合同（已批准，候选语义通过、性能待验）

2026-11-02。候选已实现且语义门禁通过，未执行性能采样、未批准默认采用。固定baseline 972fac072c2dbdd42e250f633a9e21b3d09cbb62，candidate为独立语义门禁通过后的固定SHA。两checkout/CLI/runtime隔离，同一Linux x64 glibc普通CI runner串行。Windows/macOS真实性能不由此推断，不阻平台验证。

## 成本路径与实现边界

baseline源码PyNativeModules.cs有28处RegisterNative：io/unicodedata/textwrap/math/time/json/re/collections/collections.abc/itertools/functools/contextlib/typing/__future__/logging/argparse/copy/datetime/random/string/enum/atexit/warnings/unittest/dataclasses/os/os.path/pathlib。另PySystem注册sys，不混入28计数。入口Initialize无条件调用Register，CreateAttributes构造字典、函数、签名，typing/unicodedata Lazy亦被求值。collections.abc/os.path已有主动Import。

候选仅13项fresh-table factory-on-first-import，其余16项（含sys、所有静态Lazy）保持eager时点；已审定13名单：math/time/json/re/functools/contextlib/logging/argparse/copy/datetime/string/warnings/dataclasses；实现源码已必要审，限WIP候选。typing/unicodedata Lazy提前求值是baseline事实，不是本候选改动；typing负载是未改非目标对照。总29=28+sys，并非29个CreateAttributes。

候选规则：仍登记所有动态可达名字，不按fixture裁剪；首次导入实际构造，重复导入/重绑共享同表。运行时实现方负责依赖、失败重试、初始化中状态、父属性与sys.modules兼容门禁。不得为了启动数值删除既有主动导入。延迟factory仍根引用所有实现，不预承诺native体积下降；统计体积仅验证不利增长。

## 固定四负载（无热循环、无内部计时）

1. hello：原tests/aot/perf_fixtures/hello.py字节不改。
2. import_typing：真实import typing；保存Any；重复import别名；打印模块身份/Any身份及TYPE_CHECKING。不能仅import未消费。
3. import_re：真实import re；compile('a+')，fullmatch('aaa').group(0)；重复导入模块身份及重复compile缓存身份。
4. rebind：import math，保存模块；将math.pi改为标记字符串，再import别名，打印身份、别名读到标记、已有函数math.sqrt(9)仍正确，恢复pi。覆盖真实公开写入，不以私有工具直写代替。

后三源具体代码和逐字stdout须在执行前由固定CP3.12.13校准，SHA+expected冻结。除构建/语义不支持的工具错误外不得变更源；有错误保首失败，必须重新审合同，不能为收益减负载。源码stdout和副作用验收交运行时实现方，另有C#厂方法计数/重入/失败重试测试，不加入计时矩阵。

## 矩阵、顺序与构建

4负载 × B/C两版本 × J(default)/T(disabletier)/A(NativeAOT) × 5独立新进程 = **120计时进程**。0额外warmup、0hot进程；不恢复180矩阵。每负载/版本各IL+AOT一次，共**16应用构建**；两版本Release CLI各一次另计2编译器构建。每版本J/T使用该版本AOT发布实际生成的同一DLL，A使用对应native；普通IL构建仅时间/哈希证据，不混作J/T输入。

全部构建在所有计时之前。固定负载顺序hello/typing/re/rebind，轮次外循环，每轮四负载，六臂顺序：
- r0 B-J,C-J,C-T,B-T,B-A,C-A
- r1 C-T,B-T,B-A,C-A,C-J,B-J
- r2 B-A,C-A,C-J,B-J,C-T,B-T
- r3 C-J,B-J,B-T,C-T,C-A,B-A
- r4 B-T,C-T,C-A,B-A,B-J,C-J

进程间不清磁盘cache、不清发布缓存；cold指新进程而非冷盘，构建已温热文件。不得为最后两轮“不完美平衡”补第六轮。SDK/ILCompiler首次restore联网、缓存状态按日志披露，不添加隐藏预热发布。

## 预算和失败

内层总**1200秒**覆盖全部子进程/构建/元数据/清理；外层CI22分钟。单应用构建180秒、单CLI构建180秒、单测量进程30秒，均受剩余总预算截断。使用原perf_runner阻塞wait4唯一reaper+watchdog仅kill本PGID，记录rusage；禁止轮询退出给毫秒级启动加量化误差。

任一超时、非0、stderr非预期、stdout/checksum不符、缺/坏stats、残留进程组、hash/schema/计数不符即实验失败，不补跑/删样本/改规模。失败保全部原始证据和未完成清单。全部120成功后才清各自owned host/bin,obj缓存，记录前后bytes；不得在计时之间清缓存。原文件/native/.dbg/构建日志必须保留。

## 每条原始证据

复用perf_schema严格9字段schema：main_ms/alloc_bytes/gc0/gc1/gc2/gc_pause_ms/jit_ms/jit_methods/heap_bytes，拒重复键、NaN、bool数值、缺/多字段。逐条保原stats bytes+SHA、stdout/stderr bytes+SHA、command/env/exit/timeout/rusage/maxrss/wall、版本fixture与DLL/runtime/native SHA。

主指标为进程外wall与全进程alloc；main_ms从RunMain才开始，不覆盖入口native注册，不能用main_ms代表初始化收益。AOT JIT计数/时长摘要N/A，原值仍保。记录所有五原值、median/min/max及逐轮C/B，不只挑中位。

记录native文件/.dbg/完整发布目录bytes；实际代码增长不能只据文件对齐后相等推断为零。构建墙钟是包含restore的端到端观测，不比较成纯编译器速度。

## 事前采用门槛（全部成功/语义先行）

- 正收益：hello全进程alloc中位数在J/T/A每臂至少下降20%；hello新进程wall中位数在J与A各下降至少5%。不满足则本轮不足以默认采用，不追样本。
- 退化阻断：任一负载/后端5个轮次C均慢于对应B，且median相对增长>5%、绝对增长>1ms，两项同时满足则不得默认采用。完整列出未触阈值的所有不利点。
- 各后端四负载median比值几何均值>1.03需人工复审，不用hello收益掩盖真实import延期成本。
- 任一负载alloc中位数增长>5%，或native文件增长>1%，需人工复审；不声称factory延期会自动瘦身。
- 阈值是采用/复审规则，非统计显著性证明；5次无置信稳态保证。边界/噪声不加测，解释不确定性，可拒绝默认化。

本合同独立于已完成I64配对，不能跨runner用历史cold作baseline。不得以性能成功解除任何语义/平台失败。不新增公共调参开关，不在观察结果后改门槛。

## 工具预算接线

CI在首CLI构建前写唯一started-at epoch；工具必收该时间，不重置1200s。两CLI各180s亦受剩余总预算。工具启动若预算已耗尽立即失败，不开启应用build。语义门禁在独立阶段，不能与计时区并行。

## 候选准入记录

6项factory C#专项、两独立进程完整公开API/导入顺序差分通过；完整make check退出0：
460 C#/228固定CP/202 e2e+30 ignored，无快照更新。4旧采样器+5新工具轻测通过，
未启动正式性能进程。每个Registration增加2引用槽，实际lazy导入增加attempt对象和
3个委托，成功getter/setter增加未完成表判定；这些成本不能被少建表的直觉掩盖。

原81进程基线workflow现在仅workflow_dispatch，原步骤/矩阵/证据全保；共享采样器
错误消息改通用预算描述不再意外追加81。它不是持续自动性能门禁；当前候选仅按
独立预注册120工作流自动验证，AOT七配置和平台语义回归另行执行。
