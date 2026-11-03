//! 真源码默认优化差分：模块方法先取值，参数重绑不替换当前调用目标。
use ryupy_driver::{BuildOptions, Session};
use std::path::PathBuf;
use std::process::Command;
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT_ROOT: AtomicU64 = AtomicU64::new(0);

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
    root_with_stamp(stamp)
}

fn root_with_stamp(stamp: u128) -> OwnedRoot {
    // 原子值只分配进程内唯一序号，不承担线程间同步。
    let sequence = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-module-capture-{}-{stamp}-{sequence}",
        std::process::id()
    ));
    // 只有独占创建成功才取得清理权；预存目录不删除、不复用。
    std::fs::create_dir(&path).unwrap();
    OwnedRoot(path)
}

#[test]
fn fixed_timestamp_parallel_roots_are_distinct_and_live_together() {
    let barrier = std::sync::Barrier::new(8);
    let roots = std::thread::scope(|scope| {
        let handles: Vec<_> = (0..8)
            .map(|_| {
                let barrier = &barrier;
                scope.spawn(move || {
                    barrier.wait();
                    root_with_stamp(123456789)
                })
            })
            .collect();
        handles
            .into_iter()
            .map(|handle| handle.join().unwrap())
            .collect::<Vec<_>>()
    });
    let paths: std::collections::HashSet<_> = roots.iter().map(|root| &root.0).collect();
    assert_eq!(paths.len(), 8);
    assert!(roots.iter().all(|root| root.0.is_dir()));
    // 所有guard同时保留至断言结束，不能靠先删旧目录制造唯一性。
}

const SOURCES: &[(&str, &str)] = &[
    (
        "main.py",
        include_str!("../../../tests/fixtures/module_call_capture/main.py"),
    ),
    (
        "provider.py",
        include_str!("../../../tests/fixtures/module_call_capture/provider.py"),
    ),
    (
        "other.py",
        include_str!("../../../tests/fixtures/module_call_capture/other.py"),
    ),
];

fn compare_module_calls(replacement: Option<&str>) {
    let workspace = ryupy_driver::runtime::workspace_root();
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = Command::new(&python)
        .args(["-c", "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))"])
        .output().unwrap();
    assert!(version.status.success());
    let pinned = std::fs::read_to_string(workspace.join("tests/pinned-python.txt")).unwrap();
    assert_eq!(
        String::from_utf8_lossy(&version.stdout).trim(),
        pinned.trim()
    );
    let root = root();
    for (name, text) in SOURCES {
        std::fs::write(root.0.join(name), text).unwrap();
    }
    if let Some(source) = replacement {
        std::fs::write(root.0.join("main.py"), source).unwrap();
    }
    let input = root.0.join("main.py");
    let expected = Command::new(&python)
        .arg(&input)
        .current_dir(&root.0)
        .output()
        .unwrap();
    assert!(
        expected.status.success(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    assert!(expected.stderr.is_empty());
    // 不关优化、不手造PE；多模块真实注册与公开槽在实际执行中接受检验。
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
    assert_eq!(
        actual.status.code(),
        Some(0),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}

#[test]
fn ordinary_module_calls_preserve_captured_values_before_argument_callbacks() {
    compare_module_calls(None);
}

#[test]
#[ignore = "已知旧债：源码def静态Func读取在全局重绑后仍取原函数；完整CP正期望保留"]
fn static_function_identity_after_rebinding_matches_python() {
    compare_module_calls(Some(include_str!(
        "../../../tests/fixtures/module_call_capture/static_identity_pending.py"
    )));
}

#[test]
fn generator_resume_preserves_module_target_across_argument_rebinding() {
    compare_module_calls(Some(include_str!(
        "../../../tests/fixtures/module_call_capture/generator.py"
    )));
}
