//! 必要重定位完成后的真实签名决策；仅无签名字节改写且验证有效时保留原签名。
//! 原slice提取可保留架构内签名；验证失败仍走原adhoc修复，不降低发布可执行性。
use anyhow::{Context, Result};
use std::io::Write;
use std::path::{Path, PathBuf};
use std::process::Command;
use std::sync::atomic::{AtomicU64, Ordering};

/// 直接保存系统读出的完整plist，不按字段名或类型裁剪真实授权。
pub(crate) struct Entitlements {
    xml: Option<Vec<u8>>,
    signed: bool,
}
pub(crate) fn snapshot(image: &Path) -> Result<Entitlements> {
    let output = Command::new("codesign")
        .args(["-d", "--entitlements", "-", "--xml"])
        .arg(image)
        .output()
        .context("无法读取目标切片原签名授权")?;
    // unsigned/invalid沿原重签修复路径；只有实际取得的非空plist才交回系统签名器。
    let signed = output.status.success();
    Ok(Entitlements {
        xml: (signed && !output.stdout.is_empty()).then_some(output.stdout),
        signed,
    })
}

struct OwnedPlist(PathBuf);
impl OwnedPlist {
    fn create(image: &Path, bytes: &[u8]) -> Result<Self> {
        static NEXT: AtomicU64 = AtomicU64::new(0);
        let path = image.parent().context("签名映像缺少父目录")?.join(format!(
            ".ryupy-signature-{}-{}.plist",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        let mut options = std::fs::OpenOptions::new();
        options.write(true).create_new(true);
        #[cfg(unix)]
        {
            use std::os::unix::fs::OpenOptionsExt;
            options.mode(0o600);
        }
        let mut file = options
            .open(&path)
            .context("无法独占创建本轮签名授权临时文件")?;
        let owned = Self(path);
        file.write_all(bytes)?;
        Ok(owned)
    }
}
impl Drop for OwnedPlist {
    fn drop(&mut self) {
        let _ = std::fs::remove_file(&self.0);
    }
}

pub(crate) fn finish(
    image: &Path,
    eligible: bool,
    preserve_metadata: bool,
    force_library_entitlements: bool,
    original: &Entitlements,
) -> Result<&'static str> {
    if eligible {
        let verification = Command::new("codesign")
            .args(["--verify", "--strict"])
            .arg(image)
            .output()
            .context("无法执行原Mach-O签名验证")?;
        if verification.status.success() {
            return Ok("preserved_valid_original");
        }
    }
    // 已签名的thin源同样保留身份与runtime等标志，不能将语义保留绑在fat转换上。
    let preserve_metadata = preserve_metadata || original.signed;
    let mut signing = vec!["--force".as_ref(), "--sign".as_ref(), "-".as_ref()];
    if preserve_metadata {
        signing.push("--preserve-metadata=identifier,entitlements,flags,runtime".as_ref());
    }
    // macOS的preserve-metadata可静默丢库授权；显式传回修改前的完整plist。
    let entitlement_file = original
        .xml
        .as_ref()
        .map(|bytes| OwnedPlist::create(image, bytes))
        .transpose()?;
    if let Some(file) = &entitlement_file {
        signing.push("--entitlements".as_ref());
        signing.push(file.0.as_os_str());
        if force_library_entitlements {
            signing.push("--force-library-entitlements".as_ref());
        }
    }
    signing.push(image.as_os_str());
    super::run_command("codesign", &signing)?;
    Ok(if preserve_metadata {
        "adhoc_resign_preserving_entitlements"
    } else {
        "existing_relocation_adhoc_resign"
    })
}

#[cfg(all(test, target_os = "macos"))]
#[path = "cpython_macho_relocation_signature_tests.rs"]
mod tests;
