//! 资源冻结与发布安全专测：仅普通临时文件，不调用 SDK，不以源码路径或内容相同冒充文件身份。
use super::*;
use std::fs;
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);
pub(super) struct Temp(pub(super) PathBuf);
impl Temp {
    pub(super) fn new() -> Self {
        let root = std::env::temp_dir().join(format!(
            "ryupy-stage-{}-{}",
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
    pub(super) fn source(&self, folder: &str, name: &str, logical: &str) -> SourceModule {
        let directory = self.dir(folder);
        let path = directory.join(format!("{name}.py"));
        fs::write(&path, "pass\n").unwrap();
        let mut source =
            crate::modules::load_single(&mut crate::Session::default(), &path).unwrap();
        source.name = name.to_owned();
        source.logical_source = logical.to_owned();
        source
    }
    pub(super) fn plan(
        &self,
        sources: &[SourceModule],
        entries: serde_json::Value,
    ) -> Result<ResourcePlan> {
        let request = self.0.join("request.json");
        fs::write(
            &request,
            serde_json::to_vec(&serde_json::json!({"schema_version":1,"modules":entries})).unwrap(),
        )
        .unwrap();
        ResourcePlan::read(&request, sources)
    }
    pub(super) fn owned(&self) -> PathBuf {
        self.dir("owned")
    }
    fn publish(&self) -> PathBuf {
        let root = self.dir("publish");
        fs::write(root.join("old-sentinel"), b"must stay").unwrap();
        root
    }
}
impl Drop for Temp {
    fn drop(&mut self) {
        let _ = fs::remove_dir_all(&self.0);
    }
}
pub(super) fn request(name: &str, files: &[&str]) -> serde_json::Value {
    serde_json::json!([{"module":name,"files":files}])
}
fn assert_sentinel(root: &Path) {
    assert_eq!(fs::read(root.join("old-sentinel")).unwrap(), b"must stay");
}
fn regular_files(root: &Path, output: &mut Vec<PathBuf>) {
    for entry in fs::read_dir(root).unwrap() {
        let path = entry.unwrap().path();
        let metadata = fs::symlink_metadata(&path).unwrap();
        if metadata.is_dir() {
            regular_files(&path, output);
        } else if metadata.is_file() {
            output.push(path);
        }
    }
}

#[test]
fn shared_lexical_source_freezes_once_for_two_consumer_modules() {
    let temp = Temp::new();
    let sources = vec![
        temp.source("source", "english", "modules/pkg/english.py"),
        temp.source("source", "french", "modules/pkg/french.py"),
    ];
    fs::write(temp.0.join("source/data.txt"), b"frozen").unwrap();
    let plan = temp
        .plan(
            &sources,
            serde_json::json!([
                {"module":"english","files":["data.txt","data.txt"]},
                {"module":"french","files":["data.txt"]}
            ]),
        )
        .unwrap();
    let staged = plan.stage(&temp.owned()).unwrap();
    assert_eq!(staged.records.len(), 1);
    assert_eq!(staged.records[0].consumer_module_ids, vec![0, 1]);
    assert_eq!(staged.records[0].size, 6);
    let publish = temp.publish();
    staged.publish(&publish).unwrap();
    assert_eq!(
        fs::read(publish.join("_internal/resources/v1/modules/pkg/data.txt")).unwrap(),
        b"frozen"
    );
    assert_sentinel(&publish);
}

#[test]
fn identical_bytes_from_distinct_sources_do_not_authorize_same_target() {
    let temp = Temp::new();
    let sources = vec![
        temp.source("one", "first", "modules/pkg/first.py"),
        temp.source("two", "second", "modules/pkg/second.py"),
    ];
    for folder in ["one", "two"] {
        fs::write(temp.0.join(folder).join("data.txt"), b"same").unwrap();
    }
    let plan = temp
        .plan(
            &sources,
            serde_json::json!([
                {"module":"first","files":["data.txt"]},{"module":"second","files":["data.txt"]}
            ]),
        )
        .unwrap();
    let publish = temp.publish();
    assert!(plan.stage(&temp.owned()).is_err());
    assert_sentinel(&publish);
    assert_eq!(fs::read_dir(&publish).unwrap().count(), 1);
}

#[test]
fn compiled_source_hardlink_cannot_be_smuggled_as_data() {
    let temp = Temp::new();
    let sources = vec![temp.source("source", "main", "entry/main.py")];
    fs::hard_link(
        &sources[0].resolved_source_path,
        temp.0.join("source/alias.dat"),
    )
    .unwrap();
    let plan = temp
        .plan(&sources, request("main", &["alias.dat"]))
        .unwrap();
    assert!(plan.stage(&temp.owned()).is_err());
}

#[test]
fn later_source_mutation_does_not_replace_successfully_frozen_bytes() {
    let temp = Temp::new();
    let sources = vec![temp.source("中文 source &", "main", "entry/main.py")];
    let source = temp.0.join("中文 source &/data.txt");
    fs::write(&source, b"old bytes").unwrap();
    let plan = temp.plan(&sources, request("main", &["data.txt"])).unwrap();
    let staged = plan.stage(&temp.owned()).unwrap();
    fs::write(&source, b"new bytes").unwrap();
    drop(plan);
    drop(sources);
    let publish = temp.dir("中文 publish &");
    staged.publish(&publish).unwrap();
    assert_eq!(
        fs::read(publish.join("_internal/resources/v1/entry/data.txt")).unwrap(),
        b"old bytes"
    );
    assert_eq!(
        staged.records[0].sha256,
        crate::manifest_files::hash_bytes(b"old bytes")
    );
}

#[test]
fn tampered_staging_is_rejected_instead_of_refreezing_source() {
    let temp = Temp::new();
    let sources = vec![temp.source("source", "main", "entry/main.py")];
    fs::write(temp.0.join("source/data.txt"), b"original").unwrap();
    let plan = temp.plan(&sources, request("main", &["data.txt"])).unwrap();
    let owned = temp.owned();
    let staged = plan.stage(&owned).unwrap();
    let mut files = Vec::new();
    regular_files(&owned, &mut files);
    let snapshots: Vec<_> = files
        .into_iter()
        .filter(|p| fs::read(p).unwrap() == b"original")
        .collect();
    assert_eq!(
        snapshots.len(),
        1,
        "one actual frozen resource must be retained under owned root"
    );
    fs::write(&snapshots[0], b"tampered").unwrap();
    let publish = temp.publish();
    assert!(staged.publish(&publish).is_err());
    assert_sentinel(&publish);
}

#[test]
fn existing_publish_target_is_not_overwritten() {
    let temp = Temp::new();
    let sources = vec![temp.source("source", "main", "entry/main.py")];
    fs::write(temp.0.join("source/data.txt"), b"new").unwrap();
    let staged = temp
        .plan(&sources, request("main", &["data.txt"]))
        .unwrap()
        .stage(&temp.owned())
        .unwrap();
    let publish = temp.publish();
    let parent = publish.join("_internal/resources/v1/entry");
    fs::create_dir_all(&parent).unwrap();
    fs::write(parent.join("data.txt"), b"old").unwrap();
    assert!(staged.publish(&publish).is_err());
    assert_eq!(fs::read(parent.join("data.txt")).unwrap(), b"old");
    assert_sentinel(&publish);
}

#[test]
fn nonascii_selected_anchor_and_resource_are_rejected_not_renamed() {
    let temp = Temp::new();
    let mut sources = vec![temp.source("source", "main", "entry/é.py")];
    assert!(temp.plan(&sources, request("main", &[])).is_err());
    sources[0].logical_source = "entry/main.py".to_owned();
    for name in ["é.txt", "e\u{301}.txt", "\u{345}.txt"] {
        assert!(temp.plan(&sources, request("main", &[name])).is_err());
    }
}

#[test]
fn oversized_file_is_rejected_from_sparse_length_without_large_payload_write() {
    let temp = Temp::new();
    let sources = vec![temp.source("source", "main", "entry/main.py")];
    fs::File::create(temp.0.join("source/large.dat"))
        .unwrap()
        .set_len(FILE_LIMIT + 1)
        .unwrap();
    let plan = temp
        .plan(&sources, request("main", &["large.dat"]))
        .unwrap();
    assert!(plan.stage(&temp.owned()).is_err());
}

#[test]
fn selection_and_request_size_budgets_fail_before_staging() {
    let temp = Temp::new();
    let sources = vec![temp.source("source", "main", "entry/main.py")];
    let names: Vec<_> = (0..1025).map(|n| format!("file{n}.dat")).collect();
    assert!(
        temp.plan(
            &sources,
            serde_json::json!([{"module":"main","files":names}])
        )
        .is_err()
    );
    let too_many: Vec<_> = (0..257)
        .map(|n| serde_json::json!({"module":format!("module{n}"),"files":[]}))
        .collect();
    assert!(
        temp.plan(&sources, serde_json::Value::Array(too_many))
            .is_err()
    );
    let oversized = temp.0.join("oversized.json");
    fs::File::create(&oversized)
        .unwrap()
        .set_len(1024 * 1024 + 1)
        .unwrap();
    assert!(ResourcePlan::read(&oversized, &sources).is_err());
}

#[cfg(unix)]
#[test]
fn source_leaf_and_ancestor_symlinks_fail_closed() {
    use std::os::unix::fs::symlink;
    let temp = Temp::new();
    let sources = vec![temp.source("source", "main", "entry/main.py")];
    let outside = temp.dir("outside");
    fs::write(outside.join("data.txt"), b"outside").unwrap();
    symlink(outside.join("data.txt"), temp.0.join("source/link.dat")).unwrap();
    symlink(&outside, temp.0.join("source/linkdir")).unwrap();
    for relative in ["link.dat", "linkdir/data.txt"] {
        let plan = temp.plan(&sources, request("main", &[relative])).unwrap();
        // 每例独占 owned 根，不让上一失败的残留决定下一断言。
        assert!(
            plan.stage(&temp.dir(&format!("owned-{}", relative.len())))
                .is_err()
        );
    }
}

#[cfg(unix)]
#[test]
fn publish_symlink_ancestor_does_not_write_outside_owned_directory() {
    use std::os::unix::fs::symlink;
    let temp = Temp::new();
    let sources = vec![temp.source("source", "main", "entry/main.py")];
    fs::write(temp.0.join("source/data.txt"), b"frozen").unwrap();
    let staged = temp
        .plan(&sources, request("main", &["data.txt"]))
        .unwrap()
        .stage(&temp.owned())
        .unwrap();
    let publish = temp.publish();
    let outside = temp.dir("outside");
    symlink(&outside, publish.join("_internal")).unwrap();
    assert!(staged.publish(&publish).is_err());
    assert_eq!(fs::read_dir(&outside).unwrap().count(), 0);
    assert_sentinel(&publish);
}

#[test]
fn total_budget_boundary_and_arithmetic_overflow_are_pure_checks() {
    assert_eq!(
        stage::checked_total(TOTAL_LIMIT - 1, 1).unwrap(),
        TOTAL_LIMIT
    );
    assert_eq!(stage::checked_total(0, 0).unwrap(), 0);
    assert!(stage::checked_total(TOTAL_LIMIT, 1).is_err());
    assert!(stage::checked_total(u64::MAX, 1).is_err());
}

#[test]
fn occupied_staging_directory_is_never_reused_or_cleaned() {
    let temp = Temp::new();
    let sources = vec![temp.source("source", "main", "entry/main.py")];
    fs::write(temp.0.join("source/data.txt"), b"fresh").unwrap();
    let plan = temp.plan(&sources, request("main", &["data.txt"])).unwrap();
    let owned = temp.owned();
    let stale = owned.join("resource-staging");
    fs::create_dir(&stale).unwrap();
    fs::write(stale.join("sentinel"), b"prior owner").unwrap();
    assert!(plan.stage(&owned).is_err());
    assert_eq!(fs::read(stale.join("sentinel")).unwrap(), b"prior owner");
}

#[cfg(any(unix, windows))]
#[test]
fn same_target_different_lexical_hardlink_alias_obeys_platform_contract() {
    let temp = Temp::new();
    let sources = vec![
        temp.source("one", "first", "modules/pkg/first.py"),
        temp.source("two", "second", "modules/pkg/second.py"),
    ];
    fs::write(temp.0.join("one/data.txt"), b"linked").unwrap();
    fs::hard_link(temp.0.join("one/data.txt"), temp.0.join("two/data.txt")).unwrap();
    let plan = temp
        .plan(
            &sources,
            serde_json::json!([
                {"module":"first","files":["data.txt"]},{"module":"second","files":["data.txt"]}
            ]),
        )
        .unwrap();
    let result = plan.stage(&temp.owned());
    #[cfg(windows)]
    assert!(
        result.is_err(),
        "Windows must not accept another lexical source via a 64-bit identity match"
    );
    #[cfg(unix)]
    {
        let staged = result.unwrap();
        assert_eq!(staged.records.len(), 1);
        assert_eq!(staged.records[0].consumer_module_ids, vec![0, 1]);
    }
}

#[test]
fn location_only_request_has_schema_two_without_claiming_resource_bytes() {
    let temp = Temp::new();
    let sources = vec![temp.source("source", "main", "entry/main.py")];
    let staged = temp
        .plan(&sources, request("main", &[]))
        .unwrap()
        .stage(&temp.owned())
        .unwrap();
    let publish = temp.publish();
    staged.publish(&publish).unwrap();
    assert_sentinel(&publish);
    assert_eq!(fs::read_dir(&publish).unwrap().count(), 1);
    let original = serde_json::json!({"schema_version":1,"scope":{
        "resources_included":false,"dynamic_dependency_closure_complete":false,
        "native_retention_proven":false,"manifest_self_hash_included":false
    }});
    let bytes = extend_manifest(&serde_json::to_vec(&original).unwrap(), &staged).unwrap();
    let value: serde_json::Value = serde_json::from_slice(&bytes).unwrap();
    assert_eq!(value["schema_version"], 2);
    assert_eq!(value["module_locations"].as_array().unwrap().len(), 1);
    assert_eq!(
        value["module_locations"][0]["relative_file"],
        "_internal/resources/v1/entry/main.py"
    );
    assert_eq!(value["resources"], serde_json::json!([]));
    assert_eq!(value["scope"]["resources_included"], false);
    assert_eq!(value["scope"]["declared_resources_complete"], true);
    for key in [
        "dynamic_dependency_closure_complete",
        "native_retention_proven",
        "manifest_self_hash_included",
    ] {
        assert_eq!(value["scope"][key], false);
    }
}

#[test]
fn published_resource_artifact_hash_and_schema_consumer_ids_agree() {
    let temp = Temp::new();
    let sources = vec![
        temp.source("source", "first", "modules/pkg/first.py"),
        temp.source("source", "second", "modules/pkg/second.py"),
    ];
    fs::write(temp.0.join("source/data.txt"), b"actual data").unwrap();
    let staged = temp
        .plan(
            &sources,
            serde_json::json!([
                {"module":"first","files":["data.txt"]},{"module":"second","files":["data.txt"]}
            ]),
        )
        .unwrap()
        .stage(&temp.owned())
        .unwrap();
    let publish = temp.publish();
    // 这里只核文件枚举/角色，不把普通哨兵字节称为可执行原生或 ILC 验收。
    let native = publish.join("app");
    fs::write(&native, b"test artifact placeholder").unwrap();
    staged.publish(&publish).unwrap();
    let mut artifacts = crate::manifest_files::collect(&publish, &native).unwrap();
    staged.mark_artifacts(&mut artifacts).unwrap();
    let resource = artifacts
        .iter()
        .find(|item| item.relative_path == staged.records[0].relative_path)
        .unwrap();
    assert_eq!(resource.role, "declared_resource");
    assert_eq!(resource.size, 11);
    assert_eq!(
        resource.sha256,
        crate::manifest_files::hash_bytes(b"actual data")
    );
    assert_eq!(resource.sha256, staged.records[0].sha256);
    let original = serde_json::json!({"schema_version":1,"artifacts":artifacts,"scope":{"resources_included":false}});
    let bytes = extend_manifest(&serde_json::to_vec(&original).unwrap(), &staged).unwrap();
    let value: serde_json::Value = serde_json::from_slice(&bytes).unwrap();
    assert_eq!(value["schema_version"], 2);
    assert_eq!(value["resources"].as_array().unwrap().len(), 1);
    assert_eq!(
        value["resources"][0]["consumer_module_ids"],
        serde_json::json!([0, 1])
    );
    assert_eq!(
        value["resources"][0]["sha256"],
        crate::manifest_files::hash_bytes(b"actual data")
    );
    assert_eq!(value["module_locations"].as_array().unwrap().len(), 2);
    assert_eq!(value["scope"]["resources_included"], true);
    assert_eq!(value["scope"]["declared_resources_complete"], true);
}

#[test]
fn source_changed_after_compilation_is_rejected_for_length_and_content() {
    // 两个独立临时根，排除上一失败 staging 残留造成的假阳性。
    for replacement in ["pass\n# changed length\n", "None\n"] {
        let temp = Temp::new();
        let sources = vec![temp.source("source", "main", "entry/main.py")];
        assert_eq!(sources[0].text, "pass\n");
        let plan = temp.plan(&sources, request("main", &[])).unwrap();
        fs::write(&sources[0].resolved_source_path, replacement).unwrap();
        assert!(
            plan.stage(&temp.owned()).is_err(),
            "changed compiled text must not enter resource binding: {replacement:?}"
        );
    }
}
