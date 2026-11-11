//! 一次编译的模块环境计划；build/dump/link共同消费，运行时仍只有既有两种ABI。
//! 试点仅由编译期feature默认开启，不读取环境变量、项目名表或用户公开元数据。
use crate::{
    Session,
    modules::{SourceKind, SourceModule},
};
use ryupy_hir::{OrdinaryDynamicEligibility, ordinary_dynamic_eligibility};
use ryupy_mir::ModuleExecutionKind;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ModuleEnvironmentPolicy {
    ExplicitOnly,
    OrdinaryDynamicPilot,
}
impl Default for ModuleEnvironmentPolicy {
    fn default() -> Self {
        if cfg!(feature = "ordinary-dynamic-module-pilot") {
            Self::OrdinaryDynamicPilot
        } else {
            Self::ExplicitOnly
        }
    }
}
impl ModuleEnvironmentPolicy {
    pub fn identity(self) -> &'static str {
        match self {
            Self::ExplicitOnly => "explicit-only",
            Self::OrdinaryDynamicPilot => "ordinary-dynamic-module-pilot",
        }
    }
}

/// 计划只决定编译期环境，不能在模块已发生副作用后切换或重放。
#[derive(Debug, Clone)]
pub(crate) struct ModuleEnvironmentPlan {
    pub capabilities: ryupy_base::InstanceCapabilities,
    pub execution: ModuleExecutionKind,
    pub automatic: bool,
    pub reason: String,
}
impl ModuleEnvironmentPlan {
    pub fn instance_globals(&self) -> bool {
        self.execution == ModuleExecutionKind::InstanceGlobals
    }
}

pub(crate) fn plan(session: &Session, source: &SourceModule, aot: bool) -> ModuleEnvironmentPlan {
    plan_validated(session, source, aot, usize::MAX, None)
}

pub(crate) fn plan_validated(
    session: &Session,
    source: &SourceModule,
    aot: bool,
    index: usize,
    validated: Option<&crate::instance_modules::ValidatedAot>,
) -> ModuleEnvironmentPlan {
    let admitted = !aot || validated.is_some_and(|v| v.selects(index));
    if session.instance_modules.contains(&source.name) && admitted {
        return ModuleEnvironmentPlan {
            capabilities: ryupy_base::InstanceCapabilities {
                ordinary_name_handlers: source.kind == SourceKind::ModuleFile
                    && admitted
                    && !session.fixed_binding_build,
                relative_from_import: source.kind == SourceKind::ModuleFile
                    && admitted
                    && !session.fixed_binding_build,
            },
            execution: ModuleExecutionKind::InstanceGlobals,
            automatic: false,
            reason: "explicit-instance".into(),
        };
    }
    // catalog令牌绑定实际最终位置和已解析字节，不按模块名字/公开元数据放行class。
    if !aot
        && !session.fixed_binding_build
        && session.module_environment_policy != ModuleEnvironmentPolicy::ExplicitOnly
        && source.catalog.as_ref().is_some_and(|token| {
            token.authenticates(&source.resolved_source_path, source.text.as_bytes())
        })
    {
        return ModuleEnvironmentPlan {
            capabilities: ryupy_base::InstanceCapabilities {
                ordinary_name_handlers: true,
                relative_from_import: true,
            },
            execution: ModuleExecutionKind::InstanceGlobals,
            automatic: true,
            reason: "compiler-catalog-authenticated-source".into(),
        };
    }
    let boundary = if session.module_environment_policy == ModuleEnvironmentPolicy::ExplicitOnly {
        Some("policy-disabled")
    } else if session.fixed_binding_build {
        Some("fixed-binding-build")
    } else if aot {
        Some("aot-not-migrated")
    } else if source.kind != SourceKind::ModuleFile {
        Some("entry-or-package")
    } else {
        None
    };
    let reason = if let Some(boundary) = boundary {
        boundary.to_owned()
    } else {
        match ordinary_dynamic_eligibility(&source.parsed) {
            OrdinaryDynamicEligibility::Eligible { functions } => {
                return ModuleEnvironmentPlan {
                    capabilities: Default::default(),
                    execution: ModuleExecutionKind::InstanceGlobals,
                    automatic: true,
                    reason: format!("ordinary-dynamic-pilot:{functions}-functions"),
                };
            }
            boundary => format!("hir-boundary:{boundary:?}"),
        }
    };
    ModuleEnvironmentPlan {
        capabilities: Default::default(),
        execution: ModuleExecutionKind::LegacySingleton,
        automatic: false,
        reason,
    }
}

/// 本次真实编译计划的可序列化审计事实；不靠重扫Python猜测入选数量。
#[derive(Debug, Clone, serde::Serialize)]
pub struct ModuleEnvironmentDecision {
    pub candidate: bool,
    pub hard: bool,
    pub catalog_authenticated: bool,
    pub module: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub resource_discovery: Option<crate::modules::ResourceDiscovery>,
    pub execution: String,
    pub reason: String,
    pub compiled: bool,
    pub relative_from_import: bool,
    pub ordinary_name_handlers: bool,
}
pub(crate) fn record(
    session: &mut Session,
    sources: &[SourceModule],
    plans: &[ModuleEnvironmentPlan],
) {
    session.module_environment_report = sources
        .iter()
        .zip(plans)
        .map(|(source, plan)| ModuleEnvironmentDecision {
            candidate: source.candidate,
            hard: source.hard,
            catalog_authenticated: source.catalog.as_ref().is_some_and(|token| {
                token.authenticates(&source.resolved_source_path, source.text.as_bytes())
            }),
            module: source.name.clone(),
            resource_discovery: source.resource_discovery.clone(),
            execution: format!("{:?}", plan.execution),
            reason: plan.reason.clone(),
            compiled: false,
            relative_from_import: plan.capabilities.relative_from_import,
            ordinary_name_handlers: plan.capabilities.ordinary_name_handlers,
        })
        .collect();
}
impl Session {
    /// 普通JIT无交付manifest；此报告独立记录构建策略/选择，不冒称运行或AOT证明。
    pub fn module_environment_report_json(&self) -> Result<Vec<u8>, serde_json::Error> {
        serde_json::to_vec_pretty(&serde_json::json!({
            "compiled_feature": cfg!(feature = "ordinary-dynamic-module-pilot"),
            "session_policy": self.module_environment_policy.identity(),
            "modules": self.module_environment_report,
        }))
    }
}
