//! 显式普通实例的 AOT 窄域：完整 AST 检查，不改变 JIT 或自动试点的资格。
use ryupy_base::{Diagnostic, Ranged, Span, TextRange};
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};
use ryupy_syntax::{ParsedModule, ast};

/// 资源来源由 driver 验证；这里只验证语法，并保留全部既有实例限制。
pub fn validate_instance_aot(parsed: &ParsedModule) -> Vec<Diagnostic> {
    validate_instance_aot_direct_globals(parsed, false)
}

/// 仅显式普通实例可选择直接globals；其它AOT语法守卫完全共用。
pub fn validate_instance_aot_direct_globals(
    parsed: &ParsedModule,
    direct_globals: bool,
) -> Vec<Diagnostic> {
    let mut errors = crate::instance_globals::validate(
        &parsed.module.body,
        parsed.file,
        ryupy_base::InstanceCapabilities {
            aot_class_guards: true,
            direct_globals,
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
    function_depth: usize,
    class_depth: usize,
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
            ast::Stmt::ClassDef(class) => {
                if self.function_depth != 0 || self.class_depth != 0 {
                    self.reject(class.range, "嵌套class");
                }
                if !class.decorator_list.is_empty() || class.type_params.is_some() {
                    self.reject(class.range, "类装饰器/类型参数");
                }
                if class
                    .arguments
                    .as_ref()
                    .is_some_and(|args| !args.keywords.is_empty() || args.args.len() > 1)
                {
                    self.reject(class.range, "类关键字/元类/多重继承");
                }
            }
            ast::Stmt::FunctionDef(def) => {
                if self.function_depth != 0 {
                    self.reject(def.range, "嵌套 def");
                } else {
                    self.functions += 1;
                }
                if self.class_depth != 0
                    && (!def.decorator_list.is_empty() || forbidden_class_name(def.name.as_str()))
                {
                    self.reject(def.range, "类方法装饰器/未支持类协议");
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
        let function = matches!(statement, ast::Stmt::FunctionDef(_));
        let class = matches!(statement, ast::Stmt::ClassDef(_));
        self.function_depth += usize::from(function);
        self.class_depth += usize::from(class);
        walk_stmt(self, statement);
        self.function_depth -= usize::from(function);
        self.class_depth -= usize::from(class);
    }

    fn visit_expr(&mut self, expression: &'a ast::Expr) {
        if self.class_depth != 0 && self.function_depth == 0 {
            if let ast::Expr::Name(name) = expression {
                if matches!(name.ctx, ast::ExprContext::Store | ast::ExprContext::Del)
                    && forbidden_class_name(name.id.as_str())
                {
                    self.reject(name.range, "类namespace未支持协议绑定");
                }
            }
        }
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

// 词法禁用不替代Runtime对动态父链/字段值类型的真实检查。
fn forbidden_class_name(name: &str) -> bool {
    matches!(
        name,
        "__slots__"
            | "__getattribute__"
            | "__getattr__"
            | "__setattr__"
            | "__delattr__"
            | "__init_subclass__"
            | "__class_getitem__"
            | "__mro_entries__"
            | "__set_name__"
    )
}
