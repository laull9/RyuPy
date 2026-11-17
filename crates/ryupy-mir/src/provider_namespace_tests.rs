//! 新provider命名空间的竖切结构门：旧资格持续拒绝，急切推导式继承frame，注解前言走真实D。
use crate::*;
fn compile(source: &str, profile: bool) -> Result<Program, Vec<ryupy_base::Diagnostic>> {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("namespace.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let info = ryupy_hir::ModuleInfo {
        instance_globals: true,
        instance_capabilities: if profile {
            ryupy_hir::provider_namespace_capabilities()
        } else {
            Default::default()
        },
        name: "namespace".into(),
        file: "namespace.py".into(),
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    if !diagnostics.is_empty() {
        return Err(diagnostics);
    }
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    validate::validate(&program).unwrap();
    Ok(program)
}
#[test]
fn eager_comprehensions_keep_caller_context_but_defs_capture_definition_context() {
    let source = include_str!("../../../tests/cases/stock_module_namespaces/eager/growth.py");
    assert!(compile(source, false).is_err());
    let program = compile(source, true).unwrap();
    let mut comprehensions = 0;
    let mut definitions = 0;
    for (_, function) in program.functions.iter() {
        for statement in function
            .body
            .blocks
            .iter()
            .flat_map(|(_, block)| &block.stmts)
        {
            match statement {
                Statement::Assign {
                    value:
                        Rvalue::MakeFunction {
                            func,
                            instance_context,
                            ..
                        },
                    ..
                } if program.functions[*func].is_comprehension => {
                    comprehensions += 1;
                    assert_eq!(instance_context, &function.module_state.map(Operand::Local));
                }
                Statement::Assign {
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::InstanceCaptureDefinitionContext),
                            ..
                        },
                    ..
                } => definitions += 1,
                _ => {}
            }
        }
    }
    assert!(
        comprehensions >= 6 && definitions >= 5,
        "{comprehensions}/{definitions}"
    );
}
#[test]
fn annotation_setup_precedes_source_and_retains_runtime_mapping_writes() {
    let source = include_str!("../../../tests/cases/stock_module_namespaces/annotations/notes.py");
    assert!(compile(source, false).is_err());
    let program = compile(source, true).unwrap();
    let main = &program.functions[program.entry];
    let calls: Vec<_> = main
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter_map(|stmt| match stmt {
            Statement::Assign {
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(function),
                        ..
                    },
                ..
            }
            | Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(function),
                ..
            }) => Some(*function),
            _ => None,
        })
        .collect();
    assert_eq!(
        calls
            .iter()
            .filter(|function| **function == RuntimeFn::InstanceSetupAnnotations)
            .count(),
        1
    );
    assert!(
        calls
            .iter()
            .position(|function| *function == RuntimeFn::InstanceSetupAnnotations)
            .unwrap()
            < calls
                .iter()
                .position(|function| *function == RuntimeFn::ImportModule)
                .unwrap()
    );
    assert!(calls.contains(&RuntimeFn::SetItem));
}

#[test]
fn inherited_context_is_exclusive_to_trusted_eager_functions() {
    let source = "def ordinary(value):\n    return value\nitems=[item for item in [1,2]]\n";
    for eager in [true, false] {
        let mut program = compile(source, true).unwrap();
        let targets: Vec<_> = program
            .functions
            .iter()
            .filter_map(|(id, function)| function.is_comprehension.then_some(id))
            .collect();
        let mut changed = false;
        for (_, function) in program.functions.iter_mut() {
            let state = function.module_state;
            for statement in function
                .body
                .blocks
                .iter_mut()
                .flat_map(|(_, block)| &mut block.stmts)
            {
                if let Statement::Assign {
                    value:
                        Rvalue::MakeFunction {
                            func,
                            instance_context,
                            ..
                        },
                    ..
                } = statement
                    && targets.contains(func) == eager
                {
                    *instance_context = Some(if eager {
                        Operand::Const(Constant::Null)
                    } else {
                        Operand::Local(state.unwrap())
                    });
                    changed = true;
                }
            }
        }
        assert!(changed);
        assert!(
            validate::validate(&program).is_err(),
            "不能把普通def环境改成parent frame或丢失comp context"
        );
    }
}

#[test]
fn import_attributes_use_the_existing_value_protocol_only_in_the_new_profile() {
    let source = "import builtins\nsaved=builtins.__import__\nbuiltins.__import__=saved\n";
    assert!(compile(source, false).is_err());
    let program = compile(source, true).unwrap();
    let statements: Vec<_> = program.functions[program.entry]
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .collect();
    assert!(statements.iter().any(|stmt| matches!(
        stmt,
        Statement::Assign {
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::DynGetAttr),
                ..
            },
            ..
        }
    )));
    assert!(statements.iter().any(|stmt| matches!(
        stmt,
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::SetAttr),
            ..
        })
    )));
}
