//! 仅检查语言常量__debug__的非法写目标；不顺带启用其它尚未审计的semantic语法规则。
use crate::ast;
use ast::visitor::{self, Visitor};
use ryupy_base::{Diagnostic, FileId, Ranged, Span, TextRange};

pub(crate) fn check(module: &ast::ModModule, file: FileId, diagnostics: &mut Vec<Diagnostic>) {
    let mut visitor = DebugTargets { file, diagnostics };
    visitor.visit_body(&module.body);
}
struct DebugTargets<'a> {
    file: FileId,
    diagnostics: &'a mut Vec<Diagnostic>,
}
impl DebugTargets<'_> {
    fn target(&mut self, name: &str, range: TextRange, deleting: bool) {
        if name != "__debug__" {
            return;
        }
        self.diagnostics.push(
            Diagnostic::error(
                Span::new(self.file, range),
                if deleting {
                    "cannot delete __debug__"
                } else {
                    "cannot assign to __debug__"
                },
            )
            .with_code("E0001")
            .with_deferred_failure(ryupy_base::diagnostic::DeferredFailure::ParseRejected),
        );
    }
    fn identifier(&mut self, name: &ast::Identifier) {
        self.target(name.as_str(), name.range(), false);
    }
}
impl<'a> Visitor<'a> for DebugTargets<'_> {
    fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
        // CP3.12的属性增强赋值与属性del均允许；只跳过此目标的Store检查，仍访receiver/RHS。
        if let ast::Stmt::AugAssign(assignment) = stmt
            && let ast::Expr::Attribute(attribute) = &*assignment.target
        {
            self.visit_expr(&attribute.value);
            self.visit_expr(&assignment.value);
            return;
        }
        match stmt {
            ast::Stmt::FunctionDef(value) => self.identifier(&value.name),
            ast::Stmt::ClassDef(value) => self.identifier(&value.name),
            _ => {}
        }
        visitor::walk_stmt(self, stmt);
    }
    fn visit_expr(&mut self, expr: &'a ast::Expr) {
        match expr {
            ast::Expr::Name(value) if value.ctx != ast::ExprContext::Load => self.target(
                value.id.as_str(),
                value.range,
                value.ctx == ast::ExprContext::Del,
            ),
            ast::Expr::Attribute(value) if value.ctx == ast::ExprContext::Store => self.target(
                value.attr.as_str(),
                value.range,
                value.ctx == ast::ExprContext::Del,
            ),
            _ => {}
        }
        visitor::walk_expr(self, expr);
    }
    fn visit_parameter(&mut self, value: &'a ast::Parameter) {
        self.identifier(&value.name);
        visitor::walk_parameter(self, value);
    }
    fn visit_type_param(&mut self, value: &'a ast::TypeParam) {
        self.identifier(value.name());
        visitor::walk_type_param(self, value);
    }
    fn visit_alias(&mut self, value: &'a ast::Alias) {
        if let Some(name) = &value.asname {
            self.identifier(name);
        } else {
            self.target(
                value.name.as_str().split('.').next().unwrap_or(""),
                value.range(),
                false,
            );
        }
        visitor::walk_alias(self, value);
    }
    fn visit_except_handler(&mut self, value: &'a ast::ExceptHandler) {
        let ast::ExceptHandler::ExceptHandler(handler) = value;
        if let Some(name) = &handler.name {
            self.identifier(name);
        }
        visitor::walk_except_handler(self, value);
    }
    fn visit_keyword(&mut self, value: &'a ast::Keyword) {
        if let Some(name) = &value.arg {
            self.identifier(name);
        }
        visitor::walk_keyword(self, value);
    }
    fn visit_pattern_keyword(&mut self, value: &'a ast::PatternKeyword) {
        self.identifier(&value.attr);
        visitor::walk_pattern_keyword(self, value);
    }
    fn visit_pattern(&mut self, value: &'a ast::Pattern) {
        let name = match value {
            ast::Pattern::MatchAs(value) => value.name.as_ref(),
            ast::Pattern::MatchStar(value) => value.name.as_ref(),
            ast::Pattern::MatchMapping(value) => value.rest.as_ref(),
            _ => None,
        };
        if let Some(name) = name {
            self.identifier(name);
        }
        visitor::walk_pattern(self, value);
    }
}

#[cfg(test)]
mod tests {
    fn rejected(source: &str) -> bool {
        let mut db = ryupy_base::SourceDb::default();
        let (_, errors) = crate::parse(db.add("debug.py", source), source);
        errors.iter().any(|error| error.code == Some("E0001"))
    }
    #[test]
    fn every_debug_binding_is_rejected_before_execution() {
        for source in [
            "f(__debug__=1)",
            "match x:\n    case C(__debug__=y): pass",
            "__debug__ = False",
            "__debug__ += 1",
            "__debug__: int = 1",
            "del __debug__",
            "x.__debug__ = 1",
            "x.__debug__: int",
            "def __debug__(): pass",
            "class __debug__: pass",
            "def f(__debug__): pass",
            "def f(*__debug__): pass",
            "def f(**__debug__): pass",
            "lambda __debug__: 1",
            "import __debug__",
            "import __debug__.x",
            "import x as __debug__",
            "from x import __debug__",
            "from x import y as __debug__",
            "for __debug__ in []: pass",
            "(__debug__ := 1)",
            "[x for __debug__ in []]",
            "with x as __debug__: pass",
            "try:\n    pass\nexcept Exception as __debug__:\n    pass",
            "match x:\n    case __debug__: pass",
            "match x:\n    case [*__debug__]: pass",
            "match x:\n    case {**__debug__}: pass",
            "match x:\n    case y as __debug__: pass",
            "def f[__debug__](): pass",
            "class C[__debug__]: pass",
            "type T[__debug__] = int",
        ] {
            assert!(
                rejected(&format!("print('must-not-run')\n{source}\n")),
                "{source}"
            );
        }
    }
    #[test]
    fn debug_reads_labels_and_renamed_imports_are_not_bindings() {
        for source in [
            "del x.__debug__",
            "x.__debug__ += 1",
            "f(**{\"__debug__\": 1})",
            "x = __debug__",
            "x = obj.__debug__",
            "setattr(obj, '__debug__', 1)",
            "global __debug__",
            "import a.__debug__",
            "import __debug__ as y",
            "from a import __debug__ as y",
            "from __debug__ import x",
            "x['__debug__'] = 1",
        ] {
            assert!(!rejected(source), "{source}");
        }
    }
}
