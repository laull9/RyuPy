//! 通用str.format四源应用：报告、真实协议、保存绑定/身份和完整未捕获链。
//! 四对CP/JIT，另计四个固定CP版本probe；pending长尾不被发现或偷计通过。
#[path = "support/string_format.rs"]
mod support;

use std::process::Output;

#[test]
fn string_format_field_report_matches_python() {
    support::equal(
        include_str!("../../../tests/fixtures/string_format/report.py"),
        "report.py",
        b"string-format-report-ok\n",
    );
}

#[test]
fn string_format_callback_protocol_matches_python() {
    support::equal(
        include_str!("../../../tests/fixtures/string_format/protocol.py"),
        "protocol.py",
        b"string-format-protocol-ok\n",
    );
}

#[test]
fn string_format_binding_identity_and_numeric_mir_match_python() {
    support::equal(
        include_str!("../../../tests/fixtures/string_format/binding_identity.py"),
        "binding_identity.py",
        b"string-format-binding-identity-ok\n",
    );
}

// 源码冻结的完整CP合同，必须先通过首次真实CP才允许目标编译；不是已执行golden。
fn error_reference(expected: &Output) {
    assert_eq!(expected.stdout, b"[('slot', 'spec'), 'fail', 'finally']\n");
    assert_eq!(
        support::trace_projection(expected),
        vec![
            "Traceback (most recent call last):",
            "File traceback.py, line 5, in <module>",
            "LookupError: template-cause",
            "The above exception was the direct cause of the following exception:",
            "Traceback (most recent call last):",
            "File traceback.py, line 29, in <module>",
            "RuntimeError: template-ambient",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File traceback.py, line 31, in <module>",
            "File traceback.py, line 26, in caller",
            "File traceback.py, line 20, in render",
            "File traceback.py, line 16, in __format__",
            "File traceback.py, line 11, in fail",
            "ValueError: template-body",
        ]
    );
}

#[test]
fn string_format_preserves_full_user_chain_and_finally() {
    let (_root, expected, actual) = support::execute(
        include_str!("../../../tests/fixtures/string_format/traceback.py"),
        "traceback.py",
        1,
        error_reference,
    );
    assert_eq!(actual.status.code(), Some(1));
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(
        support::trace_projection(&actual),
        support::trace_projection(&expected)
    );
}
