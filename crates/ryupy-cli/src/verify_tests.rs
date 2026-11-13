//! 公开验包命令只有一个交付目录参数，不把编译开关静默套入只读路径。
use super::*;
use clap::error::ErrorKind;

#[test]
fn verify_accepts_exact_distribution_path_without_build_options() {
    let cli = Cli::try_parse_from(["ryupy", "verify", "../已搬迁/app.dist"]).unwrap();
    let Command::Verify { distribution } = cli.command else {
        panic!("需要verify");
    };
    assert_eq!(distribution, PathBuf::from("../已搬迁/app.dist"));
    assert_eq!(
        Cli::try_parse_from(["ryupy", "verify"])
            .err()
            .unwrap()
            .kind(),
        ErrorKind::MissingRequiredArgument
    );
}

#[test]
fn verify_rejects_compilation_globals_on_both_sides_of_subcommand() {
    for fragment in [
        vec!["--distribution-metadata", "missing.json"],
        vec!["--trace-product-v1"],
        vec!["--instance-module", "module"],
        vec!["--instance-direct-globals"],
        vec!["--experimental-direct-accumulator"],
        vec!["--experimental-leaf-inline"],
        vec!["--diagnostic-scalar-slp"],
        vec!["--experimental-packed-slp"],
    ] {
        for before in [false, true] {
            let mut args = vec!["ryupy"];
            if before {
                args.extend(fragment.iter().copied());
            }
            args.extend(["verify", "app.dist"]);
            if !before {
                args.extend(fragment.iter().copied());
            }
            assert_eq!(
                Cli::try_parse_from(args).err().unwrap().kind(),
                ErrorKind::ArgumentConflict
            );
        }
    }
}

#[test]
fn verify_does_not_accept_build_deployment_or_output_options() {
    for flag in [
        "--aot",
        "--project",
        "--resources",
        "--native-binding",
        "-o",
    ] {
        assert!(Cli::try_parse_from(["ryupy", "verify", "app.dist", flag, "x"]).is_err());
    }
}
