# M3.9 第三有界实验：null压缩dirty状态，仍不批准广泛默认替换

## 源码完整性更正（2026-11-01）

本组 production.diff 是当时 `git diff` 输出，只包含已跟踪文件修改，遗漏当时新增且未跟踪的源码。它不是完整历史 null 候选补丁，不能单独从 e1c1ee1 重建。
原始diff保持不改；保存CLI/DLL的hash、CIL/MIR/asm及原始测量是该历史候选的产物证据，
不是完整源码可重建性的证明。下文构建命令记录当时执行过程，配对脚本可重跑保存的/tmp产物，
不保证仅凭此归档能重新编译同一历史候选。第一版与冷路版没有保存完整当时新增源码，
不能用今天的源码补齐并冒称历史版本。当前最终null实现将以最终提交SHA与显式实验flag重现，
或使用 ../current-source-reproduction 的明确时点源码补丁；两者不冒充当时测量二进制。


保存产物：B=/tmp/ryupy-m3_9-accumulator-baseline-e1c1ee1-B0aEmS，
C=/tmp/ryupy-m3_9-cold-candidate-X6CYIm，N=/tmp/ryupy-m3_9-null-candidate-NOm0xF。
本轮保同一Release runtime逐字节一致，无新C# ABI。构建当前CLI前，使用保存冷候选CLI为新增
accumulator_range生成冷对照，MIR证实仍有dirty bool；新CLI则是Eq(total,null)/total=null。
纯range无注解、动态参数，保存基线CIL含Create/Add/Value，三变体不是测了未优化函数。
已跟踪文件局部源码差异production.diff相对e1c1ee1（不是完整源码状态）；环境和原负载源码见../direct-first，冷实验见../direct-cold。
所有DLL/CLI只留/tmp，归档仅源码/文本/hash/原始数据。

## 正式短负载

仍保原dataclass三项与六种fallback分母，额外range单列，不删除不利场景。默认/禁tier两模式，
每变体3新进程，每进程3热身7样本；B/C/N、C/N/B、N/B/C轮转。全部做两组，先进程中位再跨进程
中位。所有结果、身份、副作用计数与短语义断言通过；函数体时间与process wall分开。

```sh
python3 compare-three.py 新目录 baseline=$B/artifact cold=$C/artifact null=$N/artifact
python3 compare-guards-three.py baseline=$B/guard-artifact cold=$C/guard-artifact null=$N/guard-artifact --output 新目录
python3 compare-range-three.py baseline=$B/range-artifact cold=$C/range-artifact null=$N/range-artifact --output 新目录
python3 diagnostics.py "$B" "$C" "$N"
```

所有脚本使用已激活.NET10环境，统计见summary.json，原始样本见六个*-triple*目录。null/B（正数更慢）：

| 场景/模式 | 第1组 | 第2组 |
|---|---:|---:|
| field_reads / 默认 | +25.5% | -2.0% |
| field_reads / 禁tier | +7.6% | +20.8% |
| int_fast / 默认 | +13.5% | +25.5% |
| int_fast / 禁tier | -18.4% | -53.2% |
| iadd / 默认 | +24.2% | +29.2% |
| overflow / 默认 | +17.1% | +152.2% |
| range_int / 默认 | +33.5% | +4.2% |
| range_int / 禁tier | +44.8% | +63.3% |

容器未固定频率/隔离CPU，尤其巨大单组比例不可视作稳定真实幅度；但不能因此丢掉不利数据，
也不能只据禁tier部分guard变快批准默认。纯range没有找出可精确限定的获益域。

## 额外独立充分预热的field_reads吞吐

旧分配harness虽有20次预热，但同时读分配/GCCollect，不将其诊断ns当正式稳态结果。此处新增
steady-harness/Program.cs：加载真实DLL、执行入口、取同一个实际Record对象，20×200000轮预热
后，每个进程7次调用field_reads(2000000, value)，校验14000000。计时仅Stopwatch/反射调用，
不读分配、不GCCollect、不开disasm；3进程三变体轮转，两tier分别记录，不混入短基准表。

```sh
dotnet build steady-harness/steady.csproj -c Release
python3 steady.py "$B" "$C" "$N"
```

csproj引用../artifact/RyuPy.Runtime.dll，运行脚本默认取N/steady-harness/bin/...；在保存目录可直接
复现，在新目录复现需按本结构放同hash runtime与产物。全部原始输出在steady/。

每200万轮跨进程中位：

| 模式 | 基线 | 冷候选 | null候选 |
|---|---:|---:|---:|
| 默认tier | 29.4086 ms | 28.7157 ms | 27.4946 ms |
| 禁tier | 26.3417 ms | 28.9898 ms | 29.2697 ms |

null/B默认-6.5%、禁tier+11.1%；冷/B禁tier+10.1%。充分预热后仍未证明跨模式无明显退化，
不能把旧短基准回归全部解释为OSR晋级中。

## 分配/汇编与结论

独立分配依旧B360/C160/N160 B每批5×200000轮，即候选净减40 B/调用，剩余含诊断固定开销。
各模式3进程全部5批一致，不宣称零分配。

真实FullOpts代码尺寸B/C/N：
- field_reads 2141/2392/2398 B；各自stack 248/248/248 B。
- accumulate 690/868/865 B；stack 72/56/56 B。
- sum_range 548/744/743 B；stack 24/24/24 B。

null替代bool几乎未改变实际代码体积/栈布局。纯range的RHS没有属性/索引/用户回调，但实际
循环仍有SetLine以及range索引装箱，不能称无任何runtime调用；null的sum_range整数槽
rbp-0x30仍跨SetLine调用写回，mode在r12d，rhs盒在rbp-0x38。全部分层版本原样保留。

结论：语义和去holder分配收益成立，三次有界结构实验未建立广泛默认替换的吞吐门槛；建议保
holder默认，实验代码是否保留由父里程碑决策。本报告不是全工程make check结果，未提交。
