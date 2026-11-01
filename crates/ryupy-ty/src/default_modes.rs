//! 参数默认值不受标注约束：静态参数槽只能接收已证明类型/范围安全的默认值。
//! 复用模块表达式类型提示；不确定的值仅降级所属callee，不拖慢无关小整数函数。
use crate::check::module_type_ref_to_ty;
use crate::ty::{FuncMode, Ty};
use la_arena::ArenaMap;
use ryupy_hir::{
    Body, ExprId, ExprKind, FuncId, FunctionKind, Literal, Module, ModuleVarId, UnaryOp,
};

pub(crate) fn apply(
    module: &Module,
    globals: &ArenaMap<ModuleVarId, Ty>,
    modes: &mut ArenaMap<FuncId, FuncMode>,
) {
    let hints = globals
        .iter()
        .map(|(id, ty)| (id, Some(ty.clone())))
        .collect();
    let main = &module.functions[module.main].body;
    for (id, function) in module.functions.iter() {
        if modes[id].is_dynamic() {
            continue;
        }
        for parameter in &function.params {
            let Some(default) = parameter.default else {
                continue;
            };
            // 嵌套定义默认值在外层函数arena；没有跨arena假定，保守走对象参数。
            if function.kind == FunctionKind::Nested {
                modes.insert(id, FuncMode::Dynamic);
                break;
            }
            let expected = parameter
                .annotation
                .as_ref()
                .map(|annotation| module_type_ref_to_ty(module, annotation));
            let actual = crate::module_vars::expr_hint(module, main, default, &hints);
            // Int类型提示本身不是i64范围证明（例如2**100或未知模块变量）。
            // 只有直接整数字面量及一元操作能在此有界精确验证；其它整数默认值保守降级。
            let safe_int = actual != Some(Ty::Int)
                || integer_value(main, default)
                    .and_then(|value| i64::try_from(value).ok())
                    .is_some();
            if actual.is_none()
                || actual.as_ref().is_some_and(Ty::is_dynamic)
                || actual != expected
                || !safe_int
            {
                modes.insert(id, FuncMode::Dynamic);
                break;
            }
        }
    }
}

fn integer_value(body: &Body, mut expr: ExprId) -> Option<i128> {
    let mut operations = Vec::new();
    let mut value = loop {
        if expr.into_raw().into_u32() as usize >= body.exprs.len() {
            return None;
        }
        match &body.exprs[expr].kind {
            ExprKind::Literal(Literal::Int(value)) => break *value,
            ExprKind::Unary { op, operand } => {
                operations.push(*op);
                expr = *operand;
            }
            _ => return None,
        }
    };
    // 不对深层一元链再引入Rust递归；按实际由内到外的求值顺序验证范围。
    for operation in operations.into_iter().rev() {
        value = match operation {
            UnaryOp::Pos => value,
            UnaryOp::Neg => value.checked_neg()?,
            UnaryOp::Invert => !value,
            UnaryOp::Not => return None,
        };
    }
    Some(value)
}
