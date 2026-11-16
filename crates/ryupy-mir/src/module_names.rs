//! 模块名字读取独立于表达式分派：typed字段保绑定位，精确删除名字才选择builtin回退。
use crate::expr_builder::ExprBuilder;
use crate::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

impl ExprBuilder<'_> {
    pub(crate) fn build_module_name(&mut self, var: ryupy_hir::ModuleVarId) -> Operand {
        if let Some(value) = self.load_instance_name(var) {
            return value;
        }
        let gid = self.module_vars[&var];
        let global_ty = self.globals[gid].ty.clone();
        let op = Operand::Global(gid);
        // 动态槽位可能是「已解绑」哨兵（`except E as e` 结束后）；
        // 读取时按 CPython 抛 NameError。
        if global_ty == MirType::Dynamic {
            let name = self.hir.vars[var].name.as_str().to_owned();
            let dest = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::Call {
                    callee: Callee::Runtime(
                        if ryupy_hir::uses_deleted_builtin_fallback(&self.hir.vars[var]) {
                            RuntimeFn::LoadDeletedModuleName
                        } else {
                            RuntimeFn::LoadName
                        },
                    ),
                    args: vec![op, Operand::Const(Constant::Str(name))],
                },
            });
            return Operand::Local(dest);
        }
        if let Some(&flag) = self.module_bound_flags.get(&gid) {
            let name = self.hir.vars[var].name.as_str().to_owned();
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CheckGlobalBound),
                args: vec![Operand::Global(flag), Operand::Const(Constant::Str(name))],
            }));
        }
        // 立即冻结本次读取；后续实参/子表达式的调用可改变同一模块槽。
        let frozen = self.alloc_temp(global_ty.clone());
        self.push_stmt(Statement::Assign {
            dest: frozen,
            value: Rvalue::Use(op),
        });
        let op = Operand::Local(frozen);
        // 动态函数里的静态模块变量要装箱。
        if self.types.is_dynamic(self.current_hir_fn) {
            let dest = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::Box {
                    value: op,
                    from: global_ty,
                },
            });
            Operand::Local(dest)
        } else {
            op
        }
    }
}
