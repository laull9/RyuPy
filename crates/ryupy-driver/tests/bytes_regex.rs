//! exact bytes正则四应用：CP-first完整输出/错误链；mutable与旧syntax pending不注册。
#[path = "bytes_regex_support/mod.rs"]
mod support;

const DOMAIN: &str = include_str!("../../../tests/fixtures/bytes_regex/domain_ascii.py");
const MATCHES: &str = include_str!("../../../tests/fixtures/bytes_regex/matches_identity.py");
const TEMPLATES: &str = include_str!("../../../tests/fixtures/bytes_regex/templates_callbacks.py");
const BINDING: &str = include_str!("../../../tests/fixtures/bytes_regex/binding_context.py");

#[test]
fn a_domains_ascii_cache_escape_and_original_errors_match_python() {
    support::check(
        "domain_ascii.py",
        DOMAIN,
        &[
            "R1.cache",
            "R1.compiled-flags",
            "R1.purge",
            "R1.bytearray-pattern",
            "R1.bytes-on-text",
            "R1.text-on-bytes",
            "R1.bad-subject",
            "R1.class-word",
            "R1.class-digit",
            "R1.class-space",
            "R1.complement-word",
            "R1.complement-digit",
            "R1.complement-space",
            "R1.ignorecase",
            "R1.boundaries",
            "R1.flags",
            "R1.flag-unicode",
            "R1.flag-unicode-ascii",
            "R1.flag-locale-ascii",
            "R1.inline-unicode",
            "R1.invalid-escapes",
            "R1.error-object",
            "R1.escape",
        ],
        None,
    );
}

#[test]
fn b_all_match_consumers_byte_offsets_identity_and_text_control_match_python() {
    support::check(
        "matches_identity.py",
        MATCHES,
        &[
            "R2.binary-identity",
            "R2.range-match",
            "R2.range-search",
            "R2.range-fullmatch",
            "R2.module-match",
            "R2.missing",
            "R2.bad-group-index",
            "R2.bad-group-name",
            "R2.findall",
            "R2.finditer",
            "R2.clamped-ranges",
            "R2.empty-0",
            "R2.empty-1",
            "R2.empty-2",
            "R2.empty-3",
            "R2.whole-slice",
            "R2.anchors-backref",
            "R2.text-control",
        ],
        None,
    );
}

#[test]
fn c_template_domains_deferred_callback_errors_and_text_control_match_python() {
    support::check(
        "templates_callbacks.py",
        TEMPLATES,
        &[
            "R3.mixed-0",
            "R3.mixed-1",
            "R3.mixed-2",
            "R3.expand-0",
            "R3.expand-1",
            "R3.template-chain",
            "R3.invalid-0",
            "R3.invalid-1",
            "R3.invalid-2",
            "R3.invalid-3",
            "R3.invalid-4",
            "R3.template-order-0",
            "R3.template-order-1",
            "R3.subject-order",
            "R3.callback-bytes",
            "R3.no-callback-bytes",
            "R3.callback-none",
            "R3.no-callback-none",
            "R3.callback-str",
            "R3.no-callback-str",
            "R3.callback-empty-str",
            "R3.no-callback-empty-str",
            "R3.callback-int",
            "R3.no-callback-int",
            "R3.later-error",
            "R3.count",
            "R3.identity",
            "R3.empty-0",
            "R3.empty-1",
            "R3.empty-2",
            "R3.empty-3",
            "R3.text-control",
        ],
        None,
    );
}

#[test]
fn d_index_conversion_order_wide_bounds_and_complete_user_chain_match_python() {
    support::check(
        "binding_context.py",
        BINDING,
        &[
            "R4.index-order",
            "R4.index-events",
            "R4.count-order",
            "R4.count-events",
            "R4.bad-index",
            "R4.bad-index-events",
            "R4.index-identity",
            "R4.wide",
            "R4.overflow-pos",
            "R4.overflow-count",
            "R4.good-index",
            "R4.chain",
        ],
        Some(EXPECTED_TRACE),
    );
}

// 全部是用户源码帧；Pattern.search/sub为C内建，不需要删除re.py包装帧。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File binding_context.py, line 108, in <module>",
    "File binding_context.py, line 87, in index_outer",
    "File binding_context.py, line 83, in index_middle",
    "File binding_context.py, line 79, in __index__",
    "RuntimeError: index context",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File binding_context.py, line 110, in <module>",
    "File binding_context.py, line 103, in replace_outer",
    "File binding_context.py, line 99, in replace_middle",
    "File binding_context.py, line 95, in callback_middle",
    "File binding_context.py, line 91, in callback_leaf",
    "LookupError: callback chain failed",
];
