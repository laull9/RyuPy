//! 纯文件快照合同测试，不执行CPython、产品或SDK。
use super::*;
#[test]
fn complete_snapshot_and_path_key_are_not_metadata_name() {
    let root = Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/cases/distribution_metadata/request.json")
        .canonicalize()
        .unwrap();
    let payload = collect(&root).unwrap();
    let snapshot: serde_json::Value = serde_json::from_str(&payload).unwrap();
    let distributions = snapshot["distributions"].as_array().unwrap();
    assert_eq!(distributions.len(), 2);
    assert_eq!(distributions[0]["discovery_key"], "alpha_pkg");
    assert_eq!(distributions[0]["metadata_name"], "Alpha.Pkg");
    assert_eq!(distributions[0]["files"].as_array().unwrap().len(), 2);
    assert!(
        distributions[0]["files"][1]["hex"]
            .as_str()
            .unwrap()
            .contains("6d797374657279")
    );
    assert_eq!(normalize("alpha-pkg"), "alpha_pkg");
    // CP路径优化在'-'处先截断，不能直接用Name归并。
    let filename = "alpha-pkg.egg-info";
    assert_eq!(
        normalize(
            filename
                .strip_suffix(".egg-info")
                .unwrap()
                .split('-')
                .next()
                .unwrap()
        ),
        "alpha"
    );
}
#[test]
fn empty_explicit_roots_and_invalid_schema() {
    let path = std::env::temp_dir().join(format!(
        "ryupy-metadata-request-{}.json",
        std::process::id()
    ));
    std::fs::write(
        &path,
        r#"{"schema_version":1,"mode":"closed_snapshot","roots":[]}"#,
    )
    .unwrap();
    let payload = collect(&path).unwrap();
    assert!(payload.contains("\"distributions\":[]"));
    std::fs::write(
        &path,
        r#"{"schema_version":2,"mode":"closed_snapshot","roots":[]}"#,
    )
    .unwrap();
    assert!(collect(&path).is_err());
    std::fs::write(
        &path,
        r#"{"schema_version":1,"mode":"closed_snapshot","roots":[],"unknown":1}"#,
    )
    .unwrap();
    assert!(collect(&path).is_err());
    std::fs::remove_file(&path).unwrap();
}

#[test]
fn distribution_metadata_keeps_unknown_bytes_and_all_shadowed_sources() {
    let serial = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let base = std::env::temp_dir().join(format!(
        "ryupy-distributions-{}-{serial}",
        std::process::id()
    ));
    let write = |relative: &str, bytes: &[u8]| {
        let path = base.join(relative);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, bytes).unwrap();
    };
    write(
        "a/Alpha_Pkg-1.dist-info/METADATA",
        b"Name: Alpha.Pkg\nVersion: 1\n",
    );
    write("a/Alpha_Pkg-1.dist-info/unknown.bin", &[0, 255, 10]);
    write(
        "b/alpha_pkg.egg-info/PKG-INFO",
        b"Name: alpha-pkg\nVersion: 2\n",
    );
    write(
        "b/alpha-pkg.egg-info/PKG-INFO",
        b"Name: alpha-pkg\nVersion: 9\n",
    );
    write("request.json", br#"{"schema_version":1,"mode":"closed_snapshot","roots":[{"id":"a","path":"a"},{"id":"b","path":"b"}]}"#);
    let payload = collect(&base.join("request.json")).unwrap();
    let value: serde_json::Value = serde_json::from_str(&payload).unwrap();
    let distributions = value["distributions"].as_array().unwrap();
    assert_eq!(distributions.len(), 3);
    assert_eq!(distributions[0]["files"][1]["hex"], "00ff0a");
    assert_eq!(distributions[1]["discovery_key"], "alpha");
    assert_eq!(distributions[1]["shadowed"], false);
    assert_eq!(distributions[2]["discovery_key"], "alpha_pkg");
    assert_eq!(distributions[2]["shadowed"], true);
    write("b/unsupported.egg", b"archive");
    assert!(collect(&base.join("request.json")).is_err());
    std::fs::remove_file(base.join("b/unsupported.egg")).unwrap();
    #[cfg(unix)]
    {
        std::os::unix::fs::symlink(
            base.join("a/Alpha_Pkg-1.dist-info/METADATA"),
            base.join("a/link"),
        )
        .unwrap();
        assert!(collect(&base.join("request.json")).is_err());
    }
    std::fs::remove_dir_all(base).unwrap();
}
