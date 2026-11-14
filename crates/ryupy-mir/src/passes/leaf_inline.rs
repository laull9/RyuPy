//! 有预算的调用点特化：模块槽来源只用于预测，原函数的不可变身份锚点才是正确性守卫。
//! 默认只选择调用前全部实参已有must精确Int64证明的位置，未知/bool不额外添加guard开销。
//! 每目标最多一个纯叶摘要、每函数四处、每模块三十二处和八个目标；失败保留完整动态调用。
use super::{ProgramPass, leaf_inline_expand, leaf_inline_summary};
use crate::mir::*;
use rustc_hash::FxHashMap;

pub struct LeafInline;
impl ProgramPass for LeafInline {
    fn name(&self) -> &'static str {
        "guarded_leaf_inline"
    }
    fn run(&self, program: &mut Program) {
        let leaves: Vec<_> = program
            .functions
            .iter()
            .filter_map(|(id, f)| leaf_inline_summary::summarize(id, f))
            .take(8)
            .collect();
        if leaves.is_empty() {
            return;
        }
        let mut globals: FxHashMap<GlobalId, usize> = leaves
            .iter()
            .enumerate()
            .map(|(i, l)| (l.global, i))
            .collect();
        // 有界传播只发现已赋过原函数的别名，不将其视为不变事实；后续未知写入仍由身份guard兜底。
        for _ in 0..8 {
            let mut changed = false;
            for (_, f) in program.functions.iter() {
                for (_, b) in f.body.blocks.iter() {
                    let mut locals: FxHashMap<LocalId, usize> = FxHashMap::default();
                    for stmt in &b.stmts {
                        if let Statement::SetGlobal { dest, value } = stmt
                            && let Some(which) = origin(value, &locals, &globals)
                            && !globals.contains_key(dest)
                        {
                            globals.insert(*dest, which);
                            changed = true;
                        }
                        transfer(stmt, &mut locals, &globals);
                    }
                }
            }
            if !changed {
                break;
            }
        }
        let mut budget = 32;
        let mut anchors = FxHashMap::default();
        let ids: Vec<_> = program.functions.iter().map(|(id, _)| id).collect();
        for id in ids {
            let f = &program.functions[id];
            if f.signature_global.is_none()
                || f.env.is_some()
                || !f.body.regions.is_empty()
                || f.body.locals.iter().any(|(_, l)| l.is_cell)
                || f.body.blocks.iter().any(|(_, b)| {
                    matches!(b.terminator, Terminator::Suspend { .. })
                        || b.stmts.iter().any(|s| {
                            matches!(
                                s,
                                Statement::Assign {
                                    value: Rvalue::MakeGenerator { .. },
                                    ..
                                }
                            )
                        })
                })
            {
                continue;
            }
            // 先记录源调用的dest，展开新增块后再找位置，避免遍历新fallback重复特化。
            let exact_sites = crate::exact_types::exact_call_sites(f);
            let mut calls = Vec::new();
            for (bb, b) in f.body.blocks.iter() {
                let mut locals: FxHashMap<LocalId, usize> = FxHashMap::default();
                for (position, stmt) in b.stmts.iter().enumerate() {
                    if let Statement::Assign {
                        dest,
                        value:
                            Rvalue::DynCallValue {
                                callee: Operand::Local(callee),
                                args,
                            },
                    } = stmt
                        && let Some(&which) = locals.get(callee)
                        && args.len() == leaves[which].arity
                        && exact_sites.contains(&(bb, position))
                    {
                        calls.push((*dest, which));
                    }
                    transfer(stmt, &mut locals, &globals);
                }
            }
            for (dest, which) in calls.into_iter().take(4) {
                if budget == 0 {
                    return;
                }
                let leaf = leaves[which];
                let anchor = *anchors.entry(which).or_insert_with(|| {
                    let slot = program.globals.alloc(GlobalDecl {
                        storage: crate::GlobalStorage::LegacyStatic,
                        name: format!("__leaf_identity_{}", leaf.function.into_raw()),
                        ty: MirType::Dynamic,
                    });
                    program.functions[leaf.function].specialization_identity_global = Some(slot);
                    slot
                });
                let f = &mut program.functions[id];
                let found=f.body.blocks.iter().find_map(|(bb,b)| b.stmts.iter().position(|s|matches!(s,Statement::Assign{dest:d,value:Rvalue::DynCallValue{..}}if *d==dest)).map(|pos|(bb,pos)));
                if let Some((bb, pos)) = found {
                    leaf_inline_expand::expand(f, bb, pos, anchor, leaf);
                    budget -= 1;
                }
            }
        }
    }
}
fn origin(
    op: &Operand,
    locals: &FxHashMap<LocalId, usize>,
    globals: &FxHashMap<GlobalId, usize>,
) -> Option<usize> {
    match op {
        Operand::Local(l) => locals.get(l).copied(),
        Operand::Global(g) => globals.get(g).copied(),
        _ => None,
    }
}
fn transfer(
    stmt: &Statement,
    locals: &mut FxHashMap<LocalId, usize>,
    globals: &FxHashMap<GlobalId, usize>,
) {
    if let Statement::Assign { dest, value } = stmt {
        let source = match value {
            Rvalue::Use(op) => origin(op, locals, globals),
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::LoadName),
                args,
            } => args.first().and_then(|op| origin(op, locals, globals)),
            _ => None,
        };
        locals.remove(dest);
        if let Some(which) = source {
            locals.insert(*dest, which);
        }
    }
}
