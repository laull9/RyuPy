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
        version: version::PythonVersion::new(3, 12, 13),
        root: root.into(),
        modules: vec!["dependency".into()],
        stdlib: Default::default(),
        declared: Default::default(),
        packages: Default::default(),
        container_transfer: ContainerTransfer::Proxy,
        payloads: vec![],
        dependency_report: serde_json::Value::Null,
        bridge: root.into(),
        shim: root.join("shim"),
        _shim_build: None,
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

#[test]
fn cpython_stdlib_inventory_excludes_site_packages_and_rejects_application_shadow() {
    let root = root();
    let lib = root.0.join(&native::stdlib_dirs().unwrap()[0]);
    std::fs::create_dir_all(lib.join("json")).unwrap();
    std::fs::write(lib.join("json/__init__.py"), "").unwrap();
    std::fs::create_dir_all(lib.join("site-packages/third_party")).unwrap();
    std::fs::write(lib.join("site-packages/third_party/__init__.py"), "").unwrap();
    let metadata = serde_json::json!({"build_info":{"extensions":{"math":[],"sys":[],"builtins":[],"bad-name":[]}}});
    let inventory = crate::cpython_inventory::stdlib(&root.0, &metadata).unwrap();
    assert!(inventory.contains("json") && inventory.contains("math"));
    assert!(
        !inventory.contains("third_party")
            && !inventory.contains("bad-name")
            && !inventory.contains("site-packages")
    );
    std::fs::write(root.0.join("entry.py"), "import json\n").unwrap();
    std::fs::write(root.0.join("json.py"), "value=1\n").unwrap();
    let mut sess = session(&root.0);
    let plan = sess.cpython_provider_state.plan.as_mut().unwrap();
    plan.modules.extend(inventory.iter().cloned());
    plan.stdlib = inventory;
    crate::modules::load_build_graph(&mut sess, &root.0.join("entry.py"), &[], false).unwrap();
    assert!(sess.diagnostics.has_errors());
    assert!(sess.render_diagnostics(false).contains("json.py"));
}

#[test]
fn provider_bundle_uses_one_explicit_bridge_root_and_binding_failure_restores_plan() {
    let root = root();
    let output = root.0.join("output");
    std::fs::create_dir(&output).unwrap();
    std::fs::write(root.0.join("RyuPy.Runtime.dll"), "runtime from bridge").unwrap();
    std::fs::write(root.0.join("RyuPy.CPython.dll"), "bridge from same root").unwrap();
    let mut sess = session(&root.0);
    copy_runtime_to(&sess, &output).unwrap();
    assert_eq!(
        std::fs::read_to_string(output.join("RyuPy.Runtime.dll")).unwrap(),
        "runtime from bridge"
    );
    assert_eq!(
        std::fs::read_to_string(output.join("RyuPy.CPython.dll")).unwrap(),
        "bridge from same root"
    );
    let options = crate::BuildOptions {
        input: root.0.join("entry.py"),
        out_dir: output,
        aot: false,
        search_paths: vec![],
    };
    let error = crate::build_configured(
        &mut sess,
        &options,
        None,
        Some(&root.0.join("binding.json")),
    )
    .unwrap_err();
    assert!(error.to_string().contains("CPython provider 声明不存在"));
    assert!(selected(&sess, "dependency.child"));
    assert_eq!(
        artifact_role(&sess, CONFIG),
        Some("runtime_provider_configuration")
    );
    assert_eq!(artifact_role(&sess, "unexpected.dll"), None);
    assert!(!options.out_dir.join(CONFIG).exists());
}
