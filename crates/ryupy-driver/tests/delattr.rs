//! delattr最多三个CP-first应用；旧pending源逐字include，保留独立原流证据。
#[path = "delattr_support/mod.rs"]
mod support;

#[test]
fn delattr_public_contract() {
    support::check(
        "public_contract.py",
        include_str!("../../../tests/fixtures/delattr/public_contract.py"),
        &[
            "delete.identity",
            "delete.binding",
            "delete.class-function",
            "delete.properties",
            "delete.evaluation",
            "delete.paused",
            "delete.chain",
        ],
        Some(EXPECTED_TRACE),
    );
}

#[test]
fn delattr_unchanged_slice_contract() {
    support::check(
        "construct_indices_delattr.py",
        include_str!(
            "../../../tests/fixtures/slice_constructor/pending/construct_indices_delattr.py.pending"
        ),
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
fn delattr_unchanged_typing_io_contract() {
    support::check(
        "builtin_delattr.py",
        include_str!("../../../tests/fixtures/typing_io/pending/builtin_delattr.py.pending"),
        &[
            "I2.constructors",
            "I2.bindings",
            "I2.properties",
            "I2.root-layout",
            "I2.shared-site",
            "I2.property-order",
            "I2.closed-subscription",
            "I2.complete",
        ],
        None,
    );
}

// CP-first冻结后精确核对全部用户帧、cause/context和错误文本。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File public_contract.py, line 112, in <module>",
    "ValueError: delattr-cause",
    "The above exception was the direct cause of the following exception:",
    "Traceback (most recent call last):",
    "File public_contract.py, line 116, in <module>",
    "RuntimeError: delattr-ambient",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File public_contract.py, line 119, in <module>",
    "File public_contract.py, line 110, in delete_outer",
    "File public_contract.py, line 108, in delete_middle",
    "File public_contract.py, line 105, in chain_delete",
    "LookupError: delattr-deleter",
];
