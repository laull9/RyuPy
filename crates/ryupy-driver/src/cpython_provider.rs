//! 显式 CPython provider 的构建期计划：固定发行来源与执行域，应用入口始终进入现有编译链。
use crate::Session;
use anyhow::{Context, Result, ensure};
use serde::{Deserialize, Serialize};
use std::path::{Component, Path, PathBuf};

#[path = "cpython_native.rs"]
pub(crate) mod native;
pub(crate) use native::version;
#[path = "cpython_dependencies.rs"]
mod dependencies;
#[path = "cpython_deployment.rs"]
mod deployment;
#[path = "cpython_discovery.rs"]
pub(crate) mod discovery;
#[path = "cpython_shim.rs"]
mod shim;
pub(crate) use deployment::{extend_project, publish};
const VERSION: &str = "3.12.13";
pub(crate) const CONFIG: &str = "ryupy-cpython.json";
pub(crate) const REPORT: &str = "cpython-provider.json";

/// Session 的不透明计划槽位；调用方只声明配置路径，driver 独立冻结实际计划。
#[derive(Default)]
pub struct CPythonProviderState {
    pub(crate) plan: Option<Plan>,
}
#[derive(Clone, Deserialize, Serialize)]
#[serde(deny_unknown_fields)]
pub(crate) struct Source {
    project: String,
    build_id: String,
    source_url: String,
    licenses: Vec<String>,
}
#[derive(Clone, Deserialize)]
#[serde(deny_unknown_fields)]
struct Declaration {
    schema: u32,
    dependencies: Option<dependencies::Declaration>,
    python_version: String,
    distribution_root: PathBuf,
    source: Source,
    #[serde(default)]
    modules: Vec<String>,
    #[serde(default = "stdlib_provider")]
    stdlib_provider: String,
    #[serde(default)]
    container_transfer: ContainerTransfer,
    #[serde(default)]
    payloads: Vec<Payload>,
    /// 只覆盖桥构建位置；不允许运行时查找开发环境。
    bridge_directory: Option<PathBuf>,
    shim: Option<PathBuf>,
}
#[derive(Clone, Deserialize, Serialize)]
#[serde(deny_unknown_fields)]
pub(crate) struct Payload {
    pub source: PathBuf,
    pub destination: String,
}
/// 精确内建布局必须显式选择同步物化；默认通用代理保持对象身份与可观察别名。
#[derive(Clone, Copy, Default, Deserialize, Serialize)]
#[serde(rename_all = "snake_case")]
pub(crate) enum ContainerTransfer {
    #[default]
    Proxy,
    Synchronized,
}
#[derive(Clone)]
pub(crate) struct Plan {
    pub version: version::PythonVersion,
    pub root: PathBuf,
    pub source: Source,
    pub modules: Vec<String>,
    pub stdlib: std::collections::BTreeSet<String>,
    pub declared: std::collections::BTreeSet<String>,
    pub packages: std::collections::BTreeSet<String>,
    pub container_transfer: ContainerTransfer,
    pub payloads: Vec<Payload>,
    pub bridge: PathBuf,
    pub shim: PathBuf,
    pub _shim_build: Option<std::sync::Arc<shim::BuildRoot>>,
    pub dependency_report: serde_json::Value,
}

pub(crate) fn prepare(sess: &mut Session, entry: &Path) -> Result<()> {
    sess.cpython_provider_state.plan = None;
    let Some(path) = &sess.cpython_provider else {
        return Ok(());
    };
    if path == Path::new("auto") {
        return prepare_auto(sess, entry);
    }
    ensure!(
        sess.distribution_metadata.is_none(),
        "CPython provider 的包数据须通过 payloads 声明"
    );
    let path = path.canonicalize().context("CPython provider 声明不存在")?;
    let base = path.parent().context("CPython provider 声明没有父目录")?;
    let declaration: Declaration =
        serde_json::from_slice(&std::fs::read(&path)?).context("CPython provider 声明格式错误")?;
    ensure!(
        declaration.schema == 1,
        "CPython provider 声明 schema 必须为 1"
    );
    let version = version::PythonVersion::parse(&declaration.python_version)?;
    ensure!(
        declaration.python_version == VERSION,
        "显式 schema 1 固定为 CPython {VERSION}；自动适配请使用 --cpython-provider auto"
    );
    let source = declaration.source.clone();
    ensure!(
        source.project == "python-build-standalone"
            && source.build_id == "20260623"
            && source.source_url
                == "https://github.com/astral-sh/python-build-standalone/releases/tag/20260623",
        "CPython provider 仅接受已冻结的 python-build-standalone 20260623 来源"
    );
    let root = absolute(base, &declaration.distribution_root)?.canonicalize()?;
    ensure!(
        !root.join("pyvenv.cfg").exists(),
        "CPython 基础发行不能使用 venv"
    );
    let build =
        std::fs::read_to_string(root.join("BUILD")).context("CPython 发行缺少 BUILD 来源标识")?;
    ensure!(
        build.trim() == source.build_id,
        "CPython 发行 BUILD 与声明来源不符"
    );
    let header_path = native::header_relpaths_for_version(&version)?
        .iter()
        .map(|path| root.join(path))
        .find(|path| path.is_file())
        .context("CPython 发行缺少固定版本头文件")?;
    let header = std::fs::read_to_string(header_path).context("CPython 发行缺少固定版本头文件")?;
    ensure!(
        header
            .lines()
            .any(|line| line.split_whitespace().collect::<Vec<_>>()
                == ["#define", "PY_VERSION", "\"3.12.13\""]),
        "CPython 头文件版本不是 {VERSION}"
    );
    ensure!(
        !source.licenses.is_empty(),
        "CPython 发行必须声明随包许可证"
    );
    for license in &source.licenses {
        relative(license)?;
        ensure!(
            root.join(license).is_file(),
            "CPython 许可证缺失：{license}"
        );
    }
    let metadata: serde_json::Value =
        serde_json::from_slice(&std::fs::read(root.join("PYTHON.json")).context(
            "CPython 基础发行缺少上游完整归档 PYTHON.json 与组件许可证，请显式准备同一发行元数据",
        )?)?;
    ensure!(
        metadata["version"] == "8"
            && metadata["python_version"] == VERSION
            && metadata["target_triple"] == native::target_triple()?
            && metadata["build_options"] == native::build_options()?
            && metadata["libpython_link_mode"] == "shared",
        "CPython 上游构建元数据与固定发行不符"
    );
    ensure!(
        root.join("licenses").is_dir()
            && source
                .licenses
                .iter()
                .any(|path| path.starts_with("licenses/")),
        "CPython 基础发行缺少组件许可证声明"
    );
    for entry in std::fs::read_dir(root.join("licenses"))? {
        let entry = entry?;
        if entry.file_type()?.is_file() {
            let logical = format!("licenses/{}", entry.file_name().to_string_lossy());
            ensure!(
                source.licenses.contains(&logical),
                "CPython 组件许可证未进入来源声明：{logical}"
            );
        }
    }
    finish_prepare(sess, entry, base, declaration, root, metadata, version)
}

pub(crate) fn prepare_auto(sess: &mut Session, entry: &Path) -> Result<()> {
    ensure!(
        sess.distribution_metadata.is_none(),
        "CPython provider 自动模式的包数据须自动探测或通过闭包处理"
    );
    let discovered = discovery::discover(entry)?;
    let root = discovered.distribution_root.canonicalize()?;
    let version = discovered.version;
    native::preflight_version(&root, &version)?;
    let header_path = native::header_relpaths_for_version(&version)?
        .iter()
        .map(|path| root.join(path))
        .find(|path| path.is_file())
        .with_context(|| format!("CPython 发行缺少 {} 版本头文件", version.series()))?;
    let header = std::fs::read_to_string(header_path).context("无法读取 CPython 发行头文件")?;
    ensure!(
        native::header_matches(&header, &version),
        "CPython 头文件版本与探测版本不符"
    );
    let metadata: serde_json::Value =
        serde_json::from_slice(&std::fs::read(root.join("PYTHON.json"))?)?;
    let default_bridge = crate::runtime::cpython_bridge_dir()?;
    let bridge = default_bridge;
    ensure!(
        bridge.join("RyuPy.CPython.dll").is_file(),
        "缺少 RyuPy.CPython.dll，请先构建随包宿主：{}",
        bridge.display()
    );
    let (shim, shim_build) = shim::build(&root, &version)?;
    let stdlib = crate::cpython_inventory::stdlib_for_version(&root, &metadata, &version)?;
    let mut modules = Vec::new();
    let mut payloads = Vec::new();
    let closure = dependencies::collect_auto(
        &discovered.prefix,
        &root,
        entry,
        &modules,
        &stdlib,
        &version,
    )?;
    let declared = modules.iter().cloned().collect();
    payloads.extend(closure.payloads);
    let mut packages = if closure.report.is_null() {
        crate::cpython_inventory::packages_for_version(&root, &payloads, &version)?
    } else {
        crate::cpython_inventory::payload_packages_for_version(&payloads, &version)?
    };
    packages.extend(closure.modules);
    modules.extend(stdlib.iter().cloned());
    modules.extend(packages.iter().cloned());
    modules.sort();
    modules.dedup();
    for included in &sess.include_modules {
        ensure!(
            !modules.iter().any(|r| domain(r, included)),
            "模块同时声明为RyuPy源码与CPython完整依赖族：{included}"
        );
    }
    let source = Source {
        project: discovered.source_project,
        build_id: discovered.source_build_id,
        source_url: discovered.source_url,
        licenses: discovered.licenses,
    };
    sess.cpython_provider_state.plan = Some(Plan {
        version,
        root,
        source,
        modules,
        stdlib,
        declared,
        packages,
        container_transfer: ContainerTransfer::Synchronized,
        payloads,
        bridge,
        shim,
        _shim_build: Some(shim_build),
        dependency_report: closure.report,
    });
    Ok(())
}

fn finish_prepare(
    sess: &mut Session,
    entry: &Path,
    base: &Path,
    declaration: Declaration,
    root: PathBuf,
    metadata: serde_json::Value,
    version: version::PythonVersion,
) -> Result<()> {
    ensure!(
        declaration.stdlib_provider == "cpython",
        "CPython provider 必须统一使用 stdlib_provider=cpython，不能与原生标准库混配"
    );
    let mut modules = declaration.modules;
    modules.sort();
    modules.dedup();
    for name in &modules {
        ensure!(
            crate::module_hints::valid_name(name) && name != "__main__",
            "CPython provider 模块名非法：{name}"
        );
        ensure!(
            !name.contains('.'),
            "CPython provider 必须整体声明顶层依赖族，避免共享状态拆分：{name}"
        );
        ensure!(
            !sess
                .include_modules
                .iter()
                .any(|included| domain(name, included)),
            "模块同时被声明为 RyuPy 编译源码与 CPython：{name}"
        );
    }
    let mut payloads = declaration.payloads;
    for payload in &mut payloads {
        relative(&payload.destination)?;
        payload.source = absolute(base, &payload.source)?
            .canonicalize()
            .with_context(|| format!("CPython 包载荷不存在：{}", payload.source.display()))?;
    }
    let stdlib = crate::cpython_inventory::stdlib_for_version(&root, &metadata, &version)?;
    let closure = declaration
        .dependencies
        .map(|request| {
            dependencies::collect_for_version(
                request, base, &root, entry, &modules, &stdlib, &version,
            )
        })
        .transpose()?
        .unwrap_or_default();
    let declared = modules.iter().cloned().collect();
    payloads.extend(closure.payloads);
    let mut packages = if closure.report.is_null() {
        crate::cpython_inventory::packages_for_version(&root, &payloads, &version)?
    } else {
        crate::cpython_inventory::payload_packages_for_version(&payloads, &version)?
    };
    packages.extend(closure.modules);
    modules.extend(stdlib.iter().cloned());
    modules.extend(packages.iter().cloned());
    modules.sort();
    modules.dedup();
    for included in &sess.include_modules {
        ensure!(
            !modules.iter().any(|root| domain(root, included)),
            "模块同时声明为RyuPy源码与CPython完整依赖族：{included}"
        );
    }
    let bridge = match declaration.bridge_directory {
        Some(path) => absolute(base, &path)?,
        None => crate::runtime::cpython_bridge_dir()?,
    };
    let (shim, shim_build) = match declaration.shim {
        Some(path) => (absolute(base, &path)?, None),
        None => {
            let (path, owner) = shim::build(&root, &version)?;
            (path, Some(owner))
        }
    };
    ensure!(
        bridge.join("RyuPy.CPython.dll").is_file(),
        "缺少 RyuPy.CPython.dll，请先构建随包宿主或指定bridge_directory：{}",
        bridge.display()
    );
    ensure!(shim.is_file(), "缺少 CPython shim：{}", shim.display());
    native::preflight_version(&root, &version)?;
    sess.cpython_provider_state.plan = Some(Plan {
        version,
        root,
        source: declaration.source,
        modules,
        stdlib,
        declared,
        packages,
        container_transfer: declaration.container_transfer,
        payloads,
        bridge,
        shim,
        _shim_build: shim_build,
        dependency_report: closure.report,
    });
    Ok(())
}

pub(crate) fn selected(sess: &Session, name: &str) -> bool {
    sess.cpython_provider_state
        .plan
        .as_ref()
        .is_some_and(|plan| plan.modules.iter().any(|root| domain(root, name)))
}
fn domain(root: &str, name: &str) -> bool {
    root == name
        || name
            .strip_prefix(root)
            .is_some_and(|tail| tail.starts_with('.'))
}
fn absolute(base: &Path, path: &Path) -> Result<PathBuf> {
    Ok(if path.is_absolute() {
        path.into()
    } else {
        base.join(path)
    })
}
pub(crate) fn relative(text: &str) -> Result<()> {
    ensure!(
        !text.is_empty()
            && !text.contains('\\')
            && !text.chars().any(char::is_control)
            && Path::new(text)
                .components()
                .all(|part| matches!(part, Component::Normal(_))),
        "CPython 分发路径必须是无逃逸相对路径：{text:?}"
    );
    Ok(())
}
/// 联合C绑定包的清单角色沿显式provider计划分类，不按扩展名猜任意文件。
pub(crate) fn artifact_role(sess: &Session, path: &str) -> Option<&'static str> {
    sess.cpython_provider_state.plan.as_ref()?;
    match path {
        "RyuPy.CPython.dll" => Some("runtime_provider_managed"),
        CONFIG => Some("runtime_provider_configuration"),
        REPORT => Some("runtime_provider_manifest"),
        value if value == native::shim_name() => Some("runtime_provider_shim"),
        value if value.starts_with("python/") => Some("runtime_provider_dependency"),
        _ => None,
    }
}
/// 显式provider使用Bridge构建的ProjectReference输出；普通模式沿既有runtime定位合同。
pub(crate) fn copy_runtime_to(sess: &Session, output: &Path) -> Result<()> {
    if let Some(plan) = &sess.cpython_provider_state.plan {
        crate::runtime::copy_runtime_from(&plan.bridge, output)
            .context("CPython桥输出必须包含Runtime与完整依赖，请先构建完整runtime-cpython")
    } else {
        crate::runtime::copy_runtime_to(output)
    }
}
pub(crate) fn stage(sess: &Session, output: &Path) -> Result<()> {
    if let Some(plan) = &sess.cpython_provider_state.plan {
        deployment::stage(plan, output)?;
    }
    Ok(())
}

#[cfg(test)]
#[path = "cpython_provider_tests.rs"]
mod tests;

fn stdlib_provider() -> String {
    "cpython".into()
}
