# AOT 内建 sum 原生整数累加器优化规格与实施报告

## 1. 目标与背景

在 RyuPy 运行时中，`sum(iterable, start=0)` 内建函数负责按 Python 对象协议对元素进行累加。
在先前的流式改造中，`BuiltinSum` 实现了严格的交错迭代与异常早停，但每一步累加均依赖通用二元操作分派 `Binary(Add, total, current)`，对于密集的整数序列（例如 `sum(range(N))` 或整数列表），会导致：
1. 每一步加法产生中间数值对象装箱（每个 boxed `long` 至少 24 字节开销）；
2. 频繁进入二元类型检查与动态分派分支；
3. 大量短期生命周期对象对 GC 造成垃圾收集压力。

本优化的核心目标是在保持流式消费、迭代/回调严格交错、异常早停及用户自定义加法协议（从不使用 `__iadd__`）完全不变的前提下，引入原生 `long` 栈上累加模式，消去连续精确 CLR `long` 累加过程中的每项中间装箱与通用分派，仅在异构项介入、用户协议回调或迭代结束时按需物化。

## 2. 状态机与算法设计

### 2.1 状态变量定义
- `object? totalObj`：保留当前累加结果的对象引用；
- `long nativeTotal`：当前原生 64 位整数累加值；
- `bool nativeMode`：当前是否处于原生整数累加模式；
- `bool dirty`：标记自上次进入快路后是否发生过新的原生累加。

### 2.2 准入与生命周期契约
1. **快路准入条件**：
   只有当 `nativeMode == true` 且当前迭代项精确为 `item is long itemLong` 时才执行原生累加。
   `bool`、`int`（32位）、`BigInteger` 或浮点等绝不隐式转换进快路。
2. **延迟装箱与引用同一性保持**：
   - 初始 `totalObj` 为 `long` 时进入 `nativeMode`，但初始化 `dirty = false`；
   - 每次通用 `Binary(Add)` 返回 `long` 时重新进入 `nativeMode`，同样置 `dirty = false`；
   - 若输入为空，或后续未发生实际快路累加（`dirty == false`），直接原样返回 `totalObj`，严格保障空 `start` 及自定义回调（如 `__radd__`）返回的 shared long 对象引用同一性（`Assert.Same`）；
   - 若 `dirty == true`，在结束或异构回调前统一调用 `PyInt.Box(nativeTotal)` 物化，落入 `[-5, 256]` 范围的自动获得规范小整数单例。
3. **同符号溢出检测与 BigInteger 升级**：
   快路累加采用 `unchecked` 计算：
   ```csharp
   long sum = unchecked(nativeTotal + itemLong);
   if (((nativeTotal ^ sum) & (itemLong ^ sum)) < 0)
   {
       // 溢出：物化准确左值后通过既有 Binary(Add) 升级为 BigInteger
       object left = dirty ? PyInt.Box(nativeTotal) : totalObj!;
       dirty = false;
       totalObj = Binary(Add, left, current ?? PyNone.Instance);
       if (totalObj is long recoveredLong)
       {
           nativeMode = true;
           nativeTotal = recoveredLong;
           dirty = false;
       }
       else
       {
           nativeMode = false;
       }
   }
   else
   {
       nativeTotal = sum;
       dirty = true;
   }
   ```
   不使用 `checked + catch` 结构，消除热路径异常抛出与栈展开开销；若后续累加因负数使结果回落并归一化到 `long`，状态机能够无缝恢复进入 `nativeMode`。
4. **严格交错与早停保证**：
   - 每轮循环保证 `MoveNext()` 与 `Current()` 各调用且仅调用一次；
   - 在调用异构类型或自定义类型加法之前，若 `dirty == true` 则先调用 `PyInt.Box(nativeTotal)` 物化，保证自定义加法能接收到准确的当前累计和；
   - 绝不调用 `__iadd__`，严格维持 `__add__` / `__radd__` 二元协议；
   - 遇到任何迭代异常、加法异常或取值异常立即中止，绝不预取或多调用后续元素。

## 3. 代码架构与文件分布

根据白名单与代码规范（单文件行数 < 600 行，无新增 public ABI，无额外堆分配 holder）：
- `runtime/RyuPy.Runtime/PyBuiltins.cs`：仅将 `BuiltinSum` 委托给 `BuiltinSumFast`，保持主分派文件精简；
- `runtime/RyuPy.Runtime/PyBuiltinSum.cs`：新增 `PyOps` 的 partial helper，集中承载状态机与原生累加逻辑；
- `runtime/RyuPy.Runtime.Tests/PySumIntegerAccumulatorTests.cs`：新增专项测试套件；
- `tests/e2e/phase3/aot_sum_integer_accumulator.py`：新增 e2e 验证用例（按规先标记 `# ryupy: pending`）；
- `docs/adr/0199-sum-native-integer-accumulator.md`：候选架构决策记录。

## 4. 验证实测输出

### 4.1 定向单元测试
执行命令：
```bash
dotnet test runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj --filter FullyQualifiedName~PySum
```
测试结果：
- 既有 `PySumStreamingTests`：9 项原样全部通过（包含严格交错、异常早停、start 身份、自定义类型从不调用 `__iadd__`、live list 变更等）；
- 新增 `PySumIntegerAccumulatorTests`：11 项全部通过；
- 总计：**20 项测试全部通过，0 失败，0 略过，持续时间 22 ms**。

### 4.2 内存分配上界实测
测试 `AllocationUpperBoundOnLongStreak` 对 10,000 个预装箱 `long` 元素进行连续累加：
- 理论上每项中间装箱（24 字节）至少产生 240,000 字节分配；
- 实测原生累加过程中，栈上原生计算完全消除中间装箱，整轮迭代除迭代器状态外分配开销远低于 2048 字节断言阈值。

### 4.3 CPython 3.12.13 对齐
使用固定的 CPython 3.12.13 执行 `tests/e2e/phase3/aot_sum_integer_accumulator.py`：
- 所有分段（连续整数累加、小整数规范单例、正负抵消、溢出与回退、bool/float 混合、协议一致性、异常早停）均正常退出且标准输出与断言严格符合预期。

## 5. 未验项与已知边界

1. **AOT 跨语言 Foreign 接口**：CLR `IPyForeignObject` 在内建 `sum` 中的边界保留既有 `Binary` 降级，其在独立编译产物下的完全端到端行为由主 AOT 切片测试覆盖；
2. **e2e pending 状态**：`tests/e2e/phase3/aot_sum_integer_accumulator.py` 当前标记为 `# ryupy: pending`，等待下一阶段整体编译流水线编排转正；
3. **参数已知偏差**：与 CPython 9 项参数校验相关的既有已知微小异常消息差异依规保持，不顺手扩大修复范围。

主代理补测负向溢出回落、原生整数段后用户回调返回shared long，20项PySum测试通过。
默认空sum现在返回规范小整数0，较旧实现的临时零盒更贴近CP；空显式start及回调结果身份保持。
此前“9项参数偏差”是将9项旧测试与已登记的两项参数偏差混淆，按semantics §4.15原记录解释。

主代理真实JIT/NativeAOT/隐藏输入空PATH中文搬迁及NumPy int64/float64混合回退双流通过。
原始6项1warm10：generators335.88→308.65ms，独立分配169262928→145263304字节；
其余保护项无明显回退。全26项与完整门禁继续验收，候选未提交dev。
