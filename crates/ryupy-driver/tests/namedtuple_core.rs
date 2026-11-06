//! 通用namedtuple五组真实源码差分；逐API锁身份、求值顺序、tuple协议与来源。
//! CP先守门，编译/JIT原流留存；Static来源另只作结构检查，不冒第六应用通过。
#[path = "namedtuple_core_support/mod.rs"]
mod support;

const IDENTITY: &str = include_str!("../../../tests/fixtures/namedtuple_core/identity_binding.py");
const FACTORY: &str = include_str!("../../../tests/fixtures/namedtuple_core/factory_rules.py");
const TUPLE: &str = include_str!("../../../tests/fixtures/namedtuple_core/tuple_protocol.py");
const HELPERS: &str = include_str!("../../../tests/fixtures/namedtuple_core/helpers.py");
const ORIGIN: &str = include_str!("../../../tests/fixtures/namedtuple_core/module_origin/entry.py");
const PROVIDER: &str =
    include_str!("../../../tests/fixtures/namedtuple_core/module_origin/origin_provider.py");

#[test]
fn fresh_classes_binding_default_references_and_readonly_fields_match_python() {
    support::success(
        "identity_binding.py",
        IDENTITY,
        &[],
        &[
            "B1.identity",
            "B1.metadata",
            "B1.instance-name",
            "B1.instance-qualname",
            "B1.binding",
            "B1.missing-one",
            "B1.missing-many",
            "B1.too-many-defaults",
            "B1.too-many-required",
            "B1.duplicate",
            "B1.unknown",
            "B1.hidden-cls",
            "B1.defaults",
            "B1.references",
            "B1.repr",
            "B1.descriptor",
            "B1.descriptor-short",
            "B1.descriptor-nontuple",
            "B1.set-field",
            "B1.delete-field",
            "B1.set-extra",
            "B1.delete-extra",
            "B1.set-item",
            "B1.no-dict",
            "B1.readonly-state",
        ],
    );
}

#[test]
fn factory_validation_iteration_conversion_truth_order_and_explicit_module_match_python() {
    support::success(
        "factory_rules.py",
        FACTORY,
        &[],
        &[
            "B2.sources",
            "B2.default-forms",
            "B2.shadow",
            "B2.rename",
            "B2.typename-invalid",
            "B2.typename-keyword",
            "B2.field-invalid",
            "B2.field-keyword",
            "B2.field-underscore",
            "B2.field-duplicate",
            "B2.name-before-default-count",
            "B2.defaults-too-many",
            "B2.factory-missing",
            "B2.factory-one-missing",
            "B2.factory-too-many",
            "B2.factory-duplicate",
            "B2.factory-unknown",
            "B2.order-ok",
            "B2.order-bad-field",
            "B2.order-fields",
            "B2.order-field-str",
            "B2.order-typename",
            "B2.order-defaults",
            "B2.order-extra-defaults",
            "B2.order-binding",
            "B2.rename-repeated",
            "B2.rename-changed",
            "B2.rename-changed-events",
            "B2.module-0",
            "B2.module-1",
            "B2.module-2",
            "B2.module-3",
            "B2.module-4",
            "B2.module-5",
        ],
    );
}

#[test]
fn tuple_interactions_key_identity_and_method_capture_order_match_python() {
    support::success(
        "tuple_protocol.py",
        TUPLE,
        &[],
        &[
            "B3.sequence",
            "B3.out-of-range",
            "B3.index-missing",
            "B3.comparison",
            "B3.keys",
            "B3.unhashable",
            "B3.plain-results",
            "B3.consumers",
            "B3.format-nonempty",
            "B3.same-callsite",
            "B3.missing-before-argument",
            "B3.noncallable-after-argument",
            "B3.argument-events",
            "B3.prepared",
        ],
    );
}

#[test]
fn helper_binding_complete_consumption_and_result_identity_match_python() {
    support::success(
        "helpers.py",
        HELPERS,
        &[],
        &[
            "B4.make-forms",
            "B4.make-short",
            "B4.make-long",
            "B4.make-length-events",
            "B4.make-original-error",
            "B4.make-failure-events",
            "B4.make-missing",
            "B4.make-extra",
            "B4.make-keyword",
            "B4.replace",
            "B4.replace-unknown",
            "B4.replace-positional",
            "B4.replace-self",
            "B4.asdict",
            "B4.asdict-extra",
            "B4.asdict-keyword",
            "B4.getnewargs",
            "B4.getnewargs-extra",
            "B4.empty",
            "B4.cross-equal",
            "B4.cross-unknown",
            "B4.cross-short",
            "B4.cross-short-unused",
            "B4.cross-long",
            "B4.cross-long-before-unknown",
            "B4.cross-content",
            "B4.cross-repr",
            "B4.cross-repr-short",
            "B4.cross-repr-long",
        ],
    );
}

#[test]
fn actual_dynamic_callers_aliases_closures_and_generator_origins_match_python() {
    support::origin_success(
        "origin_entry.py",
        ORIGIN,
        &[("origin_provider.py", PROVIDER)],
        &[
            "B5.module-top",
            "B5.definition-capture",
            "B5.current-member",
            "B5.callee-freeze",
            "B5.generator",
            "B5.generator-unwind",
            "B5.falsey-0",
            "B5.falsey-1",
            "B5.falsey-2",
            "B5.falsey-3",
            "B5.falsey-4",
            "B5.deleted-member",
            "B5.module-truth",
            "B5.module-truth-error",
            "B5.validation-before-module",
            "B5.explicit-0",
            "B5.explicit-1",
            "B5.explicit-2",
            "B5.explicit-3",
            "B5.explicit-4",
            "B5.restored",
        ],
    );
}
