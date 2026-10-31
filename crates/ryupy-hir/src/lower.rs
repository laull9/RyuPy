//! AST → HIR lowering。
//!
//! 流程（两遍）：
//! 1. **收集**：遍历模块顶层，为每个 `def` / `class` 分配 `FuncId` / `ClassId`，填入 [`GlobalScope`]。
//!    同时创建隐式 `main` 函数。
//! 2. **降级**：逐个函数体
//!    - 用 [`collect_assigned_names`] 预扫描，建立 [`FunctionScope`]；
//!    - 把参数分配为 `Local { is_param: true }`；
//!    - 递归降级语句与表达式，同时完成去糖（见 crate 文档）。
//!
//! 不支持的语法一律报诊断（错误码 `E01xx`）并产出 `ExprKind::Missing` / 跳过语句，
//! **不要 panic**——这样一次编译能报出尽可能多的错误。

use ryupy_base::Diagnostic;
use ryupy_syntax::ParsedModule;

use crate::hir::Module;
#[allow(unused_imports)]
use crate::scope::{FunctionScope, GlobalScope, collect_assigned_names};

/// 降级整个模块。即使有错误也返回一个结构完整的 `Module`。
pub fn lower_module(_parsed: &ParsedModule) -> (Module, Vec<Diagnostic>) {
    todo!("M1.0：实现 AST → HIR lowering，见 TODO.md")
}
