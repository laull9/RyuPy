//! 无EH生成器暂停边界的局部活跃性；用户绑定/实参保留引用，短命临时只在实际跨暂停时驻留。
use crate::local_access::{statement_reads, terminator_reads};
use crate::mir::*;
use rustc_hash::FxHashSet;

pub(crate) fn eligible(f: &Function) -> bool {
    f.module_state.is_none()
        && f.env.is_none()
        && f.body.regions.is_empty()
        && f.body.source_handler_contracts.is_empty()
        && f.body.direct_globals_contracts.is_empty()
        && f.body.locals.len() <= 512
        && f.body.blocks.len() <= 512
        && f.body.locals.iter().all(|(_, l)| {
            !l.is_cell
                && matches!(
                    l.ty,
                    MirType::Dynamic | MirType::I64 | MirType::F64 | MirType::Bool | MirType::Str
                )
        })
        && f.body.blocks.iter().all(|(_, b)| {
            !matches!(
                b.terminator,
                Terminator::Suspend {
                    delegated: true,
                    ..
                }
            ) && b.stmts.iter().all(|s| match s {
                Statement::Assign { value, .. } | Statement::Eval(value) => match value {
                    Rvalue::GeneratorDelegateStep { .. } | Rvalue::CatchException => false,
                    Rvalue::Call {
                        callee: Callee::Runtime(rt),
                        args,
                    } => {
                        !matches!(
                            rt,
                            RuntimeFn::FunctionLocalsValidate
                                | RuntimeFn::FunctionLocalsEnsure
                                | RuntimeFn::FunctionLocalsSync
                        ) && crate::trace_product_abi::signature(*rt).is_none()
                            && !(*rt == RuntimeFn::DynBuiltin && matches!(args.first(), Some(Operand::Const(Constant::Str(name))) if name == "locals"))
                    }
                    _ => true,
                },
                _ => true,
            })
        })
}

/// 收敛失败直接放弃优化；不把分析预算耗尽误认为没有活值。
pub(crate) fn residents(f: &Function) -> Option<FxHashSet<LocalId>> {
    let mut live = vec![FxHashSet::default(); f.body.blocks.len()];
    for _ in 0..64 {
        let mut changed = false;
        for (id, block) in f.body.blocks.iter().rev() {
            let mut used = terminator_reads(&block.terminator);
            for next in successors(&block.terminator) {
                used.extend(live.get(next.into_raw().into_u32() as usize)?.iter().copied().filter(|local|
                    !matches!(block.terminator, Terminator::Suspend { input: Some(input), .. } if *local == input)));
            }
            for s in block.stmts.iter().rev() {
                if let Statement::Assign { dest, .. } = s {
                    used.remove(dest);
                }
                used.extend(statement_reads(s));
            }
            let slot = &mut live[id.into_raw().into_u32() as usize];
            if *slot != used {
                *slot = used;
                changed = true;
            }
        }
        if !changed {
            let mut result: FxHashSet<_> = f.params.iter().copied().collect();
            // 保留Python命名绑定的引用存活；仅对编译器临时做暂停边活跃性裁剪。
            result.extend(
                f.body
                    .locals
                    .iter()
                    .filter_map(|(id, l)| l.name.is_some().then_some(id)),
            );
            for (_, b) in f.body.blocks.iter() {
                if let Terminator::Suspend { resume, input, .. } = b.terminator {
                    result.extend(
                        live[resume.into_raw().into_u32() as usize]
                            .iter()
                            .copied()
                            .filter(|local| Some(*local) != input),
                    );
                }
            }
            return Some(result);
        }
    }
    None
}

fn successors(t: &Terminator) -> Vec<BlockId> {
    match t {
        Terminator::Goto(b) => vec![*b],
        Terminator::Branch { then_, else_, .. } => vec![*then_, *else_],
        Terminator::Suspend { resume, .. } => vec![*resume],
        Terminator::Return(_) | Terminator::Raise { .. } | Terminator::Unreachable => vec![],
    }
}
