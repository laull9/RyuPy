# 兼容性清单与偏差

**2026-11-09候选与历史证据**：CI012显式V1十二产品run、C/D四拒绝及双模式完整check已过，640发现/401 compiled/239 pending不变；原无flag frames仍失败，整run failure。固定20为10 native/8 unsupported/2 failed，native是默认JIT标签，不是20项NativeAOT。现在普通CLI无flag有限选择已接源码但尚未产品验收，不增加通过数或移动accepted，见[ADR0143](adr/0143-generator-trace-default-cutover.md)。

> **2026-11-02恢复与排期历史记录**：当时代码恢复自已推送M3.10（`a788d86`），其已归档项目能力为5/20。此前未推送M3.11源码随临时工作区丢失，历史10/20及725项C#测试不能当作当前checkout能力。用户已暂停16/20前置，优先原生AOT/JIT，并批准小步提交推送与独立WIP远端备份。详见[ADR0042](adr/0042-aot-jit-recovery-priority.md)；原兼容目标未达成、不勾完成。

**2026-11-06架构复核基线**：db866ead 默认完整20入口为9 native / 8 unsupported / 3 failed，CP20项原stdout均等于原golden。当前九项为tomli/docopt/tabulate/texttable/chardet/wcwidth/colorama/termcolor/binaryornot；历史已通过的progress现因显式super失败，仍是默认晋升阻断，不能以新增docopt抵消。新增[按ID门禁](../tests/projects.md#p0按项目-id-的保留与默认晋升门禁)区分保九项与默认晋升，不改原选集/入口/golden；pyfiglet显式字体另计、Markdown窄源另计，20项AOT未运行。下文不同日期的矩阵继续作为历史记录。

**2026-11-05同一稳定提交的新完整矩阵**：`e066238929318cf51607a8dbad056be224a43f29` 默认配置9/20入口通过、9项编译未支持、2项运行失败，无跳过/超时。pyfiglet加已有显式字体模块选项另验通过，带该配置可覆盖10/20入口；不能称默认10/20或完整库兼容。CP20项、原源/入口/golden、冻结工具和全原流已独立核验，没有混入未提交re.split。逐项版本、失败与方法见[本轮完整报告](project-matrix-2026-11-05.md)。下文5/20及单入口窗口继续作为有日期的历史记录保留。

**2026-11-05后续单入口窗口**：干净`56aa6c62ba284d2ea46eb9b3dd5f6964ac604299`的原docopt0.6.2入口CP/编译成功，JIT仍失败，新首错为Argument缺少__class__；原stderr无栈，源码114行仅作静态定位。通用partition/rpartition及2045C#/240CP/208e2e门禁通过不计作项目通过，36ignored与未排除的旧路径性能回退另记[ADR0108](adr/0108-string-partition.md)。此窗口未重跑其它19项目，顶部完整矩阵口径不变。

**2026-11-05原docopt入口通过**：干净 `e0a9ac1014755150b1476c48d5ca558dee7a7b24` 的原docopt0.6.2固定入口唯一CP/编译/JIT均exit0；CP/JIT stdout16字节逐字等于原golden，JIT stderr空，CP四条上游SyntaxWarning保留。28份官方源、入口/golden及前后工具/工作树核验一致，没有改项目或重试。通用__class__读取与2056C#/246CP/211e2e门禁范围、39ignored及性能风险见[ADR0109](adr/0109-class-attribute-read.md)。这是此候选Linux JIT的单入口新通过；其它19项未重跑，不回写e066238同提交矩阵，不声明完整库或AOT。

**2026-11-05 TypeVar基础后的两入口复核**：干净 `aa0c54ee499c3801eeeeb2e211f88991cc438790` 上，原docopt0.6.2仍严格CP/compile/JIT通过；原Markdown3.5.2的CP通过，编译仍以`Registry(Generic[_T])`基类E0100失败，未启动JIT。五主TypeVar差分、独审与2072C#/258CP/216e2e门禁通过不增加Markdown项目分子；46ignored及未归因的性能回退见[ADR0110](adr/0110-typevar-foundation.md)。仅两项原固定入口，不改历史同提交完整矩阵口径。

**2026-11-05 ClassVar接入后的两入口复核**：干净 `eed648db7e96495c2b05436829f395fe137dfebe` 上，原docopt0.6.2仍严格CP/编译/JIT通过；原Markdown3.5.2的CP/编译通过，JIT首错已从ClassVar缺失推进至 `ImportError: cannot import name 'count' from 'itertools'`，仍不计通过。真实ClassVar有限语义及2202C#/390CP/287e2e门禁完成，107ignored保明确边界；1340性能样本保全不利，四字段TypedDict冷建稳定+512B及时间回退尚未优化，见[ADR0117](adr/0117-classvar-reintegration.md)。仅两项固定Linux JIT入口，不改顶部旧同提交完整矩阵或声明完整库兼容。

**2026-11-05 count接入后的两入口复核**：干净 `ed7bdb2d5d7b637ee8890338f72f5e661d7d3ea0` 上，原docopt0.6.2仍严格CP/编译/JIT通过；Markdown3.5.2的CP/编译通过，JIT首错由count缺失推进为 `ImportError: cannot import name 'overload' from 'typing'`，仍不计项目通过。count有限数值语义及2226C#/398CP/292e2e完整门禁通过、110ignored；唯一440性能样本保全不利，七旧路径分配不增但禁tier repeat/chain时间回退保留，见[ADR0118](adr/0118-itertools-count.md)。仅两项原Linux JIT固定入口，不改顶部完整矩阵或泛化完整itertools。

本文从 Phase 2 开始持续更新。项目集已于 M2.0 冻结（20 个纯 Python 项目，6 类用途），
CPython 基线已逐项目记录；RyuPy 侧2026-11-01整组历史结果为 **5/20 固定入口原生通过（25%）**。M2.9 已接入额外模块搜索根，
项目源码可以解析；剩余阻塞逐项目列在下表，集中在内建类型继承、导入/命名空间、typing元数据及剩余协议。
语言层面 M2.1–M2.8 的动态值、容器、普通类、异常、模块与 `with` 均有差分用例通过；M3.0 增加闭包、lambda 与 nonlocal，M3.1 增加 list / dict / set 推导式与惰性生成器表达式，M3.2 已增加yield状态机、返回值、send、现代单参throw/close、yield-from与跨暂停异常清理；旧式throw参数和通用兼容欠账仍明确列出。M3.3增加定义位置函数/类装饰器、可调用包装、函数元数据与混合dataclass动态表示。
2026-11-01 M3.7 进行中在 Linux x86_64 重跑固定项目集，wcwidth 入口编译、运行和固定基线输出一致，随后 termcolor、colorama 固定入口也通过，达到 3/20；没有新增桥接路径。termcolor 的 16 个强制 ANSI 上游场景现已通过，colorama 上游范围仍继续移植；不将指定范围通过称为完整库兼容。wcwidth 的独立上游核心九场景移植也通过，范围与许可见 tests/upstream/README.md，不另增加项目分子。
随后 texttable 与 progress 固定入口也通过。M3.7最终完整矩阵确认 **5 native / 14 unsupported / 1 failed**：chardet已编译并越过Flag，仍因bytes正则未支持运行失败。422 C#、214固定Python基线、189编译差分（29 ignored）、三个上游指定集合9/16/9及完整命令链均通过；这不代表原生16/20兼容门完成。
历史早期记录：ord/chr补齐后，wcwidth当时从ord前移至map；progress当时从生成器with清理前移到未知Progress基类。当时两者均未计通过，当前状态以上述最新证据与下表为准。
M3.3重跑attrs首阻断从类装饰器前移至Protocol基类，仍未计为项目通过。
M3.4原生系统/流/路径子集见stdlib-system.md；binaryornot首阻断从open前移至locals，当时固定项目仍0/20，Windows实际运行未验收。
M3.5历史阶段新增math/time/json/re限定API与稳定模块异常导入，逐API范围见stdlib-numeric.md；当时固定项目复测仍0/20，tomli动态except表达式等阻断未解除。后续未发布候选的单入口记录另列，不改该历史矩阵。
M3.6 工具/元数据原始验收与扩展 API 差分、297 C# / 122 编译 e2e / 134 基线均通过；构造守卫、死装箱消除与受守卫累加槽已有独立实测与回退证据。当时 2026-11-01 重跑固定项目仍 0/20，首阻断保持（map/locals/Protocol/NamedTuple/继承等），不把 API 数计成项目通过。
急切推导式解除若干前端阻塞，但项目仍需后续语法和 API，不将语法差分通过计入项目分子。
语法现状见 [semantics.md](semantics.md)，标准库计划见 [stdlib.md](stdlib.md)。

用户已批准将标准库M3.7有界交付与原16/20兼容门拆分，后者保留为M3.11未完成项；
分母、入口、成功标准与历史5/20如实登记，详见 [ADR-0032](adr/0032-bounded-stdlib-and-compatibility-gate.md)。

## 未发布候选固定入口证据索引（2026-11-04）

以下五项（截至2026-11-05）分别在不同的本地候选提交上完成 Linux JIT 单入口验证，CP/编译/JIT
均 exit0，CP/JIT 原 stdout 逐字等于各自既有 golden，CP/JIT stderr 为空。源码、入口、
工具、产物和原流已独立核验；具体哈希与边界见对应 ADR。

| 固定项目版本 | 实际被测候选 | 证据 |
| --- | --- | --- |
| pyfiglet 1.0.2 | 751afefbdc75f782b86dc958cd95b93a24682a24 | [ADR-0064](adr/0064-nonstar-iterator-unpack.md)，显式包含 pyfiglet.fonts |
| tabulate 0.9.0 | 6d62580c13bd9c12a49d70ad48bca798525598d1 | [ADR-0084](adr/0084-string-format-template.md)，原固定入口 |
| tomli 2.0.1 | 1d305806d0767866236180e24f809fc29f8c3fee | [ADR-0090](adr/0090-set-clear.md)，原固定入口 |
| chardet 5.2.0 | 59be1c15902d136f7e13d00ea8aff7c525e6717c | [ADR-0096](adr/0096-mutable-regex-subjects.md)，原固定入口 |
| binaryornot 0.4.4 | f3ba26bcd72629f63199a535efbc6a18c3aa597c | [ADR-0102](adr/0102-binary-translate.md)，2026-11-05原固定入口 |

这些历史窗口本身不是同一提交上的完整矩阵，也不是当前公开 main、完整库、其他平台或 NativeAOT
结果；不能仅将历史5/20与这些窗口相加。随后e066238完整重验另见本页顶部，不回写历史。下文保留各轮
失败记录以展示阻断演进，不能把旧失败段落当作上述候选的当前结论。

新增的来源跟踪、动态绑定和对象协议存在各 ADR 记录的成本；普通 Linux 门禁不替代
累计跨平台、AOT或性能验收。旧固定对照的 matmul 禁分层回归约14.723%仍未关闭，
这四次入口窗口仅作正确性验证，没有新性能结论。

## 2026-11-04 chardet 候选单入口：编译诊断与运行演进

在未发布候选 `6fe244ee908b1b4c78f80e978438ebbd089bdc68`（tree
`cb15ab72a354382c141409058580b0747e243cc4`）上，仅执行锁定 chardet 5.2.0
原固定入口。官方 sdist SHA256 为
`1b3b6ff479a8c414bc3fa2c0852995695c4a026dcd6d0633b2dd092ca39c1cf7`，
560成员中的501普通文件与展开源码逐项同字节；没有安装依赖或改源码/入口/golden。

一次 CLI 准备 exit0（0.114714秒）。CPython 3.12.13 exit0，stdout 19字节与旧golden
逐字相同、stderr为空。随后唯一编译达到90秒上限，进程组收到SIGTERM，exit -15，
编译双流均空，没有编译产物，也没有启动JIT。总窗口90.222128秒，仅为预算记录；
原30/90/30秒阶段及180秒总预算不变，没有重试或延长预算。

此结果是编译预算失败，不是新运行时API错误；空诊断不能定位编译内部阶段。
源码只读识别的codecs/BOM、可变bytes正则和bytes.isalpha需求不能冒作本次首错。
没有当前候选运行结果，不声明完整库、main、AOT或性能通过，历史5/20不改。
结果记录SHA256：`39107bfdd6e96da029b9253f5ea32f25f217c96db061a8608b7dd3bea30e8903`。

后续候选89ede2a增加默认关闭阶段诊断，专项和正常门禁通过后，只做一次同90秒诊断编译，
没有再执行CP/JIT。仍超时，但实际记录已定位到chardet.langbulgarianmodel的pipeline区间
未结束；此前多个模块pipeline与final-validate耗时较长。见[ADR-0091](adr/0091-opt-in-compile-stage-diagnostics.md)。
这不确定内部具体pass/校验热点，不把静态复杂度假设当作已定位根因，也不构成项目通过。

第二层候选fe1e765进一步分开pass执行与校验，专项和完整门禁通过后仍只做一次同90秒编译。
实际停在langbulgarianmodel第7个float_chain之后的校验，run已结束。已完成pass子区间累计
run 0.043786秒、validate 81.497053秒，未闭区间与外层pipeline不重复计入；见
[ADR-0092](adr/0092-mir-pass-diagnostics.md)。这些带日志计时只支持优先调查校验内部，
不证明具体根因或无扰动性能；无编译产物、无新CP/JIT，不构成项目通过。

候选74af537仅将校验器五处local查询改为安全常数查找，专项/完整门禁通过后一次同90秒复验，
已越过原模块并推进到langrussianmodel第6轮校验，但仍超时且没有产物/JIT。共同模块记录
显示局部成本改善，非稳定性能倍率；详见[ADR-0093](adr/0093-validator-local-lookup.md)。
该阶段预算失败和剩余校验成本保留，不将该结果计作项目通过。

随后候选e5744c4只复用安全helper替换module_pairs的两处local查询，保全部Facts/支配校验。
首次专项与完整门禁通过后，原同90秒编译一次在1.618965秒成功，44模块全部阶段闭合，
三产物哈希已核。冻结产物再以30秒上限唯一JIT，实际0.214848秒exit1，stdout空，
原stderr为无帧的 `ModuleNotFoundError: No module named 'codecs'`；CP复用原已核golden。
见[ADR-0094](adr/0094-module-pair-local-lookup.md)。编译预算问题在该候选解除，但入口
运行仍失败；不把单次带日志结果报成稳定倍率、不改变历史5/20。

后续按实际首错误依次补齐有限 codecs BOM、可变 regex eager、bytes/bytearray ASCII 分类及
可变 subject 的 sub/subn。候选59be1c1的原固定入口最终 CP/编译/JIT各一次exit0，原19字节
golden及空stderr完全一致，源/工具/产物/原流已独立核验；详见[ADR-0096](adr/0096-mutable-regex-subjects.md)。
仅该未发布候选的固定Linux JIT入口通过，不把前述带日志窗口当性能基准，不改历史5/20。

## 2026-11-03 未发布候选的单入口证据

候选 `751afef` 的 pyfiglet 1.0.2 原固定入口已在 Linux JIT 一次完成 CP/编译/JIT 对照，
输出与原 golden 严格一致；库源码及字体文件未修改，使用显式 `--include-module pyfiglet.fonts`。
源、工具、产物与原双流哈希已独立核验，见 [ADR-0064](adr/0064-nonstar-iterator-unpack.md)。
这是未发布候选的指定入口结果，不代表当前 main、完整库、其他字体/CLI、AOT或跨平台。
本页上述及下表的5/20仍为2026-11-01整组历史归档；本次没有重跑20项目，不改写为当前6/20。

## 2026-11-04 tabulate 首轮候选单入口：编译阻断

未发布候选 `57f857ee7d913767b5a1d2d15c8aed3d6e7c401f`（tree
`2f42c273140a20c7e743ba6449e8dfaf99097bb6`）仅验证 tabulate 0.9.0 原固定入口：
两行 github 表格及空表，不修改库或入口、不安装包。官方锁定 sdist 为81090字节，SHA256
`0095b12bf5966de529c0feb1fa08671671b3368eec77d7ef7ab114be2c068b3c`；
32个普通文件的归档成员、展开源码与记录逐项哈希一致。无必需第三方依赖；可选wcwidth未加入，
CP使用 `-S -B`、独立包搜索根并清除继承PYTHONHOME，排除site安装包影响。

Linux x86_64、CPython3.12.13、SDK10.0.403：一次CLI准备成功后，一次CP exit0，
stdout为原90字节golden、stderr为空；一次编译exit1，随后停止，JIT没有启动、没有编译产物。
实际四项E0100诊断为库 `__init__.py` 的1219/1229/1232行内建 `format` 尚未实现，
以及2636行 `except getopt.GetoptError` 尚不属于可识别的静态异常表达式。
这不是当前str子类失败，也不能把另行静态发现的zip_longest或TextWrapper缺口当本轮执行首错。

唯一窗口总上限180秒，CP/编译/JIT阶段上限30/90/30秒，超时终止另有最多2秒清理宽限；
实际两阶段共0.228958秒、无timeout，此时间只是预算记录而非性能测量。
runner自身status0仅表示证据保存成功，项目状态仍为unsupported。原runtime仅核原件哈希，
编译失败使产物runtime同哈希守门未执行，不冒产物验收。

- 原入口SHA256：`00977b83256c2f95d931ac1699dd4f40bce89ad74eab89123e30092b0af378d0`。
- CP/golden stdout SHA256：`95db25d24eea531e43aa75dcb084c7422afa0364d4e46ad15c45de813d45cf95`。
- 编译原stderr SHA256：`a3fad5e38c1a69114d00bf9e9b474a6f1c08ab438131d66102e37d924c4f511a`。
- 原结果JSON SHA256：`a291b3b572effc5b6fc6ceb54896d9ecd2a130fa793711515f3e36405ded6846`。

原流、工具及源码哈希已独立复核。未重跑20项目、AOT或性能组，未更新历史5/20，
也不声称main、完整tabulate库或另一平台通过。当时后续格式化方案须实现真实绑定与类型级协议，
不能仅把现有f-string子集导出成假完整builtin；当时getopt诊断是另一项已证编译阻断。

## 2026-11-04 tabulate 后续候选：编译通过，namedtuple 导入仍阻断

未发布候选 `3dba0b3c2b65c1d68d69926889dd9646ffdeba4e`（tree
`9fca9c5c4b224bae9724fb33b6aaf8a4adba8817`）完成有限format/getopt及局部稳定异常接线后，
只对同一官方锁定包、原入口/golden执行一次Linux JIT窗口。32源码与tar成员逐字哈希再次独立相同，
无库/入口改写、额外依赖安装或20项目矩阵。

单次CLI准备成功（0.65秒）；CP exit0、90字节golden及空stderr；编译exit0；JIT exit1、stdout空。
JIT完整原stderr仅 `ImportError: cannot import name 'namedtuple' from 'collections'`，没有调用帧。
已越过首轮四项编译诊断，但未输出表格，不能声称进入namedtuple内部实现，不称全库通过。
总窗口仍180秒、阶段30/90/30秒加原清理宽限，实际1.448189秒无timeout，仅为预算证据而非速度结论。

本轮三编译产物逐项哈希核验，复制runtime与原件相同守门实际执行；不同于首轮编译失败时未到该守门。
- runtime原件/产物SHA256：`528ca32b0d4b9d8c13ab504531609c438a5e7a67c82e975c54890ca6c839f76b`。
- JIT原stderr SHA256：`ece1519b0f24a895e3697bdf26c650f4757c1d5b6442d18785e8c579a5dc2686`。
- 原结果JSON SHA256：`7748a376719f5b440af34b300e4d382b4ea571118c201b24677f9a94451e8193`。

原包/入口、六原流、CLI/解释器/runtime及产物经独立只读审核。runner status0仍只是保存记录成功，
项目本轮应记failed；未改历史5/20、不称main/AOT/其它平台或性能通过。

## 2026-11-04 tomli 候选单入口：源码模块异常匹配阻断

未发布候选 `069f9a9b05afe4cf1608f8c785353608d535aeb1` 仅验证锁定 tomli 2.0.1 原入口，
不修改库或入口、不安装依赖。官方sdist为15164字节，SHA256
`de526c12914f0c550d15924c62d72abc48d6fe7364aa87328337a31007fe8a4f`；9个展开普通文件与归档逐成员核同。
包没有运行时第三方依赖；flit仅为包构建工具，本次不执行打包或安装，直接使用src搜索根。

一次CLI准备成功后，CP exit0、stdout逐字等于原golden、stderr空；编译exit1，随即停止，
JIT未启动且没有编译产物。真实唯一诊断为原入口第8行 `except tomli.TOMLDecodeError` 的E0100：
该候选当时只支持静态异常类或已证明的原生模块异常导入，不接受该源码模块属性表达式。
不能把这次失败解释为库的解析算法运行失败，也不把源码中另行发现的typing等依赖风险当作实测首错。

原结果记录SHA256为 `69ee18deafe03ceb055ab815511881dfad19abb614310ca6b8dcd33ca5fadfc5`。
窗口仍为CP/编译/JIT各30/90/30秒、总180秒及最多2秒超时清理；实际0.082587秒无timeout，
仅作预算记录，不是性能对比。runner status0只代表证据写入完成，不表示项目通过。
未运行20项目矩阵、AOT或性能组，未更新历史5/20，也不声称当前main或完整tomli能力。

## 2026-11-04 tomli 后续候选：编译通过，types 导入失败

未发布 Linux JIT 候选 `79d7ee571c7e143531fd128ebd5d22c3004bb243`（tree `146699039ab398c641cf0a2d779e382f06e7a061`）完成有限动态 except 后，只重验同一 tomli 2.0.1 固定原入口。上述官方归档的9个源文件、原入口与golden均未修改，逐成员SHA/exactset再次核同。

一次CLI准备成功；CP exit0、40字节stdout与golden相等且stderr空，编译exit0，随后JIT exit1、stdout空。实际45字节stderr仅为 `ModuleNotFoundError: No module named 'types'`，没有帧或源码行；本轮已越过原E0100编译阻断，但不能据此定位唯一动态导入位置、声称loads已执行或TOML解析已通过。静态源码 `_parser.py` 引用了 `types.MappingProxyType`，只是下一能力设计线索，不冒充运行定位。

- CLI SHA256：`56d68337fcb0b7d84cd896b81182db07606f51ceb9f2155611fd7fd45b9ce023`。
- runtime原件/产物 SHA256：`77ea9b3f7f706f27e0003ae53010d743e8bfc48eead5333cfad762c0c5d3a477`。
- JIT原stderr SHA256：`164828743f0fd7570e1dcbe6cb652523152f7bd4528d2824fde5129fc75d77d5`。
- 原结果JSON SHA256：`d12521cc9eca3322768936baa20706f0d4b929d854606df8474472aeb3340d0f`。

CP/编译/JIT各一次，30/90/30秒及180秒总预算、最多2秒超时清理不变，首失败即停；实际0.769548秒无timeout，CLI准备0.729434秒另计，仅作预算记录。九源、六原流、工具与三个编译产物经独立只读审核。项目本轮仍failed，不因runner保存记录exit0计成功；未运行完整库、20项目矩阵、AOT或性能，不改变历史5/20或声称main通过。

## 2026-11-04 tomli MappingProxy 候选：typing.BinaryIO 导入失败

未发布 Linux JIT 候选 `53bc95bedd4c15a5bc698dc19d3d596ffb4cb0c9`（tree `29ff4ef207a94e80566459e3f61a8191e7788ff9`）完成有限 MappingProxyType 后，仅重验同一 tomli 2.0.1 原入口。官方锁定 sdist、9 个源文件、入口与 golden 均未修改；归档逐成员字节再次核同，没有安装依赖。

一次 CLI 准备成功；CP exit0、40 字节 stdout 与 golden 相同、stderr 空，编译 exit0；JIT exit1、stdout 空。真实 stderr 为 `ImportError: cannot import name 'BinaryIO' from 'typing'`，没有帧或源码行。原 types 缺失错误本次不再出现，但不据此声称 loads 已执行、TOML 已解析或定位唯一动态导入行。库源码的 BinaryIO 注解仅是后续设计线索，不替代运行证据。

- CLI SHA256：`56d68337fcb0b7d84cd896b81182db07606f51ceb9f2155611fd7fd45b9ce023`。
- runtime 原件/产物 SHA256：`975484fdef065e8662475c1feb36d73b4788ed2964b7c47b6ded62210bc4cb9e`。
- JIT 原 stderr SHA256：`52cfd4c6a3d9203caa9f40143be46ce86d28db9fa6aa3c7fbdc5d0e544fb78d5`。
- 原结果 JSON SHA256：`a07fb7b05e0b0d6e016f8b88a0735576140bdac359ff3077c554fcd96926d801`。

三阶段各一次，仍为 30/90/30 秒及总 180 秒限时、首失败停；实际 0.596917 秒、无超时，仅为预算记录，不作性能结论。六原流、工具与三编译产物哈希已独立只读复核。runner status0 只表示记录保存成功，本轮项目结果仍失败；不更新历史5/20，不冒 main、完整库、其它平台、AOT 或性能通过。


## 2026-11-04 tomli IO 候选：typing.NamedTuple 导入失败

未发布 Linux JIT 候选 `dd7369977316b5b73fbdc2e4b2b19e1f506dc88c`（tree `cd470d3278cd97d2ccd9cc7c582f69f2437c427b`）完成有限 typing IO 三类后，仅重验同一 tomli 2.0.1 固定原入口。官方锁定归档及9个源文件、原入口和golden未改，逐成员SHA/exactset再次核同；无安装或源码替换。

一次 CLI 准备成功；CP exit0、40字节stdout与golden相同且stderr空，编译exit0，JIT exit1且stdout空。真实stderr是 `ImportError: cannot import name 'NamedTuple' from 'typing'`，没有帧。旧BinaryIO错误本次不再出现；不能据此指定唯一动态执行行、声称loads或TOML解析已运行，更不能将collections.namedtuple已实现当作typing.NamedTuple已支持。

CP/编译/JIT各一次，阶段上限30/90/30秒、总预算180秒；实际0.496149962秒，无超时或重试。result SHA256 `7ec1328233539c7865dca22446a8cba296a79ebede3f7a32c3152328a628a683`。六份原流、三份编译产物及CLI/runtime/解释器哈希已独立核对；CLI准备另计0.111676721秒。此为固定入口失败证据，不是性能测量，不改历史5/20，不代表当前main、完整tomli或AOT。

## 兼容性目标怎么计算

M2.0 已按下面规则冻结选集，规则本身不再变动：

- 至少 20 个版本固定的纯 Python 项目，覆盖命令行工具、文本与数据处理、终端与文件自动化、
  通用库与数据结构等类别；
- 每个项目预先指定入口样例、上游测试范围与成功标准；
- 分母中的一个项目只有在「编译 + 运行 + 对照 CPython 基线」全部通过、依赖可用、没有未接受
  偏差时，才计为通过；只完成编译或 `import` 不计入；
- 测试范围有删减时逐项写原因，结果标注为「指定范围通过」，不声称整个项目兼容。

原生通过率 = 原生通过项目数 / 冻结项目总数（当前 20）。若选集恰为 20 个，至少 16 个原生
通过才达到目标。桥接通过和带性能扩展通过分别统计，不能补入原生分子。每次发布保留原选集
结果；扩充或改版时公布新旧两组结果。

最近验证环境（2026-11-01；历史性能基线仍使用各笔记注明的平台）：

| 项 | 值 |
| --- | --- |
| CPython | 3.12.13（`tests/pinned-python.txt`，脚本经 `uv python find` 解析） |
| .NET SDK | 10.0.403（运行时目标 `net10.0`） |
| Rust | stable（`rust-toolchain.toml`） |
| 平台 | Linux x86_64 |
| 执行路径 | 原生 JIT（`dotnet x.dll`）；AOT 见 M5.2 |

选集、入口样例与上游测试范围见 [tests/projects.md](../tests/projects.md)；清单文件是
`tests/projects/manifest.tsv`，抓取锁定在 `tests/projects/lock.tsv`。

## 历史项目矩阵（2026-11-01）

结果由 `cargo run -p ryupy-e2e --bin projects` 实测，结论写进「结果与阻塞」列。
M2.9 起模块搜索支持额外根（`ryupy build/run -I <dir>`），项目 runner 会自动把
`third_party/projects/<包>-<版本>/`（含 `src/` 布局与依赖）加入搜索路径，因此「找不到第三方包」
不再是统一阻塞；下表保留该历史矩阵逐项目的第一项编译 / 运行阻塞；后续单入口证据另列，不覆盖历史行。

| 项目 | 版本与来源 | 入口与上游测试范围 | 传递依赖 | 路径 / 平台 | 结果与阻塞 | 最近验证 |
| --- | --- | --- | --- | --- | --- | --- |
| six | 1.16.0 / PyPI | `six_sample.py`；上游 test_six.py 公共 API 子集 | 无 | 原生 / Linux x86_64 | unsupported：内建名称 `__import__` 尚未实现 | 2026-11-01 |
| sortedcontainers | 2.4.0 / PyPI | 三种有序容器基本操作 | 无 | 原生 / Linux x86_64 | unsupported：未知基类 `dict` | 2026-11-01 |
| toml | 0.10.2 / PyPI | loads/dumps 往返 | 无 | 原生 / Linux x86_64 | unsupported：函数内动态类声明（toml/decoder.py:644，同时涉及 dict 与多基类） | 2026-11-01 |
| tomli | 2.0.1 / PyPI | 数组表与错误位置 | 无 | 原生 / Linux x86_64 | unsupported：except 只支持静态异常类或稳定的原生模块异常导入；动态类型表达式暂不支持 | 2026-11-01 |
| pyparsing | 3.1.2 / PyPI | 表达式与结果命名 | 无 | 原生 / Linux x86_64 | unsupported：暂不支持 `from ... import *` | 2026-11-01 |
| attrs | 23.2.0 / PyPI | define/field/asdict（元类路径） | 无 | 原生 / Linux x86_64 | unsupported：Enum 成员仅支持 int/str/bool/None 字面量及此前成员别名 | 2026-11-01 |
| networkx | 3.3 / PyPI | 建图、最短路、度序列 | 无 | 原生 / Linux x86_64 | unsupported：暂不支持 `from ... import *` | 2026-11-01 |
| python-dateutil | 2.9.0.post0 / PyPI | parse 与 relativedelta | six==1.16.0 | 原生 / Linux x86_64 | unsupported：内建名称 `globals` 尚未实现 | 2026-11-01 |
| click | 8.1.7 / PyPI | command/option/参数解析 | 无 | 原生 / Linux x86_64 | unsupported：`typing.Generic[V]` 参数化基类，TypeVar/Generic 尚未实现 | 2026-11-01 |
| docopt | 0.6.2 / PyPI | usage 解析与默认值 | 无 | 原生 / Linux x86_64 | unsupported：未知基类 `list` | 2026-11-01 |
| pyfiglet | 1.0.2 / PyPI | 字体渲染 | 无 | 原生 / Linux x86_64 | unsupported：内建名称 `exit` 尚未实现 | 2026-11-01 |
| tabulate | 0.9.0 / PyPI | 表格格式与空表 | 无 | 原生 / Linux x86_64 | unsupported：未知基类 `str` | 2026-11-01 |
| texttable | 1.7.0 / PyPI | 列对齐 | 无 | 原生 / Linux x86_64 | native：固定入口与九个原上游布局场景通过（指定范围） | 2026-11-01 |
| markdown | 3.5.2 / PyPI | 转换与扩展 | 无 | 原生 / Linux x86_64 | unsupported：未知基类 `list` | 2026-11-01 |
| chardet | 5.2.0 / PyPI | detect 结果字段 | 无 | 原生 / Linux x86_64 | failed（M3.7最终矩阵）：编译及Flag初始化已通过，运行 NotImplementedError：bytes 正则未支持 | 2026-11-01 |
| wcwidth | 0.2.13 / PyPI | 宽字符与组合字符 | 无 | 原生 / Linux x86_64 | native：固定入口及九个上游核心场景通过（指定范围） | 2026-11-01 |
| colorama | 0.4.6 / PyPI | ANSI 常量与 init | 无 | 原生 / Linux x86_64 | native：固定入口输出一致；上游测试移植范围继续验收 | 2026-11-01 |
| termcolor | 2.4.0 / PyPI | colored 属性组合 | 无 | 原生 / Linux x86_64 | native：固定入口及 16 个强制 ANSI 上游场景通过（指定范围） | 2026-11-01 |
| binaryornot | 0.4.4 / PyPI | 文本/二进制判定 | chardet==5.2.0 | 原生 / Linux x86_64 | unsupported：内建名称 `locals` 尚未实现 | 2026-11-01 |
| progress | 1.6 / PyPI | Bar 构造与计数 | 无 | 原生 / Linux x86_64 | native：类体 if 与 mapping % 完成后输出一致（最终全矩阵确认） | 2026-11-01 |

截至 2026-11-01 的已提交验收记录，分母 20，固定入口原生 JIT 通过 5，**25%**（Linux x86_64）；此处未重新运行项目矩阵，完整指定上游测试范围另行验收。M2.9 不要求此时达到最终 80%：当时阻塞集中在
**内建类型继承、动态基类、装饰器 / 元类、内建函数与文件 API**，属于 Phase 3 的语言与标准库工作。
后续指定入口复测单独记录候选与范围；只有完整矩阵复测才更新整组统计，不因未通过而修改分母。

## 结果状态

| 状态 | 含义 |
| --- | --- |
| 原生通过 | 指定测试范围在 RyuPy 编译代码与原生运行时上通过，无 libpython 回退 |
| 桥接通过 | 指定范围通过，但部分模块或对象依赖 libpython；列出边界与部署依赖 |
| 带偏差通过 | 仅在明确偏差或显式性能扩展下通过；列出偏差编号，不计入默认原生通过率 |
| 部分通过 | 部分 API 或测试可用；列出未通过项 |
| 不支持 | 已知阻塞，注明诊断或运行时异常及排期 |
| 未评估 | 尚未运行；不能据语法表推定可用 |

原生 JIT、原生 AOT、桥接 JIT 和桥接 AOT 分开记录。一个平台或入口通过不代表其它组合通过。

## 偏差登记

“低影响”由受影响项目、用例和测量支持。性能收益不能凭实现形式推断。后续接受偏差必须补上
最小示例、测试位置、受影响版本和替代写法。

| 编号 | 触发条件与差异 | 当前状态 / 处理 | 影响与替代方式 |
| --- | --- | --- | --- |
| D001 | 依赖对象离开作用域后立即析构、`__del__` 或弱引用回调的具体时机 | 架构边界，长期不复现 CPython refcount；相关用户 API 尚未完整实现 | .NET GC 的回收时机不同；使用 `with` / `finally` / `close()`，依赖原时机的项目列为有偏差或不支持 |
| D002 | `sys.getrefcount`、`id()` 的地址值、对象大小和解释器内部布局 | 内部观测接口暂缓，不伪造 CPython 数据 | 监测 CPython 内存细节的项目不在默认兼容承诺内；普通对象身份语义仍须保留 |
| D003 | 静态特化路径（带完整标注）的 Python `int` 超过 i64 范围 | 动态路径已按 ADR-0010 升级 BigInteger；静态路径仍是 i64，`+ - * // % abs ** <<` 越界抛 Python `OverflowError`（消息注明 RyuPy static integer limit），不泄漏 CLR 异常与 .NET 栈。示例 / 测试：`m2_1_bigint`、`snapshots.rs::fib_iter_100_overflows` | 去掉标注或在调用点传动态值即可走任意精度；静态路径的大整数结果当前受限 |
| D004 | Phase 1 frozen dataclass 赋值与传参采用值拷贝 | 已修（M2.5）：frozen dataclass 改为 .NET 引用类型，跨动态调用只复制引用，身份稳定；ADR-0008 的值类型降级不再用于默认路径。测试：`m2_5_identity`、`m1_5_dataclass` | 缺少针对值表示的显式性能扩展；需要值语义的优化必须先证明身份不可观察或由用户显式选择 |
| D005 | 深递归 | 已修（M2.2）：`PyTraceback.PushFrame` 按帧数守卫，超过 1000 层抛可捕获的 `RecursionError`，可用 `RYUPY_RECURSION_LIMIT` 调整。测试：`m2_2_recursion` | 默认上限比 CPython 保守（一层 Python 调用占多层 CLR 栈帧）；极深递归仍可能先耗尽 CLR 栈 |
| D006 | Phase 1 签名标注与模块级变量限制 | M2.1 / M2.7 已解除；M3.7 已支持函数内静态导入及 local/global/nonlocal 绑定，见[函数内导入范围](function-imports.md)。测试：`m2_1_*`、`m2_7_modules`、`m3_7_function_imports`、`m3_7_function_import_bindings`；星号导入、类体导入及函数本地异常别名作为动态 except 类型仍明确诊断 | 受限代码仍在编译期报错，不能据路线图认定已可用 |
| D007 | Phase 1 除零、数值转换等错误暴露 .NET 异常，浮点除法缺少完整 Python 检查 | 已修：真除法 / `//` / `%` / `int(float)` 统一抛 Python 异常，未捕获时按 Python 形式退出（M2.2）；索引 / 属性错误已随 M2.3 / M2.5 接入 Python 异常。测试：`m2_2_exceptions`、`m2_3_*`、`errors/unhandled_zero_division` | 无已知剩余差异 |
| D008 | `with` 的 `__enter__` / `__exit__` 使用普通属性查找，CPython 按类型槽查找 | M2.8 已知边界；类方法与常见上下文管理器无差异，`m2_8_with` 覆盖这些路径 | 不要在实例字典里遮蔽 `__enter__` / `__exit__`；需要类型槽精确语义时改用普通 `try` / `finally` |
| D009 | 两个独立代理码点拼接后UTF-16表示将其当作一个补充码点 | 已知待修，非已接受性能偏差；`ord(chr(0xD800)+chr(0xDC00))`应TypeError却返回65536，独立pending `m3_builtins_surrogate_pair`，见ADR-0025 | 普通补充平面/单代理项支持；依赖独立相邻代理码点的程序不计兼容，后续统一处理字符串表示 |

D001 接受生命周期模型差异，不自动接受任何 `__del__` / weakref 行为；实际开放 API 时仍需定义
支持范围和测试。D003、D005、D006、D007 属于待修限制，不作为永久性能取舍。

## 性能实现的边界

| 可以默认采用 | 需要守卫或用户选择 | 当前暂缓 |
| --- | --- | --- |
| 保留结果和副作用的拆箱、shape 槽位、模块槽位、有上限的 PIC（`RYUPY_PIC=0` 可关闭）、非逃逸临时对象消除 | 固定宽度整数、可观察的值类型身份变化、浮点重排、改变匹配或随机序列的替代算法；新增前写 ADR，默认关闭 | 任意元类、多重继承、完整描述符协议、`eval/exec`、动态 import hook、frame / tracing 内部接口、完整 asyncio |

有界 PIC 与动态热路径优化是当前默认性能实现，可用环境变量 `RYUPY_PIC=0` 关闭快速入口
（见 ADR-0012 / ADR-0013）。其它性能扩展仍是边界说明。字典顺序、相等与哈希、可变别名、异常传播、求值顺序和普通对象身份
属于常见行为，不按“冷门差异”略过。

## 拒绝与报错

静态可确定的不支持能力报告源码 `Diagnostic`。运行时才能知道的缺失属性、模块或非法操作，
分别抛 Python 语义的 `AttributeError`、`ModuleNotFoundError`、`TypeError` 等，并附导入或调用
位置。构建时声明的桥接依赖缺失必须明确失败，不能偷偷切换执行引擎或静默返回占位值。

低影响差异经 ADR 接受并公开登记后，可以采用高性能默认实现。默认构建的限制与偏差进入可读报告；
影响常用计算结果的性能扩展由用户显式启用，在执行前可见并进入产物记录。M2.9 已提供 PIC 的
运行期开关，统一的限制报告与产物记录仍安排在 M5.1。

## 规范依据

Python 数据模型允许不同垃圾回收实现，并提醒程序显式关闭外部资源；refcount 和内存地址属于
CPython 实现细节。RyuPy 因此保留 GC 差异，同时继续实现普通对象身份与可变别名。
[Python 3.12 数据模型](https://docs.python.org/3.12/reference/datamodel.html)

Python 运行时不强制函数与变量的类型标注，Phase 2 特化不能自行增加这种限制。
[Python 3.12 typing](https://docs.python.org/3.12/library/typing.html)

### 默认启动辅助对象的独立候选

2026-11-03：`exit` / `quit` 的默认 site-enabled 有限实现专项已通过，见
[ADR-0048](adr/0048-default-site-quitters.md)。本轮未重新运行 pyfiglet 或整个项目集，
上表历史入口结果与5/20分母不改；不把已解除某个名称缺口等同项目整体通过。

### slice 公开类型的独立候选

2026-11-05：公开构造/任意精度indices、有限比较/hash与真实类型入口见
[ADR0099](adr/0099-slice-public-type.md)。复用上一轮bytearray构造器失败源作为公开差分，
不将其等同新项目通过；复杂descriptor/任意对象比较返回及旧容器内部切片边界独立登记，
固定项目5/20分子与分母、NativeAOT和性能结论不变。

### delattr 公开入口的独立候选

2026-11-05：复用slice与typing.IO历史原失败源验收有限builtin delattr及共有删除守卫，见
[ADR0100](adr/0100-delattr-public-entry.md)。只补明确支持域与拒绝边界，不代表完整对象/模块删除协议；
固定项目历史5/20及不同候选入口结果不变，不新增AOT或吞吐结论。

## 2026-11-05 binaryornot与普通函数locals候选

干净8fa7023对原binaryornot0.4.4入口的单次CP/compile窗口确认：CP输出与原golden一致，
编译在check.py:23的locals()报E0100，JIT未启动。锁定原包/入口及chardet依赖未修改。
因此本轮选普通函数直接locals有限编译实现，而非把历史首错或已过项目重新当阻塞。
其身份、逐槽同步、声明范围、局部删除及真实成本见[ADR0101](adr/0101-function-locals.md)。
本段不是项目通过或完整模块兼容；历史5/20、20项分母及其它候选单入口记录保持。

候选c1ed0158f7465496f8c8294216877b1a9c160cd2（tree 2d5711b413b6d92534a3b8b41322a4145d49aaa4）
仅对同原包、依赖、入口和golden执行一次CP/编译/JIT窗口。CP exit0且原False True输出一致；
编译exit0，产物runtime与已冻结Release哈希一致；JIT exit1、stdout空，原stderr为无用户帧的
`AttributeError: 'bytes' object has no attribute 'translate'`。没有把缺帧补造或重跑到通过。
locals编译阻塞解除，真实下一首错转为bytes.translate；源码helpers.py:71/83确有该调用，
该位置是静态来源，不声称stderr给出了用户帧。CP/编译/JIT分别0.214951/1.618776/0.616350秒，
仅预算留证，不是性能比较。总窗口180秒，各30/90/30秒，均未超时。
未跑完整项目矩阵、AOT或跨平台，不改历史5/20、不计binaryornot原生通过。

## 2026-11-05 binary translate候选

依据上轮原binaryornot入口实际bytes.translate首错，现实现通用table/delete而非项目特判。
18项C#专项与三unique CP/JIT应用通过，包含16种buffer组合、活别名/暂停捕获、精确错误及
完整cause/context链。用户buffer、宽格式/多维/真实子类与全局空bytes单例保持明确边界；
旧通用重复**keyword差异已六入口复现并保pending，不算本切通过。
算法与实测成本见[ADR0102](adr/0102-binary-translate.md)及验证目录；最终第三次正常完整门禁1858C#/228CP/202e2e+30原ignored通过；原项目窗口另记。
未运行新的完整项目矩阵/AOT/跨平台，不改历史5/20或先前不同候选的通过计数。

功能提交f3ba26b的binaryornot原固定Linux JIT入口唯一复测已通过：CP/compile/JIT各exit0，
原golden为False True换行，JIT stderr空。原包76文件、chardet501文件、入口、golden与前轮
逐哈希一致，无shim/改源/重复尝试。结果及预算见ADR0102；不同候选仍不合并成当前10/20。

## 2026-11-05 attrs/docopt新鲜入口与list基础候选

干净177cd406原attrs23.2.0窗口：CP/golden一致，compile exit1，首E0100为_make.py:67 enum.auto，
原其它诊断保留，未启动JIT。动态类生成/compile-eval等只是静态后续依赖，不假造新运行首错。
同提交原docopt0.6.2窗口：CP/golden一致，CP原四条SyntaxWarning共732字节保留；compile exit1
只报TokenStream(list)与Dict(dict)两项基类E0100，未启动JIT。

因真实list派生依赖现有列表协议，经确认拆出精确list基础修复，见[ADR0103](adr/0103-list-primitives.md)。
该修复不开放派生准入，因此没有另跑docopt或宣称项目通过；attrs也未解除。固定历史5/20、
不同候选单入口记录与20项分母不改，未跑新完整项目矩阵、AOT或跨平台。

本切最终19项新C#、三原应用及转正对照通过；首次正常完整门禁1877C#/228CP/202e2e+30原ignored通过。

## 2026-11-05 真实list有限派生候选

继ADR0103基础修复后，真实list单继承存储、构造/root方法/默认协议接线见
[ADR0104](adr/0104-list-subclass-layout.md)。原三个CP应用与候选差分独立记录，
不把仅能定义类当完整默认协议成功；仍有明确custommagic/复杂slice/sort等边界。
docopt的dict仍未开放，当前未新增项目通过，不改历史5/20、不同候选证据或20项分母。

最终第六冻结候选正常完整门禁通过：1927 C#、228 CP基线、202 e2e及30原ignored；
六unique原生CP/JIT驱动应用、4项HIR及1项MIR结构通过。真实CIL三轮ABBA保全部负面流，
修掉误查类型字典的大回退；精确列表短操作仍记录约2–14%额外/共享CPU波动，不宣称零CPU成本。
ordinary与exact分配未增，wrapper保存方法等有真实额外分配，见ADR0104。

功能提交79a04f7原docopt0.6.2固定Linux入口一次复测：CP原golden与四条732字节
SyntaxWarning逐字不变；compile exit1只余docopt.py:488 Dict(dict)的E0100，
TokenStream(list)诊断已消失。官方28源码文件、入口、golden与177cd406逐hash不变，
未启动JIT，不计项目通过，也不把后续潜在问题假定已解决。

## 2026-11-05 精确dict必要更新基础候选

原docopt的真实dict派生前，先修四个独立CP/Ryu反例：generator失败前缀、坏pair消费边界、
mapping优先及update绑定，见[ADR0105](adr/0105-dict-update-protocol.md)。首候选1939 C#、
六CP/JIT应用与转正、三独立补充源通过；第二候选1942 C#、六原应用及四独立源复验通过。
首次正常完整门禁1942C#/228CP/202e2e+30原ignored通过。两轮真实CIL去除generator大固定
回退，list/tuple大pairs仍有约9–21%额外成本，部分显著省分配；逐例和原流见ADR0105。

新增generic协议限精确scalar键与可验证目标，普通用户键及tuple/bytes等合法域明确pending；
不能把此前这些输入偶然成功解释为流式/重入全支持，也不能将新的NI冒称CP通过。旧exact/native
raw的custom-key成功能力保留，已存hash/重入问题未修。真实dict布局未开放，未另跑docopt，
不改变20项目分母、历史5/20或不同候选的原入口通过记录。

### 真实 dict 默认有限布局（2026-11-05，ADR0106）

真实单继承与有限根协议、活视图/消费者候选实现已接，2004C#及六主应用转正前后CP/JIT通过，正常完整门禁2004C#/228CP/202e2e+30原ignored通过，两配置真实性能和不利样本均留证；不等于dict全部API或docopt通过。新布局scalar键、无核心槽覆盖，同尺寸迭代结构修改及高级view等明确NI；原失败与CP合法pending保留。c0786d1干净提交的docopt原窗口已复测：CP与原golden/四条732字节SyntaxWarning不变；compile exit0，原dict基类E0100消失，JIT exit1首错为docopt.py:466的re.split缺属性，stdout空。只计编译阻断推进，不计项目通过；固定20分母/历史5与不同候选记录不变。

### 通用re.split有限协议（2026-11-05，ADR0107）

三主有限域及独审真实CP/JIT通过，正常完整门禁2033C#/234CP/205e2e通过，33ignored包含原30及三条新边界；原失败与全部不利性能样本保留。干净bd06269原docopt唯一窗口CP/compile成功、JIT仍失败，re.split首错已转为str.partition缺属性；原stderr无栈，源码197行仅作静态定位。未改官方28源码/入口/golden，详见[ADR0107](adr/0107-regex-split.md)。本轮不改变e066238完整矩阵或宣称新项目通过。

## 2026-11-05 Generic 根与别名独立候选

先从 Markdown 参数化基类所需能力中拆出真实 Generic 根/TypeVar alias，五主差分和
12项新增C#及独审第六主已通过；正常完整门禁2084C#/273CP/222e2e通过、55 ignored。
440样本保留普通构造约+6.2%时间风险，八旧路径分配不增但不代表无回退。真实继承仍不支持，不增加项目分子；
同名导入/class原缺口完整保pending，见[ADR0111](adr/0111-generic-root-alias.md)。

干净d78ed61原docopt0.6.2固定入口再次严格通过；Markdown3.5.2仍以同一
Generic[_T]基类E0100编译失败，未启动JIT。原源/入口/golden/工具与clean前后审计
一致，不累计不同提交或选项窗口的项目通过率。

## 2026-11-05 Generic 参数化基类候选

G2第二运行时的五新主与G1原派生边界严格CP/JIT通过，14项新增C#及独审通过。
真实单父链、类体终结/参数元数据与普通实例布局已接候选；用户alias替换、完整descriptor、
IO祖先和元类不扩。正常完整门禁2098C#/284CP/228e2e通过，60ignored逐名对账：
旧55仅原派生转正，加六新边界。480性能样本保全部不利，旧路径分配不增但不代表
无时间回退。干净a97c720原docopt0.6.2固定入口CP/compile/JIT仍通过；Markdown3.5.2
编译首次通过但JIT首错为typing.ClassVar导入缺失，仍不增加项目分子。20根3187包源与
原入口/golden前后审计一致；见[ADR0112](adr/0112-generic-single-base.md)。

## 2026-11-05 ClassVar前置SpecialForm有限基础

原Markdown新鲜首错为ClassVar缺失。先修真实共享raw-key LRU与元数据基础，八主
专项及16新C#和独审通过；560性能样本完整保留，热路径省分配但冷Optional及反射
有明显代价，不能宣称普遍加速。正常完整门禁2114C#/298CP/236e2e通过，66ignored
保旧60并新增六边界。干净0713ed8原docopt0.6.2仍通过；Markdown3.5.2的CP/编译
通过，但JIT首错仍为ClassVar导入缺失。20根3187包源与入口/golden前后不变，
ClassVar尚未导出，不增加项目分子。
六条完整CP合法未知域/旧缺陷不冒充通过，见[ADR0113](adr/0113-special-form-foundation.md)。

## 2026-11-05 普通 typing 原始缓存前置

ClassVar候选发现真实暖/冷原键回归，先拆十一普通根共享raw LRU。八主、16新C#、
162旧typing专项与独审通过；九完整边界pending。720样本完整保留，热路径省分配，
冷与旧路径仍有负样本，不能称无回退。正常完整门禁2130C#/315CP/244e2e通过、75ignored，旧236通过/66ignored逐名保留；干净b5e6279原docopt仍通过，Markdown CP/编译过、JIT仍ClassVar缺失；
ClassVar仍未导出，Markdown不增加项目分子，见[ADR0114](adr/0114-ordinary-typing-raw-cache.md)。

## 2026-11-05 Tuple原始缓存前置

Tuple独立raw LRU及真实空/尾Ellipsis冷相位已接；八主、18新C#、178旧typing与独审
通过，十二完整边界pending。880样本保全部负值，普通Tuple冷禁tier仍+26.27%；不称
全面提速。正常完整门禁2148C#/335CP/252e2e通过、87ignored，旧319项逐名保留；干净2ee56fe原docopt仍过，Markdown CP/编译过而JIT仍ClassVar缺失，Callable未迁移、ClassVar未恢复，
Markdown不增加项目分子，见[ADR0115](adr/0115-tuple-typing-raw-cache.md)。

## 2026-11-05 Callable原始缓存前置

十一支持源CP/compile/JIT、18新C#与198旧typing/metadata及独审通过，十五完整边界
pending。1200性能样本保全部不利，热省8B但冷增120B，类型父关系及旧路径仍有回退。
正常完整门禁2166C#/361CP/263e2e通过、102ignored，旧339项逐名保留；干净68220e7原docopt0.6.2 CP/编译/JIT通过，Markdown3.5.2 CP/编译过，
JIT仍57B ClassVar ImportError。63输入、两份3187包源与20root次序均经独审不变。
ClassVar仍未导出，Markdown不增加项目分子，
见[ADR0116](adr/0116-callable-typing-raw-cache.md)。

## 2026-11-05 overload前置函数定义来源

真实词法限定名与首行冷登记、公开精确字符串限定名可变成员已接；七完整CP/JIT
及独审通过，五代码反射/str派生/旧genexpr自由名边界保持完整pending。正常完整
门禁2235C#/410CP/299e2e通过、115ignored，旧名称和状态逐项保留。740稳态性能
样本保13项时间不利与新增冷分配，不宣称无回退；详见
[ADR0119](adr/0119-function-definition-metadata.md)。干净ab7c65def6c86f43b24b419af29cfd98db0702f5
原docopt0.6.2 CP/compile/JIT仍通过；Markdown3.5.2 CP/compile通过、JIT仍首错缺
overload。63输入、两份3187文件源树及20根次序不变。overload尚未导出，Markdown
不增加项目分子，不泛化完整函数反射或更新旧完整矩阵。


## 2026-11-05 overload三联有限真实注册

真实overload/get_overloads/clear_overloads、统一dummy和有序强引用registry已接；六份
完整公开CP/JIT及23新C#通过。正常门禁2258C#/426CP/305e2e通过、125ignored，旧414
索引状态不变，新增16为6通过/10边界。公开code、helper重绑、native/wrapper元数据等
完整正例仍pending；API当前限定名错误前缀与partial repr两项已证差异没有掩盖。
唯一420性能样本保六项禁tier旧路径不利，keyword最高+11.15%；普通首次Main新增
默认3312B/禁tier3056B。未据分配稳定或API成功关闭历史风险，详见
[ADR0120](adr/0120-typing-overload.md)。干净cf2e28e242b08eca921546e2fc71ebf54728fc0a
原docopt0.6.2 CP/编译/JIT继续严格通过；原Markdown3.5.2 CP/编译通过，JIT首错推进为
ModuleNotFoundError: No module named 'importlib.util'，stdout为空，仍不计通过。
63输入、两份3187文件原包源树及20根次序前后不变，仅两项原Linux JIT固定入口，
不增加Markdown项目分子、不改历史同提交完整矩阵或泛化完整typing兼容。


## 2026-11-05 ModuleSpec 与普通源模块元数据

真实 ModuleSpec 类、有限 parent/has_location/cached/repr/eq/不可哈希，以及普通已定位
源模块同槽元数据已接。B3 描述符/空format修复与阶段差分保留；B4 新显式登记入口
保旧十参ABI，原样旧PE配最终runtime复验通过。B4门禁虽通过，真实CIL仍暴露普通
global意外Object化，未进入正式性能；B5仅豁免内部metadata初值引起的降级，保用户
动态读取规则。最终B5正常门禁2281 C#/791 Rust/433固定CP/433 cases selftest/310
编译e2e通过，127 ignored；完整阶段身份、原失败与范围见[ADR0121](adr/0121-module-spec.md)。

两份完整边界仍 pending：SourceFileLoader 缺失为实际运行失败，全局 del 为 HIR
E0100 编译失败，后续断言未运行。旧失败对象静态槽别名、删除后 builtins 回退、
混合 native/__main__ 元数据、自定义 eq 非 bool、util 执行和完整 html.parser 未完成。
干净功能提交889ba9a7f0fcb0036303e45e6c18e7466b1273be上原docopt0.6.2固定入口
CP/编译/JIT均exit0，CP/JIT16字节stdout逐字等原golden、JIT stderr空；CP四条上游
SyntaxWarning共860字节保留。Markdown3.5.2的CP/编译exit0，JIT exit1，stdout空，
首错仍为ModuleNotFoundError: No module named 'importlib.util'。63输入、两份3187文件
源树、原入口/golden与20根顺序、工具/HEAD/tree/干净状态前后不变，唯一两入口窗口见ADR0121。
不计Markdown打通，不改旧同提交20项目矩阵或外推完整importlib/AOT。

本切唯一16进程/336暖样本/16冷Main矩阵完成；Bnew资源分配2760→1296 B/item，
但冷Main +17648B、旧PE换runtime默认资源时间+28.89%/+25.01%与其它不利保留，
详见[完整性能报告](module-spec-performance.md)，不宣称统计显著或全路径提速。ADR0120首次Main默认/禁tier +3312/+3056B与禁tier六项
时间不利，ADR0119函数定义来源13项、count repeat/chain、TypedDict四字段+512B
及其它旧风险全部保留，不以静态ABI保留、局部功能通过或新API成本关闭回退风险。


## 2026-11-05 find_spec 有限发现与原项目新首错

真实importlib.util.find_spec有限发现已实现，完整门禁2312 C#/791 Rust/450 CP基线/
450 cases selftest/318 compiled e2e通过，136 ignored。八份完整公开源转正，九份完整边界
保pending；可写缓存、native/legacy发现、动态finder、实例执行与完整html.parser未完成。
详见[ADR0122](adr/0122-importlib-util-find-spec.md)与[发现合同](importlib-util.md)。

干净功能提交1be506924d94978f5bfa862d981e29ee6965d21c的原docopt0.6.2固定入口
CP/compile/JIT均exit0，CP/JIT16字节stdout逐字等原golden，JIT stderr空，CP860字节
上游SyntaxWarning保留。原Markdown3.5.2 CP/compile exit0，JIT exit1、stdout空，
traceback实际定位htmlparser.py:39的find_spec('html.parser')，首错是
NotImplementedError: find_spec(): legacy or native module registrations without source metadata are not implemented。
后续module_from_spec/exec_module尚未执行。两份3187文件源树、63旧输入与62矩阵输入、
入口/golden、20根次序、工具及干净HEAD/tree前后不变。没有重跑旧20项目矩阵，不计Markdown通过。

唯一同PE换runtime矩阵12进程/264暖样本/12冷Main完成。暖路径分配不变，但普通首次Main
+2008B，禁tier dynamic_defaults +41.34%/+17.77%，default资源−26.01%/+48.44%及其它
不利均保留，详见[完整性能报告](importlib-util-performance.md)。不宣称统计显著、无回退
或关闭ModuleSpec/overload历史风险；此处是测量完成，不是性能优化已验收。


## 2026-11-06 显式模块实例基础B1

已交付显式选择的真实顶层模块实例，不复制/交换静态槽，不开放不完整Python loader。
最终真实Python PE生命周期28项、外部字典/重入/导入18项通过，两份完整多文件正例转正。
正常完整门禁2337 C#/817 Rust/453 CP/453 selftest/320 e2e通过，137 ignored；
原注解/直接环境名字源仍明确E0100 pending，默认旧静态模块别名边界没有全局解除。
函数/类/闭包/生成器归属、公开module_from_spec/exec_module、AOT与native发现仍待做。

独立CIL确认默认新旧PE除COFF时间戳外逐字相同，运行时97方法项全部归因。
唯一12进程/204暖样本已完成，所有不利保留；共同首次Main +344B，default def创建
+15.91%/+7.61%、资源+21.41%/+5.53%，禁tier def创建−9.25%/+55.25%。
暖分配不变不代表时间无回退；新实例绝对成本不能抵消旧风险，历史ModuleSpec/find_spec/
overload不利仍未关闭。见[设计](adr/0123-module-instance-foundation.md)、[合同](module-instances.md)
和[全部性能结果](module-instances-performance.md)。

干净功能提交0eeaf8b2a2972fe94a9571491fdc4fd4f7d1a7d4的原docopt0.6.2固定入口
CP/compile/JIT均exit0，16字节stdout逐字等原golden，JIT stderr空；原860字节
CP SyntaxWarning保留。原Markdown3.5.2 CP/compile exit0，CP stdout 77字节等golden；
JIT exit1、stdout空，375字节原stderr定位htmlparser.py:39的find_spec('html.parser')，
仍是NotImplementedError: find_spec(): legacy or native module registrations without source metadata are not implemented。
后续module_from_spec/exec_module未到达，仍不计Markdown通过。原63输入、62矩阵输入、
两份3187文件源树、20根次序、入口/golden/工具及干净HEAD/tree前后均未改变；不重跑旧20项目矩阵。


## 2026-11-06 显式模块定义归属B2

显式选中模式的函数、有限普通类、闭包及生成器已携真实owner，支持旧对象跨重入、
暂停、同dict重执行及普通失败import重试隔离。最终真实PE77+64项与六完整正例通过；
正常完整门禁2355C#/836Rust/461CP/461selftest/326e2e通过，139ignored。
急切推导式、直接__build_class__、metaclass/特殊base、super/__class__等仍明确pending，
不推广到默认静态模块或公开loader。见[合同](module-owners.md)和[ADR0124](adr/0124-module-definition-owners.md)。

默认common/class两对PE仅COFF时间戳一字节差异，runtime全表与全部正文差异归因完毕，
PyFunction十字段/Invoke/PyCall保留。唯一20进程384样本完成，但default方法调用
+237.02%/+129.65%、module_bare +26.96%/+97.46%及其它不利不能省略。另一次16进程
有界诊断出现同版本大幅变化与插桩JIT层级差异，未能可靠定位可修源码成本，不宣称
只是噪声或无回退，停止追样。全部原值、诊断限制和历史风险见[性能报告](module-owners-performance.md)。

干净功能提交7b4956f92ae60181a8ccbe9693f8afb3ae5561c6的原docopt0.6.2入口
CP/compile/JIT全部exit0，CP/JIT stdout16字节逐字等golden，保留860字节CP SyntaxWarnings。
原Markdown3.5.2 CP/compile通过，JIT exit1、stdout空，375字节stderr仍为htmlparser.py:39
find_spec('html.parser')的legacy/native无source metadata明确NI；后续实例执行未到达，
不计Markdown通过。前后原输入、两份3187文件源树、工具及干净HEAD/tree均一致，无重跑。
完整首错与证据身份见ADR0124。


## 2026-11-06 模块裸名与 global 删除

有限默认静态槽与显式实例删除已接，8份完整正例转正，7份完整边界仍pending。
最终真实双实例/闭包/方法/生成器/重执行82项通过，正常完整门禁2377C#/861Rust/
476CP/476selftest/334e2e通过，146ignored；原源及golden不为通过裁剪。
默认__name__/__doc__/__annotations__删除、自定义builtins环境、class namespace/cell/
嵌套目标及可删builtin异常名作特殊基类等仍明确未支持。默认模块__dict__也未开放。
附加eval边界实际明确NI，但预写消息断言失败原样保留，不称所有边界断言全绿。
合同及失败→修复链见[模块删除](module-deletion.md)和[ADR0125](adr/0125-module-name-deletion.md)。

原Markdown的native html只有escape，不存在已实现的html.parser。固定CP3.12.13未改
四文件窄闭包的旧实际首错为_markupbase.py:20 del re，本切解除的是通用前置，不能
给native空壳伪造loader。公开模块执行、完整parser及原默认项目仍独立验收，尚不计通过。
最终纯PE另证四种typed字段/删除检查，三对无del PE仅时间戳差异。唯一20进程288样本
完成，default module_bare +4.002%/+185.153%与全部不利保留；旧路径分配相同不证明
无时间回退。新builtin fallback首次整段3,103,080B也单列，见[性能报告](module-deletion-performance.md)。
功能提交 6502e10 后原两项目 CP/compile 各一次通过。首次 JIT 新增 FSIZE 限制下
SIGABRT 保留；一次仅移除限制的同 PE 校正恢复 docopt 16 字节严格通过，Markdown
仍在原 find_spec 位置明确 NI。四文件窄闭包旧 del re 障碍消失，新实际首错是
html/parser.py:21:25 的 re.compile 环境名字守卫；共 21 条 E0100，无 PE/JIT。
不预设中止的底层因果，不计 Markdown 或窄闭包成功；全部 B2 及更早不利仍开放。

## 2026-11-06 实例代码同名属性有限准入

实例预检仅把 compile/exec/eval 的属性标识符与环境裸名区分，任意 receiver 走原有
属性协议；真正裸名、导入和其它环境协议仍拒绝。HIR 专项 17 项及正常完整门禁
2377 C#/866 Rust/479 CP/335 e2e 通过，148 ignored；独立窄完整程序最终 PE/JIT 通过。
原两份完整源实际越过编译，但普通属性程序在求值顺序断言失败，
builtins 程序先报模块不存在，均保持 pending。不得把准入等同执行语义全支持，
详见 [ADR0126](adr/0126-instance-code-attribute-admission.md)。

干净功能提交 4e0bf96 后固定复验：原 docopt 通过，Markdown 仍为原375字节 find_spec
NI。未改四文件闭包仅剩 parser.py:128/141 两处 super E0100，19条compile属性误报
消失；无PE/JIT，不计项目通过。两份原准入PE仅COFF时间戳两字节不同，最终runtime
全部方法ABI/原始IL/locals/EH相同，148字节构建身份变化单列，不能推断时间无回退。

## 2026-11-06 实例词法类 cell（S0）

显式实例模式的普通类支持真实词法__class__ cell：每次定义独立分配，类体期间为空，
成功终结后、类装饰器前填原类；方法、闭包和生成器保留定义类而不随公开类名或
__module__重绑。7份完整多文件正例、真实双实例/同字典重执行PE32项及正常make check
通过（2381 C#/887 Rust/493 CP/342 e2e，155 ignored）。7份完整边界原源继续pending。

super、创建hook协议、显式__classcell__、nonlocal cell、嵌套class及closure反射没有
随之开放；hook能力拒绝不模拟CP副作用/失败cell状态。B2-owned函数closure读NI、写删
readonly；不能以__dict__伪值替换真实Env。相位与成本边界见
[ADR0127](adr/0127-instance-lexical-class-cell.md)。原Markdown native仍旧NI，未改窄源
仍只剩128/141两处super诊断；不把前置功能通过计为原项目通过。

性能只完成一次限定观测：默认method_calls +310.450%、generator +40.126%、resource
+31.608%未可靠归因，不能以分配不变或三对生成PE相同抵消。runtime原metadata/token
scope/PE余区未全解析，不声明全DLL等价；见[完整结果](class-cell-performance.md)。


## 2026-11-06 实例词法零参super（S1）

显式实例普通非生成器函数的无参值调用按实际canonical身份使用真实定义类free cell与
当前首位置形参；alias、shadow、重绑、方法移植与空cell相位有完整源覆盖。9份新正例及
旧S0super1例转正，16份新边界保pending。默认直写仍旧TLS，未冒称所有super语义完成。

六参PIC与旧super专用委托修正后的准确源通过2409 C#/900非e2e Rust/518 CP/352 e2e，
170 ignored，真实生命周期PE24项重新通过。重新编译/JIT的docopt原入口通过；默认Markdown
仍find_spec NI，未改四源窄闭包编译已通过、运行推进至缺module_from_spec，不计项目通过。

一次14进程/144样本保留全部9条不利观察，默认普通实例零参+6.071%、closure+6.971%，
tieroff类body+10.579%、module_bare+10.136%。相同分配不能证明无回退，旧S0及更早风险不关闭。
范围见[ADR0128](adr/0128-instance-lexical-super.md)，全部观察见[性能结果](lexical-super-performance.md)。


## 2026-11-06 公开编译模块创建与执行

module_from_spec与compiled loader的create_module/exec_module已限域贯通，源码须显式
instance模式；真实dict、原spec/path、跨代码目标、None非包和重执行/失败由6完整正例验收。
最终2423 C#/900 Rust/533 CP/358 e2e通过，179 ignored；9新增完整边界保留。
原docopt过，默认Markdown仍find_spec NI；四源窄配置已执行html.parser，下一错误为第42行
re不支持lookbehind。不计新的默认项目通过数，不冒整个importlib或html.parser兼容。
新冷能力/模块属性分支成本未采样，旧性能风险不关闭。见[验收记录](module-from-spec-slice.md)。


## 2026-11-06 固定码点宽度后顾首切

无内部捕获/引用/条件/嵌套断言/显式边界的固定宽度正负后顾已限域验收。全门禁2474 C#、
900 Rust、549 CP、365 e2e通过，188 ignored；7新完整正例、9新完整pending。
原docopt仍过，默认Markdown仍find_spec NI；四源窄配置已过原html.parser三份完整后顾
模式，下一阻塞为sys.modules写入。默认项目通过数未增加，未重跑20项目全矩阵。
成本未采样，复杂后顾/极大数/D009与所有旧性能风险仍开放。见[验收](regex-lookbehind-slice.md)。


## 2026-11-06 受限模块别名挂接

sys.modules支持空闲顶层非注册名到真实模块的新别名，不支持完整可写缓存。
2519 C#/900 Rust/578 CP/370 e2e通过，212 ignored；5新完整正例、24新完整pending。
默认Markdown仍find_spec NI，四源窄配置推进到缺xml；原docopt保持通过，未更新20项目矩阵。
动态spec、精确setter错误、真dict与性能风险保留，见[验收报告](module-cache-alias-slice.md)。


## 2026-11-06 XML Element对象首切

真实有限Element/ElementTree、QName、Comment/PI和共享HTML_EMPTY验收；2538 C#、
900 Rust、600 CP、379 e2e通过，225 ignored；新增22完整场景9正13pending。
原docopt保持通过，默认Markdown仍find_spec NI；四源窄配置推进至缺OrderedDict，
不更新20项目矩阵或计作Markdown通过。完整XML、警告文件名、repr域及成本边界见
[验收报告](xml-element-core-slice.md)和[ADR0132](adr/0132-xml-element-core.md)。

## 2026-11-06 精确原生OrderedDict有限首切

基于21748729的最终candidate-003提供有限精确OD、双顺序、标量键、流式更新、
活视图/游标与有限dict根/消费者接线；不是dict别名或完整collections。隔离全C#
2577/2577通过，31完整CP合同真实PE实测22严格通过、9保pending；正常make check
最终通过2577 C#/900 Rust/631 CP/401 e2e、234 ignored，不等于远端发布。
原13在既有异常cause setter处失败不改源，独立19的真实
raise-from链完整通过；未知空key的NI不是CP等价。

最终canonical Release原docopt16B仍过；默认Markdown保375B旧find_spec NI。四源窄配置原两段及
extra未改，compile0/JIT1、stdout空，首错从缺OrderedDict推进为57B的缺typing.NoReturn，
仍不计项目通过。复用冻结CP参考，未重跑20项目矩阵，不改历史同提交分母或分子。

唯一默认ABBA保120正式及48暖样本；getitem/setitem两对同向不利，equality第二对
+36.812%，迭代大幅下降仍受旧进程漂移限制。没有性能无回退结论，S1/B2等历史风险
不关闭。详见[合同与验收](ordered-dict-core-slice.md)、[性能原报告](ordered-dict-performance.md)
及[ADR0133](adr/0133-ordered-dict-core.md)。

## 2026-11-07 显式super候选，尚未晋升

下一竖切为普通单继承实例显式二参super及属性目标求值相位，见[ADR0140](adr/0140-explicit-super-bound-instances.md)。
固定progress1.6原源与原CP入口已核，但产品恢复尚待hosted完整20项实际结果。原九保护
ID不改；同轮完整矩阵另约束progress与promotion，不预先给第十保护项。Instance class
AOT仍拒绝、自动默认Legacy不变；新增Legacy super Native合同不等于项目全AOT。

### Legacy动态生成器异常候选（2026-11-07）

以同已验源码树的20项目10/8/2为基线，选择click与networkx真实源码共有的动态except
与yield组合边界。当前只有CP合同前置与候选代码，未运行新产品矩阵；不增加原生分子。
networkx同机制函数未被固定入口调用，click终端分支也不能由greet入口代替执行证明。
范围与验收见[ADR0141](adr/0141-legacy-generator-dynamic-handlers.md)。

## 2026-11-09 共同来源归属候选（未验收）

原显式include空pkg.data导致warnings/功能式TypedDict来源歧义，现候选以实际code/env载体连接两消费者。有限Rust结构/类型门已完成，新增Runtime13具名测试及真实J1/J2、固定PE/JIT、旧新host、实际新Native和完整同HEAD保护尚待。Static TD旧债、二级None元类与非str显示边界见[ADR0145](adr/0145-compiled-source-provenance.md)。不改变固定20项目结果或原生覆盖比例，不宣称性能改善。
