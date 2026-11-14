//! 在任何类型推导之前选择函数的初始静态/动态模式。
use crate::FuncMode;
use ryupy_hir::{Function, FunctionKind, Module, TypeRef};

/// 标注齐全才可能走静态路径；模块入口没有标注，但从静态开始（Phase 1 行为）。
pub(crate) fn initial_mode(module: &Module, func: &Function) -> FuncMode {
    // 实例字典允许外部重绑定；从第一轮起不能产生任何静态名字事实。
    if module.info.instance_globals {
        return FuncMode::Dynamic;
    }
    // M3.0：闭包能力一律走动态路径——
    // - 含嵌套 def / lambda 的函数需要创建函数对象与 cell 装箱；
    // - 引用外层 cell（free 非空）的函数需要 env 参数与动态读取。
    // 两者都不影响未捕获函数保留静态快路径。
    // locals使局部值逃逸，只有直接解析到内建的当前函数需要保留对象身份。
    let observes_locals = func.body.exprs.iter().any(|(_, expr)| {
        matches!(&expr.kind, ryupy_hir::ExprKind::Call { callee, .. }
            if matches!(func.body.exprs[*callee].kind,
                ryupy_hir::ExprKind::Builtin(ryupy_hir::Builtin::Dynamic("locals"))))
    });
    if observes_locals
        || func.is_generator
        || func.closure.has_nested
        || !func.closure.free.is_empty()
    {
        return FuncMode::Dynamic;
    }
    // 装饰后的类标注只指向公开动态值，不能据原 ClassId 生成静态布局。
    if func.params.iter().filter_map(|p| p.annotation.as_ref())
        .chain(func.ret.iter())
        .chain(func.body.locals.iter().filter_map(|(_, l)| l.annotation.as_ref()))
        .any(|ann| matches!(ann, TypeRef::Class(id) if (module.classes[*id].decorated_binding.is_some() || module.classes[*id].runtime_dataclass)))
    {
        return FuncMode::Dynamic;
    }
    if func.kind == FunctionKind::ModuleMain {
        return FuncMode::Static;
    }
    // `*args` / `**kwargs` 的槽位在运行时是 tuple / dict，标注不能改变这一点；
    // 这类函数一律走动态路径（参数是装箱值），由运行时完成绑定。
    let has_variadic = func.params.iter().any(|p| {
        matches!(
            p.kind,
            ryupy_hir::ParamKind::VarPositional | ryupy_hir::ParamKind::VarKeyword
        )
    });
    let params_annotated = func.params.iter().all(|p| p.annotation.is_some());
    if !has_variadic && params_annotated && func.ret.is_some() {
        FuncMode::Static
    } else {
        FuncMode::Dynamic
    }
}
