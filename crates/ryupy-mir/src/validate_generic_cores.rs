//! 伴生核心的完整配对门：同一来源、单份元数据、通用ABI，拒绝独立发布及跨unit伪配对。
use crate::*;
use la_arena::Arena;

pub(crate) fn validate(
    functions: &Arena<Function>,
    globals: &Arena<GlobalDecl>,
    errors: &mut Vec<String>,
) {
    for (id, function) in functions.iter() {
        match function.core {
            FunctionCore::Ordinary => {}
            FunctionCore::TypedWithGeneric { generic } => {
                let peer = functions
                    .iter()
                    .find(|(other, _)| *other == generic)
                    .map(|(_, f)| f);
                if !peer.is_some_and(|f| f.core == FunctionCore::GenericCompanion { canonical: id })
                    || function.signature_global.is_none()
                    || function.function_global.is_none()
                    || function.defaults_global.is_none()
                    || function.dynamic_origin.is_some()
                    || function.env.is_some()
                    || function.module_state.is_some()
                    || function.params.is_empty()
                    || function.params.len() != function.param_meta.len()
                    || function
                        .params
                        .iter()
                        .zip(&function.param_meta)
                        .any(|(id, meta)| {
                            !function
                                .body
                                .locals
                                .iter()
                                .any(|(actual, local)| actual == *id && local.ty == meta.ty)
                        })
                {
                    errors.push(format!(
                        "函数 `{}` 的typed/generic配对或canonical ABI无效",
                        function.name
                    ));
                }
            }
            FunctionCore::GenericCompanion { canonical } => {
                let peer = functions
                    .iter()
                    .find(|(other, _)| *other == canonical)
                    .map(|(_, f)| f);
                let matching = peer.is_some_and(|f| {
                    f.core == FunctionCore::TypedWithGeneric { generic: id }
                        && f.name == function.name
                        && f.definition == function.definition
                        && f.source_file == function.source_file
                        && f.param_meta.len() == function.param_meta.len()
                        && f.param_meta.iter().zip(&function.param_meta).all(|(a, b)| {
                            a.name == b.name && a.kind == b.kind && a.has_default == b.has_default
                        })
                        && crate::source_provenance::for_function(globals, id).is_some()
                        && crate::source_provenance::for_function(globals, id)
                            == crate::source_provenance::for_function(globals, canonical)
                });
                if !matching
                    || function.signature_global.is_some()
                    || function.function_global.is_some()
                    || function.defaults_global.is_some()
                    || function.specialization_identity_global.is_some()
                    || function.env.is_some()
                    || function.module_state.is_some()
                    || function.ret != MirType::Dynamic
                    || function
                        .dynamic_origin
                        .is_none_or(|origin| origin.source_global.is_some())
                    || function.params.len() != function.param_meta.len() + 1
                    || function.param_meta.iter().any(|p| {
                        p.ty != MirType::Dynamic
                            || matches!(p.kind, ParamKind::VarPositional | ParamKind::VarKeyword)
                    })
                    || function.params.iter().any(|p| {
                        !function
                            .body
                            .locals
                            .iter()
                            .any(|(id, d)| id == *p && d.ty == MirType::Dynamic && !d.is_cell)
                    })
                {
                    errors.push(format!(
                        "函数 `{}` 的通用伴生体必须共享canonical身份且仅有通用参数/owner",
                        function.name
                    ));
                }
            }
        }
        for (_, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                let value = match statement {
                    Statement::Assign { value, .. } | Statement::Eval(value) => value,
                    _ => continue,
                };
                if let Rvalue::MakeFunction { func, .. } = value
                    && functions.iter().any(|(id, f)| {
                        id == *func && matches!(f.core, FunctionCore::GenericCompanion { .. })
                    })
                {
                    errors.push("通用伴生体不能独立创建Python函数".into());
                }
                if let Rvalue::Call {
                    callee: Callee::Fn(target),
                    ..
                } = value
                    && functions.iter().any(|(id, f)| {
                        id == *target && matches!(f.core, FunctionCore::GenericCompanion { .. })
                    })
                {
                    errors.push("通用伴生体只能由canonical冷入口调用".into());
                }
                if matches!(function.core, FunctionCore::GenericCompanion { .. })
                    && matches!(
                        value,
                        Rvalue::Call {
                            callee: Callee::Fn(_),
                            ..
                        } | Rvalue::Unbox { .. }
                    )
                {
                    errors.push("通用伴生体不得向typed核心直调或拆箱Python值".into());
                }
            }
        }
    }
}
