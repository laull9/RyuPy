//! zip_longest通用惰性类型：三个真实CP-first源码应用；pending不注册、不计通过。
#[path = "zip_longest_support/mod.rs"]
mod support;

const CONSTRUCT: &str = include_str!("../../../tests/fixtures/zip_longest/construct_values.py");
const PROTOCOL: &str = include_str!("../../../tests/fixtures/zip_longest/protocol_state.py");
const ERRORS: &str = include_str!("../../../tests/fixtures/zip_longest/errors_context.py");

#[test]
fn construction_binding_type_identity_and_fill_references_match_python() {
    support::check(
        "construct_values.py",
        CONSTRUCT,
        &[
            "Z1.type",
            "Z1.zero",
            "Z1.empty",
            "Z1.all-empty",
            "Z1.one",
            "Z1.positional-source",
            "Z1.unequal",
            "Z1.alias",
            "Z1.fill-reference",
            "Z1.fill-no-protocol",
            "Z1.fill-read",
            "Z1.fill-write",
            "Z1.fill-delete",
            "Z1.construct-order",
            "Z1.empty-next-order",
            "Z1.unknown",
            "Z1.strict",
            "Z1.fill-unknown",
            "Z1.two-unknown",
            "Z1.iter-error-runtime",
            "Z1.iter-error-stop",
            "Z1.invalid-int",
            "Z1.invalid-none",
            "Z1.invalid-result",
        ],
        None,
    );
}

#[test]
fn per_column_capture_sequence_slots_and_bounded_reentry_match_python() {
    support::check(
        "protocol_state.py",
        PROTOCOL,
        &[
            "Z2.order",
            "Z2.alias-direct",
            "Z2.alias-iter-result",
            "Z2.slot-retirement",
            "Z2.special-lookup",
            "Z2.slot-rebind",
            "Z2.stop-subclass",
            "Z2.stop-construction",
            "Z2.stop-format-error",
            "Z2.sequence-index",
            "Z2.sequence-stop",
            "Z2.sequence-stop-subclass",
            "Z2.getter-fallback",
            "Z2.property-slots",
            "Z2.reentry",
            "Z2.list-mutation",
        ],
        None,
    );
}

#[test]
fn permanent_error_state_and_complete_constructor_next_context_match_python() {
    support::check(
        "errors_context.py",
        ERRORS,
        &[
            "Z3.next-error-0",
            "Z3.next-error-1",
            "Z3.next-error-2",
            "Z3.sequence-error",
            "Z3.iter-none",
            "Z3.property-none",
            "Z3.method-error",
            "Z3.context-stop",
            "Z3.context-index",
            "Z3.chain-constructor",
            "Z3.chain-body",
        ],
        Some(EXPECTED_TRACE),
    );
}

// 这两段完整用户链先由首次CP验真，未通过不能启动build；没有内部库帧过滤。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File errors_context.py, line 194, in <module>",
    "File errors_context.py, line 172, in construct_outer",
    "File errors_context.py, line 168, in construct_middle",
    "File errors_context.py, line 164, in __iter__",
    "ValueError: constructor context",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File errors_context.py, line 196, in <module>",
    "File errors_context.py, line 190, in next_outer",
    "File errors_context.py, line 186, in next_middle",
    "File errors_context.py, line 181, in __next__",
    "LookupError: next chain failed",
];
