//! 函数直属稳定原生异常导入证明：结构支配、局部写集合与闭包边界独立于普通import执行。
//! 不提升模块导出，不传播free/cell，不依赖属性拼写或词法offset冒充控制流支配。
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Ranged, TextSize};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{
    Visitor, walk_except_handler, walk_expr, walk_pattern, walk_stmt,
};

use crate::{exception_imports::ExceptionImport, hir::Name, scope::FunctionScope};

#[derive(Debug)]
pub(crate) struct LocalExceptionImport {
    binding: ExceptionImport,
    direct_index: usize,
}
impl LocalExceptionImport {
    pub(crate) fn resolve(
        &self,
        path: &[&str],
        position: TextSize,
        handler_index: usize,
        mark_used: bool,
    ) -> Option<&'static str> {
        // 到达后续直属语句必已成功完成该import；同一复合语句内不提供证明。
        if self.direct_index >= handler_index {
            return None;
        }
        if mark_used {
            self.binding.resolve(path, position)
        } else {
            self.binding.peek(path, position)
        }
    }
}

pub(crate) fn collect(
    def: &ast::StmtFunctionDef,
    native: &FxHashSet<String>,
    scope: &FunctionScope,
) -> FxHashMap<Name, LocalExceptionImport> {
    let mut imports = FxHashMap::default();
    for (index, stmt) in def.body.iter().enumerate() {
        // 仅绝对直属import；不借空package把相对导入猜为原生模块。
        let direct = match stmt {
            ast::Stmt::Import(_) => true,
            ast::Stmt::ImportFrom(from) => from.level == 0,
            _ => false,
        };
        if !direct {
            continue;
        }
        for request in crate::imports::collect_imports(std::slice::from_ref(stmt), "") {
            if native.contains(&request.module) && !request.bind_root {
                imports.insert(
                    Name::from(request.bind.as_str()),
                    LocalExceptionImport {
                        binding: ExceptionImport::new(
                            request.module,
                            request.name,
                            request.bind_root,
                            stmt.range().end(),
                        ),
                        direct_index: index,
                    },
                );
            }
        }
    }
    let mut writes = Writes::default();
    for parameter in def.parameters.iter() {
        writes.invalidate(parameter.name().as_str());
    }
    for stmt in &def.body {
        writes.visit_stmt(stmt);
    }
    imports.retain(|name, import| {
        scope.locals.contains_key(name)
            && !scope.globals.contains(name)
            && !scope.nonlocals.contains(name)
            && !scope.free.contains_key(name)
            && !scope.cells.contains(name)
            && !writes.changed.contains(name)
            && !writes.attribute_writes.get(name).is_some_and(|paths| {
                paths
                    .iter()
                    .any(|path| import.binding.affected_by_attribute_write(path))
            })
            && writes.import_count.get(name) == Some(&1)
    });
    imports
}

/// 不依赖只扫语句目标的collect_assigned_names：表达式、pattern和隐式except删除也算写。
#[derive(Default)]
struct Writes {
    changed: FxHashSet<Name>,
    import_count: FxHashMap<Name, usize>,
    attribute_writes: FxHashMap<Name, Vec<Vec<String>>>,
    comprehension_locals: FxHashSet<Name>,
}
impl Writes {
    fn invalidate(&mut self, name: &str) {
        self.changed.insert(Name::from(name));
    }

    fn import(&mut self, aliases: &[ast::Alias], root: bool) {
        for alias in aliases {
            let name = alias.asname.as_ref().map_or_else(
                || {
                    if root {
                        alias.name.as_str().split('.').next().unwrap_or_default()
                    } else {
                        alias.name.as_str()
                    }
                },
                |name| name.as_str(),
            );
            *self.import_count.entry(Name::from(name)).or_default() += 1;
        }
    }

    // 嵌套函数普通local写不属于本作用域；其nonlocal声明保守失效，补足闭包分析的边角。
    fn nested_nonlocals(&mut self, body: &[ast::Stmt]) {
        struct Nonlocals<'a>(&'a mut FxHashSet<Name>);
        impl<'a> Visitor<'a> for Nonlocals<'_> {
            fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
                if let ast::Stmt::Nonlocal(statement) = stmt {
                    self.0
                        .extend(statement.names.iter().map(|name| Name::from(name.as_str())));
                }
                walk_stmt(self, stmt);
            }
        }
        let mut scan = Nonlocals(&mut self.changed);
        for stmt in body {
            scan.visit_stmt(stmt);
        }
    }
}
impl<'a> Visitor<'a> for Writes {
    fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
        match stmt {
            ast::Stmt::Import(import) => self.import(&import.names, true),
            ast::Stmt::ImportFrom(import) => self.import(&import.names, false),
            ast::Stmt::FunctionDef(def) => {
                self.invalidate(def.name.as_str());
                // 函数体另属作用域；定义时的默认值/装饰器/注解表达式仍可写当前绑定。
                for decorator in &def.decorator_list {
                    self.visit_expr(&decorator.expression);
                }
                self.visit_parameters(&def.parameters);
                if let Some(annotation) = &def.returns {
                    self.visit_expr(annotation);
                }
                if let Some(parameters) = &def.type_params {
                    self.visit_type_params(parameters);
                }
                self.nested_nonlocals(&def.body);
            }
            ast::Stmt::ClassDef(class) => {
                self.invalidate(class.name.as_str());
                // 函数内class当前已明确拒绝；保守扫描全部类体防未来开放后漏掉nonlocal。
                walk_stmt(self, stmt);
            }
            ast::Stmt::Global(declaration) => {
                for name in &declaration.names {
                    self.invalidate(name.as_str());
                }
            }
            ast::Stmt::Nonlocal(declaration) => {
                for name in &declaration.names {
                    self.invalidate(name.as_str());
                }
            }
            _ => walk_stmt(self, stmt),
        }
    }

    fn visit_expr(&mut self, expr: &'a ast::Expr) {
        if let Some(comp) = crate::comprehension_ast::Comprehension::from_expr(expr) {
            if let Some(first) = comp.generators.first() {
                self.visit_expr(&first.iter);
            }
            let outer = self.comprehension_locals.clone();
            self.comprehension_locals
                .extend(crate::exception_imports::comprehension_bindings(comp));
            crate::exception_imports::walk_comprehension_body(self, comp);
            self.comprehension_locals = outer;
            return;
        }
        match expr {
            ast::Expr::Named(named) => {
                if let ast::Expr::Name(name) = named.target.as_ref() {
                    self.invalidate(name.id.as_str());
                }
                self.visit_expr(&named.value);
            }
            ast::Expr::Name(name)
                if !self
                    .comprehension_locals
                    .contains(&Name::from(name.id.as_str()))
                    && matches!(name.ctx, ast::ExprContext::Store | ast::ExprContext::Del) =>
            {
                self.invalidate(name.id.as_str());
            }
            ast::Expr::Attribute(attr)
                if matches!(attr.ctx, ast::ExprContext::Store | ast::ExprContext::Del) =>
            {
                if let Some((root, path)) = crate::exception_imports::attribute_path(expr)
                    && !self.comprehension_locals.contains(&Name::from(root))
                {
                    self.attribute_writes
                        .entry(Name::from(root))
                        .or_default()
                        .push(path.into_iter().map(str::to_owned).collect());
                }
                walk_expr(self, expr);
            }
            ast::Expr::Lambda(lambda) => {
                // lambda体独立；默认表达式在包围作用域求值。
                if let Some(parameters) = &lambda.parameters {
                    self.visit_parameters(parameters);
                }
            }
            _ => walk_expr(self, expr),
        }
    }

    fn visit_except_handler(&mut self, handler: &'a ast::ExceptHandler) {
        let ast::ExceptHandler::ExceptHandler(inner) = handler;
        if let Some(name) = &inner.name {
            self.invalidate(name.as_str());
        }
        walk_except_handler(self, handler);
    }

    fn visit_pattern(&mut self, pattern: &'a ast::Pattern) {
        let binding = match pattern {
            ast::Pattern::MatchAs(pattern) => pattern.name.as_ref(),
            ast::Pattern::MatchStar(pattern) => pattern.name.as_ref(),
            ast::Pattern::MatchMapping(pattern) => pattern.rest.as_ref(),
            _ => None,
        };
        if let Some(name) = binding {
            self.invalidate(name.as_str());
        }
        walk_pattern(self, pattern);
    }
}
