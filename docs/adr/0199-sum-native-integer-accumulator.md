# ADR-0199：内建 sum 原生整数累加器优化

- 状态：已接受；有限默认，CP/JIT/NativeAOT/混合域搬迁及完整门禁通过
- 日期：2026-11-15
- 关联：ADR-0010, ADR-0044, ADR-0182

## 决定

在 `BuiltinSum` 已有的流式消费与严格交错机制基础上，针对连续精确 CLR `long`（Python 整数的核心表示）累加引入原生栈上累加模式：

1. **状态机与生命周期**：
   维护 `totalObj`、`long nativeTotal`、`bool nativeMode` 与 `bool dirty`。
   仅当 `total` 精确为 `long` 且迭代项 `item` 精确为 `long` 时进入快路。`bool` 绝不隐式压入快路，保证其类型和加法协议走标准分派。
2. **延迟装箱与引用同一性保持**：
   初始或通用加法返回 `long` 时进入 `nativeMode`，但标记 `dirty = false` 并保留原对象引用。
   在空输入或快路未发生实际累加时，原样返回原 `total` 对象，严格保证空 `start` 及自定义回调返回 shared long 的引用身份。
   仅在快路执行成功后将 `dirty` 置为 `true`。循环结束或回退到通用分派之前才按需调用 `PyInt.Box(nativeTotal)` 物化。
3. **溢出检测与 BigInteger 升级**：
   快路累加采用 `unchecked` 加法，按 `PyInt.AddI64` 相同的同符号异号位运算公式 `((nativeTotal ^ sum) & (itemLong ^ sum)) < 0` 判断溢出，避免热路径上的 checked+catch 开销。
   溢出时物化准确左值后通过既有 `Binary(Add, ...)` 升级为 `BigInteger`；若后续加法使结果落回 `long` 范围（经 `Normalize`），可自动恢复进入快路。
4. **协议交错与早停**：
   严格保留每轮 `MoveNext()` 与 `Current()` 各调用一次的交错契约。
   对自定义类型严格使用 `__add__` / `__radd__`，绝不调用 `__iadd__`。遇异构项或用户回调前，若 `dirty` 则立即物化，使自定义加法看到精确已累加值。加法或迭代异常立即早停，不漏调也不多调。
5. **架构边界**：
   不引入新的 public ABI、RuntimeFn、全局环境开关、堆分配 holder 或 unsafe 指针模型。实现作为 `PyOps` 的 partial helper 拆分到 `PyBuiltinSum.cs`，保持 `PyBuiltins.cs` 行数受限（< 600 行）。

## 验收与取舍

20项C#专项、固定CP/JIT/真实NativeAOT及输入隐藏搬迁、NumPy混合域与异常双流通过，主fixture已转正。
完整make check退出0：C#3160/2、MIR371/4、cases756、编译e2e474/286。
26项1warm5中generators343.71→312.17ms，后续重点10轮333.08→306.97ms；
独立轮分配169254912→145255296字节。RSS约22MiB持平，CP逐项内存目标仍未达到。
全量整体B/A几何耗时0.99800，sorting+2.18%、closures+1.62%和comprehensions+0.38%等不利值保留。
不以人为噪声阈值或单次内部统计覆盖不利结果，追加保护轮结论见[专项文档](../aot-sum-integer-optimization.md)。
默认空输入规范小整数0的身份修正纳入本切；显式start/回调引用保持。
两项既有sum参数偏差继续登记于semantics §4.15，未扩展其解析范围。

追加sorting配对曾+7.48%且三臂波动较大，最后独立10轮+0.25%，未稳定复现>5%回退；
仍保留所有负结果，接受整数热点的复现收益与全量接近持平的取舍。
