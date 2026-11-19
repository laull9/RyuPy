//! 单模块阶段转储；入口角色与普通构建共用环境计划，不另抄选择逻辑。
use super::*;

/// 运行到指定阶段并返回该阶段 IR 的文本。
///
/// `dump` 只处理入口模块本身（不做模块图搜索）：查看单个文件的 HIR / MIR 时，
/// 其他模块的内容只会以 `Import` 节点出现。
pub fn dump(sess: &mut Session, input: &Path, stage: Stage) -> Result<String> {
    accumulator_options::validate(sess)?;
    sess.module_environment_report.clear();
    let module =
        if sess.experimental_stock_thin_functions || sess.experimental_stock_module_namespaces {
            // CLI新Session只有配置路径；先按本次输入冻结provider，再核验实际图。
            // 不能用load_single伪造“单源码”，遗漏import/include的额外业务模块。
            cpython_provider::prepare_input(sess, input, &[])?;
            let (mut sources, entry) = modules::load_build_graph(sess, input, &[], false)?;
            stock_thin::validate(sess, &sources)?;
            stock_module_namespaces::validate(sess, &sources)?;
            sources.swap_remove(entry)
        } else {
            modules::load_single(sess, input)?
        };
    let mut invalid_selection = false;
    for name in &sess.instance_modules {
        if name != &module.name {
            invalid_selection = true;
            sess.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(module.parsed.file, ryupy_base::TextRange::default()),
                    format!("实例模块 `{name}` 不在dump的单模块图中"),
                )
                .with_code("E0100"),
            );
        }
    }
    // 仅新增选择错误前置失败；AST仍允许展示带解析诊断的结果。
    if invalid_selection {
        return Err(HasErrors.into());
    }
    if stage == Stage::Ast {
        return Ok(format!("{:#?}", module.parsed.module));
    }
    stop_if_errors(sess)?;

    trace_product::graph(sess, module.parsed.file, 1, 0, false)?;
    let plan = module_environments::plan(sess, &module, false);
    let info = ryupy_hir::ModuleInfo {
        stock_thin_functions: sess.experimental_stock_thin_functions,
        foreign_class_construction: sess.cpython_provider.is_some(),
        introspection_profile: crate::instance_modules::inspect_profile(sess),
        instance_globals: plan.instance_globals(),
        instance_capabilities: plan.capabilities,
        name: module.name.clone(),
        package: module.package.clone(),
        file: module.file.clone(),
        resolve_file_at_runtime: false,
    };
    let native_exceptions = modules::native_exception_modules(input);
    let (hir, diags) = ryupy_hir::lower_module_with_native_exceptions(
        &module.parsed,
        &info,
        &module.text,
        &native_exceptions,
    );
    sess.diagnostics.extend(diags);
    if stage == Stage::Hir {
        return Ok(ryupy_hir::pretty::dump(&hir));
    }
    stop_if_errors(sess)?;

    let (types, diags) = ryupy_ty::check_module(&hir);
    sess.diagnostics.extend(diags);
    if stage == Stage::Types {
        return Ok(types.dump());
    }
    stop_if_errors(sess)?;

    let (mir, exports, proof) = if sess.trace_product_v1 {
        let compiled = trace_product::compile(
            sess,
            &hir,
            &types,
            &plan,
            &stage_trace::StageTrace::new(false),
            trace_product::Request::RequireV1,
        )?
        .ok_or_else(|| anyhow::anyhow!("内部错误：显式dump缺少V1产物"))?;
        (compiled.program, compiled.exports, compiled.trace_proof)
    } else {
        let (mut mir, exports, diags) = ryupy_mir::build_program(&hir, &types);
        sess.diagnostics.extend(diags);
        stop_if_errors(sess)?;
        ryupy_mir::passes::run_pipeline(&mut mir, &accumulator_options::pipeline(sess, &plan)?)
            .map_err(|e| anyhow::anyhow!(e))?;
        (mir, exports, None)
    };
    ryupy_mir::source_provenance::require_program(&mir)
        .map_err(|errors| anyhow::anyhow!(errors.join("\n")))?;
    if stage == Stage::Mir {
        return Ok(ryupy_mir::pretty::dump(&mir));
    }

    let mut linked = trace_product::link(
        sess,
        module.parsed.file,
        vec![(
            mir,
            ryupy_mir::UnitMeta {
                instance_capabilities: plan.capabilities,
                execution: plan.execution,
                environment_reason: plan.reason.clone(),
                protected_exports: hir.protected_exports,
                name: module.name.clone(),
                package: module.package.clone(),
                file: module.file.clone(),
                location: ryupy_mir::ModuleLocation::LegacyDiagnostic,
                file_backed_by_global: true,
                is_package: false,
                exports,
            },
        )],
        0,
        proof.into_iter().collect(),
    )?;
    linked.native_modules =
        native_module_requirements::freeze_parsed(sess, [&module.parsed], false);
    if sess.experimental_stock_thin_functions || sess.experimental_stock_module_namespaces {
        // CIL转储也展示真实候选启动，而非没有provider的不可执行入口替身。
        linked.external_provider = sess
            .cpython_provider_state
            .plan
            .as_ref()
            .map(|_| cpython_provider::CONFIG.to_owned());
    }
    check_linked_exports(sess, &linked, module.parsed.file)?;
    let emit_opts = ryupy_codegen_cil::EmitOptions {
        assembly_name: assembly_name(input),
        entry_source_path: Some(input.to_string_lossy().into_owned()),
    };
    Ok(ryupy_codegen_cil::dump_source_cil(&linked, &emit_opts)?)
}
