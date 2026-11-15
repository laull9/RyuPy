// range候选的原始源、来源错配、支配、暂停物化及trace边界测试；只随test构建。
use super::*;

#[test]
fn original_genexpr_has_a_single_cursor_candidate_not_a_product_proof() {
    let (program, deferred) = crate::generator_sum_plan::high_with_sources(include_str!(
        "../../../tests/benchmarks/generators.py"
    ));
    assert_eq!(deferred.comprehension_inputs.len(), 1);
    let (function, input) = deferred.comprehension_inputs[0];
    let plan = analyze_explained(&program.functions[function], input).unwrap_or_else(|reason| {
        let f = &program.functions[function];
        // 只在这个手写基准失败时输出有界高层IR到测试日志，绝不写golden或生成文件入库。
        let locals: Vec<_> = f.body.locals.iter().take(64).collect();
        let blocks: Vec<_> = f.body.blocks.iter().take(24).collect();
        panic!(
            "{reason}\nparams={:?} entry={:?}\nlocals={locals:#?}\nblocks={blocks:#?}",
            f.params, f.body.entry
        );
    });
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
        let (program, deferred) = crate::generator_sum_plan::high_with_sources(source);
        assert!(!deferred.comprehension_inputs.is_empty());
        for (function, input) in deferred.comprehension_inputs {
            assert!(analyze(&program.functions[function], input).is_none());
        }
    }
}

#[test]
fn single_static_definition_does_not_authorize_read_before_definition() {
    let (mut program, deferred) =
        crate::generator_sum_plan::high_with_sources("g = (x * 2 for x in range(4))\n");
    let (function, input) = deferred.comprehension_inputs[0];
    assert!(analyze(&program.functions[function], input).is_some());
    let f = &mut program.functions[function];
    for (_, block) in f.body.blocks.iter_mut() {
        if let Some(index) = block.stmts.iter().position(|s| {
            matches!(
                s,
                Statement::Assign {
                    value: Rvalue::DynBinary { .. },
                    ..
                }
            )
        }) {
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
        let (mut program, deferred) =
            crate::generator_sum_plan::high_with_sources("g = (x * 2 for x in range(4))\n");
        let (function, input) = deferred.comprehension_inputs[0];
        assert!(analyze(&program.functions[function], input).is_some());
        let mut changed = false;
        for (_, block) in program.functions[function].body.blocks.iter_mut() {
            for statement in &mut block.stmts {
                if let Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(runtime),
                    args,
                }) = statement
                    && *runtime == RuntimeFn::TracePopTo
                {
                    if bad {
                        *runtime = RuntimeFn::TraceCheckDepth;
                    } else {
                        *args = vec![Operand::Const(Constant::I64(0))];
                    }
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
    let (program, deferred) = crate::generator_sum_plan::high_with_sources(
        "def ordinary(x):\n    yield x\ng = (x * 2 for x in range(4))\n",
    );
    let (function, input) = deferred.comprehension_inputs[0];
    assert!(analyze_source(&program, &deferred, function, input).is_some());
    let wrong_function = *deferred
        .functions()
        .iter()
        .find(|id| **id != function)
        .unwrap();
    assert!(analyze_source(&program, &deferred, wrong_function, input).is_none());
    let bad_input = LocalId::from_raw(999999u32.into());
    assert!(analyze_source(&program, &deferred, function, bad_input).is_none());
    let bad_function = FnId::from_raw(999999u32.into());
    assert!(analyze_source(&program, &deferred, bad_function, input).is_none());
}

#[test]
fn alternate_path_definition_is_not_a_dominating_fact() {
    let (mut program, deferred) =
        crate::generator_sum_plan::high_with_sources("g = (x * 2 for x in range(4))\n");
    let (function, input) = deferred.comprehension_inputs[0];
    let f = &mut program.functions[function];
    let mut moved = None;
    for (_, block) in f.body.blocks.iter_mut() {
        if let Some(index) = block.stmts.iter().position(|statement| {
            matches!(
                statement,
                Statement::Assign {
                    value: Rvalue::Box {
                        value: Operand::Const(Constant::I64(2)),
                        ..
                    },
                    ..
                }
            )
        }) {
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
            cond: Operand::Const(Constant::Bool(true)),
            then_: defines,
            else_: entry,
        },
    });
    assert!(!dominates_all_reads(f));
    assert!(analyze(f, input).is_none());
}

#[test]
fn shared_resume_does_not_get_a_second_yield_edge() {
    let (mut program, deferred) =
        crate::generator_sum_plan::high_with_sources("g = (x * 2 for x in range(4))\n");
    let (function, input) = deferred.comprehension_inputs[0];
    let f = &mut program.functions[function];
    let edge = f
        .body
        .blocks
        .iter()
        .find_map(|(_, block)| {
            matches!(block.terminator, Terminator::Suspend { .. }).then(|| block.terminator.clone())
        })
        .unwrap();
    f.body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: edge,
    });
    assert!(analyze(f, input).is_none());
}

#[test]
fn loop_first_read_cannot_use_a_definition_after_suspend() {
    let (mut program, deferred) =
        crate::generator_sum_plan::high_with_sources("g = (x * 2 for x in range(4))\n");
    let (function, input) = deferred.comprehension_inputs[0];
    let f = &mut program.functions[function];
    let plan = analyze(f, input).unwrap();
    let mut moved = None;
    for (_, block) in f.body.blocks.iter_mut() {
        if let Some(index) = block.stmts.iter().position(|statement| {
            matches!(
                statement,
                Statement::Assign {
                    value: Rvalue::Box {
                        value: Operand::Const(Constant::I64(2)),
                        ..
                    },
                    ..
                }
            )
        }) {
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
    let (program, deferred) =
        crate::generator_sum_plan::high_with_sources("g = (x * 2 for x in range(4))\n");
    let (function, input) = deferred.comprehension_inputs[0];
    let plan = analyze_source(&program, &deferred, function, input).unwrap();
    assert!(resident_set_requirements_hold(&plan, &plan.residents));
    let mut missing = plan.residents.clone();
    missing.remove(&plan.iterator);
    assert!(!resident_set_requirements_hold(&plan, &missing));
}

#[test]
fn second_frame_entry_cannot_masquerade_as_matching_trace_depth() {
    let (mut program, deferred) =
        crate::generator_sum_plan::high_with_sources("g = (x * 2 for x in range(4))\n");
    let (function, input) = deferred.comprehension_inputs[0];
    let f = &mut program.functions[function];
    let duplicate = f.body.blocks[f.body.entry]
        .stmts
        .iter()
        .find(|statement| {
            matches!(
                statement,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(
                            RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin
                        ),
                        ..
                    },
                    ..
                }
            )
        })
        .unwrap()
        .clone();
    let new_depth = crate::generators::local(&mut f.body.locals, MirType::I64);
    let Statement::Assign { value, .. } = duplicate else {
        unreachable!()
    };
    f.body.blocks[f.body.entry].stmts.push(Statement::Assign {
        dest: new_depth,
        value,
    });
    assert!(analyze(f, input).is_none());
}

#[test]
fn separate_builds_can_collide_so_numeric_ids_are_not_a_product_certificate() {
    let source = "g = (x * 2 for x in range(4))\n";
    let (_, first) = crate::generator_sum_plan::high_with_sources(source);
    let (second_program, second) = crate::generator_sum_plan::high_with_sources(source);
    assert_eq!(first.comprehension_inputs, second.comprehension_inputs);
    let (function, input) = first.comprehension_inputs[0];
    // 已知证明缺口的反证：数值匹配不会拒绝跨构建组合；因此该API必须保持test-only。
    assert!(analyze_source(&second_program, &first, function, input).is_some());
}

#[test]
fn dead_none_tail_does_not_define_reachable_values() {
    let (program, deferred) =
        crate::generator_sum_plan::high_with_sources("g = (x * 2 for x in range(4))\n");
    let (function, input) = deferred.comprehension_inputs[0];
    let f = &program.functions[function];
    let reachable = reachable_blocks(f).unwrap();
    let dead = f.body.blocks.iter().find(|(id, block)| {
        !reachable.contains(id)
            && block.stmts.iter().any(|statement| {
                matches!(
                    statement,
                    Statement::Assign {
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::DynNone),
                            ..
                        },
                        ..
                    }
                )
            })
    });
    assert!(dead.is_some(), "真实源必须包含导致误拒的无前驱尾块");
    assert!(analyze(f, input).is_some());
}

#[test]
fn connecting_dead_tail_through_resume_restores_duplicate_rejection() {
    let (mut program, deferred) =
        crate::generator_sum_plan::high_with_sources("g = (x * 2 for x in range(4))\n");
    let (function, input) = deferred.comprehension_inputs[0];
    let f = &mut program.functions[function];
    let plan = analyze(f, input).unwrap();
    let reachable = reachable_blocks(f).unwrap();
    let dead = f
        .body
        .blocks
        .iter()
        .find(|(id, _)| !reachable.contains(id))
        .unwrap()
        .0;
    f.body.blocks[plan.resume].terminator = Terminator::Goto(dead);
    let reason = analyze_explained(f, input).unwrap_err();
    assert!(reason.contains("静态定义重复"), "{reason}");
}

#[test]
fn reachable_invalid_edge_or_unresolved_terminator_is_rejected() {
    for invalid_edge in [false, true] {
        let (mut program, deferred) =
            crate::generator_sum_plan::high_with_sources("g = (x * 2 for x in range(4))\n");
        let (function, input) = deferred.comprehension_inputs[0];
        let f = &mut program.functions[function];
        let plan = analyze(f, input).unwrap();
        f.body.blocks[plan.resume].terminator = if invalid_edge {
            Terminator::Goto(BlockId::from_raw(999999u32.into()))
        } else {
            Terminator::Unreachable
        };
        assert!(analyze(f, input).is_none());
    }
}
