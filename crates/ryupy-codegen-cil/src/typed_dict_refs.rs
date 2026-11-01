//! TypedDict 冷构建入口的 CLR 签名；字段合并与 Python 类型规则留在运行时。
use dotnetdll::prelude::*;

pub(crate) struct TypedDictRefs {
    pub begin: MethodRefIndex,
    pub finish: MethodRefIndex,
}

impl TypedDictRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let ty = res.push_type_reference(type_ref! { RyuPy.Runtime.PyTypedDict in #runtime });
        let ty: MethodType = BaseType::class(ty).into();
        Self {
            begin: res.push_method_reference(
                method_ref! { static object @ty::Begin(string, object, bool, string) },
            ),
            finish: res.push_method_reference(method_ref! { static object @ty::Finish(object) }),
        }
    }
}
