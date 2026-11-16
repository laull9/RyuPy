//! 默认值反射首切的动态参数合同：函数值公开后，标注不能限制随后设置的Python默认值。
//! 单模块HIR无法证明跨模块的元数据封闭性；已有默认值先统一Dynamic，完整闭包证明后再恢复特化。
use crate::FuncMode;
use la_arena::ArenaMap;
use rustc_hash::FxHashSet;
use ryupy_hir::{Builtin, ClassItem, ClassItemKind, ExprKind, FuncId, Module};

pub(crate) fn apply(module: &Module, modes: &mut ArenaMap<FuncId, FuncMode>) {
    // 类方法经可变类属性发布；staticmethod即使无初始默认值也能随后新增任意对象默认值。
    for (_, class) in module.classes.iter() {
        exposed_methods(&class.body, modes);
    }
    // 默认值tuple/dict支持动态替换；定义处的标注和初值不足以证明以后值的类型。
    for (id, function) in module.functions.iter() {
        if function
            .params
            .iter()
            .any(|parameter| parameter.default.is_some())
        {
            modes.insert(id, FuncMode::Dynamic);
        }
    }
    let mut observes_published_functions = false;
    for (owner, function) in module.functions.iter() {
        let body = &function.body;
        let direct_callees: FxHashSet<_> = body
            .exprs
            .iter()
            .filter_map(|(_, expression)| match expression.kind {
                ExprKind::Call { callee, .. } | ExprKind::PreparedCall { callee, .. } => {
                    Some(callee)
                }
                _ => None,
            })
            .collect();
        for (id, expression) in body.exprs.iter() {
            match &expression.kind {
                // 属性、别名赋值、传参、返回、容器与装饰器引用都先保留真实对象参数。
                // 同时覆盖原来没有默认值、随后经别名增加默认值的函数。
                ExprKind::Func(target) if !direct_callees.contains(&id) => {
                    modes.insert(*target, FuncMode::Dynamic);
                }
                ExprKind::Builtin(Builtin::Dynamic("globals" | "vars" | "eval" | "exec")) => {
                    observes_published_functions = true
                }
                ExprKind::Builtin(Builtin::Dynamic("locals")) if owner == module.main => {
                    observes_published_functions = true
                }
                // 不依赖属性名常量传播来证明字典封闭：全局字典、模块字典和模块注册表
                // 都可能使未显式ExprKind::Func引用的函数逃逸。
                ExprKind::Attribute { name, .. }
                    if matches!(
                        name.as_str(),
                        "__globals__" | "__dict__" | "modules" | "f_globals"
                    ) =>
                {
                    observes_published_functions = true
                }
                _ => {}
            }
        }
    }
    if observes_published_functions {
        for function in &module.top_functions {
            modes.insert(*function, FuncMode::Dynamic);
        }
    }
}

fn exposed_methods(items: &[ClassItem], modes: &mut ArenaMap<FuncId, FuncMode>) {
    for item in items {
        match &item.kind {
            ClassItemKind::Method { func, .. } => {
                modes.insert(*func, FuncMode::Dynamic);
            }
            ClassItemKind::If { body, orelse, .. } => {
                exposed_methods(body, modes);
                exposed_methods(orelse, modes);
            }
            _ => {}
        }
    }
}
