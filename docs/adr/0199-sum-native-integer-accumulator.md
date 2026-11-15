# ADR-0199：内建 sum 原生整数累加器优化

- 状态：候选（Runtime 实现与单元测试已就绪，e2e 标记 pending 待 AOT 整体编排转正）
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

- Runtime 单元测试包含 9 项既有 `PySumStreamingTests`（全部原样通过）以及 11 项新增的 `PySumIntegerAccumulatorTests`（覆盖空 start、小整数缓存单例、负数抵消、>256 整数、正负溢出与回退、bool/float 混合、协议一致性、异常早停及 10,000 项连续累加分配上界 < 2048 字节）。
- CPython 3.12.13 对齐：新增 `tests/e2e/phase3/aot_sum_integer_accumulator.py`，经本地固定 CPython 3.12.13 校验输出一致，依规先标记 `# ryupy: pending`。
- 本 ADR 仅覆盖 Runtime 层内建 sum 的原生整数累加，CIL 编译期特定循环内联及 foreign 对象的跨语言语义仍由 AOT 主切片负责。

主代理补测负向溢出回落、原生整数段后用户回调返回shared long，20项PySum测试通过。
默认空sum现在返回规范小整数0，较旧实现的临时零盒更贴近CP；空显式start及回调结果身份保持。
此前“9项参数偏差”是将9项旧测试与已登记的两项参数偏差混淆，按semantics §4.15原记录解释。
