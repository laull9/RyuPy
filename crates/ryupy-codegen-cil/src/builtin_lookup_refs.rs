//! 类型对象与默认内建冷读取的精确引用；不在后端解释名称绑定或异常语义。
use dotnetdll::prelude::*;

pub(crate) fn build(
    res: &mut Resolution<'static>,
    pyops: &MethodType,
) -> (MethodRefIndex, MethodRefIndex) {
    let type_object =
        res.push_method_reference(method_ref! { static object @pyops::TypeObject(string) });
    let load_default_builtin =
        res.push_method_reference(method_ref! { static object @pyops::LoadDefaultBuiltin(string) });
    (type_object, load_default_builtin)
}
