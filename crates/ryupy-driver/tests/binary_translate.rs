//! bytes/bytearray.translate三份公开入口应用；CP-first保原源、原始双流及完整用户错误链。
#[path = "binary_translate_support/mod.rs"]
mod support;

#[test]
fn binary_translate_table_delete_identity() {
    support::check(
        "table_delete_identity.py",
        include_str!("../../../tests/fixtures/binary_translate/table_delete_identity.py"),
        &[
            "translate.all-bytes",
            "translate.delete-before-map",
            "translate.identity",
            "translate.empty-and-unused-delete",
        ],
        None,
    );
}

#[test]
fn binary_translate_binding_alias_capture() {
    support::check(
        "binding_alias_capture.py",
        include_str!("../../../tests/fixtures/binary_translate/binding_alias_capture.py"),
        &[
            "translate.shared-buffer",
            "translate.saved-and-root",
            "translate.live-view",
            "translate.capture-before-yield",
            "translate.binding-order",
        ],
        None,
    );
}

#[test]
fn binary_translate_errors_chain() {
    let mut labels = vec!["translate.error"; 28];
    labels.extend(["translate.error-evaluation", "translate.chain"]);
    support::check(
        "errors_chain.py",
        include_str!("../../../tests/fixtures/binary_translate/errors_chain.py"),
        &labels,
        Some(&[
            "Traceback (most recent call last):",
            "File errors_chain.py, line 51, in <module>",
            "ValueError: translate-cause",
            "The above exception was the direct cause of the following exception:",
            "Traceback (most recent call last):",
            "File errors_chain.py, line 55, in <module>",
            "RuntimeError: translate-ambient",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File errors_chain.py, line 58, in <module>",
            "File errors_chain.py, line 48, in outer",
            "File errors_chain.py, line 46, in middle",
            "File errors_chain.py, line 44, in inner",
            "TypeError: a bytes-like object is required, not 'NoneType'",
        ]),
    );
}
