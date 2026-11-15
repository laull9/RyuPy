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
    residents: FxHashSet<LocalId>,
}

// 这里只核对元数据内的数值ID记录；尚未绑定Program构建身份，不能用作产品证书。
// 产品化必须用拥有式Prepared对象封装同一Program/元数据，不能接受自由组合。
fn analyze_source(
    program: &Program,
    deferred: &crate::generator_product::DeferredGenerators,
    function: FnId,
    input: LocalId,
) -> Option<RangePlan> {
    if !deferred.comprehension_inputs.contains(&(function, input)) {
        return None;
    }
    let (_, f) = program.functions.iter().find(|(id, _)| *id == function)?;
    analyze(f, input)
}

fn analyze(f: &Function, declared_input: LocalId) -> Option<RangePlan> {
    if !crate::generator_state_analysis::eligible(f) || f.params.first() != Some(&declared_input) {
        return None;
    }
    // 非SSA函数只接受每个局部单个静态定义点，并证明它支配所有实际读取。
    // 此原型仍需运行时真实类型守卫，不向产品lowering暴露。
    let mut definitions = FxHashSet::default();
    let mut frame_depth = None;
    for (block_id, block) in f.body.blocks.iter() {
        for statement in &block.stmts {
            if let Statement::Assign { dest, .. } = statement {
                if f.params.contains(dest) || !definitions.insert(*dest) {
                    return None;
                }
                if let Statement::Assign { value: Rvalue::Call { callee: Callee::Runtime(
                    RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin), .. }, .. } = statement
                    && (block_id != f.body.entry || frame_depth.replace(*dest).is_some())
                {
                    return None;
                }
            }
        }
    }
    let frame_depth = frame_depth?;
    if !dominates_all_reads(f) {
        return None;
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
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TracePopTo),
                    args,
                }) if args.as_slice() == [Operand::Local(frame_depth)]
                    && fact(&args[0], &facts) == Some(Fact::TraceDepth) => {}
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
    let residents = crate::generator_state_analysis::residents(f)?;
    let plan = RangePlan { guard_input: declared_input, iterator: iterator?, multiply: multiply?, yield_block, resume, residents };
    resident_set_requirements_hold(&plan, &plan.residents).then_some(plan)
}

// 这里只检查候选集合要求，不是实际lowering字段映射证明。
// 发射后仍须核查真实Get/SetField位置/类型、yield前发布及resume读取同一字段。
fn resident_set_requirements_hold(plan: &RangePlan, fields: &FxHashSet<LocalId>) -> bool {
    plan.residents.is_subset(fields)
        && fields.contains(&plan.guard_input)
        && fields.contains(&plan.iterator)
}

// 保守的全CFG支配检查：单一静态赋值不能自动证明循环首次读取已经初始化。
fn dominates_all_reads(f: &Function) -> bool {
    use crate::local_access::{statement_reads, terminator_reads};
    let next = |term: &Terminator| -> Vec<BlockId> {
        match term {
            Terminator::Goto(id) => vec![*id],
            Terminator::Branch { then_, else_, .. } => vec![*then_, *else_],
            Terminator::Suspend { resume, .. } => vec![*resume],
            _ => vec![],
        }
    };
    let mut reachable = FxHashSet::default();
    let mut queue = vec![f.body.entry];
    while let Some(id) = queue.pop() {
        if reachable.insert(id) {
            queue.extend(next(&f.body.blocks[id].terminator));
        }
    }
    let mut predecessors: FxHashMap<BlockId, Vec<BlockId>> = FxHashMap::default();
    for &id in &reachable {
        for target in next(&f.body.blocks[id].terminator) {
            predecessors.entry(target).or_default().push(id);
        }
    }
    let mut dom: FxHashMap<BlockId, FxHashSet<BlockId>> = reachable
        .iter()
        .map(|id| (*id, if *id == f.body.entry { FxHashSet::from_iter([*id]) } else { reachable.clone() }))
        .collect();
    let mut converged = false;
    for _ in 0..=reachable.len() {
        let mut changed = false;
        for &id in &reachable {
            if id == f.body.entry { continue; }
            let Some(previous) = predecessors.get(&id) else { return false; };
            let mut common = reachable.clone();
            for predecessor in previous {
                common.retain(|item| dom[predecessor].contains(item));
            }
            common.insert(id);
            if dom[&id] != common {
                dom.insert(id, common);
                changed = true;
            }
        }
        if !changed { converged = true; break; }
    }
    if !converged { return false; }
    let mut definitions = FxHashMap::default();
    for &id in &reachable {
        for (index, statement) in f.body.blocks[id].stmts.iter().enumerate() {
            if let Statement::Assign { dest, .. } = statement {
                definitions.insert(*dest, (id, index));
            }
        }
    }
    for &id in &reachable {
        let block = &f.body.blocks[id];
        for (at, reads) in block.stmts.iter().map(statement_reads)
            .chain(std::iter::once(terminator_reads(&block.terminator))).enumerate()
        {
            for local in reads {
                if f.params.contains(&local) { continue; }
                let Some(&(definition, index)) = definitions.get(&local) else { return false; };
                if !dom[&id].contains(&definition) || definition == id && index >= at {
                    return false;
                }
            }
        }
    }
    true
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
    let plan = analyze_source(&program, &deferred, function, input).unwrap();
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


#[test]
fn single_static_definition_does_not_authorize_read_before_definition() {
    let (mut program, deferred) = super::generator_sum_plan::high_with_sources(
        "g = (x * 2 for x in range(4))\n"
    );
    let (function, input) = deferred.comprehension_inputs[0];
    assert!(analyze(&program.functions[function], input).is_some());
    let f = &mut program.functions[function];
    for (_, block) in f.body.blocks.iter_mut() {
        if let Some(index) = block.stmts.iter().position(|s| matches!(s,
            Statement::Assign { value: Rvalue::DynBinary { .. }, .. }))
        {
            let multiply = block.stmts.remove(index);
            block.stmts.insert(0, multiply);
            break;
        }
    }
    assert!(analyze(f, input).is_none());
}


#[test]
fn only_matching_trace_depth_may_be_popped() {
    for bad in [false, true] {
        let (mut program, deferred) = super::generator_sum_plan::high_with_sources(
            "g = (x * 2 for x in range(4))\n"
        );
        let (function, input) = deferred.comprehension_inputs[0];
        assert!(analyze(&program.functions[function], input).is_some());
        let mut changed = false;
        for (_, block) in program.functions[function].body.blocks.iter_mut() {
            for statement in &mut block.stmts {
                if let Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(runtime), args,
                }) = statement
                    && *runtime == RuntimeFn::TracePopTo
                {
                    if bad { *runtime = RuntimeFn::TraceCheckDepth; }
                    else { *args = vec![Operand::Const(Constant::I64(0))]; }
                    changed = true;
                }
            }
        }
        assert!(changed, "原高层genexpr必须保留真实TracePopTo负例");
        assert!(analyze(&program.functions[function], input).is_none());
    }
}


#[test]
fn numeric_ids_must_match_one_metadata_record() {
    let (program, deferred) = super::generator_sum_plan::high_with_sources(
        "def ordinary(x):\n    yield x\ng = (x * 2 for x in range(4))\n"
    );
    let (function, input) = deferred.comprehension_inputs[0];
    assert!(analyze_source(&program, &deferred, function, input).is_some());
    let wrong_function = *deferred.functions().iter().find(|id| **id != function).unwrap();
    assert!(analyze_source(&program, &deferred, wrong_function, input).is_none());
    let bad_input = LocalId::from_raw(999999u32.into());
    assert!(analyze_source(&program, &deferred, function, bad_input).is_none());
    let bad_function = FnId::from_raw(999999u32.into());
    assert!(analyze_source(&program, &deferred, bad_function, input).is_none());
}

#[test]
fn alternate_path_definition_is_not_a_dominating_fact() {
    let (mut program, deferred) = super::generator_sum_plan::high_with_sources(
        "g = (x * 2 for x in range(4))\n"
    );
    let (function, input) = deferred.comprehension_inputs[0];
    let f = &mut program.functions[function];
    let mut moved = None;
    for (_, block) in f.body.blocks.iter_mut() {
        if let Some(index) = block.stmts.iter().position(|statement| matches!(statement,
            Statement::Assign { value: Rvalue::Box { value: Operand::Const(Constant::I64(2)), .. }, .. }))
        {
            moved = Some(block.stmts.remove(index));
            break;
        }
    }
    let entry = f.body.entry;
    let defines = f.body.blocks.alloc(BasicBlock {
        stmts: vec![moved.expect("原程序常量2必须有明确装箱定义")],
        terminator: Terminator::Goto(entry),
    });
    f.body.entry = f.body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Branch {
            cond: Operand::Const(Constant::Bool(true)), then_: defines, else_: entry,
        },
    });
    assert!(!dominates_all_reads(f));
    assert!(analyze(f, input).is_none());
}

#[test]
fn shared_resume_does_not_get_a_second_yield_edge() {
    let (mut program, deferred) = super::generator_sum_plan::high_with_sources(
        "g = (x * 2 for x in range(4))\n"
    );
    let (function, input) = deferred.comprehension_inputs[0];
    let f = &mut program.functions[function];
    let edge = f.body.blocks.iter().find_map(|(_, block)| {
        matches!(block.terminator, Terminator::Suspend { .. }).then(|| block.terminator.clone())
    }).unwrap();
    f.body.blocks.alloc(BasicBlock { stmts: vec![], terminator: edge });
    assert!(analyze(f, input).is_none());
}


#[test]
fn loop_first_read_cannot_use_a_definition_after_suspend() {
    let (mut program, deferred) = super::generator_sum_plan::high_with_sources(
        "g = (x * 2 for x in range(4))\n"
    );
    let (function, input) = deferred.comprehension_inputs[0];
    let f = &mut program.functions[function];
    let plan = analyze(f, input).unwrap();
    let mut moved = None;
    for (_, block) in f.body.blocks.iter_mut() {
        if let Some(index) = block.stmts.iter().position(|statement| matches!(statement,
            Statement::Assign { value: Rvalue::Box { value: Operand::Const(Constant::I64(2)), .. }, .. }))
        {
            moved = Some(block.stmts.remove(index));
            break;
        }
    }
    f.body.blocks[plan.resume].stmts.push(moved.unwrap());
    assert!(!dominates_all_reads(f));
    assert!(analyze(f, input).is_none());
}

#[test]
fn required_resident_set_includes_original_cursor() {
    let (program, deferred) = super::generator_sum_plan::high_with_sources(
        "g = (x * 2 for x in range(4))\n"
    );
    let (function, input) = deferred.comprehension_inputs[0];
    let plan = analyze_source(&program, &deferred, function, input).unwrap();
    assert!(resident_set_requirements_hold(&plan, &plan.residents));
    let mut missing = plan.residents.clone();
    missing.remove(&plan.iterator);
    assert!(!resident_set_requirements_hold(&plan, &missing));
}

#[test]
fn second_frame_entry_cannot_masquerade_as_matching_trace_depth() {
    let (mut program, deferred) = super::generator_sum_plan::high_with_sources(
        "g = (x * 2 for x in range(4))\n"
    );
    let (function, input) = deferred.comprehension_inputs[0];
    let f = &mut program.functions[function];
    let duplicate = f.body.blocks[f.body.entry].stmts.iter().find(|statement| matches!(statement,
        Statement::Assign { value: Rvalue::Call { callee: Callee::Runtime(
            RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin), .. }, .. })).unwrap().clone();
    let new_depth = crate::generators::local(&mut f.body.locals, MirType::I64);
    let Statement::Assign { value, .. } = duplicate else { unreachable!() };
    f.body.blocks[f.body.entry].stmts.push(Statement::Assign { dest: new_depth, value });
    assert!(analyze(f, input).is_none());
}


#[test]
fn separate_builds_can_collide_so_numeric_ids_are_not_a_product_certificate() {
    let source = "g = (x * 2 for x in range(4))\n";
    let (_, first) = super::generator_sum_plan::high_with_sources(source);
    let (second_program, second) = super::generator_sum_plan::high_with_sources(source);
    assert_eq!(first.comprehension_inputs, second.comprehension_inputs);
    let (function, input) = first.comprehension_inputs[0];
    // 已知证明缺口的反证：数值匹配不会拒绝跨构建组合；因此该API必须保持test-only。
    assert!(analyze_source(&second_program, &first, function, input).is_some());
}
