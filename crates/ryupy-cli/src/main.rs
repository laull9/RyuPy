//! `ryupy` 命令行。只做参数解析与输出，逻辑全部在 `ryupy-driver`。

use std::io::IsTerminal;
use std::path::PathBuf;
use std::process::ExitCode;

use anyhow::Result;
use clap::Parser;
mod args;
use args::{Cli, Command};
use ryupy_driver::{BuildOptions, HasErrors, Session, dotnet};

fn default_out_dir(input: &std::path::Path) -> PathBuf {
    let dir = input.parent().unwrap_or_else(|| std::path::Path::new("."));
    let stem = input.file_stem().unwrap_or_default();
    // 与 CPython 存放 `foo.cpython-312.pyc` 的位置对齐：都收进源文件旁的 `__pycache__`，
    // 避免编译产物散落在源码目录里。每个入口单独一个子目录，防止不同入口互相覆盖。
    dir.join("__pycache__").join(stem)
}

fn main() -> ExitCode {
    let mut cli = Cli::parse();
    if cli.help_advanced {
        return match args::print_advanced_help() {
            Ok(()) => ExitCode::SUCCESS,
            Err(error) => {
                eprintln!("error: {error:#}");
                ExitCode::FAILURE
            }
        };
    }
    let command = match cli.take_command() {
        Ok(command) => command,
        Err(error) => {
            eprintln!("error: {error:#}");
            return ExitCode::FAILURE;
        }
    };
    let mut sess = Session {
        distribution_metadata: cli.distribution_metadata,
        cpython_provider: cli.cpython_provider,
        trace_product_v1: cli.trace_product_v1,
        instance_modules: cli.instance_modules,
        instance_direct_globals: cli.instance_direct_globals,
        experimental_direct_accumulator: cli.experimental_direct_accumulator,
        experimental_leaf_inline: cli.experimental_leaf_inline,
        diagnostic_scalar_slp: cli.diagnostic_scalar_slp,
        experimental_packed_slp: cli.experimental_packed_slp,
        ..Session::default()
    };
    let result = run(command, &mut sess);

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
        Command::Probe {
            input,
            out_dir,
            search_paths,
            aot,
        } => {
            let out_dir = out_dir.unwrap_or_else(|| default_out_dir(&input).join("probe"));
            let report = ryupy_driver::probe_sources(
                sess,
                &BuildOptions {
                    input,
                    out_dir,
                    search_paths,
                    aot,
                },
            )?;
            println!("{report}");
            Ok(ExitCode::SUCCESS)
        }
        Command::Build {
            input,
            diagnostic_stages,
            out_dir,
            aot,
            onefile,
            resources,
            native_binding,
            search_paths,
            include_modules,
        } => {
            sess.trace_entry = ryupy_driver::TraceEntry::StandaloneMain;
            sess.diagnostic_stages = diagnostic_stages;
            sess.include_modules = include_modules;
            let aot = aot || onefile;
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
            let build = if onefile {
                ryupy_driver::build_onefile
            } else {
                ryupy_driver::build_configured
            };
            let artifacts = build(
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
            eprintln!("已生成 {}", artifacts.dll.display());
            if let Some(exe) = artifacts.native_exe {
                eprintln!("已生成 {}", exe.display());
            }
            if let Some(manifest) = artifacts.manifest {
                eprintln!(
                    "{}：{}",
                    if onefile {
                        "内嵌 standalone 清单"
                    } else if aot {
                        "AOT 清单"
                    } else {
                        "产物清单"
                    },
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
    fn resources_and_binding_compose_only_with_aot() {
        let args = [
            "ryupy",
            "build",
            "app.py",
            "--resources",
            "resources.json",
            "--native-binding",
            "binding.json",
        ];
        assert!(Cli::try_parse_from(args).is_err());
        assert!(Cli::try_parse_from(args.into_iter().chain(["--aot"])).is_ok());
        assert!(Cli::try_parse_from(args.into_iter().chain(["--onefile"])).is_ok());
        assert!(Cli::try_parse_from(args.into_iter().chain(["--aot", "--onefile"])).is_ok());
    }
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
            let names = match cli.command.unwrap() {
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
        } = cli.command.unwrap()
        else {
            panic!("run");
        };
        assert_eq!(include_modules, ["c"]);
    }
}
