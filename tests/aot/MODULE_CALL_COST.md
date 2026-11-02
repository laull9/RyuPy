# 模块调用目标冻结：有界正确性代价观测

状态：工具轻测8/8，固定36进程已完成，触发5个成本诊断旗；完整正负结果见[证据](../../docs/validation/aot-module-call-cost/README.md)。修复真实模块属性调用的求值次序，
不是以“必须加速”作为采用条件，也不能用恢复错误语义来回避代价。

基线固定 `8b15b29b8b9e67086f386f62d22e6d4314b0a9d1`；候选为实际工作流
`github.sha`。两者独立 clean checkout/target/runtime，首构建前统一1200秒预算。
包括2 CLI及runtime准备、8应用构建、元数据/8份MIR-CIL结构检查与36热进程；
每build180秒/应用120秒并受总剩余截断，失败或缺项停止，不追测至有利。

两负载为原 matmul64逐字源和每轮100000次 `math.sqrt(4.0)` 的模块调用循环。
固定CPython3.12.13参考校验分别64345.098/200000；基线与候选各JIT默认、禁tier、AOT，
每格3独立进程，每进程3warm+7sample，合计360校验/252样本。固定轮转顺序见
`module_call_schema.py`。J/T使用同版本AOT输入DLL。默认JIT短预热不等于Tier1稳态。

采样前实际两CLI对同一冻结源产生MIR/CIL，锁NumericList Create/两次Read/Append数量、
候选typed Append块无capture而fallback保留，91ef首次候选token真实流入sqrt调用。
这是特定源结构门槛，不是全编译器优化正确性证明。静态 Func 重绑身份和实例descriptor旧债
另存正期望，不宣称通用调用体系完成。

保全部每进程7点、三进程median、配对比、整进程alloc/GC/heap/RSS及源码/编译器/runtime/
托管输入/native hashes。整进程资源包含启动、3warm+7sample和验证，不冒kernel归因；
AOT JIT计数解释为N/A。构建时间含restore/cache影响，不比较纯编译器速度。

事前诊断旗：三个paired中位数同方向变慢且格median增加>5%及>0.1ms；
分配同方向增加且格median增加>5%及>1MiB。未触旗的所有负项也保留，
不称统计显著或零成本。模块token/参数数组可能增加分配，须按结果解释而非提高阈值。

工作流只由本实验工具/fixture/workflow改动自动触发，另保手动入口；不因每次runtime改动
无限追加36进程。raw evidence仅Actions artifact，retention14天；库中只持久精简结果、
hash与CI链接，不承诺raw永久保留，不提交临时日志。


## 后续双值候选：保持原固定配对合同

首次36与五个负旗永久保留上述精简证据，不用不同job相减推断去token收益。
本地双值候选以Bool+Dynamic保留目标冻结，去除token对象，但未知callable独立参数数组仍保留。
在实际结构/语义通过后，仅用同一baseline `8b15b29b8b9e67086f386f62d22e6d4314b0a9d1`
对新候选重做原36/8应用/1200秒；不增加91ef第三组，不改变fixture/输出/诊断旗。
候选结构门槛改为同receiver的IsModule与CaptureModuleTarget及真实captured调用，
并核局部未重写、typed Append块无这两个helper；普通PIC移除token检测由独立runtime源码/测试锁。
当前本地完整门禁及11项工具测试通过，新的性能组尚未启动；32B每调用剩余成本只是结构估计，不是新实测。
