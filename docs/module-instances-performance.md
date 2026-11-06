# 模块实例基础B1性能报告

- 日期：2026-11-06；本地Linux/.NET 10 JIT，非AOT。
- A为已验find_spec运行时5cbaa9de…，B为Release运行时8c36fea5…；共同PE 9e6ce5c4…与原Host保持不变。
- 最终编译器为Debug d52be252…，仅用于生成产物，编译耗时不进入运行性能。
- 源冻结63583885…；完整门禁2337 C#/817 Rust/453 CP/453 selftest/320 e2e通过，137 ignored。
- 固定default/tieroff各common A1/B1/B2/A2和两实例进程；普通168、新实例36暖样本。
- P1双CP，P2一次Host构建、两次编译、四审计、两烟测；P3仅下述一次固定矩阵。
- 独立CIL核默认两PE的46方法、100字段及所有引用/EH一致，仅COFF时间戳两字节不同；runtime97个增删改方法项全部归因，既有公开ABI保留但布局并非零变化。
- 实例正文是真实Python编译的两句counter/result计算，Host只创建/注入/执行/检查。

详见[设计取舍](adr/0123-module-instance-foundation.md)与[支持合同](module-instances.md)。

12正式进程、204暖样本、12首次Main观测完成；无失败、超时、重跑、补采或事后调参。所有数据仅本候选Linux JIT；不声明统计显著，不把实例新API抵消默认回退。

formal-summary.json SHA256：5f9229f0b99df67ec6d18f81ade45f3fd7b50dda741a2caf3a6956858e21ea91

## 普通同PE：全部配对

单位ns/item；正变化是不利观测。每进程3样本中位数；全部原样本、CPU/GC/checksum另保原始JSON。

|配置/配对|路径|A|B|变化|A→B B/item|
|---|---|---:|---:|---:|---:|
|default/1|static_calls|287.607|309.236|+7.52%|33→33|
|default/1|dynamic_defaults|136.152|130.069|-4.47%|33→33|
|default/1|plain_def_create|81.693|94.688|+15.91%|192→192|
|default/1|closure_create|87.790|101.305|+15.39%|256→256|
|default/1|module_external|652.209|745.479|+14.30%|24.625→24.625|
|default/1|module_bare|892.425|1013.557|+13.57%|96.625→96.625|
|default/1|resource_files|2590.148|3144.695|+21.41%|1296→1296|
|default/2|static_calls|310.560|320.022|+3.05%|33→33|
|default/2|dynamic_defaults|128.502|125.822|-2.08%|33→33|
|default/2|plain_def_create|97.758|105.194|+7.61%|192→192|
|default/2|closure_create|88.724|90.310|+1.79%|256→256|
|default/2|module_external|755.633|691.572|-8.48%|24.625→24.625|
|default/2|module_bare|946.145|896.487|-5.25%|96.625→96.625|
|default/2|resource_files|2833.575|2990.319|+5.53%|1296→1296|
|tieroff/1|static_calls|76.559|68.934|-9.96%|33→33|
|tieroff/1|dynamic_defaults|40.960|37.717|-7.92%|33→33|
|tieroff/1|plain_def_create|50.295|45.642|-9.25%|192→192|
|tieroff/1|closure_create|62.458|67.251|+7.67%|256→256|
|tieroff/1|module_external|177.402|169.637|-4.38%|24.625→24.625|
|tieroff/1|module_bare|253.296|218.432|-13.76%|96.625→96.625|
|tieroff/1|resource_files|1175.064|1068.876|-9.04%|1296→1296|
|tieroff/2|static_calls|77.756|80.327|+3.31%|33→33|
|tieroff/2|dynamic_defaults|42.086|38.220|-9.19%|33→33|
|tieroff/2|plain_def_create|44.849|69.626|+55.25%|192→192|
|tieroff/2|closure_create|60.681|63.526|+4.69%|256→256|
|tieroff/2|module_external|187.357|180.968|-3.41%|24.625→24.625|
|tieroff/2|module_bare|235.492|210.737|-10.51%|96.625→96.625|
|tieroff/2|resource_files|1138.960|1087.730|-4.50%|1296→1296|

## 普通首次Main和launch

所有共同coldMain的GC0/1/2均0；分配每配对+344B。launch只到首次Main完成标记，包含CLR/Host准备与传输，不是纯Python初始化。

|配置/配对|Main A→B ms|Main变化|分配 A→B B|CPU A→B ms|launch A→B ms|
|---|---:|---:|---:|---:|---:|
|default/1|61.647→88.659|+43.82%|460416→460760|61.771→88.856|106.334→137.505|
|default/2|71.117→71.070|-0.07%|460416→460760|71.234→71.176|116.382→118.988|
|tieroff/1|173.765→168.748|-2.89%|464512→464856|173.844→168.874|233.286→229.411|
|tieroff/2|165.789→173.003|+4.35%|464512→464856|166.011→172.177|222.598→231.216|

## 实例完整操作：所有独立进程

create_seed_execute包含创建、取真实dict、三键种入、执行。alternating_two_instances一次操作包含A+B两次Execute，不能冒作单次执行。无CP速度比。

|配置/进程|路径|ns/完整操作|B/完整操作|每操作执行次数|
|---|---|---:|---:|---:|
|default/1|execute_existing|1014.793|128.000000|1|
|default/1|create_seed_execute|11353.097|4892.304688|1|
|default/1|alternating_two_instances|1802.198|256.000000|2|
|default/2|execute_existing|1245.448|128.000000|1|
|default/2|create_seed_execute|11338.901|4892.304688|1|
|default/2|alternating_two_instances|1946.307|256.000000|2|
|tieroff/1|execute_existing|375.834|128.000000|1|
|tieroff/1|create_seed_execute|4936.141|4836.304688|1|
|tieroff/1|alternating_two_instances|601.139|256.000000|2|
|tieroff/2|execute_existing|347.294|128.000000|1|
|tieroff/2|create_seed_execute|5730.500|4836.304688|1|
|tieroff/2|alternating_two_instances|622.978|256.000000|2|

## 实例首次边界

首次Main真实登记、首次创建、首次种入、首次执行分别计时。launch只到Main，后续三段不在launch内。所有这些冷段GC0/1/2均0。分段计时不能相加冒作无间断冷启动。

|配置/进程|边界|墙钟ms|线程B|CPU ms|
|---|---|---:|---:|---:|
|default/instance-Bnew1|main_registration|55.978431|441120|56.096000|
|default/instance-Bnew1|first_create|8.836542|48448|8.840000|
|default/instance-Bnew1|first_seed|0.161503|352|0.165000|
|default/instance-Bnew1|first_execute|1.030941|56|1.034000|
|default/instance-Bnew1|launch through Main|96.354068|不适用|不适用|
|default/instance-Bnew2|main_registration|57.458867|441120|57.585000|
|default/instance-Bnew2|first_create|10.646364|48448|10.691000|
|default/instance-Bnew2|first_seed|0.132109|352|0.134000|
|default/instance-Bnew2|first_execute|1.057611|56|1.061000|
|default/instance-Bnew2|launch through Main|103.025017|不适用|不适用|
|tieroff/instance-Bnew1|main_registration|125.341892|445216|125.461000|
|tieroff/instance-Bnew1|first_create|30.150982|48392|30.158000|
|tieroff/instance-Bnew1|first_seed|0.388945|352|0.393000|
|tieroff/instance-Bnew1|first_execute|4.164964|56|4.169000|
|tieroff/instance-Bnew1|launch through Main|179.196909|不适用|不适用|
|tieroff/instance-Bnew2|main_registration|133.835613|445216|133.972000|
|tieroff/instance-Bnew2|first_create|37.832997|48392|37.841000|
|tieroff/instance-Bnew2|first_seed|0.450337|352|0.453000|
|tieroff/instance-Bnew2|first_execute|4.270313|56|4.276000|
|tieroff/instance-Bnew2|launch through Main|186.671391|不适用|不适用|

## 归因与未关闭风险

旧共同PE、旧Host完全复用，只替换runtime；新compiler默认PE另经完整CIL独审，未增加新compiler性能臂。真实实例新Host只调用真实登记代码，未以手写正文或helper微测替代。

default两配对static、plain_def_create、closure、resource均不利；tieroff plain_def_create第二对+55.25%而第一对−9.25%，必须一起保留；tieroff closure两对均不利。旧路径暖分配不变不能证明无时间回退。共同coldMain稳定+344B是新增成本，default第一对冷墙钟明显不利；其余全部冷配对见上表。

实例execute_existing暖128B/操作，双实例完整交错操作256B；创建种入执行default约4892.304688B、tieroff约4836.304688B的非整数摊销值原样保留。首次Execute为56B不等于暖128B，冷/暖状态与数值装箱差别不能抹平。没有开展额外归因执行，不能据此认定具体优化责任。

既有ModuleSpec冷Main+17648B、find_spec+2008B及历史资源/动态调用/overload风险仍未关闭。本轮新实例绝对成本不能抵消这些风险，不声称无回退、统计显著、AOT或跨平台结果。

全部原stdout/stderr/command/result/每样本JSON保留在formal；完整数字、校验值与机器/运行时信息在formal-summary.json及单进程JSON。
