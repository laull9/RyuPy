//! 外部 eq/ne、ABC Iterator 与 dataclass 可变默认值的同次 CPython 差分回归。
#[path = "list_subclass_support/mod.rs"]
mod support;

#[test]
fn list_subclass_foreign_comparison_matches_python() {
    support::check(
        "foreign_comparison.py",
        include_str!("../../../tests/fixtures/list_subclass/foreign_comparison.py"),
        &[],
        false,
    );
}
#[test]
fn list_subclass_abc_iterator_matches_python() {
    support::check(
        "abc_iterator.py",
        include_str!("../../../tests/fixtures/list_subclass/abc_iterator.py"),
        &[],
        false,
    );
}
#[test]
fn list_subclass_dataclass_mutable_default_matches_python() {
    support::check(
        "dataclass_default.py",
        include_str!("../../../tests/fixtures/list_subclass/dataclass_default.py"),
        &["ValueError"],
        false,
    );
}
