//! OpaqueStr只开放不观察对象的透传；其它消费沿原整函数Dynamic回退。
//! 与最终结果是否Bool无关，逐输入检查，避免条件/比较绕过降级。
use crate::{Ty, TypeckResults};
use ryupy_hir::{AssignTarget, Binding, ExprId, ExprKind, FStringPart, Function, StmtId, StmtKind};

pub(crate) fn safe_uses(function: &Function, facts: &TypeckResults) -> bool {
    if !facts.locals.iter().any(|(_, t)| *t == Ty::OpaqueStr)
        && !facts.exprs.iter().any(|(_, t)| *t == Ty::OpaqueStr)
    {
        return true;
    }
    if function.params.iter().any(|p| p.default.is_some()) {
        return false;
    }
    let opaque = |id: ExprId| facts.expr_ty(id) == Ty::OpaqueStr;
    for (id, expression) in function.body.exprs.iter() {
        if let ExprKind::Local(local) = &expression.kind
            && facts.local_ty(*local) == Ty::OpaqueStr
            && facts.expr_ty(id) != Ty::OpaqueStr
        {
            // 早先exact读不能沿晚到的object合流继续保旧事实。
            return false;
        }
        let inputs: Vec<ExprId> = match &expression.kind {
            ExprKind::Local(_)
            | ExprKind::ModuleVar(_)
            | ExprKind::Literal(_)
            | ExprKind::ModuleFile(_)
            | ExprKind::ModuleMetadata(_, _)
            | ExprKind::Func(_)
            | ExprKind::Class(_)
            | ExprKind::Builtin(_)
            | ExprKind::Bytes(_)
            | ExprKind::Missing
            | ExprKind::UnresolvedGlobal(_)
            | ExprKind::Free(_)
            | ExprKind::Lambda(_)
            | ExprKind::ClassLocal(_)
            | ExprKind::ClassCellRead(_)
            | ExprKind::ClassFreeLocal(_) => vec![],
            ExprKind::ClassLookup { fallback, .. } => vec![*fallback],
            ExprKind::Unary { operand, .. }
            | ExprKind::Attribute { base: operand, .. }
            | ExprKind::Starred(operand)
            | ExprKind::YieldFrom(operand) => vec![*operand],
            ExprKind::Binary { lhs, rhs, .. }
            | ExprKind::Compare { lhs, rhs, .. }
            | ExprKind::BoolOp { lhs, rhs, .. } => vec![*lhs, *rhs],
            // 分支结果只复制；条件才观察对象。同类分支由旧类型规则合流。
            ExprKind::IfExp { cond, .. } => vec![*cond],
            ExprKind::Call { callee, args } | ExprKind::PreparedCall { callee, args } => {
                std::iter::once(*callee)
                    .chain(args.iter().map(|a| a.expr()))
                    .collect()
            }
            ExprKind::List(items) | ExprKind::Tuple(items) | ExprKind::Set(items) => items.clone(),
            ExprKind::Dict(items) => items.iter().flat_map(|(a, b)| [*a, *b]).collect(),
            ExprKind::FString(parts) => parts
                .iter()
                .filter_map(|p| match p {
                    FStringPart::Value { value, .. } => Some(*value),
                    _ => None,
                })
                .collect(),
            ExprKind::Subscript { base, index } => vec![*base, *index],
            ExprKind::Slice { start, stop, step } => {
                [*start, *stop, *step].into_iter().flatten().collect()
            }
            ExprKind::Yield(value) => value.iter().copied().collect(),
        };
        if inputs.into_iter().any(opaque) {
            return false;
        }
    }
    // 透传与普通if直接许可；只对完全不接触opaque的循环保留原数值路径。
    function
        .body
        .stmts
        .iter()
        .all(|(_, statement)| match &statement.kind {
            StmtKind::Assign {
                target: AssignTarget::Local(_) | AssignTarget::ModuleVar(_),
                ..
            }
            | StmtKind::Return(_)
            | StmtKind::Expr(_)
            | StmtKind::Pass => true,
            StmtKind::If { cond, .. } => !opaque(*cond),
            StmtKind::ForRange { .. } | StmtKind::While { .. } => {
                independent_statement(function, facts, &statement.kind)
            }
            StmtKind::Break | StmtKind::Continue => true,
            _ => false,
        })
}

// 循环依赖仅按现HIR与保守最终类型检查，不引入全程序别名/回调分析。
fn independent_block(function: &Function, facts: &TypeckResults, block: &[StmtId]) -> bool {
    block
        .iter()
        .all(|id| independent_statement(function, facts, &function.body.stmts[*id].kind))
}
fn independent_statement(function: &Function, facts: &TypeckResults, stmt: &StmtKind) -> bool {
    let plain = |id: ExprId| facts.expr_ty(id) != Ty::OpaqueStr;
    let local = |id| facts.local_ty(id) != Ty::OpaqueStr;
    match stmt {
        StmtKind::Assign {
            target: AssignTarget::Local(id),
            value,
        } => local(*id) && plain(*value),
        // 全局/属性写可能暴露对象；独立循环首切只允许数值局部目标。
        StmtKind::Assign { .. } => false,
        StmtKind::Expr(e) => plain(*e),
        StmtKind::Return(e) => e.is_none_or(plain),
        StmtKind::Pass | StmtKind::Break | StmtKind::Continue => true,
        StmtKind::If { cond, then_, else_ } => {
            plain(*cond)
                && independent_block(function, facts, then_)
                && independent_block(function, facts, else_)
        }
        StmtKind::While { cond, body, orelse } => {
            plain(*cond)
                && independent_block(function, facts, body)
                && independent_block(function, facts, orelse)
        }
        StmtKind::ForRange {
            var: Binding::Local(id),
            start,
            stop,
            step,
            body,
            orelse,
        } => {
            local(*id)
                && plain(*start)
                && plain(*stop)
                && step.is_none_or(plain)
                && independent_block(function, facts, body)
                && independent_block(function, facts, orelse)
        }
        _ => false,
    }
}
