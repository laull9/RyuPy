//! Mach-O加载身份与版本的只读检查；不执行映像，不按第三方库名猜ABI，供组件声明和搬迁共用。
use anyhow::{Context, Result, ensure};
use serde::{Deserialize, Serialize};
use std::path::{Path, PathBuf};
use std::process::Command;

#[derive(Clone, Copy, Debug, PartialEq, Eq, PartialOrd, Ord, Serialize, Deserialize)]
pub(crate) struct Version(pub [u32; 3]);

impl Version {
    pub(crate) fn parse(text: &str) -> Result<Self> {
        let values = text
            .split('.')
            .map(str::parse::<u32>)
            .collect::<Result<Vec<_>, _>>()?;
        ensure!(
            !values.is_empty() && values.len() <= 3,
            "Mach-O版本格式错误：{text}"
        );
        let mut version = [0; 3];
        version[..values.len()].copy_from_slice(&values);
        ensure!(
            version[0] <= 65535 && version[1] <= 255 && version[2] <= 255,
            "Mach-O版本超界：{text}"
        );
        Ok(Self(version))
    }
}
impl std::fmt::Display for Version {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}.{}.{}", self.0[0], self.0[1], self.0[2])
    }
}

#[derive(Clone, Debug)]
pub(crate) struct Dylib {
    pub name: String,
    pub compatibility: Version,
    pub current: Version,
}
#[derive(Clone, Debug)]
pub(crate) struct Image {
    pub identity: Option<Dylib>,
    pub dependencies: Vec<Dylib>,
}

/// 调用者显式授权并已核验来源/ABI的旧加载名到部署目标映射。
pub(crate) struct Alias {
    pub load_name: String,
    pub target: PathBuf,
    pub install_name: String,
}

pub(crate) fn read(path: &Path) -> Result<Image> {
    let query = |flag: &str| -> Result<String> {
        let output = Command::new("otool")
            .args(["-arch", "arm64", flag])
            .arg(path)
            .output()
            .with_context(|| format!("无法读取Mach-O {flag}：{}", path.display()))?;
        ensure!(
            output.status.success(),
            "无法读取Mach-O {flag}：{}：{}",
            path.display(),
            String::from_utf8_lossy(&output.stderr)
        );
        Ok(String::from_utf8(output.stdout)?)
    };
    parse(&query("-L")?, &query("-D")?)
        .with_context(|| format!("Mach-O身份/版本损坏：{}", path.display()))
}

fn parse(dependencies: &str, identity: &str) -> Result<Image> {
    let identity_name = identity
        .lines()
        .skip(1)
        .map(str::trim)
        .find(|line| !line.is_empty());
    let mut image = Image {
        identity: None,
        dependencies: Vec::new(),
    };
    for line in dependencies.lines() {
        let Some((name, versions)) = line.trim().split_once(" (compatibility version ") else {
            continue;
        };
        let (compatibility, current) = versions
            .trim_end_matches(')')
            .split_once(", current version ")
            .context("Mach-O依赖版本条目损坏")?;
        let current = current
            .split(',')
            .next()
            .context("Mach-O缺少current版本")?
            .trim();
        let library = Dylib {
            name: name.into(),
            compatibility: Version::parse(compatibility)?,
            current: Version::parse(current)?,
        };
        if Some(name) == identity_name {
            ensure!(image.identity.is_none(), "Mach-O包含重复LC_ID_DYLIB");
            image.identity = Some(library);
        } else {
            image.dependencies.push(library);
        }
    }
    ensure!(
        identity_name.is_none() || image.identity.is_some(),
        "Mach-O LC_ID_DYLIB缺少版本"
    );
    Ok(image)
}

pub(crate) fn require_compatible(dependency: &Dylib, candidate: &Dylib) -> Result<()> {
    ensure!(
        Path::new(&dependency.name).file_name() == Path::new(&candidate.name).file_name(),
        "Mach-O LC_ID身份不匹配：{} → {}",
        dependency.name,
        candidate.name
    );
    // Apple动态加载器比较实际库current与链接时引用compat；原前缀替换仍要求同一ABI身份。
    ensure!(
        candidate.current >= dependency.compatibility,
        "Mach-O版本不兼容：{}需要{}，{}实际{}",
        dependency.name,
        dependency.compatibility,
        candidate.name,
        candidate.current
    );
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn parses_paths_with_spaces_and_only_real_load_commands() {
        let report = "app (architecture arm64):\n\t@rpath/a b/libsample.3.dylib (compatibility version 3.0.0, current version 3.7.0)\n";
        let image = parse(report, "app:\n").unwrap();
        assert_eq!(image.dependencies[0].name, "@rpath/a b/libsample.3.dylib");
        assert!(image.identity.is_none());
        let weak = parse("app:\n\t@rpath/libweak.dylib (compatibility version 1.0.0, current version 1.2.0, weak)\n", "app:\n").unwrap();
        assert_eq!(weak.dependencies[0].current, Version([1, 2, 0]));
    }
    #[test]
    fn identity_architecture_hint_cannot_replace_version_validation() {
        let dep = Dylib {
            name: "/legacy/libsample.3.dylib".into(),
            compatibility: Version([3, 0, 0]),
            current: Version([3, 0, 0]),
        };
        let mut candidate = Dylib {
            name: "/private/libsample.3.dylib".into(),
            compatibility: Version([3, 0, 0]),
            current: Version([3, 7, 0]),
        };
        require_compatible(&dep, &candidate).unwrap();
        candidate.current = Version([2, 9, 0]);
        assert!(require_compatible(&dep, &candidate).is_err());
        candidate.name = "/private/another.3.dylib".into();
        assert!(require_compatible(&dep, &candidate).is_err());
    }
}
