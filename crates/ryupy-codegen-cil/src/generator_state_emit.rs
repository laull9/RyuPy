//! 字段状态生成器构造ABI的机械发射；状态布局与PC由MIR明确给出。
use crate::stackify::LowerCtx;
use crate::stackify_operands::lower_operand;
use dotnetdll::prelude::*;
use ryupy_mir::{FnId, Operand};

pub(crate) fn emit(
    resume: FnId,
    sum_resume: Option<FnId>,
    payload: &Operand,
    initial_pc: i64,
    origin: Option<&Operand>,
    ins: &mut Vec<Instruction>,
    ctx: &LowerCtx<'_>,
) {
    let function = &ctx.functions[resume];
    ins.push(Instruction::LoadNull);
    ins.push(Instruction::LoadMethodPointer(ctx.fn_map[&resume].into()));
    ins.push(Instruction::NewObject(UserMethod::Reference(
        ctx.runtime_refs.generators.resume_ctor,
    )));
    lower_operand(
        payload,
        ins,
        ctx.param_slots,
        ctx.local_slots,
        ctx.runtime_refs,
    );
    ins.push(Instruction::LoadConstantInt64(initial_pc));
    ins.push(Instruction::load_string(function.source_file.clone()));
    ins.push(Instruction::load_string(function.name.clone()));
    if let Some(origin) = origin {
        lower_operand(
            origin,
            ins,
            ctx.param_slots,
            ctx.local_slots,
            ctx.runtime_refs,
        );
    } else {
        ins.push(Instruction::LoadNull);
    }
    let ctor = if let Some(sum_resume) = sum_resume {
        ins.push(Instruction::LoadNull);
        ins.push(Instruction::LoadMethodPointer(
            ctx.fn_map[&sum_resume].into(),
        ));
        ins.push(Instruction::NewObject(UserMethod::Reference(
            ctx.runtime_refs.generators.sum_resume_ctor,
        )));
        ctx.runtime_refs.generators.sum_state_ctor
    } else {
        ctx.runtime_refs.generators.state_ctor
    };
    ins.push(Instruction::NewObject(UserMethod::Reference(ctor)));
}
