//! AOT 发布隔离单测：精确项目合同、保留名早拒绝及独占路径；不启动 SDK。
use super::*;

#[test]
fn host_project_is_explicit_strict_and_escapes_literal_paths() {
    let text = project(
        "RyuPyAotHost_test",
        "user",
        Path::new("/tmp/中文 & <x>/$(name);%.dll"),
        Path::new("/tmp/RyuPy.Runtime.dll"),
        None,
    )
    .unwrap();
    assert!(text.contains("<EnableDefaultCompileItems>false</EnableDefaultCompileItems>"));
    assert_eq!(text.matches("<Compile ").count(), 1);
    for property in [
        "TreatWarningsAsErrors",
        "IlcTreatWarningsAsErrors",
        "ILLinkTreatWarningsAsErrors",
    ] {
        assert!(text.contains(&format!("<{property}>true</{property}>")));
    }
    assert!(text.contains("/tmp/中文 &amp; &lt;x&gt;/%24%28name%29%3B%25.dll"));
    assert!(!text.contains("TrimmerRootAssembly"));
    assert!(!text.contains("NoWarn"));
    assert!(!text.contains("Reflection"));
    assert!(path_literal(Path::new("/tmp/new\nline")).is_err());
}

#[test]
fn reserved_runtime_name_fails_before_any_build_write() {
    let parent = std::env::temp_dir();
    let (root, _) = owned_root(&parent, "input").unwrap();
    let output = root.join("output");
    std::fs::create_dir(&output).unwrap();
    let sentinel = output.join("RyuPy.Runtime.dll");
    std::fs::write(&sentinel, b"user-owned-existing-bytes").unwrap();
    for name in ["RyuPy.Runtime.py", "ryupy.runtime.py", "RYUPY.RUNTIME.py"] {
        for aot in [false, true] {
            let mut session = crate::Session::default();
            let result = crate::build(
                &mut session,
                &crate::BuildOptions {
                    input: root.join(name),
                    out_dir: output.clone(),
                    aot,
                    search_paths: Vec::new(),
                },
            );
            assert!(result.unwrap_err().to_string().contains("保留名"));
            assert_eq!(
                std::fs::read(&sentinel).unwrap(),
                b"user-owned-existing-bytes"
            );
            assert_eq!(std::fs::read_dir(&output).unwrap().count(), 1);
        }
    }
    let missing = root.join("must-not-create");
    let error = crate::build(
        &mut crate::Session::default(),
        &crate::BuildOptions {
            input: root.join("RyuPy.Runtime.py"),
            out_dir: missing.clone(),
            aot: false,
            search_paths: Vec::new(),
        },
    )
    .unwrap_err();
    assert!(error.to_string().contains("保留名"));
    assert!(!missing.exists());
    assert!(validate_before_build(Path::new("RyuPy.Runtime.Other.py"), false).is_ok());
    std::fs::remove_dir_all(root).unwrap();
}

#[test]
fn owned_work_roots_and_host_identities_are_unique() {
    let (first, host) = owned_root(&std::env::temp_dir(), "input").unwrap();
    let (second, next_host) = owned_root(&std::env::temp_dir(), &host).unwrap();
    assert_ne!(first, second);
    assert!(!next_host.eq_ignore_ascii_case(&host));
    assert!(!next_host.eq_ignore_ascii_case(RUNTIME_ASSEMBLY));
    assert!(first.is_absolute());
    std::fs::remove_dir_all(first).unwrap();
    std::fs::remove_dir_all(second).unwrap();
}

#[test]
fn sdk_version_target_uses_same_publish_and_escaped_owned_path() {
    let text = project(
        "RyuPyAotHost_test",
        "user",
        Path::new("/tmp/user.dll"),
        Path::new("/tmp/RyuPy.Runtime.dll"),
        Some(Path::new("/tmp/a&b/$(private);%/sdk.version")),
    )
    .unwrap();
    assert!(text.contains("BeforeTargets=\"Publish\""));
    assert!(text.contains("Lines=\"$(NETCoreSdkVersion)\""));
    assert!(text.contains("Overwrite=\"true\""));
    assert!(text.contains("/tmp/a&amp;b/%24%28private%29%3B%25/sdk.version"));
    let legacy = project(
        "host",
        "user",
        Path::new("a.dll"),
        Path::new("runtime.dll"),
        None,
    )
    .unwrap();
    assert!(!legacy.contains("RyuPyRecordSdkVersion"));
}

#[test]
fn failed_post_publish_validation_cleans_only_its_owned_root() {
    let (root, _) = owned_root(&std::env::temp_dir(), "input").unwrap();
    let (sibling, _) = owned_root(&std::env::temp_dir(), "input").unwrap();
    std::fs::write(sibling.join("kept"), b"other build").unwrap();
    let missing_version = crate::manifest_sdk::read_version(&root.join("sdk.version"));
    assert!(finish_owned(&root, missing_version).is_err());
    assert!(!root.exists());
    assert_eq!(std::fs::read(sibling.join("kept")).unwrap(), b"other build");
    std::fs::remove_dir_all(sibling).unwrap();
}
