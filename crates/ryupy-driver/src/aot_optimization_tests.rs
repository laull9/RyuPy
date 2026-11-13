//! 隔离伪SDK验证真实公开构建的有效值核验和失败保旧；不冒充真实NativeAOT通过。
use super::*;

#[test]
#[cfg(all(unix, target_arch = "x86_64", target_os = "linux"))]
fn fake_sdk_effective_preference_and_failed_rebuild_preserve_dist() {
    use std::os::unix::fs::PermissionsExt;
    let root = crate::test_temp::directory("optimization-fake-sdk");
    let script = root.join("sdk");
    std::fs::write(
        &script,
        r#"#!/bin/sh
set -eu
project="$2"
root=$(dirname "$project")
host=$(sed -n 's/.*<AssemblyName>\([^<]*\)<\/AssemblyName>.*/\1/p' "$project")
while [ "$1" != '-o' ]; do shift; done
out="$2"
cp /bin/true "$out/$host"
printf '10.0.403\n' > "$root/sdk.version"
printf 'effective=%s\n' "$FAKE_EFFECTIVE" > "$root/sdk.optimization"
"#,
    )
    .unwrap();
    std::fs::set_permissions(&script, std::fs::Permissions::from_mode(0o755)).unwrap();
    std::fs::write(root.join("RyuPy.Runtime.dll"), b"fake-sdk-only-runtime").unwrap();
    for value in ["Size", "Speed"] {
        let output = std::process::Command::new(std::env::current_exe().unwrap())
            .args([
                "--exact",
                "aot_optimization::integration_tests::fake_sdk_child",
                "--nocapture",
            ])
            .env("RYUPY_OPT_TEST_ROOT", &root)
            .env("DOTNET", &script)
            .env("RYUPY_RUNTIME_DIR", &root)
            .env("FAKE_EFFECTIVE", value)
            .output()
            .unwrap();
        assert!(
            output.status.success(),
            "{}",
            String::from_utf8_lossy(&output.stderr)
        );
    }
    std::fs::remove_dir_all(root).unwrap();
}

#[test]
fn fake_sdk_child() {
    let Ok(root) = std::env::var("RYUPY_OPT_TEST_ROOT") else {
        return;
    };
    let root = std::path::PathBuf::from(root);
    let actual = std::env::var("FAKE_EFFECTIVE").unwrap();
    let expected = if actual == "Size" {
        AotOptimization::Size
    } else {
        AotOptimization::Speed
    };
    let input = root.join(format!("entry_{actual}.py"));
    std::fs::write(&input, "print(7)\n").unwrap();
    let opts = crate::BuildOptions {
        input,
        out_dir: root.clone(),
        aot: true,
        search_paths: vec![],
    };
    let mut session = crate::Session {
        aot_optimization: expected,
        ..Default::default()
    };
    let artifact = crate::build(&mut session, &opts).unwrap();
    let receipt_path = artifact
        .dll
        .parent()
        .unwrap()
        .join("ryupy-aot-0/aot-optimization.json");
    let receipt: serde_json::Value =
        serde_json::from_slice(&std::fs::read(receipt_path).unwrap()).unwrap();
    assert_eq!(receipt["effective"], actual);
    assert_eq!(receipt["requested"], actual.to_lowercase());
    let native = artifact.native_exe.unwrap();
    assert_eq!(
        receipt["native_sha256"],
        crate::manifest_files::digest_file(&native, "native", "native_executable")
            .unwrap()
            .sha256
    );
    assert!(
        !native
            .parent()
            .unwrap()
            .join("aot-optimization.json")
            .exists()
    );
    let old = std::fs::read(&native).unwrap();
    // 同一输出根请求与伪SDK真实记录不一致，必须失败且保留旧dist。
    session.aot_optimization = if expected == AotOptimization::Size {
        AotOptimization::Speed
    } else {
        AotOptimization::Size
    };
    let error = crate::build(&mut session, &opts).unwrap_err();
    assert!(format!("{error:#}").contains("SDK优化偏好与请求不符"));
    assert_eq!(std::fs::read(native).unwrap(), old);
}

#[test]
fn receipt_rejects_missing_malformed_and_existing_output() {
    let root = crate::test_temp::directory("optimization-receipt");
    let native = root.join("native");
    std::fs::write(&native, b"test-only-native").unwrap();
    let record = root.join("sdk.optimization");
    assert!(receipt(&root, &record, &native, AotOptimization::Size).is_err());
    for invalid in ["effective=Speed\n", "effective=Size\nextra", "Size\n"] {
        std::fs::write(&record, invalid).unwrap();
        assert!(receipt(&root, &record, &native, AotOptimization::Size).is_err());
        assert!(!root.join("aot-optimization.json").exists());
    }
    std::fs::write(&record, "effective=Size\n").unwrap();
    receipt(&root, &record, &native, AotOptimization::Size).unwrap();
    let saved = std::fs::read(root.join("aot-optimization.json")).unwrap();
    assert!(receipt(&root, &record, &native, AotOptimization::Size).is_err());
    assert_eq!(
        saved,
        std::fs::read(root.join("aot-optimization.json")).unwrap()
    );
    #[cfg(unix)]
    {
        std::fs::remove_file(&record).unwrap();
        std::os::unix::fs::symlink(&native, &record).unwrap();
        assert!(receipt(&root, &record, &native, AotOptimization::Default).is_err());
    }
    std::fs::remove_dir_all(root).unwrap();
}
