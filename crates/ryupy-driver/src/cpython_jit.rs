//! JIT应用候选事务：官方apphost与唯一internal依赖根完整冻结后才替换工具拥有的输出目录。
use crate::{Artifacts, BuildOptions};
use anyhow::{Context, Result, ensure};
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Candidate {
    path: PathBuf,
    retained: bool,
}
impl Drop for Candidate {
    fn drop(&mut self) {
        if !self.retained {
            let _ = std::fs::remove_dir_all(&self.path);
        }
    }
}
impl Candidate {
    fn retain_reports(
        &mut self,
        target: &Path,
        input: &Path,
        error: anyhow::Error,
    ) -> anyhow::Error {
        // 只由同次producer已知的报告名触发保留；不把任意用户文件当诊断证明。
        let source = format!("{}.source-plan.json", crate::assembly_name(input));
        let reports = [
            source.as_str(),
            "module-environment.json",
            "optional-sources.json",
            "distribution-metadata.json",
        ];
        if !reports.iter().any(|name| {
            std::fs::symlink_metadata(self.path.join(name)).is_ok_and(|meta| meta.is_file())
        }) {
            return error;
        }
        // 先交出Drop清理权：目录分配/rename失败也保留原候选，并报告真实位置。
        self.retained = true;
        let mut label = target.file_name().unwrap_or_default().to_os_string();
        label.push(format!(
            ".build-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        let destination = target.with_file_name(label);
        let moved = (|| -> Result<()> {
            // 独占创建防止rename覆盖已有目录；不接管任何历史诊断目录。
            std::fs::create_dir(&destination)?;
            std::fs::rename(&self.path, destination.join("attempt"))?;
            Ok(())
        })();
        match moved {
            Ok(()) => error.context(format!(
                "JIT失败诊断目录：{}",
                destination.join("attempt").display()
            )),
            Err(rename) => error.context(format!(
                "JIT失败诊断目录：{}（整理诊断失败：{rename:#}）",
                self.path.display()
            )),
        }
    }
}

pub(super) fn build(
    opts: &BuildOptions,
    compile: impl FnOnce(&BuildOptions) -> Result<Artifacts>,
) -> Result<Artifacts> {
    crate::aot::validate_before_build(&opts.input, false)?;
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
                && (crate::application_layout::is_canonical(&target)?
                    || target.join(super::cpython_provider::CONFIG).is_file()
                    || std::fs::read_dir(&target)?.next().is_none()),
            "JIT只替换自己已有的完整输出目录，请选择新的 --out-dir"
        );
    }
    let (candidate, backup) = unique(parent)?;
    let mut guard = Candidate {
        path: candidate.clone(),
        retained: false,
    };
    let mut inner = opts.clone();
    inner.out_dir = candidate.clone();
    let result = publish(&inner, &target, &candidate, &backup, compile);
    result.map_err(|error| guard.retain_reports(&target, &opts.input, error))
}

fn publish(
    inner: &BuildOptions,
    target: &Path,
    candidate: &Path,
    backup: &Path,
    compile: impl FnOnce(&BuildOptions) -> Result<Artifacts>,
) -> Result<Artifacts> {
    let artifacts = compile(inner)?;
    artifacts
        .dll
        .strip_prefix(candidate)
        .context("JIT产物不属于本次候选")?;
    let source = artifacts
        .dll
        .parent()
        .context("JIT程序集缺少目录")?
        .to_path_buf();
    let dll_name = artifacts.dll.file_name().context("JIT程序集缺少文件名")?;
    let manifest_bytes = artifacts.manifest.as_ref().map(std::fs::read).transpose()?;
    if let Some(path) = &artifacts.manifest {
        std::fs::remove_file(path)?;
    }
    let apphost = crate::jit_apphost::create(&source, &artifacts.dll)?;
    crate::application_layout::canonicalize(&source, &apphost, "apphost")?;
    let manifest = if let Some(bytes) = manifest_bytes {
        Some(crate::application_layout::finish_jit_manifest(
            &source, &apphost, &bytes,
        )?)
    } else {
        None
    };
    let entry = apphost.file_name().context("JIT入口缺少文件名")?.to_owned();
    let previous = target.exists();
    if previous {
        std::fs::rename(target, backup).context("无法移开上一份完整 CPython JIT 输出")?;
    }
    if let Err(error) = std::fs::rename(&source, target) {
        if previous {
            std::fs::rename(backup, target).context("CPython JIT 发布失败且恢复上一份输出失败")?;
        }
        return Err(error).context("无法安装本次完整 CPython JIT 输出");
    }
    if previous {
        std::fs::remove_dir_all(backup).context("新 CPython JIT 输出已安装，但旧目录清理失败")?;
    }
    Ok(Artifacts {
        dll: target
            .join(crate::application_layout::DIRECTORY)
            .join(dll_name),
        native_exe: Some(target.join(entry)),
        manifest: manifest
            .map(|_| crate::application_layout::manifest_path(target))
            .transpose()?,
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
    fn failed_source_plan_is_kept_outside_the_unpublished_product() {
        let root = crate::test_temp::Root::new("jit-failed-report");
        let opts = BuildOptions {
            input: root.join("main.py"),
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        };
        let error = build(&opts, |inner| {
            std::fs::write(
                inner.out_dir.join("main.source-plan.json"),
                "source evidence",
            )?;
            anyhow::bail!("planned semantic failure")
        })
        .unwrap_err();
        let message = error.to_string();
        let location = PathBuf::from(message.strip_prefix("JIT失败诊断目录：").unwrap());
        assert!(location.starts_with(&root));
        assert!(!opts.out_dir.exists());
        assert_eq!(
            std::fs::read(location.join("main.source-plan.json")).unwrap(),
            b"source evidence"
        );
        assert_eq!(error.root_cause().to_string(), "planned semantic failure");
    }

    #[test]
    fn failed_report_relocation_keeps_the_original_candidate_after_drop() {
        let root = crate::test_temp::Root::new("jit-report-rename-fail");
        let path = root.join("owned-candidate");
        std::fs::create_dir(&path).unwrap();
        std::fs::write(path.join("main.source-plan.json"), "original evidence").unwrap();
        let mut candidate = Candidate {
            path: path.clone(),
            retained: false,
        };
        // 过长目标在文件系统分配阶段失败，证实Drop清理权已在任何rename尝试前交出。
        let target = root.join("x".repeat(1024));
        let error = candidate.retain_reports(
            &target,
            &root.join("main.py"),
            anyhow::anyhow!("first error"),
        );
        assert!(
            error
                .to_string()
                .starts_with(&format!("JIT失败诊断目录：{}（", path.display()))
        );
        assert_eq!(error.root_cause().to_string(), "first error");
        drop(candidate);
        assert_eq!(
            std::fs::read(path.join("main.source-plan.json")).unwrap(),
            b"original evidence"
        );
    }

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
