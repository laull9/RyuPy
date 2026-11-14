//! bytes/bytearray八项ASCII分类的三个CP-first/JIT应用；候选待审，不代表已运行。
#[path = "binary_classification_support/mod.rs"]
mod support;

#[test]
fn a_all_bytes_four_routes_and_mutable_captures_match_python() {
    support::check(
        "values_and_live.py",
        include_str!("../../../tests/fixtures/binary_classification/values_and_live.py"),
        &[
            "V.single-bytes-isalpha",
            "V.single-bytes-isalnum",
            "V.single-bytes-isdigit",
            "V.single-bytes-isspace",
            "V.single-bytes-isascii",
            "V.single-bytes-islower",
            "V.single-bytes-isupper",
            "V.single-bytes-istitle",
            "V.single-bytearray-isalpha",
            "V.single-bytearray-isalnum",
            "V.single-bytearray-isdigit",
            "V.single-bytearray-isspace",
            "V.single-bytearray-isascii",
            "V.single-bytearray-islower",
            "V.single-bytearray-isupper",
            "V.single-bytearray-istitle",
            "V.mixed-bytes-isalpha",
            "V.mixed-bytes-isalnum",
            "V.mixed-bytes-isdigit",
            "V.mixed-bytes-isspace",
            "V.mixed-bytes-isascii",
            "V.mixed-bytes-islower",
            "V.mixed-bytes-isupper",
            "V.mixed-bytes-istitle",
            "V.mixed-bytearray-isalpha",
            "V.mixed-bytearray-isalnum",
            "V.mixed-bytearray-isdigit",
            "V.mixed-bytearray-isspace",
            "V.mixed-bytearray-isascii",
            "V.mixed-bytearray-islower",
            "V.mixed-bytearray-isupper",
            "V.mixed-bytearray-istitle",
            "V.live-isalpha",
            "V.live-isalnum",
            "V.live-isdigit",
            "V.live-isspace",
            "V.live-isascii",
            "V.live-islower",
            "V.live-isupper",
            "V.live-istitle",
            "V.view-alias",
            "V.saved-receiver",
            "V.old-methods",
            "V.complete",
        ],
        None,
    );
}

#[test]
fn b_binding_priorities_effects_and_unrelated_objects_match_python() {
    support::check(
        "binding_and_effects.py",
        include_str!("../../../tests/fixtures/binary_classification/binding_and_effects.py"),
        &[
            "B.matrix-bytes-isalpha",
            "B.matrix-bytes-isalnum",
            "B.matrix-bytes-isdigit",
            "B.matrix-bytes-isspace",
            "B.matrix-bytes-isascii",
            "B.matrix-bytes-islower",
            "B.matrix-bytes-isupper",
            "B.matrix-bytes-istitle",
            "B.matrix-bytearray-isalpha",
            "B.matrix-bytearray-isalnum",
            "B.matrix-bytearray-isdigit",
            "B.matrix-bytearray-isspace",
            "B.matrix-bytearray-isascii",
            "B.matrix-bytearray-islower",
            "B.matrix-bytearray-isupper",
            "B.matrix-bytearray-istitle",
            "B.live-direct",
            "B.live-saved",
            "B.live-root-direct",
            "B.live-root-saved",
            "B.reject-direct",
            "B.reject-saved",
            "B.reject-root-direct",
            "B.reject-root-saved",
            "B.no-coercion",
            "B.argument-identity-direct",
            "B.argument-identity-saved",
            "B.argument-identity-root-direct",
            "B.argument-identity-root-saved",
            "B.saved-root",
            "B.ordinary-site",
            "B.complete",
        ],
        None,
    );
}

#[test]
fn c_fresh_binding_errors_and_complete_user_context_chain_match_python() {
    support::check(
        "error_chain.py",
        include_str!("../../../tests/fixtures/binary_classification/error_chain.py"),
        &[
            "T.caught-direct",
            "T.caught-saved",
            "T.caught-root-direct",
            "T.caught-root-saved",
            "T.chain",
        ],
        Some(EXPECTED_TRACE),
    );
}

// 静态逐行预期，首次CP必须整链吻合再启动编译；未知/额外用户帧不能删除。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File error_chain.py, line 65, in <module>",
    "LookupError: ascii-cause",
    "The above exception was the direct cause of the following exception:",
    "Traceback (most recent call last):",
    "File error_chain.py, line 71, in <module>",
    "RuntimeError: ascii-ambient",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File error_chain.py, line 74, in <module>",
    "File error_chain.py, line 51, in argument_outer",
    "File error_chain.py, line 47, in argument_middle",
    "File error_chain.py, line 43, in argument_leaf",
    "ValueError: ascii-argument-body",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File error_chain.py, line 76, in <module>",
    "File error_chain.py, line 60, in binder_outer",
    "File error_chain.py, line 56, in binder_middle",
    "TypeError: descriptor 'istitle' for 'bytearray' objects doesn't apply to a 'bytes' object",
];
