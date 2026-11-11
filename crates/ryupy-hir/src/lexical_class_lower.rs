//! 新词法类体在真实定义者arena内降级；模板只保存表达式ID，不持运行期类或环境。
//! namespace完整静态绑定集独立于函数locals，方法的默认值和注解仍归定义者arena。
use crate::expr::ExprLowerCtx;
use crate::hir::*;
use la_arena::Arena;
use rustc_hash::FxHashSet;
use ryupy_base::{Diagnostic, Ranged, Span};
use ryupy_syntax::ast;

#[derive(Debug, Clone)]
pub enum LexicalBinding {
    Outer(Binding),
    Namespace(Name),
}
#[derive(Debug)]
pub struct LoweredClass {
    pub base: Option<ClassBase>,
    pub decorators: Vec<Decorator>,
    pub body: Vec<ClassItem>,
    pub target: LexicalBinding,
    pub annotations: bool,
}

impl crate::lower::StmtLowerCtx<'_> {
    pub(crate) fn lower_lexical_class(&mut self, class: &ast::StmtClassDef) -> Option<StmtId> {
        if self.forbid_lexical_class {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, class.range),
                    "定义位置类暂不支持生成器或async定义者",
                )
                .with_code("E0100"),
            );
            return None;
        }
        let name = Name::from(class.name.as_str());
        let target = self.resolve_binding(&name)?;
        let mut expr = ExprLowerCtx {
            class_names: None,
            allow_yield: false,
            file: self.file,
            global_scope: self.global_scope,
            func_scope: self.func_scope,
            module_vars: self.module_vars,
            nested_funcs: self.nested_funcs,
            nested_defaults: self.nested_defaults,
            body: self.body,
            diagnostics: self.diagnostics,
        };
        let id = lower_definition(
            class,
            self.classes,
            &mut expr,
            LexicalBinding::Outer(target),
        )?;
        let line = self.line_of(class.range.start().to_usize());
        Some(self.body.stmts.alloc(Stmt {
            kind: StmtKind::ClassDef(id),
            range: class.range,
            line,
        }))
    }
}

pub(crate) fn lower_definition(
    class: &ast::StmtClassDef,
    classes: &Arena<Class>,
    ctx: &mut ExprLowerCtx<'_>,
    target: LexicalBinding,
) -> Option<ClassId> {
    let id = classes.iter().find_map(|(id, value)| {
        (value.range == class.range && value.lexical.is_some()).then_some(id)
    })?;
    if class.type_params.is_some()
        || class.arguments.as_ref().is_some_and(|args| {
            !args.keywords.is_empty()
                || args.args.len() > 1
                || args
                    .args
                    .iter()
                    .any(|arg| matches!(arg, ast::Expr::Starred(_)))
        })
    {
        ctx.diagnostics.push(
            Diagnostic::error(
                Span::new(ctx.file, class.range),
                "定义位置类暂不支持多重继承、类关键字、星基类或类型参数",
            )
            .with_code("E0100"),
        );
    }
    let decorators = class
        .decorator_list
        .iter()
        .map(|decorator| Decorator::Expr(ctx.lower_expr(&decorator.expression)))
        .collect();
    let base = class
        .arguments
        .as_ref()
        .and_then(|args| args.args.first())
        .map(|expr| ClassBase::Runtime(ctx.lower_expr(expr)));
    let mut scope = crate::class_scope::ClassScope::default();
    let mut bindings = FxHashSet::default();
    collect_bindings(&class.body, &mut bindings);
    bindings.insert(Name::from("__module__"));
    bindings.insert(Name::from("__qualname__"));
    scope.lexical_bindings = Some(bindings);
    scope.lexical_class = Some(id);
    scope.outer_class_cell = ctx.class_names.and_then(|scope| scope.lexical_class);
    scope.unsupported_outer_cell = ctx.class_names.is_some_and(|scope| scope.legacy_class_body);
    let annotations = crate::class_body::has_annotations(&class.body);
    let items = {
        let mut body_ctx = ExprLowerCtx {
            class_names: Some(&scope),
            allow_yield: false,
            file: ctx.file,
            global_scope: ctx.global_scope,
            func_scope: ctx.func_scope,
            module_vars: ctx.module_vars,
            nested_funcs: ctx.nested_funcs,
            nested_defaults: ctx.nested_defaults,
            body: ctx.body,
            diagnostics: ctx.diagnostics,
        };
        let mut statements = class.body.as_slice();
        let mut items = Vec::new();
        if let Some(ast::Stmt::Expr(first)) = statements.first()
            && matches!(first.value.as_ref(), ast::Expr::StringLiteral(_))
        {
            items.push(ClassItem {
                kind: ClassItemKind::Attr {
                    name: Name::from("__doc__"),
                    value: body_ctx.lower_expr(&first.value),
                },
                range: first.range,
            });
            statements = &statements[1..];
        }
        items.extend(lower_items(statements, classes, &mut body_ctx));
        items
    };
    ctx.nested_defaults.lexical_classes.insert(
        id,
        LoweredClass {
            base,
            decorators,
            body: items,
            target,
            annotations,
        },
    );
    Some(id)
}

fn collect_bindings(statements: &[ast::Stmt], names: &mut FxHashSet<Name>) {
    for (name, _) in crate::scope::collect_assigned_names(statements) {
        names.insert(name);
    }
    for statement in statements {
        match statement {
            ast::Stmt::FunctionDef(def) => {
                names.insert(Name::from(def.name.as_str()));
            }
            ast::Stmt::ClassDef(class) => {
                names.insert(Name::from(class.name.as_str()));
            }
            ast::Stmt::If(branch) => {
                collect_bindings(&branch.body, names);
                for clause in &branch.elif_else_clauses {
                    collect_bindings(&clause.body, names);
                }
            }
            _ => {}
        }
    }
}

fn lower_items(
    statements: &[ast::Stmt],
    classes: &Arena<Class>,
    ctx: &mut ExprLowerCtx<'_>,
) -> Vec<ClassItem> {
    let mut items = Vec::new();
    for statement in statements {
        let kind = match statement {
            ast::Stmt::Pass(_) => continue,
            ast::Stmt::Expr(expr) => ClassItemKind::Expr(ctx.lower_expr(&expr.value)),
            ast::Stmt::Assign(assign) if assign.targets.len() == 1 => {
                let ast::Expr::Name(name) = &assign.targets[0] else {
                    reject(ctx, statement, "只支持给类属性名字赋值");
                    continue;
                };
                ClassItemKind::Attr {
                    name: Name::from(name.id.as_str()),
                    value: ctx.lower_expr(&assign.value),
                }
            }
            ast::Stmt::AnnAssign(ann) => {
                let ast::Expr::Name(name) = ann.target.as_ref() else {
                    reject(ctx, statement, "带标注的属性名必须是标识符");
                    continue;
                };
                if let Some(value) = &ann.value {
                    items.push(ClassItem {
                        kind: ClassItemKind::Attr {
                            name: Name::from(name.id.as_str()),
                            value: ctx.lower_expr(value),
                        },
                        range: ann.range,
                    });
                }
                let value = annotation(ctx, &ann.annotation);
                if ann.simple {
                    ClassItemKind::Annotation {
                        name: Name::from(name.id.as_str()),
                        value,
                    }
                } else {
                    ClassItemKind::Expr(value)
                }
            }
            ast::Stmt::FunctionDef(def) => {
                if def.is_async || def.type_params.is_some() {
                    reject(ctx, statement, "异步方法或类型参数");
                    continue;
                }
                let Some(&func) = ctx.nested_funcs.get(&def.range) else {
                    reject(ctx, statement, "方法模板未登记");
                    continue;
                };
                let decorators = def
                    .decorator_list
                    .iter()
                    .map(|decorator| Decorator::Expr(ctx.lower_expr(&decorator.expression)))
                    .collect();
                lower_method_metadata(def, func, ctx);
                ClassItemKind::Method { func, decorators }
            }
            ast::Stmt::ClassDef(class) => {
                let Some(id) = lower_definition(
                    class,
                    classes,
                    ctx,
                    LexicalBinding::Namespace(Name::from(class.name.as_str())),
                ) else {
                    continue;
                };
                ClassItemKind::NestedClass(id)
            }
            ast::Stmt::If(branch) => {
                let test = ctx.lower_expr(&branch.test);
                let body = lower_items(&branch.body, classes, ctx);
                let mut tail = Vec::new();
                // HIR表达式分配次序不是执行次序，CFG仍保源码从外到内的测试顺序。
                for clause in branch.elif_else_clauses.iter().rev() {
                    let body = lower_items(&clause.body, classes, ctx);
                    tail = if let Some(test) = &clause.test {
                        vec![ClassItem {
                            kind: ClassItemKind::If {
                                test: ctx.lower_expr(test),
                                body,
                                orelse: tail,
                            },
                            range: clause.range,
                        }]
                    } else {
                        body
                    };
                }
                ClassItemKind::If {
                    test,
                    body,
                    orelse: tail,
                }
            }
            _ => {
                reject(ctx, statement, "类体内暂不支持该语句");
                continue;
            }
        };
        items.push(ClassItem {
            kind,
            range: statement.range(),
        });
    }
    items
}

fn lower_method_metadata(def: &ast::StmtFunctionDef, func: FuncId, ctx: &mut ExprLowerCtx<'_>) {
    let mut defaults = Vec::new();
    for param in def
        .parameters
        .posonlyargs
        .iter()
        .chain(&def.parameters.args)
    {
        defaults.push(param.default.as_deref().map(|expr| ctx.lower_expr(expr)));
    }
    if def.parameters.vararg.is_some() {
        defaults.push(None);
    }
    for param in &def.parameters.kwonlyargs {
        defaults.push(param.default.as_deref().map(|expr| ctx.lower_expr(expr)));
    }
    if def.parameters.kwarg.is_some() {
        defaults.push(None);
    }
    ctx.nested_defaults.defaults.insert(func, defaults);
    let mut annotations = Vec::new();
    for param in def.parameters.iter() {
        if let Some(expr) = param.annotation() {
            annotations.push((Name::from(param.name().as_str()), annotation(ctx, expr)));
        }
    }
    if let Some(expr) = &def.returns {
        annotations.push((Name::from("return"), annotation(ctx, expr)));
    }
    ctx.nested_defaults.annotations.insert(func, annotations);
}
fn annotation(ctx: &mut ExprLowerCtx<'_>, expr: &ast::Expr) -> ExprId {
    if ctx.global_scope.postponed_annotations {
        crate::annotation_format::lower(expr, ctx.file, ctx.body, ctx.diagnostics)
    } else {
        ctx.lower_expr(expr)
    }
}
fn reject(ctx: &mut ExprLowerCtx<'_>, statement: &ast::Stmt, label: &str) {
    ctx.diagnostics
        .push(crate::class::unsupported_class_statement(
            ctx.file,
            statement.range(),
            label,
        ));
}

pub(crate) fn apply(
    classes: &mut Arena<Class>,
    metadata: &mut crate::definition_metadata::DefinitionMetadata,
) {
    for id in metadata.class_cell_reads.drain() {
        classes[id].has_class_cell = true;
    }
    for (id, definition) in metadata.lexical_classes.drain() {
        let class = &mut classes[id];
        class.base = definition.base;
        class.decorators = definition.decorators;
        class.body = definition.body;
        class.has_annotations = definition.annotations;
        if let Some(lexical) = &mut class.lexical {
            lexical.target = Some(definition.target);
        }
    }
}
