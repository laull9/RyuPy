//! 有界精确浮点算术链：守卫只放宽表示，不把动态协议或操作次序交给后端猜测。
//!
//! 首版仅连续的 2–8 条 Add/Sub/Mul，所有输入是动态局部；中间值无名字、无外部读取。
//! 每函数最多尝试 16 个候选（失败也计入），块/局部/语句预算限制拒绝路径扫描成本。
use super::MirPass;
use super::dead_values::{read_operand, read_value};
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub struct FloatChain;
const MAX_STEPS: usize = 8;
const MAX_INPUTS: usize = 8;
const MAX_ATTEMPTS: usize = 16;
const MAX_BLOCKS: usize = 128;
const MAX_LOCALS: usize = 512;
const MAX_STATEMENTS: usize = 4096;

pub(super) struct Step {
    pub(super) dest: LocalId,
    pub(super) op: BinOp,
    pub(super) lhs: LocalId,
    pub(super) rhs: LocalId,
}
pub(super) struct Plan {
    pub(super) block: BlockId,
    pub(super) start: usize,
    pub(super) steps: Vec<Step>,
    pub(super) inputs: Vec<LocalId>,
}

impl MirPass for FloatChain {
    fn name(&self) -> &'static str {
        "float_chain"
    }
    fn run(&self, function: &mut Function) {
        if !eligible(function) {
            return;
        }
        let mut candidates = Vec::new();
        // 初次只记每个最大连续段的稳定首 dest；不在改写后重访慢路中的同一段。
        'blocks: for (_, body) in function.body.blocks.iter() {
            let mut inside = false;
            for stmt in &body.stmts {
                if let Some((dest, ..)) = dynamic_step(stmt) {
                    if !inside {
                        candidates.push(dest);
                        if candidates.len() == MAX_ATTEMPTS {
                            break 'blocks;
                        }
                    }
                    inside = true;
                } else {
                    inside = false;
                }
            }
        }
        let mut loop_successes = 0;
        for dest in candidates {
            let definitions = definitions(function);
            let Some(positions) = definitions.get(&dest) else {
                continue;
            };
            let [(block, start)] = positions.as_slice() else {
                continue;
            };
            let Some(plan) = candidate(function, &definitions, *block, *start) else {
                continue;
            };
            // 同一初始候选只选一种表示；慢路原链不会进入新一轮扫描。
            if loop_successes < 4 && super::float_loop::try_expand(function, &plan) {
                loop_successes += 1;
                continue;
            }
            // 每输入三个局部（快照/guard/unbox），每步一个原生结果；CFG至多inputs+3块。
            if function.body.locals.len() + 3 * plan.inputs.len() + plan.steps.len() > MAX_LOCALS
                || function.body.blocks.len() + plan.inputs.len() + 3 > MAX_BLOCKS
            {
                continue;
            }
            super::float_chain_expand::expand(function, plan);
        }
    }
}
fn eligible(function: &Function) -> bool {
    function.signature_global.is_some()
        && function.env.is_none()
        && function.body.regions.is_empty()
        && function.body.locals.len() <= MAX_LOCALS
        && function.body.blocks.len() <= MAX_BLOCKS
        && function
            .body
            .blocks
            .iter()
            .map(|(_, block)| block.stmts.len())
            .sum::<usize>()
            <= MAX_STATEMENTS
        && !function.body.locals.iter().any(|(_, local)| local.is_cell)
        && !function.body.blocks.iter().any(|(_, block)| {
            matches!(block.terminator, Terminator::Suspend { .. })
                || block.stmts.iter().any(|stmt| {
                    matches!(
                        stmt,
                        Statement::Assign {
                            value: Rvalue::MakeGenerator { .. },
                            ..
                        } | Statement::Eval(Rvalue::MakeGenerator { .. })
                    )
                })
        })
}

type Definitions = FxHashMap<LocalId, Vec<(BlockId, usize)>>;
fn definitions(function: &Function) -> Definitions {
    let mut result = Definitions::default();
    for (block, body) in function.body.blocks.iter() {
        for (position, statement) in body.stmts.iter().enumerate() {
            if let Statement::Assign { dest, .. } = statement {
                result.entry(*dest).or_default().push((block, position));
            }
        }
    }
    result
}
fn dynamic_step(statement: &Statement) -> Option<(LocalId, DynOp, &Operand, &Operand, bool)> {
    match statement {
        Statement::Assign {
            dest,
            value:
                Rvalue::DynBinary {
                    op,
                    lhs,
                    rhs,
                    inplace,
                },
        } => Some((*dest, *op, lhs, rhs, *inplace)),
        _ => None,
    }
}
fn candidate(
    function: &Function,
    definitions: &Definitions,
    block: BlockId,
    start: usize,
) -> Option<Plan> {
    let statements = &function.body.blocks[block].stmts;
    let count = statements[start..]
        .iter()
        .take_while(|stmt| dynamic_step(stmt).is_some())
        .count();
    if !(2..=MAX_STEPS).contains(&count) {
        return None;
    }
    let mut destinations = FxHashSet::default();
    for (offset, stmt) in statements[start..start + count].iter().enumerate() {
        let (dest, _, _, _, _) = dynamic_step(stmt)?;
        if function.body.locals[dest].ty != MirType::Dynamic
            || function.params.contains(&dest)
            || (offset + 1 != count && function.body.locals[dest].name.is_some())
            || definitions.get(&dest)?.as_slice() != [(block, start + offset)]
            || !destinations.insert(dest)
        {
            return None;
        }
    }
    let mut plan = Plan {
        block,
        start,
        steps: vec![],
        inputs: vec![],
    };
    let mut produced = FxHashSet::default();
    let mut inputs = FxHashSet::default();
    let mut consumed = FxHashSet::default();
    for (offset, stmt) in statements[start..start + count].iter().enumerate() {
        let (dest, op, lhs, rhs, inplace) = dynamic_step(stmt)?;
        let op = match op {
            DynOp::Add => BinOp::Add,
            DynOp::Sub => BinOp::Sub,
            DynOp::Mul => BinOp::Mul,
            _ => return None,
        };
        if inplace && (offset + 1 != count || !matches!(op, BinOp::Add | BinOp::Sub)) {
            return None;
        }
        let (Operand::Local(lhs), Operand::Local(rhs)) = (lhs, rhs) else {
            return None;
        };
        for operand in [*lhs, *rhs] {
            if function.body.locals[operand].ty != MirType::Dynamic {
                return None;
            }
            if destinations.contains(&operand) {
                // 自写/前向读取不能当作已生成中间量，也不能降级成外部输入。
                if !produced.contains(&operand) {
                    return None;
                }
                consumed.insert(operand);
            } else if inputs.insert(operand) {
                plan.inputs.push(operand);
            }
        }
        plan.steps.push(Step {
            dest,
            op,
            lhs: *lhs,
            rhs: *rhs,
        });
        produced.insert(dest);
    }
    if plan.inputs.len() > MAX_INPUTS || plan.inputs.is_empty() {
        return None;
    }
    let intermediate: FxHashSet<_> = plan
        .steps
        .iter()
        .take(count - 1)
        .map(|step| step.dest)
        .collect();
    // 不删除独立、无消费者的算术；首切只接受每个中间值都真正流向链内后续操作。
    if !intermediate.iter().all(|local| consumed.contains(local)) {
        return None;
    }
    for (id, body) in function.body.blocks.iter() {
        if !terminator_reads(&body.terminator).is_disjoint(&intermediate) {
            return None;
        }
        for (index, statement) in body.stmts.iter().enumerate() {
            if id == block && (start..start + count).contains(&index) {
                continue;
            }
            if !statement_reads(statement).is_disjoint(&intermediate) {
                return None;
            }
        }
    }
    Some(plan)
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
