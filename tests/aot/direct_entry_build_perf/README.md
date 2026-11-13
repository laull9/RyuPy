# 直接入口完整构建成本反事实对照

这是同一整合提交、同机同默认配置的单变量实验，不是完整历史版本的 before/after，也不测应用运行速度。不修改 dev。

`setup.py --base <独占目录>` 准备两份 Release CLI、唯一 Runtime、锁定 pyfiglet / binaryornot 项目及 `setup.json`。控制组仅把源码 `EntryMode::select` 中 Direct 分支改成 ThinHost；保存补丁与工具身份。`run.py --base <同目录>` 进行如下测量：

- 两应用各 direct/thin 一次预热（4次不计样本），再各4个ABBA/BAAB块。每应用每组8次，共36次公开 `build --project`。
- pyfiglet 使用 `metadata` 入口，真实加载 console_scripts；binaryornot 使用 `scan` 普通分类 consumer。两边使用同一应用源码路径，保存完整managed原SHA；跨轮仅允许经过MZ/PE头定位的4字节COFF生成时间戳不同，除此之外全文件必须逐字节相同。另存仅此字段归零的等价摘要和原时间戳/偏移，绝不归一化MVID、metadata或代码。
- 每轮独占等长新build root，不复用obj/bin。NuGet审计从预热开始就在两组禁用；预热后将本轮 NuGet.Config 清空所有源，两组同一热包缓存。逐次核实际project.assets的配置文件、包目录与无恢复源；缺包直接失败，缓存内容库存前后必须不变。不宣称整台runner断网。
- 保存总wall、前端末标记及SDK Target/Task Performance Summary原文。IlcCompile/LinkNative是target，Csc是task，可能重叠，禁止相加冒充wall。Direct仍可能执行不调用编译器的Csc任务。
- 核真实ILC唯一主输入：Direct与本次managed逐字节相同；ThinHost不同且保留精确Program.cs。默认优化有效收据、Runtime身份、metadata报告、源/资源集合在两组一致。
- 全部计时完成后复制每份dist，隐藏原项目和原build树，在无关cwd以最小环境执行全部产物。pyfiglet standard/slant/listfonts、binaryornot四种输入与冻结CPython原流严格相同。运行只作正确性检查，其时长不计优化结论。

保留失败原流，失败不自动重跑、样本不剔除。只在全部样本完整且运行通过后按应用输出8次分布及4个配对块差；不作显著性、跨机器/跨系统、冷构建或普遍更快声明。热OS缓存没有清空；包缓存库存读取本身也会预热文件缓存。配置禁源不等于无网络容器。

```sh
python3 -B tests/aot/direct_entry_build_perf/run.py --base /absolute/fresh-base
python3 -B -m unittest discover -s tests/aot/direct_entry_build_perf -p 'test_contracts.py'
```

成功完成时原件包括36次SDK工作树、dist与日志，按setup/raw和两批SDK工作树/搬迁产物分包上传；失败则保留已产生的原件，不裁掉失败/中间证据。NuGet缓存本体不上传，保留完整库存JSON、project.assets与包身份/下载证据供核验，因此不是完整base目录副本。artifact并非新的运行时缓存资格证明。

首轮 CI43371083829 在CP oracle阶段停止，尚未产生构建样本：上游pyfiglet列字体调用site提供的`exit`，错误的`-S`令其NameError。保留失败原件；修为真实普通CP启动（仍固定版本、禁用户site、冻结PYTHONPATH），不注入exit替身或改上游。后续独立完整重跑不复用失败样本。

第二轮 CI43371785767 在同应用第二次预热的managed原SHA准入处停止，未形成性能比较。原件独审两份224256字节PE仅offset136一字节不同，位于COFF时间戳4字节字段（1794547575与1794547586）；全部metadata流逐字相同，MVID均为零。vendor写入器明确使用当前时间。保留该失败原件；后续只采用上述精确时间戳等价合同，不改原始PE，也不放宽每轮Direct实际ILC输入与该轮managed的原始SHA一致检查。

## 同源真实应用构建成本（2026-11-13）

CI [43372683973](https://github.com/laull9/RyuPy/actions/runs/43372683973)，实验提交 `1f52498938eaffa18fc07f39d02b110152e9f51b`。同一 Linux runner（4 vCPU AMD EPYC 7763）、SDK 10.0.403、CPython 3.12.13、同字节 Release Runtime、默认优化，两份同源CLI仅入口路径不同。每应用每组8次计时，4个交错配对块，另4次预热；36份产物全部完成搬迁运行，126组退出码/stdout/stderr与冻结CP一致。

| 应用 | 原薄宿主中位数 | 直接入口中位数 | 本机本配置降低 |
| --- | ---: | ---: | ---: |
| pyfiglet 1.0.2，真实metadata入口及完整553字体 | 13.744 s | 11.999 s | 12.695% |
| binaryornot 0.4.4 / chardet 5.2.0，真实分类应用 | 14.425 s | 12.730 s | 11.750% |

两应用各4个配对块均为直接入口更短。SDK原始计时中Csc任务中位数约从1.50秒降至0.007秒；ILC仍占主要成本（直接入口分别约7.43/8.84秒）。Target/Task可能重叠，不能相加冒充wall，也不能把完整差值全部归因为Csc。Direct的Csc任务存在不等于调用了C#编译器，应以真实ILC主输入逐字节校验和项目选项为依据。

本结果只覆盖Linux、热包/OS缓存、此两真实应用和默认配置的完整构建，不证明运行速度、冷构建、其他平台或Nuitka总体领先。测量禁NuGet源并核缓存库存不变，不是整台runner断网。所有样本保留；两次先前失败均在预热完成前停止，没有可用比较样本，不混入本次统计。

原始PE保留原SHA，跨轮仅允许定位后的4字节COFF生成时间戳不同；metadata/MVID/代码及其余全文件不允许变动，Direct每轮实际ILC输入仍须与该轮原managed完全相同。原SDK工作树过大，证据分块CI只切分原ZIP连续字节并核完整SHA，不重跑构建或更改样本。

基线包含用户dev截至`cd678640`的六提交；测量结束后用户dev已前进至`3237be71`（标准库catalog/静态来源探测与导入别名修复），本表不声称包含或验证后两提交。未改dev/main，后续整合须保留用户新增功能与测试，并处理ADR0174编号重叠。本次文档整合保留用户ADR0174，将显式资源目录选择重编号为[ADR0186](../../../docs/adr/0186-explicit-resource-trees.md)；这不改变上述性能测量基线。

完整SDK原件已独立核验36组：真实publish计时、project.assets配置/包目录/audit、实际ILC输入、项目入口选项、原始及搬迁发布文件全集逐项size/SHA。setup/raw与原ZIP的身份见[机器可读结果](results/linux-20261113.json)；原ZIP分块取回CI为[43375372825](https://github.com/laull9/RyuPy/actions/runs/43375372825)。
