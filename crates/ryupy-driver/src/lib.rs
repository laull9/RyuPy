//! 编译流水线编排。
//!
//! driver 是唯一「知道所有阶段」的 crate：负责读文件、按顺序调用各阶段、在阶段边界检查诊断、
//! 写出产物、调用 `dotnet`。各阶段 crate 之间互不调用对方的入口函数。
//!
//! ```text
//! .py ─parse→ AST ─lower→ HIR ─check→ HIR+Types ─build→ MIR ─passes→ MIR ─codegen→ .dll
//! ```

pub mod dotnet;
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
    /// 产物目录，默认 `target/ryupy/<stem>/`。
    pub out_dir: PathBuf,
    pub aot: bool,
}

/// 构建产物（布局见 docs/architecture.md「产物布局」）。
#[derive(Debug, Clone)]
pub struct Artifacts {
    pub dll: PathBuf,
    pub native_exe: Option<PathBuf>,
}

/// 运行前端 + 中端，返回 MIR。任何阶段出错都在阶段边界停止。
/// traceback 里显示的文件名。
///
/// 用**文件名**而不是完整路径：快照与差分测试要求产物与机器的绝对路径无关，
/// 而 traceback 的 `File "..."` 只需要能让用户定位到源文件。
fn traceback_file_name(sess: &Session, file: FileId) -> String {
    Path::new(&sess.sources.get(file).name)
        .file_name()
        .map(|n| n.to_string_lossy().into_owned())
        .unwrap_or_else(|| sess.sources.get(file).name.clone())
}

fn compile_to_mir(sess: &mut Session, file: FileId) -> Result<ryupy_mir::Program> {
    let src = sess.sources.get(file).text.clone();

    let (parsed, diags) = ryupy_syntax::parse(file, &src);
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;

    let source_name = traceback_file_name(sess, file);
    let (hir, diags) = ryupy_hir::lower_module(&parsed, &source_name, &src);
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;

    let (types, diags) = ryupy_ty::check_module(&hir);
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;

    let (mut mir, diags) = ryupy_mir::build_program(&hir, &types);
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;
    ryupy_mir::passes::run_pipeline(&mut mir, &ryupy_mir::passes::default_pipeline());
    if let Err(errs) = ryupy_mir::validate::validate(&mir) {
        bail!("内部错误：MIR 验证失败\n{}", errs.join("\n"));
    }
    Ok(mir)
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
    let file = sess.load_file(&opts.input)?;
    let mir = compile_to_mir(sess, file)?;

    let name = assembly_name(&opts.input);
    let emit_opts = ryupy_codegen_cil::EmitOptions {
        assembly_name: name.clone(),
    };
    let bytes = ryupy_codegen_cil::emit_assembly(&mir, &emit_opts)?;

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
pub fn dump(sess: &mut Session, input: &Path, stage: Stage) -> Result<String> {
    let file = sess.load_file(input)?;
    let src = sess.sources.get(file).text.clone();

    let (parsed, diags) = ryupy_syntax::parse(file, &src);
    sess.diagnostics.extend(diags);
    if stage == Stage::Ast {
        return Ok(format!("{:#?}", parsed.module));
    }
    stop_if_errors(sess)?;

    let source_name = traceback_file_name(sess, file);
    let (hir, diags) = ryupy_hir::lower_module(&parsed, &source_name, &src);
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

    let (mut mir, diags) = ryupy_mir::build_program(&hir, &types);
    sess.diagnostics.extend(diags);
    stop_if_errors(sess)?;
    ryupy_mir::passes::run_pipeline(&mut mir, &ryupy_mir::passes::default_pipeline());
    if stage == Stage::Mir {
        return Ok(ryupy_mir::pretty::dump(&mir));
    }

    let emit_opts = ryupy_codegen_cil::EmitOptions {
        assembly_name: assembly_name(input),
    };
    Ok(ryupy_codegen_cil::dump_cil(&mir, &emit_opts)?)
}
