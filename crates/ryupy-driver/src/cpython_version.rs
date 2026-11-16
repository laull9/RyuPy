//! CPython 版本描述符与官方稳定版范围验证：
//! 集中管理 CPython 3.9+ 版本的表示、主次版本派生、WHEEL tag、SOABI 约定及上限核验。
//! 严格拒绝未知未来盲猜版本与不支持的旧版本。

use anyhow::{Result, ensure};
use serde::{Deserialize, Serialize};

/// CPython 版本结构体，包含主版本、次版本与微版本。
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Hash, Serialize, Deserialize)]
pub(crate) struct PythonVersion {
    pub major: u32,
    pub minor: u32,
    pub micro: u32,
}

impl PythonVersion {
    /// 构造新的版本描述符。
    pub(crate) const fn new(major: u32, minor: u32, micro: u32) -> Self {
        Self {
            major,
            minor,
            micro,
        }
    }

    /// 从字符串（例如 "3.12.13"、"3.9.25"、"3.15.2"、"3.13"）严格解析版本号。
    /// 必须为纯数字分段，拒绝任何预发布版本（如 rc/alpha/beta）或带有 '+' 的非稳定后缀。
    pub(crate) fn parse(text: &str) -> Result<Self> {
        let clean = text.trim();
        let parts: Vec<&str> = clean.split('.').collect();
        ensure!(
            parts.len() >= 2 && parts.len() <= 3,
            "无效的 CPython 版本格式：{text}，期望严格纯数字 '3.x' 或 '3.x.y'"
        );
        for part in &parts {
            ensure!(
                !part.is_empty() && part.chars().all(|c| c.is_ascii_digit()),
                "CPython 版本包含非法非数字字符或预发布后缀：{text}，RyuPy 要求纯正正式发布版"
            );
        }
        let major: u32 = parts[0]
            .parse()
            .map_err(|_| anyhow::anyhow!("无效的主版本号：{}", parts[0]))?;
        let minor: u32 = parts[1]
            .parse()
            .map_err(|_| anyhow::anyhow!("无效的次版本号：{}", parts[1]))?;
        let micro: u32 = if parts.len() == 3 {
            parts[2]
                .parse()
                .map_err(|_| anyhow::anyhow!("无效的微版本号：{}", parts[2]))?
        } else {
            0
        };
        let version = Self::new(major, minor, micro);
        version.validate_supported()?;
        Ok(version)
    }

    /// 核对当前版本是否在官方当前稳定版支持范围内（3.9 至当前最新稳定分支 3.15 的常规 GIL 解释器）。
    pub(crate) fn validate_supported(&self) -> Result<()> {
        ensure!(
            self.major == 3,
            "RyuPy CPython provider 仅支持 Python 3.x，收到：{}.{}",
            self.major,
            self.minor
        );
        ensure!(
            self.minor >= 9,
            "RyuPy CPython provider 最低要求 Python 3.9，当前版本为 {}.{}.{}",
            self.major,
            self.minor,
            self.micro
        );
        // 核对官方当前发布上限：Python 3.15 已于 2026-10 正式进入稳定 bugfix 维护分支（3.15.2）。
        // 拒绝超过 3.15 的未来盲猜版本。
        ensure!(
            self.minor <= 15,
            "CPython 版本 {}.{} 超出当前官方稳定分支上限（3.9 ~ 3.15），拒绝盲猜未来版本",
            self.major,
            self.minor
        );
        Ok(())
    }

    /// 完整三段版本字符串（例如 "3.12.13"）。
    pub(crate) fn version_str(&self) -> String {
        format!("{}.{}.{}", self.major, self.minor, self.micro)
    }

    /// 两段主次版本字符串（例如 "3.12" 或 "3.9"）。
    pub(crate) fn series(&self) -> String {
        format!("{}.{}", self.major, self.minor)
    }

    /// Python WHEEL / SOABI 标签缩写（例如 "cp312" 或 "cp39"）。
    pub(crate) fn tag(&self) -> String {
        format!("cp{}{}", self.major, self.minor)
    }

    /// 十六进制版本数值（类似 PY_VERSION_HEX，例如 3.12.13 -> 0x030c0df0）。
    pub(crate) fn hex_version(&self) -> u32 {
        (self.major << 24) | (self.minor << 16) | (self.micro << 8) | 0xf0
    }
}

impl std::fmt::Display for PythonVersion {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}.{}.{}", self.major, self.minor, self.micro)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_version_parse() {
        let v312 = PythonVersion::parse("3.12.13").unwrap();
        assert_eq!(v312.major, 3);
        assert_eq!(v312.minor, 12);
        assert_eq!(v312.micro, 13);
        assert_eq!(v312.series(), "3.12");
        assert_eq!(v312.tag(), "cp312");

        let v39 = PythonVersion::parse("3.9.25").unwrap();
        assert_eq!(v39.series(), "3.9");
        assert_eq!(v39.tag(), "cp39");

        let v314 = PythonVersion::parse("3.14.0").unwrap();
        assert_eq!(v314.series(), "3.14");
        assert_eq!(v314.tag(), "cp314");
        let v315 = PythonVersion::parse("3.15.2").unwrap();
        assert_eq!(v315.series(), "3.15");
        assert_eq!(v315.tag(), "cp315");
        assert_eq!(v315.micro, 2);

        // 测试无效或超出范围版本
        assert!(PythonVersion::parse("2.7.18").is_err());
        assert!(PythonVersion::parse("3.8.10").is_err());
        assert!(
            PythonVersion::parse("3.16.0").is_err(),
            "超过官方稳定上限 3.15 应被拒绝"
        );
        assert!(PythonVersion::parse("3.99.0").is_err());

        // 预发布、rc、alpha、beta 及非法后缀必须严格拒绝
        assert!(PythonVersion::parse("3.12.0rc1").is_err());
        assert!(PythonVersion::parse("3.12.0a1").is_err());
        assert!(PythonVersion::parse("3.12.0b2").is_err());
        assert!(PythonVersion::parse("3.12.0+custom").is_err());
        assert!(PythonVersion::parse("3.12.").is_err());
        assert!(PythonVersion::parse("3.12.0.1").is_err());
        assert!(PythonVersion::parse("not-a-version").is_err());
        assert!(PythonVersion::parse("3.12.999999999999999999999").is_err());
    }
}
