//! 可发布typed定义的伴生核心预分配；仅复制函数体，所有Python元数据仍由canonical拥有。
use crate::mir::*;
use la_arena::Arena;
use rustc_hash::FxHashMap;
use ryupy_hir::{FuncId, FunctionKind, Module, ParamKind as HirParamKind};
use ryupy_ty::{ModuleTypes, Ty};

pub(crate) fn allocate(
    hir: &Module,
    types: &ModuleTypes,
    mapping: &FxHashMap<FuncId, FnId>,
    functions: &mut Arena<Function>,
    enabled: bool,
) -> Vec<(FuncId, FnId, bool)> {
    let mut work: Vec<_> = hir
        .functions
        .iter()
        .map(|(id, _)| (id, mapping[&id], false))
        .collect();
    if !enabled || hir.info.instance_globals {
        return work;
    }
    for (id, f) in hir.functions.iter() {
        if f.kind != FunctionKind::User
            || types.is_dynamic(id)
            || f.params.is_empty()
            || f.is_generator
            || f.is_coroutine
            || !f.closure.cells.is_empty()
            || !f.closure.free.is_empty()
            || f.params.iter().any(|p| {
                matches!(
                    p.kind,
                    HirParamKind::VarPositional | HirParamKind::VarKeyword
                )
            })
        {
            continue;
        }
        let canonical = mapping[&id];
        let source = &functions[canonical];
        let generic = functions.alloc(Function {
            core: FunctionCore::GenericCompanion { canonical },
            is_comprehension: false,
            module_state: None,
            module_state_source: None,
            instance_capabilities: source.instance_capabilities,
            name: source.name.clone(),
            codegen_name: format!(
                "{}$generic${}",
                source.codegen_name,
                canonical.into_raw().into_u32()
            ),
            source_file: source.source_file.clone(),
            definition: source.definition.clone(),
            params: Vec::new(),
            ret: MirType::Dynamic,
            body: Body {
                direct_globals_contracts: Vec::new(),
                source_handler_contracts: Vec::new(),
                locals: Arena::default(),
                blocks: Arena::default(),
                entry: la_arena::Idx::from_raw(0.into()),
                regions: Vec::new(),
            },
            param_meta: source
                .param_meta
                .iter()
                .map(|p| ParamMeta {
                    ty: MirType::Dynamic,
                    ..p.clone()
                })
                .collect(),
            function_global: None,
            specialization_identity_global: None,
            defaults_global: None,
            signature_global: None,
            env: None,
            dynamic_origin: None,
        });
        functions[canonical].core = FunctionCore::TypedWithGeneric { generic };
        work.push((id, generic, true));
    }
    work
}

pub fn ty_to_mir(ty: &Ty) -> MirType {
    match ty {
        Ty::Int => MirType::I64,
        Ty::Float => MirType::F64,
        Ty::Bool => MirType::Bool,
        Ty::Str => MirType::Str,
        Ty::None => MirType::Unit,
        Ty::Class(id) => MirType::Class(la_arena::Idx::from_raw(id.into_raw())),
        Ty::Dynamic | Ty::OpaqueStr => MirType::Dynamic,
        // 函数对象与毒化类型都不该走到 codegen；用 Dynamic 兜底而不是 Unit，
        // 避免把「有值」错当成 void。
        Ty::Func(_) | Ty::Error => MirType::Dynamic,
    }
}
