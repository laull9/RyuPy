//! 一等内建函数、函数元数据与方法绑定协议的静态运行时ABI引用。
use dotnetdll::prelude::*;

pub(crate) struct FunctionRefs {
    pub builtin: MethodRefIndex,
    pub metadata: MethodRefIndex,
    pub definition: MethodRefIndex,
    pub definition_with_source: MethodRefIndex,
    pub module: MethodRefIndex,
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
            definition: res.push_method_reference(method_ref! { static void @ops::FunctionDefinition(object, string, long) }),
            definition_with_source: res.push_method_reference(method_ref! { static void @ops::FunctionDefinitionWithSource(object, string, long, object) }),
            metadata: res.push_method_reference(method_ref! { static void @ops::FunctionMetadata(object, object, object[], object[]) }),
            module: res.push_method_reference(method_ref! { static void @ops::FunctionModule(object, object) }),
        }
    }
}

/// 统一建立普通方法准备与上下文类型槽绑定引用，保持引用发射次序。
pub(crate) fn build_method_protocol_refs(
    res: &mut Resolution<'static>,
    ryupy_runtime: AssemblyRefIndex,
) -> (MethodType, MethodRefIndex, MethodRefIndex) {
    let pyops_ref = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #ryupy_runtime });
    let pyops_type: MethodType = BaseType::class(pyops_ref).into();
    let context_method = res.push_method_reference(
        method_ref! { static object @pyops_type::ContextMethod(object, string) },
    );
    let prepare_method = res.push_method_reference(
        method_ref! { static object @pyops_type::PrepareMethod(object, string) },
    );
    (pyops_type, context_method, prepare_method)
}
