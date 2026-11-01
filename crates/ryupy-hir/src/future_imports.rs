//! future指令的能力与位置验证；已mandatory特性仍保留真实导入绑定，不删除运行时导入。
use rustc_hash::FxHashSet;
use ryupy_base::{Diagnostic, FileId, Ranged, Span, TextRange};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_stmt};

pub(crate) fn validate(body: &[ast::Stmt], file: FileId, diagnostics: &mut Vec<Diagnostic>) {
    let mut permitted = FxHashSet::default();
    let mut statements = body.iter().peekable();
    if statements.peek().is_some_and(|stmt| matches!(stmt, ast::Stmt::Expr(expr) if matches!(&*expr.value, ast::Expr::StringLiteral(_)))) {
        statements.next();
    }
    for stmt in statements {
        if is_future(stmt) {
            permitted.insert(stmt.range());
        } else {
            break;
        }
    }
    struct Check<'a> {
        file: FileId,
        permitted: FxHashSet<TextRange>,
        diagnostics: &'a mut Vec<Diagnostic>,
    }
    impl<'a> Visitor<'a> for Check<'_> {
        fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
            if is_future(stmt) {
                if !self.permitted.contains(&stmt.range()) {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, stmt.range()),
                            "future导入只能位于模块开头的连续指令区（可在docstring之后）",
                        )
                        .with_code("E0100"),
                    );
                }
                if let ast::Stmt::ImportFrom(import) = stmt {
                    for alias in &import.names {
                        if !matches!(
                            alias.name.as_str(),
                            "nested_scopes"
                                | "generators"
                                | "division"
                                | "absolute_import"
                                | "with_statement"
                                | "print_function"
                                | "unicode_literals"
                                | "generator_stop"
                                | "annotations"
                        ) {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, alias.range()),
                                    format!("尚未支持future特性 `{}`", alias.name),
                                )
                                .with_code("E0100")
                                .with_primary_label("不静默忽略会改变语言语义的future指令"),
                            );
                        }
                    }
                }
            }
            walk_stmt(self, stmt);
        }
    }
    let mut check = Check {
        file,
        permitted,
        diagnostics,
    };
    for stmt in body {
        check.visit_stmt(stmt);
    }
}

/// 只识别已通过位置校验的模块指令区，不将嵌套导入当作模块语义开关。
pub(crate) fn annotations(body: &[ast::Stmt]) -> bool {
    let skip = usize::from(body.first().is_some_and(|stmt| matches!(stmt, ast::Stmt::Expr(expr) if matches!(&*expr.value, ast::Expr::StringLiteral(_)))));
    body.iter().skip(skip).take_while(|stmt| is_future(stmt)).any(|stmt|
        matches!(stmt, ast::Stmt::ImportFrom(import) if import.names.iter().any(|alias| alias.name.as_str() == "annotations")))
}

fn is_future(stmt: &ast::Stmt) -> bool {
    matches!(stmt, ast::Stmt::ImportFrom(import) if import.level == 0 && import.module.as_ref().is_some_and(|name| name.as_str() == "__future__"))
}

#[cfg(test)]
mod tests {
    use super::*;
    use ryupy_base::SourceDb;
    fn check(source: &str) -> Vec<Diagnostic> {
        let mut db = SourceDb::default();
        let file = db.add("future.py", source);
        let (parsed, errors) = ryupy_syntax::parse(file, source);
        assert!(errors.is_empty(), "{errors:?}");
        let mut diagnostics = vec![];
        validate(&parsed.module.body, file, &mut diagnostics);
        diagnostics
    }
    #[test]
    fn mandatory_features_allow_aliases_after_docstring() {
        assert!(check("'说明'\nfrom __future__ import division as d, nested_scopes, generators\nfrom __future__ import absolute_import, with_statement, print_function, unicode_literals, generator_stop\nprint(d)\n").is_empty());
        assert!(check("import __future__\nprint(__future__.annotations)\n").is_empty());
    }
    #[test]
    fn semantic_switches_unknown_features_and_wrong_positions_are_rejected() {
        for source in [
            "from __future__ import barry_as_FLUFL\n",
            "from __future__ import missing\n",
            "from __future__ import *\n",
            "x = 1\nfrom __future__ import division\n",
            "if True:\n    from __future__ import division\n",
            "def f():\n    from __future__ import division\n",
            "class C:\n    from __future__ import division\n",
            "'doc'\n'not doc'\nfrom __future__ import division\n",
        ] {
            assert!(
                check(source).iter().any(|d| d.code == Some("E0100")),
                "{source}"
            );
        }
    }
}
