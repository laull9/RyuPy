//! 单文件发布事务：先构建完整 standalone，再封装到独占候选，保留上一版直到全部成功。
//! 所有权与诊断留在独立 build 目录；可分发的 .onefile 目录仅有唯一原生入口。
use crate::{Artifacts, BuildOptions, Session, manifest_files};
use anyhow::{Context, Result, ensure};
use serde::{Deserialize, Serialize};
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, Ordering};

const OWNER: &str = "owner.json";
static NEXT: AtomicU64 = AtomicU64::new(0);

#[derive(Serialize, Deserialize)]
#[serde(deny_unknown_fields)]
struct Owner {
    schema: u32,
    source: PathBuf,
    name: String,
    file: Option<FileState>,
}

/// 按现有产物所有权约定登记大小、修改时间和权限，不为普通文件引入摘要。
#[derive(PartialEq, Serialize, Deserialize)]
#[serde(deny_unknown_fields)]
struct FileState {
    size: u64,
    modified: u128,
    mode: u32,
}

struct Lock(PathBuf);
impl Drop for Lock {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir(&self.0);
    }
}

/// --onefile 隐含 NativeAOT；返回的 native_exe 指向仅含一个文件的发布目录。
/// dll/manifest 继续描述内嵌的 standalone，调用方应将清单标为内层清单。
pub fn build_onefile(
    sess: &mut Session,
    opts: &BuildOptions,
    resources: Option<&Path>,
    binding: Option<&Path>,
) -> Result<Artifacts> {
    let mut opts = opts.clone();
    opts.aot = true;
    crate::aot::validate_before_build(&opts.input, true)?;
    let stem = crate::assembly_name(&opts.input);
    crate::binding_build::validate_name(&stem)?;
    let source = opts.input.canonicalize()?;
    std::fs::create_dir_all(&opts.out_dir)?;
    let root = opts.out_dir.canonicalize()?;
    let target = root.join(format!("{stem}.onefile"));
    let work = root.join(format!("{stem}.onefile.build"));
    let lock = root.join(format!("{stem}.onefile.lock"));
    std::fs::create_dir(&lock).context("同名单文件构建已有锁，拒绝并发或崩溃后自动接管")?;
    let _lock = Lock(lock);
    let name = if cfg!(windows) {
        format!("{stem}.exe")
    } else {
        stem
    };
    let mut owner = load_owner(&work, &target, source, name)?;
    verify_target(&target, &owner)?;
    let time = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)?
        .as_nanos();
    let attempt = work.join(format!(
        "attempt-{}-{time}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&attempt)?;
    let result = (|| -> Result<Artifacts> {
        let mut artifacts = crate::build_configured(sess, &opts, resources, binding)?;
        let native = artifacts
            .native_exe
            .as_ref()
            .context("standalone 没有原生入口")?;
        let dist = native.parent().context("standalone 没有发布目录")?;
        let candidate = attempt.join("candidate");
        std::fs::create_dir(&candidate)?;
        let executable = crate::onefile::package(dist, native, &candidate.join(&owner.name))?;
        let ready = file_state(&executable)?;
        // 构建期间旧输出可能被外部修改，提交前再次核对，避免接管用户文件。
        verify_target(&target, &owner)?;
        let backup = attempt.join("previous");
        let previous = owner.file.is_some();
        if previous {
            std::fs::rename(&target, &backup)?;
        }
        if let Err(error) = std::fs::rename(&candidate, &target) {
            if previous {
                std::fs::rename(&backup, &target).context("单文件安装失败且旧版本恢复失败")?;
            }
            return Err(error.into());
        }
        owner.file = Some(ready);
        let commit =
            verify_target(&target, &owner).and_then(|()| save_owner(&work, &attempt, &owner));
        if let Err(error) = commit {
            std::fs::rename(&target, &candidate).context("单文件提交失败且候选无法保留")?;
            if previous {
                std::fs::rename(&backup, &target).context("单文件提交失败且旧版本恢复失败")?;
            }
            return Err(error);
        }
        artifacts.native_exe = Some(target.join(&owner.name));
        Ok(artifacts)
    })();
    match result {
        Ok(artifacts) => {
            std::fs::remove_dir_all(&attempt).context("单文件已发布但本轮工作目录清理失败")?;
            Ok(artifacts)
        }
        Err(error) => {
            std::fs::write(attempt.join("build-error.log"), format!("{error:#}\n"))?;
            Err(error.context(format!("单文件失败现场：{}", attempt.display())))
        }
    }
}

fn load_owner(work: &Path, target: &Path, source: PathBuf, name: String) -> Result<Owner> {
    match std::fs::symlink_metadata(work) {
        Ok(_) => {
            ensure!(
                manifest_files::plain_metadata(work)?.is_dir(),
                "单文件 build 不是普通目录"
            );
            let path = work.join(OWNER);
            ensure!(
                manifest_files::plain_metadata(&path)?.len() <= 1_048_576,
                "单文件 owner 过大"
            );
            let owner: Owner = serde_json::from_slice(&std::fs::read(path)?)?;
            ensure!(
                owner.schema == 1 && owner.source == source && owner.name == name,
                "已有单文件 build 没有匹配所有权"
            );
            Ok(owner)
        }
        Err(error) if error.kind() == std::io::ErrorKind::NotFound => {
            ensure!(!exists(target)?, "单文件目录已存在但没有工具所有权");
            std::fs::create_dir(work)?;
            let owner = Owner {
                schema: 1,
                source,
                name,
                file: None,
            };
            let bytes = serde_json::to_vec_pretty(&owner)?;
            std::fs::write(work.join(OWNER), bytes)?;
            Ok(owner)
        }
        Err(error) => Err(error.into()),
    }
}

fn save_owner(work: &Path, attempt: &Path, owner: &Owner) -> Result<()> {
    let next = attempt.join("next-owner.json");
    std::fs::write(&next, serde_json::to_vec_pretty(owner)?)?;
    std::fs::rename(next, work.join(OWNER)).context("无法提交单文件所有权")
}

fn verify_target(target: &Path, owner: &Owner) -> Result<()> {
    if let Some(expected) = &owner.file {
        ensure!(
            manifest_files::plain_metadata(target)?.is_dir(),
            "单文件发布目录不是普通目录"
        );
        let entries: Vec<_> = std::fs::read_dir(target)?.collect::<std::io::Result<_>>()?;
        ensure!(
            entries.len() == 1 && entries[0].file_name() == owner.name.as_str(),
            "单文件目录被修改或加入其他文件"
        );
        ensure!(
            file_state(&target.join(&owner.name))? == *expected,
            "已有单文件被修改，拒绝覆盖"
        );
    } else {
        ensure!(!exists(target)?, "出现未拥有的单文件发布目录");
    }
    Ok(())
}

fn exists(path: &Path) -> Result<bool> {
    match std::fs::symlink_metadata(path) {
        Ok(_) => Ok(true),
        Err(error) if error.kind() == std::io::ErrorKind::NotFound => Ok(false),
        Err(error) => Err(error.into()),
    }
}

fn file_state(path: &Path) -> Result<FileState> {
    let metadata = manifest_files::plain_metadata(path)?;
    ensure!(metadata.is_file(), "单文件入口不是普通文件");
    #[cfg(unix)]
    let mode = {
        use std::os::unix::fs::PermissionsExt;
        metadata.permissions().mode()
    };
    #[cfg(not(unix))]
    let mode = u32::from(metadata.permissions().readonly());
    Ok(FileState {
        size: metadata.len(),
        modified: metadata
            .modified()?
            .duration_since(std::time::UNIX_EPOCH)?
            .as_nanos(),
        mode,
    })
}
