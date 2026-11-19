# 精确列表布尔写入快循环性能验收

## 实现

本切片延续2026-11-15交接，优化原始sieve内层的动态计数器。MIR静态证明两块循环、
局部独立输入和临时值不逃逸，Runtime统一验证精确long、精确PyList及有效下标区间。
成功以原生long迭代，仅物化最终j；零轮和任何失败守卫返回原CFG。正溢出构造精确BigInteger。
数值背板保留旧路径；bool/object背板保持身份、别名与未写元素。详见ADR-0200。

## 方法

macOS arm64，SDK10.0.403；固定CPython3.12.13，直接脚本入口。旧版A使用交接冻结CLI
`/tmp/ryupy-sum-integer-cli-1k2l2b9e/ryupy`，产品对应692f2f17；新版B基于文档提交f36ddb80。
首次实际构建并执行验证旧CLI布局，未计算文件摘要。原始26项源码不改。
每项先构建两版，再交替CP/A/B与CP/B/A，1次预热，重点10次或全量5次，统计墙钟中位数及
各进程峰值RSS中位数。每次检查退出码与完整双流；主表不启用stats，独立轮读取分配与GC。
全部计时独占，不与构建或门禁并行；一项采样结束才构建下一项。

## 首轮重点结果

| 原始基准 | CP墙钟ms | A墙钟ms | B墙钟ms | CP/A/B峰值RSS MiB |
| --- | ---: | ---: | ---: | --- |
| sieve | 76.83 | 140.26 | 26.43 | 30.05 / 23.94 / 19.08 |
| matmul | 56.00 | 26.58 | 26.39 | 16.45 / 18.38 / 18.38 |
| hello | 8.51 | 4.89 | 4.85 | 14.69 / 13.58 / 13.57 |

sieve本切片约5.3倍加速，较同次CP约2.9倍；独立统计轮分配110905344→6571248字节，
减少104334096字节；GC0/1/2从12/1/0变为0/0/0。分配数据为实际观测，未使用写入次数乘对象大小估计。
matmul分配5987912字节持平。RSS与分配量分别记录，后者是累计值。

## 原始26项全量轮

1次预热、5次交替配对，全部退出/双流和两版搬迁通过。sieve再次测得
A141.23→B26.68ms（-81.11%），同次CP80.83ms；RSS为CP30.06/A23.94/B19.09MiB。
排除hello，各项中位数B/A几何均值0.93699。当前速度达CP13/26，RSS达CP4/26，
同时达标3/26（hello、fib、sieve）；逐项速度和内存总目标继续推进。

其余项目全量墙钟变化保留如下，正值表示新版更慢：

| 基准 | B/A耗时变化 | 基准 | B/A耗时变化 |
| --- | ---: | --- | ---: |
| hello | +0.33% | fib | -0.28% |
| mandelbrot | -2.31% | bigint | +0.11% |
| matmul | -1.17% | nbody | -0.38% |
| spectral_norm | +0.04% | calls | +1.08% |
| dynamic | -0.65% | attrs | +1.00% |
| oop | +1.71% | containers | +0.32% |
| dict_ops | -0.16% | list_ops | -1.27% |
| set_tuple | +3.83% | str_concat | +1.67% |
| strings | -0.73% | nqueens | -0.81% |
| sorting | +0.49% | exceptions | +2.57% |
| records | +0.14% | closures | -1.95% |
| comprehensions | +0.10% | decorators | +0.96% |
| generators | -0.16% | | |

set_tuple与exceptions追加一次1warm10配对：80.34→80.22ms（-0.15%）及
51.08→51.05ms（-0.05%），首轮较大回退未稳定复现。保留首轮所有不利值，其他项目的
单轮改善或退步仍按观测登记。

## 正确性与完整验收

完成6项MIR测试（含10种静态拒绝与4种错误ABI）、7项Runtime测试、CP/JIT完整双流，
真实NativeAOT删除源码及构建目录后空PATH、中文空格路径搬迁。正常协议比较/赋值/自增顺序、
零轮身份、正溢出、bool输入、负下标、异构列表及dict/用户对象回退均覆盖。

NumPy2.3.5版本断言、ndarray视图写入与numpy.int64计数器回退、普通list快路在同一应用中
通过CP/随包NativeAOT，删除输入发行、NumPy源载荷和构建后空PATH搬迁双流一致。
三类未捕获错误（越界、用户加法异常、未绑定局部）旧新AOT退出与完整双流完全相同；
CP的退出、stdout副作用与异常类型一致。列表越界赋值消息不同，越界/未绑定两个样例的原生
stderr缺回溯帧，已登记semantics §4.22；用户加法异常的帧名/行号和末行一致，路径与源码
echo/caret沿既有格式边界。错误测试按这些维度分别核对，保留完整CP/原生stderr。

完整make check首轮因CLI阶段诊断测试的固定pass顺序遗漏新pass而失败；补上该精确预期后重跑，
最终退出0：C#3167通过/2跳过，MIR377通过/4隔离，cases-selftest757通过，编译e2e475通过/286忽略。
fmt、严格Clippy及其余Rust套件通过。新增主e2e已转正并在完整门禁中通过。
本切片接受为默认实现；本轮平台为macOS arm64，Linux/Windows尚待本切片产品验收。

## 临时证据

`/tmp/ryupy-guarded-loop-focus.json`、`/tmp/ryupy-guarded-loop-full.json`及同前缀日志。
另外保留同前缀的`protection.json`、`errors.json`、`native.log`、`foreign.log`、
`make-check.log`；首轮门禁失败日志另存`make-check-first.log`。
机器结果不进入仓库；临时构建根由驱动清理。长期保留源码测试、原始基准与本文审定结论。
本轮冻结新版CLI为`/tmp/ryupy-guarded-list-loop-cli-frozen/ryupy`，恢复时先核对其实际运行。

## 后续

筛法本轮同时满足速度与RSS目标。其余项目继续按实际热点推进；generators、comprehensions、
records和list_ops仍有明显差距，先定位各自的调度/分配成本，再选择下一条闭合竖切。
上一轮交接中的CPython宿主和onefile候选仍按各自方法单独测量。
