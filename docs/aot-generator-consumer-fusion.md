# 生成器消费融合：准入与分臂验收

状态：2026-11-15，基于dev `81758bb477729aee4636f2ca6d6b0815b949849a`。
本切片仅增加手写语义输入、同源分臂基准与自检；**没有启用消费者融合，也没有性能收益结论**。
字段驻留首切已有结果不归入本切片。总合同见[架构路线](aot-architecture-performance-plan.md)。

## 当前流水线的实际边界

- `generator_state::build_program`先以`DeferredGenerators`保留高层`Suspend`，随即选择字段
  或旧数组lowering。真正融合需在该选择前分析生产者和消费者，不能事后按`$state_resume`
  名字或状态机PC猜测源结构，也不能让常规pass任意穿过暂停边。
- 普通函数名的动态查找、实际函数owner/defaults与捕获仍须保留。仅知道静态函数名不能
  证明调用目标；生成器创建以后，修改该名字不能改变已创建实例。
- HIR记录生成器表达式来源，但MIR的`is_comprehension`目前仅指急切推导式。不能复用
  该bool把genexpr当作list推导式；候选计划须显式保存生产者来源与消费种类。
- `PyGenerator.MoveNextCore`包办running、异常上下文、真实traceback、PEP479与关闭。
  绕过逐项委托前须为这些行为建立等价边界，不是只把循环体拼接起来。

## 高层计划的最小合同

先只分析有限的无EH、无yield-from、无locals观察的字段状态候选，保留其他路径不变。
计划至少携带生产者身份、创建点及首次启动点、yield/resume映射、消费者种类、逃逸证明、
每条提前退出边与物化映射。分析预算耗尽或任何未知操作应拒绝，不作乐观猜测。

1. 入口：先按原顺序求值实参和最左iterable/取迭代器；尚未消费时不能执行生产者正文。
   真实函数身份守卫在任何生产者副作用之前失败才可回到原工厂。
2. 单项：生产者逐项计算、过滤和yield顺序不变。消费者回调会观察共享cell和全局变化；
   未证明无观察的调用是区边界，不能把读取移到循环外。
3. 已有对象与逃逸：同一盒、列表别名、保存的生成器和跨桥引用原样物化。证明为未逃逸
   才可消除生成器实体；若保留实体，其暂停PC、局部、current与关闭状态须同步。
4. 提前结束：普通for的break或消费者异常不自动close仍可达的生成器。不能为了资源清理
   插入原程序没有的close，也不能为了吞掉错误继续多消费一项。
5. 异常：生产者StopIteration沿PEP479；消费者加法/循环体抛出的异常不属于生产者。
   consumer帧与producer帧的所有权保持独立，禁止把两者放入同一个异常转换catch。
6. 失效：若区内守卫失败，只能从精确当前操作物化后继续，不从入口重放副作用。
7. yield-from单独实现：send、throw、close、返回值及委托暂停都有合同；不能以普通for
   或sum成功替代。首个无委托候选通过也不勾掉整个G。

## 本切片输入与可重复命令

`tests/e2e/phase3/aot_generator_consumers.py`覆盖普通for、sum、yield-from，最左iter立即求值、
过滤惰性、函数重绑定、共享cell、break/消费者错误后的别名恢复、显式close、sum加法失败
停止点、对象身份和PEP479。产品差分尚未运行，因此保留pending；pending不计通过数。

`tests/perf/generator_consumers/`的三个手写文件分别复制原始generators的一个消费臂，
保持函数、次数、表达式与模块级累加形态。每个进程只额外输出自己的总数。
`test_generator_consumers.py`以AST核对其与原源的精确关系，并核对三结果之和。

```bash
python3 -B tests/perf/test_generator_consumers.py
python3 -B tests/perf/aot_hotspot_pair.py \
  --old /absolute/frozen-old/ryupy --new /absolute/frozen-new/ryupy \
  --python /absolute/python3.12.13 --suite generator-consumers \
  --runs 7 --output /external/generator-consumers.json
```

配对脚本默认仍为原始26项，不把三个归因项混入原套件或几何均值；报告显式登记suite。
配对前同次核对CP完整双流和退出，CP/A/B与CP/B/A交错，每臂独立新进程包含启动成本，
分配/GC另轮采样。诊断臂彼此不可加总推断原程序速度或RSS；最终仍须运行未修改的
`tests/benchmarks/generators.py`和原始26项，独占计时、NumPy与删除输入搬迁另行验收。

## 当前验证与未完成项

本环境仅运行了可用CPython的语义输入、自检2项和`git diff --check`；这些不是固定
3.12.13产品差分，也不是Rust/C#、JIT或NativeAOT通过。无本地cargo/dotnet时交由CI验证，
在实际产品通过前不移除pending，不默认开启新路径。

后续按次序：真实高层候选/拒绝结构测试；同次CP/JIT差分；融合候选的原始AOT三臂与
完整原程序配对；异常/身份/桥边界与搬迁；完整make check。只有这些证据闭合后才能
登记具体命中的消费者与收益，不将测试设施完成写成优化完成。
