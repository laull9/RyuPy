//! CPython 原生分发的平台身份、ABI 验证与搬迁重定位：
//! 集中管理 macOS arm64、Linux x64 与 Windows x64 的原生依赖封闭与二进制验证，
//! 拒绝开发机绝对引用与错架构/错版本库，保证分发目录可独立运行。

#![allow(dead_code)]

use anyhow::{Result, ensure};
use std::path::{Path, PathBuf};
use std::process::Command;

#[path = "cpython_platform.rs"]
mod platform;
#[path = "cpython_platform_abi.rs"]
mod platform_abi;
#[path = "cpython_platform_relocate.rs"]
mod platform_relocate;
#[path = "cpython_version.rs"]
pub(crate) mod version;

#[allow(dead_code)]
pub(crate) use platform::PlatformKind;
pub(crate) use version::PythonVersion;

/// 平台 C shim 动态库文件名。
pub(super) fn shim_name() -> &'static str {
    PlatformKind::current()
        .map(|p| p.shim_name())
        .unwrap_or_else(|_| match std::env::consts::OS {
            "macos" => "libryupy_cpython.dylib",
            "windows" => "ryupy_cpython.dll",
            _ => "libryupy_cpython.so",
        })
}

/// 平台固定 libpython 动态链接库文件名。
pub(super) fn library_name() -> &'static str {
    PlatformKind::current()
        .map(|p| match p {
            PlatformKind::MacOSArm64 => "libpython3.12.dylib",
            PlatformKind::WindowsX64 => "python312.dll",
            PlatformKind::LinuxX64 => "libpython3.12.so.1.0",
        })
        .unwrap_or_else(|_| match std::env::consts::OS {
            "macos" => "libpython3.12.dylib",
            "windows" => "python312.dll",
            _ => "libpython3.12.so.1.0",
        })
}

pub(super) fn library_name_for_version(version: &PythonVersion) -> Result<String> {
    Ok(PlatformKind::current()?.library_name_for_version(version))
}

/// 当前主机目标 RID。
pub(super) fn rid() -> Result<&'static str> {
    Ok(PlatformKind::current()?.rid())
}

/// 目标 Triple。
pub(super) fn target_triple() -> Result<&'static str> {
    Ok(PlatformKind::current()?.target_triple())
}

/// 固定上游构建 options（POSIX 为 pgo+lto，Windows 为 pgo）。
pub(crate) fn build_options() -> Result<&'static str> {
    Ok(PlatformKind::current()?.build_options())
}

/// 固定上游构建归档 URL。
pub(super) fn archive_url() -> Result<&'static str> {
    Ok(PlatformKind::current()?.archive_url())
}

/// 分发内核心库相对路径。
pub(super) fn library_relpath() -> Result<PathBuf> {
    Ok(PlatformKind::current()?.library_relpath())
}

pub(super) fn library_relpath_for_version(version: &PythonVersion) -> Result<PathBuf> {
    Ok(PlatformKind::current()?.library_relpath_for_version(version))
}

/// Python 运行时搜索路径。
pub(super) fn search_paths() -> Result<Vec<String>> {
    Ok(PlatformKind::current()?.search_paths())
}

pub(super) fn search_paths_for_version(version: &PythonVersion) -> Result<Vec<String>> {
    Ok(PlatformKind::current()?.search_paths_for_version(version))
}

/// site-packages 相对路径。
pub(crate) fn site_packages_relpath() -> Result<PathBuf> {
    Ok(PlatformKind::current()?.site_packages_relpath())
}

pub(crate) fn site_packages_relpath_for_version(version: &PythonVersion) -> Result<PathBuf> {
    Ok(PlatformKind::current()?.site_packages_relpath_for_version(version))
}

/// 标准库目录列表。
pub(crate) fn stdlib_dirs() -> Result<Vec<String>> {
    Ok(PlatformKind::current()?.stdlib_dirs())
}

pub(crate) fn stdlib_dirs_for_version(version: &PythonVersion) -> Result<Vec<String>> {
    Ok(PlatformKind::current()?.stdlib_dirs_for_version(version))
}

/// 候选版本头文件相对路径。
pub(super) fn header_relpaths() -> Result<Vec<String>> {
    Ok(PlatformKind::current()?.header_relpaths())
}

pub(super) fn header_relpaths_for_version(version: &PythonVersion) -> Result<Vec<String>> {
    Ok(PlatformKind::current()?.header_relpaths_for_version(version))
}

/// 必须复制的子目录名。
pub(super) fn copy_directories() -> Result<&'static [&'static str]> {
    Ok(PlatformKind::current()?.copy_directories())
}

/// 平台官方扩展模块文件后缀名。
pub(crate) fn extension_suffixes() -> Result<Vec<String>> {
    Ok(PlatformKind::current()?.extension_suffixes())
}

pub(crate) fn extension_suffixes_for_version(version: &PythonVersion) -> Result<Vec<String>> {
    Ok(PlatformKind::current()?.extension_suffixes_for_version(version))
}

/// 系统证书策略描述。
pub(super) fn certificate_policy() -> Result<serde_json::Value> {
    Ok(PlatformKind::current()?.certificate_policy())
}

/// 平台原生依赖白名单。
pub(super) fn system_dependencies() -> &'static [&'static str] {
    PlatformKind::current()
        .map(|p| p.system_dependencies())
        .unwrap_or_else(|_| &["/usr/lib/", "/System/Library/Frameworks/"])
}

/// 验证给定的目标 Triple 是否合法且与当前平台匹配。
pub(super) fn validate_target_triple(triple: &str) -> Result<()> {
    let current_triple = target_triple()?;
    ensure!(
        triple == current_triple,
        "CPython 上游构建元数据 target_triple 错配：元数据为 {triple}，当前平台预期为 {current_triple}"
    );
    Ok(())
}

/// 运行期预检：核查基础发行目录中的 libpython 文件完整性与机器架构（Mach-O arm64 / ELF x86_64 / PE x64）。
pub(super) fn preflight_version(root: &Path, version: &PythonVersion) -> Result<()> {
    let platform = PlatformKind::current()?;
    let lib_name = platform.library_name_for_version(version);
    let lib_path = match platform {
        PlatformKind::MacOSArm64 | PlatformKind::LinuxX64 => root.join("lib").join(&lib_name),
        PlatformKind::WindowsX64 => root.join(&lib_name),
    };
    ensure!(
        lib_path.is_file(),
        "CPython 发行缺少 libpython：{}",
        lib_path.display()
    );

    // 严密核验机器架构（64 位与特定指令集），绝不通过跳过检查掩盖错配
    platform_abi::check_architecture(&lib_path, platform.rid())?;

    // macOS 下若 lipo 可用，额外进行二次双重确认
    if platform == PlatformKind::MacOSArm64 {
        if let Ok(output) = Command::new("lipo").arg("-archs").arg(&lib_path).output() {
            ensure!(
                output.status.success()
                    && String::from_utf8_lossy(&output.stdout)
                        .split_whitespace()
                        .any(|arch| arch == "arm64"),
                "CPython libpython 架构不是 arm64"
            );
        }
    }
    Ok(())
}

pub(super) fn preflight(root: &Path) -> Result<()> {
    preflight_version(root, &PythonVersion::new(3, 12, 13))
}

/// 原生库重定位与依赖封闭性检查：
/// 在 macOS 上逐映像修正 install name 并重新代码签名；
/// 在 Linux 上校验 ELF DT_NEEDED 闭包并配置 rpath；
/// 在 Windows 上校验 PE 导入表，拒绝任何未经授权的外部依赖。
pub(super) fn relocate(output: &Path) -> Result<()> {
    let platform = PlatformKind::current()?;
    let shim = platform.shim_name();
    let sys_deps = platform.system_dependencies();

    match platform {
        PlatformKind::MacOSArm64 => {
            platform_relocate::relocate_macos(output, shim, sys_deps)?;
        }
        PlatformKind::LinuxX64 => {
            platform_relocate::relocate_linux(output, shim, sys_deps)?;
        }
        PlatformKind::WindowsX64 => {
            platform_relocate::relocate_windows(output, shim, sys_deps)?;
        }
    }
    Ok(())
}

/// patchlevel.h 的 PY_VERSION 必须逐段完全匹配选中解释器。
pub(super) fn header_matches(header: &str, version: &PythonVersion) -> bool {
    let expected = format!("\"{}\"", version.version_str());
    header.lines().any(|line| {
        line.split_whitespace().collect::<Vec<_>>() == ["#define", "PY_VERSION", expected.as_str()]
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn header_requires_exact_micro_and_directive() {
        let version = PythonVersion::new(3, 12, 13);
        assert!(header_matches("#define PY_VERSION \"3.12.13\"", &version));
        assert!(!header_matches("#define PY_VERSION \"3.12.14\"", &version));
        assert!(!header_matches(
            "/* 3.12.13 */\n#define PY_VERSION \"3.12.13rc1\"",
            &version
        ));
    }

    #[test]
    fn test_platform_consistency() {
        let current = PlatformKind::current().unwrap();
        assert_eq!(current.rid(), rid().unwrap());
        assert_eq!(current.shim_name(), shim_name());
        assert_eq!(current.library_name(), library_name());
    }

    #[test]
    fn test_all_platforms_metadata() {
        for rid_str in ["osx-arm64", "linux-x64", "win-x64"] {
            let p = PlatformKind::from_rid(rid_str).unwrap();
            assert_eq!(p.rid(), rid_str);
            assert!(!p.target_triple().is_empty());
            assert!(!p.archive_url().is_empty());
            assert!(!p.search_paths().is_empty());
            assert!(!p.extension_suffixes().is_empty());
            assert!(!p.system_dependencies().is_empty());
            let policy = p.certificate_policy();
            assert_eq!(policy["mode"], "system_default");
        }
    }
}
