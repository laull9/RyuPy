//! 动态数值与比较 ABI：旧求真比较保留，表达式富比较使用独立 object 返回合同。
use dotnetdll::prelude::*;
pub(crate) fn build(
    res: &mut Resolution<'static>,
    pyops: &MethodType,
) -> (
    MethodRefIndex,
    MethodRefIndex,
    MethodRefIndex,
    MethodRefIndex,
) {
    (
        res.push_method_reference(
            method_ref! { static object @pyops::Binary(int, object, object) },
        ),
        res.push_method_reference(method_ref! { static bool @pyops::Compare(int, object, object) }),
        res.push_method_reference(method_ref! { static object @pyops::Unary(int, object) }),
        res.push_method_reference(
            method_ref! { static object @pyops::CompareValue(int, object, object) },
        ),
    )
}
