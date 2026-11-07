//! TypedDict入口只验证driver身份与支持语法，不承诺条件导入已经执行或别名稳定。
use crate::{ClassBase, ModuleInfo, lower_module_with_native_exceptions};
fn lower(source: &str, native: &[&str]) -> (crate::Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("typed_dict.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    lower_module_with_native_exceptions(
        &parsed,
        &ModuleInfo {
            instance_capabilities: Default::default(),
            instance_globals: false,
            resolve_file_at_runtime: false,
            name: "__main__".into(),
            package: String::new(),
            file: "typed_dict.py".into(),
        },
        source,
        native,
    )
}
#[test]
fn conditional_import_and_parent_choose_identity_guarded_protocol() {
    let (module, errors) = lower(
        "if True:\n    from typing import TypedDict as Root\n    class Base(Root):\n        x: int\nclass Child(Base, total=False):\n    y: str\n",
        &["typing"],
    );
    assert!(errors.is_empty(), "{errors:?}");
    assert!(
        module
            .classes
            .iter()
            .all(|(_, c)| matches!(c.base, Some(ClassBase::TypedDict { .. }))
                && c.decorated_binding.is_some())
    );
    assert!(module.protected_exports.is_empty());
}
#[test]
fn shadowed_source_module_does_not_get_typed_dict_protocol() {
    let (module, errors) = lower(
        "from typing import TypedDict\nclass Record(TypedDict):\n    x: int\n",
        &[],
    );
    assert!(errors.is_empty(), "{errors:?}");
    assert!(
        module
            .classes
            .iter()
            .all(|(_, c)| matches!(c.base, Some(ClassBase::Runtime(_))))
    );
}
#[test]
fn unsupported_class_features_are_diagnostics() {
    for definition in [
        "class C(TypedDict):\n    (x): int\n",
        "class C(TypedDict, total=1):\n    x: int\n",
        "class C(TypedDict):\n    x: int = 1\n",
        "class C(TypedDict):\n    def method(self):\n        pass\n",
        "class C(TypedDict, dict):\n    pass\n",
    ] {
        let (_, errors) = lower(
            &format!("from typing import TypedDict\n{definition}"),
            &["typing"],
        );
        assert!(
            errors.iter().any(|error| error.code == Some("E0100")),
            "{errors:?}"
        );
    }
}

#[test]
fn forward_syntax_candidates_never_lose_public_binding() {
    let (module, errors) = lower(
        "from typing import TypedDict\nif False:\n    class Early(Later):\n        pass\nclass Later(TypedDict):\n    pass\n",
        &["typing"],
    );
    assert!(errors.is_empty(), "{errors:?}");
    assert!(
        module
            .classes
            .iter()
            .all(|(_, c)| c.decorated_binding.is_some())
    );
}
