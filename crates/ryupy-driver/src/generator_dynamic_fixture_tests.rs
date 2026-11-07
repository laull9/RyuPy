//! 完整fixture按真实driver编译/发射，Instance失败计划取Session而不假设CLI生成失败报告。
use super::*;

fn fixture(relative: &str) -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/cases/generator_dynamic")
        .join(relative)
}

#[test]
fn generator_dynamic_public_fixtures_compile_and_emit_under_both_policies() {
    for policy in [
        ModuleEnvironmentPolicy::ExplicitOnly,
        ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
    ] {
        for entry in [
            "default/observer_control.py",
            "default/entry.py",
            "frames/entry.py",
        ] {
            let mut session = Session {
                module_environment_policy: policy,
                ..Default::default()
            };
            let (linked, _, _) =
                compile_linked(&mut session, &fixture(entry), &[], false, None)
                    .unwrap_or_else(|error| {
                        panic!("{entry}: {error}: {}", session.render_diagnostics(false))
                    });
            assert!(!session.module_environment_report.is_empty());
            assert!(session.module_environment_report.iter().all(|module| {
                module.execution == "LegacySingleton" && module.compiled
            }));
            let options = ryupy_codegen_cil::EmitOptions {
                assembly_name: "generator_dynamic_fixture".into(),
                entry_source_path: Some(fixture(entry).to_string_lossy().into_owned()),
            };
            let bytes = ryupy_codegen_cil::emit_assembly(&linked, &options).unwrap();
            assert!(bytes.starts_with(b"MZ"));
        }
    }
}

#[test]
fn generator_dynamic_instance_negative_has_real_selected_plan_and_exact_guard() {
    let mut session = Session {
        instance_modules: vec!["subject".into()],
        ..Default::default()
    };
    assert!(
        compile_linked(
            &mut session,
            &fixture("instance_negative/entry.py"),
            &[],
            false,
            None,
        )
        .is_err()
    );
    let diagnostics = session.render_diagnostics(false);
    assert!(diagnostics.contains("实例模块首切暂不支持生成器或类体源码except handler"));
    let selected: Vec<_> = session
        .module_environment_report
        .iter()
        .filter(|module| module.module == "subject")
        .collect();
    assert_eq!(selected.len(), 1);
    assert_eq!(selected[0].execution, "InstanceGlobals");
    assert!(!selected[0].compiled);
    assert!(selected[0].ordinary_name_handlers);
}
