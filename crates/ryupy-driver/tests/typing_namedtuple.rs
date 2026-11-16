//! 5个真实CP-first应用，默认gate注册；未执行准备稿，无NI替代或sampling改变。
#[path = "typing_namedtuple_support/mod.rs"]
mod support;

#[test]
fn functional_fields_metadata_and_two_pass_match_python() {
    support::success(
        "functional_fields.py",
        include_str!("../../../tests/fixtures/typing_namedtuple/functional_fields.py"),
        &[],
        &[
            "N1.fields",
            "N1.metadata",
            "N1.refs",
            "N1.mixed-error",
            "N1.pair-error",
            "N1.duplicate-error",
            "N1.complete",
        ],
    );
}
#[test]
fn final_namespace_future_and_default_order_match_python() {
    support::success(
        "class_namespace_future.py",
        include_str!("../../../tests/fixtures/typing_namedtuple/class_namespace_future.py"),
        &[],
        &[
            "N2.defaults",
            "N2.namespace",
            "N2.future",
            "N2.metadata",
            "N2.default-error",
            "N2.forbidden-error",
            "N2.name-first",
            "N2.complete",
        ],
    );
}
#[test]
fn ordinary_methods_and_properties_match_python() {
    support::success(
        "class_members.py",
        include_str!("../../../tests/fixtures/typing_namedtuple/class_members.py"),
        &[],
        &["N3.methods", "N3.property", "N3.complete"],
    );
}
#[test]
fn aliases_current_root_and_atomic_publication_match_python() {
    support::success(
        "entry.py",
        include_str!("../../../tests/fixtures/typing_namedtuple/modules/entry.py"),
        &[(
            "provider.py",
            include_str!("../../../tests/fixtures/typing_namedtuple/modules/provider.py"),
        )],
        &[
            "N4.aliases",
            "N4.ordinary",
            "N4.rebound-root",
            "N4.restored",
            "N4.definition-binding",
            "N4.body-error",
            "N4.default-error",
            "N4.finish-error",
            "N4.decorator-error",
            "N4.decorator-replace",
            "N4.origins",
            "N4.falsey-origin",
            "N4.complete",
        ],
    );
}
#[test]
fn uncaught_constructor_binding_keeps_every_user_frame() {
    support::uncaught(
        "uncaught_binding.py",
        include_str!("../../../tests/fixtures/typing_namedtuple/uncaught_binding.py"),
    );
}
#[test]
fn runtime_class_binding_publication_and_doc_structure() {
    support::structure();
}
