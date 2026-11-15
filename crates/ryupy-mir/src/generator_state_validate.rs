//! 字段状态生成器的构造类型合同；非法载体、恢复签名或PC在进入后端前诊断。
use crate::mir::*;
use crate::validate::{local_decl, operand_type};
use crate::validate_instance_module::validate_operand;

pub(crate) fn validate_creation(
    program: &Program,
    func: &Function,
    block: BlockId,
    value: &Rvalue,
    errors: &mut Vec<String>,
) {
    let Rvalue::MakeStateGenerator {
        resume,
        payload,
        initial_pc,
        origin,
    } = value
    else {
        return;
    };
    for value in std::iter::once(payload).chain(origin) {
        validate_operand(program, func, block, value, errors);
    }
    let valid_payload = matches!(operand_type(program, func, payload), Some(MirType::Class(id))
        if program.classes.iter().any(|(key, c)| key == id && c.is_generator_state
            && !c.is_dataclass && !c.is_exception && !c.is_value_type && c.base.is_none()
            && c.builtin_base.is_none() && c.class_cell.is_none() && c.fields.iter().all(|(_, ty)| matches!(ty,
                    MirType::Dynamic | MirType::I64 | MirType::F64 | MirType::Bool | MirType::Str))));
    let valid_target = program.functions.iter().any(|(id, target)| {
        id == *resume
            && target.params.len() == 1
            && target.ret == MirType::Dynamic
            && local_decl(&target.body.locals, target.params[0])
                .is_some_and(|p| p.ty == MirType::Dynamic)
    });
    if !valid_payload
        || !valid_target
        || *initial_pc < 0
        || origin
            .as_ref()
            .is_some_and(|v| operand_type(program, func, v) != Some(MirType::Dynamic))
    {
        errors.push(format!("函数 `{}` 的字段状态生成器合同非法", func.name));
    }
}
