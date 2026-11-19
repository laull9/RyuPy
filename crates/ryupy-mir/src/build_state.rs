//! 单函数构建状态；字段由按语义拆分的构建模块共同使用。
use super::*;

/// 单个函数的 MIR 构建器。
///
/// 字段对外开放到 crate 内，是为了把「循环」「异常区域」这些小节的构建逻辑拆到
/// 独立文件（`build_loops.rs` / `build_try.rs`），避免单文件过长。
pub(crate) struct FunctionBuilder<'a> {
    pub(crate) trace_product: bool,
    pub(crate) trace_expressions: Option<Vec<crate::trace_lowering::ExpressionOrigin>>,
    pub(crate) module_state: Option<MirLocalId>,
    pub(crate) hir: &'a Module,
    pub(crate) types: crate::core_types::CoreTypes<'a>,
    pub(crate) fn_map: &'a FxHashMap<ryupy_hir::FuncId, FnId>,
    /// 每个用户函数的函数对象静态槽位（调用一等函数值 / 运行时绑定用）。
    pub(crate) fn_globals: &'a FxHashMap<ryupy_hir::FuncId, crate::mir::GlobalId>,
    /// 每个函数默认值数组的静态槽位（没有默认值为 `None`）。
    pub(crate) fn_defaults: &'a FxHashMap<ryupy_hir::FuncId, Option<crate::mir::GlobalId>>,
    /// 只由普通def成功发布、与对应code一致的私有实例槽。
    pub(crate) fn_sources: &'a FxHashMap<ryupy_hir::FuncId, crate::mir::GlobalId>,
    /// HIR 类 → MIR 类定义。
    pub(crate) class_map: &'a FxHashMap<ryupy_hir::ClassId, crate::mir::ClassDefId>,
    /// HIR 类 → 类对象静态槽位（表达式构建直接使用）。
    pub(crate) hir_class_globals: &'a FxHashMap<ryupy_hir::ClassId, crate::mir::GlobalId>,
    /// 异常类 → CLR 工厂委托的静态槽位。
    pub(crate) class_factories: &'a FxHashMap<crate::mir::ClassDefId, crate::mir::GlobalId>,
    /// HIR 模块变量 → 静态槽位。
    pub(crate) module_vars: &'a FxHashMap<ModuleVarId, crate::mir::GlobalId>,
    /// 全部静态槽位（读取槽位类型做装箱/拆箱）。
    pub(crate) module_bound_flags: &'a FxHashMap<crate::mir::GlobalId, crate::mir::GlobalId>,
    pub(crate) globals: &'a Arena<GlobalDecl>,
    pub(crate) current_hir_fn: ryupy_hir::FuncId,
    pub(crate) locals: Arena<LocalDecl>,
    pub(crate) blocks: Arena<BasicBlock>,
    pub(crate) entry: BlockId,
    pub(crate) current_bb: BlockId,
    pub(crate) local_map: FxHashMap<HirLocalId, MirLocalId>,
    pub(crate) function_locals_cache: Option<MirLocalId>,
    pub(crate) assigned_locals: FxHashSet<HirLocalId>,
    pub(crate) reported_unbound: FxHashSet<HirLocalId>,
    pub(crate) loop_stack: Vec<(BlockId, BlockId)>, // (continue_bb, break_bb)
    pub(crate) diagnostics: Vec<Diagnostic>,
    /// 已构建的异常区域。
    pub(crate) regions: Vec<Region>,
    pub(crate) direct_globals_contracts: Vec<crate::DirectGlobalsContract>,
    pub(crate) source_handler_contracts: Vec<crate::SourceHandlerContract>,
    /// 当前嵌套的受保护区域起点（块下标），最内层在末尾。
    pub(crate) protected_stack: Vec<usize>,
    /// 当前正在构建的 finally 上下文，最内层在末尾。
    pub(crate) finally_stack: Vec<FinallyCtx>,
    /// `finally` 状态机的当前动作码；只有函数里出现过 finally 时才分配。
    pub(crate) flow_kind: Option<MirLocalId>,
    /// 函数级动作表：动作码 → 跳出目标。0/1/2 是内建动作（正常/异常/返回）。
    pub(crate) actions: Vec<BlockId>,
    /// 统一返回块。
    pub(crate) epilogue: Option<BlockId>,
    /// 需要跳到统一返回块的块。
    pub(crate) epilogue_jumps: Vec<BlockId>,
    /// 本函数保存返回值的局部变量。
    pub(crate) retval: Option<MirLocalId>,
    /// 返回类型。
    pub(crate) ret_ty: MirType,
    /// traceback 帧深度。
    pub(crate) frame_depth: Option<MirLocalId>,
    /// 闭包环境形参（M3.0）：`Free` 读取 / `nonlocal` 写入都要经过它。
    pub(crate) env: Option<MirLocalId>,
    pub(crate) dynamic_owner: Option<MirLocalId>,
    /// 当前正在执行的类体原始命名空间。
    pub(crate) class_namespace: Option<Operand>,
    pub(crate) lexical_namespace: bool,
    /// 当前类体定义点的 cell，与 namespace 对象、module context 分开。
    pub(crate) class_cell: Option<(ryupy_hir::ClassId, Operand)>,
    pub(crate) lexical_cell_stack: Vec<(ryupy_hir::ClassId, Operand)>,
    pub(crate) class_cells: Vec<(crate::ClassDefId, crate::ClassCellDefinition)>,
}
