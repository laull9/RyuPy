//! mutable subject finditer的三个CP-first应用；正常租约、活读与完整用户错误链分别验收。
#[path = "mutable_regex_finditer_support/mod.rs"]
mod support;

#[test]
fn a_creation_true_exhaustion_routes_and_exact_controls_match_python() {
    support::check(
        "creation_lifetime_routes.py",
        include_str!("../../../tests/fixtures/mutable_regex_finditer/creation_lifetime_routes.py"),
        &[
            "I1.routes-bytes-module",
            "I1.routes-bytes-module_saved",
            "I1.routes-bytes-pattern",
            "I1.routes-bytes-pattern_saved",
            "I1.routes-bytearray-module",
            "I1.routes-bytearray-module_saved",
            "I1.routes-bytearray-pattern",
            "I1.routes-bytearray-pattern_saved",
            "I1.routes-view_B-module",
            "I1.routes-view_B-module_saved",
            "I1.routes-view_B-pattern",
            "I1.routes-view_B-pattern_saved",
            "I1.routes-view_c-module",
            "I1.routes-view_c-module_saved",
            "I1.routes-view_c-pattern",
            "I1.routes-view_c-pattern_saved",
            "I1.routes-view_child-module",
            "I1.routes-view_child-module_saved",
            "I1.routes-view_child-pattern",
            "I1.routes-view_child-pattern_saved",
            "I1.routes-view_readonly-module",
            "I1.routes-view_readonly-module_saved",
            "I1.routes-view_readonly-pattern",
            "I1.routes-view_readonly-pattern_saved",
            "I1.routes-clean",
            "I1.creation-last-yield",
            "I1.exhausted-stays-done",
            "I1.empty-window-nomatch",
            "I1.empty-window-pos_gt_end",
            "I1.empty-window-empty_subject",
            "I1.bounds-0",
            "I1.bounds-1",
            "I1.bounds-2",
            "I1.bounds-3",
            "I1.bounds-4",
            "I1.bounds-5",
            "I1.empty-tail",
            "I1.drain-bound",
            "I1.consumer-for",
            "I1.consumer-list",
            "I1.exact-identity",
            "I1.exact-empty-0",
            "I1.exact-empty-1",
            "I1.exact-empty-2",
            "I1.exact-empty-3",
            "I1.text-control",
            "I1.complete",
        ],
        None,
    );
}

#[test]
fn b_live_alias_readonly_and_independent_exports_match_python() {
    support::check(
        "live_aliases_exports.py",
        include_str!("../../../tests/fixtures/mutable_regex_finditer/live_aliases_exports.py"),
        &[
            "I2.live-kept",
            "I2.empty-retry",
            "I2.prefix-refresh",
            "I2.alias-write",
            "I2.child-offset",
            "I2.dual-mutable",
            "I2.dual-readonly",
            "I2.sub-nested-iterator",
            "I2.complete",
        ],
        None,
    );
}

#[test]
fn c_creation_order_consumer_error_and_full_user_chain_match_python() {
    support::check(
        "creation_errors_order.py",
        include_str!("../../../tests/fixtures/mutable_regex_finditer/creation_errors_order.py"),
        &[
            "I3.release-order-direct",
            "I3.release-clean-direct",
            "I3.release-order-saved",
            "I3.release-clean-saved",
            "I3.index-grow",
            "I3.bad-index-pos",
            "I3.bad-index-clean-pos",
            "I3.bad-index-end",
            "I3.bad-index-clean-end",
            "I3.bad-index-pos-wide",
            "I3.bad-index-clean-pos-wide",
            "I3.bad-index-end-wide",
            "I3.bad-index-clean-end-wide",
            "I3.index-before-source",
            "I3.index-before-clean",
            "I3.index-identity-pos",
            "I3.index-identity-end",
            "I3.released-direct",
            "I3.released-saved",
            "I3.released-module",
            "I3.released-module_saved",
            "I3.bytes-text-domain",
            "I3.text-bytes-domain",
            "I3.consumer-still-active",
            "I3.consumer-clean",
            "I3.protocol-complete",
            "I3.chain",
            "I3.finally-clean",
        ],
        Some(EXPECTED_TRACE),
    );
}

// 静态候选完整链；父首次真实CP必须逐帧吻合，才允许构建/JIT。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File creation_errors_order.py, line 195, in <module>",
    "LookupError: finditer-cause",
    "The above exception was the direct cause of the following exception:",
    "Traceback (most recent call last):",
    "File creation_errors_order.py, line 200, in <module>",
    "RuntimeError: finditer-ambient",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File creation_errors_order.py, line 204, in <module>",
    "File creation_errors_order.py, line 191, in finditer_outer",
    "File creation_errors_order.py, line 187, in finditer_middle",
    "File creation_errors_order.py, line 183, in __index__",
    "File creation_errors_order.py, line 178, in index_leaf",
    "ValueError: finditer-index-body",
];
