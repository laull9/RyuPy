//! 类定义 AST → HIR 降级。
//!
//! 支持两类类：
//!
//! 1. **`@dataclass`**：字段声明与类体保留顺序，运行时装饰生成方法；
//! 2. **普通 class**（M2.5 / M2.6）：类体按源码顺序降级成 [`ClassItem`]：
//!    - `name = value` 写进类命名空间；
//!    - `def` 变成方法函数（`self` 是普通参数）；
//!    - `@property` / `@staticmethod` / `@classmethod` 保留为修饰器标记。
//!
//! 单继承接受之前定义的普通类、已验证原生类，以及Name/Attribute/最外层Subscript动态基类。多重继承、
//! 元类与 `__getattribute__` 等暂缓能力都在这里明确报错，不静默降级。
//!
//! 类体属性的求值位置属于**模块入口函数**（Python 在 `class` 语句执行时求值），
//! 因此属性表达式直接降级到 `main_body`，MIR 再把它们按源码顺序接到类定义处。

use rustc_hash::FxHashMap;
use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;

use crate::expr::ExprLowerCtx;
use crate::hir::{Body, Class, ClassBase, ClassItem, ClassItemKind, ExprId, Field, FuncId, Name};
use crate::scope::{FunctionScope, GlobalScope};

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
    nested_defaults: &mut crate::definition_metadata::DefinitionMetadata,
    class_scopes: &mut FxHashMap<FuncId, crate::class_scope::ClassScope>,
    main_body: &mut Body,
    diags: &mut Vec<Diagnostic>,
) -> Option<Class> {
    let class_name = Name::from(class_def.name.as_str());
    if crate::native_bases::unsupported_custom_exception_base(class_def, globals) {
        diags.push(unsupported_class_statement(
            file,
            class_def.range(),
            "原生自定义初始化异常的用户派生尚未支持",
        ));
        return None;
    }

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

    // 原生基类仅对白名单身份放行，仍保留表达式求值；其它基类继续原有诊断。
    let native = crate::native_bases::resolve(class_def, globals);
    let parsed_base = if globals.instance_globals {
        if is_dataclass {
            diags.push(unsupported_class_statement(
                file,
                class_def.range(),
                "实例dataclass",
            ));
        }
        match &class_def.arguments {
            Some(args) if !args.keywords.is_empty() || args.args.len() > 1 => {
                diags.push(unsupported_class_statement(
                    file,
                    class_def.range(),
                    "实例类关键字与多重继承",
                ));
                None
            }
            Some(args) => args.args.first().map(|expr| {
                ClassBase::Runtime(lower_class_expr(
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
            }),
            None => None,
        }
    } else if let Some(expr) = crate::typed_dict::candidate(class_def, globals) {
        let total = crate::typed_dict::validate(class_def, is_dataclass, file, diags);
        Some(ClassBase::TypedDict {
            total,
            value: lower_class_expr(
                None,
                file,
                globals,
                module_vars,
                nested_funcs,
                nested_defaults,
                main_body,
                diags,
                expr,
            ),
        })
    } else if let Some((kind, expr)) = native {
        Some(ClassBase::Native {
            kind,
            value: lower_class_expr(
                None,
                file,
                globals,
                module_vars,
                nested_funcs,
                nested_defaults,
                main_body,
                diags,
                expr,
            ),
        })
    } else if let Some(name) = crate::native_bases::io_exception(class_def, globals) {
        Some(ClassBase::Builtin(name))
    } else if let Some(expr) = crate::runtime_bases::candidate(class_def, globals, module_vars) {
        Some(ClassBase::Runtime(lower_class_expr(
            None,
            file,
            globals,
            module_vars,
            nested_funcs,
            nested_defaults,
            main_body,
            diags,
            expr,
        )))
    } else {
        crate::class_bases::parse_base(class_def, file, globals, classes, diags)
    };
    let base = match parsed_base {
        Some(ClassBase::User(id)) if classes[id].decorated_binding.is_some() => {
            if classes[id].is_exception
                || classes[id].is_dataclass
                || classes[id].enum_members.is_some()
            {
                diags.push(
                    Diagnostic::error(
                        Span::new(file, class_def.range),
                        "暂不支持以装饰后的异常类、dataclass或已定义Enum作为基类",
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
                        let value = lower_class_expr(
                            None,
                            file,
                            globals,
                            module_vars,
                            nested_funcs,
                            nested_defaults,
                            main_body,
                            diags,
                            expr,
                        );
                        if matches!(classes[id].base, Some(ClassBase::Runtime(_))) {
                            ClassBase::Runtime(value)
                        } else {
                            ClassBase::Expr(value)
                        }
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

    let enum_members = matches!(
        base,
        Some(ClassBase::Native {
            kind: crate::hir::NativeBase::Enum | crate::hir::NativeBase::Flag,
            ..
        })
    )
    .then(|| {
        let flag = matches!(
            base,
            Some(ClassBase::Native {
                kind: crate::hir::NativeBase::Flag,
                ..
            })
        );
        crate::enum_class::members(class_def, flag, file, diags)
    });
    if enum_members.is_some() && is_dataclass {
        diags.push(
            Diagnostic::error(
                Span::new(file, class_def.range()),
                "暂不支持dataclass与Enum组合，不静默套用普通数据类转换",
            )
            .with_code("E0100"),
        );
    }
    let is_exception = match base {
        Some(ClassBase::Builtin(_)) => true,
        Some(ClassBase::User(base_id)) => classes[base_id].is_exception,
        None
        | Some(
            ClassBase::Expr(_)
            | ClassBase::Runtime(_)
            | ClassBase::Native { .. }
            | ClassBase::TypedDict { .. },
        ) => false,
    };

    let mut class_names = crate::class_scope::ClassScope::default();
    // 仅实际MakeClass路径由runtime冷初始化；Enum/TypedDict仍用各自协议。
    let runtime_namespace =
        enum_members.is_none() && !matches!(base, Some(ClassBase::TypedDict { .. }));
    if runtime_namespace {
        class_names.insert(Name::from("__module__"));
        class_names.insert(Name::from("__qualname__"));
    }
    if crate::class_body::has_annotations(&class_def.body) {
        class_names.insert(Name::from("__annotations__"));
    }
    let mut fields: Vec<Field> = Vec::new();
    let mut declared_fields = Vec::new();
    let method_map: FxHashMap<ryupy_base::TextRange, &MethodSlot> = methods
        .iter()
        .map(|method| (method.range, method))
        .collect();

    // docstring资格只看外层首语句；递归if及后续字符串不能获得该资格。
    let mut body = Vec::new();
    let mut statements = class_def.body.as_slice();
    if runtime_namespace
        && let Some(ast::Stmt::Expr(first)) = statements.first()
        && matches!(&*first.value, ast::Expr::StringLiteral(_))
    {
        let value = lower_class_expr(
            Some(&class_names),
            file,
            globals,
            module_vars,
            nested_funcs,
            nested_defaults,
            main_body,
            diags,
            &first.value,
        );
        let name = Name::from("__doc__");
        class_names.insert(name.clone());
        body.push(ClassItem {
            kind: ClassItemKind::Attr { name, value },
            range: first.range(),
        });
        statements = &statements[1..];
    }
    body.extend(crate::class_body::lower_items(
        statements,
        &mut class_names,
        file,
        globals,
        base,
        is_dataclass,
        is_dataclass || enum_members.is_some() || matches!(base, Some(ClassBase::TypedDict { .. })),
        &method_map,
        module_vars,
        nested_funcs,
        nested_defaults,
        class_scopes,
        main_body,
        diags,
        &mut fields,
        &mut declared_fields,
    ));

    if !is_dataclass && fields.is_empty() {
        // 普通类不要求字段，这里只是让 `fields` 的用途明确：仅 dataclass 使用。
    }

    Some(Class {
        has_class_cell: false,
        name: class_name,
        base,
        fields,
        declared_fields,
        enum_members,
        body,
        has_annotations: crate::class_body::has_annotations(&class_def.body),
        frozen,
        is_dataclass,
        runtime_dataclass: is_dataclass,
        is_exception,
        range: class_def.range(),
        decorators,
        decorated_binding,
    })
}

/// 在模块作用域降级类体里的表达式。
#[allow(clippy::too_many_arguments)]
pub(crate) fn lower_class_expr(
    class_names: Option<&crate::class_scope::ClassScope>,
    file: FileId,
    globals: &GlobalScope,
    module_vars: &FxHashMap<Name, crate::hir::ModuleVarId>,
    nested_funcs: &FxHashMap<ryupy_base::TextRange, crate::hir::FuncId>,
    nested_defaults: &mut crate::definition_metadata::DefinitionMetadata,
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

pub(crate) fn unsupported_class_statement(
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
