//! 编译流水线编排。
//!
//! driver 是唯一「知道所有阶段」的 crate：负责读文件、按顺序调用各阶段、在阶段边界检查诊断、
//! 写出产物、调用 `dotnet`。各阶段 crate 之间互不调用对方的入口函数。
//!
//! ```text
//! .py ─parse→ AST ─lower→ HIR ─check→ HIR+Types ─build→ MIR ─passes→ MIR ─codegen→ .dll
//! ```

mod accumulator_options;
mod cpython_inventory;
mod cpython_jit;
mod cpython_provider;
mod embedded_runtime;
pub mod onefile;
mod onefile_publication;
mod runtime_assets;
pub use cpython_provider::CPythonProviderState;
pub use onefile_publication::build_onefile;
mod aot;
mod aot_layout;
mod binding_build;
mod binding_sources;
pub mod dotnet;
/// 内嵌运行时及第三方组件的许可证文本，无需准备缓存或.NET环境。
pub fn runtime_licenses() -> &'static str {
    embedded_runtime::licenses()
}
mod dump;
pub use dump::dump;
mod default_aot;
mod linked_compile;
use linked_compile::compile_linked_traced;
mod distribution_metadata;
mod instance_modules;
mod module_environments;
mod module_hints;
mod ordinary_mi;
pub use module_environments::{ModuleEnvironmentDecision, ModuleEnvironmentPolicy};
mod manifest;
mod manifest_files;
mod manifest_sdk;
pub mod modules;
mod native_binding;
mod native_binding_manifest;
pub mod python_selection;
mod resources;
pub mod runtime;
mod stage_trace;
mod trace_product;
pub use trace_product::TraceEntry;

use std::path::{Path, PathBuf};

use anyhow::{Context, Result, bail};
use ryupy_base::FileId;

mod optional_sources;
pub use optional_sources::DefaultSourceState;
mod session;
mod source_catalog;
mod source_probe;
mod source_scan;
pub use source_probe::probe_sources;
mod source_deployment;
pub use session::Session;

/// 编译因用户代码错误而中止（诊断已存入 [`Session`]）。区别于 IO 等内部错误。
/// CLI 通过 `err.is::<HasErrors>()` 判断只需打印诊断、不必再打印错误链。
#[derive(Debug)]
pub struct HasErrors;

impl std::fmt::Display for HasErrors {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.write_str("编译失败，存在错误")
    }
}

impl std::error::Error for HasErrors {}

/// `ryupy dump --emit <stage>` 的阶段。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Stage {
    Ast,
    Hir,
    Types,
    Mir,
    Cil,
}

#[derive(Debug, Clone)]
pub struct BuildOptions {
    pub input: PathBuf,
    /// JIT为产物目录；AOT为 `<stem>.build/.dist` 的共同ROOT（CLI默认源目录）。
    pub out_dir: PathBuf,
    pub aot: bool,
    /// 额外的模块搜索根（等价于 `sys.path` 的其它条目）；入口文件所在目录始终排在最前。
    pub search_paths: Vec<PathBuf>,
}

/// 构建产物（布局见 docs/architecture.md「产物布局」）。
#[derive(Debug, Clone)]
pub struct Artifacts {
    pub dll: PathBuf,
    pub native_exe: Option<PathBuf>,
    /// 仅本次完整 Native AOT 编译成功后返回的可搬迁清单。
    pub manifest: Option<PathBuf>,
}

/// 单模块产物与跨模块绑定契约，一同交给链接器。
struct CompiledModule {
    trace_proof: Option<ryupy_mir::generator_product::Proof>,
    program: ryupy_mir::Program,
    exports: Vec<ryupy_mir::ModuleExport>,
    protected_exports: Vec<String>,
}

/// 运行前端 + 中端，返回 MIR。任何阶段出错都在阶段边界停止。
/// 编译一个模块到 MIR，并在阶段边界检查诊断。
fn compile_module(
    sess: &mut Session,
    module: &modules::SourceModule,
    native_exceptions: &[&str],
    plan: &module_environments::ModuleEnvironmentPlan,
    trace: &stage_trace::StageTrace,
    trace_request: trace_product::Request,
) -> Result<CompiledModule> {
    let info = ryupy_hir::ModuleInfo {
        instance_globals: plan.instance_globals(),
        instance_capabilities: plan.capabilities,
        name: module.name.clone(),
        package: module.package.clone(),
        file: module.file.clone(),
        resolve_file_at_runtime: true,
    };
    trace.mark("begin", "hir", &module.name);
    let (hir, diags) = ryupy_hir::lower_module_with_native_exceptions(
        &module.parsed,
        &info,
        &module.text,
        native_exceptions,
    );
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;
    if plan.automatic && !hir.protected_exports.is_empty() {
        bail!("内部错误：实例计划仍依赖静态异常导出保护");
    }
    trace.mark("end", "hir", &module.name);

    trace.mark("begin", "type-check", &module.name);
    let (types, diags) = ryupy_ty::check_module(&hir);
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;
    trace.mark("end", "type-check", &module.name);

    if let Some(compiled) = trace_product::compile(sess, &hir, &types, plan, trace, trace_request)?
    {
        return Ok(compiled);
    }
    trace.mark("begin", "mir-build", &module.name);
    let (mut mir, exports, diags) = ryupy_mir::build_program(&hir, &types);
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;
    trace.mark("end", "mir-build", &module.name);
    trace.mark("begin", "pipeline", &module.name);
    let passes = accumulator_options::pipeline(sess, plan)?;
    if sess.diagnostic_stages {
        ryupy_mir::passes::run_pipeline_observed(
            &mut mir,
            &passes,
            &mut stage_trace::PassTrace::new(trace, &module.name),
        )
    } else {
        ryupy_mir::passes::run_pipeline(&mut mir, &passes)
    }
    .map_err(|e| anyhow::anyhow!(e))?;
    trace.mark("end", "pipeline", &module.name);
    trace.mark("begin", "final-validate", &module.name);
    if let Err(errs) = ryupy_mir::validate::validate(&mir) {
        bail!("内部错误：MIR 验证失败\n{}", errs.join("\n"));
    }
    trace.mark("end", "final-validate", &module.name);
    trace.mark("result", "trace-legacy", &module.name);
    Ok(CompiledModule {
        trace_proof: None,
        program: mir,
        exports,
        protected_exports: hir.protected_exports,
    })
}

/// 编译整个模块图，返回合并后的程序。
#[cfg(test)]
fn compile_linked(
    sess: &mut Session,
    entry: &Path,
    search_paths: &[PathBuf],
    collect_manifest: bool,
    resource_request: Option<&Path>,
) -> Result<(
    ryupy_mir::LinkedProgram,
    Vec<manifest::SourceSummary>,
    Option<resources::ResourcePlan>,
)> {
    compile_linked_locations(
        sess,
        entry,
        search_paths,
        collect_manifest,
        resource_request,
        collect_manifest,
    )
}
// JIT 也收真实来源摘要，但不能因此把 __file__ 位置误切成 AOT 路径。
fn compile_linked_locations(
    sess: &mut Session,
    entry: &Path,
    search_paths: &[PathBuf],
    collect_manifest: bool,
    resource_request: Option<&Path>,
    aot_locations: bool,
) -> Result<(
    ryupy_mir::LinkedProgram,
    Vec<manifest::SourceSummary>,
    Option<resources::ResourcePlan>,
)> {
    let trace = stage_trace::StageTrace::new(sess.diagnostic_stages);
    compile_linked_traced(
        sess,
        entry,
        search_paths,
        collect_manifest,
        resource_request,
        aot_locations,
        &trace,
    )
}

/// 链接后的导出 guard 也必须验证，避免错误字段 ID 或栈类型进入 codegen。
fn check_linked_exports(
    sess: &mut Session,
    linked: &ryupy_mir::LinkedProgram,
    file: FileId,
) -> Result<()> {
    if let Err(errors) = ryupy_mir::source_provenance::require_linked(linked) {
        for error in errors {
            sess.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(
                        file,
                        ryupy_base::TextRange::empty(ryupy_base::TextSize::new(0)),
                    ),
                    format!("内部 MIR 导出契约验证失败：{error}"),
                )
                .with_code("E0399"),
            );
        }
    }
    stop_if_errors(sess)
}

fn stop_if_errors(sess: &Session) -> Result<()> {
    if sess.diagnostics.has_errors() {
        return Err(HasErrors.into());
    }
    Ok(())
}

fn assembly_name(input: &Path) -> String {
    input
        .file_stem()
        .map(|s| s.to_string_lossy().into_owned())
        .unwrap_or_else(|| "main".to_owned())
}

/// 编译一个 `.py` 文件为 `.dll`（可选再做 Native AOT）。
pub fn build(sess: &mut Session, opts: &BuildOptions) -> Result<Artifacts> {
    build_with_resources(sess, opts, None)
}

/// 资源计划与原生绑定共同进入构建及发布流程，声明错误在输出写入前拒绝。
pub fn build_configured(
    sess: &mut Session,
    opts: &BuildOptions,
    resources: Option<&Path>,
    binding: Option<&Path>,
) -> Result<Artifacts> {
    accumulator_options::validate(sess)?;
    anyhow::ensure!(
        resources.is_none() || opts.aot,
        "--resources 仅可与 --aot 同用"
    );
    if let Some(path) = binding {
        return if opts.aot {
            aot_layout::build(opts, |inner, owned| {
                binding_build::build_in_attempt(sess, inner, path, resources, owned)
            })
        } else {
            binding_build::build(sess, opts, path)
        };
    }
    build_with_resources(sess, opts, resources)
}

/// 显式资源仅用于本次 AOT 编译；旧 build/低层 DLL 发布不伪造资源来源。
pub fn build_with_resources(
    sess: &mut Session,
    opts: &BuildOptions,
    resource_request: Option<&Path>,
) -> Result<Artifacts> {
    accumulator_options::validate(sess)?;
    anyhow::ensure!(
        resource_request.is_none() || opts.aot,
        "--resources 仅可与 --aot 同用"
    );
    if opts.aot {
        return aot_layout::build(opts, |inner, owned| {
            build_with_resources_inner(sess, inner, resource_request, Some(owned))
        });
    }
    if sess.cpython_provider.is_some() {
        return cpython_jit::build(opts, |inner| {
            build_with_resources_inner(sess, inner, resource_request, None)
        });
    }
    build_with_resources_inner(sess, opts, resource_request, None)
}

fn build_with_resources_inner(
    sess: &mut Session,
    opts: &BuildOptions,
    resource_request: Option<&Path>,
    owned: Option<&aot_layout::OwnedAttempt>,
) -> Result<Artifacts> {
    let trace = stage_trace::StageTrace::new(sess.diagnostic_stages);
    aot::validate_before_build(&opts.input, opts.aot)?;
    cpython_provider::prepare(sess, opts)?;
    // 每次请求显式重置，不从前次成功/失败会话继承默认AOT权限。
    sess.default_source_state.aot_request =
        default_aot::eligible(sess, opts.aot, resource_request.is_some());
    let previous_output = sess.default_source_state.output.take();
    let previous_report = sess
        .default_source_state
        .report_output
        .replace(opts.out_dir.clone());
    if optional_sources::enabled(sess, opts.aot) {
        sess.default_source_state.output = Some(opts.out_dir.clone());
    }
    let compiled = compile_linked_traced(
        sess,
        &opts.input,
        &opts.search_paths,
        opts.aot,
        resource_request,
        opts.aot,
        &trace,
    );
    sess.default_source_state.output = previous_output;
    sess.default_source_state.report_output = previous_report;
    sess.default_source_state.aot_request = false;
    let (linked, sources, resource_plan) = compiled?;

    let name = assembly_name(&opts.input);
    let emit_opts = ryupy_codegen_cil::EmitOptions {
        assembly_name: name.clone(),
        entry_source_path: Some(opts.input.to_string_lossy().into_owned()),
    };
    trace.mark("begin", "emit", "-");
    let (bytes, context) = if opts.aot {
        let emitted = ryupy_codegen_cil::emit_source_assembly_with_report(&linked, &emit_opts)?;
        let context = manifest::ManifestContext::new(
            emitted.report,
            sources,
            linked.entry.into_raw().into_u32(),
            &emitted.bytes,
            sess,
        )?;
        (emitted.bytes, Some(context))
    } else {
        let bytes = ryupy_codegen_cil::emit_source_assembly(&linked, &emit_opts)?;
        (bytes, None)
    };

    trace.mark("end", "emit", "-");
    trace.mark("begin", "output-write", "-");
    std::fs::create_dir_all(&opts.out_dir)?;
    let dll = opts.out_dir.join(format!("{name}.dll"));
    std::fs::write(&dll, bytes)?;
    std::fs::write(
        opts.out_dir.join(format!("{name}.runtimeconfig.json")),
        ryupy_codegen_cil::runtimeconfig::render(),
    )?;
    if sess.diagnostic_stages
        && let Ok(report) = sess.module_environment_report_json()
    {
        // 审计只是观察：同名目录、权限或写盘失败不能改变原编译及runtime复制结果。
        // 普通JIT交付没有manifest，原stderr阶段协议也不插入新记录。
        let _ = std::fs::write(opts.out_dir.join("module-environment.json"), report);
    }
    distribution_metadata::write_report(sess, &opts.out_dir)?;
    optional_sources::write_report(sess, &opts.out_dir)?;
    cpython_provider::copy_runtime_to(sess, &opts.out_dir)?;
    cpython_provider::stage(sess, &opts.out_dir)?;
    trace.mark("end", "output-write", "-");

    let (native_exe, manifest) = if let Some(context) = context {
        let published = aot::publish_with_manifest(
            &dll,
            &opts.out_dir,
            &context,
            resource_plan.as_ref(),
            owned,
        )?;
        (Some(published.executable), Some(published.manifest))
    } else {
        (None, None)
    };
    Ok(Artifacts {
        dll,
        native_exe,
        manifest,
    })
}

#[cfg(test)]
mod fixed_binding_tests;

#[cfg(test)]
mod instance_aot_evidence_tests;
#[cfg(test)]
mod instance_aot_tests;
#[cfg(test)]
mod instance_class_aot_tests;
#[cfg(test)]
mod instance_module_tests;

#[cfg(test)]
mod ordinary_dynamic_tests;

#[cfg(test)]
mod foreach_accumulator_tests;

#[cfg(test)]
mod foreach_fixture_tests;

#[cfg(test)]
mod foreach_current_tests;

#[cfg(test)]
mod explicit_super_fixture_tests;

#[cfg(test)]
mod generator_dynamic_fixture_tests;

#[cfg(test)]
mod generator_product_tests;

#[cfg(test)]
mod generator_default_tests;

#[cfg(test)]
mod default_source_tests;

#[cfg(test)]
mod test_temp;
