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
