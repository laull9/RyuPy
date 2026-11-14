//! 显式与自动枚举权威分离；纯计划测试不代替真实产品oracle。
use super::*;

#[test]
fn closed_plan_only_upgrades_when_new_authority_exists() {
    let mut plan = Plan::default();
    assert!(plan.runtime_payload().unwrap().is_none());
    plan.outside.insert("unselected".into());
    let payload: serde_json::Value =
        serde_json::from_str(&plan.runtime_payload().unwrap().unwrap()).unwrap();
    assert_eq!(payload["schema_version"], 1);
    assert!(payload.get("closed_unplanned").is_none());
    plan.closed_unplanned.insert("explicit_unselected".into());
    let payload: serde_json::Value =
        serde_json::from_str(&plan.runtime_payload().unwrap().unwrap()).unwrap();
    assert_eq!(payload["schema_version"], 2);
    assert_eq!(payload["outside"][0], "unselected");
    assert_eq!(payload["closed_unplanned"][0], "explicit_unselected");
}

#[test]
fn explicit_selected_queue_survives_and_removes_previous_closed_classification() {
    let request = Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/cases/distribution_metadata/request.json")
        .canonicalize()
        .unwrap();
    let payload = crate::distribution_metadata::collect(&request).unwrap();
    for explicit_metadata in [false, true] {
        let mut plan = Plan {
            payload: Some(payload.clone()),
            explicit_metadata,
            ..Default::default()
        };
        let mut queue = VecDeque::new();
        metadata_candidates(&mut plan, &[], &[], &mut queue).unwrap();
        assert!(queue.is_empty());
        let unselected = if explicit_metadata {
            &plan.closed_unplanned
        } else {
            &plan.outside
        };
        assert!(!unselected.is_empty());
        metadata_candidates(&mut plan, &[], &["demo".into()], &mut queue).unwrap();
        assert!(!queue.is_empty());
        for (name, hard, dependency) in queue {
            assert!(!hard && !dependency);
            assert!(plan.roots.contains(&name));
            assert!(!plan.closed_unplanned.contains(&name));
            assert!(!plan.outside.contains(&name));
        }
    }
}

#[test]
fn certified_native_overlap_is_not_a_configuration_conflict() {
    // re等认证native已注册时仍按原导入；保护分类只在真正无注册fallback拒绝。
    let mut plan = Plan::default();
    plan.closed_unplanned.insert("re".into());
    plan.protected.push("re".into());
    let payload: serde_json::Value =
        serde_json::from_str(&plan.runtime_payload().unwrap().unwrap()).unwrap();
    assert_eq!(payload["schema_version"], 2);
    assert_eq!(payload["closed_unplanned"][0], "re");
    assert_eq!(payload["protected"][0], "re");
}
