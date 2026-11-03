//! 普通单继承new/init的真实源码差分；罕见槽历史及专用布局单独受控拒绝。
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
        "ryupy-object-construction-{}-{unique}-{serial}",
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
    if filename == "allocation.py" {
        let mut session = Session::default();
        let mir = ryupy_driver::dump(&mut session, &input, ryupy_driver::Stage::Mir).unwrap();
        assert!(session.diagnostics.is_empty());
        assert!(mir.contains("RuntimeFn::FinalizeOrdinaryClass"), "{mir}");
        assert!(
            mir.contains("fn numeric(_0: i64) -> i64"),
            "unrelated numeric function changed: {mir}"
        );
        assert!(
            !mir.contains("New(Class("),
            "ordinary constructors must not become CLR allocation: {mir}"
        );
    }
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

fn equal(source: &str, filename: &str) {
    let (expected, actual) = execute(source, filename, true);
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
#[test]
fn ordinary_new_returns_foreign_cached_and_child_before_actual_init_lookup() {
    equal(
        include_str!("../../../tests/fixtures/object_construction/allocation.py"),
        "allocation.py",
    );
}
#[test]
fn ordinary_new_final_namespace_binding_and_super_match_python() {
    equal(
        include_str!("../../../tests/fixtures/object_construction/binding.py"),
        "binding.py",
    );
}
#[test]
fn ordinary_constructor_errors_context_and_original_caller_are_preserved() {
    equal(
        include_str!("../../../tests/fixtures/object_construction/errors.py"),
        "errors.py",
    );
    let (expected, actual) = execute(
        include_str!("../../../tests/fixtures/object_construction/caller.py"),
        "caller.py",
        true,
    );
    let expected = expected.unwrap();
    assert_eq!(expected.status.code(), Some(1));
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    for output in [&expected, &actual] {
        let error = String::from_utf8_lossy(&output.stderr);
        for frame in [
            "line 6, in <module>",
            "line 5, in caller",
            "line 3, in __new__",
            "ValueError: new-body",
        ] {
            assert!(error.contains(frame), "{error}");
        }
        assert!(!error.contains("Unhandled exception"), "{error}");
    }
}
#[test]
fn ordinary_constructor_special_layout_and_rare_mutations_are_explicit_boundaries() {
    let (_, actual) = execute(
        include_str!("../../../tests/fixtures/object_construction/boundaries.py"),
        "boundaries.py",
        false,
    );
    assert!(
        actual.status.success(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(
        String::from_utf8_lossy(&actual.stdout).trim(),
        "boundaries-ok"
    );
    assert!(actual.stderr.is_empty());
}
