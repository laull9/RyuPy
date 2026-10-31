//! 作用域与名称解析。
//!
//! Python 的作用域规则与「块作用域」语言不同，是本阶段最容易踩坑的地方：
//!
//! 1. **函数级作用域**：`if`/`for`/`while` 不引入新作用域。
//! 2. **赋值即局部**：只要函数体内任何位置对 `x` 赋值（含 `for x in`、增量赋值），
//!    `x` 在**整个函数**内都是局部变量——即使读取发生在赋值之前（运行时 `UnboundLocalError`）。
//!    因此必须先**预扫描**整个函数体收集被赋值的名字，再进行解析。
//! 3. 未被赋值的名字按 LEGB 查找：Local → Enclosing（Phase 3 闭包）→ Global → Builtin。
//! 4. `global` / `nonlocal` 声明会改变第 2 条（Phase 1 暂不支持，报错即可）。

// 骨架阶段：lowering 实现后删除这一行。
#![allow(dead_code)]

use rustc_hash::FxHashMap;
use ryupy_base::TextRange;

use crate::hir::{Builtin, ClassId, FuncId, LocalId, Name};

/// 名字解析的结果。
#[derive(Clone, Copy, Debug)]
pub enum Resolution {
    Local(LocalId),
    Func(FuncId),
    Class(ClassId),
    Builtin(Builtin),
}

/// 模块级名字表：第一遍收集所有顶层 `def` / `class`，因此函数之间可以前向引用。
#[derive(Debug, Default)]
pub struct GlobalScope {
    pub names: FxHashMap<Name, Resolution>,
}

/// 单个函数的局部名字表。
#[derive(Debug, Default)]
pub struct FunctionScope {
    pub locals: FxHashMap<Name, LocalId>,
}

/// 预扫描函数体，返回所有被赋值的名字及其首次出现位置（规则 2）。
pub fn collect_assigned_names(_body: &[ryupy_syntax::ast::Stmt]) -> Vec<(Name, TextRange)> {
    // 需要递归进入 if/while/for 的子语句，但不要进入嵌套的 def/class/lambda。
    todo!("M1.1：实现赋值名预扫描")
}

pub fn resolve_builtin(name: &str) -> Option<Builtin> {
    Some(match name {
        "print" => Builtin::Print,
        "int" => Builtin::Int,
        "float" => Builtin::Float,
        "abs" => Builtin::Abs,
        _ => return None,
    })
}
