//! 主入口的用户合同：默认单文件、发布模式、环境选择、参数冲突与旧入口兼容。
use super::*;

#[test]
fn project_entry_selects_onefile_and_automatic_environment() {
    for arguments in [
        vec!["ryupy", "main.py"],
        vec!["ryupy", "--onefile", "main.py"],
    ] {
        let mut cli = Cli::try_parse_from(arguments).unwrap();
        let Command::Build {
            input,
            aot,
            onefile,
            out_dir,
            ..
        } = cli.take_command().unwrap()
        else {
            panic!("应用入口应构建应用");
        };
        assert_eq!(input, PathBuf::from("main.py"));
        assert!(aot && onefile);
        assert_eq!(cli.cpython_provider, Some(PathBuf::from("auto")));
        assert_eq!(
            out_dir.unwrap(),
            std::env::current_dir().unwrap().join("dist")
        );
    }
}

#[test]
fn native_is_explicit_and_modes_have_one_meaning() {
    for (mode, expected_aot, expected_onefile) in [
        ("onefile", true, true),
        ("standalone", true, false),
        ("jit", false, false),
    ] {
        let mut cli =
            Cli::try_parse_from(["ryupy", "main.py", "--mode", mode, "--native"]).unwrap();
        let Command::Build { aot, onefile, .. } = cli.take_command().unwrap() else {
            panic!("构建入口");
        };
        assert_eq!((aot, onefile), (expected_aot, expected_onefile));
        assert!(cli.cpython_provider.is_none());
    }
    let mut cli =
        Cli::try_parse_from(["ryupy", "main.py", "--standalone", "--output-dir", "out"]).unwrap();
    assert!(
        matches!(cli.take_command().unwrap(), Command::Build { aot: true, onefile: false, out_dir: Some(path), .. } if path == std::path::Path::new("out"))
    );
}

#[test]
fn conflicting_intents_and_missing_input_are_rejected() {
    for arguments in [
        vec!["ryupy", "--onefile"],
        vec!["ryupy", "main.py", "--onefile", "--standalone"],
        vec!["ryupy", "main.py", "--mode", "jit", "--onefile"],
        vec!["ryupy", "main.py", "--native", "--cpython-provider", "auto"],
        vec!["ryupy", "main.py", "--mode", "unknown"],
    ] {
        assert!(Cli::try_parse_from(arguments).is_err());
    }
    assert!(
        Cli::try_parse_from(["ryupy", "--help-advanced"])
            .unwrap()
            .help_advanced
    );
}

#[test]
fn legacy_commands_keep_execution_domain_and_global_options() {
    for arguments in [
        vec!["ryupy", "build", "main.py", "--onefile"],
        vec!["ryupy", "--native", "build", "main.py", "--onefile"],
    ] {
        let mut cli = Cli::try_parse_from(arguments).unwrap();
        assert!(matches!(
            cli.take_command().unwrap(),
            Command::Build { onefile: true, .. }
        ));
        assert!(cli.cpython_provider.is_none());
    }
    let mut cli =
        Cli::try_parse_from(["ryupy", "--cpython-provider", "auto", "build", "main.py"]).unwrap();
    cli.take_command().unwrap();
    assert_eq!(cli.cpython_provider, Some(PathBuf::from("auto")));
}
