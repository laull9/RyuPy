//! 链式比较的显式控制流：共享中间操作数、只对中间结果求真，最终值保持对象身份。
use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::{Operand, Rvalue, Statement, Terminator};
use ryupy_hir::{CmpOp, ExprId};
impl ExprBuilder<'_> {
    pub(crate) fn build_compare_chain(
        &mut self,
        operands: &[ExprId],
        ops: &[CmpOp],
        expression: ExprId,
    ) -> Operand {
        // HIR 正常构造保证数量；损坏输入仍报告诊断，不索引越界或 panic。
        if operands.len() != ops.len() + 1 || ops.is_empty() {
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(
                        self.hir.file,
                        self.hir.functions[self.current_hir_fn].body.exprs[expression].range,
                    ),
                    "链式比较的操作数数量不匹配",
                )
                .with_code("E0399"),
            );
            return self.runtime_call(
                crate::RuntimeFn::DynNone,
                Vec::new(),
                crate::MirType::Dynamic,
            );
        }
        let result_ty = self.ty_of(expression);
        let destination = self.alloc_temp(ty_to_mir(&result_ty));
        let join = self.new_block();
        let mut left_ty = self.ty_of(operands[0]);
        let initial = self.build_expr(operands[0]);
        let mut left = self.freeze_comparison_operand(initial, &left_ty);
        for (index, op) in ops.iter().enumerate() {
            let right_ty = self.ty_of(operands[index + 1]);
            let value = self.build_expr(operands[index + 1]);
            let right = self.freeze_comparison_operand(value, &right_ty);
            let result = self.compare_values(
                *op,
                left,
                right.clone(),
                &left_ty,
                &right_ty,
                result_ty.is_dynamic(),
            );
            self.assign_value(destination, result, &result_ty);
            if index + 1 == ops.len() {
                self.blocks[*self.current_bb].terminator = Terminator::Goto(join);
            } else {
                let condition = self.to_bool_operand(Operand::Local(destination), &result_ty);
                let next = self.new_block();
                self.blocks[*self.current_bb].terminator = Terminator::Branch {
                    cond: condition,
                    then_: next,
                    else_: join,
                };
                *self.current_bb = next;
            }
            left = right;
            left_ty = right_ty;
        }
        *self.current_bb = join;
        Operand::Local(destination)
    }
    fn freeze_comparison_operand(&mut self, value: Operand, ty: &ryupy_ty::Ty) -> Operand {
        let destination = self.alloc_temp(ty_to_mir(ty));
        self.push_stmt(Statement::Assign {
            dest: destination,
            value: Rvalue::Use(value),
        });
        Operand::Local(destination)
    }
}
