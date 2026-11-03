# ADR-0052：AOT 构建目录、交付目录与基准后端

状态：WIP；源码必要审与纯工具专项通过，布局 Rust 专项14项、5项假 SDK 生命周期通过，完整正常门禁通过；未执行新原生发布或基准，未 main。

## 用户入口

GNU make 不接受自定义 `--aot` 参数。提供 `make bench-aot`，等价 `make bench ARGS='--aot'`；底层 `tests/run_benchmarks.py --aot`。不替换系统 make，也不修改任何基准源或规模。

高层 `ryupy build --aot path/name.py` 默认生成 `path/name.build/` 和 `path/name.dist/`。显式 `--out-dir ROOT` 对 AOT 表示共同根，得到 `ROOT/name.build/` 与 `ROOT/name.dist/`。普通 JIT 默认 `__pycache__/name/` 及显式输出语义不变。

`.build` 保存每次独占 attempt 的托管程序集、运行时、SDK host、日志及中间输入；`.dist` 保存最终原生程序 `name`（Windows 为 `name.exe`）、必要外置内容、符号旁件和 `ryupy-artifact.json`。内部 host 仍用唯一程序集名称，不能与被引用的 `name.dll` 撞名。最终改名后重算清单相对路径、哈希和完整文件集合；不删符号或猜测依赖闭包。资源继续位于 `_internal`。唯一根级 native 文件名允许安全 Unicode，保留原先合法中文入口；非 native 旁件、资源与 selected anchors 仍保持原 ASCII 合同，不放宽资源目标命名。

低层 NativeAOT publish API 保留随机独占工作树，只有高层构建使用此布局。CLI 报告实际 managed/native/manifest 路径；验收工具据本次报告查证，不从历史目录任取可执行文件。

## 所有权与失败

同目标独占锁拒绝并发。build 内版本化 owner 绑定源路径与 stem，完整 ledger 记录根内文件/目录、大小、哈希和条目权限（不含 build/dist 根本身的 mode）；既有 build/dist 未拥有、被修改、增加内容或存在链接时拒绝接管，不删除用户文件。用户选择的 ROOT 先规范化，内部边界拒绝 symlink/reparse。

候选在本次 attempt 内准备、核验，再将旧 dist 移为备份并安装新 dist。安装失败恢复旧版本；只有最终内容与 owner 提交成功才返回成功。两次 rename 不是跨平台原子交换：正常离开释放本轮锁；崩溃遗留或已存在锁不自动接管/清除。回滚或元数据提交失败保留现场并明确失败，后续以 owner 不一致拒绝继续，不自动猜测状态。

首切保留历史 attempt/backup，因此校验成本随历史总字节线性增长。元数据上限32MiB，单文件8GiB、完整树64GiB、100000条目、深度64；实际有限读取与变更检查，超限明确失败而非自动删缓存。可选择新的输出根。验收工具主动删中间缓存后的树不再宣称可通过原 ledger 复用。

## 基准口径

AOT 每例在全新输出根编译，必须通过本次清单、输入哈希、原生头和执行权限核验；只直接执行 `.dist` 原生入口。编译失败不读取旧产物，也不回退 dotnet/JIT。`compile_ms` 包含完整构建和 ILC，不是纯 CIL 或纯 ILC 时间；构建在 wall/main 采样之外。

保存和报告明确 `jit` / `aot` 后端，默认拒绝跨后端 baseline 比较，显式 `--save` 同名目标若已有不同后端或无效结果，也在 CLI/用例构建前拒绝且保留原字节；同后端保持原覆盖行为，不新承诺并发原子写；无后端字段的旧结果只按历史 JIT 解释。原1次热身、5个新进程计时、执行顺序和输出校验不变。失败或输出不匹配的记录不进入有效加速比/几何平均。

wall 包含进程启动；main 从运行时 MarkMainStart 到统计点，含入口执行及该段 JIT/GC。资源列仍来自最后计时进程，wall/main/RSS 保留原中位数聚合；标为 MiB 而非含混 MB。AOT 仅 JIT 专属列展示 N/A，原 stats 保留，分配/GC/main 不隐藏。默认 JIT 固定窗口不等于 Tier1 稳态。

当前基准 runner 使用 POSIX wait4，仅声明 Linux x64/macOS arm64 AOT 模式；包装发布本身仍须三平台验证。`--aot --profile` 在 runner 构建 CLI/用例前拒绝，make 的 runtime prerequisite 仍可能先运行。无新增 Windows benchmark 或 AOT profiler 承诺。

## 验证与历史边界

本轮正常 `make check` 终态 exit0：fmt/clippy、587项C#、228项CP基线、workspace、202项e2e通过加30项ignored。未用快照批量接受或FORCE。此门禁不代表新布局真实NativeAOT或基准运行完成。

新布局解析及构造薄工具在固定 CPython3.12.13 下26项纯测试通过；bench首8项纯 mock 测试通过，补保存保护后固定13共10项通过，均未执行真实 SDK publish、native 或性能样本。原4项 CLI 假 SDK 生命周期及新增无 `-o` 的相对路径用例合计5/5通过，验证源码旁 `.build`、失败无 `.dist`、不写 JIT 缓存。布局首次 cargo 因集成漏复制3个新模块在编译前失败，补齐后原12/12通过，Unicode native 与非 Unicode 资源隔离增量后14/14通过，独立 Python 清单核验9/9通过（均固定3.12.13工具，Windows分支尚待真实宿主）；保留失败记录，不计为测试通过。

活跃普通/平台/资源/绑定/构造/coverage 工具迁移到新报告路径；原判据和预算不变。冻结的 module-call 固定36及历史 perf/pair/lazy 工具和匹配工作流不改；这些历史脚本仍绑定旧布局，不宣称可直接用于新高层输出。最终36历史结论和 matmul 禁 tier 回退仍开放，不由本布局或新 bench 接口关闭。

## 最小真实命令管线（首次远端前记录）

本机未发现 clang 或 ILCompiler 缓存，不盲目启动本地发布。计划在原 Ubuntu AOT smoke 环境增加一次 `make bench-aot BENCH=hello ARGS='--save <本次唯一名>'`，只用原 hello 源，保默认1次热身+5次计时，即依据固定 runner 推导 CP/native 各6次；不冒独立逐进程追踪。额外仅1次 hello AOT构建，不能当全基准组或 M5 性能复现。

现有 Runner 对整个 make 进程组强制1800秒总期限（包括 runtime、Release CLI、ILC 和采样），外层step32分钟，原Linux整job45分钟仍是更外层上限，前序步骤可能缩短本步可用窗口，不保证完整1800秒；没有单应用30秒或单publish600秒限额，不改原wait4计时器。保存make原双流、原saved JSON、唯一fresh bundle关联及清单/输入哈希，检查backend、warmup/runs、唯一hello成功与5个计时点；不重跑输出，也不导出速度结论。4项纯mock在固定3.12.13通过，真实流程仍待授权远端执行。

已有三平台布局/资源/绑定/构造回归保原预算与判据；新增布局解析纯测接入现Linux/host环境，不新增基准平台矩阵。生成的 `*.build/`、`*.dist/`、`*.build.lock/` 仅按目录加入本仓库ignore，已核无现存tracked目录冲突；不自动删除任何目录或忽略源码/JSON。

## 原始 ABC 示例的默认目录验证（首次远端前记录）

macOS 另增加一次用户指定的原 `tests/e2e/phase3/m3_6_abc.py` 功能验证。原字节复制到独占目录，保留 basename；实际公开 CLI 使用 `build --aot` 且不传 `-o`。预期源码旁 `m3_6_abc.build/`、`m3_6_abc.dist/` 与正确命名入口。固定 SDK10.0.403，单次 CP3.12.13参考、单次 AOT发布、单次完整 dist 搬迁后 native执行，0次JIT；原始双流逐字比较，并核完整清单及输入/产物哈希。

原 source/build/dist 根整体改名留证后才执行搬迁副本，不删除证据；SDK仍安装，不称干净机器或 macOS12实机验证。统一900秒总期限，publish最多600秒、参考及native各最多30秒，末尾核验也受总期限约束；step16分钟及原host job55分钟是外层上限，前序步骤可能缩短窗口。固定3.12.13下3项纯测试通过，真实应用与发布尚未运行。

本批预定四个现有工作流共9个job、21次发布：Linux原9次加hello1次，Mac/Windows原合计4次加Mac原ABC1次，绑定三平台3次，构造三平台3次。原覆盖不删减；固定36与历史性能工作流不触发。hello只验证公开基准管线，ABC只验证原示例默认布局，不据此发布全基准性能结论。

## 原始证据归档完整性

四个本批流程仅对工具自身的证据根启用隐藏文件上传，不上传仓库根、home、全局SDK/NuGet缓存或凭据。这样保留SDK生成的隐藏 `AssemblyAttributes.cs`，避免沿用旧轮该文件仅runner记录的缺项。owner本身为非隐藏 `ryupy-build-owner.json`。coverage原有主动删中间缓存策略不变，其记录不冒删除后可完整恢复原build ledger；其他有意删改的负例副本也不冒原树。

hello另将本次唯一fresh `.build/.dist` 普通树复制到证据根 `owned-bundle`，保隐藏项、空目录、条目mode与owner原字节；复制前后size/SHA/mode全核，副本清单及managed/runtime输入再核，原1800秒期限不变。失败仅在唯一安全fresh树且仍有预算时尽力归档，归档失败不遮原make错误。owner哈希用于原构建证据，不表示其绝对source绑定在复制后可再次构建；根mode不在既有ledger范围。此增量固定3.12.13共6项纯mock通过，未新增任何应用执行。

## 首轮真实验收（6faa，保留失败）

候选 `6faa3a68adb2168b32fe6f66eeb787b1493f851c` 的四流程共9个job已终止：8个成功，Windows固定原生绑定的发布失败。因此不称整批验收全部通过，也未合main或追加旧36。对应run：Linux41598273421、host41598273396、binding41598273362、construction41598273429。

用户指定的原ABC示例在macOS真实无`-o`构建通过：原源码SHA `81e0141c247e4aed3e9aefe8babc828e49d97f3f98d7c531b576fa16a2f4d23a`，源码旁 `.build/.dist` 和 `m3_6_abc` 入口；CP1/publish1/搬迁native1/JIT0，原双流逐字相同。原根改名后运行，owner及完整build/dist、隐藏SDK中间文件均独立重哈希；SDK仍安装，不称无SDK宿主或macOS12实机。

Ubuntu真实 `make bench-aot BENCH=hello` 管线成功；唯一AOT后端保存记录、原1warm5参数、5个计时点及原runner输出一致状态已核。CP/native各6仍是固定实现推导而非独立进程追踪。完整owned-bundle共234普通文件逐size/SHA核验，owner、managed/runtime/native/source和隐藏SDK文件一致；真实ILC完成，stderr空。此为接口功能验收，不以该hello窗口发布性能结论；跨后端同名save字节保护仍是mock回归证据。

Mac/Windows普通和资源包的原行为、完整异常帧、稳定原生名称与搬迁清单已独立核；它们沿原harness主动删除输入树，因此不冒build owner完整重核。构造三host保留的全输入树/owner均已核，同AOT DLL的JIT2/native2原双流与原positive/boundary完整帧一致。Linux/Mac固定绑定原10/9应用、声明/静态archive/完整schema3及保留owner树核验通过。原有功能范围和SDK/系统库依赖限制未扩大。

Windows绑定首失败为SDK已输出 `Generating native code` 后的 `LINK LNK1104`，无法打开 `obj\\Release\\net10.0\\win-x64\\native\\link.rsp`。原清理已删除该SDK树，不能证明当时rsp文件存在或把路径长度称唯一已证原因。依据原日志，本轮host路径221个UTF16单元、推算rsp265；旧同平台成功记录host168、推算rsp212，新布局层增加53。保留此失败，再评估仅高层owned attempt内缩短重复唯一私有目录及Windows SDK中间路径预检；不改系统长路径设置、不吞LINK错误、不以不变候选重跑隐藏问题。


## Windows 绑定恢复候选（恢复远端前记录）

仅高层布局创建的私有 `OwnedAttempt` 能申请较短的固定内部 binding/SDK 子目录；创建仍排他，既有目录、链接、逃逸和本轮attempt之外的边界拒绝。公开 JIT bundle 与低层随机 SDK 工作目录合同不变，用户可见 `.build/.dist` 和稳定原生名称不变。

Windows 在 SDK 前按实际 host 名、RID、framework 计算七种已观察到的 SDK 中间/输出路径，按 UTF16 单元检查小于260；超限提示使用更短的 `--out-dir`，不改系统设置。这是 SDK10.0.403 已知路径的有限预检，不保证未来所有 SDK 文件，也不把首轮失败的路径长度推断升级为唯一原因。

仅持有高层 `OwnedAttempt` 的失败发布保留 SDK 工作树，在判断退出状态前保存原始 stdout/stderr；父布局将失败 attempt 纳入 ledger，旧成功 dist 不动。低层/JIT 原失败清理不变。这样后续失败可保留实际 rsp 现场，而不是用重复执行推测。

本轮本地 driver 库测试82项、CLI假SDK生命周期5项通过；其中布局15项及独立路径3项均已单独验证，fmt和strict clippy通过。测试覆盖实际host后缀/UTF16边界、短目录已有项和逃逸拒绝、高层失败留证及旧dist/错误码保持；不代表真实Windows LINK已恢复。

绑定工作流此前没有监听共享 driver 依赖，现补 `aot*.rs`、`binding*.rs` 和 `lib.rs` 三条实际依赖路径，保原三平台矩阵、预算和断言。拟恢复批次仅原Linux/host与绑定三平台共6个job、18次发布，构造首轮三平台通过结果保留；尚未启动，须另获本轮执行确认。固定36、main及系统配置不变。


## 恢复验收完成（ec6，2026-11-03）

恢复候选 `ec6ff3b0a265523cef80f242e52b83cfe24853fa` 的三个工作流、6个job、18次发布均完成，通过后逐包独立核验；没有重跑构造专项或固定36。原 `6faa` Windows失败保留，不用本轮成功宣称它的唯一根因已被证明。恢复前正常 `make check` 亦exit0：587项C#、228项CP基线、202项e2e加30项ignored，以及fmt、strict clippy和workspace测试通过。

Windows绑定真实ILC与9项应用通过，SDK stderr为空；归档中实际 `link.rsp` 为215个UTF16单元、最长已列 `.sourcelink` 为257，七种预检路径均小于260且文件可重核。完整build owner的232文件/18目录与dist3文件、隐藏SDK项、schema3、静态archive均重新核验。JIT与AOT分别构建，分别验证输入与搬迁副本，不能称两者是同一managed DLL。

Linux/Mac绑定10/9项行为及各自owner树通过；普通/资源Mac与Windows四包、Linux原smoke/7coverage/资源判据均通过。旧工具主动删除的源与缓存仍只保输入记录，不冒完整owner可复建。SDK仍安装及系统依赖、ZIP权限仅记录等限制不变。

原ABC在Mac再次实际无`-o`构建并搬迁运行，保持正确的 `.build/.dist` 与入口名；新hello实际 `make bench-aot` 唯一管线成功，保存AOT后端和原1warm5，完整234文件归档已核。CP/native各6次仍依据固定runner推导，不冒独立逐进程追踪或全基准性能结论。新结果与官方run链接见 [恢复验收记录](../validation/aot-layout-recovery/README.md)。

这是已验证WIP源码，不自动替换旧 `releases/` 二进制，也未合main或创建正式发布。使用新功能须从此源码重建CLI。历史matmul禁tier约14.723%回退仍开放，不由布局修复关闭。


## 合入main后的普通CI路径断言修复

经明确确认，已验代码及上述记录在 `94bb8e6e35271452d3ab16ed370d99abc394e626` 正常快进到main；未创建正式Release。此前“WIP/未main”为各验收阶段的历史状态。

普通CI [41605821572](https://github.com/laull9/RyuPy/actions/runs/41605821572) 的Ubuntu通过，Mac在driver库测试80/82后失败，未进入其e2e。两处断言用原临时路径文本比较生产返回的canonical路径，实际是 `/private/var` 与 `/var` 别名差异；不是本轮真实ILC/原生程序失败。该Mac日志实际Rust为1.99.1、.NET SDK10.0.403/runtime10.0.14；工作流仍使用浮动stable/10.0.x，不声称已固定未来版本。Mac原日志SHA256为 `f67f0f313124b46eb94b263fbee64a6b6be7482e78bdeaea1ce7c3c980c2169b`，首失败保留。

修复仅规范化已经存在的预期dist/native目标，不规范化或改变实际返回值；真实alias输入、边界symlink拒绝、精确basename、重复发布旧内容及执行mode断言均保留。原prefix断言进一步改为完整native路径相等。没有生产代码或文件安全策略变化，不因宿主别名而放宽边界。恢复验证仅普通main CI，不追加原生组或固定36；本地及远端终态分别记录，不把代码审查当运行通过。

修后本地布局专项15/15通过；正常 `make check` exit0，587项C#、228项CP基线、202项e2e加30项ignored，以及fmt、strict clippy和workspace测试通过。未新增真实SDK发布或基准样本，新的Mac普通CI仍待验证。
