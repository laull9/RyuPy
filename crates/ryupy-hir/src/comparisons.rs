//! 链式比较保留有序操作数与操作码；共享中间值的求值和短路留给 MIR 控制流。
use crate::expr::ExprLowerCtx;
use crate::hir::ExprKind;
use crate::operator::map_cmp_op;
use ryupy_base::{Diagnostic, Ranged, Span};
use ryupy_syntax::ast;
/// Python 比较操作码；成员/身份测试与富比较均显式保存。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum CmpOp {
    Eq,
    NotEq,
    Lt,
    LtE,
    Gt,
    GtE,
    /// `in`：成员判断，走容器的 `__contains__`。
    In,
    /// `not in`。
    NotIn,
    /// `is`：引用身份。
    Is,
    /// `is not`。
    IsNot,
}

impl ExprLowerCtx<'_> {
    pub(crate) fn lower_compare(&mut self, cmp: &ast::ExprCompare) -> ExprKind {
        if cmp.ops.is_empty() || cmp.operands.len() != cmp.ops.len() + 1 {
            return ExprKind::Missing;
        }
        let mut ops = Vec::new();
        for raw in &cmp.ops {
            let Some(op) = map_cmp_op(*raw) else {
                self.diagnostics.push(
                    Diagnostic::error(Span::new(self.file, cmp.range()), "暂不支持该比较运算符")
                        .with_code("E0100"),
                );
                return ExprKind::Missing;
            };
            ops.push(op);
        }
        let operands: Vec<_> = cmp
            .operands
            .iter()
            .map(|operand| self.lower_expr(operand))
            .collect();
        if ops.len() == 1 {
            ExprKind::Compare {
                op: ops[0],
                lhs: operands[0],
                rhs: operands[1],
            }
        } else {
            ExprKind::CompareChain { operands, ops }
        }
    }
}
