# Scanner 局部验收记录

## 当前结论

新四主图 CP 源码合同已完成结构提升后的同源实观。旧CLI预检中construction局部class触发HIR E0100，原attr_list另窗19模块编译通过；两者都没有产品执行。新五fixture编译、C#、JIT、Native 和真实 attr_list 202B产品验收仍待执行。所有新 e2e 保持 pending，默认项目计数没有变化。

固定 CPython 3.12.13 的二进制 SHA256：`021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7`。各次启动均断言实现、版本和 optimize=0；使用最小环境、空 cwd、禁 pycache，输入与解释器前后 SHA 不变。

## 第一次窗口与真实修订

第一次计划最多五个图，实际只启动两个进程，按首次失败停止：

- construction：exit0，完整stdout74B，stderr0B。
- tokens：exit1，stdout0B，stderr802B；原断言认为 `re.Scanner([])` 可成功构造，但固定 CP 在构造阶段实际抛 `RuntimeError('invalid SRE code')`。
- live/domains/pending：未启动，不能计作失败或通过。

原五份源码、完整计划/清单、argv/env、原双流和自然exit已原样归档。没有覆盖错误原流，没有自动重试。旧34标签中的S29输入是空lexicon加L|U，并非根0；根0的新直接证据来自这次tokens失败。

修订没有弱化断言：tokens改为验证根0/False精确构造错误，并增加失败reinit保新空lexicon、旧plan和旧match、随后真正读取新lexicon的完整断言；domains非法subject改用合法非空plan，以实际到达subject消费点。其它emptyflags长尾仍pending。

## 第二次窗口

修订后另冻结四图窗口，实际启动四个进程，全部exit0、stderr0B：

- tokens：55B，修订输入后的新验收。
- live：47B，首次执行。
- domains：81B，首次执行。
- pending：87B，首次执行，仍是产品未支持的CP正面合同。

本次没有重跑construction，74B只复用第一次的相同源码成功观测。根0/False均由第二次tokens实际验证；不能把False归给第一次失败窗口。两窗口均无自动重试，历史源码和原流单独保全。`tests/validation/re_scanner_oracle/` 当前四主流已更新为下述结构提升后的实际新捕获，pending仍来自第二次窗口；源码/二进制/原进程记录hash及来源分账见 `tests/validation/re_scanner.json`。

三个原样 Markdown 3.5.2 attr_list 标准调用仍沿用已有202B CP原流，本轮没有重跑，也尚未执行产品。

## 旧CLI预检与统一结构提升

六图001计划实际只编译construction一次：HIR exit1，stdout0B、完整stderr1196B，明确E0100位于原entry.py:10的Lexicon和:93的Broken局部类。没有PE，其余五图未启动；这是既有语法边界，不是Scanner运行时失败。静态盘点另外14处同类结构不冒称实际编译失败。

独立002只编译原attr_list一次，exit0，完整stdout0B/stderr83,276B，生成791,040B PE与实际19模块图，未运行PE或旧Runtime。图包含原入口、14个Markdown模块与4个stdlib模块，无extra/md_in_html；html.parser为显式InstanceGlobals，markdown.__meta__自动采用InstanceGlobals，其余17个LegacySingleton。原3调用源码及202B golden不变，这不证明新Runtime或JIT通过。

随后五fixture共16个局部辅助类统一提升至模块顶层，用实例字段保原捕获对象引用；domains的Scanner/Setter仍继承默认__init__，环境先就绪并恢复可变类槽。原assert AST完整保留，五图计数20/36/17/52/21；原print AST、lambda/yield数量不变。旧001/002源码及证据保持，不扩Runtime/编译器，不改pending或业务源。

新同源CP窗口恰运行五进程：construction/tokens/live/domains all分别74/55/47/81B；独立properties入口后同进程再次调用，共44B。全部自然exit0、完整双流、空stderr、0重试；第五进程在每次返回后确认环境清空、Action原函数及Scanner无自身scan槽。计划97项身份和runner加入的计划/自身2项共99项前后SHA相同。四主stdout虽然与旧bytes相同，清单已引用新源码本次实际record/argv；没有沿用旧源成功结论，negative不跑CP。

## 纯控制测试

准备阶段第一轮新checker为5/5；后续增加真实selector、精确43Fact、少图多argv与业务Legacy参数检查。控制002发现主workflow为605行，原metadata自测如实失败。将重复输出语句改成等价有序循环，保全部注释与独立job原块后，workflow为600行。

控制003：新Scanner8/8、原dedent13/13、metadata23/23、原selector7/7，共51项通过。只执行纯控制/小假文件测试，没有启动被测CP fixture、CLI、dotnet、Native、下载或发布。原失败也保留，不写成首次全绿。

控制004在最终runtime003逐源绑定及强制PE/TRX原件反例补齐后，仍为Scanner8/8、dedent13/13、metadata23/23、selector7/7，共51项通过。harness绑定003的12个生产文件、4个C#测试文件及43个Fact；这仍不是C#构建或产品验收。

## 002恢复与负门窄修

001冻结包保留。独立静态审发现恢复只验证已存在run子集，以及未将state记录绑定原capture记录，均已补成严格前置：完整20条JIT、43Fact、实际C#/CLI构建及C#测试命令全部同轮成功，双流/原record/原argv和必要PE/TRX必须一起核验。旧Native helper暂换输出目录时，根state/checkpoint仍固定在job根目录；实际repo/bin测试程序集须与managed副本及工具runtime一致。

NI夹具三处按已有实现合同纠正，不改生产、不重跑CP：plan.scanner读取返回bound函数，合法调用才NI；257层嵌套要求精确RecursionError；后端可以正确快速完成，timeout不再强制发生。仅timeout场景允许RESOURCE_COMPLETED或RESOURCE_TIMED_OUT两条完整marker，分别记录实际分支；其余场景仍是单一完整stdout、空stderr与exit0。

控制005：Scanner12/12、dedent13/13、metadata23/23、selector7/7，共55项通过。新增反例包括缺单run/整图、失败run、state与磁盘原record矛盾、缺原argv/record、C#进程失败却打印Passed、稳定根persist，以及两条timeout结果对进程错误的严格拒绝。未执行产品、提交或发布。

控制006在五源结构提升及新同源oracle引用更新后，只运行一次相同四组：Scanner12/12、dedent13/13、metadata23/23、selector7/7，共55项通过。控制代码和workflow未改，原002包保全；003仅更新fixture结构、身份/参考流来源与这份文档。该结果不是C#、JIT或Native通过。

## 首次 hosted Scanner 门：测试源码语法失败

以下追加已核进展，不改写上述各窗口当时状态。旧CLI五新fixture另窗003均编译exit0，实际图各仅__main__，未执行PE；原attr_list未重复。这只解除fixture编译阻塞，不证明新Runtime行为。

Scanner专用CI实际HEAD为`df75ece84bb4b32e345a96785742169b6442c509`。官方checkpoint中的C# build自然exit1、failure=null、完整stdout1734B、stderr0B；stdout SHA256为`547109e71d67439fb9a627bc251373524e6221bebfa959d95017488cc828adf9`。Runtime子项目已生成DLL，测试项目在`PyRegexScannerStateTests.cs:189`报CS0819：隐式var声明不能含多个declarator。另有一条既有xUnit2031 warning，不将其误记为本次error。

测试程序集未构建完成，43Fact未执行，checkpoint没有TRX；本Scanner门的CLI构建、19场景JIT、attr_list JIT和全部Native阶段均not-run。其它独立保护job另行核验，不从Scanner首败推断其结果，也不沿用旧checkpoint通过状态。

修复只把context/later的一条var声明拆成顺序相同的两条，保原对象身份、断言、43Fact名称及filter。该测试文件新SHA256为`1cf95f6ba06a3998057c75bf66ef9effb3fd20cfdc6630a8d9fd89afe3a6f372`；其余15个候选源（含全部12生产文件）不变。清单保003基准来源并显式登记这一test-only身份增量，不修改原003冻结包或旧CP/CLI证据。此修复尚未经过C#重建，不能把纯控制通过当作编译成功。

## 独立完整门：case配置不被CP基线接受

默认和pilot两份完整门原stdout均为644通过、5失败；各自stderr均明确列出Scanner五个case.txt的未知timeout键。失败发生于Python基线配置解析，不能说这五个entry已执行失败，也不能把其pending标记当作跳过CP参考的理由。

Python基线的parse_manifest不接受timeout键，execute固定30秒；Rust CaseSpec::from_case_dir支持该键，但初始timeout_secs也为30。修订仅从五个case.txt删除冗余的`timeout = 30`行，使Rust解析值和CP固定超时不变。entry、args、所有断言、pending=true和源码pending标记保持；没有放宽任何parser或跳过用例，没有重跑CP/旧CLI。旧失败配置与两门原流保留，配置解析验证与纯控制结果另列，不能代替整组重验。

一次纯配置验证调用真实Python解析器：五份旧配置仍精确拒绝timeout，五份新配置全部接受且entry/pending/args/env/期望值不变；没有调用execute或启动case。Rust默认值只作源码静态核对，不冒称运行了Rust解析器。合并这两项前置修复后，控制007只运行一次同样四组55项，12+13+23+7全部通过。原控制代码、workflow、CP参考流及003历史包不变；C#重建、五例CP完整门与JIT/Native仍须新轮实际验证。
