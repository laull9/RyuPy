# S0 词法 class cell 的有界性能观测

2026-11-06。状态为 **BOUNDED_STRUCTURE_OBSERVATION_COMPLETE**：唯一正式窗口10进程、144个暖样本完整成功。此处的成功只指固定执行协议完成，不是全DLL等价认证，也不声明显著性、总体无回退、所有冷反射零成本、真实项目或AOT性能。

默认 method_calls **+310.450%**、generator_create_resume **+40.126%**、resource_files **+31.608%**，全部不利值保留。相同分配或相同生成PE不能抵消这些时间不利；未可靠归因，不解释为噪声，不关闭风险。本轮只做每配置一对A/B进程、各3个进程内样本，没有补采、重跑、追加JIT诊断或挑选好值。

历史模块删除 module_bare +185.153%，以及B2 method_calls +237.02%/+129.65%等不利仍开放；本轮其它路径的改善不作抵扣。历史证据见 [module-deletion-performance.md](module-deletion-performance.md)。

## 工具、输入与固定窗口

A为已冻结guard基线 `c138b6c279e3a704d527e585c8fa0c24df70b38a`，CLI SHA `279b59cdcee9e524767a8df0a046c0b06c4e85191c884217c5240984c84c1a8d`，runtime SHA `27029d9c2dbeef1f68d00d4b023e8fe6bd04441d2db72ad7fa9a2f16857c42f7`。
B为S0最终CLI SHA `e42e43f650d74f7718020b24157cf63e12e83b953db14eb6d691ebe968567088`，runtime-final-002 SHA `7e708c4ac8b65542d775e326de31a1e85cca44b03a2da9dd7cd70d3ae4a91c7b`。B的旧runtime-final-001仅保留历史，不混入本轮；A仍使用其独立基线目录内的runtime-final-001。

A/B从同一物理common/class Python源分别编译，默认正式路径复用旧owner Host，不另造C#模拟Python实现。每配置common各9路径、class各1路径；feature仅B的4条真实Python PE路径。显式instance无cell第三对PE只做结构和准备验收，不增加正式进程。
旧Host SHA `279163dc056ca02ad8a58211e55cec5ba2047332575c3489fecbff138924a68a`；feature Host SHA `0eac3eeeb53250d333380323bef5611141a269c90a5032ab77f106c149e4e0de`。实际运行 .NET 10.0.14，workstation GC；default未设tier覆盖，tieroff设DOTNET_TieredCompilation=0、DOTNET_TieredPGO=0。

准备24个子进程：1 CP oracle、7 CLI、1 Host构建、9份完整audit、6个无暖机/无正式样本smoke，均按固定顺序成功。既有完整功能门禁为2381 C#、887 Rust、493 CP、493 selftest、342 e2e通过、155 ignored；这不替代性能或完整metadata证明。

正式顺序固定：default common-A/B、class-A/B、feature-B；tieroff common-B/A、class-B/A、feature-B。common循环路径64次暖机、每样本512次完整调用×64 item；plain_def_create/closure_create/generator_create_resume为256暖机、8192次完整调用。class为256暖机、8192次完整类body执行。每路径2个正确性probe、1个暖机阶段、3个正式样本。

仅离线读现有结果复核：10份命令/结果/Host记录及20份原流的身份、退出状态、唯一marker、完整操作数、加载身份、probes/warmup/checksum全部吻合；96个probe、48个暖机阶段、144个样本及56项Host检查完整。逐样本重算CSV和20个A/B中位数、8个feature中位数，与原summary逐项相同。原流总stdout 300B（10个marker）、stderr 0B，10次exit0，无timeout/stop。暖样本GC0/1/2合计为156/0/0，原逐样本GC记录完整保留。
正式子进程wall合计6.918878092秒，不等于整窗口墙钟。50ms间隔采样的单子进程/进程组最大RSS均为63,979,520B，不是连续精确峰值。准备上限720秒、正式上限240秒和总根96MiB均未扩；收尾核读时证据根86,887,086B。

## 全部20组A/B中位数

每格是单进程内3样本中位数，正变化不利。分配为测量线程字节，不是进程总分配；CPU是进程CPU时间。ns/item分母严格沿旧路径：循环调用按64个item摊销，其余按完整调用。不同指标分别取中位数，不把CPU/分配当wall替代证据。

|配置|路径|A ns/item|B ns/item|wall变化%|A→B B/item|A→B CPU ns/item|
|---|---|---:|---:|---:|---:|---:|
|default|static_calls|270.546|285.188|+5.412|33.000→33.000|271.057→286.652|
|default|dynamic_defaults|128.702|127.427|-0.991|33.000→33.000|129.425→127.991|
|default|plain_def_create|85.315|88.689|+3.955|192.000→192.000|92.163→96.680|
|default|closure_create|85.902|88.330|+2.827|256.000→256.000|89.478→91.431|
|default|module_external|920.430|690.881|-24.939|24.625→24.625|921.906→691.986|
|default|module_bare|976.015|863.946|-11.482|96.625→96.625|978.760→866.516|
|default|resource_files|3309.064|4355.009|+31.608|1312.000→1312.000|3308.197→4543.579|
|default|method_calls|61.571|252.717|+310.450|1.000→1.000|62.286→253.906|
|default|generator_create_resume|859.525|1204.420|+40.126|408.000→408.000|861.450→1207.764|
|default|class_module_body_execute|4348.166|3937.170|-9.452|1984.000→1984.000|4353.027→3941.284|
|tieroff|static_calls|71.411|62.813|-12.041|33.000→33.000|72.540→63.477|
|tieroff|dynamic_defaults|37.587|39.197|+4.284|33.000→33.000|38.177→39.825|
|tieroff|plain_def_create|43.395|41.104|-5.279|192.000→192.000|46.631→43.091|
|tieroff|closure_create|56.426|62.253|+10.326|256.000→256.000|58.228→64.575|
|tieroff|module_external|162.415|162.872|+0.281|24.625→24.625|164.062→163.544|
|tieroff|module_bare|205.424|221.767|+7.956|96.625→96.625|205.872→222.260|
|tieroff|resource_files|1073.623|998.489|-6.998|1312.000→1312.000|1074.493→999.573|
|tieroff|method_calls|45.678|46.004|+0.715|1.000→1.000|46.631→46.478|
|tieroff|generator_create_resume|228.427|224.090|-1.899|408.000→408.000|232.666→231.201|
|tieroff|class_module_body_execute|1760.945|1583.925|-10.053|1984.000→1984.000|1765.747→1588.135|

## 全部8项feature绝对中位数

新功能不受旧CLI支持，因此没有同语义A/B。本表每item均是一完整操作，不能与不同class body的1984B或其它no-cell路径假配对求回退比，更不能把1800/32/256/0B说成纯CellNew/CellGet的单次成本。四条路径均调用真实Python PE；CallValue、Host委托分派和结果保存计入，绑定方法在计时外保存。

- cell_class_execute：128暖机、每样本1024次PyModuleInstances.Execute。完整执行类body、创建原类/cell/两方法env、终结、能力guard、填cell和重新绑定；每次新cell，旧方法/闭包仍持旧类。1800B是整段执行的线程分配，包含guard实际工作。
- method_read：256暖机、每样本8192次保存的绑定方法经CallValue读取词法__class__。32B包含真实绑定调用边界，不是纯读取cell成本。
- closure_create：256暖机、每样本8192次保存的capture方法调用并创建新lambda。256B是完整创建操作；lambda env持同一个raw cell，不创建新class cell或复制类值。
- retained_closure_read：256暖机、每样本8192次已保存闭包调用。0B是此暖调用的线程分配，不表示闭包/cell免费创建或没有保活成本；保活原类、原cell及owner globals的正确性独立检查。

|配置|路径|ns/完整操作|B/完整操作|CPU ns/完整操作|
|---|---|---:|---:|---:|
|default|cell_class_execute|3376.407|1800.000|3404.297|
|default|method_read|157.252|32.000|160.767|
|default|closure_create|517.907|256.000|520.264|
|default|retained_closure_read|118.912|0.000|120.972|
|tieroff|cell_class_execute|1709.611|1800.000|1740.234|
|tieroff|method_read|41.500|32.000|44.556|
|tieroff|closure_create|151.523|256.000|153.442|
|tieroff|retained_closure_read|27.384|0.000|28.931|

本基准没有把generator持续持cell再加为第五路径；其语义/生命周期依据既有功能和PE32验收，不从这8个数推导性能结论。没有开启super或实现class hooks。

## 全部10个Main marker与冷边界

launch至Main包括CLR/Host启动和marker传输，非纯Main。每进程一次真实Main；feature额外测首次Create和Execute，共14段冷记录。各边界不能相加冒充连续启动；beforefieldinit不能据此证明精确cctor触发时刻。

|正式顺序/进程|launch至Main ms|Main次数|进程wall ms|采样最大RSS B|
|---|---:|---:|---:|---:|
|default-common-A|113.450|1|1167.719|62664704|
|default-common-B|109.386|1|1247.257|63979520|
|default-class-A|103.481|1|399.146|61194240|
|default-class-B|105.200|1|389.597|61059072|
|default-feature-B|95.866|1|449.095|60751872|
|tieroff-common-B|223.605|1|924.817|59891712|
|tieroff-common-A|224.497|1|932.396|61947904|
|tieroff-class-B|208.136|1|395.067|59342848|
|tieroff-class-A|213.177|1|452.464|59322368|
|tieroff-feature-B|187.916|1|561.318|59875328|

|进程|首次边界|wall ms|线程分配B|CPU ms|GC0/1/2|
|---|---|---:|---:|---:|---|
|default-common-A|common_main|63.790|3528152|64.053|0/0/0|
|default-common-B|common_main|67.118|3528152|67.138|0/0/0|
|default-class-A|cold_class_main|60.994|3507024|61.060|0/0/0|
|default-class-B|cold_class_main|60.107|3507024|60.206|0/0/0|
|default-feature-B|main|60.926|435384|61.017|0/0/0|
|default-feature-B|first_instance_create|9.912|48840|9.898|0/0/0|
|default-feature-B|first_instance_execute|3.987|3066200|4.125|1/1/1|
|tieroff-common-B|common_main|169.245|3532248|169.319|0/0/0|
|tieroff-common-A|common_main|166.858|3532248|166.949|0/0/0|
|tieroff-class-B|cold_class_main|153.173|3511120|153.303|0/0/0|
|tieroff-class-A|cold_class_main|156.398|3511120|156.540|0/0/0|
|tieroff-feature-B|main|143.860|439480|144.023|0/0/0|
|tieroff-feature-B|first_instance_create|33.461|48784|33.457|0/0/0|
|tieroff-feature-B|first_instance_execute|10.261|3066200|10.454|1/1/1|

feature两配置首次Execute均分配3,066,200B且GC为1/1/1，不能用暖1800B代表冷启动。首次Create包含完整Create调用及该边界内触发的运行时初始化工作，不把48,840/48,784B误归给一个cell。

## 已证明的结构与真实额外工作

三对固定输入的整个生成PE逐字节SHA一致，不做timestamp/token掩码：

- common：29184B，SHA `0c39154f69abff20c0e31baaabd266e00d58ab7ae46ab365bf6444df7eb7249d`。
- class：9728B，SHA `b972566bf6f6d90365a75235eb0f219849c1b3bd73798ccba3d1c1290a3d1d84`。
- instance：17920B，SHA `382da4c8a92fc6a6e8a1c09f54dee1ab03a56fd37c19873fe277b7b20a81e44f`。

这证明这些固定输入的生成字段/调用/分配指令没有新增，不能推出换装runtime所有入口零成本。runtime现有导出728个类型的字段记录相同，body数5116→5117；Invoke未新增判断。字段记录相同不等于所有布局字节、generic signature、custom attributes已被证明。

feature实际CIL：CreateClass IL54→CellNew IL60/local4；read/capture两个env的0槽都捕获local4；FinalizeOrdinaryClass IL254→ClassCellGuard IL260→CellSet(local4,原类local3) IL268→StoreName IL280。capture只把同raw cell装入新lambda env；read/lambda从自身env作CellGetFree，owner context与词法cell分开传递。

已精确核实际五个源方法：新增ClassCellGuard（223B CIL）在需要classcell的类终结后、填cell前检查尚未实现的真实__set_name__/父__init_subclass__，命中明确NI，不执行hooks；GetMetadata/SetMetadata/DeleteMetadata仅B2-owned __closure__冷反射读NI、写删readonly；PyAttributeDeletion.Guard在旧函数/不支持名称分支内允许该删除进入readonly处理。

Guard仍有真实额外CIL：isinst后新增stloc.2/ldloc.2（IL130/131），locals 19→20，方法962→1003B。到达该检查的普通非函数删除也经过这两条CIL，但没有新增名称比较或owner解析。不知道JIT是否消除，不能宣称零JIT成本；144个样本不覆盖全部冷反射。此事实、默认PE相同与Invoke无新判断分别成立，互不抵消。

唯一编译器生成类型名DisplayClass11_0→DisplayClass12_0已定点核：同TypeDef、类型头/唯一state字段一致，3闭包方法及Create在仅此映射后指令/locals/EH/maxstack/initlocals相同，26个改变token站点逐个scoped target一致。内部反射类型名确实改变；不泛化去token，不据此证明其他方法或metadata。

## 原审计失败、限域授权与未证明范围

9份完整audit已经导出，6次smoke已经成功后，原比较报告因自设1MiB输出上限为 **FAIL_REPORT_BUDGET**。原失败文件和冻结文件保持原样，不以24准备成功代替结构准入，不为该失败签通过。

一次纯离线JSON整理耗时2.512631秒（约2.513秒），没有重跑CLI/audit/smoke。重复值正文留在原完整audit，新增清单保4374条变化方法记录、14939个token站点、700个ABI节、11个metadata节和74313个byte-range的完整定位；不因压缩报告而删除未知项。原超限展开报告未持久化，不能声称已恢复其精确超限字节数。

负责人明确批准：“同意按明确缩窄的审计口径继续，保留未解析差异和限制，完成一次有界观测后直接收尾，不扩解析器。”因此新增formal_bounded.py、bounded_freeze.py及独立bounded-review/admission；原formal.py/freeze.py未修改。新manifest继承原全部输入/产物并绑定原失败、整理清单和独审证据，仅替换准入口径与结果范围声明，Stage/Host/顺序/操作数/时限不变。

仍未证明：
- 3096个一般token-only方法候选的完整scope/signature等价；上述26站点不冲销此组。
- 700个ABI变化节及完整metadata语义；同名解析CIL不足以证明AssemblyRef/TypeRef/MemberRef完整递归身份。
- #Blob/#Strings/#US/#~、未引用heap内容及metadata根/填充区域的完整解析归因。
- PE headers、static RVA、resources、EH原字节余区等未覆盖区域；纯JSON整理没有重新分析全部PE字节。

以上是保留的证据边界，并非宣称同数量的语义缺陷。本次正式观察在明确限域下完成，不转换成全DLL等价或所有ABI兼容结论。按预定一次窗口直接收尾，不扩大解析器或测试尾巴。

## 证据身份与复核范围

证据根：`/workspace/shared/ryupy-class-cell-validation/performance-plan-001`。原10份Host JSON、10份command、10份result、20份stdout/stderr、144行CSV及全部probes/暖机/GC/CPU/分配/checksum保留；本报告没有改写它们，也没有重hash大型CLI/完整runtime审计。工具身份沿已冻结manifest，本文只读核小结果文件。

|证据（相对根）|SHA-256|
|---|---|
|input-prepare.json|`3fb2661b26113e0407993d7c3f8fee7bb59ccb085acc5220c9d846c3ca4538fa`|
|preparation-summary.json|`00274036406785aa3a0197363e4cb0b1f12d4544addfa28e751557f62b1e6a82`|
|audit-manifest.json|`285e8679240149c83b370ddaf21dd16b55e8de68e6e704eecc4151591e668301`|
|audit-comparison.json|`3685c130d3a0a4acb44261f5a90b891ab2a0b70b189542aab7e2b527d4a73253`|
|audit-correction-001/summary.json|`fafe03b100ffb3ba2d269ace1620c6521f22802f6f9791b50fd236d422b712be`|
|audit-correction-001/OUTPUTS.json|`d97de75421fc3ea0c34b65d6a429dd41674956b6721b41ffa46e0b8f38da9aa2`|
|bounded-admission.json|`98e665a2df884ab2d1e95b79254bce0ec92e870ad2a489a56b41a674481f334e`|
|bounded-review.json|`136e32f10ad186962c7878976817e8ca2c34c2fdd6d17205cd83529ec9699875`|
|formal_bounded.py|`f6eab8bcdee53225d815b127c1d17ceba0f268d0d36fa85e639de83fdeac4d3c`|
|bounded_freeze.py|`6a7af2032e0ba0a6735baf31c219d92f61def24d708073a59692fe4f6ea4e9dd`|
|input-formal.json|`689764f1d7b2f506c3556390b446b0fec35afe822d842782d291219c8d76ba35`|
|authorization-formal.json|`041c151e46540924e1b7589b2195f9fd423d4331353d4a5e0a6931ca5ff882fb`|
|formal-complete.json|`ced3aad63cd07068a5466b4b08b8dd6e849041b612bfa71893296f1f8cf8ed01`|
|summary.json|`5780c30fa8a7794dc101d185aa634d824a10e1d6c9f9e0e8fba38737b4aabae3`|
|warm-samples.csv|`e2e4970fb6d7f6e21fd8afc095b2da1e470ec0c2d1064b88226f955e79346117`|
|../performance-audit-review-001/REVIEW.zh.md|`50b882a67590d4ccb195be801e70eefad7b08c5ce0256840ab71ae465b95b086`|
|../performance-audit-review-001/exact-closure-mapping.json|`57ac2cb442340dea0f6dc1e009556ea22b063884f949e39259c95875c08f8175`|

工具/源码身份与完整功能验收见candidate.json、gate.json及其绑定证据。仅文档收尾未再运行make check/构建/性能负载；编译器/runtime/全部测试源与正常门禁清单逐项相同；门禁后只增改TODO及Markdown文档。
