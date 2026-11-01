# 最后bounds证明专项：只一个既有matmul目标

保存当前默认CLI/runtime及既有numeric matmul编译体；不在准备阶段构建或运行。
当前工作树并发已含ReadChecked，因此capture-working-tree.patch不冒称beforeCLI来源。numeric-body-source.patch与旧matmul产物准确复用最终numeric修正；SLP前后matmul MIR/CIL相同证据见slp-first。默认flags已关闭，父最终gate证明CLI政策状态。

GO后旧body+新runtime，对比新body+同新runtime，只隔离成功guard区域ReadChecked变化。源码/完整hash、runtime同一性保留。候选只省第二显式normalized<count，CLR数组/List检查仍在，不宣称整循环无边界检查。

只原100×100连续3次matmul真实compiled函数，初始矩阵创建和checksum核验外。default/disabled各3进程×3正式样本、每进程3预热；B/C轮转。时间12进程；资源另两tier×两方=4进程、3样本；实际JIT再4进程。合计20个诊断/测量进程，语义初始化逐次核对。无新增micro或原list第三轮重试，不重测Nbody。

复用core harness及run_bounds_pair.py --samples 3 --processes 3 --warmups 3 --resource-processes 1 --resource-samples 3。只跑一次，任何异常/输出不符先报告父。JIT必须对比guard路径重复normalized<count是否真正消掉、保下层检查，保raw方法大小和caller/callee证据，不拿源码proof替代机器码。资源/GC/JIT与时间分开。

104整26保持暂缓，待本切focused/gate+专项终态才执行一次，不把bounds冷/热结论混入单样本全套。
