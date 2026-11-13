//! 构建期 Python 选择的纯合同：只消费已探测事实，不执行解释器、下载或改变 provider。
//!
//! 本模块尚未接入生产路径。版本可表示不代表运行支持；适配器与目标组合须另行验收。
//! 不解析 PEP 440 文本：调用方必须提供已正确解析的稳定发行约束，不能忽略未知语法。
use std::collections::BTreeMap;

/// 只表示正式 CPython 发行；预发行不能通过丢弃后缀转换进来。
#[derive(Clone, Copy, Debug, PartialEq, Eq, PartialOrd, Ord)]
pub struct Version {
    pub major: u16,
    pub minor: u16,
    pub patch: u16,
}

/// 已解析的约束原子。所有原子取交集；兼容发行、通配排除由正确的上层解析器展开。
/// 例如 ~=3.9.0 为 >=3.9.0 且 <3.10.0，不在这里猜测字符串语义。
#[derive(Clone, Debug, PartialEq, Eq)]
pub enum Constraint {
    Equal(Version),
    NotEqual(Version),
    AtLeast(Version),
    Greater(Version),
    AtMost(Version),
    Less(Version),
    ExcludeMinor { major: u16, minor: u16 },
}
impl Constraint {
    fn accepts(&self, version: Version) -> bool {
        match *self {
            Self::Equal(value) => version == value,
            Self::NotEqual(value) => version != value,
            Self::AtLeast(value) => version >= value,
            Self::Greater(value) => version > value,
            Self::AtMost(value) => version <= value,
            Self::Less(value) => version < value,
            Self::ExcludeMinor { major, minor } => (version.major, version.minor) != (major, minor),
        }
    }
}

/// 精确目标身份须含 RID、CPU、libc 或最低系统版本；跨编译不能填宿主探测值。
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Target {
    pub rid: String,
    pub architecture: String,
    pub platform_abi: String,
}

/// 冻结发行身份。相同版本号但不同发行/ABI/布局不能复用旧锁。
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Binding {
    pub distribution_id: String,
    pub version: Version,
    pub target: Target,
    pub soabi: String,
    pub shim_adapter: String,
    pub layout_id: String,
    pub source_id: String,
    pub dependency_set_id: String,
}

/// 探测证据由执行层提供；这些布尔值不代替真实文件或 ABI 校验。
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Facts {
    pub cpython: bool,
    pub stable_release: bool,
    pub gil_enabled: bool,
    pub debug_build: bool,
    pub shared_library: bool,
    pub headers: bool,
    pub licenses: bool,
    pub dependency_abi_matches: bool,
    pub complete_base_distribution: bool,
}

/// 实验支持与发布验收分开。此状态必须对应精确版本、发行、目标和依赖组合。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Support {
    Unsupported,
    Implemented,
    Verified,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq, PartialOrd, Ord)]
pub enum Origin {
    Explicit,
    ProjectEnvironment,
    ManagedCache,
    ManagedCatalog,
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Candidate {
    pub binding: Binding,
    pub facts: Facts,
    pub support: Support,
    pub origin: Origin,
    /// 解释器身份仅由隔离探测得出，不拿路径字符串推断版本。
    pub interpreter_id: Option<String>,
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub enum Selector {
    Exact(Version),
    Minor { major: u16, minor: u16 },
    Distribution(String),
    Interpreter(String),
}
impl Selector {
    fn accepts(&self, candidate: &Candidate) -> bool {
        let binding = &candidate.binding;
        match self {
            Self::Exact(version) => binding.version == *version,
            Self::Minor { major, minor } => {
                (binding.version.major, binding.version.minor) == (*major, *minor)
            }
            Self::Distribution(id) => binding.distribution_id == *id,
            Self::Interpreter(id) => candidate.interpreter_id.as_ref() == Some(id),
        }
    }
}

/// 内存请求 schema 1。与现有 ryupy-cpython.json schema 完全独立，不提供生产反序列化入口。
#[derive(Clone, Debug)]
pub struct Request {
    pub target: Target,
    pub requires_python: Vec<Constraint>,
    pub explicit: Option<Selector>,
    /// 项目单值版本 pin；显式选择存在时由显式选择覆盖，requires_python 永不被覆盖。
    pub project_pin: Option<Selector>,
    pub lock: Option<Binding>,
    pub update_lock: bool,
    pub allow_download: bool,
    /// 仅显式选择时有效；允许已实现但未验收的组合，不允许无适配器的未来版本。
    pub allow_experimental: bool,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq, PartialOrd, Ord)]
pub enum Rejection {
    InvalidIdentity,
    NotCPython,
    VersionBelow39,
    NotStable,
    TargetMismatch,
    FreeThreaded,
    DebugBuild,
    MissingSharedLibrary,
    MissingHeaders,
    MissingLicenses,
    IncompleteDistribution,
    DependencyAbiMismatch,
    UnsupportedAdapter,
    NotVerified,
    RequiresPython,
    SelectorMismatch,
    DownloadDisabled,
    UnrequestedExplicitCandidate,
}

#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Rejected {
    pub distribution_id: String,
    pub reasons: Vec<Rejection>,
}
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum FailureCode {
    InvalidRequest,
    ConflictingCandidateIdentity,
    LockNeedsUpdate,
    NoCandidate,
    AmbiguousDistribution,
}
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Failure {
    pub code: FailureCode,
    pub rejected: Vec<Rejected>,
    /// 涉及的发行ID；歧义/事实冲突即使没有过滤拒绝，也能定位候选。
    pub candidate_ids: Vec<String>,
}
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum SelectionReason {
    Locked,
    Explicit,
    ProjectPin,
    ProjectEnvironment,
    ManagedCache,
    ManagedDownload,
}
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Resolution {
    pub binding: Binding,
    pub support: Support,
    pub reason: SelectionReason,
    /// 仅准备下载计划，绝不是下载已完成。
    pub needs_download: bool,
    pub rejected: Vec<Rejected>,
}

fn valid_target(target: &Target) -> bool {
    [&target.rid, &target.architecture, &target.platform_abi]
        .iter()
        .all(|value| !value.trim().is_empty())
}

fn rejections(request: &Request, candidate: &Candidate) -> Vec<Rejection> {
    let binding = &candidate.binding;
    let facts = &candidate.facts;
    let selector = request.explicit.as_ref().or(request.project_pin.as_ref());
    let checks = [
        (
            Rejection::UnrequestedExplicitCandidate,
            candidate.origin == Origin::Explicit && request.explicit.is_none(),
        ),
        (
            Rejection::InvalidIdentity,
            !valid_target(&binding.target)
                || [
                    &binding.distribution_id,
                    &binding.soabi,
                    &binding.shim_adapter,
                    &binding.layout_id,
                    &binding.source_id,
                    &binding.dependency_set_id,
                ]
                .iter()
                .any(|value| value.trim().is_empty()),
        ),
        (Rejection::NotCPython, !facts.cpython),
        (
            Rejection::VersionBelow39,
            binding.version.major != 3 || binding.version.minor < 9,
        ),
        (Rejection::NotStable, !facts.stable_release),
        (Rejection::TargetMismatch, binding.target != request.target),
        (Rejection::FreeThreaded, !facts.gil_enabled),
        (Rejection::DebugBuild, facts.debug_build),
        (Rejection::MissingSharedLibrary, !facts.shared_library),
        (Rejection::MissingHeaders, !facts.headers),
        (Rejection::MissingLicenses, !facts.licenses),
        (
            Rejection::IncompleteDistribution,
            !facts.complete_base_distribution,
        ),
        (
            Rejection::DependencyAbiMismatch,
            !facts.dependency_abi_matches,
        ),
        (
            Rejection::UnsupportedAdapter,
            candidate.support == Support::Unsupported,
        ),
        (
            Rejection::NotVerified,
            candidate.support == Support::Implemented
                && !(request.allow_experimental && request.explicit.is_some()),
        ),
        (
            Rejection::RequiresPython,
            !request
                .requires_python
                .iter()
                .all(|item| item.accepts(binding.version)),
        ),
        (
            Rejection::SelectorMismatch,
            selector.is_some_and(|value| !value.accepts(candidate)),
        ),
        (
            Rejection::DownloadDisabled,
            candidate.origin == Origin::ManagedCatalog && !request.allow_download,
        ),
    ];
    checks
        .into_iter()
        .filter_map(|(reason, reject)| reject.then_some(reason))
        .collect()
}

/// 确定性决策：显式约束/锁定优先；项目环境先于缓存，缓存先于目录下载计划。
/// 输入只能是可信探测器/验收登记的事实；本函数不是安全或发行验证边界。
pub fn resolve(request: &Request, candidates: &[Candidate]) -> Result<Resolution, Failure> {
    let failure = |code, rejected| Failure {
        code,
        rejected,
        candidate_ids: candidates
            .iter()
            .map(|candidate| candidate.binding.distribution_id.clone())
            .collect::<std::collections::BTreeSet<_>>()
            .into_iter()
            .collect(),
    };
    if !valid_target(&request.target)
        || matches!(
            request.project_pin,
            Some(Selector::Distribution(_) | Selector::Interpreter(_))
        )
    {
        return Err(failure(FailureCode::InvalidRequest, vec![]));
    }
    // 一个发行可同时从环境、缓存、目录发现，解释器路径也可能有别名。
    // 只有不可变事实冲突才拒绝；对同一发行先筛选再取最优可用来源。
    let mut unique: BTreeMap<&str, Vec<&Candidate>> = BTreeMap::new();
    for candidate in candidates {
        let group = unique
            .entry(&candidate.binding.distribution_id)
            .or_default();
        group.push(candidate);
    }
    // 先按发行ID检查冲突，确保输入乱序时仍报告同一个冲突。
    for (id, group) in &unique {
        let first = group[0];
        if group.iter().any(|candidate| {
            candidate.binding != first.binding
                || candidate.facts != first.facts
                || candidate.support != first.support
        }) {
            return Err(Failure {
                code: FailureCode::ConflictingCandidateIdentity,
                rejected: vec![],
                candidate_ids: vec![(*id).into()],
            });
        }
    }
    let mut rejected = Vec::new();
    let mut eligible = Vec::new();
    for (id, group) in unique {
        let mut reasons = Vec::new();
        let mut accepted = Vec::new();
        for candidate in group {
            let current = rejections(request, candidate);
            if current.is_empty() {
                accepted.push(candidate);
            } else {
                reasons.extend(current);
            }
        }
        if let Some(best) = accepted
            .into_iter()
            .min_by_key(|candidate| candidate.origin)
        {
            eligible.push(best);
        } else {
            reasons.sort();
            reasons.dedup();
            rejected.push(Rejected {
                distribution_id: id.into(),
                reasons,
            });
        }
    }
    let result = |candidate: &Candidate, reason, rejected| Resolution {
        binding: candidate.binding.clone(),
        support: candidate.support,
        reason,
        needs_download: candidate.origin == Origin::ManagedCatalog,
        rejected,
    };
    if let Some(lock) = &request.lock
        && !request.update_lock
    {
        return eligible
            .iter()
            .find(|candidate| candidate.binding == *lock)
            .map(|candidate| result(candidate, SelectionReason::Locked, rejected.clone()))
            .ok_or_else(|| failure(FailureCode::LockNeedsUpdate, rejected));
    }
    let Some(best_origin) = eligible.iter().map(|candidate| candidate.origin).min() else {
        return Err(failure(FailureCode::NoCandidate, rejected));
    };
    eligible.retain(|candidate| candidate.origin == best_origin);
    let best_version = eligible
        .iter()
        .map(|candidate| candidate.binding.version)
        .max()
        .unwrap();
    eligible.retain(|candidate| candidate.binding.version == best_version);
    if eligible.len() != 1 {
        return Err(Failure {
            code: FailureCode::AmbiguousDistribution,
            rejected,
            candidate_ids: eligible
                .iter()
                .map(|candidate| candidate.binding.distribution_id.clone())
                .collect(),
        });
    }
    let candidate = eligible[0];
    let reason = if request.explicit.is_some() {
        SelectionReason::Explicit
    } else if request.project_pin.is_some() {
        SelectionReason::ProjectPin
    } else {
        match candidate.origin {
            Origin::Explicit => return Err(failure(FailureCode::InvalidRequest, rejected)),
            Origin::ProjectEnvironment => SelectionReason::ProjectEnvironment,
            Origin::ManagedCache => SelectionReason::ManagedCache,
            Origin::ManagedCatalog => SelectionReason::ManagedDownload,
        }
    };
    Ok(result(candidate, reason, rejected))
}

#[cfg(test)]
#[path = "python_selection_tests.rs"]
mod tests;
