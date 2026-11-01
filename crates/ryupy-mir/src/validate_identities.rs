//! 特化身份槽只能由程序集初始化器写入；禁止复用公开/签名/默认值槽，防止身份guard被重绑定绕过。
use crate::mir::*;
use rustc_hash::FxHashSet;

pub(crate) fn validate(program: &Program, errors: &mut Vec<String>) {
    let mut anchors = FxHashSet::default();
    let metadata: FxHashSet<_> = program
        .functions
        .iter()
        .flat_map(|(_, f)| [f.function_global, f.defaults_global, f.signature_global])
        .flatten()
        .collect();
    for (_, f) in program.functions.iter() {
        let Some(anchor) = f.specialization_identity_global else {
            continue;
        };
        if !anchors.insert(anchor) || metadata.contains(&anchor) {
            errors.push(format!("函数 `{}` 特化身份锚点必须独立且唯一", f.name));
        }
        if f.function_global.is_none()
            || f.defaults_global.is_none()
            || f.signature_global.is_none()
            || f.env.is_some()
        {
            errors.push(format!(
                "函数 `{}` 特化身份锚点要求完整顶层函数元数据",
                f.name
            ));
        }
        if !matches!(
            program.globals.iter().find(|(id, _)| *id == anchor),
            Some((
                _,
                GlobalDecl {
                    ty: MirType::Dynamic,
                    ..
                }
            ))
        ) {
            errors.push(format!(
                "函数 `{}` 特化身份锚点必须引用有效Dynamic槽",
                f.name
            ));
        }
    }
    for (_, f) in program.functions.iter() {
        for (_, b) in f.body.blocks.iter() {
            for stmt in &b.stmts {
                if let Statement::SetGlobal { dest, .. } = stmt
                    && anchors.contains(dest)
                {
                    errors.push(format!("函数 `{}` 不能写入只读特化身份锚点", f.name));
                }
            }
        }
    }
}
