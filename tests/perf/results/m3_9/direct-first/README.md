# M3.9 首次直接累加器候选：保留回归，不批准默认切换

## 源码完整性更正（2026-11-01）

本组 candidate/production.diff 是当时 `git diff` 输出，只包含已跟踪文件修改，遗漏当时新增且未跟踪的源码。它不是完整历史候选补丁，不能单独从 e1c1ee1 重建。
原始diff保持不改；保存CLI/DLL的hash、CIL/MIR/asm及原始测量是该历史候选的产物证据，
不是完整源码可重建性的证明。下文构建命令记录当时执行过程，配对脚本可重跑保存的/tmp产物，
不保证仅凭此归档能重新编译同一历史候选。第一版与冷路版没有保存完整当时新增源码，
不能用今天的源码补齐并冒称历史版本。当前最终null实现将以最终提交SHA与显式实验flag重现，
或使用 ../current-source-reproduction 的明确时点源码补丁；两者不冒充当时测量二进制。


基线生产提交 e1c1ee101fb8480e1b375b62a0f056978cfca8aa；候选当时基于同提交；其已跟踪文件差异见
candidate/production.diff（不完整，见上方更正）。原始二进制只保存在 /tmp，不纳入版本库：

- 基线 `/tmp/ryupy-m3_9-accumulator-baseline-e1c1ee1-B0aEmS`
- 候选 `/tmp/ryupy-m3_9-direct-candidate-IA30py`

基线与候选 Release runtime 字节一致，artifacts-sha256.json 可复核。候选执行
`cargo build -p ryupy-cli` 后保存CLI，分别 `ryupy build tests/perf/dataclass_hot.py -o artifact`
及 `ryupy build tests/perf/accumulator_guards.py -o guard-artifact`；产物目录相对各自根目录。
对应 `ryupy dump <源文件> --emit cil/mir` 保存 IR。候选 CIL 无 NumericAccumulator，实际
IsInstance(long)、UnboxIntoValue(long)、Add 和溢出守卫已可见，不能只据此判性能胜利。

## 测量与复现

先 source `/workspace/shared/toolchains/activate.sh`。每组各3个新进程，每进程3轮热身7轮测量，
基线/候选交错先后；先每进程中位，再取3进程中位。默认tier清除外部Jit/Tiered环境，禁tier仅
设置 DOTNET_TieredCompilation=0。两组完整测量均保留，不挑最佳组。函数体时间与process wall分开。

```sh
python3 baseline/compare.py 新输出目录 baseline=基线artifact candidate=候选artifact
python3 baseline/compare_accumulator_guards.py baseline=基线guard-artifact candidate=候选guard-artifact --output 新输出目录
```

所有语义值、动态方法调用计数和iadd身份均验证。guard负载六场景使用同一 m0_accumulate，
严格range索引循环确保基线与候选都匹配累加器。列表/实例预建且计时外重置计数。按输入和MIR
路径推导int_fast是100%快路，其他五项每轮fallback；尤其overflow交替的下一轮是BigInteger左值，
不是50%快路。这些比例不是插桩实测计数器。索引读取/装箱公共成本仍在，不是纯加法微基准。

summary.json为所有场景两轮变化，正数表示候选更慢。主要证据：

- 默认tier int_fast两轮约快21%，float两模式快50–66%。
- 禁tier bool两轮慢33.1%与16.0%，int慢19.5%与3.9%。
- 默认iadd首轮慢85%但第二轮快9.9%，不能宣称该幅度稳定；默认radd慢13.6%与5.4%。
- field_reads默认两轮+3.5%/-14.5%，禁tier-0.5%/+32.1%；equality等对照也大幅波动。
  此容器未隔离CPU/固定频率，不能从噪声中挑某一轮宣称整体吞吐收益。

结论：语义通过，去holder有确切分配收益；当前尚不满足“频繁fallback无明显恶化”的默认开启条件。

## 分配与实际机器码

分配程序为candidate/harness/Program.cs，引用相同runtime，加载各自产物/执行模块入口后使用实际
同一Record对象；20次额外热身，每进程5批、每批5×200000轮。两模式各3进程全部批次：
基线360 B，候选160 B，净减200 B/5调用，即40 B/调用。剩余包括诊断Stopwatch、反射与返回值盒，
不宣称零分配，也不把原360 B全当生产holder成本。分配诊断ns不是正式时间表。

JIT使用 DOTNET_JitDisasm=*field_reads* 或 *accumulate*，JitDisasmDiffable=1 与独立
JitStdOutFile。field_reads通过相同harness触发，accumulate通过完整guard负载触发。采集和正式
计时分离。每个汇编文件保留全部分层版本，不能仅挑一个Tier0版本比较。

- field_reads：FullOpts 2141→3083 B，Tier1-OSR 2981→3802 B。
- accumulate：FullOpts 690→1488 B，栈保留空间72→152 B；旧Tier1-OSR 910 B，候选实际
  产生1665/2057 B两个OSR版本。公开helper原已在优化JIT中内联，不可称首次消灭逐轮调用。
- `candidate/jit-accumulate-disabled-candidate.asm`：IG19–25及IG36–53展开string、float、
  BigInteger等DynBinary分支；旧版冷路直接InPlaceBinary再Store。候选mode、rhs、dirty及integer
  分别可见rbp-0x34/-0xB0/-0x38/-0x30栈槽，快路跨GetItem/SetLine调用spill/reload。
  这是实际布局证据，但没有硬件计数器，不能把全部时间变化定量归因于某个分支。

下一有界实验应检验不内联冷fallback是否减轻代码体积/活跃值压力，仍需相同成对验收；本次没有
改变生产实现、没有提交。归档仅源码、命令、环境、hash、原始文本/JSON/CIL/MIR/asm，无DLL/CLI/bin/obj。
