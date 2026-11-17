//! 必要重定位完成后的真实签名决策；仅无签名字节改写且验证有效时保留原签名。
//! 原slice提取可保留架构内签名；验证失败仍走原adhoc修复，不降低发布可执行性。
use anyhow::{Context, Result};
use std::path::Path;
use std::process::Command;

pub(super) fn finish(
    image: &Path,
    eligible: bool,
    converted: bool,
    force_library_entitlements: bool,
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
    let mut signing = vec!["--force".as_ref(), "--sign".as_ref(), "-".as_ref()];
    if converted {
        signing.push("--preserve-metadata=identifier,entitlements,flags,runtime".as_ref());
        if force_library_entitlements {
            signing.push("--force-library-entitlements".as_ref());
        }
    }
    signing.push(image.as_os_str());
    super::run_command("codesign", &signing)?;
    Ok(if converted {
        "adhoc_resign_preserving_entitlements"
    } else {
        "existing_relocation_adhoc_resign"
    })
}
