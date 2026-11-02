//! 将已证明无观察的浮点链展开成精确类型守卫、原生计算与原动态慢路。
//!
//! 输入只冻结引用，失败路径搬移原语句而非重新求值；快路保持每次 F64 运算与舍入顺序，
//! 不重结合、不使用 FMA，只有最终结果装箱。普通对象仍按原 DynBinary 协议执行。
use super::float_chain::Plan;
use crate::mir::*;
use rustc_hash::FxHashMap;

pub(super) fn expand(function: &mut Function, plan: Plan, integer: bool) {
    let Some(final_dest) = plan.steps.last().map(|step| step.dest) else {
        return;
    };
    let tail = function.body.blocks[plan.block]
        .stmts
        .split_off(plan.start + plan.steps.len());
    let original = function.body.blocks[plan.block].stmts.split_off(plan.start);
    let next = std::mem::replace(
        &mut function.body.blocks[plan.block].terminator,
        Terminator::Unreachable,
    );
    let continuation = block(function, tail, next);
    let fallback = block(function, original, Terminator::Goto(continuation));
    let mut raw = FxHashMap::default();
    let mut frozen = Vec::new();
    let mut fast = Vec::new();
    for input in &plan.inputs {
        let snapshot = local(function, MirType::Dynamic);
        function.body.blocks[plan.block]
            .stmts
            .push(assign(snapshot, Rvalue::Use(get(*input))));
        let value = local(function, MirType::F64);
        fast.push(assign(
            value,
            Rvalue::UnboxExactF64 {
                value: get(snapshot),
            },
        ));
        raw.insert(*input, value);
        frozen.push((*input, snapshot));
    }
    for step in &plan.steps {
        let result = local(function, MirType::F64);
        fast.push(assign(
            result,
            Rvalue::Binary {
                op: step.op,
                lhs: get(raw[&step.lhs]),
                rhs: get(raw[&step.rhs]),
            },
        ));
        raw.insert(step.dest, result);
    }
    fast.push(assign(
        final_dest,
        Rvalue::Box {
            value: get(raw[&final_dest]),
            from: MirType::F64,
        },
    ));
    let mut success = block(function, fast, Terminator::Goto(continuation));
    // 不额外增加F64成功路径的守卫；仅原失败边尝试有界I64，所有失败共用原动态段。
    let fallback = if integer {
        super::integer_chain_expand::expand(
            function,
            &plan,
            &frozen,
            fallback,
            continuation,
            final_dest,
        )
    } else {
        fallback
    };
    // 每个 exact unbox 均由所有输入的真分支支配；不靠运行时转换或静态注解冒充证明。
    for (_, snapshot) in frozen.into_iter().rev() {
        let condition = local(function, MirType::Bool);
        success = block(
            function,
            vec![assign(
                condition,
                Rvalue::IsBoxedF64 {
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
    function.body.blocks[plan.block].terminator = Terminator::Goto(success);
}
fn local(function: &mut Function, ty: MirType) -> LocalId {
    function.body.locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}
fn block(function: &mut Function, stmts: Vec<Statement>, terminator: Terminator) -> BlockId {
    function.body.blocks.alloc(BasicBlock { stmts, terminator })
}
fn assign(dest: LocalId, value: Rvalue) -> Statement {
    Statement::Assign { dest, value }
}
fn get(local: LocalId) -> Operand {
    Operand::Local(local)
}
