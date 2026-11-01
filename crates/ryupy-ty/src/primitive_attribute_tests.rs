//! primitive属性动态兜底只影响实际使用属性的函数，不能全模块关闭数值静态路径。
use ryupy_base::SourceDb;
use ryupy_hir::lower_module;
use ryupy_syntax::parse;

#[test]
fn primitive_attribute_fallback_keeps_unrelated_numeric_function_static() {
    let source = "def upper(value: str) -> str:\n    return value.upper()\n\ndef add(a: int, b: int) -> int:\n    return a + b\n\nprint(upper('hello'), add(20, 22))\n";
    let mut db = SourceDb::default();
    let file = db.add("attributes.py", source);
    let (parsed, diagnostics) = parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: "__main__".into(),
            package: String::new(),
            file: "attributes.py".into(),
        },
        source,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = crate::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let upper = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "upper")
        .unwrap()
        .0;
    let add = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "add")
        .unwrap()
        .0;
    assert_eq!(types.mode(upper), crate::ty::FuncMode::Dynamic);
    assert_eq!(types.mode(add), crate::ty::FuncMode::Static);
    assert_eq!(
        types.sigs[add].params,
        vec![crate::ty::Ty::Int, crate::ty::Ty::Int]
    );
    assert_eq!(types.sigs[add].ret, crate::ty::Ty::Int);
    assert!(
        types
            .body(add)
            .unwrap()
            .exprs
            .iter()
            .all(|(_, ty)| !ty.is_dynamic())
    );
}
