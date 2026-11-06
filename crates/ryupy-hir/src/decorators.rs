//! 装饰器定义位置降级：保留源码顺序与表达式所属作用域，不在HIR执行应用。
//! 原函数/类的创建及逆序应用由MIR负责，最终绑定只在整条装饰链成功后写入。

use crate::hir::{Body, Decorator, FuncId, ModuleVarId, Name};
use crate::scope::GlobalScope;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Diagnostic, FileId, Ranged, Span, TextRange};
use ryupy_syntax::ast;

/// 只把定义位置确实由 dataclasses 导入的名字认作专用标记。
/// 按模块语句顺序追踪，用户同名 def/赋值不会被语法拼写误认。
pub(crate) fn dataclass_markers(stmts: &[ast::Stmt]) -> FxHashSet<TextRange> {
    let mut names = FxHashSet::<Name>::default();
    let mut markers = FxHashSet::default();
    for stmt in stmts {
        // 控制流不产生新作用域：稳定的外层导入标记也适用于其中的类。
        // 若该语句块可能重绑定标记，保守取消，不能擅自假定分支执行结果。
        if !matches!(
            stmt,
            ast::Stmt::ClassDef(_) | ast::Stmt::FunctionDef(_) | ast::Stmt::ImportFrom(_)
        ) {
            for (name, _) in crate::scope::collect_assigned_names(std::slice::from_ref(stmt)) {
                names.remove(&name);
            }
        }
        for definition in crate::module_definitions::definitions(std::slice::from_ref(stmt)) {
            let ast::Stmt::ClassDef(class) = definition else {
                continue;
            };
            for decorator in &class.decorator_list {
                let expression = &decorator.expression;
                let callee = match expression {
                    ast::Expr::Call(call) => call.func.as_ref(),
                    other => other,
                };
                if matches!(callee, ast::Expr::Name(name) if names.contains(&Name::from(name.id.as_str())))
                {
                    markers.insert(expression.range());
                }
            }
        }
        for (name, _) in crate::scope::collect_assigned_names(std::slice::from_ref(stmt)) {
            names.remove(&name);
        }
        match stmt {
            ast::Stmt::FunctionDef(def) => {
                names.remove(&Name::from(def.name.as_str()));
            }
            ast::Stmt::ClassDef(class) => {
                names.remove(&Name::from(class.name.as_str()));
            }
            ast::Stmt::ImportFrom(from) => {
                for alias in &from.names {
                    let bind = alias
                        .asname
                        .as_ref()
                        .map_or(alias.name.as_str(), |name| name.as_str());
                    names.remove(&Name::from(bind));
                    if from.level == 0
                        && from.module.as_deref() == Some("dataclasses")
                        && alias.name.as_str() == "dataclass"
                    {
                        names.insert(Name::from(bind));
                    }
                }
            }
            ast::Stmt::Import(import) => {
                for alias in &import.names {
                    let bind = alias.asname.as_ref().map_or_else(
                        || alias.name.as_str().split('.').next().unwrap_or_default(),
                        |name| name.as_str(),
                    );
                    names.remove(&Name::from(bind));
                }
            }
            _ => {}
        }
    }
    markers
}

/// dataclasses 尚无一等运行时工厂；可能被回调改写的标记不能静态替代。
pub(crate) fn validate_mutable_markers(
    stmts: &[ast::Stmt],
    markers: &FxHashSet<TextRange>,
    file: FileId,
    diagnostics: &mut Vec<Diagnostic>,
) {
    use ryupy_syntax::ast::visitor::{Visitor, walk_stmt};
    struct Writes(FxHashSet<Name>);
    impl<'a> Visitor<'a> for Writes {
        fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
            if let ast::Stmt::FunctionDef(function) = stmt {
                let globals = crate::scope::collect_global_names(&function.body);
                for (name, _) in crate::scope::collect_assigned_names(&function.body) {
                    if globals.contains(&name) {
                        self.0.insert(name);
                    }
                }
            }
            walk_stmt(self, stmt);
        }
    }
    let mut writes = Writes(FxHashSet::default());
    for stmt in stmts {
        writes.visit_stmt(stmt);
    }
    for definition in crate::module_definitions::definitions(stmts) {
        let ast::Stmt::ClassDef(class) = definition else {
            continue;
        };
        for decorator in &class.decorator_list {
            let expression = &decorator.expression;
            if !markers.contains(&expression.range()) {
                continue;
            }
            let callee = match expression {
                ast::Expr::Call(call) => call.func.as_ref(),
                other => other,
            };
            if matches!(callee, ast::Expr::Name(name) if writes.0.contains(&Name::from(name.id.as_str())))
            {
                diagnostics.push(
                    Diagnostic::error(
                        Span::new(file, expression.range()),
                        "dataclass 导入别名存在 global 写入，暂不支持作为静态装饰器",
                    )
                    .with_code("E0100"),
                );
            }
        }
    }
}

pub(crate) fn has_runtime_class_decorators(
    class: &ast::StmtClassDef,
    _markers: &FxHashSet<TextRange>,
) -> bool {
    // 数据类装饰同样可能抛错，公开绑定必须等转换成功后才写入。
    !class.decorator_list.is_empty()
}

/// dataclass仍走专用字段转换，但在混合链中保留其准确位置。
pub(crate) fn dataclass_flags(
    class: &ast::StmtClassDef,
    markers: &FxHashSet<TextRange>,
    file: FileId,
    diagnostics: &mut Vec<Diagnostic>,
) -> (bool, bool) {
    let mut present = false;
    let mut frozen = false;
    for decorator in &class.decorator_list {
        if !markers.contains(&decorator.expression.range()) {
            continue;
        }
        if present {
            diagnostics.push(
                Diagnostic::error(
                    Span::new(file, decorator.expression.range()),
                    "暂不支持同一类上的多个 dataclass 装饰器",
                )
                .with_code("E0100"),
            );
        }
        present = true;
        if let ast::Expr::Call(call) = &decorator.expression {
            let supported = call.arguments.args.is_empty()
                && call.arguments.keywords.iter().all(|keyword| {
                    keyword
                        .arg
                        .as_ref()
                        .is_some_and(|name| name.as_str() == "frozen")
                        && matches!(keyword.value, ast::Expr::BooleanLiteral(_))
                });
            if !supported {
                diagnostics.push(
                    Diagnostic::error(
                        Span::new(file, call.range()),
                        "dataclass 当前仅支持无参数或 frozen=True/False 字面量",
                    )
                    .with_code("E0100"),
                );
            }
            for keyword in &call.arguments.keywords {
                if keyword
                    .arg
                    .as_ref()
                    .is_some_and(|name| name.as_str() == "frozen")
                    && let ast::Expr::BooleanLiteral(value) = &keyword.value
                {
                    frozen = value.value;
                }
            }
        }
    }
    (present, frozen)
}

#[allow(clippy::too_many_arguments)]
pub(crate) fn lower_list(
    decorators: &[ast::Decorator],
    class: bool,
    class_names: Option<&crate::class_scope::ClassScope>,
    file: FileId,
    globals: &GlobalScope,
    module_vars: &FxHashMap<Name, ModuleVarId>,
    nested_funcs: &FxHashMap<ryupy_base::TextRange, FuncId>,
    nested_defaults: &mut crate::definition_metadata::DefinitionMetadata,
    body: &mut Body,
    diagnostics: &mut Vec<Diagnostic>,
) -> Vec<Decorator> {
    decorators
        .iter()
        .map(|decorator| {
            let expression = &decorator.expression;
            if class && globals.dataclass_markers.contains(&expression.range()) {
                return Decorator::Dataclass;
            }
            // 仅真正的内建名字走既有描述符快路径；同名用户绑定必须正常求值。
            if !globals.instance_globals
                && !class
                && let ast::Expr::Name(name) = expression
            {
                let name = Name::from(name.id.as_str());
                if !globals.names.contains_key(&name)
                    && !module_vars.contains_key(&name)
                    && !class_names.is_some_and(|names| names.contains(&name))
                {
                    match name.as_str() {
                        "property" => return Decorator::Property,
                        "staticmethod" => return Decorator::StaticMethod,
                        "classmethod" => return Decorator::ClassMethod,
                        _ => {}
                    }
                }
            }
            Decorator::Expr(crate::class::lower_class_expr(
                class_names,
                file,
                globals,
                module_vars,
                nested_funcs,
                nested_defaults,
                body,
                diagnostics,
                expression,
            ))
        })
        .collect()
}

#[cfg(test)]
mod tests {
    use super::*;

    fn lower(source: &str) -> (crate::Module, Vec<Diagnostic>) {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("decorators.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        crate::lower_module(
            &parsed,
            &crate::ModuleInfo {
                instance_globals: false,
                resolve_file_at_runtime: false,
                name: "__main__".into(),
                package: String::new(),
                file: "decorators.py".into(),
            },
            source,
        )
    }

    #[test]
    fn marker_tracks_import_alias_and_definition_position() {
        let (module, diagnostics) = lower(
            "from dataclasses import dataclass as record\n@record\nclass First:\n    x: int\ndef record(value):\n    return value\n@record\nclass Second:\n    pass\n",
        );
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let classes: Vec<_> = module.classes.iter().map(|(_, class)| class).collect();
        assert!(classes[0].is_dataclass);
        assert!(classes[0].decorated_binding.is_some());
        assert!(!classes[1].is_dataclass);
        assert!(classes[1].decorated_binding.is_some());
    }

    #[test]
    fn ordinary_class_annotation_metadata_preserves_first_declaration_order() {
        let (module, diagnostics) = lower(
            "class Replacement:
    z: int
    a: str
    z: float
",
        );
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let class = module.classes.iter().next().unwrap().1;
        assert_eq!(
            class.declared_fields,
            vec![Name::from("z"), Name::from("a")]
        );
        assert!(class.fields.is_empty(), "普通类标注不能误建静态字段布局");
    }

    #[test]
    fn user_dataclass_name_is_an_ordinary_decorator() {
        let (module, diagnostics) =
            lower("def dataclass(value):\n    return value\n@dataclass\nclass Value:\n    pass\n");
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let class = module.classes.iter().next().unwrap().1;
        assert!(!class.is_dataclass);
        assert!(matches!(class.decorators.as_slice(), [Decorator::Expr(_)]));
    }

    #[test]
    fn callback_mutating_imported_marker_is_rejected() {
        let (_, diagnostics) = lower(
            "from dataclasses import dataclass as record
def swap():
    global record
    record = lambda value: value
    return lambda value: value
@swap()
@record
class Value:
    x: int
",
        );
        assert!(
            diagnostics
                .iter()
                .any(|diagnostic| diagnostic.code == Some("E0100")
                    && diagnostic.message.contains("global"))
        );
    }

    #[test]
    fn unsupported_dataclass_options_are_diagnostics_not_ignored() {
        for decorators in [
            "@dataclass(order=True)",
            "@dataclass(frozen=bool(1))",
            "@dataclass(1)",
            "@dataclass\n@dataclass(frozen=True)",
        ] {
            let source = format!(
                "from dataclasses import dataclass\n{decorators}\nclass Value:\n    x: int\n"
            );
            let (_, diagnostics) = lower(&source);
            assert!(
                diagnostics
                    .iter()
                    .any(|diagnostic| diagnostic.code == Some("E0100")),
                "{source}: {diagnostics:?}"
            );
        }
    }
}
