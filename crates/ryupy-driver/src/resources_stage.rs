//! ILC 前冻结资源字节，发布阶段只消费快照；句柄身份与 portable 目标分别检查，不按内容相同合并来源。
use super::paths::{self, Kind, Targets};
use super::{FILE_LIMIT, ModuleLocationRecord, ResourcePlan, ResourceRecord, TOTAL_LIMIT};
use crate::manifest_files::{FileDigest, plain_metadata};
use anyhow::{Context, Result, ensure};
use same_file::Handle;
use sha2::{Digest, Sha256};
use std::collections::{BTreeMap, BTreeSet};
use std::fs::{File, Metadata, OpenOptions};
use std::io::{Read, Write};
use std::path::{Path, PathBuf};

struct Snapshot {
    identity: Handle,
    path: PathBuf,
    size: u64,
    hash: String,
}
pub(crate) struct StagedResources {
    pub(crate) locations: Vec<ModuleLocationRecord>,
    pub(crate) records: Vec<ResourceRecord>,
    files: Vec<PathBuf>,
    anchors: Vec<String>,
    pub(crate) automatic: Option<crate::default_aot::Audit>,
}

pub(super) fn checked_total(current: u64, additional: u64) -> Result<u64> {
    let total = current.checked_add(additional).context("资源总大小溢出")?;
    ensure!(total <= TOTAL_LIMIT, "资源总大小超过64MiB");
    Ok(total)
}

fn unchanged(before: &Metadata, after: &Metadata) -> Result<()> {
    #[cfg(unix)]
    {
        use std::os::unix::fs::MetadataExt;
        ensure!(
            before.dev() == after.dev() && before.ino() == after.ino(),
            "文件身份在打开时变化"
        );
    }
    ensure!(
        before.len() == after.len() && before.modified()? == after.modified()?,
        "资源在冻结期间发生变化"
    );
    Ok(())
}
fn open_plain(path: &Path) -> Result<Handle> {
    let before = plain_metadata(path)?;
    ensure!(before.is_file(), "资源/源码身份对象不是普通文件");
    let file = File::open(path)?;
    let opened = file.metadata()?;
    ensure!(opened.is_file(), "打开后的资源/源码不是普通文件");
    unchanged(&before, &opened)?;
    Ok(Handle::from_file(file)?)
}
fn same_target_source(first: usize, next: usize, snapshots: &[Snapshot]) -> bool {
    // Windows 的旧 64 位 file index 不能证明 ReFS 同源；仅同一原 lexical 记录复用同一活句柄。
    if cfg!(windows) {
        first == next
    } else {
        snapshots[first].identity == snapshots[next].identity
    }
}

pub(super) fn freeze(plan: &ResourcePlan, root: &Path) -> Result<StagedResources> {
    let stage = root.join("resource-staging");
    std::fs::create_dir(&stage).context("资源 staging 必须为本次独占新目录")?;
    // 编译源码全部持活句柄，禁止资源经 hardlink/其它后缀部署源码。Windows 相等仅保守拒绝。
    let mut compiled = Vec::new();
    let mut hashes = Vec::new();
    let mut compiled_keys = BTreeMap::new();
    let mut source_ids = Vec::new();
    for (module_id, path) in plan.source_files.iter().enumerate() {
        let expected = plan.source_lengths[module_id];
        let id = if let Some(id) = compiled_keys.get(path) {
            *id
        } else {
            let identity = open_plain(path)?;
            let before = identity.as_file().metadata()?;
            ensure!(before.len() == expected, "编译源码在资源冻结前长度变化");
            let mut input = identity
                .as_file()
                .take(expected.checked_add(1).context("源码长度溢出")?);
            let mut size = 0_u64;
            let mut hash = Sha256::new();
            let mut buffer = [0_u8; 65536];
            loop {
                let count = input.read(&mut buffer)?;
                if count == 0 {
                    break;
                }
                size += count as u64;
                ensure!(size <= expected, "编译源码在资源冻结时增长");
                hash.update(&buffer[..count]);
            }
            ensure!(size == expected, "编译源码在资源冻结时截短");
            unchanged(&before, &identity.as_file().metadata()?)?;
            let verified = open_plain(path)?;
            ensure!(verified == identity, "编译源码在资源冻结期间被替换");
            unchanged(&before, &verified.as_file().metadata()?)?;
            let id = compiled.len();
            compiled.push(identity);
            hashes.push(format!("{:x}", hash.finalize()));
            compiled_keys.insert(path.clone(), id);
            id
        };
        ensure!(
            hashes[id] == plan.source_hashes[module_id],
            "编译源码在资源冻结前已变化"
        );
        source_ids.push(id);
    }
    let mut anchors: BTreeMap<&str, usize> = BTreeMap::new();
    for location in &plan.locations {
        let id = location.module_id as usize;
        if let Some(previous) = anchors.insert(&location.relative_file, id) {
            let same = if cfg!(windows) {
                source_ids[previous] == source_ids[id]
            } else {
                compiled[source_ids[previous]] == compiled[source_ids[id]]
            };
            ensure!(same, "同一模块位置锚点来自不同源码");
        }
    }
    let mut cache: BTreeMap<PathBuf, usize> = BTreeMap::new();
    let mut snapshots: Vec<Snapshot> = Vec::new();
    let mut records = Vec::new();
    let mut files = Vec::new();
    let mut total = 0_u64;
    let mut frozen_total = 0_u64;
    for (target, claims) in &plan.claims {
        let mut chosen = None;
        let mut consumers = BTreeSet::new();
        for claim in claims {
            let parent = plan.source_files[claim.source_module]
                .parent()
                .context("源码没有父目录")?;
            // 不 canonicalize 此 key；另一拼写不能变成 Windows 的同一已验证记录。
            let lexical = parent.join(&claim.file);
            let index = if let Some(index) = cache.get(&lexical) {
                *index
            } else {
                let checked = paths::checked_source(parent, &claim.file).with_context(|| {
                    format!(
                        "无法冻结资源 {}（源码模块 {}）",
                        claim.file, claim.source_module
                    )
                })?;
                let identity = open_plain(&checked)?;
                ensure!(
                    !compiled.iter().any(|source| source == &identity),
                    "不能把本次编译源码声明为资源"
                );
                // Unix 上真同 inode 可共用同一冻结快照；Windows 不以64位index放行别名合并。
                let prior = if cfg!(windows) {
                    None
                } else {
                    snapshots.iter().position(|s| s.identity == identity)
                };
                if let Some(prior) = prior {
                    cache.insert(lexical, prior);
                    prior
                } else {
                    let before = identity.as_file().metadata()?;
                    ensure!(before.len() <= FILE_LIMIT, "单资源超过16MiB");
                    frozen_total = checked_total(frozen_total, before.len())?;
                    let index = snapshots.len();
                    let frozen = stage.join(index.to_string());
                    let mut output = OpenOptions::new()
                        .write(true)
                        .create_new(true)
                        .open(&frozen)?;
                    let mut hasher = Sha256::new();
                    let mut size = 0_u64;
                    let mut buffer = [0_u8; 65536];
                    let mut input = identity.as_file();
                    loop {
                        let count = input.read(&mut buffer)?;
                        if count == 0 {
                            break;
                        }
                        size += count as u64;
                        ensure!(
                            size <= FILE_LIMIT && size <= before.len(),
                            "资源读取时增长或超过16MiB"
                        );
                        checked_total(frozen_total - before.len(), size)?;
                        output.write_all(&buffer[..count])?;
                        hasher.update(&buffer[..count]);
                    }
                    output.flush()?;
                    output.sync_all()?;
                    unchanged(&before, &identity.as_file().metadata()?)?;
                    let after_path = open_plain(&checked)?;
                    ensure!(after_path == identity, "资源路径在冻结期间被替换");
                    unchanged(&before, &after_path.as_file().metadata()?)?;
                    ensure!(size == before.len(), "资源读取长度变化");
                    snapshots.push(Snapshot {
                        identity,
                        path: frozen,
                        size,
                        hash: format!("{:x}", hasher.finalize()),
                    });
                    cache.insert(lexical, index);
                    index
                }
            };
            if let Some(previous) = chosen {
                ensure!(
                    same_target_source(previous, index, &snapshots),
                    "同一资源目标来自不同源文件（Windows 不合并别名路径）"
                );
            } else {
                chosen = Some(index);
            }
            consumers.extend(claim.consumers.iter().copied());
        }
        let snapshot = &snapshots[chosen.context("资源声明缺少来源")?];
        total = checked_total(total, snapshot.size)?;
        records.push(ResourceRecord {
            relative_path: target.clone(),
            size: snapshot.size,
            sha256: snapshot.hash.clone(),
            consumer_module_ids: consumers.into_iter().collect(),
        });
        files.push(snapshot.path.clone());
    }
    Ok(StagedResources {
        locations: if let Some(audit) = &plan.automatic {
            plan.locations
                .iter()
                .filter_map(|location| {
                    audit.source_to_emitted[location.module_id as usize].map(|id| {
                        let mut projected = location.clone();
                        projected.module_id = id;
                        projected
                    })
                })
                .collect()
        } else {
            plan.locations.clone()
        },
        automatic: plan.automatic.clone(),
        anchors: plan
            .locations
            .iter()
            .map(|l| l.relative_file.clone())
            .collect(),
        records,
        files,
    })
}

/// staging 与已发布资源重核同走有界读取；检查活句柄前后状态，不退回路径无界 digest。
fn copy_snapshot(path: &Path, record: &ResourceRecord, output: &mut dyn Write) -> Result<()> {
    let identity = open_plain(path)?;
    let before = identity.as_file().metadata()?;
    ensure!(before.len() == record.size, "资源快照长度变化");
    let mut input = identity.as_file().take(record.size + 1);
    let mut copied = 0_u64;
    let mut hash = Sha256::new();
    let mut buffer = [0_u8; 65536];
    loop {
        let count = input.read(&mut buffer)?;
        if count == 0 {
            break;
        }
        copied += count as u64;
        ensure!(copied <= record.size, "资源快照读取时增长");
        output.write_all(&buffer[..count])?;
        hash.update(&buffer[..count]);
    }
    ensure!(
        copied == record.size && format!("{:x}", hash.finalize()) == record.sha256,
        "资源快照字节与冻结记录不同"
    );
    unchanged(&before, &identity.as_file().metadata()?)?;
    let after = open_plain(path)?;
    ensure!(after == identity, "资源快照路径在读取时被替换");
    unchanged(&before, &after.as_file().metadata()?)?;
    Ok(())
}

impl StagedResources {
    pub(crate) fn publish(&self, root: &Path) -> Result<()> {
        let mut targets = Targets::default();
        targets.insert(crate::manifest_files::MANIFEST_NAME, Kind::Reserved)?;
        targets.insert(".ryupy-artifact.json.tmp", Kind::Reserved)?;
        // Deferred虽无输出module_id，其虚拟源锚仍不能被发布文件覆盖。
        for anchor in &self.anchors {
            targets.insert(anchor, Kind::Anchor)?;
        }
        for record in &self.records {
            targets.insert(&record.relative_path, Kind::Resource)?;
        }
        targets.include_publish(root)?;
        for (index, (record, source)) in self.records.iter().zip(&self.files).enumerate() {
            ensure!(
                plain_metadata(source)?.len() == record.size,
                "staging 快照长度变化"
            );
            let destination = root.join(&record.relative_path);
            let parent = destination.parent().context("资源目标缺父目录")?;
            let mut current = root.to_owned();
            for component in parent.strip_prefix(root)?.components() {
                current.push(component);
                if current.exists() {
                    ensure!(plain_metadata(&current)?.is_dir(), "资源目标祖先不是目录");
                } else {
                    std::fs::create_dir(&current)?;
                }
            }
            let temporary = root.join(format!(".ryupy-resource-{index}.tmp"));
            let mut output = OpenOptions::new()
                .write(true)
                .create_new(true)
                .open(&temporary)?;
            copy_snapshot(source, record, &mut output)?;
            output.flush()?;
            output.sync_all()?;
            drop(output);
            ensure!(!destination.try_exists()?, "资源发布不能覆盖已有项");
            std::fs::rename(&temporary, &destination)?;
            copy_snapshot(&destination, record, &mut std::io::sink())?;
        }
        Ok(())
    }
    pub(crate) fn mark_artifacts(&self, artifacts: &mut [FileDigest]) -> Result<()> {
        for resource in &self.records {
            let artifact = artifacts
                .iter_mut()
                .find(|a| a.relative_path == resource.relative_path)
                .context("发布清单缺少已声明资源")?;
            ensure!(
                artifact.size == resource.size && artifact.sha256 == resource.sha256,
                "资源清单哈希不一致"
            );
            artifact.role = "declared_resource".to_owned();
        }
        Ok(())
    }
}
