//! 使用合成的最小合法清单验证离线接口，不将普通字节误称为已认证原生程序。
use crate::verify_distribution;
use serde_json::{Value, json};
use std::path::{Path, PathBuf};

struct Case(PathBuf);
impl Case {
    fn new() -> Self {
        let root = crate::test_temp::directory("ryupy-distribution-verify");
        std::fs::write(root.join("app"), b"not a real native executable").unwrap();
        let case = Self(root);
        case.save(&json!({
            "kind": "ryupy-native-aot-artifact", "schema_version": 1,
            "artifacts": [case.artifact("app", "native_executable")]
        }));
        case
    }
    fn artifact(&self, path: &str, role: &str) -> Value {
        let bytes = std::fs::read(self.0.join(path)).unwrap();
        json!({"relative_path": path, "role": role, "size": bytes.len(),
            "sha256": crate::manifest_files::hash_bytes(&bytes)})
    }
    fn value(&self) -> Value {
        serde_json::from_slice(&std::fs::read(self.0.join("ryupy-artifact.json")).unwrap()).unwrap()
    }
    fn save(&self, value: &Value) {
        std::fs::write(
            self.0.join("ryupy-artifact.json"),
            serde_json::to_vec(value).unwrap(),
        )
        .unwrap();
    }
}
impl Drop for Case {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

#[test]
fn distribution_verify_minimal_summary_and_relocation() {
    let mut case = Case::new();
    let first = verify_distribution(&case.0).unwrap();
    assert_eq!(first.report_schema_version, 1);
    assert_eq!(first.kind, "ryupy-distribution-integrity-verification");
    assert!(first.content_integrity_verified);
    assert_eq!(first.manifest_schema_version, 1);
    assert_eq!(first.native_executable, "app");
    assert_eq!(first.artifact_count, 1);
    assert_eq!(first.total_bytes, 28);
    assert_eq!(first.reported_target_rid, None);
    assert_eq!(first.authenticity, "not_checked");
    assert_eq!(first.runtime_compatibility, "not_checked");
    assert_eq!(first.schema_semantics, "not_checked");
    let moved = case.0.with_extension("moved");
    std::fs::rename(&case.0, &moved).unwrap();
    case.0 = moved;
    assert_eq!(first, verify_distribution(&case.0).unwrap());
}

#[test]
fn distribution_verify_reported_rid_is_optional_untrusted_metadata() {
    let case = Case::new();
    let mut value = case.value();
    value["target"] = json!({"rid": "not-a-validated-platform", "unrelated": true});
    value["unrelated"] = json!({"credential": "do-not-copy"});
    case.save(&value);
    let summary = verify_distribution(&case.0).unwrap();
    assert_eq!(
        summary.reported_target_rid.as_deref(),
        Some("not-a-validated-platform")
    );
    let output = serde_json::to_string(&summary).unwrap();
    assert!(!output.contains("credential") && !output.contains("do-not-copy"));
    value["target"]["rid"] = json!(123);
    case.save(&value);
    assert_eq!(
        verify_distribution(&case.0).unwrap().reported_target_rid,
        None
    );
}

#[test]
fn distribution_verify_keeps_versions_one_through_nine_integrity_only() {
    let case = Case::new();
    let mut value = case.value();
    for version in 1..=9 {
        value["schema_version"] = json!(version);
        case.save(&value);
        assert_eq!(
            verify_distribution(&case.0)
                .unwrap()
                .manifest_schema_version,
            version
        );
    }
    // 底座未要求这些最小清单具有完整模块/ABI语义，本接口不得偷偷收紧或冒称已验证。
    for version in [json!(0), json!(10), json!("1"), Value::Null] {
        value["schema_version"] = version;
        case.save(&value);
        assert!(verify_distribution(&case.0).is_err());
    }
}

#[test]
fn distribution_verify_rejects_modified_missing_extra_files() {
    for operation in 0..3 {
        let case = Case::new();
        match operation {
            0 => std::fs::write(case.0.join("app"), b"corrupt").unwrap(),
            1 => std::fs::remove_file(case.0.join("app")).unwrap(),
            _ => std::fs::write(case.0.join("extra"), b"extra").unwrap(),
        }
        assert!(verify_distribution(&case.0).is_err());
    }
}

#[test]
fn distribution_verify_rejects_duplicate_roles_paths_and_self_reference() {
    let case = Case::new();
    std::fs::write(case.0.join("second"), b"second").unwrap();
    let mut value = case.value();
    value["artifacts"]
        .as_array_mut()
        .unwrap()
        .push(case.artifact("second", "native_executable"));
    case.save(&value);
    assert!(verify_distribution(&case.0).is_err());
    std::fs::remove_file(case.0.join("second")).unwrap();
    value["artifacts"][1] = value["artifacts"][0].clone();
    case.save(&value);
    assert!(verify_distribution(&case.0).is_err());
    value["artifacts"][1]["relative_path"] = json!("ryupy-artifact.json");
    case.save(&value);
    assert!(verify_distribution(&case.0).is_err());
}

#[test]
fn distribution_verify_rejects_unsafe_paths_and_non_aot_kind() {
    let case = Case::new();
    let original = case.value();
    for path in [
        "../app", "/app", "C:app", "x\\app", "./app", "a//b", "", "a\nb",
    ] {
        let mut value = original.clone();
        value["artifacts"][0]["relative_path"] = json!(path);
        case.save(&value);
        assert!(verify_distribution(&case.0).is_err(), "{path:?}");
    }
    let mut value = original;
    value["kind"] = json!("ryupy-jit-artifact");
    case.save(&value);
    assert!(verify_distribution(&case.0).is_err());
}

#[test]
fn distribution_verify_allows_empty_directories_without_counting_them() {
    let case = Case::new();
    let before = verify_distribution(&case.0).unwrap();
    std::fs::create_dir_all(case.0.join("empty/nested")).unwrap();
    assert_eq!(before, verify_distribution(&case.0).unwrap());
}

#[cfg(unix)]
#[test]
fn distribution_verify_rejects_root_manifest_and_artifact_symlinks() {
    use std::os::unix::fs::symlink;
    let case = Case::new();
    let link = case.0.with_extension("link");
    symlink(&case.0, &link).unwrap();
    assert!(verify_distribution(&link).is_err());
    std::fs::remove_file(link).unwrap();
    for name in ["app", "ryupy-artifact.json"] {
        let moved = case.0.with_extension("outside");
        std::fs::rename(case.0.join(name), &moved).unwrap();
        symlink(&moved, case.0.join(name)).unwrap();
        assert!(verify_distribution(&case.0).is_err());
        std::fs::remove_file(case.0.join(name)).unwrap();
        std::fs::rename(&moved, case.0.join(name)).unwrap();
    }
}

#[cfg(unix)]
#[test]
fn distribution_verify_readonly_tree_is_not_changed() {
    use std::os::unix::fs::PermissionsExt;
    let case = Case::new();
    let manifest = std::fs::read(case.0.join("ryupy-artifact.json")).unwrap();
    let app = std::fs::read(case.0.join("app")).unwrap();
    for name in ["app", "ryupy-artifact.json"] {
        std::fs::set_permissions(case.0.join(name), std::fs::Permissions::from_mode(0o444))
            .unwrap();
    }
    std::fs::set_permissions(&case.0, std::fs::Permissions::from_mode(0o555)).unwrap();
    let result = verify_distribution(&case.0);
    assert_eq!(std::fs::read(case.0.join("app")).unwrap(), app);
    assert_eq!(
        std::fs::read(case.0.join("ryupy-artifact.json")).unwrap(),
        manifest
    );
    assert_eq!(std::fs::read_dir(&case.0).unwrap().count(), 2);
    assert_eq!(
        std::fs::metadata(&case.0).unwrap().permissions().mode() & 0o777,
        0o555
    );
    std::fs::set_permissions(&case.0, std::fs::Permissions::from_mode(0o755)).unwrap();
    result.unwrap();
}

#[test]
fn distribution_verify_resource_versions_keep_portable_name_rejection() {
    let case = Case::new();
    // safe_relative允许Unicode，但资源版本对非native项有额外ASCII策略；锁住真实分支。
    std::fs::write(case.0.join("数据.txt"), b"resource").unwrap();
    let mut value = case.value();
    value["artifacts"]
        .as_array_mut()
        .unwrap()
        .push(case.artifact("数据.txt", "declared_resource"));
    case.save(&value);
    verify_distribution(&case.0).unwrap();
    for version in [2, 4, 5, 6, 8, 9] {
        value["schema_version"] = json!(version);
        case.save(&value);
        let error = verify_distribution(&case.0).unwrap_err();
        assert!(error.to_string().contains("ASCII"), "{version}: {error:#}");
    }
}

#[test]
fn distribution_verify_rejects_zero_native_and_wrong_digest_field_types() {
    let case = Case::new();
    let original = case.value();
    let mut value = original.clone();
    value["artifacts"][0]["role"] = json!("unclassified_sidecar");
    case.save(&value);
    assert!(verify_distribution(&case.0).is_err());
    for size in [json!("28"), json!(-1), json!(28.0), json!(null)] {
        let mut value = original.clone();
        value["artifacts"][0]["size"] = size;
        case.save(&value);
        assert!(verify_distribution(&case.0).is_err());
    }
    for hash in [json!(0), json!(true), json!([]), json!(null)] {
        let mut value = original.clone();
        value["artifacts"][0]["sha256"] = hash;
        case.save(&value);
        assert!(verify_distribution(&case.0).is_err());
    }
}

#[test]
fn distribution_verify_offline_child() {
    // 父测试单独启动本测试进程，避免并行测试修改全局环境。
    if let Some(root) = std::env::var_os("RYUPY_VERIFY_OFFLINE_CASE") {
        verify_distribution(Path::new(&root)).unwrap();
    }
}

#[test]
fn distribution_verify_needs_no_dotnet_python_or_path_tools() {
    let case = Case::new();
    let result = std::process::Command::new(std::env::current_exe().unwrap())
        .args([
            "--exact",
            "distribution_verify_tests::distribution_verify_offline_child",
            "--nocapture",
        ])
        .env("RYUPY_VERIFY_OFFLINE_CASE", &case.0)
        .env("DOTNET", case.0.join("missing-dotnet"))
        .env("PYTHON", case.0.join("missing-python"))
        .env("PATH", case.0.join("missing-tools"))
        .output()
        .unwrap();
    assert!(
        result.status.success(),
        "{}",
        String::from_utf8_lossy(&result.stderr)
    );
    let stdout = String::from_utf8(result.stdout).unwrap();
    assert!(
        stdout.lines().any(|line| line == "running 1 test"),
        "{stdout}"
    );
    assert!(
        stdout
            .lines()
            .any(|line| line.starts_with("test result: ok. 1 passed; 0 failed; 0 ignored;")),
        "{stdout}"
    );
}

#[test]
fn distribution_verify_rejects_file_root_before_manifest_access() {
    let case = Case::new();
    let error = verify_distribution(&case.0.join("app")).unwrap_err();
    assert!(error.to_string().contains("验包根不是普通目录"));
}

#[cfg(unix)]
#[test]
fn distribution_verify_rejects_link_root_before_invalid_json_read() {
    let case = Case::new();
    let destination = case.0.join("invalid-target");
    std::fs::create_dir(&destination).unwrap();
    std::fs::write(destination.join("ryupy-artifact.json"), b"invalid-json").unwrap();
    let link = case.0.join("linked-root");
    std::os::unix::fs::symlink(&destination, &link).unwrap();
    let error = verify_distribution(&link).unwrap_err();
    assert!(error.to_string().contains("清单拒绝符号链接"), "{error:#}");
}
