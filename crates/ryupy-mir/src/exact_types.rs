//! 非SSA的有限must类型事实：可达输入为空不等于尚未访问；合流取交集，写入杀死旧证明。
//! 目前只证明boxed Int64，并仅用于内部精确拆箱；不从Python标注/isinstance推断精确类型。
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};
use std::collections::VecDeque;

#[derive(Clone, Debug, Default, PartialEq, Eq)]
struct Facts {
    exact: FxHashSet<LocalId>,
    /// 条件局部 → 被测试的冻结局部；不传播predicate别名，避免漏跟踪依赖写入。
    predicates: FxHashMap<LocalId, LocalId>,
}
impl Facts {
    fn meet(&mut self, other: &Self) {
        self.exact.retain(|local| other.exact.contains(local));
        self.predicates
            .retain(|cond, subject| other.predicates.get(cond) == Some(subject));
    }
    fn transfer(&mut self, stmt: &Statement) {
        let Statement::Assign { dest, value } = stmt else {
            return;
        };
        // 先读右值事实，再kill目标；x=x保留类型，但任何旧predicate依赖都保守作废。
        let exact = match value {
            Rvalue::Box {
                from: MirType::I64, ..
            } => true,
            Rvalue::Use(Operand::Local(source)) => self.exact.contains(source),
            _ => false,
        };
        let predicate = match value {
            Rvalue::IsBoxedI64 {
                value: Operand::Local(subject),
            } if subject != dest => Some(*subject),
            _ => None,
        };
        self.exact.remove(dest);
        self.predicates
            .retain(|cond, subject| cond != dest && subject != dest);
        if exact {
            self.exact.insert(*dest);
        }
        if let Some(subject) = predicate {
            self.predicates.insert(*dest, subject);
        }
    }
}

fn inputs(function: &Function) -> FxHashMap<BlockId, Facts> {
    let mut states = FxHashMap::default();
    let mut queue = VecDeque::from([function.body.entry]);
    states.insert(function.body.entry, Facts::default());
    while let Some(id) = queue.pop_front() {
        let Some((_, block)) = function
            .body
            .blocks
            .iter()
            .find(|(candidate, _)| *candidate == id)
        else {
            continue;
        };
        let mut output = states[&id].clone();
        for stmt in &block.stmts {
            output.transfer(stmt);
        }
        let mut edges = Vec::new();
        match &block.terminator {
            Terminator::Goto(target) => edges.push((*target, output)),
            Terminator::Branch { cond, then_, else_ } => {
                let mut yes = output.clone();
                if let Operand::Local(condition) = cond
                    && let Some(subject) = output.predicates.get(condition).copied()
                {
                    yes.exact.insert(subject);
                    output.exact.remove(&subject);
                }
                edges.push((*then_, yes));
                edges.push((*else_, output));
            }
            _ => {}
        }
        for (target, incoming) in edges {
            if let Some(previous) = states.get_mut(&target) {
                let before = previous.clone();
                previous.meet(&incoming);
                if *previous != before {
                    queue.push_back(target);
                }
            } else {
                states.insert(target, incoming);
                queue.push_back(target);
            }
        }
    }
    states
}

/// 一次固定点和一次线扫，仅记录实参当前均被证明为boxed Int64的动态调用位置。
/// 不存逐点全量facts、不每个调用重新分析；未达块与已达无证明的块都不会产生候选。
pub(crate) fn exact_call_sites(function: &Function) -> FxHashSet<(BlockId, usize)> {
    let states = inputs(function);
    let mut sites = FxHashSet::default();
    for (id, block) in function.body.blocks.iter() {
        let Some(input) = states.get(&id) else {
            continue;
        };
        let mut facts = input.clone();
        for (position, stmt) in block.stmts.iter().enumerate() {
            if let Statement::Assign {
                value: Rvalue::DynCallValue { args, .. },
                ..
            } = stmt
                && args
                    .iter()
                    .all(|arg| matches!(arg, Operand::Local(local) if facts.exact.contains(local)))
            {
                sites.insert((id, position));
            }
            facts.transfer(stmt);
        }
    }
    sites
}

/// 在固定点之后检验所有拆箱点；不在worklist中途用尚未到达的回边充当证明。
pub(crate) fn validate(program: &Program, function: &Function, errors: &mut Vec<String>) {
    let has_primitives = function.body.blocks.iter().any(|(_, b)| {
        b.stmts.iter().any(|s| {
            matches!(
                s,
                Statement::Assign {
                    value: Rvalue::IsBoxedI64 { .. }
                        | Rvalue::UnboxExactI64 { .. }
                        | Rvalue::Binary {
                            op: BinOp::WrappingAdd,
                            ..
                        },
                    ..
                } | Statement::Eval(
                    Rvalue::IsBoxedI64 { .. }
                        | Rvalue::UnboxExactI64 { .. }
                        | Rvalue::Binary {
                            op: BinOp::WrappingAdd,
                            ..
                        }
                )
            )
        })
    });
    if !has_primitives {
        return;
    }
    if !function.body.regions.is_empty()
        || function
            .body
            .blocks
            .iter()
            .any(|(_, b)| matches!(b.terminator, Terminator::Suspend { .. }))
    {
        errors.push(format!(
            "函数 `{}` 的精确数值原语暂不支持EH/暂停边事实传播",
            function.name
        ));
        return;
    }
    let states = inputs(function);
    for (id, block) in function.body.blocks.iter() {
        let mut facts = states.get(&id).cloned().unwrap_or_default();
        for stmt in &block.stmts {
            let (dest, value) = match stmt {
                Statement::Assign { dest, value } => (Some(*dest), value),
                Statement::Eval(value) => (None, value),
                _ => continue,
            };
            let expected = match value {
                Rvalue::IsBoxedI64 { .. } => Some(MirType::Bool),
                Rvalue::UnboxExactI64 { value } => {
                    if !matches!(value, Operand::Local(local) if facts.exact.contains(local)) {
                        errors.push(format!(
                            "函数 `{}` 块 {:?} 的精确拆箱缺少当前局部的boxed Int64真边证明",
                            function.name, id
                        ));
                    }
                    Some(MirType::I64)
                }
                Rvalue::Binary {
                    op: BinOp::WrappingAdd,
                    lhs,
                    rhs,
                } => {
                    if [lhs, rhs].iter().any(|op| {
                        crate::validate::operand_type(program, function, op) != Some(MirType::I64)
                    }) {
                        errors.push(format!(
                            "函数 `{}` 的WrappingAdd只允许i64操作数",
                            function.name
                        ));
                    }
                    Some(MirType::I64)
                }
                _ => None,
            };
            if let Some(expected) = expected {
                let actual = dest.and_then(|local| {
                    function
                        .body
                        .locals
                        .iter()
                        .find(|(id, _)| *id == local)
                        .map(|(_, d)| d.ty.clone())
                });
                if actual != Some(expected) {
                    errors.push(format!(
                        "函数 `{}` 的精确数值原语必须写入匹配类型的局部，不能丢弃结果",
                        function.name
                    ));
                }
            }
            facts.transfer(stmt);
        }
    }
}

#[cfg(test)]
#[path = "exact_types_tests.rs"]
mod tests;
