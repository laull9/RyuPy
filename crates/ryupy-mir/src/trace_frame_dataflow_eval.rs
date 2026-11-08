//! 逐原子提升原有限查询；潜在非法成员整体拒绝，不从集合筛掉坏路径。
use super::*;
use crate::trace_frame_values::{compare, inputs};
use domain::Pending;

pub(super) fn operand(
    value: &Operand,
    state: &State,
    budget: &mut Budget,
) -> Result<Values, String> {
    budget.work(1)?;
    match value {
        Operand::Local(id) => {
            let cell = state.locals.get(index(*id)).ok_or("局部越界")?;
            budget.work(cell.len())?;
            Ok(cell.clone())
        }
        Operand::Array(items) => {
            for item in items {
                for atom in operand(item, state, budget)? {
                    budget.work(1)?;
                    if atom == Value::Frame {
                        return Err("私有 frame 逃逸到数组".into());
                    }
                }
            }
            Ok(vec![Value::Opaque])
        }
        _ => Ok(vec![crate::trace_frame_values::operand(value, &[])?]),
    }
}

pub(super) fn evaluate(
    value: &Rvalue,
    state: &mut State,
    engine: &mut Engine<'_>,
) -> Result<Values, String> {
    let args = inputs(value)?;
    let mut values = Vec::new();
    for arg in &args {
        values.push(operand(arg, state, &mut engine.budget)?);
    }
    Ok(match value {
        Rvalue::Use(_) | Rvalue::Box {
            ..
        } | Rvalue::Unbox { .. } => values[0].clone(),
        Rvalue::Binary { op, .. } => {
            let mut result = Vec::new();
            for left in &values[0] {
                for right in &values[1] {
                    engine.budget.work(1)?;
                    domain::add(
                        &mut result,
                        compare(*op, left.clone(), right.clone()),
                        &mut engine.budget,
                    )?;
                }
            }
            result
        }
        Rvalue::CatchException => {
            if !state.key.physical {
                return Err("CatchException 没有物理 EH 输入".into());
            }
            state.key.physical = false;
            vec![Value::Exception]
        }
        Rvalue::Call { callee: Callee::Runtime(runtime), args } => match runtime {
            RuntimeFn::ArrayGet | RuntimeFn::ArraySet => {
                if values.first().map(Vec::as_slice) != Some(&[Value::Frame][..]) {
                    return Err("frame 实参身份失效".into());
                }
                let Some([Value::Integer(slot)]) = values.get(1).map(Vec::as_slice) else {
                    return Err("未知槽索引".into());
                };
                let slot = usize::try_from(*slot).map_err(|_| "负槽索引")?;
                if slot >= state.slots.len() {
                    return Err("槽越界".into());
                }
                if *runtime == RuntimeFn::ArraySet {
                    let written = values.get(2).ok_or("缺失槽写值")?;
                    engine.budget.work(written.len())?;
                    if slot >= 4 {
                        let local = engine.layout.saved_locals.get(slot - 4).ok_or("缺失槽局部")?;
                        let sources = match &args[2] {
                            Operand::Local(id) => &state.sources[index(*id)],
                            _ => return Err("spill 复制来源不是当前局部定义版本".into()),
                        };
                        engine.budget.work(sources.len())?;
                        if !sources.contains(local) {
                            return Err("spill 复制来源不是当前局部定义版本".into());
                        }
                        state.stored[slot] = true;
                    }
                    if slot == 3 {
                        state.key.pending = match (&state.key.pending, written.as_slice()) {
                            (Pending::Read, [Value::None]) => Pending::Empty,
                            (Pending::Empty, [Value::Exception]) => Pending::AwaitingRead,
                            _ => return Err("caught 槽必须先读取再清空，不能丢弃或覆盖 pending".into()),
                        };
                    }
                    state.slots[slot] = written.clone();
                    state.written[slot] = true;
                    vec![Value::None]
                } else {
                    if slot == 3 {
                        if state.slots[slot] != [Value::StoredException]
                            || state.key.pending != Pending::AwaitingRead
                        {
                            return Err("handler 读取了非异常槽 3".into());
                        }
                        state.key.pending = Pending::Read;
                        engine.reads += 1;
                    }
                    engine.budget.work(state.slots[slot].len())?;
                    state.slots[slot].clone()
                }
            }
            RuntimeFn::GeneratorRetry => vec![Value::Retry],
            RuntimeFn::GeneratorInput => {
                engine.budget.work(state.slots[1].len() + state.slots[2].len())?;
                if state.slots[1] != [Value::None] || state.slots[2] != [Value::None] {
                    return Err("正常 next 控制槽被污染".into());
                }
                vec![Value::None]
            }
            RuntimeFn::RaisePrepare => {
                let mut result = Vec::new();
                if let Some(values) = values.first() {
                    for value in values {
                        engine.budget.work(1)?;
                        let value = if matches!(value, Value::Exception | Value::StoredException) {
                            value.clone()
                        } else {
                Value::Opaque
            };
                        domain::add(&mut result, value, &mut engine.budget)?;
                    }
                } else {
                result.push(Value::Opaque);
            }
                result
            }
            _ => vec![Value::Opaque],
        },
        _ => vec![Value::Opaque],
    })
}
