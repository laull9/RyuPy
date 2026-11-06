//! 真实包资源差分：显式纳图不等于初始化，包根不依赖公开__file__或运行cwd。
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);
struct OwnedRoot(PathBuf);
impl Drop for OwnedRoot {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn root() -> OwnedRoot {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!(
        "ryupy-package-resources-{}-{stamp}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&path).unwrap();
    OwnedRoot(path)
}
const FILES: &[(&str, &str)] = &[
    (
        "roots.py",
        include_str!("../../../tests/fixtures/package_resources/roots.py"),
    ),
    (
        "left/__init__.py",
        include_str!("../../../tests/fixtures/package_resources/left/__init__.py"),
    ),
    (
        "left/data.txt",
        include_str!("../../../tests/fixtures/package_resources/left/data.txt"),
    ),
    (
        "right/__init__.py",
        include_str!("../../../tests/fixtures/package_resources/right/__init__.py"),
    ),
    (
        "right/data.txt",
        include_str!("../../../tests/fixtures/package_resources/right/data.txt"),
    ),
    (
        "cycle.py",
        include_str!("../../../tests/fixtures/package_resources/cycle.py"),
    ),
    (
        "cycle_events.py",
        include_str!("../../../tests/fixtures/package_resources/cycle_events.py"),
    ),
    (
        "cyclepkg/__init__.py",
        include_str!("../../../tests/fixtures/package_resources/cyclepkg/__init__.py"),
    ),
    (
        "cyclepkg/child.py",
        include_str!("../../../tests/fixtures/package_resources/cyclepkg/child.py"),
    ),
    (
        "cyclepkg/data.txt",
        include_str!("../../../tests/fixtures/package_resources/cyclepkg/data.txt"),
    ),
    (
        "failure.py",
        include_str!("../../../tests/fixtures/package_resources/failure.py"),
    ),
    (
        "failure_events.py",
        include_str!("../../../tests/fixtures/package_resources/failure_events.py"),
    ),
    (
        "failpkg/__init__.py",
        include_str!("../../../tests/fixtures/package_resources/failpkg/__init__.py"),
    ),
    (
        "failpkg/data.txt",
        include_str!("../../../tests/fixtures/package_resources/failpkg/data.txt"),
    ),
    (
        "missing.py",
        include_str!("../../../tests/fixtures/package_resources/missing.py"),
    ),
];
fn sources(root: &Path) -> PathBuf {
    let source = root.join("源码 & packages");
    for (relative, contents) in FILES {
        let file = source.join(relative);
        std::fs::create_dir_all(file.parent().unwrap()).unwrap();
        std::fs::write(file, contents).unwrap();
    }
    source
}
fn python() -> std::ffi::OsString {
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = Command::new(&python)
        .args(["-B", "-c", "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))"])
        .output().unwrap();
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert!(version.status.success());
    assert_eq!(
        String::from_utf8(version.stdout).unwrap().trim(),
        pinned.trim()
    );
    python
}
fn save(root: &Path, label: &str, output: &Output) {
    std::fs::write(root.join(format!("{label}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.join(format!("{label}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.join(format!("{label}.exit")),
        format!("{:?}", output.status.code()),
    )
    .unwrap();
}
fn differential(entry: &str, included: &[&str]) {
    let root = root();
    let source = sources(&root.0);
    let outside = root.0.join("outside");
    std::fs::create_dir(&outside).unwrap();
    let input = source.join(entry);
    let expected = Command::new(python())
        .arg("-B")
        .arg(&input)
        .current_dir(&outside)
        .output()
        .unwrap();
    save(&root.0, "python", &expected);
    assert!(
        expected.status.success(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    assert!(expected.stderr.is_empty());
    let mut session = Session {
        include_modules: included.iter().map(|name| (*name).to_owned()).collect(),
        ..Default::default()
    };
    let artifacts = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input,
            out_dir: root.0.join("artifacts"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let actual = ryupy_driver::dotnet::run_dll_with(
        &artifacts.dll,
        &ryupy_driver::dotnet::RunOptions {
            cwd: Some(outside),
            ..Default::default()
        },
    )
    .unwrap();
    save(&root.0, "jit", &actual);
    assert_eq!(
        actual.status.code(),
        expected.status.code(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}
#[test]
fn two_package_roots_ignore_cwd_and_public_file_rebinding() {
    differential("roots.py", &["left", "right"]);
}
#[test]
fn string_anchor_initializes_once_and_cycle_observes_partial_package() {
    differential("cycle.py", &["cyclepkg", "cyclepkg"]);
}
#[test]
fn failed_init_is_retried_but_successful_dependency_and_cached_package_survive() {
    differential("failure.py", &["failpkg"]);
}
#[test]
fn explicitly_included_missing_module_is_a_build_error() {
    let root = root();
    let source = sources(&root.0);
    let output = root.0.join("artifacts");
    std::fs::create_dir(&output).unwrap();
    let sentinel = output.join("user-sentinel");
    std::fs::write(&sentinel, b"unchanged").unwrap();
    let mut session = Session {
        include_modules: vec!["missing_included_package".to_owned()],
        ..Default::default()
    };
    let result = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: source.join("missing.py"),
            out_dir: output.clone(),
            aot: false,
            search_paths: vec![],
        },
    );
    let error = result.expect_err("explicit include cannot silently fall back to native");
    let detail = format!("{error:#}");
    assert!(detail.contains("missing_included_package"), "{detail}");
    assert_eq!(std::fs::read(sentinel).unwrap(), b"unchanged");
    assert!(!output.join("missing.dll").exists());
}
