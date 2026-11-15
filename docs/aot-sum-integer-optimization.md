# sum精确整数原生累计

## 实现与合同

`BuiltinSum`委托到独立partial helper，在既有流式迭代中维护原生long、原对象引用和dirty状态。
仅精确CLR long相加进入快路；bool、BigInteger、float及foreign对象沿普通Binary(Add)。
同号异号位公式检测正负溢出，回到既有任意精度加法；结果归一化回long后可重新进入快路。
发生实际原生加法后才延迟装箱，在用户协议或结束前物化准确左值。
空显式start、用户回调返回的shared long且其后没有快路加法时，返回原对象引用。
默认空sum改用规范小整数0，`sum([]) is zero`现与固定CPython一致。
每轮MoveNext/Current各一次，保留迭代与加法交错、活列表变更、异常早停及__add__/__radd__，不调用__iadd__。
无新public ABI、RuntimeFn、环境开关、holder或unsafe模型。

## 正确性与门禁

旧9项流式测试和新增11项整数测试共20项通过，包括正负溢出回落、dirty后回调身份和异常早停。
10,000个预装箱long的连续累计分配通过<2048字节上界断言；小整数命中缓存，不能用项数乘24当作实际分配。
主e2e由pending开始，核实results.jsonl的unexpected-pass后转正，非ignored再次passed。
固定CPython3.12.13/JIT/NativeAOT、源与构建隐藏、中文空格路径及空PATH搬迁双流与退出一致。
NumPy2.3.5 int64/float64在原生整数段后的foreign回退、空start及随包搬迁验证通过。
未捕获__radd__与生成器迭代异常两类中，旧/新AOT完整stderr及退出相同；
CPython的帧名/行号/异常一致，路径与源码echo/caret沿既有输出边界分列。

最终完整make check退出0：C#3160通过/2跳过，MIR371通过/4隔离，
cases-selftest756通过，编译e2e474通过/286忽略；fmt与严格Clippy通过。
本轮在macOS arm64验收，Linux/Windows尚未运行本切片。
两项既有参数偏差继续见semantics §4.15；本次没有新增九项参数偏差。

## AOT配对实测

M5/macOS arm64，SDK10.0.403；A为0c162eb2只读生成器基线，B为本切片。
原始26项源码，1warm5交替，主表无stats直接CP脚本/原生入口，RSS按进程采集；统计独立轮。
全部26项双流/退出及旧新产物隐藏原路径后的搬迁通过。

| 全量轮重点 | A墙钟ms | B墙钟ms | 变化 | A/B分配字节 |
| --- | ---: | ---: | ---: | ---: |
| hello | 4.94 | 5.05 | +2.18% | 913328 / 913328 |
| matmul | 26.05 | 25.87 | -0.69% | 5979896 / 5979896 |
| sieve | 139.58 | 139.83 | +0.18% | 110897312 / 110897312 |
| sorting | 127.04 | 129.80 | +2.18% | 65167192 / 65167168 |
| closures | 64.33 | 65.37 | +1.62% | 107106856 / 107106856 |
| comprehensions | 115.07 | 115.51 | +0.38% | 125611128 / 125611128 |
| generators | 343.71 | 312.17 | -9.18% | 169254912 / 145255296 |

排除hello，各项中位数B/A几何均值0.99800，逐轮配对比值几何均值0.99819；
整体耗时约减少0.2%。候选速度达CP为12/26，RSS为4/26，同时达标2/26；
B中位数不高于A为9/26，不能将人为2%窗口称为严格不回退。

后续1warm10独立配对：

| 重点 | A墙钟ms | B墙钟ms | 变化 |
| --- | ---: | ---: | ---: |
| generators | 333.08 | 306.97 | -7.84% |
| comprehensions | 113.02 | 113.71 | +0.61% |
| hello | 5.92 | 5.61 | -5.22% |
| sieve | 146.62 | 146.45 | -0.12% |
| matmul | 28.01 | 27.85 | -0.57% |

生成器重点A样本327.63–367.80ms，B为304.11–318.45ms，两个区间分离；
独立轮分配169254912→145255296字节，减少23999616字节（22.89MiB，14.18%），
GC20/1/0→17/1/0。RSS约22MiB，基本持平，仍高于同次CP约14.8MiB。
comprehensions重点仍增加0.61%，sorting全量增加2.18%，后者没有包含在代理重点5项里。
24字节alloc差异不足以解释墙钟，单次main_ms也不能替代进程墙钟结论。
主代理追加1warm10：sorting147.05→158.06ms（+7.48%），closures64.44→65.04ms（+0.94%）。
该sorting轮CP区间134.38–198.92ms、A128.78–181.86、B126.39–210.02，波动很大，不能归因于sum。
再独立1warm10为sorting127.48→127.80ms（+0.25%），A125.33–137.04、B125.22–138.72；
没有稳定复现>5%回退。保留三组不利值，不声称零回退。
接受这一整数连续累计默认实现：生成器多组收益及23,999,616字节分配下降复现，
全量整体接近持平，保护项波动与局部不利结果明确登记。

## 证据与后续

原始26项、重点10轮、异常及完整门禁日志保留仓库外，仓库仅登记源码和本次判定：
`/tmp/ryupy-sum-integer-full-ab.json`、`/tmp/ryupy-sum-integer-focus10.json`、
`/tmp/ryupy-sum-integer-exceptions.json`、`/tmp/ryupy-sum-integer-make-check.log`。
追加原始证据为`/tmp/ryupy-sum-integer-protection10.json`和
`/tmp/ryupy-sum-integer-sorting-confirm10.json`。
后续继续优化原生迭代、动态计数器及容器分配；全面达到CPython逐项速度/内存目标仍有差距。
