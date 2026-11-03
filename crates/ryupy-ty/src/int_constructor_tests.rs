//! int完整调用形态动态绑定，唯固定单位置数值转换保静态路径。
use ryupy_base::SourceDb;

#[test]
fn int_constructor_shapes_do_not_discard_arguments_or_demote_numeric_fast_path() {
    let source = "def numeric(n: int) -> int:\n    return int(n) + 1\ndef text(value: str) -> int:\n    return int(value)\ndef zero() -> int:\n    return int()\ndef pair(n: int) -> int:\n    return int(n, 16)\ndef many(n: int) -> int:\n    return int(n, 16, 2)\ndef keyword(n: int) -> int:\n    return int(n, base=16)\ndef star(n: int) -> int:\n    return int(*[n])\ndef double(n: int) -> int:\n    return int(n, **{'base': 16})\n";
    let mut db = SourceDb::default();
    let file = db.add("int_shapes.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: "__main__".into(),
            package: String::new(),
            file: "int_shapes.py".into(),
            resolve_file_at_runtime: false,
        },
        source,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = crate::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for (id, function) in hir.functions.iter() {
        if function.name.as_str() == "numeric" {
            assert_eq!(types.mode(id), crate::FuncMode::Static);
            assert_eq!(types.sigs[id].params, vec![crate::Ty::Int]);
            assert_eq!(types.sigs[id].ret, crate::Ty::Int);
            assert!(
                types
                    .body(id)
                    .unwrap()
                    .exprs
                    .iter()
                    .all(|(_, ty)| !ty.is_dynamic())
            );
        } else if ["text", "zero", "pair", "many", "keyword", "star", "double"]
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
