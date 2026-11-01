# M3.9 累加器优化前基线（e1c1ee1）

本目录是未改生产源码的独立基线，不覆盖 M3.6/M3.7 结果。原负载是
`tests/perf/dataclass_hot.py`（不是 dataclass_hotpaths.py）。初始 git 树干净；测量结束仅父线程
新增 `m3_9_direct_accumulator.py` pending，见 status-before/after.txt。没有生产修改。

## 构建与重测

已在 e1c1ee101fb8480e1b375b62a0f056978cfca8aa 执行当前源码的 cargo build CLI 和 make runtime，
再用保存的 CLI 编译原负载。Cargo 验证并复用有效增量缓存，cli-build.time 不是冷编译时间。
Release runtime 与编译体一起保存在 artifact/，CLI 保存为 ryupy-baseline，hashes.txt 校验固定产物。
workload-build.time 仅编译/写出托管程序集，不包括进程启动；原始各命令日志单独保存。

在 toolchains/activate.sh 环境下：

```
python3 <本目录>/compare.py <新的结果目录> baseline=<本目录>/artifact optimized=<新产物目录>
```

compare.py 由已归档 M3.7 脚本口径整理：默认 tier / DOTNET_TieredCompilation=0 分开，每变体
3 个新进程，变体交错先后顺序；负载每进程 3 轮热身、7 轮测量。输出内函数体秒数与外部
process_wall_seconds 分开记录。先每进程中位数，再跨进程取中位数。不能把它与旧历史单次
样本直接相除宣称速度提升；优化后应同时重新运行本目录 baseline 与 candidate。

所有进程均检查 semantics True Record(x=3, y=4)，construction=1249975000、
field_reads=1400000、equality=50000。baseline-measurement/保存全部原始 stdout/stderr 和 JSON。

## 实际 compiled_field_reads 分配

harness/ 是诊断程序，不是生产路径。它加载真实 DLL、运行入口初始化模块，再读取同一
m0___mv3_first 对象并反射调用 m0_field_reads。实参数组在测量前复用。额外热身20次，
每进程5批，每批5次×200000循环；默认/禁tier各3个进程，全部校验结果。

每批均360 B/1000000轮，包括 Stopwatch/反射固定开销、holder与最终结果盒；不能说函数零分配。
这个诊断的 ns/轮不是正式性能表，正式比较使用 compare.py 的未插桩进程输出。

未来若 runtime ABI改变，诊断harness须在独立目录引用 candidate 实际 runtime重新构建，
不要把baseline runtime混给candidate。原程序与先前诊断副本分别在harness/Program.cs与
harness-original.cs，可核验方法/初始化来源。

## 实际 CIL 与 JIT

workload.cil/MIR由本目录CLI产生，field_reads.cil提取完整方法。CIL明确含每函数Create、
每轮Add、出口Value。实际运行分别用DOTNET_JitDisasm=*field_reads*与Add、
DOTNET_JitStdOutFile=<独立文件>、DOTNET_JitDisasmDiffable=1保存机器码；Add另限定
DOTNET_JitDisasmAssemblies=RyuPy.Runtime。JIT诊断进程与正式计时进程分离。

默认Instrumented Tier0方法687B，仍调用PyNumericAccumulator.Add；默认Tier1-OSR 2981B与
禁tierFullOpts 2141B没有Add公开包装调用。其内联报告与真实整合后的类型/溢出/状态更新指令
证明不能仅凭CIL声称每轮helper未被内联。禁tier仍含内部NumericAccumulator.Value/Store冷路径
及初始化/出口调用；holder-fastpath-disabled.asm显示r13+0x18状态位、+0x10整数、+0x08盒字段
的读取/写回。优化应证明消除holder状态/缩减机器码或改变guard布局的实际效果，而非仅少一个
CIL Call。diagnostic-summary.json保留分层版本与调用计数、分配全部批次。

环境非隔离裸机、未固定CPU频率；容器lscpu因sysfs缺失不可用，保存/proc摘要与.NET/Rust信息
于environment.txt。本阶段不做性能承诺、不提交仓库文件。
