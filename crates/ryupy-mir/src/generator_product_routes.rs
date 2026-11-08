//! 实际物理 catch、逻辑首次匹配顺序与 Retry/完成出口的结构归纳。
//! 只证明有限模板守恒：当前 CLR 局部→逐槽spill→唯一restore→caught读取清除；没有第二轮值固定点。
use crate::generator_product_cursor::{block, number, same, Cursor};
use crate::generator_product_structure::Checker;
use crate::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub(super) fn inspect(checker: &mut Checker<'_>, exceptions: &FxHashMap<String, GlobalId>) -> Result<(), String> {
    let [region] = checker.body.regions.as_slice() else { return Err("恢复函数不具有唯一物理异常区域".into()); };
    if !matches!(region.kind, RegionKind::CatchAll) || region.try_start != block(0)
        || region.try_len != checker.protected || region.source_handler_chain.is_some() {
        return Err("恢复函数物理EH窗口/种类错误".into());
    }
    let handler = checker.take(region.handler)?;
    let mut cursor = Cursor::new(&handler.stmts, checker.source.locals.len() + 1);
    let (caught, value) = cursor.assignment()?;
    checker.private(caught, MirType::Dynamic)?;
    if !matches!(value, Rvalue::CatchException) || region.exc_local != Some(caught) {
        return Err("物理catch没有接收当前CLR异常".into());
    }
    cursor.eval_call(RuntimeFn::TraceV1GeneratorCatch, &[Operand::Local(caught)])?;
    cursor.end()?;
    let Terminator::Goto(mut dispatch) = handler.terminator else { return Err("物理catch未离开EH到分派".into()); };
    let active = checker.active.ok_or("缺少当前源块PC")?;
    let source = checker.source;
    for route in &source.regions {
        let lower = checker.take(dispatch)?;
        let (upper_id, next) = checker.comparison(lower, active, BinOp::Ge, number(route.start) as i64)?;
        let upper = checker.take(upper_id)?;
        let (mut matched, upper_next) = checker.comparison(upper, active, BinOp::Lt, (number(route.start) + route.length) as i64)?;
        if upper_next != next { return Err("EH窗口上/下界的不匹配边不一致".into()); }
        if let RegionKind::Catch { type_names } = &route.kind {
            if type_names.is_empty() { return Err("静态逻辑handler缺少类型".into()); }
            let mut chosen = None;
            for name in type_names {
                let typed = checker.take(matched)?;
                let mut cursor = Cursor::new(&typed.stmts, checker.source.locals.len() + 1);
                let ty = match exceptions.get(name) {
                    Some(global) => cursor.use_value(&Operand::Global(*global))?,
                    None => cursor.assign_call(RuntimeFn::TypeObject, &[Operand::Const(Constant::Str(name.clone()))])?,
                };
                checker.private(ty, MirType::Dynamic)?;
                let answer = cursor.assign_call(RuntimeFn::DynBuiltin, &[
                    Operand::Const(Constant::Str("isinstance".into())),
                    Operand::Array(vec![Operand::Local(caught), Operand::Local(ty)]),
                    Operand::Array(vec![]), Operand::Array(vec![]),
                ])?;
                checker.private(answer, MirType::Dynamic)?;
                let cond = cursor.assign_call(RuntimeFn::DynTruthy, &[Operand::Local(answer)])?;
                checker.private(cond, MirType::Bool)?; cursor.end()?;
                let Terminator::Branch { cond: Operand::Local(test), then_, else_ } = typed.terminator else {
                    return Err("实际类型匹配结果没有分支".into());
                };
                if test != cond || chosen.is_some_and(|old| old != then_) { return Err("静态类型匹配目标不一致".into()); }
                chosen = Some(then_); matched = else_;
            }
            if matched != next { return Err("类型all-miss未去下一个真实EH窗口".into()); }
            matched = chosen.ok_or("类型链没有选择目标")?;
        }
        let choose = checker.take(matched)?;
        let mut cursor = Cursor::new(&choose.stmts, checker.source.locals.len() + 1);
        let pc = cursor.use_value(&Operand::Const(Constant::I64(number(route.handler) as i64)))?;
        checker.private(pc, MirType::I64)?; cursor.end()?;
        if checker.next_pc.is_some_and(|old| old != pc) { return Err("逻辑handler未共用实际Retry PC".into()); }
        checker.next_pc = Some(pc);
        if !matches!(choose.terminator, Terminator::Goto(target) if target == block(checker.protected + 2)) {
            return Err("逻辑handler绕过了统一Retry".into());
        }
        dispatch = next;
    }
    if dispatch != block(checker.protected + 1) { return Err("逻辑all-miss没有到唯一外逸出口".into()); }
    let unmatched = checker.take(dispatch)?;
    if !unmatched.stmts.is_empty() || !same(&unmatched.terminator, &Terminator::Raise { value: Some(Operand::Local(caught)), cause: None }) {
        return Err("逻辑all-miss不是原异常对象的内部直抛".into());
    }
    let retry = checker.take(block(checker.protected + 2))?;
    let mut cursor = Cursor::new(&retry.stmts, checker.source.locals.len() + 1);
    cursor.spill(checker.frame, &source.locals)?;
    cursor.pc(checker.frame, Operand::Local(checker.next_pc.ok_or("Retry缺少handler PC")?))?;
    cursor.slot(checker.frame, 3, Operand::Local(caught))?;
    let signal = cursor.assign_call(RuntimeFn::GeneratorRetry, &[])?;
    checker.private(signal, MirType::Dynamic)?; cursor.end()?;
    if !same(&retry.terminator, &Terminator::Return(Some(Operand::Local(signal)))) {
        return Err("Retry没有返回真实内部哨兵".into());
    }
    let epilogue = checker.take(block(checker.protected))?;
    let result = checker.result.ok_or("正常yield/complete缺少保护区外出口值")?;
    if !epilogue.stmts.is_empty() || !same(&epilogue.terminator, &Terminator::Return(Some(Operand::Local(result)))) {
        return Err("正常出口值被保护区外操作改写".into());
    }
    let roles = [checker.frame, active, caught, result, checker.state, checker.next_pc.unwrap()];
    if roles.iter().copied().collect::<FxHashSet<_>>().len() != roles.len() {
        return Err("私有frame/active/caught/result/state/next_pc角色别名".into());
    }
    Ok(())
}
