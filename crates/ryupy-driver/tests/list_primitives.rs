//! 精确list必要基础协议三份公开应用；CP-first固定原源/双流与完整六帧错误链。
#[path = "list_primitives_support/mod.rs"]
mod support;

#[test]
fn list_primitives_slice_index() {
    support::check(
        "slice_index.py",
        include_str!("../../../tests/fixtures/list_primitives/slice_index.py"),
        &[
            "list-primitives.contiguous-delete",
            "list-primitives.negative-step-delete",
            "list-primitives.empty-window",
            "list-primitives.slice-self-and-size",
            "list-primitives.index-missing",
            "list-primitives.index-empty",
        ],
        false,
    );
}

#[test]
fn list_primitives_extend_order() {
    let mut labels = Vec::new();
    for _ in 0..2 {
        labels.extend(["list-primitives.extend-success"; 4]);
        labels.extend(["list-primitives.extend-error"; 7]);
    }
    labels.extend([
        "list-primitives.self-alias",
        "list-primitives.common-sources",
    ]);
    support::check(
        "extend_order.py",
        include_str!("../../../tests/fixtures/list_primitives/extend_order.py"),
        &labels,
        false,
    );
}

#[test]
fn list_primitives_iterator_chain() {
    support::check(
        "iterator_chain.py",
        include_str!("../../../tests/fixtures/list_primitives/iterator_chain.py"),
        &[
            "list-primitives.iterator-append-and-exhaust",
            "list-primitives.iterator-live-delete",
            "list-primitives.iterator-initially-empty",
            "list-primitives.chain",
            "list-primitives.chain-prefix",
        ],
        true,
    );
}
