# Scanner 首切验收准备

状态：两次历史 CP 窗口、辅助类结构提升后的新五进程 CP 窗口及纯控制测试已有真实结果，详见 `RESULTS.zh.md`。旧CLI预检曾因construction局部类报E0100；原attr_list另窗19模块编译通过，但未执行。新五fixture尚未编译，C#、JIT、Native 尚未执行，workflow尚未发布。所有新 e2e 仍为 pending，当前四主图stdout已绑定新同源CP原流，pending保原CP来源；NI仍待产品执行。

## 冻结范围与分账

生产实现采用普通 Scanner 类、内部复合 Pattern 子型和现有 Match；验收代码不设计另一套正则执行器。只有无内部捕获的首切语法是候选支持。内部捕获、backref、全部 lookbehind、scoped/atomic/possessive、可变 buffer、私有 backend、groupindex、Pattern repr、Match.expand 等另保 CP 正面 pending，产品 NI 不替代它们。

输入清单是 `tests/validation/re_scanner.json`。四个候选完整图位于 `tests/cases/re_scanner/{construction,tokens,live,domains}/entry.py`，每图无参数或 `all` 跑完整断言，另接受少量 argv 场景。完整 pending 图位于同目录的 `pending/entry.py`。NI 图是 `tests/fixtures/re_scanner/negative.py`，不被普通 CP 基线发现。

八组覆盖：

1. construction：构造/scan 绑定、重复 string 优先级、逐项迭代/解包/当前语法错、生成器首次 action 下标、用户迭代异常身份、失败 reinit 保新 lexicon/旧 plan/旧 match。
2. tokens/flow：至少三个实际 token、跳过 None、左优先、完整输入锚点、capture-free lookahead、零长立即停、完整 remainder、不匹配，根0/False空lexicon构造错误及失败reinit现场。
3. tokens/constants、exception：不调用 bool/len，返回对象身份，实例同名 __call__ 不覆盖类槽，回调异常原身份、cause、context、suppress_context、traceback 引用与已保存 match 现场。
4. live：每 token 两层 live 下标、词条/lexicon 替换、单轮固定 backend、m.re 原计划、独立 nested cursor、不恢复公共 match、action 查找失败及属性异常身份。
5. domains/flags、text-bytes：根0 ASCII 与 Unicode 字面、显式 U/I/M/S、global-inline 的词法/执行分离、混合 exact str/bytes、astral 码点、非法 scalar、外部 flags 与 inline flags 错误优先级。
6. domains/metadata：真实 type/isinstance、compile 身份、非零 flags 拒绝、purge、原 subject 身份、pos/endpos、group/groups/groupdict/span/lastindex/lastgroup/regs。
7. NI 图：保存的 bound 方法先 binder 后 NI；模块消费者在 subject/count/template/index 消费前拒绝；私有 backend 不半调用；源/flags/语法消费顺序；项数、单源、总源、嵌套、累计译文和 timeout 资源门。嵌套上限保持精确RecursionError。timeout仅允许精确完成([],原输入)或精确RuntimeError两条固定完整marker，每个JIT/Native原位/搬移独立记录分支与timeout_observed；正常完成不冒超时覆盖，进程超时/信号/任意异常仍失败。资源拒绝不冒称CP同错。
8. domains/properties：普通继承、match setter 在真正调用前替换类 __call__、实例/类 scan 重绑、最终 scanner setter 失败不回滚新 lexicon。

## 有限 CP 窗口与源码身份

固定版本 CPython 3.12.13，二进制 SHA256 `021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7`。执行前后 fresh hash，源码及完整 argv/hash 同样记录。消费机的固定绝对路径只在外置执行计划里登记，不写进可发布候选。

第一次窗口计划五图但实际两进程后按首失败停止；修订输入的第二次窗口实际四进程通过，construction同源74B复用第一次观测。各次每进程30秒、512MiB地址空间、每流64KiB；第二次总预算120秒/512KiB。完整输入、启动脚本、argv/env、原双流、自然exit和前后SHA均已保存，bootstrap断言optimize=0。专用CI仅复核这些已保存原流，不再重复局部CP采样；正常完整基线仍保持独立门。

旧CLI预检发现函数内class属于既有HIR边界后，五fixture的16辅助类统一提升到模块顶层。普通constructor只保存原事件列表、异常或结果引用；domains两Scanner子类不增加__init__，环境先于构造就绪，入口及finally恢复Action原函数和Scanner真实继承槽。全部原assert AST、marker和回调/生成器结构保留；pending及原业务源不动，不扩编译器。

结构提升后的新窗口实际五进程全部exit0、stderr0B：construction/tokens/live/domains的all完整输出74/55/47/81B；独立properties入口后同进程再调用一次，完整44B，并在每次返回后核环境、原Action函数及无自身scan槽。每进程30秒/512MiB/每流64KiB，总150秒；计划97项身份加runner两项共99项前后不变。当前四主oracle引用这些新捕获，不能挪用旧源的成功记录；旧源与原流单独保全，NI不跑CP。

已有业务 `attr_list_entry.py` 与 `attr_list.stdout` 原样复制，分别 SHA256 `caf016dfcca374517b8ef6becccf1152c637edbdb23a15be88a884e3befb33d1` 和 `dc300b8e53382cab9e070d6f5489dffef85297718925168b918f55a2296cdda2`。这三个 Markdown 3.5.2 标准调用的202B既存 CP 原流继续使用，不在本新窗口重跑，原归档 SHA256 `e1ac7b3dc550ee80e602e71c1d168002f062e49f1b11e26a36264dafd4df2ef8`。

## 最小 hosted CI 控制方案（尚未启用）

1. 复用 `textwrap_dedent_contract` 的完整双流、TRX、相对路径检查和原 metadata 的身份/单次采集器，不复制另一整套框架，不修改旧 helper 语义。新纯控制模块与自测为 `scripts/re_scanner_contract.py`、`scripts/re_scanner_selftest.py`。
2. 新 job 先纯控制 selftest，再独立 C# build、writer 提供的精确 Fact 清单及过滤器。现已绑定43个公开Fact和精确filter，名称来自实现测试源码；零测试/skip不算通过。
3. 一次 CLI 工具构建，每个源码图编译一次 PE，逐 argv 执行13个候选场景与6个负门场景（4个纯NI、1个混合资源门、1个timeout观测）。只创建四个候选 Native 图加一个 NI 图，不按标签建 ELF；不构建 pending Native。
4. Native 前上传实际 PE、Runtime身份、全部 stdout/stderr/record、TRX 和同轮 checkpoint。恢复先重核 HEAD/tree/run_id/run_attempt/manifest/全部 sources/tools/产物及双流，重新计算通过状态，不继承旧 passed。恢复必须精确包含19个图argv加attr_list共20条JIT成功记录，缺一个run或整图均停止；原capture的.json/.argv.json与双流一并绑定。43Fact以及实际dotnet test/build、CLI build命令也须完整成功，不能仅凭TRX或DLL恢复。错误state使旧pass全部失效。
5. Native 发布发生后先保存实际 dist/ELF、publish日志和 manifest，再校验。原位及搬移各按场景运行，原始编译输入撤离的范围明确登记；失败仍保产物，不能只留 summary。不自动重试。
6. attr_list 原样包及标准入口实际 JIT，完整202B及空 stderr逐字核对。用原合法 Legacy 配置预检 AOT 图与资源，可承载才真实发布/执行；否则保存真实 publish 前诊断，不扩全模块 AOT、不把局部 Scanner Native 当成 Markdown Native。
7. `.github/workflows/re-scanner.yml` 已准备，尚未发布或执行；`wcwidth-legacy-aot.yml` 只增加精确标题 selector 与独立 reusable job。dedent/metadata/baseline/fullcheck/projects/S1/S2 等原独立 DAG 保留，不因 Scanner 失败而 skip。旧 search/noreturn 的历史范围保持原分账。
8. 原完整 extra 的178B入口源文件、77B stdout golden和默认20入口不改。显式 full 应实际前进并记录下一首错；默认 source metadata NI、显式 AOT 的 E0100 和 TreeBuilder后继仍分别记录，固定计数维持11/8/1，直到原整组门验收证明变化。

## 纯控制反例

新 selftest 已验证：exit1、负信号、timeout、残流、缺stdout、缺stderr、额外流字节、错 CP/source hash、NI冒CP、旧HEAD/tree/run/attempt/manifest/source/tool恢复、修改PE、缺绑定原流、state偷带passed、TRX零测试/重复/skip/错名。测试只用几字节假文件，不启动被测产品。控制自测通过不等于 Scanner 通过。

## 空间与证据取回

本地保存小源码、计划和获准的小oracle；旧CLI预检只从已有官方ZIP选择性提取所需工具，不安装SDK或下载整套旧产物，产物与原流先冻结再按该窗口计划回收所提取工具。验收脚本不自行清理或提交/发布。CI 大工具/ELF单独归档；后续先取清单与失败原流，按实际需要单取一个 PE/ELF或流式抽取所需成员，并记录每件hash。保留可恢复的远端完整产物，不预留/承诺本地700MB，不以摘要替代可追溯原流。
