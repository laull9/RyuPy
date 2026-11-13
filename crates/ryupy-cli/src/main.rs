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
    /// 显式封闭分发快照请求（JIT/AOT）；不改变sys.path
    #[arg(long, global = true)]
    distribution_metadata: Option<PathBuf>,
    /// 强制有限trace V1：域外整产物编译拒绝；普通build/run无flag自动检查有限域
    #[arg(long, global = true)]
    trace_product_v1: bool,
    /// 显式启用已发现源码模块的真实实例全局；不会自动包含模块（可重复）
    #[arg(long = "instance-module", global = true)]
    instance_modules: Vec<String>,
    /// 显式普通Instance的直接globals冷路；不支持推导式或间接builtin调用
    #[arg(long, global = true)]
    instance_direct_globals: bool,
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
    /// 编译为 .dll；--aot或项目配置的aot模式生成原生可执行文件
    Build {
        #[arg(required_unless_present = "project")]
        input: Option<PathBuf>,
        /// 统一部署配置；与零散部署参数互斥，不隐式合并
        #[arg(long, requires = "target", conflicts_with_all = [
            "input", "aot", "search_paths", "include_modules", "resources",
            "native_binding", "distribution_metadata", "instance_modules",
            "instance_direct_globals", "aot_optimization"
        ])]
        project: Option<PathBuf>,
        /// 配置中要构建的目标；首切目标的mode必须为aot
        #[arg(long, requires = "project")]
        target: Option<String>,
        /// 诊断：逐阶段写 stderr（编译至产物写出，不含 AOT publish；默认关闭）
        #[arg(long, conflicts_with = "native_binding")]
        diagnostic_stages: bool,
        /// 输出根（相对调用目录）；项目默认配置目录/build/<目标>，旧AOT/JIT默认不变
        #[arg(short, long)]
        out_dir: Option<PathBuf>,
        #[arg(long)]
        aot: bool,
        /// NativeAOT优化取舍：default/size/speed；default保留SDK默认
        #[arg(long, requires = "aot", conflicts_with = "project")]
        aot_optimization: Option<ryupy_driver::AotOptimization>,
        /// 显式资源清单（仅 AOT；目标相对路径首切限 ASCII）
        #[arg(long, requires = "aot")]
        resources: Option<PathBuf>,
        /// 显式固定签名 C ABI 声明，首切不能与资源组合
        #[arg(long, conflicts_with = "resources")]
        native_binding: Option<PathBuf>,
        /// 额外的模块搜索根（可重复；入口文件所在目录始终最优先）
        #[arg(short = 'I', long = "search-path")]
        search_paths: Vec<PathBuf>,
        /// 显式包含ASCII点分源码模块（可重复；仍按import惰性初始化，不复制资源）
        #[arg(long = "include-module")]
        include_modules: Vec<String>,
    },
    /// 编译并立即用 dotnet 运行
    Run {
        input: PathBuf,
        /// 诊断：逐阶段写 stderr（仅编译，不含 dotnet 运行；默认关闭）
        #[arg(long)]
        diagnostic_stages: bool,
        /// 额外的模块搜索根（可重复；入口文件所在目录始终最优先）
        #[arg(short = 'I', long = "search-path")]
        search_paths: Vec<PathBuf>,
        /// 显式包含ASCII点分源码模块（可重复；仍按import惰性初始化，不复制资源）
        #[arg(long = "include-module")]
        include_modules: Vec<String>,
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
        distribution_metadata: cli.distribution_metadata,
        trace_product_v1: cli.trace_product_v1,
        instance_modules: cli.instance_modules,
        instance_direct_globals: cli.instance_direct_globals,
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
            project,
            target,
            diagnostic_stages,
            out_dir,
            aot,
            aot_optimization,
            resources,
            native_binding,
            search_paths,
            include_modules,
        } => {
            sess.trace_entry = ryupy_driver::TraceEntry::StandaloneMain;
            sess.diagnostic_stages = diagnostic_stages;
            sess.aot_optimization = aot_optimization.unwrap_or_default();
            // 配置解析、相对路径与部署请求都由driver统一处理；CLI不读JSON。
            let (artifacts, aot) = if let Some(project) = project {
                let target = target
                    .as_deref()
                    .ok_or_else(|| anyhow::anyhow!("项目模式需要--target"))?;
                let request = ryupy_driver::deployment_request::DeploymentRequest::load(
                    &project,
                    target,
                    out_dir.as_deref(),
                )?;
                let aot = request.options().aot;
                (request.build(sess)?, aot)
            } else {
                let input = input.ok_or_else(|| anyhow::anyhow!("缺少输入文件"))?;
                sess.include_modules = include_modules;
                let out_dir = out_dir.unwrap_or_else(|| {
                    if aot {
                        input
                            .parent()
                            .filter(|p| !p.as_os_str().is_empty())
                            .unwrap_or_else(|| std::path::Path::new("."))
                            .to_path_buf()
                    } else {
                        default_out_dir(&input)
                    }
                });
                let artifacts = ryupy_driver::build_configured(
                    sess,
                    &BuildOptions {
                        input,
                        out_dir,
                        aot,
                        search_paths,
                    },
                    resources.as_deref(),
                    native_binding.as_deref(),
                )?;
                (artifacts, aot)
            };
            eprintln!("已生成 {}", artifacts.dll.display());
            if let Some(exe) = artifacts.native_exe {
                eprintln!("已生成 {}", exe.display());
            }
            if let Some(symbols) = artifacts.debug_symbols {
                eprintln!("AOT 调试符号清单：{}", symbols.display());
            }
            if let Some(manifest) = artifacts.manifest {
                eprintln!(
                    "{}：{}",
                    if aot { "AOT 清单" } else { "产物清单" },
                    manifest.display()
                );
            }
            Ok(ExitCode::SUCCESS)
        }
        Command::Run {
            input,
            diagnostic_stages,
            search_paths,
            include_modules,
        } => {
            sess.trace_entry = ryupy_driver::TraceEntry::StandaloneMain;
            sess.diagnostic_stages = diagnostic_stages;
            sess.include_modules = include_modules;
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

#[cfg(test)]
mod include_module_tests {
    use super::*;
    #[test]
    fn include_module_is_repeatable_for_build_and_run() {
        for command in ["build", "run"] {
            let cli = Cli::try_parse_from([
                "ryupy",
                command,
                "main.py",
                "--include-module",
                "pkg.fonts",
                "--include-module",
                "other",
            ])
            .unwrap();
            let names = match cli.command {
                Command::Build {
                    include_modules, ..
                }
                | Command::Run {
                    include_modules, ..
                } => include_modules,
                _ => panic!("build/run expected"),
            };
            assert_eq!(names, ["pkg.fonts", "other"]);
        }
    }
    #[test]
    fn include_does_not_enable_dump_or_jit_resource_copy() {
        assert!(
            Cli::try_parse_from(["ryupy", "dump", "main.py", "--include-module", "pkg"]).is_err()
        );
        assert!(
            Cli::try_parse_from([
                "ryupy",
                "build",
                "main.py",
                "--include-module",
                "pkg",
                "--resources",
                "data.json"
            ])
            .is_err()
        );
    }
}

#[cfg(test)]
mod instance_module_tests {
    use super::*;
    #[test]
    fn instance_module_is_explicit_repeatable_and_separate_from_include() {
        let cli = Cli::try_parse_from([
            "ryupy",
            "run",
            "main.py",
            "--instance-module",
            "a",
            "--instance-module",
            "b",
            "--include-module",
            "c",
        ])
        .unwrap();
        assert_eq!(cli.instance_modules, ["a", "b"]);
        let Command::Run {
            include_modules, ..
        } = cli.command
        else {
            panic!("run");
        };
        assert_eq!(include_modules, ["c"]);
    }
}

#[cfg(test)]
mod project_tests;

mod cli_validation;
