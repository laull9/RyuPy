//! 实际低层模板污染反例；逐次重建原B高层再lowering，不以生产者标签或旧反向证明器判定。
use crate::generator_product_snapshot::Source;
use crate::generator_product_structure::inspect;
use crate::generator_product_tests::{B, Observer, frontend};
use crate::*;
use rustc_hash::FxHashMap;

fn lowered() -> (Program, Source, FxHashMap<String, GlobalId>) {
    let (hir, types) = frontend(B);
    let mut plan = generator_product::DeferredGenerators::default();
    let (mut program, _, errors) = build::build_program_mode(&hir, &types, None, Some(&mut plan));
    assert!(errors.is_empty());
    let contract =
        crate::generator_product_trace::Contract::capture(&program, plan.functions()).unwrap();
    passes::run_pipeline_high(
        &mut program,
        &passes::default_pipeline(),
        &mut Observer,
        plan.functions(),
    )
    .unwrap();
    contract.verify(&program, plan.functions()).unwrap();
    let source = Source::capture(plan.functions()[0], &program.functions[plan.functions()[0]]);
    generators::lower_with_trace(&mut program, plan.functions(), &plan.exceptions, true);
    assert!(validate::validate(&program).is_ok());
    assert!(inspect(&program, &source, &plan.exceptions).is_ok());
    (program, source, plan.exceptions)
}

#[test]
fn actual_low_template_rejects_slot_pc_retry_handler_line_and_extra_source_mutations() {
    for mutation in 0..9 {
        let (mut program, source, exceptions) = lowered();
        let resume = inspect(&program, &source, &exceptions).unwrap().resume;
        let mut changed = false;
        for (_, block) in program.functions[resume].body.blocks.iter_mut() {
            for statement in &mut block.stmts {
                if changed {
                    break;
                }
                match statement {
                    Statement::Assign {
                        value:
                            Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::ArrayGet),
                                args,
                            },
                        ..
                    } if mutation == 0 => {
                        args[1] = Operand::Const(Constant::I64(999));
                        changed = true;
                    }
                    Statement::Assign {
                        value: Rvalue::Call { callee, .. },
                        ..
                    } if mutation == 1
                        && *callee == Callee::Runtime(RuntimeFn::TraceV1GeneratorDepth) =>
                    {
                        *callee = Callee::Runtime(RuntimeFn::DynNone);
                        changed = true;
                    }
                    Statement::Assign {
                        value:
                            Rvalue::Binary {
                                op: BinOp::Eq, rhs, ..
                            },
                        ..
                    } if mutation == 2 => {
                        *rhs = Operand::Const(Constant::I64(999));
                        changed = true;
                    }
                    Statement::Assign {
                        value: Rvalue::Call { callee, .. },
                        ..
                    } if mutation == 3 && *callee == Callee::Runtime(RuntimeFn::GeneratorRetry) => {
                        *callee = Callee::Runtime(RuntimeFn::DynNone);
                        changed = true;
                    }
                    Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ArraySet),
                        args,
                    }) if mutation == 4 && args[1] == Operand::Const(Constant::I64(3)) => {
                        args[1] = Operand::Const(Constant::I64(2));
                        changed = true;
                    }
                    Statement::Line(value) if mutation == 5 => {
                        *value += 1000;
                        changed = true;
                    }
                    _ => {}
                }
            }
        }
        match mutation {
            6 => {
                program.functions[resume].body.regions[0].try_len -= 1;
                changed = true;
            }
            7 => {
                let entry = program.functions[resume].body.entry;
                program.functions[resume].body.blocks[entry]
                    .stmts
                    .push(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::TraceV1Explicit),
                        args: vec![Operand::Const(Constant::None)],
                    }));
                changed = true;
            }
            8 => {
                program.functions[resume].ret = MirType::I64;
                changed = true;
            }
            _ => {}
        }
        assert!(changed, "mutation {mutation} 未命中实际结构");
        assert!(
            inspect(&program, &source, &exceptions).is_err(),
            "mutation {mutation} 被错误接受"
        );
    }
}

#[test]
fn actual_factory_owner_and_fresh_box_are_checked_not_just_slot_count() {
    for owner in [false, true] {
        let (mut program, source, exceptions) = lowered();
        let entry = program.functions[source.factory].body.entry;
        let block = &mut program.functions[source.factory].body.blocks[entry];
        if owner {
            let make = block
                .stmts
                .iter_mut()
                .find_map(|s| match s {
                    Statement::Assign {
                        value: Rvalue::MakeGenerator { origin, .. },
                        ..
                    } => Some(origin),
                    _ => None,
                })
                .unwrap();
            *make = Some(Operand::Const(Constant::None));
        } else {
            let first = block.stmts[0].clone();
            block.stmts.insert(1, first);
        }
        assert!(inspect(&program, &source, &exceptions).is_err());
    }
}

#[test]
fn template_box_cannot_overwrite_original_dynamic_parameter() {
    for in_resume in [false, true] {
        let (mut program, source, exceptions) = lowered();
        let target = source.params[0];
        assert_eq!(
            source.locals[target.into_raw().into_u32() as usize],
            MirType::Dynamic
        );
        let checked = inspect(&program, &source, &exceptions).unwrap();
        if !in_resume {
            let body = &mut program.functions[checked.factory].body;
            let block = &mut body.blocks[body.entry];
            let Statement::Assign {
                dest,
                value: Rvalue::Box { .. },
            } = &mut block.stmts[0]
            else {
                panic!("原B工厂缺少首个PC Box")
            };
            *dest = target;
            let frame = block
                .stmts
                .iter_mut()
                .find_map(|statement| match statement {
                    Statement::Assign {
                        value: Rvalue::MakeGenerator { frame, .. },
                        ..
                    } => Some(frame),
                    _ => None,
                })
                .unwrap();
            frame[0] = Operand::Local(target);
        } else {
            let mut changed = false;
            for (_, block) in program.functions[checked.resume].body.blocks.iter_mut() {
                if changed {
                    break;
                }
                for position in 0..block.stmts.len().saturating_sub(1) {
                    let (before, after) = block.stmts.split_at_mut(position + 1);
                    if let Statement::Assign {
                        dest,
                        value: Rvalue::Box { .. },
                    } = &mut before[position]
                        && dest.into_raw().into_u32() as usize > source.locals.len()
                        && let Statement::Eval(Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ArraySet),
                            args,
                        }) = &mut after[0]
                        && args[2] == Operand::Local(*dest)
                    {
                        *dest = target;
                        args[2] = Operand::Local(target);
                        changed = true;
                        break;
                    }
                }
            }
            assert!(changed, "原B恢复体缺少Box→实际slot写入");
        }
        // 普通类型/ID检查无法识别这个语义覆盖，必须由末端模板freshness拒绝。
        assert!(validate::validate(&program).is_ok());
        assert!(inspect(&program, &source, &exceptions).is_err());
    }
}

#[test]
fn high_contract_rejects_extra_push_and_pure_control_overwrite() {
    for pure in [false, true] {
        let (hir, types) = frontend(B);
        let mut plan = generator_product::DeferredGenerators::default();
        let (mut program, _, _) = build::build_program_mode(&hir, &types, None, Some(&mut plan));
        let contract =
            crate::generator_product_trace::Contract::capture(&program, plan.functions()).unwrap();
        let function = &mut program.functions[plan.functions()[0]];
        if pure {
            let condition = function
                .body
                .blocks
                .iter()
                .find_map(|(_, b)| match b.terminator {
                    Terminator::Branch {
                        cond: Operand::Local(local),
                        ..
                    } => Some(local),
                    _ => None,
                })
                .unwrap();
            function.body.blocks[function.body.entry]
                .stmts
                .push(Statement::Assign {
                    dest: condition,
                    value: Rvalue::Use(Operand::Const(Constant::Bool(false))),
                });
        } else {
            let push = function.body.blocks[function.body.entry]
                .stmts
                .iter()
                .find(|s| {
                    matches!(
                        s,
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
            function.body.blocks[function.body.entry].stmts.push(push);
        }
        assert!(contract.verify(&program, plan.functions()).is_err());
    }
}
