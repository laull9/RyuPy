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
    if matches!(schema, 2 | 4..=6) {
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
    for schema in 1..=6 {
        let c = Case::new();
        let a = build(&c.opts, |o, _owned| fake(o, b"native", schema)).unwrap();
        let value = files::verify_manifest(a.manifest.unwrap().parent().unwrap()).unwrap();
        assert_eq!(value["schema_version"], schema);
        assert_eq!(value["keep_unknown"]["values"], json!([1, 2]));
        assert_eq!(
            value["build_inputs"][0]["sha256"],
            manifest_files::hash_bytes(b"managed")
        );
        if matches!(schema, 2 | 4..=6) {
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

#[test]
fn os_sidecars_are_ignored_in_build_dist_and_publish_without_hiding_real_changes() {
    let c = Case::new();
    build(&c.opts, |o, _owned| {
        let a = fake(o, b"first", 2)?;
        let publish = a.manifest.as_ref().unwrap().parent().unwrap();
        // 假定 Finder/Explorer 在 publish 阶段写入了精确的 OS 旁件。
        std::fs::write(publish.join(".DS_Store"), b"finder-meta")?;
        std::fs::write(publish.join("_internal/Thumbs.db"), b"explorer-meta")?;
        Ok(a)
    })
    .unwrap();
    // OS 旁件不得被复制进最终 dist。
    assert!(!c.dist().join(".DS_Store").exists());
    assert!(!c.dist().join("_internal/Thumbs.db").exists());
    // owner 元数据不得包含普通文件 sha256 字段。
    let owner_text = std::fs::read_to_string(c.build().join(files::OWNER)).unwrap();
    assert!(!owner_text.contains("\"sha256\""));

    // 之后桌面环境在 build/dist 写入 .DS_Store / Thumbs.db 不得阻断后续增量发布。
    std::fs::write(c.build().join(".DS_Store"), b"finder-build").unwrap();
    std::fs::write(c.dist().join("Thumbs.db"), b"explorer-dist").unwrap();
    build(&c.opts, |o, _owned| fake(o, b"second", 2)).unwrap();

    // 锁文件、tmp 临时文件与非精确旁件绝不能被忽略。
    for non_ignored in ["build.lock", "temp.tmp", ".DS_Store.bak", "Thumbs.db.tmp"] {
        let d = Case::new();
        build(&d.opts, |o, _owned| fake(o, b"base", 1)).unwrap();
        std::fs::write(d.dist().join(non_ignored), b"foreign").unwrap();
        assert!(
            build(&d.opts, |_, _owned| panic!("must not compile")).is_err(),
            "必须拒绝非精确 OS 旁件：{non_ignored}"
        );
    }

    // .DS_Store / Thumbs.db 若是目录（非普通旁件文件）不得被静默忽略。
    let dir_case = Case::new();
    build(&dir_case.opts, |o, _owned| fake(o, b"base", 1)).unwrap();
    std::fs::create_dir(dir_case.dist().join(".DS_Store")).unwrap();
    assert!(build(&dir_case.opts, |_, _owned| panic!("must not compile")).is_err());

    // 保持文件长度不变但改写内容（更新 mtime）必须被拒绝。
    let mut mtime_case = Case::new();
    build(&mtime_case.opts, |o, _owned| fake(o, b"abcd", 1)).unwrap();
    let exe_name = if cfg!(windows) {
        "入口 & app.exe"
    } else {
        "入口 & app"
    };
    let target_exe = mtime_case.dist().join(exe_name);
    // 显式推进 mtime，避免高速测试同时间戳覆盖导致系统时钟分辨率掩盖变更。
    std::thread::sleep(std::time::Duration::from_millis(20));
    std::fs::write(&target_exe, b"wxyz").unwrap();
    assert!(build(&mtime_case.opts, |_, _owned| panic!("must not compile")).is_err());
    let _ = &mut mtime_case;
}

#[cfg(unix)]
#[test]
fn os_sidecar_symlinks_and_permission_changes_remain_rejected() {
    use std::os::unix::fs::{PermissionsExt, symlink};
    for name in [".DS_Store", "Thumbs.db"] {
        let c = Case::new();
        build(&c.opts, |o, _owned| fake(o, b"one", 1)).unwrap();
        symlink(c.root.join("入口 & app.py"), c.dist().join(name)).unwrap();
        assert!(
            build(&c.opts, |_, _owned| panic!("must not compile")).is_err(),
            "符号链接 {name} 必须被拒绝"
        );
    }

    // 权限改动必须继续被拒绝。
    let c = Case::new();
    let a = build(&c.opts, |o, _owned| fake(o, b"perm", 1)).unwrap();
    let exe = a.native_exe.unwrap();
    std::fs::set_permissions(&exe, std::fs::Permissions::from_mode(0o644)).unwrap();
    assert!(build(&c.opts, |_, _owned| panic!("must not compile")).is_err());
}

#[test]
fn legacy_owner_without_modified_and_with_sha256_migrates_cleanly_or_rejects_tamper() {
    let c = Case::new();
    build(&c.opts, |o, _owned| fake(o, b"first", 1)).unwrap();
    let owner_path = c.build().join(files::OWNER);
    let current: serde_json::Value =
        serde_json::from_slice(&std::fs::read(&owner_path).unwrap()).unwrap();
    let strip_modified_add_sha = |list: &serde_json::Value| -> serde_json::Value {
        serde_json::Value::Array(
            list.as_array()
                .unwrap()
                .iter()
                .map(|node| {
                    let mut obj = node.as_object().unwrap().clone();
                    obj.remove("modified");
                    obj.insert("sha256".into(), json!(""));
                    serde_json::Value::Object(obj)
                })
                .collect(),
        )
    };
    let legacy = json!({
        "version": current["version"],
        "source": current["source"],
        "stem": current["stem"],
        "build": strip_modified_add_sha(&current["build"]),
        "dist": strip_modified_add_sha(&current["dist"]),
    });
    std::fs::write(&owner_path, serde_json::to_vec_pretty(&legacy).unwrap()).unwrap();

    // 旧版 owner 匹配真实树时应迁移成功并写出无 sha256 的新版 owner。
    build(&c.opts, |o, _owned| fake(o, b"second", 1)).unwrap();
    let migrated_text = std::fs::read_to_string(&owner_path).unwrap();
    assert!(!migrated_text.contains("\"sha256\""));
    assert!(migrated_text.contains("\"modified\""));

    // 若旧版 owner 存在的同时 dist 被外部篡改，必须给出明确诊断并保留原 dist。
    let d = Case::new();
    build(&d.opts, |o, _owned| fake(o, b"base", 1)).unwrap();
    let d_owner = d.build().join(files::OWNER);
    let d_cur: serde_json::Value =
        serde_json::from_slice(&std::fs::read(&d_owner).unwrap()).unwrap();
    let d_legacy = json!({
        "version": d_cur["version"],
        "source": d_cur["source"],
        "stem": d_cur["stem"],
        "build": strip_modified_add_sha(&d_cur["build"]),
        "dist": strip_modified_add_sha(&d_cur["dist"]),
    });
    std::fs::write(&d_owner, serde_json::to_vec_pretty(&d_legacy).unwrap()).unwrap();
    std::fs::write(d.dist().join("tampered"), b"x").unwrap();
    let err = build(&d.opts, |_, _owned| panic!("must not compile")).unwrap_err();
    assert!(format!("{err:#}").contains("旧版owner"), "{err:#}");

    // 非法未知字段必须明确拒绝。
    std::fs::write(&d_owner, b"{\"version\":1,\"unknown\":true}").unwrap();
    let bad_err = build(&d.opts, |_, _owned| panic!("must not compile")).unwrap_err();
    assert!(format!("{bad_err:#}").contains("不兼容"), "{bad_err:#}");
}
