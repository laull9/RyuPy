//! 编译流水线编排。
//!
//! driver 是唯一「知道所有阶段」的 crate：负责读文件、按顺序调用各阶段、在阶段边界检查诊断、
//! 写出产物、调用 `dotnet`。各阶段 crate 之间互不调用对方的入口函数。
//!
//! ```text
//! .py ─parse→ AST ─lower→ HIR ─check→ HIR+Types ─build→ MIR ─passes→ MIR ─codegen→ .dll
//! ```

mod accumulator_options;
mod aot;
mod aot_layout;
mod binding_build;
pub mod dotnet;
mod dump;
pub use dump::dump;
mod instance_modules;
mod module_environments;
pub use module_environments::{ModuleEnvironmentDecision, ModuleEnvironmentPolicy};
mod manifest;
mod manifest_files;
mod manifest_sdk;
pub mod modules;
mod native_binding;
mod native_binding_manifest;
mod resources;
pub mod runtime;
mod stage_trace;
mod trace_product;
pub use trace_product::TraceEntry;

use std::path::{Path, PathBuf};

use anyhow::{Context, Result, bail};
use ryupy_base::{Diagnostics, FileId, SourceDb};

/// 一次编译会话。诊断累积在这里，由调用方（CLI / 测试）决定如何打印。
#[derive(Default)]
pub struct Session {
    /// 显式强制有限trace V1；合法域外E0310，内部损坏硬停。
    pub trace_product_v1: bool,
    /// 明确正常Main执行的请求；裸API默认Unknown，不从DLL扩展名推断宿主生命周期。
    pub trace_entry: TraceEntry,
    /// 已在实际依赖图中的源码模块显式启用实例全局，不隐式加入依赖。
    pub instance_modules: Vec<String>,
    /// 编译会话的隔离候选策略；无公开CLI开关，也不改变运行时模式数。
    pub module_environment_policy: ModuleEnvironmentPolicy,
    /// 当前实际源码图的编译计划；compiled区分已完成编译与首错后未执行者。
    pub module_environment_report: Vec<ModuleEnvironmentDecision>,
    /// 固定绑定编排期间禁止自动迁移，结束后恢复；显式能力仍按原边界。
    #[doc(hidden)]
    pub fixed_binding_build: bool,
    /// 显式输出普通构建的编译阶段标记；默认关闭，不改变运行时 ABI。
    pub diagnostic_stages: bool,
    /// 本次编译显式包含的源码模块；只加入依赖图，仍在import时初始化。
    pub include_modules: Vec<String>,
    pub sources: SourceDb,
    pub diagnostics: Diagnostics,
    /// 显式启用直接 CIL 累加实验；默认保留已验证的 holder 路径。
    /// 仅控制本次会话的 MIR 流水线，不从进程环境隐式读取，也不改变语言语义。
    pub experimental_direct_accumulator: bool,
    /// 显式实验：仅在实际自动选择的普通动态实例模块启用严格ForEach holder。
    /// 默认关闭，不提供公共CLI模式，也不影响显式实例模块与旧ForRange累加器。
    pub experimental_foreach_accumulator: bool,
    /// 实验：精确Int64实参的受守卫纯叶内联；guard失败性能未达默认门槛。
    pub experimental_leaf_inline: bool,
    /// 仅诊断：SLP保相同守卫/物化，但使用直接标量运算，隔离packed本身的收益。
    pub diagnostic_scalar_slp: bool,
    /// 实验：两lane packed SLP已实现，但相对同守卫标量无净收益，默认关闭。
    pub experimental_packed_slp: bool,
}

impl Session {
    pub fn load_file(&mut self, path: &Path) -> Result<FileId> {
        let text = std::fs::read_to_string(path)
            .with_context(|| format!("无法读取源文件 {}", path.display()))?;
        Ok(self.sources.add(path.display().to_string(), text))
    }

    /// 把全部诊断渲染成文本。
    pub fn render_diagnostics(&self, color: bool) -> String {
        self.diagnostics
            .iter()
            .map(|d| ryupy_base::diagnostic::render(&self.sources, d, color))
            .collect()
    }
}

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
    trace.mark("end", "trace-legacy", &module.name);
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

fn compile_linked_traced(
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
    sess.module_environment_report.clear();
    let include_modules = sess.include_modules.clone();
    trace.mark("begin", "graph-load", "-");
    let (sources, entry_index) =
        modules::load_program_including(sess, entry, search_paths, &include_modules)?;
    // 加载函数返回不等于无语法诊断：沿用原有逐模块 stop_if_errors 边界。
    trace.mark("end", "graph-load", "-");
    let trace_request = trace_product::graph(
        sess,
        sources[entry_index].parsed.file,
        sources.len(),
        entry_index,
        resource_request.is_some(),
    )?;
    instance_modules::validate_selection(
        sess,
        &sources,
        entry_index,
        aot_locations,
        resource_request.is_some(),
    )?;
    let resource_plan = resource_request
        .map(|path| resources::ResourcePlan::read(path, &sources))
        .transpose()?;
    let validated_aot = instance_modules::validate_aot(
        sess,
        &sources,
        entry_index,
        aot_locations,
        resource_plan.as_ref(),
    )?;
    let entry_file = sources[entry_index].parsed.file;
    let native_exceptions: Vec<&str> = [
        "json",
        "zipfile",
        "optparse",
        "getopt",
        "re",
        "dataclasses",
        "unittest",
        "enum",
        "io",
        "typing",
    ]
    .into_iter()
    .filter(|name| !sources.iter().any(|source| source.name == *name))
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
    for (index, (module, plan)) in sources.into_iter().zip(plans).enumerate() {
        if collect_manifest {
            summaries.push(manifest::SourceSummary::new(u32::try_from(index)?, &module));
        }
        let CompiledModule {
            trace_proof,
            program,
            exports,
            protected_exports,
        } = compile_module(
            sess,
            &module,
            &native_exceptions,
            &plan,
            trace,
            trace_request,
        )?;
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
            validated.validate_location(sess, index, &module, &location)?;
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
    let linked = trace_product::link(sess, entry_file, units, entry_index, trace_proofs)?;
    trace.mark("end", "link", "-");
    trace.mark("begin", "export-check", "-");
    check_linked_exports(sess, &linked, entry_file)?;
    trace.mark("end", "export-check", "-");
    Ok((linked, summaries, resource_plan))
}

/// 链接后的导出 guard 也必须验证，避免错误字段 ID 或栈类型进入 codegen。
fn check_linked_exports(
    sess: &mut Session,
    linked: &ryupy_mir::LinkedProgram,
    file: FileId,
) -> Result<()> {
    if let Err(errors) = ryupy_mir::validate::validate_linked_exports(linked) {
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

/// 首切资源与绑定互斥，在任何输出写入前拒绝组合。
pub fn build_configured(
    sess: &mut Session,
    opts: &BuildOptions,
    resources: Option<&Path>,
    binding: Option<&Path>,
) -> Result<Artifacts> {
    accumulator_options::validate(sess)?;
    anyhow::ensure!(
        resources.is_none() || binding.is_none(),
        "首切不支持 --resources 与 --native-binding 同用"
    );
    if let Some(path) = binding {
        return if opts.aot {
            aot_layout::build(opts, |inner, owned| {
                binding_build::build_in_attempt(sess, inner, path, owned)
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
    let (linked, sources, resource_plan) = compile_linked_traced(
        sess,
        &opts.input,
        &opts.search_paths,
        opts.aot,
        resource_request,
        opts.aot,
        &trace,
    )?;

    let name = assembly_name(&opts.input);
    let emit_opts = ryupy_codegen_cil::EmitOptions {
        assembly_name: name.clone(),
        entry_source_path: Some(opts.input.to_string_lossy().into_owned()),
    };
    trace.mark("begin", "emit", "-");
    let (bytes, context) = if opts.aot {
        let emitted = ryupy_codegen_cil::emit_assembly_with_report(&linked, &emit_opts)?;
        let context = manifest::ManifestContext::new(
            emitted.report,
            sources,
            linked.entry.into_raw().into_u32(),
            &emitted.bytes,
            sess,
        )?;
        (emitted.bytes, Some(context))
    } else {
        (ryupy_codegen_cil::emit_assembly(&linked, &emit_opts)?, None)
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
    runtime::copy_runtime_to(&opts.out_dir)?;
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
