//! 检查函数内 global / nonlocal 的声明冲突和先使用后声明。
//!
//! 只进入当前函数的语句块。嵌套函数体另行检查，其默认值仍在当前作用域读取。

use crate::hir::Name;
use rustc_hash::FxHashSet;
use ryupy_base::{Diagnostic, FileId, Span, TextRange};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};

pub(crate) fn validate_declarations(
    file: FileId,
    def: &ast::StmtFunctionDef,
    diagnostics: &mut Vec<Diagnostic>,
) {
    let mut checker = DeclarationChecker {
        file,
        params: def
            .parameters
            .iter()
            .map(|p| Name::from(p.name().as_str()))
            .collect(),
        seen: FxHashSet::default(),
        globals: FxHashSet::default(),
        nonlocals: FxHashSet::default(),
        diagnostics,
    };
    for stmt in &def.body {
        checker.visit_stmt(stmt);
    }
}

struct DeclarationChecker<'a> {
    file: FileId,
    params: FxHashSet<Name>,
    seen: FxHashSet<Name>,
    globals: FxHashSet<Name>,
    nonlocals: FxHashSet<Name>,
    diagnostics: &'a mut Vec<Diagnostic>,
}

impl DeclarationChecker<'_> {
    fn declaration(&mut self, name: &str, range: TextRange, nonlocal: bool) {
        let name = Name::from(name);
        let (own, other) = if nonlocal {
            (&mut self.nonlocals, &self.globals)
        } else {
            (&mut self.globals, &self.nonlocals)
        };
        let message = if other.contains(&name) {
            Some(format!("`{name}` 不能同时声明为 global 与 nonlocal"))
        } else if self.params.contains(&name) {
            Some(format!("参数 `{name}` 不能声明为 global 或 nonlocal"))
        } else if !own.contains(&name) && self.seen.contains(&name) {
            Some(format!("`{name}` 在作用域声明之前已经使用或赋值"))
        } else {
            None
        };
        own.insert(name);
        if let Some(message) = message {
            self.diagnostics.push(
                Diagnostic::error(Span::new(self.file, range), message)
                    .with_code("E0103")
                    .with_primary_label("调整作用域声明或局部绑定"),
            );
        }
    }

    fn defaults(&mut self, params: &ast::Parameters) {
        for param in params.iter_non_variadic_params() {
            if let Some(default) = &param.default {
                self.visit_expr(default);
            }
        }
    }
}

impl<'a> Visitor<'a> for DeclarationChecker<'_> {
    fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
        match stmt {
            ast::Stmt::Global(decl) => {
                for name in &decl.names {
                    self.declaration(name.as_str(), name.range, false);
                }
            }
            ast::Stmt::Nonlocal(decl) => {
                for name in &decl.names {
                    self.declaration(name.as_str(), name.range, true);
                }
            }
            ast::Stmt::FunctionDef(def) => {
                self.defaults(&def.parameters);
                self.seen.insert(Name::from(def.name.as_str()));
            }
            ast::Stmt::ClassDef(class) => {
                self.seen.insert(Name::from(class.name.as_str()));
            }
            _ => walk_stmt(self, stmt),
        }
    }

    fn visit_expr(&mut self, expr: &'a ast::Expr) {
        match expr {
            ast::Expr::Name(name) => {
                self.seen.insert(Name::from(name.id.as_str()));
            }
            ast::Expr::ListComp(_) | ast::Expr::DictComp(_) | ast::Expr::SetComp(_) => {
                // 推导式自身的绑定不属于外层声明；只有最左 iterable 在这里执行。
                if let Some(comp) = crate::comprehension_ast::Comprehension::from_expr(expr)
                    && let Some(first) = comp.generators.first()
                {
                    self.visit_expr(&first.iter);
                }
            }
            ast::Expr::Lambda(lambda) => {
                if let Some(params) = &lambda.parameters {
                    self.defaults(params);
                }
            }
            _ => walk_expr(self, expr),
        }
    }
}
