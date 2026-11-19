//! 标量执行区CFG发射：精确盒/范围守卫、原生值DAG、边界物化和原区慢路。
use super::scalar_region::{LIMIT, Op, Plan};
use crate::mir::*;
use rustc_hash::FxHashMap;

pub(super) fn expand(f: &mut Function, p: Plan) {
    let original = &mut f.body.blocks[p.block];
    let suffix = original.stmts.split_off(p.end);
    let source = original.stmts.split_off(p.start);
    let term = std::mem::replace(&mut original.terminator, Terminator::Unreachable);
    let continuation = block(f, suffix, term);
    let fallback = block(f, source, Terminator::Goto(continuation));
    let mut raw = FxHashMap::default();
    for id in &p.inputs {
        raw.insert(*id, local(f, MirType::I64));
    }
    let mut body = Vec::new();
    for (dest, op) in &p.steps {
        if let Op::Line(line) = op {
            body.push(Statement::Line(*line));
            continue;
        }
        let fact = &p.values[dest];
        let ty = if fact.boolean {
            MirType::Bool
        } else {
            MirType::I64
        };
        let out = local(f, ty);
        let rewrite = |op: &Operand| match op {
            Operand::Local(id) => Operand::Local(raw[id]),
            _ => op.clone(),
        };
        let value = match op {
            Op::Copy(op) => Rvalue::Use(rewrite(op)),
            Op::Binary(op, a, b) => Rvalue::Binary {
                op: *op,
                lhs: rewrite(a),
                rhs: rewrite(b),
            },
            Op::Mod(a, b) => Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ModI64),
                args: vec![rewrite(a), rewrite(b)],
            },
            Op::Line(_) => unreachable!(),
        };
        body.push(assign(out, value));
        raw.insert(*dest, out);
    }
    // 原定义顺序物化；同一区的别名沿原盒复制，新结果每个逻辑定义只装箱一次。
    for (dest, op) in &p.steps {
        if matches!(op, Op::Line(_)) || !p.live.contains(dest) {
            continue;
        }
        let fact = &p.values[dest];
        let value = if f.body.locals[*dest].ty == MirType::Bool {
            Rvalue::Use(get(raw[dest]))
        } else if let Some(original) = &fact.original {
            Rvalue::Use(original.clone())
        } else {
            Rvalue::Box {
                value: get(raw[dest]),
                from: if fact.boolean {
                    MirType::Bool
                } else {
                    MirType::I64
                },
            }
        };
        body.push(assign(*dest, value));
    }
    let mut entry = block(f, body, Terminator::Goto(continuation));
    // 全部守卫在原区效果之前。失败不执行快路定义，逐字进入原区且只运行一次。
    for id in p.inputs.iter().rev() {
        let native = raw[id];
        if p.bounded {
            let upper = local(f, MirType::Bool);
            entry = block(
                f,
                vec![assign(
                    upper,
                    Rvalue::Binary {
                        op: BinOp::Le,
                        lhs: get(native),
                        rhs: Operand::Const(Constant::I64(LIMIT)),
                    },
                )],
                Terminator::Branch {
                    cond: get(upper),
                    then_: entry,
                    else_: fallback,
                },
            );
            let lower = local(f, MirType::Bool);
            entry = block(
                f,
                vec![assign(
                    lower,
                    Rvalue::Binary {
                        op: BinOp::Ge,
                        lhs: get(native),
                        rhs: Operand::Const(Constant::I64(-LIMIT)),
                    },
                )],
                Terminator::Branch {
                    cond: get(lower),
                    then_: entry,
                    else_: fallback,
                },
            );
        }
        entry = block(
            f,
            vec![assign(native, Rvalue::UnboxExactI64 { value: get(*id) })],
            Terminator::Goto(entry),
        );
        let exact = local(f, MirType::Bool);
        entry = block(
            f,
            vec![assign(exact, Rvalue::IsBoxedI64 { value: get(*id) })],
            Terminator::Branch {
                cond: get(exact),
                then_: entry,
                else_: fallback,
            },
        );
    }
    f.body.blocks[p.block].terminator = Terminator::Goto(entry);
}
fn get(id: LocalId) -> Operand {
    Operand::Local(id)
}
fn assign(dest: LocalId, value: Rvalue) -> Statement {
    Statement::Assign { dest, value }
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
