//! frame 有限状态的单步求值与 factory 初值检查；只对私有槽负责。
use super::*;
use crate::trace_frame_values::{compare, inputs};

pub(super) fn may_throw(value: &Rvalue) -> bool {
    !matches!(
        value,
        Rvalue::Use(_)
            | Rvalue::Box { .. }
            | Rvalue::Binary {
                op: BinOp::Eq | BinOp::Ne | BinOp::Lt | BinOp::Le | BinOp::Gt | BinOp::Ge,
                ..
            }
            | Rvalue::CatchException
            | Rvalue::Call {
                callee: Callee::Runtime(
                    RuntimeFn::ArrayGet | RuntimeFn::ArraySet | RuntimeFn::GeneratorRetry
                ),
                ..
            }
    )
}

pub(super) fn evaluate(
    value: &Rvalue,
    state: &mut State,
    layout: &GeneratorLayout,
    reads: &mut usize,
) -> Result<Value, String> {
    let args = inputs(value)?;
    // shape 检查禁止别名与逃逸；这里仍逐状态检查，不依赖 LocalId 的终身身份。
    let values = args
        .iter()
        .map(|arg| operand(arg, &state.locals))
        .collect::<Result<Vec<_>, _>>()?;
    Ok(match value {
        Rvalue::Use(_) | Rvalue::Box { .. } | Rvalue::Unbox { .. } => values[0].clone(),
        Rvalue::Binary { op, .. } => compare(*op, values[0].clone(), values[1].clone()),
        Rvalue::CatchException => {
            if !state.physical_catch {
                return Err("CatchException 没有物理 EH 输入".into());
            }
            state.physical_catch = false;
            Value::Exception
        }
        Rvalue::Call {
            callee: Callee::Runtime(runtime),
            args,
        } => match runtime {
            RuntimeFn::ArrayGet | RuntimeFn::ArraySet => {
                if values.first() != Some(&Value::Frame) {
                    return Err("frame 实参身份失效".into());
                }
                let Some(Value::Integer(slot)) = values.get(1) else {
                    return Err("未知槽索引".into());
                };
                let slot = usize::try_from(*slot).map_err(|_| "负槽索引")?;
                if *runtime == RuntimeFn::ArraySet {
                    let written = values.get(2).ok_or("缺失槽写值")?;
                    if slot >= 4 {
                        let local = *layout.saved_locals.get(slot - 4).ok_or("缺失槽局部")?;
                        if !relations::sources(&args[2], state).contains(&local) {
                            return Err("spill 复制来源不是当前局部定义版本".into());
                        }
                        state.stored_current[slot] = true;
                    }
                    if slot == 3 {
                        state.pending = match (&state.pending, written) {
                            (Pending::Read, Value::None) => Pending::Empty,
                            (Pending::Empty, Value::Exception) => Pending::AwaitingRead,
                            _ => {
                                return Err(
                                    "caught 槽必须先读取再清空，不能丢弃或覆盖 pending".into()
                                );
                            }
                        };
                    }
                    *state.slots.get_mut(slot).ok_or("槽越界")? = written.clone();
                    state.written[slot] = true;
                    Value::None
                } else {
                    let target = state.slots.get(slot).ok_or("槽越界")?;
                    if slot == 3 {
                        if *target != Value::StoredException
                            || state.pending != Pending::AwaitingRead
                        {
                            return Err("handler 读取了非异常槽 3".into());
                        }
                        state.pending = Pending::Read;
                        *reads += 1;
                    }
                    target.clone()
                }
            }
            RuntimeFn::GeneratorRetry => Value::Retry,
            RuntimeFn::GeneratorInput => {
                if state.slots[1] != Value::None || state.slots[2] != Value::None {
                    return Err("正常 next 控制槽被污染".into());
                }
                Value::None
            }
            RuntimeFn::RaisePrepare
                if matches!(
                    values.first(),
                    Some(Value::Exception | Value::StoredException)
                ) =>
            {
                values[0].clone()
            }
            _ => Value::Opaque,
        },
        _ => Value::Opaque,
    })
}

pub(super) fn initial_slots(
    factory: &Function,
    origin: &GeneratorOrigin,
    layout: &GeneratorLayout,
) -> Result<Vec<Value>, String> {
    if factory.body.blocks.len() != 1 {
        return Err("factory 不是有限直线模板".into());
    }
    let mut locals = vec![Value::Uninitialized; factory.body.locals.len()];
    for parameter in &factory.params {
        locals[index(*parameter)] = Value::Parameter(index(*parameter));
    }
    let block = &factory.body.blocks[factory.body.entry];
    let mut found = None;
    for statement in &block.stmts {
        if found.is_some() {
            return Err("MakeGenerator 后存在额外 factory 操作".into());
        }
        let Statement::Assign { dest, value } = statement else {
            return Err("未知 factory 语句".into());
        };
        match value {
            Rvalue::Box { value, .. } | Rvalue::Use(value) => {
                locals[index(*dest)] = operand(value, &locals)?;
            }
            Rvalue::MakeGenerator { resume, frame, .. } => {
                if *resume != origin.resume || found.is_some() {
                    return Err("factory resume 不匹配".into());
                }
                let slots = frame
                    .iter()
                    .map(|v| operand(v, &locals))
                    .collect::<Result<Vec<_>, _>>()?;
                if slots.len() != layout.saved_locals.len() + 4
                    || slots.len() != origin.initial_slots.len()
                    || slots[..4]
                        != [
                            Value::Integer(layout.initial_pc),
                            Value::None,
                            Value::None,
                            Value::None,
                        ]
                {
                    return Err("factory 初始控制槽不匹配".into());
                }
                for parameter in &factory.params {
                    if slots.get(index(*parameter) + 4)
                        != Some(&Value::Parameter(index(*parameter)))
                    {
                        return Err("factory 普通或隐藏参数槽被替换".into());
                    }
                }
                found = Some(slots);
                if !matches!(&block.terminator, Terminator::Return(Some(Operand::Local(id))) if id == dest)
                {
                    return Err("factory 没有返回真实生成器".into());
                }
            }
            _ => return Err("factory 中存在额外操作".into()),
        }
    }
    found.ok_or_else(|| "factory 没有 MakeGenerator".into())
}

/// 当前物理 catch 与前一轮已保存的异常不能混同。
pub(super) fn age_exceptions(values: &mut [Value]) {
    for value in values {
        if *value == Value::Exception {
            *value = Value::StoredException;
        }
    }
}
