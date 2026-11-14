//! 新定义位置类的静态模板登记。ClassId只标识源码模板，owner在闭包发现后解析成真实FuncId。
//! 这里不降级表达式，也不开放语法；运行时namespace与类身份不得存在这张表中。
use crate::hir::{Class, ClassId, FuncId, Function, Name};
use la_arena::Arena;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::TextRange;
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_stmt};

#[derive(Debug)]
pub struct LexicalClassDefinition {
    pub qualname: String,
    pub owner_range: TextRange,
    pub owner: Option<FuncId>,
    pub target: Option<crate::lexical_class_lower::LexicalBinding>,
}

pub(crate) fn register(
    body: &[ast::Stmt],
    module_range: TextRange,
    classes: &mut Arena<Class>,
) -> FxHashMap<TextRange, ClassId> {
    let mut scan = Registry {
        classes,
        found: FxHashMap::default(),
        prefix: String::new(),
        globals: FxHashSet::default(),
        owner: module_range,
        lexical_depth: 0,
    };
    for statement in body {
        scan.visit_stmt(statement);
    }
    scan.found
}

pub(crate) fn resolve_owners(classes: &mut Arena<Class>, functions: &Arena<Function>) {
    for (_, class) in classes.iter_mut() {
        if let Some(definition) = &mut class.lexical {
            definition.owner = functions.iter().find_map(|(id, function)| {
                (function.range == definition.owner_range).then_some(id)
            });
        }
    }
}

struct Registry<'a> {
    classes: &'a mut Arena<Class>,
    found: FxHashMap<TextRange, ClassId>,
    prefix: String,
    globals: FxHashSet<Name>,
    owner: TextRange,
    lexical_depth: usize,
}
impl Registry<'_> {
    fn qualified(&self, name: &str) -> String {
        if self.prefix.is_empty() || self.globals.contains(&Name::from(name)) {
            name.to_owned()
        } else {
            format!("{}.{name}", self.prefix)
        }
    }
}
impl<'a> Visitor<'a> for Registry<'_> {
    fn visit_stmt(&mut self, statement: &'a ast::Stmt) {
        match statement {
            ast::Stmt::FunctionDef(function) => {
                // 头表达式属于外层，但它们不能含class语句，不需要额外发现。
                let prefix = self.prefix.clone();
                let owner = self.owner;
                self.prefix = format!("{}.<locals>", self.qualified(function.name.as_str()));
                self.owner = function.range;
                let globals = std::mem::replace(
                    &mut self.globals,
                    crate::scope::collect_global_names(&function.body)
                        .into_iter()
                        .collect(),
                );
                self.lexical_depth += 1;
                for item in &function.body {
                    self.visit_stmt(item);
                }
                self.lexical_depth -= 1;
                self.globals = globals;
                self.owner = owner;
                self.prefix = prefix;
            }
            ast::Stmt::ClassDef(class) => {
                let qualname = self.qualified(class.name.as_str());
                if self.lexical_depth > 0 {
                    let id = self.classes.alloc(Class {
                        lexical: Some(LexicalClassDefinition {
                            qualname: qualname.clone(),
                            owner_range: self.owner,
                            owner: None,
                            target: None,
                        }),
                        has_class_cell: false,
                        name: Name::from(class.name.as_str()),
                        base: None,
                        ordinary_mi_bases: None,
                        deferred_runtime_base: false,
                        fields: Vec::new(),
                        declared_fields: Vec::new(),
                        enum_members: None,
                        body: Vec::new(),
                        has_annotations: false,
                        frozen: false,
                        is_dataclass: false,
                        runtime_dataclass: false,
                        is_exception: false,
                        range: class.range,
                        decorators: Vec::new(),
                        decorated_binding: None,
                    });
                    self.found.insert(class.range, id);
                }
                let prefix = std::mem::replace(&mut self.prefix, qualname);
                let globals = std::mem::take(&mut self.globals);
                self.lexical_depth += 1;
                for item in &class.body {
                    self.visit_stmt(item);
                }
                self.lexical_depth -= 1;
                self.globals = globals;
                self.prefix = prefix;
            }
            _ => walk_stmt(self, statement),
        }
    }
}

/// 保留旧类登记顺序，在函数发现后才把词法owner范围解析为实际函数身份。
pub(crate) fn analyze(
    parsed: &ryupy_syntax::ParsedModule,
    functions: &mut Arena<Function>,
    classes: &mut Arena<Class>,
    main: FuncId,
    instance_classes: Option<&FxHashMap<TextRange, ClassId>>,
    lines: &[usize],
    diagnostics: &mut Vec<ryupy_base::Diagnostic>,
) -> crate::closures::ClosureAnalysis {
    let lexical = register(&parsed.module.body, parsed.module.range, classes);
    let analysis = crate::closures::analyze_closures(
        parsed,
        functions,
        main,
        instance_classes,
        &lexical,
        diagnostics,
    );
    resolve_owners(classes, functions);
    for (id, class) in classes.iter_mut() {
        if class.lexical.is_some() {
            class.has_class_cell = analysis
                .infos
                .values()
                .any(|info| info.class_cell == Some(id));
        }
    }
    crate::function_definition::attach(
        &parsed.module.body,
        lines,
        functions,
        parsed.file,
        diagnostics,
    );
    analysis
}
