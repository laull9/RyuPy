//! 真实dict派生的CP-first公开driver应用；合法暂缓域独立保留，不能把NI计成功。
#[path = "dict_subclass_support/mod.rs"]
mod support;

#[test]
fn dict_subclass_consumers() {
    support::check(
        "consumers.py",
        include_str!("../../../tests/fixtures/dict_subclass/consumers.py"),
        &[
            "dict-subclass.mapping-consumers",
            "dict-subclass.json-policy-cycle",
            "dict-subclass.dataclass-default",
        ],
        include_bytes!("../../../tests/fixtures/dict_subclass/golden/consumers.stdout"),
        None,
    );
}

#[test]
fn dict_subclass_custom_policy() {
    support::check(
        "custom_policy.py",
        include_str!("../../../tests/fixtures/dict_subclass/custom_policy.py"),
        &[
            "dict-subclass.policy-root",
            "dict-subclass.missing-lookup",
            "dict-subclass.fromkeys-init",
        ],
        include_bytes!("../../../tests/fixtures/dict_subclass/golden/custom_policy.stdout"),
        None,
    );
}

#[test]
fn dict_subclass_records() {
    support::check(
        "records.py",
        include_str!("../../../tests/fixtures/dict_subclass/records.py"),
        &[
            "dict-subclass.records",
            "dict-subclass.reinit",
            "dict-subclass.prefix",
            "dict-subclass.copy-union-methods",
        ],
        include_bytes!("../../../tests/fixtures/dict_subclass/golden/records.stdout"),
        None,
    );
}

#[test]
fn dict_subclass_scalar_comparison() {
    support::check(
        "scalar_comparison.py",
        include_str!("../../../tests/fixtures/dict_subclass/scalar_comparison.py"),
        &["dict-subclass.scalar-keys", "dict-subclass.value-equality"],
        include_bytes!("../../../tests/fixtures/dict_subclass/golden/scalar_comparison.stdout"),
        None,
    );
}

#[test]
fn dict_subclass_views() {
    support::check(
        "views.py",
        include_str!("../../../tests/fixtures/dict_subclass/views.py"),
        &[
            "dict-subclass.live-views",
            "dict-subclass.size-sticky",
            "dict-subclass.size-sticky",
            "dict-subclass.size-sticky",
            "dict-subclass.size-sticky",
        ],
        include_bytes!("../../../tests/fixtures/dict_subclass/golden/views.stdout"),
        None,
    );
}

#[test]
fn dict_subclass_consumer_adapters() {
    support::check(
        "consumer_adapters.py",
        include_str!("../../../tests/fixtures/dict_subclass/consumer_adapters.py"),
        &[
            "json",
            "jsonindent",
            "emptyjson",
            "counterempty",
            "counterfull",
            "counterminus",
            "defaultdict",
            "proxyget",
            "proxyitem",
            "proxykeys",
            "percent",
        ],
        include_bytes!("../../../tests/fixtures/dict_subclass/golden/consumer_adapters.stdout"),
        None,
    );
}
