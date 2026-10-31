//! HIR（High-level IR）：去糖后的、名字已解析的 Python 子集。
//!
//! 与 AST 的区别：
//! - 名字已解析：变量引用是 [`LocalId`] / [`FuncId`] / [`Builtin`]，不再是字符串。
//! - 语法糖已展开：`x += 1` → `x = x + 1`；`for i in range(..)` → [`StmtKind::ForRange`]；
//!   `elif` → 嵌套 `If`；`a and b and c` → 嵌套二元 `BoolOp`。
//! - 顶层语句被收拢进隐式入口函数 [`Module::main`]，后续编译为 CIL 的 `Main`。
//!
//! 数据布局采用 rust-analyzer 风格：每个函数体一组 arena，节点之间用 `Idx` 引用，
//! 避免生命周期在各层之间传染。

mod aug_assign;
pub mod class;
mod control_flow;
pub mod expr;
mod hir;
pub mod imports;
pub mod lower;
mod module_vars;
pub mod operator;
pub mod pretty;
mod scope;
mod with;

pub use hir::*;
pub use lower::lower_module;

#[cfg(test)]
mod lower_tests;
