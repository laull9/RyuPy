# ADR-0198：生成器只读形参挂起写回消除

- 状态：测试候选；产品入口非活跃性实验默认路径验证通过，待主代理统一实测
- 日期：2026-11-15
- 关联：ADR-0182、ADR-0194

## 背景

在 RyuPy 普通生成器降级实现中，生成器实例由工厂函数一次性分配帧数组（`object[] frame`），槽位 0..3 为状态与异常控制槽，槽 4 起按原局部索引存放局部变量。原降级逻辑在每次遇到挂起边（`Terminator::Suspend`）时，全量遍历 `original` 局部列表，对每个局部均生成一条 `ArraySet(frame, slot + 4, value)` 调用将当前局部写回帧数组。

对于基准测试如 `generators.py` 中的 `count_up(n)`，形参 `n` 为只读 `Dynamic` 变量。在工厂函数 `MakeGenerator` 时，`n` 已作为初始实参被写入 `frame[4]`。在函数体生命周期内，`n` 从未被重新赋值，其引用的对象绑定恒定不变。然而在循环迭代产出（外部 3M 产出、yield from 合计约 4M 成功 yield）过程中，每次挂起都会重复向 `frame[4]` 执行冗余的 `ArraySet` 写入。

真实 `count_up` 中 `n` 与 `i` 均为 `Dynamic` 类型，消除挂起写回所节省的是运行时的对象引用写入，而非装箱操作；此前推测的“每次 yield 分配 24 字节或每帧 91 MiB”与事实不符。

## 候选决定

引入生成器只读形参静态分析 helper，识别在整个函数体中引用绑定恒定不变的形参，并在 Suspend 时消除其冗余 ArraySet：

1. **安全准入门禁**：
   - 仅在非产品追踪模式（`!product`）下启用；
   - 仅在非活跃性实验路径（`!optimize_liveness`）下启用，与未提交的临时槽活跃性实验严格隔离；
   - 函数体必须无异常保护区域（`body.regions.is_empty()`，EH 场景必须保持原有全量帧状态以供 catch/retry/finish 恢复）；
   - 形参候选必须属于 `Dynamic` 类型且非闭包 cell（`!decl.is_cell`）；
   - 严格排除编译器合成隐藏参数（`function.env`、`function.dynamic_origin`、`function.module_state` 及 `__` 前缀参数）。

2. **穷尽定义检查**：
   - 枚举函数所有基本块中的所有 `Statement` 与 `Terminator`；
   - 任何 `Statement::Assign.dest` 赋值目标、或 `Terminator::Suspend.input` 恢复定义目标，均被视为重新定义并从候选集合中排除；
   - 对于非形参局部变量，工厂函数初始帧中仅为 `null` 或类型默认值，绝不省略首次写回；
   - 仅绑定不变对象的就地变动（如 `list.append`）不修改对象引用本身，允许保留候选。

3. **MIR 降级配合**：
   - 在 `Plan::take` 前计算只读形参集合；
   - 挂起（`Suspend`）遍历 `original` 局部时，跳过只读形参对应的槽位，不发射 `ArraySet`；
   - 工厂函数初始帧构造保持不变（形参正常填入初始帧槽位）；
   - 每次挂起恢复入口处的 `ArrayGet` 完整保留（确保每次 `$resume` 恢复执行时均能正确加载局部变量引用）；
   - 所有非参数局部变量、状态控制槽及完成清帧路径保持不变。

## 验证状态

1. **结构与 MIR 校验**：
   - 单测覆盖原始 `generators.py count_up`，确认形参 `n` 命中只读优化，Suspend 块消除 `ArraySet(frame, 4, ...)`，同时保留局部 `i` 的写回与恢复入口 `ArrayGet`；
   - 负例覆盖形参重赋值、send 恢复定义、静态 Typed 形参、闭包 cell 形参、异常保护区 (EH)、product 模式及 optimize_liveness 模式，均确认严格拒绝并维持全量写回；
   - 降级后全量 MIR 验证（`crate::validate::validate`）通过。

2. **端到端测试**：
   - 新增 E2E 用例 `tests/e2e/phase3/aot_generator_readonly_params.py`（保留 pending 标记）；
   - 覆盖只读 param 多 yield 读取、可变对象别名跨暂停、参数重赋值、send 覆写、局部首次初始化、闭包 nonlocal 写、yield from、throw/close/finally、locals() 观察等 9 类完整场景；
   - CPython 3.12.13 同次差分通过，待主代理统一实测真实 AOT 与基准性能。


主代理纠正：首次e2e runner显示1 passed，但results.jsonl实际为pending，locals()不支持且finally依赖隐式销毁。
已保留独立observation pending原反例；主用例改显式close，并增加send直接覆写形参与nonlocal直接修改形参。
新增真实源捕获参数结构断言，当前9项定向MIR通过，原“完整CP差分通过”结论撤回，待新验证。
