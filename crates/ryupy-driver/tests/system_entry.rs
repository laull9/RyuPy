//! M3.4 系统入口：实际 CLR Main 参数、退出规则及缺源码原生模块边界。
use ryupy_driver::{BuildOptions, Session, Stage, dump};
use std::process::Command;

#[test]
fn open_builtin_supports_direct_and_first_class_lowering() {
    let input = std::env::temp_dir().join("ryupy_system_open_lowering.py");
    std::fs::write(
        &input,
        "reader = open\nprint(reader is open)\nopen('missing.txt')\n",
    )
    .unwrap();
    let mut session = Session::default();
    let result = dump(&mut session, &input, Stage::Mir);
    std::fs::remove_file(input).unwrap();
    assert!(result.is_ok(), "{}", session.render_diagnostics(false));
    let mir = result.unwrap();
    assert_eq!(
        mir.matches("RuntimeFn::BuiltinFunction(const \"open\")")
            .count(),
        2,
        "两次读取一等内建应共享运行时缓存入口"
    );
    assert!(mir.contains("RuntimeFn::DynBuiltin(const \"open\""));
}

#[test]
fn absent_native_modules_remain_runtime_imports() {
    let root = std::env::temp_dir().join(format!("ryupy_native_imports_{}", std::process::id()));
    std::fs::create_dir_all(&root).unwrap();
    let input = root.join("main.py");
    std::fs::write(
        &input,
        "import sys\nimport os\nimport os.path\nimport io\nimport pathlib\n",
    )
    .unwrap();
    let mut session = Session::default();
    let (modules, entry) = ryupy_driver::modules::load_program(&mut session, &input, &[]).unwrap();
    assert_eq!(entry, 0);
    assert_eq!(modules.len(), 1, "缺源码模块必须留给运行时，不引入伪造源码");
    assert!(session.diagnostics.is_empty());
    std::fs::remove_file(input).unwrap();
    std::fs::remove_dir(root).unwrap();
}

#[test]
fn actual_main_arguments_and_exit_status_are_preserved() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = std::env::temp_dir().join("ryupy_system_entry_arguments.py");
    std::fs::write(&input, "import sys\nprint(sys.argv[0])\nfor argument in sys.argv[1:]:\n    print('[' + argument + ']')\nif len(sys.argv) > 1:\n    if sys.argv[1] == 'exit':\n        sys.exit(7)\n    if sys.argv[1] == 'raise_exit':\n        raise SystemExit(7)\n    if sys.argv[1] == 'error':\n        raise ValueError('expected')\n").unwrap();
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: input.clone(),
            out_dir: root.join("target/test_system_entry"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let run = |args: &[&str]| {
        Command::new(ryupy_driver::dotnet::dotnet_exe())
            .arg(&artifact.dll)
            .args(args)
            .output()
            .unwrap()
    };
    let empty = run(&[]);
    assert_eq!(
        empty.status.code(),
        Some(0),
        "{}",
        String::from_utf8_lossy(&empty.stderr)
    );
    assert_eq!(
        String::from_utf8_lossy(&empty.stdout),
        format!("{}\n", input.display())
    );
    let values = run(&["", "a b", "中文", "--flag"]);
    assert_eq!(values.status.code(), Some(0));
    assert_eq!(
        String::from_utf8_lossy(&values.stdout),
        format!("{}\n[]\n[a b]\n[中文]\n[--flag]\n", input.display())
    );
    let exit = run(&["exit"]);
    assert_eq!(exit.status.code(), Some(7));
    assert!(exit.stderr.is_empty());
    let raised_exit = run(&["raise_exit"]);
    assert_eq!(raised_exit.status.code(), Some(7));
    assert!(raised_exit.stderr.is_empty());
    let error = run(&["error"]);
    assert_eq!(error.status.code(), Some(1));
    let stderr = String::from_utf8_lossy(&error.stderr);
    assert!(stderr.contains("ValueError: expected"), "{stderr}");
    assert!(!stderr.contains("Unhandled exception"), "{stderr}");
    std::fs::remove_file(input).unwrap();
}

#[test]
fn directory_error_types_have_hir_and_cil_bindings() {
    let input = std::env::temp_dir().join("ryupy_directory_error_types.py");
    std::fs::write(
        &input,
        "try:
    raise IsADirectoryError('directory')
except IsADirectoryError:
    pass
try:
    raise NotADirectoryError('file')
except NotADirectoryError:
    pass
",
    )
    .unwrap();
    let mut session = Session::default();
    let result = dump(&mut session, &input, Stage::Cil);
    std::fs::remove_file(input).unwrap();
    assert!(result.is_ok(), "{}", session.render_diagnostics(false));
    let cil = result.unwrap();
    assert!(cil.contains("PyIsADirectoryError"));
    assert!(cil.contains("PyNotADirectoryError"));
}

#[test]
fn print_target_is_captured_once_after_argument_evaluation() {
    let input = std::env::temp_dir().join("ryupy_print_target_lowering.py");
    std::fs::write(&input, "print(1, 2)\nprint(3, file=None, flush=True)\n").unwrap();
    let mut session = Session::default();
    let result = dump(&mut session, &input, Stage::Mir).unwrap();
    std::fs::remove_file(input).unwrap();
    assert_eq!(result.matches("RuntimeFn::PrintTarget(").count(), 1);
    assert!(
        result.contains("RuntimeFn::DynBuiltin(const \"print\""),
        "关键字print必须走完整运行时参数绑定"
    );
}
