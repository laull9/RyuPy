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

impl ExprBuilder<'_> {
    pub(crate) fn direct_globals_call(
        &mut self,
        call: ryupy_hir::ExprId,
        callee: ryupy_hir::ExprId,
    ) -> Option<Operand> {
        if !self.hir.info.instance_capabilities.direct_globals
            || !self.hir.functions[self.current_hir_fn]
                .body
                .direct_globals_calls
                .contains(&(call, callee))
        {
            return None;
        }
        let state = self.module_state?;
        // 求值实际callee一次；参数/局部遮蔽同样走真实动态调用，不按名称直取dict。
        let value = self.build_expr(callee);
        let ty = self.ty_of(callee);
        let callable = self.coerce(value, &ty, &MirType::Dynamic);
        let snapshot = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(crate::Statement::Assign {
            dest: snapshot,
            value: crate::Rvalue::Use(callable.clone()),
        });
        let result = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(crate::Statement::Assign {
            dest: result,
            value: crate::Rvalue::Call {
                callee: crate::Callee::Runtime(RuntimeFn::InstanceDirectGlobals),
                args: vec![Operand::Local(state), Operand::Local(snapshot)],
            },
        });
        self.direct_globals_contracts
            .push(crate::DirectGlobalsContract {
                source_call: call.into_raw().into_u32(),
                source_callee: callee.into_raw().into_u32(),
                context: state,
                snapshot,
                result,
                callee_value: callable,
            });
        Some(Operand::Local(result))
    }
}
