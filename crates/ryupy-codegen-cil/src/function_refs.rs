//! 一等内建函数与函数元数据的静态运行时ABI引用。
use dotnetdll::prelude::*;

pub(crate) struct FunctionRefs {
    pub builtin: MethodRefIndex,
    pub metadata: MethodRefIndex,
    pub class_namespace_get: MethodRefIndex,
    pub declare_dataclass: MethodRefIndex,
    pub finalize_ordinary_class: MethodRefIndex,
    pub apply_dataclass: MethodRefIndex,
}
impl FunctionRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let ops = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #runtime });
        let ops: MethodType = BaseType::class(ops).into();
        Self {
            finalize_ordinary_class: res.push_method_reference(method_ref! { static void @ops::FinalizeOrdinaryClass(object) }),
            class_namespace_get: res.push_method_reference(method_ref! { static object @ops::ClassNamespaceGet(object, string) }),
            declare_dataclass: res.push_method_reference(method_ref! { static void @ops::DeclareDataclass(object, object[], bool) }),
            apply_dataclass: res.push_method_reference(method_ref! { static object @ops::ApplyDataclass(object, bool) }),
            builtin: res.push_method_reference(method_ref! { static object @ops::BuiltinFunction(string) }),
            metadata: res.push_method_reference(method_ref! { static void @ops::FunctionMetadata(object, object, object[], object[]) }),
        }
    }
}
