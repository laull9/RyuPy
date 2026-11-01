//! 编译流水线编排。
//!
//! driver 是唯一「知道所有阶段」的 crate：负责读文件、按顺序调用各阶段、在阶段边界检查诊断、
//! 写出产物、调用 `dotnet`。各阶段 crate 之间互不调用对方的入口函数。
//!
//! ```text
//! .py ─parse→ AST ─lower→ HIR ─check→ HIR+Types ─build→ MIR ─passes→ MIR ─codegen→ .dll
//! ```

pub mod dotnet;
pub mod modules;
pub mod runtime;

use std::path::{Path, PathBuf};

use anyhow::{Context, Result, bail};
use ryupy_base::{Diagnostics, FileId, SourceDb};

/// 一次编译会话。诊断累积在这里，由调用方（CLI / 测试）决定如何打印。
#[derive(Default)]
pub struct Session {
    pub sources: SourceDb,
    pub diagnostics: Diagnostics,
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
    /// 产物目录，CLI 默认 `<源目录>/__pycache__/<stem>/`（见 `ryupy-cli`）。
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
}

/// 单模块产物与跨模块绑定契约，一同交给链接器。
struct CompiledModule {
    program: ryupy_mir::Program,
    exports: Vec<(String, ryupy_mir::GlobalId)>,
    protected_exports: Vec<String>,
}

/// 运行前端 + 中端，返回 MIR。任何阶段出错都在阶段边界停止。
/// 编译一个模块到 MIR，并在阶段边界检查诊断。
fn compile_module(
    sess: &mut Session,
    module: &modules::SourceModule,
    native_exceptions: &[&str],
) -> Result<CompiledModule> {
    let info = ryupy_hir::ModuleInfo {
        name: module.name.clone(),
        package: module.package.clone(),
        file: module.file.clone(),
    };
    let (hir, diags) = ryupy_hir::lower_module_with_native_exceptions(
        &module.parsed,
        &info,
        &module.text,
        native_exceptions,
    );
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;

    let (types, diags) = ryupy_ty::check_module(&hir);
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;

    let (mut mir, exports, diags) = ryupy_mir::build_program(&hir, &types);
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;
    ryupy_mir::passes::run_pipeline(&mut mir, &ryupy_mir::passes::default_pipeline())
        .map_err(|e| anyhow::anyhow!(e))?;
    if let Err(errs) = ryupy_mir::validate::validate(&mir) {
        bail!("内部错误：MIR 验证失败\n{}", errs.join("\n"));
    }
    Ok(CompiledModule {
        program: mir,
        exports,
        protected_exports: hir.protected_exports,
    })
}

/// 编译整个模块图，返回合并后的程序。
fn compile_linked(
    sess: &mut Session,
    entry: &Path,
    search_paths: &[PathBuf],
) -> Result<ryupy_mir::LinkedProgram> {
    let (sources, entry_index) = modules::load_program(sess, entry, search_paths)?;
    let native_exceptions: Vec<&str> = ["json", "re"]
        .into_iter()
        .filter(|name| !sources.iter().any(|source| source.name == *name))
        .collect();
    let mut units = Vec::new();
    for module in sources {
        let CompiledModule {
            program,
            exports,
            protected_exports,
        } = compile_module(sess, &module, &native_exceptions)?;
        units.push((
            program,
            ryupy_mir::UnitMeta {
                protected_exports,
                name: module.name.clone(),
                package: module.package.clone(),
                file: module.file.clone(),
                exports,
            },
        ));
    }
    Ok(ryupy_mir::link(units, entry_index))
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
    let linked = compile_linked(sess, &opts.input, &opts.search_paths)?;

    let name = assembly_name(&opts.input);
    let emit_opts = ryupy_codegen_cil::EmitOptions {
        assembly_name: name.clone(),
        entry_source_path: Some(opts.input.to_string_lossy().into_owned()),
    };
    let bytes = ryupy_codegen_cil::emit_assembly(&linked, &emit_opts)?;

    std::fs::create_dir_all(&opts.out_dir)?;
    let dll = opts.out_dir.join(format!("{name}.dll"));
    std::fs::write(&dll, bytes)?;
    std::fs::write(
        opts.out_dir.join(format!("{name}.runtimeconfig.json")),
        ryupy_codegen_cil::runtimeconfig::render(),
    )?;
    runtime::copy_runtime_to(&opts.out_dir)?;

    let native_exe = if opts.aot {
        Some(dotnet::publish_aot(&dll, &opts.out_dir)?)
    } else {
        None
    };
    Ok(Artifacts { dll, native_exe })
}

/// 运行到指定阶段并返回该阶段 IR 的文本。
///
/// `dump` 只处理入口模块本身（不做模块图搜索）：查看单个文件的 HIR / MIR 时，
/// 其他模块的内容只会以 `Import` 节点出现。
pub fn dump(sess: &mut Session, input: &Path, stage: Stage) -> Result<String> {
    let module = modules::load_single(sess, input)?;
    if stage == Stage::Ast {
        return Ok(format!("{:#?}", module.parsed.module));
    }
    stop_if_errors(sess)?;

    let info = ryupy_hir::ModuleInfo {
        name: module.name.clone(),
        package: module.package.clone(),
        file: module.file.clone(),
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

    let (mut mir, exports, diags) = ryupy_mir::build_program(&hir, &types);
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;
    ryupy_mir::passes::run_pipeline(&mut mir, &ryupy_mir::passes::default_pipeline())
        .map_err(|e| anyhow::anyhow!(e))?;
    if stage == Stage::Mir {
        return Ok(ryupy_mir::pretty::dump(&mir));
    }

    let linked = ryupy_mir::link(
        vec![(
            mir,
            ryupy_mir::UnitMeta {
                protected_exports: hir.protected_exports,
                name: module.name.clone(),
                package: module.package.clone(),
                file: module.file.clone(),
                exports,
            },
        )],
        0,
    );
    let emit_opts = ryupy_codegen_cil::EmitOptions {
        assembly_name: assembly_name(input),
        entry_source_path: Some(input.to_string_lossy().into_owned()),
    };
    Ok(ryupy_codegen_cil::dump_cil(&linked, &emit_opts)?)
}
