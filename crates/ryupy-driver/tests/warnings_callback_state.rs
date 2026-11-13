//! 共享warnings回调帧及活动异常恢复与固定CPython逐次比较。
use ryupy_driver::Session;
#[path = "support/process_oracle.rs"]
mod process_oracle;
use process_oracle::{TempRoot, bounded, pinned_python};
fn source() -> std::path::PathBuf {
    ryupy_driver::runtime::workspace_root().join("tests/e2e/phase3/m3_7_warnings_callback_state.py")
}
#[test]
fn warning_callback_state_matches_pinned_python() {
    let root = TempRoot::new("warnings-callback-state");
    let python = pinned_python(root.path());
    let cp_cwd = root.path().join("cp-cwd");
    let jit_cwd = root.path().join("jit-cwd");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&jit_cwd).unwrap();
    let expected = bounded(
        root.path(),
        "cp",
        std::process::Command::new(python)
            .args(["-I", "-B", "-S"])
            .arg(source())
            .current_dir(cp_cwd)
            .env("LC_ALL", "C")
            .env("TZ", "UTC"),
        std::time::Duration::from_secs(60),
    )
    .unwrap();
    assert!(expected.status.success() && expected.stderr.is_empty());
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input: source(),
            out_dir: root.path().join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    assert!(session.diagnostics.is_empty());
    let actual = bounded(
        root.path(),
        "jit",
        std::process::Command::new(std::env::var_os("DOTNET").unwrap_or_else(|| "dotnet".into()))
            .arg(artifact.dll)
            .current_dir(jit_cwd)
            .env("LC_ALL", "C")
            .env("TZ", "UTC"),
        std::time::Duration::from_secs(60),
    )
    .unwrap();
    assert!(
        actual.status.success(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.stderr, expected.stderr);
    assert_eq!(actual.stdout, expected.stdout);
}

#[test]
fn swallowed_io_frames_do_not_pollute_next_exception_and_valueerror_frames_survive() {
    for (name, exception, tail) in [
        ("swallowed", "OSError", "raise ValueError('after')\n"),
        ("propagated", "ValueError", ""),
    ] {
        let root = TempRoot::new(name);
        let input = root.path().join("warning_trace.py");
        let text = format!(
            "import warnings\nimport sys\nclass Sink:\n    def write(self, text):\n        raise {exception}('sink')\nold=sys.stderr\ntry:\n    sys.stderr=Sink()\n    with warnings.catch_warnings():\n        warnings.simplefilter('always')\n        for i in range(2):\n            warnings.warn('direct')\nfinally:\n    sys.stderr=old\n{tail}"
        );
        std::fs::write(&input, text).unwrap();
        let cp_cwd = root.path().join("cp-cwd");
        let jit_cwd = root.path().join("jit-cwd");
        std::fs::create_dir(&cp_cwd).unwrap();
        std::fs::create_dir(&jit_cwd).unwrap();
        let expected = bounded(
            root.path(),
            "cp",
            std::process::Command::new(pinned_python(root.path()))
                .args(["-I", "-B", "-S"])
                .arg(&input)
                .current_dir(cp_cwd)
                .env("LC_ALL", "C")
                .env("TZ", "UTC"),
            std::time::Duration::from_secs(60),
        )
        .unwrap();
        let mut session = Session::default();
        let artifact = ryupy_driver::build(
            &mut session,
            &ryupy_driver::BuildOptions {
                input,
                out_dir: root.path().join("out"),
                aot: false,
                search_paths: vec![],
            },
        )
        .unwrap();
        assert!(session.diagnostics.is_empty());
        let actual = bounded(
            root.path(),
            "jit",
            std::process::Command::new(
                std::env::var_os("DOTNET").unwrap_or_else(|| "dotnet".into()),
            )
            .arg(artifact.dll)
            .current_dir(jit_cwd)
            .env("LC_ALL", "C")
            .env("TZ", "UTC"),
            std::time::Duration::from_secs(60),
        )
        .unwrap();
        assert!(!expected.status.success() && !actual.status.success());
        assert_eq!(actual.stdout, expected.stdout);
        let expected = String::from_utf8(expected.stderr).unwrap();
        let actual = String::from_utf8(actual.stderr).unwrap();
        let frames = |error: &str| -> Vec<String> {
            error
                .lines()
                .filter(|line| {
                    // CP的warnings.py内部帧与原生实现不同；只比较本测试用户源码帧，
                    // 被吞回调残留的write也来自同一文件，不能被这个范围过滤掉。
                    line.trim_start().starts_with("File \"")
                        && line
                            .split_once("\", line ")
                            .is_some_and(|(file, _)| file.ends_with("warning_trace.py"))
                })
                .map(|line| line.split_once("\", line ").unwrap().1.to_owned())
                .collect()
        };
        let expected_frames = frames(&expected);
        assert!(!expected_frames.is_empty());
        assert_eq!(frames(&actual), expected_frames, "{name}: {actual}");
        assert_eq!(actual.lines().last(), expected.lines().last());
    }
}
