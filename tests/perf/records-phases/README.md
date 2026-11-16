# 新dev records阶段诊断，不改产品

固定a1419ac5，原records.py直接读取产品树，字节未改。phases.py逐AST核函数和类体完全相同，仅主程序外加perf_counter，所有输出在阶段完成后。generate与parse分别计时，四次summarize分别计时；后者包含聚合、排序和格式化，不凭此拆出哈希/属性成本。

四轮独立默认产品构建；每轮完整原程序与阶段诊断各CP/JIT/AOT每臂12新进程，六排列平衡，288冷进程样本、8实际AOT清单。另轮JIT/AOT统计，不把分配与main_ms当wall因果。RSS是进程峰值，不是平台驻留/活堆；不加总阶段时间为完整原程序速度。阶段内连续四次聚合有缓存与GC历史，不称四次独立冷进程。

所有build和dump明确--native，CLI参数与provider互斥；保留真实MIR/CIL、原生清单与选项供独审核实入口，无provider参数或实验feature/flag。清单核records与direct accumulator关闭。普通默认生成器状态优化不等于实验开启。SDK10.0.403、CP3.12.13，固定源码与CLI/原生exe哈希。不运行默认check/搬迁或候选性能验收，只有诊断。

同源诊断计时输出剥离后必须与原程序完整stdout一致；stderr必须空且exit0，阶段必须恰为generate/parse/四summarize，非负有限上界。静态发射不等于动态热点归因，若无法确认native路径停止结论。共享runner、每轮单一构建与每cell四独立构建，不宣称统计显著或架构优化收益。原始报告在仓库外。

不归档完整二进制/SDK树：采样后删除本诊断独占构建目录，保留真实清单、原生hash、MIR/CIL和双流；这是运行证据而非可恢复二进制包。

新增time导入及计时全局变量可能改变模块优化；函数AST相同不保证发射完全相同，两输入MIR/CIL均保留。runtimeconfig与已安装运行时清单是选择约束证据，不等于进程加载追踪。

首轮43804453820仅SDK环境门失败：runner默认选择11.0.100，未进行产品构建或测试。修订采用独立DOTNET_INSTALL_DIR、host readlink与10.0.403实测断言，传播DOTNET/HOST_PATH/ROOT/ROOT_X64。使用独立CARGO_TARGET_DIR及显式RELEASE_DIR取得make发行CLI，不向固定产品树写global.json，保留git diff门。
JIT使用绝对DOTNET_HOST_PATH；额外独立非计时运行记录COREHOST_TRACEFILE以核实际Framework/host路径，完整双流仍相等。trace不启用于288计时样本或分配统计。
