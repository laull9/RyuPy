//! 变量注解的可观察顺序：模块一次建字典，每条简单注解按当前绑定写入；局部注解不执行。
use crate::hir::{AssignTarget, Binding, Expr, ExprKind, Literal, Name, Stmt, StmtId, StmtKind};
use ryupy_base::TextRange;
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_stmt};

/// SETUP_ANNOTATIONS 按语法扫描，包含未执行分支，但排除类和函数的独立命名空间。
pub(crate) fn has_module_annotations(body: &[ast::Stmt]) -> bool {
    #[derive(Default)]
    struct Scan(bool);
    impl<'a> Visitor<'a> for Scan {
        fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
            match stmt {
                ast::Stmt::FunctionDef(_) | ast::Stmt::ClassDef(_) => {}
                ast::Stmt::AnnAssign(_) => self.0 = true,
                _ => walk_stmt(self, stmt),
            }
        }
        fn visit_expr(&mut self, _: &'a ast::Expr) {}
    }
    let mut scan = Scan::default();
    for stmt in body {
        scan.visit_stmt(stmt);
    }
    scan.0
}
impl crate::lower::StmtLowerCtx<'_> {
    pub(crate) fn setup_annotations(&mut self, range: TextRange) {
        let Some(&var) = self.module_vars.get(&Name::from("__annotations__")) else {
            return;
        };
        let value = self.body.exprs.alloc(Expr {
            kind: ExprKind::Dict(vec![]),
            range,
        });
        let id = self.annotation_stmt(
            StmtKind::Assign {
                target: AssignTarget::ModuleVar(var),
                value,
            },
            range,
        );
        self.body.root.push(id);
    }
    pub(crate) fn lower_postponed_assignment(
        &mut self,
        annotation: &ast::StmtAnnAssign,
    ) -> Option<StmtId> {
        if let Some(range) = invalid_postponed_annotation(&annotation.annotation) {
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(self.file, range),
                    "future 注解的当前作用域不允许 yield、await 或海象表达式",
                )
                .with_code("E0100"),
            );
            return None;
        }
        self.lower_annotation_assignment(annotation)
    }

    /// 函数内属性/下标注解不求值，也不存储类型元数据；仅目标与可选右值执行。
    pub(crate) fn lower_local_annotated_target(
        &mut self,
        annotation: &ast::StmtAnnAssign,
    ) -> Option<StmtId> {
        // 注解虽不执行，海象仍会改变CPython局部符号绑定；未实现时不能悄悄忽略。
        struct FindNamedExpression(bool);
        impl<'a> Visitor<'a> for FindNamedExpression {
            fn visit_expr(&mut self, expr: &'a ast::Expr) {
                if matches!(expr, ast::Expr::Named(_)) {
                    self.0 = true;
                } else {
                    ryupy_syntax::ast::visitor::walk_expr(self, expr);
                }
            }
        }
        let mut scan = FindNamedExpression(false);
        scan.visit_expr(&annotation.annotation);
        if scan.0 {
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(self.file, annotation.range),
                    "暂不支持局部目标注解中的海象表达式：注解不执行但仍影响局部符号绑定",
                )
                .with_code("E0100"),
            );
            return None;
        }
        self.lower_annotation_assignment(annotation)
    }

    /// 两种调用场景共用赋值时序；只有 future 模块简单名称会走末尾注解字典写入。
    fn lower_annotation_assignment(&mut self, annotation: &ast::StmtAnnAssign) -> Option<StmtId> {
        let range = annotation.range;
        let mut statements = Vec::new();
        // 右值先求值，再计算/写入目标；无初值的属性/下标仍求值目标组成部分，但不读取目标值。
        let value = annotation
            .value
            .as_deref()
            .map(|value| self.lower_expr(value));
        let target = match annotation.target.as_ref() {
            ast::Expr::Name(name) => {
                let binding = self.resolve_binding(&Name::from(name.id.as_str()))?;
                match binding {
                    Binding::Local(id) => {
                        self.body.locals[id].annotation = Some(crate::hir::TypeRef::Runtime);
                        AssignTarget::Local(id)
                    }
                    Binding::ModuleVar(id) => {
                        self.vars[id].annotation = Some(crate::hir::TypeRef::Runtime);
                        AssignTarget::ModuleVar(id)
                    }
                    Binding::Free(id) => AssignTarget::Free(id),
                }
            }
            ast::Expr::Attribute(attribute) => {
                let base = self.lower_expr(&attribute.value);
                if value.is_none() {
                    statements.push(self.annotation_stmt(StmtKind::Expr(base), range));
                }
                AssignTarget::Attribute {
                    base,
                    name: Name::from(attribute.attr.as_str()),
                }
            }
            ast::Expr::Subscript(subscript) => {
                let base = self.lower_expr(&subscript.value);
                let index = self.lower_subscript_index(&subscript.slice);
                if value.is_none() {
                    statements.push(self.annotation_stmt(StmtKind::Expr(base), range));
                    statements.push(self.annotation_stmt(StmtKind::Expr(index), range));
                }
                AssignTarget::Subscript { base, index }
            }
            _ => return None,
        };
        if let Some(value) = value {
            statements.push(self.annotation_stmt(StmtKind::Assign { target, value }, range));
        }
        if self.module_scope
            && annotation.simple
            && let ast::Expr::Name(name) = annotation.target.as_ref()
        {
            let value = self.lower_annotation(&annotation.annotation);
            let &var = self.module_vars.get(&Name::from("__annotations__"))?;
            let base = self.body.exprs.alloc(Expr {
                kind: ExprKind::ModuleVar(var),
                range,
            });
            let index = self.body.exprs.alloc(Expr {
                kind: ExprKind::Literal(Literal::Str(name.id.as_str().into())),
                range,
            });
            statements.push(self.annotation_stmt(
                StmtKind::Assign {
                    target: AssignTarget::Subscript { base, index },
                    value,
                },
                range,
            ));
        }
        if statements.len() == 1 {
            return statements.pop();
        }
        if statements.is_empty() {
            return None;
        }
        let cond = self.body.exprs.alloc(Expr {
            kind: ExprKind::Literal(Literal::Bool(true)),
            range,
        });
        Some(self.annotation_stmt(
            StmtKind::If {
                cond,
                then_: statements,
                else_: vec![],
            },
            range,
        ))
    }
    fn annotation_stmt(&mut self, kind: StmtKind, range: TextRange) -> StmtId {
        self.body.stmts.alloc(Stmt {
            kind,
            range,
            line: self.line_of(range.start().to_usize()),
        })
    }
}

#[cfg(test)]
mod tests {
    use crate::*;

    fn lower(source: &str) -> (Module, Vec<ryupy_base::Diagnostic>) {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("annotated_targets.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty());
        lower_module(&parsed, &ModuleInfo::default(), source)
    }

    #[test]
    fn local_target_annotations_are_not_lowered_as_runtime_expressions() {
        let (module, diagnostics) = lower(
            "def f(value):\n    value.x: Unknown = 1\n    value[0]: (lambda: missing)\n    return 2\n",
        );
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        assert_eq!(module.functions.len(), 2);
        assert!(
            !module
                .vars
                .iter()
                .any(|(_, var)| matches!(var.name.as_str(), "Unknown" | "missing"))
        );
    }

    #[test]
    fn ignored_yield_marks_generator_but_ignored_super_is_not_called() {
        let (module, diagnostics) =
            lower("def f(value):\n    value.x: (yield super()) = 1\n    return 2\n");
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let (_, function) = module
            .functions
            .iter()
            .find(|(_, f)| f.name.as_str() == "f")
            .unwrap();
        assert!(function.is_generator);
        assert!(
            !function
                .body
                .exprs
                .iter()
                .any(|(_, expr)| matches!(expr.kind, ExprKind::Yield(_) | ExprKind::YieldFrom(_)))
        );
    }

    #[test]
    fn eager_module_target_annotation_remains_explicitly_unsupported() {
        let (_, diagnostics) = lower("object.field: int = 1\n");
        assert!(diagnostics.iter().any(|d| d.code == Some("E0100")));
    }
}

#[cfg(test)]
mod named_annotation_tests {
    #[test]
    fn ignored_named_expression_is_not_silently_treated_as_a_global_read() {
        let source = "y = 5\ndef f(obj):\n    obj.x: (y := int) = 1\n    return y\n";
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("annotation_walrus.py", source);
        let (parsed, parse_errors) = ryupy_syntax::parse(file, source);
        assert!(parse_errors.is_empty());
        let (_, diagnostics) = crate::lower_module(&parsed, &crate::ModuleInfo::default(), source);
        assert!(diagnostics.iter().any(|d| d.code == Some("E0100")));
    }
}

/// future注解禁止当前作用域的yield/await/海象；lambda/推导式内部是独立作用域。
fn invalid_postponed_annotation(expr: &ast::Expr) -> Option<TextRange> {
    use ryupy_base::Ranged;
    struct Check(Option<TextRange>);
    impl<'a> Visitor<'a> for Check {
        fn visit_expr(&mut self, expr: &'a ast::Expr) {
            if self.0.is_some() {
                return;
            }
            if let ast::Expr::Lambda(lambda) = expr {
                if let Some(parameters) = &lambda.parameters {
                    for parameter in parameters.iter_non_variadic_params() {
                        if let Some(default) = &parameter.default {
                            self.visit_expr(default);
                        }
                    }
                }
                return;
            }
            if let Some(comprehension) = crate::comprehension_ast::Comprehension::from_expr(expr) {
                if let Some(generator) = comprehension.generators.first() {
                    self.visit_expr(&generator.iter);
                }
                return;
            }
            if matches!(
                expr,
                ast::Expr::Named(_)
                    | ast::Expr::Yield(_)
                    | ast::Expr::YieldFrom(_)
                    | ast::Expr::Await(_)
            ) {
                self.0 = Some(expr.range());
            } else {
                ryupy_syntax::ast::visitor::walk_expr(self, expr);
            }
        }
    }
    let mut check = Check(None);
    check.visit_expr(expr);
    check.0
}

#[cfg(test)]
mod postponed_annotation_scope_tests {
    fn lower(annotation: &str) -> Vec<ryupy_base::Diagnostic> {
        let source = format!(
            "from __future__ import annotations\ndef f(obj):\n    obj.value: {annotation} = 1\n    return 2\n"
        );
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("future_scope.py", source.clone());
        let (parsed, diagnostics) = ryupy_syntax::parse(file, &source);
        // Ruff只负责语法形状，future的此项编译期限制在HIR明确校验。
        assert!(diagnostics.is_empty(), "{annotation}: {diagnostics:?}");
        crate::lower_module(&parsed, &crate::ModuleInfo::default(), &source).1
    }
    #[test]
    fn rejects_current_annotation_scope_yield_and_named_expression() {
        for annotation in [
            "(yield 1)",
            "(yield from [])",
            "(name := int)",
            "(lambda arg=(yield 1): arg)",
            "(lambda arg=(name := int): arg)",
            "[x for x in (yield 1)]",
            "(await pending)",
        ] {
            let diagnostics = lower(annotation);
            assert!(
                diagnostics.iter().any(|d| d.code == Some("E0100")),
                "{annotation}: {diagnostics:?}"
            );
        }
    }
    #[test]
    fn permits_independent_lambda_and_comprehension_scopes() {
        for annotation in [
            "(lambda: (yield 1))",
            "(lambda: (name := int))",
            "[(name := int) for _ in []]",
            "((name := int) for _ in [])",
        ] {
            let diagnostics = lower(annotation);
            assert!(diagnostics.is_empty(), "{annotation}: {diagnostics:?}");
        }
    }
}
