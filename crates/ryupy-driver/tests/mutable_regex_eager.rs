//! 阶段A三个候选应用：CP先守门，保原双流与完整错误链；未验证前不能计通过。
#[path = "mutable_regex_eager_support/mod.rs"]
mod support;

const EAGER: &str =
    include_str!("../../../tests/fixtures/mutable_regex_eager/eager_domains_live.py");
const ORDER: &str =
    include_str!("../../../tests/fixtures/mutable_regex_eager/release_order_frames.py");
// 第三个应用直接复用既有fixture，不篡改exact bytes/text/immutable finditer正例。
const EXACT: &str = include_str!("../../../tests/fixtures/bytes_regex/matches_identity.py");

#[test]
fn a_eager_carriers_identity_live_groups_and_short_leases_match_python() {
    support::check(
        "eager_domains_live.py",
        EAGER,
        &[
            "A1.search-bytes",
            "A1.match-full-bytes",
            "A1.findall-bytes",
            "A1.search-bytearray",
            "A1.match-full-bytearray",
            "A1.findall-bytearray",
            "A1.search-view_B",
            "A1.match-full-view_B",
            "A1.findall-view_B",
            "A1.search-view_c",
            "A1.match-full-view_c",
            "A1.findall-view_c",
            "A1.search-view_slice",
            "A1.match-full-view_slice",
            "A1.findall-view_slice",
            "A1.search-view_readonly",
            "A1.match-full-view_readonly",
            "A1.findall-view_readonly",
            "A1.carriers-released",
            "A1.live-initial",
            "A1.live-write",
            "A1.live-shrunk",
            "A1.live-empty",
            "A1.live-regrown",
            "A1.view-live",
            "A1.view-released",
            "A1.short-lease-match",
            "A1.short-lease-search",
            "A1.short-lease-fullmatch",
            "A1.short-lease-findall",
            "A1.bounds-match",
            "A1.bounds-search",
            "A1.bounds-fullmatch",
            "A1.empty-and-findall",
            "A1.complete",
        ],
        None,
    );
}

#[test]
fn b_release_index_order_and_complete_user_error_chain_match_python() {
    support::check(
        "release_order_frames.py",
        ORDER,
        &[
            "A2.released-metadata",
            "A2.released-group",
            "A2.released-named",
            "A2.released-groups",
            "A2.released-groupdict",
            "A2.released-expand",
            "A2.released-repr",
            "A2.released-search",
            "A2.released-bad-group",
            "A2.released-unlocks",
            "A2.released-empty-whole",
            "A2.released-empty-group",
            "A2.released-empty-marks",
            "A2.released-missing-default",
            "A2.pos-release-match",
            "A2.pos-events-match",
            "A2.pos-release-search",
            "A2.pos-events-search",
            "A2.pos-release-fullmatch",
            "A2.pos-events-fullmatch",
            "A2.pos-release-findall",
            "A2.pos-events-findall",
            "A2.group-shrink",
            "A2.group-release",
            "A2.group-release-marks",
            "A2.multigroup-interleaved",
            "A2.pos-exception-identity",
            "A2.group-exception-identity",
            "A2.bad-index-result",
            "A2.overflow-before-release",
            "A2.group-wide-before-release",
            "A2.protocol-complete",
            "A2.chain",
        ],
        Some(EXPECTED_TRACE),
    );
}

#[test]
fn c_existing_exact_bytes_and_text_controls_remain_unchanged() {
    support::check(
        "matches_identity.py",
        EXACT,
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

// 源码静态预期，尚非运行事实；CP完整链不符就停，不编译或启动JIT。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File release_order_frames.py, line 203, in <module>",
    "File release_order_frames.py, line 190, in index_outer",
    "File release_order_frames.py, line 186, in index_middle",
    "File release_order_frames.py, line 182, in __index__",
    "RuntimeError: frame index marker",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File release_order_frames.py, line 207, in <module>",
    "File release_order_frames.py, line 198, in group_outer",
    "File release_order_frames.py, line 194, in group_middle",
    "TypeError: expected string or bytes-like object, got 'memoryview'",
];
