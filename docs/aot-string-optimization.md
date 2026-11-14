> 主代理复核（2026-11-15 修订）：第一版草稿因缺乏 LHS 精确字符串证明、零次迭代未绑定保护以及 header/latch 白名单而被驳回。现已重构为极窄且可证明正确的模块全局候选，仅在 `#[cfg(test)]` 下供单测与验证，不进入默认流水线。下文已纠正“零分配”及“异常映射”等断言，未编译、未默认启用。

# NativeAOT 有界字符串累加优化设计与实施规范

2026-11-15：实现子代理完成字符串累加优化（AOT String Accumulator）第二版修复。本文记录优化背景、模块观察语义、修复清单、安全证明边界及待验证项。

## 1. 现状与瓶颈分析

在首轮 26 项真实基线测量中，字符串拼接在以下两项基准中构成了极大的耗时与内存分配瓶颈：
- **`str_concat`**：10 万次字符串 `+= "ab"` 累加及 join 对照，运行耗时约 **1065ms**，累计堆分配高达 **19GiB**；
- **`containers`**：20 万列表追加 + 10 万字典读写 + 5 万次 `text = text + "b"` 拼接，运行耗时约 **166ms**，累计堆分配约 **2.4GiB**。

### 根因剖析
在 Python 语义中，`str` 是不可变对象。每次执行 `+` 或增量 `+=`，如果没有专门的原地复用特化，都会申请一块全新的内存并将前缀与后缀完整拷贝进去。对于 $N$ 次循环，累加产生的总内存分配量为 $O(N^2)$。10 万次长度为 2 的追加累计复制达：
$$\sum_{k=1}^{100000} 2k \approx 10^{10} \text{ 字节} \approx 9.31 \text{ GiB}$$
加上运行时对象头与中间包装，累计分配达 19GiB，导致频繁触发 GC，严重拖慢 NativeAOT 吞吐。

## 2. 第一版审查驳回根因与第二版修复清单

第一版实现存在严重语义盲区，已被主代理驳回：
1. **LHS 自定义协议与中间 globals 泄漏**：仅检查 RHS 为字面量，未证明 LHS 类型。若 LHS 是定义了 `__add__` 或 `__iadd__` 的用户对象，holder fallback 会无条件以 `inplace: true` 执行 `__iadd__`，破坏原非 inplace 语义，且在回调方法中通过 `globals()` 会观察到未更新的旧全局变量；
2. **零次循环 NameError 时序破坏**：若变量在循环前未绑定且循环 0 次（如 `for i in range(0): text += "a"`），Python 规范不应抛出错误。若在 preheader 提前执行 `LoadName`，会导致多报 `NameError`；
3. **缺少全环路指令白名单**：循环内仅粗略过滤少数 Call，无法阻止 `DynCallValue`、`DynCallMethod`、`InstanceZeroCall`、`DynUnary`、`Truthy`、`New` 或字段写入等触发动态用户协议；header 与 latch 同样可能包含动态条件与回调；
4. **SSA 与真实管线失配**：旧 matcher 假设 RHS 字面量紧挨加法，遇到实际编译器生成的 `Box` 语句直接 break，且扫描可能发生用后定义；
5. **虚假断言与手搓测试过时**：旧文档断言“循环体0分配”（实际上 `StringBuilder` 会扩容和分块分配）以及“OverflowException对齐Python MemoryError”；旧测试手搓 MIR 结构，字段过时且未过验证器。

## 3. 第二版严格安全边界与设计

### 3.1 极窄竖切准入条件
1. **仅限模块全局槽位（`GlobalId`）**：暂时关闭通用 local 分支，杜绝局部临时量生存期遗漏；
2. **LHS 精确内建字符串与绑定证明**：
   - 必须由 `preheader` 最终直接赋值字符串字面量（`Statement::SetGlobal`）；
   - 该赋值之后至 `Goto(header)` 之间无任何重写、无函数调用与副作用；
   - 证明 LHS 为确凿内建 `str` 且已绑定，消除了用户类回调与零次迭代 `NameError` 时序破坏；
3. **全环路指令严格白名单**：
   - `header`：仅限原生 `i64` / `bool` 循环计数比较与分支；
   - `latch`：仅限原生计数器步进；
   - `body`：除单条累加切片（`LoadName -> [Box] -> DynBinary -> SetGlobal`）与计数变量发布外，严禁任何动态指令；
4. **完整递归读者与唯一读写**：
   - 实现完整覆盖所有 `Rvalue` 与 `Operand` 的递归遍历，确保目标槽在循环内有且仅有单次 LoadName 与单次 SetGlobal。

### 3.2 展开改写（Expansion）
1. **前驱块（preheader）**：
   - 提升右操作数字面量单次装箱；
   - 使用 preheader 中已证明确凿初值直接创建内部 holder（`NumericAccumulatorCreate`）；
2. **循环体（body）**：
   - 将原切片替换为单条 `Eval(NumericAccumulatorAdd(holder, rhs_boxed))`；
3. **循环出口（exit）**：
   - 首部调用 `NumericAccumulatorValue(holder)` 物化规范不可变字符串；
   - 立即通过 `SetGlobal` 写回模块槽位，后续语句立即可见完整终值。

## 4. 交付文件清单

| 文件路径 | 说明 |
| --- | --- |
| [string_accumulator.rs](../crates/ryupy-mir/src/passes/string_accumulator.rs) | 优化 Pass 入口定义与函数级合法性过滤（受限于 `#[cfg(test)]`） |
| [string_accumulator_match.rs](../crates/ryupy-mir/src/passes/string_accumulator_match.rs) | 严格自然循环拓扑、LHS 初值确凿证明与全环路指令白名单 |
| [string_accumulator_expand.rs](../crates/ryupy-mir/src/passes/string_accumulator_expand.rs) | 安全 holder 初始化、循环体追加与出口物化 |
| [string_accumulator_tests.rs](../crates/ryupy-mir/src/passes/string_accumulator_tests.rs) | 基于真实源编译器管线的结构测试与 8 项完备反例 |
| [mod.rs](../crates/ryupy-mir/src/passes/mod.rs) | 在 `#[cfg(test)]` 下注册候选模块供单测调用，**未**接入默认流水线 |
| [0193-bounded-string-accumulator.md](adr/0193-bounded-string-accumulator.md) | 架构设计决策记录 ADR-0193（修订版） |
| [aot-string-optimization.md](aot-string-optimization.md) | 本文档 |

## 5. 当前状态与未跑项说明

为配合基准代理独占环境进行计时测量，本实现代理严格遵守约束：
- **未执行 `cargo build`、`dotnet build`、`cargo test` 或任何性能测试**；
- 候选仅在 `#[cfg(test)]` 隔离注册，默认管线保持零变动；
- 所有正确性依赖源码级演绎推理与已有编译器/运行时合同。

待统一验证项（待主代理协调后推进）：
1. 运行 `cargo test -p ryupy-mir -- string_accumulator_tests` 验证所有正反用例与验证器；
2. 在基准代理独立环境中进行 `str_concat` 与 `containers` 的 AOT 差分验证；
3. 经主代理与异构审查验收后，再评估是否晋升到默认流水线。
