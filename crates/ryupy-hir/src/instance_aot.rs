//! 显式普通实例的 AOT 窄域：完整 AST 检查，不改变 JIT 或自动试点的资格。
use ryupy_base::{Diagnostic, Ranged, Span, TextRange};
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};
use ryupy_syntax::{ParsedModule, ast};

/// 资源来源由 driver 验证；这里只验证语法，并保留全部既有实例限制。
pub fn validate_instance_aot(parsed: &ParsedModule) -> Vec<Diagnostic> {
    let mut errors = crate::instance_globals::validate(
        &parsed.module.body,
        parsed.file,
        ryupy_base::InstanceCapabilities {
            ordinary_name_handlers: true,
            relative_from_import: true,
        },
    );
    let mut scan = Scan::default();
    for statement in &parsed.module.body {
        scan.visit_stmt(statement);
    }
    if scan.functions == 0 {
        scan.reject(TextRange::default(), "NoFunction：至少需要一个普通动态 def");
    }
    errors.extend(scan.rejected.into_iter().map(|(range, reason)| {
        Diagnostic::error(
            Span::new(parsed.file, range),
            format!("显式实例 AOT 首切不支持{reason}"),
        )
        .with_code("E0100")
    }));
    errors
}

#[derive(Default)]
struct Scan {
    depth: usize,
    functions: usize,
    rejected: Vec<(TextRange, &'static str)>,
}
impl Scan {
    fn reject(&mut self, range: TextRange, reason: &'static str) {
        self.rejected.push((range, reason));
    }
}
impl<'a> Visitor<'a> for Scan {
    fn visit_stmt(&mut self, statement: &'a ast::Stmt) {
        match statement {
            ast::Stmt::ClassDef(_) => self.reject(statement.range(), "class"),
            ast::Stmt::FunctionDef(def) => {
                if self.depth != 0 {
                    self.reject(def.range, "嵌套 def");
                } else {
                    self.functions += 1;
                }
                if def.returns.is_some()
                    && def.parameters.vararg.is_none()
                    && def.parameters.kwarg.is_none()
                    && def.parameters.iter().all(|p| p.annotation().is_some())
                {
                    self.reject(def.range, "完整 typed 签名");
                }
            }
            _ => {}
        }
        // 默认值、装饰器和不可达分支同样递归；不使用运行期可达性放宽域。
        let scope = matches!(statement, ast::Stmt::FunctionDef(_) | ast::Stmt::ClassDef(_));
        self.depth += usize::from(scope);
        walk_stmt(self, statement);
        self.depth -= usize::from(scope);
    }
    fn visit_expr(&mut self, expression: &'a ast::Expr) {
        match expression {
            ast::Expr::Lambda(lambda) if !crate::instance_aot_lambda::eligible(lambda) => {
                self.reject(expression.range(), "参数闭合白名单以外的 lambda");
            }
            ast::Expr::Yield(_) | ast::Expr::YieldFrom(_) | ast::Expr::Generator(_) => {
                self.reject(expression.range(), "yield / 生成器表达式");
            }
            _ => {}
        }
        walk_expr(self, expression);
    }
}
