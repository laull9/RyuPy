//! 扫描当前函数的 yield 与首个生成器竖切边界；不进入嵌套函数/类/lambda。
//!
//! 即使 yield 位于不可达分支，整个函数仍是生成器。异常区域跨暂停、返回值、
//! yield from 和表达式型 yield 留待后续竖切，不能静默按普通函数执行。

use ryupy_base::{Diagnostic, FileId, Span};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};

pub(crate) fn analyze(file: FileId, body: &[ast::Stmt], diagnostics: &mut Vec<Diagnostic>) -> bool {
    #[derive(Default)]
    struct Scan {
        has_yield: bool,
        unsupported: Vec<(ryupy_base::TextRange, &'static str)>,
    }
    impl<'a> Visitor<'a> for Scan {
        fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
            match stmt {
                ast::Stmt::FunctionDef(_) | ast::Stmt::ClassDef(_) => return,
                ast::Stmt::Try(value) => self
                    .unsupported
                    .push((value.range, "生成器内 try 暂未支持跨暂停的异常状态")),
                ast::Stmt::With(value) => self
                    .unsupported
                    .push((value.range, "生成器内 with 暂未支持跨暂停的资源清理")),
                ast::Stmt::Return(value) if value.value.is_some() => self
                    .unsupported
                    .push((value.range, "生成器 return value 留待返回值协议竖切")),
                _ => {}
            }
            walk_stmt(self, stmt);
        }
        fn visit_expr(&mut self, expr: &'a ast::Expr) {
            match expr {
                ast::Expr::Lambda(_) => return,
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
    let mut scan = Scan::default();
    for stmt in body {
        scan.visit_stmt(stmt);
    }
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
