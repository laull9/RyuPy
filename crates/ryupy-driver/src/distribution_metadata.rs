//! 显式封闭分发快照；仅driver读文件，完整保留未知元数据，不把源码图当发现环境。
use anyhow::{Context, Result, ensure};
use serde::{Deserialize, Serialize};
use std::collections::BTreeSet;
use std::path::{Path, PathBuf};

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
        ensure!(ids.insert(root.id.clone()), "分发根id重复");
        result.roots.push(root.id.clone());
        let path = base.join(&root.path);
        files::plain_path(&path)?;
        ensure!(path.is_dir(), "分发根必须为普通目录");
        for (name, child) in files::children(&path)? {
            if name.ends_with(".zip") || name.ends_with(".egg") {
                anyhow::bail!("封闭分发根不支持zip/egg archive：{name}");
            }
            let kind = if name.ends_with(".dist-info") {
                "dist-info"
            } else if name.ends_with(".egg-info") {
                "egg-info"
            } else {
                continue;
            };
            ensure!(result.distributions.len() < 256, "分发数超过256");
            files::plain_path(&child)?;
            let mut records = Vec::new();
            let mut directories = Vec::new();
            if child.is_dir() {
                files::walk(&child, "", &mut records, &mut directories, &mut budget, 0)?;
            } else {
                ensure!(kind == "egg-info", "dist-info必须为目录");
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
                .context("分发缺少身份metadata")?;
            let text = String::from_utf8(files::unhex(&source.hex)?)
                .context("分发身份metadata不是UTF-8")?;
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
    ensure!(
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
    }
    Ok(distribution_payload)
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
