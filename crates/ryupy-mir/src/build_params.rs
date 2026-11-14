//! 从已收敛类型表构造Python参数元数据，隐藏env与owner不参与绑定。
use crate::build::ty_to_mir;
use crate::mir::{MirType, ParamKind, ParamMeta};
use ryupy_hir::Module;
use ryupy_ty::{ModuleTypes, Ty};

/// 从 HIR 参数与类型签名构造 MIR 参数元数据。
///
/// `*args` / `**kwargs` 的槽位在运行时是 tuple / dict，即使写了标注也不能当成普通值，
/// 因此强制为 `Dynamic`。
pub(crate) fn build_param_meta(
    hir: &Module,
    types: &ModuleTypes,
    hir_fn_id: ryupy_hir::FuncId,
) -> Vec<ParamMeta> {
    let hir_fn = &hir.functions[hir_fn_id];
    let sig = &types.sigs[hir_fn_id];
    hir_fn
        .params
        .iter()
        .enumerate()
        .map(|(index, param)| {
            let kind = match param.kind {
                ryupy_hir::ParamKind::PositionalOnly => ParamKind::PositionalOnly,
                ryupy_hir::ParamKind::PositionalOrKeyword => ParamKind::PositionalOrKeyword,
                ryupy_hir::ParamKind::VarPositional => ParamKind::VarPositional,
                ryupy_hir::ParamKind::KeywordOnly => ParamKind::KeywordOnly,
                ryupy_hir::ParamKind::VarKeyword => ParamKind::VarKeyword,
            };
            let ty = match kind {
                ParamKind::VarPositional | ParamKind::VarKeyword => MirType::Dynamic,
                _ => ty_to_mir(sig.params.get(index).unwrap_or(&Ty::Dynamic)),
            };
            ParamMeta {
                name: hir_fn.body.locals[param.local].name.to_string(),
                kind,
                has_default: param.default.is_some(),
                ty,
            }
        })
        .collect()
}
