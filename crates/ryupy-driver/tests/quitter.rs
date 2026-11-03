//! 默认site合同下的真实Quitter差分；不运行项目集，不将CP -S无对象模式冒充已支持。
use ryupy_driver::{BuildOptions, Session};
use std::path::PathBuf;
use std::process::{Command, Output};

struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn compare(source: &str, filename: &str) -> (Output, Output) {
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = Command::new(&python).args(["-c", "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))"]).output().unwrap();
    assert!(version.status.success());
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(
        String::from_utf8_lossy(&version.stdout).trim(),
        pinned.trim()
    );
    let unique = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root =
        Root(std::env::temp_dir().join(format!("ryupy-quitter-{}-{unique}", std::process::id())));
    std::fs::create_dir(&root.0).unwrap();
    let input = root.0.join(filename);
    std::fs::write(&input, source).unwrap();
    std::fs::write(
        root.0.join("shadow.py"),
        include_str!("../../../tests/fixtures/quitter/shadow.py"),
    )
    .unwrap();
    // 不传-S：现固定项目/默认解释器契约包含site注入的两个辅助对象。
    let expected = Command::new(&python)
        .arg("-B")
        .arg(&input)
        .current_dir(&root.0)
        .output()
        .unwrap();
    let artifact = ryupy_driver::build(
        &mut Session::default(),
        &BuildOptions {
            input,
            out_dir: root.0.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    (expected, actual)
}
#[test]
fn quitter_default_site_identity_binding_context_and_reentry_match_python() {
    let (expected, actual) = compare(
        include_str!("../../../tests/fixtures/quitter/core.py"),
        "core.py",
    );
    assert!(
        expected.status.success(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    assert!(
        actual.status.success(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}
#[test]
fn quitter_binding_error_keeps_original_caller_without_close_callback() {
    let (expected, actual) = compare(
        include_str!("../../../tests/fixtures/quitter/caller.py"),
        "caller.py",
    );
    assert_eq!(expected.status.code(), Some(1));
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    for output in [&expected, &actual] {
        let stderr = String::from_utf8_lossy(&output.stderr);
        assert!(stderr.contains("line 8, in <module>"), "{stderr}");
        assert!(stderr.contains("line 7, in caller"), "{stderr}");
        assert!(stderr.contains("TypeError:"), "{stderr}");
        assert!(!stderr.contains("swallowed"), "{stderr}");
        assert!(!stderr.contains("Unhandled exception"), "{stderr}");
    }
}
#[test]
fn quitter_real_exit_status_stderr_and_finally_match_python() {
    for code in ["None", "7", "'exit-message'"] {
        let source = format!("try:\n    quit({code})\nfinally:\n    print('cleanup')\n");
        let (expected, actual) = compare(&source, "terminate.py");
        assert_eq!(actual.status.code(), expected.status.code());
        assert_eq!(actual.stdout, expected.stdout);
        assert_eq!(actual.stderr, expected.stderr);
    }
}
