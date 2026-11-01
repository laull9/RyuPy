//! 浮点链的循环状态策略：只接受唯一入口/出口的五块循环与本次入口的精确浮点重置。
//!
//! 共用 FloatChain 的候选、逃逸检查及尝试预算；此处再证明累加槽不可在循环中观察。
//! 外层可重入 preheader，但每次必须先重新赋初值；不猜用户类型，不改变旧整数 holder。
use super::dead_values::{read_operand, read_value};
use super::float_chain::Plan as Chain;
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub(super) struct Plan {
    pub(super) preheader: BlockId,
    pub(super) header: BlockId,
    pub(super) exit: BlockId,
    pub(super) total: LocalId,
}

pub(super) fn try_expand(function: &mut Function, chain: &Chain) -> bool {
    let Some(plan) = candidate(function, chain) else {
        return false;
    };
    let mut island = super::numeric_list::candidate(function, chain, &plan);
    let inputs = chain.inputs.len() - 1;
    // 岛预算不够只退回原FloatLoop，不能先改工厂再放弃load/sink任一端。
    if let Some(numeric) = &island
        && (function.body.locals.len()
            + 4
            + 3 * inputs
            + chain.steps.len()
            + numeric.extra_locals()
            > 512
            || function.body.blocks.len() + inputs + 10 + numeric.extra_blocks() > 128)
    {
        island = None;
    }
    // 状态四槽；其余每输入三个槽，每步一个原生结果。慢路/出口/初始化共新增九块；岛额外分离输入拆箱与算术块。
    if function.body.locals.len() + 4 + 3 * inputs + chain.steps.len() > 512
        || function.body.blocks.len() + inputs + 9 > 128
    {
        return false;
    }
    super::float_loop_expand::expand(function, chain, plan, island);
    true
}
fn candidate(function: &Function, chain: &Chain) -> Option<Plan> {
    let body = chain.block;
    let predecessor = super::numeric_accumulator_match::predecessors(function);
    let [header] = predecessor.get(&body)?.as_slice() else {
        return None;
    };
    let header = *header;
    let Terminator::Branch {
        then_, else_: exit, ..
    } = function.body.blocks[header].terminator
    else {
        return None;
    };
    if then_ != body {
        return None;
    }
    let Terminator::Goto(latch) = function.body.blocks[body].terminator else {
        return None;
    };
    if !matches!(function.body.blocks[latch].terminator, Terminator::Goto(target) if target == header)
        || predecessor.get(&latch)?.as_slice() != [body]
        || predecessor.get(&exit)?.as_slice() != [header]
    {
        return None;
    }
    let incoming = predecessor.get(&header)?;
    if incoming.len() != 2 || !incoming.contains(&latch) {
        return None;
    }
    let preheader = *incoming.iter().find(|&&source| source != latch)?;
    if !matches!(function.body.blocks[preheader].terminator, Terminator::Goto(target) if target == header)
        || [preheader, header, body, latch, exit]
            .into_iter()
            .collect::<FxHashSet<_>>()
            .len()
            != 5
        || [header, body, latch, exit].contains(&function.body.entry)
    {
        return None;
    }
    let last_position = chain.start + chain.steps.len() - 1;
    let Statement::Assign {
        dest: temporary,
        value:
            Rvalue::DynBinary {
                op: DynOp::Add | DynOp::Sub,
                lhs: Operand::Local(total),
                inplace: true,
                ..
            },
    } = &function.body.blocks[body].stmts[last_position]
    else {
        return None;
    };
    let temporary = *temporary;
    let total = *total;
    if !chain.inputs.contains(&total)
        || function.params.contains(&total)
        || function.body.locals[total].ty != MirType::Dynamic
        || !matches!(function.body.blocks[body].stmts.get(last_position + 1),
            Some(Statement::Assign { dest, value: Rvalue::Use(Operand::Local(source)) }) if *dest == total && *source == temporary)
    {
        return None;
    }
    // s 在链内只能是末条 lhs；其它读取即使没有逃逸，也会要求观察当前盒。
    if chain.steps[..chain.steps.len() - 1]
        .iter()
        .any(|step| step.lhs == total || step.rhs == total)
        || chain.steps.last()?.rhs == total
    {
        return None;
    }
    let mut writes = Vec::new();
    let mut temporary_reads = 0;
    for (id, block) in function.body.blocks.iter() {
        let reads = terminator_reads(&block.terminator);
        if reads.contains(&temporary)
            || ([header, body, latch].contains(&id) && reads.contains(&total))
        {
            return None;
        }
        for (position, statement) in block.stmts.iter().enumerate() {
            let reads = statement_reads(statement);
            if reads.contains(&temporary) {
                if id != body || position != last_position + 1 {
                    return None;
                }
                temporary_reads += 1;
            }
            if [header, body, latch].contains(&id)
                && reads.contains(&total)
                && (id != body || position != last_position)
            {
                return None;
            }
            if let Statement::Assign { dest, .. } = statement
                && *dest == total
            {
                writes.push((id, position));
            }
        }
    }
    if temporary_reads != 1 || writes.len() != 2 || !writes.contains(&(body, last_position + 1)) {
        return None;
    }
    let reset = writes.iter().find(|(id, _)| *id == preheader)?.1;
    if !initial_float(function, preheader, reset, total) {
        return None;
    }
    Some(Plan {
        preheader,
        header,
        exit,
        total,
    })
}

fn initial_float(function: &Function, block: BlockId, reset: usize, total: LocalId) -> bool {
    // 一次建全函数定义表；沿最多16层同pre块、严格向前的只读Use链寻找BoxF64。
    let mut definitions: FxHashMap<LocalId, Vec<(BlockId, usize)>> = FxHashMap::default();
    for (id, body) in function.body.blocks.iter() {
        for (position, stmt) in body.stmts.iter().enumerate() {
            if let Statement::Assign { dest, .. } = stmt {
                definitions.entry(*dest).or_default().push((id, position));
            }
        }
    }
    let mut current = reset;
    for _ in 0..16 {
        match &function.body.blocks[block].stmts[current] {
            Statement::Assign {
                value: Rvalue::Box {
                    from: MirType::F64, ..
                },
                ..
            } => return true,
            Statement::Assign {
                value: Rvalue::Use(Operand::Local(source)),
                ..
            } => {
                if *source == total || function.params.contains(source) {
                    return false;
                }
                let Some(positions) = definitions.get(source) else {
                    return false;
                };
                let [(id, position)] = positions.as_slice() else {
                    return false;
                };
                if *id != block || *position >= current {
                    return false;
                }
                current = *position;
            }
            _ => return false,
        }
    }
    false
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
            for value in value.iter().chain(cause.iter()) {
                read_operand(value, &mut reads);
            }
        }
        Terminator::Goto(_) | Terminator::Unreachable => {}
    }
    reads
}
