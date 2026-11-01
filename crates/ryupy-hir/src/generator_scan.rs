//! 扫描当前函数的 yield 与首个生成器竖切边界；不进入嵌套函数/类/lambda 的函数体，默认值仍在当前作用域。
//!
//! 即使 yield 位于不可达分支，整个函数仍是生成器。异常区域跨暂停、
//! yield from 留待后续竖切，不能静默按普通函数执行。

use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};

#[derive(Default)]
struct Scan {
    has_yield: bool,
    unsupported: Vec<(ryupy_base::TextRange, &'static str)>,
}
impl<'a> Visitor<'a> for Scan {
    fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
        match stmt {
            ast::Stmt::FunctionDef(def) => {
                visit_defaults(self, &def.parameters);
                return;
            }
            ast::Stmt::ClassDef(_) => return,
            ast::Stmt::Try(value) => self
                .unsupported
                .push((value.range, "生成器内 try 暂未支持跨暂停的异常状态")),
            ast::Stmt::With(value) => self
                .unsupported
                .push((value.range, "生成器内 with 暂未支持跨暂停的资源清理")),
            _ => {}
        }
        walk_stmt(self, stmt);
    }
    fn visit_expr(&mut self, expr: &'a ast::Expr) {
        match expr {
            ast::Expr::Lambda(lambda) => {
                if let Some(parameters) = &lambda.parameters {
                    visit_defaults(self, parameters);
                }
                return;
            }
            ast::Expr::Yield(_) | ast::Expr::YieldFrom(_) => self.has_yield = true,
            ast::Expr::Call(call)
                if matches!(call.func.as_ref(), ast::Expr::Name(name) if name.id.as_str() == "super")
                    && call.arguments.args.is_empty()
                    && call.arguments.keywords.is_empty() =>
            {
                self.unsupported.push((
                    call.arguments.range,
                    "生成器内零参 super 暂未保存跨暂停的方法上下文",
                ));
            }
            _ => {}
        }
        walk_expr(self, expr);
    }
}

pub(crate) fn analyze(file: FileId, body: &[ast::Stmt], diagnostics: &mut Vec<Diagnostic>) -> bool {
    let mut scan = Scan::default();
    for stmt in body {
        scan.visit_stmt(stmt);
    }
    finish(scan, file, diagnostics)
}

pub(crate) fn analyze_lambda(
    file: FileId,
    body: &ast::Expr,
    diagnostics: &mut Vec<Diagnostic>,
) -> bool {
    let mut scan = Scan::default();
    scan.visit_expr(body);
    finish(scan, file, diagnostics)
}

fn finish(scan: Scan, file: FileId, diagnostics: &mut Vec<Diagnostic>) -> bool {
    if scan.has_yield {
        for (range, message) in scan.unsupported {
            diagnostics.push(
                Diagnostic::error(Span::new(file, range), message)
                    .with_code("E0100")
                    .with_primary_label("当前普通 yield 首切不能保证此路径的暂停语义"),
            );
        }
    }
    scan.has_yield
}

/// 生成器表达式的最左 iterable 属于外层；仅检查会在恢复函数中执行的表达式。
pub(crate) fn validate_expression(
    file: FileId,
    comp: crate::comprehension_ast::Comprehension<'_>,
    diagnostics: &mut Vec<Diagnostic>,
) {
    struct Check<'a> {
        file: FileId,
        diagnostics: &'a mut Vec<Diagnostic>,
        generator: bool,
    }
    impl<'a> Visitor<'a> for Check<'_> {
        fn visit_expr(&mut self, expr: &'a ast::Expr) {
            if let ast::Expr::Lambda(lambda) = expr {
                if let Some(parameters) = &lambda.parameters {
                    visit_defaults(self, parameters);
                }
                return;
            }
            if matches!(expr, ast::Expr::Yield(_) | ast::Expr::YieldFrom(_)) {
                self.diagnostics.push(
                    Diagnostic::error(Span::new(self.file, expr.range()), "推导式内部不允许 yield")
                        .with_code("E0100"),
                );
            }
            if let ast::Expr::Call(call) = expr
                && self.generator
                && matches!(call.func.as_ref(), ast::Expr::Name(name) if name.id.as_str() == "super")
                && call.arguments.args.is_empty()
                && call.arguments.keywords.is_empty()
            {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, call.arguments.range),
                        "生成器表达式内零参 super 暂未支持独立恢复作用域",
                    )
                    .with_code("E0100"),
                );
            }
            walk_expr(self, expr);
        }
    }
    let mut check = Check {
        file,
        diagnostics,
        generator: comp.kind == crate::comprehension_ast::ComprehensionKind::Generator,
    };
    check.visit_expr(comp.elt);
    if let Some(key) = comp.key {
        check.visit_expr(key);
    }
    for (index, generator) in comp.generators.iter().enumerate() {
        if index > 0 {
            check.visit_expr(&generator.iter);
        }
        for condition in &generator.ifs {
            check.visit_expr(condition);
        }
    }
}

/// 默认值在定义时执行，不能随嵌套函数体一起跳过。
fn visit_defaults<'a>(visitor: &mut impl Visitor<'a>, parameters: &'a ast::Parameters) {
    for parameter in parameters.iter_non_variadic_params() {
        if let Some(default) = &parameter.default {
            visitor.visit_expr(default);
        }
    }
}

/// 当前调用的实参准备尚不能跨暂停，避免把方法绑定 / 星号展开延迟到恢复后。
/// lambda 本体不在此处执行，但其默认值和生成器表达式最左 iterable 会执行。
pub(crate) fn call_suspends(call: &ast::ExprCall) -> bool {
    let mut scan = Scan::default();
    for argument in &call.arguments.args {
        scan.visit_expr(argument);
    }
    for keyword in &call.arguments.keywords {
        scan.visit_expr(&keyword.value);
    }
    scan.has_yield
}

/// 当前内建构造 ABI 的能力门禁应在 HIR 诊断，不让 MIR 混用 E01xx。
pub(crate) fn prepared_builtin_supported(
    builtin: crate::hir::Builtin,
    args: &[crate::hir::CallArg],
) -> bool {
    use crate::hir::{Builtin, CallArg};
    if matches!(builtin, Builtin::Dynamic("super")) {
        return false;
    }
    let narrow = matches!(
        builtin,
        Builtin::Int
            | Builtin::Float
            | Builtin::Str
            | Builtin::Type
            | Builtin::Exception(_)
            | Builtin::Dynamic("bool" | "bytes" | "bytearray")
    );
    !narrow || (args.len() == 1 && matches!(args[0], CallArg::Positional(_)))
}
