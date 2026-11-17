//! 显式、封闭的部署文件选择：scope实际成员必须与声明全集一致，逐项保存未部署原生映像和加载边审计。
use super::{Payload, destination, native};
use anyhow::{Context, Result, ensure};
use serde::Deserialize;
use serde_json::{Value, json};
use std::collections::{BTreeMap, BTreeSet};
use std::io::Read;
use std::path::Path;

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
pub(super) struct Group {
    name: String,
    scope: String,
    members: Vec<String>,
    selected: Vec<String>,
    reason: String,
}

fn targets(values: Vec<String>) -> Result<BTreeSet<String>> {
    let length = values.len();
    let result = values
        .into_iter()
        .map(|s| destination(&s))
        .collect::<Result<BTreeSet<_>>>()?;
    ensure!(result.len() == length, "部署选择含重复规范目标");
    Ok(result)
}

fn inspect(source: &Path) -> Result<Value> {
    let mut magic = [0; 4];
    let mut file = std::fs::File::open(source)?;
    let count = file.read(&mut magic)?;
    if count < 4
        || !matches!(
            magic,
            [0xFE, 0xED, 0xFA, 0xCF] | [0xCF, 0xFA, 0xED, 0xFE] | [0xCA, 0xFE, 0xBA, 0xBE]
        )
    {
        ensure!(
            !magic.starts_with(b"\x7fELF") && !magic.starts_with(b"MZ"),
            "部署选择的原生审计当前只支持Mach-O"
        );
        return Ok(json!({"format": "data", "size": file.metadata()?.len()}));
    }
    native::check_architecture(source, native::rid()?)?;
    let image = native::macho_identity::read(source)?;
    let dylib = |item: native::macho_identity::Dylib| {
        json!({
            "install_name": item.name, "compatibility_version": item.compatibility.to_string(),
            "current_version": item.current.to_string()
        })
    };
    Ok(json!({"format": "mach_o", "architecture": native::rid()?,
        "identity": image.identity.map(dylib),
        "dependencies": image.dependencies.into_iter().map(dylib).collect::<Vec<_>>() }))
}

/// 先核验全体，再一次提交过滤；解析失败不会改变原计划，默认空声明零改动。
pub(super) fn apply(groups: Vec<Group>, payloads: &mut Vec<Payload>) -> Result<Value> {
    if groups.is_empty() {
        return Ok(json!([]));
    }
    // 一次规范化并建立目标索引；大型scope按有序前缀查找，逐成员审计无需重扫整个发行。
    let mut indexed = BTreeMap::new();
    for (index, payload) in payloads.iter().enumerate() {
        let target = destination(&payload.destination)?;
        ensure!(
            indexed.insert(target.clone(), index).is_none(),
            "部署选择遇到重复规范目标：{target}"
        );
    }
    let mut omitted = BTreeSet::new();
    let mut claimed = BTreeSet::new();
    let mut names = BTreeSet::new();
    let mut reports = Vec::new();
    for group in groups {
        ensure!(
            !group.name.trim().is_empty() && !group.reason.trim().is_empty(),
            "部署选择必须登记名称和理由"
        );
        ensure!(names.insert(group.name.clone()), "部署选择名称重复");
        let scope = destination(&group.scope)?;
        let members = targets(group.members)?;
        let selected = targets(group.selected)?;
        ensure!(
            !members.is_empty() && selected.is_subset(&members),
            "部署选择selected必须属于非空members全集"
        );
        let prefix = scope + "/";
        let actual = indexed
            .range(prefix.clone()..)
            .take_while(|(target, _)| target.starts_with(&prefix))
            .map(|(target, _)| target.clone())
            .collect::<BTreeSet<_>>();
        ensure!(
            actual == members,
            "部署选择实际成员与冻结全集不一致：{}",
            group.name
        );
        let mut files = Vec::new();
        for target in &members {
            ensure!(
                claimed.insert(target.clone()),
                "部署选择scope重叠：{target}"
            );
            let payload = &payloads[*indexed.get(target).context("部署选择成员丢失")?];
            let deployed = selected.contains(target);
            let image = inspect(&payload.source)?;
            ensure!(
                deployed || image["format"] == "mach_o",
                "部署选择只能省略实际原生映像，metadata/许可证/源码/资源继续部署：{target}"
            );
            files.push(json!({"source": payload.source, "destination": target,
                "selected": deployed, "image": image}));
            if !deployed {
                omitted.insert(target.clone());
            }
        }
        reports.push(json!({"name": group.name, "scope": group.scope, "reason": group.reason, "files": files}));
    }
    let removed: BTreeSet<_> = omitted.iter().map(|target| indexed[target]).collect();
    let mut index = 0;
    payloads.retain(|_| {
        let keep = !removed.contains(&index);
        index += 1;
        keep
    });
    Ok(Value::Array(reports))
}

#[cfg(test)]
mod tests {
    use super::*;
    fn group(members: &[&str], selected: &[&str]) -> Group {
        Group {
            name: "isolated-capability".into(),
            scope: "plugins".into(),
            members: members.iter().map(|s| (*s).into()).collect(),
            selected: selected.iter().map(|s| (*s).into()).collect(),
            reason: "明确部署功能".into(),
        }
    }
    #[test]
    fn closed_selection_preserves_resources_and_other_scopes() {
        let root = crate::test_temp::Root::new("payload-selection");
        let source = root.join("input");
        std::fs::write(&source, "手写资源").unwrap();
        let mut payloads = ["plugins/alpha", "plugins/beta", "other/resource"]
            .map(|d| Payload {
                source: source.clone(),
                destination: d.into(),
            })
            .to_vec();
        let report = apply(
            vec![group(
                &["plugins/alpha", "plugins/beta"],
                &["plugins/alpha", "plugins/beta"],
            )],
            &mut payloads,
        )
        .unwrap();
        assert_eq!(payloads.len(), 3);
        assert_eq!(report[0]["files"][1]["selected"], true);
        assert!(payloads.iter().any(|p| p.destination == "other/resource"));
    }
    #[test]
    fn stale_members_duplicates_escape_overlap_and_unknown_selection_are_rejected() {
        let root = crate::test_temp::Root::new("payload-selection-errors");
        let source = root.join("input");
        std::fs::write(&source, "input").unwrap();
        let original = vec![Payload {
            source,
            destination: "plugins/alpha".into(),
        }];
        for groups in [
            vec![group(&["plugins/alpha"], &[])],
            vec![group(&["plugins/missing"], &[])],
            vec![group(&["plugins/alpha", "plugins/./alpha"], &[])],
            vec![group(&["../escape"], &[])],
            vec![group(&["plugins/alpha"], &["plugins/unknown"])],
            {
                let mut second = group(&["plugins/alpha"], &["plugins/alpha"]);
                second.name = "independent-selection".into();
                vec![group(&["plugins/alpha"], &["plugins/alpha"]), second]
            },
        ] {
            let mut payloads = original.clone();
            assert!(apply(groups, &mut payloads).is_err());
            assert_eq!(payloads.len(), original.len());
        }
    }
    #[test]
    fn canonical_payload_aliases_cannot_hide_a_second_member_from_audit() {
        let root = crate::test_temp::Root::new("payload-selection-aliases");
        let source = root.join("input");
        std::fs::write(&source, "手写资源").unwrap();
        let mut payloads = ["plugins/alpha", "plugins/./alpha"]
            .map(|target| Payload {
                source: source.clone(),
                destination: target.into(),
            })
            .to_vec();
        let error = apply(
            vec![group(&["plugins/alpha"], &["plugins/alpha"])],
            &mut payloads,
        )
        .unwrap_err();
        assert!(error.to_string().contains("重复规范目标"));
        assert_eq!(payloads.len(), 2);
    }
}
