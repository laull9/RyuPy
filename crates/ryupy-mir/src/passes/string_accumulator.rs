//! 严格有界字符串累加优化候选（AOT String Accumulator）：
//! 仅在可严格证明左操作数（LHS）为精确内建字符串且循环完全隔离、无动态观察点的前提下，
//! 将二次方字符串拼接重写为 StringBuilder 延迟物化 Holder。
//!
//! 范围限制（窄竖切）：
//! 1. 仅限模块全局槽（GlobalId），关闭未完全证明的 local 槽以避免局部临时量生命周期遗漏；
//! 2. 必须由 preheader 显式赋予字符串字面量（精确保证内建 str 身份与已绑定状态，无 NameError 风险）；
//! 3. header/body/latch 实行全指令严格白名单，彻底杜绝用户自定义 __add__/__iadd__、动态协议与中间 globals() 观察；
//! 4. 当前本地候选接入流水线接受差分与A/B验收，尚未提交晋升。

use super::MirPass;
use super::string_accumulator_match::predecessors;
use crate::mir::*;

pub struct StringAccumulator;

impl MirPass for StringAccumulator {
    fn name(&self) -> &'static str {
        "string_accumulator"
    }

    fn run(&self, function: &mut Function) {
        // 1. 拒绝包含异常保护区、闭包环境、直接 globals 合约的函数
        if function.module_state.is_some()
            || !function.body.regions.is_empty()
            || function.env.is_some()
            || !function.body.direct_globals_contracts.is_empty()
        {
            return;
        }

        // 2. 拒绝包含生成器挂起点、局部字典反射或 globals 逃逸调用的函数
        let has_disallowed_ops = function.body.blocks.iter().any(|(_, block)| {
            if matches!(block.terminator, Terminator::Suspend { .. }) {
                return true;
            }
            block.stmts.iter().any(|stmt| match stmt {
                Statement::Assign { value, .. } | Statement::Eval(value) => match value {
                    Rvalue::MakeGenerator { .. } => true,
                    Rvalue::Call {
                        callee: Callee::Runtime(rt),
                        ..
                    } => matches!(
                        rt,
                        RuntimeFn::FunctionLocalsValidate
                            | RuntimeFn::FunctionLocalsEnsure
                            | RuntimeFn::FunctionLocalsSync
                    ),
                    _ => false,
                },
                _ => false,
            })
        });

        if has_disallowed_ops {
            return;
        }

        let preds = predecessors(function);
        // 按 CFG 拓扑遍历所有可能的循环头块，寻找满足严格证明的累加计划
        let plan = function.body.blocks.iter().find_map(|(header, _)| {
            super::string_accumulator_match::candidate(function, header, &preds)
        });

        if let Some(plan) = plan {
            super::string_accumulator_expand::expand(function, plan);
        }
    }
}
