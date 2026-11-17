//! 真实部署文件的通用接口目录：CIL来源、CPython源码/扩展、stub声明和普通原生依赖分列。
//! 只解析已选第三方载荷与编译来源；标准库保留库存分类，避免默认全发行AST扫描。
use anyhow::{Context, Result, ensure};
use serde_json::{Value, json};
use std::collections::{BTreeMap, HashSet};
use std::path::Path;
#[path = "interface_native.rs"]
mod native;
#[path = "interface_stub.rs"]
mod stub;

pub(crate) fn analyze(
    plan: &crate::cpython_provider::Plan,
    output: &Path,
    files: &[Value],
) -> Result<Value> {
    let site = crate::cpython_provider::native::site_packages_relpath_for_version(&plan.version)?;
    let suffixes = crate::cpython_provider::native::extension_suffixes_for_version(&plan.version)?;
    let version = ryupy_syntax::ast::PythonVersion::from((
        u8::try_from(plan.version.major)?,
        u8::try_from(plan.version.minor)?,
    ));
    let platform = crate::cpython_provider::native::PlatformKind::current()?;
    let rid = platform.rid();
    // 文件payload以exact目标命中；目录payload沿路径祖先集合命中，成本随目录深度而非wheel文件数增长。
    let payload_targets: HashSet<_> = plan
        .payloads
        .iter()
        .map(|p| std::path::PathBuf::from(&p.destination))
        .collect();
    let compiled_targets: HashSet<_> = plan
        .delegation
        .compiled_sources
        .iter()
        .map(|s| Path::new("python").join(&s.destination))
        .collect();
    let mut entries = Vec::new();
    let mut modules = BTreeMap::<String, Value>::new();
    for file in files {
        let relative = file["path"].as_str().context("接口库存文件缺少path")?;
        let path = Path::new(relative);
        let compiled = compiled_targets.contains(path);
        let delegated =
            path.starts_with("python/ryupy_delegated") || path.starts_with("python/ryupy_entry");
        let third_party = path.strip_prefix(&site).ok().is_some_and(|p| {
            p.ancestors()
                .any(|ancestor| payload_targets.contains(ancestor))
        });
        if !compiled && !delegated && !third_party {
            continue;
        }
        let module_path = if compiled {
            path.strip_prefix("python/ryupy_compiled")?
        } else if delegated {
            path.strip_prefix("python/ryupy_delegated")
                .or_else(|_| path.strip_prefix("python/ryupy_entry"))?
        } else {
            path.strip_prefix(&site)?
        };
        let is_stub = path.extension().is_some_and(|e| e == "pyi");
        let source = path.extension().is_some_and(|e| e == "py");
        let native_candidate = native::is_native_filename(path);
        if !source && !is_stub && !native_candidate {
            continue;
        }
        let accepted = if source {
            vec![".py".into()]
        } else if is_stub {
            vec![".pyi".into()]
        } else {
            suffixes.clone()
        };
        let name = native::module_name(module_path, &accepted);
        let execution = if is_stub {
            "declaration_only"
        } else if compiled {
            "cil"
        } else if source {
            "cpython_source"
        } else {
            "native_unresolved"
        };
        let mut entry = json!({"path":relative,"module":name,"kind":if is_stub{"type_stub"}else if source{"python_source"}else{"native_image"},
            "execution":execution,"origin":if compiled{"compiled_source"}else if delegated{"explicit_delegation"}else{"selected_third_party_payload"},
            "business_body":if is_stub {json!(false)}else{Value::Null},"contains_python_source":source,"contains_native_code":native_candidate,"source_role":if compiled{"diagnostic_source_for_cil"}else if is_stub{"interface_declaration"}else{"provider_implementation"}});
        if source || is_stub {
            entry["interface"] = read_interface(&output.join(path), relative, version, is_stub);
        } else {
            entry["native"] = native::inspect(&output.join(path), name.as_deref(), rid);
            entry["execution"] = entry["native"]["kind"].clone();
        }
        if let Some(name) = name {
            let record=modules.entry(name).or_insert_with(||json!({"implementations":[],"declarations":[],"execution_provider":null,"declared_call_shape_only":true}));
            let key = if is_stub {
                "declarations"
            } else {
                "implementations"
            };
            record[key].as_array_mut().unwrap().push(json!(relative));
            if !is_stub && (source || compiled || entry["native"]["initializer_verified"] == true) {
                if record["execution_provider"].is_null() {
                    record["execution_provider"] = entry["execution"].clone();
                } else if record["execution_provider"] != entry["execution"] {
                    record["execution_provider"] = "multiple_candidates".into();
                }
            }
        }
        entries.push(entry);
    }
    Ok(
        json!({"schema":1,"analysis":"static_deployed_files_and_local_native_exports",
        "python_version":plan.version.version_str(),"rid":rid,"files":entries,"modules":modules,
        "coverage":{"selected_third_party_payloads":true,"compiled_sources":true,"explicit_delegation":true,
            "stdlib":"inventory_only","dynamic_export_sets":"not_resolved","runtime_signature_execution":false,"native_filename_families":["so","so.numeric_version","pyd","dylib","dll"]},
        "abi_authority":{"explicit_c_abi":"existing_v3_declarations_and_static_pinvoke","inference":"Python调用形状由声明AST推导；更多ABI/布局/所有权推导按额外证据与实测扩展"}}),
    )
}
fn read_interface(
    path: &Path,
    display: &str,
    version: ryupy_syntax::ast::PythonVersion,
    is_stub: bool,
) -> Value {
    let read = || -> Result<String> {
        ensure!(
            std::fs::metadata(path)?.len() <= 4 * 1024 * 1024,
            "接口文本超过4MiB预算"
        );
        std::fs::read_to_string(path).context("接口文本不是可读UTF-8")
    };
    match read() {
        Ok(text) => stub::extract(&text, display, version, is_stub),
        Err(error) => {
            json!({"parse_status":"unreadable","diagnostics":[format!("{error:#}")],"declarations":[],"complete":false})
        }
    }
}

#[cfg(test)]
#[path = "interface_catalog_tests.rs"]
mod tests;
