//! 链接后模块导出协议验证：在进入 CIL 字段索引之前拒绝无效或自相矛盾的元数据。
use crate::{ExportGuard, LinkedProgram, MirType};

/// 验证导出槽与绑定守卫。错误由 driver 在阶段边界报告，不留给后端索引 panic。
pub fn validate_linked_exports(program: &LinkedProgram) -> Result<(), Vec<String>> {
    let mut errors = Vec::new();
    let anchors: rustc_hash::FxHashSet<_> = program
        .functions
        .iter()
        .flat_map(|(_, f)| {
            [
                f.specialization_identity_global,
                f.dynamic_origin.and_then(|o| o.source_global),
            ]
        })
        .flatten()
        .collect();
    for (_, module) in program.modules.iter() {
        let instance = module.execution == crate::ModuleExecutionKind::InstanceGlobals;
        let state = program
            .functions
            .iter()
            .find(|(id, _)| *id == module.init)
            .and_then(|(_, function)| function.module_state);
        if instance != state.is_some() || (instance && !module.exports.is_empty()) {
            errors.push(format!(
                "模块 `{}` 的实例执行能力、state与静态导出不一致",
                module.name
            ));
        }
        for export in &module.exports {
            if anchors.contains(&export.value)
                || matches!(export.guard, crate::ExportGuard::BoundFlag(g) if anchors.contains(&g))
            {
                errors.push(format!(
                    "模块 `{}` 不能导出私有函数身份/来源槽",
                    module.name
                ));
            }
            let value = program.globals.iter().find(|(id, _)| *id == export.value);
            let Some((_, value)) = value else {
                errors.push(format!(
                    "模块 `{}` 导出 `{}` 引用不存在的值槽 {:?}",
                    module.name, export.name, export.value
                ));
                continue;
            };
            if !matches!(value.storage, crate::GlobalStorage::LegacyStatic) {
                errors.push(format!("模块 `{}` 不得静态导出实例字典名称", module.name));
            }
            match export.guard {
                ExportGuard::None => {}
                ExportGuard::ValueSentinel if value.ty != MirType::Dynamic => {
                    errors.push(format!(
                        "模块 `{}` 导出 `{}` 的值哨兵守卫要求 Dynamic 槽",
                        module.name, export.name
                    ));
                }
                ExportGuard::ValueSentinel => {}
                ExportGuard::BoundFlag(flag) => {
                    if flag == export.value {
                        errors.push(format!(
                            "模块 `{}` 导出 `{}` 的绑定标记不能引用自身值槽",
                            module.name, export.name
                        ));
                    }
                    match program.globals.iter().find(|(id, _)| *id == flag) {
                        Some((_, bound)) if bound.ty == MirType::Bool => {}
                        Some(_) => errors.push(format!(
                            "模块 `{}` 导出 `{}` 的绑定标记必须是 Bool",
                            module.name, export.name
                        )),
                        None => errors.push(format!(
                            "模块 `{}` 导出 `{}` 引用不存在的绑定标记 {:?}",
                            module.name, export.name, flag
                        )),
                    }
                }
            }
        }
    }
    if errors.is_empty() {
        Ok(())
    } else {
        Err(errors)
    }
}
