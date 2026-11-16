//! html.escape逐API真实源码差分：三个应用，固定CP先验，未支持项不计通过。
#[path = "html_escape_support/mod.rs"]
mod support;

const TEXT: &str = include_str!("../../../tests/fixtures/html_escape/text_binding.py");
const PROTOCOL: &str = include_str!("../../../tests/fixtures/html_escape/replace_protocol.py");
const ERRORS: &str = include_str!("../../../tests/fixtures/html_escape/errors_context.py");

#[test]
fn text_identity_subclasses_and_complete_argument_binding_match_python() {
    support::check(
        "text_binding.py",
        TEXT,
        &[
            "H1.text",
            "H1.binding",
            "H1.quote-values",
            "H1.identity",
            "H1.subclass-root",
            "H1.subclass-override",
            "H1.module-rebind",
            "H1.missing",
            "H1.quote-only",
            "H1.unknown",
            "H1.unknown-missing",
            "H1.too-many",
            "H1.duplicate-s",
            "H1.duplicate-quote",
            "H1.many-unknown",
            "H1.many-duplicate-s",
            "H1.many-duplicate-quote",
            "H1.duplicate-then-unknown",
            "H1.unknown-then-duplicate",
            "H1.argument-order",
        ],
        None,
    );
}

#[test]
fn dynamic_replace_lookup_return_identity_and_late_quote_truth_match_python() {
    support::check(
        "replace_protocol.py",
        PROTOCOL,
        &[
            "H2.property",
            "H2.getattr",
            "H2.property-again",
            "H2.false-list",
            "H2.false-none",
            "H2.true-dict",
            "H2.true-none",
            "H2.class-rebind",
            "H2.instance-callable",
            "H2.str-property",
            "H2.str-instance",
            "H2.quote-true",
            "H2.quote-false",
            "H2.quote-length",
            "H2.quote-default",
        ],
        None,
    );
}

#[test]
fn original_protocol_errors_and_complete_user_context_chain_match_python() {
    support::check(
        "errors_context.py",
        ERRORS,
        &[
            "H3.replace-1",
            "H3.replace-2",
            "H3.replace-3",
            "H3.replace-4",
            "H3.replace-5",
            "H3.early-skips-quote",
            "H3.getter",
            "H3.bool-marker",
            "H3.wrong-bool",
            "H3.negative-len",
            "H3.missing-first",
            "H3.noncallable",
            "H3.missing-second",
            "H3.missing-fourth",
            "H3.cause",
            "H3.context",
            "H3.chain-callback",
        ],
        Some(EXPECTED_TRACE),
    );
}

// 固定用户源码行号；CP多出的唯一html帧由实际绝对源码路径+line19精确投影。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File errors_context.py, line 162, in <module>",
    "ValueError: outer context",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File errors_context.py, line 164, in <module>",
    "File errors_context.py, line 157, in outer",
    "File errors_context.py, line 153, in middle",
    "File errors_context.py, line 145, in replace",
    "File errors_context.py, line 149, in leaf",
    "LookupError: replace chain failed",
];
