# M3.9 第二有界实验：冷 fallback，仍不批准默认切换

## 源码完整性更正（2026-11-01）

本组 production.diff 是当时 `git diff` 输出，只包含已跟踪文件修改，遗漏当时新增且未跟踪的源码。它不是完整历史冷路候选补丁，不能单独从 e1c1ee1 重建。
原始diff保持不改；保存CLI/DLL的hash、CIL/MIR/asm及原始测量是该历史候选的产物证据，
不是完整源码可重建性的证明。下文构建命令记录当时执行过程，配对脚本可重跑保存的/tmp产物，
不保证仅凭此归档能重新编译同一历史候选。第一版与冷路版没有保存完整当时新增源码，
不能用今天的源码补齐并冒称历史版本。当前最终null实现将以最终提交SHA与显式实验flag重现，
或使用 ../current-source-reproduction 的明确时点源码补丁；两者不冒充当时测量二进制。


本轮只将累加器fallback从PyPic.Binary改既有PyOps.Binary；运行时完全相同，无新增C# ABI。
已跟踪生产文件差异见production.diff（相对e1c1ee1，不含当时未跟踪源码）。所有负载源码与环境、基线及首候选
证据在 ../direct-first；本轮独立保存原始结果，不覆盖首轮失败证据。

二进制只在/tmp：
- B：/tmp/ryupy-m3_9-accumulator-baseline-e1c1ee1-B0aEmS
- F：/tmp/ryupy-m3_9-direct-candidate-IA30py
- C：/tmp/ryupy-m3_9-cold-candidate-X6CYIm

各自artifact为dataclass_hot、guard-artifact为accumulator_guards。runtime逐字节一致；新候选hash
在artifacts-sha256.json。构建：cargo build -p ryupy-cli，再CLI build两个原负载至新目录，dump
CIL/MIR。guard --validate与保存CPython3.12.13输出逐字节相同；CIL的fallback确实PyOps.Binary，
无NumericAccumulator。工程完整门禁不由本报告宣称，本报告仅专项语义/测量证据。

## 三变体配对

默认/禁tier每变体3新进程、每进程3热身7测量。为不把首候选始终固定居中，顺序轮转：
B/F/C、F/C/B、C/B/F。所有变体同轮交错。与首报告仅两变体反转的顺序口径区别明确记录。
完整三变体跑两组，全部保留；每进程中位后再跨进程中位。函数体秒数与进程wall分开。

在已激活.NET10工具链的环境重现（所有输出目录必须不存在）：
```sh
python3 compare-three.py 新输出目录 baseline=$B/artifact first=$F/artifact cold=$C/artifact
python3 compare-guards-three.py baseline=$B/guard-artifact first=$F/guard-artifact cold=$C/guard-artifact --output 新输出目录
python3 diagnostics.py "$B" "$F" "$C"
```

diagnostics.py加载首候选目录已构建harness，引用的runtime与三个产物相同。其源码和csproj见
../direct-first/candidate/harness。诊断与正式计时严格分离；无DLL、CLI、bin、obj进入归档。

## 结果与默认判定

summary.json及四个*-triple*目录保存全部数值、样本、输出。冷候选相对B（正数=更慢）：

| 负载/模式 | 第1组 | 第2组 |
|---|---:|---:|
| int_fast / 默认 | -36.8% | -25.9% |
| int_fast / 禁tier | +14.7% | +34.1% |
| iadd / 禁tier | +27.8% | +10.2% |
| field_reads / 禁tier | +28.7% | +19.7% |
| field_reads / 默认 | +12.5% | +4.5% |
| overflow / 默认 | +4.3% | +11.3% |

bool/float/radd本轮多数变快；默认iadd一组+2.9%、另一组-17.3%。equality对照与第一候选也存在
较大波动，非隔离容器、未固定频率不能作精确微架构归因。即便如此，禁tier int/iadd/field_reads
连续两组退化，不能用默认int收益抵消后宣布门槛通过。建议保旧默认；若做下一实验必须有界且
继续保留反例，不能无限调参或挑组。

## 分配与实际JIT

三变体同harness，默认/禁tier各3新进程、每进程5批、每批5×200000轮：全部批次B360/F160/C160 B。
相对B净减40 B/函数调用；不是每轮减40 B，剩余含反射/Stopwatch/返回盒，不能宣称零分配。

真实FullOpts：
- accumulate代码 B690/F1488/C868 B，栈保留空间 B72/F152/C56 B。
- field_reads代码 B2141/F3083/C2392 B，栈保留空间 B248/F344/C248 B。
- C accumulate默认实际Tier1-OSR产生920与888 B两个版本，全部原样保留。

C的PyOps.Binary优化后只余直接InPlaceBinary调用，F内联的string/float/BigInteger整片分支消失，
证明确实达到第二实验隔离目的，无需凭猜测再加NoInlining ABI。但C仍在IG06/IG13/IG16维护
integer/mode/dirty及rhs栈槽，合流写回、跨GetItem/SetLine调用spill；B则holder字段承载状态。
这解释了可继续检查的布局差异，不是对时间回归的硬件计数器证明。去holder与缩代码确实成功，
当前吞吐门槛仍未成功。
