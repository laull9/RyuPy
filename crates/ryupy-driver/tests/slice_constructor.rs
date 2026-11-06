//! slice公开类型的三个CP-first入口，完整保存源、CP与JIT原流及用户异常链。
#[path = "slice_constructor_support/mod.rs"]
mod support;

#[test]
fn slice_constructor_identity_binding_and_wide_indices() {
    support::check(
        "construct_indices.py",
        include_str!("../../../tests/fixtures/slice_constructor/construct_indices.py"),
        &[
            "public.identity",
            "public.fields",
            "public.wide",
            "bind.constructor-empty",
            "bind.constructor-many",
            "bind.constructor-keyword",
            "bind.constructor-mixed",
            "bind.indices-empty",
            "bind.indices-many",
            "bind.indices-keyword",
            "bind.root-empty",
            "bind.root-self",
            "bind.root-length",
            "bind.root-many",
            "bind.root-keyword",
            "bind.length-none",
            "bind.length-negative",
            "bind.step-zero",
            "bind.eq-empty",
            "bind.eq-many",
            "bind.eq-keyword",
            "bind.hash-many",
            "bind.repr-many",
            "public.bindings-complete",
            "public.saved-paused",
            "public.non-subclassable",
            "public.direct-non-subclassable",
            "public.method-identity",
        ],
        None,
    );
}

#[test]
fn slice_constructor_comparison_hash_callbacks_and_chain() {
    support::check(
        "compare_callbacks.py",
        include_str!("../../../tests/fixtures/slice_constructor/compare_callbacks.py"),
        &[
            "public.compare",
            "public.short-circuit",
            "public.hash",
            "public.indices-order",
            "public.indices-stop",
            "public.exception-identity",
            "public.chain",
            "public.finally",
        ],
        Some(EXPECTED_TRACE),
    );
}

#[test]
fn unchanged_bytearray_slice_constructor_fixture() {
    support::check(
        "basics_slice_constructor.py",
        include_str!(
            "../../../tests/fixtures/slice_constructor/../bytearray_slice/pending/basics_slice_constructor.py.pending"
        ),
        &[
            "slice.basics",
            "slice.wide",
            "slice.self",
            "slice.alias",
            "slice.mismatch",
            "slice.lock",
            "slice.lock",
            "slice.release",
        ],
        None,
    );
}

// 冻结CP3.12.13全部用户帧及cause/context，不过滤未知stderr行。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File compare_callbacks.py, line 87, in <module>",
    "ValueError: slice-cause",
    "The above exception was the direct cause of the following exception:",
    "Traceback (most recent call last):",
    "File compare_callbacks.py, line 91, in <module>",
    "RuntimeError: slice-ambient",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File compare_callbacks.py, line 95, in <module>",
    "File compare_callbacks.py, line 85, in slice_outer",
    "File compare_callbacks.py, line 83, in slice_middle",
    "File compare_callbacks.py, line 81, in __index__",
    "LookupError: slice-index",
];
