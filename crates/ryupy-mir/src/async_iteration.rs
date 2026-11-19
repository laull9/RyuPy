//! 异步迭代只发射中性静态协议调用；循环和异常区域由HIR显式控制流决定。
use crate::expr_builder::ExprBuilder;
use crate::{MirType, Operand, RuntimeFn};
use ryupy_hir::{AsyncIterationOp, ExprId};

impl ExprBuilder<'_> {
    pub(crate) fn build_async_iteration(&mut self, value: ExprId, op: AsyncIterationOp) -> Operand {
        let raw = self.build_expr(value);
        let ty = self.ty_of(value);
        let source = self.coerce(raw, &ty, &MirType::Dynamic);
        let runtime = match op {
            AsyncIterationOp::Start => RuntimeFn::AsyncIterStart,
            AsyncIterationOp::Next => RuntimeFn::AsyncIterNext,
            AsyncIterationOp::IsStop => RuntimeFn::AsyncIterIsStop,
        };
        self.runtime_call(runtime, vec![source], MirType::Dynamic)
    }
}
