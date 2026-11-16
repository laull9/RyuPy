//! 新定义位置类的私名能力拒绝。按最近词法类上下文扫描，不改写AST或旧顶层类。
//! 这是编译期诊断，不允许执行decorator/base/body后才以运行时NI补救。
use ryupy_base::{Diagnostic, FileId, Span, TextRange};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_except_handler, walk_expr, walk_stmt};

pub(crate) fn validate(file: FileId, body: &[ast::Stmt]) -> Vec<Diagnostic> {
    let mut visitor = PrivateNames {
        file,
        class: None,
        class_depth: 0,
        function_depth: 0,
        active: false,
        diagnostics: Vec::new(),
    };
    for statement in body {
        visitor.visit_stmt(statement);
    }
    visitor.diagnostics
}
struct PrivateNames {
    file: FileId,
    class: Option<String>,
    class_depth: usize,
    function_depth: usize,
    active: bool,
    diagnostics: Vec<Diagnostic>,
}
impl PrivateNames {
    fn name(&mut self, name: &str, range: TextRange) {
        if self.active
            && name.starts_with("__")
            && !name.ends_with("__")
            && !name.contains('.')
            && self
                .class
                .as_ref()
                .is_some_and(|name| !name.trim_start_matches('_').is_empty())
        {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, range),
                    "定义位置类暂不支持需要私名改写的标识符",
                )
                .with_code("E0100"),
            );
        }
    }
}
impl<'a> Visitor<'a> for PrivateNames {
    fn visit_stmt(&mut self, statement: &'a ast::Stmt) {
        match statement {
            ast::Stmt::ClassDef(class) => {
                let active = self.active;
                self.active |= self.function_depth > 0 || self.class_depth > 0;
                // 类绑定和头部表达式仍受外层类私名上下文影响。
                self.name(class.name.as_str(), class.name.range);
                for decorator in &class.decorator_list {
                    self.visit_expr(&decorator.expression);
                }
                if let Some(arguments) = &class.arguments {
                    for base in &arguments.args {
                        self.visit_expr(base);
                    }
                    for keyword in &arguments.keywords {
                        self.visit_expr(&keyword.value);
                    }
                }
                let previous = self.class.replace(class.name.to_string());
                self.class_depth += 1;
                for item in &class.body {
                    self.visit_stmt(item);
                }
                self.class_depth -= 1;
                self.class = previous;
                self.active = active;
            }
            ast::Stmt::FunctionDef(function) => {
                self.name(function.name.as_str(), function.name.range);
                for parameter in function.parameters.iter() {
                    self.name(parameter.name().as_str(), parameter.name().range);
                }
                self.function_depth += 1;
                walk_stmt(self, statement);
                self.function_depth -= 1;
            }
            ast::Stmt::Global(declaration) => {
                for name in &declaration.names {
                    self.name(name.as_str(), name.range);
                }
            }
            ast::Stmt::Nonlocal(declaration) => {
                for name in &declaration.names {
                    self.name(name.as_str(), name.range);
                }
            }
            ast::Stmt::Import(import) => {
                for alias in &import.names {
                    let name = alias
                        .asname
                        .as_ref()
                        .map(|name| name.as_str())
                        .unwrap_or_else(|| alias.name.as_str().split('.').next().unwrap_or(""));
                    self.name(name, alias.range);
                }
            }
            ast::Stmt::ImportFrom(import) => {
                for alias in &import.names {
                    self.name(
                        alias.asname.as_ref().unwrap_or(&alias.name).as_str(),
                        alias.range,
                    );
                }
            }
            _ => walk_stmt(self, statement),
        }
    }
    fn visit_except_handler(&mut self, handler: &'a ast::ExceptHandler) {
        let ast::ExceptHandler::ExceptHandler(inner) = handler;
        if let Some(name) = &inner.name {
            self.name(name.as_str(), name.range);
        }
        walk_except_handler(self, handler);
    }
    fn visit_expr(&mut self, expression: &'a ast::Expr) {
        match expression {
            ast::Expr::Name(name) => self.name(name.id.as_str(), name.range),
            ast::Expr::Attribute(attribute) => {
                self.name(attribute.attr.as_str(), attribute.attr.range)
            }
            ast::Expr::Lambda(lambda) => {
                if let Some(parameters) = &lambda.parameters {
                    for parameter in parameters.iter() {
                        self.name(parameter.name().as_str(), parameter.name().range);
                    }
                }
            }
            _ => {}
        }
        walk_expr(self, expression);
    }
}
