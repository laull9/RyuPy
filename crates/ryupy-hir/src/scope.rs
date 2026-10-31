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

use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Ranged, TextRange};
use ryupy_syntax::ast;

use crate::hir::{Builtin, ClassId, FuncId, LocalId, Name};

/// 名字解析的结果。
#[allow(dead_code)]
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
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
pub fn collect_assigned_names(body: &[ast::Stmt]) -> Vec<(Name, TextRange)> {
    let mut names = Vec::new();
    let mut seen = FxHashSet::default();

    fn collect_expr(
        expr: &ast::Expr,
        names: &mut Vec<(Name, TextRange)>,
        seen: &mut FxHashSet<Name>,
    ) {
        match expr {
            ast::Expr::Name(name) => {
                let n = Name::from(name.id.as_str());
                if seen.insert(n.clone()) {
                    names.push((n, name.range()));
                }
            }
            ast::Expr::Tuple(tuple) => {
                for elt in &tuple.elts {
                    collect_expr(elt, names, seen);
                }
            }
            ast::Expr::List(list) => {
                for elt in &list.elts {
                    collect_expr(elt, names, seen);
                }
            }
            _ => {}
        }
    }

    fn walk_stmts(
        stmts: &[ast::Stmt],
        names: &mut Vec<(Name, TextRange)>,
        seen: &mut FxHashSet<Name>,
    ) {
        for stmt in stmts {
            match stmt {
                ast::Stmt::Assign(s) => {
                    for target in &s.targets {
                        collect_expr(target, names, seen);
                    }
                }
                ast::Stmt::AnnAssign(s) => {
                    collect_expr(&s.target, names, seen);
                }
                ast::Stmt::AugAssign(s) => {
                    collect_expr(&s.target, names, seen);
                }
                ast::Stmt::For(s) => {
                    collect_expr(&s.target, names, seen);
                    walk_stmts(&s.body, names, seen);
                    walk_stmts(&s.orelse, names, seen);
                }
                ast::Stmt::While(s) => {
                    walk_stmts(&s.body, names, seen);
                    walk_stmts(&s.orelse, names, seen);
                }
                ast::Stmt::If(s) => {
                    walk_stmts(&s.body, names, seen);
                    for clause in &s.elif_else_clauses {
                        walk_stmts(&clause.body, names, seen);
                    }
                }
                ast::Stmt::Match(s) => {
                    for case in &s.cases {
                        walk_stmts(&case.body, names, seen);
                    }
                }
                ast::Stmt::With(s) => {
                    for item in &s.items {
                        if let Some(target) = &item.optional_vars {
                            collect_expr(target, names, seen);
                        }
                    }
                    walk_stmts(&s.body, names, seen);
                }
                // 嵌套 def / class 不递归进去（它们有独立作用域）
                _ => {}
            }
        }
    }

    walk_stmts(body, &mut names, &mut seen);
    names
}

pub fn resolve_builtin(name: &str) -> Option<Builtin> {
    Some(match name {
        "print" => Builtin::Print,
        "int" => Builtin::Int,
        "float" => Builtin::Float,
        "abs" => Builtin::Abs,
        // 常用异常类：M2.2 只支持「构造 + 抛出 + 未捕获退出」，
        // `except`/traceback 在后续竖切里接入。
        "ValueError" => Builtin::Exception("ValueError"),
        "TypeError" => Builtin::Exception("TypeError"),
        "RuntimeError" => Builtin::Exception("RuntimeError"),
        "ZeroDivisionError" => Builtin::Exception("ZeroDivisionError"),
        "OverflowError" => Builtin::Exception("OverflowError"),
        "IndexError" => Builtin::Exception("IndexError"),
        "KeyError" => Builtin::Exception("KeyError"),
        "NameError" => Builtin::Exception("NameError"),
        "AttributeError" => Builtin::Exception("AttributeError"),
        "ArithmeticError" => Builtin::Exception("ArithmeticError"),
        "Exception" => Builtin::Exception("Exception"),
        "StopIteration" => Builtin::Exception("StopIteration"),
        "AssertionError" => Builtin::Exception("AssertionError"),
        _ => return None,
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use ryupy_base::SourceDb;

    #[test]
    fn collects_assigned_variables_across_blocks() {
        let mut db = SourceDb::default();
        let src = r#"
y = 1
if True:
    z = 2
for i in range(10):
    w = 3
def nested():
    inner = 4
"#;
        let file = db.add("t.py", src);
        let (parsed, _) = ryupy_syntax::parse(file, src);
        let names = collect_assigned_names(&parsed.module.body);
        let name_strs: Vec<&str> = names.iter().map(|(n, _)| n.as_str()).collect();
        assert_eq!(name_strs, vec!["y", "z", "i", "w"]);
    }
}
