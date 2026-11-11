//! 可选源码工作队列：hard/soft失败强度与candidate/active注册状态互相独立。
//! 只在实际JIT产品会话启用，不更改旧公开硬图、AOT或fixed-binding接口。
use crate::{
    Session,
    modules::{SourceKind, SourceModule},
};
use anyhow::{Context, Result, ensure};
use std::collections::{BTreeMap, BTreeSet, VecDeque};
use std::path::{Path, PathBuf};

/// Session公开结构的兼容载体；外部只能使用Default，不能配置内部候选/来源权限。
///
/// 外部调用方保留结构更新，但不能写入部署位置或伪造候选计划：
/// ```compile_fail
/// let mut state = ryupy_driver::DefaultSourceState::default();
/// state.output = Some(std::path::PathBuf::from("untrusted"));
/// ```
/// ```compile_fail
/// let mut state = ryupy_driver::DefaultSourceState::default();
/// state.plan = Default::default();
/// ```
/// catalog认证令牌也没有公开构造路径：
/// ```compile_fail
/// use ryupy_driver::source_catalog::Token;
/// ```
#[doc(hidden)]
#[derive(Default)]
pub struct DefaultSourceState {
    pub(crate) output: Option<PathBuf>,
    pub(crate) plan: Plan,
    pub(crate) diagnostics: BTreeMap<String, Vec<String>>,
}

#[derive(Default)]
pub(crate) struct Plan {
    pub payload: Option<String>,
    pub metadata_unavailable: Option<String>,
    pub roots: BTreeSet<String>,
    pub dependencies: BTreeMap<String, Vec<String>>,
    pub deferred: BTreeMap<String, String>,
    pub outside: BTreeSet<String>,
    pub protected: Vec<String>,
    pub locations: BTreeMap<String, serde_json::Value>,
}
impl Plan {
    pub(crate) fn runtime_payload(&self) -> Result<Option<String>> {
        if self.roots.is_empty() && self.outside.is_empty() {
            return Ok(None);
        }
        Ok(Some(serde_json::to_string(&serde_json::json!({
            "schema_version": 1, "roots": self.roots, "dependencies": self.dependencies,
            "deferred": self.deferred, "outside": self.outside,
            "protected": self.protected, "locations": self.locations,
        }))?))
    }
}

/// 有限节点与字节预算不依赖宿主安装环境；每身份首次发现/升级才传播。
pub(crate) fn extend(
    sess: &mut Session,
    entry: &Path,
    roots: &[PathBuf],
    sources: &mut Vec<SourceModule>,
    explicit_payload: Option<String>,
    aot: bool,
) -> Result<()> {
    sess.default_source_state.plan = Plan {
        payload: explicit_payload,
        ..Default::default()
    };
    if !enabled(sess, aot) {
        return Ok(());
    }
    let Some(out) = sess.default_source_state.output.clone() else {
        return Ok(());
    };
    sess.default_source_state.diagnostics.clear();
    let mut plan = std::mem::take(&mut sess.default_source_state.plan);
    plan.protected =
        crate::modules::native_exception_modules_for_build(entry, &roots[1..], sources)
            .into_iter()
            .map(str::to_owned)
            .collect();
    let active: BTreeSet<_> = sources.iter().map(|source| source.name.clone()).collect();
    let mut nodes: BTreeMap<String, usize> = sources
        .iter()
        .enumerate()
        .map(|(index, source)| (source.name.clone(), index))
        .collect();
    let mut queue = VecDeque::new();
    let mut groups = Vec::new();
    let mut metadata_requested = false;
    for source in sources.iter() {
        for name in requests(source) {
            // 尚无用户源码的静态导入仍不把native替换为活动source。
            queue.push_back((name, true, false));
        }
        let hints = crate::module_hints::collect(&source.parsed);
        metadata_requested |= hints.metadata;
        groups.extend(hints.groups);
        for name in hints.modules {
            plan.roots.insert(name.clone());
            queue.push_back((name, false, false));
        }
    }
    if metadata_requested {
        metadata_candidates(&mut plan, roots, &groups, &mut queue)?;
    }
    let mut bytes = 0usize;
    while let Some((name, hard, dependency_role)) = queue.pop_front() {
        if active.contains(&name) {
            continue;
        }
        if let Some(&index) = nodes.get(&name) {
            verify_identity(roots, &sources[index])?;
            if hard && !sources[index].hard {
                sources[index].hard = true;
                for dependency in plan.dependencies.get(&name).into_iter().flatten() {
                    queue.push_back((dependency.clone(), true, true));
                }
            }
            continue;
        }
        if !crate::module_hints::valid_name(&name) {
            continue;
        }
        let top = name.split('.').next().unwrap_or("");
        let user = crate::modules::resolve_in(roots, &name);
        let (path, kind, catalog) = if let Some((path, kind)) = user {
            (path, kind, None)
        } else if crate::modules::resolve_in(roots, top).is_some() {
            // 普通用户父包已确定，即使缺子级也不能跨root/catalog补齐。
            continue;
        } else if hard && !dependency_role && !crate::source_catalog::direct_request(&name) {
            continue;
        } else if let Some((path, kind, token)) = crate::source_catalog::resolve(&out, &name)? {
            // 普通父包仅作为依赖族成员，不把静态出现的native父变成请求目标。
            if hard && !dependency_role {
                plan.roots.insert(name.clone());
            }
            (path, kind, Some(token))
        } else {
            continue;
        };
        if sources.len() >= 512 || bytes >= 4 * 1024 * 1024 {
            ensure!(!hard, "默认来源硬图超过512节点或4MiB");
            plan.outside.insert(name);
            continue;
        }
        if std::fs::symlink_metadata(&path)?.len() > (4 * 1024 * 1024 - bytes) as u64 {
            ensure!(!hard, "默认来源硬图字节预算超限");
            plan.outside.insert(name);
            continue;
        }
        let raw = crate::source_deployment::read(&path, 4 * 1024 * 1024 - bytes)?;
        bytes += raw.len();
        if let Some(token) = &catalog {
            ensure!(token.authenticates(&path, &raw), "catalog最终来源认证失败");
        }
        let text = String::from_utf8(raw).context("候选源码不是UTF-8")?;
        let resolved = if path.is_absolute() {
            path
        } else {
            std::env::current_dir()?.join(path)
        };
        let file = sess
            .sources
            .add(resolved.display().to_string(), text.clone());
        let (parsed, parse_diagnostics) = ryupy_syntax::parse(file, &text);
        let package = crate::modules::module_package(&name, &resolved);
        let display = resolved
            .file_name()
            .context("候选来源缺文件名")?
            .to_string_lossy()
            .into_owned();
        let source = SourceModule {
            candidate: true,
            hard,
            catalog,
            parse_diagnostics,
            kind,
            resource_discovery: None,
            resolved_source_path: resolved,
            logical_source: crate::modules::logical_source(&name, &display, kind),
            name: name.clone(),
            package,
            file: display,
            parsed,
            text,
        };
        let hints = crate::module_hints::collect(&source.parsed);
        for target in hints.modules {
            plan.roots.insert(target.clone());
            queue.push_back((target, false, false));
        }
        if hints.metadata {
            groups.extend(hints.groups);
            metadata_candidates(&mut plan, roots, &groups, &mut queue)?;
        }
        let mut dependencies = requests(&source);
        dependencies.extend(crate::modules::package_prefixes(&name));
        dependencies.sort();
        dependencies.dedup();
        for dependency in &dependencies {
            queue.push_back((dependency.clone(), hard, true));
        }
        // 同名节点只插入一次；后续只升级强度，不以最后一次读取覆盖来源。
        plan.locations.insert(name.clone(), serde_json::json!({
            "package": source.package, "file": source.file,
            "path": source.resolved_source_path, "is_package": source.kind == SourceKind::Package,
        }));
        nodes.insert(name.clone(), sources.len());
        plan.dependencies.insert(name, dependencies);
        sources.push(source);
    }
    // active身份永不被Outside/Deferred状态遮蔽；无候选的提示不改变普通native查找。
    plan.roots.retain(|name| {
        nodes
            .get(name)
            .is_some_and(|&index| sources[index].candidate)
    });
    plan.outside.retain(|name| !nodes.contains_key(name));
    if let Some(payload) = &plan.payload {
        crate::distribution_metadata::record_payload(sess, payload)?;
    }
    sess.default_source_state.plan = plan;
    Ok(())
}
/// 同名队列节点只能升级既有真实身份；来源/种类改变是显式冲突而非最后写者覆盖。
fn verify_identity(roots: &[PathBuf], source: &SourceModule) -> Result<()> {
    let user = crate::modules::resolve_in(roots, &source.name);
    if source.catalog.is_some() {
        let top = source.name.split('.').next().unwrap_or("");
        ensure!(
            user.is_none() && crate::modules::resolve_in(roots, top).is_none(),
            "候选catalog与用户源码身份冲突：{}",
            source.name
        );
    } else {
        let (path, kind) = user.context("候选来源在固定点期间消失")?;
        let absolute = if path.is_absolute() {
            path
        } else {
            std::env::current_dir()?.join(path)
        };
        ensure!(
            absolute == source.resolved_source_path && kind == source.kind,
            "同名候选的来源/种类冲突：{}",
            source.name
        );
    }
    Ok(())
}

/// inventory只采集一次；后续候选发现的新group只重新选择完整静态快照。
fn metadata_candidates(
    plan: &mut Plan,
    roots: &[PathBuf],
    groups: &[String],
    queue: &mut VecDeque<(String, bool, bool)>,
) -> Result<()> {
    if plan.payload.is_none() && plan.metadata_unavailable.is_none() {
        match crate::distribution_metadata::automatic(roots)? {
            Ok(payload) => plan.payload = Some(payload),
            Err(reason) => plan.metadata_unavailable = Some(reason),
        }
    }
    if let Some(payload) = &plan.payload {
        let (targets, outside) = crate::distribution_metadata::targets(payload, groups)?;
        plan.outside.extend(outside);
        for name in targets {
            plan.outside.remove(&name);
            plan.roots.insert(name.clone());
            queue.push_back((name, false, false));
        }
    }
    Ok(())
}

fn requests(source: &SourceModule) -> Vec<String> {
    let mut requests = Vec::new();
    for request in
        ryupy_hir::imports::collect_all_imports(&source.parsed.module.body, &source.package)
    {
        requests.push(request.module.clone());
        if let Some(name) = request.name {
            requests.push(format!("{}.{name}", request.module));
        }
    }
    requests
}

/// 单SourceDb、逐节点诊断桶；只有明确产生点标记的软失败可以延期。
pub(crate) fn compile<T>(
    sess: &mut Session,
    source: &SourceModule,
    run: impl FnOnce(&mut Session) -> Result<T>,
) -> Result<Option<T>> {
    if !source.candidate {
        return run(sess).map(Some);
    }
    let retained = std::mem::take(&mut sess.diagnostics);
    sess.diagnostics.extend(source.parse_diagnostics.clone());
    let result = if sess.diagnostics.has_errors() {
        Err(crate::HasErrors.into())
    } else {
        run(sess)
    };
    let diagnostics = std::mem::replace(&mut sess.diagnostics, retained);
    let errors: Vec<_> = diagnostics
        .iter()
        .filter(|diagnostic| diagnostic.is_error())
        .collect();
    if !source.hard
        && result
            .as_ref()
            .is_err_and(|error| error.is::<crate::HasErrors>())
        && !errors.is_empty()
        && errors.iter().all(|error| error.deferred_failure.is_some())
    {
        let reason = if errors.iter().any(|error| {
            error.deferred_failure == Some(ryupy_base::diagnostic::DeferredFailure::ParseRejected)
        }) {
            "bad source loading is not implemented"
        } else {
            "source module support boundary is not implemented"
        };
        sess.default_source_state
            .plan
            .deferred
            .insert(source.name.clone(), reason.into());
        // 原诊断全文留同次报告，不丢来源、Span、错误码与原文字。
        let audit: Vec<_> = diagnostics
            .iter()
            .map(|d| ryupy_base::diagnostic::render(&sess.sources, d, false))
            .collect();
        sess.default_source_state
            .diagnostics
            .insert(source.name.clone(), audit);
        return Ok(None);
    }
    sess.diagnostics.extend(diagnostics.iter().cloned());
    result.map(Some)
}

/// 同次构建审计保留候选身份与延期诊断；该文件不是运行时来源。
pub(crate) fn write_report(sess: &Session, out: &Path) -> Result<()> {
    if let Some(payload) = sess.default_source_state.plan.runtime_payload()? {
        let mut value: serde_json::Value = serde_json::from_str(&payload)?;
        value["diagnostics"] = serde_json::to_value(&sess.default_source_state.diagnostics)?;
        value["metadata_unavailable"] =
            serde_json::to_value(&sess.default_source_state.plan.metadata_unavailable)?;
        std::fs::write(
            out.join("optional-sources.json"),
            serde_json::to_vec_pretty(&value)?,
        )?;
    }
    Ok(())
}

/// 单一策略门，显式关闭/AOT/fixed-binding均保既有接口。
pub(crate) fn enabled(sess: &Session, aot: bool) -> bool {
    !aot && !sess.fixed_binding_build
        && sess.module_environment_policy != crate::ModuleEnvironmentPolicy::ExplicitOnly
}
