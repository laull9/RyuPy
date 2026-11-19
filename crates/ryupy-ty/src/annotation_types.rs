//! 注解到类型旁表的映射；模块感知入口会排除可被装饰器替换的类布局事实。
use crate::Ty;
use ryupy_hir::{Module, TypeRef};

/// 类装饰器可替换类对象；原静态布局不再是可信类型事实。
pub(crate) fn module_type_ref_to_ty(module: &Module, annotation: &TypeRef) -> Ty {
    match annotation {
        TypeRef::Class(id)
            if (module.classes[*id].decorated_binding.is_some()
                || module.classes[*id].runtime_dataclass) =>
        {
            Ty::Dynamic
        }
        _ => type_ref_to_ty(annotation),
    }
}

/// 字符串注解保留未知对象身份，运行时注解走动态合同。
pub fn type_ref_to_ty(type_ref: &TypeRef) -> Ty {
    match type_ref {
        TypeRef::Int => Ty::Int,
        TypeRef::Float => Ty::Float,
        TypeRef::Bool => Ty::Bool,
        TypeRef::Str => Ty::OpaqueStr,
        TypeRef::None => Ty::None,
        TypeRef::Class(id) => Ty::Class(*id),
        TypeRef::Error => Ty::Error,
        TypeRef::Runtime => Ty::Dynamic,
    }
}
