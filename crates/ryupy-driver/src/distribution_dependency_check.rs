//! 显式无条件依赖闭包：只读同次冻结metadata，验证已安装版本，不求解、不执行包或下载。
use super::{Distribution, Snapshot, files};
use anyhow::{Context, Result, bail, ensure};
use pep440_rs::{Operator, Version};
use pep508_rs::{ExtraName, PackageName, Requirement, VerbatimUrl, VersionOrUrl};
use serde::Deserialize;
use serde_json::{Value, json};
use std::collections::{BTreeSet, VecDeque};
use std::str::FromStr;

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
pub(super) struct Config {
    mode: String,
    requirements: Vec<String>,
}

/// 所有边都检查约束，只有展开分发去重，因此环不会吞掉回边冲突。
pub(super) fn validate(config: &Config, snapshot: &Snapshot) -> Result<Value> {
    ensure!(
        config.mode == "unconditional_closure",
        "依赖预检只支持unconditional_closure"
    );
    ensure!(
        !config.requirements.is_empty() && config.requirements.len() <= 64,
        "依赖预检根约束必须为1到64项"
    );
    let mut queue: VecDeque<_> = config
        .requirements
        .iter()
        .map(|s| ("<root>".to_owned(), s.clone()))
        .collect();
    let mut visited = BTreeSet::new();
    let mut checks = Vec::new();
    while let Some((from, raw)) = queue.pop_front() {
        ensure!(checks.len() < 4096, "依赖预检边超过4096项");
        let requirement = parse(&raw).with_context(|| format!("依赖预检：{from} -> {raw}"))?;
        let name = requirement.name.to_string();
        // 只查询所达名称，但同时检查Name与运行时路径key两种身份，不能误取影子版本。
        let matches: Vec<_> = snapshot
            .distributions
            .iter()
            .filter(|d| {
                canonical(&d.metadata_name).is_ok_and(|n| n == name)
                    || canonical(&d.discovery_key).is_ok_and(|n| n == name)
            })
            .collect();
        ensure!(
            !matches.is_empty(),
            "依赖预检缺少分发：{from} -> {raw}（需要{name}）"
        );
        ensure!(matches.len() == 1, "依赖预检分发身份歧义：{from} -> {raw}");
        let dist = matches[0];
        ensure!(
            !dist.shadowed
                && canonical(&dist.metadata_name)? == name
                && canonical(&dist.discovery_key)? == name,
            "依赖预检Name与运行时discovery_key不一致或被遮蔽：{}:{}",
            dist.root,
            dist.path
        );
        let version = Version::from_str(&dist.metadata_version)
            .with_context(|| format!("依赖预检版本不是PEP440：{name} {}", dist.metadata_version))?;
        if let Some(VersionOrUrl::VersionSpecifier(spec)) = &requirement.version_or_url {
            ensure!(
                spec.contains(&version),
                "依赖预检版本不满足：{from} -> {raw}；已安装{name}=={}",
                dist.metadata_version
            );
        }
        checks.push(
            json!({"from":from,"requirement":raw,"name":name,"version":dist.metadata_version,
            "root":dist.root,"path":dist.path}),
        );
        if visited.insert(name.clone()) {
            for dependency in dependencies(dist).with_context(|| format!("依赖预检读取{name}"))?
            {
                ensure!(queue.len() + checks.len() < 4096, "依赖预检边超过4096项");
                queue.push_back((name.clone(), dependency));
            }
        }
    }
    Ok(
        json!({"schema_version":1,"mode":config.mode,"requirements":config.requirements,
        "validated_names":visited,"checks":checks}),
    )
}

fn canonical(name: &str) -> Result<String> {
    Ok(PackageName::from_str(name)
        .context("依赖预检分发名称不是PEP508名称")?
        .to_string())
}

fn parse(raw: &str) -> Result<Requirement<VerbatimUrl>> {
    ensure!(raw.len() <= 16 * 1024, "单条依赖约束超过16KiB");
    let mut warnings = Vec::new();
    let result = Requirement::<VerbatimUrl>::parse_reporter(raw, ".", &mut |kind, message| {
        warnings.push(format!("{kind:?}: {message}"));
    })
    .context("不是有效PEP508依赖")?;
    ensure!(
        warnings.is_empty(),
        "依赖解析警告不允许静默降级：{}",
        warnings.join("; ")
    );
    ensure!(
        result.extras.is_empty(),
        "unconditional_closure不支持请求extras：{raw}"
    );
    ensure!(
        !matches!(result.version_or_url, Some(VersionOrUrl::Url(_))),
        "unconditional_closure不支持URL依赖：{raw}"
    );
    if let Some(VersionOrUrl::VersionSpecifier(spec)) = &result.version_or_url {
        // 库的ExactEqual对Version规范化后比较，不能证明PEP440任意原文字符串相等。
        ensure!(
            !spec.iter().any(|s| *s.operator() == Operator::ExactEqual),
            "unconditional_closure不支持任意字符串等号===：{raw}"
        );
    }
    // 模式不承诺任何目标环境，即使marker被库化简为True也保守拒绝显式条件。
    ensure!(
        !raw.contains(';') && result.marker.is_true(),
        "unconditional_closure不支持环境marker：{raw}"
    );
    Ok(result)
}

/// 只从头部取字段；Description正文中的Requires-Dist不是依赖声明。
fn headers(text: &str) -> Result<Vec<(String, String)>> {
    let mut result: Vec<(String, String)> = Vec::new();
    for line in text.lines() {
        if line.is_empty() {
            break;
        }
        if line.starts_with([' ', '\t']) {
            let (_, value) = result.last_mut().context("metadata无前置字段的折行")?;
            value.push(' ');
            value.push_str(line.trim());
            continue;
        }
        let (name, value) = line.split_once(':').context("metadata头部缺少冒号")?;
        ensure!(
            !name.is_empty() && name.bytes().all(|b| b.is_ascii_alphanumeric() || b == b'-'),
            "metadata字段名无效"
        );
        result.push((name.to_ascii_lowercase(), value.trim().to_owned()));
    }
    Ok(result)
}

fn content(dist: &Distribution, name: &str) -> Result<Option<String>> {
    dist.files
        .iter()
        .find(|f| f.path == name)
        .map(|f| {
            String::from_utf8(files::unhex(&f.hex)?).with_context(|| format!("{name}不是UTF-8"))
        })
        .transpose()
}

fn dependencies(dist: &Distribution) -> Result<Vec<String>> {
    let source = if dist.kind == "dist-info" {
        "METADATA"
    } else {
        "PKG-INFO"
    };
    let text = content(dist, source)?.context("分发缺少metadata")?;
    let headers = headers(&text)?;
    let versions: Vec<_> = headers
        .iter()
        .filter(|(name, _)| name == "metadata-version")
        .map(|(_, value)| value.as_str())
        .collect();
    ensure!(
        versions.len() == 1
            && matches!(
                versions[0],
                "1.0" | "1.1" | "1.2" | "2.1" | "2.2" | "2.3" | "2.4"
            ),
        "依赖预检Metadata-Version缺失、歧义或超出支持版本1.0/1.1/1.2/2.1/2.2/2.3/2.4"
    );

    let mut result = Vec::new();
    for (name, value) in &headers {
        match name.as_str() {
            "requires-dist" => {
                ensure!(!value.is_empty(), "Requires-Dist为空");
                result.push(value.clone());
            }
            "requires" => bail!("旧Requires字段不支持；不能当成无依赖"),
            "dynamic"
                if value
                    .split(',')
                    .any(|v| v.trim().eq_ignore_ascii_case("Requires-Dist")) =>
            {
                bail!("动态Requires-Dist不能作封闭依赖证明");
            }
            "name" => ensure!(value == &dist.metadata_name, "折行Name与快照身份不一致"),
            "version" => ensure!(
                value == &dist.metadata_version,
                "折行Version与快照身份不一致"
            ),
            _ => {}
        }
    }
    if dist.kind == "egg-info"
        && let Some(text) = content(dist, "requires.txt")?
    {
        ensure!(
            result.is_empty() || text.trim().is_empty(),
            "Requires-Dist与requires.txt同时声明，依赖权威歧义"
        );
        let mut optional = false;
        for line in text
            .lines()
            .map(str::trim)
            .filter(|l| !l.is_empty() && !l.starts_with('#'))
        {
            if line.starts_with('[') {
                let section = line
                    .strip_prefix('[')
                    .and_then(|s| s.strip_suffix(']'))
                    .context("requires.txt分节格式无效")?;
                let extra = section.split_once(':').map_or(section, |(extra, _)| extra);
                ensure!(
                    !extra.is_empty(),
                    "unconditional_closure不支持requires.txt base条件段：{line}"
                );
                ExtraName::from_str(extra).context("requires.txt extra名称无效")?;
                optional = true;
            } else if !optional {
                result.push(line.to_owned());
            }
        }
    }
    Ok(result)
}

#[cfg(test)]
#[path = "distribution_dependency_tests.rs"]
mod tests;
