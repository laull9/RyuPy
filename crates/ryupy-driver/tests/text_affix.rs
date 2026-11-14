//! str前后缀的四个真实CP/JIT应用与一个源码结构门；合法长尾独立pending，不计通过。
//! 每源先固定CP自证，完整双流/退出码和失败状态先归档，不隐藏重试或新增项目入口。
#[path = "text_affix_support/structure.rs"]
mod structure;
#[path = "text_affix_support/mod.rs"]
mod support;

#[test]
fn text_affix_window_scan_matches_python() {
    support::check(
        "window_scan.py",
        include_str!("../../../tests/fixtures/text_affix/window_scan.py"),
        &[
            "scan",
            "bounds",
            "empty",
            "unicode",
            "candidate",
            "text-affix-window-ok",
        ],
        None,
    );
}

#[test]
fn text_affix_binding_routes_and_capture_match_python() {
    support::check(
        "binding_routes.py",
        include_str!("../../../tests/fixtures/text_affix/binding_routes.py"),
        &[
            "direct",
            "saved",
            "prepared",
            "non-affix",
            "non-callable",
            "getter-error",
            "roots",
            "direct-zero-kw",
            "saved-zero-kw",
            "direct-four-kw",
            "saved-four-kw",
            "root-zero-kw",
            "root-four-kw",
            "root-missing-self",
            "root-wrong-self",
            "zero-arity",
            "four-arity",
            "text-affix-binding-ok",
        ],
        None,
    );
}

#[test]
fn text_affix_index_protocol_and_reentry_match_python() {
    support::check(
        "index_protocol.py",
        include_str!("../../../tests/fixtures/text_affix/index_protocol.py"),
        &[
            "order",
            "empty-tuple",
            "reentry",
            "late-slot",
            "user-error",
            "user-error",
            "bad-return",
            "no-index",
            "bad-candidate",
            "identity",
            "text-affix-protocol-ok",
        ],
        None,
    );
}

// 源码冻结的完整CP合同，父首次CP先验后才可构建；不能只比末条错误或删除用户帧。
const ERROR_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File traceback.py, line 5, in <module>",
    "LookupError: affix-cause",
    "The above exception was the direct cause of the following exception:",
    "Traceback (most recent call last):",
    "File traceback.py, line 34, in <module>",
    "RuntimeError: affix-ambient",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File traceback.py, line 36, in <module>",
    "File traceback.py, line 31, in caller",
    "File traceback.py, line 25, in match_window",
    "File traceback.py, line 16, in __index__",
    "File traceback.py, line 11, in fail",
    "ValueError: affix-index-body",
];

#[test]
fn text_affix_preserves_full_index_exception_chain() {
    support::check(
        "traceback.py",
        include_str!("../../../tests/fixtures/text_affix/traceback.py"),
        &["trace-events"],
        Some(ERROR_TRACE),
    );
}
