//! 真实候选树与结构化清单的布局回归；不运行SDK，也不复制生产转换函数作为oracle。
use super::*;
use serde_json::json;
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Tree(PathBuf);
impl Tree {
    fn new() -> Self {
        let path = std::env::temp_dir().join(format!(
            "ryupy-layout-unit-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        std::fs::create_dir(&path).unwrap();
        Self(path)
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

#[test]
fn plain_schema_one_preserves_absent_extensions_and_logical_build_inputs() {
    let before = json!({"schema_version":1,"kind":"ryupy-native-aot-artifact",
        "artifacts":[{"relative_path":"app","role":"native_executable","size":2},
            {"relative_path":"libe_sqlite3.so","role":"runtime_native_dependency","size":3}],
        "build_inputs":[{"relative_path":"RyuPy.Runtime.dll"},{"relative_path":"managed-entry.dll"}]});
    let after: Value = serde_json::from_slice(
        &manifest_bytes(&serde_json::to_vec(&before).unwrap(), "app").unwrap(),
    )
    .unwrap();
    for absent in [
        "resources",
        "module_locations",
        "native_bindings",
        "binding_interfaces",
    ] {
        assert!(
            after.get(absent).is_none(),
            "可选扩展不应凭IndexMut插入null: {absent}"
        );
    }
    assert_eq!(after["schema_version"], 1);
    assert_eq!(after["build_inputs"], before["build_inputs"]);
    assert_eq!(after["artifacts"][0]["relative_path"], "app");
    assert_eq!(
        after["artifacts"][1]["relative_path"],
        "internal/libe_sqlite3.so"
    );
}

#[test]
fn canonical_tree_keeps_exactly_entry_and_internal_and_flattens_versioned_payload() {
    let tree = Tree::new();
    let root = &tree.0;
    std::fs::write(root.join("app"), b"entry").unwrap();
    std::fs::write(root.join("RyuPy.Runtime.dll"), b"runtime").unwrap();
    std::fs::create_dir_all(root.join("_internal/resources/v1")).unwrap();
    std::fs::write(root.join("_internal/resources/v1/data.txt"), b"resource").unwrap();
    canonicalize(root, &root.join("app"), "apphost").unwrap();
    verify_root(root, "app").unwrap();
    assert_eq!(
        std::fs::read(root.join("internal/resources/v1/data.txt")).unwrap(),
        b"resource"
    );
    assert!(root.join("internal/RyuPy.Runtime.dll").is_file());
    assert!(!root.join("_internal").exists());
    assert!(is_canonical(root).unwrap());
    assert_eq!(
        manifest_path(root).unwrap(),
        root.join("internal/ryupy-artifact.json")
    );
}

#[test]
fn marker_collisions_are_rejected_without_overwriting_existing_payload() {
    for payload in [MARKER.to_owned(), format!("_internal/{MARKER}")] {
        let tree = Tree::new();
        let path = tree.0.join(&payload);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(&path, b"user-payload").unwrap();
        std::fs::write(tree.0.join("app"), b"entry").unwrap();
        assert!(canonicalize(&tree.0, &tree.0.join("app"), "apphost").is_err());
        assert_eq!(std::fs::read(path).unwrap(), b"user-payload");
    }
}

#[test]
fn malformed_duplicate_or_linked_marker_never_falls_back_to_legacy() {
    let tree = Tree::new();
    std::fs::create_dir(tree.0.join(DIRECTORY)).unwrap();
    let path = tree.0.join(DIRECTORY).join(MARKER);
    for text in [
        "{}",
        r#"{"schema_version":1,"schema_version":1,"kind":"ryupy-application-layout","payload_mode":"canonical-v1","entry":"app","entry_kind":"apphost"}"#,
    ] {
        std::fs::write(&path, text).unwrap();
        assert!(is_canonical(&tree.0).is_err());
        assert!(manifest_path(&tree.0).is_err());
    }
    #[cfg(unix)]
    {
        std::fs::remove_file(&path).unwrap();
        std::os::unix::fs::symlink("missing", &path).unwrap();
        assert!(is_canonical(&tree.0).is_err());
    }
}

#[test]
fn internal_source_name_uses_a_non_conflicting_actual_executable_name() {
    if cfg!(windows) {
        assert_eq!(entry_name("internal"), "internal.exe");
    } else {
        assert_eq!(entry_name("internal"), "internal.bin");
        assert_eq!(entry_name("INTERNAL"), "INTERNAL.bin");
    }
}
