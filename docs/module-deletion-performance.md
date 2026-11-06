# 模块名称删除的性能与结构验收

2026-11-06。A为已验收B2 Release runtime 6df960a202da34ec9c77647edad92cb2efd5bbb883109180b2e3fd238a75b6cf；
B为删除候选c8be21bec4939eaecc4fde175576b49b0908b796eee562d9e467da329c0fca57。
正式默认臂复用B2最终CLI生成的相同common/class PE及相同Host，只交换runtime。
本轮最终CLI 3f9a2ba685011930b6ce1872693127f16e3797b36d44355bb653480c2d0a046b重编相同物理源，
无del common/class及独立typed A/B三对PE的全部raw metadata、字段、签名、locals、EH与CIL相同，
各仅COFF时间戳不同；这些新PE只做审计/smoke，不偷偷换入正式B臂。

## 唯一窗口与实际typed证明

准备30进程全部符合预登记：29 exit0，typed重复删除负例exit1与CP NameError一致。
首次runtime完整审计只读复用，不重复导出54MiB。前置包含正常make check、最终82核心、
8完整正例、真实Host和13次smoke独审。纯顶层删除PE实际保Int64/Double/Boolean/String
四字段、四个bool绑定位、四个CheckModuleDelete(bool,string)；重复删除仍走同位检查。
feature内typed_read签名long→long，仅数值/bool局部，循环无box/BoxInt。不能由int标注替代此证据。

正式default、禁tier各common A1/B1/B2/A2、class A1/B1/B2/A2、feature B1/B2，
20进程、288暖样本、20个唯一Main marker全部完成，无失败、超时、重跑或补采。
子进程wall合计14.282451秒，不等于整个窗口墙钟；采样峰值RSS66,437,120B。
每进程三样本，保两镜像配对。新feature只列绝对成本，不与不同边界伪配对。

默认module_bare +4.002%/+185.153%、dynamic_defaults +23.124%/+2.126%、
generator_create_resume +22.518%/+14.675%等全部不利保留。旧暖路径和首次Main分配相同，
不能解释或抵消时间不利；未可靠归因，不称噪声、不关闭风险。本轮按预定停止，未补采或
开展新的JIT诊断。B2及ModuleSpec/find_spec/overload历史不利继续开放。

新builtin fallback的首次观察还分配3,103,080B、伴随GC 1/1/1；这是包含首次有限builtin环境
初始化的整段生成调用，不是单字典查找成本。beforefieldinit限制下不能由该边界证明精确
cctor触发时刻；它也不能与其它冷分段相加冒充连续启动。暖builtin调用每完整操作56B，
static/instance删除重绑这四进程观测为0B，typed每调用返回装箱24B而非每read装箱。

证据根：/workspace/shared/ryupy-module-delete-validation/performance-plan-001。
input-formal SHA d186a641ccdad91335e56a88ca42810c917038db52fa208358f3eb2684cad744；
formal-complete SHA 268f903376afa09b8ee9b191befcea60558a224a2dadb52f069512280ecdc332；
完整汇总JSON SHA 3dbde89f824a3e460fbab84ec253e676a8ba1580b7cc79f065d07d133125af81。
原288样本CSV、所有probes/warmup/CPU/GC/状态/原流及只读身份全部保留。

## 唯一正式窗口完整汇总

状态：20进程/288原样本完整，所有验证通过。

此报告是有界合成路径观测，不声明显著性、无回退、真实项目或AOT性能。B2历史method_calls +237.02%/+129.65%、module_bare +26.96%/+97.46%、module_external +12.10%/+15.42%等风险仍未关闭；本轮收益不能抵扣。

每进程固定三样本先取中位数。A1/B1与A2/B2均完整呈现；正变化不利。Host委托调度与最终结果存储计入。所有原样本、probes、warmup、起止状态、CPU/GC/分配/checksum及原流身份在summary.json。

## 全部40默认镜像配对

|配置|路径|配对|A ns/item|B ns/item|变化%|A→B B/item|A→B CPU ns/item|
|---|---|---:|---:|---:|---:|---:|---:|
|default|static_calls|1|277.681|283.733|2.179|33.000→33.000|278.595→284.637|
|default|static_calls|2|334.684|279.162|-16.590|33.000→33.000|335.968→279.816|
|default|dynamic_defaults|1|127.849|157.412|23.124|33.000→33.000|128.418→159.485|
|default|dynamic_defaults|2|133.163|135.993|2.126|33.000→33.000|134.460→137.817|
|default|plain_def_create|1|83.242|96.208|15.577|192.000→192.000|90.576→107.788|
|default|plain_def_create|2|135.753|88.939|-34.485|192.000→192.000|150.024→122.803|
|default|closure_create|1|90.358|88.501|-2.055|256.000→256.000|97.046→96.191|
|default|closure_create|2|126.940|94.537|-25.527|256.000→256.000|131.348→98.145|
|default|module_external|1|683.871|690.980|1.039|24.625→24.625|684.387→694.519|
|default|module_external|2|710.740|730.764|2.817|24.625→24.625|713.989→731.781|
|default|module_bare|1|880.475|915.713|4.002|96.625→96.625|881.256→917.206|
|default|module_bare|2|966.418|2755.766|185.153|96.625→96.625|972.504→2780.457|
|default|resource_files|1|4059.648|2771.418|-31.733|1312.000→1312.000|4530.396→2773.285|
|default|resource_files|2|3899.238|4673.612|19.860|1312.000→1312.000|4391.571→4739.655|
|default|method_calls|1|154.654|148.538|-3.955|1.000→1.000|155.609→149.536|
|default|method_calls|2|408.574|485.917|18.930|1.000→1.000|409.882→487.518|
|default|generator_create_resume|1|792.431|970.867|22.518|408.000→408.000|798.340→975.098|
|default|generator_create_resume|2|1356.968|1556.108|14.675|408.000→408.000|1360.474→1566.895|
|default|class_module_body_execute|1|4891.973|4789.428|-2.096|1984.000→1984.000|4900.513→4794.189|
|default|class_module_body_execute|2|4054.699|5508.160|35.846|1984.000→1984.000|4075.195→5513.672|
|tieroff|static_calls|1|60.372|62.429|3.408|33.000→33.000|60.944→62.927|
|tieroff|static_calls|2|65.744|64.109|-2.487|33.000→33.000|66.376→64.636|
|tieroff|dynamic_defaults|1|57.268|38.561|-32.665|33.000→33.000|59.631→40.375|
|tieroff|dynamic_defaults|2|39.386|40.644|3.193|33.000→33.000|42.877→41.748|
|tieroff|plain_def_create|1|46.251|43.372|-6.225|192.000→192.000|52.002→45.166|
|tieroff|plain_def_create|2|45.730|41.959|-8.247|192.000→192.000|47.974→45.654|
|tieroff|closure_create|1|57.437|55.426|-3.501|256.000→256.000|60.181→57.739|
|tieroff|closure_create|2|56.793|63.619|12.018|256.000→256.000|59.448→66.284|
|tieroff|module_external|1|169.839|167.690|-1.265|24.625→24.625|171.570→169.617|
|tieroff|module_external|2|159.692|160.675|0.616|24.625→24.625|160.248→162.384|
|tieroff|module_bare|1|219.822|215.784|-1.837|96.625→96.625|220.520→216.400|
|tieroff|module_bare|2|221.640|206.227|-6.954|96.625→96.625|222.076→207.886|
|tieroff|resource_files|1|1094.433|1161.584|6.136|1312.000→1312.000|1095.306→1163.086|
|tieroff|resource_files|2|1069.737|1030.270|-3.689|1312.000→1312.000|1071.014→1031.555|
|tieroff|method_calls|1|42.663|46.146|8.164|1.000→1.000|43.610→47.485|
|tieroff|method_calls|2|53.025|43.268|-18.401|1.000→1.000|53.619→44.159|
|tieroff|generator_create_resume|1|251.950|225.875|-10.349|408.000→408.000|254.639→231.934|
|tieroff|generator_create_resume|2|264.663|234.532|-11.385|408.000→408.000|271.729→236.572|
|tieroff|class_module_body_execute|1|2109.710|1674.620|-20.623|1984.000→1984.000|2107.300→1677.856|
|tieroff|class_module_body_execute|2|1481.084|1496.260|1.025|1984.000→1984.000|1484.375→1500.000|

## 全部16个feature绝对中位数

typed_read每个样本8次完整调用，每调用65536 reads，共524288 reads。下表分配按完整调用列出，Host返回装箱属于每调用，不是逐read装箱。typed时间另给摊销ns/read，不把摊销分配误当每读生成对象。其它三路径按完整删除重绑/内建调用操作；instance包括真实CallValue，不是纯字典删除。

|配置/进程|路径|ns/完整调用|B/完整调用|CPU ns/调用|typed摊销ns/read|
|---|---|---:|---:|---:|---:|
|default/feature-B1|typed_read|91814.375|24.000|94750.000|1.401|
|default/feature-B1|static_cycle|62.897|0.000|92.773|不适用|
|default/feature-B1|builtin_read|310.145|56.000|360.352|不适用|
|default/feature-B1|instance_cycle|558.291|0.000|590.820|不适用|
|default/feature-B2|typed_read|83104.875|24.000|88375.000|1.268|
|default/feature-B2|static_cycle|62.163|0.000|67.383|不适用|
|default/feature-B2|builtin_read|339.495|56.000|393.555|不适用|
|default/feature-B2|instance_cycle|558.691|0.000|585.938|不适用|
|tieroff/feature-B1|typed_read|146855.875|24.000|152000.000|2.241|
|tieroff/feature-B1|static_cycle|29.057|0.000|41.992|不适用|
|tieroff/feature-B1|builtin_read|109.560|56.000|113.281|不适用|
|tieroff/feature-B1|instance_cycle|141.697|0.000|155.273|不适用|
|tieroff/feature-B2|typed_read|146117.250|24.000|151000.000|2.230|
|tieroff/feature-B2|static_cycle|52.119|0.000|57.617|不适用|
|tieroff/feature-B2|builtin_read|81.021|56.000|99.609|不适用|
|tieroff/feature-B2|instance_cycle|147.263|0.000|165.039|不适用|

## 全部20次首次Main/launch

|配置/进程|launch至Main ms（全部marker）|Main次数|状态|
|---|---:|---:|---|
|default/common-A1|116.215|1|passed|
|default/common-B1|127.464|1|passed|
|default/common-B2|120.032|1|passed|
|default/common-A2|126.308|1|passed|
|default/class-A1|112.032|1|passed|
|default/class-B1|127.010|1|passed|
|default/class-B2|109.754|1|passed|
|default/class-A2|110.846|1|passed|
|default/feature-B1|103.986|1|passed|
|default/feature-B2|113.594|1|passed|
|tieroff/common-A1|227.180|1|passed|
|tieroff/common-B1|226.104|1|passed|
|tieroff/common-B2|222.359|1|passed|
|tieroff/common-A2|224.346|1|passed|
|tieroff/class-A1|239.122|1|passed|
|tieroff/class-B1|223.863|1|passed|
|tieroff/class-B2|217.618|1|passed|
|tieroff/class-A2|205.400|1|passed|
|tieroff/feature-B1|199.870|1|passed|
|tieroff/feature-B2|192.032|1|passed|

## 全部冷边界及分配

完整窗口包含20次首次Main及feature额外12段（4进程×首次builtin fallback/Create/Execute）。分段不得相加冒充连续冷启动；beforefieldinit仍使精确cctor时间无法据此证明。launch含CLR/Host及marker传输，不是纯Main。

|配置/进程|边界|wall ms|线程分配B|CPU ms|GC0/1/2|
|---|---|---:|---:|---:|---|
|default/common-A1|common_main|67.413|3528152|67.574|0/0/0|
|default/common-B1|common_main|73.810|3528152|73.974|0/0/0|
|default/common-B2|common_main|72.982|3528152|73.140|0/0/0|
|default/common-A2|common_main|79.901|3528152|80.032|0/0/0|
|default/class-A1|cold_class_main|64.686|3507024|64.857|0/0/0|
|default/class-B1|cold_class_main|74.411|3507024|74.646|0/0/0|
|default/class-B2|cold_class_main|65.882|3507024|66.075|0/0/0|
|default/class-A2|cold_class_main|64.225|3507024|64.341|0/0/0|
|default/feature-B1|main|73.885|458536|74.048|0/0/0|
|default/feature-B1|first_builtin_fallback|5.195|3103080|5.235|1/1/1|
|default/feature-B1|first_instance_create|0.709|4992|0.713|0/0/0|
|default/feature-B1|first_instance_execute|0.945|1064|0.952|0/0/0|
|default/feature-B2|main|76.958|458536|77.054|0/0/0|
|default/feature-B2|first_builtin_fallback|5.631|3103080|5.688|1/1/1|
|default/feature-B2|first_instance_create|0.716|4992|0.719|0/0/0|
|default/feature-B2|first_instance_execute|0.716|1064|0.722|0/0/0|
|tieroff/common-A1|common_main|167.528|3532248|167.628|0/0/0|
|tieroff/common-B1|common_main|169.476|3532248|169.548|0/0/0|
|tieroff/common-B2|common_main|165.058|3532248|165.061|0/0/0|
|tieroff/common-A2|common_main|168.610|3532248|168.690|0/0/0|
|tieroff/class-A1|cold_class_main|174.780|3511120|174.728|0/0/0|
|tieroff/class-B1|cold_class_main|166.737|3511120|166.828|0/0/0|
|tieroff/class-B2|cold_class_main|161.983|3511120|162.120|0/0/0|
|tieroff/class-A2|cold_class_main|151.429|3511120|151.534|0/0/0|
|tieroff/feature-B1|main|163.817|462632|163.865|0/0/0|
|tieroff/feature-B1|first_builtin_fallback|10.430|3103080|10.440|1/1/1|
|tieroff/feature-B1|first_instance_create|1.344|4936|1.349|0/0/0|
|tieroff/feature-B1|first_instance_execute|2.245|1064|2.250|0/0/0|
|tieroff/feature-B2|main|158.946|462632|159.020|0/0/0|
|tieroff/feature-B2|first_builtin_fallback|12.050|3103080|12.089|1/1/1|
|tieroff/feature-B2|first_instance_create|1.353|4936|1.359|0/0/0|
|tieroff/feature-B2|first_instance_execute|2.333|1064|2.339|0/0/0|

## 完整性及限制

原样本实际288，默认配对可用40/40，feature行16/16，Main冷段20/20，额外冷段12/12。
全部不利值保留，无删点、重跑、补采或按显著性停止。墙钟、CPU及分配不是互相替代的证据；三者均保留。未完成/失败进程不冒作成功配对。
原始JSON与原流文件不被本汇总改写。只读引用的输入身份见inputs.json；全部20个计划进程及实际记录含在summary.json。
