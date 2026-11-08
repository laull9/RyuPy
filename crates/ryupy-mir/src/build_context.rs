//! 函数 MIR 构建上下文初始化；保持局部、环境、类定义 cell 的身份分别保存。
use crate::build::{FunctionBuilder, ty_to_mir};
use crate::{BasicBlock, FnId, GlobalDecl, Terminator};
use la_arena::Arena;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_hir::{Module, ModuleVarId};
use ryupy_ty::ModuleTypes;
impl<'a> FunctionBuilder<'a> {
    #[allow(clippy::too_many_arguments)]
    pub(crate) fn new(
        hir: &'a Module,
        types: &'a ModuleTypes,
        fn_map: &'a FxHashMap<ryupy_hir::FuncId, FnId>,
        fn_globals: &'a FxHashMap<ryupy_hir::FuncId, crate::mir::GlobalId>,
        fn_defaults: &'a FxHashMap<ryupy_hir::FuncId, Option<crate::mir::GlobalId>>,
        fn_sources: &'a FxHashMap<ryupy_hir::FuncId, crate::mir::GlobalId>,
        class_map: &'a FxHashMap<ryupy_hir::ClassId, crate::mir::ClassDefId>,
        hir_class_globals: &'a FxHashMap<ryupy_hir::ClassId, crate::mir::GlobalId>,
        class_factories: &'a FxHashMap<crate::mir::ClassDefId, crate::mir::GlobalId>,
        module_vars: &'a FxHashMap<ModuleVarId, crate::mir::GlobalId>,
        module_bound_flags: &'a FxHashMap<crate::mir::GlobalId, crate::mir::GlobalId>,
        globals: &'a Arena<GlobalDecl>,
        current_hir_fn: ryupy_hir::FuncId,
    ) -> Self {
        let mut blocks = Arena::default();
        let entry = blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: Terminator::Unreachable,
        });
        Self {
            trace_product: false,
            trace_expressions: None,
            module_state: None,
            hir,
            types,
            fn_map,
            fn_globals,
            fn_defaults,
            fn_sources,
            class_map,
            hir_class_globals,
            class_factories,
            module_vars,
            module_bound_flags,
            globals,
            current_hir_fn,
            locals: Arena::default(),
            blocks,
            entry,
            current_bb: entry,
            local_map: FxHashMap::default(),
            function_locals_cache: None,
            assigned_locals: FxHashSet::default(),
            reported_unbound: FxHashSet::default(),
            loop_stack: Vec::new(),
            diagnostics: Vec::new(),
            regions: Vec::new(),
            source_handler_contracts: Vec::new(),
            protected_stack: Vec::new(),
            finally_stack: Vec::new(),
            flow_kind: None,
            retval: None,
            actions: Vec::new(),
            epilogue: None,
            epilogue_jumps: Vec::new(),
            ret_ty: ty_to_mir(&types.sigs[current_hir_fn].ret),
            frame_depth: None,
            env: None,
            dynamic_owner: None,
            class_namespace: None,
            class_cell: None,
            class_cells: Vec::new(),
        }
    }
}
