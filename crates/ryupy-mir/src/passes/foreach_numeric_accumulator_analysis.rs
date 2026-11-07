//! ForEach候选的绑定与读写证明；复用exhaustive操作数遍历，包含数组和零参数隐式frame。
use super::dead_values::{read_operand, read_value};
use crate::mir::*;
use rustc_hash::FxHashSet;

pub(super) fn meaningful(block: &BasicBlock) -> Vec<(usize, &Statement)> {
    block
        .stmts
        .iter()
        .enumerate()
        .filter(|(_, s)| !matches!(s, Statement::Line(_)))
        .collect()
}
pub(super) fn runtime(s: &Statement) -> Option<RuntimeFn> {
    match s {
        Statement::Assign { value, .. } | Statement::Eval(value) => match value {
            Rvalue::Call {
                callee: Callee::Runtime(r),
                ..
            }
            | Rvalue::CallWithArgs { callee: r, .. } => Some(*r),
            _ => None,
        },
        _ => None,
    }
}
// 解包目标有编译器命名槽，不能把name.is_some误当源码simple target证明。
// 首切保守拒绝整个函数的解包ABI，不依据隐藏名称拼写。
pub(super) fn eligible(f: &Function) -> bool {
    f.signature_global.is_some()
        && f.env.is_none()
        && f.body.regions.is_empty()
        && !f.body.locals.iter().any(|(_, l)| l.is_cell)
        && !f.body.blocks.iter().any(|(_, b)| {
            matches!(b.terminator, Terminator::Suspend { .. })
                || b.stmts.iter().any(|s| {
                    matches!(
                        s,
                        Statement::Assign {
                            value: Rvalue::MakeGenerator { .. },
                            ..
                        } | Statement::Eval(Rvalue::MakeGenerator { .. })
                    ) || runtime(s).is_some_and(|r| {
                        matches!(
                            r,
                            RuntimeFn::UnpackSequence
                                | RuntimeFn::UnpackEx
                                | RuntimeFn::UnpackCheck
                                | RuntimeFn::MakeUnbound
                                | RuntimeFn::LoadChecked
                                | RuntimeFn::CellNew
                                | RuntimeFn::CellGet
                                | RuntimeFn::CellSet
                                | RuntimeFn::CellGetFree
                                | RuntimeFn::FunctionLocalsEnsure
                                | RuntimeFn::FunctionLocalsSync
                                | RuntimeFn::FunctionLocalsValidate
                        )
                    })
                })
        })
}
pub(super) fn named_local(f: &Function, l: LocalId) -> bool {
    let decl = &f.body.locals[l];
    decl.ty == MirType::Dynamic
        && decl.name.is_some()
        && !decl.is_cell
        && f.module_state != Some(l)
        && f.env != Some(l)
        && !f.dynamic_origin.is_some_and(|origin| origin.owner == l)
}
pub(super) fn operand_reads(o: &Operand) -> FxHashSet<LocalId> {
    let mut reads = FxHashSet::default();
    read_operand(o, &mut reads);
    reads
}
fn statement_reads(s: &Statement) -> FxHashSet<LocalId> {
    let mut reads = FxHashSet::default();
    match s {
        Statement::Assign { value, .. } | Statement::Eval(value) => read_value(value, &mut reads),
        Statement::SetField { obj, value, .. } => {
            read_operand(obj, &mut reads);
            read_operand(value, &mut reads);
        }
        Statement::SetGlobal { value, .. } => read_operand(value, &mut reads),
        Statement::Line(_) => {}
    }
    reads
}
pub(super) fn counts(f: &Function, l: LocalId, blocks: Option<&[BlockId]>) -> (usize, usize) {
    let (mut reads, mut writes) = (0, 0);
    for (id, b) in f.body.blocks.iter() {
        if blocks.is_some_and(|blocks| !blocks.contains(&id)) {
            continue;
        }
        for s in &b.stmts {
            reads += usize::from(statement_reads(s).contains(&l));
            writes += usize::from(matches!(s,Statement::Assign {dest,..} if *dest==l));
        }
        let mut term = FxHashSet::default();
        match &b.terminator {
            Terminator::Branch { cond, .. } => read_operand(cond, &mut term),
            Terminator::Return(value) => {
                if let Some(v) = value {
                    read_operand(v, &mut term);
                }
            }
            Terminator::Raise { value, cause } => {
                for v in value.iter().chain(cause.iter()) {
                    read_operand(v, &mut term);
                }
            }
            Terminator::Suspend { value, input, .. } => {
                read_operand(value, &mut term);
                term.extend(input);
            }
            Terminator::Goto(_) | Terminator::Unreachable => {}
        }
        reads += usize::from(term.contains(&l));
    }
    (reads, writes)
}

/// 前向跟踪允许的种子来源与最后一次写入，而不是“曾有Assign”即判绑定。
/// 首切只证明参数/非null常量经Use或Box形成的初始化；其它初始化保守走原路径。
pub(super) fn initialized(f: &Function, total: LocalId) -> bool {
    fn bound(o: &Operand, known: &FxHashSet<LocalId>) -> bool {
        match o {
            Operand::Local(l) => known.contains(l),
            // Array虽是合法Dynamic MIR值，却不是本实验承诺的参数/常量经Use/Box来源。
            Operand::Const(Constant::Null) | Operand::Global(_) | Operand::Array(_) => false,
            Operand::Const(_) => true,
        }
    }
    let mut known: FxHashSet<_> = f.params.iter().copied().collect();
    let mut assigned = false;
    for s in &f.body.blocks[f.body.entry].stmts {
        if let Statement::Assign { dest, value } = s {
            let is_bound = match value {
                Rvalue::Use(o) | Rvalue::Box { value: o, .. } => bound(o, &known),
                _ => false,
            };
            known.remove(dest);
            if is_bound {
                known.insert(*dest);
            }
            assigned |= *dest == total;
        }
    }
    assigned && known.contains(&total)
}
