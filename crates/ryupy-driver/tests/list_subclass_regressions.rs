//! 独立review原失败源的公开回归：外部eq/ne、ABC Iterator及dataclass可变默认值。
//! 不替换三份首轮应用；每份仍先固定CP原始输出，再由同一严格harness执行JIT。
#[path = "list_subclass_support/mod.rs"]
mod support;

fn check_success(filename: &str, source: &str, golden: &[u8]) {
    let frozen = std::str::from_utf8(golden).unwrap();
    let labels: Vec<_> = frozen
        .lines()
        .map(|line| line.split_whitespace().next().expect("冻结输出不得有空行"))
        .collect();
    support::check(filename, source, &labels, golden, None);
}

#[test]
fn list_subclass_foreign_comparison_matches_python() {
    check_success(
        "foreign_comparison.py",
        include_str!("../../../tests/fixtures/list_subclass/foreign_comparison.py"),
        include_bytes!("../../../tests/fixtures/list_subclass/golden/foreign_comparison.stdout"),
    );
}

#[test]
fn list_subclass_abc_iterator_matches_python() {
    check_success(
        "abc_iterator.py",
        include_str!("../../../tests/fixtures/list_subclass/abc_iterator.py"),
        include_bytes!("../../../tests/fixtures/list_subclass/golden/abc_iterator.stdout"),
    );
}

#[test]
fn list_subclass_dataclass_mutable_default_matches_python() {
    check_success(
        "dataclass_default.py",
        include_str!("../../../tests/fixtures/list_subclass/dataclass_default.py"),
        include_bytes!("../../../tests/fixtures/list_subclass/golden/dataclass_default.stdout"),
    );
}
