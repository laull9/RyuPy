//! 类体条件分支保留range身份、源序命名空间和语法级注解初始化。
use crate::*;
fn lower(source: &str) -> (Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("class_if.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    lower_module_with_native_exceptions(
        &parsed,
        &ModuleInfo::default(),
        source,
        &["typing", "enum", "dataclasses"],
    )
}
#[test]
fn conditional_methods_have_distinct_ranges_and_defaults_keep_class_lookup() {
    let (module, diagnostics) = lower(
        "value=7\nclass C:\n    if True:\n        value=8\n        def method(self, x=value):\n            return x\n    else:\n        def method(self, x=value):\n            return x\n    after=value\n",
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let methods: Vec<_> = module
        .functions
        .iter()
        .filter(|(_, function)| function.name.as_str() == "method")
        .collect();
    assert_eq!(methods.len(), 2);
    assert_ne!(methods[0].1.range, methods[1].1.range);
    assert!(module.functions.iter().any(|(_, function)| {
        function
            .body
            .exprs
            .iter()
            .any(|(_, expr)| matches!(expr.kind, ExprKind::ClassLookup { .. }))
    }));
}
#[test]
fn unexecuted_annotations_initialize_namespace_and_special_classes_stay_explicit() {
    let (module, diagnostics) =
        lower("class C:\n    before=__annotations__\n    if False:\n        absent: Unknown\n");
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(module.classes.iter().next().unwrap().1.has_annotations);
    for source in [
        "from dataclasses import dataclass\n@dataclass\nclass C:\n    if True:\n        field:int\n",
        "from enum import Enum\nclass C(Enum):\n    if True:\n        FIELD=1\n",
        "from typing import TypedDict\nclass C(TypedDict):\n    if True:\n        field:int\n",
    ] {
        let (_, diagnostics) = lower(source);
        assert!(
            diagnostics
                .iter()
                .any(|diagnostic| diagnostic.code == Some("E0100")),
            "{diagnostics:?}"
        );
    }
}

#[test]
fn parenthesized_annotations_evaluate_without_declaring_fields() {
    let (module, diagnostics) = lower(
        "def annotation():\n    return int\nclass C:\n    if True:\n        (absent): annotation()\n        (assigned): annotation() = 3\nclass D:\n    (field): annotation() = 4\n",
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for (_, class) in module.classes.iter() {
        assert!(class.has_annotations);
        assert!(class.declared_fields.is_empty());
        assert!(class.fields.is_empty());
    }
    let class = module.classes.iter().next().unwrap().1;
    let ClassItemKind::If { body, .. } = &class.body[0].kind else {
        panic!("应保留类条件")
    };
    assert!(matches!(body[0].kind, ClassItemKind::Expr(_)));
    assert!(matches!(body[1].kind, ClassItemKind::Attr { .. }));
    assert!(matches!(body[2].kind, ClassItemKind::Expr(_)));
}
