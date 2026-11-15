# CPython桥专项：探索性实测与主代理复核（2026-11-15）

## 范围与结论

使用已验收sum版本的冻结CLI、CPython3.12.13和NumPy2.3.5，synchronized provider，macOS arm64。
7个程序的随包standalone NativeAOT均完成中文空格路径搬迁、输入/构建隐藏及空PATH运行，
搬迁双流和退出与当次CP oracle相同。主代理也核对全部70个计时进程的JSON：退出均0，逐项stdout/stderr一致。
本轮没有修改产品、没有新增兼容API，也没有运行新一轮完整门禁。产品仍为692f2f17。

短程序随包AOT端到端明显慢于CP，继续优化启动/导入交付路径有价值。
当前证据没有分离宿主初始化、模块导入、桥分派、原生加载和关闭各层成本，
尚不足以直接批准CaptureModulePaths门控、GIL重入或Vectorcall产品修改。

## 无marker主表

1次热身、5次CP→AOT交替，每次新进程；无RYUPY_STATS/RYUPY_CPYTHON_REPORT。
子代理原表使用墙钟均值、五进程RSS最大值；以下由主代理从原始样本重新计算墙钟与进程峰RSS的中位数。
每个RSS样本自身是wait4峰值，macOS字节正确折算为MiB。
注意本专项计时CP使用`python -B -c <源码>`，AOT使用发布入口；oracle阶段CP使用脚本文件。
因此该探索轮单列，不替代26项direct-wall-v2直接脚本基线。

| 程序 | CP墙钟ms | AOT墙钟ms | CP峰RSS中位数MiB | AOT峰RSS中位数MiB |
| --- | ---: | ---: | ---: | ---: |
| m4_1_numpy_objects | 32.59 | 139.37 | 26.75 | 46.97 |
| probe1_import | 32.55 | 137.12 | 26.69 | 44.38 |
| probe2_array_view | 33.98 | 143.91 | 26.73 | 46.58 |
| probe3_kernel_n1 | 32.78 | 141.61 | 26.75 | 46.69 |
| probe3_kernel_n10 | 32.52 | 139.64 | 26.72 | 46.66 |
| probe3_kernel_n100 | 33.10 | 141.82 | 26.75 | 47.00 |
| probe3_kernel_n1000 | 35.87 | 153.47 | 26.77 | 50.08 |

原始m4_1源码未改；import探针还打印版本号，不是纯粹的无输出导入。
probe2含数组/视图别名与原地修改；kernel为3×3整数数组的切片修改和广播，N=1/10/100/1000。
不同程序的总耗时相减不能命名单一层成本，N尺度差也只能说明该具体负载的端到端增量。
1warm5为新进程与页缓存热身，没有清空系统页缓存，不称为严格冷缓存测试。

## 独立报告轮

以下均来自真实报告字段；125指有文件路径的模块项，不包含所有内建/无文件模块。
主代理逐项核对这些路径属于搬迁后的各自私有分发；该核验不覆盖未执行的动态导入。

| 程序 | Enter计数bridge_calls | 关闭前live_handles_at_shutdown |
| --- | ---: | ---: |
| m4_1_numpy_objects | 202 | 58 |
| probe1_import | 157 | 14 |
| probe2_array_view | 187 | 49 |
| probe3_kernel_n1 | 189 | 45 |
| probe3_kernel_n10 | 261 | 111 |
| probe3_kernel_n100 | 981 | 741 |
| probe3_kernel_n1000 | 8181 | 7042 |

7轮state均closed，关闭后active_leases/handles/managed_roots/pending_releases/late_releases均0，
synchronization_failure均null。这些是退出快照，不等于运行期峰值、完整无泄漏证明或所有重入已验证。
关闭前句柄随N增加值得下一轮分析引用保活；不能仅凭该字段认定泄漏。
N=1/10/100/1000的bridge_calls符合181+8N，说明Enter总计数随负载增长，尚未分离各类桥调用。
报告轮脚本未单独断言双流，主代理不能把它提升为与无报告轮同等级的双流验收。

## Marker与sample的证据限制

marker程序在源码中调用perf_counter并flush，父进程communicate结束后解析这些源内时间戳；
没有逐行接收时刻，也没有独立首字节或退出阶段时间戳。marker版还额外导入sys/time，
它和原程序执行路径不同。脚本仅strip后比较业务stdout，未严格记录每次退出与完整stderr。
AOT marker父墙钟均值203.51ms，源内START→BEFORE_EXIT136.10ms；
源内标为import的区间133.15ms、kernel区间0.34ms，仅作为该带观测程序的粗线索。
差额67.41ms同时混入程序边缘、观测、调度及关闭，不能直接命名为Dispose或启动成本。
无法将133.15ms搬进无marker主表，或断言它就是宿主初始化占比；动态库重定位/字典填充归因尚未测量。

主脚本先在隐藏输入后沿旧provider路径构建采样目标失败，随后代理在独立命令重建百万轮目标，
取得sample/目标退出0并回填JSON。最终JSON只保留符号布尔值和匹配行摘要，完整采样树在临时目录被清理。
该补充目标没有同次CP oracle记录；保存的主脚本也不能独立复现补充采样命令。
摘要含PyImport与Python调用/内部vectorcall栈，只能确认采样窗口出现这些符号，
不能据此断言已进入稳态、定位RyuPy调用协议占比，或把CP内部vectorcall等同桥已实现Vectorcall。
CaptureModulePaths/GIL/同步符号未命中不能排除相应成本，尤其运行窗口未覆盖关闭时。
因此撤回原代理报告中“约133ms均为初始化大头”“无常态GIL等待”“无持续同步停顿”等过强结论。

## 下一轮建议与原始证据

本轮按用户要求到此停止，以下均未执行：
1. 统一直接脚本CP入口与中位数口径，在无marker程序中独占复测，冻结同输入和载荷。
2. 完善阶段探针的完整退出/双流和观测开销对照，再分离导入前、导入体与关闭；
   针对CaptureModulePaths单独测关闭窗口，保留完整带符号栈与时段，评估门控的错误/atexit合同。
3. 若高频调用证据成立，再单开Vectorcall竖切；租约重入必须有线程/GIL与异常配对证明。

原始`/tmp/ryupy-cpython-bridge-measurements.json`与脚本`/tmp/run_cpython_bridge_benchmarks.py`保留本机，
临时构建树已清理。普通文件不做hash验证。机器原始数据不提交仓库；本文保存审定结论及方法限制。
