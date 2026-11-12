//! 普通运行时基类与类命名空间的CLR方法引用；Python能力边界由运行时负责。
use dotnetdll::prelude::*;
pub(crate) struct ClassRefs {
    pub prepare_ordinary_mi: MethodRefIndex,
    pub lexical_prepare: MethodRefIndex,
    pub lexical_finish: MethodRefIndex,
    pub lexical_contains: MethodRefIndex,
    pub lexical_get: MethodRefIndex,
    pub lexical_set: MethodRefIndex,
    pub validate: MethodRefIndex,
    pub namespace_contains: MethodRefIndex,
}
impl ClassRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let ty = res.push_type_reference(type_ref! { RyuPy.Runtime.PyRuntimeBases in #runtime });
        let ty: MethodType = BaseType::class(ty).into();
        let ops = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #runtime });
        let ops: MethodType = BaseType::class(ops).into();
        let factory =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyLexicalClassFactory in #runtime });
        let factory: MethodType = BaseType::class(factory).into();
        Self {
            prepare_ordinary_mi: res.push_method_reference(
                method_ref! { static object @ops::PrepareOrdinaryMiClass(string, object[]) },
            ),
            lexical_prepare: res.push_method_reference(
                method_ref! { static object @factory::Prepare(string, string, object, object[]) },
            ),
            lexical_finish: res
                .push_method_reference(method_ref! { static object @factory::Finish(object) }),
            lexical_contains: res.push_method_reference(
                method_ref! { static bool @factory::NamespaceContains(object, string) },
            ),
            lexical_get: res.push_method_reference(
                method_ref! { static object @factory::NamespaceGet(object, string) },
            ),
            lexical_set: res.push_method_reference(
                method_ref! { static void @factory::NamespaceSet(object, string, object) },
            ),
            validate: res
                .push_method_reference(method_ref! { static object @ty::Validate(object) }),
            namespace_contains: res.push_method_reference(
                method_ref! { static bool @ops::ClassNamespaceContains(object, string) },
            ),
        }
    }
}
