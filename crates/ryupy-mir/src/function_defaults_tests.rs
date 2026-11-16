//! 默认值缺省调用由实参后的身份/冷状态守卫保护；未准入形状和失败臂保实时绑定。
use crate::*;

fn compile(source: &str) -> Program {
    compile_mode(source, false)
}

fn compile_mode(source: &str, instance: bool) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("defaults.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            instance_globals: instance,
            ..Default::default()
        },
        source,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    validate::validate(&program).unwrap();
    program
}

fn direct_calls(source: &str) -> usize {
    let program = compile(source);
    let target = program
        .functions
        .iter()
        .find(|(_, function)| function.name == "value")
        .unwrap()
        .0;
    program.functions[program.entry]
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter(|statement| {
            matches!(statement,
            Statement::Assign { value: Rvalue::Call { callee: Callee::Fn(found), .. }, .. }
            | Statement::Eval(Rvalue::Call { callee: Callee::Fn(found), .. }) if *found == target)
        })
        .count()
}

#[test]
fn omitted_defaults_have_guarded_direct_calls_and_binder_fallbacks() {
    for (definition, omitted, complete, guarded) in [
        (
            "def value(a=1):\n    return a\n",
            "value()",
            "value(2)",
            true,
        ),
        (
            "def value(a: int = 1) -> int:\n    return a\n",
            "value()",
            "value(2)",
            true,
        ),
        (
            "def value(a, *, b=2):\n    return a + b\n",
            "value(1)",
            "value(1, b=3)",
            false,
        ),
        (
            "def value(a=1, *args, **kwargs):\n    return a\n",
            "value()",
            "value(2)",
            false,
        ),
    ] {
        assert_eq!(
            direct_calls(&format!("{definition}{omitted}\n")),
            usize::from(guarded)
        );
        let program = compile(&format!("{definition}{omitted}\n"));
        let main = &program.functions[program.entry];
        let values = main
            .body
            .blocks
            .iter()
            .flat_map(|(_, b)| &b.stmts)
            .filter_map(|s| match s {
                Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
                _ => None,
            })
            .collect::<Vec<_>>();
        assert_eq!(
            values
                .iter()
                .filter(|v| matches!(
                    v,
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::FunctionDefaultsUnobserved),
                        ..
                    }
                ))
                .count(),
            usize::from(guarded)
        );
        assert!(values.iter().any(|v| matches!(
            v,
            Rvalue::DynCallValue { .. }
                | Rvalue::CallWithArgs {
                    callee: RuntimeFn::CallValue,
                    ..
                }
        )));
        assert_eq!(direct_calls(&format!("{definition}{complete}\n")), 1);
    }
}

#[test]
fn nested_env_instance_and_wide_shapes_keep_runtime_binding() {
    for (source, instance) in [
        (
            "def factory(value):\n    def nested(item=[]):\n        return value, item\n    return nested\nf = factory(1)\nf()\n",
            false,
        ),
        ("def value(a=1):\n    return a\nvalue()\n", true),
        (
            "def value(a=1,b=2,c=3,d=4):\n    return a\nvalue()\n",
            false,
        ),
    ] {
        let program = compile_mode(source, instance);
        for (_, function) in program.functions.iter() {
            assert!(
                !function
                    .body
                    .blocks
                    .iter()
                    .flat_map(|(_, b)| &b.stmts)
                    .any(|statement| matches!(
                        statement,
                        Statement::Assign {
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::FunctionDefaultsUnobserved),
                                ..
                            },
                            ..
                        }
                    ))
            );
        }
    }
}
