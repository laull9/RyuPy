//! 小型新鲜 tuple 的同块标量替换：只消除没有别名、逃逸或身份观察的构造。
//!
//! 元素在原构造点冻结到新的动态局部，不能在读取时重读已改写的源变量。
//! 固定下标必须有同块、在使用之前的唯一常量定义；不做跨块 SSA 或用户类型推测。
use super::MirPass;
use super::dead_values::{read_operand, read_value};
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub struct TupleScalar;
const MAX_ITEMS: usize = 8;
const MAX_ATTEMPTS: usize = 32;

struct Plan {
    block: BlockId,
    position: usize,
    elements: Vec<Operand>,
    reads: Vec<(usize, usize)>,
    lengths: Vec<usize>,
    checks: FxHashSet<usize>,
}

impl MirPass for TupleScalar {
    fn name(&self) -> &'static str {
        "tuple_scalar"
    }

    fn run(&self, function: &mut Function) {
        // 合成 resume 没有 signature；工厂有 MakeGenerator。模块、闭包和 EH 首版保守不动。
        if function.signature_global.is_none()
            || function.env.is_some()
            || !function.body.regions.is_empty()
            || function.body.locals.iter().any(|(_, local)| local.is_cell)
            || function.body.blocks.iter().any(|(_, block)| {
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
        {
            return;
        }
        // 预算计所有尝试而非仅成功改写：全部逃逸的输入也最多作 32 次全函数扫描。
        // 先保留源序候选的稳定 dest；每轮重建位置，避免插入快照后复用旧下标。
        let candidates: Vec<_> = function
            .body
            .blocks
            .iter()
            .flat_map(|(_, body)| &body.stmts)
            .filter_map(|stmt| match stmt {
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::CallWithArgs {
                            callee: RuntimeFn::MakeTuple,
                            ..
                        },
                } => Some(*dest),
                _ => None,
            })
            .take(MAX_ATTEMPTS)
            .collect();
        for dest in candidates {
            let definitions = definitions(function);
            let Some(positions) = definitions.get(&dest) else {
                continue;
            };
            let [(block, position)] = positions.as_slice() else {
                continue;
            };
            let statement = &function.body.blocks[*block].stmts[*position];
            if let Some(plan) = candidate(function, &definitions, *block, *position, statement) {
                expand(function, plan);
            }
        }
    }
}

type Definitions = FxHashMap<LocalId, Vec<(BlockId, usize)>>;
fn definitions(function: &Function) -> Definitions {
    let mut definitions = Definitions::default();
    for (block, body) in function.body.blocks.iter() {
        for (position, stmt) in body.stmts.iter().enumerate() {
            if let Statement::Assign { dest, .. } = stmt {
                definitions
                    .entry(*dest)
                    .or_default()
                    .push((block, position));
            }
        }
    }
    definitions
}

fn candidate(
    function: &Function,
    definitions: &Definitions,
    block: BlockId,
    position: usize,
    statement: &Statement,
) -> Option<Plan> {
    let Statement::Assign {
        dest,
        value:
            Rvalue::CallWithArgs {
                callee: RuntimeFn::MakeTuple,
                prefix,
                args,
            },
    } = statement
    else {
        return None;
    };
    if !prefix.is_empty()
        || args.len() > MAX_ITEMS
        || function.params.contains(dest)
        || function.body.locals[*dest].name.is_some()
        || definitions.get(dest)?.as_slice() != [(block, position)]
        || args.iter().any(|arg| {
            !matches!(arg,
            Operand::Local(local) if function.body.locals[*local].ty == MirType::Dynamic)
        })
    {
        return None;
    }
    let mut plan = Plan {
        block,
        position,
        elements: args.clone(),
        reads: vec![],
        lengths: vec![],
        checks: FxHashSet::default(),
    };
    for (id, body) in function.body.blocks.iter() {
        if terminator_reads(&body.terminator).contains(dest) {
            return None;
        }
        for (index, stmt) in body.stmts.iter().enumerate() {
            if !statement_reads(stmt).contains(dest) {
                continue;
            }
            if id != block || index <= position {
                return None;
            }
            let Statement::Assign {
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(callee),
                        args,
                    },
                ..
            } = stmt
            else {
                return None;
            };
            match (callee, args.as_slice()) {
                (RuntimeFn::Len, [Operand::Local(source)]) if source == dest => {
                    plan.lengths.push(index);
                }
                (RuntimeFn::GetItem, [Operand::Local(source), key]) if source == dest => {
                    let key = constant_index(function, definitions, block, index, key)?;
                    let normalized = if key < 0 {
                        key.checked_add(plan.elements.len() as i64)?
                    } else {
                        key
                    };
                    if normalized < 0 || normalized >= plan.elements.len() as i64 {
                        return None;
                    }
                    plan.reads.push((index, normalized as usize));
                }
                _ => return None,
            }
        }
    }
    // 无使用也可以删分配，但首切只处理明确存在长度/下标消费者的候选。
    if plan.reads.is_empty() && plan.lengths.is_empty() {
        return None;
    }
    // 唯一 Len 定义一次建表，不能为每个长度再扫描整块（重复 len 可达平方成本）。
    let mut known_lengths = FxHashMap::default();
    for &length_position in &plan.lengths {
        let Statement::Assign { dest: length, .. } =
            &function.body.blocks[block].stmts[length_position]
        else {
            continue;
        };
        if definitions.get(length)?.as_slice() == [(block, length_position)] {
            known_lengths.insert(*length, length_position);
        }
    }
    for (index, stmt) in function.body.blocks[block].stmts.iter().enumerate() {
        if let Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::UnpackCheck),
            args,
        }) = stmt
            && let [
                Operand::Local(actual),
                Operand::Const(Constant::I64(required)),
                Operand::Const(Constant::Bool(star)),
            ] = args.as_slice()
            && known_lengths
                .get(actual)
                .is_some_and(|position| *position < index)
            && *required >= 0
            && (if *star {
                plan.elements.len() as i64 >= *required
            } else {
                plan.elements.len() as i64 == *required
            })
        {
            plan.checks.insert(index);
        }
    }
    Some(plan)
}

fn constant_index(
    function: &Function,
    definitions: &Definitions,
    block: BlockId,
    before: usize,
    key: &Operand,
) -> Option<i64> {
    let Operand::Local(key) = key else {
        return None;
    };
    if function.params.contains(key) {
        return None;
    }
    let [(defined_block, defined_position)] = definitions.get(key)?.as_slice() else {
        return None;
    };
    if *defined_block != block || *defined_position >= before {
        return None;
    }
    // 只接字面常量，不沿可重绑定源局部传播，不把 __index__ 调用误当纯取值。
    match &function.body.blocks[block].stmts[*defined_position] {
        Statement::Assign {
            value:
                Rvalue::Box {
                    value: Operand::Const(Constant::I64(index)),
                    from: MirType::I64,
                },
            ..
        } => Some(*index),
        _ => None,
    }
}

fn expand(function: &mut Function, plan: Plan) {
    let mut snapshots = Vec::new();
    let mut stores = Vec::new();
    for value in plan.elements.iter().cloned() {
        let dest = function.body.locals.alloc(LocalDecl {
            ty: MirType::Dynamic,
            name: None,
            is_cell: false,
        });
        snapshots.push(dest);
        stores.push(Statement::Assign {
            dest,
            value: Rvalue::Use(value),
        });
    }
    let body = &mut function.body.blocks[plan.block];
    for (position, element) in plan.reads {
        if let Statement::Assign { value, .. } = &mut body.stmts[position] {
            *value = Rvalue::Use(Operand::Local(snapshots[element]));
        }
    }
    for position in plan.lengths {
        if let Statement::Assign { value, .. } = &mut body.stmts[position] {
            *value = Rvalue::Use(Operand::Const(Constant::I64(plan.elements.len() as i64)));
        }
    }
    // 保留原语句位置/行号；失败的 UnpackCheck 未删除，仍先于任何目标写入。
    let original = std::mem::take(&mut body.stmts);
    for (index, stmt) in original.into_iter().enumerate() {
        if index == plan.position {
            body.stmts.append(&mut stores);
        } else if !plan.checks.contains(&index) {
            body.stmts.push(stmt);
        }
    }
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
