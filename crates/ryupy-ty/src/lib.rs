//! 类型推导与检查。
//!
//! 输入 HIR，输出每个表达式 / 局部变量的类型（[`TypeckResults`]），以及每个函数的签名。
//! **不修改 HIR**：类型信息以旁表（side table）形式存放，MIR 构建时同时读取两者。
//!
//! Phase 1 策略（见 docs/semantics.md）：
//! - 函数签名必须写全标注，签名就是「真相」，不做跨函数推导；
//! - 函数体内局部变量用并查集做单态推导：同一变量的所有赋值必须统一到同一类型；
//! - `int` 与 `float` 混合运算时，`int` 隐式提升为 `float`（与 Python 一致）。
//!
//! Phase 2 会引入 [`Ty::Dynamic`]：推不出来的变量不再报错，而是降级为 `PyValue`。

mod call;
mod check;
mod infer;
pub mod ops;
mod ty;

pub use check::{check_module, type_ref_to_ty};
pub use ty::*;

#[cfg(test)]
mod check_tests;
