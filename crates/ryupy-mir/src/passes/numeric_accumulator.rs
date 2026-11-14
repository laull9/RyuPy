//! 有界循环累加优化入口：保持严格候选，把类型守卫/溢出/回退明确展开到MIR。
//! 配对测量未证明direct稳定净收益：默认保留holder，直接局部只由显式实验流水线选择。
use super::MirPass;
use crate::mir::*;

/// 默认：已经验收的holder表示。
pub struct NumericAccumulator;
/// 显式实验：MIR直接有类型局部，不能因结构更直接而默认启用。
pub struct DirectNumericAccumulator;
impl MirPass for NumericAccumulator {
    fn name(&self) -> &'static str {
        "numeric_accumulator"
    }
    fn run(&self, function: &mut Function) {
        run(function, false);
    }
}
impl MirPass for DirectNumericAccumulator {
    fn name(&self) -> &'static str {
        "direct_numeric_accumulator_experimental"
    }
    fn run(&self, function: &mut Function) {
        run(function, true);
    }
}
fn run(function: &mut Function, direct: bool) {
    // 合成resume没有signature；工厂含MakeGenerator。拒绝闭包/EH及全函数locals观察ABI。
    if !function.body.regions.is_empty()
        || function.signature_global.is_none()
        || function.env.is_some()
        || function.body.blocks.iter().any(|(_, block)| {
            matches!(block.terminator, Terminator::Suspend { .. })
                || block.stmts.iter().any(|statement| {
                    matches!(
                        statement,
                        Statement::Assign {
                            value: Rvalue::MakeGenerator { .. },
                            ..
                        } | Statement::Eval(Rvalue::MakeGenerator { .. })
                            | Statement::Assign {
                                value: Rvalue::Call {
                                    callee: Callee::Runtime(
                                        RuntimeFn::FunctionLocalsValidate
                                            | RuntimeFn::FunctionLocalsEnsure
                                            | RuntimeFn::FunctionLocalsSync,
                                    ),
                                    ..
                                },
                                ..
                            }
                            | Statement::Eval(Rvalue::Call {
                                callee: Callee::Runtime(
                                    RuntimeFn::FunctionLocalsValidate
                                        | RuntimeFn::FunctionLocalsEnsure
                                        | RuntimeFn::FunctionLocalsSync,
                                ),
                                ..
                            })
                    )
                })
        })
    {
        return;
    }
    let predecessors = super::numeric_accumulator_match::predecessors(function);
    // 每函数最多改写一个严格候选，避免重叠循环之间共享累加状态与退出物化顺序。
    let plan = function.body.blocks.iter().find_map(|(header, _)| {
        super::numeric_accumulator_match::candidate(function, header, &predecessors)
    });
    let Some(plan) = plan else {
        return;
    };
    if direct {
        super::numeric_accumulator_expand::expand(function, plan);
    } else {
        super::numeric_accumulator_holder::expand(function, plan);
    }
}
