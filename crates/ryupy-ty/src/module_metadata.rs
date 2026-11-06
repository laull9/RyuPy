//! 合成元数据初值只写独立object槽，不把普通模块主体和全局槽一并降为动态。
use crate::TypeckResults;
use ryupy_hir::{ExprKind, Function, FunctionKind};

pub(crate) fn has_dynamic_values(function: &Function, facts: &TypeckResults) -> bool {
    facts.exprs.iter().any(|(id, ty)| {
        // 仅豁免编译器内部的初始化表达式；用户裸名读取仍是ModuleVar，必须降级。
        ty.is_dynamic()
            && !(function.kind == FunctionKind::ModuleMain
                && matches!(function.body.exprs[id].kind, ExprKind::ModuleMetadata(_, _)))
    }) || facts.locals.iter().any(|(_, ty)| ty.is_dynamic())
}
