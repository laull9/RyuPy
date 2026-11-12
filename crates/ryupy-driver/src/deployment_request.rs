//! 单一部署请求的严格配置入口；只翻译为既有构建参数，不另建模块图或发布管线。
use crate::{Artifacts, BuildOptions, Session, manifest_files};
use anyhow::{Context, Result, ensure};
use serde::{
    Deserialize, Deserializer,
    de::{MapAccess, Visitor},
};
use std::collections::{BTreeMap, BTreeSet};
use std::fmt;
use std::io::Read;
use std::path::{Component, Path, PathBuf};

const CONFIG_LIMIT: u64 = 1024 * 1024;
const ENTRY_LIMIT: u64 = 16 * 1024 * 1024;

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct Config {
    schema_version: u32,
    mode: String,
    #[serde(default)]
    search_paths: Vec<String>,
    #[serde(default)]
    include_modules: Vec<String>,
    resources: Option<String>,
    distribution_metadata: Option<String>,
    #[serde(deserialize_with = "unique_targets")]
    targets: BTreeMap<String, Target>,
}
#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct Target {
    entry: String,
}

fn unique_targets<'de, D: Deserializer<'de>>(
    deserializer: D,
) -> std::result::Result<BTreeMap<String, Target>, D::Error> {
    struct Targets;
    impl<'de> Visitor<'de> for Targets {
        type Value = BTreeMap<String, Target>;
        fn expecting(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
            formatter.write_str("非重复目标对象")
        }
        fn visit_map<A: MapAccess<'de>>(
            self,
            mut map: A,
        ) -> std::result::Result<Self::Value, A::Error> {
            let mut targets = BTreeMap::new();
            while let Some((name, target)) = map.next_entry::<String, Target>()? {
                if targets.insert(name, target).is_some() {
                    return Err(serde::de::Error::custom("部署目标重复"));
                }
                if targets.len() > 64 {
                    return Err(serde::de::Error::custom("部署目标超过64项"));
                }
            }
            Ok(targets)
        }
    }
    deserializer.deserialize_map(Targets)
}

#[derive(Clone)]
struct Input {
    path: PathBuf,
    limit: u64,
    hash: String,
}

/// 会话兼容字段的不可构造输入守卫；仅项目读取器可生成，默认构建没有此状态。
#[doc(hidden)]
#[derive(Clone)]
pub struct InputGuard {
    root: PathBuf,
    directories: Vec<PathBuf>,
    inputs: Vec<Input>,
}
impl InputGuard {
    pub(crate) fn verify(&self) -> Result<()> {
        for directory in &self.directories {
            plain_chain(&self.root, directory)?;
            ensure!(
                manifest_files::plain_metadata(directory)?.is_dir(),
                "部署搜索根已改变类型"
            );
        }
        for input in &self.inputs {
            let bytes = read_plain(&self.root, &input.path, input.limit)?;
            ensure!(
                manifest_files::hash_bytes(&bytes) == input.hash,
                "部署输入在构建期间改变：{}",
                input.path.display()
            );
        }
        Ok(())
    }
    fn add(&mut self, path: &Path, limit: u64) -> Result<Vec<u8>> {
        let bytes = read_plain(&self.root, path, limit)?;
        self.inputs.push(Input {
            path: path.to_owned(),
            limit,
            hash: manifest_files::hash_bytes(&bytes),
        });
        Ok(bytes)
    }
}

/// 已解析单目标部署请求；原JSON与声明文件在发布提交前仍须保持原字节。
pub struct DeploymentRequest {
    options: BuildOptions,
    includes: Vec<String>,
    resources: Option<PathBuf>,
    metadata: Option<PathBuf>,
    guard: InputGuard,
}
impl DeploymentRequest {
    /// 路径均以配置父目录为根；显式输出目录保留旧CLI的调用目录语义。
    pub fn load(project: &Path, target: &str, out_dir: Option<&Path>) -> Result<Self> {
        ensure!(
            valid_target(target),
            "部署目标名只支持1至64个ASCII字母/数字/下划线/连字符"
        );
        let project = if project.is_absolute() {
            project.to_owned()
        } else {
            std::env::current_dir()?.join(project)
        };
        // 用户显式选择的父目录建立可信根，允许定位参数中的..及宿主祖先别名。
        // 配置文件本身和此根内的路径仍不跟随链接。
        let filename = project.file_name().context("部署配置缺少文件名")?;
        let root = project
            .parent()
            .context("部署配置没有父目录")?
            .canonicalize()?;
        ensure!(
            manifest_files::plain_metadata(&root)?.is_dir(),
            "部署根不是普通目录"
        );
        let project = root.join(filename);
        let root = root.as_path();
        let mut guard = InputGuard {
            root: root.to_owned(),
            directories: Vec::new(),
            inputs: Vec::new(),
        };
        let bytes = guard.add(&project, CONFIG_LIMIT)?;
        let config: Config =
            serde_json::from_slice(&bytes).context("部署配置不是有效schema1 JSON")?;
        ensure!(
            config.schema_version == 1 && config.mode == "aot",
            "部署配置只支持schema1 mode=aot"
        );
        ensure!(
            !config.targets.is_empty() && config.targets.keys().all(|name| valid_target(name)),
            "部署targets为空或目标名非法"
        );
        ensure!(
            config.search_paths.len() <= 64 && config.include_modules.len() <= 256,
            "部署搜索根或模块数超限"
        );
        let selected = config.targets.get(target).context("部署目标不存在")?;
        // 未选目标同样验证语法/来源边界，避免配置随target选择改变安全含义。
        for entry in config.targets.values() {
            let _ = within(root, &entry.entry, false)?;
        }
        let input = within(root, &selected.entry, false)?;
        ensure!(
            input.extension().is_some_and(|v| v == "py"),
            "部署入口须为.py源码"
        );
        guard.add(&input, ENTRY_LIMIT)?;
        let mut seen = BTreeSet::new();
        let search_paths = config
            .search_paths
            .iter()
            .map(|path| {
                let resolved = within(root, path, true)?;
                ensure!(seen.insert(resolved.clone()), "部署搜索根重复");
                Ok(resolved)
            })
            .collect::<Result<Vec<_>>>()?;
        let mut seen = BTreeSet::new();
        for module in &config.include_modules {
            ensure!(
                module.len() <= 1024 && module.split('.').all(identifier),
                "部署include_modules不是ASCII点分模块名"
            );
            ensure!(seen.insert(module), "部署include_modules重复");
        }
        guard.directories = search_paths.clone();
        let resources = declaration(root, config.resources.as_deref(), &mut guard)?;
        let metadata = declaration(root, config.distribution_metadata.as_deref(), &mut guard)?;
        guard.verify()?;
        Ok(Self {
            options: BuildOptions {
                input,
                out_dir: out_dir
                    .map(Path::to_owned)
                    .unwrap_or_else(|| root.join("build").join(target)),
                aot: true,
                search_paths,
            },
            includes: config.include_modules,
            resources,
            metadata,
            guard,
        })
    }
    /// 与普通build相同的输入与输出配置，供CLI沿用原产物报告。
    pub fn options(&self) -> &BuildOptions {
        &self.options
    }
    /// 保留调用方默认模块策略；会话补充字段成功/失败后都恢复，不污染后续构建。
    pub fn build(&self, session: &mut Session) -> Result<Artifacts> {
        ensure!(
            session.include_modules.is_empty()
                && session.distribution_metadata.is_none()
                && session.instance_modules.is_empty()
                && !session.instance_direct_globals
                && !session.fixed_binding_build
                && session.deployment_inputs.is_none(),
            "项目部署不能混用外部声明或实例/绑定配置"
        );
        self.guard.verify()?;
        let includes = std::mem::replace(&mut session.include_modules, self.includes.clone());
        let metadata = std::mem::replace(&mut session.distribution_metadata, self.metadata.clone());
        session.deployment_inputs = Some(self.guard.clone());
        let result =
            crate::build_configured(session, &self.options, self.resources.as_deref(), None);
        session.deployment_inputs = None;
        session.include_modules = includes;
        session.distribution_metadata = metadata;
        result
    }
}
fn identifier(name: &str) -> bool {
    let mut chars = name.bytes();
    chars
        .next()
        .is_some_and(|c| c.is_ascii_alphabetic() || c == b'_')
        && chars.all(|c| c.is_ascii_alphanumeric() || c == b'_')
}
fn valid_target(name: &str) -> bool {
    !name.is_empty()
        && name.len() <= 64
        && name
            .bytes()
            .all(|c| c.is_ascii_alphanumeric() || c == b'_' || c == b'-')
}
fn declaration(root: &Path, name: Option<&str>, guard: &mut InputGuard) -> Result<Option<PathBuf>> {
    name.map(|name| {
        let path = within(root, name, false)?;
        guard.add(&path, CONFIG_LIMIT)?;
        Ok(path)
    })
    .transpose()
}
fn within(root: &Path, relative: &str, directory: bool) -> Result<PathBuf> {
    ensure!(relative.len() <= 4096, "部署相对路径过长");
    if !(directory && relative == ".") {
        manifest_files::safe_relative(relative)?;
    }
    let path = if directory && relative == "." {
        root.to_owned()
    } else {
        root.join(relative)
    };
    plain_chain(root, &path)?;
    let info = manifest_files::plain_metadata(&path)?;
    ensure!(
        if directory {
            info.is_dir()
        } else {
            info.is_file()
        },
        "部署路径类型不符：{relative}"
    );
    let canonical = path.canonicalize()?;
    ensure!(canonical.starts_with(root), "部署路径越出配置根");
    Ok(canonical)
}
fn plain_chain(root: &Path, path: &Path) -> Result<()> {
    ensure!(
        manifest_files::plain_metadata(root)?.is_dir(),
        "部署根已不再是普通目录"
    );
    let relative = path.strip_prefix(root).context("部署输入越出配置根")?;
    let mut checked = root.to_owned();
    for component in relative.components() {
        ensure!(
            matches!(component, Component::Normal(_)),
            "部署根内路径含相对跳转"
        );
        checked.push(component.as_os_str());
        manifest_files::plain_metadata(&checked)?;
    }
    Ok(())
}
fn read_plain(root: &Path, path: &Path, limit: u64) -> Result<Vec<u8>> {
    plain_chain(root, path)?;
    let info = manifest_files::plain_metadata(path)?;
    ensure!(
        info.is_file() && info.len() <= limit,
        "部署输入须为有界普通文件"
    );
    let mut bytes = Vec::new();
    std::fs::File::open(path)?
        .take(limit + 1)
        .read_to_end(&mut bytes)?;
    ensure!(
        bytes.len() as u64 <= limit && bytes.len() as u64 == info.len(),
        "部署输入读取时改变或超限"
    );
    Ok(bytes)
}

#[cfg(test)]
#[path = "deployment_request_tests.rs"]
mod tests;
