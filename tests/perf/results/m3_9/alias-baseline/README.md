# M3.9 纯叶函数调用基线：模块别名与静态调用

产物保存 `/tmp/ryupy-m3_9-alias-baseline-HVOxHG`。生产来源为当前已完整验收的默认holder+
TupleScalar，尚无叶函数特化。source.patch含当时crates/runtime所有tracked修改及untracked新
源码，manifest锁基提交/UTC/逐文件hash，排.pending/results。保存CLI和Release后再释放其它
worker编辑权；hashes.txt锁实际产物，不提交DLL/CLI/bin/obj。

`twice(x): return x+x`无注解。两个同形循环分别alias(index)与twice(index)，都用相同holder累加。
固定3.12.13和真实DLL短验均380，alias身份与零轮断言通过；正式20000轮均399980000。
MIR/CIL证明alias走dyn.call/PyPic.CallValue1，direct走静态m0_twice，不按源码名称猜测路径。

## 两组正式基线

每组默认/禁tier各3进程，每进程各场景3热身7计时，20000循环。环境清Jit/Tiered变量，禁tier
仅设置DOTNET_TieredCompilation=0。先每进程中位再跨进程中位，wall分开保存；分配/JIT诊断另开
进程，不在正式计时中开日志。两场景在同进程按原负载alias→direct顺序，未做跨场景随机化，
因此这张基线对照不是严格隔离调用开销的因果估计；后续candidate须跨独立产物交错重跑。

| 模式 | 第1组alias/direct | 第2组alias/direct |
|---|---:|---:|
| 默认 | 2.284978 / 1.410732 ms | 3.211029 / 1.417352 ms |
| 禁tier | 1.179236 / 0.634112 ms | 1.399495 / 0.575425 ms |

原始输出/每个样本见baseline、baseline-repeat。此处没有候选性能提升结论，更不外推项目性能。

## 分配与实际调用链

独立harness复用反射参数，先执行模块再每方法20次预热，每进程5次20000循环，两个tier各3
进程。所有样本alias/direct都是950800 B，即47.54 B/轮。总数包含索引/结果盒、holder与固定
返回开销，不能全部归因调用框架；但明确不能宣称alias每轮额外创建实参数组。

实际CIL已有PyPic.CallValue1而非逐轮newarr；实际FullOpts调用循环已内联CallValue1/ArgFor的槽
访问，复用调用点参数数组后call CallValueFast。完整实际分层机器码按loops/leaf/thunk/pic分文件：

- 禁tier loops：alias706 B、direct570 B。alias调用CallValueFast；其精确函数PIC路径内联
  PyFunction.Invoke薄包装后通过PyCall delegate间接tailjmp，非“所有封装均没被JIT内联”。
- `__thunk_0`禁tier77 B：先检查args.Length==1和kwNames.Length==0，快路直接取参数并tailjmp
  m0_twice；只有不匹配时才调用Binder.Bind。因此不能把未来收益表述成“省掉每次binder”。
- 默认Tier1-OSR loops：alias1639 B、direct885 B。alias已内联更多PIC逻辑但仍有PyCall委托
  间接调用；direct仍有静态m0_twice调用，未观察到把叶函数完整内联进循环。
- 本轮默认leaf/thunk文件只捕获到Tier0版本，不据此断言任何未来热度都不会提升或内联。
  捕获的leaf含PushFrame/PopTo；禁tierleaf也保trace调用。默认caller中的部分trace/行号操作
  由JIT内联，机器码原样保留，不能笼统声称trace均已消失或均为独立调用。

这是已发生的机器码证据，不能只从源代码的AggressiveInlining属性推断是否内联。当前最明确
额外成本是动态PIC守卫、参数槽写入、委托/thunk边界；仍需候选实际配对才能判断收益和分配变化。

## 重现

在独立基提交应用source.patch，cargo build -p ryupy-cli，默认CLI编workload.py至artifact。
激活固定.NET10工具链后：

```sh
python3 compare.py baseline=产物根/artifact --output 新结果目录
dotnet build harness/alloc.csproj -c Release
python3 diagnostics.py 产物根
```

candidate完成后compare.py可增加candidate=目录，轮转进程先后。harness引用../artifact中的
同版本runtime，diagnostics.py写入传入根目录，复现应使用新根目录，不覆盖原记录。当前专项只
保存基线；未提交，工程全门禁由父维护。
