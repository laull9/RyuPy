> 主代理复核（2026-11-15 修订）：第一版草稿因缺乏 LHS 精确字符串证明、零次迭代未绑定保护以及 header/latch 白名单而被驳回。现已重构为极窄且可证明正确的模块全局候选，仅在 `#[cfg(test)]` 下供单测与验证，不进入默认流水线。下文已剔除“循环体0分配”及“OverflowException等同MemoryError”等不实断言。

# ADR-0193：有界字符串累加器与顶层模块观察语义

- 状态：受限测试候选（仅在 `#[cfg(test)]` 供测试调用，待主代理与基准代理统一验证，不进默认流水线）
- 日期：2026-11-15
- 决策来源：NativeAOT 速度优先优化；消除 `str_concat`（1065ms/19GiB 累计分配）与 `containers`（166ms/2.4GiB 累计分配，5 万次非 inplace 拼接）的二次方堆复制热点
- 涉及组件：`ryupy-mir`（新建 `string_accumulator` pass 家族，暂限 `#[cfg(test)]`）、`RyuPy.Runtime`（复用 `PyNumericAccumulator` 内部 StringBuilder 延迟物化）

## 背景与问题

在 RyuPy 首轮 26 项差分基线测量中，`str_concat`（10 万次字符串 `+=` 拼接与 join 对照）与 `containers`（包含 5 万次 `text = text + "b"` 拼接）消耗了巨额运行时间与堆内存分配（分别达 19GiB 与 2.4GiB）。其本质原因是 Python 原生字符串拼接每轮均产生全新的托管 `System.String` 实例，导致 $O(N^2)$ 的内存复制与 GC 压力。

虽然运行时 `PyNumericAccumulator.cs` 已经具备针对精确 `System.String` 的 `StringBuilder` 延迟物化能力，但现有的中端 `NumericAccumulator` 无法优化上述两个基准：
1. **`signature_global` 门禁**：`NumericAccumulator` 显式以 `function.signature_global.is_none()` 拒绝模块入口 `ModuleMain`，而两个基准的累加循环均位于顶层模块；
2. **`entry preheader` 严格 5 块**：现有匹配器强制要求 `preheader == function.body.entry`，而 `containers` 的字符串循环位于第 4 个循环，前置包含列表与字典构造，无法命中；
3. **`inplace` 限制**：现有匹配器强制要求 `inplace: true`，而 `containers` 源码使用非增量形式 `text = text + "b"`（`inplace: false`）；
4. **模块槽位与可观察语义**：模块级变量在 MIR 中通过静态槽位 `GlobalId` 与 `RuntimeFn::LoadName` / `Statement::SetGlobal` 交互，并非普通局部变量 `LocalId`。若盲目放开签名门禁，可能导致模块中间状态泄露、别名失效或动态回调在未物化状态下观察到过期值的严重语义错误。

## 决定

不直接修改或放开既有 `NumericAccumulator`，新建独立且极窄的 `StringAccumulator` 优化 Pass（位于 `crates/ryupy-mir/src/passes/string_accumulator.rs` 等模块，当前仅注册于 `#[cfg(test)]`），并确立以下严格可证明的安全准入合同：

### 1. 目标覆盖范围（极窄竖切）
- 暂时**关闭**通用局部变量（`LocalId`）与参数前缀支持，**仅支持顶层模块全局槽位（`GlobalId`）**；
- 支持任意自然循环前驱块（`preheader`），不要求 `preheader == entry`，允许前置列表/字典构造语句；
- 仅当 LHS 证明为精确字符串时，接纳 `+=`（`inplace: true`）与 `= +`（`inplace: false`）。

### 2. 严格安全边界与证明条件
- **LHS 精确内建字符串与绑定证明**：
  - 必须由 `preheader` 最终直接赋值字符串字面量（`Statement::SetGlobal { dest: gid, value: Const(Str) | Local(boxed_str) }`）；
  - 该定值之后至进入循环头（`header`）之间，禁止包含任意写入、外部函数调用或可产生副作用的动态操作；
  - 彻底杜绝 LHS 为自定义用户类（带有 `__add__` / `__iadd__` 回调）所导致的中间 globals 泄露与非 inplace 语义篡改；
  - 彻底杜绝 `range(0)` 零次循环时因提前执行 `LoadName` 对未绑定变量多抛 `NameError` 的时序破坏。
- **全环路指令严格白名单**：
  - `header`：仅允许纯原生 `i64` / `bool` 循环计数控制计算，杜绝任意 `Truthy`、动态比较或回调；
  - `latch`：仅允许纯原生计数器步进计算；
  - `body`：除累加切片（`LoadName -> [Box] -> DynBinary -> SetGlobal`）与循环变量发布外，严禁任意 `DynCall`、`InstanceZeroCall`、`DynUnary`、`New` 或字段写操作；
- **无模块字典逃逸**：
  - 函数内无异常保护区（`regions` 为空）、无生成器挂起点、无 `direct_globals_contracts`，且不包含 `globals()` 反射调用。
- **循环出口物化保证**：
  - 循环出口（`exit`）首部立即调用 `NumericAccumulatorValue`，将延迟缓冲区物化为不可变 `System.String`，并写回目标全局槽位。

### 3. 内存与异常合同校准
- **内存分配**：`StringBuilder` 在追加过程中存在容量成倍扩容与分块重分配，并非“循环内0分配”，但将 $O(N^2)$ 的全量字符串复制降为平摊 $O(N)$ 的缓冲追加，总分配量显著收敛；`MirType::Str` 的 Box 在 CIL codegen 中本来即为 no-op；
- **异常边界**：运行时 `checked` 长度溢出触发 CLR `OverflowException`，不作虚假 Python `MemoryError` 等价断言，未由专门异常翻译器映射。

## 影响与后果

1. **安全性**：
   - 在完整观察合同下保持极窄候选，杜绝一切潜在的回调逃逸与语义偏差；
   - 处于 `#[cfg(test)]` 隔离状态，不会影响已有生产流水线。
2. **待验证项**：
   - 待主代理统一安排基准代理独占编译与端到端差分测试。
