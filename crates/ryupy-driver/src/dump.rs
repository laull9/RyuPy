//! 单模块阶段转储；入口角色与普通构建共用环境计划，不另抄选择逻辑。
use super::*;

/// 运行到指定阶段并返回该阶段 IR 的文本。
///
/// `dump` 只处理入口模块本身（不做模块图搜索）：查看单个文件的 HIR / MIR 时，
/// 其他模块的内容只会以 `Import` 节点出现。
pub fn dump(sess: &mut Session, input: &Path, stage: Stage) -> Result<String> {
    accumulator_options::validate(sess)?;
    sess.module_environment_report.clear();
    let module = modules::load_single(sess, input)?;
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
        let compiled = trace_product::compile(sess, &hir, &types, &plan, &stage_trace::StageTrace::new(false))?;
        (compiled.program, compiled.exports, compiled.trace_proof)
    } else {
        let (mut mir, exports, diags) = ryupy_mir::build_program(&hir, &types);
        sess.diagnostics.extend(diags);
        stop_if_errors(sess)?;
        ryupy_mir::passes::run_pipeline(&mut mir, &accumulator_options::pipeline(sess, &plan)?)
            .map_err(|e| anyhow::anyhow!(e))?;
        (mir, exports, None)
    };
    if stage == Stage::Mir {
        return Ok(ryupy_mir::pretty::dump(&mir));
    }

    let linked = trace_product::link(
        sess, module.parsed.file,
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
                file_backed_by_global: false,
                is_package: false,
                exports,
            },
        )],
        0, proof.into_iter().collect(),
    )?;
    check_linked_exports(sess, &linked, module.parsed.file)?;
    let emit_opts = ryupy_codegen_cil::EmitOptions {
        assembly_name: assembly_name(input),
        entry_source_path: Some(input.to_string_lossy().into_owned()),
    };
    Ok(ryupy_codegen_cil::dump_cil(&linked, &emit_opts)?)
}
