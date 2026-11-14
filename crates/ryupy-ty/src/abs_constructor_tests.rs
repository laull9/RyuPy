//! abs非单位置形态动态绑定；整数与浮点单参保持静态。
use ryupy_base::SourceDb;

#[test]
fn abs_constructor_shapes_do_not_discard_arguments_or_demote_numeric_fast_path() {
    let source = "def numeric(n: int) -> int:\n    return abs(n)\ndef floating(n: float) -> float:\n    return abs(n)\ndef zero() -> int:\n    return abs()\ndef pair(n: int) -> int:\n    return abs(n, 16)\ndef many(n: int) -> int:\n    return abs(n, 16, 2)\ndef keyword(n: int) -> int:\n    return abs(x=n)\ndef star(n: int) -> int:\n    return abs(*[n])\ndef double(n: int) -> int:\n    return abs(n, **{})\n";
    let mut db = SourceDb::default();
    let file = db.add("abs_shapes.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            instance_capabilities: Default::default(),
            name: "__main__".into(),
            package: String::new(),
            file: "abs_shapes.py".into(),
            instance_globals: false,
            resolve_file_at_runtime: false,
        },
        source,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = crate::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for (id, function) in hir.functions.iter() {
        if ["numeric", "floating"].contains(&function.name.as_str()) {
            let expected = if function.name.as_str() == "numeric" {
                crate::Ty::Int
            } else {
                crate::Ty::Float
            };
            assert_eq!(types.mode(id), crate::FuncMode::Static);
            assert_eq!(types.sigs[id].params, vec![expected.clone()]);
            assert_eq!(types.sigs[id].ret, expected);
            assert!(
                types
                    .body(id)
                    .unwrap()
                    .exprs
                    .iter()
                    .all(|(_, ty)| !ty.is_dynamic())
            );
        } else if ["zero", "pair", "many", "keyword", "star", "double"]
            .contains(&function.name.as_str())
        {
            assert_eq!(
                types.mode(id),
                crate::FuncMode::Dynamic,
                "{}",
                function.name
            );
            assert_eq!(types.sigs[id].ret, crate::Ty::Dynamic);
        }
    }
}
