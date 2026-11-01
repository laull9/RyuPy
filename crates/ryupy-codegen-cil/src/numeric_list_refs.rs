//! 数值列表生产/预检/读取的CLR签名；Python身份与可观察协议由运行时和MIR守卫负责。
use dotnetdll::prelude::*;
pub(crate) struct NumericListRefs {
    pub create: MethodRefIndex,
    pub append_f64: MethodRefIndex,
    pub peek_list: MethodRefIndex,
    pub can_read_f64: MethodRefIndex,
    pub read_f64: MethodRefIndex,
    pub read_checked_f64: MethodRefIndex,
}
impl NumericListRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let ty = res.push_type_reference(type_ref! { RyuPy.Runtime.PyNumericListOps in #runtime });
        let ty: MethodType = BaseType::class(ty).into();
        Self {
            create: res.push_method_reference(method_ref! { static object @ty::Create() }),
            append_f64: res.push_method_reference(
                method_ref! { static object @ty::AppendF64(object, double) },
            ),
            peek_list: res
                .push_method_reference(method_ref! { static object @ty::PeekList(object, object) }),
            can_read_f64: res
                .push_method_reference(method_ref! { static bool @ty::CanReadF64(object, object) }),
            read_checked_f64: res.push_method_reference(
                method_ref! { static double @ty::ReadCheckedF64(object, object) },
            ),
            read_f64: res
                .push_method_reference(method_ref! { static double @ty::ReadF64(object, object) }),
        }
    }
}
