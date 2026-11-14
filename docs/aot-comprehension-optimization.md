# AOT 列表推导式私有收集器 ListAppend 直调优化

## 1. 目标与背景

在 RyuPy 当前的 HIR 降级实现（`ryupy-hir/src/comprehensions.rs:lower_comprehension_body`）中，列表推导式（`ListComp`）被编译为一个独立的 `<listcomp>` 隐式嵌套函数：
1. 编译器在入口为其分配用户源码不可拼写的私有局部变量 `.result`；
2. 在 root 首条语句将其初始化为空列表 `[]`（`StmtKind::Assign { target: Local(.result), value: List([]) }`）；
3. 推导式循环体内将元素追加表示为 `.result.append(elt)` 形式的 `ExprStmt`；
4. 循环结束时返回 `.result`。

此前在 MIR 降级阶段（`call_builder::build_method_call`），该方法调用无差别走通用动态方法分派：
- 发射 `RuntimeFn::IsModule` 检查；
- 发射 `RuntimeFn::CaptureModuleTarget` 包装目标；
- 发射 `Rvalue::DynCallMethod` 或 `RuntimeFn::CallCapturedMethod` 运行时调用。

本优化在 `crates/ryupy-mir` 的 `build_method_call` 最前端引入专用 Guard 辅助逻辑，将已形式证明为编译器私有空列表收集器的 `.result.append(elt)` 直接转换为 `RuntimeFn::ListAppend`（对应底层 `PyOps.ListAppend` void），彻底消除推导式循环核心中的动态方法分派与中间临时分配。

---

## 2. 形式化 Guard 证明设计

为防止误伤普通用户对象、自定义类的 `append` 方法或逃逸变量，直调优化设立了严格的 HIR 全量前置 Guard，只有在全部通过后才发射直调；任何一项未通过则立刻返回 `None`，不产生任何下层副作用：

1. **函数身份与编译器来源证明**：
   - `hir_fn.kind == FunctionKind::Nested`；
   - `hir_fn.name.as_str() == "<listcomp>"`；
   - `hir_fn.definition.is_none()`：区别于用户显式编写的 `def` 或 `lambda`（后者在 AST 阶段均由 `function_definition::attach` 挂载了源码来源 `DefinitionInfo`，推导式等隐式函数保留为 `None`）；
   - `!hir_fn.is_generator`：确保非生成器表达式。
2. **接收者身份证明**：
   - `base` 表达式在 HIR 中必须为 `ExprKind::Local(local_id)`；
   - 该局部变量名称严格为不可拼写的 `".result"`（Python 语法禁止标识符以 `.` 开头，用户代码无法构造）；
   - `!local.is_param` 且不在函数参数列表 `params` 中。
3. **闭包逃逸证明**：
   - `hir_fn.closure.cells` 中不包含 `".result"`；
   - `hir_fn.closure.free` 中不包含 `".result"`。
4. **根语句空列表精确初始化证明**：
   - 函数 `hir_fn.body.root` 的第一条语句必须为 `StmtKind::Assign { target: AssignTarget::Local(target_id), value: val_id }`；
   - `target_id == local_id`；
   - `hir_fn.body.exprs[val_id].kind` 严格为 `ExprKind::List(items)` 且 `items.is_empty()`。
5. **单态性与单一定义证明**：
   - 遍历 `hir_fn.body.stmts` 中的所有分配语句；
   - 除 root[0] 首条初始化语句外，整函数体内绝不存在任何对该 `local_id` 的二次赋值（包括普通赋值、序列解包目标 `AssignTarget::Sequence`、`ForEach`/`ForRange` 循环变量绑定以及嵌套定义写入）；
   - 由此在编译期完全证明 `.result` 在整个生命周期内类型单态且严格为内建空列表实例。
6. **调用契约证明**：
   - 方法名严格为 `"append"`；
   - 实参个数严格为 1，且必须为单一位置参数 `CallArg::Positional(elt_expr)`（拒绝关键字实参及 `*` / `**` 解包）。

---

## 3. 求值顺序与语义一致性

- **副作用顺序保证**：严格遵守 Python 规定，先调用 `self.build_expr(base)` 求值 receiver 并装箱，再调用 `self.build_expr(elt_expr)` 求值实参（各求值一次），随后发射 `Statement::Eval(Rvalue::Call { callee: RuntimeFn::ListAppend, args: [receiver, elt] })`；
- **返回值**：方法表达式返回 Python `None` 单例，保持与普通 `append()` 一致的空值语义；
- **独立边界**：不破坏推导式的独立嵌套函数作用域、闭包环境捕获、外部 iterator 先行求值时刻、多层循环与过滤顺序；
- **非目标排除**：未改动 HIR 结构、不包含函数去包装、不包含容量预估、不改变 dict/set/generator 推导式或普通方法调用路径。

---

## 4. 历史统计澄清与 GC 说明

1. **调用频次修正**：
   原分析报告中将基准 `unique` 推导式的元素去重结果（1000 项）误记为调用次数。实际在 `tests/benchmarks/comprehensions.py` 中：
   - 每轮遍历中：
     * `squares`：20,000 次 append；
     * `evens`：10,000 次 append；
     * `grid`：9,900 次 append；
     * 3 个 listcomp 每轮共计 39,900 次 append；
     * 20 轮基准循环累计实际执行 **798,000 次（79.8 万次）** `ListAppend`；
     * 伴随的 `unique` set 推导式每轮遍历 20,000 个元素，20 轮共计 **400,000 次（40 万次）** `set.add`。
2. **CIL locals init 与 GC 根分析**：
   CIL 中 `locals init` 标志位由 runtime/JIT 处理，无法直接在无机器码 liveness 证据的前提下断言局部变量持续存活为 GC 根。本优化严格限定于 MIR 直调生成，不触碰 CIL 标志位或栈帧生命周期。

---

## 5. 验证记录

### 5.1 已执行并通过的验证
1. **定向 MIR 结构与 Guard 测试**：
   执行 `cargo test -p ryupy-mir private_comp_append_tests`：
   - `test_original_benchmark_comprehensions_list_comp_direct_append`：原始基准 3 个 listcomp 均生成 `RuntimeFn::ListAppend`，私有函数内部 `DynCallMethod("append")` 完全降为 0；同时验证伴随的 `<setcomp>` 依然保留 `DynCallMethod("add")`；
   - `test_ordinary_user_append_is_not_intercepted`：普通用户函数 `items.append()` 与自定义类 `c.append()` 严格保留原有的 `DynCallMethod`；
   - `test_guard_rejections_and_edges`：嵌套推导式与多重 for / if 过滤正例；
   - `test_hir_guard_rejection_negative_fake_init`：合法 HIR 篡改负例，非空列表初始化被 Guard 拒绝并回退为 `DynCallMethod`；
   - `test_hir_guard_rejection_negative_multiple_writes`：重赋值负例，被 Guard 拒绝并回退；
   - `test_hir_guard_rejection_negative_cell_capture`：cell 逃逸负例，被 Guard 拒绝并回退。
2. **代码规范与严格 Clippy**：
   执行 `cargo clippy -p ryupy-mir --all-targets -- -D warnings`，零告警通过。
3. **CPython 语义基线**：
   编写端到端测试 `tests/e2e/phase3/aot_list_comp_append.py`（标记 `# ryupy: pending`），在 CPython 下验证输出确定性（求值顺序、晚绑定、短路抛错与全局 rebinding）。

### 5.2 未验项（留待主代理统一推进）
- JIT / 真实 AOT / Benchmark 全套性能测量；
- C# / CoreCLR 驱动端端到端执行验收。
