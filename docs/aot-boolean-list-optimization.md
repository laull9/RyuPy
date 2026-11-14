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


## 主代理正确性复核

移除未使用的内部CreateBoolean入口，补充公开Items直接物化后的永久别名、原地重复与异构自引用追加测试。
新增100000元素真实构造分配上界（100000至200000字节，排除8B引用数组）及完整迭代后仍紧凑断言。
合计25项定向测试通过；编译时仍有仓库既有xUnit分析器警告，本切未新增警告。
固定CPython3.12.13/真实JIT/NativeAOT、隐藏源码与构建目录、空PATH中文搬迁完整双流一致。
随包NumPy2.3.5转换布尔列表、operator.setitem对布尔/异构值的双向同步及搬迁也通过。
主用例已转正，原运算符自定义__index__计数缺口保留独立pending反例，见semantics §4.18。
全26项性能及完整门禁继续验收，未声明最终默认准入。


## 第一版26项实测与修正候选

第一版全部26项输出/退出及搬迁通过。sieve177.29→139.88ms，RSS52.73→23.98MiB，
CP78.20ms/30.03MiB；但matmul25.92→30.62ms，五次采样均上升，必须修正后再评估。
其余如records135.00→135.00ms、RSS50.84→50.95MiB，保留字段代价实测。
主代理移除TryReadF64中冗余的bool提前判断（_items模式匹配已安全返回false），
ReadCheckedNumeric保留原“同一守卫真边、无回调/写入”前置合同，不额外增加不可能命中的bool分支。
该修正是否解决matmul回退尚待新配对，第一版不能准入默认。


## 三臂重点复测纠正

前次精简守卫仍有matmul25.32→29.53ms的稳定回退，未执行完整门禁。
调研报告Alloc列误读不存在的managed_alloc_bytes并默认0；真实原始字段为alloc_bytes，
matmul基线5975808字节、修正5979896字节。原始JSON保留正确字段，零分配论断撤回。
主代理继续验证Count数值优先分流及显式内联，尚未准入默认。

主代理Count数值优先分流并显式内联后，新的三臂1warm10对照matmul为基线25.80ms、首版30.55ms、Count修正26.63ms。
修正对基线仍增加3.22%；25项专项通过，真实AOT输出及搬迁一致。全套继续验收。
