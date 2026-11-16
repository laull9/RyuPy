//! 显式资源选择与普通/绑定发布的资源报告；仅针对本次已编译模块，不扫描目录或扩大导入闭包。
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
    pub(super) source_bytes: Vec<Vec<u8>>,
    pub(super) source_lengths: Vec<u64>,
    pub(super) selected: BTreeSet<usize>,
    pub(crate) automatic: Option<crate::default_aot::Audit>,
    pub(super) declared: bool,
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
            source_bytes: sources.iter().map(|s| s.text.as_bytes().to_vec()).collect(),
            selected: BTreeSet::new(),
            automatic: None,
            declared: true,
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
    /// 默认域只签发虚拟锚；所有源仍由原stage持句柄复核，无数据claims。
    pub(crate) fn automatic(sources: &[SourceModule]) -> Result<Self> {
        ensure!(sources.len() <= 256, "默认AOT位置超过256项");
        let mut targets = Targets::default();
        let mut locations = Vec::new();
        for (index, source) in sources.iter().enumerate() {
            let relative_file = format!("{PREFIX}{}", source.logical_source);
            targets.insert(&relative_file, Kind::Anchor)?;
            locations.push(ModuleLocationRecord {
                module_id: u32::try_from(index)?,
                module_name: source.name.clone(),
                kind: "executable_relative",
                relative_file,
            });
        }
        Ok(Self {
            locations,
            claims: BTreeMap::new(),
            source_files: sources
                .iter()
                .map(|s| s.resolved_source_path.clone())
                .collect(),
            source_bytes: sources.iter().map(|s| s.text.as_bytes().to_vec()).collect(),
            source_lengths: sources.iter().map(|s| s.text.len() as u64).collect(),
            selected: (0..sources.len()).collect(),
            automatic: None,
            declared: false,
        })
    }
    /// 在同次真实源码图上补齐锚点；保留显式 claims，并统一检查锚点与数据冲突。
    pub(crate) fn add_automatic_locations(&mut self, sources: &[SourceModule]) -> Result<()> {
        let automatic = Self::automatic(sources)?;
        let mut targets = Targets::default();
        for location in &automatic.locations {
            targets.insert(&location.relative_file, Kind::Anchor)?;
        }
        for target in self.claims.keys() {
            targets.insert(target, Kind::Resource)?;
        }
        self.locations = automatic.locations;
        self.selected = automatic.selected;
        Ok(())
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

/// 在同次发射清单上追加资源声明；普通与绑定各有独立版本，保留原生模块来源与 PInvoke 记录。
/// 无资源请求不调用，旧 schema1/3/4 的快路和字段合同保持原样。
pub(crate) fn extend_manifest(bytes: &[u8], staged: &StagedResources) -> Result<Vec<u8>> {
    let mut value: serde_json::Value = serde_json::from_slice(bytes)?;
    let version = match (
        value["schema_version"].as_u64(),
        value.get("native_bindings").is_some(),
        staged.automatic.is_some(),
    ) {
        (Some(1), false, false) => 2,
        (Some(1), false, true) => 4,
        (Some(3), true, false) => 5,
        (Some(4), true, false) => 6,
        (Some(7), true, false) => 8,
        (Some(3 | 4 | 7), true, true) => 9,
        _ => anyhow::bail!("资源扩展与基础发布清单版本不匹配"),
    };
    if value.get("native_bindings").is_some() {
        ensure!(
            value["kind"] == "ryupy-native-aot-artifact",
            "绑定资源仅用于 AOT 清单"
        );
    }
    value["schema_version"] = version.into();
    value["module_locations"] = serde_json::to_value(&staged.locations)?;
    value["resources"] = serde_json::to_value(&staged.records)?;
    value["scope"]["resources_included"] = (!staged.records.is_empty()).into();
    value["scope"]["declared_resources_complete"] = staged.declared.into();
    if let Some(audit) = &staged.automatic {
        value["default_source_plan"] = audit.manifest.clone();
        value["scope"]["automatic_module_locations_complete"] = true.into();
    }
    let mut output = serde_json::to_vec_pretty(&value)?;
    output.push(b'\n');
    Ok(output)
}

#[cfg(test)]
#[path = "resources_stage_tests.rs"]
mod stage_tests;

/// 布局重定位只给唯一顶层native文件Unicode例外；其它发布项复用原ASCII资源策略。
pub(crate) fn validate_layout_artifact_path(path: &str) -> Result<()> {
    paths::portable(path)
}

#[cfg(test)]
#[path = "resources_combined_tests.rs"]
mod combined_tests;
