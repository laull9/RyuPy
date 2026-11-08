//! 对最终生成器 MIR 做有界、按实际 PC 分区的私有 frame 检查。
//! 只证明帧协议；不透明普通调用、运行时 helper 与入口契约仍由总资格器负责。
//! 返回值借用原 IR，不能在改变 IR 后继续持有；本模块不提供生产发射许可。
use crate::trace_frame_values::{Value, index, operand};
use std::collections::{HashSet, VecDeque};
#[path = "trace_frame_eval.rs"]
mod eval;
use crate::trace_lowering::{GeneratorLayout, GeneratorOrigin};
use crate::*;
use eval::{age_exceptions, evaluate, initial_slots, may_throw};
#[path = "trace_frame_relations.rs"]
mod relations;
#[cfg(test)]
#[path = "trace_frame_observer.rs"]
pub(crate) mod observer;

pub const STATE_BUDGET: usize = 32768;
pub const STEP_BUDGET: usize = 524288;
/// 同时限制状态内容，避免仅有状态数量上界却允许复制过大的版本表。
pub const VALUE_CELL_BUDGET: usize = 4000000;
const LINEAGE_BUDGET: usize = 32;

#[derive(Debug)]
pub struct FrameObligations<'a> {
    pub program: &'a Program,
    pub frame_accesses: usize,
    pub explored_states: usize,
    pub exception_reads: usize,
    pub retry_edges: usize,
    pub diagnostics: FrameDiagnostics,
    /// 即使所有帧义务闭合，也没有总证书或运行时准入。
    pub complete_production_certificate: bool,
    pub required_runtime_contracts: &'static [&'static str],
}

/// 外置诊断镜像不进入 State，不参与预算或安全查询。
#[derive(Clone, Debug, Default)]
pub struct FrameDiagnostics {
    pub processed: usize,
    pub unique_states: usize,
    pub duplicate_states: usize,
    pub self_edges: usize,
    pub value_cells: usize,
    pub states_by_pc: std::collections::BTreeMap<i64, usize>,
}

#[derive(Clone, Debug, PartialEq, Eq, Hash)]
enum Pending {
    Empty,
    AwaitingRead,
    Read,
}

#[derive(Clone, Debug, PartialEq, Eq, Hash)]
struct State {
    pc: i64,
    block: BlockId,
    cursor: usize,
    locals: Vec<Value>,
    slots: Vec<Value>,
    physical_catch: bool,
    dispatching: bool,
    written: Vec<bool>,
    expected_retry_pc: Option<i64>,
    expected_return_pc: Option<i64>,
    current_sources: Vec<Vec<LocalId>>,
    stored_current: Vec<bool>,
    pending: Pending,
}

/// 无可选 pass 的 build 输出可以作为输入；任何后续变更必须重新调用本检查。
pub fn inspect<'a>(
    program: &'a Program,
    origin: &GeneratorOrigin,
    layout: &GeneratorLayout,
) -> Result<FrameObligations<'a>, String> {
    let mut diagnostics = FrameDiagnostics::default();
    let result = inspect_with_diagnostics(program, origin, layout, &mut diagnostics);
    #[cfg(test)]
    observer::analysis(&diagnostics);
    result.map_err(|error| format!("{error}; diagnostics={diagnostics:?}"))
}

pub fn inspect_with_diagnostics<'a>(
    program: &'a Program,
    origin: &GeneratorOrigin,
    layout: &GeneratorLayout,
    diagnostics: &mut FrameDiagnostics,
) -> Result<FrameObligations<'a>, String> {
    *diagnostics = FrameDiagnostics::default();
    crate::validate::validate(program).map_err(|errors| format!("MIR 结构无效：{errors:?}"))?;
    if origin.factory != layout.factory || origin.resume != layout.resume {
        return Err("factory/resume 旁证不一致".into());
    }
    let factory = arena_get(&program.functions, origin.factory).ok_or("缺失 factory")?;
    let resume = arena_get(&program.functions, origin.resume).ok_or("缺失 resume")?;
    let body = &resume.body;
    if resume.params != vec![origin.frame_parameter] {
        return Err("resume 参数不是唯一私有 frame".into());
    }
    if layout.pc_targets.is_empty()
        || layout.pc_targets.len() > 256
        || body.blocks.len() > 2048
        || body.locals.len() > 512
        || body
            .blocks
            .iter()
            .map(|(_, block)| block.stmts.len())
            .sum::<usize>()
            > 16384
        || layout.exception_routes.len() > 256
        || layout.exit_pcs.len() > 2048
    {
        return Err("frame 模型形状预算超限".into());
    }
    let pcs: HashSet<_> = layout.pc_targets.iter().map(|(pc, _)| *pc).collect();
    if pcs.len() != layout.pc_targets.len()
        || !pcs.contains(&layout.initial_pc)
        || layout
            .pc_targets
            .iter()
            .any(|(pc, block)| *pc < 0 || arena_get(&body.blocks, *block).is_none())
    {
        return Err("PC 映射缺失、重复或越界".into());
    }
    if layout
        .exception_routes
        .iter()
        .any(|route| !route.unconditional)
        || body
            .regions
            .first()
            .map(|region| (region.try_len, region.handler))
            != layout.physical_region
    {
        return Err("逻辑 typed EH 未证明或物理 EH 映射被改变".into());
    }
    let originals: HashSet<_> = layout.original_blocks.iter().copied().collect();
    if originals.len() != layout.original_blocks.len()
        || layout.original_blocks.len() > 2048
        || layout
            .original_blocks
            .iter()
            .any(|id| arena_get(&body.blocks, *id).is_none())
        || layout
            .pc_targets
            .iter()
            .any(|(pc, target)| layout.original_blocks.get(*pc as usize) != Some(target))
        || layout.exception_routes.iter().any(|route| {
            route.start < 0
                || route.length > layout.original_blocks.len()
                || route.start as usize > layout.original_blocks.len() - route.length
                || !pcs.contains(&route.handler_pc)
        })
        || layout
            .exit_pcs
            .iter()
            .any(|(block, pc)| !originals.contains(block) || (*pc != -1 && !pcs.contains(pc)))
        || layout.physical_region.is_some() != !layout.exception_routes.is_empty()
    {
        return Err("原始块、退出或逻辑 EH 旁证缺失或越界".into());
    }
    let slots = initial_slots(factory, origin, layout)?;
    let frame_accesses = crate::trace_frame_shape::check(body, origin, layout)?;
    if body.regions.len() > 1
        || body.regions.iter().any(|region| {
            !matches!(region.kind, RegionKind::CatchAll)
                || region.try_start.into_raw().into_u32() != 0
                || region.try_len > body.blocks.len()
                || region.handler.into_raw().into_u32() < region.try_len as u32
                || arena_get(&body.blocks, region.handler).is_none()
        })
    {
        return Err("物理 catch 区域不符合有限模板".into());
    }
    let mut queue = VecDeque::from([restart(body, origin, layout.initial_pc, slots)]);
    let mut visited = HashSet::new();
    let mut exception_reads = 0;
    let mut retry_edges = 0;
    let mut steps = 0;
    let mut value_cells = 0;
    while let Some(mut state) = queue.pop_front() {
        steps += 1;
        diagnostics.processed = steps;
        if steps > STEP_BUDGET || visited.len() >= STATE_BUDGET {
            return Err("frame 状态或迭代预算耗尽".into());
        }
        let inserted = visited.insert(state.clone());
        #[cfg(test)]
        observer::record(&state, inserted);
        if !inserted {
            diagnostics.duplicate_states += 1;
            continue;
        }
        value_cells += state.locals.len() * 4
            + state.slots.len() * 3
            + state
                .current_sources
                .iter()
                .map(|chain| chain.len() * 2)
                .sum::<usize>();
        diagnostics.unique_states = visited.len();
        diagnostics.value_cells = value_cells;
        *diagnostics.states_by_pc.entry(state.pc).or_default() += 1;
        if value_cells > VALUE_CELL_BUDGET {
            return Err("frame 状态内容预算耗尽".into());
        }
        let block = arena_get(&body.blocks, state.block).ok_or("CFG 目标缺失")?;
        if state.dispatching
            && state.cursor == 0
            && let Some((pc, _)) = layout
                .pc_targets
                .iter()
                .find(|(_, target)| *target == state.block)
        {
            if *pc != state.pc {
                return Err("实际 dispatch 与 old-PC 映射不符".into());
            }
            state.dispatching = false;
        }
        if state.cursor == 0
            && let Some((_, pc)) = layout.exit_pcs.iter().find(|(id, _)| *id == state.block)
        {
            state.expected_return_pc = Some(*pc);
        }
        if let Some(stmt) = block.stmts.get(state.cursor) {
            let value = match stmt {
                Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
                Statement::Line(_) => None,
                _ => return Err("未登记 frame 语句".into()),
            };
            if let Some(value) = value {
                // 非帧纯操作的异常边在赋值前保存真实局部；catch 值只来自物理 EH 边。
                if may_throw(value) {
                    exception_edge(body, layout, &state, &mut queue);
                }
                let result = evaluate(value, &mut state, layout, &mut exception_reads)?;
                if let Statement::Assign { dest, .. } = stmt {
                    relations::define(*dest, value, &mut state)?;
                    let position = index(*dest);
                    *state.locals.get_mut(position).ok_or("局部写越界")? = result;
                }
            }
            state.cursor += 1;
            queue.push_back(state);
            continue;
        }
        match &block.terminator {
            Terminator::Goto(target) => jump(state, *target, &mut queue),
            Terminator::Branch { cond, then_, else_ } => {
                let condition = operand(cond, &state.locals)?;
                if *then_ == state.block && matches!(condition, Value::Bool(true) | Value::Opaque) {
                    diagnostics.self_edges += 1;
                }
                if *else_ == state.block && matches!(condition, Value::Bool(false) | Value::Opaque)
                {
                    diagnostics.self_edges += 1;
                }
                match condition {
                    Value::Bool(true) => jump(state, *then_, &mut queue),
                    Value::Bool(false) => jump(state, *else_, &mut queue),
                    Value::Opaque => {
                        jump(state.clone(), *then_, &mut queue);
                        jump(state, *else_, &mut queue);
                    }
                    _ => return Err("分支条件未初始化或不是布尔值".into()),
                }
            }
            Terminator::Raise { .. } => exception_edge(body, layout, &state, &mut queue),
            Terminator::Return(value) => {
                if state.dispatching {
                    return Err("dispatch 没有到达已证明 PC 目标".into());
                }
                let value = match value {
                    Some(v) => operand(v, &state.locals)?,
                    None => Value::None,
                };
                let Value::Integer(pc) = state.slots[0] else {
                    return Err("返回边 PC 未知".into());
                };
                if value != Value::Retry && Some(pc) != state.expected_return_pc {
                    return Err("实际退出 PC 与原暂停/返回边不符".into());
                }
                if value == Value::Retry && Some(pc) != state.expected_retry_pc {
                    return Err("Retry PC 与实际异常发生块的逻辑 EH 不符".into());
                }
                if !state.written[0] {
                    return Err("退出没有写入 PC".into());
                }
                if pc == -1 {
                    if value == Value::Retry {
                        return Err("Retry 使用结束 PC".into());
                    }
                    if state.slots[3] != Value::None {
                        return Err("完成前没有消费待接异常".into());
                    }
                    continue;
                }
                if !pcs.contains(&pc) {
                    return Err("返回边使用未知 PC".into());
                }
                if state.written[4..].iter().any(|done| !done)
                    || layout
                        .saved_locals
                        .iter()
                        .any(|local| !state.stored_current[index(*local) + 4])
                {
                    return Err("暂停或 Retry 缺少当前版本的局部逐槽保存".into());
                }
                if value == Value::Retry {
                    if state.slots[3] != Value::Exception {
                        return Err("Retry 没有保存物理 caught 异常".into());
                    }
                    retry_edges += 1;
                } else if state.slots[3] != Value::None {
                    return Err("普通暂停保留未消费的 caught 值".into());
                }
                queue.push_back(restart(body, origin, pc, state.slots));
            }
            Terminator::Unreachable => return Err("可达 Unreachable 终结符".into()),
            Terminator::Suspend { .. } => return Err("未降级暂停".into()),
        }
    }
    Ok(FrameObligations {
        program,
        frame_accesses,
        explored_states: visited.len(),
        exception_reads,
        retry_edges,
        diagnostics: diagnostics.clone(),
        complete_production_certificate: false,
        required_runtime_contracts: &[
            "GeneratorInput 的正常 next 槽协议；禁止 send/throw/close 注入",
            "GeneratorRetry 不向 Python 暴露且按同一 frame 恢复",
            "非 frame operation 的能力、异常来源、backend helper 与 EnterV1 尚待总检查",
        ],
    })
}

fn restart(body: &Body, origin: &GeneratorOrigin, pc: i64, mut slots: Vec<Value>) -> State {
    age_exceptions(&mut slots);
    let mut locals = vec![Value::Uninitialized; body.locals.len()];
    locals[index(origin.frame_parameter)] = Value::Frame;
    let written = vec![false; slots.len()];
    let stored_current = vec![false; slots.len()];
    let current_sources = body.locals.iter().map(|(id, _)| vec![id]).collect();
    let pending = if slots[3] == Value::StoredException {
        Pending::AwaitingRead
    } else {
        Pending::Empty
    };
    State {
        pc,
        block: body.entry,
        cursor: 0,
        locals,
        slots,
        physical_catch: false,
        dispatching: true,
        written,
        expected_retry_pc: None,
        expected_return_pc: None,
        current_sources,
        stored_current,
        pending,
    }
}

fn jump(mut state: State, block: BlockId, queue: &mut VecDeque<State>) {
    state.block = block;
    state.cursor = 0;
    queue.push_back(state);
}

fn exception_edge(
    body: &Body,
    layout: &GeneratorLayout,
    state: &State,
    queue: &mut VecDeque<State>,
) {
    for region in &body.regions {
        let start = region.try_start.into_raw().into_u32() as usize;
        let block = state.block.into_raw().into_u32() as usize;
        if block >= start && block < start + region.try_len {
            let mut caught = state.clone();
            age_exceptions(&mut caught.locals);
            age_exceptions(&mut caught.slots);
            caught.expected_retry_pc = layout
                .original_blocks
                .iter()
                .position(|id| *id == state.block)
                .and_then(|old| {
                    layout.exception_routes.iter().find(|route| {
                        old as i64 >= route.start
                            && (old as i64) < route.start + route.length as i64
                    })
                })
                .map(|route| route.handler_pc);
            caught.physical_catch = true;
            caught.dispatching = false;
            jump(caught, region.handler, queue);
        }
    }
}

fn arena_get<T>(arena: &la_arena::Arena<T>, id: la_arena::Idx<T>) -> Option<&T> {
    arena
        .iter()
        .find_map(|(candidate, value)| (candidate == id).then_some(value))
}
