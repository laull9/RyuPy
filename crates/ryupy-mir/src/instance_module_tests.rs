//! 实例模块真实全局读写与隐藏state参数的结构回归。
use crate::*;

fn compile_instance(source: &str) -> (Program, Vec<ModuleExport>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("instance.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let info = ryupy_hir::ModuleInfo {
        instance_globals: true,
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty());
    let (program, exports, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty());
    (program, exports)
}

#[test]
fn instance_main_uses_explicit_state_and_dictionary_calls() {
    let (program, exports) = compile_instance("x = 1\nx = x + 2\nprint(x)\n");
    assert!(exports.is_empty());
    let main = &program.functions[program.entry];
    assert_eq!(
        main.params,
        vec![main.module_state.expect("实例入口必须有state")]
    );
    assert!(
        program
            .globals
            .iter()
            .all(|(_, global)| matches!(global.storage, GlobalStorage::InstancePublic { .. }))
    );
    assert!(validate::validate(&program).is_ok());
    let dump = pretty::dump(&program);
    assert!(dump.contains("InstanceLoadName"), "{dump}");
    assert!(dump.contains("InstanceStoreName"), "{dump}");
    assert!(!main.body.blocks.iter().any(|(_, block)| {
        block
            .stmts
            .iter()
            .any(|stmt| matches!(stmt, Statement::SetGlobal { .. }))
    }));
}

#[test]
fn instance_dictionary_rejects_static_reads_and_wrong_state() {
    let (mut program, _) = compile_instance("x = 1\n");
    let global = program.globals.iter().next().unwrap().0;
    let main = &mut program.functions[program.entry];
    main.body.blocks[main.body.entry]
        .stmts
        .push(Statement::SetGlobal {
            dest: global,
            value: Operand::Const(Constant::Null),
        });
    assert!(
        validate::validate(&program)
            .unwrap_err()
            .iter()
            .any(|error| error.contains("静态GlobalId"))
    );
    let (mut program, _) = compile_instance("x = 1\n");
    let main = &mut program.functions[program.entry];
    main.body.blocks[main.body.entry]
        .stmts
        .push(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::InstanceStoreName),
            args: vec![
                Operand::Const(Constant::Null),
                Operand::Const(Constant::Str("x".into())),
                Operand::Const(Constant::Null),
            ],
        }));
    assert!(
        validate::validate(&program)
            .unwrap_err()
            .iter()
            .any(|error| error.contains("显式state"))
    );
}

#[test]
fn instance_pipeline_keeps_dictionary_calls_and_real_builtin_lookup() {
    let (mut program, _) = compile_instance(
        "x = None\nfor value in range(2):\n    x = len([value])\nraise RuntimeError('failed')\n",
    );
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    assert!(validate::validate(&program).is_ok());
    let dump = pretty::dump(&program);
    assert!(dump.contains("InstanceLoadName"));
    assert!(!dump.contains("CallBuiltin"));
}

#[test]
fn instance_import_guard_runs_at_each_import_consumption() {
    let (mut program, _) = compile_instance("import sys, math\nfrom math import sin, cos\n");
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    let main = &program.functions[program.entry];
    let guards = main
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter(|stmt| {
            matches!(
                stmt,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::InstanceImportGuard),
                    ..
                })
            )
        })
        .count();
    assert_eq!(guards, 3);
    assert!(validate::validate(&program).is_ok());
}

#[test]
fn instance_functions_capture_context_without_static_identity_slots() {
    let (program, _) = compile_instance(
        "x = 1\ndef outer(a=2):\n    z = a\n    def inner(b=3):\n        nonlocal z\n        z = z + b\n        return z + x\n    return inner\nf = outer()\ny = f()\n",
    );
    assert!(
        validate::validate(&program).is_ok(),
        "{:?}",
        validate::validate(&program)
    );
    for (id, function) in program.functions.iter() {
        if id == program.entry {
            continue;
        }
        assert!(function.function_global.is_none());
        assert!(function.defaults_global.is_none());
        assert_eq!(
            function.module_state_source,
            Some(InstanceStateSource::FunctionParameter)
        );
        assert_eq!(
            function.params[function.param_meta.len() + usize::from(function.env.is_some())],
            function.module_state.unwrap()
        );
    }
    let dump = pretty::dump(&program);
    assert!(dump.contains("InstanceCaptureDefinitionContext"), "{dump}");
    assert!(dump.contains("CellSet"), "{dump}");
    assert!(
        !program
            .globals
            .iter()
            .any(|(_, g)| g.name.starts_with("__fn")
                || g.name.starts_with("__defaults")
                || g.name.starts_with("__origin"))
    );
}

#[test]
fn instance_lambda_and_definition_order_keep_real_operand_context() {
    let (program, _) = compile_instance(
        "def decorate(f):\n    return f\n@decorate\ndef f(x=seed()):\n    return lambda y=x: y + other\n",
    );
    assert!(
        validate::validate(&program).is_ok(),
        "{:?}",
        validate::validate(&program)
    );
    let made: Vec<_> = program
        .functions
        .iter()
        .flat_map(|(_, f)| f.body.blocks.iter())
        .flat_map(|(_, b)| &b.stmts)
        .filter_map(|s| match s {
            Statement::Assign {
                value:
                    Rvalue::MakeFunction {
                        instance_context, ..
                    },
                ..
            } => Some(instance_context),
            _ => None,
        })
        .collect();
    assert_eq!(made.len(), 3);
    assert!(made.iter().all(|context| context.is_some()));
}

#[test]
fn instance_generator_and_genexpr_preserve_frame_owner() {
    let (program, _) = compile_instance(
        "x = 1\ndef gen(a):\n    def read():\n        return x + a\n    try:\n        yield read()\n        yield from (x + n for n in [1, 2])\n    finally:\n        finish(x)\ng = gen(3)\n",
    );
    assert!(
        validate::validate(&program).is_ok(),
        "{:?}",
        validate::validate(&program)
    );
    let resumed: Vec<_> = program
        .functions
        .iter()
        .filter(|(_, f)| {
            matches!(
                f.module_state_source,
                Some(InstanceStateSource::GeneratorFrame { .. })
            )
        })
        .collect();
    assert_eq!(resumed.len(), 2);
    for (_, function) in resumed {
        let Some(InstanceStateSource::GeneratorFrame { frame, slot }) =
            function.module_state_source
        else {
            unreachable!()
        };
        assert_eq!(function.params, [frame]);
        assert_eq!(
            slot,
            function.module_state.unwrap().into_raw().into_u32() as usize + 4
        );
    }
}

#[test]
fn instance_builtin_rebinding_is_real_source_and_capture_follows_defaults() {
    let (program, _) = compile_instance(
        "__builtins__ = first\ndef change():\n    global __builtins__\n    __builtins__ = second\n    return 1\ndef f(x=change()):\n    return token\n",
    );
    assert!(
        validate::validate(&program).is_ok(),
        "{:?}",
        validate::validate(&program)
    );
    let main = &program.functions[program.entry];
    let captures: Vec<_> = main
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter(|s| {
            matches!(
                s,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::InstanceCaptureDefinitionContext),
                        ..
                    },
                    ..
                }
            )
        })
        .collect();
    assert_eq!(captures.len(), 2);
}

#[test]
fn instance_classes_have_local_namespace_and_owned_methods_without_static_class() {
    let (program, _) = compile_instance(
        "value = 10\nclass Base:\n    pass\nclass Box(Base):\n    value = 70\n    def read(self, x=value):\n        return value + x\n    @staticmethod\n    def plain():\n        return value\n",
    );
    assert!(
        validate::validate(&program).is_ok(),
        "{:?}",
        validate::validate(&program)
    );
    assert!(
        !program
            .globals
            .iter()
            .any(|(_, g)| g.name.starts_with("__class_")
                || g.name.starts_with("__fn_")
                || g.name.starts_with("__defaults_"))
    );
    let dump = pretty::dump(&program);
    assert!(dump.contains("InstanceClassGuard"), "{dump}");
    assert!(dump.contains("InstanceMakeClass"), "{dump}");
    assert!(dump.contains("ClassNamespaceGet"), "{dump}");
}

#[test]
fn instance_owner_validation_rejects_missing_source_and_fake_capture() {
    let (mut program, _) = compile_instance("def f():\n    return 1\n");
    program.functions[program.entry].module_state_source = None;
    assert!(validate::validate(&program).is_err());
    let main = &mut program.functions[program.entry];
    main.module_state_source = Some(InstanceStateSource::ModuleEntryParameter);
    for (_, block) in main.body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            if let Statement::Assign {
                value:
                    Rvalue::MakeFunction {
                        instance_context, ..
                    },
                ..
            } = statement
            {
                *instance_context = main.module_state.map(Operand::Local);
            }
        }
    }
    assert!(validate::validate(&program).is_err());
}

#[test]
fn instance_review_rejects_generator_helper_before_restore() {
    let (mut program, _) = compile_instance("def g():\n    yield outside\n");
    let resume = program
        .functions
        .iter()
        .find_map(|(id, f)| {
            matches!(
                f.module_state_source,
                Some(InstanceStateSource::GeneratorFrame { .. })
            )
            .then_some(id)
        })
        .unwrap();
    let function = &mut program.functions[resume];
    let helper = function
        .body
        .blocks
        .iter_mut()
        .find_map(|(_, block)| {
            let i = block.stmts.iter().position(|s| {
                matches!(
                    s,
                    Statement::Assign {
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::InstanceLoadName),
                            ..
                        },
                        ..
                    }
                )
            })?;
            Some(block.stmts.remove(i))
        })
        .unwrap();
    function.body.blocks[function.body.entry]
        .stmts
        .insert(0, helper);
    assert!(
        validate::validate(&program).is_err(),
        "恢复之前读取实例名称必须拒绝"
    );
}

#[test]
fn instance_review_rejects_generator_frame_parameter_overwrite() {
    let (mut program, _) = compile_instance("def g():\n    yield outside\n");
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
    let frame = function.params[0];
    function.body.blocks[function.body.entry].stmts.insert(
        0,
        Statement::Assign {
            dest: frame,
            value: Rvalue::Use(Operand::Const(Constant::Null)),
        },
    );
    assert!(
        validate::validate(&program).is_err(),
        "恢复frame参数不可覆写"
    );
}

#[test]
fn instance_review_rejects_resume_as_owned_function_even_with_signature() {
    let (mut program, _) = compile_instance("def g():\n    yield outside\n");
    let resume = program
        .functions
        .iter()
        .find_map(|(id, f)| {
            matches!(
                f.module_state_source,
                Some(InstanceStateSource::GeneratorFrame { .. })
            )
            .then_some(id)
        })
        .unwrap();
    let signature = program.globals.alloc(GlobalDecl {
        storage: GlobalStorage::LegacyStatic,
        name: "fake_signature".into(),
        ty: MirType::Dynamic,
    });
    program.functions[resume].signature_global = Some(signature);
    for (_, block) in program.functions[program.entry].body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            if let Statement::Assign {
                value: Rvalue::MakeFunction { func, .. },
                ..
            } = statement
            {
                *func = resume;
            }
        }
    }
    assert!(
        validate::validate(&program).is_err(),
        "不能给resume伪造signature后当owned thunk创建"
    );
}

#[test]
fn instance_review_rejects_direct_core_call_without_captured_owner() {
    let (mut program, _) = compile_instance("def f():\n    return outside\n");
    let target = program
        .functions
        .iter()
        .find_map(|(id, f)| {
            (f.module_state_source == Some(InstanceStateSource::FunctionParameter)).then_some(id)
        })
        .unwrap();
    let arity = program.functions[target].params.len();
    let main = &mut program.functions[program.entry];
    let dest = main.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    main.body.blocks[main.body.entry]
        .stmts
        .push(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Fn(target),
                args: vec![Operand::Const(Constant::Null); arity],
            },
        });
    assert!(
        validate::validate(&program).is_err(),
        "实例core必须经携带owner的函数对象调用"
    );
}

#[path = "instance_owner_review_tests.rs"]
mod owner_review;

#[test]
fn aot_parameter_closed_lambdas_keep_owner_without_environment() {
    let (program, _) = compile_instance(
        "def make():\n    return lambda value: value.encode()\ndef zero():\n    return lambda: 1\ndef many():\n    return lambda f, x, /, y, *, z: f(x, y, z=z)\n",
    );
    assert!(validate::validate(&program).is_ok());
    let made: Vec<_> = program
        .functions
        .iter()
        .flat_map(|(_, f)| f.body.blocks.iter())
        .flat_map(|(_, b)| &b.stmts)
        .filter_map(|s| match s {
            Statement::Assign {
                value:
                    Rvalue::MakeFunction {
                        func,
                        env,
                        defaults,
                        instance_context,
                    },
                ..
            } => Some((func, env, defaults, instance_context)),
            _ => None,
        })
        .collect();
    assert_eq!(made.len(), 6);
    for (func, env, defaults, context) in made {
        assert!(env.is_empty());
        assert!(context.is_some());
        assert!(
            defaults
                .iter()
                .all(|v| matches!(v, Operand::Const(Constant::Null)))
        );
        let function = &program.functions[*func];
        assert_eq!(
            function.module_state_source,
            Some(InstanceStateSource::FunctionParameter)
        );
        assert!(function.function_global.is_none());
        assert!(function.defaults_global.is_none());
    }
}
