# B2模块实例归属有界性能结果

正式矩阵原流位于验收材料的performance-fixed-001，后续诊断位于diagnosis-001。
两窗口分开保留，不能把诊断当重新挑选的正式结果。

2026-11-06；Linux .NET 10.0.14 JIT。唯一正式窗口20进程、384暖样本完整完成，无失败、超时、重跑、补采或事后调参。此结论只描述执行完整性，不代表无性能回退。

原P2确有一次common-A smoke失败（双入口PE共用__main__缓存）；原流仍保留。随后独立class进程的新P2fix通过一次Host构建、全CIL审计和七smoke，独审后才执行本次唯一P3。不能称整个准备历程无失败。

formal-summary.json SHA256：241b4c13c97a5d9582a4cb939ef98df275169436980a5b9aaf9ca71d5616810f
实际Host SHA256：279163dc056ca02ad8a58211e55cec5ba2047332575c3489fecbff138924a68a
A runtime：8c36fea58c16843814e30892c0b9cdc09ea27e15bae5e1d9b625278ea77ed9f3
B runtime：6df960a202da34ec9c77647edad92cb2efd5bbb883109180b2e3fd238a75b6cf
最终CLI（Debug，用于生成而非计时）：6e813e2e3ef0125fe6074ed372aa6c2d41f42b640bbda0b024666f87bb64cba7

## 必须保留的不利与限制

- default method_calls两镜像配对+237.02%/+129.65%，module_bare +26.96%/+97.46%，static_calls +9.92%/+3.53%，module_external +12.10%/+15.42%，generator_create_resume +23.64%/+3.81%，dynamic_defaults +4.59%/+2.39%。
- tieroff closure_create两配对+9.98%/+5.19%，class_module_body_execute +1.29%/+15.88%；method_calls一对+20.73%、另一对−5.97%，不能只选有利值。
- default class body两对−3.16%/−4.98%，不能抵消tieroff不利；plain_def、closure、resource等其余完整正负观测均在下表。
- 所有默认暖路径A/B分配相同，但相同分配和相同CIL不证明无时间回退。仅两配对，不声明统计显著或将波动归因给某条指令。这部分为原P3结论；随后一次独立有界归因见文末，不能替代或重算本表。
- 实例def同builtins/缺键均224B每完整操作，重绑256B，多32B；包含factory调用及实际child创建，不是纯CaptureDefinitionContext成本。新功能绝对值不能抵扣默认回退。
- 普通common冷Main约3.53MB、class冷Main约3.51MB；A/B对应边界分配相同。instance首次Execute为3067984B，均原样保留，不能据首次观察定位具体初始化分配责任。
- 默认common源较B1增加method/generator及class初始化，资源绝对路径也不同；B1旧报告不是本轮同PE对照。资源暖分配1312B/item不能直接当B1的1296B/item运行时增量。
- beforefieldinit允许静态初始化提前发生。首次builtins Host观察在first_execute之后；完整cold段与触达链不能证明精确cctor时间，更不能声称零初始化成本。
- 这是合成生成代码路径，不是真实项目/CP速度比，也不外推AOT或其它平台。历史B1、ModuleSpec/find_spec/overload风险未关闭。

## 默认全部40镜像配对

每进程三个原样本取中位数，再列A1/B1、A2/B2；正变化不利。loop以内部单item为分母，generator以创建+两次resume为完整操作，class为完整模块body。class独立进程先正常Main一次后直接生成body，不是重复Main或纯MakeClass。CPU为进程CPU差折算ns/item。

|配置/对|路径|A ns/item|B ns/item|变化|A→B B/item|A→B CPU ns/item|
|---|---|---:|---:|---:|---:|---:|
|default/1|static_calls|293.401|322.507|+9.92%|33→33|294.098→323.303|
|default/1|dynamic_defaults|135.484|141.709|+4.59%|33→33|136.444→142.822|
|default/1|plain_def_create|88.904|84.367|-5.10%|192→192|96.436→94.849|
|default/1|closure_create|89.102|88.138|-1.08%|256→256|91.064→91.553|
|default/1|module_external|674.068|755.606|+12.10%|24.625→24.625|675.018→757.568|
|default/1|module_bare|872.339|1107.483|+26.96%|96.625→96.625|875.793→1109.772|
|default/1|resource_files|3181.179|3267.249|+2.71%|1312→1312|3324.646→3542.358|
|default/1|method_calls|65.667|221.310|+237.02%|1→1|67.474→222.931|
|default/1|generator_create_resume|912.425|1128.079|+23.64%|408→408|915.649→1138.062|
|default/2|static_calls|272.997|282.624|+3.53%|33→33|273.712→283.264|
|default/2|dynamic_defaults|138.430|141.741|+2.39%|33→33|139.252→144.348|
|default/2|plain_def_create|108.172|83.536|-22.77%|192→192|109.375→91.675|
|default/2|closure_create|100.356|102.257|+1.89%|256→256|102.295→112.183|
|default/2|module_external|729.779|842.278|+15.42%|24.625→24.625|730.713→844.482|
|default/2|module_bare|954.480|1884.708|+97.46%|96.625→96.625|956.879→2033.661|
|default/2|resource_files|2957.277|2887.799|-2.35%|1312→1312|3076.111→3106.110|
|default/2|method_calls|151.368|347.622|+129.65%|1→1|153.168→349.579|
|default/2|generator_create_resume|1085.860|1127.196|+3.81%|408→408|1088.745→1136.719|
|default/1|class_module_body_execute|4185.226|4053.036|-3.16%|1984→1984|4189.331→4058.228|
|default/2|class_module_body_execute|4432.702|4212.003|-4.98%|1984→1984|4439.697→4218.262|
|tieroff/1|static_calls|67.259|66.097|-1.73%|33→33|67.963→66.711|
|tieroff/1|dynamic_defaults|39.301|37.328|-5.02%|33→33|39.764→39.276|
|tieroff/1|plain_def_create|42.340|43.762|+3.36%|192→192|46.021→45.898|
|tieroff/1|closure_create|55.107|60.606|+9.98%|256→256|58.105→70.435|
|tieroff/1|module_external|166.668|171.417|+2.85%|24.625→24.625|168.365→172.394|
|tieroff/1|module_bare|215.642|227.278|+5.40%|96.625→96.625|216.278→227.753|
|tieroff/1|resource_files|1044.195|1065.569|+2.05%|1312→1312|1044.769→1066.437|
|tieroff/1|method_calls|42.186|50.931|+20.73%|1→1|45.746→52.246|
|tieroff/1|generator_create_resume|233.615|227.276|-2.71%|408→408|235.962→229.492|
|tieroff/2|static_calls|69.188|61.815|-10.66%|33→33|69.916→63.721|
|tieroff/2|dynamic_defaults|40.572|37.452|-7.69%|33→33|42.114→39.520|
|tieroff/2|plain_def_create|49.990|44.020|-11.94%|192→192|52.002→47.974|
|tieroff/2|closure_create|55.026|57.880|+5.19%|256→256|57.129→61.401|
|tieroff/2|module_external|170.542|165.622|-2.88%|24.625→24.625|171.234→168.243|
|tieroff/2|module_bare|216.001|218.208|+1.02%|96.625→96.625|217.804→218.842|
|tieroff/2|resource_files|1058.982|1047.224|-1.11%|1312→1312|1059.845→1043.304|
|tieroff/2|method_calls|45.201|42.501|-5.97%|1→1|46.906→43.243|
|tieroff/2|generator_create_resume|223.935|227.059|+1.40%|408→408|232.178→230.591|
|tieroff/1|class_module_body_execute|1812.316|1835.734|+1.29%|1984→1984|1816.528→1840.332|
|tieroff/2|class_module_body_execute|1634.343|1893.808|+15.88%|1984→1984|1636.597→1898.804|

## 实例全部48绝对观测

每进程三个样本中位数。两实例交错操作包含两次Execute；saved owners包含两次函数调用；generator包含创建+两次resume。Create/Seed/Execute含真实dict获取和种入；owned_method_call含GetAttr绑定与CallValue。

|配置/进程|路径|ns/完整操作|B/完整操作|CPU ns/操作|Execute/调用/resume数|
|---|---|---:|---:|---:|---|
|default/1|execute_existing|6160.928|2312.000000000|6179.688|1/1/0|
|default/1|create_seed_execute|18790.677|8932.609375000|18830.078|1/1/0|
|default/1|alternating_two_instances|12215.796|4624.000000000|12233.398|2/1/0|
|default/1|owned_function_call|435.646|32.000000000|542.969|0/1/0|
|default/1|def_same_builtins|481.780|224.000000000|515.625|0/1/0|
|default/1|def_missing_builtins|478.386|224.000000000|498.047|0/1/0|
|default/1|def_rebound_builtins|542.917|256.000000000|561.523|0/1/0|
|default/1|closure_create|675.372|288.000000000|693.359|0/1/0|
|default/1|owned_method_call|1518.438|112.000000000|1553.711|0/1/0|
|default/1|generator_create_resume|4731.167|400.000000000|4753.906|0/1/2|
|default/1|class_module_execute|7666.338|2048.000000000|7690.430|1/1/0|
|default/1|alternating_saved_owners|1413.720|64.000000000|1437.500|0/2/0|
|default/2|execute_existing|7914.428|2312.000000000|7929.688|1/1/0|
|default/2|create_seed_execute|20840.984|8932.609375000|21054.688|1/1/0|
|default/2|alternating_two_instances|12608.378|4624.000000000|12763.672|2/1/0|
|default/2|owned_function_call|402.931|32.000000000|436.523|0/1/0|
|default/2|def_same_builtins|460.332|224.000000000|479.492|0/1/0|
|default/2|def_missing_builtins|516.793|224.000000000|548.828|0/1/0|
|default/2|def_rebound_builtins|547.983|256.000000000|569.336|0/1/0|
|default/2|closure_create|1382.208|288.000000000|2446.289|0/1/0|
|default/2|owned_method_call|1129.689|112.000000000|1154.297|0/1/0|
|default/2|generator_create_resume|4485.455|400.000000000|4553.711|0/1/2|
|default/2|class_module_execute|6674.013|2048.000000000|6702.148|1/1/0|
|default/2|alternating_saved_owners|1424.058|64.000000000|1454.102|0/2/0|
|tieroff/1|execute_existing|2528.024|2312.000000000|2546.875|1/1/0|
|tieroff/1|create_seed_execute|8174.453|8876.609375000|8239.258|1/1/0|
|tieroff/1|alternating_two_instances|5226.705|4624.000000000|5248.047|2/1/0|
|tieroff/1|owned_function_call|109.804|32.000000000|158.203|0/1/0|
|tieroff/1|def_same_builtins|142.177|224.000000000|174.805|0/1/0|
|tieroff/1|def_missing_builtins|133.990|224.000000000|164.062|0/1/0|
|tieroff/1|def_rebound_builtins|149.483|256.000000000|169.922|0/1/0|
|tieroff/1|closure_create|181.719|288.000000000|201.172|0/1/0|
|tieroff/1|owned_method_call|165.072|112.000000000|191.406|0/1/0|
|tieroff/1|generator_create_resume|588.337|400.000000000|615.234|0/1/2|
|tieroff/1|class_module_execute|1912.762|2048.000000000|1958.008|1/1/0|
|tieroff/1|alternating_saved_owners|197.025|64.000000000|214.844|0/2/0|
|tieroff/2|execute_existing|2435.728|2312.000000000|2451.172|1/1/0|
|tieroff/2|create_seed_execute|9147.640|8876.609375000|9179.688|1/1/0|
|tieroff/2|alternating_two_instances|4859.708|4624.000000000|5144.531|2/1/0|
|tieroff/2|owned_function_call|110.812|32.000000000|138.672|0/1/0|
|tieroff/2|def_same_builtins|150.646|224.000000000|229.492|0/1/0|
|tieroff/2|def_missing_builtins|145.767|224.000000000|171.875|0/1/0|
|tieroff/2|def_rebound_builtins|156.446|256.000000000|176.758|0/1/0|
|tieroff/2|closure_create|187.167|288.000000000|212.891|0/1/0|
|tieroff/2|owned_method_call|168.438|112.000000000|184.570|0/1/0|
|tieroff/2|generator_create_resume|570.967|400.000000000|587.891|0/1/2|
|tieroff/2|class_module_execute|1910.308|2048.000000000|1928.711|1/1/0|
|tieroff/2|alternating_saved_owners|229.965|64.000000000|273.438|0/2/0|

## 全部冷边界及launch

所有冷段GC0/1/2都保留。launch含CLR/Host准备与标记传输，只到该进程首次Main完成，不含随后Create/Seed/Execute。分段冷时间不能相加冒称连续冷启动。

|配置/进程|边界|墙钟ms|线程分配B|CPU ms|GC0/1/2|
|---|---|---:|---:|---:|---|
|default/common-A1|launch至Main|118.749835|不适用|不适用|不适用|
|default/common-A1|common_main|71.222121|3528152|71.331000|0/0/0|
|default/common-B1|launch至Main|110.718981|不适用|不适用|不适用|
|default/common-B1|common_main|66.371779|3528152|66.501000|0/0/0|
|default/common-B2|launch至Main|119.942657|不适用|不适用|不适用|
|default/common-B2|common_main|71.274442|3528152|71.395000|0/0/0|
|default/common-A2|launch至Main|122.579411|不适用|不适用|不适用|
|default/common-A2|common_main|72.352198|3528152|72.456000|0/0/0|
|default/class-A1|launch至Main|119.304155|不适用|不适用|不适用|
|default/class-A1|cold_class_main|70.065284|3507024|70.191000|0/0/0|
|default/class-B1|launch至Main|119.716020|不适用|不适用|不适用|
|default/class-B1|cold_class_main|70.983211|3507024|71.092000|0/0/0|
|default/class-B2|launch至Main|110.576885|不适用|不适用|不适用|
|default/class-B2|cold_class_main|64.134537|3507024|64.241000|0/0/0|
|default/class-A2|launch至Main|107.472406|不适用|不适用|不适用|
|default/class-A2|cold_class_main|62.835870|3507024|62.835000|0/0/0|
|default/instance-B1|launch至Main|102.114038|不适用|不适用|不适用|
|default/instance-B1|main_registration|54.236827|444504|54.348000|0/0/0|
|default/instance-B1|first_create|11.302303|48848|11.333000|0/0/0|
|default/instance-B1|first_seed|0.132349|352|0.134000|0/0/0|
|default/instance-B1|first_execute|3.483815|3067984|3.488000|0/0/0|
|default/instance-B1|first_class_create|0.069564|4624|0.072000|0/0/0|
|default/instance-B1|first_class_execute|1.767050|2352|1.771000|0/0/0|
|default/instance-B2|launch至Main|135.918466|不适用|不适用|不适用|
|default/instance-B2|main_registration|55.015335|444504|55.167000|0/0/0|
|default/instance-B2|first_create|10.184068|48848|10.189000|0/0/0|
|default/instance-B2|first_seed|0.125389|352|0.128000|0/0/0|
|default/instance-B2|first_execute|3.127443|3067984|3.131000|0/0/0|
|default/instance-B2|first_class_create|0.058538|4624|0.061000|0/0/0|
|default/instance-B2|first_class_execute|1.720728|2352|1.724000|0/0/0|
|tieroff/common-A1|launch至Main|240.319261|不适用|不适用|不适用|
|tieroff/common-A1|common_main|175.924530|3532248|176.024000|0/0/0|
|tieroff/common-B1|launch至Main|237.576570|不适用|不适用|不适用|
|tieroff/common-B1|common_main|176.434656|3532248|176.536000|0/0/0|
|tieroff/common-B2|launch至Main|240.271946|不适用|不适用|不适用|
|tieroff/common-B2|common_main|179.399546|3532248|179.486000|0/0/0|
|tieroff/common-A2|launch至Main|230.392468|不适用|不适用|不适用|
|tieroff/common-A2|common_main|171.425792|3532248|171.531000|0/0/0|
|tieroff/class-A1|launch至Main|207.403219|不适用|不适用|不适用|
|tieroff/class-A1|cold_class_main|153.539767|3511120|153.651000|0/0/0|
|tieroff/class-B1|launch至Main|226.628534|不适用|不适用|不适用|
|tieroff/class-B1|cold_class_main|165.016485|3511120|165.111000|0/0/0|
|tieroff/class-B2|launch至Main|230.151847|不适用|不适用|不适用|
|tieroff/class-B2|cold_class_main|165.253948|3511120|165.361000|0/0/0|
|tieroff/class-A2|launch至Main|229.012187|不适用|不适用|不适用|
|tieroff/class-A2|cold_class_main|174.820106|3511120|174.927000|0/0/0|
|tieroff/instance-B1|launch至Main|199.399332|不适用|不适用|不适用|
|tieroff/instance-B1|main_registration|136.355020|448600|136.451000|0/0/0|
|tieroff/instance-B1|first_create|34.050386|48792|34.058000|0/0/0|
|tieroff/instance-B1|first_seed|0.408555|352|0.412000|0/0/0|
|tieroff/instance-B1|first_execute|11.363757|3067984|11.368000|0/0/0|
|tieroff/instance-B1|first_class_create|0.137627|4568|0.140000|0/0/0|
|tieroff/instance-B1|first_class_execute|9.006217|2352|9.012000|0/0/0|
|tieroff/instance-B2|launch至Main|200.126045|不适用|不适用|不适用|
|tieroff/instance-B2|main_registration|141.520713|448600|141.677000|0/0/0|
|tieroff/instance-B2|first_create|35.674640|48792|35.684000|0/0/0|
|tieroff/instance-B2|first_seed|0.419342|352|0.422000|0/0/0|
|tieroff/instance-B2|first_execute|11.583969|3067984|11.581000|0/0/0|
|tieroff/instance-B2|first_class_create|0.133951|4568|0.136000|0/0/0|
|tieroff/instance-B2|first_class_execute|8.635291|2352|8.640000|0/0/0|

## 原始证据与资源

20进程wall_seconds合计15.503265秒（不冒称含清单核验间隔的总窗口时间）；采样峰值单进程RSS 67493888B，同session合计 67493888B。RSS每50ms采样，不是OS瞬时峰值。
384暖样本GC合计0/1/2：348/0/0；逐样本GC/CPU/时间/分配/checksum/起止状态/完整操作数均在formal及formal-summary.all_runs，不删除任何原样本。
原stdout/stderr/command（完整环境与cwd）/result均保留；20次Main标记唯一。正式记录不包含CP计时、受测编译时间或新CLI速度臂。
完整CIL独审报告见/workspace/shared/ryupy-module-owner-validation/cil-review-001/REPORT.zh.md及绑定的24份机器证据；原失败见/workspace/shared/ryupy-module-owner-validation/performance/p2-stopped.json及common-A-smoke原流。
如继续调查默认method/module_bare明显不利，应提出独立、预冻结的归因窗口，不能在本窗口补采或事后只重跑坏点。


# 独立有界归因：保留原风险

## 裁决

**未能可靠归因到可修复的 B2 结构性回退；不修改生产源码，不再采样。** 原 P3 默认 method_calls +237.02%/+129.65%、module_bare +26.96%/+97.46%、module_external +12.10%/+15.42%风险仍未关闭。新诊断不覆盖、不重算原 P3，不宣称“只是噪声”“无回退”或“已经修复”。

本次明确发现默认同版本结果可大幅变化，以及插桩进程中实际 JIT 层级/代码形状不同，说明不能从原两组结果直接推导某个 B2 源码增量的固定热成本。但缺少原 P3 对应的 JIT/采样时间线，不能证明这些因素解释了原回退的全部或任何确定比例。无精确可修证据，不盲改非热 MakeClass wrapper，也不添加 owner 快路径补丁追数。

## 窗口与完整性

依据冻结清单 e01bbd8901a3c2c41b2b901b0c8c6796f798162ae63184246f60b5071569351c 独占运行一次，用时33.2807秒。16进程全部exit0、432暖样本（324非插桩/108插桩）、288语义探针、16次Main全部通过。无失败、无丢样、无outlier删除、无重试、无补采、无编译、无Host/PE/源码/预热调整。仅common九路径，未重跑原完整矩阵。4份JIT日志齐全；stdout/stderr/JIT总325887B，峰值单进程/组RSS均66813952B。

输入结束核验11823项全部通过，见analysis/final-input-verification.json；原formal-summary SHA仍为241b4c13c97a5d9582a4cb939ef98df275169436980a5b9aaf9ca71d5616810f。原P3报告和原始样本不变。采样已永久停止；后续可进入doc-only收尾，无需再次为本报告冻结生产源码。

## 实际热链

Host闭包直绑m0_method_calls(owner)，每次正文64项。CIL含traceback、BoxInt、LoadName、IsModule/CaptureModuleTarget、site26 CallCapturedMethod1(captured=false)、NumericAccumulator。PIC命中经CallMethod1 → InvokePrepared → PyFunction.Invoke → __thunk_12 → m0_read；read再做traceback和Binary。MakeClass仅冷Main建类，不是每项额外调用。bare路径也未走新增函数环境metadata getter。完整静态证据见PLAN.zh.md与hot-generated-cil.json。

## 全部九路径，不选择性呈现

以下单位为百分比；正号表示后者更慢。每进程先取其3样本中位数。默认前向组为A01/A02对B03/B04；反向组为A07/A08对B05/B06。组值取两个进程中位数的中位数，B/A−1。TierOff为A09/A12对B10/B11。样本不当独立进程，n小，不作显著性推断。

| 路径 | 默认前向B/A | 默认反向B/A | TierOff B/A |
|---|---:|---:|---:|
| static_calls | -2.68% | -6.25% | -16.62% |
| dynamic_defaults | +21.90% | -4.76% | -5.89% |
| plain_def_create | +103.39% | -15.68% | +1.96% |
| closure_create | -4.23% | -5.20% | -4.10% |
| module_external | +3.92% | -14.43% | -11.54% |
| module_bare | -2.50% | -11.44% | -6.60% |
| resource_files | -20.23% | -30.07% | -0.25% |
| method_calls | -12.24% | +28.19% | -5.84% |
| generator_create_resume | -18.86% | -3.90% | +56.58% |

同版本对照：默认列为A02/A01、B04/B03、B06/B05、A08/A07；TierOff列为A12/A09、B11/B10。

| 路径 | A/A前 | B/B前 | B/B后 | A/A后 | TierOff A/A | TierOff B/B |
|---|---:|---:|---:|---:|---:|---:|
| static_calls | -10.52% | +1.01% | +9.35% | -3.61% | -19.40% | -2.07% |
| dynamic_defaults | -4.22% | +16.52% | +0.12% | +8.01% | -0.28% | -1.72% |
| plain_def_create | +31.28% | +235.77% | +1.92% | +31.56% | -7.51% | -5.58% |
| closure_create | -14.72% | +5.44% | -1.78% | +0.61% | +2.61% | +0.23% |
| module_external | -5.00% | -3.91% | -4.16% | -11.99% | -3.43% | +5.94% |
| module_bare | -67.73% | -64.65% | -3.48% | -0.84% | +1.59% | +9.36% |
| resource_files | +8.43% | +4.57% | -9.45% | +27.41% | +0.82% | +21.70% |
| method_calls | -1.92% | +92.94% | -30.83% | -71.36% | -9.58% | +9.21% |
| generator_create_resume | +11.53% | +18.92% | -15.75% | -25.98% | -7.81% | +134.28% |

重点路径全部非插桩进程中位数（ns/item），没有隐藏任何组：

| 进程 | method_calls | module_bare | module_external |
|---|---:|---:|---:|
| 01-default-A | 317.004 | 2674.647 | 701.044 |
| 02-default-A | 310.931 | 863.080 | 666.005 |
| 03-default-B | 188.125 | 2548.630 | 724.532 |
| 04-default-B | 362.963 | 900.819 | 696.169 |
| 05-default-B | 206.276 | 851.183 | 682.726 |
| 06-default-B | 142.687 | 821.531 | 654.318 |
| 07-default-A | 211.610 | 948.389 | 831.086 |
| 08-default-A | 60.608 | 940.457 | 731.422 |
| 09-tieroff-A | 48.951 | 228.581 | 187.612 |
| 10-tieroff-B | 41.952 | 205.569 | 158.406 |
| 11-tieroff-B | 45.816 | 224.810 | 167.811 |
| 12-tieroff-A | 44.261 | 232.213 | 181.182 |

method默认两方向−12.24%/+28.19%，而相邻B/B可+92.94%、相邻A/A可−71.36%，不能稳定隔离版本效应。bare同版本A前−67.73%、B前−64.65%，两方向B/A−2.50%/−11.44%也不能用于撤销原P3不利值。其他风险同样保留：plain_def_create本次默认前向+103.39%而反向−15.68%；generator_create_resume本次TierOff组+56.58%且B/B+134.28%。这些现象不能只挑有利部分说已稳定。

summary.json保留所有16进程全部九路径每个样本的wall/CPU/分配/GC，原runs/*.json还含全部原始状态与校验和。method每样本32768B（1B/item）且所有GC计数为0；bare每样本3166208B（96.625B/item）且所有GC计数为0，A/B一致。CPU通常同步变化，但process CPU包含后台JIT；bare A01第二样本CPU 3903ns/item而wall2675ns/item，A08第三样本CPU2793而wall1582，说明不能把CPU同步当成排除运行时/并行工作影响的依据。没有线程级归因，不指定是哪条后台线程。

## 四份JIT证据：能证与不能证

### 默认A13/B14，插桩条件独立

A日志有24段方法/层级正文，B有25段；见analysis/jit-inventory.json。A出现PyFunction.Invoke、PushFrameWithOrigin、PushFrameCore的Tier1，而B日志没有这些Tier1；B出现Binary、CallCapturedMethod1、CallMethod1、NumericAccumulator.Add的额外Instrumented Tier0，A日志没有对应段。两边m0_method_calls均记录774B Instrumented Tier0、m0_read均160B Tier0、__thunk_12均277B Tier0。

可匹配的同方法/同层级正文中，三处地址规范化后仍不同：
- PreparedFor Tier0：176→177B，CORINFO_HELP_ARRADDR_ST由直接call变成间接call。
- InvokePreparedMethod Tier0：140→141B，CORINFO_HELP_CHKCASTCLASS由直接call变成间接call。
- PushFrameCore Instrumented Tier0：363→366B，三个CORINFO_HELP_ARRADDR_ST调用的形式变化。

没有从汇编中抹掉这些差异。analysis/default-native.diff完整保留；它们不是B2新增CIL分支的证据，可能涉及运行时地址/代码布局等条件，当前不能确定原因。尤其PreparedFor的写入慢分支是否在暖循环执行也未计数，不能把1B差异直接算成每次热成本。

**限制：**日志没有与各sample绑定的时间戳，出现Tier1不能证明某个sample全部用了Tier1；未单独输出可能因内联，也不能解释为未执行。每配置只有一对插桩，插桩改变时序，本次A/B method中位数154.825/833.401ns仅列透明证据，不能混入非插桩裁决。没有原P3 JIT日志，不能倒推原回退原因。

### TierOff A15/B16

各输出7段FullOpts：PushFrameCore1044B、CallMethod1 1094B、m0_module_bare2233B、m2_bare_metadata559B、m0_method_calls813B、__thunk_12 114B、m0_read623B。

仅把10–16位十六进制绝对地址样式按每文件首次出现顺序重命名，并保留同地址的别名关系后，两份全部文本相同。实际原始地址不同，不声称原机器字节相等或完整语义证明；短offset、分支、helper调用形式和正文长度未隐藏。原日志、四份normalized文本、地址映射与空tieroff-normalized.diff都保留。这个结论只覆盖被选过滤器实际输出的7段，不扩张到整个运行时。

说明在该最优化条件下未看到所选热链的版本相关指令形状增量；不能把TierOff当默认替代，也不能凭此宣告默认无回退。

## 环境限制与停止决定

新根、快照/守卫造成的进程外活动及JIT日志都是诊断条件；未更换原Host/PE/工作量/暖机。所有显式环境字典及默认/关闭控制项按冻结清单执行。CPU affinity保持0–8未更改；/sys/fs/cgroup/cpu.max、cpu.stat等缺失明确记录，无法据此保证无外部资源波动。低loadavg也不是噪声排除证明。

目前可作的最强结论是：已证同版本漂移和插桩运行的JIT层级/代码形状差异，尚未建立“B2特定源码改变→原默认不利值”的可靠因果链，亦无精确已证多余热成本可安全修除。停止调查，保留原默认性能风险，文档记录本次诊断边界后完成既定提交；不再扩样、调warmup、改环境或进行推测性优化。

## 复核入口

- complete.json、start.json、authorization-run.json：唯一窗口与许可。
- runs/：全部16原记录、命令完整env、前后快照、资源计数、stdout/stderr与4份JIT原流。
- analysis/comparisons.json：全部九路径所有同版本和双方向组数值。
- analysis/default-native-comparison.json、default-native.diff、jit-inventory.json：层级与代码形状证据。
- analysis/reproduce.py：只读复算配对、层级清单、地址别名规范化，已通过；不运行负载。
- analysis/final-input-verification.json：11823冻结输入结束核验通过。

诊断原报告SHA256：ce1296bd8fba533b2f20a86ca451471d696ae813ed191216b709d8abbebd0fc2。
