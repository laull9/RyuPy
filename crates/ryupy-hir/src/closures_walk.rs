//! 遍历函数作用域并登记嵌套函数。默认值属于定义位置，函数体属于新作用域。
//!
//! def 与 lambda 共用登记流程；递归时只携带直接父作用域，随后由闭包分析
//! 沿父链解析 free / cell。收集器不进入类体，类方法由模块入口单独处理。

use la_arena::Arena;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::TextRange;
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};

use crate::closures::ScopeData;
use crate::hir::{ClosureInfo, FuncId, Function, FunctionKind, Name};

#[derive(Default)]
pub(crate) struct ScopeCollector {
    assigned: Vec<Name>,
    globals: FxHashSet<Name>,
    nonlocals: Vec<(Name, TextRange)>,
    reads: Vec<(Name, TextRange)>,
    nested: Vec<Nested>,
}

/// 保持发现顺序，避免按函数种类重排后改变环境槽位和诊断顺序。
enum Nested {
    Def(ast::StmtFunctionDef),
    Lambda(ast::ExprLambda),
}

pub(crate) fn collect_module_nested(
    body: &[ast::Stmt],
    parent: usize,
    functions: &mut Arena<Function>,
    scopes: &mut Vec<ScopeData>,
    nested_funcs: &mut FxHashMap<TextRange, FuncId>,
    nested_def_names: &mut FxHashMap<TextRange, Name>,
) {
    let mut collector = ScopeCollector::default();
    for stmt in body {
        match stmt {
            ast::Stmt::FunctionDef(def) => collector.visit_defaults(&def.parameters),
            ast::Stmt::ClassDef(class) => {
                for item in &class.body {
                    if let ast::Stmt::FunctionDef(def) = item {
                        collector.visit_defaults(&def.parameters);
                    }
                }
            }
            _ => collector.visit_stmt(stmt),
        }
    }
    register_discovered(
        collector,
        parent,
        functions,
        scopes,
        nested_funcs,
        nested_def_names,
    );
}

#[allow(clippy::too_many_arguments)]
pub(crate) fn walk_function_scope(
    body: &[ast::Stmt],
    parameters: &ast::Parameters,
    func_id: FuncId,
    range: TextRange,
    parent: usize,
    functions: &mut Arena<Function>,
    scopes: &mut Vec<ScopeData>,
    nested_funcs: &mut FxHashMap<TextRange, FuncId>,
    nested_def_names: &mut FxHashMap<TextRange, Name>,
) {
    let mut collector = ScopeCollector::default();
    for parameter in parameters.iter() {
        collector.push_assigned(Name::from(parameter.name().as_str()));
    }
    // except 的绑定名不以 Expr::Name 出现，复用已有的局部变量扫描补齐。
    for (name, _) in crate::scope::collect_assigned_names(body) {
        collector.push_assigned(name);
    }
    for stmt in body {
        collector.visit_stmt(stmt);
    }
    let scope = push_scope(&mut collector, func_id, range, parent, scopes);
    register_discovered(
        collector,
        scope,
        functions,
        scopes,
        nested_funcs,
        nested_def_names,
    );
}

fn push_scope(
    collector: &mut ScopeCollector,
    func_id: FuncId,
    range: TextRange,
    parent: usize,
    scopes: &mut Vec<ScopeData>,
) -> usize {
    // 声明为 global / nonlocal 的赋值不创建当前函数的局部绑定。
    collector.assigned.retain(|name| {
        !collector.globals.contains(name) && !collector.nonlocals.iter().any(|(n, _)| n == name)
    });
    let index = scopes.len();
    scopes.push(ScopeData {
        func_id,
        is_module: false,
        range,
        parent: Some(parent),
        assigned: std::mem::take(&mut collector.assigned),
        globals: std::mem::take(&mut collector.globals),
        nonlocals: std::mem::take(&mut collector.nonlocals),
        reads: std::mem::take(&mut collector.reads),
        nested: Vec::new(),
        free: Vec::new(),
        cells: Vec::new(),
    });
    index
}

fn register_discovered(
    collector: ScopeCollector,
    parent: usize,
    functions: &mut Arena<Function>,
    scopes: &mut Vec<ScopeData>,
    nested_funcs: &mut FxHashMap<TextRange, FuncId>,
    nested_def_names: &mut FxHashMap<TextRange, Name>,
) {
    for node in collector.nested {
        let (name, range) = match &node {
            Nested::Def(def) => (Name::from(def.name.as_str()), def.range),
            Nested::Lambda(lambda) => (Name::from("<lambda>"), lambda.range),
        };
        scopes[parent].nested.push(range);
        if matches!(node, Nested::Def(_)) {
            nested_def_names.insert(range, name.clone());
        }
        let child = functions.alloc(Function {
            name,
            kind: FunctionKind::Nested,
            params: Vec::new(),
            ret: None,
            body: Default::default(),
            range,
            closure: ClosureInfo::default(),
        });
        nested_funcs.insert(range, child);
        match node {
            Nested::Def(def) => walk_function_scope(
                &def.body,
                &def.parameters,
                child,
                range,
                parent,
                functions,
                scopes,
                nested_funcs,
                nested_def_names,
            ),
            Nested::Lambda(lambda) => {
                let mut collector = ScopeCollector::default();
                if let Some(parameters) = &lambda.parameters {
                    for parameter in parameters.iter() {
                        collector.push_assigned(Name::from(parameter.name().as_str()));
                    }
                }
                collector.visit_expr(&lambda.body);
                let scope = push_scope(&mut collector, child, range, parent, scopes);
                register_discovered(
                    collector,
                    scope,
                    functions,
                    scopes,
                    nested_funcs,
                    nested_def_names,
                );
            }
        }
    }
}

impl ScopeCollector {
    fn push_assigned(&mut self, name: Name) {
        if !self.assigned.contains(&name) {
            self.assigned.push(name);
        }
    }

    /// 默认值在外层求值：读到的变量和内含的 lambda 都归入当前收集器。
    fn visit_defaults(&mut self, parameters: &ast::Parameters) {
        for parameter in parameters.iter_non_variadic_params() {
            if let Some(default) = &parameter.default {
                self.visit_expr(default);
            }
        }
    }
}

impl<'a> Visitor<'a> for ScopeCollector {
    fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
        match stmt {
            ast::Stmt::FunctionDef(def) => {
                self.push_assigned(Name::from(def.name.as_str()));
                self.visit_defaults(&def.parameters);
                self.nested.push(Nested::Def(def.clone()));
            }
            ast::Stmt::ClassDef(class) => self.push_assigned(Name::from(class.name.as_str())),
            ast::Stmt::Global(global) => {
                self.globals
                    .extend(global.names.iter().map(|n| Name::from(n.as_str())));
            }
            ast::Stmt::Nonlocal(nonlocal) => {
                self.nonlocals.extend(
                    nonlocal
                        .names
                        .iter()
                        .map(|n| (Name::from(n.as_str()), n.range)),
                );
            }
            // 增量赋值的 Name 带 Store 上下文，但旧值仍然会被读取。
            ast::Stmt::AugAssign(assign) => {
                if let ast::Expr::Name(name) = assign.target.as_ref() {
                    self.reads.push((Name::from(name.id.as_str()), name.range));
                }
                walk_stmt(self, stmt);
            }
            _ => walk_stmt(self, stmt),
        }
    }

    fn visit_expr(&mut self, expr: &'a ast::Expr) {
        match expr {
            ast::Expr::Name(name) => match name.ctx {
                ast::ExprContext::Load => {
                    self.reads.push((Name::from(name.id.as_str()), name.range))
                }
                ast::ExprContext::Store | ast::ExprContext::Del => {
                    self.push_assigned(Name::from(name.id.as_str()))
                }
                ast::ExprContext::Invalid => {}
            },
            ast::Expr::Lambda(lambda) => {
                if let Some(parameters) = &lambda.parameters {
                    self.visit_defaults(parameters);
                }
                self.nested.push(Nested::Lambda(lambda.clone()));
            }
            _ => walk_expr(self, expr),
        }
    }
}
