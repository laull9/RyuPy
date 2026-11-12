//! 项目请求路径/字节守卫与失败隔离；不启动SDK，不把解析通过冒充原生交付。
use super::*;
use std::fs;
struct Tree(PathBuf);
impl Tree {
    fn new() -> Self {
        let root = crate::test_temp::directory("ryupy-deployment")
            .canonicalize()
            .unwrap();
        fs::create_dir(root.join("package")).unwrap();
        fs::create_dir(root.join("config")).unwrap();
        fs::write(root.join("entry.py"), "print(7)\n").unwrap();
        fs::write(
            root.join("config/resources.json"),
            "{\"schema_version\":1,\"modules\":[]}",
        )
        .unwrap();
        fs::write(
            root.join("config/metadata.json"),
            "{\"schema_version\":1,\"mode\":\"closed_snapshot\",\"roots\":[]}",
        )
        .unwrap();
        let tree = Self(root);
        tree.config(r#"{"schema_version":1,"mode":"aot","search_paths":["package"],"include_modules":["pyfiglet"],"resources":"config/resources.json","distribution_metadata":"config/metadata.json","targets":{"app":{"entry":"entry.py"}}}"#);
        tree
    }
    fn config(&self, text: &str) {
        fs::write(self.0.join("ryupy.json"), text).unwrap();
    }
    fn load(&self) -> Result<DeploymentRequest> {
        DeploymentRequest::load(&self.0.join("ryupy.json"), "app", None)
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        let _ = fs::remove_dir_all(&self.0);
    }
}

#[test]
fn project_paths_and_nested_request_meaning_are_preserved() {
    let tree = Tree::new();
    let request = tree.load().unwrap();
    assert_eq!(request.options.input, tree.0.join("entry.py"));
    assert_eq!(request.options.search_paths, vec![tree.0.join("package")]);
    assert_eq!(request.options.out_dir, tree.0.join("build/app"));
    assert_eq!(request.metadata, Some(tree.0.join("config/metadata.json")));
    assert!(request.options.aot);
    let request = DeploymentRequest::load(
        &tree.0.join("ryupy.json"),
        "app",
        Some(Path::new("caller-output")),
    )
    .unwrap();
    assert_eq!(request.options.out_dir, Path::new("caller-output"));
    assert_eq!(
        fs::read_to_string(tree.0.join("config/metadata.json")).unwrap(),
        "{\"schema_version\":1,\"mode\":\"closed_snapshot\",\"roots\":[]}"
    );
}

#[test]
fn unknown_duplicate_and_invalid_targets_are_rejected() {
    let tree = Tree::new();
    for json in [
        r#"{"schema_version":1,"schema_version":1,"mode":"aot","targets":{"app":{"entry":"entry.py"}}}"#,
        r#"{"schema_version":1,"mode":"aot","unknown":1,"targets":{"app":{"entry":"entry.py"}}}"#,
        r#"{"schema_version":1,"mode":"aot","targets":{"app":{"entry":"entry.py"},"app":{"entry":"entry.py"}}}"#,
        r#"{"schema_version":1,"mode":"aot","targets":{"app":{"entry":"entry.py","entry":"entry.py"}}}"#,
        r#"{"schema_version":1,"mode":"aot","targets":{"app":{"entry":"entry.py","unknown":1}}}"#,
        r#"{"schema_version":1,"mode":"jit","targets":{"app":{"entry":"entry.py"}}}"#,
        r#"{"schema_version":1,"mode":"aot","targets":{}}"#,
        r#"{"schema_version":1,"mode":"aot","search_paths":["package","package"],"targets":{"app":{"entry":"entry.py"}}}"#,
        r#"{"schema_version":1,"mode":"aot","include_modules":["x","x"],"targets":{"app":{"entry":"entry.py"}}}"#,
    ] {
        tree.config(json);
        assert!(tree.load().is_err(), "{json}");
    }
}

#[test]
fn relative_escape_and_wrong_file_types_fail_before_outputs() {
    let tree = Tree::new();
    for entry in [
        "../entry.py",
        "/tmp/entry.py",
        "C:/entry.py",
        "package",
        "entry.py/child",
        "a\\b.py",
    ] {
        tree.config(
            &serde_json::json!({"schema_version":1,"mode":"aot","targets":{"app":{"entry":entry}}})
                .to_string(),
        );
        assert!(tree.load().is_err(), "{entry}");
        assert!(!tree.0.join("build").exists());
    }
    tree.config(&" ".repeat(CONFIG_LIMIT as usize + 1));
    assert!(tree.load().is_err());
}

#[cfg(unix)]
#[test]
fn linked_entry_or_parent_is_not_followed() {
    let tree = Tree::new();
    std::os::unix::fs::symlink(tree.0.join("entry.py"), tree.0.join("linked.py")).unwrap();
    std::os::unix::fs::symlink(tree.0.join("package"), tree.0.join("linked-dir")).unwrap();
    for entry in ["linked.py", "linked-dir/inside.py"] {
        tree.config(
            &serde_json::json!({"schema_version":1,"mode":"aot","targets":{"app":{"entry":entry}}})
                .to_string(),
        );
        assert!(tree.load().is_err());
    }
}

#[test]
fn changed_config_entry_or_declaration_is_rejected_without_publication() {
    for name in [
        "ryupy.json",
        "entry.py",
        "config/resources.json",
        "config/metadata.json",
    ] {
        let tree = Tree::new();
        let request = tree.load().unwrap();
        let mut changed = fs::read(tree.0.join(name)).unwrap();
        changed[0] ^= 1; // 同长度修改仍须由字节hash检出。
        fs::write(tree.0.join(name), changed).unwrap();
        assert!(request.guard.verify().is_err());
        assert!(request.build(&mut Session::default()).is_err());
        assert!(!tree.0.join("build").exists());
    }
}

#[test]
fn compilation_failure_restores_session_and_never_enables_pilot() {
    let tree = Tree::new();
    fs::write(tree.0.join("entry.py"), "def :\n").unwrap();
    let request = tree.load().unwrap();
    let mut session = Session {
        module_environment_policy: crate::ModuleEnvironmentPolicy::ExplicitOnly,
        ..Default::default()
    };
    assert!(request.build(&mut session).is_err());
    assert!(session.include_modules.is_empty());
    assert!(session.distribution_metadata.is_none());
    assert!(session.deployment_inputs.is_none());
    assert_eq!(
        session.module_environment_policy,
        crate::ModuleEnvironmentPolicy::ExplicitOnly
    );
    assert!(!tree.0.join("build/app/entry.dist").exists());
}

#[test]
fn explicit_project_parent_navigation_and_root_search_are_valid() {
    let tree = Tree::new();
    tree.config(r#"{"schema_version":1,"mode":"aot","search_paths":["."],"targets":{"app":{"entry":"entry.py"}}}"#);
    let request =
        DeploymentRequest::load(&tree.0.join("config/../ryupy.json"), "app", None).unwrap();
    assert_eq!(request.options.search_paths, vec![tree.0.clone()]);
    request.guard.verify().unwrap();
}

#[cfg(unix)]
#[test]
fn explicit_parent_alias_is_allowed_but_config_link_is_rejected() {
    let tree = Tree::new();
    std::os::unix::fs::symlink(&tree.0, tree.0.join("alias")).unwrap();
    let request = DeploymentRequest::load(&tree.0.join("alias/ryupy.json"), "app", None).unwrap();
    request.guard.verify().unwrap();
    std::os::unix::fs::symlink(tree.0.join("ryupy.json"), tree.0.join("linked.json")).unwrap();
    assert!(DeploymentRequest::load(&tree.0.join("linked.json"), "app", None).is_err());
}

#[cfg(unix)]
#[test]
fn changed_search_root_link_is_rejected_before_build() {
    let tree = Tree::new();
    let request = tree.load().unwrap();
    fs::rename(tree.0.join("package"), tree.0.join("original-package")).unwrap();
    std::os::unix::fs::symlink(tree.0.join("config"), tree.0.join("package")).unwrap();
    assert!(request.guard.verify().is_err());
    assert!(request.build(&mut Session::default()).is_err());
    assert!(!tree.0.join("build").exists());
}
