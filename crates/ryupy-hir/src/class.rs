//! 类定义 AST → HIR 降级。
//!
//! 支持两类类：
//!
//! 1. **`@dataclass`**（M1.5）：只提取带类型标注的字段，构造由静态 `NewObject` 完成；
//! 2. **普通 class**（M2.5 / M2.6）：类体按源码顺序降级成 [`ClassItem`]：
//!    - `name = value` 写进类命名空间；
//!    - `def` 变成方法函数（`self` 是普通参数）；
//!    - `@property` / `@staticmethod` / `@classmethod` 保留为修饰器标记。
//!
//! 单继承只接受一个基类；基类可以是之前定义的普通类或内建异常类。多重继承、
//! 元类与 `__getattribute__` 等暂缓能力都在这里明确报错，不静默降级。
//!
//! 类体属性的求值位置属于**模块入口函数**（Python 在 `class` 语句执行时求值），
//! 因此属性表达式直接降级到 `main_body`，MIR 再把它们按源码顺序接到类定义处。

use rustc_hash::FxHashMap;
use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;

use crate::expr::ExprLowerCtx;
use crate::hir::{Body, Class, ClassBase, ClassItem, ClassItemKind, ExprId, Field, FuncId, Name};
use crate::operator::lower_type_ref;
use crate::scope::{FunctionScope, GlobalScope, Resolution};

/// 类体里一条 `def` 的登记信息（第一遍收集时产生）。
#[derive(Debug, Clone)]
pub struct MethodSlot {
    pub name: Name,
    pub func: FuncId,
    pub range: ryupy_base::TextRange,
}

/// 解析一个类定义。
///
/// `classes` 是**已经解析过**的类（基类因此必须写在前面，与 CPython 的类体执行顺序一致）。
#[allow(clippy::too_many_arguments)]
pub fn parse_class_def(
    class_def: &ast::StmtClassDef,
    file: FileId,
    globals: &GlobalScope,
    classes: &la_arena::Arena<Class>,
    methods: &[MethodSlot],
    module_vars: &FxHashMap<Name, crate::hir::ModuleVarId>,
    nested_funcs: &FxHashMap<ryupy_base::TextRange, crate::hir::FuncId>,
    nested_defaults: &mut FxHashMap<FuncId, Vec<Option<ExprId>>>,
    class_scopes: &mut FxHashMap<FuncId, rustc_hash::FxHashSet<Name>>,
    main_body: &mut Body,
    diags: &mut Vec<Diagnostic>,
) -> Option<Class> {
    let class_name = Name::from(class_def.name.as_str());

    let (is_dataclass, frozen) =
        crate::decorators::dataclass_flags(class_def, &globals.dataclass_markers, file, diags);
    let decorators = crate::decorators::lower_list(
        &class_def.decorator_list,
        true,
        None,
        file,
        globals,
        module_vars,
        nested_funcs,
        nested_defaults,
        main_body,
        diags,
    );
    let decorated_binding = globals
        .dynamic_names
        .contains(&class_name)
        .then(|| module_vars.get(&class_name).copied())
        .flatten();

    let base = match parse_base(class_def, file, globals, classes, diags) {
        Some(ClassBase::User(id)) if classes[id].decorated_binding.is_some() => {
            if classes[id].is_exception || classes[id].is_dataclass {
                diags.push(
                    Diagnostic::error(
                        Span::new(file, class_def.range),
                        "暂不支持以装饰后的异常类或dataclass作为基类",
                    )
                    .with_code("E0100"),
                );
                None
            } else {
                class_def
                    .arguments
                    .as_ref()
                    .and_then(|args| args.args.first())
                    .map(|expr| {
                        ClassBase::Expr(lower_class_expr(
                            None,
                            file,
                            globals,
                            module_vars,
                            nested_funcs,
                            nested_defaults,
                            main_body,
                            diags,
                            expr,
                        ))
                    })
            }
        }
        base => base,
    };

    // 基类如果是 dataclass：字段布局会变化，暂不支持继承数据类。
    if let Some(ClassBase::User(base_id)) = base
        && classes[base_id].is_dataclass
    {
        diags.push(
            Diagnostic::error(
                Span::new(file, class_def.range()),
                "暂不支持继承 @dataclass 类",
            )
            .with_code("E0100")
            .with_primary_label("数据类继承留到后续里程碑"),
        );
    }

    let is_exception = match base {
        Some(ClassBase::Builtin(_)) => true,
        Some(ClassBase::User(base_id)) => classes[base_id].is_exception,
        None | Some(ClassBase::Expr(_)) => false,
    };

    let mut class_names = rustc_hash::FxHashSet::default();
    let mut fields = Vec::new();
    let mut declared_fields = Vec::new();
    let mut body = Vec::new();
    let method_map: FxHashMap<ryupy_base::TextRange, &MethodSlot> = methods
        .iter()
        .map(|method| (method.range, method))
        .collect();

    for stmt in &class_def.body {
        if let ast::Stmt::AnnAssign(annotation) = stmt
            && let ast::Expr::Name(name) = annotation.target.as_ref()
        {
            let name = Name::from(name.id.as_str());
            if !declared_fields.contains(&name) {
                declared_fields.push(name);
            }
        }
        match stmt {
            ast::Stmt::Pass(_) => {}
            ast::Stmt::AnnAssign(ann) if is_dataclass => {
                if let ast::Expr::Name(target_name) = &*ann.target {
                    if ann.value.is_some() {
                        diags.push(
                            Diagnostic::error(
                                Span::new(file, ann.range()),
                                "dataclass 暂不支持字段默认值",
                            )
                            .with_code("E0100")
                            .with_primary_label("不支持默认值赋初值"),
                        );
                    }
                    fields.push(Field {
                        name: Name::from(target_name.id.as_str()),
                        annotation: lower_type_ref(&ann.annotation, file, globals, diags),
                        range: ann.range(),
                    });
                } else {
                    diags.push(
                        Diagnostic::error(Span::new(file, ann.range()), "字段名必须为标识符")
                            .with_code("E0100")
                            .with_primary_label("无效的字段名"),
                    );
                }
            }
            ast::Stmt::Assign(assign) => {
                if is_dataclass {
                    diags.push(
                        Diagnostic::error(
                            Span::new(file, assign.range()),
                            "dataclass 类体内只允许字段标注",
                        )
                        .with_code("E0100")
                        .with_primary_label("不允许此类语句"),
                    );
                    continue;
                }
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
                    Some(&class_names),
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
                    let Some(value_expr) = &ann.value else {
                        // 只有标注没有值：Python 只记录 __annotations__，不写属性。
                        continue;
                    };
                    let value = lower_class_expr(
                        Some(&class_names),
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
                } else {
                    diags.push(unsupported_class_statement(
                        file,
                        ann.range(),
                        "带标注的属性名必须是标识符",
                    ));
                }
            }
            ast::Stmt::FunctionDef(fn_def) => {
                if is_dataclass {
                    diags.push(
                        Diagnostic::error(
                            Span::new(file, fn_def.range()),
                            "dataclass 暂不支持定义方法",
                        )
                        .with_code("E0100")
                        .with_primary_label("方法属于普通 class"),
                    );
                    continue;
                }
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
                    Some(&class_names),
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
                // docstring：只保留为副作用表达式会无意义地求值字符串，直接跳过。
                if matches!(&*expr_stmt.value, ast::Expr::StringLiteral(_)) {
                    continue;
                }
                if is_dataclass {
                    diags.push(
                        Diagnostic::error(
                            Span::new(file, expr_stmt.range()),
                            "dataclass 类体内只允许字段标注",
                        )
                        .with_code("E0100")
                        .with_primary_label("不允许此类语句"),
                    );
                    continue;
                }
                let value = lower_class_expr(
                    Some(&class_names),
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

    if !is_dataclass && fields.is_empty() {
        // 普通类不要求字段，这里只是让 `fields` 的用途明确：仅 dataclass 使用。
    }

    Some(Class {
        name: class_name,
        base,
        fields,
        declared_fields,
        body,
        frozen,
        is_dataclass,
        is_exception,
        range: class_def.range(),
        decorators,
        decorated_binding,
    })
}

/// 解析 `class X(Base):` 的基类。
fn parse_base(
    class_def: &ast::StmtClassDef,
    file: FileId,
    globals: &GlobalScope,
    classes: &la_arena::Arena<Class>,
    diags: &mut Vec<Diagnostic>,
) -> Option<ClassBase> {
    let Some(args) = &class_def.arguments else {
        return None;
    };
    if !args.keywords.is_empty() {
        diags.push(
            Diagnostic::error(
                Span::new(file, class_def.range()),
                "不支持类关键字参数（元类等属于暂缓能力）",
            )
            .with_code("E0100")
            .with_primary_label("不支持 metaclass / 其它类关键字"),
        );
        return None;
    }
    if args.args.len() > 1 {
        diags.push(
            Diagnostic::error(Span::new(file, class_def.range()), "暂不支持多重继承")
                .with_code("E0100")
                .with_primary_label("只支持一个基类"),
        );
        return None;
    }
    let base_expr = args.args.first()?;
    let ast::Expr::Name(base_name) = base_expr else {
        diags.push(
            Diagnostic::error(
                Span::new(file, base_expr.range()),
                "基类必须是已定义的类名或内建异常类型",
            )
            .with_code("E0100")
            .with_primary_label("不支持任意基类表达式"),
        );
        return None;
    };
    let name = Name::from(base_name.id.as_str());
    if let Some(resolution) = globals.names.get(&name) {
        return match resolution {
            Resolution::Class(id) => {
                // 基类必须在源码里已经定义：类体在 class 语句执行时求值，
                // 前向引用在 CPython 里同样会 NameError。
                let _ = &classes[*id];
                Some(ClassBase::User(*id))
            }
            _ => {
                diags.push(
                    Diagnostic::error(
                        Span::new(file, base_name.range),
                        format!("`{name}` 不是可继承的类"),
                    )
                    .with_code("E0100")
                    .with_primary_label("基类必须是类"),
                );
                None
            }
        };
    }
    // `class X(object)` 与不写基类等价；显式写出来是常见写法，不应被当成未知基类。
    if base_name.id.as_str() == "object" {
        return None;
    }
    match crate::scope::resolve_builtin(base_name.id.as_str()) {
        Some(crate::hir::Builtin::Exception(type_name)) => Some(ClassBase::Builtin(type_name)),
        _ => {
            diags.push(
                Diagnostic::error(
                    Span::new(file, base_name.range),
                    format!("未知基类 `{name}`"),
                )
                .with_code("E0100")
                .with_primary_label("基类必须是用户类或内建异常类"),
            );
            None
        }
    }
}

/// 在模块作用域降级类体里的表达式。
#[allow(clippy::too_many_arguments)]
pub(crate) fn lower_class_expr(
    class_names: Option<&rustc_hash::FxHashSet<Name>>,
    file: FileId,
    globals: &GlobalScope,
    module_vars: &FxHashMap<Name, crate::hir::ModuleVarId>,
    nested_funcs: &FxHashMap<ryupy_base::TextRange, crate::hir::FuncId>,
    nested_defaults: &mut FxHashMap<FuncId, Vec<Option<ExprId>>>,
    body: &mut Body,
    diags: &mut Vec<Diagnostic>,
    expr: &ast::Expr,
) -> ExprId {
    let scope = FunctionScope::default();
    let mut ctx = ExprLowerCtx {
        class_names,
        allow_yield: false,
        file,
        global_scope: globals,
        func_scope: &scope,
        module_vars,
        nested_funcs,
        nested_defaults,
        body,
        diagnostics: diags,
    };
    ctx.lower_expr(expr)
}

fn unsupported_class_statement(
    file: FileId,
    range: ryupy_base::TextRange,
    label: &str,
) -> Diagnostic {
    Diagnostic::error(Span::new(file, range), format!("类体内暂不支持：{label}"))
        .with_code("E0100")
        .with_primary_label("类体目前支持属性赋值、方法与表达式语句")
}

/// 检查是否为合法导入：`from dataclasses import dataclass`。
pub fn is_allowed_dataclass_import(import_from: &ast::StmtImportFrom) -> bool {
    if let Some(module) = &import_from.module {
        if module.as_str() == "dataclasses" {
            return import_from
                .names
                .iter()
                .any(|alias| alias.name.as_str() == "dataclass");
        }
    }
    false
}
