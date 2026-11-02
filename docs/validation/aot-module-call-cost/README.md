# 模块调用冻结：正确性代价观测

状态：WIP `91efcfd0150441fce16a2f748c8dc803f95199e3`，尚未推广 main。
固定 [36进程CI](https://github.com/laull9/RyuPy/actions/runs/41476203839) 成功，
不是“无回归”或优化采用门槛通过。基线 `8b15b29b8b9e67086f386f62d22e6d4314b0a9d1`。

原 [Linux AOT](https://github.com/laull9/RyuPy/actions/runs/41476203926)、
[Windows/macOS](https://github.com/laull9/RyuPy/actions/runs/41476204024) 与
[固定C ABI机制](https://github.com/laull9/RyuPy/actions/runs/41476203939) 同SHA回归均成功。
这不代表新的公开Python FFI或B部署已实现。

## 真实固定矩阵

两版 × matmul64/100000次模块sqrt × JIT默认/禁tier/AOT ×3独立进程，共36进程；
每进程3warm+7sample，360校验/252计时点。8应用构建、2CLI及runtime准备、8份实际MIR/CIL
前置检查全部在同一1200秒预算内，实际94.282秒（含编译器准备57.564秒）。无追加测量。
每版J/T使用该版AOT输入DLL；IR检查确认原NumericList岛未丢、候选模块token进入真实sqrt调用。

|负载/后端|基线中位ms|候选中位ms|变化|整进程分配变化|
|---|---:|---:|---:|---:|
|matmul / JIT默认|2.449532|2.582142|+5.414%|+336 B|
|matmul / 禁tier|2.392767|2.732438|+14.196%|+344 B|
|matmul / AOT|2.225085|2.227470|+0.107%|+336 B|
|math模块 / JIT默认|18.386053|15.641076|-14.930%|+56,000,304 B|
|math模块 / 禁tier|6.837835|6.636472|-2.945%|+56,000,304 B|
|math模块 / AOT|7.217822|6.920975|-4.113%|+56,000,304 B|

触发全部五个事前诊断旗：matmul默认/禁tier三对同方向变慢且>5%与>0.1ms；
math三臂整进程分配均增加约108.8%，超过5%与1MiB。不能用math时间改善掩盖分配代价。
原每进程点、配对比及GC/RSS等保留在summary；三独立进程不冒21独立样本。

## 成本证据与限制

实际AOT反汇编可见模块token分配和固定一参数数组分配；100万次×(24+32)字节约56MB，
与整进程增量吻合。这是代码结构解释，不是分配栈采样的唯一归因证明。
matmul AOT函数5223→5268字节，math run374→392字节；NumericList创建/读取/追加仍命中。
不能据AOT反汇编把JIT禁tier的14.196%唯一归因为同一指令或直接称噪声。
保正确性前提下的更轻表示/调用约定仍待独立设计与验证，不能恢复旧错求值顺序。

此轮不是冷启动测试；默认JIT短预热不称Tier1稳态。资源stats是整进程，包含启动、校验、
3warm+7sample，不冒kernel隔离归因。AOT的JIT指标解释为N/A；构建时间含缓存/restore影响。
实际CLI文件没有归档，只能交叉compiler-summary与执行记录hash，不能称独立重hash。
原始日志/产物在Actions artifact保留14天，仓库只存精简数据和整体hash，不承诺raw永久可用。

另有明确授权的两次本地诊断运行，复用原artifact的两版DLL/runtime，仅单次结果校验、
禁tier/JitDisasm，不传hot、不启用stats，不加入原36性能点。两次首次均exit0且结果一致。
本机EPYC9V74不同于CI的9V45：FullOpts matmul代码4693→4776 B、栈504→536 B、
call指令83→89（新增2个捕获调用、4个get_Count调用），数值岛4个vmulsd/4个vaddsd仍在。
CallMethod1代码1155→1281 B。它们证明本机真实调用/内联/布局形态发生变化，
不能定量或唯一归因CI的14.196%。下一切先去token并恢复非模块PIC入口形态，
未知callable仍保独立参数数组，避免重入及数组逃逸被共享scratch破坏；尚未声称已改善。
