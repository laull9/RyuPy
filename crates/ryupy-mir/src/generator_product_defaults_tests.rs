//! 从真实无默认参数def冷发布链做集中变异；直接调用新合同，避免别的validator提前拒绝冒充覆盖。
use crate::generator_product_defaults::Defaults;
use crate::generator_product_tests::{Observer, frontend};
use crate::*;

#[path = "generator_product_defaults_fixture.rs"]
mod fixture;
use fixture::{Fixture, SOURCE};

#[test]
fn actual_null_slots_and_local_aliases_preserve_python_arity_not_owner() {
    let mut f = Fixture::new();
    let target = &f.program.functions[f.target];
    assert!(target.dynamic_origin.is_some());
    assert_eq!(target.param_meta.len(), 2);
    assert_eq!(target.params.len(), 3);
    for boxed in [false, true] {
        let alias = f.local();
        let input = f.receiver;
        let at = f.getter;
        f.stmts().insert(
            at,
            Statement::Assign {
                dest: alias,
                value: if boxed {
                    Rvalue::Box {
                        value: Operand::Local(input),
                        from: MirType::Dynamic,
                    }
                } else {
                    Rvalue::Use(Operand::Local(input))
                },
            },
        );
        f.getter += 1;
        f.publish += 1;
        let at = f.getter;
        let Statement::Assign {
            value: Rvalue::Call { args, .. },
            ..
        } = &mut f.stmts()[at]
        else {
            panic!("getter");
        };
        args[0] = Operand::Local(alias);
        f.receiver = alias;
        validate::validate_high(&f.program, &[f.target]).unwrap();
        Defaults::inspect(&f.program, &f.exports).unwrap();
    }
    for boxed in [false, true] {
        let alias = f.local();
        let input = f.result;
        let at = f.publish;
        f.stmts().insert(
            at,
            Statement::Assign {
                dest: alias,
                value: if boxed {
                    Rvalue::Box {
                        value: Operand::Local(input),
                        from: MirType::Dynamic,
                    }
                } else {
                    Rvalue::Use(Operand::Local(input))
                },
            },
        );
        f.publish += 1;
        let at = f.publish;
        let Statement::SetGlobal { value, .. } = &mut f.stmts()[at] else {
            panic!("publish");
        };
        *value = Operand::Local(alias);
        f.result = alias;
        validate::validate_high(&f.program, &[f.target]).unwrap();
        Defaults::inspect(&f.program, &f.exports).unwrap();
    }
}

#[test]
fn receiver_requires_real_same_block_instance_not_labels_null_or_overwrite() {
    for value in [Constant::Null, Constant::None, Constant::I64(1)] {
        let mut f = Fixture::new();
        let at = f.getter;
        let Statement::Assign {
            value: Rvalue::Call { args, .. },
            ..
        } = &mut f.stmts()[at]
        else {
            panic!("getter");
        };
        args[0] = Operand::Const(value);
        f.reject("receiver不是同块真实MakeFunction局部");
    }
    for mutation in 0..5 {
        let mut f = Fixture::new();
        let receiver = f.receiver;
        let at = f.producer;
        match mutation {
            0 => {
                let global = f.program.functions[f.target].function_global.unwrap();
                let Statement::Assign { value, .. } = &mut f.stmts()[at] else {
                    panic!("producer");
                };
                *value = Rvalue::Use(Operand::Global(global));
                f.reject("receiver不是同块真实MakeFunction局部");
            }
            1 => {
                let at = f.getter;
                f.stmts().insert(
                    at,
                    Statement::Assign {
                        dest: receiver,
                        value: Rvalue::Use(Operand::Const(Constant::None)),
                    },
                );
                f.reject("局部缺少唯一实际定义或被覆盖");
            }
            2 => {
                let statement = f.stmts().remove(at);
                let target = f.block;
                f.program.functions[f.program.entry]
                    .body
                    .blocks
                    .alloc(BasicBlock {
                        stmts: vec![statement],
                        terminator: Terminator::Goto(target),
                    });
                f.reject("receiver定义跨块或晚于读取");
            }
            3 => {
                f.program.functions[f.program.entry].params.push(receiver);
                f.reject("局部不是独立Dynamic定义");
            }
            _ => {
                let statement = f.stmts()[at].clone();
                let at = f.getter;
                f.stmts().insert(at, statement);
                f.reject("局部缺少唯一实际定义或被覆盖");
            }
        }
    }
    let mut f = Fixture::new();
    let at = f.getter;
    let getter = f.stmts()[at].clone();
    let entry = f.program.functions[f.target].body.entry;
    f.program.functions[f.target].body.blocks[entry]
        .stmts
        .push(getter);
    f.reject("getter不在模块def冷发布入口");
}

#[test]
fn construction_checks_actual_ids_metadata_null_slots_and_hidden_owner() {
    for mutation in 0..10 {
        let mut f = Fixture::new();
        match mutation {
            0 => f.program.functions[f.target].param_meta[0].has_default = true,
            1 => f.program.functions[f.target].param_meta[0].kind = ParamKind::VarPositional,
            2 => {
                f.program.functions[f.target].params.pop().unwrap();
            }
            _ => {
                let wrong = f.program.entry;
                let at = f.producer;
                let Statement::Assign {
                    value:
                        Rvalue::MakeFunction {
                            func,
                            instance_context,
                            env,
                            defaults,
                        },
                    ..
                } = &mut f.stmts()[at]
                else {
                    panic!("producer");
                };
                match mutation {
                    3 => {
                        defaults.pop();
                    }
                    4 => defaults.push(Operand::Const(Constant::Null)), // 错把hidden owner算进槽数。
                    5 => defaults[0] = Operand::Const(Constant::None),
                    6 => defaults[0] = Operand::Const(Constant::I64(8)),
                    7 => *instance_context = Some(Operand::Const(Constant::Null)),
                    8 => {
                        *func = FnId::from_raw(999_999.into());
                        env.clear();
                    }
                    _ => *func = wrong,
                }
            }
        }
        f.reject(if mutation == 8 {
            "构造FnId不存在"
        } else {
            "构造不是精确Python参数数"
        });
    }
    let mut f = Fixture::new();
    let at = f.producer;
    let Statement::Assign {
        value: Rvalue::MakeFunction { env, .. },
        ..
    } = &mut f.stmts()[at]
    else {
        panic!("producer");
    };
    env.push(Operand::Const(Constant::Null));
    f.reject("构造不是精确Python参数数");
}

#[test]
fn cold_chain_rejects_unknown_steps_and_wrong_module_receiver() {
    let mut f = Fixture::new();
    let at = f.getter;
    f.stmts().insert(
        at,
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::TraceCheckDepth),
            args: vec![],
        }),
    );
    f.reject("producer与getter之间有未认证操作");
    let mut f = Fixture::new();
    let at = f.producer + 1;
    let Statement::Eval(Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::FunctionModule),
        args,
    }) = &mut f.stmts()[at]
    else {
        panic!("真实冷写");
    };
    args[0] = Operand::Const(Constant::None);
    f.reject("receiver不是同块真实MakeFunction局部");
    let mut f = Fixture::new();
    let mut receiver = f.receiver;
    for _ in 0..129 {
        let dest = f.local();
        let at = f.getter;
        f.stmts().insert(
            at,
            Statement::Assign {
                dest,
                value: Rvalue::Use(Operand::Local(receiver)),
            },
        );
        f.getter += 1;
        receiver = dest;
    }
    let at = f.getter;
    let Statement::Assign {
        value: Rvalue::Call { args, .. },
        ..
    } = &mut f.stmts()[at]
    else {
        panic!("getter");
    };
    args[0] = Operand::Local(receiver);
    f.reject("同块别名链超过128上界");
    let source = "def generate():\n    \"metadata stays outside\"\n    yield 'ready'\nit = generate()\nnext(it)\n";
    let (hir, types) = frontend(source);
    let error =
        generator_product::compile(&hir, &types, &passes::default_pipeline(), &mut Observer)
            .expect_err("FunctionMetadata仍不准入");
    assert!(
        error.contains("producer与getter之间有未认证操作"),
        "{error}"
    );
}

#[test]
fn private_slot_role_export_wrong_id_and_duplicate_publication_reject() {
    for mutation in 0..6 {
        let mut f = Fixture::new();
        let slot = f.program.functions[f.target].defaults_global.unwrap();
        match mutation {
            0 => f.program.functions[f.target].signature_global = Some(slot),
            1 => f.program.functions[f.program.entry].defaults_global = Some(slot),
            2 => f.exports[0].value = slot,
            3 => {
                let bad = GlobalId::from_raw(999_999.into());
                f.program.functions[f.target].defaults_global = Some(bad);
                let at = f.publish;
                let Statement::SetGlobal { dest, .. } = &mut f.stmts()[at] else {
                    panic!("publish");
                };
                *dest = bad;
            }
            4 => {
                let at = f.publish;
                let statement = f.stmts()[at].clone();
                f.stmts().insert(at + 1, statement);
            }
            _ => {
                let wrong = f.program.functions[f.target].function_global.unwrap();
                let at = f.publish;
                let Statement::SetGlobal { dest, .. } = &mut f.stmts()[at] else {
                    panic!("publish");
                };
                *dest = wrong;
            }
        }
        f.reject(match mutation {
            0..=3 => "私有槽角色冲突、公开导出或无效GlobalId",
            4 => "私有槽重复或错误发布",
            _ => "结果未沿同块私有发布链消费",
        });
    }
    let mut f = Fixture::new();
    let at = f.getter;
    let Statement::Assign { value, .. } = &mut f.stmts()[at] else {
        panic!("getter");
    };
    *value = Rvalue::Use(Operand::Const(Constant::Null));
    f.reject("私有槽存在未认证写入");
}

#[test]
fn opaque_defaults_reject_other_consumers_and_control_escape() {
    for mutation in 0..7 {
        let mut f = Fixture::new();
        let value = Operand::Local(f.result);
        let statement = match mutation {
            0 => Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ArraySet),
                args: vec![
                    value,
                    Operand::Const(Constant::I64(0)),
                    Operand::Const(Constant::None),
                ],
            }),
            1 => Statement::Eval(Rvalue::Call {
                callee: Callee::Fn(f.target),
                args: vec![value],
            }),
            2 => Statement::SetField {
                obj: Operand::Const(Constant::None),
                field: 0,
                value,
            },
            3 => Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::WriteObject),
                args: vec![Operand::Const(Constant::None), value],
            }),
            4 => {
                let dest = f.local();
                Statement::Assign {
                    dest,
                    value: Rvalue::Use(value),
                }
            }
            5 => {
                f.program.functions[f.program.entry].body.blocks[f.block].terminator =
                    Terminator::Return(Some(value));
                f.reject("不透明结果从控制边/return/yield外逃");
                continue;
            }
            _ => {
                f.program.functions[f.program.entry].body.blocks[f.block].terminator =
                    Terminator::Suspend {
                        value,
                        resume: f.block,
                        input: None,
                        delegated: false,
                    };
                f.reject("不透明结果从控制边/return/yield外逃");
                continue;
            }
        };
        let at = f.publish + 1;
        f.stmts().insert(at, statement);
        f.reject("不透明结果存在其它消费者或覆盖");
    }
    let f = Fixture::new();
    let defaults = Defaults::inspect(&f.program, &f.exports).unwrap();
    let bounds = crate::generator_product_values::Bounds {
        parameters: Default::default(),
        returns: Default::default(),
        yields: Default::default(),
        classes: Default::default(),
    };
    let resolver = crate::generator_product_values::Resolver {
        program: &f.program,
        bounds: &bounds,
        generators: &[],
        defaults: &defaults,
        properties: Default::default(),
    };
    let slot = f.program.functions[f.target].defaults_global.unwrap();
    let error = resolver
        .operand(f.program.entry, &Operand::Global(slot))
        .unwrap_err();
    assert!(error.contains("FunctionDefaults合同：私有槽不能作为高层值读取"));
    assert!(
        resolver
            .runtime(
                f.program.entry,
                Some(f.result),
                RuntimeFn::FunctionDefaults,
                &[vec![crate::generator_product_values::Value::Function(
                    f.target
                )]],
            )
            .is_err()
    );
    let statement = &f.program.functions[f.program.entry].body.blocks[f.block].stmts[f.getter];
    let Statement::Assign { value, .. } = statement else {
        panic!("getter");
    };
    assert!(
        resolver
            .inspect_value(f.program.entry, None, value)
            .unwrap_err()
            .contains("getter没有实际同块发布证明")
    );
}

#[test]
fn zero_multi_renamed_and_aliased_functions_link_but_defaults_stay_outside() {
    for source in [
        "def empty():\n    yield 'ready'\nit = empty()\nnext(it)\n",
        SOURCE,
        "def renamed(one, two):\n    yield one\nfactory = renamed\nit = factory('ready', 'other')\nnext(it)\n",
    ] {
        let linked = crate::generator_product_tests::linked(source);
        assert_eq!(generator_product::validate_emission(&linked), Ok(Some(1)));
    }
    for source in [
        "def generate(value=None):\n    yield value\nit = generate()\nnext(it)\n".to_owned(),
        SOURCE.replace("generate('ready', 'other')", "generate('ready')"),
        SOURCE.replace(
            "generate('ready', 'other')",
            "generate(value='ready', other='other')",
        ),
    ] {
        let (hir, types) = frontend(&source);
        assert!(
            generator_product::compile(&hir, &types, &passes::default_pipeline(), &mut Observer)
                .is_err()
        );
    }
}
