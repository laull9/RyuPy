# AOT三项热点分派优化验收

## 选择依据

本轮基于767e665c，按用户要求继续处理高差距项目。原始records、comprehensions、list_ops、
generators分别构建真实NativeAOT，先核对CPython退出/双流，再各运行3次独立sample诊断。
使用同次构建dSYM、各进程加载地址和atos还原符号；命中元组键相等比较、集合方法通用分派、
生成器的整数比较与恢复链。短进程样本有限，只据此确定候选，不推断精确耗时占比。

采用三个无新public ABI、缓存或CLI开关的运行时切片，合同见ADR-0201：

- 精确tuple直接数组比较，元素继续走完整身份/相等协议；修复不同长度时共同前缀比较副作用。
- 小参数PIC在普通实例原热路之后，为sealed list/dict/set的append/pop/get/add省去通用分派。
  星号标记、关键字、错误数量、用户派生布局与显式PIC关闭均沿原路径。
- 两侧精确long的CompareValue直接返回规范bool，混合数值、foreign和用户比较结果保持原协议。

## 测量方法

macOS arm64、SDK10.0.403、固定CPython3.12.13。原始26项源码与直接脚本入口保持不变。
每片独立冻结前后CLI；构建和计时串行，1次预热、7次交替CP/A/B及CP/B/A，报告中位数。
退出码和完整双流先校验，主表无stats；分配与GC取独立诊断轮。每项两版产物另做空PATH中文
目录搬迁。三个主回归程序另执行删除源码/build后的真正搬迁，区别于基准源仍留仓库的保护轮。

## 切片自身结果

| 切片 | 基准 | A墙钟ms | B墙钟ms | 说明 |
| --- | --- | ---: | ---: | --- |
| tuple | records | 134.450 | 123.083 | 本片前后，RSS50.83/50.80MiB |
| tuple | set_tuple | 79.169 | 75.617 | RSS44.84MiB持平 |
| tuple | comprehensions | 114.025 | 112.767 | 保护项，分配持平 |
| 集合方法 | records | 121.697 | 110.714 | A已含tuple，不与上表跨轮拼接 |
| 集合方法 | list_ops | 83.821 | 77.285 | RSS76.12/76.08MiB |
| 集合方法 | comprehensions | 113.378 | 105.199 | RSS46.89/46.98MiB |
| 集合方法 | oop | 86.084 | 85.472 | 原实例PIC保护 |
| 整数比较 | generators | 310.068 | 283.380 | 分配145263344字节持平 |
| 整数比较 | comprehensions | 109.021 | 96.792 | 分配125614512字节持平 |
| 整数比较 | nqueens | 69.480 | 68.957 | 保护项 |
| 整数比较 | matmul | 25.496 | 26.160 | +2.60%，保留不利样本，追加复核 |

matmul比较片+2.60%的不利样本以及下述全量轮出现的bigint/oop回退，统一追加一次10轮保护对照。上述主要收益为减少分派，累计分配与RSS
整体接近持平；几十字节的分配差异不用于解释毫秒收益。

## 最终26项合并对照

A为767e665c冻结CLI，B含三个切片。1次预热、5次交替采样，全部退出/双流及两臂搬迁通过。

| 重点基准 | CP墙钟ms | A墙钟ms | B墙钟ms | B/A变化 | A/B峰值RSS MiB |
| --- | ---: | ---: | ---: | ---: | --- |
| records | 68.076 | 134.110 | 112.846 | -15.86% | 50.81 / 50.86 |
| comprehensions | 45.035 | 114.895 | 100.127 | -12.85% | 46.83 / 47.14 |
| list_ops | 58.810 | 84.545 | 75.969 | -10.14% | 76.06 / 76.14 |
| generators | 122.817 | 321.262 | 303.617 | -5.49% | 21.88 / 21.80 |
| set_tuple | 69.274 | 80.564 | 70.190 | -12.88% | 44.83 / 44.84 |
| dict_ops | 48.748 | 60.000 | 55.658 | -7.24% | 32.95 / 33.03 |

排除hello，各项中位数B/A几何均值0.95617。两版本轮均为速度14/26、RSS4/26、同时3/26达CP；
本轮缩小高差距项目耗时，达标项目数持平。comprehensions峰值RSS增加0.31MiB，累计分配减少
4640字节；两者属于不同指标，按本次观测分别保留。

其余项目墙钟变化，正值为新版更慢：

| 基准 | B/A变化 | 基准 | B/A变化 |
| --- | ---: | --- | ---: |
| hello | +2.12% | fib | +0.77% |
| mandelbrot | +0.69% | bigint | +3.98% |
| matmul | -1.06% | nbody | +1.04% |
| sieve | +0.30% | spectral_norm | +1.07% |
| calls | -2.50% | dynamic | -1.43% |
| attrs | +0.96% | oop | +5.64% |
| containers | -22.07% | str_concat | -16.41% |
| strings | -0.72% | nqueens | +1.06% |
| sorting | -4.65% | exceptions | -4.89% |
| closures | +1.32% | decorators | -1.55% |

同一基线A/最终B追加1次预热、10次交替：bigint79.191→80.584ms（+1.76%），
matmul26.696→26.234ms（-1.73%），oop91.892→91.159ms（-0.80%）。
oop和matmul先前较大回退未稳定复现；bigint两轮均不利，保留为本批实现的局部性能代价。
接受三个切片：四个重点项目以及dict_ops/set_tuple均有实测收益，整体耗时几何均值下降；
保留全部不利样本和bigint后续优化任务，不作所有路径零回退结论。

## 正确性

8项C#边界测试，三个手写主e2e已通过同次CP/JIT、PIC开关双模式和真实NativeAOT。
源文件与build删除后，在空PATH和中文空格路径执行，完整双流与退出一致。覆盖NaN身份、
元组元素回调/异常、数值混合、子类覆盖、星号和关键字拒绝、同一get站点hash回调重入。
NumPy2.3.5版本断言后，同一应用交错foreign整数比较、tuple元素、append/pop/get与数组方法，
通过CP/随包NativeAOT；删除输入发行、NumPy输入载荷和build后的空PATH搬迁也通过。

四类未捕获异常（tuple元素比较、空list.pop、dict.get中的hash、混合富比较回调）
旧新AOT退出与完整双流一致；CP退出、stdout副作用和异常末行一致。回溯文本的路径及
源码echo/caret沿既有格式边界分别保留，原始stderr记录在errors.json。
完整make check退出0：C#3175通过/2跳过，MIR377通过/4隔离，cases-selftest760通过，
编译e2e478通过/286忽略；fmt、严格Clippy和其余Rust套件全部通过。三个新e2e已转正。
本批不改变已有隔离/忽略项状态。

## 证据与范围

临时证据为`/tmp/ryupy-hotspots-profile/`的采样与符号化记录、
`/tmp/ryupy-hotspots-{tuple,method,compare}-ab.json`以及native/foreign日志。
四类错误与完整门禁证据为`/tmp/ryupy-hotspots-errors.json`、`/tmp/ryupy-hotspots-make-check.log`。
本轮最终冻结CLI为`/tmp/ryupy-hotspots-compare-cli/ryupy`；恢复时核对实际布局并执行。
采样构建树与各配对临时根已清理，采样文本和独立冻结CLI保留。
最终26项及追加保护原始数据为`/tmp/ryupy-hotspots-full-ab.json`、`/tmp/ryupy-hotspots-protection.json`。
仓库只保留必要源码、手写输入和本文审定结论；最终平台验收限定于本机macOS arm64。

配对工具已保存在`tests/perf/aot_hotspot_pair.py`，拒绝错误Python版本、未知基准名、仓库内原始报告；
错误Python版本、未知名称及报告路径三个拒绝分支已实际检查。复现示例（两版CLI须先各自构建冻结）：

```sh
python3 tests/perf/aot_hotspot_pair.py --old /外部目录/旧版/ryupy --new /外部目录/新版/ryupy \
  --python /固定CPython3.12.13路径/python3.12 --runs 10 \
  --only bigint,oop,matmul --output /tmp/aot-protection.json
```

省略`--only`运行原始26项；主表直接启动CP脚本，独立stats轮与基准搬迁都验证退出/双流。
