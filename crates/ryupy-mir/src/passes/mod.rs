//! MIR 优化 / 变换 Pass。
//!
//! 每个 Pass 是一个独立模块，实现 [`MirPass`]。约定：
//! - Pass 结束后 MIR 必须仍能通过 [`crate::validate`]（debug 构建下自动检查）；
//! - 每个 Pass 配一组 insta 快照：输入 MIR → 输出 MIR。
//!
//! 计划中的 Pass：
//! - Phase 1：`simplify_cfg`（删除不可达块、合并 Goto 链）
//! - Phase 3：`narrow_types`、`scalar_replace`、`closure_convert`、`generator_lower`

use crate::mir::{Function, Program};

mod simplify_cfg;

pub use simplify_cfg::SimplifyCfg;

pub trait MirPass {
    fn name(&self) -> &'static str;
    fn run(&self, func: &mut Function);
}

/// 默认 Pass 流水线。
pub fn default_pipeline() -> Vec<Box<dyn MirPass>> {
    vec![Box::new(SimplifyCfg)]
}

pub fn run_pipeline(program: &mut Program, passes: &[Box<dyn MirPass>]) {
    for pass in passes {
        for (_, func) in program.functions.iter_mut() {
            pass.run(func);
        }
        if let Err(errs) = crate::validate::validate(program) {
            panic!(
                "Pass `{}` 产出了非法 MIR:\n{}",
                pass.name(),
                errs.join("\n")
            );
        }
    }
}
