//! 候选薄函数AST合同；所有可调用形状在降级前拒绝，禁止静默回旧工厂。
use ryupy_base::{Diagnostic, Ranged, Span};
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};
use ryupy_syntax::{ParsedModule, ast};

#[path = "stock_thin_body.rs"]
mod body;

pub(crate) fn validate(parsed: &ParsedModule) -> Vec<Diagnostic> {
    let mut scan = Scan {
        parsed,
        diagnostics: vec![],
        top: true,
    };
    for stmt in &parsed.module.body {
        scan.visit_stmt(stmt);
    }
    // 模块语句沿用已验证InstanceGlobals合同；仅受限def由本候选单独准入。
    let other: Vec<_> = parsed
        .module
        .body
        .iter()
        .filter(|stmt| !matches!(stmt, ast::Stmt::FunctionDef(_)))
        .cloned()
        .collect();
    scan.diagnostics.extend(crate::instance_globals::validate(
        &other,
        parsed.file,
        crate::stock_entry_capabilities(),
    ));
    scan.diagnostics
}
struct Scan<'a> {
    parsed: &'a ParsedModule,
    diagnostics: Vec<Diagnostic>,
    top: bool,
}
impl Scan<'_> {
    fn reject(&mut self, range: ryupy_base::TextRange) {
        self.diagnostics.push(
            Diagnostic::error(
                Span::new(self.parsed.file, range),
                "薄函数候选只支持顶层同步def f(x)，无装饰器、注解、默认值",
            )
            .with_code("E0100"),
        );
    }
}
impl<'a> Visitor<'a> for Scan<'_> {
    fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
        if let ast::Stmt::FunctionDef(f) = stmt {
            let p = &f.parameters;
            let valid = self.top
                && !f.is_async
                && f.decorator_list.is_empty()
                && f.returns.is_none()
                && f.type_params.is_none()
                && p.posonlyargs.is_empty()
                && p.args.len() == 1
                && p.kwonlyargs.is_empty()
                && p.vararg.is_none()
                && p.kwarg.is_none()
                // 冷ABI的Python胶水冻结为x；参数改名需单独扩展ABI合同。
                && p.args[0].parameter.name.as_str() == "x"
                && p.args[0].default.is_none()
                && p.args[0].parameter.annotation.is_none();
            if !valid {
                self.reject(stmt.range());
            }
            self.diagnostics
                .extend(body::validate(&f.body, self.parsed.file));
            return;
        }
        if matches!(stmt, ast::Stmt::ClassDef(_)) {
            self.reject(stmt.range());
            return;
        }
        let top = self.top;
        self.top = false;
        walk_stmt(self, stmt);
        self.top = top;
    }
    fn visit_expr(&mut self, expr: &'a ast::Expr) {
        if matches!(expr, ast::Expr::Lambda(_)) {
            self.reject(expr.range());
        }
        walk_expr(self, expr);
    }
}

#[cfg(test)]
mod tests {
    #[test]
    fn stock_thin_language_constant_never_becomes_a_shadowable_global() {
        for (returned, rejected) in [("__debug__", true), ("abs", false), ("x", false)] {
            let source =
                format!("def f(x):\n    return {returned}\nglobals()['__debug__'] = False\n");
            let mut db = ryupy_base::SourceDb::default();
            let file = db.add("thin_constant.py", &source);
            let (parsed, diagnostics) = ryupy_syntax::parse(file, &source);
            assert!(diagnostics.is_empty());
            let diagnostics = super::validate(&parsed);
            assert_eq!(diagnostics.is_empty(), !rejected, "{diagnostics:?}");
            if rejected {
                assert!(diagnostics[0].message.contains("__debug__"));
            }
        }
    }
}
