//! 真实文件树验证，展开结果必须经过同一个资源冻结入口。
use super::*;
use crate::resources::ResourcePlan;
use std::fs;
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Temp(PathBuf);
impl Temp {
    fn new() -> Self {
        let root = std::env::temp_dir().join(format!(
            "ryupy-tree-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        fs::create_dir(&root).unwrap();
        Self(root)
    }
    fn write(&self, name: &str) {
        let path = self.0.join(name);
        fs::create_dir_all(path.parent().unwrap()).unwrap();
        fs::write(path, b"pass\n").unwrap();
    }
    fn plan(&self, directory: &str) -> Result<ResourcePlan> {
        self.write("module.py");
        let source =
            crate::modules::load_single(&mut crate::Session::default(), &self.0.join("module.py"))?;
        let request = self.0.join("request.json");
        fs::write(
            &request,
            serde_json::to_vec(
                &serde_json::json!({"schema_version":1,"modules":[{"module":source.name,"trees":[{"directory":directory,"suffixes":[".flf",".flc"]}]}]}),
            )?,
        )?;
        ResourcePlan::read(&request, &[source])
    }
}
impl Drop for Temp {
    fn drop(&mut self) {
        let _ = fs::remove_dir_all(&self.0);
    }
}
fn request(directory: &str, suffixes: &[&str]) -> TreeRequest {
    TreeRequest {
        directory: directory.into(),
        suffixes: suffixes.iter().map(|s| s.to_string()).collect(),
    }
}
fn selected(temp: &Temp, directory: &str, suffixes: &[&str]) -> Result<Vec<String>> {
    let mut budget = SCAN_LIMIT;
    expand(&temp.0, &request(directory, suffixes), &mut budget)
}
#[test]
fn deterministic_recursive_selection_and_dot_root() {
    let temp = Temp::new();
    for file in [
        "fonts/z.flf",
        "fonts/nested/a.flc",
        "fonts/no.txt",
        "fonts/no.py",
        "root.flf",
    ] {
        temp.write(file);
    }
    assert_eq!(
        selected(&temp, "fonts", &[".flf", ".flc"]).unwrap(),
        ["fonts/nested/a.flc", "fonts/z.flf"]
    );
    assert_eq!(
        selected(&temp, ".", &[".flf"]).unwrap(),
        ["fonts/z.flf", "root.flf"]
    );
    let plan = temp.plan("fonts").unwrap();
    let explicit_files = plan
        .claims
        .values()
        .flat_map(|claims| claims.iter().map(|c| c.file.as_str()))
        .collect::<Vec<_>>();
    assert_eq!(explicit_files, ["fonts/nested/a.flc", "fonts/z.flf"]);
    fs::create_dir(temp.0.join("owned")).unwrap();
    assert_eq!(plan.stage(&temp.0.join("owned")).unwrap().records.len(), 2);
}
#[test]
fn malformed_empty_and_python_selectors_fail() {
    let temp = Temp::new();
    temp.write("fonts/a.flf");
    for directory in ["../fonts", "/fonts", "./fonts", "fonts/..", "missing"] {
        assert!(
            selected(&temp, directory, &[".flf"]).is_err(),
            "{directory}"
        );
    }
    for suffixes in [
        vec![],
        vec![""],
        vec!["."],
        vec!["*.flf"],
        vec![".f*"],
        vec![".é"],
        vec![".py"],
        vec![".PYc"],
        vec![".x.pyo"],
        vec![".flf", ".flf"],
        vec![".absent"],
    ] {
        assert!(selected(&temp, "fonts", &suffixes).is_err(), "{suffixes:?}");
    }
}
#[test]
fn matching_inventory_change_rejected_including_automatic_composition() {
    let temp = Temp::new();
    temp.write("fonts/a.flf");
    let plan = temp.plan("fonts").unwrap();
    let source =
        crate::modules::load_single(&mut crate::Session::default(), &temp.0.join("module.py"))
            .unwrap();
    let plan = ResourcePlan::compose_automatic(&[source], Some(plan)).unwrap();
    temp.write("fonts/new.flf");
    fs::create_dir(temp.0.join("owned")).unwrap();
    assert!(plan.stage(&temp.0.join("owned")).is_err());
    fs::remove_file(temp.0.join("fonts/new.flf")).unwrap();
    temp.write("fonts/unselected.txt");
    assert!(plan.verify_selections().is_ok());
    fs::remove_file(temp.0.join("fonts/a.flf")).unwrap();
    assert!(plan.verify_selections().is_err());
}
#[test]
fn original_source_identity_guard_still_rejects_disguised_hardlink() {
    let temp = Temp::new();
    temp.write("module.py");
    fs::create_dir(temp.0.join("fonts")).unwrap();
    fs::hard_link(temp.0.join("module.py"), temp.0.join("fonts/alias.flf")).unwrap();
    let plan = temp.plan("fonts").unwrap();
    fs::create_dir(temp.0.join("owned")).unwrap();
    assert!(plan.stage(&temp.0.join("owned")).is_err());
}
#[test]
fn budget_depth_and_nonportable_entries_rejected() {
    let temp = Temp::new();
    temp.write("fonts/a.flf");
    assert!(expand(&temp.0, &request("fonts", &[".flf"]), &mut 0).is_err());
    temp.write("fonts/CON.txt");
    assert!(selected(&temp, "fonts", &[".flf"]).is_err());
    fs::remove_file(temp.0.join("fonts/CON.txt")).unwrap();
    let deep = format!("fonts/{}a.flf", "d/".repeat(65));
    temp.write(&deep);
    assert!(selected(&temp, "fonts", &[".flf"]).is_err());
}
#[cfg(unix)]
#[test]
fn links_and_special_files_rejected_even_when_unselected() {
    use std::os::unix::fs::symlink;
    let temp = Temp::new();
    temp.write("fonts/a.flf");
    symlink("a.flf", temp.0.join("fonts/ignored.txt")).unwrap();
    assert!(selected(&temp, "fonts", &[".flf"]).is_err());
    fs::remove_file(temp.0.join("fonts/ignored.txt")).unwrap();
    assert!(
        std::process::Command::new("mkfifo")
            .arg(temp.0.join("fonts/pipe"))
            .status()
            .unwrap()
            .success()
    );
    assert!(selected(&temp, "fonts", &[".flf"]).is_err());
}

#[test]
fn file_count_limit_is_enforced_and_suffixes_are_literal() {
    let temp = Temp::new();
    temp.write("fonts/a.FLF");
    assert!(selected(&temp, "fonts", &[".flf"]).is_err());
    assert_eq!(
        selected(&temp, "fonts", &[".FLF"]).unwrap(),
        ["fonts/a.FLF"]
    );
    for index in 0..1025 {
        temp.write(&format!("many/{index}.flf"));
    }
    assert!(selected(&temp, "many", &[".flf"]).is_err());
}

#[test]
fn explicit_files_overlap_deduplicates_and_unknown_module_does_not_expand_graph() {
    let temp = Temp::new();
    temp.write("fonts/a.flf");
    temp.write("module.py");
    let source =
        crate::modules::load_single(&mut crate::Session::default(), &temp.0.join("module.py"))
            .unwrap();
    let path = temp.0.join("request.json");
    let mut entry = serde_json::json!({"module":source.name,"files":["fonts/a.flf"],"trees":[{"directory":"fonts","suffixes":[".flf"]}]});
    fs::write(
        &path,
        serde_json::to_vec(&serde_json::json!({"schema_version":1,"modules":[entry]})).unwrap(),
    )
    .unwrap();
    let plan = ResourcePlan::read(&path, std::slice::from_ref(&source)).unwrap();
    assert_eq!(plan.claims.len(), 1);
    assert_eq!(plan.claims.values().next().unwrap().len(), 1);
    entry["module"] = "uncompiled".into();
    fs::write(
        &path,
        serde_json::to_vec(&serde_json::json!({"schema_version":1,"modules":[entry]})).unwrap(),
    )
    .unwrap();
    assert!(ResourcePlan::read(&path, &[source]).is_err());
}
