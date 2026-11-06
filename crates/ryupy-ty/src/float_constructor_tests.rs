//! float非单位置形态动态绑定；整数与浮点单参保持静态。
use ryupy_base::SourceDb;

#[test]
fn float_constructor_shapes_do_not_discard_arguments_or_demote_numeric_fast_path() {
    let source = "def numeric(n: int) -> float:\n    return float(n)\ndef floating(n: float) -> float:\n    return float(n)\ndef boolean(n: bool) -> float:\n    return float(n)\ndef text(n: str) -> float:\n    return float(n)\ndef zero() -> float:\n    return float()\ndef pair(n: int) -> float:\n    return float(n, 16)\ndef keyword(n: int) -> float:\n    return float(x=n)\ndef star(n: int) -> float:\n    return float(*[n])\ndef double(n: int) -> float:\n    return float(n, **{})\n";
    let mut db = SourceDb::default();
    let file = db.add("float_shapes.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: "__main__".into(),
            package: String::new(),
            file: "float_shapes.py".into(),
            instance_globals: false,
            resolve_file_at_runtime: false,
        },
        source,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = crate::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for (id, function) in hir.functions.iter() {
        if ["numeric", "floating", "boolean"].contains(&function.name.as_str()) {
            let parameter = match function.name.as_str() {
                "numeric" => crate::Ty::Int,
                "floating" => crate::Ty::Float,
                _ => crate::Ty::Bool,
            };
            assert_eq!(types.mode(id), crate::FuncMode::Static);
            assert_eq!(types.sigs[id].params, vec![parameter]);
            assert_eq!(types.sigs[id].ret, crate::Ty::Float);
            assert!(
                types
                    .body(id)
                    .unwrap()
                    .exprs
                    .iter()
                    .all(|(_, ty)| !ty.is_dynamic())
            );
        } else if ["text", "zero", "pair", "keyword", "star", "double"]
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
