//! 模块删除仅改变真实绑定：typed槽清绑定位，动态槽清哨兵，实例只删唯一字典。
use crate::build::FunctionBuilder;
use crate::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

impl FunctionBuilder<'_> {
    pub(crate) fn build_module_delete(
        &mut self,
        var: ryupy_hir::ModuleVarId,
        target: ryupy_hir::ExprId,
    ) {
        let name = Operand::Const(Constant::Str(self.hir.vars[var].name.to_string()));
        if let Some(state) = self.module_state {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceDeleteName),
                args: vec![Operand::Local(state), name],
            }));
            return;
        }
        let global = self.module_vars[&var];
        if self.globals[global].ty == MirType::Dynamic {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CheckModuleDeleteValue),
                args: vec![Operand::Global(global), name],
            }));
            let unbound =
                self.expr_builder()
                    .runtime_call(RuntimeFn::MakeUnbound, vec![], MirType::Dynamic);
            self.push_stmt(Statement::SetGlobal {
                dest: global,
                value: unbound,
            });
        } else if let Some(&flag) = self.module_bound_flags.get(&global) {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CheckModuleDelete),
                args: vec![Operand::Global(flag), name],
            }));
            // flag不是value字段，不会被module_bindings::instrument重新标true。
            // 保留typed backing不改变旧别名；引用回收仍服从既有GC边界。
            self.push_stmt(Statement::SetGlobal {
                dest: flag,
                value: Operand::Const(Constant::Bool(false)),
            });
        } else {
            let range = self.hir.functions[self.current_hir_fn].body.exprs[target].range;
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(self.hir.file, range),
                    "模块静态删除目标缺少绑定位",
                )
                .with_code("E0305"),
            );
        }
    }
}
