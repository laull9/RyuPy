//! 运行时发布的顶层函数没有完整静态调用点集合，标注不能变成参数限制。
//! 只收集已有HIR发布入口，不把无碰撞数值helper、类方法或局部函数全量降级。

use la_arena::ArenaMap;
use rustc_hash::FxHashSet;
use ryupy_hir::{AssignTarget, FuncId, Module, StmtKind};

use crate::FuncMode;

pub(crate) fn apply(module: &Module, modes: &mut ArenaMap<FuncId, FuncMode>) {
    let top_functions: FxHashSet<_> = module.top_functions.iter().copied().collect();
    for (_, stmt) in module.functions[module.main].body.stmts.iter() {
        if let StmtKind::DecoratedDef {
            func,
            target: AssignTarget::ModuleVar(_),
            ..
        } = &stmt.kind
            && top_functions.contains(func)
        {
            // 动态名称/alias调用不经过check_user_call；thunk必须接收原Python对象。
            modes.insert(*func, FuncMode::Dynamic);
        }
    }
}
