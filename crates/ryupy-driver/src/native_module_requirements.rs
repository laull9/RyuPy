//! 闭图的有限原生inspect保留需求；仅扫描语法入口，不追踪别名/值流、不执行源码。
use crate::{Session, modules::SourceModule};
use ryupy_syntax::ast::{
    self,
    visitor::{Visitor, walk_expr, walk_stmt},
};

pub(crate) fn freeze(
    sess: &Session,
    sources: &[SourceModule],
    complete_graph: bool,
) -> ryupy_mir::NativeModuleRequirements {
    freeze_parsed(
        sess,
        sources.iter().map(|source| &source.parsed),
        complete_graph,
    )
}

pub(crate) fn freeze_parsed<'a>(
    sess: &Session,
    parsed: impl IntoIterator<Item = &'a ryupy_syntax::ParsedModule>,
    complete_graph: bool,
) -> ryupy_mir::NativeModuleRequirements {
    // 外部消费者未进入CIL语法图，保留旧能力；包括显式inspect profile及动态模块声明。
    let mut scan = Scan {
        inspect: sess.cpython_provider.is_some() || sess.cpython_provider_state.plan.is_some(),
        imports: false,
    };
    for module in parsed {
        for statement in &module.module.body {
            scan.visit_stmt(statement);
        }
    }
    ryupy_mir::NativeModuleRequirements {
        inspect: scan.inspect || (!complete_graph && scan.imports),
    }
}

#[derive(Default)]
struct Scan {
    inspect: bool,
    imports: bool,
}
fn imported(name: &str) -> bool {
    matches!(
        name.split('.').next().unwrap_or(name),
        "inspect" | "importlib" | "builtins"
    )
}
impl<'a> Visitor<'a> for Scan {
    fn visit_stmt(&mut self, statement: &'a ast::Stmt) {
        match statement {
            ast::Stmt::Import(value) => {
                self.imports = true;
                self.inspect |= value
                    .names
                    .iter()
                    .any(|alias| imported(alias.name.as_str()));
            }
            ast::Stmt::ImportFrom(value) => {
                self.imports = true;
                self.inspect |= value
                    .module
                    .as_ref()
                    .is_some_and(|name| imported(name.as_str()))
                    || value
                        .names
                        .iter()
                        .any(|alias| alias.name.as_str() == "inspect");
            }
            _ => {}
        }
        walk_stmt(self, statement);
    }
    fn visit_expr(&mut self, expression: &'a ast::Expr) {
        match expression {
            ast::Expr::Name(value) => {
                self.inspect |= matches!(
                    value.id.as_str(),
                    "__import__"
                        | "__builtins__"
                        | "__globals__"
                        | "globals"
                        | "locals"
                        | "vars"
                        | "getattr"
                        | "eval"
                        | "exec"
                )
            }
            ast::Expr::Attribute(value) => {
                self.inspect |= matches!(
                    value.attr.as_str(),
                    "__import__" | "__builtins__" | "__globals__" | "__dict__" | "modules"
                )
            }
            _ => {}
        }
        walk_expr(self, expression);
    }
}
