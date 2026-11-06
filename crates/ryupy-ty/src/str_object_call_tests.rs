//! 公共str只在已证精确单参时返回静态字符串；未知协议结果保object身份。
#[test]
fn str_object_results_and_complete_binding_keep_only_proven_fast_paths() {
    let source = "def numeric(value: int) -> str:\n    return str(value)\ndef floating(value: float) -> str:\n    return str(value)\ndef boolean(value: bool) -> str:\n    return str(value)\ndef literal() -> str:\n    return str('hello')\ndef none_value() -> str:\n    return str(None)\ndef opaque(value: str) -> str:\n    return str(value)\ndef unknown(value):\n    return str(value)\ndef zero() -> str:\n    return str()\ndef many() -> str:\n    return str(11, 22, 33)\ndef keyword(value: int) -> str:\n    return str(object=value)\ndef star(value: int) -> str:\n    return str(*[value])\ndef double(value: int) -> str:\n    return str(value, **{})\nclass Plain:\n    pass\ndef instance(value: Plain) -> str:\n    return str(value)\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("str_object_shapes.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let (module, diagnostics) =
        ryupy_hir::lower_module(&parsed, &ryupy_hir::ModuleInfo::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = crate::check_module(&module);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let mut fast = 0;
    let mut dynamic = 0;
    for (id, function) in module.functions.iter() {
        if ["numeric", "floating", "boolean", "literal", "none_value"]
            .contains(&function.name.as_str())
        {
            fast += 1;
            assert_eq!(types.mode(id), crate::FuncMode::Static, "{}", function.name);
            assert_eq!(types.sigs[id].ret, crate::Ty::OpaqueStr);
            let body = types.body(id).unwrap();
            let results: Vec<_> = function
                .body
                .exprs
                .iter()
                .filter(|(_, value)| matches!(value.kind, ryupy_hir::ExprKind::Call { .. }))
                .map(|(expr, _)| body.expr_ty(expr))
                .collect();
            assert_eq!(results, vec![crate::Ty::Str]);
        } else if [
            "opaque", "unknown", "zero", "many", "keyword", "star", "double", "instance",
        ]
        .contains(&function.name.as_str())
        {
            dynamic += 1;
            assert_eq!(
                types.mode(id),
                crate::FuncMode::Dynamic,
                "{}",
                function.name
            );
            assert_eq!(types.sigs[id].ret, crate::Ty::Dynamic);
        }
    }
    assert_eq!((fast, dynamic), (5, 8));
}
