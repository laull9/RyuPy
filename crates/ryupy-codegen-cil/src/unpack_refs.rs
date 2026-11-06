//! 解包入口的机械 CLR 引用；旧入口签名不变，星号入口固定三个参数。
use dotnetdll::prelude::*;

pub(crate) fn build(
    res: &mut Resolution<'static>,
    pyops_type: MethodType,
) -> (MethodRefIndex, MethodRefIndex, MethodRefIndex) {
    let check = res.push_method_reference(
        method_ref! { static void @pyops_type::UnpackCheck(long, long, bool) },
    );
    let sequence = res.push_method_reference(
        method_ref! { static object @pyops_type::UnpackSequence(object, long) },
    );
    let starred = res.push_method_reference(
        method_ref! { static object @pyops_type::UnpackEx(object, long, long) },
    );
    (check, sequence, starred)
}
