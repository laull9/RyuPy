//! 比较真实输出边界，不用共同父目录的字符串前缀误拒兄弟资源。
use super::*;
use std::fs;
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Root(PathBuf);
impl Root {
    fn new() -> Self {
        let root = std::env::temp_dir().join(format!(
            "ryupy-resource-output-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        fs::create_dir(&root).unwrap();
        Self(root)
    }
    fn dir(&self, name: &str) -> PathBuf {
        let path = self.0.join(name);
        fs::create_dir_all(&path).unwrap();
        path
    }
}
impl Drop for Root {
    fn drop(&mut self) {
        let _ = fs::remove_dir_all(&self.0);
    }
}
#[test]
fn same_ancestor_descendant_rejected_but_siblings_allowed() {
    let root = Root::new();
    let build = root.dir("app.build");
    let dist = root.0.join("app.dist"); // 首次不存在的dist也必须检查。
    let outputs = [build.clone(), dist];
    root.dir("app.build/nested");
    for directory in [
        ".",
        "app.build",
        "app.build/nested",
        "app.dist",
        "app.dist/missing",
    ] {
        let error = reject_overlap(&root.0, directory, &outputs).unwrap_err();
        assert!(
            format!("{error:#}").contains("生成输出重叠"),
            "{directory}: {error:#}"
        );
    }
    root.dir("assets");
    root.dir("app.build-other");
    assert!(reject_overlap(&root.0, "assets", &outputs).is_ok());
    assert!(reject_overlap(&root.0, "app.build-other", &outputs).is_ok());
    assert!(reject_overlap(&root.0.join("assets"), ".", &outputs).is_ok());
}
#[test]
fn all_selectors_preflight_before_any_expansion() {
    let root = Root::new();
    let source_path = root.0.join("app.py");
    fs::write(&source_path, "print(1)\n").unwrap();
    let source = crate::modules::load_single(&mut crate::Session::default(), &source_path).unwrap();
    // 第一个目录缺失，本应在展开时报错；第二个相交必须先截获，证明统一预检。
    let request = root.0.join("resources.json");
    fs::write(&request, serde_json::to_vec(&serde_json::json!({"schema_version":1,"modules":[{"module":source.name,"trees":[{"directory":"missing","suffixes":[".json"]},{"directory":".","suffixes":[".json"]}]}]})).unwrap()).unwrap();
    let outputs = [root.dir("app.build"), root.0.join("app.dist")];
    let error = crate::resources::ResourcePlan::read_context(
        crate::resources::ResourceRequestContext {
            path: &request,
            output_roots: &outputs,
        },
        &[source],
    )
    .err()
    .unwrap();
    assert!(format!("{error:#}").contains("生成输出重叠"), "{error:#}");
}
#[cfg(unix)]
#[test]
fn aliases_resolve_and_declared_symlink_is_not_followed() {
    let root = Root::new();
    let build = root.dir("app.build");
    std::os::unix::fs::symlink(&build, root.0.join("link")).unwrap();
    assert!(reject_overlap(&root.0, "link", std::slice::from_ref(&build)).is_err());
    std::os::unix::fs::symlink(&root.0, root.0.join("alias")).unwrap();
    let alias_parent = root.0.join("alias/app.build");
    assert!(reject_overlap(&alias_parent, ".", &[build]).is_err());
}
#[cfg(windows)]
#[test]
fn windows_component_case_is_conservative_not_string_prefix() {
    assert!(
        intersects(
            Path::new(r"C:\\Root\\App.Build"),
            Path::new(r"c:\\root\\APP.BUILD\\attempt")
        )
        .unwrap()
    );
    assert!(
        !intersects(
            Path::new(r"C:\\Root\\App.Build"),
            Path::new(r"C:\\Root\\App.Build-other")
        )
        .unwrap()
    );
}
