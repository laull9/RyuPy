# M3.10 首切：短 F64 算术链单轮配对

## 实际结果与限制

本轮不宣称所有输入净赢，也不以微负载代替项目分母。`summary.md/json` 是完整绝对时间/每轮 ns/分配表；所有正式样本保留，不追加统计轮次、不删不利值。

- 30000轮精确float的两算术节点：分配1,440,000→720,000B；五节点平方和3,600,000→720,000B。默认时间分别-65.7%/-69.0%，禁tier-64.2%/-74.1%。
- **默认首bool失配+32.1%（113.09→149.43ns/轮），首int失配+24.1%（13.26→16.45ns/轮）**，不可把失败守卫说成免费。禁tier这两项为-3.4%/-3.0%；所有回退场景分配相同。首custom/末radd身份与副作用计数均校验。mixed默认+1.1%、禁tier+2.3%，仅1/3输入精确float命中，少240000B；比例来自确定输入序列，不冒称插桩计数。
- 真实matmul kernel（100×100连续3次）：default157.556→136.033ms（-13.7%），禁tier204.180→178.882（-12.4%）；约144.67MB→72.67MB，每次kernel少72MB。
- 真实nbody kernel（100000步）：default753.654→607.171ms（-19.4%），禁tier920.651→770.450（-16.3%）；约679.31MB→415.27MB，少约264MB（38.9%）。不能按所有理论链估成少276MB：候选MIR中最后`b[2] += dt*b[5]`仍是动态Mul/Add，有限尝试预算未覆盖；本轮未为基准调预算。
- JIT快边为直接double算术，采集文本中未出现FMA指令；原有PIC也可能被JIT内联，不能声称首次消除每次helper调用。**总机器码不保证更小**：禁tier matmul2442→2569B、advance8106→8471B。默认tier版本数/形状有差异，所有版本与原始hash见`jit-summary.json`及asm。
- 一次已知并发扰动：父报告另一worker在2026-11-01 16:51:11 UTC左右跑固定13短基线约0.05s。按wall与文件mtime估计重叠`nbody-default-time-candidate-0`进程（约16:51:07.399–14.675），不能精确定位7样本中哪条。`concurrency-note.json`保留限制；未删除、修正或重跑该样本/整组。

## 来源与运行协议

基线是提交9023f21的M3.9默认实现：保留直接累加器/叶内联实验开关关闭，TupleScalar与有界Interval默认。保存CLI来自M3.9 full-suite的Release CLI，与本轮开始时`target/release/ryupy`哈希完全相同；非从正在编辑的M3.10树重建。matmul/nbody旧DLL来自同基线归档产物。候选使用本次冻结Release CLI、`source.patch`同时包括tracked和未跟踪crates/runtime源码，以`base-commit.txt`为基点。

两方使用完全相同的Release runtime（见`binary-hashes.txt`），本切无新增C#运行时算术解释器。所有二进制只保留/tmp：

- `/tmp/ryupy-m3_10-float-baseline-ST6PGl`
- `/tmp/ryupy-m3_10-float-candidate-Fvkv7H`

固定CPython 3.12.13先验证pending与perf `--validate`。两方正式测量前，纯链短验证及原matmul/nbody stdout逐字匹配固定13答案。输入/回调对象计时外构造，复用实例；每轮reset与结果、类型、身份、回调次数检查计时外。

- `float-paired`：7场景，3进程×7样本、3次完整预热，两tier，进程顺序轮转。正常命中/首失配/末失配/混合均保留。
- `core-paired`：真实编译函数，不是原main冷墙钟。3进程×7样本、3次完整kernel预热，两tier，baseline/candidate轮转。每样本矩阵/天体输入重新建立且校验在计时外；规模不变。只重测两个编译体，CPython提供已保存逐字答案，不额外重复其时间。
- `float-resources`：独立进程3样本；资源harness在全部样本结束才统一JSON输出，减少序列化后台tier干扰。`core-paired`资源仍复用原M3.9独立harness，其样本间JSON与全进程JitInfo边界继续适用，不能称默认整个进程完全稳态。分配计数不与正式时间同时读取。
- `float-jit`/`numeric-jit`：另12进程actualasm，所有语义与回调检查通过，不混入正式时间。源码/命令/每进程stdout/stderr/JSON完整保存。

## 复现入口

加载toolchains/activate.sh并设既有XDG路径与固定13后，在全新/tmp保存CLI和同runtime，按原相对结构构建`float_chain.py`、`matmul.py`、`nbody.py`。`compare_float_chain.py baseline=<B/float-artifact> candidate=<C/float-artifact> --output <新目录>`执行纯链组。`run_numeric_kernels.py`参数见其argparse与`core-paired/results.json`，baseline/candidate目录需含`bench/<name>/<name>.dll`。资源与JIT命令分别为`float_resources.py B C`、`float_resources.py B C jit`、`collect_numeric_jit.py B C`。core/资源/JIT逐进程JSON保留实际命令；纯链命令由保存runner及comparison.json中的variant路径给出。

资源脚本的固定13短答案原路径为`/tmp/ryupy-m3_10-float-chain-perf-cpython.stdout`；内容随baseline归档，可复制回该路径或显式改为新路径（答案不得改）。core harness源码和csproj已附，runtime引用路径是其上级`runtime/RyuPy.Runtime.dll`。仓库不含CLI、DLL、bin、obj。
