# M3.10 最后bounds证明专项：同runtime、同一matmul

本切完成原验收的支配范围检查消除，不是第三次NumericList布局调优。只把必须证明同list/index守卫真边且无中间effects的ReadF64改ReadCheckedF64；普通runtime入口/布局/算法不变。

## 来源与口径

before /tmp/ryupy-m3_10-bounds-baseline-Q6n58h，after /tmp/ryupy-m3_10-bounds-candidate-rNMtDu。旧matmul体准确复用已验证numeric-list-revised/C；SLP旧新matmulMIR/CIL逐字相同，不受关闭SLP政策影响。
保存before CLI时生产编辑已经开始，所以baseline/capture-working-tree.patch明确不是旧CLI的精确复建来源；baseline/numeric-body-source.patch是旧matmul体的完整来源。候选source.patch完整含tracked+untracked crates/runtime，基点9023f21。全部hash在binary-hashes.json，二进制只留/tmp。

旧体与新体、同一个core harness全部统一新runtime，排除普通runtime/layout变化；旧体hash与保存产物逐字一致。只编译候选matmul，没有重新编造before实现。

## 一次有界配对

原100×100连续3次矩阵乘法真实compiled函数；每个样本输入重建外，结果作为下一次输入；最终固定13 checksum校验外。两tier、每方3进程×3样本、每进程3预热，轮转before/after。共12时间进程；另4资源进程各3资源样本，另4actualJIT进程。全部20进程exit0、stdout一致。
无新micro、无Nbody重测、无第三列表retry，无额外CP运行；固定13原期望复用。固定3预热不保证后台tier完全稳定，core旧harness样本间JSON局限保留，不把本次小样本外推全套。

## 时间与资源

每进程3点中位数后跨3进程中位数：

|tier|before ms|after ms|变化|
|---|---:|---:|---:|
|default|73.4273|60.9257|-17.03%|
|disabled|75.9249|69.9619|-7.85%|

两方每个资源样本都1,278,672B，分配不变。全部raw含GC/JIT/rss/wall；资源与时间/asm进程分离，不说bounds消除减少了装箱或分配。

## 实际机器码：只删除重复逻辑范围比较

禁tierbefore matmul4563B，after4693B（+130B，非代码体积减少）。新旧实际helper都inline。

- `candidate/jit/disabled-before.asm` G_M000_IG62：`cmp r9, rdx; jae G_M000_IG68`，这是第二次long normalized/count比较；IG68调用ReadError(2)。另一lane同类路径也存在。
- `candidate/jit/disabled-after.asm` 对应读取段IG73/IG81不再有该long normalized<count比较；保留`cmp edi,[list+0x10]`与`cmp edi,[array+0x08]`，分别是List/托管数组检查。
- 成功guard之前的范围检查仍在；两版均有CORINFO_HELP_RNGCHKFAIL，after保CORINFO_HELP_UNBOX的CLR类型安全路径。
- before全部4个ReadError调用（类型/范围），after读取不再调用ReadError；这并不等于整个循环无检查或运行时不再安全。

默认Tier0/OSR/Tier1全版本保留，不选取最优版本；jit-summary.json含每method hash/字节/检查helper证据。

## 复现与终态

run_bounds_pair.py显式`--samples 3 --processes 3 --warmups 3 --resource-processes 1 --resource-samples 3`，before/after目录均用同新runtime。collect_bounds_jit.py独立4进程，不能把其资源运行混进时间表。
18:58:28 UTC最后JIT终态后通知父释放窗口；随后只读归档。104整26尚未启动，等最终gate与父独占GO，最终默认不带任何SLP实验/诊断flag。
