//! 项目模式只选目标，不与旧零散参数隐式合并；旧命令继续接受原合同。
use super::*;
use clap::error::ErrorKind;

#[test]
fn project_selects_target_without_positional_input_or_aot_switch() {
    let cli = Cli::try_parse_from([
        "ryupy",
        "build",
        "--project",
        "../app/ryupy.json",
        "--target",
        "console",
        "-o",
        "relative-out",
    ])
    .unwrap();
    let Command::Build {
        input,
        project,
        target,
        aot,
        out_dir,
        ..
    } = cli.command
    else {
        panic!("需要build");
    };
    assert!(input.is_none());
    assert_eq!(project, Some(PathBuf::from("../app/ryupy.json")));
    assert_eq!(target.as_deref(), Some("console"));
    assert!(!aot); // driver从配置mode决定，不伪造命令行标志。
    assert_eq!(out_dir, Some(PathBuf::from("relative-out")));
}

#[test]
fn input_or_complete_project_pair_is_required() {
    for args in [
        vec!["ryupy", "build"],
        vec!["ryupy", "build", "--project", "ryupy.json"],
        vec!["ryupy", "build", "--target", "console"],
        vec!["ryupy", "build", "main.py", "--target", "console"],
    ] {
        let error = Cli::try_parse_from(args).err().expect("不完整选择必须拒绝");
        assert_eq!(error.kind(), ErrorKind::MissingRequiredArgument);
    }
}

#[test]
fn project_rejects_each_fragment_instead_of_merging() {
    for fragment in [
        vec!["main.py"],
        vec!["--aot"],
        vec!["-I", "deps"],
        vec!["--include-module", "pkg"],
        vec!["--resources", "resources.json"],
        vec!["--native-binding", "native.json"],
        vec!["--distribution-metadata", "metadata.json"],
        vec!["--instance-module", "pkg"],
        vec!["--instance-direct-globals"],
    ] {
        let mut args = vec![
            "ryupy",
            "build",
            "--project",
            "ryupy.json",
            "--target",
            "console",
        ];
        args.extend(fragment);
        let error = Cli::try_parse_from(args).err().expect("零散参数必须拒绝");
        assert_eq!(error.kind(), ErrorKind::ArgumentConflict);
    }
    // 全局参数放在子命令前也不能绕过互斥。
    for fragment in [
        vec!["--distribution-metadata", "metadata.json"],
        vec!["--instance-module", "pkg"],
        vec!["--instance-direct-globals"],
    ] {
        let mut args = vec!["ryupy"];
        args.extend(fragment);
        args.extend(["build", "--project", "ryupy.json", "--target", "console"]);
        let error = Cli::try_parse_from(args)
            .err()
            .expect("前置全局参数也必须拒绝");
        assert_eq!(error.kind(), ErrorKind::ArgumentConflict);
    }
}

#[test]
fn old_commands_and_explicit_aot_remain_available() {
    for command in ["build", "run", "dump"] {
        assert!(Cli::try_parse_from(["ryupy", command, "main.py"]).is_ok());
        if command != "build" {
            assert!(
                Cli::try_parse_from([
                    "ryupy",
                    command,
                    "--project",
                    "ryupy.json",
                    "--target",
                    "console"
                ])
                .is_err()
            );
        }
    }
    let cli = Cli::try_parse_from([
        "ryupy",
        "build",
        "main.py",
        "--aot",
        "--resources",
        "data.json",
        "-I",
        "deps",
        "--include-module",
        "pkg",
    ])
    .unwrap();
    let Command::Build {
        input,
        project,
        target,
        aot,
        ..
    } = cli.command
    else {
        panic!("需要build")
    };
    assert_eq!(input, Some(PathBuf::from("main.py")));
    assert!(project.is_none() && target.is_none() && aot);
}
