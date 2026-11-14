//! bytearray切片三个CP-first公开入口，复用保留源、版本、原流和编译/JIT证据的既有驱动。
#[path = "bytearray_slice_support/mod.rs"]
mod support;

#[test]
fn bytearray_slice_basics_alias_and_export_lock_match_python() {
    support::check(
        "basics_buffers.py",
        include_str!("../../../tests/fixtures/bytearray_slice/basics_buffers.py"),
        &[
            "slice.basics",
            "slice.wide",
            "slice.self",
            "slice.alias",
            "slice.mismatch",
            "slice.lock",
            "slice.lock",
            "slice.release",
        ],
        None,
    );
}

#[test]
fn bytearray_slice_callback_rounds_and_sequence_sources_match_python() {
    support::check(
        "callbacks_sequences.py",
        include_str!("../../../tests/fixtures/bytearray_slice/callbacks_sequences.py"),
        &[
            "slice.round-array",
            "slice.round-bytes",
            "slice.round-self",
            "slice.round-view",
            "slice.round-list",
            "slice.round-tuple",
            "slice.round-range",
            "slice.item-between",
            "slice.final-length",
            "slice.final-length",
            "slice.source-failure",
            "slice.user-error",
            "slice.sequences",
            "slice.live-list-append",
            "slice.live-list-remove",
            "slice.live-list-replace",
            "slice.second-error",
            "slice.self-between",
            "slice.chain",
            "slice.finally",
        ],
        Some(EXPECTED_TRACE),
    );
}

#[test]
fn unchanged_regex_slice_assignment_fixture_matches_python() {
    // 不复制、不改写原pending源；直接用同一字节内容解除公开切片阻断。
    support::check(
        "inputs_templates_slice_assignment.py",
        include_str!(
            "../../../tests/fixtures/mutable_regex_replacement/pending/inputs_templates_slice_assignment.py.pending"
        ),
        &[
            "F2.inputs-exact-pattern",
            "F2.inputs-exact-saved",
            "F2.inputs-exact-module",
            "F2.inputs-array-pattern",
            "F2.inputs-array-saved",
            "F2.inputs-array-module",
            "F2.inputs-view-pattern",
            "F2.inputs-view-saved",
            "F2.inputs-view-module",
            "F2.counts",
            "F2.same-object",
            "F2.expand-exact",
            "F2.expand-array",
            "F2.expand-view",
            "F2.expand-missing",
            "F2.expand-live",
            "F2.invalid-match",
            "F2.invalid-nomatch",
            "F2.invalid-negative",
            "F2.invalid-badsubject",
            "F2.invalid-expand",
            "F2.count-mutates",
            "F2.count-grows",
            "F2.count-error",
            "F2.released-source-order",
            "F2.released-source-clean",
            "F2.alias-array",
            "F2.alias-view",
            "F2.exact-text-controls",
            "F2.inputs-complete",
        ],
        None,
    );
}

// 固定CP验证全部帧、cause/context与最终异常，原始stderr另存，不做宽泛过滤。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File callbacks_sequences.py, line 145, in <module>",
    "ValueError: slice-cause",
    "The above exception was the direct cause of the following exception:",
    "Traceback (most recent call last):",
    "File callbacks_sequences.py, line 149, in <module>",
    "RuntimeError: slice-ambient",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File callbacks_sequences.py, line 154, in <module>",
    "File callbacks_sequences.py, line 143, in slice_outer",
    "File callbacks_sequences.py, line 141, in slice_middle",
    "File callbacks_sequences.py, line 139, in __index__",
    "LookupError: slice-item",
];
