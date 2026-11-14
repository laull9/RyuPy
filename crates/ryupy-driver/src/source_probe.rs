//! 输出与实际源码发现图一致的版本化构建期报告：唯一 provider、导入边与已知缺口。
//! 读取源码并部署已授权catalog；显式provider可运行隔离的静态metadata工具，不执行应用包。
use crate::{BuildOptions, Session, modules::SourceModule, source_catalog, source_scan};
use anyhow::{Context, Result, ensure};
use serde_json::{Value, json};
use std::collections::{BTreeMap, BTreeSet, VecDeque};
use std::path::{Path, PathBuf};

/// 与 build 使用相同策略门和搜索根；调用方可显式选择报告输出目录。
pub fn probe_sources(sess: &mut Session, opts: &BuildOptions) -> Result<String> {
    crate::cpython_provider::prepare(sess, opts)?;
    ensure!(!sess.fixed_binding_build, "固定绑定构建使用自身来源合同");
    // 探测是独立事务，不能抹掉同一 Session 原有的来源计划和权限载体。
    let previous_state = std::mem::take(&mut sess.default_source_state);
    sess.default_source_state.output = Some(opts.out_dir.clone());
    sess.default_source_state.aot_request = crate::default_aot::eligible(sess, opts.aot, false);
    let result = (|| {
        let (mut sources, _) =
            crate::modules::load_build_graph(sess, &opts.input, &opts.search_paths, !opts.aot)?;
        let roots = roots(opts);
        crate::optional_sources::extend(sess, &opts.input, &roots, &mut sources, None, opts.aot)?;
        let report = analyze(sess, &sources, &roots)?;
        save(&opts.out_dir, &opts.input, &report)?;
        serde_json::to_string_pretty(&report).map_err(Into::into)
    })();
    sess.default_source_state = previous_state;
    result
}

fn roots(opts: &BuildOptions) -> Vec<PathBuf> {
    std::iter::once(
        opts.input
            .parent()
            .filter(|path| !path.as_os_str().is_empty())
            .unwrap_or(Path::new("."))
            .to_path_buf(),
    )
    .chain(opts.search_paths.iter().cloned())
    .collect()
}

/// 已加载源码是 provider 权威；同名用户模块绝不借用 catalog 事实或权限。
pub(crate) fn analyze(
    sess: &Session,
    sources: &[SourceModule],
    roots: &[PathBuf],
) -> Result<Value> {
    let facts = source_catalog::facts()?;
    let runtime = runtime_modules();
    let loaded: BTreeMap<_, _> = sources
        .iter()
        .map(|source| (source.name.as_str(), source))
        .collect();
    let mut modules = BTreeMap::<String, Value>::new();
    let mut uncertainties = Vec::new();
    let mut queue: VecDeque<_> = sources.iter().map(|source| source.name.clone()).collect();
    while let Some(name) = queue.pop_front() {
        if modules.contains_key(&name) {
            continue;
        }
        ensure!(modules.len() < 1024, "静态探测超过1024模块预算");
        let top = name.split('.').next().unwrap_or("");
        let detected_user = if !loaded.contains_key(name.as_str()) {
            crate::modules::resolve_in(roots, &name)
        } else {
            None
        };
        let blocked_parent = !loaded.contains_key(name.as_str())
            && detected_user.is_none()
            && name.contains('.')
            && crate::modules::resolve_in(roots, top).is_some();
        let cpython = crate::cpython_provider::selected(sess, &name);
        let mut record = if cpython {
            json!({"provider": "cpython", "status": "provider_selected", "version": "3.12.13", "reason": sess.cpython_provider_state.plan.as_ref().map(|plan| crate::cpython_inventory::reason(plan,&name)).unwrap_or("explicit_dependency_family"), "dependencies": [], "dependency_coverage": "完整基础发行及显式包载荷"})
        } else if let Some(source) = loaded.get(name.as_str()) {
            let catalog = source.catalog.is_some();
            json!({
                "provider": if catalog { "cpython_source" } else { "user_source" },
                "status": "source_selected", "source": source.resolved_source_path,
                "version": if catalog { facts["modules"][&name]["source_version"].clone() } else { Value::Null },
                "catalog": if catalog { facts["modules"][&name].clone() } else { Value::Null },
                "provenance_authorized": catalog,
                "reason": if catalog { "已授权 catalog 的真实来源闭包" } else { "入口目录及 -I 顺序决定的真实用户源码" },
                "registration": if source.candidate { "candidate" } else { "active" },
                "syntax_diagnostics": source.parse_diagnostics.iter().map(|diagnostic| format!("{diagnostic:?}")).collect::<Vec<_>>(),
                "dependencies": [], "imports": {},
            })
        } else if blocked_parent {
            json!({"provider": null, "status": "dependency_missing",
                "reason": "用户顶层模块/包已选定，禁止从其它根或 catalog 补齐子模块", "dependencies": []})
        } else if let Some((path, _)) = &detected_user {
            json!({"provider": null, "status": "source_detected_unselected", "source": path,
                "version": null, "catalog": null, "provenance_authorized": false,
                "reason": "搜索根存在用户源码，但本次真实编译图尚未选入；仅继续静态解析，禁止改选同名 C# 或 catalog",
                "dependencies": []})
        } else if sess.default_source_state.binding_module.as_deref() == Some(name.as_str()) {
            json!({"provider": "c_abi_binding", "status": "declaration_selected",
                "reason": "本次构建已解析的静态 C ABI 声明；追加模块前核验源码冲突",
                "dependencies": [], "dependency_coverage": "原生制品由绑定发布清单记录"})
        } else if let Some(registrations) = runtime.get(&name) {
            json!({"provider": "csharp_runtime", "status": "runtime_registered",
                "source": registrations,
                "version": env!("CARGO_PKG_VERSION"), "target_framework": "net10.0",
                "reason": "随编译器固化的 C# 显式注册事实；API 覆盖须查逐 API 验收",
                "dependencies": [], "dependency_coverage": "C# 实现内部依赖由现有运行时发布合同管理"})
        } else if !facts["modules"][&name].is_null() {
            json!({"provider": null, "status": "catalog_unavailable",
                "catalog": facts["modules"][&name], "version": facts["modules"][&name]["source_version"],
                "reason": if facts["modules"][&name]["admission"] == "detected_only" {
                    "此版本仅登记探测事实，尚未接入编译 catalog；依赖和 API 待验收"
                } else {
                    "本次策略/闭包未授予 catalog 准入；需检查 policy 与默认 AOT 资格门"
                }, "dependencies": []})
        } else {
            json!({"provider": null, "status": "dependency_missing",
                "reason": "显式搜索根、已准入 catalog 和 C# 注册表均无本次 provider", "dependencies": []})
        };
        let scan = if cpython {
            None
        } else if let Some(source) = loaded.get(name.as_str()) {
            Some(source_scan::collect(&source.parsed, &source.package))
        } else if let Some((path, _)) = &detected_user {
            // 未选源码只在局部数据库解析；包括星号导入和未准入 catalog 的用户依赖。
            let bytes = crate::source_deployment::read(path, 4 * 1024 * 1024)?;
            let text = std::str::from_utf8(&bytes)?;
            let package = crate::modules::module_package(&name, path);
            let (scan, diagnostics) = scan_detected(text, &path.display().to_string(), &package);
            record["syntax_diagnostics"] = diagnostics.into();
            Some(scan)
        } else if !blocked_parent && record["status"] == "catalog_unavailable" {
            // 未接入源码也可提供依赖事实；只在局部 SourceDb 解析，绝不创建 Token。
            let bytes = source_catalog::source(&name)?.context("版本事实缺少内嵌源码")?;
            let text = std::str::from_utf8(bytes)?;
            let (scan, diagnostics) = scan_detected(
                text,
                &format!("catalog/{name}"),
                name.rsplit_once('.').map_or("", |pair| pair.0),
            );
            record["syntax_diagnostics"] = diagnostics.into();
            Some(scan)
        } else {
            None
        };
        if let Some(scan) = scan {
            let mut dependencies = BTreeSet::new();
            for (module, attributes) in &scan.imports {
                dependencies.insert(module.clone());
                // from m import value 的 value 通常是属性；只有真实来源证明才增加子模块边。
                for attribute in attributes
                    .iter()
                    .filter(|attribute| attribute.as_str() != "*")
                {
                    let child = format!("{module}.{attribute}");
                    let imported_top = module.split('.').next().unwrap_or("");
                    let non_user_child = crate::modules::resolve_in(roots, imported_top).is_none()
                        && (runtime.contains_key(&child) || !facts["modules"][&child].is_null());
                    if loaded.contains_key(child.as_str())
                        || crate::modules::resolve_in(roots, &child).is_some()
                        || non_user_child
                    {
                        dependencies.insert(child);
                    }
                }
                dependencies.extend(crate::modules::package_prefixes(module));
            }
            if name != "__main__" {
                dependencies.extend(crate::modules::package_prefixes(&name));
            }
            queue.extend(dependencies.iter().cloned());
            record["dependencies"] = serde_json::to_value(&dependencies)?;
            record["imports"] = serde_json::to_value(&scan.imports)?;
            for dynamic in scan.dynamic {
                uncertainties.push(json!({"source_module": name, "evidence": dynamic}));
            }
        }
        modules.insert(name, record);
    }
    // 固定点传播缺口，环内没有缺口时保持闭合；终端未授权项不能被父节点隐藏。
    let mut unavailable: BTreeSet<_> = modules
        .iter()
        .filter(|(_, record)| record["provider"].is_null())
        .map(|(name, _)| name.clone())
        .collect();
    loop {
        let mut changed = false;
        for (name, record) in &modules {
            if record["dependencies"]
                .as_array()
                .into_iter()
                .flatten()
                .filter_map(Value::as_str)
                .any(|dependency| unavailable.contains(dependency))
            {
                changed |= unavailable.insert(name.clone());
            }
        }
        if !changed {
            break;
        }
    }
    for (name, record) in &mut modules {
        record["dependency_closure"] = if unavailable.contains(name) {
            "unavailable"
        } else {
            "closed"
        }
        .into();
        record["missing_dependencies"] = record["dependencies"]
            .as_array()
            .into_iter()
            .flatten()
            .filter(|dependency| {
                dependency
                    .as_str()
                    .is_some_and(|name| unavailable.contains(name))
            })
            .cloned()
            .collect::<Vec<_>>()
            .into();
    }
    Ok(json!({
        "schema_version": 1, "catalog_version": source_catalog::VERSION,
        "python_reference": facts["python_reference"], "complete_stdlib": false,
        "policy": sess.module_environment_policy.identity(), "search_roots": roots,
        "analysis": "AST 全分支保守闭包；无 Python 执行、无宿主 site-packages、无解释器回退",
        "compiler_verified": false, "static_closure_complete": unavailable.is_empty(),
        "dynamic_status": if uncertainties.is_empty() { "no_recognized_dynamic_import" } else { "uncertain" },
        "dynamic_analysis_complete": false,
        "limitations": ["任意间接调用、反射、导入钩子无法用有限静态扫描穷尽", "源码已选仍须通过正常语义编译与 API 验收", "runtime_registered 仅确认模块注册，属性覆盖按 API 独立验收"],
        "modules": modules, "uncertainties": uncertainties,
    }))
}

/// 探测数据库与编译会话分开，不生成权限令牌或活动模块注册。
fn scan_detected(text: &str, path: &str, package: &str) -> (source_scan::Scan, Vec<String>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add(path, text);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, text);
    (
        source_scan::collect(&parsed, package),
        diagnostics
            .iter()
            .map(|diagnostic| format!("{diagnostic:?}"))
            .collect(),
    )
}

/// 从完整 C# 编译源生成的字面注册事实；动态注册仍须另行登记覆盖。
fn runtime_modules() -> BTreeMap<String, Vec<String>> {
    let facts: &[(&str, &str)] = include!(concat!(env!("OUT_DIR"), "/runtime_registry.rs"));
    let mut modules = BTreeMap::<String, Vec<String>>::new();
    for &(name, source) in facts {
        modules.entry(name.into()).or_default().push(source.into());
    }
    modules
}

#[cfg(test)]
#[path = "../runtime_registry_scan.rs"]
mod runtime_registry_scan;

/// 报告是独立审计产物，不进入 runtime payload，也不扩张既有来源权限。
pub(crate) fn write(
    sess: &mut Session,
    entry: &Path,
    roots: &[PathBuf],
    sources: &[SourceModule],
) -> Result<()> {
    if let Some(out) = &sess.default_source_state.report_output {
        let report = analyze(sess, sources, roots)?;
        if sess.default_source_state.binding_catalog {
            sess.default_source_state.pending_report = Some(report);
        } else {
            save(out, entry, &report)?;
        }
    }
    Ok(())
}

pub(crate) fn save(out: &Path, entry: &Path, report: &Value) -> Result<()> {
    std::fs::create_dir_all(out)?;
    let path = out.join(format!("{}.source-plan.json", crate::assembly_name(entry)));
    std::fs::write(path, serde_json::to_vec_pretty(report)?)?;
    Ok(())
}

#[cfg(test)]
#[path = "source_probe_tests.rs"]
mod tests;
