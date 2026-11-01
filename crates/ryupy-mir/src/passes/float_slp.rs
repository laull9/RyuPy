//! 两 lane 新鲜浮点链的有界 SLP：只配对有序依赖拓扑相同、连续且无观察的产物。
//!
//! 容器与未配对尾项保持原位置；每lane分别装箱，不能把数值相同当作可共享对象身份。
//! 全函数≤16次尝试/≤4次成功；拒绝失败同样消耗预算，不跨用户调用、Line或Use。
use super::MirPass;
use super::numeric_list_analysis::{Definitions, definitions, reads, terminator_reads};
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub struct FloatSlp {
    /// 公平测量控制臂：保同ISA/type守卫，仅用原生标量F64发射快路。
    pub scalar: bool,
}
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(super) enum Source {
    Input(usize),
    Step(usize),
}
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(super) struct Operation {
    pub(super) op: BinOp,
    pub(super) lhs: Source,
    pub(super) rhs: Source,
}
pub(super) struct Lane {
    pub(super) start: usize,
    pub(super) finish: usize,
    pub(super) final_dest: LocalId,
    pub(super) inputs: Vec<LocalId>,
    pub(super) destinations: Vec<LocalId>,
    pub(super) operations: Vec<Operation>,
}
pub(super) struct Plan {
    pub(super) block: BlockId,
    pub(super) lanes: [Lane; 2],
    pub(super) inputs: Vec<LocalId>,
}
impl MirPass for FloatSlp {
    fn name(&self) -> &'static str {
        "float_slp"
    }
    fn run(&self, f: &mut Function) {
        if !eligible(f) {
            return;
        }
        let mut candidates = Vec::new();
        'scan: for (_, body) in f.body.blocks.iter() {
            for stmt in &body.stmts {
                let Statement::Assign {
                    dest,
                    value:
                        Rvalue::CallWithArgs {
                            callee: RuntimeFn::MakeList | RuntimeFn::MakeTuple,
                            prefix,
                            args,
                        },
                } = stmt
                else {
                    continue;
                };
                if !prefix.is_empty() || args.len() > 8 {
                    continue;
                }
                for low in 0..args.len() {
                    for high in low + 1..args.len() {
                        if matches!(
                            (&args[low], &args[high]),
                            (Operand::Local(_), Operand::Local(_))
                        ) {
                            candidates.push((*dest, low, high));
                            if candidates.len() == 16 {
                                break 'scan;
                            }
                        }
                    }
                }
            }
        }
        let mut successes = 0;
        for (sink, low, high) in candidates {
            if successes == 4 {
                break;
            }
            let definitions = definitions(f);
            let Some(locations) = definitions.get(&sink) else {
                continue;
            };
            let [(block, position)] = locations.as_slice() else {
                continue;
            };
            let Some(plan) = candidate(f, &definitions, *block, *position, low, high) else {
                continue;
            };
            // 每个外部引用快照/拆箱/guard三槽；packed输入/每步向量/两lane结果/ISA/慢路末值。
            let vector_extra = plan.lanes[0].inputs.len() + plan.lanes[0].operations.len() + 5;
            let scalar_extra = 2 * plan.lanes[0].operations.len() + 3;
            let extra = 3 * plan.inputs.len() + vector_extra.max(scalar_extra);
            if f.body.locals.len() + extra > 512
                || f.body.blocks.len() + plan.inputs.len() + 4 > 128
            {
                continue;
            }
            super::float_slp_expand::expand(f, plan, self.scalar);
            successes += 1;
        }
    }
}
fn eligible(f: &Function) -> bool {
    f.signature_global.is_some()
        && f.env.is_none()
        && f.body.regions.is_empty()
        && f.body.blocks.len() <= 128
        && f.body.locals.len() <= 512
        && f.body
            .blocks
            .iter()
            .map(|(_, b)| b.stmts.len())
            .sum::<usize>()
            <= 4096
        && !f.body.locals.iter().any(|(_, l)| l.is_cell)
        && !f.body.blocks.iter().any(|(_, b)| {
            matches!(b.terminator, Terminator::Suspend { .. })
                || b.stmts.iter().any(|s| {
                    matches!(
                        s,
                        Statement::Assign {
                            value: Rvalue::MakeGenerator { .. },
                            ..
                        } | Statement::Eval(Rvalue::MakeGenerator { .. })
                    )
                })
        })
}
fn candidate(
    f: &Function,
    defs: &Definitions,
    block: BlockId,
    sink: usize,
    low: usize,
    high: usize,
) -> Option<Plan> {
    let Statement::Assign {
        value: Rvalue::CallWithArgs { args, .. },
        ..
    } = &f.body.blocks[block].stmts[sink]
    else {
        return None;
    };
    let (Operand::Local(left), Operand::Local(right)) = (&args[low], &args[high]) else {
        return None;
    };
    if left == right
        || args
            .iter()
            .filter(|op| matches!(op,Operand::Local(id)if id==left))
            .count()
            != 1
        || args
            .iter()
            .filter(|op| matches!(op,Operand::Local(id)if id==right))
            .count()
            != 1
    {
        return None;
    }
    let lane0 = lane(f, defs, block, sink, *left)?;
    let lane1 = lane(f, defs, block, sink, *right)?;
    if lane0.finish + 1 != lane1.start || lane0.operations != lane1.operations {
        return None;
    }
    let destinations: FxHashSet<_> = lane0
        .destinations
        .iter()
        .chain(&lane1.destinations)
        .copied()
        .collect();
    if destinations.len() != lane0.destinations.len() + lane1.destinations.len() {
        return None;
    }
    let mut inputs = Vec::new();
    let mut seen = FxHashSet::default();
    for input in lane0.inputs.iter().chain(&lane1.inputs) {
        if destinations.contains(input) {
            return None;
        }
        if seen.insert(*input) {
            inputs.push(*input);
        }
    }
    // 最终值只在同一个容器直接消费一次；任何其它读（含tail回调参数/嵌套Array）拒绝。
    for (id, body) in f.body.blocks.iter() {
        if !terminator_reads(&body.terminator).is_disjoint(&destinations) {
            return None;
        }
        for (position, stmt) in body.stmts.iter().enumerate() {
            if id == block && (lane0.start..=lane1.finish).contains(&position) {
                continue;
            }
            let used = reads(stmt);
            if used.is_disjoint(&destinations) {
                continue;
            }
            if id != block
                || position != sink
                || used
                    .intersection(&destinations)
                    .any(|id| id != left && id != right)
            {
                return None;
            }
        }
    }
    Some(Plan {
        block,
        lanes: [lane0, lane1],
        inputs,
    })
}
fn dyn_operation(stmt: &Statement) -> Option<(LocalId, BinOp, &Operand, &Operand)> {
    let Statement::Assign {
        dest,
        value:
            Rvalue::DynBinary {
                op,
                lhs,
                rhs,
                inplace: false,
            },
    } = stmt
    else {
        return None;
    };
    let op = match op {
        DynOp::Add => BinOp::Add,
        DynOp::Sub => BinOp::Sub,
        DynOp::Mul => BinOp::Mul,
        _ => return None,
    };
    Some((*dest, op, lhs, rhs))
}
fn lane(
    f: &Function,
    defs: &Definitions,
    block: BlockId,
    sink: usize,
    final_dest: LocalId,
) -> Option<Lane> {
    let [(id, finish)] = defs.get(&final_dest)?.as_slice() else {
        return None;
    };
    if *id != block || *finish >= sink {
        return None;
    }
    let finish = *finish;
    let statements = &f.body.blocks[block].stmts;
    let mut boundary = finish;
    while boundary > 0 && dyn_operation(&statements[boundary - 1]).is_some() {
        boundary -= 1;
    }
    let mut pending = vec![finish];
    let mut positions = FxHashSet::default();
    while let Some(position) = pending.pop() {
        if !positions.insert(position) {
            continue;
        }
        if positions.len() > 8 {
            return None;
        }
        let (dest, _, lhs, rhs) = dyn_operation(&statements[position])?;
        if f.params.contains(&dest)
            || f.body.locals[dest].name.is_some()
            || f.body.locals[dest].ty != MirType::Dynamic
            || defs.get(&dest)?.as_slice() != [(block, position)]
        {
            return None;
        }
        for operand in [lhs, rhs] {
            let Operand::Local(source) = operand else {
                return None;
            };
            if f.body.locals[*source].ty != MirType::Dynamic {
                return None;
            }
            if let Some(locations) = defs.get(source)
                && let [(source_block, source_position)] = locations.as_slice()
                && *source_block == block
                && (boundary..=finish).contains(source_position)
            {
                if *source_position >= position {
                    return None;
                }
                pending.push(*source_position);
            }
        }
    }
    let start = *positions.iter().min()?;
    if !(2..=8).contains(&positions.len()) || positions.len() != finish - start + 1 {
        return None;
    }
    let mut known = FxHashMap::default();
    let mut input_slots = FxHashMap::default();
    let mut inputs = Vec::new();
    let mut destinations = Vec::new();
    let mut operations = Vec::new();
    for (index, stmt) in statements[start..=finish].iter().enumerate() {
        let (dest, op, lhs, rhs) = dyn_operation(stmt)?;
        let mut source = |operand: &Operand| -> Option<Source> {
            let Operand::Local(local) = operand else {
                return None;
            };
            if let Some(step) = known.get(local) {
                Some(Source::Step(*step))
            } else {
                let slot = *input_slots.entry(*local).or_insert_with(|| {
                    let index = inputs.len();
                    inputs.push(*local);
                    index
                });
                Some(Source::Input(slot))
            }
        };
        operations.push(Operation {
            op,
            lhs: source(lhs)?,
            rhs: source(rhs)?,
        });
        known.insert(dest, index);
        destinations.push(dest);
    }
    Some(Lane {
        start,
        finish,
        final_dest,
        inputs,
        destinations,
        operations,
    })
}
