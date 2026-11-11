//! 按源码位置找回嵌套函数，并收集当前函数的 nonlocal 声明与 def 绑定。

use crate::hir::Name;
use ast::Stmt;
use ryupy_base::TextRange;
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};

pub(crate) enum NestedAst<'a> {
    Def(&'a ast::StmtFunctionDef),
    Lambda(&'a ast::ExprLambda),
    Comprehension(crate::comprehension_ast::Comprehension<'a>),
}

/// 使用语法树的完整遍历，覆盖 assert、raise、f-string 等表达式位置。
pub(crate) fn find_nested_ast(stmts: &[Stmt], range: TextRange) -> Option<NestedAst<'_>> {
    struct Finder<'a> {
        range: TextRange,
        found: Option<NestedAst<'a>>,
    }
    impl<'a> Visitor<'a> for Finder<'a> {
        fn visit_stmt(&mut self, stmt: &'a Stmt) {
            if self.found.is_some() {
                return;
            }
            if let Stmt::FunctionDef(def) = stmt
                && def.range == self.range
            {
                self.found = Some(NestedAst::Def(def));
            } else {
                walk_stmt(self, stmt);
            }
        }
        fn visit_expr(&mut self, expr: &'a ast::Expr) {
            if self.found.is_some() {
                return;
            }
            if let ast::Expr::Lambda(lambda) = expr
                && lambda.range == self.range
            {
                self.found = Some(NestedAst::Lambda(lambda));
            } else if let Some(comp) = crate::comprehension_ast::Comprehension::from_expr(expr)
                && comp.range == self.range
            {
                self.found = Some(NestedAst::Comprehension(comp));
            } else {
                walk_expr(self, expr);
            }
        }
    }
    let mut finder = Finder { range, found: None };
    for stmt in stmts {
        finder.visit_stmt(stmt);
    }
    finder.found
}

/// 收集函数体内的 `nonlocal` 声明（不进入嵌套 def / class）。
pub(crate) fn collect_nonlocal_names(body: &[Stmt]) -> Vec<Name> {
    let mut out = Vec::new();
    fn walk(stmts: &[Stmt], out: &mut Vec<Name>) {
        for stmt in stmts {
            match stmt {
                Stmt::Nonlocal(n) => {
                    out.extend(n.names.iter().map(|n| Name::from(n.as_str())));
                }
                Stmt::If(s) => {
                    walk(&s.body, out);
                    for clause in &s.elif_else_clauses {
                        walk(&clause.body, out);
                    }
                }
                Stmt::For(s) => {
                    walk(&s.body, out);
                    walk(&s.orelse, out);
                }
                Stmt::While(s) => {
                    walk(&s.body, out);
                    walk(&s.orelse, out);
                }
                Stmt::Try(s) => {
                    walk(&s.body, out);
                    for handler in &s.handlers {
                        let ast::ExceptHandler::ExceptHandler(h) = handler;
                        walk(&h.body, out);
                    }
                    walk(&s.orelse, out);
                    walk(&s.finalbody, out);
                }
                Stmt::With(s) => walk(&s.body, out),
                _ => {}
            }
        }
    }
    walk(body, &mut out);
    out
}

/// 收集函数体内的嵌套 `def` 绑定名（它们是本作用域的局部变量）。
pub(crate) fn collect_def_names(body: &[Stmt]) -> Vec<Name> {
    let mut out = Vec::new();
    fn walk(stmts: &[Stmt], out: &mut Vec<Name>) {
        for stmt in stmts {
            match stmt {
                Stmt::FunctionDef(fn_def) => {
                    out.push(Name::from(fn_def.name.as_str()));
                }
                Stmt::ClassDef(class) => {
                    out.push(Name::from(class.name.as_str()));
                }
                // 嵌套 def/class 自己的函数体不属于本作用域，不下行。
                Stmt::If(s) => {
                    walk(&s.body, out);
                    for clause in &s.elif_else_clauses {
                        walk(&clause.body, out);
                    }
                }
                Stmt::For(s) => {
                    walk(&s.body, out);
                    walk(&s.orelse, out);
                }
                Stmt::While(s) => {
                    walk(&s.body, out);
                    walk(&s.orelse, out);
                }
                Stmt::Try(s) => {
                    walk(&s.body, out);
                    for handler in &s.handlers {
                        let ast::ExceptHandler::ExceptHandler(h) = handler;
                        walk(&h.body, out);
                    }
                    walk(&s.orelse, out);
                    walk(&s.finalbody, out);
                }
                Stmt::With(s) => walk(&s.body, out),
                _ => {}
            }
        }
    }
    walk(body, &mut out);
    out
}
