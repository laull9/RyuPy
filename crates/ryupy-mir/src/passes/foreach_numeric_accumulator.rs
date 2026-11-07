//! 严格通用迭代局部holder：保留真实迭代器、Current和动态求值次序。
//! 只接纳入口初始化、单回边、返回total及已知TracePop出口；不扩旧range matcher。
use super::MirPass;
use super::foreach_numeric_accumulator_analysis::*;
use super::numeric_accumulator_match::{Plan, predecessors};
use crate::mir::*;
use rustc_hash::FxHashSet;

pub struct ForEachNumericAccumulator;
impl MirPass for ForEachNumericAccumulator {
    fn name(&self) -> &'static str {
        "foreach_numeric_accumulator_experimental"
    }
    fn run(&self, function: &mut Function) {
        if !eligible(function) {
            return;
        }
        if let Some(plan) = candidate(function) {
            // 与已验收holder共用ABI；不添加direct整数表示或运行时类型猜测。
            super::numeric_accumulator_holder::expand(function, plan);
        }
    }
}

fn candidate(f: &Function) -> Option<Plan> {
    let p = f.body.entry;
    let Terminator::Goto(h) = f.body.blocks[p].terminator else {
        return None;
    };
    let Terminator::Branch {
        cond: Operand::Local(ok),
        then_: b,
        else_: x,
    } = f.body.blocks[h].terminator
    else {
        return None;
    };
    if [p, h, b, x].into_iter().collect::<FxHashSet<_>>().len() != 4 {
        return None;
    }
    let preds = predecessors(f);
    if !preds[&p].is_empty()
        || preds[&h].len() != 2
        || !preds[&h].contains(&p)
        || !preds[&h].contains(&b)
        || preds[&b] != [h]
        || preds[&x] != [h]
        || !matches!(f.body.blocks[b].terminator, Terminator::Goto(to) if to == h)
    {
        return None;
    }
    let hs = meaningful(&f.body.blocks[h]);
    let [
        (
            _,
            Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::IterNextOk),
                        args,
                    },
            },
        ),
    ] = hs.as_slice()
    else {
        return None;
    };
    let [Operand::Local(iterator)] = args.as_slice() else {
        return None;
    };
    let iterator = *iterator;
    if *dest != ok || counts(f, ok, None) != (1, 1) || counts(f, iterator, None) != (2, 1) {
        return None;
    }
    if !f.body.blocks[p].stmts.iter().any(|s| {
        matches!(s, Statement::Assign {
        dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::IterStart), args }
    } if *dest == iterator && args.len() == 1)
    }) {
        return None;
    }
    let bs = meaningful(&f.body.blocks[b]);
    let (_, first) = *bs.first()?;
    let Statement::Assign {
        dest: current,
        value:
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::IterValue),
                args,
            },
    } = first
    else {
        return None;
    };
    if !matches!(args.as_slice(), [Operand::Local(i)] if *i == iterator) {
        return None;
    }
    let (_, second) = *bs.get(1)?;
    let Statement::Assign {
        dest: target,
        value: Rvalue::Use(Operand::Local(source)),
    } = second
    else {
        return None;
    };
    if source != current || !named_local(f, *target) || counts(f, *current, None) != (1, 1) {
        return None;
    }
    let (update, add) = *bs.get(bs.len().checked_sub(2)?)?;
    let Statement::Assign {
        dest: tmp,
        value:
            Rvalue::DynBinary {
                op: DynOp::Add,
                inplace: true,
                lhs: Operand::Local(total),
                rhs,
            },
    } = add
    else {
        return None;
    };
    let (write_index, write) = *bs.last()?;
    if write_index != update + 1
        || !matches!(write, Statement::Assign {
        dest, value: Rvalue::Use(Operand::Local(t)) } if dest == total && t == tmp)
        || !named_local(f, *total)
        || !initialized(f, *total)
        || [iterator, ok, *current, *target, *tmp, *total]
            .into_iter()
            .collect::<FxHashSet<_>>()
            .len()
            != 6
        || counts(f, *total, Some(&[h, b])) != (1, 1)
        || counts(f, *tmp, None) != (1, 1)
        || operand_reads(rhs).contains(total)
    {
        return None;
    }
    // 协议操作只能出现一次：额外Current即使使用另一迭代器也不属于本合同。
    if bs
        .iter()
        .filter(|(_, s)| {
            runtime(s).is_some_and(|r| {
                matches!(
                    r,
                    RuntimeFn::IterStart | RuntimeFn::IterNextOk | RuntimeFn::IterValue
                )
            })
        })
        .count()
        != 1
    {
        return None;
    }
    let epilogue = return_epilogue(f, x, *total, &preds)?;
    // 不可达空块可以保留；其它可执行块意味着还有未证明的出口/循环。
    if f.body.blocks.iter().any(|(id, block)| {
        ![p, h, b, x, epilogue].contains(&id)
            && (!block.stmts.is_empty() || !matches!(block.terminator, Terminator::Unreachable))
    }) {
        return None;
    }
    Some(Plan {
        preheader: p,
        body: b,
        exit: x,
        update,
        total: *total,
        rhs: rhs.clone(),
    })
}

fn return_epilogue(
    f: &Function,
    x: BlockId,
    total: LocalId,
    preds: &rustc_hash::FxHashMap<BlockId, Vec<BlockId>>,
) -> Option<BlockId> {
    let xs = meaningful(&f.body.blocks[x]);
    let [
        (
            _,
            Statement::Assign {
                dest: result,
                value: Rvalue::Use(Operand::Local(source)),
            },
        ),
    ] = xs.as_slice()
    else {
        return None;
    };
    if *source != total || *result == total || counts(f, *result, None) != (1, 1) {
        return None;
    }
    let Terminator::Goto(e) = f.body.blocks[x].terminator else {
        return None;
    };
    if e == x || preds[&e] != [x] {
        return None;
    }
    let es = meaningful(&f.body.blocks[e]);
    let [
        (
            _,
            Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TracePopTo),
                args,
            }),
        ),
    ] = es.as_slice()
    else {
        return None;
    };
    let [Operand::Local(depth)] = args.as_slice() else {
        return None;
    };
    if !matches!(f.body.blocks[e].terminator,Terminator::Return(Some(Operand::Local(r))) if r == *result)
        || counts(f, *depth, None) != (1, 1)
        || !f.body.blocks[f.body.entry].stmts.iter().any(|s| {
            matches!(s,
            Statement::Assign {dest,value:Rvalue::Call {callee:Callee::Runtime(
                RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin),..}} if dest == depth)
        })
    {
        return None;
    }
    Some(e)
}
