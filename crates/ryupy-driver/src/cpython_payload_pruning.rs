//! 显式裁剪锁定 wheel 的可选能力：只允许删其独占文件，完整记录删除项，保留许可与发行元数据。
//! 选后仍走原生加载闭包校验；Python 动态导入不能由 Mach-O 保证，须由产品独立测试。
use super::{Payload, destination};
use anyhow::{Context, Result, ensure};
use serde::Deserialize;
use serde_json::{Value, json};
use std::collections::{BTreeMap, BTreeSet};

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
pub(super) struct Request {
    distribution: String,
    version: String,
    omit: Vec<String>,
    reason: String,
}

pub(super) fn apply(
    requests: Vec<Request>,
    report: &Value,
    payloads: &mut Vec<Payload>,
) -> Result<Value> {
    if requests.is_empty() {
        return Ok(json!([]));
    }
    let distributions = report["distributions"]
        .as_array()
        .context("裁剪需要冻结 wheel 文件所有权")?;
    let mut owners = BTreeMap::<String, Vec<&Value>>::new();
    for distribution in distributions {
        for path in distribution["files"].as_array().context("发行缺文件表")? {
            let path = destination(path.as_str().context("发行文件名无效")?)?;
            owners.entry(path).or_default().push(distribution);
        }
    }
    let mut available = BTreeMap::new();
    for payload in payloads.iter() {
        let path = destination(&payload.destination)?;
        ensure!(
            available.insert(path.clone(), payload).is_none(),
            "裁剪遇到重复目标：{path}"
        );
    }
    let mut removed = BTreeSet::new();
    let mut rows = Vec::new();
    for request in requests {
        ensure!(
            !request.omit.is_empty() && !request.reason.trim().is_empty(),
            "裁剪需要目标与理由"
        );
        ensure!(
            distributions
                .iter()
                .any(|dist| dist["name"] == request.distribution
                    && dist["version"] == request.version),
            "裁剪发行或版本不匹配：{} {}",
            request.distribution,
            request.version
        );
        let mut files = Vec::new();
        let mut bytes = 0u64;
        for raw in &request.omit {
            let target = destination(raw)?;
            let prefix = format!("{target}/");
            let matched = available.get_key_value(&target).into_iter().chain(
                available
                    .range(prefix.clone()..)
                    .take_while(|(path, _)| path.starts_with(&prefix)),
            );
            let mut count = 0;
            for (path, payload) in matched {
                count += 1;
                let owned = owners
                    .get(path)
                    .filter(|records| records.len() == 1)
                    .and_then(|records| records.first())
                    .is_some_and(|dist| {
                        dist["name"] == request.distribution && dist["version"] == request.version
                    });
                ensure!(owned, "裁剪文件并非目标 wheel 独占：{path}");
                ensure!(!protected(path), "不能裁剪发行元数据或许可证：{path}");
                ensure!(removed.insert(path.clone()), "裁剪范围重叠：{path}");
                bytes += payload.source.metadata()?.len();
                files.push(path.clone());
            }
            ensure!(count != 0, "裁剪目标在冻结发行中不存在：{target}");
        }
        files.sort();
        rows.push(
            json!({"distribution": request.distribution, "version": request.version,
            "reason": request.reason, "omitted_files": files, "source_bytes": bytes}),
        );
    }
    // 只有全部请求验证成功才改变计划；保留顺序和未选择的路径。
    payloads.retain(|payload| {
        !destination(&payload.destination).is_ok_and(|path| removed.contains(&path))
    });
    Ok(Value::Array(rows))
}

fn protected(path: &str) -> bool {
    path.split('/').any(|part| {
        let name = part.to_ascii_lowercase();
        name.ends_with(".dist-info")
            || name.ends_with(".egg-info")
            || name.starts_with("license")
            || name.starts_with("licence")
            || name.starts_with("copying")
            || name.starts_with("notice")
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn exact_wheel_paths_omit_subtree_but_preserve_other_version_and_metadata() {
        let root = crate::test_temp::Root::new("wheel-pruning");
        let source = root.join("data");
        std::fs::write(&source, "test").unwrap();
        let paths = [
            "first/addon/tool",
            "first/addon/doc",
            "first/addons.py",
            "first/LICENSE",
        ];
        let original = paths
            .map(|path| Payload {
                source: source.clone(),
                destination: path.into(),
            })
            .to_vec();
        let report = json!({"distributions":[{"name":"first","version":"1.2","files":paths}]});
        let request = |version: &str, path: &str| Request {
            distribution: "first".into(),
            version: version.into(),
            omit: vec![path.into()],
            reason: "本应用未启用".into(),
        };
        let mut payloads = original.clone();
        for (version, path) in [
            ("1.1", "first/addon"),
            ("1.2", "first/LICENSE"),
            ("1.2", "first/absent"),
            ("1.2", "../first"),
        ] {
            assert!(apply(vec![request(version, path)], &report, &mut payloads).is_err());
            assert_eq!(payloads.len(), original.len());
        }
        let audit = apply(vec![request("1.2", "first/addon")], &report, &mut payloads).unwrap();
        assert_eq!(audit[0]["source_bytes"], 8);
        assert_eq!(
            payloads
                .iter()
                .map(|item| item.destination.as_str())
                .collect::<Vec<_>>(),
            paths[2..]
        );
    }
}
