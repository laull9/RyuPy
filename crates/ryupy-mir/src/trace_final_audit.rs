//! 链接及优化后的真实调用点审计输入。
//!
//! 此接口故意不返回证书或 V1 准入：来源映射只提供待证明义务，不能由
//! RuntimeFn 名单或 HIR Facts 为空直接签发。借用最终 IR 防止检查期间被修改。
use crate::trace_lowering::{LinkRemap, LoweringOrigins};
use crate::{BlockId, FnId, LinkedProgram, Rvalue, Statement};

#[derive(Debug)]
pub struct OperationObligation<'a> {
    pub function: FnId,
    pub block: BlockId,
    pub statement: usize,
    /// 保留完整真实操作及所有实参，不只收集 RuntimeFn 名称。
    pub operation: &'a Rvalue,
}

#[derive(Debug)]
pub struct FinalAudit<'a> {
    pub program: &'a LinkedProgram,
    pub operations: Vec<OperationObligation<'a>>,
    pub mapping_errors: Vec<String>,
}

/// 在最后一次可能改变调用图的 pass 之后调用。缺失来源不是可忽略警告。
/// 逐调用点的能力/effect、状态槽读写证明尚未实现，返回值只能用于审计。
pub fn inspect<'a>(
    program: &'a LinkedProgram,
    origins: &LoweringOrigins,
    links: &[LinkRemap],
) -> FinalAudit<'a> {
    let mut audit = FinalAudit { program, operations: Vec::new(), mapping_errors: Vec::new() };
    if program.modules.len() != 1 || links.len() != 1 {
        audit.mapping_errors.push("首切只接受单源码模块及一份真实链接重映射".into());
        return audit;
    }
    let link = &links[0];
    for (source, original) in &origins.functions {
        let Some(linked) = link.functions.get(original) else {
            audit.mapping_errors.push("函数来源没有链接身份".into());
            continue;
        };
        let executable = origins.generators.iter().find(|item| item.factory == *original)
            .and_then(|item| link.functions.get(&item.resume)).unwrap_or(linked);
        let Some(function) = program.functions.iter().find_map(|(id, value)| (id == *executable).then_some(value)) else {
            audit.mapping_errors.push("最终 IR 丢失来源函数或恢复入口".into());
            continue;
        };
        let Some(body) = origins.bodies.get(source) else {
            audit.mapping_errors.push("函数缺少真实构建局部/表达式来源".into());
            continue;
        };
        for local in body.locals.values() {
            if !function.body.locals.iter().any(|(id, _)| id == *local) {
                audit.mapping_errors.push("优化后局部映射失效，必须重建证明".into());
            }
        }
        // block 仍存在只证明身份未越界，不证明该点的值仍具有旧能力。
        // 后续值/effect验证必须重新检查def-use，不能据此信任旧表达式结果。
        for expression in &body.expressions {
            if !function.body.blocks.iter().any(|(id, _)| id == expression.block) {
                audit.mapping_errors.push("优化后表达式 block 来源失效".into());
            }
        }
    }
    for (function, value) in program.functions.iter() {
        for (block, value) in value.body.blocks.iter() {
            for (statement, value) in value.stmts.iter().enumerate() {
                let (Statement::Assign { value, .. } | Statement::Eval(value)) = value else { continue; };
                // Unbox/动态运算等也可能在后端调用 runtime，不能只列显式Call。
                audit.operations.push(OperationObligation {
                    function, block, statement, operation: value,
                });
            }
        }
    }
    audit
}
