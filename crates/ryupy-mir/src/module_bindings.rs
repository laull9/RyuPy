//! 模块绑定位与导出协议：静态值保留原布局，仅另存一个 bool 表示名字是否已绑定。
use crate::mir::*;
use rustc_hash::FxHashMap;

pub(crate) fn allocate(
    globals: &mut la_arena::Arena<GlobalDecl>,
    module_vars: &FxHashMap<ryupy_hir::ModuleVarId, GlobalId>,
) -> FxHashMap<GlobalId, GlobalId> {
    let mut result = FxHashMap::default();
    // 按 arena 顺序分配，避免哈希表遍历顺序影响 IR/程序集可复现性。
    let mut values: Vec<_> = module_vars.values().copied().collect();
    values.sort_by_key(|id| id.into_raw().into_u32());
    for value in values {
        if globals[value].ty != MirType::Dynamic {
            let flag = globals.alloc(GlobalDecl {
                storage: crate::GlobalStorage::LegacyStatic,
                name: format!("__bound_{}", value.into_raw().into_u32()),
                ty: MirType::Bool,
            });
            result.insert(value, flag);
        }
    }
    result
}

/// 必须在生成器降级前执行，确保暂停路径与普通路径都使用同一写入协议。
pub(crate) fn instrument(program: &mut Program, flags: &FxHashMap<GlobalId, GlobalId>) {
    for (_, function) in program.functions.iter_mut() {
        for (_, block) in function.body.blocks.iter_mut() {
            let mut rewritten = Vec::new();
            for statement in std::mem::take(&mut block.stmts) {
                let flag = match &statement {
                    Statement::SetGlobal { dest, .. } => flags.get(dest).copied(),
                    _ => None,
                };
                rewritten.push(statement);
                if let Some(flag) = flag {
                    // 转换与 value store 先成功，随后才公开已绑定状态。
                    rewritten.push(Statement::SetGlobal {
                        dest: flag,
                        value: Operand::Const(Constant::Bool(true)),
                    });
                }
            }
            block.stmts = rewritten;
        }
    }
    let body = &mut program.functions[program.entry].body;
    let mut reset: Vec<_> = flags.values().copied().collect();
    reset.sort_by_key(|id| id.into_raw().into_u32());
    let mut statements: Vec<_> = reset
        .into_iter()
        .map(|flag| Statement::SetGlobal {
            dest: flag,
            value: Operand::Const(Constant::Bool(false)),
        })
        .collect();
    // 每次模块初始化（包括失败重试）先清状态，而不是仅依赖 CLR 初始零值。
    statements.append(&mut body.blocks[body.entry].stmts);
    body.blocks[body.entry].stmts = statements;
}

pub(crate) fn exports(
    hir: &ryupy_hir::Module,
    program: &Program,
    module_vars: &FxHashMap<ryupy_hir::ModuleVarId, GlobalId>,
    flags: &FxHashMap<GlobalId, GlobalId>,
    functions: &FxHashMap<ryupy_hir::FuncId, FnId>,
    classes: &FxHashMap<ryupy_hir::ClassId, GlobalId>,
) -> Vec<ModuleExport> {
    let mut exports = Vec::new();
    if hir.info.instance_globals {
        return exports;
    }
    for (id, var) in hir.vars.iter() {
        if let Some(&value) = module_vars.get(&id) {
            let guard = flags
                .get(&value)
                .copied()
                .map_or(ExportGuard::ValueSentinel, ExportGuard::BoundFlag);
            exports.push(ModuleExport {
                name: var.name.as_str().to_owned(),
                value,
                guard,
            });
        }
    }
    // 模块变量（包括装饰结果）优先，不能被原始函数/类对象覆盖。
    for id in &hir.top_functions {
        let name = hir.functions[*id].name.as_str();
        if !exports.iter().any(|export| export.name == name)
            && let Some(value) = functions
                .get(id)
                .and_then(|id| program.functions[*id].function_global)
        {
            exports.push(ModuleExport {
                name: name.to_owned(),
                value,
                guard: ExportGuard::ValueSentinel,
            });
        }
    }
    for id in &hir.top_classes {
        let name = hir.classes[*id].name.as_str();
        if !exports.iter().any(|export| export.name == name)
            && let Some(&value) = classes.get(id)
        {
            exports.push(ModuleExport {
                name: name.to_owned(),
                value,
                guard: ExportGuard::ValueSentinel,
            });
        }
    }
    exports
}
