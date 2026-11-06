//! slice构造的只编译结构回归：类型值、全部参数副作用与用户同名绑定，不计入应用数。
use ryupy_mir::{Callee, Constant, Function, Operand, RuntimeFn, Rvalue, Statement};

const SOURCE: &str = "
def saved():
    return slice
def excess(callback):
    return slice(callback(), callback(), callback(), callback())
def shadowed(slice):
    return slice(3)
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
fn slice_type_value_preserves_all_arguments_and_shadowing() {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = std::env::temp_dir().join(format!(
        "ryupy-slice-structure-{}-{stamp}",
        std::process::id()
    ));
    std::fs::create_dir(&root).unwrap();
    eprintln!("slice structure evidence: {}", root.display());
    std::fs::write(root.join("structure.py"), SOURCE).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("structure.py", SOURCE);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
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
        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::TypeObject), args }
            if matches!(args.as_slice(), [Operand::Const(Constant::Str(name))] if name == "slice")
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
                if matches!(args.first(), Some(Operand::Const(Constant::Str(name))) if name == "slice") => {
                    let Operand::Array(positional) = &args[1] else { panic!("构造实参数组必须保留") };
                    Some(positional.len())
                }
            _ => None,
        })
        .collect();
    assert_eq!(
        calls,
        [0, 0, 0, 0, 4],
        "四次回调必须在最终slice绑定前完整保留"
    );
    assert_eq!(
        values(function("shadowed"))
            .into_iter()
            .filter(|value| matches!(value, Rvalue::DynCallValue { args, .. } if args.len() == 1))
            .count(),
        1
    );
}
