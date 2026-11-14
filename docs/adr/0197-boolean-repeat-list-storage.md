# ADR-0197：布尔重复列表紧凑存储

- 状态：已接受；有限默认，26项配对/搬迁、对象桥和完整门禁通过
- 日期：2026-11-15
- 关联：ADR-0039、ADR-0068、ADR-0182

## 背景

在 Python 应用中常见诸如 `[True] * n` 或 `[False] * n` 的布尔标记数组模式。通用列表默认使用 `List<object?>` 存储引用，每个布尔槽位占用8字节引用；重复元素共享同一布尔单例，不逐元素装箱。

## 候选决定

引入 `PyList` 的布尔紧凑背板支持：
1. **私有字段互斥存储**：新增私有字段 `List<bool>? _booleans`，与已有 `_items`（通用对象）和 `_numeric`（数值槽）互斥。每个 `PyList` 实例新增一个 8 字节引用字段。
2. **保守紧凑准入**：仅当源列表 `Count == 1`、元素为 exact bool（严格 `bool` 类型，严禁 truthy 或 `== 1` 误压整数），且重复次数大于 0 时，才在生成新列表时启用紧凑存储。所有已有通用对象列表永不重新紧凑化。
3. **双入口共享工厂**：
   - 普通运算符二元乘法走 `PySequenceOps.TryRepeat`；
   - 根列表乘法 `list.__mul__` 走 `PyListProtocols.RepeatCore`。
   - 两处入口各自保留原有的 `RepeatCount` 与参数错误合同（严禁顺手合并 `__index__` 行为）。根乘法的 `__index__` 回调在判定前执行，紧凑判定严格在回调与大小校验之后。
4. **物化语义与别名一致性**：
   - 纯布尔读写（`GetItem`、`SetItem` 写入 bool、`Append` 追加 bool、`Pop`、`Clear`、`Reverse`、`Copy`）保持紧凑，读取时返回规范单例 `PyOps.BoxBool`。
   - 异构写（如写入整数 1 或字符串）、追加非布尔对象、或公开访问 `Items` 属性时，先完整构建包含规范单例对象的 `List<object?>`，成功后发布给 `_items` 并清除 `_booleans`。物化后永久维持该 `List<object?>` 实例，保证 C# 别名和公开 `Items` 同步。
   - 活迭代器通过 `ReadIteratorItem` 直读规范布尔单例，不触发物化；若迭代途中发生异构写入，迭代器能实时观察到物化后的新旧内容。

## 验收与取舍

25项C#、CP/JIT/NativeAOT/隐藏输入搬迁、NumPy/operator双向同步以及完整make check通过。
原始26项配对：sieve170.77→138.89ms、RSS52.78→24.02MiB；matmul25.16→25.40ms，
nbody246.76→250.67ms；新增字段使records多分配480152字节。接受这些有限代价，默认启用。
资源耗尽时刻和物化分配几何不做逐分配相等承诺，常用结果/身份/别名/副作用保持。
详细全套口径、负结果和修正见[布尔列表专项](../aot-boolean-list-optimization.md)。
