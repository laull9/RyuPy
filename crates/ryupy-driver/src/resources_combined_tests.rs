//! 自动源码位置与显式数据联合计划：验证 consumer 重编号、延期拒绝和绑定清单版本。
use super::*;
use crate::resources::stage_tests::{Temp, request};
use serde_json::json;

#[test]
fn automatic_locations_keep_declared_data_and_project_consumers() {
    let temp = Temp::new();
    let sources = vec![
        temp.source("source", "unused", "modules/unused.py"),
        temp.source("source", "main", "entry/main.py"),
    ];
    std::fs::write(temp.0.join("source/data.txt"), b"payload").unwrap();
    let mut plan = temp.plan(&sources, request("main", &["data.txt"])).unwrap();
    plan.add_automatic_locations(&sources).unwrap();
    assert_eq!(plan.locations.len(), 2);
    plan.automatic = Some(crate::default_aot::Audit {
        source_to_emitted: vec![None, Some(0)],
        manifest: json!({"mode":"automatic_locations"}),
    });
    let staged = plan.stage(&temp.owned()).unwrap();
    assert_eq!(staged.records[0].consumer_module_ids, vec![0]);
    assert_eq!(staged.locations.len(), 1);
    assert_eq!(staged.locations[0].module_id, 0);
    for base in [1, 3, 4, 7] {
        let mut original =
            json!({"schema_version":base,"kind":"ryupy-native-aot-artifact","scope":{}});
        if base != 1 {
            original["native_bindings"] = json!([]);
        }
        let bytes = extend_manifest(&serde_json::to_vec(&original).unwrap(), &staged).unwrap();
        let value: serde_json::Value = serde_json::from_slice(&bytes).unwrap();
        assert_eq!(value["schema_version"], if base == 1 { 4 } else { 9 });
        assert_eq!(value["scope"]["declared_resources_complete"], true);
        assert_eq!(value["scope"]["automatic_module_locations_complete"], true);
        assert_eq!(value["resources"][0]["consumer_module_ids"], json!([0]));
    }
}

#[test]
fn deferred_resource_consumer_fails_without_publishing() {
    let temp = Temp::new();
    let sources = vec![temp.source("source", "deferred", "modules/deferred.py")];
    std::fs::write(temp.0.join("source/data.txt"), b"payload").unwrap();
    let mut plan = temp
        .plan(&sources, request("deferred", &["data.txt"]))
        .unwrap();
    plan.add_automatic_locations(&sources).unwrap();
    plan.automatic = Some(crate::default_aot::Audit {
        source_to_emitted: vec![None],
        manifest: json!({}),
    });
    let error = match plan.stage(&temp.owned()) {
        Ok(_) => panic!("延期consumer不能发布"),
        Err(e) => e,
    };
    assert!(error.to_string().contains("consumer"));
}

#[test]
fn automatic_anchor_conflicts_with_declared_data_are_rejected() {
    let temp = Temp::new();
    let sources = vec![
        temp.source("source", "main", "entry/main.py"),
        temp.source("source", "other", "entry/data.txt"),
    ];
    let mut plan = temp.plan(&sources, request("main", &["data.txt"])).unwrap();
    assert!(plan.add_automatic_locations(&sources).is_err());
}
