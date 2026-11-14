# 生成器只读形参挂起写回消除

## 实现与证明

仅非trace product、无EH、Dynamic且非cell的形参准入。排除env/origin/module_state及合成隐藏槽。
完整穷尽Statement/Terminator定义，Assign目标和Suspend.input恢复定义均排除。
工厂已将形参写入frame；绑定不变时挂起省去冗余ArraySet，恢复ArrayGet继续读取。
非参数局部首次初始化、共享cell、状态/异常槽及Finish清帧保持。可变对象的内容变化仍由同一引用可见。
实现没有新Runtime ABI/环境开关或运行时分配。旧活跃性实验已存远端WIP并撤出当前树，
本实现不依赖该框架。决定见[ADR-0198](adr/0198-generator-readonly-param-spills.md)。

## 正确性

9项定向MIR测试，覆盖只读n实际省写回、恢复读取和i写回保留、重赋值、send、typed/cell、
真实nonlocal捕获、EH/product及隐藏参数拒绝。全MIR371通过/4忽略，严格Clippy通过。
主用例经fixed CPython3.12.13/JIT/NativeAOT、源码与构建输入隐藏、空PATH中文搬迁双流一致后转正。
首次报告把runner的1 passed误读为通过；results.jsonl实际pending，已纠正并保留观察反例。
主用例显式close，直接覆盖send覆写形参和nonlocal修改形参。

未捕获普通异常：旧/新AOT完整双流/退出相同，CP帧行号及函数一致；仅路径及源码echo/caret按约定分列。
throw注入：旧/新AOT也完全相同，但CP生成器帧line2而RyuPy为line0，既有差异见semantics §4.20。
生成器locals及隐式销毁时机反例保留observation pending；没有据此扩展反射或refcount承诺。

## 性能与取舍

M5/macOS arm64、SDK10.0.403，原始26项源未改；A为c36be201布尔存储基线，B为本次独立实现。
直接CP脚本、1warm5交替、无插桩墙钟/RSS；统计独立轮；26项退出码/双流及隐藏原发布路径搬迁全部通过。

全量轮generators335.61→336.79ms（+0.35%）；分配两臂均169254880字节，RSS22.02→22.05MiB。
排除hello，各项中位数B/A几何平均1.00102，整体基本持平；保留closures+2.62%、
comprehensions+2.69%、calls+1.76%等不利值，没有证据将它们全部归为调度或证明无系统性回退。

后续同次1warm10重点配对：

| 原始基准 | A墙钟ms | B墙钟ms | A/B独立轮main_ms |
| --- | ---: | ---: | ---: |
| generators | 335.96 | 331.32 | 323.878 / 318.583 |
| hello | 4.77 | 4.76 | 0.019 / 0.024 |
| comprehensions | 114.81 | 114.36 | 110.416 / 109.677 |
| closures | 63.61 | 64.85 | 56.660 / 55.119 |

生成器中位数改善1.38%，样本区间重叠，收益较小；分配均169254896字节，GC20/1/0，RSS基本持平。
closures墙钟仍增加1.96%，不能用单次main统计更低来代替墙钟结论。
原早期6项1warm10也观测到generators约1.7%改善，但最终准入依据保留全量与重点两组结果。
接受这一受限、语义冗余写入消除作为默认，后续按用户速度优先继续降低主要算术/迭代成本。
当前CP/B中位数几何加速约1.1516；速度12/26、RSS4/26、同时2/26，目标尚未全面达成。

报告表格曾错误转录多项alloc/main_ms，原始JSON与工具日志已由主代理重算；
例如fib分配实际915592字节，两臂一致；nbody实际416482168字节，两臂一致。
不沿用错误汇总；本轮26项二进制尺寸两臂相等，实际分配差仅少量8–112字节元数据波动。

## 完整门禁

主代理核对原始make check日志：C#3149通过/2跳过，MIR371通过/4忽略，
cases755通过，编译e2e473通过/286忽略；fmt、workspace严格Clippy及其他Rust测试通过，退出0。
固定CP、空RUSTC_WRAPPER、项目target、仓库外TMPDIR与单测试线程。
