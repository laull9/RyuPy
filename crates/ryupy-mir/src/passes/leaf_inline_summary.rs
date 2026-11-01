//! 纯叶摘要只解释可证明的参数复制与一次普通加法；未知操作立即放弃，不猜注解。
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

#[derive(Clone, Copy)]
pub(super) struct Leaf {
    pub function: FnId,
    pub global: GlobalId,
    pub arity: usize,
    pub left: usize,
    pub right: usize,
}
#[derive(Clone, Copy)]
enum Value {
    Param(usize),
    Sum,
}

pub(super) fn summarize(id: FnId, f: &Function) -> Option<Leaf> {
    let global = f.function_global?;
    if !(1..=2).contains(&f.params.len())
        || f.ret != MirType::Dynamic
        || f.env.is_some()
        || !f.body.regions.is_empty()
        || f.defaults_global.is_none()
        || f.signature_global.is_none()
        || f.param_meta.len() != f.params.len()
        || f.param_meta.iter().any(|p| {
            p.has_default
                || !matches!(
                    p.kind,
                    ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword
                )
        })
        || f.body.locals.iter().any(|(_, l)| l.is_cell)
        || f.params
            .iter()
            .any(|p| f.body.locals[*p].ty != MirType::Dynamic)
    {
        return None;
    }
    let mut values: FxHashMap<_, _> = f
        .params
        .iter()
        .enumerate()
        .map(|(i, p)| (*p, Value::Param(i)))
        .collect();
    let mut visited = FxHashSet::default();
    let mut next = f.body.entry;
    let mut addition = None;
    let mut trace = None;
    let mut popped = false;
    let mut statements = 0;
    loop {
        if !visited.insert(next) {
            return None;
        }
        let b = &f.body.blocks[next];
        for stmt in &b.stmts {
            statements += 1;
            if statements > 32 {
                return None;
            }
            match stmt {
                Statement::Line(_) if !popped => {}
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::TracePush),
                            args,
                        },
                } if trace.is_none()
                    && addition.is_none()
                    && args.len() == 2
                    && args
                        .iter()
                        .all(|a| matches!(a, Operand::Const(Constant::Str(_)))) =>
                {
                    // TracePush也写局部：不能沿用该槽之前的参数/求和值事实。
                    values.remove(dest);
                    trace = Some(*dest);
                }
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TracePopTo),
                    args,
                }) if !popped
                    && matches!(args.as_slice(), [Operand::Local(value)] if Some(*value)==trace) =>
                {
                    popped = true;
                }
                Statement::Assign {
                    dest,
                    value: Rvalue::Use(Operand::Local(src)),
                } if !popped && Some(*dest) != trace => {
                    let value = *values.get(src)?;
                    values.insert(*dest, value);
                }
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::DynBinary {
                            op: DynOp::Add,
                            lhs: Operand::Local(lhs),
                            rhs: Operand::Local(rhs),
                            inplace: false,
                        },
                } if addition.is_none() && !popped && Some(*dest) != trace => {
                    let (Value::Param(left), Value::Param(right)) =
                        (*values.get(lhs)?, *values.get(rhs)?)
                    else {
                        return None;
                    };
                    addition = Some((left, right));
                    values.insert(*dest, Value::Sum);
                }
                _ => return None,
            }
        }
        match b.terminator {
            Terminator::Goto(target) => next = target,
            Terminator::Return(Some(Operand::Local(result)))
                if popped && matches!(values.get(&result), Some(Value::Sum)) =>
            {
                let (left, right) = addition?;
                return Some(Leaf {
                    function: id,
                    global,
                    arity: f.params.len(),
                    left,
                    right,
                });
            }
            _ => return None,
        }
    }
}
