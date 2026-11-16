//! handler目标必须真正参与类型检查；静态数值函数不因别的动态handler降级。
#[test]
fn dynamic_except_targets_are_checked_without_poisoning_static_functions() {
    let source = "def target(kind: int):\n    try:\n        pass\n    except kind.problem.Error:\n        pass\ndef numeric(value: int) -> int:\n    return value + 1\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("dynamic_except.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (module, diagnostics) =
        ryupy_hir::lower_module(&parsed, &ryupy_hir::ModuleInfo::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = crate::check_module(&module);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (id, target) = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "target")
        .unwrap();
    let expr = target
        .body
        .handlers
        .iter()
        .next()
        .unwrap()
        .1
        .type_expr
        .unwrap();
    assert_eq!(types.mode(id), crate::FuncMode::Dynamic);
    assert_eq!(types.body(id).unwrap().expr_ty(expr), crate::Ty::Dynamic);
    let (id, _) = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "numeric")
        .unwrap();
    assert_eq!(types.mode(id), crate::FuncMode::Static);
    assert_eq!(types.sigs[id].ret, crate::Ty::Int);
}
