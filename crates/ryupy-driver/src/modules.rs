//! 模块发现与模块图（M2.7）。
//!
//! 职责边界（见 docs/architecture.md §7）：
//! - driver 读文件、决定搜索路径、把导入名解析成源文件；
//! - HIR 只接收「模块身份」并做纯语法层面的相对导入绝对化，不读文件。
//!
//! 搜索路径为入口目录及显式search_paths，不自动扫描site-packages。普通AST导入未找到
//! 源码时仍交原生注册表；显式include必须找到源码及普通父包，否则构建失败。
//! include只建立硬依赖图；产品JIT另可选发现空资源子包，均不提前初始化或复制资源。

use std::collections::{BTreeMap, BTreeSet};
use std::path::{Path, PathBuf};

use anyhow::{Result, ensure};
use ryupy_syntax::ParsedModule;

use crate::Session;

#[path = "module_resolver.rs"]
mod module_resolver;

#[path = "resource_discovery.rs"]
mod resource_discovery;

/// 只记录可选发现的实际理由，不改变模块执行资格。
#[derive(Debug, Clone, serde::Serialize)]
pub struct ResourceDiscovery {
    pub source: String,
    pub anchor: String,
    pub reason: &'static str,
}

/// 一个已解析的源模块。
pub struct SourceModule {
    /// 候选可执行代码与活动导入图分开；硬度只决定编译失败是否延期。
    pub(crate) candidate: bool,
    pub(crate) hard: bool,
    pub(crate) catalog: Option<crate::source_catalog::Token>,
    pub(crate) parse_diagnostics: Vec<ryupy_base::Diagnostic>,
    /// 由实际resolver分支保留；同路径可有Package与ModuleFile两种身份。
    pub kind: SourceKind,
    pub resource_discovery: Option<ResourceDiscovery>,
    /// 本次resolver选中的绝对lexical路径；不是canonical安全身份证明。
    pub resolved_source_path: PathBuf,
    /// 点分绝对模块名；入口是 `__main__`。
    pub name: String,
    /// `__package__`。
    pub package: String,
    /// `__file__` / traceback 用的文件名。
    pub file: String,
    /// 审计逻辑来源，与诊断file和真实部署位置分别建模；不包含源目录绝对路径。
    pub logical_source: String,
    pub parsed: ParsedModule,
    pub text: String,
}

/// 从入口文件构建模块图，返回模块列表（入口固定在 0 号）。
///
/// 搜索顺序：入口文件所在目录 → `search_paths`（按给定顺序）。与 CPython 的
/// `sys.path[0]` 优先规则一致；额外的搜索根用于项目集里的 `third_party/` 源码。
pub fn load_program(
    sess: &mut Session,
    entry: &Path,
    search_paths: &[PathBuf],
) -> Result<(Vec<SourceModule>, usize)> {
    load_program_including(sess, entry, search_paths, &[])
}

/// 将显式源码模块声明纳入同一依赖图；不执行模块，不复制资源。
pub fn load_program_including(
    sess: &mut Session,
    entry: &Path,
    search_paths: &[PathBuf],
    include_modules: &[String],
) -> Result<(Vec<SourceModule>, usize)> {
    load_program_with_discovery(sess, entry, search_paths, include_modules, false)
}

/// 产品编排入口复制本次显式include配置；发现范围仍由调用者的构建种类决定。
pub(crate) fn load_build_graph(
    sess: &mut Session,
    entry: &Path,
    search_paths: &[PathBuf],
    discover_resources: bool,
) -> Result<(Vec<SourceModule>, usize)> {
    let include_modules = sess.include_modules.clone();
    load_program_with_discovery(
        sess,
        entry,
        search_paths,
        &include_modules,
        discover_resources,
    )
}

/// 现有编排显式选择JIT发现；AOT与旧公开硬图入口不自动增图。
pub(crate) fn load_program_with_discovery(
    sess: &mut Session,
    entry: &Path,
    search_paths: &[PathBuf],
    include_modules: &[String],
    discover_resources: bool,
) -> Result<(Vec<SourceModule>, usize)> {
    let root = entry
        .parent()
        .filter(|p| !p.as_os_str().is_empty())
        .unwrap_or(Path::new("."))
        .to_path_buf();
    let roots: Vec<PathBuf> = std::iter::once(root)
        .chain(search_paths.iter().cloned())
        .collect();

    let required = explicit_includes(&roots, include_modules)?;
    let mut modules: BTreeMap<String, SourceModule> = BTreeMap::new();
    let mut queue: Vec<String> = required.iter().cloned().collect();
    let mut seen: BTreeSet<String> = BTreeSet::new();

    // 入口模块：名字固定 `__main__`，不参与按名字查重。
    let entry_source = read_module(sess, entry, "__main__", "", SourceKind::Entry)?;
    collect_requests(&entry_source.parsed, &entry_source.package, &mut queue);
    let entry_module = entry_source;

    while let Some(name) = queue.pop() {
        if !seen.insert(name.clone()) {
            continue;
        }
        if crate::cpython_provider::selected(sess, &name) {
            let top = name.split('.').next().unwrap_or(&name);
            if sess
                .cpython_provider_state
                .plan
                .as_ref()
                .is_some_and(|plan| plan.stdlib.contains(top))
                && let Some((path, _)) = resolve_in(&roots, top)
            {
                sess.diagnostics.push(ryupy_base::Diagnostic::error(ryupy_base::Span::new(entry_module.parsed.file,ryupy_base::TextRange::default()),format!("CPython标准库 `{top}` 已由固定发行库存整体选择，与应用源码 {} 冲突；当前试点拒绝同名遮蔽",path.display())).with_code("E0100"));
            }
            continue;
        }
        // 父包先登记（`import a.b.c` 会依次初始化 a、a.b，运行时依赖它们已注册）。
        if name.contains('.') {
            for prefix in package_prefixes(&name) {
                if !seen.contains(&prefix)
                    && !queue.contains(&prefix)
                    && resolve_in(&roots, &prefix).is_some()
                {
                    queue.push(prefix);
                }
            }
        }
        let Some((path, kind)) = resolve_in(&roots, &name) else {
            // 显式声明不得悄悄退回native；也覆盖预检后文件消失。
            ensure!(!required.contains(&name), "显式包含模块未找到源码：{name}");
            // 未找到源码：留给原生模块注册表；运行时仍未知才抛 ModuleNotFoundError。
            continue;
        };
        if include_modules.iter().any(|declared| {
            declared
                .strip_prefix(&name)
                .is_some_and(|tail| tail.starts_with('.'))
        }) {
            ensure!(
                kind == SourceKind::Package,
                "显式包含模块的父路径不是普通包：{name}"
            );
        }
        let package = module_package(&name, &path);
        let source = read_module(sess, &path, &name, &package, kind)?;
        collect_requests(&source.parsed, &source.package, &mut queue);
        modules.insert(name, source);
    }

    if discover_resources && sess.cpython_provider.is_none() {
        resource_discovery::extend(sess, &roots, &entry_module, &mut modules);
    }
    let mut ordered: Vec<SourceModule> = modules.into_values().collect();
    ordered.insert(0, entry_module);
    Ok((ordered, 0))
}

/// 入口模块的「单文件」来源（`dump` 用）：不做模块图搜索。
pub fn load_single(sess: &mut Session, entry: &Path) -> Result<SourceModule> {
    read_module(sess, entry, "__main__", "", SourceKind::Entry)
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SourceKind {
    Entry,
    ModuleFile,
    Package,
}

fn read_module(
    sess: &mut Session,
    path: &Path,
    name: &str,
    package: &str,
    kind: SourceKind,
) -> Result<SourceModule> {
    let file = sess.load_file(path)?;
    let text = sess.sources.get(file).text.clone();
    let (parsed, diags) = ryupy_syntax::parse(file, &text);
    sess.diagnostics.extend(diags);
    let display = path
        .file_name()
        .map(|n| n.to_string_lossy().into_owned())
        .unwrap_or_else(|| path.display().to_string());
    let resolved_source_path = if path.is_absolute() {
        path.to_path_buf()
    } else {
        std::env::current_dir()?.join(path)
    };
    Ok(SourceModule {
        candidate: false,
        hard: true,
        catalog: None,
        parse_diagnostics: Vec::new(),
        kind,
        resource_discovery: None,
        resolved_source_path,
        name: name.to_owned(),
        package: package.to_owned(),
        logical_source: logical_source(name, &display, kind),
        file: display,
        parsed,
        text,
    })
}

fn collect_requests(parsed: &ParsedModule, package: &str, queue: &mut Vec<String>) {
    for request in ryupy_hir::imports::collect_all_imports(&parsed.module.body, package) {
        queue.push(request.module.clone());
        if let Some(name) = &request.name {
            queue.push(format!("{}.{name}", request.module));
        }
    }
}

/// `a.b.c` 的所有包前缀：`a`、`a.b`。
pub(crate) fn package_prefixes(name: &str) -> Vec<String> {
    let parts: Vec<&str> = name.split('.').collect();
    (1..parts.len()).map(|i| parts[..i].join(".")).collect()
}

/// 首个顶层普通包决定后代来源，不跨root拼接父子。
pub(crate) fn resolve_in(roots: &[PathBuf], name: &str) -> Option<(PathBuf, SourceKind)> {
    module_resolver::resolve(roots, name)
}

/// 固定入口/显式搜索根的负向证据；不承诺该源码能够按包路径加载。
pub(crate) fn source_evidence(roots: &[PathBuf], name: &str) -> bool {
    module_resolver::source_evidence(roots, name)
}

/// 模块的 `__package__`：包自身是模块名，普通文件的包是它的父命名空间。
pub(crate) fn module_package(name: &str, path: &Path) -> String {
    if path.file_name().is_some_and(|n| n == "__init__.py") {
        return name.to_owned();
    }
    name.rsplit_once('.')
        .map(|(parent, _)| parent.to_owned())
        .unwrap_or_default()
}

/// build/dump 共用候选与来源门；原生点分模块的任一源码父路径均取消证明。
/// 只认证已有原生注册项，不因拼接导入文字扩大模块解析能力。
fn certified_native_exception_modules(
    roots: &[PathBuf],
    sources: &[SourceModule],
) -> Vec<&'static str> {
    [
        "json",
        "zipfile",
        "zlib",
        "gzip",
        "optparse",
        "getopt",
        "re",
        "dataclasses",
        "unittest",
        "enum",
        "io",
        "typing",
        "xml.etree.ElementTree",
        "collections.abc",
    ]
    .into_iter()
    .filter(|name| {
        package_prefixes(name)
            .iter()
            .map(String::as_str)
            .chain(std::iter::once(*name))
            .all(|part| {
                !sources
                    .iter()
                    .any(|source| !source.candidate && source.name == part)
                    && !source_evidence(roots, part)
            })
    })
    .collect()
}

/// build 同时检查实际模块图及入口/显式搜索根，父包遮蔽不依赖加载顺序。
pub(crate) fn native_exception_modules_for_build(
    entry: &Path,
    search_paths: &[PathBuf],
    sources: &[SourceModule],
) -> Vec<&'static str> {
    let root = entry.parent().unwrap_or(Path::new(".")).to_path_buf();
    let roots: Vec<_> = std::iter::once(root)
        .chain(search_paths.iter().cloned())
        .collect();
    certified_native_exception_modules(&roots, sources)
}

/// dump 不遍历依赖源码，但使用同一来源门检查入口目录。
pub(crate) fn native_exception_modules(entry: &Path) -> Vec<&'static str> {
    native_exception_modules_for_build(entry, &[], &[])
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn resolves_packages_and_modules() {
        assert_eq!(module_package("a.b", Path::new("a/b.py")), "a");
        assert_eq!(module_package("a.b", Path::new("a/b/__init__.py")), "a.b");
        assert_eq!(module_package("x", Path::new("x.py")), "");
        assert_eq!(package_prefixes("a.b.c"), vec!["a", "a.b"]);
    }
}

/// 按已解析的实际文件种类记录逻辑来源；不从点分模块名猜是否为包。
pub(crate) fn logical_source(module: &str, file_name: &str, kind: SourceKind) -> String {
    match kind {
        SourceKind::Entry => format!("entry/{file_name}"),
        SourceKind::ModuleFile => format!("modules/{}.py", module.replace('.', "/")),
        SourceKind::Package => format!("modules/{}/__init__.py", module.replace('.', "/")),
    }
}

#[cfg(test)]
mod logical_source_tests {
    use super::*;

    #[test]
    fn actual_package_kind_and_same_basename_stay_distinct() {
        assert_eq!(
            logical_source("__main__", "main.py", SourceKind::Entry),
            "entry/main.py"
        );
        assert_eq!(
            logical_source("left.common", "common.py", SourceKind::ModuleFile),
            "modules/left/common.py"
        );
        assert_eq!(
            logical_source("right.common", "common.py", SourceKind::ModuleFile),
            "modules/right/common.py"
        );
        assert_eq!(
            logical_source("left", "__init__.py", SourceKind::Package),
            "modules/left/__init__.py"
        );
        assert_eq!(
            logical_source("left", "left.py", SourceKind::ModuleFile),
            "modules/left.py"
        );
    }
    #[test]
    fn loaded_sources_keep_actual_text_and_distinct_logical_names() {
        let root = crate::test_temp::Root::new("module-paths");
        let result = (|| -> anyhow::Result<()> {
            for package in ["left", "right"] {
                std::fs::create_dir(root.join(package))?;
                std::fs::write(root.join(package).join("__init__.py"), "")?;
                std::fs::write(
                    root.join(package).join("common.py"),
                    format!("value = '{package}'\r\n"),
                )?;
            }
            std::fs::write(
                root.join("common.py"),
                "import left.common\nimport right.common\n",
            )?;
            let (sources, entry) =
                load_program(&mut Session::default(), &root.join("common.py"), &[])?;
            assert_eq!(entry, 0);
            let names: std::collections::BTreeSet<_> = sources
                .iter()
                .map(|source| source.logical_source.as_str())
                .collect();
            assert_eq!(names.len(), sources.len());
            assert!(names.contains("entry/common.py"));
            assert!(names.contains("modules/left/common.py"));
            assert!(names.contains("modules/right/common.py"));
            assert!(names.contains("modules/left/__init__.py"));
            let original = sources
                .iter()
                .find(|source| source.name == "left.common")
                .unwrap();
            std::fs::write(root.join("left/common.py"), "changed = True\n")?;
            assert_eq!(original.text.as_bytes(), b"value = 'left'\r\n");
            assert!(original.resolved_source_path.is_absolute());
            assert_eq!(original.resolved_source_path, root.join("left/common.py"));
            assert!(
                sources
                    .iter()
                    .all(|source| !source.logical_source.contains(root.to_str().unwrap()))
            );
            Ok(())
        })();
        result.unwrap();
    }
    #[test]
    fn importing_init_as_module_uses_actual_file_branch_not_package_guess() {
        let root = crate::test_temp::Root::new("module-paths");
        let result = (|| -> anyhow::Result<()> {
            std::fs::create_dir(root.join("pkg"))?;
            std::fs::write(root.join("pkg/__init__.py"), "value = 1\n")?;
            std::fs::write(root.join("main.py"), "import pkg\nimport pkg.__init__\n")?;
            let (sources, _) = load_program(&mut Session::default(), &root.join("main.py"), &[])?;
            let package = sources.iter().find(|source| source.name == "pkg").unwrap();
            let module = sources
                .iter()
                .find(|source| source.name == "pkg.__init__")
                .unwrap();
            assert_eq!(package.logical_source, "modules/pkg/__init__.py");
            assert_eq!(package.resolved_source_path, module.resolved_source_path);
            assert_eq!(package.resolved_source_path, root.join("pkg/__init__.py"));
            assert_eq!(module.logical_source, "modules/pkg/__init__.py");
            assert_ne!(package.name, module.name);
            // 两个模块身份可对应同一真实源文件；不得虚构第二层__init__位置。
            assert_eq!(package.text, module.text);
            Ok(())
        })();
        result.unwrap();
    }
}

/// 新公开选项限定ASCII点分标识符；普通AST导入不经过此限制。
fn explicit_includes(roots: &[PathBuf], names: &[String]) -> Result<BTreeSet<String>> {
    let mut required = BTreeSet::new();
    for name in names {
        ensure!(
            name.split('.').next() != Some("__main__")
                && name.split('.').all(|part| {
                    let mut bytes = part.bytes();
                    bytes
                        .next()
                        .is_some_and(|b| b.is_ascii_alphabetic() || b == b'_')
                        && bytes.all(|b| b.is_ascii_alphanumeric() || b == b'_')
                }),
            "--include-module 需要ASCII点分绝对模块名（不接受路径）：{name:?}"
        );
        for prefix in package_prefixes(name) {
            ensure!(
                matches!(resolve_in(roots, &prefix), Some((_, SourceKind::Package))),
                "显式包含模块 {name} 的父包未找到或不是普通包：{prefix}"
            );
            required.insert(prefix);
        }
        ensure!(
            resolve_in(roots, name).is_some(),
            "显式包含模块未找到源码：{name}"
        );
        required.insert(name.clone());
    }
    Ok(required)
}

#[cfg(test)]
#[path = "modules_include_tests.rs"]
mod include_tests;

#[cfg(test)]
#[path = "modules_exception_identity_tests.rs"]
mod exception_identity_tests;
