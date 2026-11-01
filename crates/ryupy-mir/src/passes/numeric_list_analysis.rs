//! 数值列表岛的局部分析工具：穷尽读取扫描与有限CFG支配关系，不引入全程序SSA。
use super::dead_values::{read_operand, read_value};
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};
pub(super) type Definitions = FxHashMap<LocalId, Vec<(BlockId, usize)>>;
pub(super) fn definitions(f: &Function) -> Definitions {
    let mut result = Definitions::default();
    for (id, body) in f.body.blocks.iter() {
        for (position, stmt) in body.stmts.iter().enumerate() {
            if let Statement::Assign { dest, .. } = stmt {
                result.entry(*dest).or_default().push((id, position));
            }
        }
    }
    result
}
pub(super) fn reads(statement: &Statement) -> FxHashSet<LocalId> {
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
pub(super) fn terminator_reads(terminator: &Terminator) -> FxHashSet<LocalId> {
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
pub(super) fn dominators(f: &Function) -> FxHashMap<BlockId, FxHashSet<BlockId>> {
    let predecessors = super::numeric_accumulator_match::predecessors(f);
    // 先取可达集合；不可达源不能凭空证明动态局部已初始化。
    let mut reachable = FxHashSet::default();
    let mut pending = vec![f.body.entry];
    while let Some(id) = pending.pop() {
        if !reachable.insert(id) {
            continue;
        }
        match &f.body.blocks[id].terminator {
            Terminator::Goto(target) => pending.push(*target),
            Terminator::Branch { then_, else_, .. } => pending.extend([*then_, *else_]),
            _ => {}
        }
    }
    let mut result: FxHashMap<_, _> = reachable
        .iter()
        .map(|id| {
            (
                *id,
                if *id == f.body.entry {
                    FxHashSet::from_iter([*id])
                } else {
                    reachable.clone()
                },
            )
        })
        .collect();
    // 至多128块，集合严格单调收缩；有限交集迭代不依赖Python运行时数据。
    loop {
        let mut changed = false;
        for &id in &reachable {
            if id == f.body.entry {
                continue;
            }
            let mut incoming = predecessors[&id]
                .iter()
                .filter(|source| reachable.contains(source));
            let mut next = incoming
                .next()
                .map(|source| result[source].clone())
                .unwrap_or_default();
            for source in incoming {
                next.retain(|item| result[source].contains(item));
            }
            next.insert(id);
            if next != result[&id] {
                result.insert(id, next);
                changed = true;
            }
        }
        if !changed {
            break;
        }
    }
    result
}
pub(super) fn dominates(
    dom: &FxHashMap<BlockId, FxHashSet<BlockId>>,
    definition: (BlockId, usize),
    usage: (BlockId, usize),
) -> bool {
    if definition.0 == usage.0 {
        definition.1 < usage.1 && dom.contains_key(&usage.0)
    } else {
        dom.get(&usage.0)
            .is_some_and(|set| set.contains(&definition.0))
    }
}
