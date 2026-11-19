//! 同一次模块闭图的编译与链接；默认AOT双索引和旧入口在同一流水线中分支。
use super::*;

pub(super) fn compile_linked_traced(
    sess: &mut Session,
    entry: &Path,
    search_paths: &[PathBuf],
    collect_manifest: bool,
    resource_request: Option<&Path>,
    aot_locations: bool,
    trace: &stage_trace::StageTrace,
) -> Result<(
    ryupy_mir::LinkedProgram,
    Vec<manifest::SourceSummary>,
    Option<resources::ResourcePlan>,
)> {
    accumulator_options::validate(sess)?;
    let distribution_payload = distribution_metadata::prepare(sess)?;
    sess.module_environment_report.clear();
    trace.mark("begin", "graph-load", "-");
    let (mut sources, entry_index) =
        modules::load_build_graph(sess, entry, search_paths, !aot_locations)?;
    let roots: Vec<_> = std::iter::once(entry.parent().unwrap_or(Path::new(".")).to_path_buf())
        .chain(search_paths.iter().cloned())
        .collect();
    optional_sources::extend(
        sess,
        entry,
        &roots,
        &mut sources,
        distribution_payload,
        aot_locations,
    )?;
    stock_module_namespaces::validate(sess, &sources)?;
    stock_thin::validate(sess, &sources)?;
    crate::source_probe::write(sess, entry, &roots, &sources)?;
    // 加载函数返回不等于无语法诊断：沿用原有逐模块 stop_if_errors 边界。
    trace.mark("end", "graph-load", "-");
    let automatic = default_aot::needed(sess, &sources);
    // 仅真正产生新默认域时排除旧trace准入；无需求None路径仍传原布尔。
    let trace_request = trace_product::graph(
        sess,
        sources[entry_index].parsed.file,
        sources.len(),
        entry_index,
        resource_request.is_some() || automatic,
    )?;
    instance_modules::validate_selection(
        sess,
        &sources,
        entry_index,
        aot_locations,
        resource_request.is_some(),
        &roots,
    )?;
    let mut resource_plan = resource_request
        .map(|path| resources::ResourcePlan::read(path, &sources))
        .transpose()?;
    let selected = if automatic {
        default_aot::selected(sess, &sources)?
    } else {
        Vec::new()
    };
    if automatic {
        if let Some(plan) = resource_plan.as_mut() {
            plan.add_automatic_locations(&sources)?;
        } else {
            resource_plan = Some(resources::ResourcePlan::automatic(&sources)?);
        }
    }
    let validated_aot = if automatic {
        instance_modules::validate_indices(
            sess,
            &sources,
            entry_index,
            resource_plan.as_ref(),
            &selected,
        )?
    } else {
        instance_modules::validate_aot(
            sess,
            &sources,
            entry_index,
            aot_locations,
            resource_plan.as_ref(),
        )?
    };
    let entry_file = sources[entry_index].parsed.file;
    let native_exceptions: Vec<_> =
        modules::native_exception_modules_for_build(entry, search_paths, &sources)
            .into_iter()
            .filter(|name| !cpython_provider::selected(sess, name))
            .collect();
    let mut units = Vec::new();
    let mut trace_proofs = Vec::new();
    let mut summaries = Vec::new();
    let plans: Vec<_> = sources
        .iter()
        .enumerate()
        .map(|(index, source)| {
            module_environments::plan_validated(
                sess,
                source,
                aot_locations,
                index,
                validated_aot.as_ref(),
            )
        })
        .collect();
    module_environments::record(sess, &sources, &plans);
    let candidate_names = sources
        .iter()
        .filter(|source| source.candidate)
        .map(|source| source.name.clone())
        .collect();
    let mut mapping = vec![None; sources.len()];
    if let Some(provider) = sess.cpython_provider_state.plan.as_mut() {
        provider.delegation.compiled_sources.clear();
    }
    for (index, (module, plan)) in sources.iter().zip(plans).enumerate() {
        if collect_manifest && !automatic {
            summaries.push(manifest::SourceSummary::new(u32::try_from(index)?, module));
        }
        let is_delegated_entry = index == entry_index
            && sess
                .cpython_provider_state
                .plan
                .as_ref()
                .is_some_and(|p| p.delegation.entry_script.is_some());
        // 真正已选Instance的编译失败必须硬停，不能借soft候选吞掉AOT证明失败。
        let compiled = if is_delegated_entry {
            let (stub_parsed, _) = ryupy_syntax::parse(module.parsed.file, "");
            let stub = modules::SourceModule {
                candidate: module.candidate,
                hard: module.hard,
                catalog: None,
                parse_diagnostics: Vec::new(),
                kind: module.kind,
                resource_discovery: None,
                resolved_source_path: module.resolved_source_path.clone(),
                name: module.name.clone(),
                package: module.package.clone(),
                file: module.file.clone(),
                logical_source: module.logical_source.clone(),
                parsed: stub_parsed,
                text: String::new(),
            };
            Some(compile_module(
                sess,
                &stub,
                &native_exceptions,
                &plan,
                trace,
                trace_request,
                sources.len() > 1,
            )?)
        } else {
            let compile = |sess: &mut Session| {
                compile_module(
                    sess,
                    module,
                    &native_exceptions,
                    &plan,
                    trace,
                    trace_request,
                    sources.len() > 1,
                )
            };
            if sess.experimental_stock_module_namespaces || (automatic && selected.contains(&index))
            {
                Some(compile(sess)?)
            } else {
                optional_sources::compile(sess, module, compile)?
            }
        };
        let Some(CompiledModule {
            trace_proof,
            program,
            exports,
            protected_exports,
        }) = compiled
        else {
            continue;
        };
        if aot_locations && plan.instance_globals() && instance_modules::inspect_profile(sess) {
            if let Err(errors) = ryupy_mir::inspect_instance_contract::validate(&program) {
                sess.diagnostics.push(
                    ryupy_base::Diagnostic::error(
                        ryupy_base::Span::new(module.parsed.file, ryupy_base::TextRange::default()),
                        format!("inspect Instance AOT后置证明失败：{}", errors.join("；")),
                    )
                    .with_code("E0100"),
                );
                stop_if_errors(sess)?;
            }
        }
        if aot_locations {
            ordinary_mi::reject_emitted_aot(sess, module, &program);
            stop_if_errors(sess)?;
        }
        mapping[index] = Some(u32::try_from(units.len())?);
        if collect_manifest && automatic {
            summaries.push(manifest::SourceSummary::new(
                u32::try_from(units.len())?,
                module,
            ));
        }
        ryupy_mir::source_provenance::require_program(&program)
            .map_err(|errors| anyhow::anyhow!(errors.join("\n")))?;
        if let Some(proof) = trace_proof {
            trace_proofs.push(proof);
        }
        if is_delegated_entry {
            sess.module_environment_report[index].compiled = false;
            sess.module_environment_report[index].execution = "CPythonDelegatedEntry".into();
            sess.module_environment_report[index].reason = "explicit-cpython-entry".into();
        } else {
            sess.module_environment_report[index].compiled = true;
        }
        let bundled_location = if !is_delegated_entry
            && let Some(provider) = sess.cpython_provider_state.plan.as_mut()
        {
            let relative = bundled_sources::bundled_relative_path(
                provider.delegation.bundle_data.layout_root(),
                module,
            )?;
            provider.delegation.compiled_sources.push(
                cpython_provider::delegation::DelegatedEntry {
                    source: module.resolved_source_path.clone(),
                    destination: relative.clone(),
                },
            );
            Some(ryupy_mir::ModuleLocation::BundleRelative(format!(
                "python/{relative}"
            )))
        } else {
            None
        };
        // 显式资源元数据是源码锚点权威；provider仍保源码部署/审计，但不能覆盖该位置。
        let location = if aot_locations
            && (resource_request.is_some() || bundled_location.is_none())
            && let Some(relative) = resource_plan
                .as_ref()
                .and_then(|plan| plan.module_location(index as u32))
        {
            ryupy_mir::ModuleLocation::ExecutableRelative(relative.to_owned())
        } else if let Some(location) = bundled_location {
            location
        } else if !aot_locations {
            ryupy_mir::ModuleLocation::SourceAbsolute(
                module
                    .resolved_source_path
                    .to_str()
                    .context("模块源码绝对路径必须可表示为 UTF-8")?
                    .to_owned(),
            )
        } else {
            ryupy_mir::ModuleLocation::LegacyDiagnostic
        };
        if let Some(validated) = &validated_aot {
            validated.validate_location(sess, index, module, &location)?;
        }
        units.push((
            program,
            ryupy_mir::UnitMeta {
                instance_capabilities: plan.capabilities,
                execution: plan.execution,
                environment_reason: if is_delegated_entry {
                    "explicit-cpython-entry".into()
                } else {
                    plan.reason.clone()
                },
                protected_exports,
                name: module.name.clone(),
                package: module.package.clone(),
                file: module.file.clone(),
                location,
                file_backed_by_global: true,
                is_package: module.kind == modules::SourceKind::Package,
                exports,
            },
        ));
    }
    trace.mark("begin", "link", "-");
    let emitted_entry = if automatic {
        mapping[entry_index].context("入口不能延期")? as usize
    } else {
        entry_index
    };
    let mut linked = trace_product::link(sess, entry_file, units, emitted_entry, trace_proofs)?;
    linked.native_modules = native_module_requirements::freeze(sess, &sources, true);
    linked.external_provider = sess
        .cpython_provider_state
        .plan
        .as_ref()
        .map(|_| cpython_provider::CONFIG.to_owned());
    linked.distribution_metadata = sess.default_source_state.plan.payload.clone();
    linked.candidate_modules = candidate_names;
    linked.optional_sources = if automatic {
        let plan = resource_plan.as_mut().context("自动位置计划丢失")?;
        let (payload, audit) = default_aot::finish(sess, &sources, &selected, mapping, plan)?;
        plan.automatic = Some(audit);
        sess.default_source_state.plan.generated_payload = Some(payload.clone());
        Some(payload)
    } else {
        sess.default_source_state.plan.runtime_payload()?
    };
    linked.metadata_unavailable = sess.default_source_state.plan.metadata_unavailable.clone();
    trace.mark("end", "link", "-");
    trace.mark("begin", "export-check", "-");
    check_linked_exports(sess, &linked, entry_file)?;
    if sess.experimental_stock_module_namespaces {
        // 实际完成的编译计划随provider产品保留；不靠运行期模块名猜测哪些模块用了权威字典。
        let audit = serde_json::json!({"profile": "experimental-stock-module-namespaces",
            "modules": sess.module_environment_report});
        let provider = sess
            .cpython_provider_state
            .plan
            .as_mut()
            .context("权威模块字典provider丢失")?;
        if provider.dependency_report.is_null() {
            provider.dependency_report = serde_json::json!({});
        }
        provider.dependency_report["module_namespaces"] = audit;
    }
    trace.mark("end", "export-check", "-");
    Ok((linked, summaries, resource_plan))
}
