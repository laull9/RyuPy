//! 普通standalone请求只触发有界选择；本次实际proof才选择link，Session意图不是协议能力。
//! 能力域外丢弃未lower候选，普通诊断/坏IR/finish/link永不回退，不清理诊断伪装探测。
use crate::{
    CompiledModule, HasErrors, Session, accumulator_options, module_environments, stage_trace,
};
use anyhow::{Result, anyhow, bail};
use ryupy_base::{Diagnostic, FileId, Span, TextRange};
use ryupy_mir::generator_product::{self, Failure};

/// 仅声明调用者将按生成的正常Main执行；不保证任意CLR反射、直接Public调用或复入宿主。
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq)]
pub enum TraceEntry {
    #[default]
    Unknown,
    StandaloneMain,
}
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(crate) enum Request {
    Legacy,
    AutoBounded,
    RequireV1,
}

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
) -> Result<Request> {
    let supported = session.cpython_provider.is_none()
        && session.distribution_metadata.is_none()
        && count == 1
        && entry == 0
        && !resources
        && !session.fixed_binding_build
        && session.instance_modules.is_empty()
        && session.include_modules.is_empty();
    if session.trace_product_v1 {
        if !supported {
            return reject(
                session,
                file,
                "仅支持无资源扩展/原生绑定/外宿主/Instance的单源码完整产物",
            );
        }
        Ok(Request::RequireV1)
    } else if supported && session.trace_entry == TraceEntry::StandaloneMain {
        Ok(Request::AutoBounded)
    } else {
        Ok(Request::Legacy)
    }
}

/// 只用同一真实HIR函数的generator身份和SourceDynamic需求；不分析事实、不靠残留Suspend。
fn needed(hir: &ryupy_hir::Module) -> bool {
    hir.functions.iter().any(|(_, function)| {
        function.is_generator
            && function
                .body
                .handlers
                .iter()
                .any(|(_, handler)| handler.semantics == ryupy_hir::HandlerSemantics::SourceDynamic)
    })
}

pub(crate) fn compile(
    session: &mut Session,
    hir: &ryupy_hir::Module,
    types: &ryupy_ty::ModuleTypes,
    plan: &module_environments::ModuleEnvironmentPlan,
    trace: &stage_trace::StageTrace,
    request: Request,
) -> Result<Option<CompiledModule>> {
    if request == Request::Legacy {
        return Ok(None);
    }
    if plan.instance_globals() || (request == Request::AutoBounded && !needed(hir)) {
        return if request == Request::RequireV1 {
            reject(session, hir.file, "实际模块计划不是LegacySingleton")
        } else {
            Ok(None)
        };
    }
    let passes = accumulator_options::pipeline(session, plan)?;
    trace.mark("begin", "trace-v1-prepare", &hir.info.name);
    let prepared = generator_product::prepare_high(
        hir,
        types,
        &passes,
        &mut stage_trace::PassTrace::new(trace, &hir.info.name),
    );
    trace.mark("end", "trace-v1-prepare", &hir.info.name);
    let prepared = match prepared {
        Ok(prepared) => prepared,
        Err(Failure::NotInDomain(reason)) if request == Request::RequireV1 => {
            return reject(session, hir.file, reason);
        }
        Err(Failure::NotInDomain(_)) => {
            trace.mark("result", "trace-v1-not-in-domain", &hir.info.name);
            return Ok(None);
        }
        Err(Failure::Diagnostics(diagnostics)) => {
            session.diagnostics.extend(diagnostics);
            return Err(HasErrors.into());
        }
        Err(Failure::InvalidCompilerState(reason)) => {
            bail!("内部错误：trace V1 高层合同损坏：{reason}")
        }
    };
    session
        .diagnostics
        .extend(prepared.diagnostics().iter().cloned());
    trace.mark("begin", "trace-v1-lower", &hir.info.name);
    let (program, exports, proof) = generator_product::finish(prepared)
        .map_err(|reason| anyhow!("内部错误：trace V1 末端合同损坏：{reason}"))?;
    trace.mark("end", "trace-v1-lower", &hir.info.name);
    Ok(Some(CompiledModule {
        program,
        exports,
        protected_exports: hir.protected_exports.clone(),
        trace_proof: Some(proof),
    }))
}

pub(crate) fn link(
    session: &mut Session,
    _file: FileId,
    units: Vec<(ryupy_mir::Program, ryupy_mir::UnitMeta)>,
    entry: usize,
    proofs: Vec<generator_product::Proof>,
) -> Result<ryupy_mir::LinkedProgram> {
    if !proofs.is_empty() {
        generator_product::link(units, entry, proofs)
            .map_err(|reason| anyhow!("内部错误：trace V1 link合同损坏：{reason}"))
    } else {
        if session.trace_product_v1 {
            bail!("内部错误：强制V1缺少本次产物proof");
        }
        let linked = ryupy_mir::link(units, entry);
        generator_product::validate_emission(&linked)
            .map_err(|reason| anyhow!("内部错误：Legacy产物混入V1操作：{reason}"))?;
        Ok(linked)
    }
}
