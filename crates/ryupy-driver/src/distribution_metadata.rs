//! 显式封闭分发快照；仅driver读文件，完整保留未知元数据，不把源码图当发现环境。
use anyhow::{Context, Result, ensure};
use serde::{Deserialize, Serialize};
use std::collections::BTreeSet;
use std::path::{Path, PathBuf};

/// 自动环境读取的已知拒绝，不覆盖载荷验证、内部不变量或编译器错误。
#[derive(Debug)]
struct EnvironmentUnavailable(String);
impl std::fmt::Display for EnvironmentUnavailable {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.write_str(&self.0)
    }
}
impl std::error::Error for EnvironmentUnavailable {}
macro_rules! environment_ensure {
    ($condition:expr, $($arg:tt)*) => {
        if !$condition { return Err(EnvironmentUnavailable(format!($($arg)*)).into()); }
    };
}

#[path = "distribution_metadata_files.rs"]
mod files;
#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct Request {
    schema_version: u32,
    mode: String,
    roots: Vec<Root>,
}
#[derive(Deserialize, Serialize)]
#[serde(deny_unknown_fields)]
struct Root {
    id: String,
    path: PathBuf,
}
#[derive(Serialize, PartialEq, Debug)]
struct Snapshot {
    schema_version: u32,
    mode: &'static str,
    roots: Vec<String>,
    distributions: Vec<Distribution>,
}
#[derive(Serialize, PartialEq, Debug)]
struct Distribution {
    root: String,
    path: String,
    kind: &'static str,
    metadata_name: String,
    metadata_version: String,
    discovery_key: String,
    shadowed: bool,
    directories: Vec<String>,
    files: Vec<files::Record>,
}

/// 两次完整读取只检测可信非并发输入的一般变化，不声称对恶意竞态的原子性。
pub(crate) fn collect(path: &Path) -> Result<String> {
    files::plain_path(path)?;
    let request_bytes = files::read(path, 64 * 1024)?;
    let request: Request = serde_json::from_slice(&request_bytes).context("分发请求schema无效")?;
    ensure!(
        request.schema_version == 1 && request.mode == "closed_snapshot",
        "分发请求只支持schema1 closed_snapshot"
    );
    ensure!(request.roots.len() <= 64, "分发根超过64项");
    let first = scan(&request, path.parent().unwrap_or(Path::new(".")))?;
    let second = scan(&request, path.parent().unwrap_or(Path::new(".")))?;
    ensure!(
        first == second && files::read(path, 64 * 1024)? == request_bytes,
        "分发输入在采集中改变"
    );
    let payload = serde_json::to_string(&first)?;
    ensure!(payload.len() <= 4 * 1024 * 1024, "分发静态载荷超过4MiB");
    Ok(payload)
}
fn scan(request: &Request, base: &Path) -> Result<Snapshot> {
    let mut result = Snapshot {
        schema_version: 1,
        mode: "closed_snapshot",
        roots: Vec::new(),
        distributions: Vec::new(),
    };
    let mut ids = BTreeSet::new();
    let mut seen = BTreeSet::new();
    let mut budget = files::Budget::default();
    for root in &request.roots {
        crate::manifest_files::safe_relative(&root.id)?;
        environment_ensure!(ids.insert(root.id.clone()), "分发根id重复");
        result.roots.push(root.id.clone());
        let path = base.join(&root.path);
        files::plain_path(&path)?;
        environment_ensure!(path.is_dir(), "分发根必须为普通目录");
        for (name, child) in files::children(&path)? {
            if name.ends_with(".zip") || name.ends_with(".egg") {
                return Err(EnvironmentUnavailable(format!(
                    "封闭分发根不支持zip/egg archive：{name}"
                ))
                .into());
            }
            let kind = if name.ends_with(".dist-info") {
                "dist-info"
            } else if name.ends_with(".egg-info") {
                "egg-info"
            } else {
                continue;
            };
            environment_ensure!(result.distributions.len() < 256, "分发数超过256");
            files::plain_path(&child)?;
            let mut records = Vec::new();
            let mut directories = Vec::new();
            if child.is_dir() {
                files::walk(&child, "", &mut records, &mut directories, &mut budget, 0)?;
            } else {
                environment_ensure!(kind == "egg-info", "dist-info必须为目录");
                records.push(files::record(&child, "PKG-INFO", &mut budget)?);
            }
            let metadata_file = if kind == "dist-info" {
                "METADATA"
            } else {
                "PKG-INFO"
            };
            let source = records
                .iter()
                .find(|f| f.path == metadata_file)
                .ok_or_else(|| EnvironmentUnavailable("分发缺少身份metadata".into()))?;
            let text = String::from_utf8(files::unhex(&source.hex)?)
                .map_err(|_| EnvironmentUnavailable("分发身份metadata不是UTF-8".into()))?;
            let metadata_name = header(&text, "Name")?;
            let metadata_version = header(&text, "Version")?;
            let stem = name.strip_suffix(&format!(".{kind}")).unwrap_or("");
            let prefix = stem.split('-').next().unwrap_or("");
            let discovery_key = normalize(if prefix.is_empty() {
                &metadata_name
            } else {
                prefix
            });
            let shadowed = !seen.insert(discovery_key.clone());
            result.distributions.push(Distribution {
                root: root.id.clone(),
                path: name,
                kind,
                metadata_name,
                metadata_version,
                discovery_key,
                shadowed,
                directories,
                files: records,
            });
        }
    }
    Ok(result)
}
fn header(text: &str, field: &str) -> Result<String> {
    let values: Vec<_> = text
        .lines()
        .take_while(|line| !line.is_empty())
        .filter_map(|line| {
            let (name, value) = line.split_once(':')?;
            name.eq_ignore_ascii_case(field)
                .then(|| value.trim().to_owned())
        })
        .collect();
    environment_ensure!(
        values.len() == 1 && !values[0].is_empty(),
        "分发身份字段{field}缺失或歧义"
    );
    Ok(values[0].clone())
}
fn normalize(value: &str) -> String {
    let mut result = String::new();
    let mut separator = false;
    for ch in value.chars() {
        if matches!(ch, '-' | '_' | '.') {
            if !separator {
                result.push('_');
            }
            separator = true;
        } else {
            result.extend(ch.to_lowercase());
            separator = false;
        }
    }
    result
}
#[cfg(test)]
#[path = "distribution_metadata_tests.rs"]
mod tests;

/// 编排同次审计摘要；无配置不改变默认清单。
pub(crate) fn prepare(sess: &mut crate::Session) -> Result<Option<String>> {
    sess.distribution_metadata_report = None;
    anyhow::ensure!(
        !(sess.fixed_binding_build && sess.distribution_metadata.is_some()),
        "分发快照首切不支持native binding宿主"
    );
    let distribution_payload = sess
        .distribution_metadata
        .as_deref()
        .map(collect)
        .transpose()?;
    if let Some(payload) = &distribution_payload {
        record_payload(sess, payload)?;
    }
    Ok(distribution_payload)
}

/// 同次显式/自动快照共用审计格式；不把自动失败伪造成空inventory。
pub(crate) fn record_payload(sess: &mut crate::Session, payload: &str) -> Result<()> {
    let mut report: serde_json::Value = serde_json::from_str(payload)?;
    for dist in report["distributions"]
        .as_array_mut()
        .expect("validated snapshot")
    {
        for file in dist["files"].as_array_mut().expect("validated snapshot") {
            file.as_object_mut().expect("validated file").remove("hex");
        }
    }
    report["payload_sha256"] = crate::manifest_files::hash_bytes(payload.as_bytes()).into();
    sess.distribution_metadata_report = Some(report);
    Ok(())
}

/// 显式配置产品的同次审计输出，JIT/AOT均可核验；不是运行时数据来源。
pub(crate) fn write_report(sess: &crate::Session, out: &Path) -> Result<()> {
    if let Some(report) = &sess.distribution_metadata_report {
        std::fs::write(
            out.join("distribution-metadata.json"),
            serde_json::to_vec_pretty(report)?,
        )?;
    }
    Ok(())
}

/// 编译器自有源码部署复用同一有界普通文件读取；错误始终硬停。
pub(crate) fn read_source_bytes(path: &Path, limit: usize) -> Result<Vec<u8>> {
    files::read(path, limit)
}

/// 自动扫描只接受入口与有序显式根；失败为状态而非假空快照。显式请求完全覆盖。
pub(crate) fn automatic(roots: &[PathBuf]) -> Result<std::result::Result<String, String>> {
    if roots.len() > 64 {
        return Ok(Err("分发根超过64项".into()));
    }
    let request = Request {
        schema_version: 1,
        mode: "closed_snapshot".into(),
        roots: roots
            .iter()
            .enumerate()
            .map(|(index, path)| Root {
                id: format!("search-{index}"),
                path: path.clone(),
            })
            .collect(),
    };
    let read = || -> Result<Snapshot> {
        let first = scan(&request, Path::new("."))?;
        let second = scan(&request, Path::new("."))?;
        environment_ensure!(first == second, "分发输入在采集中改变");
        Ok(first)
    };
    match read() {
        Ok(snapshot) => {
            let payload = serde_json::to_string(&snapshot)?;
            if payload.len() > 4 * 1024 * 1024 {
                return Ok(Err("分发静态载荷超过4MiB".into()));
            }
            Ok(Ok(payload))
        }
        Err(error)
            if error.downcast_ref::<EnvironmentUnavailable>().is_some()
                || error.downcast_ref::<std::io::Error>().is_some_and(|error| {
                    matches!(
                        error.kind(),
                        std::io::ErrorKind::NotFound
                            | std::io::ErrorKind::PermissionDenied
                            | std::io::ErrorKind::NotADirectory
                    )
                }) =>
        {
            Ok(Err(error.to_string()))
        }
        Err(error) => Err(error),
    }
}

/// 完整快照保全部group/value；只把选中literal group的合法模块部分加入候选。
/// 未选组的目标留轻量Outside，不把EntryPoint.Load的实时callee变成静态调用。
pub(crate) fn targets(payload: &str, groups: &[String]) -> Result<(Vec<String>, Vec<String>)> {
    let snapshot: serde_json::Value = serde_json::from_str(payload)?;
    let mut planned = BTreeSet::new();
    let mut outside = BTreeSet::new();
    for distribution in snapshot["distributions"]
        .as_array()
        .context("内部快照缺distributions")?
    {
        if distribution["shadowed"].as_bool() == Some(true) {
            continue;
        }
        for file in distribution["files"]
            .as_array()
            .context("内部快照缺files")?
        {
            if file["path"] != "entry_points.txt" {
                continue;
            }
            let bytes = files::unhex(file["hex"].as_str().context("内部快照缺hex")?)?;
            let Ok(text) = std::str::from_utf8(&bytes) else {
                continue;
            };
            let mut group = "";
            for line in text.lines().map(str::trim) {
                if let Some(value) = line
                    .strip_prefix('[')
                    .and_then(|value| value.strip_suffix(']'))
                {
                    group = value;
                    continue;
                }
                let Some((_, value)) = line.split_once('=') else {
                    continue;
                };
                let name = value.trim().split([':', '[']).next().unwrap_or("").trim();
                if !crate::module_hints::valid_name(name) {
                    continue;
                }
                if groups.iter().any(|wanted| wanted == group) {
                    planned.insert(name.to_owned());
                } else {
                    outside.insert(name.to_owned());
                }
            }
        }
    }
    outside.retain(|name| !planned.contains(name));
    Ok((planned.into_iter().collect(), outside.into_iter().collect()))
}
