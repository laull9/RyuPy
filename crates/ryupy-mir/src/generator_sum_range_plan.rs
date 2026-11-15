//! 关闭态range来源证据：入口仍需检查真实sealed PyRangeIter，不把源码range名字视为证明。
//! 这里只识别单游标+精确整数乘法；不替换MoveNext、不消除盒、不做预求和或原生计数。
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

#[derive(Clone, Copy, Debug, PartialEq)]
enum Fact {
    // 只有对应入口的运行时类型守卫成功后，才可假定此事实成立。
    GuardedRangeCursor,
    ExactLong,
    BuiltinInteger,
    Boolean,
    None,
    TraceDepth,
}

#[derive(Debug)]
pub(super) struct RangePlan {
    pub(super) guard_input: LocalId,
    iterator: LocalId,
    multiply: LocalId,
    pub(super) yield_block: BlockId,
    resume: BlockId,
    pub(super) residents: FxHashSet<LocalId>,
}

impl RangePlan {
    // 首次接线进一步要求yield直接发布已核验的乘法结果及原恢复边，不接别名扩展形状。
    pub(super) fn direct_yield(&self, function: &Function) -> bool {
        matches!(&function.body.blocks[self.yield_block].terminator,
            Terminator::Suspend { value: Operand::Local(value), resume, .. }
                if *value == self.multiply && *resume == self.resume)
    }
}

// 这里只核对元数据内的数值ID记录；尚未绑定Program构建身份，不能用作产品证书。
// 产品化必须用拥有式Prepared对象封装同一Program/元数据，不能接受自由组合。
pub(super) fn analyze_source(
    program: &Program,
    deferred: &crate::generator_product::DeferredGenerators,
    function: FnId,
    input: LocalId,
) -> Option<RangePlan> {
    if !deferred.comprehension_inputs.contains(&(function, input)) {
        return None;
    }
    let (_, f) = program.functions.iter().find(|(id, _)| *id == function)?;
    analyze(f, input)
}

fn analyze(f: &Function, declared_input: LocalId) -> Option<RangePlan> {
    analyze_explained(f, declared_input).ok()
}

// 仅给受控测试提供精确拒绝点；与Option入口共享同一判定，不另设宽松诊断路径。
fn analyze_explained(f: &Function, declared_input: LocalId) -> Result<RangePlan, String> {
    if !crate::generator_state_analysis::eligible(f) || f.params.first() != Some(&declared_input) {
        return Err("字段资格或输入参数不匹配".to_owned());
    }
    // 降级器仍为全函数暂停边分配恢复PC；不可达的额外Suspend也会改变状态机布局。
    // 因而只忽略普通死尾块的定义，不能把额外暂停边视为无影响数据。
    let suspend_count = f
        .body
        .blocks
        .iter()
        .filter(|(_, block)| matches!(block.terminator, Terminator::Suspend { .. }))
        .count();
    if suspend_count != 1 {
        return Err("全函数必须恰有一个暂停边".to_owned());
    }
    let reachable = reachable_blocks(f).ok_or_else(|| "不可识别的控制流".to_owned())?;
    // 非SSA函数只接受每个局部单个静态定义点，并证明它支配所有实际读取。
    // 此原型仍需运行时真实类型守卫，不向产品lowering暴露。
    let mut definitions = FxHashSet::default();
    let mut frame_depth = None;
    for (block_id, block) in f
        .body
        .blocks
        .iter()
        .filter(|(id, _)| reachable.contains(id))
    {
        for (position, statement) in block.stmts.iter().enumerate() {
            if let Statement::Assign { dest, .. } = statement {
                if f.params.contains(dest) || !definitions.insert(*dest) {
                    return Err(format!(
                        "参数被覆盖或静态定义重复，位置 {block_id:?}:{position}: {statement:?}"
                    ));
                }
                if let Statement::Assign {
                    value:
                        Rvalue::Call {
                            callee:
                                Callee::Runtime(RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin),
                            ..
                        },
                    ..
                } = statement
                    && (block_id != f.body.entry || frame_depth.replace(*dest).is_some())
                {
                    return Err(format!("trace帧非入口或重复，位置 {block_id:?}:{position}"));
                }
            }
        }
    }
    let frame_depth = frame_depth.ok_or_else(|| "缺少trace帧定义".to_owned())?;
    if !dominates_all_reads(f) {
        return Err("定义未支配读取（含循环首次迭代）".to_owned());
    }
    let mut facts = FxHashMap::default();
    facts.insert(declared_input, Fact::GuardedRangeCursor);
    for _ in 0..f.body.locals.len() + 1 {
        let before = facts.len();
        for (_, block) in f
            .body
            .blocks
            .iter()
            .filter(|(id, _)| reachable.contains(id))
        {
            for statement in &block.stmts {
                if let Statement::Assign { dest, value } = statement
                    && let Some(fact) = infer(value, &facts)
                {
                    facts.insert(*dest, fact);
                }
            }
        }
        if facts.len() == before {
            break;
        }
    }
    let mut iterator = None;
    let mut multiply = None;
    let mut edge = None;
    for (id, block) in f
        .body
        .blocks
        .iter()
        .filter(|(id, _)| reachable.contains(id))
    {
        for (position, statement) in block.stmts.iter().enumerate() {
            match statement {
                Statement::Line(_) => {}
                Statement::Assign { dest, value } => {
                    infer(value, &facts).ok_or_else(|| {
                        format!("未知值，位置 {id:?}:{position}: {value:?}; facts={facts:?}")
                    })?;
                    if matches!(
                        value,
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::IterStart),
                            ..
                        }
                    ) {
                        if iterator.replace(*dest).is_some() {
                            return Err(format!("多游标定义，位置 {id:?}:{position}"));
                        }
                    }
                    if matches!(value, Rvalue::DynBinary { .. })
                        && multiply.replace(*dest).is_some()
                    {
                        return Err(format!("多个动态算术定义，位置 {id:?}:{position}"));
                    }
                }
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TracePopTo),
                    args,
                }) if args.as_slice() == [Operand::Local(frame_depth)]
                    && fact(&args[0], &facts) == Some(Fact::TraceDepth) => {}
                _ => {
                    return Err(format!(
                        "未批准语句，位置 {id:?}:{position}: {statement:?}; frame={frame_depth:?}"
                    ));
                }
            }
        }
        match &block.terminator {
            Terminator::Goto(_) | Terminator::Return(None) => {}
            Terminator::Return(Some(value)) if fact(value, &facts) == Some(Fact::None) => {}
            Terminator::Branch { cond, .. } if fact(cond, &facts) == Some(Fact::Boolean) => {}
            Terminator::Suspend {
                value,
                resume,
                input: None,
                delegated: false,
            } if fact(value, &facts) == Some(Fact::BuiltinInteger) => {
                if edge.replace((id, *resume)).is_some() {
                    return Err(format!("多个暂停边，位置 {id:?}"));
                }
            }
            _ => {
                return Err(format!(
                    "未批准终结符，位置 {id:?}: {:?}; facts={facts:?}",
                    block.terminator
                ));
            }
        }
    }
    let (yield_block, resume) = edge.ok_or_else(|| "缺少整数yield边".to_owned())?;
    let residents = crate::generator_state_analysis::residents(f)
        .ok_or_else(|| "resident分析预算耗尽".to_owned())?;
    let plan = RangePlan {
        guard_input: declared_input,
        iterator: iterator.ok_or_else(|| "缺少游标".to_owned())?,
        multiply: multiply.ok_or_else(|| "缺少乘法".to_owned())?,
        yield_block,
        resume,
        residents,
    };
    if resident_set_requirements_hold(&plan, &plan.residents) {
        Ok(plan)
    } else {
        Err(format!("resident缺少输入或游标: {plan:?}"))
    }
}

// 这里只检查候选集合要求，不是实际lowering字段映射证明。
// 发射后仍须核查真实Get/SetField位置/类型、yield前发布及resume读取同一字段。
fn resident_set_requirements_hold(plan: &RangePlan, fields: &FxHashSet<LocalId>) -> bool {
    plan.residents.is_subset(fields)
        && fields.contains(&plan.guard_input)
        && fields.contains(&plan.iterator)
}

// 入口可达图包含暂停后的恢复边。无EH资格由调用者先核验；未知终结符不能当作终点。
// 合成的无前驱return尾块不参与定义/事实，但一旦出现可达边即恢复全部检查。
fn reachable_blocks(f: &Function) -> Option<FxHashSet<BlockId>> {
    let mut reachable = FxHashSet::default();
    let mut queue = vec![f.body.entry];
    while let Some(id) = queue.pop() {
        let (_, block) = f
            .body
            .blocks
            .iter()
            .find(|(candidate, _)| *candidate == id)?;
        if !reachable.insert(id) {
            continue;
        }
        match &block.terminator {
            Terminator::Goto(next) => queue.push(*next),
            Terminator::Branch { then_, else_, .. } => queue.extend([*then_, *else_]),
            Terminator::Suspend { resume, .. } => queue.push(*resume),
            Terminator::Return(_) => {}
            _ => return None,
        }
    }
    Some(reachable)
}

// 保守的全CFG支配检查：单一静态赋值不能自动证明循环首次读取已经初始化。
fn dominates_all_reads(f: &Function) -> bool {
    use crate::local_access::{statement_reads, terminator_reads};
    let next = |term: &Terminator| -> Vec<BlockId> {
        match term {
            Terminator::Goto(id) => vec![*id],
            Terminator::Branch { then_, else_, .. } => vec![*then_, *else_],
            Terminator::Suspend { resume, .. } => vec![*resume],
            _ => vec![],
        }
    };
    let Some(reachable) = reachable_blocks(f) else {
        return false;
    };
    let mut predecessors: FxHashMap<BlockId, Vec<BlockId>> = FxHashMap::default();
    for &id in &reachable {
        for target in next(&f.body.blocks[id].terminator) {
            predecessors.entry(target).or_default().push(id);
        }
    }
    let mut dom: FxHashMap<BlockId, FxHashSet<BlockId>> = reachable
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
    let mut converged = false;
    for _ in 0..=reachable.len() {
        let mut changed = false;
        for &id in &reachable {
            if id == f.body.entry {
                continue;
            }
            let Some(previous) = predecessors.get(&id) else {
                return false;
            };
            let mut common = reachable.clone();
            for predecessor in previous {
                common.retain(|item| dom[predecessor].contains(item));
            }
            common.insert(id);
            if dom[&id] != common {
                dom.insert(id, common);
                changed = true;
            }
        }
        if !changed {
            converged = true;
            break;
        }
    }
    if !converged {
        return false;
    }
    let mut definitions = FxHashMap::default();
    for &id in &reachable {
        for (index, statement) in f.body.blocks[id].stmts.iter().enumerate() {
            if let Statement::Assign { dest, .. } = statement {
                definitions.insert(*dest, (id, index));
            }
        }
    }
    for &id in &reachable {
        let block = &f.body.blocks[id];
        for (at, reads) in block
            .stmts
            .iter()
            .map(statement_reads)
            .chain(std::iter::once(terminator_reads(&block.terminator)))
            .enumerate()
        {
            for local in reads {
                if f.params.contains(&local) {
                    continue;
                }
                let Some(&(definition, index)) = definitions.get(&local) else {
                    return false;
                };
                if !dom[&id].contains(&definition) || definition == id && index >= at {
                    return false;
                }
            }
        }
    }
    true
}

fn fact(value: &Operand, facts: &FxHashMap<LocalId, Fact>) -> Option<Fact> {
    match value {
        Operand::Local(id) => facts.get(id).copied(),
        Operand::Const(Constant::I64(_)) => Some(Fact::ExactLong),
        Operand::Const(Constant::None) => Some(Fact::None),
        _ => None,
    }
}

fn infer(value: &Rvalue, facts: &FxHashMap<LocalId, Fact>) -> Option<Fact> {
    match value {
        Rvalue::Use(value)
        | Rvalue::Box {
            value,
            from: MirType::I64,
        } => fact(value, facts),
        Rvalue::DynBinary {
            op: DynOp::Mul,
            lhs,
            rhs,
            inplace: false,
        } if fact(lhs, facts) == Some(Fact::ExactLong)
            && fact(rhs, facts) == Some(Fact::ExactLong) =>
        {
            // long×long可能升级BigInteger；不能声称ExactLong或换成unchecked乘法。
            Some(Fact::BuiltinInteger)
        }
        Rvalue::Call {
            callee: Callee::Runtime(runtime),
            args,
        } => match runtime {
            RuntimeFn::IterStart | RuntimeFn::IterNextOk | RuntimeFn::IterValue
                if args.len() == 1 && fact(&args[0], facts) == Some(Fact::GuardedRangeCursor) =>
            {
                Some(match runtime {
                    RuntimeFn::IterStart => Fact::GuardedRangeCursor,
                    RuntimeFn::IterNextOk => Fact::Boolean,
                    _ => Fact::ExactLong,
                })
            }
            RuntimeFn::DynNone if args.is_empty() => Some(Fact::None),
            // 守卫由实际工厂参数提供；trace来源不用于推断range或用户值。
            RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin => Some(Fact::TraceDepth),
            _ => None,
        },
        _ => None,
    }
}

#[cfg(test)]
#[path = "generator_sum_range_plan_tests.rs"]
mod tests;
