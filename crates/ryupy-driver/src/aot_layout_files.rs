//! 布局事务的有限树快照：记录普通文件/目录及权限，拒绝链接，不把目录名当作所有权。
use crate::manifest_files as files;
use anyhow::{Context, Result, ensure};
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};
use std::io::Read;
use std::path::Path;
pub(super) const JSON_LIMIT: u64 = 32 * 1024 * 1024;
const FILE_LIMIT: u64 = 8 * 1024 * 1024 * 1024;
const TREE_LIMIT: u64 = 64 * 1024 * 1024 * 1024;

pub(super) const OWNER: &str = "ryupy-build-owner.json";
#[derive(Clone, Debug, Serialize, Deserialize, PartialEq, Eq)]
#[serde(deny_unknown_fields)]
pub(super) struct Node {
    path: String,
    directory: bool,
    size: u64,
    sha256: String,
    mode: u32,
}
fn mode(m: &std::fs::Metadata) -> u32 {
    #[cfg(unix)]
    {
        use std::os::unix::fs::PermissionsExt;
        m.permissions().mode() & 0o7777
    }
    #[cfg(not(unix))]
    {
        u32::from(m.permissions().readonly())
    }
}
/// 只排除根owner文件；其它新增文件（包括未完成owner临时文件）必须导致核验失败。
pub(super) fn snapshot(root: &Path, owner: bool) -> Result<Vec<Node>> {
    ensure!(files::plain_metadata(root)?.is_dir(), "布局根不是普通目录");
    let mut result = Vec::new();
    walk(root, root, owner, 0, &mut 0, &mut result)?;
    result.sort_by(|a, b| a.path.cmp(&b.path));
    Ok(result)
}
fn walk(
    root: &Path,
    dir: &Path,
    owner: bool,
    depth: usize,
    total: &mut u64,
    out: &mut Vec<Node>,
) -> Result<()> {
    ensure!(depth <= 64, "布局树深度超过64");
    for entry in std::fs::read_dir(dir)? {
        ensure!(out.len() < 100_000, "布局树条目超过100000");
        let path = entry?.path();
        let relative = path
            .strip_prefix(root)?
            .iter()
            .map(|p| p.to_str().context("布局文件名不是UTF8"))
            .collect::<Result<Vec<_>>>()?
            .join("/");
        if owner && relative == OWNER {
            ensure!(files::plain_metadata(&path)?.is_file(), "owner不是普通文件");
            continue;
        }
        files::safe_relative(&relative)?;
        let meta = files::plain_metadata(&path)?;
        let directory = meta.is_dir();
        let digest = if directory {
            None
        } else {
            ensure!(meta.len() <= FILE_LIMIT, "布局单文件超过8GiB");
            *total = total.checked_add(meta.len()).context("布局总大小溢出")?;
            ensure!(
                *total <= TREE_LIMIT,
                "布局历史树超过64GiB；请保留诊断后人工选择新输出ROOT"
            );
            Some(bounded_hash(&path, meta.len())?)
        };
        out.push(Node {
            path: relative,
            directory,
            size: if directory { 0 } else { meta.len() },
            sha256: digest.unwrap_or_default(),
            mode: mode(&meta),
        });
        if directory {
            walk(root, &path, owner, depth + 1, total, out)?;
        }
    }
    Ok(())
}
pub(super) fn exists(path: &Path) -> Result<bool> {
    match std::fs::symlink_metadata(path) {
        Ok(_) => Ok(true),
        Err(e) if e.kind() == std::io::ErrorKind::NotFound => Ok(false),
        Err(e) => Err(e.into()),
    }
}
/// 使用已核快照复制整个publish；copy保权限，复制后以及源树均重新核对。
pub(super) fn copy_tree(source: &Path, target: &Path) -> Result<()> {
    let before = snapshot(source, false)?;
    std::fs::create_dir(target)?;
    for node in &before {
        let from = source.join(&node.path);
        let to = target.join(&node.path);
        if node.directory {
            std::fs::create_dir(&to)?;
        } else {
            let mut input = std::fs::File::open(&from)?.take(node.size + 1);
            let mut output = std::fs::OpenOptions::new()
                .write(true)
                .create_new(true)
                .open(&to)?;
            ensure!(
                std::io::copy(&mut input, &mut output)? == node.size,
                "复制源大小变化"
            );
            output.sync_all()?;
        }
        std::fs::set_permissions(&to, std::fs::metadata(&from)?.permissions())?;
    }
    ensure!(
        snapshot(source, false)? == before && snapshot(target, false)? == before,
        "复制发布树期间内容或权限变化"
    );
    Ok(())
}
/// 原清单在SDK阶段已验证；这里再次严格核全部文件集合/长度/hash，绝不忽略未分类旁件。
pub(super) fn verify_manifest(root: &Path) -> Result<serde_json::Value> {
    let path = root.join(files::MANIFEST_NAME);
    let value: serde_json::Value = serde_json::from_slice(&read_json(&path)?)?;
    ensure!(
        value["kind"] == "ryupy-native-aot-artifact"
            && matches!(value["schema_version"].as_u64(), Some(1..=6)),
        "布局只接受AOT清单1至6"
    );
    let artifacts = value["artifacts"].as_array().context("清单缺artifacts")?;
    ensure!(artifacts.len() <= 100_000, "清单artifacts条目超过预算");
    let snapshot = snapshot(root, false)?;
    if matches!(value["schema_version"].as_u64(), Some(2 | 4..=6)) {
        validate_resource_names(&value, &snapshot)?;
    }
    let by_path: std::collections::BTreeMap<_, _> = snapshot
        .iter()
        .filter(|n| !n.directory)
        .map(|n| (n.path.as_str(), n))
        .collect();
    let mut expected = std::collections::BTreeSet::new();
    let mut native = 0;
    for item in artifacts {
        let name = item["relative_path"].as_str().context("产物缺路径")?;
        files::safe_relative(name)?;
        ensure!(
            name != files::MANIFEST_NAME && expected.insert(name.to_owned()),
            "清单重复/自引用产物"
        );
        let size = item["size"].as_u64().context("产物缺size")?;
        let actual = by_path.get(name).copied().context("清单产物不在真实树")?;
        ensure!(
            actual.size == size && item["sha256"].as_str() == Some(actual.sha256.as_str()),
            "清单与产物hash/size不一致"
        );
        if item["role"] == "native_executable" {
            native += 1;
        }
    }
    expected.insert(files::MANIFEST_NAME.into());
    let actual: std::collections::BTreeSet<_> = snapshot
        .into_iter()
        .filter(|n| !n.directory)
        .map(|n| n.path)
        .collect();
    ensure!(
        native == 1 && actual == expected,
        "清单文件集合或native数量不符"
    );
    Ok(value)
}
pub(super) fn relocate(root: &Path, old: &str, new: &str) -> Result<()> {
    crate::binding_build::validate_name(new)?;
    let mut value = verify_manifest(root)?;
    if matches!(value["schema_version"].as_u64(), Some(2 | 4..=6)) {
        ensure!(
            ![
                "_internal",
                files::MANIFEST_NAME,
                ".ryupy-artifact.json.tmp"
            ]
            .iter()
            .any(|reserved| new.eq_ignore_ascii_case(reserved)),
            "最终native与资源发布保留项冲突"
        );
        ensure!(
            !snapshot(root, false)?
                .iter()
                .any(|n| n.path != old && n.path.eq_ignore_ascii_case(new)),
            "最终native与资源树大小写冲突"
        );
    }
    let entries = value["artifacts"].as_array_mut().context("缺artifacts")?;
    let entry = entries
        .iter_mut()
        .find(|v| v["role"] == "native_executable")
        .context("缺native")?;
    ensure!(entry["relative_path"] == old, "报告native与清单不同");
    if old != new {
        files::ensure_absent(&root.join(new))?;
        std::fs::rename(root.join(old), root.join(new))?;
        entry["relative_path"] = new.into();
    }
    entries.sort_by(|a, b| {
        a["relative_path"]
            .as_str()
            .cmp(&b["relative_path"].as_str())
    });
    let mut bytes = serde_json::to_vec_pretty(&value)?;
    bytes.push(b'\n');
    // 仅本轮独占候选副本；原publish清单和旧dist完全不动。
    std::fs::write(root.join(files::MANIFEST_NAME), bytes)?;
    verify_manifest(root)?;
    Ok(())
}

/// 有限读取并在同活handle前后核身份/mtime；不把stat长度当作实际读取上限的替代。
fn bounded_hash(path: &Path, size: u64) -> Result<String> {
    ensure!(size <= FILE_LIMIT, "布局单文件超过8GiB");
    let before = files::plain_metadata(path)?;
    ensure!(
        before.is_file() && before.len() == size,
        "文件类型/大小变化"
    );
    let file = std::fs::File::open(path)?;
    let handle = same_file::Handle::from_file(file)?;
    ensure!(
        handle == same_file::Handle::from_path(path)?,
        "文件打开身份变化"
    );
    let opened = handle.as_file().metadata()?;
    ensure!(
        opened.len() == size && opened.modified()? == before.modified()?,
        "文件打开元数据变化"
    );
    let mut reader = handle.as_file().take(size + 1);
    let mut hash = Sha256::new();
    let mut buffer = [0u8; 65536];
    let mut count = 0u64;
    loop {
        let n = reader.read(&mut buffer)?;
        if n == 0 {
            break;
        }
        count += n as u64;
        hash.update(&buffer[..n]);
    }
    let after = files::plain_metadata(path)?;
    let closed = handle.as_file().metadata()?;
    ensure!(
        count == size
            && after.len() == size
            && closed.len() == size
            && before.modified()? == after.modified()?
            && opened.modified()? == closed.modified()?
            && handle == same_file::Handle::from_path(path)?,
        "文件读取期间变化"
    );
    Ok(format!("{:x}", hash.finalize()))
}
pub(super) fn read_json(path: &Path) -> Result<Vec<u8>> {
    let before = files::plain_metadata(path)?;
    ensure!(
        before.is_file() && before.len() <= JSON_LIMIT,
        "布局JSON超过32MiB或不是文件"
    );
    let digest = bounded_hash(path, before.len())?;
    let mut bytes = Vec::new();
    std::fs::File::open(path)?
        .take(JSON_LIMIT + 1)
        .read_to_end(&mut bytes)?;
    ensure!(
        bytes.len() as u64 == before.len() && files::hash_bytes(&bytes) == digest,
        "布局JSON读取变化"
    );
    Ok(bytes)
}

// 仅native角色允许顶层Unicode basename；这不是资源路径Unicode支持或跨文件系统全量等价。
fn validate_resource_names(value: &serde_json::Value, nodes: &[Node]) -> Result<()> {
    let artifacts = value["artifacts"].as_array().context("缺artifacts")?;
    let native = artifacts
        .iter()
        .find(|a| a["role"] == "native_executable")
        .and_then(|a| a["relative_path"].as_str())
        .context("缺native路径")?;
    crate::binding_build::validate_name(native)?;
    ensure!(
        ![
            "_internal",
            files::MANIFEST_NAME,
            ".ryupy-artifact.json.tmp"
        ]
        .iter()
        .any(|reserved| native.eq_ignore_ascii_case(reserved)),
        "native与资源发布保留项冲突"
    );
    let mut names = std::collections::BTreeSet::new();
    for node in nodes {
        if node.path != native || node.directory {
            crate::resources::validate_layout_artifact_path(&node.path)?;
        }
        ensure!(
            names.insert(node.path.to_ascii_lowercase()),
            "发布树ASCII大小写冲突"
        );
    }
    Ok(())
}
