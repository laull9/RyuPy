//! 与CLI相同的请求/真实图路径测试；只验证编译与PE，不冒充CLI子进程或CP执行。
//! 公开裸Session是明确Legacy对照，不能用编译e2e/项目runner的绿色替代Auto覆盖。
use super::*;

fn source(name: &str) -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/validation/generator_default/sources")
        .join(format!("{name}.py"))
}
fn original_b() -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/cases/generator_dynamic/frames/entry.py")
}
fn compiled(session: &mut Session, path: &Path) -> ryupy_mir::LinkedProgram {
    compile_linked(session, path, &[], false, None)
        .unwrap_or_else(|e| panic!("{e}: {}", session.render_diagnostics(false)))
        .0
}

#[test]
fn explicit_standalone_selects_actual_proof_and_reuse_never_retains_old_selection() {
    let mut session = Session {
        trace_entry: TraceEntry::StandaloneMain,
        ..Default::default()
    };
    for (path, expected) in [
        (original_b(), Some(1)),
        (source("unused_identity"), None),
        (source("no_need"), None),
        (original_b(), Some(1)),
    ] {
        let program = compiled(&mut session, &path);
        assert_eq!(
            ryupy_mir::generator_product::validate_emission(&program),
            Ok(expected)
        );
        assert!(!session.diagnostics.has_errors());
        assert_eq!(session.module_environment_report.len(), 1);
    }
}

#[test]
fn no_demand_and_legal_outside_sources_keep_legacy_without_new_diagnostics() {
    for name in [
        "no_need",
        "static_generator",
        "unused_identity",
        "getter_handler",
        "helper_call",
        "nested_defaults_metadata",
        "send_throw_close",
        "yield_from_finally",
        "budget_set",
        "metadata_read",
    ] {
        let mut auto = Session {
            trace_entry: TraceEntry::StandaloneMain,
            ..Default::default()
        };
        let actual = compiled(&mut auto, &source(name));
        assert_eq!(
            ryupy_mir::generator_product::validate_emission(&actual),
            Ok(None),
            "{name}"
        );
        assert!(!auto.diagnostics.has_errors());
        let old = compiled(&mut Session::default(), &source(name));
        // 同候选明确Legacy API身份，不声称重新运行了历史旧CLI。
        assert_eq!(format!("{actual:?}"), format!("{old:?}"), "{name}");
    }
}

#[test]
fn require_v1_is_not_auto_and_dump_never_inherits_standalone_auto() {
    let mut required = Session {
        trace_product_v1: true,
        trace_entry: TraceEntry::StandaloneMain,
        ..Default::default()
    };
    assert!(compile_linked(&mut required, &source("unused_identity"), &[], false, None).is_err());
    assert!(required.render_diagnostics(false).contains("E0310"));
    let mut auto = Session {
        trace_entry: TraceEntry::StandaloneMain,
        ..Default::default()
    };
    let plain = dump(&mut auto, &original_b(), Stage::Mir).unwrap();
    let old = dump(&mut Session::default(), &original_b(), Stage::Mir).unwrap();
    assert_eq!(plain, old);
}

#[test]
fn actual_graph_boundary_cannot_be_overridden_by_standalone_intent() {
    let mut session = Session {
        trace_entry: TraceEntry::StandaloneMain,
        ..Default::default()
    };
    let file = session.sources.add("boundary.py", "");
    for (count, entry, resource) in [(2, 0, false), (1, 0, true), (1, 1, false)] {
        assert_eq!(
            trace_product::graph(&mut session, file, count, entry, resource).unwrap(),
            trace_product::Request::Legacy
        );
    }
    session.fixed_binding_build = true;
    assert_eq!(
        trace_product::graph(&mut session, file, 1, 0, false).unwrap(),
        trace_product::Request::Legacy
    );
    session.fixed_binding_build = false;
    session.include_modules.push("included".into());
    assert_eq!(
        trace_product::graph(&mut session, file, 1, 0, false).unwrap(),
        trace_product::Request::Legacy
    );
    session.include_modules.clear();
    session.instance_modules.push("__main__".into());
    assert_eq!(
        trace_product::graph(&mut session, file, 1, 0, false).unwrap(),
        trace_product::Request::Legacy
    );
}
