//! 从固定发行源码布局和上游构建元数据静态冻结执行域；不启动 Python、不加载扩展。
use super::cpython_provider::{Payload, Plan, native};
use anyhow::{Context, Result, ensure};
use std::collections::BTreeSet;
use std::path::Path;

#[allow(dead_code)]
pub(crate) fn stdlib(root: &Path, metadata: &serde_json::Value) -> Result<BTreeSet<String>> {
    stdlib_for_version(
        root,
        metadata,
        &native::version::PythonVersion::new(3, 12, 13),
    )
}

pub(crate) fn stdlib_for_version(
    root: &Path,
    metadata: &serde_json::Value,
    version: &native::version::PythonVersion,
) -> Result<BTreeSet<String>> {
    let mut names = BTreeSet::new();
    for directory_path in native::stdlib_dirs_for_version(version)? {
        names.extend(directory_for_version(
            &root.join(directory_path),
            true,
            version,
        )?);
    }
    if let Some(extensions) = metadata["build_info"]["extensions"].as_object() {
        names.extend(extensions.keys().filter(|name| valid_root(name)).cloned());
    }
    // 任何合规 CPython 实现中，核心内置模块均直接在 libpython 中可用
    for core_mod in &[
        "sys",
        "builtins",
        "math",
        "time",
        "gc",
        "itertools",
        "marshal",
        "posix",
        "nt",
        "_io",
        "_thread",
    ] {
        names.insert((*core_mod).to_string());
    }
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

#[allow(dead_code)]
pub(crate) fn packages(root: &Path, payloads: &[Payload]) -> Result<BTreeSet<String>> {
    packages_for_version(
        root,
        payloads,
        &native::version::PythonVersion::new(3, 12, 13),
    )
}

pub(crate) fn packages_for_version(
    root: &Path,
    payloads: &[Payload],
    version: &native::version::PythonVersion,
) -> Result<BTreeSet<String>> {
    let site_rel = native::site_packages_relpath_for_version(version)?;
    let relative_site = site_rel.strip_prefix("python").unwrap_or(&site_rel);
    let mut names = directory_for_version(&root.join(relative_site), true, version)?;
    names.extend(payload_packages_for_version(payloads, version)?);
    Ok(names)
}

/// 自动闭包排除基础发行开发包，库存只从本次实际载荷产生。
#[allow(dead_code)]
pub(crate) fn payload_packages(payloads: &[Payload]) -> Result<BTreeSet<String>> {
    payload_packages_for_version(payloads, &native::version::PythonVersion::new(3, 12, 13))
}

pub(crate) fn payload_packages_for_version(
    payloads: &[Payload],
    version: &native::version::PythonVersion,
) -> Result<BTreeSet<String>> {
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
            if let Some(name) = module_file_for_version(&payload.destination, true, version) {
                names.insert(name);
            }
        }
    }
    Ok(names)
}

#[allow(dead_code)]
fn directory(root: &Path, extensions: bool) -> Result<BTreeSet<String>> {
    directory_for_version(
        root,
        extensions,
        &native::version::PythonVersion::new(3, 12, 13),
    )
}

fn directory_for_version(
    root: &Path,
    extensions: bool,
    version: &native::version::PythonVersion,
) -> Result<BTreeSet<String>> {
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
            if let Some(name) = module_file_for_version(file, extensions, version) {
                names.insert(name);
            }
        }
    }
    Ok(names)
}

fn valid_root(name: &str) -> bool {
    !name.contains('.') && name != "__main__" && super::module_hints::valid_name(name)
}

#[allow(dead_code)]
fn module_file(name: &str, extensions: bool) -> Option<String> {
    module_file_for_version(
        name,
        extensions,
        &native::version::PythonVersion::new(3, 12, 13),
    )
}

fn module_file_for_version(
    name: &str,
    extensions: bool,
    version: &native::version::PythonVersion,
) -> Option<String> {
    let module = if let Some(name) = name.strip_suffix(".py") {
        name
    } else if extensions {
        // 按目标平台与版本正式扩展后缀识别
        native::extension_suffixes_for_version(version)
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
