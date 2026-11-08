//! 有限集合值域、精确phase和must交；不可达由工作表缺席表示。
use super::*;

pub(super) type Values = Vec<Value>;

#[derive(Clone, Debug, PartialEq, Eq, Hash)]
pub(super) enum Pending {
    Empty,
    AwaitingRead,
    Read,
}

#[derive(Clone, Debug, PartialEq, Eq, Hash)]
pub(super) struct Key {
    pub pc: i64,
    pub block: BlockId,
    pub cursor: usize,
    pub physical: bool,
    pub dispatching: bool,
    pub pending: Pending,
    pub expected_retry: Option<i64>,
    pub expected_return: Option<i64>,
    pub slot0: Value,
    pub slot3: Value,
}

#[derive(Clone, Debug)]
pub(super) struct State {
    pub key: Key,
    pub locals: Vec<Values>,
    pub slots: Vec<Values>,
    pub written: Vec<bool>,
    pub stored: Vec<bool>,
    pub sources: Vec<Vec<LocalId>>,
}

pub(super) fn add(values: &mut Values, value: Value, budget: &mut Budget) -> Result<bool, String> {
    // 成员处理基本费独立于相等比较，空目标也不能免费新增。
    budget.work(1)?;
    for old in values.iter() {
        budget.work(1)?;
        if old == &value {
            return Ok(false);
        }
    }
    if values.len() >= 32 {
        return Err("Value 原子预算耗尽".into());
    }
    values.push(value);
    Ok(true)
}

pub(super) fn join(target: &mut State, source: &State, budget: &mut Budget) -> Result<bool, String> {
    if target.key != source.key {
        return Err("phase key 不一致".into());
    }
    let mut changed = false;
    for (left, right) in target
        .locals
        .iter_mut()
        .chain(&mut target.slots)
        .zip(source.locals.iter().chain(&source.slots))
    {
        for value in right {
            changed |= add(left, value.clone(), budget)?;
        }
    }
    for (left, right) in target.sources.iter_mut().zip(&source.sources) {
        let kept = intersect_sources(left, right, budget)?;
        changed |= kept.len() != left.len();
        *left = kept;
    }
    for (left, right) in target
        .written
        .iter_mut()
        .chain(&mut target.stored)
        .zip(source.written.iter().chain(&source.stored))
    {
        budget.work(1)?;
        changed |= *left && !*right;
        *left &= *right;
    }
    Ok(changed)
}

/// 每个左成员收处理费，右集合为空时仍计费；实际比较另行计费。
pub(super) fn intersect_sources(
    left: &[LocalId],
    right: &[LocalId],
    budget: &mut Budget,
) -> Result<Vec<LocalId>, String> {
    let mut kept = Vec::new();
    for value in left {
        budget.work(1)?;
        let mut present = false;
        for other in right {
            budget.work(1)?;
            present |= other == value;
        }
        if present {
            kept.push(*value);
        }
    }
    Ok(kept)
}

pub(super) fn define(
    state: &mut State,
    dest: LocalId,
    value: &Rvalue,
    budget: &mut Budget,
) -> Result<(), String> {
    let captured = match value {
        Rvalue::Use(Operand::Local(source)) | Rvalue::Box {
            value: Operand::Local(source),
            ..
        } => state.sources[index(*source)].clone(),
        _ => Vec::new(),
    };
    for sources in &mut state.sources {
        budget.work(sources.len() + 1)?;
        sources.retain(|id| *id != dest);
    }
    if let Some(stored) = state.stored.get_mut(index(dest) + 4) {
        *stored = false;
    }
    budget.work(captured.len())?;
    let mut sources: Vec<_> = captured.into_iter().filter(|id| *id != dest).collect();
    budget.work(sources.len() + 1)?;
    let position = sources
        .iter()
        .position(|id| index(*id) > index(dest))
        .unwrap_or(sources.len());
    sources.insert(position, dest);
    if sources.len() > 32 {
        return Err("局部版本复制链预算耗尽".into());
    }
    state.sources[index(dest)] = sources;
    Ok(())
}

pub(super) fn age(values: &mut [Values], budget: &mut Budget) -> Result<(), String> {
    for cell in values {
        let mut result = Vec::new();
        for value in cell.iter() {
            budget.work(1)?;
            let mapped = if *value == Value::Exception {
                Value::StoredException
            } else {
                value.clone()
            };
            add(&mut result, mapped, budget)?;
        }
        *cell = result;
    }
    Ok(())
}

pub(super) fn restart(
    body: &Body,
    origin: &GeneratorOrigin,
    pc: i64,
    mut slots: Vec<Values>,
    budget: &mut Budget,
) -> Result<State, String> {
    age(&mut slots, budget)?;
    budget.work(body.locals.len() + slots.len())?;
    let mut locals = vec![vec![Value::Uninitialized]; body.locals.len()];
    locals[index(origin.frame_parameter)] = vec![Value::Frame];
    // 每次restart的slot3来自已核单值退出；不凭HIR构造caught。
    let pending = match slots[3].as_slice() {
        [Value::StoredException] => Pending::AwaitingRead,
        [Value::None] => Pending::Empty,
        _ => return Err("restart caught 槽不是确定异常或None".into()),
    };
    Ok(State {
        key: Key {
            pc,
            block: body.entry,
            cursor: 0,
            physical: false,
            dispatching: true,
            pending,
            expected_retry: None,
            expected_return: None,
            slot0: slots[0][0].clone(),
            slot3: slots[3][0].clone(),
        },
        locals,
        written: vec![false; slots.len()],
        stored: vec![false; slots.len()],
        slots,
        sources: body.locals.iter().map(|(id, _)| vec![id]).collect(),
    })
}
