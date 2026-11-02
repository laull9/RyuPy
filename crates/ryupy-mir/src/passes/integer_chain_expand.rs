//! 为已通过原浮点链逃逸检查的短 Add 段附加精确 Int64 臂。
//!
//! 原 F64 路径保持第一顺位；本臂不调用用户协议，逐步检查溢出，全部成功才物化最终盒。
//! 失败重放唯一原动态段；不把回绕结果传入慢路，也不改变原整数缓存规则。
use super::float_chain::Plan;
use crate::mir::*;
use rustc_hash::FxHashMap;

/// 在任何 CFG 改写之前核完整三臂增量；原动态语句只是搬移，不能从总量扣除。
pub(super) fn fits(function: &Function, plan: &Plan) -> bool {
    let n = plan.inputs.len();
    let s = plan.steps.len();
    if !(2..=4).contains(&s)
        || !(1..=5).contains(&n)
        || plan.steps.iter().any(|step| step.op != BinOp::Add)
    {
        return false;
    }
    let statements = function
        .body
        .blocks
        .iter()
        .try_fold(0usize, |sum, (_, block)| sum.checked_add(block.stmts.len()));
    // F64: locals=3N+S, blocks=N+3, statements=3N+S+1。
    // I64: locals=2N+5S, blocks=N+S+2, statements=2N+5S+1。
    let local_extra = n
        .checked_mul(5)
        .and_then(|v| s.checked_mul(6).and_then(|w| v.checked_add(w)));
    let block_extra = n
        .checked_mul(2)
        .and_then(|v| v.checked_add(s))
        .and_then(|v| v.checked_add(5));
    let statement_extra = local_extra.and_then(|v| v.checked_add(2));
    within(function.body.locals.len(), local_extra, 512)
        && within(function.body.blocks.len(), block_extra, 128)
        && statements.is_some_and(|current| within(current, statement_extra, 4096))
}
fn within(current: usize, extra: Option<usize>, maximum: usize) -> bool {
    extra
        .and_then(|extra| current.checked_add(extra))
        .is_some_and(|total| total <= maximum)
}

/// 返回 I64 guard 入口；调用方将原 F64 失败边接到这里，fallback/continuation均复用。
pub(super) fn expand(
    function: &mut Function,
    plan: &Plan,
    snapshots: &[(LocalId, LocalId)],
    fallback: BlockId,
    continuation: BlockId,
    final_dest: LocalId,
) -> BlockId {
    let mut raw = FxHashMap::default();
    let mut unbox = Vec::new();
    for &(input, snapshot) in snapshots {
        let integer = local(function, MirType::I64);
        unbox.push(assign(
            integer,
            Rvalue::UnboxExactI64 {
                value: get(snapshot),
            },
        ));
        raw.insert(input, integer);
    }
    let mut operations = Vec::new();
    for step in &plan.steps {
        let left = raw[&step.lhs];
        let right = raw[&step.rhs];
        let sum = local(function, MirType::I64);
        let left_sign = local(function, MirType::I64);
        let right_sign = local(function, MirType::I64);
        let bits = local(function, MirType::I64);
        let safe = local(function, MirType::Bool);
        operations.push((
            vec![
                binary(sum, BinOp::WrappingAdd, get(left), get(right)),
                binary(left_sign, BinOp::BitXor, get(left), get(sum)),
                binary(right_sign, BinOp::BitXor, get(right), get(sum)),
                binary(bits, BinOp::BitAnd, get(left_sign), get(right_sign)),
                binary(safe, BinOp::Ge, get(bits), Operand::Const(Constant::I64(0))),
            ],
            safe,
        ));
        raw.insert(step.dest, sum);
    }
    let mut success = block(
        function,
        vec![assign(
            final_dest,
            Rvalue::Box {
                value: get(raw[&final_dest]),
                from: MirType::I64,
            },
        )],
        Terminator::Goto(continuation),
    );
    // 逆序建块，实际执行仍严格保持原算术次序；溢出前的raw结果从不发布到原动态槽。
    for (statements, safe) in operations.into_iter().rev() {
        success = block(
            function,
            statements,
            Terminator::Branch {
                cond: get(safe),
                then_: success,
                else_: fallback,
            },
        );
    }
    success = block(function, unbox, Terminator::Goto(success));
    for &(_, snapshot) in snapshots.iter().rev() {
        let condition = local(function, MirType::Bool);
        success = block(
            function,
            vec![assign(
                condition,
                Rvalue::IsBoxedI64 {
                    value: get(snapshot),
                },
            )],
            Terminator::Branch {
                cond: get(condition),
                then_: success,
                else_: fallback,
            },
        );
    }
    success
}
fn local(function: &mut Function, ty: MirType) -> LocalId {
    function.body.locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}
fn assign(dest: LocalId, value: Rvalue) -> Statement {
    Statement::Assign { dest, value }
}
fn binary(dest: LocalId, op: BinOp, lhs: Operand, rhs: Operand) -> Statement {
    assign(dest, Rvalue::Binary { op, lhs, rhs })
}
fn get(local: LocalId) -> Operand {
    Operand::Local(local)
}
fn block(function: &mut Function, stmts: Vec<Statement>, terminator: Terminator) -> BlockId {
    function.body.blocks.alloc(BasicBlock { stmts, terminator })
}
