//! MIR 优化 / 变换 Pass。
//!
//! 每个 Pass 是一个独立模块，实现 [`MirPass`]。约定：
//! - Pass 结束后 MIR 必须仍能通过 [`crate::validate`]（每个 Pass 后始终检查）；
//! - 每个 Pass 配一组 insta 快照：输入 MIR → 输出 MIR。
//!
//! 计划中的 Pass：
//! - Phase 1：`simplify_cfg`（删除不可达块、合并 Goto 链）
//! - Phase 3：`narrow_types`、`scalar_replace`、`closure_convert`、`generator_lower`

use crate::mir::{Function, Program};

mod observer;
pub use observer::{PipelineEvent, PipelineObserver, PipelinePhase, PipelineStep};
#[cfg(test)]
mod observer_tests;

pub(crate) mod dead_values;
#[cfg(test)]
mod dead_values_tests;
mod float_chain;
mod float_chain_expand;
#[cfg(test)]
mod float_chain_tests;
mod float_loop;
mod float_loop_expand;
#[cfg(test)]
mod float_loop_tests;
mod float_slp;
mod float_slp_expand;
#[cfg(test)]
mod float_slp_tests;
mod foreach_numeric_accumulator;
mod foreach_numeric_accumulator_analysis;
#[cfg(test)]
mod foreach_numeric_accumulator_tests;
mod integer_chain_expand;
#[cfg(test)]
mod integer_chain_tests;
mod interval_facts;
mod interval_narrow;
#[cfg(test)]
mod interval_narrow_tests;
mod leaf_inline;
mod leaf_inline_expand;
mod leaf_inline_summary;
mod numeric_accumulator;
mod numeric_accumulator_expand;
mod numeric_accumulator_holder;
mod numeric_accumulator_match;
mod simplify_cfg;
mod tuple_scalar;
#[cfg(test)]
mod tuple_scalar_tests;

pub use dead_values::DeadValues;
pub use float_chain::FloatChain;
pub use float_slp::FloatSlp;
pub use foreach_numeric_accumulator::ForEachNumericAccumulator;
pub use interval_narrow::IntervalNarrow;
pub use numeric_accumulator::{DirectNumericAccumulator, NumericAccumulator};
pub use simplify_cfg::SimplifyCfg;
pub use tuple_scalar::TupleScalar;

pub trait MirPass {
    fn name(&self) -> &'static str;
    fn run(&self, func: &mut Function);
}

/// 明确区分函数局部变换与需要跨函数摘要的程序变换。
pub trait ProgramPass {
    fn name(&self) -> &'static str;
    fn run(&self, program: &mut Program);
}

pub enum PipelinePass {
    Function(Box<dyn MirPass>),
    Program(Box<dyn ProgramPass>),
}
impl PipelinePass {
    pub fn name(&self) -> &'static str {
        match self {
            Self::Function(p) => p.name(),
            Self::Program(p) => p.name(),
        }
    }
}

/// 默认 Pass 流水线。
pub fn default_pipeline() -> Vec<PipelinePass> {
    pipeline_with_direct_accumulator(false)
}

/// 实验开关由调用者显式传入；同一严格matcher只选择一种表示，不叠加两个累加pass。
pub fn pipeline_with_direct_accumulator(experimental: bool) -> Vec<PipelinePass> {
    pipeline_with_options(experimental, false)
}

/// 两项实验独立选择；没有实测稳定收益的叶内联不进入默认流水线。
pub fn pipeline_with_options(
    experimental: bool,
    experimental_leaf_inline: bool,
) -> Vec<PipelinePass> {
    pipeline_with_slp_options(experimental, experimental_leaf_inline, false, false)
}

/// SLP两臂均默认关闭：packed未比同守卫标量控制获得净收益，显式保留复现实验。
/// 同时指定时标量控制优先；不改变全局硬件加速开关。
pub fn pipeline_with_slp_options(
    experimental: bool,
    experimental_leaf_inline: bool,
    scalar_slp: bool,
    experimental_packed_slp: bool,
) -> Vec<PipelinePass> {
    pipeline_with_foreach_options(
        experimental,
        experimental_leaf_inline,
        scalar_slp,
        experimental_packed_slp,
        false,
    )
}

/// 独立ForEach实验只由真实模块计划开启；旧入口保持默认关闭。
/// driver先显式拒绝direct冲突；底层另保守禁止两者叠加。
pub fn pipeline_with_foreach_options(
    experimental: bool,
    experimental_leaf_inline: bool,
    scalar_slp: bool,
    experimental_packed_slp: bool,
    experimental_foreach_accumulator: bool,
) -> Vec<PipelinePass> {
    let mut passes: Vec<PipelinePass> = vec![PipelinePass::Function(Box::new(SimplifyCfg))];
    if experimental_foreach_accumulator && !experimental {
        passes.push(PipelinePass::Function(Box::new(ForEachNumericAccumulator)));
    }
    passes.push(PipelinePass::Function(Box::new(DeadValues)));
    if experimental {
        passes.push(PipelinePass::Function(Box::new(DirectNumericAccumulator)));
    } else {
        passes.push(PipelinePass::Function(Box::new(NumericAccumulator)));
    }
    passes.push(PipelinePass::Function(Box::new(TupleScalar)));
    passes.push(PipelinePass::Function(Box::new(DeadValues)));
    passes.push(PipelinePass::Function(Box::new(IntervalNarrow)));
    // 配对测量未支持默认替换；两实验复用相同matcher与守卫，避免误归因收益。
    if scalar_slp || experimental_packed_slp {
        passes.push(PipelinePass::Function(Box::new(FloatSlp {
            scalar: scalar_slp,
        })));
    }
    passes.push(PipelinePass::Function(Box::new(FloatChain)));
    if experimental_leaf_inline {
        passes.push(PipelinePass::Program(Box::new(leaf_inline::LeafInline)));
    }
    passes
}

/// 依次运行 Pass，并在每个 Pass 之后校验 MIR。
///
/// # 为什么返回 `Result` 而不是 `panic!`
///
/// Pass 产出非法 MIR 是**编译器内部错误**，但内部错误不应该让编译器进程崩溃：
/// 崩溃只会打印一条 Rust panic 与回溯，用户既拿不到稳定的错误信息，也无法把它
/// 当作编译失败处理（例如在 CI 里收集全部问题）。这里返回错误，由 driver 转成
/// 诊断 / <c>anyhow</c> 错误，维持「任何输入都不 panic」的约定。
pub fn run_pipeline(program: &mut Program, passes: &[PipelinePass]) -> Result<(), String> {
    run_pipeline_observed(program, passes, &mut observer::NoopObserver)
}

/// 只通知不可变诊断元数据；观察者不参与变换、校验或错误决策。
pub fn run_pipeline_observed<O: PipelineObserver>(
    program: &mut Program,
    passes: &[PipelinePass],
    observer: &mut O,
) -> Result<(), String> {
    run_pipeline_stage(program, passes, observer, None)
}

/// 延迟生成器产品在每个 pass 后运行高层验证，最终 lowering 后仍必须运行严格验证。
pub fn run_pipeline_high<O: PipelineObserver>(
    program: &mut Program,
    passes: &[PipelinePass],
    observer: &mut O,
    generators: &[crate::FnId],
) -> Result<(), String> {
    run_pipeline_stage(program, passes, observer, Some(generators))
}

fn run_pipeline_stage<O: PipelineObserver>(
    program: &mut Program,
    passes: &[PipelinePass],
    observer: &mut O,
    generators: Option<&[crate::FnId]>,
) -> Result<(), String> {
    for (index, pass) in passes.iter().enumerate() {
        let mut notify = |phase, step| {
            if O::ENABLED {
                observer.observe(PipelineEvent {
                    ordinal: index + 1,
                    pass_name: pass.name(),
                    phase,
                    step,
                });
            }
        };
        notify(PipelinePhase::Run, PipelineStep::Begin);
        match pass {
            PipelinePass::Function(pass) => {
                for (_, func) in program.functions.iter_mut() {
                    pass.run(func);
                }
            }
            PipelinePass::Program(pass) => pass.run(program),
        }
        notify(PipelinePhase::Run, PipelineStep::End);
        notify(PipelinePhase::Validate, PipelineStep::Begin);
        let validation = match generators {
            Some(ids) => crate::validate::validate_high(program, ids),
            None => crate::validate::validate(program),
        };
        if let Err(errs) = validation {
            return Err(format!(
                "内部错误：Pass `{}` 产出了非法 MIR:\n{}",
                pass.name(),
                errs.join("\n")
            ));
        }
        notify(PipelinePhase::Validate, PipelineStep::End);
    }
    Ok(())
}

#[cfg(test)]
mod numeric_accumulator_tests;

#[cfg(test)]
mod leaf_inline_tests;

mod numeric_list;
mod numeric_list_analysis;
mod numeric_list_expand;
#[cfg(test)]
mod numeric_list_tests;
