//! 普通运行时基类与类命名空间的CLR方法引用；Python能力边界由运行时负责。
use dotnetdll::prelude::*;
pub(crate) struct ClassRefs {
    pub validate: MethodRefIndex,
    pub namespace_contains: MethodRefIndex,
}
impl ClassRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let ty = res.push_type_reference(type_ref! { RyuPy.Runtime.PyRuntimeBases in #runtime });
        let ty: MethodType = BaseType::class(ty).into();
        let ops = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #runtime });
        let ops: MethodType = BaseType::class(ops).into();
        Self {
            validate: res
                .push_method_reference(method_ref! { static object @ty::Validate(object) }),
            namespace_contains: res.push_method_reference(
                method_ref! { static bool @ops::ClassNamespaceContains(object, string) },
            ),
        }
    }
}
