//! 清单纯单元测试：固定摘要、发布枚举边界、实际编译文本与结构报告；不调用 SDK。
use super::*;
use crate::{manifest_files as files, manifest_sdk};
use ryupy_codegen_cil::{DelegateReference, ModuleEmission};
use std::path::PathBuf;
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);
struct Temp(PathBuf);
impl Temp {
    fn new() -> Self {
        let path = std::env::temp_dir().join(format!(
            "ryupy-manifest-test-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        std::fs::create_dir(&path).unwrap();
        Self(path)
    }
}
impl Drop for Temp {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
fn descriptor(name: &str) -> MethodDescriptor {
    MethodDescriptor {
        owner: "RyuPy.Generated.Module".to_owned(),
        name: name.to_owned(),
        signature: "static void ()".to_owned(),
    }
}
fn context() -> ManifestContext {
    let init = descriptor("init_actual");
    let report = EmissionReport {
        schema_version: 1,
        entry: descriptor("Main"),
        modules: vec![ModuleEmission {
            module_id: 0,
            module_name: "__main__".to_owned(),
            init: init.clone(),
            getter: Some(descriptor("get_actual")),
            setter: Some(descriptor("set_actual")),
            exports: vec!["中文\"name".to_owned()],
        }],
        delegate_references: vec![DelegateReference {
            source: descriptor("register_actual"),
            instruction: 12,
            target: Some(init),
            target_display: "actual init".to_owned(),
            role: "module_init".to_owned(),
            module_id: Some(0),
            function_id: None,
            class_id: None,
        }],
    };
    let sources = vec![SourceSummary {
        module_id: 0,
        module_name: "__main__".to_owned(),
        package: "".to_owned(),
        logical_source: "entry/main.py".to_owned(),
        compiled_text_sha256: files::hash_bytes(b"print(1)\n"),
    }];
    ManifestContext::new(
        report,
        sources,
        0,
        b"managed bytes",
        &crate::Session::default(),
    )
    .unwrap()
}

#[test]
fn sha256_is_standard_and_streaming_file_matches_current_bytes() {
    assert_eq!(
        files::hash_bytes(b"abc"),
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"
    );
    let temp = Temp::new();
    let path = temp.0.join("input");
    std::fs::write(&path, b"abc").unwrap();
    let record = files::digest_file(&path, "input", "test").unwrap();
    assert_eq!(record.size, 3);
    assert_eq!(record.sha256, files::hash_bytes(b"abc"));
}

#[test]
fn sdk_version_accepts_single_real_line_and_rejects_log_or_extra_lines() {
    for version in ["10.0.403", "10.0.100-preview.7.25380.108"] {
        for suffix in ["", "\n", "\r\n"] {
            assert_eq!(
                manifest_sdk::parse_version(&format!("{version}{suffix}")).unwrap(),
                version
            );
        }
    }
    assert_eq!(
        manifest_sdk::parse_version("\u{feff}10.0.403\n").unwrap(),
        "10.0.403"
    );
    for bad in [
        "",
        "\n",
        " 10.0.403",
        "10.0.403 ",
        "10.0.403\n\n",
        "10.0.403\nlog",
        "SDK 10.0.403",
        "10.0",
        "10.0.403-",
        "10.0.403\r",
        "10.0.403/other",
    ] {
        assert!(manifest_sdk::parse_version(bad).is_err(), "{bad:?}");
    }
    let temp = Temp::new();
    assert!(manifest_sdk::read_version(&temp.0.join("missing")).is_err());
    let path = temp.0.join("sdk.version");
    std::fs::write(&path, vec![b'1'; 257]).unwrap();
    assert!(manifest_sdk::read_version(&path).is_err());
}

#[test]
fn paths_are_safe_on_both_platforms_not_just_the_host_parser() {
    for path in [
        "",
        "/x",
        "C:/x",
        "C:x",
        "\\\\server\\share",
        "a\\b",
        "../x",
        "a/../b",
        "a/./b",
        "a//b",
        "a/",
        "a\n",
    ] {
        assert!(files::safe_relative(path).is_err(), "{path:?}");
    }
    for path in ["native", "symbols/native.dbg", "包/源.py"] {
        assert!(files::safe_relative(path).is_ok());
    }
}

#[test]
fn publish_enumeration_is_exact_sorted_and_does_not_guess_sidecar_dependencies() {
    let temp = Temp::new();
    let native = temp.0.join("native");
    std::fs::write(&native, b"native").unwrap();
    std::fs::create_dir(temp.0.join("symbols")).unwrap();
    std::fs::write(temp.0.join("symbols/name.dbg"), b"debug").unwrap();
    let records = files::collect(&temp.0, &native).unwrap();
    assert_eq!(records.len(), 2);
    assert_eq!(records[0].role, "native_executable");
    assert_eq!(records[1].role, "unclassified_sidecar");
    assert_eq!(records[1].relative_path, "symbols/name.dbg");
    let manifest = files::write_atomic(&temp.0, b"{}\n").unwrap();
    assert_eq!(std::fs::read(&manifest).unwrap(), b"{}\n");
    assert!(files::collect(&temp.0, &native).is_err());
    assert!(files::write_atomic(&temp.0, b"replaced").is_err());
    assert_eq!(std::fs::read(&manifest).unwrap(), b"{}\n");
}

#[test]
fn exclusive_temporary_collision_and_directory_destination_fail_without_overwrite() {
    let temp = Temp::new();
    let temporary = temp.0.join(".ryupy-artifact.json.tmp");
    std::fs::write(&temporary, b"other").unwrap();
    assert!(files::write_atomic(&temp.0, b"{}").is_err());
    assert_eq!(std::fs::read(&temporary).unwrap(), b"other");
    std::fs::remove_file(&temporary).unwrap();
    std::fs::create_dir(temp.0.join(files::MANIFEST_NAME)).unwrap();
    assert!(files::write_atomic(&temp.0, b"{}").is_err());
}

#[test]
fn schema_preserves_real_reference_fields_escapes_names_and_denies_closure_claims() {
    let context = context();
    let artifact = FileDigest {
        relative_path: "native".to_owned(),
        role: "native_executable".to_owned(),
        size: 6,
        sha256: files::hash_bytes(b"native"),
    };
    let inputs = vec![
        FileDigest {
            relative_path: "managed-entry.dll".to_owned(),
            role: "compiled_managed_input".to_owned(),
            size: 13,
            sha256: files::hash_bytes(b"managed bytes"),
        },
        FileDigest {
            relative_path: "RyuPy.Runtime.dll".to_owned(),
            role: "runtime_managed_input".to_owned(),
            size: 7,
            sha256: files::hash_bytes(b"runtime"),
        },
    ];
    let artifacts = [artifact];
    let raw = context
        .render("linux-x64", "ELF", "x64", "10.0.403", &artifacts, &inputs)
        .unwrap();
    let parsed: Value = serde_json::from_slice(&raw).unwrap();
    assert_eq!(parsed["schema_version"], 1);
    assert_eq!(parsed["sources"][0]["logical_source"], "entry/main.py");
    assert_eq!(parsed["modules"][0]["exports"][0], "中文\"name");
    assert_eq!(
        parsed["emitted_delegate_references"][0]["site"]["instruction"],
        12
    );
    assert_eq!(
        parsed["emitted_delegate_references"][0]["target"]["name"],
        "init_actual"
    );
    for key in [
        "resources_included",
        "dynamic_dependency_closure_complete",
        "native_retention_proven",
        "manifest_self_hash_included",
    ] {
        assert_eq!(parsed["scope"][key], false);
    }
    assert!(parsed["compiler"]["build_revision"].is_null());
    assert_eq!(
        raw,
        context
            .render("linux-x64", "ELF", "x64", "10.0.403", &artifacts, &inputs)
            .unwrap()
    );
}

#[test]
fn invalid_reports_and_changed_managed_input_cannot_report_success() {
    let mut context = context();
    let input = FileDigest {
        relative_path: "managed-entry.dll".to_owned(),
        role: "compiled_managed_input".to_owned(),
        size: 13,
        sha256: files::hash_bytes(b"managed bytes"),
    };
    context.verify_managed_input(&input).unwrap();
    let mut wrong = input;
    wrong.sha256 = files::hash_bytes(b"changed bytes");
    assert!(context.verify_managed_input(&wrong).is_err());
    context.report.modules[0].module_id = 8;
    assert!(context.validate().is_err());
    context.report.modules[0].module_id = 0;
    context.sources[0].logical_source = "/private/host/source.py".to_owned();
    assert!(context.validate().is_err());
}

#[test]
fn actual_loader_text_has_independent_identity_for_equal_basenames() {
    let temp = Temp::new();
    for name in ["a", "b"] {
        std::fs::create_dir(temp.0.join(name)).unwrap();
        std::fs::write(temp.0.join(name).join("__init__.py"), "").unwrap();
        std::fs::write(
            temp.0.join(name).join("same.py"),
            format!("value = '{name}'\n"),
        )
        .unwrap();
    }
    let entry = temp.0.join("same.py");
    std::fs::write(&entry, "import a.same\nimport b.same\n").unwrap();
    let (_, summaries, _) =
        crate::compile_linked(&mut crate::Session::default(), &entry, &[], true, None).unwrap();
    for (name, logical, text) in [
        (
            "__main__",
            "entry/same.py",
            "import a.same\nimport b.same\n",
        ),
        ("a.same", "modules/a/same.py", "value = 'a'\n"),
        ("b.same", "modules/b/same.py", "value = 'b'\n"),
    ] {
        let source = summaries.iter().find(|s| s.module_name == name).unwrap();
        assert_eq!(source.logical_source, logical);
        assert_eq!(
            source.compiled_text_sha256,
            files::hash_bytes(text.as_bytes())
        );
    }
    assert!(
        summaries
            .iter()
            .any(|s| s.logical_source == "modules/a/__init__.py")
    );
}

#[cfg(unix)]
#[test]
fn symlinks_and_non_regular_entries_are_not_followed_for_hashing() {
    use std::os::unix::fs::symlink;
    let temp = Temp::new();
    let outside = Temp::new();
    let native = temp.0.join("native");
    std::fs::write(&native, b"native").unwrap();
    std::fs::write(outside.0.join("secret"), b"not ours").unwrap();
    symlink(&outside.0, temp.0.join("outside")).unwrap();
    assert!(files::collect(&temp.0, &native).is_err());
    assert!(files::digest_file(&temp.0.join("outside"), "outside", "bad").is_err());
    std::fs::remove_file(temp.0.join("outside")).unwrap();
    symlink(outside.0.join("secret"), temp.0.join("linked-file")).unwrap();
    assert!(files::collect(&temp.0, &native).is_err());
}

#[cfg(windows)]
#[test]
fn junction_directory_is_rejected_without_hashing_outside_publish() {
    let temp = Temp::new();
    let outside = Temp::new();
    let native = temp.0.join("native.exe");
    std::fs::write(&native, b"native").unwrap();
    let junction = temp.0.join("outside");
    let status = std::process::Command::new("cmd")
        .args(["/C", "mklink", "/J"])
        .arg(&junction)
        .arg(&outside.0)
        .status()
        .unwrap();
    assert!(status.success());
    assert!(files::collect(&temp.0, &native).is_err());
    std::fs::remove_dir(junction).unwrap();
}
