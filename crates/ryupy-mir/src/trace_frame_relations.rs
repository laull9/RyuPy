//! 只保留“副本仍对应当前定义”的有限关系；每次写全局 kill 旧来源。
//! 不用小整数轮换，也不根据 Opaque 值相等恢复失效别名。
use super::*;

pub(super) fn sources(value: &Operand, state: &State) -> Vec<LocalId> {
    match value {
        Operand::Local(id) => state.current_sources[index(*id)].clone(),
        _ => Vec::new(),
    }
}

pub(super) fn define(dest: LocalId, value: &Rvalue, state: &mut State) -> Result<(), String> {
    // 必须先捕获副本，再 kill；异常边已经在调用此函数之前克隆旧状态。
    let mut captured = match value {
        Rvalue::Use(source) | Rvalue::Box { value: source, .. } => sources(source, state),
        _ => Vec::new(),
    };
    for carriers in &mut state.current_sources {
        carriers.retain(|id| *id != dest);
    }
    if let Some(current) = state.stored_current.get_mut(index(dest) + 4) {
        *current = false;
    }
    captured.retain(|id| *id != dest);
    captured.push(dest);
    captured.sort_unstable_by_key(|id| index(*id));
    captured.dedup();
    if captured.len() > LINEAGE_BUDGET {
        return Err("局部版本复制链预算耗尽".into());
    }
    state.current_sources[index(dest)] = captured;
    Ok(())
}
