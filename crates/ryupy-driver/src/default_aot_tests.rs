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
    assert!(!eligible(&s, true, true));
    s.instance_modules.push("html.parser".into());
    assert!(!eligible(&s, true, false));
    s.instance_modules.clear();
    s.include_modules.push("x".into());
    assert!(!eligible(&s, true, false));
    s.include_modules.clear();
    s.fixed_binding_build = true;
    assert!(!eligible(&s, true, false));
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
#[test]
#[ignore = "真实工作负载显式窗口；只编译/发射，不publish或执行"]
fn default_aot_real_graph_emit() {
    let entry = PathBuf::from(std::env::var("RYUPY_DEFAULT_AOT_ENTRY").unwrap());
    let root = PathBuf::from(std::env::var("RYUPY_DEFAULT_AOT_ROOT").unwrap());
    let out = PathBuf::from(std::env::var("RYUPY_DEFAULT_AOT_OUTPUT").unwrap());
    std::fs::create_dir_all(&out).unwrap();
    let mut s = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
    s.default_source_state.aot_request = true;
    s.default_source_state.output = Some(out.clone());
    let result = crate::compile_linked_locations(&mut s, &entry, &[root], true, None, true);
    std::fs::write(out.join("diagnostics.txt"), s.render_diagnostics(false)).unwrap();
    std::fs::write(
        out.join("module-environment.json"),
        s.module_environment_report_json().unwrap(),
    )
    .unwrap();
    let (linked, sources, resources) = result.unwrap();
    let plan = resources.unwrap();
    assert_eq!(sources.len(), linked.modules.len());
    for (i, source) in sources.iter().enumerate() {
        assert_eq!(source.module_id, i as u32);
    }
    let payload = linked.optional_sources.as_ref().unwrap();
    std::fs::write(out.join("candidate.json"), payload).unwrap();
    std::fs::write(
        out.join("default_source_plan.json"),
        serde_json::to_vec_pretty(&plan.automatic.as_ref().unwrap().manifest).unwrap(),
    )
    .unwrap();
    let name = crate::assembly_name(&entry);
    // 发射前MIR权威独立保存；后端slot报告只能给位置，不能由实际Call重建期望。
    let modules:Vec<_>=linked.modules.iter().map(|(id,m)|json!({"module_id":id.into_raw().into_u32(),
        "name":m.name,"init_fn":m.init.into_raw().into_u32(),"execution":format!("{:?}",m.execution)})).collect();
    let functions: Vec<_> = linked
        .functions
        .iter()
        .map(|(id, f)| {
            json!({"function_id":id.into_raw().into_u32(),
        "codegen_name":f.codegen_name,"state":format!("{:?}",f.module_state_source)})
        })
        .collect();
    std::fs::write(
        out.join("mir-oracle.json"),
        serde_json::to_vec_pretty(&json!({"modules":modules,"functions":functions,
        "sources":sources,"source_to_emitted":plan.automatic.as_ref().unwrap().source_to_emitted,
        "candidate_payload_sha256":crate::manifest_files::hash_bytes(payload.as_bytes())}))
        .unwrap(),
    )
    .unwrap();
    let options = ryupy_codegen_cil::EmitOptions {
        assembly_name: crate::assembly_name(&entry),
        entry_source_path: Some(entry.to_string_lossy().into_owned()),
    };
    let emitted = ryupy_codegen_cil::emit_source_assembly_with_report(&linked, &options).unwrap();
    std::fs::write(out.join(format!("{name}.dll")), &emitted.bytes).unwrap();
    let roots:Vec<_>=emitted.report.delegate_references.iter().map(|r|json!({
        "role":r.role,"module_id":r.module_id,"function_id":r.function_id,"instruction":r.instruction,
        "source":{"owner":r.source.owner,"name":r.source.name,"signature":r.source.signature,"slot":r.source_slot},
        "target":r.target.as_ref().map(|t|json!({"owner":t.owner,"name":t.name,"signature":t.signature,"slot":r.target_slot}))})).collect();
    std::fs::write(
        out.join("emission-roots.json"),
        serde_json::to_vec_pretty(&roots).unwrap(),
    )
    .unwrap();
    let selected: Vec<String> =
        plan.automatic.as_ref().unwrap().manifest["automatic_instance_modules"]
            .as_array()
            .unwrap()
            .iter()
            .map(|v| v.as_str().unwrap().to_owned())
            .collect();
    if !selected.is_empty() {
        if let Ok(path) = std::env::var("RYUPY_DEFAULT_AOT_SOURCE_MANIFEST") {
            assert!(std::env::var_os("GITHUB_ACTIONS").is_none());
            let hash = crate::manifest_files::hash_bytes(&std::fs::read(path).unwrap());
            crate::instance_aot_evidence_tests::save_with_source_manifest(
                &out, &name, &linked, &selected, &sources, &emitted, &hash,
            );
        } else {
            crate::instance_aot_evidence_tests::save(
                &out, &name, &linked, &selected, &sources, &emitted,
            );
        }
    }
    std::fs::write(out.join("emission.txt"), format!("{:#?}", emitted.report)).unwrap();
    let context = crate::manifest::ManifestContext::new(
        emitted.report,
        sources,
        linked.entry.into_raw().into_u32(),
        &emitted.bytes,
        &s,
    )
    .unwrap();
    assert!(
        context.options["instance_modules"]
            .as_array()
            .unwrap()
            .is_empty()
    );
    let stage = out.join("stage");
    std::fs::create_dir(&stage).unwrap();
    let staged = plan.stage(&stage).unwrap();
    assert!(staged.records.is_empty());
    assert_eq!(staged.locations.len(), linked.modules.len());
    std::fs::write(
        out.join("staged-locations.json"),
        serde_json::to_vec_pretty(&staged.locations).unwrap(),
    )
    .unwrap();
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
