# ADR-0096：可变正则 subject 的分阶段活视图

日期2026-11-04。状态：阶段A/C/B已验；阶段F有限replacement/escape的429专项、三unique应用及正常完整门禁通过。固定项目证据另列，不代表完整库。

## 背景

codecs BOM候选之后，锁定chardet入口真实执行至universaldetector.py:220，因bytearray正则subject未支持而失败。不能把bytearray预先转为不可变bytes来掩盖缺口：CP的Match.string保原对象，保存的匹配稍后读取group会看到修改；迭代器还具有不同的导出寿命。

唯一固定CP3.12.13参考在30秒预算内实际0.032306秒exit0、stderr空，完整134个唯一JSON标签、8族。脚本SHA c527602979a46cd460813ae7e4c5219a36a49eb7319310b98a4eb567a4ab21be，stdout SHA d65eb0ca9024529bb539e682980caa7e368dcc072b5505d82c64b4a5688a2ca5。官方CP源码只读交叉与实际结果分开记录；没有以危险回溯或异步信号制造异常。

## 阶段A时的分阶段合同（B/C当前进度见后文）

A仅开放bytes pattern对exact bytearray和已有一维连续B/c memoryview的match/search/fullmatch/findall，以及这些结果的既有Match API。view切片偏移相对view，readonly view也保原身份。捕获结果为bytes。参与组每次访问先处理组参数，再按当前buffer长度裁切；span和endpos保持匹配时标记，未参与组不读取buffer。released view的参与group按regex协议抛TypeError，未参与组/default、固定span、.string和不读取参与捕获的expand仍可用。

扫描需要短期导出租约，返回Match不长期锁resize。Match保存原对象和组标记，不保大块后端输入快照。pos/endpos的__index__回调先于获取扫描租约，用户异常原身份/现场不改写。所有本次扫描成功和异常路径都释放自己的租约。

view自身导出计数与bytearray exporter计数分开。公开release在活动导出时拒绝，不先破坏view状态；finalizer不能调用会抛BufferError的公开release。每个regex view lease另持bytearray exporter保护，view和lease最终化先后均不依赖另一个对象仍可公开访问。清理幂等，构造失败回滚；自然回收时点沿既有D001边界，不用强制GC伪证确定释放。

B随后实现finditer：创建即锁resize，最后一个yield仍锁，下一次真正耗尽才解除；next之间同长写必须可见。C随后实现sub/subn：前缀、回调、后续扫描、尾部读取各有真实时点。A不提前放开这两类mutable入口。mutable replacement/template/escape仍是独立域，不因subject支持自动开放。

## 性能与明确边界

exact bytes保原执行路径和一次Latin1编码，不加可变内容版本扫描。A可变eager扫描在租约内一次编码，group只复制实际捕获区间，不每组复制全源；新增租约计数/同步、匹配标记和返回bytes具有成本，未测量收益。当前方案每个memoryview（即使不用regex）新增生命周期锁对象和计数字段，release增加锁；共有Match新增可变域引用/判断。不能把保exact扫描算法表述成整个对象路径零额外成本。

B若每次next刷新全窗口，输入N和M个匹配可能O(N×M)复制。保完整前缀才不破坏锚点/边界；ResizeEpoch不能检测同长写，不能当缓存失效凭据。marks-only Match避免每个结果长期保留完整快照而形成同样量级的驻留内存。后续优化须覆盖全部写路径，不在A扩全局写框架。

当前未支持任意buffer exporter、原生子类、多维/宽格式view或新regex语法。CP非连续view本身拒绝；多维/宽格式是合法但本运行时未开放的正例，二者不能混称非法。既有单执行流允许同步回调重入，不宣称公开List存储已经支持任意CLR线程并发修改。该设计阶段未新增chardet成功、AOT、跨平台或性能基准结论。

## 验证计划

内部专项覆盖两级导出、失败回滚、公开release和最终清理两种顺序、捕获活读、缩短/复长、参数副作用、异常身份及exact旧路。最多三项公共CP/JIT差分覆盖新eager与原exactbytes/text，保全部原流；未实现边界保真实CP正例pending。先独审隔离源码再首次专项，公共接线差分之后正常完整门禁，按实际结果更新本节。

首次内部专项尚未进入测试：新增测试的纯throw lambda被C#选择为Assert.Throws(Func<Task>)，触发CS0619。原日志保留；已显式Action消除重载歧义，不更改异常身份或finally释放断言。后续真实结果另记。

窄修后首次实际运行65项专项（新增15及既有bytes regex/memoryview回归50）通过，三项真实CP/JIT应用首次3/3通过，含两项新域及原exact bytes/text回归；当时正常完整门禁进行中。源码/应用原流保留，未执行新的chardet入口。

随后首次正常make check exit0：fmt、严格clippy、workspace、1666项C#零失败跳过、228项CP基线、202项e2e及原30 ignored通过；日志SHA256 `f2303743f9c014e6b4df0368e96a1d4f34bcb603a5a94b010d2c3176a301dadc`。该门禁阶段候选未发布，尚未新跑chardet。应用归档仅保输入/原流与产物哈希，DLL不打入源码恢复包；原执行目录的全量产物另已独立重hash，归档实际子集有独立清单。

## 阶段A后的固定项目单入口

冻结候选0509d90a51f9acaeb480923912a2a4aaf429f435随后只执行一次锁定chardet 5.2.0原入口。CLI单次准备成功；官方包/501源码文件、原入口及黄金输出先核哈希。CPython exit0（0.265015秒），编译exit0（1.971170秒），Linux JIT exit1（0.716860秒），总2.954184秒，无超时；仍用30/90/30及总180秒限制，首错误停止，未重试。

实际JIT原stderr仅`AttributeError: 'bytes' object has no attribute 'isalpha'`，无帧，stdout空；不能据此指定内部唯一调用点，也不能把不再出现旧mutable subject拒绝说成整个项目通过。CP原stdout仍为固定19字节黄金输出，stderr空。结果JSON SHA256为`f03b6fcd6e948b0f64066e1fa7628658be1343bdd918cdfee88b3349c075fccc`；JIT stderr SHA256为`f63e368e3227d1de82e3762befd775e5ba8e459824e0250c3e4f9a36d70b591a`。这是未发布候选的单入口Linux JIT结果，不更新历史5/20，不代表全库、main、AOT或性能测量。

## 阶段C：可变subject替换的内部准备

ASCII分类候选后的原锁定入口真实到charsetprober.py:73的re.sub（详见ADR0097），故先推进通用sub/subn，不要求先公开finditer。合同复用既有134行CP原流，新增sub组合仍由公共应用CP-first验证；没有重跑旧参考或第三方项目。

准备顺序仍为count转换、callable/模板准备、subject获取。扫描冻结初始长度，prefix在回调前复制，尾片段按结束时内容读取；Collect作用域确定释放租约后才Join。子调用有独立lease，任何扫描/展开/回调异常立即释放，不套用finditer的重试保留政策。公开Match仍只保原source/marks，捕获活读，不保宿主全量快照。

literal/template无Python回调时一次完整Latin1编码；callback成功且需续扫时才使输入dirty并重编码完整前缀窗口，内部空匹配retry失败/游标推进没有新用户写，不重复编码。回调最坏O(N×M)复制成本明确，不能用仅suffix或ResizeEpoch陈旧缓存省掉语义。原exact bytes/text正文不迁移，公共接线将仅增加mutable/view分流。

C1当时仅两个新增内部文件及9项专项，支持已准备的literal/template/count和Collect→释放→Join；当时callback与公共mutable sub仍不开放。每调用可选内部受控executor仅供失败finally测试，默认直接RunBytes、不造delegate，不使用全局hook或危险超时。C1首次专项9/9通过，零失败跳过；当时尚未公开接线或完整门禁。

C2补callback修改、活捕获、全窗口刷新及异常；C1/C2均闭合后C3才接公开sub/subn并做最多3个CP-first/JIT应用和完整门禁。mutable replacement/template/escape、mutable finditer仍为独立未支持域，CP合法正例保留，不据实际项目只做bytes快照特判。

C2随后补齐内部callback正文，首次20/20专项通过（原9及新增11），当时仍未公共接线。原E的paxaT→QZabY得到p##bY、marks活读、嵌套租约2→1、None/晚Join/原异常释放、全前缀dirty刷新均内部验证；三公共应用的新增CP组合当时仍待执行。exact bytes/text路径未改变，未运行新的项目或完整门禁。

C3公共候选现已接线：原count和模板准备之后仅新增mutable/view分流，exact bytes正文逐字保留。原阶段性sub NI期待按已实现域窄迁，finditer/mutable模板/结果/escape不变；新增两项公共准备顺序专项。当时完整regex/memoryview专项、三个CP-first/JIT应用和正常完整门禁待验，不能把C1/C2内部通过外推成公共能力验收。

C3首次runtime窗口384/384通过（完整regex及memoryview回归）。driver首次命令误填不存在的mutable_regex_substitution目标，Cargo在选择target时exit101，未执行任何CP/JIT应用；原失败日志保留。更正为已审mutable_regex_sub后，第一次实际三应用3/3通过，45/8/22标签及完整七帧异常链保持，原输入/双流和产物hash留存。新sub空重试、前缀词边界刷新、嵌套sub等组合现由实际CP-first核验，不再只是原finditer参考推导。当时首次正常完整门禁进行中，未新跑chardet。

阶段C首次正常make check随后exit0：fmt、严格clippy、workspace、1702项C#零失败跳过、228项CP基线、202项e2e及原30 ignored通过。日志SHA256 `6b649c54bdbb9dba1027f8b7265dba071e5ec0e7d910d2d0658cbb3c0d69c58e`。源码各阶段增量、三应用原流/完整链与原产物hash独立核验；恢复包保78项输入/原流等文件，DLL只记hash不打包。候选未发布，该门禁阶段尚未再次运行chardet；不增加项目通过率或AOT/性能声明。

## 阶段C后的固定chardet入口

冻结候选 `59be1c15902d136f7e13d00ea8aff7c525e6717c`（tree
`aa3872b7c1c03c2177863269481e9e389a1a72da`）完成备份后，仅执行一次锁定chardet 5.2.0
原固定入口。官方sdist SHA256仍为
`1b3b6ff479a8c414bc3fa2c0852995695c4a026dcd6d0633b2dd092ca39c1cf7`；
501普通源码文件集合/逐文件哈希与归档一致，没有安装包、改源码/入口/golden。

CLI单次准备exit0（0.165182秒，独列）；CPython3.12.13 exit0（0.214875秒）、编译exit0
（1.619447秒）、Linux JIT exit0（0.666377秒）。三阶段各一次，总2.502024秒，无timeout；
仍为30/90/30秒及总180秒、最多2秒终止清理宽限，首失败停止、不重试。此次未开阶段诊断。
CP与JIT原stdout均逐字为19字节 `utf-8 0.505\nUTF-16\n`，stderr均空；编译stderr仅产物路径。
这些时间仅为预算记录，不是性能基准或稳定倍率。

结果JSON SHA256 `834eb7d64e478219013a17e661f4dd02479736e2a67b31b46b612dbebdbb3a58`；
stdout/golden SHA256 `2ac46b754effd0e2746634a311e925ad16797d643f1823ca31c39eb41df0d059`。
候选身份、CLI/runtime/解释器、501源码、六原流及三个产物均已独立重核；独审报告SHA256
`9c27c2e8b627ba3c5ef2d16236a29d0435b6472aad9a0be2edfeff5256b10f00`。
本次只确认该未发布候选的原固定Linux JIT入口通过，不代表完整chardet、全部编码/输入、
当前公开main、跨平台、NativeAOT或性能验收。历史5/20不改；文档归档未重跑完整门禁。

## 阶段B：可变subject的finditer内部状态

固定项目入口通过后继续补通用finditer，不再重复已通过入口。复用原134行CP参考C/D/E，
源码独审后B1仅新增内部显式IPyIterator与15项专项；公共入口当时仍NI。无需编译器或
通用PyIter改动，exact bytes/text的原Apply/Enumerate/PyRegexIterator保持不变。

创建时即获取原subject租约且不编码；每次next最多一次完整[0,endpos) Latin1复制，
同次空匹配retry/advance复用该快照。Match只保原source和marks；最后一次yield仍持租约，
真正耗尽才Dispose并清Current/source/lease/pattern，耗尽后不复活。遗弃由既有lease
finalizer兜底，不新增iterator finalizer，不承诺CP引用计数式即时释放或强制GC测试。

非Stop后端/分配错误保上次成功checkpoint、Current与租约；下一次显式next才再试，
不隐藏重试。这是Ryu资源策略，不声称等同CP异常后cursor：CP相关scanner/calliter源码
仅支持非Stop不自动清scanner拥有关系，原小参考没有故障注入。内部每次Step的可选受控
executor仅用于测试，正常调用不创建/保存delegate、不使用global hook或危险regex超时。
重入Own错误与外层guard恢复单独验证，不因此宣传正常扫描存在Python回调。

B1首次内部专项15/15通过、零失败跳过，包含五载体、创建/最后yield/真耗尽、前缀与alias
活写、候选advance后错误不提交、重入/原context、耗尽清引用及两种确定性资源清理顺序。
当时这是内部验收，公共B2接线、三项CP-first应用与正常完整门禁仍待；没有新项目/AOT/perf。
创建O(1)，M次产出及耗尽检查最坏O(N×(M+1))复制成本明确，不称性能优化或零成本。

B2随后仅在原两个Index转换完成后的mutable分流加入factory；eager、sub与exact/text执行正文不迁移。
原阶段性finditer NI期待按新域窄迁，混合replacement等F正例继续pending。首次接线误写不存在的
shared.patch路径，public测试文件未被apply；改为冻结tests.patch后首次执行，没有CP/JIT重跑。
完整regex/memoryview专项首次401/401通过，三公共应用首次3/3通过（47/9/28标签、完整七帧
用户异常链），原流/源/产物随后独立核验。当时正常完整门禁进行中；不重跑已通过chardet入口。

阶段B首次正常make check随后exit0：fmt、严格clippy、workspace、1719项C#零失败跳过、
228项CP基线、202项e2e及原30 ignored全部通过；日志SHA256
`9e7977af90df661fff4873e9e1dfec73d24c1e552428fd8d586678c7c9604698`。
三应用84标签及完整七帧链、原源码/CP与JIT双流/九产物独立重核；恢复包归档90项实际输入/原流等
文件，DLL等仅记录hash而不复制，原完整执行树保留。候选未公开，没有重跑固定chardet、
项目矩阵、AOT或性能测试，mutable replacement/template/escape等剩余域仍独立待实现。

## 阶段F的补参考与晚Join首切

阶段B完成后，仅为剩余replacement/template/escape域新增一次已审小参考，复用旧F而不重跑项目。
CPython3.12.13在30秒预算内实际0.032428秒exit0、stderr空，67唯一标签/65观察/5族；
脚本SHA256 `620cabfa92b860866d119250054ca447da6cbecaf42f109ff636bdedf476cb26`，stdout
SHA256 `b07420ffec80a9328c58c34ef0fd2c6c9859910a344f7a2163bf99b8aa4e221b`，原流独审。
实证区分bytearray模板快照、writable view模板hash ValueError、readonly无效模板AttributeError，
以及readonly暖cache在release后仍成功而purge后失败；不能把全部view模板统一快照当成兼容。
F2范围只扩bytearray模板/expand和buffer escape，view作为sub/expand输入仍NI并保CP正例与错误差异。

F1先只开放callback返回bytearray及现有连续B/c view的晚Join，覆盖所有已支持subject。
原callback结果引用保留至全部callback完成；不在返回时复制/锁定。Join按parts顺序取得所有lease，
覆盖输出分配和复制，finally全撤；先验证全部片段再空值/容量检查，released空view不能被跳过。
复制直接进入最终数组，没有逐片中间bytes；源扫描lease在Join前已结束。现exact Join已有mutable
NI判断只改为冷分流，exact全bytes旧拼接正文与单片身份不变，没有新增判断或lease数组。
新冷路成本为O(parts+输出)、O(parts)引用及逐mutable片段Acquire/Dispose，不宣称性能数字。

原F的同一增长bytearray返回最终XYYXYY、view晚释放的错误索引与全部callback先运行保持。
新返回空bytes使用regex局部Empty，不等于全局b''驻留已实现；CP的mutable空结果全局身份及
exact escape空/单byte身份正例单列pending，不偷换测试EMPTY，也不扩全局PyBytes构造缓存。

F1六文件源审后首次完整regex/memoryview专项415/415通过（新增14项及既有401），零失败跳过。
两旧mutable返回NI断言按新域窄迁，str/int晚错误与模板/escape未开放边界保留。
当时唯一公共callback应用及F2、正常完整门禁仍待；没有新项目、AOT或性能运行。

F1唯一公共callback应用随后首次1/1通过，52标签与最后完整五帧Join错误链保留，
CP先核exit1及原流再编译/JIT。真实六载体晚引用、全部callback先运行、错误索引、独立租约
与后callback原异常优先均由此应用差分核验；当时F2与正常完整门禁仍待，不把阶段通过称全域交付。


## 阶段F2：模板与escape有限闭合

bytearray replacement在原count转换后准备，短lease检查反斜杠；literal保原引用走晚Join，模板复制为bytes后交原parser，error.pattern因此保不可变快照。Match.expand无论literal或template均快照。memoryview模板仍NI，不把CP的hash/cache/release差异吞掉。escape只接受现有bytearray/连续B/c view域，短lease下计算最终长度并直接填数组，两遍O(n)、一次最终分配；释放view按CP该API的TypeError处理。容量沿Array.MaxLength明确NI，实际OOM不捕获/重试，不加任意小上限。

F2八源经独审接线，新增14项及三旧NI期待窄迁，完整regex/memoryview专项首次429/429通过，零失败跳过。两新应用加原callback应用首次2/3：escape与callback通过，模板应用CP0/JIT1，原stderr为bytearray不支持item assignment且没有用户帧；结合首标签前源码与SetItem分派，独立审定位为旧slice赋值缺口，不冒实际帧定位。首失败原流保留，不能把该旧语法缺口冒成正则模板失败或放宽断言；后续仅审测试表达方式，未扩bytearray生产语义。当时正常完整门禁尚未运行，没有新项目、AOT或性能测试。


随后独审仅迁七处测试slice写：变长用同对象del+extend，同长用逐index写；原213行CP成功source逐字保pending，所有断言/输出未删。模板应用单独第二次CP-first 1/1通过，30标签；因此三unique应用各已通过（30/38/52标签），不是首轮3/3。29条view模板CP记录及来源哈希也在仓内pending保存，不计通过。正常完整门禁随后启动，终态见下段。


阶段F首次正常make check最终exit0：fmt、严格clippy、workspace、1747项C#零失败跳过、固定CP基线228、e2e202及原30 ignored全部通过。完整日志SHA256 `4a0eaeb6afe56cfbf85fcb933178ab3cb19b3e091adfcc09a9e1a17bbfe37e96`。门禁内三项公开差分亦通过；此前首2/3与单独R1复验历史保留，不改口为所有首试通过。结果为本地未发布有限API候选，没有重跑固定项目、项目矩阵、AOT或性能组。新冷分支/租约/快照成本及view模板、驻留身份pending保持。
