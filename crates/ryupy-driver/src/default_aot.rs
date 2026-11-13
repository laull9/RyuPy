//! 默认AOT的私有联合计划：真实源索引保留到stage，发射ID仅在最终输出投影。
use crate::{
    Session,
    module_environments::ModuleEnvironmentPolicy,
    modules::{SourceKind, SourceModule},
};
use anyhow::{Result, ensure};
use serde_json::{Value, json};

#[derive(Clone)]
pub(crate) struct Audit {
    pub source_to_emitted: Vec<Option<u32>>,
    pub manifest: Value,
}
pub(crate) fn eligible(sess: &Session, aot: bool, _resources: bool) -> bool {
    // 普通源码保既有metadata/include位置规划；绑定仅凭本次解析后的私有授权。
    if !aot || !sess.instance_modules.is_empty() {
        return false;
    }
    if sess.fixed_binding_build {
        sess.default_source_state.binding_catalog
            && sess.module_environment_policy == ModuleEnvironmentPolicy::OrdinaryDynamicPilot
            && sess.include_modules.is_empty()
            && sess.distribution_metadata.is_none()
    } else {
        true
    }
}
pub(crate) fn needed(sess: &Session, sources: &[SourceModule]) -> bool {
    sess.default_source_state.aot_request
        && (sources.iter().any(|s| s.candidate)
            || sess.default_source_state.plan.payload.is_some()
            || sess
                .default_source_state
                .plan
                .metadata_unavailable
                .is_some())
}
/// Token只证明来源；先按旧JIT完整资格纯查询选择，再验证所选者AOT，编译失败不回退。
pub(crate) fn selected(sess: &Session, sources: &[SourceModule]) -> Result<Vec<usize>> {
    // 部署规划不授予Instance能力；默认ExplicitOnly始终保留原Legacy ABI。
    if sess.module_environment_policy == ModuleEnvironmentPolicy::ExplicitOnly {
        return Ok(Vec::new());
    }
    let mut selected = Vec::new();
    for (i, source) in sources.iter().enumerate() {
        if source.kind != SourceKind::ModuleFile
            || source.catalog.is_none()
            || !ryupy_hir::has_defined_functions(&source.parsed)
        {
            continue;
        }
        ensure!(
            source.catalog.as_ref().is_some_and(
                |t| t.authenticates(&source.resolved_source_path, source.text.as_bytes())
            ),
            "默认AOT catalog身份变化"
        );
        let capabilities = ryupy_base::InstanceCapabilities {
            aot_class_guards: false,
            direct_globals: false,
            ordinary_name_handlers: true,
            relative_from_import: true,
        };
        if !ryupy_hir::instance_ast_supported(&source.parsed, capabilities) {
            continue;
        }
        selected.push(i);
    }
    let _ = sess;
    Ok(selected)
}
/// 同一发现表生成cold v3和artifact4审计，不重新按名字搜索文件或制造deferred代码。
pub(crate) fn finish(
    sess: &Session,
    sources: &[SourceModule],
    selected: &[usize],
    mapping: Vec<Option<u32>>,
    resources: &crate::resources::ResourcePlan,
) -> Result<(String, Audit)> {
    let plan = &sess.default_source_state.plan;
    ensure!(sources.len() == mapping.len(), "默认AOT索引表长度不符");
    for (expected, actual) in mapping.iter().flatten().enumerate() {
        ensure!(*actual == u32::try_from(expected)?, "默认AOT发射ID不连续");
    }
    let mut locations = serde_json::Map::new();
    let mut deferred = Vec::new();
    let mut automatic = Vec::new();
    for (i, source) in sources.iter().enumerate() {
        let path = resources
            .module_location(i as u32)
            .ok_or_else(|| anyhow::anyhow!("默认AOT缺锚"))?;
        let registration = if mapping[i].is_none() {
            "deferred"
        } else if source.candidate {
            "candidate"
        } else {
            "active"
        };
        locations.insert(source.name.clone(), json!({"package":source.package,"file":source.file,
            "kind":2,"path":path,"is_package":source.kind==SourceKind::Package,"registration":registration}));
        if mapping[i].is_none() {
            let reason = plan
                .deferred
                .get(&source.name)
                .ok_or_else(|| anyhow::anyhow!("未发射source缺延期原因"))?;
            deferred.push(json!({"module_name":source.name,"package":source.package,
                "logical_source":source.logical_source,"source_sha256":crate::manifest_files::hash_bytes(source.text.as_bytes()),
                "relative_file":path,"is_package":source.kind==SourceKind::Package,"reason":reason}));
        } else if selected.contains(&i) {
            automatic.push(source.name.clone());
        }
    }
    automatic.sort();
    let external: std::collections::BTreeSet<_> = plan
        .dependencies
        .values()
        .flatten()
        .filter(|name| {
            !locations.contains_key(name.as_str())
                && !plan.outside.contains(*name)
                && !plan.closed_unplanned.contains(*name)
                && !plan.protected.contains(*name)
        })
        .cloned()
        .collect();
    let payload = serde_json::to_string(
        &json!({"schema_version":3,"roots":plan.roots,"external_dependencies":external,
        "dependencies":plan.dependencies,"locations":locations,"deferred":plan.deferred,
        "outside":plan.outside,"protected":plan.protected,"closed_unplanned":plan.closed_unplanned}),
    )?;
    let audit = Audit {
        source_to_emitted: mapping,
        manifest: json!({"schema_version":1,
        "mode":"automatic_locations","candidate_payload_sha256":crate::manifest_files::hash_bytes(payload.as_bytes()),
        "automatic_instance_modules":automatic,"deferred_sources":deferred}),
    };
    Ok((payload, audit))
}

#[cfg(test)]
#[path = "default_aot_tests.rs"]
mod tests;
