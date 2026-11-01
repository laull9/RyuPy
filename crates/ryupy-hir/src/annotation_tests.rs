//! 延迟注解的HIR边界：字符串元数据与作用域/类型提示分离，不执行未知类型表达式。
use crate::*;
fn lower(source: &str) -> (Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("future.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    lower_module(&parsed, &ModuleInfo::default(), source)
}
#[test]
fn future_unknown_names_do_not_resolve_or_capture() {
    let (module, diagnostics) = lower(
        "from __future__ import annotations\ndef outer():\n    Unknown: int\n    def inner(value: Unknown) -> Missing:\n        return value\n    return inner\n",
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (_, inner) = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "inner")
        .unwrap();
    assert!(inner.closure.free.is_empty());
    assert_eq!(inner.params[0].annotation, Some(TypeRef::Runtime));
    assert_eq!(inner.ret, Some(TypeRef::Runtime));
    assert_eq!(inner.annotation_values.len(), 2);
}
#[test]
fn future_complex_annotation_remains_explicitly_rejected() {
    let (_, diagnostics) =
        lower("from __future__ import annotations\ndef f(value: unknown()):\n    pass\n");
    assert!(diagnostics.iter().any(|d| d.code == Some("E0100")));
    assert!(!diagnostics.iter().any(|d| d.code == Some("E0106")));
}
#[test]
fn module_setup_scans_dead_branches_but_excludes_local_annotations() {
    let (module, diagnostics) = lower(
        "from __future__ import annotations\nif False:\n    value: Missing\nprint(__annotations__)\n",
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(
        module
            .vars
            .iter()
            .any(|(_, v)| v.name.as_str() == "__annotations__")
    );
    let (module, diagnostics) =
        lower("from __future__ import annotations\ndef f():\n    local: missing()\n    return 1\n");
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(
        !module
            .vars
            .iter()
            .any(|(_, v)| v.name.as_str() == "__annotations__")
    );
}
