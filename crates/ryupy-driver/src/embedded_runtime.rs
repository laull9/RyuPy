//! 将当前编译器内嵌的完整C#闭包原子准备到用户私有缓存；无源码仓库或旁置runtime依赖。
use anyhow::{Context, Result, ensure};
use std::path::{Path, PathBuf};
use std::sync::OnceLock;

include!(concat!(env!("OUT_DIR"), "/runtime_payload.rs"));
static PREPARED: OnceLock<PathBuf> = OnceLock::new();

fn ready(root: &Path) -> bool {
    crate::manifest_files::plain_metadata(root).is_ok_and(|metadata| metadata.is_dir())
        && std::fs::read_to_string(root.join("ready")).is_ok_and(|text| text == BUNDLE_ID)
        && FILES.iter().all(|(name, bytes)| {
            crate::manifest_files::plain_metadata(&root.join(name))
                .is_ok_and(|metadata| metadata.is_file() && metadata.len() == bytes.len() as u64)
        })
}

fn cache() -> Result<PathBuf> {
    let base = std::env::var_os(if cfg!(windows) {
        "LOCALAPPDATA"
    } else {
        "XDG_CACHE_HOME"
    })
    .map(PathBuf::from)
    .or_else(|| {
        std::env::var_os(if cfg!(windows) { "USERPROFILE" } else { "HOME" })
            .map(|home| PathBuf::from(home).join(".cache"))
    })
    .context("无法定位用户缓存目录，请配置XDG_CACHE_HOME或LOCALAPPDATA")?;
    let root = base.join("ryupy/runtime");
    #[cfg(unix)]
    let mut builder = std::fs::DirBuilder::new();
    #[cfg(not(unix))]
    let mut builder = std::fs::DirBuilder::new();
    builder.recursive(true);
    #[cfg(unix)]
    {
        use std::os::unix::fs::DirBuilderExt;
        builder.mode(0o700);
    }
    builder.create(&root)?;
    Ok(root)
}

pub(crate) fn prepare() -> Result<PathBuf> {
    if let Some(root) = PREPARED.get().filter(|root| ready(root)) {
        return Ok(root.clone());
    }
    let cache = cache()?;
    let root = cache.join(BUNDLE_ID);
    let lock = std::fs::OpenOptions::new()
        .create(true)
        .truncate(false)
        .read(true)
        .write(true)
        .open(cache.join(format!("{BUNDLE_ID}.lock")))?;
    fs2::FileExt::lock_exclusive(&lock).context("无法锁定当前编译器的运行时缓存")?;
    if !ready(&root) {
        let timestamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)?
            .as_nanos();
        let candidate = cache.join(format!("candidate-{}-{timestamp}", std::process::id()));
        std::fs::create_dir(&candidate)?;
        let result = (|| -> Result<()> {
            for (name, bytes) in FILES {
                let path = candidate.join(name);
                std::fs::create_dir_all(path.parent().context("内嵌文件没有父目录")?)?;
                std::fs::write(path, bytes)?;
            }
            std::fs::write(candidate.join("ready"), BUNDLE_ID)?;
            ensure!(ready(&candidate), "内嵌运行时尚未齐备");
            if root.exists() {
                std::fs::rename(
                    &root,
                    cache.join(format!("incomplete-{BUNDLE_ID}-{timestamp}")),
                )?;
            }
            std::fs::rename(&candidate, &root)?;
            Ok(())
        })();
        if result.is_err() {
            let _ = std::fs::remove_dir_all(&candidate);
        }
        result?;
    }
    let _ = PREPARED.set(root.clone());
    Ok(root)
}

pub(crate) fn licenses() -> &'static str {
    let bytes = FILES
        .iter()
        .find(|(name, _)| *name == crate::runtime_assets::LICENSE)
        .unwrap()
        .1;
    std::str::from_utf8(bytes).expect("内嵌许可证必须为UTF-8")
}
