//! 在原调用点展开身份、精确类型和溢出守卫。成功执行有类型算术、递归深度检查与整数装箱；失败执行原调用。
//! 不重读公开模块槽、不重新求参数，因而参数回调重绑定公开函数也不改变本次callee。
use super::leaf_inline_summary::Leaf;
use crate::mir::*;

pub(super) fn expand(f: &mut Function, bb: BlockId, pos: usize, anchor: GlobalId, leaf: Leaf) {
    let Statement::Assign {
        dest,
        value: Rvalue::DynCallValue { callee, args },
    } = &f.body.blocks[bb].stmts[pos]
    else {
        return;
    };
    let (dest, callee, args) = (*dest, callee.clone(), args.clone());
    let rest = f.body.blocks[bb].stmts.split_off(pos + 1);
    f.body.blocks[bb].stmts.truncate(pos);
    let term = std::mem::replace(&mut f.body.blocks[bb].terminator, Terminator::Unreachable);
    let next = block(f, rest, term);
    let saved_callee = local(f, MirType::Dynamic);
    f.body.blocks[bb]
        .stmts
        .push(assign(saved_callee, Rvalue::Use(callee)));
    let mut saved = Vec::new();
    for arg in args {
        let slot = local(f, MirType::Dynamic);
        f.body.blocks[bb].stmts.push(assign(slot, Rvalue::Use(arg)));
        saved.push(slot);
    }
    let fallback = block(
        f,
        vec![assign(
            dest,
            Rvalue::DynCallValue {
                callee: get(saved_callee),
                args: saved.iter().copied().map(get).collect(),
            },
        )],
        Terminator::Goto(next),
    );
    let left = local(f, MirType::I64);
    let right = local(f, MirType::I64);
    let sum = local(f, MirType::I64);
    let left_sign = local(f, MirType::I64);
    let right_sign = local(f, MirType::I64);
    let overflow = local(f, MirType::I64);
    let safe = local(f, MirType::Bool);
    let success = block(
        f,
        vec![
            Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TraceCheckDepth),
                args: vec![],
            }),
            assign(
                dest,
                Rvalue::Box {
                    value: get(sum),
                    from: MirType::I64,
                },
            ),
        ],
        Terminator::Goto(next),
    );
    let calculation = block(
        f,
        vec![
            assign(
                left,
                Rvalue::UnboxExactI64 {
                    value: get(saved[leaf.left]),
                },
            ),
            assign(
                right,
                Rvalue::UnboxExactI64 {
                    value: get(saved[leaf.right]),
                },
            ),
            binary(sum, BinOp::WrappingAdd, get(left), get(right)),
            binary(left_sign, BinOp::BitXor, get(left), get(sum)),
            binary(right_sign, BinOp::BitXor, get(right), get(sum)),
            binary(overflow, BinOp::BitAnd, get(left_sign), get(right_sign)),
            binary(
                safe,
                BinOp::Ge,
                get(overflow),
                Operand::Const(Constant::I64(0)),
            ),
        ],
        branch(safe, success, fallback),
    );
    let mut guarded = calculation;
    for arg in saved.into_iter().rev() {
        let condition = local(f, MirType::Bool);
        guarded = block(
            f,
            vec![assign(condition, Rvalue::IsBoxedI64 { value: get(arg) })],
            branch(condition, guarded, fallback),
        );
    }
    let same = local(f, MirType::Bool);
    // Binary Eq在Dynamic上是CLR引用ceq，不调用Python __eq__；锚点从不由Python写入。
    f.body.blocks[bb].stmts.push(binary(
        same,
        BinOp::Eq,
        get(saved_callee),
        Operand::Global(anchor),
    ));
    f.body.blocks[bb].terminator = branch(same, guarded, fallback);
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
fn get(id: LocalId) -> Operand {
    Operand::Local(id)
}
fn assign(dest: LocalId, value: Rvalue) -> Statement {
    Statement::Assign { dest, value }
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
