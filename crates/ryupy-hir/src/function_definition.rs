//! 从真实 AST 提取函数的不可变定义来源；与运行时名字绑定及可变类属性分离。
//! 默认值、注解和装饰器仍属外层；函数体加 <locals>，类体只加词法类名。
use la_arena::Arena;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Diagnostic, FileId, Ranged, Span, TextRange};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};

use crate::hir::{Function, FunctionKind, Name};

/// 只描述用户源码定义，不用 FuncId、后端名字或可变属性推测来源。
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct DefinitionInfo {
    pub lexical_qualname: String,
    pub firstlineno: u32,
}

/// 独立只读 AST 扫描不改变现有默认值/注解/装饰器的求值及闭包分析顺序。
pub(crate) fn attach(
    body: &[ast::Stmt],
    lines: &[usize],
    functions: &mut Arena<Function>,
    file: FileId,
    diagnostics: &mut Vec<Diagnostic>,
) {
    let mut collector = Collector {
        lines,
        prefix: String::new(),
        globals: FxHashSet::default(),
        definitions: FxHashMap::default(),
        implicit: FxHashSet::default(),
        collisions: Vec::new(),
    };
    for stmt in body {
        collector.visit_stmt(stmt);
    }
    for (_, function) in functions.iter_mut() {
        if function.kind == FunctionKind::ModuleMain || collector.implicit.contains(&function.range)
        {
            continue;
        }
        function.definition = collector.definitions.remove(&function.range);
        if function.definition.is_none() {
            diagnostics.push(
                Diagnostic::error(
                    Span::new(file, function.range),
                    "内部错误：函数定义缺少匹配的源码来源",
                )
                .with_code("E0101"),
            );
        }
    }
    for range in collector.collisions {
        diagnostics.push(
            Diagnostic::error(Span::new(file, range), "内部错误：函数源码定义范围重复")
                .with_code("E0101"),
        );
    }
}

struct Collector<'a> {
    lines: &'a [usize],
    prefix: String,
    globals: FxHashSet<Name>,
    definitions: FxHashMap<TextRange, DefinitionInfo>,
    implicit: FxHashSet<TextRange>,
    collisions: Vec<TextRange>,
}

impl Collector<'_> {
    fn qualify(&self, name: &str) -> String {
        if self.globals.contains(&Name::from(name)) {
            name.to_owned()
        } else {
            format!("{}{name}", self.prefix)
        }
    }

    fn record(&mut self, range: TextRange, start: TextRange, name: &str) -> String {
        let qualname = self.qualify(name);
        // partition_point 返回从 1 开始的真实源行号，包括首个跨行装饰器表达式。
        let firstlineno = self
            .lines
            .partition_point(|&line| line <= start.start().to_usize());
        if self
            .definitions
            .insert(
                range,
                DefinitionInfo {
                    lexical_qualname: qualname.clone(),
                    firstlineno: firstlineno as u32,
                },
            )
            .is_some()
        {
            self.collisions.push(range);
        }
        qualname
    }

    fn body(&mut self, body: &[ast::Stmt], prefix: String) {
        let previous_prefix = std::mem::replace(&mut self.prefix, prefix);
        let globals = crate::scope::collect_global_names(body)
            .into_iter()
            .collect();
        let previous_globals = std::mem::replace(&mut self.globals, globals);
        for stmt in body {
            self.visit_stmt(stmt);
        }
        self.prefix = previous_prefix;
        self.globals = previous_globals;
    }
}

impl<'a> Visitor<'a> for Collector<'_> {
    fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
        match stmt {
            ast::Stmt::FunctionDef(def) => {
                for decorator in &def.decorator_list {
                    self.visit_expr(&decorator.expression);
                }
                self.visit_parameters(&def.parameters);
                if let Some(returns) = &def.returns {
                    self.visit_expr(returns);
                }
                let start = def
                    .decorator_list
                    .first()
                    .map_or(def.range, |d| d.expression.range());
                let qualname = self.record(def.range, start, def.name.as_str());
                self.body(&def.body, format!("{qualname}.<locals>."));
            }
            ast::Stmt::ClassDef(class) => {
                for decorator in &class.decorator_list {
                    self.visit_expr(&decorator.expression);
                }
                if let Some(arguments) = &class.arguments {
                    self.visit_arguments(arguments);
                }
                self.body(
                    &class.body,
                    format!("{}.", self.qualify(class.name.as_str())),
                );
            }
            _ => walk_stmt(self, stmt),
        }
    }

    fn visit_expr(&mut self, expr: &'a ast::Expr) {
        if let Some(comprehension) = crate::comprehension_ast::Comprehension::from_expr(expr) {
            self.implicit.insert(comprehension.range);
        }
        match expr {
            ast::Expr::Lambda(lambda) => {
                if let Some(parameters) = &lambda.parameters {
                    self.visit_parameters(parameters);
                }
                let qualname = self.record(lambda.range, lambda.range, "<lambda>");
                let prefix = std::mem::replace(&mut self.prefix, format!("{qualname}.<locals>."));
                let globals = std::mem::take(&mut self.globals);
                self.visit_expr(&lambda.body);
                self.prefix = prefix;
                self.globals = globals;
            }
            ast::Expr::Generator(generator) => {
                // 生成器表达式的最左 iterable 属于外层；隐式函数本身不伪造来源。
                // CPython 3.12 的生成器表达式子作用域不插入 <locals>。
                if let Some(first) = generator.generators.first() {
                    self.visit_expr(&first.iter);
                }
                let child_prefix = format!("{}<genexpr>.", self.prefix);
                let prefix = std::mem::replace(&mut self.prefix, child_prefix);
                let globals = std::mem::take(&mut self.globals);
                for (index, item) in generator.generators.iter().enumerate() {
                    // 目标中的属性/下标表达式也属于生成器体，可能含真实lambda定义。
                    self.visit_expr(&item.target);
                    if index > 0 {
                        self.visit_expr(&item.iter);
                    }
                    for condition in &item.ifs {
                        self.visit_expr(condition);
                    }
                }
                self.visit_expr(&generator.elt);
                self.prefix = prefix;
                self.globals = globals;
            }
            // CPython 3.12 内联 list/dict/set 推导式，不给其中 lambda 添加隐式前缀。
            _ => walk_expr(self, expr),
        }
    }
}
