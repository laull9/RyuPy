//! 不启动SDK的事务测试：闭包只产固定假native/真实SHA清单，校验所有权与失败保旧。
use super::*;
use serde_json::json;
struct Case {
    root: PathBuf,
    opts: BuildOptions,
}
impl Case {
    fn new() -> Self {
        let root = std::env::temp_dir().join(format!(
            "ryupy-layout-test-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        std::fs::create_dir(&root).unwrap();
        let input = root.join("入口 & app.py");
        std::fs::write(&input, "print(1)").unwrap();
        Self {
            opts: BuildOptions {
                input,
                out_dir: root.join("output"),
                aot: true,
                search_paths: vec![],
            },
            root,
        }
    }
    fn dist(&self) -> PathBuf {
        self.opts.out_dir.join("入口 & app.dist")
    }
    fn build(&self) -> PathBuf {
        self.opts.out_dir.join("入口 & app.build")
    }
}
impl Drop for Case {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.root);
    }
}
fn fake(opts: &BuildOptions, data: &[u8], schema: u64) -> Result<Artifacts> {
    let dll = opts.out_dir.join("入口 & app.dll");
    std::fs::write(&dll, b"managed")?;
    let publish = opts.out_dir.join("ryupy-aot-fake").join("publish");
    std::fs::create_dir_all(&publish)?;
    let native = publish.join(if cfg!(windows) {
        "RyuPyAotHost_fake.exe"
    } else {
        "RyuPyAotHost_fake"
    });
    std::fs::write(&native, data)?;
    #[cfg(unix)]
    {
        use std::os::unix::fs::PermissionsExt;
        std::fs::set_permissions(&native, std::fs::Permissions::from_mode(0o755))?;
    }
    let mut artifacts = vec![
        json!({"relative_path":native.file_name().unwrap().to_str().unwrap(),"role":"native_executable","size":data.len(),"sha256":manifest_files::hash_bytes(data)}),
    ];
    if schema == 2 {
        let resource = publish.join("_internal/resources/v1/data.txt");
        std::fs::create_dir_all(resource.parent().unwrap())?;
        std::fs::write(resource, b"data")?;
        artifacts.push(json!({"relative_path":"_internal/resources/v1/data.txt","role":"resource","size":4,"sha256":manifest_files::hash_bytes(b"data")}));
    }
    let value = json!({"schema_version":schema,"kind":"ryupy-native-aot-artifact","artifacts":artifacts,"keep_unknown":{"values":[1,2]},"build_inputs":[{"relative_path":"入口 & app.dll","sha256":manifest_files::hash_bytes(b"managed")} ]});
    let manifest = manifest_files::write_atomic(&publish, &serde_json::to_vec(&value)?)?;
    Ok(Artifacts {
        dll,
        native_exe: Some(native),
        manifest: Some(manifest),
    })
}
#[test]
fn first_and_repeat_publish_preserve_old_work_and_exact_final_name() {
    let c = Case::new();
    let first = build(&c.opts, |o, _owned| fake(o, b"first", 1)).unwrap();
    let old_dll = first.dll.clone();
    let native = first.native_exe.unwrap();
    // ROOT 可经系统临时目录别名进入；只规范化已存在的预期目标。
    assert_eq!(native.parent().unwrap(), c.dist().canonicalize().unwrap());
    assert_eq!(
        native.file_name().unwrap().to_str().unwrap(),
        if cfg!(windows) {
            "入口 & app.exe"
        } else {
            "入口 & app"
        }
    );
    let second = build(&c.opts, |o, _owned| fake(o, b"second", 1)).unwrap();
    assert_eq!(
        std::fs::read(second.native_exe.unwrap()).unwrap(),
        b"second"
    );
    assert!(old_dll.is_file());
    assert!(
        std::fs::read_dir(c.build())
            .unwrap()
            .filter_map(|entry| entry.ok())
            .any(|e| e.path().join("previous-dist").is_dir())
    );
    #[cfg(unix)]
    {
        use std::os::unix::fs::PermissionsExt;
        assert_eq!(
            std::fs::metadata(native).unwrap().permissions().mode() & 0o777,
            0o755
        );
    }
}
#[test]
fn failure_retains_old_dist_and_records_owned_attempt() {
    let c = Case::new();
    build(&c.opts, |o, _owned| fake(o, b"old", 1)).unwrap();
    let before = files::snapshot(&c.dist(), false).unwrap();
    assert!(
        build(&c.opts, |o, _owned| {
            std::fs::write(o.out_dir.join("failure.log"), b"failure")?;
            anyhow::bail!("fake SDK failed")
        })
        .is_err()
    );
    assert_eq!(before, files::snapshot(&c.dist(), false).unwrap());
    build(&c.opts, |o, _owned| fake(o, b"next", 1)).unwrap();
}
#[test]
fn unowned_build_and_dist_are_not_adopted() {
    let c = Case::new();
    std::fs::create_dir_all(c.dist()).unwrap();
    std::fs::write(c.dist().join("sentinel"), b"mine").unwrap();
    assert!(build(&c.opts, |_, _owned| panic!("must not compile")).is_err());
    assert!(c.dist().join("sentinel").exists());
    let d = Case::new();
    std::fs::create_dir_all(d.build()).unwrap();
    assert!(build(&d.opts, |_, _owned| panic!("must not compile")).is_err());
}
#[test]
fn modified_build_or_dist_rejects_before_compile() {
    for in_build in [true, false] {
        let c = Case::new();
        build(&c.opts, |o, _owned| fake(o, b"one", 1)).unwrap();
        let root = if in_build { c.build() } else { c.dist() };
        std::fs::write(root.join("foreign"), b"foreign").unwrap();
        assert!(build(&c.opts, |_, _owned| panic!("must not compile")).is_err());
        assert!(root.join("foreign").is_file());
    }
}
#[test]
fn concurrent_lock_is_never_removed_or_reused() {
    let c = Case::new();
    std::fs::create_dir_all(&c.opts.out_dir).unwrap();
    let lock = c.opts.out_dir.join("入口 & app.build.lock");
    std::fs::create_dir(&lock).unwrap();
    assert!(build(&c.opts, |_, _owned| panic!("must not compile")).is_err());
    assert!(lock.is_dir());
}
#[test]
fn all_manifest_schemas_and_resource_subtree_survive() {
    for schema in 1..=3 {
        let c = Case::new();
        let a = build(&c.opts, |o, _owned| fake(o, b"native", schema)).unwrap();
        let value = files::verify_manifest(a.manifest.unwrap().parent().unwrap()).unwrap();
        assert_eq!(value["schema_version"], schema);
        assert_eq!(value["keep_unknown"]["values"], json!([1, 2]));
        assert_eq!(
            value["build_inputs"][0]["sha256"],
            manifest_files::hash_bytes(b"managed")
        );
        if schema == 2 {
            assert_eq!(
                std::fs::read(c.dist().join("_internal/resources/v1/data.txt")).unwrap(),
                b"data"
            );
        }
    }
}
#[test]
fn rename_collision_and_bad_manifest_preserve_prior_dist() {
    let c = Case::new();
    build(&c.opts, |o, _owned| fake(o, b"good", 1)).unwrap();
    let before = files::snapshot(&c.dist(), false).unwrap();
    assert!(
        build(&c.opts, |o, _owned| {
            let a = fake(o, b"bad", 1)?;
            let root = a.manifest.as_ref().unwrap().parent().unwrap();
            let name = if cfg!(windows) {
                "入口 & app.exe"
            } else {
                "入口 & app"
            };
            std::fs::write(root.join(name), b"collision")?;
            Ok(a)
        })
        .is_err()
    );
    assert_eq!(before, files::snapshot(&c.dist(), false).unwrap());
}
#[test]
fn portable_names_reject_before_output_creation() {
    for name in ["NUL.py", "CON.txt.py", "bad .py", "bad?.py"] {
        let mut c = Case::new();
        c.opts.input = c.root.join(name);
        // 名称拒绝必须早于源文件读取和任何输出写入。
        assert!(build(&c.opts, |_, _owned| panic!("must not compile")).is_err());
        assert!(!c.opts.out_dir.exists());
    }
}
#[test]
fn owner_duplicate_fields_and_unknown_version_are_rejected() {
    let c = Case::new();
    build(&c.opts, |o, _owned| fake(o, b"one", 1)).unwrap();
    let path = c.build().join(files::OWNER);
    let bytes = std::fs::read_to_string(&path).unwrap();
    std::fs::write(
        &path,
        bytes.replacen("\"version\": 1", "\"version\": 1, \"version\": 1", 1),
    )
    .unwrap();
    assert!(build(&c.opts, |_, _owned| panic!("must not compile")).is_err());
}
#[cfg(unix)]
#[test]
fn boundary_links_are_rejected_but_root_alias_is_resolved() {
    use std::os::unix::fs::symlink;
    let c = Case::new();
    std::fs::create_dir_all(&c.opts.out_dir).unwrap();
    let external = c.root.join("external");
    std::fs::create_dir(&external).unwrap();
    symlink(&external, c.build()).unwrap();
    assert!(build(&c.opts, |_, _owned| panic!("must not compile")).is_err());
    assert!(external.is_dir());
    let mut d = Case::new();
    let real = d.root.join("real");
    std::fs::create_dir(&real).unwrap();
    let alias = d.root.join("alias");
    symlink(&real, &alias).unwrap();
    d.opts.out_dir = alias;
    let a = build(&d.opts, |o, _owned| fake(o, b"one", 1)).unwrap();
    // 保留 alias 输入，校验真实目标的完整路径而非只比较前缀。
    assert_eq!(
        a.native_exe.unwrap(),
        real.join("入口 & app.dist/入口 & app")
            .canonicalize()
            .unwrap()
    );
}

#[test]
fn actual_rename_failure_restores_previous_dist_bytes() {
    let c = Case::new();
    build(&c.opts, |o, _owned| fake(o, b"old-and-good", 1)).unwrap();
    let before = files::snapshot(&c.dist(), false).unwrap();
    let backup = c.build().join("rollback-test-backup");
    let missing = c.build().join("deliberately-absent-candidate");
    let error = swap(&missing, &c.dist(), &backup, true).unwrap_err();
    assert!(error.to_string().contains("无法安装"));
    assert_eq!(before, files::snapshot(&c.dist(), false).unwrap());
    assert!(!backup.exists());
}
#[test]
fn reserved_assembly_name_is_rejected_before_root_creation() {
    let mut c = Case::new();
    c.opts.input = c.root.join("RyuPy.Runtime.py");
    let error = build(&c.opts, |_, _owned| panic!("must not compile")).unwrap_err();
    assert!(error.to_string().contains("保留名"));
    assert!(!c.opts.out_dir.exists());
}

#[test]
fn resource_native_unicode_is_not_unicode_resource_permission() {
    let c = Case::new();
    let a = build(&c.opts, |o, _owned| fake(o, b"good", 2)).unwrap();
    let root = a.native_exe.as_ref().unwrap().parent().unwrap();
    assert!(
        a.native_exe
            .as_ref()
            .unwrap()
            .file_name()
            .unwrap()
            .to_str()
            .unwrap()
            .contains("入口")
    );
    let before = files::snapshot(root, false).unwrap();
    let error=build(&c.opts,|o,_owned|{
        let a=fake(o,b"bad",2)?;let root=a.native_exe.as_ref().unwrap().parent().unwrap();
        let path="_internal/resources/v1/中文.txt";std::fs::write(root.join(path),b"x")?;
        let mp=a.manifest.as_ref().unwrap();let mut value:serde_json::Value=serde_json::from_slice(&std::fs::read(mp)?)?;
        value["artifacts"].as_array_mut().unwrap().push(json!({"relative_path":path,"role":"declared_resource","size":1,"sha256":manifest_files::hash_bytes(b"x")}));
        std::fs::write(mp,serde_json::to_vec(&value)?)?;Ok(a)
    }).unwrap_err();
    assert!(format!("{error:#}").contains("ASCII"));
    assert_eq!(before, files::snapshot(root, false).unwrap());
}
#[test]
fn resource_native_escaping_and_ascii_aliases_are_rejected() {
    let c = Case::new();
    let attempt = unique_attempt(&c.root).unwrap();
    let mut opts = c.opts.clone();
    opts.out_dir = attempt;
    let a = fake(&opts, b"native", 2).unwrap();
    let native = a.native_exe.as_ref().unwrap();
    let root = native.parent().unwrap();
    let old = native.file_name().unwrap().to_str().unwrap();
    let before = files::snapshot(root, false).unwrap();
    assert!(files::relocate(root, old, "../escape").is_err());
    assert!(files::relocate(root, old, "_INTERNAL").is_err());
    assert_eq!(before, files::snapshot(root, false).unwrap());
}

#[test]
fn owned_short_roots_reject_conflicts_and_escaping_without_cleanup() {
    let c = Case::new();
    build(&c.opts, |opts, owned| {
        let binding = owned.create_child(&opts.out_dir, "ryupy-binding-0")?;
        std::fs::write(binding.join("sentinel"), b"keep")?;
        assert!(
            owned
                .create_child(&opts.out_dir, "ryupy-binding-0")
                .is_err()
        );
        assert_eq!(std::fs::read(binding.join("sentinel"))?, b"keep");
        let bundle = binding.join("bundle");
        std::fs::create_dir(&bundle)?;
        let sdk = owned.create_child(&bundle, "ryupy-aot-0")?;
        assert!(owned.create_child(&bundle, "ryupy-aot-0").is_err());
        assert!(sdk.is_dir());
        assert!(owned.create_child(&c.root, "ryupy-aot-0").is_err());
        assert!(
            owned
                .create_child(&opts.out_dir.join(".."), "ryupy-aot-0")
                .is_err()
        );
        #[cfg(unix)]
        {
            let link = opts.out_dir.join("linked");
            std::os::unix::fs::symlink(&c.root, &link)?;
            assert!(owned.create_child(&link, "ryupy-aot-0").is_err());
            std::fs::remove_file(link)?;
        }
        fake(opts, b"native", 1)
    })
    .unwrap();
}

#[path = "aot_resource_output_tests.rs"]
mod resource_output_tests;
