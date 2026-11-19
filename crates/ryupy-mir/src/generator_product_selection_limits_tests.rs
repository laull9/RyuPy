//! 同一高层基底的预算/组合反例；只检验已解析硬反证，不扩大超预算正向能力。
use super::*;

#[test]
fn unknown_definition_preserves_known_bad_token_through_alias_in_either_order() {
    for unknown_first in [false, true] {
        for bad in [false, true] {
            let mut high = High::baseline(B);
            let body = &mut high.program.functions[high.plan.functions()[0]].body;
            let token = body.locals.alloc(LocalDecl {
                ty: MirType::Dynamic,
                name: None,
                is_cell: false,
            });
            let alias = body.locals.alloc(LocalDecl {
                ty: MirType::Dynamic,
                name: None,
                is_cell: false,
            });
            let (block_id, at) = body
                .blocks
                .iter()
                .find_map(|(id, b)| {
                    b.stmts
                        .iter()
                        .position(|s| {
                            matches!(
                                s,
                                Statement::Eval(Rvalue::Call {
                                    callee: Callee::Runtime(
                                        RuntimeFn::TraceV1Match
                                            | RuntimeFn::TraceV1Abort
                                            | RuntimeFn::TraceV1AllMiss
                                    ),
                                    ..
                                })
                            )
                        })
                        .map(|at| (id, at))
                })
                .unwrap();
            let block = &mut body.blocks[block_id];
            let Statement::Eval(Rvalue::Call { args, .. }) = &mut block.stmts[at] else {
                panic!("token")
            };
            args[0] = Operand::Local(alias);
            let unknown = Statement::Assign {
                dest: token,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::GetItem),
                    args: vec![Operand::Const(Constant::None); 2],
                },
            };
            let known = Statement::Assign {
                dest: token,
                value: Rvalue::Use(Operand::Const(if bad {
                    Constant::Null
                } else {
                    Constant::None
                })),
            };
            let mut definitions = if unknown_first {
                vec![unknown, known]
            } else {
                vec![known, unknown]
            };
            definitions.push(Statement::Assign {
                dest: alias,
                value: Rvalue::Use(Operand::Local(token)),
            });
            block.stmts.splice(at..at, definitions);
            validate::validate_high(&high.program, high.plan.functions()).unwrap();
            if bad {
                token_invalid(high.check());
            } else {
                domain(high.check());
            }
        }
    }
}

#[test]
fn one_pending_defaults_chain_cannot_hide_another_proved_chain_alias_escape() {
    let source = with_definitions("def other():\n    return 1\n", false, "");
    for pending_first in [false, true] {
        for escape in [false, true] {
            let mut high = High::baseline(&source);
            let chains = roots::cold_chains(&high);
            assert_eq!(chains.len(), 2, "必须是真正两个不同普通def发布链");
            let pending = &chains[usize::from(!pending_first)];
            let proved = &chains[usize::from(pending_first)];
            let module = &mut high.program.functions[high.program.entry];
            module.body.blocks[pending.block].stmts.insert(
                pending.getter,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::Len),
                    args: vec![Operand::Const(Constant::None)],
                }),
            );
            if escape {
                let alias = module.body.locals.alloc(LocalDecl {
                    ty: MirType::Dynamic,
                    name: None,
                    is_cell: false,
                });
                module.body.blocks[proved.block]
                    .stmts
                    .push(Statement::Assign {
                        dest: alias,
                        value: Rvalue::Use(Operand::Local(proved.result)),
                    });
            }
            let checked =
                generator_product_defaults::Defaults::inspect(&high.program, &high.exports)
                    .map(|_| ());
            if escape {
                invalid_reason(checked, "不透明结果存在其它消费者或覆盖");
            } else {
                domain(checked);
            }
        }
    }
}

#[test]
fn thirty_two_and_thirty_three_known_token_values_never_hide_behind_budget() {
    for count in [32, 33] {
        for reverse in [false, true] {
            let mut high = High::baseline(B);
            let body = &mut high.program.functions[high.plan.functions()[0]].body;
            let token = body.locals.alloc(LocalDecl {
                ty: MirType::Dynamic,
                name: None,
                is_cell: false,
            });
            let (block_id, at) = body
                .blocks
                .iter()
                .find_map(|(id, b)| {
                    b.stmts
                        .iter()
                        .position(|s| {
                            matches!(
                                s,
                                Statement::Eval(Rvalue::Call {
                                    callee: Callee::Runtime(
                                        RuntimeFn::TraceV1Match
                                            | RuntimeFn::TraceV1Abort
                                            | RuntimeFn::TraceV1AllMiss
                                    ),
                                    ..
                                })
                            )
                        })
                        .map(|at| (id, at))
                })
                .unwrap();
            let block = &mut body.blocks[block_id];
            let Statement::Eval(Rvalue::Call { args, .. }) = &mut block.stmts[at] else {
                panic!("token")
            };
            args[0] = Operand::Local(token);
            let mut definitions: Vec<_> = (0..count)
                .map(|i| Statement::Assign {
                    dest: token,
                    value: Rvalue::Box {
                        value: Operand::Const(Constant::Str(format!("known-{i}"))),
                        from: MirType::Str,
                    },
                })
                .collect();
            if reverse {
                definitions.reverse();
            }
            block.stmts.splice(at..at, definitions);
            validate::validate_high(&high.program, high.plan.functions()).unwrap();
            token_invalid(high.check());
        }
    }
}

#[test]
fn actual_defaults_frontier_aliases_keep_eval_and_return_escape_hard() {
    for aliases in [129, 132] {
        for escape in 0..3 {
            let mut f = defaults_fixture::Fixture::new();
            let mut input = f.result;
            for _ in 0..aliases {
                let dest = f.local();
                let at = f.publish;
                f.stmts().insert(
                    at,
                    Statement::Assign {
                        dest,
                        value: Rvalue::Use(Operand::Local(input)),
                    },
                );
                f.publish += 1;
                input = dest;
            }
            let at = f.publish;
            let Statement::SetGlobal { value, .. } = &mut f.stmts()[at] else {
                panic!("publish")
            };
            *value = Operand::Local(input);
            match escape {
                1 => f
                    .stmts()
                    .push(Statement::Eval(Rvalue::Use(Operand::Local(input)))),
                2 => {
                    f.program.functions[f.program.entry].body.blocks[f.block].terminator =
                        Terminator::Return(Some(Operand::Local(input)))
                }
                _ => {}
            }
            let checked =
                generator_product_defaults::Defaults::inspect(&f.program, &f.exports).map(|_| ());
            if escape == 0 {
                domain(checked);
            } else {
                invalid_reason(
                    checked,
                    if escape == 1 {
                        "不透明结果存在其它消费者或覆盖"
                    } else {
                        "不透明结果从控制边/return/yield外逃"
                    },
                );
            }
        }
    }
}

#[test]
fn uncovered_summary_never_masks_other_token_or_missing_mapping() {
    for helper_first in [false, true] {
        let helper = "\ndef helper():\n    return next\n";
        let source = with_definitions(helper, helper_first, "");
        let mut high = High::new(&source);
        high.facts_ready();
        let hir_id = high
            .origins
            .functions
            .iter()
            .find(|(_, id)| high.program.functions[**id].name == "helper")
            .unwrap()
            .0;
        assert_eq!(
            high.facts.functions[hir_id].returns.atoms(),
            &[trace_domain::Atom::Next]
        );
        domain(high.check());
        let body = &mut high.program.functions[high.plan.functions()[0]].body;
        let args = body
            .blocks
            .iter_mut()
            .flat_map(|(_, b)| &mut b.stmts)
            .find_map(|s| match s {
                Statement::Eval(Rvalue::Call {
                    callee:
                        Callee::Runtime(
                            RuntimeFn::TraceV1Match
                            | RuntimeFn::TraceV1Abort
                            | RuntimeFn::TraceV1AllMiss,
                        ),
                    args,
                }) => Some(args),
                _ => None,
            })
            .unwrap();
        args[0] = Operand::Const(Constant::Null);
        token_invalid(high.check());
        let mut high = High::new(&source);
        high.facts_ready();
        let id = *high.origins.functions.keys().next().unwrap();
        high.origins.functions.remove(&id);
        invalid_reason(
            generator_product_values::Bounds::from_facts(&high.facts, &high.origins).map(|_| ()),
            "映射",
        );
    }
}

#[test]
fn long_initialization_chain_never_masks_fake_reset_or_extra_effect() {
    for first in [false, true] {
        for bad in 0..3 {
            let mut f = initialization_fixture::Fixture::new(true);
            let mut at = f.publish("__name__");
            let Statement::SetGlobal {
                value: mut input, ..
            } = f.stmts()[at].clone()
            else {
                panic!("name")
            };
            for _ in 1..129 {
                let dest = f.local();
                f.stmts().insert(
                    at,
                    Statement::Assign {
                        dest,
                        value: Rvalue::Use(input),
                    },
                );
                input = Operand::Local(dest);
                at += 1;
            }
            let Statement::SetGlobal { value, .. } = &mut f.stmts()[at] else {
                panic!("publish")
            };
            *value = input;
            let at = if first { 0 } else { f.publish("__spec__") };
            if bad == 1 {
                let slot = f.program.globals.alloc(GlobalDecl {
                    storage: GlobalStorage::LegacyStatic,
                    name: "fake_reset".into(),
                    ty: MirType::Dynamic,
                });
                f.stmts().insert(
                    at,
                    Statement::SetGlobal {
                        dest: slot,
                        value: Operand::Const(Constant::Null),
                    },
                );
            } else if bad == 2 {
                f.stmts().insert(
                    at,
                    Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::DynNone),
                        args: vec![],
                    }),
                );
            }
            let checked = f.initialization.inspect(&f.program, &f.exports);
            if bad == 0 {
                domain(checked);
            } else {
                invalid_reason(checked, "前言全部发布前存在未知effect或伪reset");
            }
        }
    }
}

#[test]
fn thirty_third_known_argument_contradiction_is_checked_before_storage_cap() {
    for kind in 0..4 {
        for reverse in [false, true] {
            for bad in [false, true] {
                let source = with_definitions(
                    "def passthrough(value):\n    return value\n",
                    true,
                    "passthrough(0)\npassthrough(None)",
                );
                let mut high = High::baseline(&source);
                let target = high
                    .program
                    .functions
                    .iter()
                    .find(|(_, f)| f.name == "passthrough")
                    .unwrap()
                    .0;
                assert_eq!(
                    high.program.functions[target].param_meta[0].ty,
                    MirType::Dynamic
                );
                let bounds =
                    generator_product_values::Bounds::from_facts(&high.facts, &high.origins)
                        .unwrap();
                let parameter = high.program.functions[target].params[0];
                let expected = bounds.parameters[&(target, parameter)].value().unwrap();
                assert_eq!(expected.len(), 2);
                assert!(
                    expected.contains(&generator_product_values::Value::Primitive(
                        trace_domain::Primitive::Integer
                    ))
                );
                assert!(expected.contains(&generator_product_values::Value::None));
                let receiver = high.program.functions[high.program.entry]
                    .body
                    .blocks
                    .iter()
                    .flat_map(|(_, b)| &b.stmts)
                    .find_map(|s| match s {
                        Statement::Assign {
                            dest,
                            value: Rvalue::MakeFunction { func, .. },
                        } if *func == target => Some(*dest),
                        _ => None,
                    })
                    .unwrap();
                let body = &mut high.program.functions[high.program.entry].body;
                let argument = body.locals.alloc(LocalDecl {
                    ty: MirType::Dynamic,
                    name: None,
                    is_cell: false,
                });
                let mut definitions: Vec<_> = (0..33)
                    .map(|i| {
                        let (value, from) = if i == 32 && bad {
                            (Constant::Str("bad".into()), MirType::Str)
                        } else {
                            (Constant::I64(i), MirType::I64)
                        };
                        Statement::Assign {
                            dest: argument,
                            value: Rvalue::Box {
                                value: Operand::Const(value),
                                from,
                            },
                        }
                    })
                    .collect();
                if reverse {
                    definitions.reverse();
                }
                let positional = if kind == 3 {
                    let array = body.locals.alloc(LocalDecl {
                        ty: MirType::Dynamic,
                        name: None,
                        is_cell: false,
                    });
                    definitions.push(Statement::Assign {
                        dest: array,
                        value: Rvalue::Use(Operand::Array(vec![Operand::Local(argument)])),
                    });
                    Operand::Local(array)
                } else {
                    Operand::Array(vec![Operand::Local(argument)])
                };
                let call = match kind {
                    0 => Rvalue::Call {
                        callee: Callee::Fn(target),
                        args: vec![Operand::Local(argument), Operand::Local(receiver)],
                    },
                    1 => Rvalue::DynCallValue {
                        callee: Operand::Local(receiver),
                        args: vec![Operand::Local(argument)],
                    },
                    _ => Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::CallValue),
                        args: vec![
                            Operand::Local(receiver),
                            positional,
                            Operand::Array(vec![]),
                            Operand::Array(vec![]),
                        ],
                    },
                };
                definitions.push(Statement::Eval(call));
                let block = &mut body.blocks[body.entry];
                let at = block.stmts.len() - 1;
                block.stmts.splice(at..at, definitions);
                validate::validate_high(&high.program, high.plan.functions()).unwrap();
                if bad {
                    invalid_reason(high.check(), "真实参数/env/owner不满足函数定义合同");
                } else {
                    domain(high.check());
                }
            }
        }
    }
}

#[test]
fn unknown_only_tokens_through_zero_one_or_many_aliases_stay_unproved() {
    for aliases in [0, 1, 3] {
        for reverse in [false, true] {
            let mut high = High::baseline(B);
            let body = &mut high.program.functions[high.plan.functions()[0]].body;
            let token = body.locals.alloc(LocalDecl {
                ty: MirType::Dynamic,
                name: None,
                is_cell: false,
            });
            let mut definitions = vec![Statement::Assign {
                dest: token,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::GetItem),
                    args: vec![Operand::Const(Constant::None); 2],
                },
            }];
            let mut input = token;
            for index in 0..aliases {
                let alias = body.locals.alloc(LocalDecl {
                    ty: MirType::Dynamic,
                    name: None,
                    is_cell: false,
                });
                let value = if index % 2 == 0 {
                    Rvalue::Use(Operand::Local(input))
                } else {
                    Rvalue::Box {
                        value: Operand::Local(input),
                        from: MirType::Dynamic,
                    }
                };
                definitions.push(Statement::Assign { dest: alias, value });
                input = alias;
            }
            if reverse {
                definitions.reverse();
            }
            let (block_id, at) = body
                .blocks
                .iter()
                .find_map(|(id, b)| {
                    b.stmts
                        .iter()
                        .position(|s| {
                            matches!(
                                s,
                                Statement::Eval(Rvalue::Call {
                                    callee: Callee::Runtime(
                                        RuntimeFn::TraceV1Match
                                            | RuntimeFn::TraceV1Abort
                                            | RuntimeFn::TraceV1AllMiss
                                    ),
                                    ..
                                })
                            )
                        })
                        .map(|at| (id, at))
                })
                .unwrap();
            let block = &mut body.blocks[block_id];
            let Statement::Eval(Rvalue::Call { args, .. }) = &mut block.stmts[at] else {
                panic!("token")
            };
            args[0] = Operand::Local(input);
            block.stmts.splice(at..at, definitions);
            validate::validate_high(&high.program, high.plan.functions()).unwrap();
            domain(high.check());
        }
    }
}
