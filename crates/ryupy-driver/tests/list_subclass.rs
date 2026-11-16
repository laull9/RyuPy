//! 真实list派生载体三份公开入口应用；固定 CPython 同次差分，默认协议与根raw边界独立验收。
#[path = "list_subclass_support/mod.rs"]
mod support;

#[test]
fn list_subclass_construction_methods() {
    support::check(
        "construction_methods.py",
        include_str!("../../../tests/fixtures/list_subclass/construction_methods.py"),
        &[
            "list-subclass.default-and-types",
            "list-subclass.custom-init",
            "list-subclass.root-allocation",
            "list-subclass.methods",
            "list-subclass.fresh-base-results",
            "list-subclass.core-protocols",
            "list-subclass.in-place-identity",
            "list-subclass.foreign-reflection",
        ],
        false,
    );
}

#[test]
fn list_subclass_alias_override() {
    support::check(
        "alias_override.py",
        include_str!("../../../tests/fixtures/list_subclass/alias_override.py"),
        &[
            "list-subclass.named-overrides",
            "list-subclass.live-lookup",
            "list-subclass.property-root-bypass",
            "list-subclass.self-alias",
            "list-subclass.init-order",
            "list-subclass.partial-mutation",
            "list-subclass.capture-before-yield",
        ],
        false,
    );
}

#[test]
fn list_subclass_errors_chain() {
    let mut labels = vec!["list-subclass.error"; 21];
    labels.extend(["list-subclass.error-evaluation", "list-subclass.chain"]);
    support::check(
        "errors_chain.py",
        include_str!("../../../tests/fixtures/list_subclass/errors_chain.py"),
        &labels,
        true,
    );
}
