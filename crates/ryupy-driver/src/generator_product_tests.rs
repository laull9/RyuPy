//! 正常driver的显式产品选择与整图拒绝；只核实际PE发射，真实执行另由产品门负责。
use super::*;

fn fixture(name: &str) -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/validation/generator_product/goldens")
        .join(name)
}

#[test]
fn explicit_product_driver_reaches_normal_pe_emitter() {
    for entry in [
        fixture("a_all_miss.py"),
        fixture("hot_same_getter.py"),
        Path::new(env!("CARGO_MANIFEST_DIR"))
            .join("../../tests/cases/generator_dynamic/frames/entry.py"),
    ] {
        let mut session = Session {
            trace_product_v1: true,
            ..Default::default()
        };
        let (linked, _, _) = compile_linked(&mut session, &entry, &[], false, None)
            .unwrap_or_else(|error| panic!("{error}: {}", session.render_diagnostics(false)));
        assert_eq!(
            ryupy_mir::generator_product::validate_emission(&linked),
            Ok(Some(1))
        );
        let bytes = ryupy_codegen_cil::emit_assembly(
            &linked,
            &ryupy_codegen_cil::EmitOptions {
                assembly_name: "trace_product_driver".into(),
                entry_source_path: Some(entry.to_string_lossy().into_owned()),
            },
        )
        .unwrap();
        assert!(bytes.starts_with(b"MZ"));
    }
}

#[test]
fn original_c_d_reject_before_normal_and_native_emission() {
    for name in ["c_nested_same_object.py", "d_same_object_escape.py"] {
        for aot in [false, true] {
            let mut session = Session {
                trace_product_v1: true,
                ..Default::default()
            };
            assert!(compile_linked(&mut session, &fixture(name), &[], aot, None).is_err());
            let diagnostic = session.render_diagnostics(false);
            assert!(diagnostic.contains("E0310"), "{diagnostic}");
            assert!(diagnostic.contains("trace V1 整产物拒绝："), "{diagnostic}");
        }
    }
}

#[test]
fn product_is_not_inferred_from_generator_source_or_default_session() {
    let mut session = Session::default();
    assert!(!session.trace_product_v1);
    let (linked, _, _) =
        compile_linked(&mut session, &fixture("a_all_miss.py"), &[], false, None).unwrap();
    assert!(linked.trace_product.is_none());
    assert_eq!(
        ryupy_mir::generator_product::validate_emission(&linked),
        Ok(None)
    );
}
