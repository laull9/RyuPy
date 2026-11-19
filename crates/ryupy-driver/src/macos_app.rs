//! macOS Finder 发布入口；整棵 NativeAOT 目录保持相对位置，不引入二次启动器或解包。
use crate::{BuildOptions, Session, manifest_files};
use anyhow::{Context, Result, ensure};
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);

/// 按标准 Contents 层级发布原生应用；已有同名应用由调用者显式保留或换名。
pub fn build_macos_app(sess: &mut Session, options: &BuildOptions, name: &str) -> Result<PathBuf> {
    ensure!(cfg!(target_os = "macos"), ".app 仅支持 macOS 本机构建");
    ensure!(options.aot, ".app 必须使用 NativeAOT");
    validate_name(name)?;
    std::fs::create_dir_all(&options.out_dir)?;
    let root = options.out_dir.canonicalize()?;
    let target = root.join(format!("{name}.app"));
    let _lock = create_lock(&root, name)?;
    manifest_files::ensure_absent(&target)?;
    let stage = Scratch::new(&root)?;
    let mut inner = options.clone();
    inner.out_dir = stage.0.join("aot");
    let product = crate::build_configured(sess, &inner, None, None)?;
    let executable = product.native_exe.context(".app 缺少原生入口")?;
    let entry = executable
        .file_name()
        .and_then(|name| name.to_str())
        .context(".app 入口名不是 UTF-8")?;
    let dist = executable.parent().context(".app 缺少原生发布目录")?;
    crate::application_layout::verify_root(dist, entry)?;
    let candidate = stage.0.join("candidate.app");
    let contents = candidate.join("Contents");
    std::fs::create_dir_all(&contents)?;
    // 保留 entry/internal 的邻接关系：Mach-O @loader_path 和发布清单均无需二次改写。
    std::fs::rename(dist, contents.join("MacOS"))?;
    std::fs::write(contents.join("Info.plist"), plist(name, entry))?;
    install(&candidate, &target)?;
    Ok(target)
}

fn install(candidate: &Path, target: &Path) -> Result<()> {
    // 独占创建包壳而不是用 rename 覆盖空目录；只有原子装入 Contents 后才是有效应用。
    std::fs::create_dir(target).with_context(|| format!("无法独占发布 {}", target.display()))?;
    if let Err(error) = std::fs::rename(candidate.join("Contents"), target.join("Contents")) {
        // 只收回本轮创建的空壳；不递归删除外部进程写入的内容。
        let _ = std::fs::remove_dir(target);
        return Err(error).context("不能安装 .app Contents");
    }
    Ok(())
}

fn validate_name(name: &str) -> Result<()> {
    ensure!(
        !name.trim().is_empty()
            && name == name.trim()
            && !name.starts_with('.')
            && name.len() <= 160
            && !name.ends_with(".app")
            && !name
                .chars()
                .any(|c| c.is_control() || matches!(c, '/' | '\\' | ':')),
        "无效的 macOS 应用名：{name:?}"
    );
    Ok(())
}

fn xml(value: &str) -> String {
    value
        .replace('&', "&amp;")
        .replace('<', "&lt;")
        .replace('>', "&gt;")
        .replace('"', "&quot;")
        .replace('\'', "&apos;")
}

fn plist(name: &str, entry: &str) -> String {
    let mut identifier = String::new();
    for value in name.chars() {
        if value.is_ascii_alphanumeric() {
            identifier.push(value.to_ascii_lowercase());
        } else {
            identifier.push_str(&format!("-{:x}-", u32::from(value)));
        }
    }
    format!(
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n\
         <!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n\
         <plist version=\"1.0\"><dict>\n\
         <key>CFBundlePackageType</key><string>APPL</string>\n\
         <key>CFBundleIdentifier</key><string>local.ryupy.{identifier}</string>\n\
         <key>CFBundleName</key><string>{name}</string>\n\
         <key>CFBundleDisplayName</key><string>{name}</string>\n\
         <key>CFBundleExecutable</key><string>{entry}</string>\n\
         <key>CFBundleVersion</key><string>1</string>\n\
         <key>NSHighResolutionCapable</key><true/>\n\
         </dict></plist>\n",
        name = xml(name),
        entry = xml(entry)
    )
}

struct Scratch(PathBuf);
impl Scratch {
    fn new(root: &Path) -> Result<Self> {
        for _ in 0..128 {
            let number = NEXT.fetch_add(1, Ordering::Relaxed);
            let path = root.join(format!(".ryupy-app-{}-{number}", std::process::id()));
            match std::fs::create_dir(&path) {
                Ok(()) => return Ok(Self(path)),
                Err(error) if error.kind() == std::io::ErrorKind::AlreadyExists => {}
                Err(error) => return Err(error.into()),
            }
        }
        anyhow::bail!("不能创建独占 .app 构建目录")
    }
}
impl Drop for Scratch {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

struct Lock(PathBuf);
fn create_lock(root: &Path, name: &str) -> Result<Lock> {
    let path = root.join(format!(".{name}.app.lock"));
    std::fs::create_dir(&path).context("同名 .app 已有构建锁，拒绝并发覆盖")?;
    Ok(Lock(path))
}
impl Drop for Lock {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir(&self.0);
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn display_name_cannot_escape_and_plist_treats_text_as_data() {
        for name in ["", " ", "../Other", ".other", "x.app", "a:b", "x\n"] {
            assert!(validate_name(name).is_err(), "{name:?}");
        }
        let name = "窗口 & 编辑";
        assert!(validate_name(name).is_ok());
        let output = plist(name, "入口 & app");
        assert!(output.contains("窗口 &amp; 编辑") && output.contains("入口 &amp; app"));
        assert!(!output.contains(name));
    }

    #[cfg(target_os = "macos")]
    #[test]
    fn publication_never_replaces_an_existing_empty_bundle() {
        let root = crate::test_temp::Root::new("app-exclusive");
        let source = root.join("candidate.app");
        let target = root.join("existing.app");
        std::fs::create_dir(&source).unwrap();
        std::fs::create_dir(&target).unwrap();
        assert!(install(&source, &target).is_err());
        assert!(source.is_dir() && target.is_dir());
    }
}
