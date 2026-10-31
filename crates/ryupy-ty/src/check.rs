//! 模块级类型检查入口。
//!
//! 流程：
//! 1. 由每个函数的 `TypeRef` 标注计算 [`FnSig`]（缺标注 → 报错 `E0201`，Phase 2 起改为 `Dynamic`）；
//! 2. 逐函数体：为局部变量建 `TyVar` → 遍历语句收集约束并 unify → 最后 resolve 成具体 `Ty`；
//! 3. 仍未确定的变量（例如只声明从未赋值）报错 `E0202`。
//!
//! 运算规则（`int + float → float`、`int / int → float`、`bool` 参与算术视作 `int` 等）
//! 统一写在一个独立函数里，并与 docs/semantics.md 的表格保持一致。

use ryupy_base::Diagnostic;
use ryupy_hir::Module;

use crate::ty::ModuleTypes;

pub fn check_module(_module: &Module) -> (ModuleTypes, Vec<Diagnostic>) {
    todo!("M1.0：实现类型检查，见 TODO.md")
}
