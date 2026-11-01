//! 累加器严格候选识别：不因表示从holder变为局部而放宽身份/异常/逃逸条件。
use super::dead_values::{read_operand, read_value};
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub(super) struct Plan {
    pub(super) preheader: BlockId,
    pub(super) body: BlockId,
    pub(super) exit: BlockId,
    pub(super) update: usize,
    pub(super) total: LocalId,
    pub(super) rhs: Operand,
}

pub(super) fn candidate(
    function: &Function,
    header: BlockId,
    predecessors: &FxHashMap<BlockId, Vec<BlockId>>,
) -> Option<Plan> {
    let Terminator::Branch {
        then_: body,
        else_: exit,
        ..
    } = &function.body.blocks[header].terminator
    else {
        return None;
    };
    let (body, exit) = (*body, *exit);
    let Terminator::Goto(latch) = function.body.blocks[body].terminator else {
        return None;
    };
    if !matches!(function.body.blocks[latch].terminator, Terminator::Goto(target) if target == header)
    {
        return None;
    }
    let incoming = predecessors.get(&header)?;
    if incoming.len() != 2 || !incoming.contains(&latch) {
        return None;
    }
    let preheader = *incoming.iter().find(|&&block| block != latch)?;
    if preheader != function.body.entry
        || !predecessors.get(&preheader)?.is_empty()
        || !matches!(function.body.blocks[preheader].terminator, Terminator::Goto(target) if target == header)
    {
        return None;
    }
    if [preheader, header, body, latch, exit]
        .into_iter()
        .collect::<FxHashSet<_>>()
        .len()
        != 5
        || function.body.entry == header
        || function.body.entry == body
        || function.body.entry == latch
        || function.body.entry == exit
        || predecessors.get(&body)?.as_slice() != [header]
        || predecessors.get(&latch)?.as_slice() != [body]
        || predecessors.get(&exit)?.as_slice() != [header]
    {
        return None;
    }
    let statements = &function.body.blocks[body].stmts;
    for (update, pair) in statements.windows(2).enumerate() {
        let Statement::Assign {
            dest: temporary,
            value:
                Rvalue::DynBinary {
                    op: DynOp::Add,
                    lhs: Operand::Local(total),
                    rhs,
                    inplace: true,
                },
        } = &pair[0]
        else {
            continue;
        };
        if !matches!(&pair[1], Statement::Assign { dest, value: Rvalue::Use(Operand::Local(source)) }
            if dest == total && source == temporary)
        {
            continue;
        }
        let local = &function.body.locals[*total];
        if local.is_cell
            || local.name.is_none()
            || local.ty != MirType::Dynamic
            || statements[update + 2..]
                .iter()
                .any(|statement| !matches!(statement, Statement::Line(_)))
        {
            continue;
        }
        // 入口必须已经直接赋值；不提前执行可能未绑定的读，也不处理LoadChecked(total)模式。
        if !function.body.blocks[preheader]
            .stmts
            .iter()
            .any(|statement| matches!(statement, Statement::Assign { dest, .. } if dest == total))
        {
            continue;
        }
        let mut rhs_reads = FxHashSet::default();
        read_operand(rhs, &mut rhs_reads);
        if rhs_reads.contains(total) {
            continue;
        }
        let loop_blocks = [header, body, latch];
        let (mut total_reads, mut total_writes, mut temporary_reads, mut temporary_writes) =
            (0, 0, 0, 0);
        for (id, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                let reads = statement_reads(statement);
                temporary_reads += usize::from(reads.contains(temporary));
                if let Statement::Assign { dest, .. } = statement {
                    temporary_writes += usize::from(dest == temporary);
                    if loop_blocks.contains(&id) {
                        total_writes += usize::from(dest == total);
                    }
                }
                if loop_blocks.contains(&id) {
                    total_reads += usize::from(reads.contains(total));
                }
            }
            let reads = terminator_reads(&block.terminator);
            temporary_reads += usize::from(reads.contains(temporary));
            if loop_blocks.contains(&id) {
                total_reads += usize::from(reads.contains(total));
            }
        }
        if total_reads == 1 && total_writes == 1 && temporary_reads == 1 && temporary_writes == 1 {
            return Some(Plan {
                preheader,
                body,
                exit,
                update,
                total: *total,
                rhs: rhs.clone(),
            });
        }
    }
    None
}

pub(super) fn predecessors(function: &Function) -> FxHashMap<BlockId, Vec<BlockId>> {
    let mut result: FxHashMap<_, Vec<_>> = function
        .body
        .blocks
        .iter()
        .map(|(id, _)| (id, vec![]))
        .collect();
    for (id, block) in function.body.blocks.iter() {
        let destinations = match &block.terminator {
            Terminator::Goto(target) | Terminator::Suspend { resume: target, .. } => vec![*target],
            Terminator::Branch { then_, else_, .. } => vec![*then_, *else_],
            Terminator::Return(_) | Terminator::Raise { .. } | Terminator::Unreachable => vec![],
        };
        for target in destinations {
            if let Some(incoming) = result.get_mut(&target) {
                incoming.push(id);
            }
        }
    }
    result
}

fn statement_reads(statement: &Statement) -> FxHashSet<LocalId> {
    let mut reads = FxHashSet::default();
    match statement {
        Statement::Assign { value, .. } | Statement::Eval(value) => read_value(value, &mut reads),
        Statement::SetField { obj, value, .. } => {
            read_operand(obj, &mut reads);
            read_operand(value, &mut reads);
        }
        Statement::SetGlobal { value, .. } => read_operand(value, &mut reads),
        Statement::Line(_) => {}
    }
    reads
}
fn terminator_reads(terminator: &Terminator) -> FxHashSet<LocalId> {
    let mut reads = FxHashSet::default();
    match terminator {
        Terminator::Branch { cond, .. } => read_operand(cond, &mut reads),
        Terminator::Suspend { value, input, .. } => {
            read_operand(value, &mut reads);
            if let Some(input) = input {
                reads.insert(*input);
            }
        }
        Terminator::Return(value) => {
            if let Some(value) = value {
                read_operand(value, &mut reads);
            }
        }
        Terminator::Raise { value, cause } => {
            for operand in value.iter().chain(cause.iter()) {
                read_operand(operand, &mut reads);
            }
        }
        Terminator::Goto(_) | Terminator::Unreachable => {}
    }
    reads
}
