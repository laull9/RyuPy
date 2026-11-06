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

#[test]
fn runtime_metadata_reads_are_class_local_without_synthetic_attr_writes() {
    let (module, diagnostics) = lower(
        "from typing import IO\nclass C:\n    module=__module__\n    qualified=__qualname__\nclass D(IO):\n    module=__module__\n    qualified=__qualname__\n",
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for (_, class) in module.classes.iter() {
        assert_eq!(class.body.len(), 2);
        for item in &class.body {
            let ClassItemKind::Attr { name, value } = &item.kind else {
                panic!("expected attribute")
            };
            let expected = if name.as_str() == "module" {
                "__module__"
            } else {
                "__qualname__"
            };
            assert!(
                matches!(&module.functions[module.main].body.exprs[*value].kind,
                ExprKind::ClassLocal(actual) if actual.as_str() == expected)
            );
        }
    }
}

#[test]
fn only_outer_first_string_is_doc_and_later_override_keeps_order() {
    let (module, diagnostics) = lower(
        "class C:\n    'first'\n    seen=__doc__\n    if True:\n        'branch'\n        x=1\n    'later'\n    __doc__='override'\nclass D:\n    if True:\n        'not doc'\n        x=1\n",
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let classes: Vec<_> = module.classes.iter().map(|(_, class)| class).collect();
    let docs: Vec<_> = classes[0]
        .body
        .iter()
        .enumerate()
        .filter_map(|(index, item)| {
            matches!(&item.kind, ClassItemKind::Attr { name, .. } if name.as_str() == "__doc__")
                .then_some(index)
        })
        .collect();
    assert_eq!(docs, [0, 3]);
    assert_eq!(classes[0].body.len(), 4);
    let ClassItemKind::If { body, .. } = &classes[0].body[2].kind else {
        panic!("expected if")
    };
    assert_eq!(body.len(), 1);
    assert_eq!(classes[1].body.len(), 1);
    let ClassItemKind::If { body, .. } = &classes[1].body[0].kind else {
        panic!("expected if")
    };
    assert_eq!(body.len(), 1);
}

#[test]
fn typed_dict_doc_keeps_special_expression_protocol() {
    let (module, diagnostics) = lower(
        "from typing import TypedDict\nclass C(TypedDict):\n    'typed dict doc'\n    x: int\n",
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let class = module.classes.iter().next().unwrap().1;
    assert!(matches!(class.base, Some(ClassBase::TypedDict { .. })));
    assert!(matches!(class.body[0].kind, ClassItemKind::Expr(_)));
}
