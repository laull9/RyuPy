//! 文本与大整数字面量工厂引用；独立保持引用表文件大小，发射使用明确中性ABI。
use dotnetdll::prelude::*;
pub(crate) fn literal_factories(
    res: &mut Resolution<'static>,
    pyops_type: &MethodType,
) -> (MethodRefIndex, MethodRefIndex) {
    let unicode = res.push_method_reference(
        method_ref! { static string @pyops_type::UnicodeFromCodePoints(string) },
    );
    let integer = res.push_method_reference(
        method_ref! { static object @pyops_type::FromDecimalString(string) },
    );
    (unicode, integer)
}
