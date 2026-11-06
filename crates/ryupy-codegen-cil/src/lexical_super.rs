//! 已验证零参frame的机械CIL发射和惰性ABI引用；不从名称、HIR或方法owner推断来源。
use crate::stackify::LowerCtx;
use crate::stackify_operands::lower_operand;
use dotnetdll::prelude::*;
use ryupy_mir::{
    Constant, LinkedProgram, Operand, Rvalue, Statement, SuperClassSource, ZeroCallFrame,
};

pub(crate) fn for_program(
    res: &mut Resolution<'static>,
    program: &LinkedProgram,
    runtime: AssemblyRefIndex,
) -> Option<MethodRefIndex> {
    let used = program.functions.iter().any(|(_, function)| {
        function.body.blocks.iter().any(|(_, block)| {
            block.stmts.iter().any(|statement| {
                matches!(
                    statement,
                    Statement::Assign {
                        value: Rvalue::InstanceZeroCall { .. },
                        ..
                    } | Statement::Eval(Rvalue::InstanceZeroCall { .. })
                )
            })
        })
    });
    used.then(|| {
        let owner = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #runtime });
        let owner: MethodType = BaseType::class(owner).into();
        res.push_method_reference(method_ref! {
            static object @owner::CallZeroWithFrame(int, object, object, long, object, long)
        })
    })
}

pub(crate) fn emit(
    callee: &Operand,
    frame: &ZeroCallFrame,
    ins: &mut Vec<Instruction>,
    ctx: &LowerCtx<'_>,
) {
    // 即使本次callee为canonical，也消耗原值调用的一个site，后续PIC编号不漂移。
    ins.push(Instruction::LoadConstantInt32(ctx.next_site()));
    let (environment, slot) = match frame.class {
        SuperClassSource::Lexical { environment, index } => {
            (Operand::Local(environment), index as i64)
        }
        _ => (Operand::Const(Constant::Null), -1),
    };
    let arg0 = frame
        .first_parameter
        .map(Operand::Local)
        .unwrap_or(Operand::Const(Constant::Null));
    let kind = if frame.class == SuperClassSource::UnsupportedFree {
        -1
    } else {
        frame
            .first_parameter
            .map_or(0, |local| if ctx.locals[local].is_cell { 2 } else { 1 })
    };
    // 普通首参的ldarg/starg槽是当前绑定；own-cell只传引用，不能发CellGet。
    for operand in [
        callee,
        &environment,
        &Operand::Const(Constant::I64(slot)),
        &arg0,
        &Operand::Const(Constant::I64(kind)),
    ] {
        lower_operand(
            operand,
            ins,
            ctx.param_slots,
            ctx.local_slots,
            ctx.runtime_refs,
        );
    }
    ins.push(Instruction::call(
        ctx.runtime_refs
            .zero_call_frame
            .expect("实际MIR使用必须建立ABI引用"),
    ));
}
