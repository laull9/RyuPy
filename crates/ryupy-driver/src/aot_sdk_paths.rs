//! SDK路径边界：Windows仅将可无损回查的实际Disk/UNC路径交给MSBuild。
//! 内部产物、返回路径和owned清理继续使用原canonical路径；不修改进程/系统长路径策略。
use anyhow::Result;
use std::path::{Path, PathBuf};

pub(super) fn sdk_path(path: &Path) -> Result<PathBuf> {
    #[cfg(windows)]
    {
        use anyhow::{Context, ensure};
        let canonical = path.canonicalize().context("SDK路径必须已实际创建")?;
        let ordinary = ordinary_windows_path(&canonical)?;
        let roundtrip = ordinary
            .canonicalize()
            .context("普通SDK路径无法回查原文件身份")?;
        ensure!(
            roundtrip == canonical,
            "SDK路径去namespace前缀后改变实际目标，拒绝发布"
        );
        Ok(ordinary)
    }
    #[cfg(not(windows))]
    {
        Ok(path.to_owned())
    }
}

#[cfg(windows)]
fn ordinary_windows_path(path: &Path) -> Result<PathBuf> {
    use anyhow::{bail, ensure};
    use std::path::{Component, Prefix};
    let mut components = path.components();
    let Some(Component::Prefix(prefix)) = components.next() else {
        bail!("SDK路径必须为实际Windows Disk或UNC绝对路径");
    };
    let mut result = match prefix.kind() {
        Prefix::VerbatimDisk(drive) | Prefix::Disk(drive) => {
            ensure!(drive.is_ascii_alphabetic(), "SDK路径盘符无效");
            PathBuf::from(format!("{}:\\", char::from(drive)))
        }
        Prefix::VerbatimUNC(server, share) | Prefix::UNC(server, share) => {
            for part in [server, share] {
                safe_component(part)?;
            }
            let mut root = std::ffi::OsString::from("\\\\");
            root.push(server);
            root.push("\\");
            root.push(share);
            root.push("\\");
            PathBuf::from(root)
        }
        _ => bail!("SDK路径不支持device/volume/generic verbatim namespace"),
    };
    ensure!(
        matches!(components.next(), Some(Component::RootDir)),
        "SDK路径不能是相对盘符路径"
    );
    for component in components {
        let Component::Normal(name) = component else {
            bail!("SDK路径含不能无损转换的相对组件");
        };
        safe_component(name)?;
        result.push(name);
    }
    use std::os::windows::ffi::OsStrExt;
    ensure!(
        result.as_os_str().encode_wide().count() < 260,
        "SDK普通路径达到260个UTF-16单位，当前不支持无损长路径发布"
    );
    Ok(result)
}

#[cfg(windows)]
fn safe_component(component: &std::ffi::OsStr) -> Result<()> {
    use anyhow::{Context, ensure};
    let text = component.to_str().context("SDK路径组件必须为Unicode")?;
    ensure!(
        !text.is_empty() && text != "." && text != ".." && !text.ends_with(['.', ' ']),
        "SDK路径组件可能被普通Win32路径规则改变"
    );
    ensure!(
        !text
            .chars()
            .any(|c| c.is_control() || "<>:\"/\\|?*".contains(c)),
        "SDK路径组件含保留字符"
    );
    let stem = text.split('.').next().unwrap_or("").to_ascii_uppercase();
    let device = matches!(stem.as_str(), "CON" | "PRN" | "AUX" | "NUL")
        || ["COM", "LPT"].iter().any(|prefix| {
            stem.strip_prefix(prefix).is_some_and(|suffix| {
                matches!(
                    suffix,
                    "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" | "¹" | "²" | "³"
                )
            })
        });
    ensure!(!device, "SDK路径组件为保留设备名");
    Ok(())
}

#[cfg(test)]
#[path = "aot_sdk_paths_tests.rs"]
mod tests;
