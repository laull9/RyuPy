//! mutable subject sub/subn的三个CP-first候选应用；mutable replacement独立pending。
#[path = "mutable_regex_sub_support/mod.rs"]
mod support;

#[test]
fn a_templates_counts_public_routes_and_exact_controls_match_python() {
    support::check(
        "templates_counts_routes.py",
        include_str!("../../../tests/fixtures/mutable_regex_sub/templates_counts_routes.py"),
        &[
            "S1.routes-bytes",
            "S1.callback-bytes",
            "S1.optional-bytes",
            "S1.routes-bytearray",
            "S1.callback-bytearray",
            "S1.optional-bytearray",
            "S1.routes-view_B",
            "S1.callback-view_B",
            "S1.optional-view_B",
            "S1.routes-view_c",
            "S1.callback-view_c",
            "S1.optional-view_c",
            "S1.routes-view_slice",
            "S1.callback-view_slice",
            "S1.optional-view_slice",
            "S1.routes-view_readonly",
            "S1.callback-view_readonly",
            "S1.optional-view_readonly",
            "S1.carriers-release",
            "S1.count-bytes--1",
            "S1.count-bytes-0",
            "S1.count-bytes-1",
            "S1.none-count-bytes",
            "S1.count-bytearray--1",
            "S1.count-bytearray-0",
            "S1.count-bytearray-1",
            "S1.none-count-bytearray",
            "S1.empty-bytes",
            "S1.empty-bytearray",
            "S1.empty-view",
            "S1.typed-template-bytes-0",
            "S1.typed-template-bytes-1",
            "S1.typed-template-bytes-2",
            "S1.typed-template-bytearray-0",
            "S1.typed-template-bytearray-1",
            "S1.typed-template-bytearray-2",
            "S1.mutable-snapshot",
            "S1.view-snapshot",
            "S1.exact-identity",
            "S1.empty-progress-0",
            "S1.empty-progress-1",
            "S1.empty-progress-2",
            "S1.empty-progress-3",
            "S1.text-control",
            "S1.complete",
        ],
        None,
    );
}

#[test]
fn b_live_mutation_reentry_aliases_and_empty_retry_match_python() {
    support::check(
        "callbacks_leases.py",
        include_str!("../../../tests/fixtures/mutable_regex_sub/callbacks_leases.py"),
        &[
            "S2.mutation",
            "S2.retained",
            "S2.nested-mutable",
            "S2.nested-readonly",
            "S2.child-alias",
            "S2.empty-retry",
            "S2.prefix-refresh",
            "S2.complete",
        ],
        None,
    );
}

#[test]
fn c_order_late_results_and_full_callback_error_chain_match_python() {
    support::check(
        "errors_order.py",
        include_str!("../../../tests/fixtures/mutable_regex_sub/errors_order.py"),
        &[
            "S3.callback-identity-bytearray",
            "S3.callback-identity-view",
            "S3.retained-released",
            "S3.invalid-template",
            "S3.invalid-template-clean",
            "S3.count-release--1",
            "S3.count-release-clean--1",
            "S3.count-release-0",
            "S3.count-release-clean-0",
            "S3.template-before-subject",
            "S3.template-before-clean",
            "S3.count-before-template",
            "S3.count-before-clean",
            "S3.count-resize",
            "S3.late-result-str",
            "S3.late-result-clean-str",
            "S3.late-result-int",
            "S3.late-result-clean-int",
            "S3.later-error",
            "S3.protocol-complete",
            "S3.chain",
            "S3.finally-clean",
        ],
        Some(EXPECTED_TRACE),
    );
}

// 静态候选链，不冒充已运行事实；首次真实CP不符立即停，不编译/JIT。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File errors_order.py, line 188, in <module>",
    "LookupError: sub-cause",
    "The above exception was the direct cause of the following exception:",
    "Traceback (most recent call last):",
    "File errors_order.py, line 194, in <module>",
    "RuntimeError: sub-ambient",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File errors_order.py, line 197, in <module>",
    "File errors_order.py, line 184, in sub_outer",
    "File errors_order.py, line 180, in sub_middle",
    "File errors_order.py, line 176, in callback_middle",
    "File errors_order.py, line 172, in callback_leaf",
    "ValueError: sub-callback-body",
];
