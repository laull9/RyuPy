//! codecs BOM 有限合同与同名源码遮蔽：两个正常 CP-first/JIT 差分。
#[path = "codecs_bom_support/mod.rs"]
mod support;

#[test]
fn codecs_bom_values_aliases_bytes_and_rebinding() {
    support::check(
        "constants.py",
        include_str!("../../../tests/fixtures/codecs_bom/constants.py"),
        &[
            "B1.values",
            "B1.aliases",
            "B1.immutable",
            "B1.rebind",
            "B1.unknown-attribute",
            "B1.unknown-import",
            "B1.complete",
        ],
        None,
    );
}

#[test]
fn source_codecs_shadows_native_module() {
    support::check(
        "entry.py",
        include_str!("../../../tests/fixtures/codecs_bom/shadow/entry.py"),
        &["B2.source", "B2.complete"],
        Some(include_str!(
            "../../../tests/fixtures/codecs_bom/shadow/codecs.py"
        )),
    );
}
