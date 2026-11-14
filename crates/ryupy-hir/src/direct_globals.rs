//! 记录原AST调用与实际HIR调用/callee的精确关系，不以range猜测synthetic节点身份。
use crate::{Body, ExprId, ExprKind};
use ryupy_syntax::ast;
pub(crate) fn record(body: &mut Body, source: &ast::Expr, id: ExprId) {
    if let ast::Expr::Call(call) = source
        && call.arguments.args.is_empty()
        && call.arguments.keywords.is_empty()
        && matches!(call.func.as_ref(), ast::Expr::Name(name) if name.id.as_str() == "globals")
        && let ExprKind::Call { callee, args } = &body.exprs[id].kind
        && args.is_empty()
    {
        body.direct_globals_calls.push((id, *callee));
    }
}
