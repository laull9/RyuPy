//! 类命名空间查询的返回值ABI；bool不可写入object槽或丢弃为遗留CIL栈值。
use crate::mir::{Callee, Function, LocalId, MirType, RuntimeFn, Rvalue};
pub(super) fn validate_destination(
    func: &Function,
    dest: LocalId,
    value: &Rvalue,
    errors: &mut Vec<String>,
) {
    if matches!(
        value,
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ClassNamespaceContains),
            ..
        }
    ) && func
        .body
        .locals
        .iter()
        .find(|(id, _)| *id == dest)
        .is_some_and(|(_, local)| local.ty != MirType::Bool)
    {
        errors.push(format!(
            "函数 `{}` 的类命名空间存在性调用必须写入bool临时",
            func.name
        ));
    }
}
