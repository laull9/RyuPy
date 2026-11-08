//! 产品生成器的延迟计划。身份来自 HIR 的 is_generator，包括不可达 yield；
//! 最后高层 pass 完成后才消费该计划，不从残余 Suspend 或符号后缀推断身份。
use crate::{FnId, GlobalId, Program};
use rustc_hash::FxHashMap;

#[derive(Debug, Default)]
pub struct DeferredGenerators {
    pub(crate) functions: Vec<FnId>,
    pub(crate) exceptions: FxHashMap<String, GlobalId>,
}

pub(crate) fn finish_build(
    program: &mut Program,
    hir: &ryupy_hir::Module,
    functions: &FxHashMap<ryupy_hir::FuncId, FnId>,
    classes: &FxHashMap<ryupy_hir::ClassId, GlobalId>,
    deferred: Option<&mut DeferredGenerators>,
) -> Vec<crate::trace_lowering::GeneratorLayout> {
    let plan = DeferredGenerators {
        functions: hir
            .functions
            .iter()
            .filter(|(_, f)| f.is_generator)
            .map(|(id, _)| functions[&id])
            .collect(),
        exceptions: hir
            .classes
            .iter()
            .filter(|(_, c)| c.is_exception)
            .map(|(id, c)| (c.name.to_string(), classes[&id]))
            .collect(),
    };
    if let Some(deferred) = deferred {
        *deferred = plan;
        Vec::new()
    } else {
        crate::generators::lower(program, &plan.functions, &plan.exceptions)
    }
}

impl DeferredGenerators {
    pub fn functions(&self) -> &[FnId] {
        &self.functions
    }
}

pub use crate::generator_product_link::{Certificate, Proof};

/// 显式产品入口：高层验证→全部选定pass→高层闭包→复用末端lowering→独立实际结构检查→严格低层验证。
/// 任何失败只返回拒绝，不能在生成V1操作后改走局部Legacy。
pub fn compile<O: crate::passes::PipelineObserver>(
    hir: &ryupy_hir::Module,
    types: &ryupy_ty::ModuleTypes,
    passes: &[crate::passes::PipelinePass],
    observer: &mut O,
) -> Result<(Program, Vec<crate::ModuleExport>, Proof), String> {
    if cfg!(feature = "trace-adapter-test") {
        return Err("产品V1不能与S2测试编译器混用".into());
    }
    for (_, function) in hir.functions.iter() {
        if function
            .body
            .handlers
            .iter()
            .any(|(_, handler)| handler.types.is_none() && handler.type_expr.is_none())
        {
            return Err("源码bare except尚未迁入产品域".into());
        }
    }
    let facts = crate::trace_domain::analyze(hir);
    // next/getter 调用的真实深度守卫可能在 Push 之前失败。该具体 producer 已以
    // NativeFresh 建源，外层接收/根收尾；保留效果，不据此开放源码异常类或未知 helper。
    if facts.functions.values().any(|summary| {
        summary.errors.iter().any(|name| {
            !matches!(
                *name,
                "ValueError"
                    | "TypeError"
                    | "RuntimeError"
                    | "StopIteration"
                    | "OSError"
                    | "RecursionError"
            )
        })
    }) {
        return Err("传播效果包含未迁入异常类型（含SystemExit/GeneratorExit）".into());
    }
    if !facts.converged || !facts.refusals.is_empty() {
        return Err(facts
            .refusals
            .iter()
            .cloned()
            .collect::<Vec<_>>()
            .join("；"));
    }
    check_exception_arguments(hir, &facts)?;
    let mut origins = crate::trace_lowering::LoweringOrigins::default();
    let mut plan = DeferredGenerators::default();
    let (mut program, exports, diagnostics) =
        crate::build::build_program_mode(hir, types, Some(&mut origins), Some(&mut plan));
    if !diagnostics.is_empty() {
        return Err(format!("高层构建诊断：{diagnostics:?}"));
    }
    crate::validate::validate_high(&program, &plan.functions).map_err(|e| e.join("\n"))?;
    let initialization = crate::generator_product_initialization::Initialization::expected(
        hir, &origins, &program, &exports,
    )?;
    let contract = crate::generator_product_trace::Contract::capture(&program, &plan.functions)?;
    crate::passes::run_pipeline_high(&mut program, passes, observer, &plan.functions)?;
    contract.verify(&program, &plan.functions)?;
    crate::generator_product_high::inspect(
        &program,
        &facts,
        &origins,
        &plan.functions,
        &exports,
        &initialization,
    )?;
    let unchanged = crate::generator_product_identity::UnchangedHigh::capture(
        &program,
        &exports,
        &plan.functions,
    );
    let sources: Vec<_> = plan
        .functions
        .iter()
        .map(|id| crate::generator_product_snapshot::Source::capture(*id, &program.functions[*id]))
        .collect();
    // 不读取返回的GeneratorLayout作为证明。检查器只看上面的实际高层与下面的实际输出。
    crate::generators::lower_with_trace(&mut program, &plan.functions, &plan.exceptions, true);
    unchanged.verify(&program, &exports)?;
    crate::validate::validate(&program).map_err(|e| e.join("\n"))?;
    let mut resumes = rustc_hash::FxHashSet::default();
    for source in &sources {
        let checked =
            crate::generator_product_structure::inspect(&program, source, &plan.exceptions)?;
        if checked.factory != source.factory
            || !resumes.insert(checked.resume)
            || plan.functions.contains(&checked.resume)
        {
            return Err("真实工厂与恢复委托不是独立一对一身份".into());
        }
    }
    if program.functions.len() != origins.functions.len() + sources.len() {
        return Err("末端lowering增加了未知函数".into());
    }
    let proof = Proof::capture(&program, &exports, initialization);
    Ok((program, exports, proof))
}

// 旧MakeException ABI把无参和显式None都编码成None消息；本切不迁该ABI。
// 在仍能区分源码实参数量的HIR阶段，以收敛实际值事实拒绝可能None的单参构造，
// 包括callee/参数/返回别名；零参和直接raise已准入类型不受这个额外边界影响。
fn check_exception_arguments(
    hir: &ryupy_hir::Module,
    facts: &crate::trace_domain::Facts,
) -> Result<(), String> {
    use crate::trace_domain::{Atom, Primitive};
    for (function, definition) in hir.functions.iter() {
        for (_, expression) in definition.body.exprs.iter() {
            let ryupy_hir::ExprKind::Call { callee, args } = &expression.kind else {
                continue;
            };
            let [argument] = args.as_slice() else {
                continue;
            };
            let target = facts
                .expressions
                .get(&(function, *callee))
                .ok_or("异常构造缺少实际callee事实")?;
            if target.atoms().iter().any(|atom| {
                matches!(
                    atom,
                    Atom::ExceptionClass("ValueError" | "TypeError" | "RuntimeError")
                )
            }) {
                let values = facts
                    .expressions
                    .get(&(function, argument.expr()))
                    .ok_or("异常构造缺少实际参数事实")?;
                if values.is_unknown() || values.atoms().contains(&Atom::Primitive(Primitive::None))
                {
                    return Err("产品异常单参构造可能为None；现有ABI不区分显式None与无参".into());
                }
            }
        }
    }
    Ok(())
}

/// 只接收本轮完整单模块候选，核真实link后的身份与所有IR字段后封存。
pub fn link(
    units: Vec<(Program, crate::UnitMeta)>,
    entry: usize,
    proofs: Vec<Proof>,
) -> Result<crate::LinkedProgram, String> {
    crate::generator_product_link::link(units, entry, proofs)
}

/// 所有可执行产物出口都要调用。旧产物不得偷偷含V1动作，证书也不能转授给改写后的IR。
pub fn validate_emission(program: &crate::LinkedProgram) -> Result<Option<i64>, String> {
    if let Some(certificate) = &program.trace_product {
        certificate.verify(program)?;
        Ok(Some(1))
    } else {
        for (_, function) in program.functions.iter() {
            for (_, block) in function.body.blocks.iter() {
                for statement in &block.stmts {
                    let value = match statement {
                        crate::Statement::Assign { value, .. } | crate::Statement::Eval(value) => {
                            value
                        }
                        _ => continue,
                    };
                    let runtime = match value {
                        crate::Rvalue::Call {
                            callee: crate::Callee::Runtime(runtime),
                            ..
                        }
                        | crate::Rvalue::CallWithArgs {
                            callee: runtime, ..
                        } => Some(*runtime),
                        _ => None,
                    };
                    if runtime.is_some_and(|runtime| {
                        crate::trace_product_abi::signature(runtime).is_some()
                    }) {
                        return Err("未获完整产品证书的IR含V1动作".into());
                    }
                }
            }
        }
        Ok(None)
    }
}
