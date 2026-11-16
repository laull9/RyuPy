//! 迭代及精确enumerate分量ABI；后端仅发射MIR已决定的调用。
use dotnetdll::prelude::*;
pub(crate) struct IterationRefs {
    pub comp_list: MethodRefIndex,
    pub iter_start: MethodRefIndex,
    pub iter_next_ok: MethodRefIndex,
    pub iter_value: MethodRefIndex,
    pub enumerate_exact: MethodRefIndex,
    pub enumerate_index: MethodRefIndex,
    pub enumerate_value: MethodRefIndex,
}
pub(crate) fn build(res: &mut Resolution<'static>, ops: MethodType) -> IterationRefs {
    IterationRefs {
        comp_list: res
            .push_method_reference(method_ref! {static object @ops::MakeComprehensionList(object)}),
        iter_start: res.push_method_reference(method_ref! {static object @ops::IterStart(object)}),
        iter_next_ok: res.push_method_reference(method_ref! {static bool @ops::IterNextOk(object)}),
        iter_value: res.push_method_reference(method_ref! {static object @ops::IterValue(object)}),
        enumerate_exact: res
            .push_method_reference(method_ref! {static bool @ops::IsEnumerateIterator(object)}),
        enumerate_index: res
            .push_method_reference(method_ref! {static object @ops::EnumerateCurrentIndex(object)}),
        enumerate_value: res
            .push_method_reference(method_ref! {static object @ops::EnumerateCurrentValue(object)}),
    }
}
