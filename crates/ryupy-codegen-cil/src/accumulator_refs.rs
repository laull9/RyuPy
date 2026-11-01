//! 数值累加器的普通静态ABI；Python类型守卫和回退语义只存在运行时。
use dotnetdll::prelude::*;

pub(crate) struct AccumulatorRefs {
    pub create: MethodRefIndex,
    pub add: MethodRefIndex,
    pub value: MethodRefIndex,
}
impl AccumulatorRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let ty =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyNumericAccumulator in #runtime });
        let ty: MethodType = BaseType::class(ty).into();
        Self {
            create: res.push_method_reference(method_ref! { static object @ty::Create(object) }),
            add: res.push_method_reference(method_ref! { static void @ty::Add(object, object) }),
            value: res.push_method_reference(method_ref! { static object @ty::Value(object) }),
        }
    }
}
