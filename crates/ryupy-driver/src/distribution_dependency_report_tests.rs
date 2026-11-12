//! 编译会话的依赖预检收据隔离：不复用旧结果，不改变既有metadata报告。
use super::*;
use serde_json::json;

#[test]
fn prepare_clears_previous_receipts_when_disabled_or_failed() {
    let mut session = crate::Session {
        distribution_metadata_report: Some(json!({"old": true})),
        distribution_dependency_report: Some(json!({"old": true})),
        ..Default::default()
    };
    assert!(prepare(&mut session).unwrap().is_none());
    assert!(session.distribution_metadata_report.is_none());
    assert!(session.distribution_dependency_report.is_none());
    session.distribution_metadata = Some(PathBuf::from("/absent-ryupy-dependency-request.json"));
    session.distribution_metadata_report = Some(json!({"old": true}));
    session.distribution_dependency_report = Some(json!({"old": true}));
    assert!(prepare(&mut session).is_err());
    assert!(session.distribution_metadata_report.is_none());
    assert!(session.distribution_dependency_report.is_none());
}

#[test]
fn dependency_receipt_is_separate_from_runtime_metadata() {
    let serial = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let out = std::env::temp_dir().join(format!(
        "ryupy-dependency-report-{}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&out).unwrap();
    let mut session = crate::Session::default();
    write_report(&session, &out).unwrap();
    assert_eq!(std::fs::read_dir(&out).unwrap().count(), 0);
    let payload = r#"{"schema_version":1,"mode":"closed_snapshot","roots":[],"distributions":[]}"#;
    record_payload(&mut session, payload).unwrap();
    let metadata_before = session.distribution_metadata_report.clone().unwrap();
    session.distribution_dependency_report = Some(json!({"separate_build_receipt": true}));
    write_report(&session, &out).unwrap();
    let metadata_after: serde_json::Value =
        serde_json::from_slice(&std::fs::read(out.join("distribution-metadata.json")).unwrap())
            .unwrap();
    assert_eq!(metadata_after, metadata_before);
    let receipt: serde_json::Value =
        serde_json::from_slice(&std::fs::read(out.join("dependency-check.json")).unwrap()).unwrap();
    assert_eq!(receipt, json!({"separate_build_receipt": true}));
    session.distribution_dependency_report = None;
    write_report(&session, &out).unwrap();
    assert!(!out.join("dependency-check.json").exists());
    std::fs::remove_dir_all(out).unwrap();
}
