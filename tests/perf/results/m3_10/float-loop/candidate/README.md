# M3.10 FloatLoop：short-chain 基线上的单轮配对

## 结果（不保证所有路径更快）

本轮只比较已测短F64链基线与新增FloatLoop，不将两轮不同测量的百分比相乘冒称对原e1整体收益。`summary.md/json`保留全部绝对时间和资源数字；一次预设两tier、3进程×7样本，未追加统计轮次或删除不利样本。

- 原matmul kernel仍是100×100连续3次乘法：default120.087→100.609ms（-16.2%），禁tier179.759→143.353（-20.3%）；分配72,671,472→1,391,448B，约减少71.28MB/98.1%。禁tier分配72,671,448→1,391,448，吻合每输出只保最后盒而不是每次内层迭代装箱。
- 30000轮微负载纯float：1,433,920→713,944B，减少719,976B；动态getitem下标盒仍在，不冒称整个循环零分配。default时间仅-0.6%，禁tier-15.7%，不以分配下降自动宣称等比例提速。
- 多次外层重入reentry：分配1,422,456→702,528B，default-5.6%、禁tier-30.1%。空内层/前一行shared值不会污染下一行，返回别名按check验证。
- 最后shared回调的结果身份保留，分配1,434,040→714,088B。首/中/末custom与shared-float均核协议次数、传入prefix值、返回身份。
- **不利时间保留**：禁tier custom_middle3.952→4.883ms（+23.6%），mixed_restore7.778→9.172（+17.9%）。default这两项分别-19.4%/-2.2%。mixed_restore分配6,449,552→6,449,760B（+208B），原样记录，未凭猜测改为相等。
- empty为零轮，资源两方88B，时间亚微秒；只作身份/开销对照，不拿+14.2%等极小分母比例决定性能政策。bool/首custom资源相同，不声称所有慢路无代价。

## 真实JIT与代码体积

独立采集loop四进程、matmul四进程，所有语义检查通过。`jit-summary.json`、原始asm保存完整版本和hash，未混入正式计时。

- matmul禁tier候选`G_M000_IG37`为`vmulsd`、`vaddsd [rbp-0x78]`、`vmovsd`回typed栈槽再跳回循环。该算术快边没有NEWSFAST。accumulate对应IG19使用`[rbp-0x48]`。
- 这证明raw f64跨迭代，不证明全程驻留寄存器：跨GetItem调用仍有栈spill；慢边仍保物化和动态协议，整个方法仍能包含cold allocation调用。
- 总机器码更大：禁tier accumulate1958→2228B、repeated2385→2681B、matmul2569→2879B。默认有多种Tier0/OSR/Tier1版本，不能只挑一种说全代码更短。采样未见FMA指令。
- 固定预热kernel不是“整个进程保证不再晋级”。loop资源harness统一在所有采样结束后JSON输出；matmul沿用独立M3.9 kernel harness，样本间JSON与全进程JitInfo边界仍须保留。

## 对照、身份与规模

`float_loop.py`统一使用外层range(1)→内层range，避开旧entry-only整数holder；`repeated(rows)`多次进入内层preheader。输入/复用实例计时外建立、reset与check计时外；没有按基准函数名特判生产优化。

11场景包括double、zero、bool、reentry、custom首中末、shared-float首中末、persistent custom经Release返回shared float后恢复数值。`--validate`用24元素，`--diagnostic`保完整30000元素但不计时；两种固定CPython3.12.13基线与两方compiled输出都通过。资源/JIT进程继续调用同编译函数与原check，核真实结果/身份/副作用而非只打印假定常量。

matmul输入每样本重建计时外，加载原main输出和每次checksum核对固定13，仍为原100×100×3；不是原main冷墙钟表。Nbody本切未命中，只跑原输出语义对照，不重跑80进程：两方完整MIR/CIL raw hash均相同，见`nbody-control.json`（CIL bc15ace8…）。

## 来源与统一runtime

- 基线CLI：2b64e382…，即已测short-chain Release CLI，保存时与target/release完全同hash。基线来源是9023f21加`short-chain-source.patch`，**不是**从正在编辑的FloatLoop树重建，也不冒称仅9023f21就含short-chain。
- 候选：新冻结Release CLI，`source.patch`含tracked与未跟踪crates/runtime源码，以base-commit为基点。
- 本轮两方统一当前Release runtime **0ccd1c…**。前轮保存d9678b…与当前DLL字节不同，保存时runtime git diff/status为空；可能构建元数据变化，但不假定原因，更不混用两版本。baseline的初始`hashes.txt`保留发现差异时的快照，`provenance.json`和candidate的`binary-hashes.txt`标明正式选用版本。旧runtime二进制只留/tmp。
- 临时目录：`/tmp/ryupy-m3_10-loop-baseline-d5jFe4`与`/tmp/ryupy-m3_10-loop-candidate-rMw9l4`。二进制不入仓。

## 复现

在全新目录保存对应CLI与同runtime，编译float_loop.py与原matmul.py；基线/候选目录均需`loop-artifact/float_loop.dll`与`bench/matmul/matmul.dll`。`compare_float_loop.py baseline=B/loop-artifact candidate=C/loop-artifact --output NEW`执行11场景。`run_matmul_kernel.py`按保存argparse与core-paired/results.json参数执行；只测这一个core，不附带Nbody时间。资源为`loop_resources.py B C`，实际JIT为同命令加`jit`及`collect_matmul_jit.py B C`。helper源码与csproj已附，runtime放其上级runtime目录。原始每进程stdout/stderr/JSON和CLI产物hash、完整MIR/CIL均保留。
