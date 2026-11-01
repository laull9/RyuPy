//! 把一个严格累加候选展开为局部状态与普通CFG，不让后端决定Python类型/溢出语义。
//! total保存最近可观察的原盒；整数模式下CLR null表示需要物化，零轮/慢路返回身份不变。
use super::numeric_accumulator_match::Plan;
use crate::mir::*;

pub(super) fn expand(f: &mut Function, plan: Plan) {
    let Terminator::Goto(header) = f.body.blocks[plan.preheader].terminator else {
        return;
    };
    let total = plan.total;
    let integer = local(f, MirType::I64);
    let mode = local(f, MirType::Bool);
    // 这两个bool只在物化检查块内使用，不是跨循环迭代保存的脏状态。
    let is_null = local(f, MirType::Bool);
    let needs_box = local(f, MirType::Bool);
    let rhs = local(f, MirType::Dynamic);
    let rhs_exact = local(f, MirType::Bool);
    let right = local(f, MirType::I64);
    let sum = local(f, MirType::I64);
    let left_sign = local(f, MirType::I64);
    let right_sign = local(f, MirType::I64);
    let overflow_bits = local(f, MirType::I64);
    let safe = local(f, MirType::Bool);
    let result = local(f, MirType::Dynamic);

    // 保留原更新之后的行号与latch控制；原候选已证明这里没有其它可观察操作。
    let tail = f.body.blocks[plan.body].stmts.split_off(plan.update + 2);
    f.body.blocks[plan.body].stmts.truncate(plan.update);
    let next = std::mem::replace(
        &mut f.body.blocks[plan.body].terminator,
        Terminator::Unreachable,
    );
    let continuation = block(f, tail, next);
    let initial_integer = block(
        f,
        vec![exact_unbox(integer, total)],
        Terminator::Goto(header),
    );
    let result_integer = block(
        f,
        vec![exact_unbox(integer, total)],
        Terminator::Goto(continuation),
    );
    let dynamic = block(
        f,
        vec![
            assign(
                result,
                // 已有通用冷入口保完整inplace协议，避免在快路函数内重复内联PIC分派树。
                Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynBinary),
                    args: vec![
                        Operand::Const(Constant::I64(i64::from(
                            DynOp::Add.code() | DynOp::INPLACE_FLAG,
                        ))),
                        get(total),
                        get(rhs),
                    ],
                },
            ),
            assign(total, Rvalue::Use(get(result))),
            assign(mode, Rvalue::IsBoxedI64 { value: get(total) }),
        ],
        branch(mode, result_integer, continuation),
    );
    let box_left = block(f, vec![boxed(total, integer)], Terminator::Goto(dynamic));
    let fallback = block(
        f,
        materialization_guard(total, mode, is_null, needs_box),
        branch(needs_box, box_left, dynamic),
    );
    let commit = block(
        f,
        vec![
            assign(integer, Rvalue::Use(get(sum))),
            assign(total, Rvalue::Use(Operand::Const(Constant::Null))),
        ],
        Terminator::Goto(continuation),
    );
    let addition = block(
        f,
        vec![
            exact_unbox(right, rhs),
            binary(sum, BinOp::WrappingAdd, get(integer), get(right)),
            binary(left_sign, BinOp::BitXor, get(integer), get(sum)),
            binary(right_sign, BinOp::BitXor, get(right), get(sum)),
            binary(
                overflow_bits,
                BinOp::BitAnd,
                get(left_sign),
                get(right_sign),
            ),
            binary(
                safe,
                BinOp::Ge,
                get(overflow_bits),
                Operand::Const(Constant::I64(0)),
            ),
        ],
        branch(safe, commit, fallback),
    );
    let check_rhs = block(
        f,
        vec![assign(rhs_exact, Rvalue::IsBoxedI64 { value: get(rhs) })],
        branch(rhs_exact, addition, fallback),
    );

    // RHS只在原位置求一次；guard/unbox都读专用Local，绝不重读可能被回调改变的Global。
    f.body.blocks[plan.body]
        .stmts
        .push(assign(rhs, Rvalue::Use(plan.rhs)));
    f.body.blocks[plan.body].terminator = branch(mode, check_rhs, fallback);
    f.body.blocks[plan.preheader].stmts.extend([
        assign(integer, Rvalue::Use(Operand::Const(Constant::I64(0)))),
        assign(mode, Rvalue::IsBoxedI64 { value: get(total) }),
    ]);
    f.body.blocks[plan.preheader].terminator = branch(mode, initial_integer, header);

    // 唯一出口先按需物化，然后原出口的任意多次观察都复用同一total盒。
    let exit_stmts = std::mem::take(&mut f.body.blocks[plan.exit].stmts);
    let exit_term = std::mem::replace(
        &mut f.body.blocks[plan.exit].terminator,
        Terminator::Unreachable,
    );
    let final_exit = block(f, exit_stmts, exit_term);
    let materialize = block(f, vec![boxed(total, integer)], Terminator::Goto(final_exit));
    f.body.blocks[plan.exit].stmts = materialization_guard(total, mode, is_null, needs_box);
    f.body.blocks[plan.exit].terminator = branch(needs_box, materialize, final_exit);
}

fn local(f: &mut Function, ty: MirType) -> LocalId {
    f.body.locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}
fn block(f: &mut Function, stmts: Vec<Statement>, terminator: Terminator) -> BlockId {
    f.body.blocks.alloc(BasicBlock { stmts, terminator })
}
fn get(local: LocalId) -> Operand {
    Operand::Local(local)
}
fn assign(dest: LocalId, value: Rvalue) -> Statement {
    Statement::Assign { dest, value }
}
/// Eq(Dynamic,Null)是CLR引用比较，绝不调用Python __eq__；mode=false时真实null不物化。
fn materialization_guard(
    total: LocalId,
    mode: LocalId,
    is_null: LocalId,
    needs_box: LocalId,
) -> Vec<Statement> {
    vec![
        binary(
            is_null,
            BinOp::Eq,
            get(total),
            Operand::Const(Constant::Null),
        ),
        binary(needs_box, BinOp::BitAnd, get(mode), get(is_null)),
    ]
}
fn exact_unbox(dest: LocalId, value: LocalId) -> Statement {
    assign(dest, Rvalue::UnboxExactI64 { value: get(value) })
}
fn boxed(dest: LocalId, value: LocalId) -> Statement {
    assign(
        dest,
        Rvalue::Box {
            value: get(value),
            from: MirType::I64,
        },
    )
}
fn binary(dest: LocalId, op: BinOp, lhs: Operand, rhs: Operand) -> Statement {
    assign(dest, Rvalue::Binary { op, lhs, rhs })
}
fn branch(cond: LocalId, then_: BlockId, else_: BlockId) -> Terminator {
    Terminator::Branch {
        cond: get(cond),
        then_,
        else_,
    }
}
