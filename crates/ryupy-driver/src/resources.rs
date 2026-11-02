//! 显式资源选择与schema2报告；仅针对本次已编译模块，不扫描目录或扩大Python导入闭包。
use crate::modules::SourceModule;
use anyhow::{Context, Result, ensure};
use serde::{Deserialize, Serialize};
use std::collections::{BTreeMap, BTreeSet};
use std::io::Read;
use std::path::{Path, PathBuf};

#[path = "resources_paths.rs"]
mod paths;
#[path = "resources_stage.rs"]
mod stage;
use paths::{Kind, Targets};
pub(crate) use stage::StagedResources;
pub(super) const PREFIX: &str = "_internal/resources/v1/";
pub(super) const FILE_LIMIT: u64 = 16 * 1024 * 1024;
pub(super) const TOTAL_LIMIT: u64 = 64 * 1024 * 1024;

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct Request {
    schema_version: u32,
    modules: Vec<ModuleRequest>,
}
#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct ModuleRequest {
    module: String,
    files: Vec<String>,
}
#[derive(Clone, Debug, Serialize)]
pub(crate) struct ModuleLocationRecord {
    pub module_id: u32,
    pub module_name: String,
    pub kind: &'static str,
    pub relative_file: String,
}
#[derive(Debug, Serialize)]
pub(crate) struct ResourceRecord {
    pub relative_path: String,
    pub size: u64,
    pub sha256: String,
    pub consumer_module_ids: Vec<u32>,
}
#[derive(Clone)]
pub(super) struct Claim {
    pub source_module: usize,
    pub file: String,
    pub consumers: BTreeSet<u32>,
}
pub(crate) struct ResourcePlan {
    pub(crate) locations: Vec<ModuleLocationRecord>,
    pub(super) claims: BTreeMap<String, Vec<Claim>>,
    pub(super) source_files: Vec<PathBuf>,
    pub(super) source_hashes: Vec<String>,
    pub(super) source_lengths: Vec<u64>,
    pub(super) selected: BTreeSet<usize>,
}

impl ResourcePlan {
    pub(crate) fn read(path: &Path, sources: &[SourceModule]) -> Result<Self> {
        let metadata = crate::manifest_files::plain_metadata(path)?;
        ensure!(
            metadata.is_file() && metadata.len() <= 1024 * 1024,
            "资源清单须为不超过1MiB普通文件"
        );
        let mut bytes = Vec::new();
        std::fs::File::open(path)?
            .take(1024 * 1024 + 1)
            .read_to_end(&mut bytes)?;
        ensure!(bytes.len() <= 1024 * 1024, "资源清单超过1MiB");
        let request: Request =
            serde_json::from_slice(&bytes).context("资源清单不是有效schema1 JSON")?;
        ensure!(
            request.schema_version == 1 && request.modules.len() <= 256,
            "资源清单版本或模块数超限"
        );
        let mut plan = Self {
            locations: Vec::new(),
            claims: BTreeMap::new(),
            source_files: sources
                .iter()
                .map(|s| s.resolved_source_path.clone())
                .collect(),
            source_lengths: sources.iter().map(|s| s.text.len() as u64).collect(),
            source_hashes: sources
                .iter()
                .map(|s| crate::manifest_files::hash_bytes(s.text.as_bytes()))
                .collect(),
            selected: BTreeSet::new(),
        };
        let mut targets = Targets::default();
        // 锚点先全部保留，防先处理的资源覆盖后声明模块的虚拟源位置。
        for entry in &request.modules {
            let index = sources
                .iter()
                .position(|s| s.name == entry.module)
                .context("资源模块不是本次已编译源码模块")?;
            ensure!(plan.selected.insert(index), "资源模块重复声明");
            let relative_file = format!("{PREFIX}{}", sources[index].logical_source);
            targets.insert(&relative_file, Kind::Anchor)?;
            plan.locations.push(ModuleLocationRecord {
                module_id: u32::try_from(index)?,
                module_name: entry.module.clone(),
                kind: "executable_relative",
                relative_file,
            });
        }
        for (entry, location) in request.modules.iter().zip(&plan.locations) {
            let source_module = location.module_id as usize;
            let parent = location
                .relative_file
                .rsplit_once('/')
                .context("模块锚点没有父目录")?
                .0;
            for file in &entry.files {
                paths::portable(file)?;
                let lower = file.to_ascii_lowercase();
                ensure!(
                    ![".py", ".pyc", ".pyo"]
                        .iter()
                        .any(|ext| lower.ends_with(ext)),
                    "首切资源不能部署Python源码/字节码"
                );
                let target = format!("{parent}/{file}");
                targets.insert(&target, Kind::Resource)?;
                let claims = plan.claims.entry(target).or_default();
                if let Some(existing) = claims
                    .iter_mut()
                    .find(|c| c.source_module == source_module && c.file == *file)
                {
                    existing.consumers.insert(location.module_id);
                } else {
                    claims.push(Claim {
                        source_module,
                        file: file.clone(),
                        consumers: BTreeSet::from([location.module_id]),
                    });
                }
                ensure!(plan.claims.len() <= 1024, "去重资源超过1024项");
            }
        }
        plan.locations.sort_by_key(|l| l.module_id);
        Ok(plan)
    }
    pub(crate) fn module_location(&self, id: u32) -> Option<&str> {
        self.locations
            .iter()
            .find(|l| l.module_id == id)
            .map(|l| l.relative_file.as_str())
    }
    pub(crate) fn stage(&self, root: &Path) -> Result<StagedResources> {
        stage::freeze(self, root)
    }
}

/// 在同次schema1序列化结果上追加资源声明；无请求不调用，保schema1原快路/字节合同。
pub(crate) fn extend_manifest(bytes: &[u8], staged: &StagedResources) -> Result<Vec<u8>> {
    let mut value: serde_json::Value = serde_json::from_slice(bytes)?;
    value["schema_version"] = 2.into();
    value["module_locations"] = serde_json::to_value(&staged.locations)?;
    value["resources"] = serde_json::to_value(&staged.records)?;
    value["scope"]["resources_included"] = (!staged.records.is_empty()).into();
    value["scope"]["declared_resources_complete"] = true.into();
    let mut output = serde_json::to_vec_pretty(&value)?;
    output.push(b'\n');
    Ok(output)
}

#[cfg(test)]
#[path = "resources_stage_tests.rs"]
mod stage_tests;
