//! delattr删除的只编译结构回归：builtin值、全部参数副作用与用户同名绑定，不计入应用数。
#[path = "../src/test_temp.rs"]
mod test_temp;
use ryupy_mir::{Callee, Constant, Function, Operand, RuntimeFn, Rvalue, Statement};

const SOURCE: &str = "
def saved():
    return delattr
def excess(callback):
    return delattr(callback(), callback(), callback(), callback())
def paused(target):
    return delattr(target, (yield 'name'))
def shadowed(delattr):
    return delattr(3, 'x')
";

fn values(function: &Function) -> Vec<&Rvalue> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter_map(|stmt| match stmt {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
        .collect()
}

#[test]
fn delattr_builtin_value_preserves_all_arguments_and_shadowing() {
    let root = test_temp::Root::new("delattr-structure");
    eprintln!("delattr structure evidence: {}", root.display());
    std::fs::write(root.join("structure.py"), SOURCE).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("structure.py", SOURCE);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(
        hir.functions
            .iter()
            .any(
                |(_, function)| function.body.exprs.iter().any(|(_, expression)| matches!(
                    expression.kind,
                    ryupy_hir::ExprKind::PreparedCall { .. }
                ))
            ),
        "yield参数必须标记预保存callee"
    );
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (mut program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline()).unwrap();
    std::fs::write(
        root.join("structure.mir"),
        ryupy_mir::pretty::dump(&program),
    )
    .unwrap();
    let function = |name: &str| {
        program
            .functions
            .iter()
            .find(|(_, function)| function.name == name)
            .unwrap()
            .1
    };
    let type_calls = |body: &Function| {
        values(body).into_iter().filter(|value| matches!(value,
        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::BuiltinFunction), args }
            if matches!(args.as_slice(), [Operand::Const(Constant::Str(name))] if name == "delattr")
    )).count()
    };
    assert_eq!(type_calls(function("saved")), 1);
    assert_eq!(type_calls(function("excess")), 0);
    assert_eq!(type_calls(function("shadowed")), 0);
    let excess = values(function("excess"));
    let calls: Vec<_> = excess
        .into_iter()
        .filter_map(|value| match value {
            Rvalue::DynCallValue { args, .. } => Some(args.len()),
            Rvalue::Call { callee: Callee::Runtime(RuntimeFn::DynBuiltin), args }
                if matches!(args.first(), Some(Operand::Const(Constant::Str(name))) if name == "delattr") => {
                    let Operand::Array(positional) = &args[1] else { panic!("删除实参数组必须保留") };
                    Some(positional.len())
                }
            _ => None,
        })
        .collect();
    assert_eq!(
        calls,
        [0, 0, 0, 0, 4],
        "四次回调必须在最终delattr绑定前完整保留"
    );
    assert_eq!(
        values(function("shadowed"))
            .into_iter()
            .filter(|value| matches!(value, Rvalue::DynCallValue { args, .. } if args.len() == 2))
            .count(),
        1
    );
}
