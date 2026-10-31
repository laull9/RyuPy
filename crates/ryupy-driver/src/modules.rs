//! 模块发现与模块图（M2.7）。
//!
//! 职责边界（见 docs/architecture.md §7）：
//! - driver 读文件、决定搜索路径、把导入名解析成源文件；
//! - HIR 只接收「模块身份」并做纯语法层面的相对导入绝对化，不读文件。
//!
//! 搜索路径当前只有一条：**入口文件所在目录**（等价于 `sys.path[0]`）。命名空间包、
//! 第三方 site-packages 与动态模块名留到后续里程碑；找不到的模块不阻止编译，
//! 由运行时抛 `ModuleNotFoundError`。

use std::collections::{BTreeMap, BTreeSet};
use std::path::{Path, PathBuf};

use anyhow::Result;
use ryupy_syntax::ParsedModule;

use crate::Session;

/// 一个已解析的源模块。
pub struct SourceModule {
    /// 点分绝对模块名；入口是 `__main__`。
    pub name: String,
    /// `__package__`。
    pub package: String,
    /// `__file__` / traceback 用的文件名。
    pub file: String,
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
    let root = entry
        .parent()
        .filter(|p| !p.as_os_str().is_empty())
        .unwrap_or(Path::new("."))
        .to_path_buf();
    let roots: Vec<PathBuf> = std::iter::once(root)
        .chain(search_paths.iter().cloned())
        .collect();

    let mut modules: BTreeMap<String, SourceModule> = BTreeMap::new();
    let mut queue: Vec<String> = Vec::new();
    let mut seen: BTreeSet<String> = BTreeSet::new();

    // 入口模块：名字固定 `__main__`，不参与按名字查重。
    let entry_source = read_module(sess, entry, "__main__", "")?;
    collect_requests(&entry_source.parsed, &entry_source.package, &mut queue);
    let entry_module = entry_source;

    while let Some(name) = queue.pop() {
        if !seen.insert(name.clone()) {
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
        let Some(path) = resolve_in(&roots, &name) else {
            // 未找到：编译不过滤，运行时抛 ModuleNotFoundError。
            continue;
        };
        let package = module_package(&name, &path);
        let source = read_module(sess, &path, &name, &package)?;
        collect_requests(&source.parsed, &source.package, &mut queue);
        modules.insert(name, source);
    }

    let mut ordered: Vec<SourceModule> = modules.into_values().collect();
    ordered.insert(0, entry_module);
    Ok((ordered, 0))
}

/// 入口模块的「单文件」来源（`dump` 用）：不做模块图搜索。
pub fn load_single(sess: &mut Session, entry: &Path) -> Result<SourceModule> {
    read_module(sess, entry, "__main__", "")
}

fn read_module(sess: &mut Session, path: &Path, name: &str, package: &str) -> Result<SourceModule> {
    let file = sess.load_file(path)?;
    let text = sess.sources.get(file).text.clone();
    let (parsed, diags) = ryupy_syntax::parse(file, &text);
    sess.diagnostics.extend(diags);
    let display = path
        .file_name()
        .map(|n| n.to_string_lossy().into_owned())
        .unwrap_or_else(|| path.display().to_string());
    Ok(SourceModule {
        name: name.to_owned(),
        package: package.to_owned(),
        file: display,
        parsed,
        text,
    })
}

fn collect_requests(parsed: &ParsedModule, package: &str, queue: &mut Vec<String>) {
    for request in ryupy_hir::imports::collect_imports(&parsed.module.body, package) {
        queue.push(request.module.clone());
        if let Some(name) = &request.name {
            queue.push(format!("{}.{name}", request.module));
        }
    }
}

/// `a.b.c` 的所有包前缀：`a`、`a.b`。
fn package_prefixes(name: &str) -> Vec<String> {
    let parts: Vec<&str> = name.split('.').collect();
    (1..parts.len()).map(|i| parts[..i].join(".")).collect()
}

/// 在所有搜索根里找模块：每个根都优先 `<root>/a/b.py`，其次 `<root>/a/b/__init__.py`。
fn resolve_in(roots: &[PathBuf], name: &str) -> Option<PathBuf> {
    let relative = name.replace('.', "/");
    for root in roots {
        let file = root.join(format!("{relative}.py"));
        if file.is_file() {
            return Some(file);
        }
        let init = root.join(&relative).join("__init__.py");
        if init.is_file() {
            return Some(init);
        }
    }
    None
}

/// 模块的 `__package__`：包自身是模块名，普通文件的包是它的父命名空间。
fn module_package(name: &str, path: &Path) -> String {
    if path.file_name().is_some_and(|n| n == "__init__.py") {
        return name.to_owned();
    }
    name.rsplit_once('.')
        .map(|(parent, _)| parent.to_owned())
        .unwrap_or_default()
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
