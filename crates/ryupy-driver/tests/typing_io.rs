//! typing.IO 四个真实 CP/JIT 应用草稿；正常 gate 注册，无批准变量/外置 golden。
//! 当前隔离准备目录未运行；须实体源审和批准执行后才能登记任何通过结果。
#[path = "typing_io_support/mod.rs"]
mod support;

#[test]
fn typing_io_roots_and_pass_methods() {
    support::check(
        "roots_calls.py",
        include_str!("../../../tests/fixtures/typing_io/roots_calls.py"),
        &[
            "I1.identity",
            "I1.inheritance",
            "I1.pass",
            "I1.methods",
            "I1.getters",
            "I1.complete",
        ],
        None,
    );
}

#[test]
fn typing_io_binding_properties_and_root_layout() {
    support::check(
        "binding_properties.py",
        include_str!("../../../tests/fixtures/typing_io/binding_properties.py"),
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

#[test]
fn typing_io_ordinary_subclasses_and_context_order() {
    support::check(
        "subclasses_context.py",
        include_str!("../../../tests/fixtures/typing_io/subclasses_context.py"),
        &[
            "I3.ordinary",
            "I3.late-binding",
            "I3.root-context",
            "I3.override-normal",
            "I3.override-error",
            "I3.nominal",
            "I3.complete",
        ],
        None,
    );
}

#[test]
fn typing_io_import_bindings_and_definition_module() {
    support::check(
        "entry.py",
        include_str!("../../../tests/fixtures/typing_io/modules/entry.py"),
        &[
            "I4.imports",
            "I4.definition-origin",
            "I4.provider-rebind",
            "I4.native-rebind",
            "I4.bound-origin",
            "I4.complete",
        ],
        Some(include_str!(
            "../../../tests/fixtures/typing_io/modules/io_provider.py"
        )),
    );
}
