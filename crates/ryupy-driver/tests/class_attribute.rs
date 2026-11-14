//! 通用 __class__ 只读合同：先核固定 CP 原流，再独立构建/JIT；实例字典反射另留 pending。
#[path = "class_attribute_support/mod.rs"]
mod support;

#[test]
fn class_attribute_identity() {
    support::check(
        "identity.py",
        include_str!("../../../tests/cases/class_attribute/identity/entry.py"),
        &[
            "default-Plain",
            "default-Child",
            "default-Grandchild",
            "before-fallback",
            "roots",
            "version-invalidation",
        ],
    );
}

#[test]
fn class_attribute_lookup() {
    support::check(
        "lookup.py",
        include_str!("../../../tests/cases/class_attribute/lookup/entry.py"),
        &[
            "plain-and-none",
            "property-each-read",
            "method-binding",
            "static-binding",
            "class-binding",
            "nearest-item",
        ],
    );
}

#[test]
fn class_attribute_errors() {
    support::check(
        "errors.py",
        include_str!("../../../tests/cases/class_attribute/errors/entry.py"),
        &["value-error", "attribute-error", "no-getter"],
    );
}
