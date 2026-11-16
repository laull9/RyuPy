//! 无过滤、单层列表推导式的有界预留；只读取隐藏迭代器参数，容量计算不调用用户协议。
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, MirType, Operand, RuntimeFn, Rvalue, Statement};
use ryupy_hir::{AssignTarget, ExprId, ExprKind, StmtKind};
impl ExprBuilder<'_> {
    pub(crate) fn private_comp_capacity(&mut self, id: ExprId) -> Option<Operand> {
        let f = &self.hir.functions[self.current_hir_fn];
        if self.trace_expressions.is_some()
            || !f.is_comprehension
            || f.is_generator
            || !f.closure.cells.is_empty()
            || !f.closure.free.is_empty()
        {
            return None;
        }
        let [init, loop_id, _ret] = f.body.root.as_slice() else {
            return None;
        };
        let StmtKind::Assign {
            target: AssignTarget::Local(result),
            value,
        } = &f.body.stmts[*init].kind
        else {
            return None;
        };
        if *value != id
            || f.body.locals[*result].name.as_str() != ".result"
            || !matches!(&f.body.exprs[id].kind,ExprKind::List(items)if items.is_empty())
        {
            return None;
        }
        let StmtKind::ForEach {
            iter, body, orelse, ..
        } = &f.body.stmts[*loop_id].kind
        else {
            return None;
        };
        if !orelse.is_empty()
            || body.len() != 1
            || !matches!(f.body.stmts[body[0]].kind, StmtKind::Expr(_))
        {
            return None;
        }
        let ExprKind::Local(input) = f.body.exprs[*iter].kind else {
            return None;
        };
        if !f.body.locals[input].is_param || f.body.locals[input].name.as_str() != ".0" {
            return None;
        }
        let input = Operand::Local(self.local_map[&input]);
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::MakeComprehensionList),
                args: vec![input],
            },
        });
        Some(Operand::Local(dest))
    }
}
