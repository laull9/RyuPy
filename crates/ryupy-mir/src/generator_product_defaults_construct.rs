//! 实际MakeFunction的身份、隐藏参数和Null默认数组合同；由原Defaults与值检查共用。
use super::{error, unsupported};
use crate::generator_product_failure::Failure;
use crate::*;

/// 校验真实构造及Python参数；也用于原有MakeFunction值事实，避免坏FnId索引panic。
pub(crate) fn make_function(program: &Program, value: &Rvalue) -> Result<FnId, Failure> {
    let Rvalue::MakeFunction {
        func,
        instance_context,
        env,
        defaults,
    } = value
    else {
        return Err(error("不是实际MakeFunction构造"));
    };
    if func.into_raw().into_u32() as usize >= program.functions.len() {
        return Err(error("构造FnId不存在"));
    }
    let target = &program.functions[*func];
    let hidden = usize::from(target.dynamic_origin.is_some())
        + usize::from(target.env.is_some())
        + usize::from(target.module_state.is_some());
    if target.params.len() != target.param_meta.len() + hidden
        || defaults.len() != target.param_meta.len()
    {
        return Err(error("构造不是精确Python参数数的全CLR Null无默认槽"));
    }
    for (local, meta) in target.params.iter().zip(&target.param_meta) {
        if crate::validate::local_decl(&target.body.locals, *local)
            .is_none_or(|decl| decl.ty != meta.ty)
        {
            return Err(error("构造的实际参数类型不符"));
        }
    }
    if let Some(origin) = target.dynamic_origin
        && (target.params.last() != Some(&origin.owner)
            || crate::validate::local_decl(&target.body.locals, origin.owner)
                .is_none_or(|decl| decl.ty != MirType::Dynamic || decl.is_cell))
    {
        return Err(error("构造的hidden owner不符"));
    }
    if instance_context.is_some()
        || !env.is_empty()
        || target.env.is_some()
        || target.module_state.is_some()
        || target.module_state_source.is_some()
        || target
            .params
            .iter()
            .take(target.param_meta.len())
            .any(|local| target.body.locals[*local].is_cell)
        || defaults
            .iter()
            .any(|v| *v != Operand::Const(Constant::Null))
        || target.param_meta.iter().any(|p| {
            p.has_default
                || !matches!(
                    p.kind,
                    ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword
                )
        })
    {
        return Err(unsupported("构造不是精确Python参数数的全CLR Null无默认槽"));
    }
    Ok(*func)
}
