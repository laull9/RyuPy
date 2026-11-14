# BuiltinSum 流式迭代设计与验证记录

## 1. 背景与动机

在既有实现中，`PyBuiltins.cs` 内的 `BuiltinSum` 实现为：

```csharp
foreach (object? item in ToSequence(args[0]))
{
    total = Binary(Add, total ?? PyNone.Instance, item ?? PyNone.Instance);
}
```

其中 `ToSequence` 对列表创建数组副本，对生成器和通用迭代器调用 `DrainIterator` 收集后复制数组；精确元组复用其数组。流式来源在相加前已经耗尽。

这种预先物化存在如下语义与执行缺陷：
1. **全量物化开销**：必须分配完整数组，无法流式处理大序列或潜在惰性流；
2. **副作用交错颠倒**：生成器的 `yield` 副作用全部发生于加法及元素的 `__radd__` 之前，与 Python 标准对象协议不符；
3. **加法异常无法早停**：若第 2 次加法抛出异常，原本在物化阶段已提前消耗了后续所有元素（第 3 项及之后）；
4. **活列表变异不可见**：对 `list` 的迭代如果通过预先拷贝或物化，加法回调中对被迭代 `list` 的追加在当次 `sum` 中将不可见。

## 2. 改动方案

### 2.1 运行时流式消费

在 `runtime/RyuPy.Runtime/PyBuiltins.cs` 中，将 `BuiltinSum` 循环调整为单次创建通用迭代器并即时累加：

```csharp
object? source = args[0] is object?[] array ? new PyTuple(array) : args[0];
IPyIterator iterator = PyIter.Create(source);
while (iterator.MoveNext())
{
    total = Binary(Add, total ?? PyNone.Instance, iterator.Current() ?? PyNone.Instance);
}
return total ?? PyNone.Instance;
```

核心语义约束保持：
- **单次创建**：仅在入口处通过 `PyIter.Create(args[0])` 调用一次 `__iter__`；
- **即时累加与早停**：每次 `MoveNext()` 为真后读取 `Current()`，立即通过 `Binary(Add, ...)` 二元加法计算；若加法抛出异常，后续元素不再被拉取；
- **参数绑定契约保持**：沿用既有位置/关键字 `start` 解析与报错逻辑；
- **空输入身份保持**：当序列为空时，直接返回初始 `total`，保留 `start` 原对象引用身份；
- **严格普通二元加法**：不引入 `+=` / `InPlaceBinary` 或 `NumericAccumulator.Add`，不调用 `__iadd__`，不引入整数去装箱/fast-sum/float compensated 假设。

### 2.2 注释同步

在 `runtime/RyuPy.Runtime/PyIteration.cs` 中修补 `DrainIterator` 上的过时注释，移除其中的 `sum(it)` 提及。

## 3. 验证事实

### 3.1 C# 单元测试 (`runtime/RyuPy.Runtime.Tests/PySumStreamingTests.cs`)

执行定向测试：
```bash
dotnet test runtime/RyuPy.Runtime.Tests --filter FullyQualifiedName~PySumStreaming
```
测试结果：
- 失败: 0，通过: 8，略过: 0，总计: 8，持续时间: 19 ms。

用例覆盖：
1. `StreamingConsumptionIsStrictlyInterleaved`：验证有状态 `IPyIterator` 与自定义 `__radd__` 严格交错（`MoveNext` -> `Current` -> `Add` -> `MoveNext` ...）；
2. `EarlyStoppingWhenAdditionThrowsException`：验证加法抛错时迭代器立刻终止，不发生后续项的 `MoveNext`；
3. `EmptyIterablePreservesStartIdentity`：验证空输入保持 `start` 对象身份引用（`Assert.Same`）；
4. `SumWithNumericStartPositionalAndKeyword`：验证数值位置/关键字 `start` 累加正确；
5. `LiveListMutationDuringStreamingSumVisitsAppendedItem`：验证在累加回调中向被迭代列表追加新元素时，游标能访问到追加项；
6. `CustomStartUsesAddAndNeverIAdd`：验证自定义 `start` 仅触发 `__add__`，`__iadd__` 调用计数为 0；
7. `IteratorExceptionPropagatesImmediately`：验证迭代器内部异常即时向上传播并中断计算；
8. `BuiltinSumParameterValidation`：验证缺少参数、过多参数与未知关键字报错。

### 3.2 CPython 3.12.13 E2E 自检 (`tests/e2e/phase3/aot_sum_streaming.py`)

使用环境固定 Python `/Users/laull/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3.12` 独立执行该脚本：
- 基础序列（list、tuple、range、generator）计算无误；
- 空列表保留 start 身份为 `True`；
- 生成器 yield 与 `__radd__` 严格交错：`['yield:10', 'radd:10', 'yield:20', 'radd:20', 'yield:30', 'radd:30']`；
- 加法失败早停：第 2 项抛错后未生成第 3 项（`['gen:1', 'add:1', 'gen:2', 'add:2', 'caught:failed at step 2']`）；
- 自定义 start 对象 `add_count == 1` 且 `iadd_count == 0`；
- 迭代中修改 list 长度变为 3 且总和为 6；
- 迭代器中途异常被捕获且 `iter_calls == 1`。

（注：`tests/e2e/phase3/aot_sum_streaming.py` 首行保留 `# ryupy: pending`，按要求交回主代理统一处理。）

## 4. 独立记录的问题（本轮不扩展修复）

在对照 CPython 3.12 行为时，记录以下既有行为偏差，留待后续独立切片处理：
1. **字符串/字节 start 拦截**：CPython `sum` 对 `str`、`bytes`、`bytearray` 作为 `start` 时会显式抛出拒绝消息（例如 `TypeError: sum() can't sum strings [use ''.join(seq) instead]`）；RyuPy 目前未在 `sum` 入口针对这些类型特判抛错，而是交由后续加法协议处理；
2. **多重指定 start 校验**：若同时通过第 2 位置参数与关键字参数 `start` 传入，CPython 抛出 `TypeError: sum() got multiple values for argument 'start'`，RyuPy 目前参数解析分支对该特殊重叠未做特异报错。

## 5. 未验项说明

1. 本任务仅执行了定向 C# 测试与独立 CPython 3.12.13 预期校验；
2. 按照约束，未运行 cargo 全套、dotnet 全套或 benchmark，AOT 编译门禁与完整回归由主代理统一收尾。

## 主代理审查补充

保留内部object数组输入：零元素复制地包装成PyTuple后使用原游标，避免流式入口收窄原ToSequence接受域。新增原生数组、元组、浮点与大整数C#回归，E2E增加浮点/大整数/布尔数值。测试文件头和关键注释统一中文。新增部分尚待主代理重跑。

## 主代理验证进展

9项定向C#测试通过，pending E2E即时CPython/JIT差分通过，已转正。正式Release CLI真实NativeAOT在源码和构建目录隐藏、空PATH、中文空格路径搬迁后退出码与双流一致。迭代器抛错和加法回调抛错两类未捕获异常，比较完整stderr，仅规范本次源路径、对应源码回显与caret；退出码/stdout/帧顺序/行号/函数/异常消息一致。六项AOT对照与完整门禁待完成。
