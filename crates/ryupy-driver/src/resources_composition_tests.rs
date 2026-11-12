//! 自动源码位置与显式包数据的联合合同；测试真实冻结、索引投影和失败边界，不执行SDK。
use super::*;
use std::fs;

#[test]
fn explicit_data_survives_automatic_locations_and_projects_consumers() {
    let root = std::env::temp_dir().join(format!("ryupy-compose-{}", std::process::id()));
    fs::create_dir_all(&root).unwrap();
    let mut sources = Vec::new();
    for name in ["deferred", "active"] {
        let file = root.join(format!("{name}.py"));
        fs::write(&file, "pass\n").unwrap();
        let mut source =
            crate::modules::load_single(&mut crate::Session::default(), &file).unwrap();
        source.name = name.into();
        source.logical_source = format!("{name}.py");
        sources.push(source);
    }
    fs::write(root.join("data.txt"), b"real data\n").unwrap();
    let request = root.join("request.json");
    fs::write(
        &request,
        r#"{"schema_version":1,"modules":[{"module":"active","files":["data.txt"]}]}"#,
    )
    .unwrap();
    let explicit = ResourcePlan::read(&request, &sources).unwrap();
    let mut plan = ResourcePlan::compose_automatic(&sources, Some(explicit)).unwrap();
    assert!(plan.explicit_declaration);
    assert_eq!(plan.locations.len(), 2);
    plan.automatic = Some(crate::default_aot::Audit {
        source_to_emitted: vec![None, Some(0)],
        manifest: serde_json::json!({}),
    });
    let stage = root.join("stage");
    fs::create_dir_all(&stage).unwrap();
    let staged = plan.stage(&stage).unwrap();
    assert_eq!(staged.records.len(), 1);
    assert_eq!(staged.records[0].consumer_module_ids, vec![0]);
    assert_eq!(staged.locations.len(), 1);
    assert_eq!(staged.locations[0].module_id, 0);
    let manifest: serde_json::Value =
        serde_json::from_slice(&extend_manifest(br#"{"scope":{}}"#, &staged).unwrap()).unwrap();
    assert_eq!(manifest["schema_version"], 4);
    assert_eq!(manifest["scope"]["declared_resources_complete"], true);
    assert_eq!(manifest["scope"]["resources_included"], true);
    for mapping in [vec![None, None], vec![Some(0)]] {
        plan.automatic.as_mut().unwrap().source_to_emitted = mapping;
        let bad = root.join(format!(
            "bad{}",
            plan.automatic.as_ref().unwrap().source_to_emitted.len()
        ));
        fs::create_dir_all(&bad).unwrap();
        assert!(plan.stage(&bad).is_err());
    }
    // 新自动锚点必须也参与与显式资源路径的冲突校验。
    sources[0].logical_source = "data.txt".into();
    let explicit = ResourcePlan::read(&request, &sources).unwrap();
    assert!(ResourcePlan::compose_automatic(&sources, Some(explicit)).is_err());
    fs::remove_dir_all(root).unwrap();
}
