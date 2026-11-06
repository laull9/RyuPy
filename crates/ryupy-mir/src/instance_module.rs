//! 实例模块冷入口及显式字典访问；不建立任何全局副本或ambient状态。
use crate::build::FunctionBuilder;
use crate::expr_builder::ExprBuilder;
use crate::{Constant, LocalDecl, LocalId, MirType, Operand, RuntimeFn};

impl FunctionBuilder<'_> {
    pub(crate) fn guard_instance_import(&mut self) {
        if let Some(state) = self.module_state {
            self.push_stmt(crate::Statement::Eval(crate::Rvalue::Call {
                callee: crate::Callee::Runtime(RuntimeFn::InstanceImportGuard),
                args: vec![Operand::Local(state)],
            }));
        }
    }
    pub(crate) fn add_module_state(&mut self, params: &mut Vec<LocalId>) {
        if !self.hir.info.instance_globals {
            return;
        }
        let state = self.locals.alloc(LocalDecl {
            ty: MirType::Dynamic,
            name: Some("__module_state".into()),
            is_cell: false,
        });
        self.module_state = Some(state);
        params.push(state);
    }
}
impl ExprBuilder<'_> {
    pub(crate) fn load_instance_name(&mut self, var: ryupy_hir::ModuleVarId) -> Option<Operand> {
        let state = self.module_state?;
        Some(self.runtime_call(
            RuntimeFn::InstanceLoadName,
            vec![
                Operand::Local(state),
                Operand::Const(Constant::Str(self.hir.vars[var].name.to_string())),
            ],
            MirType::Dynamic,
        ))
    }
}
