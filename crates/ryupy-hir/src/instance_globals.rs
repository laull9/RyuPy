//! 实例模块首切的完整 AST 预检；不可达代码也不能绕过能力边界。
//! 环境裸名与属性标识符分别设能力边界；动态 getattr 的能力仍由运行时约束。
use ryupy_base::{Diagnostic, FileId, Ranged, Span, TextRange};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_except_handler, walk_expr, walk_stmt};

pub(crate) fn validate(
    body: &[ast::Stmt],
    file: FileId,
    capabilities: ryupy_base::InstanceCapabilities,
) -> Vec<Diagnostic> {
    let mut check = Check {
        requirements: Vec::new(),
        scope: Default::default(),
        generator: false,
    };
    for statement in body {
        check.visit_stmt(statement);
    }
    check
        .requirements
        .into_iter()
        .filter_map(|(range, requirement)| {
            let feature = match requirement {
                Requirement::OrdinaryNameHandler if capabilities.ordinary_name_handlers => {
                    return None;
                }
                Requirement::OrdinaryNameHandler => "普通Name/bare except handler".to_owned(),
                Requirement::RelativeFrom if capabilities.relative_from_import => return None,
                Requirement::RelativeFrom => "相对导入".to_owned(),
                Requirement::Unsupported(feature) => feature,
            };
            Some(
                Diagnostic::error(
                    Span::new(file, range),
                    format!("实例模块首切暂不支持{feature}"),
                )
                .with_code("E0100"),
            )
        })
        .collect()
}

enum Requirement {
    OrdinaryNameHandler,
    RelativeFrom,
    Unsupported(String),
}

struct Check {
    requirements: Vec<(TextRange, Requirement)>,
    scope: crate::module_deletions::DeleteScope,
    generator: bool,
}

impl Check {
    fn reject(&mut self, range: TextRange, feature: &str) {
        self.requirements
            .push((range, Requirement::Unsupported(feature.to_owned())));
    }

    fn name(&mut self, name: &str, range: TextRange) {
        if matches!(
            name,
            "__classcell__"
                | "globals"
                | "locals"
                | "vars"
                | "eval"
                | "exec"
                | "compile"
                | "__import__"
        ) {
            self.reject(range, &format!("环境名字 `{name}` 的读写"));
        }
    }
}

impl<'a> Visitor<'a> for Check {
    fn visit_stmt(&mut self, statement: &'a ast::Stmt) {
        let previous_generator = self.generator;
        match statement {
            ast::Stmt::FunctionDef(def) => {
                self.generator = crate::generator_scan::contains_yield(&def.body)
            }
            ast::Stmt::ClassDef(_) => self.generator = false,
            _ => {}
        }
        let saved = crate::module_deletions::DeleteScope::enter(statement)
            .map(|scope| std::mem::replace(&mut self.scope, scope));
        match statement {
            ast::Stmt::FunctionDef(def) if def.is_async => {
                self.reject(statement.range(), "异步函数")
            }
            ast::Stmt::FunctionDef(def) if def.type_params.is_some() => {
                self.reject(def.range, "PEP695 泛型函数的注解作用域");
            }
            ast::Stmt::ClassDef(class) if class.type_params.is_some() => {
                self.reject(class.range, "PEP695 泛型类的注解作用域");
            }
            ast::Stmt::AnnAssign(_) | ast::Stmt::TypeAlias(_) => {
                self.reject(statement.range(), "注解与类型别名")
            }
            ast::Stmt::Delete(delete) => {
                for target in &delete.targets {
                    if !matches!(target, ast::Expr::Name(name) if self.scope.owns_module_name(name.id.as_str()))
                    {
                        self.reject(target.range(), "非模块/global裸名del目标");
                    }
                }
            }
            ast::Stmt::ImportFrom(import) if import.level != 0 => self
                .requirements
                .push((statement.range(), Requirement::RelativeFrom)),
            ast::Stmt::Global(global) => {
                for name in &global.names {
                    self.name(name.as_str(), name.range());
                }
            }
            ast::Stmt::Nonlocal(nonlocal) => {
                for name in &nonlocal.names {
                    if name.as_str() == "__class__" {
                        self.reject(name.range(), "nonlocal __class__ 的显式类 cell 协议");
                    } else {
                        self.name(name.as_str(), name.range());
                    }
                }
            }
            ast::Stmt::FunctionDef(def) if def.name.as_str() == "__classcell__" => {
                self.reject(def.name.range(), "显式 __classcell__ 类创建协议");
            }
            ast::Stmt::ClassDef(class) if class.name.as_str() == "__classcell__" => {
                self.reject(class.name.range(), "显式 __classcell__ 类创建协议");
            }
            _ => {}
        }
        // 即使整个语句被拒绝，仍扫描所有子节点，统一报告隐藏的非法结构。
        walk_stmt(self, statement);
        self.generator = previous_generator;
        if let Some(scope) = saved {
            self.scope = scope;
        }
    }

    fn visit_expr(&mut self, expression: &'a ast::Expr) {
        match expression {
            // CP3.12急切推导式沿用当前frame builtins；现synthetic函数协议尚未接好。
            ast::Expr::ListComp(_) | ast::Expr::SetComp(_) | ast::Expr::DictComp(_) => {
                self.reject(expression.range(), "急切推导式的frame环境继承")
            }
            ast::Expr::Name(name) if name.id.as_str() == "__debug__" => {
                // 这是编译期语言常量，不能伪装成可由实例字典遮蔽的普通名字。
                self.reject(name.range, "语言常量 __debug__");
            }
            ast::Expr::Name(name) => self.name(name.id.as_str(), name.range),
            // 这三个属性名没有专用 frame 协议，走已有的通用属性读写/调用即可。
            // 不能把 re.compile 或任意对象的同名属性误当环境裸名；receiver 仍递归预检。
            // 其它环境属性仍保守禁用，这是尚未验收的能力边界，不是裸名语义。
            ast::Expr::Attribute(attribute) if attribute.attr.as_str() == "__class__" => {
                self.reject(
                    attribute.attr.range(),
                    "属性 __class__ 协议；词法裸名 cell 与属性不同",
                );
            }
            ast::Expr::Attribute(attribute)
                if !matches!(attribute.attr.as_str(), "compile" | "exec" | "eval") =>
            {
                self.name(attribute.attr.as_str(), attribute.attr.range());
            }
            _ => {}
        }
        walk_expr(self, expression);
    }

    fn visit_except_handler(&mut self, handler: &'a ast::ExceptHandler) {
        let ast::ExceptHandler::ExceptHandler(except) = handler;
        if self.generator || matches!(self.scope, crate::module_deletions::DeleteScope::Class) {
            self.reject(handler.range(), "生成器或类体源码except handler");
        } else if except
            .type_
            .as_ref()
            .is_some_and(|target| !matches!(target.as_ref(), ast::Expr::Name(_)))
        {
            self.reject(handler.range(), "非Name/bare except目标");
        } else {
            self.requirements
                .push((handler.range(), Requirement::OrdinaryNameHandler));
        }
        walk_except_handler(self, handler);
    }

    fn visit_parameter(&mut self, parameter: &'a ast::Parameter) {
        if parameter.name.as_str() == "__classcell__" {
            self.reject(parameter.name.range(), "显式 __classcell__ 类创建协议");
        }
        ryupy_syntax::ast::visitor::walk_parameter(self, parameter);
    }

    fn visit_alias(&mut self, alias: &'a ast::Alias) {
        // 导入别名也是对环境的真实写入，不仅检查普通 Name 表达式。
        for name in alias.name.as_str().split('.') {
            self.name(name, alias.name.range());
        }
        if let Some(name) = &alias.asname {
            self.name(name.as_str(), name.range());
        }
    }
}
