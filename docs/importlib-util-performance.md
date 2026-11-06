# find_spec 有限发现性能记录

- 日期：2026-11-05。仅Linux JIT、真实公开Python函数，不外推AOT或其它平台。
- 状态：唯一正式矩阵12进程、264暖样本、12首次Main观测全部完成，无失败/超时/补采。
- 共同源同一PE和Host，仅替换runtime；新API没有CP性能分母。

## 身份和方法

A runtime是上一切ModuleSpec版本，SHA256 c2fb6b3f3b423631ae13eedb246bbc3125b00fba05b15051dccf619b772ca7f8；
B runtime为find_spec Release候选，SHA256 5cbaa9deb3e524d90ada91fed58cb003840e7e88648679b2386ccc8dceaf15ba。
固定debug compiler SHA256 f6956679307d0f88483916e5b442d94505d55c3d480e1450e4e973875764ba92不变，
生成PE配Release runtime；共同PE 9e6ce5c4b4db39792daac67d24dc1ddde260b8fbf52a8b0c69c5d45ffcfa5983
不重新编译。API PE为956606dc88b2a3feb670d9daeff6280c15c4d080e008cf7f7329c932dabf3897。

每配置按A1、B1、B2、A2、API1、API2执行；default无tier覆盖，tieroff关闭TieredCompilation和TieredPGO。
每进程3暖样本，先取进程中位数，再保留A1/B1、A2/B2两镜像配对。无校准、重跑、剔除异常值或挑最好值。
共同loop路径64次预热、512调用×64item；single路径256次预热、8192调用×1item。
API八路径均为公开编译函数；未缓存目标用实际--include-module登记而不执行，父包首次导入在Main内。
计时前后核对象/别名/包列表身份及副作用账本；Host消费返回值及checksum。每进程45秒、矩阵480秒硬上限。

708共同类型独审未发现未归因CIL差异或公开ABI破坏；PyFunction18、PyOps293、PyPic37方法CIL不变。
新增util登记、loader协议和元数据构造仍有真实冷成本。CIL不变不能推出机器码或耗时不变。

## 共同路径：全部镜像配对

时间为ns/item。正百分比是不利观测；不宣称统计显著。各路径分配在四个配对中都不变。

| 配置/配对 | 路径 | A | B | 时间变化 | A→B B/item |
| --- | --- | ---: | ---: | ---: | ---: |
| default/1 | static_calls | 297.312 | 302.285 | +1.67% | 33→33 |
| default/1 | dynamic_defaults | 134.603 | 136.153 | +1.15% | 33→33 |
| default/1 | plain_def_create | 80.932 | 96.723 | +19.51% | 192→192 |
| default/1 | closure_create | 92.290 | 100.612 | +9.02% | 256→256 |
| default/1 | module_external | 799.851 | 707.741 | -11.52% | 24.625→24.625 |
| default/1 | module_bare | 1019.081 | 857.023 | -15.90% | 96.625→96.625 |
| default/1 | resource_files | 3996.429 | 2956.847 | -26.01% | 1296→1296 |
| default/2 | static_calls | 301.015 | 281.831 | -6.37% | 33→33 |
| default/2 | dynamic_defaults | 124.496 | 135.230 | +8.62% | 33→33 |
| default/2 | plain_def_create | 97.589 | 87.540 | -10.30% | 192→192 |
| default/2 | closure_create | 92.878 | 96.121 | +3.49% | 256→256 |
| default/2 | module_external | 762.105 | 838.386 | +10.01% | 24.625→24.625 |
| default/2 | module_bare | 1017.041 | 933.422 | -8.22% | 96.625→96.625 |
| default/2 | resource_files | 2942.301 | 4367.643 | +48.44% | 1296→1296 |
| tieroff/1 | static_calls | 79.808 | 71.832 | -9.99% | 33→33 |
| tieroff/1 | dynamic_defaults | 39.835 | 56.303 | +41.34% | 33→33 |
| tieroff/1 | plain_def_create | 47.180 | 65.663 | +39.17% | 192→192 |
| tieroff/1 | closure_create | 71.896 | 61.465 | -14.51% | 256→256 |
| tieroff/1 | module_external | 194.687 | 183.649 | -5.67% | 24.625→24.625 |
| tieroff/1 | module_bare | 270.366 | 261.947 | -3.11% | 96.625→96.625 |
| tieroff/1 | resource_files | 1356.796 | 1481.573 | +9.20% | 1296→1296 |
| tieroff/2 | static_calls | 73.398 | 70.895 | -3.41% | 33→33 |
| tieroff/2 | dynamic_defaults | 37.207 | 43.820 | +17.77% | 33→33 |
| tieroff/2 | plain_def_create | 43.894 | 46.003 | +4.80% | 192→192 |
| tieroff/2 | closure_create | 56.990 | 60.181 | +5.60% | 256→256 |
| tieroff/2 | module_external | 159.934 | 171.346 | +7.14% | 24.625→24.625 |
| tieroff/2 | module_bare | 209.144 | 265.727 | +27.05% | 96.625→96.625 |
| tieroff/2 | resource_files | 1214.243 | 1155.455 | -4.84% | 1296→1296 |

## 首次Main与启动

coldMain只含首次真实Main；launch从Popen前到Main完成标记，另含CLR加载、Host准备和管道传输，
不能冒称纯Python初始化时间。四对共同源coldMain线程分配都增加2008B。所有首次Main的GC0/1/2均为0。

| 配置/配对 | Main A→B ms | Main变化 | 分配 A→B B | CPU A→B ms | launch A→B ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| default/1 | 72.307→64.104 | -11.34% | 458408→460416 | 72.521→64.208 | 119.299→109.693 |
| default/2 | 62.538→66.177 | +5.82% | 458408→460416 | 62.607→66.378 | 109.943→116.441 |
| tieroff/1 | 186.696→202.760 | +8.60% | 462504→464512 | 186.794→202.842 | 246.515→270.892 |
| tieroff/2 | 167.329→210.935 | +26.06% | 462504→464512 | 167.451→211.036 | 224.102→282.164 |

## 新API：全部独立进程绝对成本

时间和分配均为完整Python函数调用，不是内部helper微测。uncached指目标未执行/未缓存，
不等于CLR冷态或父包未初始化。两个规格/loader比较是独立等价对象，非同一对象捷径。

| 配置/进程 | 路径 | ns/函数 | B/函数 |
| --- | --- | ---: | ---: |
| default/api-Bnew1 | cached_spec | 412.098 | 0 |
| default/api-Bnew1 | cached_keyword_alias | 330.814 | 120 |
| default/api-Bnew1 | uncached_plain | 15373.336 | 3304 |
| default/api-Bnew1 | uncached_package | 11590.478 | 3624 |
| default/api-Bnew1 | uncached_absolute_child | 11643.119 | 3352 |
| default/api-Bnew1 | uncached_relative_child | 12171.434 | 3448 |
| default/api-Bnew1 | spec_eq | 3019.427 | 368 |
| default/api-Bnew1 | loader_eq | 429.687 | 96 |
| default/api-Bnew2 | cached_spec | 144.158 | 0 |
| default/api-Bnew2 | cached_keyword_alias | 274.442 | 120 |
| default/api-Bnew2 | uncached_plain | 15140.415 | 3304 |
| default/api-Bnew2 | uncached_package | 11865.094 | 3624 |
| default/api-Bnew2 | uncached_absolute_child | 11412.504 | 3352 |
| default/api-Bnew2 | uncached_relative_child | 9093.799 | 3400 |
| default/api-Bnew2 | spec_eq | 2697.865 | 368 |
| default/api-Bnew2 | loader_eq | 322.843 | 96 |
| tieroff/api-Bnew1 | cached_spec | 53.963 | 0 |
| tieroff/api-Bnew1 | cached_keyword_alias | 110.119 | 120 |
| tieroff/api-Bnew1 | uncached_plain | 7759.950 | 2752 |
| tieroff/api-Bnew1 | uncached_package | 7926.744 | 3072 |
| tieroff/api-Bnew1 | uncached_absolute_child | 7645.140 | 2800 |
| tieroff/api-Bnew1 | uncached_relative_child | 7786.867 | 2896 |
| tieroff/api-Bnew1 | spec_eq | 1312.821 | 320 |
| tieroff/api-Bnew1 | loader_eq | 165.984 | 96 |
| tieroff/api-Bnew2 | cached_spec | 54.912 | 0 |
| tieroff/api-Bnew2 | cached_keyword_alias | 149.870 | 120 |
| tieroff/api-Bnew2 | uncached_plain | 11133.014 | 2752 |
| tieroff/api-Bnew2 | uncached_package | 11180.917 | 3072 |
| tieroff/api-Bnew2 | uncached_absolute_child | 9119.723 | 2800 |
| tieroff/api-Bnew2 | uncached_relative_child | 9135.752 | 2896 |
| tieroff/api-Bnew2 | spec_eq | 1378.213 | 320 |
| tieroff/api-Bnew2 | loader_eq | 149.733 | 96 |

| API配置/进程 | Main ms | Main B | CPU ms | launch ms |
| --- | ---: | ---: | ---: | ---: |
| default/api-Bnew1 | 91.818 | 3549928 | 91.922 | 147.469 |
| default/api-Bnew2 | 79.410 | 3549928 | 79.073 | 133.173 |
| tieroff/api-Bnew1 | 182.832 | 3550712 | 182.945 | 239.891 |
| tieroff/api-Bnew2 | 184.404 | 3550712 | 184.074 | 236.512 |

## 风险、原始证据与后续

本轮只完成测量，不宣称性能问题解决：default资源路径两配对为−26.01%/+48.44%，不能选前者宣布收益；
禁tier dynamic_defaults为+41.34%/+17.77%、plain_def_create为+39.17%/+4.80%，
共同Main禁tier为+8.60%/+26.06%。其余有利与不利配对均在上表，不按代码未变或分配不增抹去。
新API默认相对查找两进程3448/3400B，不能压成同一分配数；所有原样本保留，包括明显时间波动。
后续若定位，要另开有明确假设的有界窗口，不在本窗口补采或靠新API成本抵消旧路径风险。
上一切ModuleSpec冷Main+17648B、旧PE资源不利、overload及更早风险均未关闭。

完整证据根为私有恢复材料中的importlib-util-validation/performance：
- PROTOCOL.zh.md、iterations.json、reuse-manifest.json及input-p1/p2/p3.json保方案/源/工具/参数身份；
- p1两份CP oracle、p2编译/真实CIL/两份smoke、p3-complete.json与全部formal原流/command/result/JSON保留；
- audits/independent-review.zh.md、independent-check.json、independent-integrity.json归因全程序集；
- formal JSON含每个样本CPU、GC0/1/2、分配、原时间、语义checksum及唯一首次Main，不只保中位数；
- formal-summary.json SHA256为d792e87714bcfad2841465a9bc2ce4e093570d9c63d95628adc66bbdeb36649d。

P1两次CP曾使用旧B5目录作cwd，隔离绝对源oracle有效；原命令/原流保留、不重跑。
P2/P3通过已冻结v2启动器明确绑定新工作目录，未改旧证据。功能与性能范围见[ADR0122](adr/0122-importlib-util-find-spec.md)。
