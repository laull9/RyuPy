# M3.10 NumericList 首轮：三臂收益/普通路径成本隔离

2026-11-01；一次预定两tier×3进程×7样本，未追加救测。全部语义/资源12进程/时间36进程/JIT12进程通过，但**不建议当前实现默认启用**：实际matmul及普通ref路径显著变慢，分配仅少8.28%，不能盖过时间回退。

## 来源与三臂

基点9023f21，加各自完整source.patch（tracked与untracked crates/runtime，不含.pending）。基线CLI与已验证FloatLoop候选完全相同，源patch复用该候选；baseline/capture-working-tree.patch只标记保存时工作树，已含可能新编辑，不能当旧CLI来源。

- A：旧FloatLoop compiled+旧runtime 0ccd1c66…。
- B：与A逐字相同compiled+新runtime bc8d9fff…。
- C：NumericList compiled+同B runtime。

每臂独立目录、进程、匹配runtime的诊断harness。binary-hashes.json核验程序与harness DLL选择。二进制仅/tmp，归档无DLL/CLI/bin/obj。
保存基线/tmp/ryupy-m3_10-numeric-list-baseline-4y3zve，候选/tmp/ryupy-m3_10-numeric-list-candidate-tWv1iY。

## 有界负载与校验

原tests/benchmarks/matmul.py完全未改：100×100连续三次，首次矩阵输入创建计时外，每次输出用作下次输入。实际compiled函数反射顶层调用，不是C#重写kernel。输入重建/最终checksum校验计时外；原main也执行并核对固定13已保存stdout，但main不混入kernel时间。

numeric_list.py的30000轮微负载包括普通ref构造、append、read，fresh输出列表生产消费float，以及bool/custom/mixed失败。生产者两行、消费者一行，两个loop不同total/consumed槽；乘法叶全来自GetItem、fresh append sink。此形状是有意的候选命中正例，不估算全项目命中率。所有引用结果逐元素is校验、自定义回调次数校验计时外。bool/custom为算术慢路，结果仍float，不能冒称16B存储降级。

固定13--validate与--diagnostic通过，A/B/C完整7场景及原matmul均通过。为独立资源诊断增加reset函数后，微负载重新编译并重做完整语义，计时才开始；最终源码已归档。
候选MIR micro有2 Create/2 Append/4 Read/4 CanRead；matmul五ABI全齐（加PeekList）。原程序输出/身份不变。

## 时间（ms；每进程7样本中位数后跨3进程取中位数）

| 场景 | default A/B/C | disabled A/B/C |
|---|---|---|
| matmul |123.916 /172.332 /222.232|175.663 /403.174 /309.148|
| ref construct |0.492 /0.634 /0.604|0.489 /0.516 /0.623|
| ref append |4.853 /7.133 /5.986|1.663 /2.112 /1.996|
| ref read |2.135 /3.415 /3.223|1.091 /1.777 /1.738|
| float |6.017 /13.720 /6.881|2.812 /4.846 /3.893|
| bool |13.318 /13.784 /18.575|6.026 /8.779 /8.960|
| custom |28.354 /23.300 /32.438|11.079 /13.997 /14.029|
| mixed |9.779 /6.825 /8.538|7.224 /10.351 /9.002|

matmul B/A default+39.1%、disabled+129.5%；C/A+79.3%/+76.0%。普通ref read B/A+60.0%/+62.9%，append+47.0%/+27.0%。C在disabled比B快23.3%仍比A慢76%，不能只展示C/B。
三个进程轮转ABC/BCA/CAB；全部raw与每进程中位数在summary.json，无删样本。固定3次预热不保证所有默认PGO稳定；资源JitInfo显示后期仍有后台编译，未重跑或挑tier。没有整26suite或长期吞吐结论。

## 独立资源

每tier每臂独立进程3预热+3资源样本，JSON放micro全部资源样本之后；core旧harness样本间JSON的后台tier局限保留。time进程不读取资源counter/不打开disasm。

- matmul A/B 1,391,448 B，C 1,276,248 B（少115,200 B，8.28%）。
- ref construct 480,288 B；append 524,648 B，三臂相同。
- micro float A/B1,428,224 B，C1,428,240 B（+16）。
- bool/custom/mixed C各+64 B；16B槽容量与少量输出的成本保留。
- read default C+24 B，disabled三臂同1,427,776 B；不猜这常数来源。
- 下标盒/普通列表协议仍有成本，不能称整个loop无分配。

## 实际JIT

禁tier matmul A/B原始全文SHA相同：924d6d69de825beac1c502b21da158b7952af250310ce87be4d47eb6d8322465，均2879B。故该caller的B/A时间回退不是编译体算法或caller机器码变化。
C为3059B。热路径仍有五个未内联调用CanReadF64→PeekList→CanReadF64→ReadF64→ReadF64，然后typed乘加；旧路径三个GetItem仍在失败边。不能把它称为纯raw直CIL容器读取。无FMA。
源码只读定位：普通GetItem经Normalize/Count与ReadSlot各自storage判型；普通Items/Append也经storage分支。此轮未单独捕获这些runtime callee机器码，不把源码推测冒称callee JIT实证。各default Tier0/OSR/Tier1全文保留，不挑最优版本。

## 复现

已编产物目录布局A/B/C，每臂micro/numeric_list.dll与bench/matmul/matmul.dll；micro-harness及core-harness各自Reference ../runtime/RyuPy.Runtime.dll。source patch含生产，不含tests/perf，所以使用本归档numeric_list.py（含reset）及仓库原matmul.py。

运行 `python run_three_arms.py ROOT resources`、`time`、`jit`，各输出目录必须不存在；每阶段完成才启动下一阶段。正式执行时所有其它worker停止构建/运行。summarize.py聚合、jit-summary.json保存方法版本与大小，binary-hashes.json明确各DLL版本。

18:07:40 UTC最后JIT进程终态，已通知父窗口释放；后续仅文本分析/归档，是否进行唯一有据修正由父决定，不自行继续救测。
