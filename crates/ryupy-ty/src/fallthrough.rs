//! 保守判断函数是否可能正常落尾，为隐式 `return None` 提供类型事实。
//! 只证明结构上的终止，不做表达式求值或异常匹配；未知条件保留所有路径。
use ryupy_hir::{Body, ExprKind, Literal, StmtId, StmtKind};

const NORMAL: u8 = 1;
const EXIT: u8 = 2;
const BREAK: u8 = 4;
const CONTINUE: u8 = 8;

pub(crate) fn may_fall_through(body: &Body) -> bool {
    block(body, &body.root) & NORMAL != 0
}

fn block(body: &Body, stmts: &[StmtId]) -> u8 {
    let mut flow = NORMAL;
    for &id in stmts {
        if flow & NORMAL == 0 {
            break;
        }
        flow = (flow & !NORMAL) | statement(body, id);
    }
    flow
}

fn statement(body: &Body, id: StmtId) -> u8 {
    match &body.stmts[id].kind {
        StmtKind::Return(_) | StmtKind::Raise { .. } => EXIT,
        StmtKind::Break => BREAK,
        StmtKind::Continue => CONTINUE,
        StmtKind::If { then_, else_, .. } => block(body, then_) | block(body, else_),
        StmtKind::While {
            cond,
            body: inner,
            orelse,
        } => {
            let always = matches!(
                body.exprs[*cond].kind,
                ExprKind::Literal(Literal::Bool(true))
            );
            loop_flow(body, inner, orelse, !always)
        }
        StmtKind::ForEach {
            body: inner,
            orelse,
            ..
        }
        | StmtKind::ForRange {
            body: inner,
            orelse,
            ..
        } => {
            // 即便循环体总是 return，也不能排除零次迭代。
            loop_flow(body, inner, orelse, true)
        }
        StmtKind::Try {
            body: inner,
            handlers,
            orelse,
            finalbody,
        } => {
            let initial = block(body, inner);
            let mut flow = initial & !NORMAL;
            if initial & NORMAL != 0 {
                flow |= block(body, orelse);
            }
            // 任意表达式都可能抛异常；不猜测 except 是否匹配。
            // else 的异常不进入这些 handler，但保留全部 handler 是安全上界。
            for &handler in handlers {
                flow |= block(body, &body.handlers[handler].body);
            }
            let final_flow = block(body, finalbody);
            // finally 正常完成时保留原 return/raise/break；否则覆盖原转移。
            // 即便主体只有无限循环，也保守保留 finally 的非正常出口。
            (if final_flow & NORMAL != 0 { flow } else { 0 }) | (final_flow & !NORMAL)
        }
        // 嵌套定义只在当前位置创建函数，不把其内部 return 算进外层。
        _ => NORMAL,
    }
}

fn loop_flow(body: &Body, inner: &[StmtId], orelse: &[StmtId], may_exhaust: bool) -> u8 {
    let flow = block(body, inner);
    let mut result = flow & EXIT;
    if flow & BREAK != 0 {
        result |= NORMAL;
    }
    // 当前层 break/continue 被循环消费；else 里的转移属于外层循环。
    if may_exhaust {
        result |= block(body, orelse);
    }
    result
}
