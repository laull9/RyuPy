//! 类型推导与检查。
//!
//! 输入 HIR，输出每个表达式 / 局部变量的类型（[`TypeckResults`]），以及每个函数的签名。
//! **不修改 HIR**：类型信息以旁表（side table）形式存放，MIR 构建时同时读取两者。
//!
//! M2.1 策略（见 docs/semantics.md 与 ADR-0010）：
//! - **静态优先**：标注齐全、调用点一致、函数体可静态定型的函数走 Phase 1 的强类型路径；
//! - **降级而非报错**：缺标注、异构赋值、多返回类型、调用点实参不符、超出 i64 的字面量，
//!   都会让函数整体降级为 [`FuncMode::Dynamic`]（参数与局部变量变成装箱的 `object`）；
//! - 降级判定用不动点迭代，且只降不升，保证收敛；
//! - 标注只提供优化线索：实参类型不符不再报 `E0203`，而是运行时的 Python `TypeError`。

mod call;
mod check;
mod check_attrs;
mod check_stmts;
mod default_modes;
mod infer;
mod module_metadata;
mod module_vars;
mod opaque;
pub mod ops;
mod runtime_function_modes;
mod ty;

pub use check::{check_module, type_ref_to_ty};
pub use ty::*;

#[cfg(test)]
mod check_tests;
#[cfg(test)]
mod primitive_attribute_tests;

#[cfg(test)]
mod module_location_tests;

#[cfg(test)]
mod int_constructor_tests;

#[cfg(test)]
mod abs_constructor_tests;

#[cfg(test)]
mod float_constructor_tests;

#[cfg(test)]
mod opaque_tests;

#[cfg(test)]
mod str_object_call_tests;

#[cfg(test)]
mod module_function_binding_tests;

#[cfg(test)]
mod dynamic_except_tests;

#[cfg(test)]
mod function_locals_tests;

mod initial_mode;
#[cfg(test)]
mod instance_globals_tests;

#[cfg(test)]
mod module_delete_tests;
