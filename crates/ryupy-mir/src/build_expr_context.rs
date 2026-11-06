//! 将函数构建上下文借给表达式构建器，集中维护共享槽位与类命名空间。
use crate::build::FunctionBuilder;
use crate::expr_builder::ExprBuilder;
impl FunctionBuilder<'_> {
    // ───────────────────── 帧记录 / 行号 / 统一返回 ─────────────────────
    pub(crate) fn expr_builder(&mut self) -> ExprBuilder<'_> {
        ExprBuilder {
            module_state: self.module_state,
            hir: self.hir,
            types: self.types,
            fn_map: self.fn_map,
            fn_globals: self.fn_globals,
            fn_defaults: self.fn_defaults,
            fn_sources: self.fn_sources,
            class_globals: self.hir_class_globals,
            module_vars: self.module_vars,
            globals: self.globals,
            module_bound_flags: self.module_bound_flags,
            current_hir_fn: self.current_hir_fn,
            env: self.env,
            function_locals_cache: &mut self.function_locals_cache,
            class_namespace: self.class_namespace.clone(),
            class_cell: self.class_cell.clone(),
            locals: &mut self.locals,
            blocks: &mut self.blocks,
            current_bb: &mut self.current_bb,
            local_map: &self.local_map,
            assigned_locals: &mut self.assigned_locals,
            reported_unbound: &mut self.reported_unbound,
            diagnostics: &mut self.diagnostics,
        }
    }
}
