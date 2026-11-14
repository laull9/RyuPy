//! CPython provider 的多平台布局、ABI 规范与系统依赖定义：
//! 集中管理 macOS arm64、Linux x64 和 Windows x64 的发行目录结构、
//! 动态库与扩展名约定、OpenSSL / 系统证书策略以及依赖闭包白名单。

#![allow(dead_code)]

use anyhow::{Result, bail};
use serde_json::json;
use std::path::PathBuf;

/// 支持的 CPython provider 目标平台。
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub(crate) enum PlatformKind {
    MacOSArm64,
    LinuxX64,
    WindowsX64,
}

impl PlatformKind {
    /// 识别当前编译/运行主机平台。
    pub(crate) fn current() -> Result<Self> {
        match (std::env::consts::OS, std::env::consts::ARCH) {
            ("macos", "aarch64") => Ok(Self::MacOSArm64),
            ("linux", "x86_64") => Ok(Self::LinuxX64),
            ("windows", "x86_64") => Ok(Self::WindowsX64),
            (os, arch) => bail!("暂不支持的 CPython 目标平台与架构：{os}-{arch}，待独立验收"),
        }
    }

    /// 从 RID 字符串解析平台枚举。
    pub(crate) fn from_rid(rid: &str) -> Result<Self> {
        match rid {
            "osx-arm64" => Ok(Self::MacOSArm64),
            "linux-x64" => Ok(Self::LinuxX64),
            "win-x64" => Ok(Self::WindowsX64),
            _ => bail!("未知的 CPython RID: {rid}"),
        }
    }

    /// .NET RID 标识符。
    pub(crate) fn rid(&self) -> &'static str {
        match self {
            Self::MacOSArm64 => "osx-arm64",
            Self::LinuxX64 => "linux-x64",
            Self::WindowsX64 => "win-x64",
        }
    }

    /// 目标 Rust / CPython Triple。
    pub(crate) fn target_triple(&self) -> &'static str {
        match self {
            Self::MacOSArm64 => "aarch64-apple-darwin",
            Self::LinuxX64 => "x86_64-unknown-linux-gnu",
            Self::WindowsX64 => "x86_64-pc-windows-msvc",
        }
    }

    /// 上游固定发行期望的 build_options 元数据（Windows MSVC 为 pgo，POSIX 为 pgo+lto）。
    pub(crate) fn build_options(&self) -> &'static str {
        match self {
            Self::MacOSArm64 | Self::LinuxX64 => "pgo+lto",
            Self::WindowsX64 => "pgo",
        }
    }

    /// CPython C shim 共享库文件名。
    pub(crate) fn shim_name(&self) -> &'static str {
        match self {
            Self::MacOSArm64 => "libryupy_cpython.dylib",
            Self::LinuxX64 => "libryupy_cpython.so",
            Self::WindowsX64 => "ryupy_cpython.dll",
        }
    }

    /// 指定版本的 CPython 核心动态链接库文件名。
    pub(crate) fn library_name_for_version(
        &self,
        version: &super::version::PythonVersion,
    ) -> String {
        match self {
            Self::MacOSArm64 => format!("libpython{}.{}.dylib", version.major, version.minor),
            Self::LinuxX64 => format!("libpython{}.{}.so.1.0", version.major, version.minor),
            Self::WindowsX64 => format!("python{}{}.dll", version.major, version.minor),
        }
    }

    /// 固定 CPython 3.12 核心动态链接库文件名（默认兼容）。
    pub(crate) fn library_name(&self) -> String {
        self.library_name_for_version(&super::version::PythonVersion::new(3, 12, 13))
    }

    /// 上游固定 python-build-standalone 20260623 归档 URL。
    pub(crate) fn archive_url(&self) -> &'static str {
        match self {
            Self::MacOSArm64 => {
                "https://github.com/astral-sh/python-build-standalone/releases/download/20260623/cpython-3.12.13%2B20260623-aarch64-apple-darwin-install_only.tar.gz"
            }
            Self::LinuxX64 => {
                "https://github.com/astral-sh/python-build-standalone/releases/download/20260623/cpython-3.12.13%2B20260623-x86_64-unknown-linux-gnu-install_only.tar.gz"
            }
            Self::WindowsX64 => {
                "https://github.com/astral-sh/python-build-standalone/releases/download/20260623/cpython-3.12.13%2B20260623-x86_64-pc-windows-msvc-install_only.tar.gz"
            }
        }
    }

    /// 分发内 libpython 相对路径。
    pub(crate) fn library_relpath_for_version(
        &self,
        version: &super::version::PythonVersion,
    ) -> PathBuf {
        match self {
            Self::MacOSArm64 | Self::LinuxX64 => {
                PathBuf::from("python/lib").join(self.library_name_for_version(version))
            }
            Self::WindowsX64 => {
                PathBuf::from("python").join(self.library_name_for_version(version))
            }
        }
    }

    pub(crate) fn library_relpath(&self) -> PathBuf {
        self.library_relpath_for_version(&super::version::PythonVersion::new(3, 12, 13))
    }

    /// 运行时 Python 模块搜索路径（相对分发根）。
    pub(crate) fn search_paths_for_version(
        &self,
        version: &super::version::PythonVersion,
    ) -> Vec<String> {
        match self {
            Self::MacOSArm64 | Self::LinuxX64 => vec![
                format!("python/lib/python{}.{}", version.major, version.minor),
                format!(
                    "python/lib/python{}.{}/lib-dynload",
                    version.major, version.minor
                ),
                format!(
                    "python/lib/python{}.{}/site-packages",
                    version.major, version.minor
                ),
            ],
            Self::WindowsX64 => vec![
                "python/Lib".into(),
                "python/DLLs".into(),
                "python/Lib/site-packages".into(),
            ],
        }
    }

    pub(crate) fn search_paths(&self) -> Vec<String> {
        self.search_paths_for_version(&super::version::PythonVersion::new(3, 12, 13))
    }

    /// site-packages 目录相对分发根的路径。
    pub(crate) fn site_packages_relpath_for_version(
        &self,
        version: &super::version::PythonVersion,
    ) -> PathBuf {
        match self {
            Self::MacOSArm64 | Self::LinuxX64 => PathBuf::from(format!(
                "python/lib/python{}.{}/site-packages",
                version.major, version.minor
            )),
            Self::WindowsX64 => PathBuf::from("python/Lib/site-packages"),
        }
    }

    pub(crate) fn site_packages_relpath(&self) -> PathBuf {
        self.site_packages_relpath_for_version(&super::version::PythonVersion::new(3, 12, 13))
    }

    /// 基础发行标准库目录列表。
    pub(crate) fn stdlib_dirs_for_version(
        &self,
        version: &super::version::PythonVersion,
    ) -> Vec<String> {
        match self {
            Self::MacOSArm64 | Self::LinuxX64 => vec![
                format!("lib/python{}.{}", version.major, version.minor),
                format!("lib/python{}.{}/lib-dynload", version.major, version.minor),
            ],
            Self::WindowsX64 => vec!["Lib".into(), "DLLs".into()],
        }
    }

    pub(crate) fn stdlib_dirs(&self) -> Vec<String> {
        self.stdlib_dirs_for_version(&super::version::PythonVersion::new(3, 12, 13))
    }

    /// 候选版本头文件相对路径（兼容 include/python3.x 与 Windows include）。
    pub(crate) fn header_relpaths_for_version(
        &self,
        version: &super::version::PythonVersion,
    ) -> Vec<String> {
        match self {
            Self::MacOSArm64 | Self::LinuxX64 => vec![
                format!(
                    "include/python{}.{}/patchlevel.h",
                    version.major, version.minor
                ),
                "include/patchlevel.h".into(),
            ],
            Self::WindowsX64 => vec![
                "include/patchlevel.h".into(),
                format!(
                    "include/python{}.{}/patchlevel.h",
                    version.major, version.minor
                ),
            ],
        }
    }

    pub(crate) fn header_relpaths(&self) -> Vec<String> {
        self.header_relpaths_for_version(&super::version::PythonVersion::new(3, 12, 13))
    }

    /// 发行必须复制的子目录名。
    pub(crate) fn copy_directories(&self) -> &'static [&'static str] {
        match self {
            Self::MacOSArm64 | Self::LinuxX64 => &["lib", "include", "share", "licenses"],
            Self::WindowsX64 => &["Lib", "DLLs", "include", "libs", "licenses", "share"],
        }
    }

    /// 平台官方扩展模块文件后缀名（有序匹配）。
    pub(crate) fn extension_suffixes_for_version(
        &self,
        version: &super::version::PythonVersion,
    ) -> Vec<String> {
        let tag = version.tag();
        let cp_tag = tag.strip_prefix("cp").unwrap_or(&tag);
        match self {
            Self::MacOSArm64 => vec![
                format!(".cpython-{}-darwin.so", cp_tag),
                ".abi3.so".into(),
                ".dylib".into(),
                ".so".into(),
            ],
            Self::LinuxX64 => vec![
                format!(".cpython-{}-x86_64-linux-gnu.so", cp_tag),
                ".abi3.so".into(),
                ".so".into(),
            ],
            Self::WindowsX64 => vec![
                format!(".{}-win_amd64.pyd", tag),
                ".pyd".into(),
                ".dll".into(),
            ],
        }
    }

    pub(crate) fn extension_suffixes(&self) -> Vec<String> {
        self.extension_suffixes_for_version(&super::version::PythonVersion::new(3, 12, 13))
    }

    /// 允许的原生系统库前缀与白名单。
    pub(crate) fn system_dependencies(&self) -> &'static [&'static str] {
        match self {
            Self::MacOSArm64 => &["/usr/lib/", "/System/Library/Frameworks/"],
            Self::LinuxX64 => &[
                "ld-linux-x86-64.so.2",
                "libc.so.6",
                "libm.so.6",
                "libpthread.so.0",
                "libdl.so.2",
                "librt.so.1",
                "libresolv.so.2",
                "libutil.so.1",
                "libgcc_s.so.1",
                "libstdc++.so.6",
            ],
            Self::WindowsX64 => &[
                "KERNEL32.dll",
                "USER32.dll",
                "WS2_32.dll",
                "ADVAPI32.dll",
                "SHELL32.dll",
                "OLEAUT32.dll",
                "ole32.dll",
                "COMDLG32.dll",
                // 固定PBS的44个原生映像实际导入的Windows操作系统组件。
                "COMCTL32.dll",
                "CRYPT32.dll",
                "Cabinet.dll",
                "GDI32.dll",
                "IMM32.dll",
                "IPHLPAPI.dll",
                "NETAPI32.dll",
                "PROPSYS.dll",
                "RPCRT4.dll",
                "USERENV.dll",
                "VERSION.dll",
                "WINMM.dll",
                // Tk9打印能力使用Windows自带的Winspool API，不收集第三方库。
                "WINSPOOL.DRV",
                // 系统自带的历史CRT与NT原生API；VC可再分发运行库仍必须随包。
                "msvcrt.dll",
                "ntdll.dll",
                "bcrypt.dll",
                "msi.dll",
                "ucrtbase.dll",
                "api-ms-win-",
            ],
        }
    }

    /// 证书策略：如实记录宿主 CA 来源与 OpenSSL 系统默认路径。
    pub(crate) fn certificate_policy(&self) -> serde_json::Value {
        match self {
            Self::MacOSArm64 => json!({
                "mode": "system_default",
                "bundled": false,
                "source": "macos_system_openssl_paths",
                "default_cafile": "/private/etc/ssl/cert.pem",
                "default_capath": "/private/etc/ssl/certs",
                "environment_overrides": ["SSL_CERT_FILE", "SSL_CERT_DIR"]
            }),
            Self::LinuxX64 => json!({
                "mode": "system_default",
                "bundled": false,
                "source": "linux_system_openssl_paths",
                "default_cafile": "/etc/ssl/certs/ca-certificates.crt",
                "default_capath": "/etc/ssl/certs",
                "environment_overrides": ["SSL_CERT_FILE", "SSL_CERT_DIR"]
            }),
            Self::WindowsX64 => json!({
                "mode": "system_default",
                "bundled": false,
                "source": "windows_system_root_store",
                "default_cafile": null,
                "default_capath": null,
                "environment_overrides": ["SSL_CERT_FILE", "SSL_CERT_DIR"]
            }),
        }
    }
}
