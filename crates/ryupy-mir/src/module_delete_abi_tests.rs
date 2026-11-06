//! 删除ABI的伪造IR负测：固定签名、void/结果消费、真实state来源与支配均不可绕过。
use crate::module_delete_tests::compile;
use crate::*;

fn arguments(program: &Program, runtime: RuntimeFn) -> Vec<Operand> {
    let first = match runtime {
        RuntimeFn::CheckModuleDelete => Operand::Const(Constant::Bool(true)),
        RuntimeFn::InstanceDeleteName => {
            Operand::Local(program.functions[program.entry].module_state.unwrap())
        }
        _ => Operand::Const(Constant::Null),
    };
    vec![first, Operand::Const(Constant::Str("x".into()))]
}

fn append(
    program: &mut Program,
    runtime: RuntimeFn,
    args: Vec<Operand>,
    assigned: Option<MirType>,
) {
    let main = &mut program.functions[program.entry];
    let call = Rvalue::Call {
        callee: Callee::Runtime(runtime),
        args,
    };
    let statement = if let Some(ty) = assigned {
        let dest = main.body.locals.alloc(LocalDecl {
            ty,
            name: None,
            is_cell: false,
        });
        Statement::Assign { dest, value: call }
    } else {
        Statement::Eval(call)
    };
    main.body.blocks[main.body.entry].stmts.push(statement);
}

#[test]
fn module_delete_fixed_abi_rejects_wrong_arguments_and_call_with_args() {
    for runtime in [
        RuntimeFn::CheckModuleDelete,
        RuntimeFn::CheckModuleDeleteValue,
        RuntimeFn::LoadDeletedModuleName,
        RuntimeFn::InstanceDeleteName,
    ] {
        for variant in 0..5 {
            let mut program = compile("pass\n", runtime == RuntimeFn::InstanceDeleteName);
            let mut args = arguments(&program, runtime);
            match variant {
                0 => args.clear(),
                1 => {
                    args.pop();
                }
                2 => args.push(Operand::Const(Constant::Null)),
                3 => args[0] = Operand::Const(Constant::I64(1)),
                _ => args[1] = Operand::Const(Constant::Null),
            }
            let assigned =
                (runtime == RuntimeFn::LoadDeletedModuleName).then_some(MirType::Dynamic);
            append(&mut program, runtime, args, assigned);
            assert!(
                validate::validate(&program).is_err(),
                "{runtime:?}, variant {variant}"
            );
        }
        let mut program = compile("pass\n", runtime == RuntimeFn::InstanceDeleteName);
        let prefix = arguments(&program, runtime);
        let main = &mut program.functions[program.entry];
        main.body.blocks[main.body.entry]
            .stmts
            .push(Statement::Eval(Rvalue::CallWithArgs {
                callee: runtime,
                prefix,
                args: vec![],
            }));
        let errors = validate::validate(&program).unwrap_err();
        assert!(
            errors.iter().any(|error| error.contains("固定Call ABI")),
            "{errors:?}"
        );
    }
}

#[test]
fn module_delete_void_and_value_return_contracts_are_enforced() {
    for runtime in [
        RuntimeFn::CheckModuleDelete,
        RuntimeFn::CheckModuleDeleteValue,
        RuntimeFn::InstanceDeleteName,
    ] {
        let mut program = compile("pass\n", runtime == RuntimeFn::InstanceDeleteName);
        let args = arguments(&program, runtime);
        append(&mut program, runtime, args, Some(MirType::Dynamic));
        assert!(
            validate::validate(&program)
                .unwrap_err()
                .iter()
                .any(|error| error.contains("返回void"))
        );
    }
    for assigned in [None, Some(MirType::I64)] {
        let mut program = compile("pass\n", false);
        let args = arguments(&program, RuntimeFn::LoadDeletedModuleName);
        append(
            &mut program,
            RuntimeFn::LoadDeletedModuleName,
            args,
            assigned,
        );
        assert!(validate::validate(&program).is_err());
    }
}

#[test]
fn instance_delete_rejects_forged_context_even_if_it_has_dynamic_type() {
    for constant in [true, false] {
        let mut program = compile("pass\n", true);
        let main = &mut program.functions[program.entry];
        let first = if constant {
            Operand::Const(Constant::Null)
        } else {
            let fake = main.body.locals.alloc(LocalDecl {
                ty: MirType::Dynamic,
                name: None,
                is_cell: false,
            });
            main.body.blocks[main.body.entry]
                .stmts
                .push(Statement::Assign {
                    dest: fake,
                    value: Rvalue::Use(Operand::Local(main.module_state.unwrap())),
                });
            Operand::Local(fake)
        };
        append(
            &mut program,
            RuntimeFn::InstanceDeleteName,
            vec![first, Operand::Const(Constant::Str("x".into()))],
            None,
        );
        assert!(
            validate::validate(&program)
                .unwrap_err()
                .iter()
                .any(|error| error.contains("显式state"))
        );
    }
}

#[test]
fn instance_delete_requires_capture_to_dominate_all_paths() {
    let mut program = compile("pass\n", true);
    let main = &mut program.functions[program.entry];
    let state = main.module_state.unwrap();
    let captured = main.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let join = main.body.blocks.alloc(BasicBlock {
        stmts: vec![Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::InstanceDeleteName),
            args: vec![
                Operand::Local(captured),
                Operand::Const(Constant::Str("x".into())),
            ],
        })],
        terminator: Terminator::Return(None),
    });
    let yes = main.body.blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: captured,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceCaptureDefinitionContext),
                args: vec![Operand::Local(state)],
            },
        }],
        terminator: Terminator::Goto(join),
    });
    let no = main.body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Goto(join),
    });
    main.body.blocks[main.body.entry].terminator = Terminator::Branch {
        cond: Operand::Const(Constant::Bool(true)),
        then_: yes,
        else_: no,
    };
    assert!(
        validate::validate(&program)
            .unwrap_err()
            .iter()
            .any(|error| error.contains("显式state"))
    );
}

#[test]
fn instance_delete_cannot_execute_before_generator_owner_restore() {
    let mut program = compile("def g():\n    global x\n    yield 1\n    del x\n", true);
    let function = program
        .functions
        .iter_mut()
        .find_map(|(_, f)| {
            matches!(
                f.module_state_source,
                Some(InstanceStateSource::GeneratorFrame { .. })
            )
            .then_some(f)
        })
        .unwrap();
    let helper = function
        .body
        .blocks
        .iter_mut()
        .find_map(|(_, block)| {
            let index = block.stmts.iter().position(|stmt| {
                matches!(
                    stmt,
                    Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::InstanceDeleteName),
                        ..
                    })
                )
            })?;
            Some(block.stmts.remove(index))
        })
        .unwrap();
    function.body.blocks[function.body.entry]
        .stmts
        .insert(0, helper);
    assert!(
        validate::validate(&program)
            .unwrap_err()
            .iter()
            .any(|error| error.contains("显式state"))
    );
}
