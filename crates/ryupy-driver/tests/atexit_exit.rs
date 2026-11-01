//! 显式退出回调的真实进程验收：保原退出码、错误继续、不关闭未注册流。
use ryupy_driver::{BuildOptions, Session};

#[test]
fn atexit_runs_for_all_python_exit_paths_and_preserves_status() {
    let root = ryupy_driver::runtime::workspace_root();
    let directory = std::env::temp_dir().join(format!("ryupy_atexit_{}", std::process::id()));
    std::fs::create_dir_all(&directory).unwrap();
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: root.join("tests/fixtures/atexit_exit.py"),
            out_dir: directory.join("output"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    for (mode, code) in [
        ("normal", 0),
        ("callback_error", 0),
        ("stderr_error", 0),
        ("bad_repr", 0),
        ("chain", 0),
        ("system_exit", 3),
        ("body_error", 1),
        ("body_callback_error", 1),
        ("no_close", 0),
        ("flush_error", 120),
    ] {
        let output = std::process::Command::new(ryupy_driver::dotnet::dotnet_exe())
            .arg(&artifact.dll)
            .arg(mode)
            .output()
            .unwrap();
        let stdout = String::from_utf8_lossy(&output.stdout);
        let stderr = String::from_utf8_lossy(&output.stderr);
        assert_eq!(output.status.code(), Some(code), "{mode}: {stderr}");
        if mode != "flush_error" {
            assert_eq!(stdout, "body\ncallback\noldest\n", "{mode}: {stderr}");
        }
        assert!(!stderr.contains("Unhandled exception"), "{mode}: {stderr}");
        assert!(!stderr.contains("must not close"), "{mode}: {stderr}");
        if [
            "callback_error",
            "bad_repr",
            "chain",
            "system_exit",
            "body_callback_error",
        ]
        .contains(&mode)
        {
            assert!(
                stderr.contains("Exception ignored in atexit callback:"),
                "{stderr}"
            );
        }
        if mode == "bad_repr" {
            assert!(stderr.contains("<object repr() failed>"), "{stderr}");
        }
        if mode == "chain" {
            assert!(!stderr.contains("ValueError: hidden cause"), "{stderr}");
        }
        if mode == "body_callback_error" {
            let callback_report = stderr
                .split("Exception ignored in atexit callback:")
                .nth(1)
                .unwrap();
            assert!(
                !callback_report.contains("in <module>"),
                "{callback_report}"
            );
        }
        if mode == "normal" || mode == "no_close" {
            assert!(stderr.is_empty(), "{stderr}");
        }
    }
    let _ = std::fs::remove_dir_all(directory);
}
