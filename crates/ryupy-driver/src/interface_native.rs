//! 静态读取Mach-O/ELF/PE本地导出，真实PyInit符号区分扩展与普通附属库。
//! 支持目标架构fat Mach-O；PE转发导出由object的本地exports接口排除，不加载库或执行探针。
use anyhow::{Context, Result, ensure};
use object::{
    Architecture, BinaryFormat, Object,
    read::macho::{FatArch, MachOFatFile32, MachOFatFile64},
};
use serde_json::{Value, json};
use std::path::Path;

pub(super) fn module_name(path: &Path, suffixes: &[String]) -> Option<String> {
    let file = path.file_name()?.to_str()?;
    let stem = suffixes
        .iter()
        .find_map(|suffix| file.strip_suffix(suffix))?;
    let mut parts: Vec<_> = path
        .parent()?
        .components()
        .map(|c| c.as_os_str().to_str())
        .collect::<Option<_>>()?;
    if stem != "__init__" {
        parts.push(stem);
    }
    if parts.is_empty()
        || parts.iter().any(|part| {
            part.is_empty()
                || !part.chars().all(|c| c == '_' || c.is_alphanumeric())
                || part.starts_with(|c: char| c.is_ascii_digit())
        })
    {
        return None;
    }
    Some(parts.join("."))
}
pub(super) fn symbol_matches(macho: bool, observed: &[u8], expected: &[u8]) -> bool {
    let name = if macho {
        observed.strip_prefix(b"_").unwrap_or(observed)
    } else {
        observed
    };
    name == expected
}
fn target(rid: &str) -> Result<(Architecture, BinaryFormat)> {
    Ok(match rid {
        "osx-arm64" => (Architecture::Aarch64, BinaryFormat::MachO),
        "linux-x64" => (Architecture::X86_64, BinaryFormat::Elf),
        "win-x64" => (Architecture::X86_64, BinaryFormat::Pe),
        _ => anyhow::bail!("接口目录未知RID：{rid}"),
    })
}
fn image(data: &[u8], architecture: Architecture) -> Result<&[u8]> {
    match object::FileKind::parse(data)? {
        object::FileKind::MachOFat32 => MachOFatFile32::parse(data)?
            .arches()
            .iter()
            .find(|a| a.architecture() == architecture)
            .context("fat映像缺目标架构")?
            .data(data)
            .map_err(Into::into),
        object::FileKind::MachOFat64 => MachOFatFile64::parse(data)?
            .arches()
            .iter()
            .find(|a| a.architecture() == architecture)
            .context("fat映像缺目标架构")?
            .data(data)
            .map_err(Into::into),
        _ => Ok(data),
    }
}
fn inspect_inner(path: &Path, module: Option<&str>, rid: &str) -> Result<Value> {
    ensure!(
        std::fs::metadata(path)?.len() <= 512 * 1024 * 1024,
        "原生接口审计超过512MiB预算"
    );
    let bytes = std::fs::read(path)?;
    let (architecture, format) = target(rid)?;
    let file = object::File::parse(image(&bytes, architecture)?)?;
    ensure!(
        file.architecture() == architecture && file.format() == format,
        "原生映像架构/格式与目标RID不符"
    );
    let exports = file.exports().context("读取本地导出表失败")?;
    let macho = format == BinaryFormat::MachO;
    let initializers: Vec<_> = exports
        .iter()
        .filter_map(|e| {
            let name = if macho {
                e.name().strip_prefix(b"_").unwrap_or(e.name())
            } else {
                e.name()
            };
            std::str::from_utf8(name)
                .ok()
                .filter(|name| name.starts_with("PyInit_") || name.starts_with("PyInitU_"))
                .map(str::to_owned)
        })
        .collect();
    let ascii = module.is_some_and(|name| name.is_ascii());
    let expected = module
        .filter(|_| ascii)
        .map(|name| format!("PyInit_{}", name.rsplit('.').next().unwrap_or(name)));
    let verified = expected.as_ref().is_some_and(|expected| {
        exports
            .iter()
            .any(|e| e.address() != 0 && symbol_matches(macho, e.name(), expected.as_bytes()))
    });
    Ok(
        json!({"status":"audited","kind":if verified {"cpython_extension"}else{"native_dependency"},
        "module":module,"format":format!("{format:?}"),"architecture":format!("{architecture:?}"),
        "local_export_count":exports.len(),"observed_initializers":initializers,
        "expected_initializer":expected,"initializer_verified":verified,
        "export_scope":"local_defined_exports_excluding_pe_forwarders",
        "naming_status":if module.is_some() && !ascii {"non_ascii_requires_PyInitU_punycode_audit"}else if module.is_none(){"no_importable_name_from_target_suffix"}else{"ascii_name"},
        "abi_validation":"target_image_and_initializer_only","method_signatures_verified":false,
        "machine_abi":{"status":"not_derived","evidence":"CPython初始化入口已审计；业务方法还需stub/header/动态测试等后续证据"}}),
    )
}
pub(super) fn inspect(path: &Path, module: Option<&str>, rid: &str) -> Value {
    match inspect_inner(path, module, rid) {
        Ok(value) => value,
        Err(error) => json!({"status":"audit_error","kind":"native_unresolved","module":module,
            "initializer_verified":false,"error":format!("{error:#}"),"abi_validation":"incomplete"}),
    }
}
