//! 实体key接线的只读计划器。未接入生产pass；仅明确同块连续窗口，不猜跨块别名。
use crate::local_access::{statement_reads, terminator_reads};
use crate::mir::*;
use rustc_hash::FxHashSet;

#[derive(Debug)]
pub(crate) struct Plan {
    pub block: BlockId,
    pub start: usize,
    pub receiver: LocalId,
    pub key: LocalId,
}

pub(crate) fn plans(f: &Function) -> Vec<Plan> {
    if f.env.is_some()
        || f.module_state.is_some()
        || !f.body.regions.is_empty()
        || !f.body.direct_globals_contracts.is_empty()
        || !f.body.source_handler_contracts.is_empty()
        || f.body.locals.len() > 512
        || f.body.blocks.len() > 128
        || f.body.blocks.iter().map(|(_, b)| b.stmts.len()).sum::<usize>() > 4096
        || f.body.locals.iter().any(|(_, l)| l.is_cell)
        || f.body.blocks.iter().any(|(_, b)| {
            matches!(b.terminator, Terminator::Suspend { .. })
                || b.stmts.iter().any(|s| match s {
                    Statement::Assign { value, .. } | Statement::Eval(value) => matches!(
                        value,
                        Rvalue::MakeGenerator { .. }
                            | Rvalue::MakeStateGenerator { .. }
                            | Rvalue::Call {
                                callee: Callee::Runtime(
                                    RuntimeFn::FunctionLocalsValidate
                                        | RuntimeFn::FunctionLocalsEnsure
                                        | RuntimeFn::FunctionLocalsSync
                                ),
                                ..
                            }
                    ),
                    _ => false,
                })
        })
    {
        return Vec::new();
    }
    let mut result = Vec::new();
    for (block, b) in f.body.blocks.iter() {
        for start in 4..b.stmts.len().saturating_sub(3) {
            if let Some(plan) = candidate(f, block, start) {
                result.push(plan);
                if result.len() == 4 {
                    return result;
                }
            }
        }
    }
    result
}

fn assignment(s: &Statement) -> Option<(LocalId, &Rvalue)> {
    match s {
        Statement::Assign { dest, value } => Some((*dest, value)),
        _ => None,
    }
}

fn candidate(f: &Function, block: BlockId, start: usize) -> Option<Plan> {
    let statements = &f.body.blocks[block].stmts;
    let (snapshot, Rvalue::Use(Operand::Local(receiver))) = assignment(&statements[start - 4])? else {
        return None;
    };
    let (flag, Rvalue::Call { callee: Callee::Runtime(RuntimeFn::IsModule), args: flag_args }) = assignment(&statements[start - 3])? else {
        return None;
    };
    let (target, Rvalue::Call { callee: Callee::Runtime(RuntimeFn::CaptureModuleTarget), args: capture_args }) = assignment(&statements[start - 2])? else {
        return None;
    };
    let (zero, Rvalue::Box { value: Operand::Const(Constant::I64(0)), from: MirType::I64 }) = assignment(&statements[start - 1])? else {
        return None;
    };
    let (old, Rvalue::DynCallMethod { captured: Some(captured), obj, name, args }) = assignment(&statements[start])? else {
        return None;
    };
    let (one, Rvalue::Box { value: Operand::Const(Constant::I64(1)), from: MirType::I64 }) = assignment(&statements[start + 1])? else {
        return None;
    };
    let (updated, Rvalue::DynBinary { op: DynOp::Add, lhs, rhs, inplace: false }) = assignment(&statements[start + 2])? else {
        return None;
    };
    let Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::SetItem), args: set_args }) = &statements[start + 3] else {
        return None;
    };
    let [Operand::Local(key), default] = args.as_slice() else { return None; };
    if flag_args != &[Operand::Local(snapshot)]
        || capture_args != &[Operand::Local(snapshot), Operand::Const(Constant::Str("get".into()))]
        || captured != &Operand::Local(flag)
        || obj != &Operand::Local(target)
        || name != "get"
        || default != &Operand::Local(zero)
        || lhs != &Operand::Local(old)
        || rhs != &Operand::Local(one)
        || set_args != &[Operand::Local(*receiver), Operand::Local(*key), Operand::Local(updated)]
    {
        return None;
    }
    let temps = [snapshot, flag, target, zero, old, one, updated];
    let unique: FxHashSet<_> = temps.into_iter().collect();
    if unique.len() != temps.len() || unique.contains(receiver) || unique.contains(key)
        || f.body.locals[*receiver].ty != MirType::Dynamic || f.body.locals[*key].ty != MirType::Dynamic
        || temps.iter().any(|id| f.params.contains(id) || f.body.locals[*id].name.is_some())
    {
        return None;
    }
    // 七个临时量的定义和读取全部封闭在原八语句内，且每个仅定义一次。
    // 因此快路跳过old/one/updated不会令后续值未初始化；capture也不会被外部覆盖。
    for temp in temps {
        let mut definitions = 0;
        for (other_block, body) in f.body.blocks.iter() {
            for (position, statement) in body.stmts.iter().enumerate() {
                let inside = other_block == block && (start - 4..start + 4).contains(&position);
                if let Statement::Assign { dest, .. } = statement {
                    if *dest == temp {
                        definitions += 1;
                        if !inside { return None; }
                    }
                }
                if !inside && statement_reads(statement).contains(&temp) { return None; }
            }
            if terminator_reads(&body.terminator).contains(&temp) { return None; }
        }
        if definitions != 1 { return None; }
    }
    Some(Plan { block, start, receiver: *receiver, key: *key })
}
