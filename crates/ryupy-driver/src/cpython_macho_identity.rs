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
    pub rpaths: Vec<String>,
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
    let mut image = parse(&query("-L")?, &query("-D")?)
        .with_context(|| format!("Mach-O身份/版本损坏：{}", path.display()))?;
    // 自身不用@rpath的映像也能向依赖链提供祖先runpath；规划保留加载边时必须完整观察。
    image.rpaths = parse_rpaths(&query("-l")?)?;
    Ok(image)
}

fn parse(dependencies: &str, identity: &str) -> Result<Image> {
    let identity_names = identity
        .lines()
        .skip(1)
        .map(str::trim)
        .filter(|line| !line.is_empty())
        .collect::<Vec<_>>();
    ensure!(identity_names.len() <= 1, "Mach-O包含多个LC_ID_DYLIB");
    let identity_name = identity_names.first().copied();
    let mut image = Image {
        identity: None,
        dependencies: Vec::new(),
        rpaths: Vec::new(),
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
        // -L先列出-D确认的唯一ID；后续相同名称仍可能是真实self-load边，不能当重复ID丢弃。
        if Some(name) == identity_name && image.identity.is_none() {
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

/// 只提取LC_RPATH实际path字段并保留命令顺序；偏移/空格不能当路径内容。
fn parse_rpaths(report: &str) -> Result<Vec<String>> {
    let mut active = false;
    let mut paths = Vec::new();
    for line in report.lines().map(str::trim) {
        if line.starts_with("cmd ") {
            active = line == "cmd LC_RPATH";
        }
        if active && let Some(path) = line.strip_prefix("path ") {
            let (path, _) = path
                .rsplit_once(" (offset ")
                .context("Mach-O LC_RPATH字段损坏")?;
            paths.push(path.to_owned());
            active = false;
        }
    }
    Ok(paths)
}

/// 当前映像的loader相对runpath按dyld顺序精确命中；不汇集其它映像的runpath猜继承链。
/// @executable_path需要发布入口/host上下文，当前合同明确拒绝，不能把dotnet目录冒充应用根。
pub(crate) fn local_runpath(
    image: &Path,
    paths: &[String],
    dependency: &str,
    output: &Path,
) -> Result<Option<PathBuf>> {
    let Some(leaf) = dependency.strip_prefix("@rpath/") else {
        return Ok(None);
    };
    for rpath in paths {
        ensure!(
            !rpath.starts_with("@executable_path"),
            "Mach-O @executable_path需要明确host上下文，不能推定为产品根：{}",
            image.display()
        );
        let root = if let Some(relative) = rpath.strip_prefix("@loader_path/") {
            image.parent().context("Mach-O缺少父目录")?.join(relative)
        } else if rpath == "@loader_path" {
            image.parent().context("Mach-O缺少父目录")?.to_path_buf()
        } else if Path::new(rpath).is_absolute() {
            PathBuf::from(rpath)
        } else {
            continue;
        };
        let Ok(candidate) = root.join(leaf).canonicalize() else {
            continue;
        };
        // 第一条存在的路径决定实际加载结果；不得越过错误/外部映像去挑后面的“可用”项。
        ensure!(
            candidate.starts_with(output),
            "Mach-O runpath命中部署边界外：{}",
            candidate.display()
        );
        return Ok(Some(candidate));
    }
    Ok(None)
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
    struct Root(PathBuf);
    impl Root {
        fn new() -> Self {
            let token = std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_nanos();
            let path = std::env::temp_dir().join(format!(
                "ryupy-macho-runpath-{}-{token}",
                std::process::id()
            ));
            std::fs::create_dir(&path).unwrap();
            Self(path)
        }
    }
    impl Drop for Root {
        fn drop(&mut self) {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
    #[test]
    fn runpath_order_and_host_boundary_do_not_use_global_guesses() {
        for folder in ["copper/assets", "quartz/foreign/deep"] {
            let root = Root::new();
            let output = root.0.canonicalize().unwrap();
            let parent = output.join(folder);
            std::fs::create_dir_all(parent.join("first")).unwrap();
            std::fs::create_dir_all(parent.join("second")).unwrap();
            for place in ["first", "second"] {
                std::fs::write(parent.join(place).join("libsample.3.dylib"), "fixture").unwrap();
            }
            let image = parent.join("plugin");
            let paths = vec!["@loader_path/first".into(), "@loader_path/second".into()];
            assert_eq!(
                local_runpath(&image, &paths, "@rpath/libsample.3.dylib", &output).unwrap(),
                Some(parent.join("first/libsample.3.dylib"))
            );
            assert!(
                local_runpath(
                    &image,
                    &["@executable_path/first".into()],
                    "@rpath/libsample.3.dylib",
                    &output
                )
                .is_err()
            );
            assert!(
                local_runpath(&image, &[], "@rpath/libsample.3.dylib", &output)
                    .unwrap()
                    .is_none()
            );
        }
        assert_eq!(parse_rpaths("cmd LC_RPATH\ncmdsize 40\npath @loader_path/first path (offset 12)\ncmd LC_LOAD_DYLIB\npath ignored (offset 12)\ncmd LC_RPATH\npath @loader_path/second (offset 12)\n").unwrap(), ["@loader_path/first path", "@loader_path/second"]);
    }
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
    fn self_load_is_a_dependency_while_multiple_actual_ids_are_rejected() {
        let report = "image:\n@rpath/libself.2.dylib (compatibility version 2.0.0, current version 2.3.0)\n@rpath/libself.2.dylib (compatibility version 2.0.0, current version 2.1.0)\n";
        let image = parse(report, "image:\n@rpath/libself.2.dylib\n").unwrap();
        assert_eq!(image.identity.as_ref().unwrap().current, Version([2, 3, 0]));
        assert_eq!(image.dependencies.len(), 1);
        assert_eq!(image.dependencies[0].current, Version([2, 1, 0]));
        assert!(
            parse(
                report,
                "image:\n@rpath/libself.2.dylib\n@rpath/libanother.2.dylib\n"
            )
            .is_err()
        );
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
