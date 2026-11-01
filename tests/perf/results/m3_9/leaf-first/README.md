# M3.9 首次纯叶调用特化：成功守卫获益，失败守卫默认门槛未闭环

候选目录 `/tmp/ryupy-m3_9-leaf-candidate-MHRhp1`，原基线CLI/编译体在
`/tmp/ryupy-m3_9-alias-baseline-HVOxHG`。当前新增TraceCheckDepth运行时接口，因此将旧基线
编译体复制到候选baseline-artifact并替换为**同当前Release runtime**，不覆盖原始基线。
所有配对同runtime；hashes.txt/guard-hashes.txt固定产物。source.patch含捕获时crates/runtime
tracked和untracked完整源码，manifest锁来源，不含.pending/results。无DLL/CLI/bin/obj归档。

原alias_leaf负载保持不变：动态alias与静态direct同形loop、holder累加。新增leaf_guards.py
独立临时诊断：同一repeat调用点依次int、bool、float、溢出、用户__add__、重绑函数，输入构造与
用户计数重置在计时外；检查结果类型、身份和每轮用户调用计数。固定3.12.13与两编译体短验全对。
MIR实际命中私有函数身份anchor、精确i64守卫、WrappingAdd/overflow及TraceCheckDepth，失败回
原dyn.call。不是未优化路径的假守卫测试，不声称完整工程make check。

## 两组配对

默认/禁tier各3新进程，每进程3热身7样本、20000循环，基线/候选轮转；完整做两组，不挑好结果。
每进程中位后跨进程中位，wall单列；原始paired*和guard-paired*全部保留，summary.json统一汇总。
非隔离容器、未固定频率，较大单组比例不视作精确可重复幅度。

候选相对同runtime基线（正数更慢）：

| 场景/模式 | 第1组 | 第2组 |
|---|---:|---:|
| 原alias / 默认 | -70.8% | -72.9% |
| 原alias / 禁tier | -11.5% | -50.3% |
| 原direct对照 / 默认 | +15.5% | +12.0% |
| 原direct对照 / 禁tier | -15.5% | +31.3% |
| guard int / 默认 | -89.3% | -91.6% |
| guard int / 禁tier | -80.2% | -77.5% |
| guard bool / 默认 | +52.3% | +19.6% |
| guard custom / 默认 | +46.0% | +6.0% |
| guard rebind / 默认 | +288.4% | +34.1% |
| guard custom / 禁tier | +15.3% | +4.6% |

float默认+59.6%后-13.6%、rebind禁tier-6.9%后+14.6%，其余也逐项保留；不能只以成功int
收益批准广泛默认。重绑+288%幅度未复现但默认两组均退化，bool/custom也有重复不利证据。

## 分配与实际JIT

同真实函数harness复用参数，20热身后5批20000循环，两tier各3进程。候选alias/direct与本轮
换同runtime重测基线均950800 B（47.54 B/轮），没有分配收益。原PIC已复用实参数组，精确arity
thunk已绕binder，不能声称新优化省了每轮binder/args数组。成功路径主要省动态PIC/delegate/thunk
及被内联叶函数的trace push/pop，仍保递归深度检查；实际FullOpts可见PyTraceback.CheckDepth调用。

机器码证据：
- 原alias loop FullOpts基线706→候选857B；默认Tier1-OSR基线1639→候选1797B。direct仍570B
  FullOpts/885B Tier1，仍静态调用twice。成功guard分支直接算术；失败路径仍回PIC。
- guard repeat FullOpts368→524B，stack24→40B；默认基线Tier1 1035B，候选捕获两个OSR版本
  1193/1324B。所有版本原样保存，未挑一个版本代表所有阶段。
- guard禁tier候选IG09先LoadName再比较私有anchor，IG03–05精确type/overflow；失败IG10–12
  复用PIC实参槽后CallValueFast。没有直接累加器first实验那样巨量多类型算术内联，不能未经
  计数器证据把全部退化都归因“PIC巨量内联”。新增守卫与布局/活跃值压力是可查因素而非已定量
  证明的唯一原因。默认PIC部分内联、委托间接调用仍在失败路径。

结论：成功守卫净收益明确，分配没有改善；失败守卫当前不足以满足默认无明显回退。最多做有
证据的有界修正或收窄/显式实验，不应无限追逐微基准；最终策略由父审阅决定。

## 重现

应用source.patch到manifest基提交，在独立树make runtime、cargo build -p ryupy-cli；默认编
workload.py、leaf_guards.py。保旧基线编译体但更新为相同runtime到隔离目录，再运行compare.py
及compare-guards.py的baseline=目录、candidate=目录、--output新目录。diagnostics.py和harness
专门用于alias_leaf分配/JIT，与正式计时分开；guard JIT用DOTNET_JitDisasm=*repeat*，分别指定
默认/禁tier与独立JitStdOutFile。诊断产生的输出不得作为正式时间样本。未提交。
