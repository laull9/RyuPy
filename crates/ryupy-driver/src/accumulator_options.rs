//! 局部累加实验的会话边界：只信任实际自动模块计划，不消费审计reason字符串。
//! build/dump共用选择，link不再决定优化；未选择时保持全部旧流水线与ABI。
use crate::{Session, module_environments::ModuleEnvironmentPlan};
use anyhow::{Result, ensure};
use ryupy_mir::{ModuleExecutionKind, passes::PipelinePass};

pub(crate) fn validate(session: &Session) -> Result<()> {
    ensure!(
        !(session.experimental_direct_accumulator && session.experimental_foreach_accumulator),
        "直接累加实验与ForEach局部累加实验不能同时启用"
    );
    Ok(())
}

pub(crate) fn enabled(session: &Session, plan: &ModuleEnvironmentPlan) -> bool {
    cfg!(feature = "ordinary-dynamic-module-pilot")
        && session.experimental_foreach_accumulator
        && plan.automatic
        && plan.execution == ModuleExecutionKind::InstanceGlobals
}

pub(crate) fn pipeline(
    session: &Session,
    plan: &ModuleEnvironmentPlan,
) -> Result<Vec<PipelinePass>> {
    validate(session)?;
    Ok(ryupy_mir::passes::pipeline_with_foreach_options(
        session.experimental_direct_accumulator,
        session.experimental_leaf_inline,
        session.diagnostic_scalar_slp,
        session.experimental_packed_slp,
        enabled(session, plan),
    ))
}
