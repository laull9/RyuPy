//! 普通函数locals的三个固定CLR ABI，后端不解释Python作用域。
use dotnetdll::prelude::*;

pub(crate) fn build(res: &mut Resolution<'_>, pyops: &MethodType) -> [MethodRefIndex; 3] {
    [
        res.push_method_reference(method_ref! { static void @pyops::FunctionLocalsValidate(object[], object[], object[]) }),
        res.push_method_reference(method_ref! { static object @pyops::FunctionLocalsEnsure(object) }),
        res.push_method_reference(method_ref! { static void @pyops::FunctionLocalsSync(object, string, object) }),
    ]
}
