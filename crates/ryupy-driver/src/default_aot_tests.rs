//! 默认联合计划纯门与实际小图编译；不把普通managed执行当kind2 Native。
use super::*;
use std::path::PathBuf;
use std::sync::atomic::{AtomicUsize, Ordering};
static NEXT: AtomicUsize = AtomicUsize::new(0);
fn root() -> PathBuf {
    let p = std::env::temp_dir().join(format!(
        "ryupy-default-aot-{}-{}-{}",
        std::process::id(),
        std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&p).unwrap();
    p
}
#[test]
fn eligibility_keeps_explicit_and_binding_paths() {
    let mut s = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
    assert!(eligible(&s, true, false));
    assert!(!eligible(&s, false, false));
    assert!(eligible(&s, true, true));
    s.instance_modules.push("html.parser".into());
    assert!(!eligible(&s, true, false));
    s.instance_modules.clear();
    s.include_modules.push("x".into());
    assert!(eligible(&s, true, false));
    s.distribution_metadata = Some(PathBuf::from("snapshot.json"));
    assert!(eligible(&s, true, true));
    s.include_modules.clear();
    s.distribution_metadata = None;
    s.fixed_binding_build = true;
    assert!(!eligible(&s, true, false));
    s.default_source_state.binding_catalog = true;
    assert!(eligible(&s, true, true));
    s.distribution_metadata = Some(PathBuf::from("binding-snapshot.json"));
    assert!(!eligible(&s, true, true));
    s.distribution_metadata = None;
    s.include_modules.push("binding-extra".into());
    assert!(!eligible(&s, true, true));
    s.include_modules.clear();
    s.module_environment_policy = ModuleEnvironmentPolicy::ExplicitOnly;
    assert!(!eligible(&s, true, true));
}
#[test]
fn real_optional_graph_has_contiguous_emitted_ids_and_virtual_locations() {
    let root = root();
    std::fs::write(
        root.join("entry.py"),
        "import importlib.util\nprint(importlib.util.find_spec('subject'))\n",
    )
    .unwrap();
    std::fs::write(root.join("subject.py"), "value = 7\n").unwrap();
    let mut s = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
    s.default_source_state.aot_request = true;
    s.default_source_state.output = Some(root.join("out"));
    let result =
        crate::compile_linked_locations(&mut s, &root.join("entry.py"), &[], true, None, true);
    let (linked, sources, plan) = result.unwrap();
    assert_eq!(sources.len(), linked.modules.len());
    for (i, source) in sources.iter().enumerate() {
        assert_eq!(source.module_id, i as u32);
    }
    let plan = plan.unwrap();
    assert!(plan.automatic.is_some());
    assert!(
        plan.locations
            .iter()
            .all(|p| p.relative_file.starts_with(crate::resources::PREFIX))
    );
}
#[path = "default_aot_oracle_tests.rs"]
mod oracle;

#[test]
#[ignore = "真实工作负载显式窗口；只编译/发射，不publish或执行"]
fn default_aot_real_graph_emit() {
    oracle::emit_real_graph();
}

#[test]
fn deferred_hole_keeps_real_source_snapshot_and_compiled_ids() {
    let root = root();
    std::fs::write(root.join("entry.py"),"import importlib.util\nimportlib.util.find_spec('a')\nimportlib.util.find_spec('bad')\nimportlib.util.find_spec('c')\n").unwrap();
    std::fs::write(root.join("a.py"), "x=1\n").unwrap();
    std::fs::write(root.join("bad.py"), "def :\n").unwrap();
    std::fs::write(root.join("c.py"), "x=3\n").unwrap();
    let mut s = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
    s.default_source_state.aot_request = true;
    s.default_source_state.output = Some(root.join("out"));
    let (linked, sources, plan) =
        crate::compile_linked_locations(&mut s, &root.join("entry.py"), &[], true, None, true)
            .unwrap();
    let plan = plan.unwrap();
    let audit = plan.automatic.as_ref().unwrap();
    assert_eq!(
        audit
            .source_to_emitted
            .iter()
            .filter(|v| v.is_none())
            .count(),
        1
    );
    assert!(sources.iter().all(|s| s.module_name != "bad"));
    assert_eq!(sources.len(), linked.modules.len());
    for (i, source) in sources.iter().enumerate() {
        assert_eq!(source.module_id, i as u32);
    }
    let frozen = root.join("frozen");
    std::fs::create_dir(&frozen).unwrap();
    let staged = plan.stage(&frozen).unwrap();
    assert_eq!(staged.locations.len(), sources.len());
    std::fs::write(root.join("bad.py"), "def changed(:\n").unwrap();
    let changed = root.join("changed");
    std::fs::create_dir(&changed).unwrap();
    assert!(plan.stage(&changed).is_err());
}

#[test]
fn no_optional_demand_preserves_legacy_aot_and_explicit_ni() {
    let root = root();
    std::fs::write(root.join("entry.py"), "print(7)\n").unwrap();
    let mut s = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
    s.default_source_state.aot_request = true;
    s.default_source_state.output = Some(root.join("out"));
    let (linked, _, plan) =
        crate::compile_linked_locations(&mut s, &root.join("entry.py"), &[], true, None, true)
            .unwrap();
    assert!(plan.is_none());
    assert!(linked.optional_sources.is_none());
    assert!(
        linked
            .modules
            .iter()
            .all(|(_, m)| matches!(m.location, ryupy_mir::ModuleLocation::LegacyDiagnostic))
    );
    s.instance_modules.push("missing".into());
    s.default_source_state.aot_request = false;
    assert!(
        crate::compile_linked_locations(&mut s, &root.join("entry.py"), &[], true, None, true)
            .is_err()
    );
    assert!(
        s.render_diagnostics(false)
            .contains("显式实例 AOT 需要资源清单")
    );
}

#[test]
fn subsequent_graphs_do_not_reuse_automatic_payload_or_authority() {
    let root = root();
    let entry = root.join("entry.py");
    std::fs::write(
        &entry,
        "import importlib.util\nimportlib.util.find_spec('subject')\n",
    )
    .unwrap();
    std::fs::write(root.join("subject.py"), "x=1\n").unwrap();
    let mut s = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
    s.default_source_state.aot_request = true;
    s.default_source_state.output = Some(root.join("out"));
    let (_, _, plan) =
        crate::compile_linked_locations(&mut s, &entry, &[], true, None, true).unwrap();
    assert!(plan.is_some());
    assert!(s.default_source_state.plan.generated_payload.is_some());
    std::fs::write(&entry, "def :\n").unwrap();
    assert!(crate::compile_linked_locations(&mut s, &entry, &[], true, None, true).is_err());
    assert!(s.default_source_state.plan.generated_payload.is_none());
    // Session原诊断由调用者消费后清空；没有清理/补造本轮来源计划来获得准入。
    let _ = std::mem::take(&mut s.diagnostics);
    std::fs::write(&entry, "print(1)\n").unwrap();
    let (linked, _, plan) =
        crate::compile_linked_locations(&mut s, &entry, &[], true, None, true).unwrap();
    assert!(plan.is_none());
    assert!(linked.optional_sources.is_none());
}

#[test]
fn mi_in_parse_rejected_soft_source_stays_deferred() {
    let root = root();
    std::fs::write(
        root.join("entry.py"),
        "import importlib.util\nimportlib.util.find_spec('bad')\n",
    )
    .unwrap();
    // parser保留完整多基节点，后续坏语法仍由旧ParseRejected来源桶延期。
    std::fs::write(
        root.join("bad.py"),
        "class A: pass\nclass B: pass\nclass D(A,B): pass\ndef :\n",
    )
    .unwrap();
    let mut session = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
    session.default_source_state.aot_request = true;
    session.default_source_state.output = Some(root.join("out"));
    let (linked, sources, plan) = crate::compile_linked_locations(
        &mut session,
        &root.join("entry.py"),
        &[],
        true,
        None,
        true,
    )
    .unwrap();
    assert!(sources.iter().all(|source| source.module_name != "bad"));
    assert!(
        session
            .default_source_state
            .plan
            .deferred
            .contains_key("bad")
    );
    assert_eq!(sources.len(), linked.modules.len());
    assert!(
        plan.unwrap()
            .automatic
            .as_ref()
            .unwrap()
            .source_to_emitted
            .iter()
            .any(Option::is_none)
    );
}

#[test]
fn compiled_ordinary_mi_aot_is_explicitly_rejected() {
    let root = root();
    std::fs::write(
        root.join("entry.py"),
        "class A: pass\nclass B: pass\nclass D(A,B): pass\nprint(D())\n",
    )
    .unwrap();
    let mut session = Session::default();
    assert!(
        crate::compile_linked_locations(
            &mut session,
            &root.join("entry.py"),
            &[],
            true,
            None,
            true
        )
        .is_err()
    );
    assert!(
        session
            .diagnostics
            .iter()
            .any(|d| d.message.contains("多继承C3"))
    );
}

#[test]
fn compiled_abc_single_base_cold_contract_still_rejects_aot() {
    for source in [
        "from collections.abc import MutableSequence\nclass C(MutableSequence): pass\n",
        "try:\n    from collections.abc import MutableSequence\nexcept ImportError:\n    from collections import MutableSequence\nclass C(MutableSequence): pass\n",
    ] {
        let root = root();
        std::fs::write(root.join("entry.py"), source).unwrap();
        let mut session = Session::default();
        assert!(
            crate::compile_linked_locations(
                &mut session,
                &root.join("entry.py"),
                &[],
                true,
                None,
                true
            )
            .is_err()
        );
        assert!(
            session
                .diagnostics
                .iter()
                .any(|d| d.message.contains("多继承C3")),
            "{:?}",
            session.diagnostics
        );
    }
}

#[test]
fn unknown_single_base_keeps_aot_plan_and_runtime_capability_guard() {
    // 只验编译计划未把未知单基冒认为MI；实际Native执行由独立后验覆盖。
    let root = root();
    std::fs::write(
        root.join("entry.py"),
        "from source import Base\nclass Child(Base): pass\nprint(Child([1, 2]))\n",
    )
    .unwrap();
    std::fs::write(root.join("source.py"), "Base = list\n").unwrap();
    let mut session = Session::default();
    let (linked, _, _) = crate::compile_linked_locations(
        &mut session,
        &root.join("entry.py"),
        &[],
        true,
        None,
        true,
    )
    .unwrap_or_else(|error| panic!("{error:?}: {:?}", session.diagnostics));
    assert!(session.diagnostics.is_empty(), "{:?}", session.diagnostics);
    let mut calls = Vec::new();
    for (_, function) in linked.functions.iter() {
        for (_, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                if let ryupy_mir::Statement::Assign {
                    value:
                        ryupy_mir::Rvalue::Call {
                            callee: ryupy_mir::Callee::Runtime(function),
                            ..
                        },
                    ..
                } = statement
                {
                    calls.push(*function);
                }
            }
        }
    }
    assert!(calls.contains(&ryupy_mir::RuntimeFn::PrepareRuntimeDerivedClass));
    assert!(!calls.contains(&ryupy_mir::RuntimeFn::PrepareOrdinaryMiClass));
}

#[test]
fn lexical_multiple_bases_reject_aot_without_changing_old_single_base_plans() {
    for (bases, rejected) in [("left, right", true), ("left", false), ("", false)] {
        let root = root();
        std::fs::write(
            root.join("entry.py"),
            format!("def make(left, right):\n    class C({bases}):\n        pass\n    return C\n"),
        )
        .unwrap();
        let mut session = Session::default();
        let result = crate::compile_linked_locations(
            &mut session,
            &root.join("entry.py"),
            &[],
            true,
            None,
            true,
        );
        if rejected {
            assert!(result.is_err());
            assert!(
                session
                    .diagnostics
                    .iter()
                    .any(|d| d.message.contains("多继承C3"))
            );
        } else {
            assert!(result.is_ok(), "{:?}", session.diagnostics);
        }
    }
}

#[test]
fn explicit_metadata_and_data_share_one_default_source_graph() {
    let root = root();
    std::fs::write(
        root.join("entry.py"),
        "import importlib.metadata\nimport app\nprint(app.value)\n",
    )
    .unwrap();
    std::fs::write(root.join("app.py"), "value = 7\n").unwrap();
    std::fs::write(root.join("payload.txt"), "package data\n").unwrap();
    std::fs::create_dir(root.join("demo-1.dist-info")).unwrap();
    std::fs::write(
        root.join("demo-1.dist-info/METADATA"),
        "Name: demo\nVersion: 1\n",
    )
    .unwrap();
    std::fs::write(
        root.join("demo-1.dist-info/entry_points.txt"),
        "[console_scripts]\ndemo = app:main\n",
    )
    .unwrap();
    let metadata = root.join("metadata.json");
    std::fs::write(
        &metadata,
        r#"{"schema_version":1,"mode":"closed_snapshot","roots":[{"id":"app","path":"."}]}"#,
    )
    .unwrap();
    let resources = root.join("resources.json");
    std::fs::write(
        &resources,
        r#"{"schema_version":1,"modules":[{"module":"app","files":["payload.txt"]}]}"#,
    )
    .unwrap();
    let mut session = Session {
        distribution_metadata: Some(metadata),
        include_modules: vec!["app".into()],
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
    assert!(eligible(&session, true, true));
    session.default_source_state.aot_request = true;
    let (linked, sources, plan) = crate::compile_linked_locations(
        &mut session,
        &root.join("entry.py"),
        &[],
        true,
        Some(&resources),
        true,
    )
    .unwrap();
    assert!(
        linked
            .distribution_metadata
            .as_ref()
            .unwrap()
            .contains("demo")
    );
    assert_eq!(sources.len(), linked.modules.len());
    let plan = plan.unwrap();
    assert!(plan.declared);
    assert!(plan.automatic.is_some());
    assert!(linked.optional_sources.is_some());
    let stage = root.join("stage");
    std::fs::create_dir(&stage).unwrap();
    let frozen = plan.stage(&stage).unwrap();
    assert_eq!(frozen.records.len(), 1);
    assert_eq!(frozen.records[0].size, 13);
    std::fs::remove_dir_all(root).unwrap();
}

#[test]
fn explicit_only_aot_planning_preserves_legacy_and_source_priority() {
    let root = root();
    std::fs::write(
        root.join("entry.py"),
        "import importlib.util\nimport json\nprint(importlib.util.find_spec('subject'))\n",
    )
    .unwrap();
    std::fs::write(root.join("subject.py"), "value = 7\n").unwrap();
    std::fs::write(root.join("json.py"), "value = 99\n").unwrap();
    let mut s = Session {
        module_environment_policy: ModuleEnvironmentPolicy::ExplicitOnly,
        ..Default::default()
    };
    assert!(eligible(&s, true, false));
    assert!(!crate::optional_sources::enabled(&s, false));
    assert!(!crate::optional_sources::enabled(&s, true));
    s.default_source_state.aot_request = true;
    assert!(crate::optional_sources::enabled(&s, true));
    assert!(!crate::optional_sources::enabled(&s, false));
    s.default_source_state.output = Some(root.join("out"));
    let (linked, sources, plan) =
        crate::compile_linked_locations(&mut s, &root.join("entry.py"), &[], true, None, true)
            .unwrap();
    assert!(
        linked
            .modules
            .iter()
            .all(|(_, m)| m.execution == ryupy_mir::ModuleExecutionKind::LegacySingleton)
    );
    let plan = plan.unwrap();
    assert_eq!(
        plan.automatic.as_ref().unwrap().manifest["automatic_instance_modules"],
        json!([])
    );
    let user = sources.iter().find(|m| m.module_name == "json").unwrap();
    assert_eq!(
        user.compiled_text_sha256,
        crate::manifest_files::hash_bytes(b"value = 99\n")
    );
    assert_eq!(
        sources.iter().filter(|m| m.module_name == "json").count(),
        1
    );
    s.fixed_binding_build = true;
    assert!(!eligible(&s, true, true));
    assert!(!crate::optional_sources::enabled(&s, true));
    std::fs::remove_dir_all(root).unwrap();
}

#[test]
fn explicit_only_aot_without_plan_demand_keeps_old_artifact_path() {
    let root = root();
    std::fs::write(root.join("entry.py"), "import math\nprint(7)\n").unwrap();
    let mut s = Session {
        module_environment_policy: ModuleEnvironmentPolicy::ExplicitOnly,
        ..Default::default()
    };
    s.default_source_state.aot_request = true;
    s.default_source_state.output = Some(root.join("out"));
    let (linked, sources, plan) =
        crate::compile_linked_locations(&mut s, &root.join("entry.py"), &[], true, None, true)
            .unwrap();
    assert_eq!(sources.len(), 1); // 已有native math不被catalog候选替换。
    assert!(plan.is_none());
    assert!(linked.optional_sources.is_none());
    assert!(
        linked
            .modules
            .iter()
            .all(|(_, m)| m.execution == ryupy_mir::ModuleExecutionKind::LegacySingleton)
    );
    s.instance_modules.push("subject".into());
    assert!(!eligible(&s, true, true));
    std::fs::remove_dir_all(root).unwrap();
}
