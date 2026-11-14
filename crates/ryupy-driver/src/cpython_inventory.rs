//! 从固定发行源码布局和上游构建元数据静态冻结执行域；不启动 Python、不加载扩展。
use super::cpython_provider::{Payload, Plan, native};
use anyhow::{Context, Result, ensure};
use std::collections::BTreeSet;
use std::path::Path;

pub(crate) fn stdlib(root: &Path, metadata: &serde_json::Value) -> Result<BTreeSet<String>> {
    let mut names = BTreeSet::new();
    for directory_path in native::stdlib_dirs()? {
        names.extend(directory(&root.join(directory_path), true)?);
    }
    let extensions = metadata["build_info"]["extensions"]
        .as_object()
        .context("CPython 上游元数据缺少扩展模块库存")?;
    names.extend(extensions.keys().filter(|name| valid_root(name)).cloned());
    names.remove("__main__");
    ensure!(
        names.contains("math")
            && names.contains("sys")
            && names.contains("json")
            && names.contains("builtins"),
        "CPython 标准库库存缺少必要模块"
    );
    Ok(names)
}
pub(crate) fn packages(root: &Path, payloads: &[Payload]) -> Result<BTreeSet<String>> {
    let mut names = directory(
        &root.join(native::site_packages_relpath()?.strip_prefix("python")?),
        true,
    )?;
    names.extend(payload_packages(payloads)?);
    Ok(names)
}
/// 自动闭包排除基础发行开发包，库存只从本次实际载荷产生。
pub(crate) fn payload_packages(payloads: &[Payload]) -> Result<BTreeSet<String>> {
    let mut names = BTreeSet::new();
    for payload in payloads {
        let path = Path::new(&payload.destination);
        // 单层载荷可建立导入身份；嵌套资源本身不凭目录名字创建新模块。
        if path.components().count() != 1 {
            continue;
        }
        if payload.source.is_dir() && payload.source.join("__init__.py").is_file() {
            if valid_root(&payload.destination) {
                names.insert(payload.destination.clone());
            }
        } else if payload.source.is_file() {
            if let Some(name) = module_file(&payload.destination, true) {
                names.insert(name);
            }
        }
    }
    Ok(names)
}
fn directory(root: &Path, extensions: bool) -> Result<BTreeSet<String>> {
    let mut names = BTreeSet::new();
    if !root.is_dir() {
        return Ok(names);
    }
    for entry in std::fs::read_dir(root)? {
        let entry = entry?;
        let path = entry.path();
        let file = entry.file_name();
        let file = file.to_str().context("CPython 库存路径不是UTF-8")?;
        if path.is_dir() && path.join("__init__.py").is_file() && valid_root(file) {
            names.insert(file.into());
        } else if path.is_file() {
            if let Some(name) = module_file(file, extensions) {
                names.insert(name);
            }
        }
    }
    Ok(names)
}
fn valid_root(name: &str) -> bool {
    !name.contains('.') && name != "__main__" && super::module_hints::valid_name(name)
}
fn module_file(name: &str, extensions: bool) -> Option<String> {
    let module = if let Some(name) = name.strip_suffix(".py") {
        name
    } else if extensions {
        // 按目标平台正式扩展后缀识别，不按首个点猜模块名。
        native::extension_suffixes()
            .ok()?
            .iter()
            .find_map(|suffix| name.strip_suffix(suffix))?
    } else {
        return None;
    };
    valid_root(module).then(|| module.into())
}
pub(crate) fn reason(plan: &Plan, name: &str) -> &'static str {
    let root = name.split('.').next().unwrap_or(name);
    if plan.stdlib.contains(root) {
        "fixed_stdlib_inventory"
    } else if plan.dependency_report["distributions"]
        .as_array()
        .is_some_and(|items| {
            items.iter().any(|item| {
                item["modules"].as_array().is_some_and(|modules| {
                    modules.iter().any(|module| module.as_str() == Some(root))
                })
            })
        })
    {
        "installed_dependency_closure"
    } else if plan.declared.contains(root) {
        "explicit_dependency_family"
    } else {
        "bundled_package_inventory"
    }
}
