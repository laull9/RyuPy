//! 完整正常、赋值前EH与跨Retry边；固定点不复用暂态通过标志。
use super::*;

fn jump(engine: &mut Engine<'_>, mut state: State, block: BlockId) -> Result<(), String> {
    state.key.block = block;
    state.key.cursor = 0;
    engine.enqueue(state)
}

fn exception_edge(engine: &mut Engine<'_>, state: &State) -> Result<(), String> {
    let body = engine.body;
    for region in &body.regions {
        engine.budget.work(1)?;
        let start = region.try_start.into_raw().into_u32() as usize;
        let block = state.key.block.into_raw().into_u32() as usize;
        if block >= start && block < start + region.try_len {
            let mut caught = state.clone();
            domain::age(&mut caught.locals, &mut engine.budget)?;
            domain::age(&mut caught.slots, &mut engine.budget)?;
            caught.key.expected_retry = engine
                .lookup
                .retry(state.key.block, &mut engine.budget)?;
            caught.key.physical = true;
            caught.key.dispatching = false;
            jump(engine, caught, region.handler)?;
        }
    }
    Ok(())
}

pub(super) fn step(engine: &mut Engine<'_>, mut state: State) -> Result<(), String> {
    let body = engine.body;
    // Arena按已校验的稠密BlockId直接访问；仍显式拒绝越界目标。
    engine.budget.work(1)?;
    if state.key.block.into_raw().into_u32() as usize >= body.blocks.len() {
        return Err("CFG 目标缺失".into());
    }
    let block = &body.blocks[state.key.block];
    if state.key.dispatching && state.key.cursor == 0 {
        if let Some(pc) = engine.lookup.dispatch(state.key.block, &mut engine.budget)? {
            if pc != state.key.pc {
                return Err("实际 dispatch 与 old-PC 映射不符".into());
            }
            state.key.dispatching = false;
        }
    }
    if state.key.cursor == 0 {
        if let Some(pc) = engine.lookup.exit(state.key.block, &mut engine.budget)? {
            state.key.expected_return = Some(pc);
        }
    }
    if let Some(statement) = block.stmts.get(state.key.cursor) {
        let value = match statement {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            Statement::Line(_) => None,
            _ => return Err("未登记 frame 语句".into()),
        };
        if let Some(value) = value {
            if crate::trace_frame::operation_may_throw(value) {
                exception_edge(engine, &state)?;
            }
            let result = eval::evaluate(value, &mut state, engine)?;
            if let Statement::Assign { dest, .. } = statement {
                domain::define(&mut state, *dest, value, &mut engine.budget)?;
                *state.locals.get_mut(index(*dest)).ok_or("局部写越界")? = result;
            }
        }
        state.key.cursor += 1;
        return engine.enqueue(state);
    }
    match &block.terminator {
        Terminator::Goto(target) => jump(engine, state, *target),
        Terminator::Branch { cond, then_, else_ } => {
            let values = eval::operand(cond, &state, &mut engine.budget)?;
            let (mut yes, mut no) = (false, false);
            for value in values {
                engine.budget.work(1)?;
                match value {
                    Value::Bool(true) => yes = true,
                    Value::Bool(false) => no = true,
                    Value::Opaque => {
                        yes = true;
                        no = true;
                    }
                    _ => return Err("分支条件未初始化或不是布尔值".into()),
                }
            }
            if yes {
                if *then_ == state.key.block {
                    engine.budget.diagnostics.self_edges += 1;
                }
                jump(engine, state.clone(), *then_)?;
            }
            if no {
                if *else_ == state.key.block {
                    engine.budget.diagnostics.self_edges += 1;
                }
                jump(engine, state, *else_)?;
            }
            Ok(())
        }
        Terminator::Raise { .. } => exception_edge(engine, &state),
        Terminator::Return(value) => {
            let values = match value {
                Some(value) => eval::operand(value, &state, &mut engine.budget)?,
                None => Values::single(Value::None, &mut engine.budget)?,
            };
            engine.budget.work(values.len())?;
            let retry = values.contains(&Value::Retry);
            if retry && values.len() != 1 {
                return Err("Return 混合 Retry 与普通结果".into());
            }
            if state.key.dispatching {
                return Err("dispatch 没有到达已证明 PC 目标".into());
            }
            let [Value::Integer(pc)] = state.slots[0].as_slice() else {
                return Err("返回边 PC 未知".into());
            };
            let pc = *pc;
            if !retry && Some(pc) != state.key.expected_return {
                return Err("实际退出 PC 与原暂停/返回边不符".into());
            }
            if retry && Some(pc) != state.key.expected_retry {
                return Err("Retry PC 与实际异常发生块的逻辑 EH 不符".into());
            }
            if !state.written[0] {
                return Err("退出没有写入 PC".into());
            }
            if pc == -1 {
                if retry {
                    return Err("Retry 使用结束 PC".into());
                }
                if state.slots[3] != [Value::None] {
                    return Err("完成前没有消费待接异常".into());
                }
                return Ok(());
            }
            if !engine.pcs.contains(&pc) {
                return Err("返回边使用未知 PC".into());
            }
            engine
                .budget
                .work(state.written.len() + engine.layout.saved_locals.len())?;
            if state.written[4..].iter().any(|done| !done)
                || engine
                    .layout
                    .saved_locals
                    .iter()
                    .any(|id| !state.stored[index(*id) + 4])
            {
                return Err("暂停或 Retry 缺少当前版本的局部逐槽保存".into());
            }
            if retry {
                if state.slots[3] != [Value::Exception] {
                    return Err("Retry 没有保存物理 caught 异常".into());
                }
                engine.retries += 1;
            } else if state.slots[3] != [Value::None] {
                return Err("普通暂停保留未消费的 caught 值".into());
            }
            let restarted = domain::restart(
                engine.body,
                engine.origin,
                pc,
                state.slots,
                &mut engine.budget,
            )?;
            engine.enqueue(restarted)
        }
        Terminator::Unreachable => Err("可达 Unreachable 终结符".into()),
        Terminator::Suspend { .. } => Err("未降级暂停".into()),
    }
}
