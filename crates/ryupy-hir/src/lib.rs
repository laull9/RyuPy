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

mod annotation_format;
mod annotation_stmts;
mod assign_target;
mod aug_assign;
mod chain_assign;
pub mod class;
mod class_bases;
mod class_hir;
mod closures;
mod closures_decl;
mod closures_find;
mod closures_lower;
mod closures_walk;
mod comprehension_ast;
mod comprehensions;
mod control_flow;
mod decorators;
mod definition_metadata;
mod enum_class;
mod exception_imports;
mod exception_targets;
mod handler_semantics;
pub use handler_semantics::HandlerSemantics;
pub mod expr;
mod function_definition;
mod function_exception_imports;
pub use function_definition::DefinitionInfo;
#[cfg(test)]
mod function_definition_tests;
mod future_imports;
mod generator_scan;
mod hir;
pub mod imports;
pub mod lower;
mod lower_fn;
mod lower_stmts;
mod module_definition_bindings;
mod module_deletions;
pub use module_deletions::uses_deleted_builtin_fallback;
mod module_definitions;
mod module_vars;
mod native_bases;
pub mod operator;
pub mod pretty;
mod scope;
mod source_lines;
mod typed_dict;
mod with;

pub use hir::*;
pub use lower::{lower_module, lower_module_with_native_exceptions};

#[cfg(test)]
mod lower_tests;

#[cfg(test)]
mod exception_imports_tests;

#[cfg(test)]
mod native_bases_tests;

#[cfg(test)]
mod enum_class_tests;

#[cfg(test)]
mod annotation_tests;

mod sequence_target;

mod runtime_bases;

mod unresolved_globals;

mod imports_lower;

#[cfg(test)]
mod function_exception_import_tests;
#[cfg(test)]
mod function_import_tests;

mod integer_literal;

#[cfg(test)]
mod typed_dict_tests;

mod class_body;
#[cfg(test)]
mod class_if_tests;
mod class_scope;
mod pretty_class;

#[cfg(test)]
mod flag_tests;

#[cfg(test)]
mod module_location_tests;

#[cfg(test)]
mod string_class_publication_tests;

#[cfg(test)]
mod module_function_binding_tests;

#[cfg(test)]
mod dynamic_except_tests;

mod function_locals;

#[cfg(test)]
mod function_locals_tests;

#[cfg(test)]
mod instance_globals_tests;

mod instance_globals;
mod ordinary_dynamic;
pub use ordinary_dynamic::{OrdinaryDynamicEligibility, ordinary_dynamic_eligibility};
#[cfg(test)]
mod ordinary_dynamic_tests;

#[cfg(test)]
mod module_delete_tests;

#[cfg(test)]
mod class_cell_tests;
#[cfg(test)]
mod lexical_super_tests;
