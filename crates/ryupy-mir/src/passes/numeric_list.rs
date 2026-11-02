//! 数值列表岛的双端匹配：同时证明无观察的 append 出口与纯列表读取前缀，才改变工厂。
//!
//! 仅复用已确认的 FloatLoop；≤4 次 GetItem、≤2 层，索引/外部根均已求值为动态局部。
//! 工厂与 Use 链必须支配使用，不能把“唯一赋值”误当“所有路径已初始化”。
use super::float_chain::Plan as Chain;
use super::float_loop::Plan as Loop;
use super::numeric_list_analysis::{
    Definitions, definitions, dominates, dominators, reads, terminator_reads,
};
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub(super) struct Load {
    pub(super) dest: LocalId,
    pub(super) root: LocalId,
    pub(super) key: LocalId,
    pub(super) child: bool,
}
pub(super) struct Plan {
    pub(super) factory: (BlockId, usize),
    pub(super) prefix_start: usize,
    pub(super) loads: Vec<Load>,
    pub(super) sink_position: usize,
    pub(super) sink_dest: LocalId,
    pub(super) receiver: LocalId,
}
impl Plan {
    pub(super) fn extra_locals(&self) -> usize {
        // 每次预检一个bool，子节点额外一个私有引用；typed读取复用已有原生输入槽。
        self.loads.len() + self.loads.iter().filter(|load| load.child).count()
    }
    pub(super) fn extra_blocks(&self) -> usize {
        // 预检块、typed读取、原GetItem前缀以及分离出口后继；通用unbox分块单独计费。
        self.loads.len() + 3
    }
}

pub(super) fn candidate(f: &Function, chain: &Chain, loop_plan: &Loop) -> Option<Plan> {
    let definitions = definitions(f);
    let exit = &f.body.blocks[loop_plan.exit];
    let first = exit
        .stmts
        .iter()
        .position(|stmt| !matches!(stmt, Statement::Line(_)))?;
    let prefix = module_capture(f, loop_plan.exit, first, &definitions);
    let sink_position = prefix.as_ref().map(|p| p.sink).unwrap_or(first);
    let Statement::Assign {
        dest: sink_dest,
        value:
            Rvalue::DynCallMethod {
                captured,
                obj: Operand::Local(receiver),
                name,
                args,
            },
    } = &exit.stmts[sink_position]
    else {
        return None;
    };
    if name != "append"
        || !matches!(args.as_slice(), [Operand::Local(total)] if *total == loop_plan.total)
        || f.body.locals[*receiver].ty != MirType::Dynamic
        || *sink_dest == loop_plan.total
    {
        return None;
    }
    // 两局部只在实际MakeList支配证明完成后才视作identity；未知接收者不借bool猜类型。
    let (receiver, receiver_usage) = if let Some(prefix) = prefix {
        if !matches!(captured, Some(Operand::Local(flag)) if *flag == prefix.flag)
            || *receiver != prefix.target
        {
            return None;
        }
        (prefix.source, (loop_plan.exit, first))
    } else {
        if captured.is_some() {
            return None;
        }
        (*receiver, (loop_plan.exit, sink_position))
    };
    let last = chain.start + chain.steps.len() - 1;
    // 累加槽只允许链末 lhs 与唯一 append；临时/帧/容器中的隐藏观察同样拒绝。
    for (id, body) in f.body.blocks.iter() {
        if terminator_reads(&body.terminator).contains(&loop_plan.total) {
            return None;
        }
        for (position, stmt) in body.stmts.iter().enumerate() {
            if reads(stmt).contains(&loop_plan.total)
                && (id, position) != (chain.block, last)
                && (id, position) != (loop_plan.exit, sink_position)
            {
                return None;
            }
        }
    }
    let dominators = dominators(f);
    let factory = factory(f, &definitions, &dominators, receiver, receiver_usage)?;
    // 工厂不能只在循环某一轮执行；必须先于每次preheader及其出口。
    if !dominates(
        &dominators,
        factory,
        (
            loop_plan.preheader,
            f.body.blocks[loop_plan.preheader].stmts.len(),
        ),
    ) {
        return None;
    }
    let statements = &f.body.blocks[chain.block].stmts;
    let mut start = chain.start;
    while start > 0 && item(&statements[start - 1]).is_some() {
        start -= 1;
    }
    if !(1..=4).contains(&(chain.start - start)) {
        return None;
    }
    let mut positions = FxHashMap::default();
    let mut loads = Vec::new();
    for (offset, stmt) in statements[start..chain.start].iter().enumerate() {
        let (dest, root, key) = item(stmt)?;
        if f.params.contains(&dest)
            || f.body.locals[dest].name.is_some()
            || f.body.locals[dest].ty != MirType::Dynamic
            || f.body.locals[root].ty != MirType::Dynamic
            || f.body.locals[key].ty != MirType::Dynamic
            || definitions.get(&dest)?.as_slice() != [(chain.block, start + offset)]
            || positions.insert(dest, offset).is_some()
        {
            return None;
        }
        loads.push(Load {
            dest,
            root,
            key,
            child: false,
        });
    }
    let leaf_inputs: FxHashSet<_> = chain
        .inputs
        .iter()
        .copied()
        .filter(|input| *input != loop_plan.total)
        .collect();
    if leaf_inputs.is_empty()
        || !leaf_inputs
            .iter()
            .all(|input| positions.contains_key(input))
    {
        return None;
    }
    let mut depths = Vec::new();
    let mut children = FxHashSet::default();
    for (index, load) in loads.iter().enumerate() {
        if positions.contains_key(&load.key) {
            return None;
        }
        let depth = if let Some(parent) = positions.get(&load.root) {
            if *parent >= index {
                return None;
            }
            children.insert(load.root);
            depths[*parent] + 1
        } else {
            1
        };
        if depth > 2 {
            return None;
        }
        depths.push(depth);
    }
    for load in &mut loads {
        load.child = children.contains(&load.dest);
        if load.child == leaf_inputs.contains(&load.dest) {
            return None;
        }
    }
    let outputs: FxHashSet<_> = positions.keys().copied().collect();
    for (id, body) in f.body.blocks.iter() {
        if !terminator_reads(&body.terminator).is_disjoint(&outputs) {
            return None;
        }
        for (position, stmt) in body.stmts.iter().enumerate() {
            if id == chain.block && (start..chain.start + chain.steps.len()).contains(&position) {
                continue;
            }
            if !reads(stmt).is_disjoint(&outputs) {
                return None;
            }
        }
    }
    Some(Plan {
        factory,
        prefix_start: start,
        loads,
        sink_position,
        sink_dest: *sink_dest,
        receiver,
    })
}
/// 只接收紧邻的匿名冻结/判定/目标三元组；任一额外观察均拒绝岛优化。
struct CapturePrefix {
    source: LocalId,
    flag: LocalId,
    target: LocalId,
    sink: usize,
}
fn module_capture(
    f: &Function,
    block: BlockId,
    first: usize,
    definitions: &Definitions,
) -> Option<CapturePrefix> {
    let statements = &f.body.blocks[block].stmts;
    let positions: Vec<_> = statements
        .iter()
        .enumerate()
        .skip(first)
        .filter_map(|(i, s)| (!matches!(s, Statement::Line(_))).then_some(i))
        .take(4)
        .collect();
    let [freeze, test, capture, sink] = positions.as_slice() else {
        return None;
    };
    let Statement::Assign {
        dest: receiver,
        value: Rvalue::Use(Operand::Local(source)),
    } = &statements[*freeze]
    else {
        return None;
    };
    let Statement::Assign {
        dest: flag,
        value:
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::IsModule),
                args: flag_args,
            },
    } = &statements[*test]
    else {
        return None;
    };
    let Statement::Assign {
        dest: target,
        value:
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CaptureModuleTarget),
                args: target_args,
            },
    } = &statements[*capture]
    else {
        return None;
    };
    if !matches!(flag_args.as_slice(), [Operand::Local(r)] if r == receiver)
        || !matches!(target_args.as_slice(), [Operand::Local(r), Operand::Const(Constant::Str(name))] if r == receiver && name == "append")
    {
        return None;
    }
    for (local, position, ty, uses) in [
        (*receiver, *freeze, MirType::Dynamic, vec![*test, *capture]),
        (*flag, *test, MirType::Bool, vec![*sink]),
        (*target, *capture, MirType::Dynamic, vec![*sink]),
    ] {
        if definitions.get(&local)?.as_slice() != [(block, position)]
            || f.params.contains(&local)
            || f.env == Some(local)
            || f.body.locals[local].name.is_some()
            || f.body.locals[local].is_cell
            || f.body.locals[local].ty != ty
        {
            return None;
        }
        for (id, body) in f.body.blocks.iter() {
            if terminator_reads(&body.terminator).contains(&local) {
                return None;
            }
            for (position, statement) in body.stmts.iter().enumerate() {
                if reads(statement).contains(&local) && (id != block || !uses.contains(&position)) {
                    return None;
                }
            }
        }
    }
    Some(CapturePrefix {
        source: *source,
        flag: *flag,
        target: *target,
        sink: *sink,
    })
}

fn item(statement: &Statement) -> Option<(LocalId, LocalId, LocalId)> {
    let Statement::Assign {
        dest,
        value:
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::GetItem),
                args,
            },
    } = statement
    else {
        return None;
    };
    let [Operand::Local(root), Operand::Local(key)] = args.as_slice() else {
        return None;
    };
    Some((*dest, *root, *key))
}
fn factory(
    f: &Function,
    definitions: &Definitions,
    dominators: &FxHashMap<BlockId, FxHashSet<BlockId>>,
    mut receiver: LocalId,
    mut usage: (BlockId, usize),
) -> Option<(BlockId, usize)> {
    for _ in 0..16 {
        if f.params.contains(&receiver) {
            return None;
        }
        let [location] = definitions.get(&receiver)?.as_slice() else {
            return None;
        };
        if !dominates(dominators, *location, usage) {
            return None;
        }
        match &f.body.blocks[location.0].stmts[location.1] {
            Statement::Assign {
                value: Rvalue::Use(Operand::Local(source)),
                ..
            } => {
                receiver = *source;
                usage = *location;
            }
            Statement::Assign {
                value:
                    Rvalue::CallWithArgs {
                        callee: RuntimeFn::MakeList,
                        prefix,
                        args,
                    },
                ..
            } if prefix.is_empty() && args.is_empty() => return Some(*location),
            Statement::Assign {
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::NumericListCreate),
                        args,
                    },
                ..
            } if args.is_empty() => return Some(*location),
            _ => return None,
        }
    }
    None
}
