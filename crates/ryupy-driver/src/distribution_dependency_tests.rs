//! 无条件闭包的冻结文件合同：环、版本、egg权威、身份、域外拒绝与默认不变。
use super::*;
use crate::distribution_metadata::collect_checked;
use std::path::PathBuf;
use std::sync::atomic::{AtomicUsize, Ordering};
static NEXT: AtomicUsize = AtomicUsize::new(0);
struct Tree(PathBuf);
impl Tree {
    fn new() -> Self {
        let path = std::env::temp_dir().join(format!(
            "ryupy-dependency-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        std::fs::create_dir_all(path.join("vendor")).unwrap();
        Self(path)
    }
    fn write(&self, name: &str, bytes: impl AsRef<[u8]>) {
        let path = self.0.join(name);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, bytes).unwrap();
    }
    fn dist(&self, name: &str, version: &str, extra: &str) {
        self.write(
            &format!("vendor/{name}.dist-info/METADATA"),
            format!("Metadata-Version: 2.1\nName: {name}\nVersion: {version}\n{extra}"),
        );
    }
    fn egg(&self, name: &str, version: &str, requirements: &str) {
        self.write(
            &format!("vendor/{name}.egg-info/PKG-INFO"),
            format!("Metadata-Version: 1.1\nName: {name}\nVersion: {version}\n"),
        );
        self.write(
            &format!("vendor/{name}.egg-info/requires.txt"),
            requirements,
        );
    }
    fn request(&self, roots: Option<Vec<&str>>) -> PathBuf {
        let mut value = json!({"schema_version":1,"mode":"closed_snapshot","roots":[{"id":"vendor","path":"vendor"}]});
        if let Some(roots) = roots {
            value["dependency_check"] =
                json!({"mode":"unconditional_closure","requirements":roots});
        }
        self.write("request.json", serde_json::to_vec(&value).unwrap());
        self.0.join("request.json")
    }
    fn error(&self, roots: Vec<&str>) -> String {
        format!(
            "{:#}",
            collect_checked(&self.request(Some(roots))).unwrap_err()
        )
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

#[test]
fn real_egg_dependency_missing_then_installed_and_frozen_report() {
    let tree = Tree::new();
    tree.egg("binaryornot", "0.4.4", "chardet>=3.0.2\n");
    let error = tree.error(vec!["binaryornot==0.4.4"]);
    assert!(
        error.contains("依赖预检")
            && error.contains("缺少分发")
            && error.contains("chardet>=3.0.2"),
        "{error}"
    );
    tree.egg("chardet", "5.2.0", "");
    let (default, none) = collect_checked(&tree.request(None)).unwrap();
    assert!(none.is_none());
    let request = tree.request(Some(vec!["binaryornot==0.4.4"]));
    let (payload, report) = collect_checked(&request).unwrap();
    assert_eq!(default, payload);
    let report = report.unwrap();
    assert_eq!(report["validated_names"], json!(["binaryornot", "chardet"]));
    assert_eq!(report["checks"].as_array().unwrap().len(), 2);
    assert_eq!(
        report["request_sha256"],
        crate::manifest_files::hash_bytes(&std::fs::read(request).unwrap())
    );
    assert_eq!(
        report["payload_sha256"],
        crate::manifest_files::hash_bytes(payload.as_bytes())
    );
}

#[test]
fn reached_closure_and_all_cycle_constraints_are_checked() {
    let tree = Tree::new();
    tree.dist("a", "1.2", "Requires-Dist: b>=2\n");
    tree.dist("b", "2.3", "Requires-Dist: a>=1\n");
    tree.dist(
        "unrelated",
        "not-a-version",
        "Requires-Dist: bad ; os_name == 'nt'\n",
    );
    let (_, report) = collect_checked(&tree.request(Some(vec!["a"]))).unwrap();
    assert_eq!(report.unwrap()["checks"].as_array().unwrap().len(), 3);
    tree.dist("b", "2.3", "Requires-Dist: a>=2\n");
    let error = tree.error(vec!["a"]);
    assert!(error.contains("版本不满足") && error.contains("a>=2"));
}

#[test]
fn pep440_installed_version_contract_is_not_an_index_solver() {
    for (spec, version, success) in [
        ("~=1.4.5", "1.4.9", true),
        ("~=1.4.5", "1.5", false),
        (">=1", "1.1a1", true),
        ("<1.1", "1.1a1", false),
        ("==1.0", "1.0+vendor.1", true),
        (">=1!1.0", "2.0", false),
        ("===01.0", "1.0", false),
    ] {
        let tree = Tree::new();
        tree.dist("a", version, "");
        assert_eq!(
            collect_checked(&tree.request(Some(vec![&format!("a{spec}")]))).is_ok(),
            success,
            "{spec} {version}"
        );
    }
}

#[test]
fn unsupported_active_requirements_fail_without_interpreting_a_target() {
    for requirement in [
        "b[x]>=1",
        "b @ https://example.invalid/b.whl",
        "b; os_name == 'nt'",
        "b; python_version ~= 'broken'",
    ] {
        let tree = Tree::new();
        tree.egg("a", "1", &format!("{requirement}\n"));
        tree.dist("b", "2", "");
        let error = tree.error(vec!["a"]);
        assert!(
            error.contains("依赖预检") && error.contains(requirement),
            "{error}"
        );
        assert!(collect_checked(&tree.request(None)).is_ok());
    }
}

#[test]
fn egg_optional_sections_are_not_base_condition_sections() {
    let tree = Tree::new();
    tree.egg(
        "a",
        "1",
        "b>=1\n[tests]\nmissing[x]\n[other:os_name=='nt']\nnot-installed\n",
    );
    tree.dist("b", "1", "");
    assert!(collect_checked(&tree.request(Some(vec!["a"]))).is_ok());
    tree.egg("a", "1", "[:os_name=='nt']\nnot-installed\n");
    assert!(tree.error(vec!["a"]).contains("base条件段"));
    tree.egg("a", "1", "[]\nb\n");
    assert!(tree.error(vec!["a"]).contains("base条件段"));
}

#[test]
fn folded_headers_and_repeated_dist_requirements_are_preserved() {
    let tree = Tree::new();
    tree.dist(
        "a",
        "1",
        "Requires-Dist: b\n >=1\nRequires-Dist: b<3\n\nRequires-Dist: not-a-real-dependency\n",
    );
    tree.dist("b", "2", "");
    let (_, report) = collect_checked(&tree.request(Some(vec!["a"]))).unwrap();
    assert_eq!(report.unwrap()["checks"].as_array().unwrap().len(), 3);
    tree.dist("a", "1", "Requires-Dist: b\n >=3\n");
    assert!(tree.error(vec!["a"]).contains("版本不满足"));
}

#[test]
fn ambiguous_shadowed_or_misnamed_distribution_is_never_selected() {
    let tree = Tree::new();
    tree.dist("a", "1", "");
    tree.egg("a", "2", "");
    assert!(tree.error(vec!["a"]).contains("身份歧义"));
    std::fs::remove_dir_all(tree.0.join("vendor/a.egg-info")).unwrap();
    tree.write(
        "vendor/a.dist-info/METADATA",
        "Metadata-Version: 2.1\nName: b\nVersion: 1\n",
    );
    assert!(tree.error(vec!["a"]).contains("discovery_key"));
    assert!(tree.error(vec!["b"]).contains("discovery_key"));
    assert!(collect_checked(&tree.request(None)).is_ok());
}

#[test]
fn legacy_dynamic_malformed_and_two_authorities_are_rejected() {
    for extra in [
        "Requires: b\n",
        "Dynamic: Requires-Dist\n",
        "Requires-Dist:\n",
        "Broken header\n",
    ] {
        let tree = Tree::new();
        tree.dist("a", "1", extra);
        assert!(tree.error(vec!["a"]).contains("依赖预检"));
    }
    let tree = Tree::new();
    tree.egg("a", "1", "b\n");
    tree.dist("b", "1", "");
    tree.write(
        "vendor/a.egg-info/PKG-INFO",
        "Metadata-Version: 2.1\nName: a\nVersion: 1\nRequires-Dist: b\n",
    );
    assert!(tree.error(vec!["a"]).contains("权威歧义"));
    tree.write(
        "vendor/a.egg-info/PKG-INFO",
        "Metadata-Version: 2.1\nName: a\nVersion: 1\n",
    );
    tree.write("vendor/a.egg-info/requires.txt", [0xff]);
    assert!(tree.error(vec!["a"]).contains("UTF-8"));
}

#[test]
fn mode_root_and_edge_budgets_are_explicit() {
    let tree = Tree::new();
    tree.dist("a", "1", "");
    assert!(tree.error(vec![]).contains("1到64"));
    assert!(tree.error(vec!["a"; 65]).contains("1到64"));
    let path = tree.request(Some(vec!["a"]));
    let mut request: Value = serde_json::from_slice(&std::fs::read(&path).unwrap()).unwrap();
    request["dependency_check"]["mode"] = "solve".into();
    tree.write("request.json", serde_json::to_vec(&request).unwrap());
    assert!(format!("{:#}", collect_checked(&path).unwrap_err()).contains("unconditional_closure"));
    tree.dist("a", "1", &"Requires-Dist: a\n".repeat(4095));
    assert!(collect_checked(&tree.request(Some(vec!["a"]))).is_ok());
    tree.dist("a", "1", &"Requires-Dist: a\n".repeat(4096));
    assert!(tree.error(vec!["a"]).contains("4096"));
}

#[test]
fn empty_optional_metadata_does_not_override_dist_authority() {
    let tree = Tree::new();
    tree.egg("a", "1", "");
    tree.dist("b", "1", "");
    tree.write(
        "vendor/a.egg-info/PKG-INFO",
        "Metadata-Version: 2.1\nName: a\nVersion: 1\nRequires-Dist: b\n",
    );
    assert!(collect_checked(&tree.request(Some(vec!["a"]))).is_ok());
    assert_eq!(canonical("Some_Pkg.Name").unwrap(), "some-pkg-name");
    assert!(parse("b>=1 ; os_name < 'z'").is_err());
    // 无效版本的失败在所达包，不会把它猜成0或其它可满足版本。
    tree.dist("b", "legacy-version", "");
    assert!(tree.error(vec!["a"]).contains("PEP440"));
}

#[test]
fn metadata_version_must_be_a_single_known_contract() {
    for field in [
        "",
        "Metadata-Version: 9.0\n",
        "Metadata-Version: 2.1\nMetadata-Version: 2.1\n",
    ] {
        let tree = Tree::new();
        tree.write(
            "vendor/a.dist-info/METADATA",
            format!("{field}Name: a\nVersion: 1\n"),
        );
        assert!(tree.error(vec!["a"]).contains("Metadata-Version"));
        assert!(collect_checked(&tree.request(None)).is_ok());
    }
}
