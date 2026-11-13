//! provider 计划的执行域与发布边界测试：依赖源码无需通过 RyuPy，应用入口的错误仍须诊断。
use super::*;
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
fn root() -> Root {
    let root = Root(std::env::temp_dir().join(format!(
        "ryupy-provider-tests-{}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    )));
    std::fs::create_dir(&root.0).unwrap();
    root
}
fn session(root: &Path) -> Session {
    let mut sess = Session {
        cpython_provider: Some(root.join("provider.json")),
        ..Session::default()
    };
    sess.cpython_provider_state.plan = Some(Plan {
        root: root.into(),
        modules: vec!["dependency".into()],
        container_transfer: ContainerTransfer::Proxy,
        payloads: vec![],
        bridge: root.into(),
        shim: root.join("shim"),
        source: Source {
            project: "python-build-standalone".into(),
            build_id: "20260623".into(),
            source_url:
                "https://github.com/astral-sh/python-build-standalone/releases/tag/20260623".into(),
            licenses: vec![],
        },
    });
    sess
}
#[test]
fn cpython_dependency_family_bypasses_rust_parse_and_preserves_compiled_entry() {
    let root = root();
    std::fs::create_dir(root.0.join("dependency")).unwrap();
    std::fs::write(
        root.0.join("dependency/__init__.py"),
        "this is deliberately invalid syntax !",
    )
    .unwrap();
    std::fs::write(
        root.0.join("entry.py"),
        "import dependency\nimport dependency.child\nprint(42)\n",
    )
    .unwrap();
    let mut sess = session(&root.0);
    let (modules, entry) =
        crate::modules::load_build_graph(&mut sess, &root.0.join("entry.py"), &[], false).unwrap();
    assert_eq!(entry, 0);
    assert_eq!(modules.len(), 1);
    assert_eq!(modules[0].name, "__main__");
    assert!(!sess.diagnostics.has_errors());
    assert!(selected(&sess, "dependency.child"));
    assert!(!selected(&sess, "dependency_extra"));
    assert!(!selected(&sess, "__main__"));
    std::fs::write(
        root.0.join("entry.py"),
        "import dependency\nthis is invalid !\n",
    )
    .unwrap();
    crate::modules::load_build_graph(&mut sess, &root.0.join("entry.py"), &[], false).unwrap();
    assert!(sess.diagnostics.has_errors());
}
#[test]
fn provider_paths_reject_escape_absolute_and_ambiguous_components() {
    for path in [
        "",
        "../lib",
        "/absolute",
        "lib/../../outside",
        "lib\\outside",
        "x\n",
    ] {
        assert!(relative(path).is_err(), "{path:?}");
    }
    assert!(relative("lib/python3.12/site-packages").is_ok());
}
#[test]
fn unselected_mode_retains_normal_dependency_discovery() {
    let root = root();
    std::fs::write(root.0.join("dependency.py"), "value = 7\n").unwrap();
    std::fs::write(root.0.join("entry.py"), "import dependency\n").unwrap();
    let (modules, _) = crate::modules::load_build_graph(
        &mut Session::default(),
        &root.0.join("entry.py"),
        &[],
        false,
    )
    .unwrap();
    assert_eq!(modules.len(), 2);
}
