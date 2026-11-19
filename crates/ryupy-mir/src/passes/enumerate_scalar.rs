//! 精确enumerate游标的立即二元解包：仅删除无逃逸临时元组，两个元素各求值一次且保持顺序。
use super::MirPass;
use crate::local_access::{statement_reads, terminator_reads};
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};
pub struct EnumerateScalar;
struct Plan {
    bb: BlockId,
    start: usize,
    end: usize,
    iterator: Operand,
    first: LocalId,
    second: LocalId,
}
impl MirPass for EnumerateScalar {
    fn name(&self) -> &'static str {
        "enumerate_scalar"
    }
    fn run(&self, f: &mut Function) {
        if !f.is_comprehension
            || f.env.is_some()
            || f.module_state.is_some()
            || !f.body.regions.is_empty()
            || f.body.locals.iter().any(|(_, l)| l.is_cell)
            || f.body.blocks.len() > 64
            || !f.body.source_handler_contracts.is_empty()
            || !f.body.direct_globals_contracts.is_empty()
            || f.body.blocks.iter().any(|(_,b)| b.stmts.iter().any(|s| matches!(s,
                Statement::Assign {value:Rvalue::Call {callee:Callee::Runtime(rt),..},..}
                | Statement::Eval(Rvalue::Call {callee:Callee::Runtime(rt),..}) if crate::trace_product_abi::signature(*rt).is_some())))
        {
            return;
        }
        let mut plans = vec![];
        for (bb, b) in f.body.blocks.iter() {
            for at in 0..b.stmts.len() {
                if plans.len() < 8 {
                    if let Some(p) = analyze(f, bb, at) {
                        plans.push(p);
                    }
                }
            }
        }
        for p in plans.into_iter().rev() {
            expand(f, p);
        }
    }
}
fn analyze(f: &Function, bb: BlockId, start: usize) -> Option<Plan> {
    let b = &f.body.blocks[bb];
    let Statement::Assign {
        dest,
        value:
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::IterValue),
                args,
            },
    } = &b.stmts[start]
    else {
        return None;
    };
    let [iterator] = args.as_slice() else {
        return None;
    };
    let mut tuple = FxHashSet::default();
    tuple.insert(*dest);
    let mut ephemeral = tuple.clone();
    let mut indices = FxHashMap::default();
    let mut unpacked = None;
    let mut outputs = [None, None];
    let mut end = start + 1;
    for (at, s) in b.stmts.iter().enumerate().skip(start + 1).take(12) {
        match s {
            Statement::Line(_) => {}
            Statement::Assign {
                dest,
                value: Rvalue::Use(Operand::Local(id)),
            } if tuple.contains(id) => {
                if f.body.locals[*dest]
                    .name
                    .as_ref()
                    .is_some_and(|name| !name.starts_with(".item"))
                {
                    return None;
                }
                tuple.insert(*dest);
                ephemeral.insert(*dest);
            }
            Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::UnpackSequence),
                        args,
                    },
            } if args.len() == 2
                && matches!(&args[0],Operand::Local(id)if tuple.contains(id))
                && args[1] == Operand::Const(Constant::I64(2))
                && unpacked.is_none() =>
            {
                unpacked = Some(*dest);
                ephemeral.insert(*dest);
            }
            Statement::Assign {
                dest,
                value:
                    Rvalue::Box {
                        from: MirType::I64,
                        value: Operand::Const(Constant::I64(v)),
                    },
            } if *v == 0 || *v == 1 => {
                indices.insert(*dest, *v as usize);
                ephemeral.insert(*dest);
            }
            Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::GetItem),
                        args,
                    },
            } if args.len() == 2 => {
                let (Operand::Local(source), Operand::Local(index)) = (&args[0], &args[1]) else {
                    return None;
                };
                if Some(*source) != unpacked {
                    return None;
                }
                let index = *indices.get(index)?;
                // 原降级先冻结第0项再第1项，不能提前执行用户赋值目标。
                if outputs[index].is_some() || index == 1 && outputs[0].is_none() {
                    return None;
                }
                outputs[index] = Some(*dest);
            }
            _ => return None,
        }
        end = at + 1;
        if outputs[1].is_some() {
            break;
        }
    }
    let [Some(first), Some(second)] = outputs else {
        return None;
    };
    // 临时tuple、解包结果与索引盒在整个函数均不能有额外观察者。
    for (id, body) in f.body.blocks.iter() {
        for (at, s) in body.stmts.iter().enumerate() {
            if (id != bb || at < start || at >= end) && !statement_reads(s).is_disjoint(&ephemeral)
            {
                return None;
            }
        }
        if !terminator_reads(&body.terminator).is_disjoint(&ephemeral) {
            return None;
        }
    }
    Some(Plan {
        bb,
        start,
        end,
        iterator: iterator.clone(),
        first,
        second,
    })
}
fn expand(f: &mut Function, p: Plan) {
    let b = &mut f.body.blocks[p.bb];
    let rest = b.stmts.split_off(p.end);
    let original = b.stmts.split_off(p.start);
    let term = std::mem::replace(&mut b.terminator, Terminator::Unreachable);
    let join = f.body.blocks.alloc(BasicBlock {
        stmts: rest,
        terminator: term,
    });
    let mut fast = vec![
        call(
            p.first,
            RuntimeFn::EnumerateCurrentIndex,
            p.iterator.clone(),
        ),
        call(
            p.second,
            RuntimeFn::EnumerateCurrentValue,
            p.iterator.clone(),
        ),
    ];
    fast.extend(
        original
            .iter()
            .filter(|s| matches!(s, Statement::Line(_)))
            .cloned(),
    );
    let yes = f.body.blocks.alloc(BasicBlock {
        stmts: fast,
        terminator: Terminator::Goto(join),
    });
    let no = f.body.blocks.alloc(BasicBlock {
        stmts: original,
        terminator: Terminator::Goto(join),
    });
    let guard = f.body.locals.alloc(LocalDecl {
        ty: MirType::Bool,
        name: None,
        is_cell: false,
    });
    f.body.blocks[p.bb]
        .stmts
        .push(call(guard, RuntimeFn::IsEnumerateIterator, p.iterator));
    f.body.blocks[p.bb].terminator = Terminator::Branch {
        cond: Operand::Local(guard),
        then_: yes,
        else_: no,
    };
}
fn call(dest: LocalId, callee: RuntimeFn, arg: Operand) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::Call {
            callee: Callee::Runtime(callee),
            args: vec![arg],
        },
    }
}
