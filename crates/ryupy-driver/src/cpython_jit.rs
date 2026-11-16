//! 显式 provider 的 JIT 候选事务：编译及完整依赖冻结成功之后才替换本工具拥有的输出目录。
use crate::{Artifacts, BuildOptions};
use anyhow::{Context, Result, ensure};
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Candidate(PathBuf);
impl Drop for Candidate {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

pub(super) fn build(
    opts: &BuildOptions,
    compile: impl FnOnce(&BuildOptions) -> Result<Artifacts>,
) -> Result<Artifacts> {
    let target = if opts.out_dir.is_absolute() {
        opts.out_dir.clone()
    } else {
        std::env::current_dir()?.join(&opts.out_dir)
    };
    let parent = target.parent().context("CPython JIT 输出缺少父目录")?;
    std::fs::create_dir_all(parent)?;
    if target.exists() {
        ensure!(
            std::fs::symlink_metadata(&target)?.is_dir()
                && target.join(super::cpython_provider::CONFIG).is_file(),
            "CPython JIT 只替换自己已有的完整输出目录，请选择新的 --out-dir"
        );
    }
    let (candidate, backup) = unique(parent)?;
    let guard = Candidate(candidate.clone());
    let mut inner = opts.clone();
    inner.out_dir = candidate.clone();
    let artifacts = compile(&inner)?;
    let dll = artifacts.dll.strip_prefix(&candidate)?.to_path_buf();
    let previous = target.exists();
    if previous {
        std::fs::rename(&target, &backup).context("无法移开上一份完整 CPython JIT 输出")?;
    }
    if let Err(error) = std::fs::rename(&candidate, &target) {
        if previous {
            std::fs::rename(&backup, &target)
                .context("CPython JIT 发布失败且恢复上一份输出失败")?;
        }
        return Err(error).context("无法安装本次完整 CPython JIT 输出");
    }
    drop(guard);
    if previous {
        std::fs::remove_dir_all(&backup).context("新 CPython JIT 输出已安装，但旧目录清理失败")?;
    }
    Ok(Artifacts {
        dll: target.join(dll),
        native_exe: None,
        manifest: None,
    })
}
fn unique(parent: &Path) -> Result<(PathBuf, PathBuf)> {
    for _ in 0..64 {
        let serial = NEXT.fetch_add(1, Ordering::Relaxed);
        let label = format!(".ryupy-cpython-jit-{}-{serial}", std::process::id());
        let candidate = parent.join(&label);
        let backup = parent.join(format!("{label}-previous"));
        if backup.exists() {
            continue;
        }
        match std::fs::create_dir(&candidate) {
            Ok(()) => return Ok((candidate, backup)),
            Err(error) if error.kind() == std::io::ErrorKind::AlreadyExists => continue,
            Err(error) => return Err(error.into()),
        }
    }
    anyhow::bail!("无法创建独占 CPython JIT 候选目录")
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn cpython_jit_failed_candidate_preserves_previous_complete_output() {
        let root = std::env::temp_dir().join(format!(
            "ryupy-cpython-jit-test-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        std::fs::create_dir(&root).unwrap();
        let target = root.join("out");
        std::fs::create_dir(&target).unwrap();
        std::fs::write(target.join(crate::cpython_provider::CONFIG), "{}").unwrap();
        std::fs::write(target.join("app.dll"), "previous").unwrap();
        let opts = BuildOptions {
            input: root.join("app.py"),
            out_dir: target.clone(),
            aot: false,
            search_paths: vec![],
        };
        let result = build(&opts, |inner| {
            std::fs::write(inner.out_dir.join("app.dll"), "partial")?;
            anyhow::bail!("planned failure")
        });
        assert!(result.is_err());
        assert_eq!(std::fs::read(target.join("app.dll")).unwrap(), b"previous");
        assert_eq!(std::fs::read_dir(&root).unwrap().count(), 1);
        std::fs::remove_dir_all(root).unwrap();
    }
}
