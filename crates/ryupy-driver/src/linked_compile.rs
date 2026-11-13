//! 同一次模块闭图的编译与链接；默认AOT双索引和旧入口在同一流水线中分支。
use super::*;

pub(super) fn compile_linked_traced(
    sess: &mut Session,
    entry: &Path,
    search_paths: &[PathBuf],
    collect_manifest: bool,
    resource_request: Option<resources::ResourceRequestContext<'_>>,
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
        .map(|request| resources::ResourcePlan::read_context(request, &sources))
        .transpose()?;
    let selected = if automatic {
        default_aot::selected(sess, &sources)?
    } else {
        Vec::new()
    };
    if automatic {
        resource_plan = Some(resources::ResourcePlan::compose_automatic(
            &sources,
            resource_plan,
        )?);
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
    let native_exceptions =
        modules::native_exception_modules_for_build(entry, search_paths, &sources);
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
    for (index, (module, plan)) in sources.iter().zip(plans).enumerate() {
        if collect_manifest && !automatic {
            summaries.push(manifest::SourceSummary::new(u32::try_from(index)?, module));
        }
        // 真正已选Instance的编译失败必须硬停，不能借soft候选吞掉AOT证明失败。
        let compile = |sess: &mut Session| {
            compile_module(
                sess,
                module,
                &native_exceptions,
                &plan,
                trace,
                trace_request,
            )
        };
        let compiled = if automatic && selected.contains(&index) {
            Some(compile(sess)?)
        } else {
            optional_sources::compile(sess, module, compile)?
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
        sess.module_environment_report[index].compiled = true;
        let location = if !aot_locations {
            ryupy_mir::ModuleLocation::SourceAbsolute(
                module
                    .resolved_source_path
                    .to_str()
                    .context("模块源码绝对路径必须可表示为 UTF-8")?
                    .to_owned(),
            )
        } else if let Some(relative) = resource_plan
            .as_ref()
            .and_then(|plan| plan.module_location(index as u32))
        {
            ryupy_mir::ModuleLocation::ExecutableRelative(relative.to_owned())
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
                environment_reason: plan.reason.clone(),
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
    trace.mark("end", "export-check", "-");
    Ok((linked, summaries, resource_plan))
}
