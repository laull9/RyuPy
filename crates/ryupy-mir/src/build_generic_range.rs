//! 通用伴生体的range循环：边界按源序求值一次，复用range对象与通用迭代CFG。
//! typed计数循环及原Dynamic i64计数快路保留；本路径不向Python参数插入Unbox。
use crate::build::FunctionBuilder;
use crate::{Constant, MirType, Operand, RuntimeFn};
use ryupy_hir::{Binding, ExprId, StmtId};

impl FunctionBuilder<'_> {
    pub(crate) fn build_for_range_generic(
        &mut self,
        var: Binding,
        start: ExprId,
        stop: ExprId,
        step: Option<ExprId>,
        body: &[StmtId],
        orelse: &[StmtId],
    ) {
        let mut arguments = Vec::with_capacity(3);
        for expression in [Some(start), Some(stop), step].into_iter().flatten() {
            let value = self.expr_builder().build_expr(expression);
            let ty = self.expr_builder().ty_of(expression);
            let value = self.expr_builder().coerce(value, &ty, &MirType::Dynamic);
            arguments.push(self.expr_builder().freeze_generic_argument(value));
        }
        // HIR的一参数range已经合成为start=0/stop；两参数形式保持同一范围。
        // 所有用户表达式完成后才进入原生range绑定，步长0等错误在循环体之前抛出。
        let iterable = self.expr_builder().runtime_call(
            RuntimeFn::DynBuiltin,
            vec![
                Operand::Const(Constant::Str("range".into())),
                Operand::Array(arguments),
                Operand::Array(Vec::new()),
                Operand::Array(Vec::new()),
            ],
            MirType::Dynamic,
        );
        self.build_for_each_value(var, iterable, body, orelse);
    }
}
