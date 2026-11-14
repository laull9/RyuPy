//! 将冻结的 CPython 基础发行与显式包载荷部署到应用旁；JIT 与 NativeAOT 共用相同配置与分发清单。
use super::{CONFIG, Plan, REPORT, native};
use anyhow::{Context, Result, ensure};
use serde_json::json;
use std::path::Path;

pub(super) fn stage(plan: &Plan, output: &Path) -> Result<()> {
    let resolved_output = output.canonicalize()?;
    ensure!(
        !resolved_output.starts_with(&plan.root),
        "CPython 输出不能位于基础发行输入内"
    );
    for payload in &plan.payloads {
        ensure!(
            !resolved_output.starts_with(&payload.source),
            "CPython 输出不能位于包载荷输入内"
        );
    }
    let python = output.join("python");
    ensure!(
        !python.exists(),
        "CPython 输出目录已存在，须选择新的输出目录：{}",
        python.display()
    );
    std::fs::create_dir(&python)?;
    let site_packages = native::site_packages_relpath_for_version(&plan.version)?;
    let relative_site = site_packages
        .strip_prefix("python")
        .context("平台site-packages路径必须以python开头")?;
    let excluded = (!plan.dependency_report.is_null()).then(|| plan.root.join(relative_site));
    for directory in native::copy_directories()? {
        if plan.root.join(directory).is_dir() {
            // 自动闭包只复制已选第三方文件；基础发行中的开发包不能旁路冻结计划。
            copy_tree(
                &plan.root.join(directory),
                &python.join(directory),
                &plan.root.canonicalize()?,
                &mut Vec::new(),
                excluded.as_deref(),
            )?;
        }
    }
    for file in [
        "BUILD",
        "PYTHON.json",
        "license-sources.json",
        "distribution-source.json",
    ] {
        if plan.root.join(file).is_file() {
            copy_authorized(&plan.root.join(file), &python.join(file), &plan.root)?;
        }
    }
    // Windows核心库位于发行根；Unix核心库已随lib目录复制。
    if cfg!(windows) {
        for entry in std::fs::read_dir(&plan.root)? {
            let entry = entry?;
            if entry.file_type()?.is_file()
                && entry
                    .path()
                    .extension()
                    .is_some_and(|ext| ext.eq_ignore_ascii_case("dll"))
            {
                copy_authorized(&entry.path(), &python.join(entry.file_name()), &plan.root)?;
            }
        }
    }
    // Windows等发行可把许可证放在根目录；声明的每份许可都必须进入产品。
    for license in &plan.source.licenses {
        let target = python.join(license);
        if !target.is_file() {
            copy_authorized(&plan.root.join(license), &target, &plan.root)?;
        }
    }
    for payload in &plan.payloads {
        let destination = output.join(&site_packages).join(&payload.destination);
        ensure!(
            !destination.exists(),
            "CPython 包载荷目标冲突：{}",
            payload.destination
        );
        copy_authorized(&payload.source, &destination, &payload.source)?;
    }
    ensure!(
        output.join("RyuPy.CPython.dll").is_file(),
        "CPython托管桥尚未随同Runtime冻结"
    );
    std::fs::copy(&plan.shim, output.join(native::shim_name()))?;
    native::relocate(output)?;
    let mut files = Vec::new();
    list_files(&python, output, &mut files)?;
    // 冻结第三方附属DLL目录，运行时注册相同搜索闭包，避免审计与实际加载脱节。
    let native_search_paths: std::collections::BTreeSet<_> = files
        .iter()
        .filter_map(|file| file["path"].as_str())
        .filter(|path| {
            Path::new(path)
                .extension()
                .is_some_and(|ext| ext.eq_ignore_ascii_case("dll"))
        })
        .filter_map(|path| {
            Path::new(path)
                .parent()
                .map(|parent| parent.to_string_lossy().into_owned())
        })
        .collect();
    let version_str = plan.version.version_str();
    let config = json!({
        "schema": 1, "python_version": version_str, "home": "python",
        "library": native::library_relpath_for_version(&plan.version)?,
        "shim": native::shim_name(),
        "search_paths": native::search_paths_for_version(&plan.version)?, "native_search_paths": native_search_paths,
        "stdlib_provider": "cpython", "modules": plan.modules, "container_transfer": plan.container_transfer,
    });
    std::fs::write(output.join(CONFIG), serde_json::to_vec_pretty(&config)?)?;
    let report = json!({
        "schema": 1, "python_version": version_str, "rid": native::rid()?,
        "source": plan.source,
        "archive_url": native::archive_url()?,
        "build_configuration": {"implementation": "cpython", "gil": true, "archive_layout": "install_only", "source_build": format!("{}@{}", plan.source.project, plan.source.build_id)},
        "container_transfer": plan.container_transfer, "execution": "ryupy_compiled_entry",
        "modules": plan.modules.iter().map(|name| (name.clone(), json!({"provider": "cpython", "reason": crate::cpython_inventory::reason(plan,name)}))).collect::<serde_json::Map<_,_>>(),
        "stdlib_provider": "cpython", "stdlib_inventory": plan.stdlib, "declared_modules": plan.declared, "package_inventory": plan.packages,
        "entry_provider": "ryupy_compiled_source", "files": files,
        "dependency_collection": plan.dependency_report,
        "payloads": plan.payloads.iter().map(|payload| &payload.destination).collect::<Vec<_>>(),
        "managed_bundle": {"source": "single_bridge_output", "symbols": "embedded_portable_pdb"},
        "system_dependencies": native::system_dependencies(),
        // 证书属于宿主数据依赖；固定PBS首发使用OpenSSL的系统默认路径。
        "certificate_policy": native::certificate_policy()?,
        "dependency_downloads_at_runtime": false,
    });
    std::fs::write(output.join(REPORT), serde_json::to_vec_pretty(&report)?)?;
    Ok(())
}

/// AOT 项目只在本次输入旁存在显式计划时引用互操作程序集；旧原生构建保留原项目。
pub(crate) fn extend_project(text: String, runtime: &Path) -> Result<String> {
    let base = runtime.parent().context("运行时路径缺少父目录")?;
    if !base.join(CONFIG).is_file() {
        return Ok(text);
    }
    let bridge = crate::aot::path_literal(&base.join("RyuPy.CPython.dll"))?;
    Ok(text.replace("</Project>", &format!("  <ItemGroup><Reference Include=\"RyuPy.CPython\"><HintPath>{bridge}</HintPath></Reference></ItemGroup>\n</Project>")))
}

/// 在 AOT 候选 publish 中加入与 JIT 相同的封闭依赖，再由外层事务发布。
pub(crate) fn publish(source: &Path, output: &Path) -> Result<()> {
    if !source.join(CONFIG).is_file() {
        return Ok(());
    }
    for name in [CONFIG, REPORT, native::shim_name()] {
        ensure!(source.join(name).is_file(), "CPython 构建分发缺少 {name}");
        std::fs::copy(source.join(name), output.join(name))?;
    }
    copy_authorized(
        &source.join("python"),
        &output.join("python"),
        &source.join("python"),
    )?;
    Ok(())
}

/// 基础发行内链接必须仍指向发行树内部；解引用复制后产品目录不依赖原安装路径。
fn copy_authorized(source: &Path, output: &Path, boundary: &Path) -> Result<()> {
    copy_tree(
        source,
        output,
        &boundary.canonicalize()?,
        &mut Vec::new(),
        None,
    )
}
fn copy_tree(
    source: &Path,
    output: &Path,
    boundary: &Path,
    stack: &mut Vec<std::path::PathBuf>,
    excluded: Option<&Path>,
) -> Result<()> {
    if excluded == Some(source) {
        return Ok(());
    }
    let resolved = source
        .canonicalize()
        .with_context(|| format!("CPython 载荷无法解析：{}", source.display()))?;
    ensure!(
        resolved.starts_with(boundary),
        "CPython 载荷符号链接越过授权源根：{}",
        source.display()
    );
    let metadata = std::fs::metadata(&resolved)?;
    if metadata.is_dir() {
        ensure!(
            !stack.contains(&resolved),
            "CPython 载荷包含目录链接环：{}",
            source.display()
        );
        stack.push(resolved.clone());
        std::fs::create_dir_all(output)?;
        let mut entries = std::fs::read_dir(&resolved)?.collect::<std::io::Result<Vec<_>>>()?;
        entries.sort_by_key(|entry| entry.file_name());
        for entry in entries {
            let name = entry.file_name();
            // 字节码由源文件决定，开发机缓存与工具安装标记不进入发行。
            if name == "__pycache__" || name == ".DS_Store" {
                continue;
            }
            copy_tree(&entry.path(), &output.join(name), boundary, stack, excluded)?;
        }
        stack.pop();
    } else {
        ensure!(
            metadata.is_file(),
            "CPython 发行含非普通文件：{}",
            source.display()
        );
        if let Some(parent) = output.parent() {
            std::fs::create_dir_all(parent)?;
        }
        std::fs::copy(&resolved, output)?;
    }
    Ok(())
}
fn list_files(root: &Path, base: &Path, files: &mut Vec<serde_json::Value>) -> Result<()> {
    for entry in std::fs::read_dir(root)? {
        let path = entry?.path();
        if path.is_dir() {
            list_files(&path, base, files)?;
        } else {
            files.push(json!({"path": path.strip_prefix(base)?.to_string_lossy(), "bytes": path.metadata()?.len()}));
        }
    }
    files.sort_by(|left, right| left["path"].as_str().cmp(&right["path"].as_str()));
    Ok(())
}

#[cfg(all(test, unix))]
mod tests {
    use super::*;
    use std::os::unix::fs::symlink;
    use std::sync::atomic::{AtomicU64, Ordering};
    static NEXT: AtomicU64 = AtomicU64::new(0);
    struct Root(std::path::PathBuf);
    impl Drop for Root {
        fn drop(&mut self) {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
    #[test]
    fn cpython_payload_rejects_symlink_escape_and_directory_cycle() {
        let root = Root(std::env::temp_dir().join(format!(
            "ryupy-cpython-links-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        )));
        let input = root.0.join("input");
        let output = root.0.join("output");
        std::fs::create_dir_all(&input).unwrap();
        std::fs::write(root.0.join("outside"), "secret").unwrap();
        symlink(root.0.join("outside"), input.join("escape")).unwrap();
        assert!(
            copy_authorized(&input, &output, &input)
                .unwrap_err()
                .to_string()
                .contains("越过授权源根")
        );
        std::fs::remove_file(input.join("escape")).unwrap();
        symlink(&input, input.join("cycle")).unwrap();
        assert!(
            copy_authorized(&input, &output, &input)
                .unwrap_err()
                .to_string()
                .contains("目录链接环")
        );
        std::fs::remove_file(input.join("cycle")).unwrap();
        std::fs::write(input.join("original"), "content").unwrap();
        symlink(input.join("original"), input.join("alias")).unwrap();
        copy_authorized(&input, &output, &input).unwrap();
        assert_eq!(std::fs::read(output.join("alias")).unwrap(), b"content");
        assert!(
            !std::fs::symlink_metadata(output.join("alias"))
                .unwrap()
                .file_type()
                .is_symlink()
        );
    }
}
