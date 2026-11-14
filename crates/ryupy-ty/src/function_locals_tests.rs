//! 直接locals带来函数内对象逃逸，但不会污染其它静态函数。
#[test]
fn direct_locals_preserves_identity_without_demoting_unrelated_functions() {
    let source = "def observed(value: int) -> int:\n    local: int = value\n    locals()\n    return local\ndef numeric(value: int) -> int:\n    return value + 1\ndef locals_shadow(locals: int) -> int:\n    return locals + 1\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("function_locals.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (module, diagnostics) =
        ryupy_hir::lower_module(&parsed, &ryupy_hir::ModuleInfo::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = crate::check_module(&module);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for (id, function) in module.functions.iter() {
        let expected = match function.name.as_str() {
            "observed" => crate::FuncMode::Dynamic,
            "numeric" | "locals_shadow" => crate::FuncMode::Static,
            _ => continue,
        };
        assert_eq!(types.mode(id), expected, "{}", function.name);
    }
}
