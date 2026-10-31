//! `ryupy` 命令行。只做参数解析与输出，逻辑全部在 `ryupy-driver`。

use std::io::IsTerminal;
use std::path::PathBuf;
use std::process::ExitCode;

use anyhow::Result;
use clap::{Parser, Subcommand, ValueEnum};
use ryupy_driver::{BuildOptions, HasErrors, Session, Stage, dotnet};

#[derive(Parser)]
#[command(name = "ryupy", version, about = "Python → .NET CIL 编译器")]
struct Cli {
    #[command(subcommand)]
    command: Command,
}

#[derive(Subcommand)]
enum Command {
    /// 编译为 .dll（加 --aot 生成原生可执行文件）
    Build {
        input: PathBuf,
        /// 产物目录，默认 target/ryupy/<文件名>/
        #[arg(short, long)]
        out_dir: Option<PathBuf>,
        #[arg(long)]
        aot: bool,
    },
    /// 编译并立即用 dotnet 运行
    Run { input: PathBuf },
    /// 打印某一阶段的中间表示
    Dump {
        input: PathBuf,
        #[arg(long, value_enum, default_value_t = EmitArg::Hir)]
        emit: EmitArg,
    },
}

#[derive(Clone, Copy, ValueEnum)]
enum EmitArg {
    Ast,
    Hir,
    Types,
    Mir,
    Cil,
}

impl From<EmitArg> for Stage {
    fn from(e: EmitArg) -> Self {
        match e {
            EmitArg::Ast => Stage::Ast,
            EmitArg::Hir => Stage::Hir,
            EmitArg::Types => Stage::Types,
            EmitArg::Mir => Stage::Mir,
            EmitArg::Cil => Stage::Cil,
        }
    }
}

fn default_out_dir(input: &std::path::Path) -> PathBuf {
    let stem = input.file_stem().unwrap_or_default();
    PathBuf::from("target/ryupy").join(stem)
}

fn main() -> ExitCode {
    let cli = Cli::parse();
    let mut sess = Session::default();
    let result = run(cli.command, &mut sess);

    if !sess.diagnostics.is_empty() {
        eprint!(
            "{}",
            sess.render_diagnostics(std::io::stderr().is_terminal())
        );
    }
    match result {
        Ok(code) => code,
        Err(e) if e.is::<HasErrors>() => ExitCode::FAILURE,
        Err(e) => {
            eprintln!("error: {e:#}");
            ExitCode::FAILURE
        }
    }
}

fn run(command: Command, sess: &mut Session) -> Result<ExitCode> {
    match command {
        Command::Build {
            input,
            out_dir,
            aot,
        } => {
            let out_dir = out_dir.unwrap_or_else(|| default_out_dir(&input));
            let artifacts = ryupy_driver::build(
                sess,
                &BuildOptions {
                    input,
                    out_dir,
                    aot,
                },
            )?;
            eprintln!("已生成 {}", artifacts.dll.display());
            if let Some(exe) = artifacts.native_exe {
                eprintln!("已生成 {}", exe.display());
            }
            Ok(ExitCode::SUCCESS)
        }
        Command::Run { input } => {
            let out_dir = default_out_dir(&input);
            let artifacts = ryupy_driver::build(
                sess,
                &BuildOptions {
                    input,
                    out_dir,
                    aot: false,
                },
            )?;
            let status = std::process::Command::new(dotnet::dotnet_exe())
                .arg(&artifacts.dll)
                .status()?;
            Ok(if status.success() {
                ExitCode::SUCCESS
            } else {
                ExitCode::FAILURE
            })
        }
        Command::Dump { input, emit } => {
            print!("{}", ryupy_driver::dump(sess, &input, emit.into())?);
            Ok(ExitCode::SUCCESS)
        }
    }
}
