//! 真实AOT图的显式测试导出器：只产独立发射前oracle，不执行SDK或生成交付声明。
use super::*;

pub(super) fn emit_real_graph() {
    let entry = PathBuf::from(std::env::var("RYUPY_DEFAULT_AOT_ENTRY").unwrap());
    let root = PathBuf::from(std::env::var("RYUPY_DEFAULT_AOT_ROOT").unwrap());
    let out = PathBuf::from(std::env::var("RYUPY_DEFAULT_AOT_OUTPUT").unwrap());
    std::fs::create_dir_all(&out).unwrap();
    let mut s = Session {
        include_modules: std::env::var("RYUPY_DEFAULT_AOT_INCLUDE")
            .map(|v| serde_json::from_str(&v).expect("include须为JSON字符串数组"))
            .unwrap_or_default(),
        distribution_metadata: std::env::var_os("RYUPY_DEFAULT_AOT_METADATA").map(PathBuf::from),
        ..Default::default()
    };
    let resource_request = std::env::var_os("RYUPY_DEFAULT_AOT_RESOURCES").map(PathBuf::from);
    assert!(eligible(&s, true, resource_request.is_some()));
    s.default_source_state.aot_request = true;
    s.default_source_state.output = Some(out.clone());
    let result = crate::compile_linked_locations(
        &mut s,
        &entry,
        std::slice::from_ref(&root),
        true,
        resource_request.as_deref(),
        true,
    );
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
    // 来源由同次计划及其source→emitted映射导出，不从产物清单反造预期。
    let input_files: Vec<_> = plan
        .automatic
        .as_ref()
        .unwrap()
        .source_to_emitted
        .iter()
        .enumerate()
        .filter_map(|(source_index, emitted)| {
            emitted.map(|id| {
                let source = sources.iter().find(|s| s.module_id == id).unwrap();
                let path = &plan.source_files[source_index];
                let raw = std::fs::read(path).unwrap();
                let hash = crate::manifest_files::hash_bytes(&raw);
                assert_eq!(hash, plan.source_hashes[source_index]);
                json!({"module_name":source.module_name,"path":path,"sha256":hash})
            })
        })
        .collect();
    let tool_hash = |key: &str| {
        std::env::var_os(key).map(|p| crate::manifest_files::hash_bytes(&std::fs::read(p).unwrap()))
    };
    std::fs::write(
        out.join("source-inputs.json"),
        serde_json::to_vec_pretty(&json!({
            "entry":entry,"files":input_files,
            "cli_sha256":tool_hash("RYUPY_DEFAULT_AOT_CLI"),
            "runtime_sha256":tool_hash("RYUPY_DEFAULT_AOT_RUNTIME"),
            "search_roots":[root],"include_modules":s.include_modules,
            "resources":resource_request,"metadata":s.distribution_metadata
        }))
        .unwrap(),
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
    // 此处只读发射前Linked MIR；字段次序来自已有非InstancePublic分配合同。
    let static_globals: Vec<_> = linked.globals.iter().filter(|(_, g)| {
        !matches!(g.storage, ryupy_mir::GlobalStorage::InstancePublic { .. })
    }).map(|(id, g)| {
        json!({"global_id":id.into_raw().into_u32(),"name":g.name,
            "storage":if matches!(g.storage, ryupy_mir::GlobalStorage::CodeSource { .. }) {"CodeSource"} else {"LegacyStatic"}})
    }).collect();
    let code_sources: Vec<_> = linked
        .globals
        .iter()
        .filter_map(|(id, g)| {
            let ryupy_mir::GlobalStorage::CodeSource {
                entry,
                legacy_entry,
                ..
            } = &g.storage
            else {
                return None;
            };
            let owners: Vec<_> = linked
                .modules
                .iter()
                .filter(|(_, m)| m.init == *entry)
                .collect();
            assert_eq!(owners.len(), 1);
            let (module_id, module) = owners[0];
            assert_eq!(
                module.execution == ryupy_mir::ModuleExecutionKind::LegacySingleton,
                legacy_entry.is_some()
            );
            Some(json!({"global_id":id.into_raw().into_u32(),"name":g.name,
            "module_id":module_id.into_raw().into_u32(),"entry_fn":entry.into_raw().into_u32(),
            "source_file":linked.functions[*entry].source_file,
            "legacy_entry":legacy_entry.as_ref().map(|v|json!({"initial_name":v.initial_name,
                "name_global":v.name_global.into_raw().into_u32()}))}))
        })
        .collect();
    assert_eq!(code_sources.len(), linked.modules.len());
    std::fs::write(
        out.join("mir-oracle.json"),
        serde_json::to_vec_pretty(&json!({"modules":modules,"functions":functions,
        "sources":sources,"static_globals":static_globals,"code_sources":code_sources,"source_to_emitted":plan.automatic.as_ref().unwrap().source_to_emitted,
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
    if resource_request.is_none() {
        assert!(staged.records.is_empty());
    }
    std::fs::write(
        out.join("staged-resources.json"),
        serde_json::to_vec_pretty(&staged.records).unwrap(),
    )
    .unwrap();
    assert_eq!(staged.locations.len(), linked.modules.len());
    std::fs::write(
        out.join("staged-locations.json"),
        serde_json::to_vec_pretty(&staged.locations).unwrap(),
    )
    .unwrap();
}
