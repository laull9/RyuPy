//! 类体控制流降级；保留每条属性/方法的源码顺序，条件分支不做常量折叠。
use crate::class::{MethodSlot, lower_class_expr, unsupported_class_statement};
use crate::class_scope::ClassScope;
use crate::hir::{Body, ClassBase, ClassItem, ClassItemKind, Field, FuncId, Name};
use crate::operator::lower_type_ref;
use crate::scope::GlobalScope;
use rustc_hash::FxHashMap;
use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;

/// 只穿过类体 if，方法体与嵌套类不是同一个命名空间。
pub(crate) fn methods(statements: &[ast::Stmt]) -> Vec<&ast::StmtFunctionDef> {
    let mut result = Vec::new();
    for statement in statements {
        match statement {
            ast::Stmt::FunctionDef(function) => result.push(function),
            ast::Stmt::If(branch) => {
                result.extend(methods(&branch.body));
                for clause in &branch.elif_else_clauses {
                    result.extend(methods(&clause.body));
                }
            }
            _ => {}
        }
    }
    result
}

pub(crate) fn has_annotations(statements: &[ast::Stmt]) -> bool {
    statements.iter().any(|statement| match statement {
        ast::Stmt::AnnAssign(_) => true,
        ast::Stmt::If(branch) => {
            has_annotations(&branch.body)
                || branch
                    .elif_else_clauses
                    .iter()
                    .any(|clause| has_annotations(&clause.body))
        }
        _ => false,
    })
}

#[allow(clippy::too_many_arguments)]
pub(crate) fn lower_items(
    statements: &[ast::Stmt],
    class_names: &mut ClassScope,
    file: FileId,
    globals: &GlobalScope,
    base: Option<ClassBase>,
    is_dataclass: bool,
    special: bool,
    method_map: &FxHashMap<ryupy_base::TextRange, &MethodSlot>,
    module_vars: &FxHashMap<Name, crate::hir::ModuleVarId>,
    nested_funcs: &FxHashMap<ryupy_base::TextRange, FuncId>,
    nested_defaults: &mut crate::definition_metadata::DefinitionMetadata,
    class_scopes: &mut FxHashMap<FuncId, ClassScope>,
    main_body: &mut Body,
    diags: &mut Vec<Diagnostic>,
    fields: &mut Vec<Field>,
    declared_fields: &mut Vec<Name>,
) -> Vec<ClassItem> {
    let mut body = Vec::new();
    for stmt in statements {
        if let ast::Stmt::AnnAssign(annotation) = stmt
            && annotation.simple
            && let ast::Expr::Name(name) = annotation.target.as_ref()
        {
            let name = Name::from(name.id.as_str());
            if !declared_fields.contains(&name) {
                declared_fields.push(name.clone());
            }
            // 保存原静态字段布局线索，默认执行仍走runtime_dataclass；后续特化须另证安全。
            if is_dataclass {
                let field = Field {
                    name: name.clone(),
                    annotation: lower_type_ref(&annotation.annotation, file, globals, diags),
                    range: annotation.range(),
                };
                if let Some(previous) = fields.iter_mut().find(|field| field.name == name) {
                    *previous = field;
                } else {
                    fields.push(field);
                }
            }
        }
        match stmt {
            ast::Stmt::If(branch) => {
                if special {
                    diags.push(unsupported_class_statement(
                        file,
                        branch.range(),
                        "特殊类暂不支持条件类体",
                    ));
                    continue;
                }
                let mut alternatives = Vec::new();
                alternatives.push((
                    Some(branch.test.as_ref()),
                    branch.body.as_slice(),
                    branch.range(),
                ));
                for clause in &branch.elif_else_clauses {
                    alternatives.push((
                        clause.test.as_ref(),
                        clause.body.as_slice(),
                        clause.range(),
                    ));
                }
                let entry = class_names.clone();
                let mut lowered = Vec::new();
                let mut merged: Option<ClassScope> = None;
                let mut has_else = false;
                for (test, statements, range) in alternatives {
                    let test = test.map(|test| {
                        lower_class_expr(
                            Some(&entry),
                            file,
                            globals,
                            module_vars,
                            nested_funcs,
                            nested_defaults,
                            main_body,
                            diags,
                            test,
                        )
                    });
                    has_else |= test.is_none();
                    let mut branch_scope = entry.clone();
                    let items = lower_items(
                        statements,
                        &mut branch_scope,
                        file,
                        globals,
                        base,
                        is_dataclass,
                        special,
                        method_map,
                        module_vars,
                        nested_funcs,
                        nested_defaults,
                        class_scopes,
                        main_body,
                        diags,
                        fields,
                        declared_fields,
                    );
                    if let Some(previous) = merged.take() {
                        let mut state = ClassScope::default();
                        state.merge(previous, branch_scope);
                        merged = Some(state);
                    } else {
                        merged = Some(branch_scope);
                    }
                    lowered.push((test, items, range));
                }
                if let Some(mut state) = merged {
                    if !has_else {
                        let previous = state.clone();
                        state.merge(previous, entry);
                    }
                    *class_names = state;
                }
                let mut tail = Vec::new();
                for (test, items, range) in lowered.into_iter().rev() {
                    tail = match test {
                        Some(test) => vec![ClassItem {
                            kind: ClassItemKind::If {
                                test,
                                body: items,
                                orelse: tail,
                            },
                            range,
                        }],
                        None => items,
                    };
                }
                body.extend(tail);
            }
            ast::Stmt::Pass(_) => {}
            ast::Stmt::Assign(assign) => {
                // 只支持 `name = value` 的类属性；其它目标（属性 / 下标 / 解包）暂缓。
                if assign.targets.len() != 1 {
                    diags.push(unsupported_class_statement(
                        file,
                        assign.range(),
                        "链式赋值",
                    ));
                    continue;
                }
                let ast::Expr::Name(target) = &assign.targets[0] else {
                    diags.push(unsupported_class_statement(
                        file,
                        assign.targets[0].range(),
                        "只支持给类属性名字赋值",
                    ));
                    continue;
                };
                let value = lower_class_expr(
                    Some(class_names),
                    file,
                    globals,
                    module_vars,
                    nested_funcs,
                    nested_defaults,
                    main_body,
                    diags,
                    &assign.value,
                );
                body.push(ClassItem {
                    kind: ClassItemKind::Attr {
                        name: Name::from(target.id.as_str()),
                        value,
                    },
                    range: assign.range(),
                });
            }
            ast::Stmt::AnnAssign(ann) => {
                if let ast::Expr::Name(target) = &*ann.target {
                    if let Some(value_expr) = &ann.value {
                        let value = lower_class_expr(
                            Some(class_names),
                            file,
                            globals,
                            module_vars,
                            nested_funcs,
                            nested_defaults,
                            main_body,
                            diags,
                            value_expr,
                        );
                        body.push(ClassItem {
                            kind: ClassItemKind::Attr {
                                name: Name::from(target.id.as_str()),
                                value,
                            },
                            range: ann.range(),
                        });
                        // 右值先写入命名空间，标注表达式可立即引用该名字。
                        class_names.insert(Name::from(target.id.as_str()));
                    }
                    if globals.postponed_annotations && !ann.simple {
                        continue;
                    }
                    let value = if globals.postponed_annotations {
                        crate::annotation_format::lower(&ann.annotation, file, main_body, diags)
                    } else {
                        lower_class_expr(
                            Some(class_names),
                            file,
                            globals,
                            module_vars,
                            nested_funcs,
                            nested_defaults,
                            main_body,
                            diags,
                            &ann.annotation,
                        )
                    };
                    body.push(ClassItem {
                        // 括号名字标注只求表达式，不登记字段；可选初值已在前面写入。
                        kind: if ann.simple {
                            ClassItemKind::Annotation {
                                name: Name::from(target.id.as_str()),
                                value,
                            }
                        } else {
                            ClassItemKind::Expr(value)
                        },
                        range: ann.range(),
                    });
                } else {
                    diags.push(unsupported_class_statement(
                        file,
                        ann.range(),
                        "带标注的属性名必须是标识符",
                    ));
                }
            }
            ast::Stmt::FunctionDef(fn_def) => {
                if fn_def.name.as_str() == "__getattribute__" {
                    diags.push(
                        Diagnostic::error(
                            Span::new(file, fn_def.range()),
                            "暂不支持自定义 __getattribute__",
                        )
                        .with_code("E0100")
                        .with_primary_label("属性查找协议目前由运行时固定实现"),
                    );
                    continue;
                }
                let Some(slot) = method_map.get(&fn_def.range) else {
                    diags.push(
                        Diagnostic::error(
                            Span::new(file, fn_def.range()),
                            "内部错误：方法没有登记为函数",
                        )
                        .with_code("E0101")
                        .with_primary_label("方法预扫描遗漏"),
                    );
                    continue;
                };
                class_scopes.insert(slot.func, class_names.clone());
                let decorators = crate::decorators::lower_list(
                    &fn_def.decorator_list,
                    false,
                    Some(class_names),
                    file,
                    globals,
                    module_vars,
                    nested_funcs,
                    nested_defaults,
                    main_body,
                    diags,
                );
                body.push(ClassItem {
                    kind: ClassItemKind::Method {
                        func: slot.func,
                        decorators,
                    },
                    range: fn_def.range(),
                });
            }
            ast::Stmt::Expr(expr_stmt) => {
                // 外层首docstring已单独降低；这里字符串只是无副作用Expr。TypedDict保原协议。
                if matches!(&*expr_stmt.value, ast::Expr::StringLiteral(_))
                    && !matches!(base, Some(ClassBase::TypedDict { .. }))
                {
                    continue;
                }
                let value = lower_class_expr(
                    Some(class_names),
                    file,
                    globals,
                    module_vars,
                    nested_funcs,
                    nested_defaults,
                    main_body,
                    diags,
                    &expr_stmt.value,
                );
                body.push(ClassItem {
                    kind: ClassItemKind::Expr(value),
                    range: expr_stmt.range(),
                });
            }
            _ => {
                diags.push(unsupported_class_statement(
                    file,
                    stmt.range(),
                    "类体内暂不支持该语句",
                ));
            }
        }
        match stmt {
            ast::Stmt::Assign(assign) => {
                for target in &assign.targets {
                    if let ast::Expr::Name(name) = target {
                        class_names.insert(Name::from(name.id.as_str()));
                    }
                }
            }
            ast::Stmt::AnnAssign(assign) if assign.value.is_some() => {
                if let ast::Expr::Name(name) = assign.target.as_ref() {
                    class_names.insert(Name::from(name.id.as_str()));
                }
            }
            ast::Stmt::FunctionDef(def) => {
                class_names.insert(Name::from(def.name.as_str()));
            }
            _ => {}
        }
    }

    body
}
