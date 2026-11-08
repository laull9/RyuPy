//! 受控trace产品的显式会话入口。编译期拒绝整图后才允许生成产物；不从环境变量或旧审计结果切换。
use crate::{
    CompiledModule, HasErrors, Session, accumulator_options, module_environments, stage_trace,
};
use anyhow::Result;
use ryupy_base::{Diagnostic, FileId, Span, TextRange};

pub(crate) fn reject<T>(
    session: &mut Session,
    file: FileId,
    reason: impl std::fmt::Display,
) -> Result<T> {
    session.diagnostics.push(
        Diagnostic::error(
            Span::new(file, TextRange::default()),
            format!("trace V1 整产物拒绝：{reason}"),
        )
        .with_code("E0310"),
    );
    Err(HasErrors.into())
}

pub(crate) fn graph(
    session: &mut Session,
    file: FileId,
    count: usize,
    entry: usize,
    resources: bool,
) -> Result<()> {
    if session.trace_product_v1
        && (count != 1
            || entry != 0
            || resources
            || session.fixed_binding_build
            || !session.instance_modules.is_empty()
            || !session.include_modules.is_empty())
    {
        return reject(
            session,
            file,
            "仅支持无资源扩展/原生绑定/外宿主/Instance的单源码完整产物",
        );
    }
    Ok(())
}

pub(crate) fn compile(
    session: &mut Session,
    hir: &ryupy_hir::Module,
    types: &ryupy_ty::ModuleTypes,
    plan: &module_environments::ModuleEnvironmentPlan,
    trace: &stage_trace::StageTrace,
) -> Result<CompiledModule> {
    if plan.instance_globals() {
        return reject(session, hir.file, "实际模块计划不是LegacySingleton");
    }
    let passes = accumulator_options::pipeline(session, plan)?;
    trace.mark("begin", "trace-v1-high-to-low", &hir.info.name);
    let result = ryupy_mir::generator_product::compile(
        hir,
        types,
        &passes,
        &mut stage_trace::PassTrace::new(trace, &hir.info.name),
    );
    let (program, exports, proof) = match result {
        Ok(value) => value,
        Err(reason) => return reject(session, hir.file, reason),
    };
    trace.mark("end", "trace-v1-high-to-low", &hir.info.name);
    Ok(CompiledModule {
        program,
        exports,
        protected_exports: hir.protected_exports.clone(),
        trace_proof: Some(proof),
    })
}

pub(crate) fn link(
    session: &mut Session,
    file: FileId,
    units: Vec<(ryupy_mir::Program, ryupy_mir::UnitMeta)>,
    entry: usize,
    proofs: Vec<ryupy_mir::generator_product::Proof>,
) -> Result<ryupy_mir::LinkedProgram> {
    if session.trace_product_v1 {
        match ryupy_mir::generator_product::link(units, entry, proofs) {
            Ok(linked) => Ok(linked),
            Err(reason) => reject(session, file, reason),
        }
    } else {
        Ok(ryupy_mir::link(units, entry))
    }
}
