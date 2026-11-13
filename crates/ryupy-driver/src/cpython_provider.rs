//! 显式 CPython provider 的构建期计划：固定发行来源与执行域，应用入口始终进入现有编译链。
use crate::Session;
use anyhow::{Context, Result, ensure};
use serde::{Deserialize, Serialize};
use std::path::{Component, Path, PathBuf};

#[path = "cpython_deployment.rs"]
mod deployment;
#[path = "cpython_native.rs"]
mod native;
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
    python_version: String,
    distribution_root: PathBuf,
    source: Source,
    modules: Vec<String>,
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
    pub root: PathBuf,
    pub source: Source,
    pub modules: Vec<String>,
    pub container_transfer: ContainerTransfer,
    pub payloads: Vec<Payload>,
    pub bridge: PathBuf,
    pub shim: PathBuf,
}

pub(crate) fn prepare(sess: &mut Session) -> Result<()> {
    sess.cpython_provider_state.plan = None;
    let Some(path) = &sess.cpython_provider else {
        return Ok(());
    };
    ensure!(
        !sess.fixed_binding_build,
        "CPython provider 与独立 C ABI 绑定请使用独立构建入口"
    );
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
    ensure!(
        declaration.python_version == VERSION,
        "CPython provider 固定版本为 {VERSION}"
    );
    let source = declaration.source;
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
    let header = std::fs::read_to_string(root.join("include/python3.12/patchlevel.h"))
        .context("CPython 发行缺少固定版本头文件")?;
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
    ensure!(
        !declaration.modules.is_empty(),
        "CPython provider 必须显式声明模块执行域"
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
    let default_bridge =
        crate::runtime::workspace_root().join("runtime/RyuPy.CPython/bin/Release/net10.0");
    let bridge = declaration
        .bridge_directory
        .map(|path| absolute(base, &path))
        .transpose()?
        .unwrap_or(default_bridge);
    let shim = declaration
        .shim
        .map(|path| absolute(base, &path))
        .transpose()?
        .unwrap_or_else(|| bridge.join(native::shim_name()));
    ensure!(
        bridge.join("RyuPy.CPython.dll").is_file(),
        "缺少 RyuPy.CPython.dll，请先构建随包宿主"
    );
    ensure!(shim.is_file(), "缺少 CPython shim：{}", shim.display());
    native::preflight(&root)?;
    sess.cpython_provider_state.plan = Some(Plan {
        root,
        source,
        modules,
        container_transfer: declaration.container_transfer,
        payloads,
        bridge,
        shim,
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
pub(crate) fn stage(sess: &Session, output: &Path) -> Result<()> {
    if let Some(plan) = &sess.cpython_provider_state.plan {
        deployment::stage(plan, output)?;
    }
    Ok(())
}

#[cfg(test)]
#[path = "cpython_provider_tests.rs"]
mod tests;
