//! 默认值可经任意别名修改：省略参数必须交运行时绑定，完整实参仍保留原直调。
use crate::*;

fn compile(source: &str) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("defaults.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
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
fn omitted_defaults_use_binder_and_explicit_arguments_keep_direct_calls() {
    for (definition, omitted, complete) in [
        ("def value(a=1):\n    return a\n", "value()", "value(2)"),
        (
            "def value(a: int = 1) -> int:\n    return a\n",
            "value()",
            "value(2)",
        ),
        (
            "def value(a, *, b=2):\n    return a + b\n",
            "value(1)",
            "value(1, b=3)",
        ),
        (
            "def value(a=1, *args, **kwargs):\n    return a\n",
            "value()",
            "value(2)",
        ),
    ] {
        assert_eq!(direct_calls(&format!("{definition}{omitted}\n")), 0);
        assert_eq!(direct_calls(&format!("{definition}{complete}\n")), 1);
    }
}
