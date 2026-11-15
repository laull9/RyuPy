//! 关闭态range来源证据：入口仍需检查真实sealed PyRangeIter，不把源码range名字视为证明。
//! 这里只识别单游标+精确整数乘法；不替换MoveNext、不消除盒、不做预求和或原生计数。
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

#[derive(Clone, Copy, Debug, PartialEq)]
enum Fact {
    // 只有对应入口的运行时类型守卫成功后，才可假定此事实成立。
    GuardedRangeCursor,
    ExactLong,
    BuiltinInteger,
    Boolean,
    None,
    TraceDepth,
}

#[derive(Debug)]
struct RangePlan {
    guard_input: LocalId,
    iterator: LocalId,
    multiply: LocalId,
    yield_block: BlockId,
    resume: BlockId,
}

fn analyze(f: &Function, declared_input: LocalId) -> Option<RangePlan> {
    if !crate::generator_state_analysis::eligible(f) || f.params.first() != Some(&declared_input) {
        return None;
    }
    // 非SSA函数只接受每个局部单个静态定义点；控制流支配证明还须在发射前完成。
    // 此原型输出候选，不是允许发射的最终证明，不向产品lowering暴露。
    let mut definitions = FxHashSet::default();
    for (_, block) in f.body.blocks.iter() {
        for statement in &block.stmts {
            if let Statement::Assign { dest, .. } = statement {
                if f.params.contains(dest) || !definitions.insert(*dest) {
                    return None;
                }
            }
        }
    }
    let mut facts = FxHashMap::default();
    facts.insert(declared_input, Fact::GuardedRangeCursor);
    for _ in 0..f.body.locals.len() + 1 {
        let before = facts.len();
        for (_, block) in f.body.blocks.iter() {
            for statement in &block.stmts {
                if let Statement::Assign { dest, value } = statement
                    && let Some(fact) = infer(value, &facts)
                {
                    facts.insert(*dest, fact);
                }
            }
        }
        if facts.len() == before {
            break;
        }
    }
    let mut iterator = None;
    let mut multiply = None;
    let mut edge = None;
    for (id, block) in f.body.blocks.iter() {
        for statement in &block.stmts {
            match statement {
                Statement::Line(_) => {}
                Statement::Assign { dest, value } => {
                    infer(value, &facts)?;
                    if matches!(value, Rvalue::Call { callee: Callee::Runtime(RuntimeFn::IterStart), .. }) {
                        if iterator.replace(*dest).is_some() {
                            return None;
                        }
                    }
                    if matches!(value, Rvalue::DynBinary { .. }) && multiply.replace(*dest).is_some() {
                        return None;
                    }
                }
                _ => return None,
            }
        }
        match &block.terminator {
            Terminator::Goto(_) | Terminator::Return(None) => {}
            Terminator::Return(Some(value)) if fact(value, &facts) == Some(Fact::None) => {}
            Terminator::Branch { cond, .. } if fact(cond, &facts) == Some(Fact::Boolean) => {}
            Terminator::Suspend { value, resume, input: None, delegated: false }
                if fact(value, &facts) == Some(Fact::BuiltinInteger) =>
            {
                if edge.replace((id, *resume)).is_some() {
                    return None;
                }
            }
            _ => return None,
        }
    }
    let (yield_block, resume) = edge?;
    Some(RangePlan { guard_input: declared_input, iterator: iterator?, multiply: multiply?, yield_block, resume })
}

fn fact(value: &Operand, facts: &FxHashMap<LocalId, Fact>) -> Option<Fact> {
    match value {
        Operand::Local(id) => facts.get(id).copied(),
        Operand::Const(Constant::I64(_)) => Some(Fact::ExactLong),
        Operand::Const(Constant::None) => Some(Fact::None),
        _ => None,
    }
}

fn infer(value: &Rvalue, facts: &FxHashMap<LocalId, Fact>) -> Option<Fact> {
    match value {
        Rvalue::Use(value) | Rvalue::Box { value, from: MirType::I64 } => fact(value, facts),
        Rvalue::DynBinary { op: DynOp::Mul, lhs, rhs, inplace: false }
            if fact(lhs, facts) == Some(Fact::ExactLong) && fact(rhs, facts) == Some(Fact::ExactLong) =>
        {
            // long×long可能升级BigInteger；不能声称ExactLong或换成unchecked乘法。
            Some(Fact::BuiltinInteger)
        }
        Rvalue::Call { callee: Callee::Runtime(runtime), args } => match runtime {
            RuntimeFn::IterStart | RuntimeFn::IterNextOk | RuntimeFn::IterValue
                if args.len() == 1 && fact(&args[0], facts) == Some(Fact::GuardedRangeCursor) =>
            {
                Some(match runtime {
                    RuntimeFn::IterStart => Fact::GuardedRangeCursor,
                    RuntimeFn::IterNextOk => Fact::Boolean,
                    _ => Fact::ExactLong,
                })
            }
            RuntimeFn::DynNone if args.is_empty() => Some(Fact::None),
            // 守卫由实际工厂参数提供；trace来源不用于推断range或用户值。
            RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin => Some(Fact::TraceDepth),
            _ => None,
        },
        _ => None,
    }
}

#[test]
fn original_genexpr_has_a_single_cursor_candidate_not_a_product_proof() {
    let (program, deferred) = super::generator_sum_plan::high_with_sources(include_str!(
        "../../../tests/benchmarks/generators.py"
    ));
    assert_eq!(deferred.comprehension_inputs.len(), 1);
    let (function, input) = deferred.comprehension_inputs[0];
    let plan = analyze(&program.functions[function], input).unwrap();
    assert_eq!(plan.guard_input, input);
    assert_ne!(plan.iterator, input);
    assert_ne!(plan.multiply, plan.iterator);
    assert_ne!(plan.yield_block, plan.resume);
}

#[test]
fn callbacks_closure_and_multiple_cursors_remain_rejected() {
    for source in [
        "def f(x):\n    return x\ng = (f(x) for x in range(4))\n",
        "g = (x * y for x in range(3) for y in range(4))\n",
        "def f(n):\n    return (x * n for x in range(4))\n",
        "g = (x * 2 for x in range(4) if x)\n",
    ] {
        let (program, deferred) = super::generator_sum_plan::high_with_sources(source);
        assert!(!deferred.comprehension_inputs.is_empty());
        for (function, input) in deferred.comprehension_inputs {
            assert!(analyze(&program.functions[function], input).is_none());
        }
    }
}
