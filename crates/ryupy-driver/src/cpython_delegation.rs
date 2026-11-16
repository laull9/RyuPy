//! 路线 A 显式本地整模块与入口 CPython 委派计划：校验、依赖收集辅助与随包源码部署。
//! 禁止自动探测或静默回退；所有委派目标必须由用户通过 CLI 或显式声明指定。
use crate::{Session, modules::SourceKind};
use anyhow::{Context, Result, bail, ensure};
use std::collections::BTreeSet;
use std::path::{Path, PathBuf};

/// 显式本地委派冻结计划。
#[derive(Debug, Clone, Default)]
pub(crate) struct DelegationPlan {
    pub local_modules: Vec<String>,
    pub dynamic_modules: Vec<String>,
    pub local_payloads: Vec<DelegatedPayload>,
    pub entry_script: Option<DelegatedEntry>,
    /// 编译源码只作为 __file__ / inspect 的随包数据，不加入解释器搜索路径。
    pub compiled_sources: Vec<DelegatedEntry>,
}

/// 单个被委派的本地模块或整包目录载荷。
#[derive(Debug, Clone)]
pub(crate) struct DelegatedPayload {
    pub module_name: String,
    pub source: PathBuf,
    pub boundary: PathBuf,
    pub destination: String,
}

/// 被委派的入口脚本载荷（独立目录部署，不加入 `sys.path` 以防同名遮蔽）。
#[derive(Debug, Clone)]
pub(crate) struct DelegatedEntry {
    pub source: PathBuf,
    pub destination: String,
}

/// 校验后的显式委派请求。
pub(crate) struct DelegationRequest {
    pub local_modules: Vec<String>,
    pub dynamic_modules: Vec<String>,
    pub delegate_entry: bool,
}

/// 在启动依赖收集之前校验 `--cpython-module` / `--cpython-dynamic-module` / `--cpython-entry`
/// 的语法与会话选项冲突，避免先跑外部收集器再失败。
pub(crate) fn validate_requests(
    sess: &Session,
    extra_local: &[String],
    extra_entry: bool,
) -> Result<DelegationRequest> {
    let mut local = sess.cpython_modules.clone();
    local.extend(extra_local.iter().cloned());
    local.sort();
    local.dedup();
    for name in &local {
        ensure!(
            name != "__main__",
            "--cpython-module 不接受 __main__，入口委派请使用 --cpython-entry"
        );
        ensure!(
            crate::module_hints::valid_name(name),
            "--cpython-module 本地模块名非法：{name}"
        );
        ensure!(
            !name.contains('.'),
            "--cpython-module 必须声明顶层模块或包名（如 pkg），避免共享状态拆分：{name}"
        );
        ensure!(
            !sess
                .include_modules
                .iter()
                .any(|included| super::domain(name, included)),
            "模块同时声明为 RyuPy 编译源码（--include-module）与 CPython 委派（--cpython-module）：{name}"
        );
        ensure!(
            !sess
                .instance_modules
                .iter()
                .any(|instance| super::domain(name, instance)),
            "模块同时声明为 RyuPy 实例模块（--instance-module）与 CPython 委派（--cpython-module）：{name}"
        );
    }

    let mut dynamic = sess.cpython_dynamic_modules.clone();
    dynamic.sort();
    dynamic.dedup();
    for name in &dynamic {
        ensure!(
            crate::module_hints::valid_name(name) && name != "__main__",
            "--cpython-dynamic-module 模块名非法：{name}"
        );
        ensure!(
            !local.iter().any(|delegated| super::domain(delegated, name)),
            "模块同时声明为本地委派模块与动态第三方模块：{name}"
        );
        ensure!(
            !sess
                .include_modules
                .iter()
                .any(|included| super::domain(name, included) || super::domain(included, name)),
            "模块同时声明为 RyuPy 编译源码与动态 CPython 模块：{name}"
        );
    }

    let delegate_entry = sess.cpython_entry || extra_entry;
    if delegate_entry {
        ensure!(
            !sess.instance_modules.iter().any(|name| name == "__main__"),
            "入口 `__main__` 不能同时声明为 --instance-module 与 --cpython-entry"
        );
    }
    Ok(DelegationRequest {
        local_modules: local,
        dynamic_modules: dynamic,
        delegate_entry,
    })
}

/// 在标准库与第三方依赖闭包确立后，解析并锁定所有显式本地委派源码路径。
pub(crate) fn resolve_plan(
    entry: &Path,
    search_paths: &[PathBuf],
    request: DelegationRequest,
    stdlib: &BTreeSet<String>,
    third_party: &BTreeSet<String>,
    declared: &BTreeSet<String>,
) -> Result<DelegationPlan> {
    let DelegationRequest {
        local_modules,
        dynamic_modules,
        delegate_entry,
    } = request;
    let entry_dir = entry
        .parent()
        .filter(|p| !p.as_os_str().is_empty())
        .unwrap_or(Path::new("."))
        .to_path_buf();
    let roots: Vec<PathBuf> = std::iter::once(entry_dir)
        .chain(search_paths.iter().cloned())
        .collect();

    let mut local_payloads = Vec::new();
    for name in &local_modules {
        ensure!(
            !stdlib.contains(name),
            "--cpython-module 本地模块 `{name}` 与 CPython 标准库同名，拒绝覆盖标准库"
        );
        ensure!(
            !third_party.contains(name) && !declared.contains(name),
            "--cpython-module 仅用于本地模块，不能与第三方包冲突：{name}"
        );
        let Some((resolved, kind)) = crate::modules::resolve_in(&roots, name) else {
            bail!("--cpython-module 指定的本地模块未在入口目录或搜索路径中找到：{name}");
        };
        match kind {
            SourceKind::ModuleFile => {
                let source = resolved
                    .canonicalize()
                    .with_context(|| format!("无法解析委派本地模块路径：{}", resolved.display()))?;
                let boundary = source
                    .parent()
                    .context("委派本地模块缺少父目录")?
                    .to_path_buf();
                local_payloads.push(DelegatedPayload {
                    module_name: name.clone(),
                    source,
                    boundary,
                    destination: format!("ryupy_delegated/{name}.py"),
                });
            }
            SourceKind::Package => {
                let pkg_dir = resolved
                    .parent()
                    .context("委派本地包缺少父目录")?
                    .canonicalize()
                    .with_context(|| format!("无法解析委派本地包路径：{}", resolved.display()))?;
                local_payloads.push(DelegatedPayload {
                    module_name: name.clone(),
                    source: pkg_dir.clone(),
                    boundary: pkg_dir,
                    destination: format!("ryupy_delegated/{name}"),
                });
            }
            SourceKind::Entry => unreachable!("resolve_in 不会返回 Entry"),
        }
    }

    let entry_script = if delegate_entry {
        let source = entry
            .canonicalize()
            .with_context(|| format!("委派入口文件不存在：{}", entry.display()))?;
        ensure!(
            source.is_file(),
            "委派入口不是普通文件：{}",
            source.display()
        );
        let file_name = source
            .file_name()
            .and_then(|n| n.to_str())
            .context("委派入口文件名必须为 UTF-8")?;
        ensure!(
            !file_name.is_empty() && !file_name.contains(['/', '\\', ':']),
            "委派入口文件名非法：{file_name}"
        );
        let destination = format!("ryupy_entry/{file_name}");
        Some(DelegatedEntry {
            source,
            destination,
        })
    } else {
        None
    };

    Ok(DelegationPlan {
        local_modules,
        dynamic_modules,
        local_payloads,
        entry_script,
        compiled_sources: Vec::new(),
    })
}

/// 将冻结的本地模块/包与委派入口复制到产物 `python/` 目录内。
/// 返回 `(新增 search_paths 相对路径, entry_script 相对路径)`。
pub(crate) fn stage(
    plan: &DelegationPlan,
    python_dir: &Path,
) -> Result<(Option<String>, Option<String>)> {
    for source in &plan.compiled_sources {
        let target = python_dir.join(&source.destination);
        std::fs::create_dir_all(target.parent().context("编译源码缺少目标目录")?)?;
        let boundary = source
            .source
            .parent()
            .context("编译源码缺少输入目录")?
            .canonicalize()?;
        copy_delegated_tree(&source.source, &target, &boundary, &mut Vec::new())?;
    }
    let search_path = if !plan.local_payloads.is_empty() {
        let delegated_root = python_dir.join("ryupy_delegated");
        ensure!(
            !delegated_root.exists(),
            "CPython 委派本地模块目录冲突：{}",
            delegated_root.display()
        );
        std::fs::create_dir_all(&delegated_root)?;
        for payload in &plan.local_payloads {
            let target = python_dir.join(&payload.destination);
            ensure!(
                !target.exists(),
                "CPython 委派目标冲突：{} ({})",
                payload.destination,
                payload.module_name
            );
            copy_delegated_tree(&payload.source, &target, &payload.boundary, &mut Vec::new())?;
        }
        Some("python/ryupy_delegated".to_owned())
    } else {
        None
    };

    let entry_rel = if let Some(entry) = &plan.entry_script {
        let entry_root = python_dir.join("ryupy_entry");
        ensure!(
            !entry_root.exists(),
            "CPython 委派入口目录冲突：{}",
            entry_root.display()
        );
        std::fs::create_dir_all(&entry_root)?;
        let target = python_dir.join(&entry.destination);
        std::fs::copy(&entry.source, &target).with_context(|| {
            format!(
                "无法复制委派入口源码 {} -> {}",
                entry.source.display(),
                target.display()
            )
        })?;
        Some(format!("python/{}", entry.destination))
    } else {
        None
    };

    Ok((search_path, entry_rel))
}

/// 递归复制委派源码树：解引用内部安全符号链接，拒绝越界链接和目录环，跳过字节码缓存及平台旁件。
fn copy_delegated_tree(
    source: &Path,
    output: &Path,
    boundary: &Path,
    stack: &mut Vec<PathBuf>,
) -> Result<()> {
    let resolved = source
        .canonicalize()
        .with_context(|| format!("CPython 委派源码无法解析：{}", source.display()))?;
    ensure!(
        resolved.starts_with(boundary),
        "CPython 委派源码符号链接越过授权源根：{}",
        source.display()
    );
    let metadata = std::fs::metadata(&resolved)?;
    if metadata.is_dir() {
        ensure!(
            !stack.contains(&resolved),
            "CPython 委派源码包含目录链接环：{}",
            source.display()
        );
        stack.push(resolved.clone());
        std::fs::create_dir_all(output)?;
        let mut entries = std::fs::read_dir(&resolved)?.collect::<std::io::Result<Vec<_>>>()?;
        entries.sort_by_key(|entry| entry.file_name());
        for entry in entries {
            let name = entry.file_name();
            let entry_path = entry.path();
            let sym = std::fs::symlink_metadata(&entry_path)?;
            let lossy = name.to_string_lossy();
            if lossy == "__pycache__"
                || lossy.ends_with(".pyc")
                || lossy.ends_with(".pyo")
                || (!sym.file_type().is_symlink()
                    && crate::manifest_files::is_os_sidecar(&entry_path, &sym))
            {
                continue;
            }
            copy_delegated_tree(&entry_path, &output.join(name), boundary, stack)?;
        }
        stack.pop();
    } else {
        ensure!(
            metadata.is_file(),
            "CPython 委派源码含非普通文件：{}",
            source.display()
        );
        if let Some(parent) = output.parent() {
            std::fs::create_dir_all(parent)?;
        }
        std::fs::copy(&resolved, output)?;
    }
    Ok(())
}

/// 为显式委派本地模块/包收集其静态 `import` 目标，以便未列入 `--cpython-module` 的本地依赖
/// 仍进入 RyuPy 编译图（或对未支持语法明确报错，禁止静默回退）。
pub(crate) fn enqueue_local_imports(roots: &[PathBuf], name: &str, queue: &mut Vec<String>) {
    let Some((resolved, kind)) = crate::modules::resolve_in(roots, name) else {
        return;
    };
    let mut db = ryupy_base::SourceDb::default();
    match kind {
        SourceKind::ModuleFile => {
            let pkg = crate::modules::module_package(name, &resolved);
            collect_file_imports(&resolved, &pkg, &mut db, queue);
        }
        SourceKind::Package => {
            let Some(pkg_dir) = resolved.parent() else {
                return;
            };
            collect_package_imports(pkg_dir, name, &mut db, queue, 0);
        }
        SourceKind::Entry => {}
    }
}

fn collect_file_imports(
    path: &Path,
    package: &str,
    db: &mut ryupy_base::SourceDb,
    queue: &mut Vec<String>,
) {
    let Ok(text) = std::fs::read_to_string(path) else {
        return;
    };
    let file = db.add(path.display().to_string(), text.clone());
    let (parsed, _) = ryupy_syntax::parse(file, &text);
    crate::modules::collect_requests(&parsed, package, queue);
}

fn collect_package_imports(
    dir: &Path,
    pkg_name: &str,
    db: &mut ryupy_base::SourceDb,
    queue: &mut Vec<String>,
    depth: usize,
) {
    if depth > 32 {
        return;
    }
    let Ok(entries) = std::fs::read_dir(dir) else {
        return;
    };
    let mut items: Vec<_> = entries.flatten().collect();
    items.sort_by_key(|e| e.file_name());
    for entry in items {
        let path = entry.path();
        let file_name = entry.file_name();
        let lossy = file_name.to_string_lossy();
        if lossy == "__pycache__" || lossy.starts_with('.') {
            continue;
        }
        if path.is_dir() {
            if path.join("__init__.py").is_file() {
                collect_package_imports(
                    &path,
                    &format!("{pkg_name}.{lossy}"),
                    db,
                    queue,
                    depth + 1,
                );
            }
        } else if path.extension().is_some_and(|ext| ext == "py") {
            collect_file_imports(&path, pkg_name, db, queue);
        }
    }
}

#[cfg(test)]
#[path = "cpython_delegation_tests.rs"]
mod tests;
