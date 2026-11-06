//! 精确 dict 更新的 CP-first 公开 driver 回归：原始反例、消费协议及完整异常链。
#[path = "dict_update_support/mod.rs"]
mod support;

#[test]
fn dict_update_errors_chain() {
    support::check(
        "errors_chain.py",
        include_str!("../../../tests/fixtures/dict_update/errors_chain.py"),
        &["dict-update.chain-identity"],
        include_bytes!("../../../tests/fixtures/dict_update/golden/errors_chain.stdout"),
        Some(include_str!(
            "../../../tests/fixtures/dict_update/golden/errors_chain.trace"
        )),
    );
}

#[test]
fn dict_update_generator_prefix() {
    support::check(
        "generator_prefix.py",
        include_str!("../../../tests/fixtures/dict_update/generator_prefix.py"),
        &["yield-first", "before-raise", "caught", "after"],
        include_bytes!("../../../tests/fixtures/dict_update/golden/generator_prefix.stdout"),
        None,
    );
}

#[test]
fn dict_update_invalid_pair() {
    support::check(
        "invalid_pair.py",
        include_str!("../../../tests/fixtures/dict_update/invalid_pair.py"),
        &["yield-first", "yield-invalid", "caught", "after"],
        include_bytes!("../../../tests/fixtures/dict_update/golden/invalid_pair.stdout"),
        None,
    );
}

#[test]
fn dict_update_mapping_contract() {
    support::check(
        "mapping_contract.py",
        include_str!("../../../tests/fixtures/dict_update/mapping_contract.py"),
        &["keys", "getitem", "getitem", "caught", "after"],
        include_bytes!("../../../tests/fixtures/dict_update/golden/mapping_contract.stdout"),
        None,
    );
}

#[test]
fn dict_update_protocol_contract() {
    support::check(
        "protocol_contract.py",
        include_str!("../../../tests/fixtures/dict_update/protocol_contract.py"),
        &[
            "outer-no-hint",
            "pair-full-materialize",
            "outer-getitem-fallback",
            "inner-getitem-fallback",
            "keys-get-attributeerror",
            "keys-get-other-error",
            "keys-call-attributeerror",
            "keys-noncallable",
            "pair-typeerror-rewrite",
            "pair-other-error",
            "keys-materialization",
            "pair-hint-typeerror",
            "pair-next-typeerror",
            "keys-failure-no-new-prefix",
            "saved-mapping-keywords",
            "constructor-mapping-keywords",
            "constructor-pair-sequence",
            "constructor-source-failure",
            "user-iterator-mapping-priority",
        ],
        include_bytes!("../../../tests/fixtures/dict_update/golden/protocol_contract.stdout"),
        None,
    );
}

#[test]
fn dict_update_update_binding() {
    support::check(
        "update_binding.py",
        include_str!("../../../tests/fixtures/dict_update/update_binding.py"),
        &["empty", "kwargs", "arity-error", "after", "constructor"],
        include_bytes!("../../../tests/fixtures/dict_update/golden/update_binding.stdout"),
        None,
    );
}
