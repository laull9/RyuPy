//! 受守卫的循环累加器实验：只匹配无异常区、无逃逸的三块自然循环，不猜测Python类型。
//!
//! 运行时holder保存原initial身份，只在精确long加法安全时延迟装箱；其它类型/溢出完整回退。
//! 本pass不改变CFG，仅把preheader末尾、body唯一+=和唯一exit入口接到普通RuntimeFn ABI。
use super::numeric_accumulator_match::Plan;
use crate::mir::*;

pub(super) fn expand(function: &mut Function, plan: Plan) {
    let holder = function.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    function.body.blocks[plan.preheader]
        .stmts
        .push(Statement::Assign {
            dest: holder,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::NumericAccumulatorCreate),
                args: vec![Operand::Local(plan.total)],
            },
        });
    let statements = &mut function.body.blocks[plan.body].stmts;
    statements.splice(
        plan.update..plan.update + 2,
        [Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::NumericAccumulatorAdd),
            args: vec![Operand::Local(holder), plan.rhs],
        })],
    );
    function.body.blocks[plan.exit].stmts.insert(
        0,
        Statement::Assign {
            dest: plan.total,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::NumericAccumulatorValue),
                args: vec![Operand::Local(holder)],
            },
        },
    );
}
