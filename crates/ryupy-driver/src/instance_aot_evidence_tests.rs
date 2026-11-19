//! 实际源门的审计期望：仅由 MIR 身份与同次发射报告构造，不从实际 Call 反推 core。
use ryupy_codegen_cil::{DelegateReference, EmissionArtifact, MethodDescriptor};
use ryupy_mir::{InstanceStateSource, LinkedProgram, ParamKind};
use serde_json::{Value, json};
use std::path::Path;

fn locator(method: &MethodDescriptor, slot: u32) -> Value {
    json!({"owner": method.owner, "name": method.name,
        "signature": method.signature, "slot": slot})
}
fn root(reference: &DelegateReference) -> Value {
    json!({"source": locator(&reference.source, reference.source_slot),
        "target": locator(reference.target.as_ref().expect("实际 Definition"),
            reference.target_slot.expect("实际 slot")),
        "instruction": reference.instruction, "role": reference.role})
}

pub(super) fn save(
    output: &Path,
    name: &str,
    linked: &LinkedProgram,
    selected: &[String],
    sources: &[crate::manifest::SourceSummary],
    artifact: &EmissionArtifact,
) {
    let commit = std::env::var("GITHUB_SHA").expect("当前源 commit");
    save_identity(
        output,
        name,
        linked,
        selected,
        sources,
        artifact,
        json!({"commit": commit}),
    );
}

pub(super) fn save_with_source_manifest(
    output: &Path,
    name: &str,
    linked: &LinkedProgram,
    selected: &[String],
    sources: &[crate::manifest::SourceSummary],
    artifact: &EmissionArtifact,
    sha256: &str,
) {
    assert!(sha256.is_empty(), "普通产物跳过摘要");
    save_identity(
        output,
        name,
        linked,
        selected,
        sources,
        artifact,
        json!({"identity_kind": "source-manifest", "source_manifest_sha256": sha256}),
    );
}

fn save_identity(
    output: &Path,
    name: &str,
    linked: &LinkedProgram,
    selected: &[String],
    sources: &[crate::manifest::SourceSummary],
    artifact: &EmissionArtifact,
    identity: Value,
) {
    let report = &artifact.report;
    assert_eq!(report.schema_version, 2);
    let mut owners = Vec::new();
    for (id, function) in linked.functions.iter() {
        if function.module_state_source != Some(InstanceStateSource::FunctionParameter) {
            continue;
        }
        let id = id.into_raw().into_u32();
        let definitions: Vec<_> = report
            .function_definitions
            .iter()
            .filter(|definition| definition.function_id == id)
            .collect();
        assert_eq!(definitions.len(), 1, "每个 FnId 必须唯一映射实际 core");
        let definition = definitions[0];
        assert_eq!(definition.core.name, function.codegen_name);
        let roots: Vec<_> = report
            .delegate_references
            .iter()
            .filter(|reference| {
                reference.function_id == Some(id)
                    && reference.role == "callable_thunk"
                    && reference.source.name == "__init_functions"
            })
            .collect();
        assert_eq!(roots.len(), 1, "每个 owner 必须恰有一个静态初始化根");
        let simple = function.param_meta.iter().all(|parameter| {
            matches!(
                parameter.kind,
                ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword
            )
        });
        owners.push(json!({"function_id": id,
            "core": locator(&definition.core, definition.core_slot),
            "root": root(roots[0]), "calls": if simple { 2 } else { 1 }}));
    }
    assert!(!owners.is_empty());
    let mut module_roots = Vec::new();
    for name in selected {
        let modules: Vec<_> = linked
            .modules
            .iter()
            .filter(|(_, m)| &m.name == name)
            .collect();
        assert_eq!(modules.len(), 1);
        let id = modules[0].0.into_raw().into_u32();
        let roots: Vec<_> = report
            .delegate_references
            .iter()
            .filter(|reference| reference.role == "module_init" && reference.module_id == Some(id))
            .collect();
        assert_eq!(roots.len(), 1);
        module_roots.push(json!({"module_id": id, "module_name": name, "root": root(roots[0])}));
    }
    let mut expected = json!({"schema_version": 1, "report_version": report.schema_version,
        "case": name,
        "pe": {"name": format!("{name}.dll"), "size": artifact.bytes.len(),
            "sha256": crate::manifest_files::hash_bytes(&artifact.bytes)},
        "selected": selected, "sources": sources, "owner_count": linked.functions.iter()
            .filter(|(_, f)| f.module_state_source == Some(InstanceStateSource::FunctionParameter)).count(),
        "entry": locator(&report.entry, report.entry_slot),
        "owners": owners, "modules": module_roots});
    expected
        .as_object_mut()
        .unwrap()
        .extend(identity.as_object().unwrap().clone());
    std::fs::write(
        output.join(format!("{name}-expected.json")),
        serde_json::to_vec_pretty(&expected).unwrap(),
    )
    .unwrap();
}
