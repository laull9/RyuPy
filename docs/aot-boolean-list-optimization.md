# AOT 布尔列表紧凑存储优化与候选事实

2026-11-15。当前为实现小竖切候选；主代理负责后续全套 review、NativeAOT 验证与 gate。

## 方案概述

针对 Python 中形如 `[True] * n` 和 `[False] * n` 的布尔标记数组模式，在 `PyList` 中引入紧凑布尔背板 `List<bool>`，以降低元素连续存储时的内存开销。

## 已实现的竖切边界

1. **存储设计**：
   - 字段互斥：`List<bool>? _booleans` 与 `_items`、`_numeric` 互斥。
   - 准入范围：仅针对 `Count == 1`、元素严格为 `bool`（排除整数 1、truthy 值）、且重复次数正向的新结果生成。
   - 永不重紧凑化：任何已有通用 `_items` 对象列表在生命周期内永不尝试重新转为紧凑存储。
   - 零新增 unsafe 模型：工厂使用 `CollectionsMarshal.SetCount` 与 `AsSpan().Fill` 分配并填充连续内存，先完整构建再发布，复用私有构造器避免分配空 `_items`。

2. **双入口行为保持**：
   - 普通运算符二元乘法入口 `PySequenceOps.TryRepeat` 与根列表乘法 `list.__mul__`（`PyListProtocols.RepeatCore`）均接入共享紧凑生成工厂。
   - 严格保留各自已有的 `RepeatCount`、参数检查及错误消息，未合并 `__index__` 逻辑。
   - `list.__mul__` 的 `__index__` 回调可能改变源列表（如就地修改元素为异构对象、修改为其他布尔值或清空列表），存储形态的判定严格在回调执行与大小检查之后进行。

3. **操作契约**：
   - 纯布尔读写（`GetItem`、`SetItem` bool、`Append` bool、`Pop`、`Clear`、`Reverse`、`Copy`）保持紧凑形态，返回 `PyOps.BoxBool` 规范单例对象。
   - 异构写（如写入整数 1）、异构追加或公开访问 `Items` 属性时完整物化为 `List<object?>`，并永久维持该实例以保证别名一致性。
   - 活迭代器通过 `ReadIteratorItem` 直读布尔规范单例，不触发物化；若迭代途中发生异构修改，迭代器能直接观察到物化后的新旧内容；耗尽后追加新元素依然保持耗尽。
   - 复杂切片赋值、排序、索引与删除回退至 `Items` 保证协议兼容。

4. **数值与互操作修补**：
   - `TryReadF64` 在遇到布尔列表时直接返回 `false` 且不物化。
   - `AppendFreshF64` 在遇到布尔列表时安全物化为对象列表并追加数值。
   - `ExtendExact`、`ReserveExtend` 和 `Count` 修补了对 `_numeric!` 的非空假设。

## 未验事实与待主代理测量项

1. **字段内存代价**：
   - 每个 `PyList` 对象增加一个 8 字节私有引用字段（`_booleans`），对所有普通列表和微小列表存在固有内存开销。
   - 该开销对全套 26 项基准的全局内存、GC 压力及吞吐的影响必须由主代理进行实测测量，禁止主观预测或声称提速。

2. **NativeAOT 与全套 Gate**：
   - 本竖切仅在 `dotnet build runtime` 及定向测试 `FullyQualifiedName~PyBooleanList|FullyQualifiedName~PyNumericList` 范围内验证通过。
   - 完整的端到端用例（`tests/e2e/phase3/aot_boolean_list_storage.py` 等）、全套 gate、NativeAOT 编译、吞吐 baseline 与 cargo 集成由主代理负责，本子任务未运行以避免与其他并行任务冲突。
