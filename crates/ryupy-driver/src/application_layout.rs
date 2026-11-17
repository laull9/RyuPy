//! 应用发布的唯一依赖根：先在独占候选中规范物理树，再生成指向真实路径的清单。
//! 旧CIL的版本化_internal逻辑ABI由Runtime marker映射；构建输入身份与业务名字保持原值。
use anyhow::{Context, Result, ensure};
use serde_json::{Value, json};
use std::path::{Path, PathBuf};
use std::io::{Read, Write};
pub(crate) const DIRECTORY: &str = "internal";
pub(crate) const MARKER: &str = ".ryupy-layout.json";

pub(crate) fn entry_name(stem: &str) -> String {
    if cfg!(windows) { format!("{stem}.exe") }
    else if stem.eq_ignore_ascii_case(DIRECTORY) { format!("{stem}.bin") }
    else { stem.into() }
}
pub(crate) fn manifest_path(root: &Path) -> Result<PathBuf> {
    let internal = root.join(DIRECTORY);
    Ok(if is_canonical(root)? { internal.join(crate::manifest_files::MANIFEST_NAME) }
    else { root.join(crate::manifest_files::MANIFEST_NAME) })
}
pub(crate) fn is_canonical(root: &Path) -> Result<bool> { Ok(marker(root)?.is_some()) }
#[derive(serde::Deserialize)]
#[serde(deny_unknown_fields)]
struct Marker {
    schema_version: u32, kind: String, payload_mode: String, entry: String, entry_kind: String,
}
fn marker(root: &Path) -> Result<Option<Value>> {
    let path = root.join(DIRECTORY).join(MARKER);
    match std::fs::symlink_metadata(&path) {
        Err(error) if error.kind() == std::io::ErrorKind::NotFound => return Ok(None),
        Err(error) => return Err(error.into()), Ok(_) => {}
    }
    ensure!(crate::manifest_files::plain_metadata(&root.join(DIRECTORY))?.is_dir(), "marker父目录不是普通internal");
    let meta = crate::manifest_files::plain_metadata(&path)?;
    ensure!(meta.is_file() && meta.len() <= 4096, "布局marker不是有界普通文件");
    let mut bytes = Vec::new(); std::fs::File::open(path)?.take(4097).read_to_end(&mut bytes)?;
    ensure!(bytes.len() <= 4096, "布局marker超过预算");
    // struct反序列化同时拒绝未知字段与重复key，不能由Value的后项覆盖伪造五字段证明。
    let record: Marker = serde_json::from_slice(&bytes)?;
    ensure!(record.schema_version == 1 && record.kind == "ryupy-application-layout"
        && record.payload_mode == "canonical-v1"
        && matches!(record.entry_kind.as_str(), "apphost" | "native_aot"), "布局marker字段无效");
    let name = &record.entry;
    crate::manifest_files::safe_relative(name)?;
    ensure!(!name.contains('/') && !name.eq_ignore_ascii_case(DIRECTORY), "布局入口不是独立文件名");
    Ok(Some(serde_json::from_slice(&bytes)?))
}

/// 只有入口留在公开根；名字冲突必须在移动前发现，绝不覆盖payload或已有资源。
pub(crate) fn canonicalize(root: &Path, entry: &Path, kind: &str) -> Result<()> {
    let name = entry.file_name().context("应用入口缺少文件名")?;
    ensure!(entry.parent() == Some(root), "应用入口不在本次发布根");
    let internal = root.join(DIRECTORY);
    crate::manifest_files::ensure_absent(&internal)?;
    let items = std::fs::read_dir(root)?.collect::<std::io::Result<Vec<_>>>()?;
    for item in &items {
        crate::manifest_files::plain_metadata(&item.path())?;
        ensure!(!item.file_name().to_string_lossy().eq_ignore_ascii_case(DIRECTORY),
            "应用payload与保留依赖目录internal冲突，请更换输出或资源目标");
        ensure!(item.file_name() != MARKER, "payload与布局marker保留名冲突");
        if item.file_name() == "_internal" { crate::manifest_files::ensure_absent(&item.path().join(MARKER))?; }
    }
    std::fs::create_dir(&internal)?;
    for item in items {
        if item.file_name() == name { continue; }
        if item.file_name() == "_internal" {
            ensure!(item.file_type()?.is_dir(), "旧版本化依赖根不是目录");
            for child in std::fs::read_dir(item.path())? {
                let child = child?;
                let target = internal.join(child.file_name());
                crate::manifest_files::ensure_absent(&target)?;
                std::fs::rename(child.path(), target)?;
            }
            std::fs::remove_dir(item.path())?;
        } else {
            let target = internal.join(item.file_name());
            crate::manifest_files::ensure_absent(&target)?;
            std::fs::rename(item.path(), target)?;
        }
    }
    let marker = json!({"schema_version":1,"kind":"ryupy-application-layout",
        "payload_mode":"canonical-v1","entry":name.to_string_lossy(),"entry_kind":kind});
    std::fs::OpenOptions::new().write(true).create_new(true).open(internal.join(MARKER))?
        .write_all(&serde_json::to_vec_pretty(&marker)?)?;
    verify_root(root, name.to_str().context("入口名不是UTF8")?)?;
    Ok(())
}
pub(crate) fn verify_root(root: &Path, entry: &str) -> Result<()> {
    let mut names = std::fs::read_dir(root)?.map(|e| Ok(e?.file_name())).collect::<Result<Vec<_>>>()?;
    names.sort();
    let mut expected = vec![std::ffi::OsString::from(entry), DIRECTORY.into()]; expected.sort();
    ensure!(names == expected, "公开产品根只允许入口与internal目录");
    ensure!(crate::manifest_files::plain_metadata(&root.join(entry))?.is_file(), "应用入口不是普通文件");
    ensure!(crate::manifest_files::plain_metadata(&root.join(DIRECTORY))?.is_dir(), "internal不是普通目录");
    Ok(())
}

/// 内部分类仍消费已验证的老逻辑名；返回值只能由真实canonical树的枚举结果产生。
pub(crate) fn logical_path(path: &str) -> String {
    let Some(rest) = path.strip_prefix("internal/") else { return path.into(); };
    if ["resources/", "native/", "interfaces/"].iter().any(|p| rest.starts_with(p)) {
        format!("_internal/{rest}")
    } else { rest.into() }
}
pub(crate) fn physical_path(path: &str, entry: &str) -> String {
    if path == entry { path.into() }
    else { format!("internal/{}", path.strip_prefix("_internal/").unwrap_or(path)) }
}
pub(crate) fn logical_artifacts(items: &mut [crate::manifest_files::FileDigest]) {
    for item in items {
        item.relative_path = logical_path(&item.relative_path);
        if item.relative_path == MARKER { item.role = "application_layout".into(); }
        else if let Some(role) = crate::runtime::dependency_role(&item.relative_path) { item.role = role.into(); }
    }
}

/// 只重定位明确的发布路径字段；build_inputs及源码/符号/声明原名不会被全局字符串替换。
pub(crate) fn manifest_bytes(bytes: &[u8], entry: &str) -> Result<Vec<u8>> {
    let mut value: Value = serde_json::from_slice(bytes)?;
    for (list, field) in [("artifacts", "relative_path"), ("resources", "relative_path"),
        ("module_locations", "relative_file"), ("native_bindings", "relative_file")] {
        if let Some(items) = value[list].as_array_mut() {
            for item in items { map_field(item, field, entry); }
        }
    }
    if let Some(interfaces) = value.get_mut("binding_interfaces") {
        for key in ["search_root", "python_stub", "machine_contract"] { map_field(interfaces, key, entry); }
    }
    value["application_layout"] = json!({"schema_version":1,"dependency_root":DIRECTORY,
        "marker":"internal/.ryupy-layout.json","artifact_paths":"product_root"});
    let mut result = serde_json::to_vec_pretty(&value)?; result.push(b'\n'); Ok(result)
}
fn map_field(value: &mut Value, key: &str, entry: &str) {
    if let Some(path) = value.get(key).and_then(Value::as_str) {
        let mapped = physical_path(path, entry); value[key] = mapped.into();
    }
}

/// artifact清单在internal中落盘，调用者先执行canonicalize和真实树收集。
pub(crate) fn write_manifest(root: &Path, bytes: &[u8]) -> Result<PathBuf> {
    crate::manifest_files::write_atomic(&root.join(DIRECTORY), bytes)
}

pub(crate) fn finish_jit_manifest(root: &Path, apphost: &Path, bytes: &[u8]) -> Result<PathBuf> {
    let entry = apphost.file_name().and_then(|p| p.to_str()).context("JIT入口名无效")?;
    let mapped = manifest_bytes(bytes, entry)?;
    let mut value: Value = serde_json::from_slice(&mapped)?;
    let artifacts = value["artifacts"].as_array_mut().context("绑定清单缺artifacts")?;
    for (path, role) in [(entry.to_owned(), "application_launcher"),
        (format!("internal/{MARKER}"), "application_layout")] {
        let item = crate::manifest_files::digest_file(&root.join(&path), &path, role)?;
        artifacts.push(serde_json::to_value(item)?);
    }
    artifacts.sort_by(|a,b| a["relative_path"].as_str().cmp(&b["relative_path"].as_str()));
    write_manifest(root, &serde_json::to_vec_pretty(&value)?)
}

pub(crate) fn relocate_entry(root: &Path, entry: &str) -> Result<u64> {
    let mut value = marker(root)?.context("canonical产品缺marker")?;
    value["entry"] = entry.into();
    let path = root.join(DIRECTORY).join(MARKER);
    std::fs::write(&path, serde_json::to_vec_pretty(&value)?)?;
    verify_root(root, entry)?;
    Ok(crate::manifest_files::plain_metadata(&path)?.len())
}
