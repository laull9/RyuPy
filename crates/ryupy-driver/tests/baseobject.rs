//! 纯object身份、根关系和错误帧的真实差分；派生分配范围另锁明确拒绝。
use ryupy_driver::{BuildOptions, Session};
use std::path::PathBuf;
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT_ROOT: AtomicU64 = AtomicU64::new(0);

struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn execute(source: &str, filename: &str, reference: bool) -> (Option<Output>, Output) {
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
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-baseobject-{}-{unique}-{serial}",
        std::process::id()
    ));
    // 只有create成功后才拥有清理权；碰撞失败不能删除他人或旧产物。
    std::fs::create_dir(&path).unwrap();
    let root = Root(path);
    let input = root.0.join(filename);
    std::fs::write(&input, source).unwrap();
    let expected = reference.then(|| {
        Command::new(&python)
            .arg("-B")
            .arg(&input)
            .current_dir(&root.0)
            .output()
            .unwrap()
    });
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
fn baseobject_sentinel_and_root_relationships_match_fixed_python() {
    for (name, source) in [
        (
            "sentinel.py",
            include_str!("../../../tests/fixtures/baseobject/sentinel.py"),
        ),
        (
            "roots.py",
            include_str!("../../../tests/fixtures/baseobject/roots.py"),
        ),
    ] {
        let (expected, actual) = execute(source, name, true);
        let expected = expected.unwrap();
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
}
#[test]
fn baseobject_argument_error_keeps_evaluation_and_caller_frames() {
    let (expected, actual) = execute(
        include_str!("../../../tests/fixtures/baseobject/caller.py"),
        "caller.py",
        true,
    );
    let expected = expected.unwrap();
    assert_eq!(expected.status.code(), Some(1));
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    for output in [&expected, &actual] {
        let error = String::from_utf8_lossy(&output.stderr);
        assert!(error.contains("line 7, in <module>"), "{error}");
        assert!(error.contains("line 6, in caller"), "{error}");
        assert!(error.contains("TypeError:"), "{error}");
        assert!(!error.contains("Unhandled exception"), "{error}");
    }
}
#[test]
fn baseobject_derived_new_allocates_real_instance_without_initializing() {
    let (expected, actual) = execute(
        include_str!("../../../tests/fixtures/baseobject/derived_new_boundary.py"),
        "derived_new_boundary.py",
        true,
    );
    let expected = expected.unwrap();
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

// 完整CP正期望不删改；现Compare/Eq返回bool且Ne取反，不能保任意富比较对象。
#[test]
#[ignore = "pending: existing bool-only comparison ABI cannot preserve arbitrary reflected comparison results"]
fn baseobject_original_arbitrary_reflected_comparison_result_is_preserved_pending() {
    let (expected, actual) = execute(
        include_str!("../../../tests/fixtures/baseobject/sentinel_rich_comparison_pending.py"),
        "sentinel_rich_comparison_pending.py",
        true,
    );
    let expected = expected.unwrap();
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
