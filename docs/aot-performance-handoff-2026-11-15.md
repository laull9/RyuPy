# AOT性能优化交接（2026-11-15）

## 用户要求与收尾范围

用户要求：完成本轮后写交接报告并停下。原目标为逐项速度不弱于CPython、内存不高于CPython，运行速度优先。
本报告正在收尾：产品优化已完成提交；筛法只读审查完成，CPython桥专项测量在途，返回后回填并停止。
不再启动新实现、代理或下一轮基准。下一轮事项仅列建议，不自动执行。

## 已交付代码与验证

产品截止`692f2f17`，分支`dev`，已推送`origin/dev`；本轮文档收尾提交另见git log。
七个已验证优化切片与主要证据：

| 提交 | 切片 | 实测或边界 |
| --- | --- | --- |
| 6aa3d992 | 有界顶层精确字符串累加 | str_concat约941.6→11.2ms，containers约159.1→31.7ms；只允许无观察的精确字面量循环 |
| 9cb7a4d7 | PIC四张核心表按需分页 | 固定总容量，惰性分配；局部速度取舍登记ADR-0195 |
| c9abdb5e | sum流式消费 | generators约416.9→336.4ms，RSS83.6→21.9MiB，保留交错与早停 |
| 6ad1cce8 | 列表推导式私有collector直调 | comprehensions约130.1→117.7ms，保持独立作用域，RSS持平 |
| c36be201 | 布尔重复列表紧凑背板 | sieve170.8→138.9ms，RSS52.8→24.0MiB；修复Count内联导致的数值回退 |
| 0c162eb2 | 生成器只读形参省写回 | 重点335.96→331.32ms，分配持平；全量整体持平，负结果保留 |
| 692f2f17 | sum精确整数原生累计 | 重点333.08→306.97ms，分配减少23999616字节，RSS基本持平 |

以上为各切片自身配对结果，来自不同轮次；不要将跨轮端点拼接成单切片收益。
基准入口修复`ac7ff75a`已解决现代CLI布局误判与失败流漏检，原始26项可执行。
AOT Speed探索`a96aa032`保持现有默认，不新增CLI开关；没有采用所有方法折叠或激进元数据裁剪。

最新完整make check退出0，日志由主代理核对：
C#3160通过/2跳过，MIR371通过/4隔离，cases-selftest756通过，编译e2e474通过/286忽略。
fmt/严格Clippy通过。主sum用例已从pending转正。
CP/JIT/真实NativeAOT、源码与构建隐藏、中文空格路径、空PATH搬迁均通过；
NumPy2.3.5混合整数/浮点回退和随包搬迁通过。未捕获加法/迭代异常旧新AOT完整双流相同，
CP的路径与源码echo/caret差异沿既有边界单列。此次产品验收平台为macOS arm64。

## 当前达标情况

最新原始26项全量1warm5中位数统计：

| 目标 | 达标 | 尚未达标 |
| --- | ---: | ---: |
| 速度不慢于CPython | 12 | 14 |
| 峰值RSS不高于CPython | 4 | 22 |
| 两者同时 | 2 | 24 |

速度未达标：bigint、nbody、sieve、oop、dict_ops、list_ops、set_tuple、strings、nqueens、
exceptions、records、comprehensions、decorators、generators。
RSS达标：hello、fib、sieve、nqueens；两者同时达标：hello、fib。
贴近1倍的项目会随采样波动，计数是本轮实测状态，不能据此给总体工程完成百分比。

最新全量排除hello的CP/B几何加速比1.14489；原始优化前约0.86，逐项目标仍有明显差距。
sum本切片全量B/A中位数比几何均值0.99800，逐轮配对0.99819。
sorting全量+2.18%，主代理追加10轮+7.48%且三臂波动较大，再独立10轮+0.25%；
closures追加+0.94%，comprehensions重点+0.61%。这些不利结果保留，未声称零回退。
详细方法与取舍见[aot-sum-integer-optimization.md](aot-sum-integer-optimization.md)。

## 本轮最后两项任务

### 筛法完整运行时快循环审查（已结束，无实现）

候选采用单一受守卫Runtime helper执行原生long计数与精确列表False写入，失败null回原CFG，成功返回最终整数。
目标是同时消除`j += i`生产端装箱和动态下标分派，仅新增SetItemI64 ABI覆盖不到主要分配。
原始2M筛法内层写入数学次数4347778，任何次数乘24均为理论估算，尚无本候选实测。
异构复审支持候选；主代理修正TryI64会接受bool、最终装箱/大整数分配、无名临时槽跨块活性、
PyList私有背板访问及资源错误Line/j写回等未闭合点，下一轮仍须源码结构证明与实际验收。
守卫必须精确long、正步、0<=j<=bound<Count，排除数值背板/cell/EH/product/locals/环内回调与resize。
零轮沿原路保留身份，正溢出用精确BigInteger终值退出。短列表j=bound=1、step=long.Max才覆盖快路溢出。
详情与检查矩阵见[aot-integer-index-optimization-plan.md](aot-integer-index-optimization-plan.md)。

### CPython桥专项（在途，完成后回填）

限定冻结CLI、CPython3.12.13/NumPy2.3.5、synchronized provider、原样例与独立长短探针，
主表无stats/report，独立诊断轮与真实搬迁。不得由总耗时差直接推断GIL/初始化/关闭各层占比。
测量结果、原始数据位置、已完成边界与未完成项将在收尾时登记。
源码已知：当前调用走PyObject_Call而非Vectorcall；Enter计数不是Python函数调用数；
Dispose无条件CaptureModulePaths属可能热点，尚无耗时占比证据。
方法见[aot-cpython-bridge-measurement-plan.md](aot-cpython-bridge-measurement-plan.md)。

## 下一轮建议（本轮不执行）

1. 完成筛法候选的闭合匹配/失败无写入证明，手写e2e先pending，真实sieve源结构测试，Runtime/MIR/codegen ABI同步。
   定向验证后冻结旧新CLI独占A/B，特别保护matmul及所有存储模式，再跑全量门禁。
2. 依据桥实测开新竖切；关闭观测门控、Vectorcall或租约重入必须各有实际热点证据及生命周期回归。
3. 推导式、records及容器继续优化分配；不要恢复此前错误的“全部cell闭包”归因。
4. onefile尚无产品优化：默认资源写入隔离保留，先闭合缓存租约/有界回收/可写克隆与信号生命周期。
   exec后峰RSS可能保留解包高水位，父子高水位不能相加冒充同时峰值。
   设计见[aot-onefile-optimization-plan.md](aot-onefile-optimization-plan.md)。
5. 生成器活跃性实验已撤出当前树、保留远端WIP；它没有收益验收，不作为当前只读形参实现依赖。

## 复现约定与证据

- 固定CP：`/Users/laull/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3.12`。
- macOS M5 arm64，SDK10.0.403；CP来源及NumPy2.3.5载荷须实际校验版本并成功执行。
- `PYTHON`与`RYUPY_FOREACH_PYTHON`固定同CP；`RUSTC_WRAPPER=''`、`CARGO_TARGET_DIR=$PWD/target`、
  `RUST_TEST_THREADS=1`、`PYTHONDONTWRITEBYTECODE=1`，清除`RYUPY_RUNTIME_DIR`；TMPDIR使用仓库外独占临时目录。
- `make runtime`后`make check`；基准独占，不能与构建/门禁并行。单代码文件<=600行，关键注释保留。
- CP预期同次即时生成，退出码和完整双流先核验，再接纳时间/RSS；真实AOT搬迁、删除输入及平台分别登记。
- 不计算普通文件SHA/MD5，不在仓库保存机器产物/原始JSON/patch/golden/snap；仅源码、必要手写输入与审定文档。

本机临时证据（可能被系统清理，永久结论以已提交文档为准）：
`/tmp/ryupy-sum-integer-full-ab.json`、`/tmp/ryupy-sum-integer-focus10.json`、
`/tmp/ryupy-sum-integer-protection10.json`、`/tmp/ryupy-sum-integer-sorting-confirm10.json`、
`/tmp/ryupy-sum-integer-exceptions.json`、`/tmp/ryupy-sum-integer-make-check.log`。
冻结当前CLI：`/tmp/ryupy-sum-integer-cli-1k2l2b9e/ryupy`，路径指针`/tmp/ryupy-sum-integer-cli-path`；
旧基线：`/tmp/ryupy-generator-readonly-cli-frozen/ryupy`。
不要假设/tmp仍存在；恢复实验时先核对必要布局和真实执行，不做hash校验。

远端WIP：`origin/wip/aot-performance-20261115-022614`，保存阶段性研究与已撤下实验历史。
产品只使用已验收dev提交；WIP不作为验收版本。最终文档提交、工作树状态及在途任务收尾将在末次更新登记。
