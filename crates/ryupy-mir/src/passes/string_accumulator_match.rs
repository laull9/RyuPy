//! 精确字符串静态槽的有限自然循环匹配。
//!
//! 真实顶层 MIR 使用 Use(Global)→StrConcat→SetGlobal；这里只接受预头直接字符串
//! 初值和常量后缀，环内其余语句仅允许已定型计数器计算、发布及已绑定检查。
use super::dead_values::{read_operand, read_value};
pub(super) use super::numeric_accumulator_match::predecessors;
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub(super) struct Plan {
    pub preheader: BlockId,
    pub body: BlockId,
    pub exit: BlockId,
    pub update_start: usize,
    pub target_id: GlobalId,
    pub rhs_literal: String,
    pub initial: Operand,
    pub update_len: usize,
    pub typed: bool,
}

pub(super) fn candidate(
    f: &Function,
    header: BlockId,
    preds: &FxHashMap<BlockId, Vec<BlockId>>,
) -> Option<Plan> {
    let Terminator::Branch {
        then_: body,
        else_: exit,
        ..
    } = f.body.blocks[header].terminator
    else {
        return None;
    };
    let Terminator::Goto(latch) = f.body.blocks[body].terminator else {
        return None;
    };
    if !matches!(f.body.blocks[latch].terminator, Terminator::Goto(back) if back == header) {
        return None;
    }
    let incoming = preds.get(&header)?;
    if incoming.len() != 2 || !incoming.contains(&latch) {
        return None;
    }
    let preheader = *incoming.iter().find(|&&b| b != latch)?;
    if [preheader, header, body, latch, exit]
        .into_iter()
        .collect::<FxHashSet<_>>()
        .len()
        != 5
        || [header, body, latch, exit].contains(&f.body.entry)
        || preds.get(&body)?.as_slice() != [header]
        || preds.get(&latch)?.as_slice() != [body]
        || preds.get(&exit)?.as_slice() != [header]
        || !matches!(f.body.blocks[preheader].terminator, Terminator::Goto(to) if to == header)
        || !f.body.blocks[header]
            .stmts
            .iter()
            .all(|s| control_statement(f, s))
        || !f.body.blocks[latch]
            .stmts
            .iter()
            .all(|s| control_statement(f, s))
    {
        return None;
    }
    if !canonical_unit_range(f, preheader, header, latch) {
        return None;
    }
    for start in 0..f.body.blocks[body].stmts.len() {
        let tail = &f.body.blocks[body].stmts[start..];
        let matched = match tail {
            [
                Statement::Assign {
                    dest: loaded,
                    value: Rvalue::Use(Operand::Global(target)),
                },
                Statement::Assign {
                    dest: result,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::StrConcat),
                            args,
                        },
                },
                Statement::SetGlobal {
                    dest: stored,
                    value: Operand::Local(value),
                },
                ..,
            ] => {
                let [Operand::Local(left), Operand::Const(Constant::Str(suffix))] = args.as_slice()
                else {
                    continue;
                };
                if left != loaded
                    || stored != target
                    || value != result
                    || f.body.locals[*loaded].ty != MirType::Str
                {
                    continue;
                }
                Some((*target, *loaded, *result, suffix.clone(), 3, true))
            }
            [
                Statement::Assign {
                    dest: loaded,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::LoadName),
                            args,
                        },
                },
                Statement::Assign {
                    dest: rhs,
                    value:
                        Rvalue::Box {
                            value: Operand::Const(Constant::Str(suffix)),
                            from: MirType::Str,
                        },
                },
                Statement::Assign {
                    dest: result,
                    value:
                        Rvalue::DynBinary {
                            op: DynOp::Add,
                            lhs: Operand::Local(left),
                            rhs: Operand::Local(right),
                            ..
                        },
                },
                Statement::SetGlobal {
                    dest: stored,
                    value: Operand::Local(value),
                },
                ..,
            ] => {
                let [Operand::Global(target), Operand::Const(Constant::Str(_))] = args.as_slice()
                else {
                    continue;
                };
                if left != loaded
                    || right != rhs
                    || stored != target
                    || value != result
                    || !single_use_definition(f, *rhs)
                {
                    continue;
                }
                Some((*target, *loaded, *result, suffix.clone(), 4, false))
            }
            _ => None,
        };
        let Some((target, loaded, result, suffix, update_len, typed)) = matched else {
            continue;
        };
        if !single_use_definition(f, loaded) || !single_use_definition(f, result) {
            continue;
        }
        let Some(initial) = initial_literal(f, preheader, target) else {
            continue;
        };
        if !f.body.blocks[body]
            .stmts
            .iter()
            .enumerate()
            .all(|(index, statement)| {
                (start..start + update_len).contains(&index)
                    || body_statement(f, preheader, body, statement, target)
            })
        {
            continue;
        }
        return Some(Plan {
            preheader,
            body,
            exit,
            update_start: start,
            target_id: target,
            rhs_literal: suffix,
            initial,
            update_len,
            typed,
        });
    }
    None
}

/// 控制局部只含原生 i64/bool，无全局读、引用、转换或用户协议。
fn native_operand(f: &Function, op: &Operand) -> bool {
    match op {
        Operand::Const(Constant::I64(_) | Constant::Bool(_)) => true,
        Operand::Local(id) => {
            matches!(f.body.locals[*id].ty, MirType::I64 | MirType::Bool)
                && !f.body.locals[*id].is_cell
        }
        _ => false,
    }
}
fn control_statement(f: &Function, s: &Statement) -> bool {
    match s {
        Statement::Line(_) => true,
        Statement::Assign { dest, value } if native_operand(f, &Operand::Local(*dest)) => {
            match value {
                Rvalue::Use(op) | Rvalue::Unary { operand: op, .. } => native_operand(f, op),
                Rvalue::Binary { lhs, rhs, op } => {
                    matches!(
                        op,
                        BinOp::Add
                            | BinOp::Lt
                            | BinOp::Gt
                            | BinOp::Eq
                            | BinOp::BitAnd
                            | BinOp::BitOr
                    ) && native_operand(f, lhs)
                        && native_operand(f, rhs)
                }
                _ => false,
            }
        }
        _ => false,
    }
}

/// 最后一次静态槽写入必须是字面量，其后仅允许无调用的控制初始化和计数器发布。
fn initial_literal(f: &Function, preheader: BlockId, target: GlobalId) -> Option<Operand> {
    let statements = &f.body.blocks[preheader].stmts;
    let (position, last) = statements
        .iter()
        .enumerate()
        .rev()
        .find(|(_, s)| matches!(s, Statement::SetGlobal { dest, .. } if *dest == target))?;
    let Statement::SetGlobal { value, .. } = last else {
        return None;
    };
    let initial = match value {
        Operand::Const(Constant::Str(_)) => value.clone(),
        Operand::Local(id) => {
            let defs: Vec<_> = statements[..position]
                .iter()
                .filter_map(|s| match s {
                    Statement::Assign { dest, value } if dest == id => Some(value),
                    _ => None,
                })
                .collect();
            if defs.len() != 1
                || !single_definition(f, *id)
                || !matches!(
                    defs[0],
                    Rvalue::Box {
                        value: Operand::Const(Constant::Str(_)),
                        from: MirType::Str
                    }
                )
            {
                return None;
            }
            value.clone()
        }
        _ => return None,
    };
    if statements[position + 1..].iter().all(|s| control_statement(f, s)
        || matches!(s, Statement::SetGlobal { dest, value } if *dest != target && native_operand(f, value))
        || literal_range_setup(f, statements, s)) { Some(initial) } else { None }
}

fn body_statement(
    f: &Function,
    preheader: BlockId,
    body: BlockId,
    s: &Statement,
    target: GlobalId,
) -> bool {
    if matches!(s, Statement::Line(_)) {
        return true;
    }
    match s {
        Statement::Assign {
            value:
                Rvalue::Box {
                    value,
                    from: MirType::I64,
                },
            ..
        } => native_operand(f, value),
        Statement::SetGlobal { dest, value } => {
            *dest != target
                && (native_operand(f, value)
                    || matches!(value,
            Operand::Local(id) if single_definition(f, *id) && f.body.blocks[body].stmts.iter().any(|s| matches!(s,
                Statement::Assign { dest, value: Rvalue::Box { value, from: MirType::I64 } } if dest == id && native_operand(f, value)))))
        }
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::CheckGlobalBound),
            args,
        }) => {
            let [Operand::Global(bound), Operand::Const(Constant::Str(_))] = args.as_slice() else {
                return false;
            };
            // 检查读取布尔绑定位；进入循环前已为 true，环内也只能写 true。
            matches!(f.body.blocks[preheader].stmts.iter().rev().find(|s| matches!(s, Statement::SetGlobal { dest, .. } if dest == bound)),
                Some(Statement::SetGlobal { value: Operand::Const(Constant::Bool(true)), .. }))
                && f.body.blocks[body].stmts.iter().all(|s| !matches!(s, Statement::SetGlobal { dest, value } if dest == bound && !matches!(value, Operand::Const(Constant::Bool(true)))))
        }
        _ => false,
    }
}

/// 读取器复用完整 MIR 定义；中间字符串只能在被改写切片中使用一次。
fn single_use_definition(f: &Function, local: LocalId) -> bool {
    let (mut writes, mut reads) = (0, 0);
    for (_, block) in f.body.blocks.iter() {
        for s in &block.stmts {
            let mut used = FxHashSet::default();
            match s {
                Statement::Assign { dest, value } => {
                    writes += usize::from(*dest == local);
                    read_value(value, &mut used);
                }
                Statement::Eval(v) => read_value(v, &mut used),
                Statement::SetGlobal { value, .. } => read_operand(value, &mut used),
                Statement::SetField { obj, value, .. } => {
                    read_operand(obj, &mut used);
                    read_operand(value, &mut used);
                }
                Statement::Line(_) => {}
            }
            reads += usize::from(used.contains(&local));
        }
        let mut used = FxHashSet::default();
        read_terminator(&block.terminator, &mut used);
        reads += usize::from(used.contains(&local));
    }
    reads == 1 && writes == 1
}

/// 只允许 range 字面量先装箱后精确 i64 拆箱，不接受动态转换或用户协议。
fn literal_range_setup(f: &Function, statements: &[Statement], s: &Statement) -> bool {
    match s {
        Statement::Assign { value: Rvalue::Box { value: Operand::Const(Constant::I64(_)), from: MirType::I64 }, .. } => true,
        Statement::Assign { value: Rvalue::Unbox { value: Operand::Local(id), to: MirType::I64 }, .. } => {
            single_definition(f, *id) && statements.iter().any(|s| matches!(s,
                Statement::Assign { dest, value: Rvalue::Box { value: Operand::Const(Constant::I64(_)), from: MirType::I64 } } if dest == id))
        }
        _ => false,
    }
}
fn single_definition(f: &Function, id: LocalId) -> bool {
    f.body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter(|s| matches!(s, Statement::Assign { dest, .. } if *dest == id))
        .count()
        == 1
}

/// 终结符只读实际消费的值；Suspend.input 是恢复端定义，非读取。
fn read_terminator(term: &Terminator, reads: &mut FxHashSet<LocalId>) {
    match term {
        Terminator::Branch { cond, .. } => read_operand(cond, reads),
        Terminator::Suspend { value, .. } => read_operand(value, reads),
        Terminator::Return(value) => {
            if let Some(value) = value {
                read_operand(value, reads);
            }
        }
        Terminator::Raise { value, cause } => {
            for operand in value.iter().chain(cause.iter()) {
                read_operand(operand, reads);
            }
        }
        Terminator::Goto(_) | Terminator::Unreachable => {}
    }
}

/// 只改写编译器的 +1 range：counter < stop 保证回边加法不越过 i64 上界。
/// 禁止普通 while 或动态步长，避免可捕获的步进异常观察到尚未写回的绑定。
fn canonical_unit_range(f: &Function, preheader: BlockId, header: BlockId, latch: BlockId) -> bool {
    let latch_stmts: Vec<_> = f.body.blocks[latch]
        .stmts
        .iter()
        .filter(|s| !matches!(s, Statement::Line(_)))
        .collect();
    let [
        Statement::Assign {
            dest: counter,
            value:
                Rvalue::Binary {
                    op: BinOp::Add,
                    lhs: Operand::Local(left),
                    rhs: Operand::Local(step),
                },
        },
    ] = latch_stmts.as_slice()
    else {
        return false;
    };
    if counter != left || !single_definition(f, *step)
        || !f.body.blocks[preheader].stmts.iter().any(|s| matches!(s,
            Statement::Assign { dest, value: Rvalue::Use(Operand::Const(Constant::I64(1))) } if dest == step))
    { return false; }
    let header_stmts: Vec<_> = f.body.blocks[header]
        .stmts
        .iter()
        .filter(|s| !matches!(s, Statement::Line(_)))
        .collect();
    let Terminator::Branch {
        cond: Operand::Local(branch),
        ..
    } = f.body.blocks[header].terminator
    else {
        return false;
    };
    if let [
        Statement::Assign {
            dest,
            value:
                Rvalue::Binary {
                    op: BinOp::Lt,
                    lhs: Operand::Local(value),
                    rhs,
                },
        },
    ] = header_stmts.as_slice()
    {
        return *dest == branch
            && value == counter
            && native_operand(f, rhs)
            && rhs != &Operand::Local(*counter);
    }
    let [pos, lt, gt, positive, neg, negative, combined] = header_stmts.as_slice() else {
        return false;
    };
    let Some(pos_id) = binary_result(
        pos,
        BinOp::Gt,
        &Operand::Local(*step),
        &Operand::Const(Constant::I64(0)),
    ) else {
        return false;
    };
    let Statement::Assign {
        dest: lt_id,
        value:
            Rvalue::Binary {
                op: BinOp::Lt,
                lhs: Operand::Local(value),
                rhs: stop,
            },
    } = lt
    else {
        return false;
    };
    if value != counter || !native_operand(f, stop) || stop == &Operand::Local(*counter) {
        return false;
    }
    let Some(gt_id) = binary_result(gt, BinOp::Gt, &Operand::Local(*counter), stop) else {
        return false;
    };
    let Some(positive_id) = binary_result(
        positive,
        BinOp::BitAnd,
        &Operand::Local(pos_id),
        &Operand::Local(*lt_id),
    ) else {
        return false;
    };
    let Statement::Assign {
        dest: neg_id,
        value:
            Rvalue::Unary {
                op: UnaryOp::Not,
                operand: Operand::Local(value),
            },
    } = neg
    else {
        return false;
    };
    if *value != pos_id {
        return false;
    }
    let Some(negative_id) = binary_result(
        negative,
        BinOp::BitAnd,
        &Operand::Local(*neg_id),
        &Operand::Local(gt_id),
    ) else {
        return false;
    };
    binary_result(
        combined,
        BinOp::BitOr,
        &Operand::Local(positive_id),
        &Operand::Local(negative_id),
    ) == Some(branch)
}
fn binary_result(
    s: &Statement,
    expected: BinOp,
    left: &Operand,
    right: &Operand,
) -> Option<LocalId> {
    match s {
        Statement::Assign {
            dest,
            value: Rvalue::Binary { op, lhs, rhs },
        } if *op == expected && lhs == left && rhs == right => Some(*dest),
        _ => None,
    }
}
