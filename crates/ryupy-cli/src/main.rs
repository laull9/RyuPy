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
    /// 实验：将受守卫循环累加展开成直接 CIL（不保证更快；默认使用 holder）
    #[arg(long, global = true)]
    experimental_direct_accumulator: bool,
    /// 实验：受守卫纯叶调用内联（默认关闭，不保证更快）
    #[arg(long, global = true)]
    experimental_leaf_inline: bool,
    /// 诊断：保SLP守卫/物化，以标量运算作为packed SIMD的公平控制组
    #[arg(long, global = true, conflicts_with = "experimental_packed_slp")]
    diagnostic_scalar_slp: bool,
    /// 实验：真正packed两lane SLP（相对同守卫标量未获净收益，默认关闭）
    #[arg(long, global = true, conflicts_with = "diagnostic_scalar_slp")]
    experimental_packed_slp: bool,
    #[command(subcommand)]
    command: Command,
}

#[derive(Subcommand)]
enum Command {
    /// 编译为 .dll（加 --aot 生成原生可执行文件）
    Build {
        input: PathBuf,
        /// 产物目录，默认 <源文件目录>/__pycache__/<文件名>/
        #[arg(short, long)]
        out_dir: Option<PathBuf>,
        #[arg(long)]
        aot: bool,
        /// 显式资源清单（仅 AOT；目标相对路径首切限 ASCII）
        #[arg(long, requires = "aot")]
        resources: Option<PathBuf>,
        /// 额外的模块搜索根（可重复；入口文件所在目录始终最优先）
        #[arg(short = 'I', long = "search-path")]
        search_paths: Vec<PathBuf>,
    },
    /// 编译并立即用 dotnet 运行
    Run {
        input: PathBuf,
        /// 额外的模块搜索根（可重复；入口文件所在目录始终最优先）
        #[arg(short = 'I', long = "search-path")]
        search_paths: Vec<PathBuf>,
    },
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
    let dir = input.parent().unwrap_or_else(|| std::path::Path::new("."));
    let stem = input.file_stem().unwrap_or_default();
    // 与 CPython 存放 `foo.cpython-312.pyc` 的位置对齐：都收进源文件旁的 `__pycache__`，
    // 避免编译产物散落在源码目录里。每个入口单独一个子目录，防止不同入口互相覆盖。
    dir.join("__pycache__").join(stem)
}

fn main() -> ExitCode {
    let cli = Cli::parse();
    let mut sess = Session {
        experimental_direct_accumulator: cli.experimental_direct_accumulator,
        experimental_leaf_inline: cli.experimental_leaf_inline,
        diagnostic_scalar_slp: cli.diagnostic_scalar_slp,
        experimental_packed_slp: cli.experimental_packed_slp,
        ..Session::default()
    };
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
            resources,
            search_paths,
        } => {
            let out_dir = out_dir.unwrap_or_else(|| default_out_dir(&input));
            let artifacts = ryupy_driver::build_with_resources(
                sess,
                &BuildOptions {
                    input,
                    out_dir,
                    aot,
                    search_paths,
                },
                resources.as_deref(),
            )?;
            eprintln!("已生成 {}", artifacts.dll.display());
            if let Some(exe) = artifacts.native_exe {
                eprintln!("已生成 {}", exe.display());
            }
            if let Some(manifest) = artifacts.manifest {
                eprintln!("AOT 清单：{}", manifest.display());
            }
            Ok(ExitCode::SUCCESS)
        }
        Command::Run {
            input,
            search_paths,
        } => {
            let out_dir = default_out_dir(&input);
            let artifacts = ryupy_driver::build(
                sess,
                &BuildOptions {
                    input,
                    out_dir,
                    aot: false,
                    search_paths,
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

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn direct_accumulator_is_explicit_for_every_command() {
        for command in ["build", "run", "dump"] {
            let default = Cli::try_parse_from(["ryupy", command, "input.py"]).unwrap();
            assert!(!default.experimental_direct_accumulator);
            for arguments in [
                vec![
                    "ryupy",
                    "--experimental-direct-accumulator",
                    command,
                    "input.py",
                ],
                vec![
                    "ryupy",
                    command,
                    "input.py",
                    "--experimental-direct-accumulator",
                ],
            ] {
                let enabled = Cli::try_parse_from(arguments).unwrap();
                assert!(enabled.experimental_direct_accumulator);
            }
        }
    }

    #[test]
    fn direct_accumulator_flag_rejects_unrecognized_values() {
        // 它是明确的布尔开关，不接受可能被误读为其它优化模式的字符串。
        assert!(
            Cli::try_parse_from([
                "ryupy",
                "--experimental-direct-accumulator=unknown",
                "build",
                "input.py",
            ])
            .is_err()
        );
    }
}

#[cfg(test)]
mod leaf_flag_tests {
    use super::*;
    #[test]
    fn leaf_inline_is_explicit_independent_and_composable() {
        for command in ["build", "run", "dump"] {
            let default = Cli::try_parse_from(["ryupy", command, "input.py"]).unwrap();
            assert!(!default.experimental_leaf_inline && !default.experimental_direct_accumulator);
            for arguments in [
                vec!["ryupy", "--experimental-leaf-inline", command, "input.py"],
                vec!["ryupy", command, "input.py", "--experimental-leaf-inline"],
            ] {
                let leaf = Cli::try_parse_from(arguments).unwrap();
                assert!(leaf.experimental_leaf_inline && !leaf.experimental_direct_accumulator);
            }
            let both = Cli::try_parse_from([
                "ryupy",
                "--experimental-direct-accumulator",
                command,
                "input.py",
                "--experimental-leaf-inline",
            ])
            .unwrap();
            assert!(both.experimental_leaf_inline && both.experimental_direct_accumulator);
        }
        assert!(
            Cli::try_parse_from([
                "ryupy",
                "--experimental-leaf-inline=unknown",
                "build",
                "input.py"
            ])
            .is_err()
        );
    }
}

#[cfg(test)]
mod slp_flag_tests {
    use super::*;

    #[test]
    fn scalar_slp_control_is_explicit_and_does_not_enable_other_experiments() {
        for command in ["build", "run", "dump"] {
            let default = Cli::try_parse_from(["ryupy", command, "input.py"]).unwrap();
            assert!(!default.diagnostic_scalar_slp && !default.experimental_packed_slp);
            let selected =
                Cli::try_parse_from(["ryupy", command, "input.py", "--diagnostic-scalar-slp"])
                    .unwrap();
            assert!(selected.diagnostic_scalar_slp);
            assert!(
                !selected.experimental_direct_accumulator && !selected.experimental_leaf_inline
            );
        }
        assert!(
            Cli::try_parse_from([
                "ryupy",
                "--diagnostic-scalar-slp=unknown",
                "build",
                "input.py",
            ])
            .is_err()
        );
    }
}

#[cfg(test)]
mod packed_slp_flag_tests {
    use super::*;
    #[test]
    fn packed_slp_is_explicit_and_exclusive_with_scalar_control() {
        for command in ["build", "run", "dump"] {
            let selected =
                Cli::try_parse_from(["ryupy", command, "input.py", "--experimental-packed-slp"])
                    .unwrap();
            assert!(selected.experimental_packed_slp && !selected.diagnostic_scalar_slp);
            assert!(
                Cli::try_parse_from([
                    "ryupy",
                    command,
                    "input.py",
                    "--experimental-packed-slp",
                    "--diagnostic-scalar-slp",
                ])
                .is_err()
            );
        }
    }
}
