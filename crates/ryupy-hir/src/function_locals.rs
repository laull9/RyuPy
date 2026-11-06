//! 普通函数的源码局部可见性与 CPython 编译访问顺序。
//!
//! 与 HIR arena 分配顺序分离：预扫描名字和去糖临时量不等于 co_varnames。
//! 只保存真实源码读写涉及的 LocalId，MIR 不需要根据名字前缀猜测来源。
use crate::hir::{Body, LocalId, Name, Param, ParamKind};
use crate::lower_fn::FnSource;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};

pub(crate) fn collect(
    source: &FnSource<'_>,
    body: &Body,
    params: &[Param],
) -> Option<Vec<LocalId>> {
    let mut scan = Locals {
        bindings: body
            .locals
            .iter()
            .map(|(id, local)| (local.name.clone(), id))
            .collect(),
        order: Vec::new(),
        seen: FxHashSet::default(),
        supported: true,
    };
    // CPython 的参数槽序是位置、仅关键字、*args、**kwargs，不是源码参数列表序。
    for group in 0..4 {
        for param in params {
            let rank = match param.kind {
                ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword => 0,
                ParamKind::KeywordOnly => 1,
                ParamKind::VarPositional => 2,
                ParamKind::VarKeyword => 3,
            };
            if rank == group {
                scan.seen.insert(param.local);
                scan.order.push(param.local);
            }
        }
    }
    match source {
        FnSource::Def(def) => {
            for stmt in &def.body {
                scan.visit_stmt(stmt);
            }
        }
        FnSource::Lambda(lambda) => scan.visit_expr(&lambda.body),
        FnSource::Comprehension(_) => return None,
    }
    scan.supported.then_some(scan.order)
}

struct Locals {
    bindings: FxHashMap<Name, LocalId>,
    order: Vec<LocalId>,
    // 哈希集合只负责去重，Vec独立保留CPython可见的首次访问顺序。
    seen: FxHashSet<LocalId>,
    supported: bool,
}
impl Locals {
    fn name(&mut self, name: &str) {
        if let Some(&id) = self.bindings.get(&Name::from(name))
            && self.seen.insert(id)
        {
            self.order.push(id);
        }
    }
    fn imports(&mut self, aliases: &[ast::Alias], root: bool) {
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
            self.name(name);
        }
    }
    fn defaults(&mut self, params: &ast::Parameters) {
        for param in params
            .posonlyargs
            .iter()
            .chain(&params.args)
            .chain(&params.kwonlyargs)
        {
            if let Some(value) = &param.default {
                self.visit_expr(value);
            }
        }
    }
}
impl<'a> Visitor<'a> for Locals {
    fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
        match stmt {
            ast::Stmt::Assign(assign) => {
                self.visit_expr(&assign.value);
                for target in &assign.targets {
                    self.visit_expr(target);
                }
            }
            ast::Stmt::AugAssign(assign) => {
                self.visit_expr(&assign.target);
                self.visit_expr(&assign.value);
            }
            ast::Stmt::Try(try_) => {
                // CPython 先编译正常路径的 else，再编译异常处理路径。
                for stmt in try_.body.iter().chain(&try_.orelse) {
                    self.visit_stmt(stmt);
                }
                for handler in &try_.handlers {
                    self.visit_except_handler(handler);
                }
                for stmt in &try_.finalbody {
                    self.visit_stmt(stmt);
                }
            }
            ast::Stmt::AnnAssign(assign) => {
                // 函数局部注解不求值；仅注解无读写的名字不占 CPython 局部槽。
                if let Some(value) = &assign.value {
                    self.visit_expr(value);
                    self.visit_expr(&assign.target);
                } else if !matches!(assign.target.as_ref(), ast::Expr::Name(_)) {
                    self.visit_expr(&assign.target);
                }
            }
            ast::Stmt::For(loop_) => {
                self.visit_expr(&loop_.iter);
                self.visit_expr(&loop_.target);
                for stmt in loop_.body.iter().chain(&loop_.orelse) {
                    self.visit_stmt(stmt);
                }
            }
            ast::Stmt::FunctionDef(def) => {
                for decorator in &def.decorator_list {
                    self.visit_expr(&decorator.expression);
                }
                self.defaults(&def.parameters);
                // 带运行时注解的嵌套定义另有注解求值次序，尚未纳入此窄契约。
                if def.parameters.iter().any(|p| p.annotation().is_some()) || def.returns.is_some()
                {
                    self.supported = false;
                }
                self.name(def.name.as_str());
            }
            ast::Stmt::ClassDef(_) | ast::Stmt::Match(_) => self.supported = false,
            ast::Stmt::Import(import) => self.imports(&import.names, true),
            ast::Stmt::ImportFrom(import) => self.imports(&import.names, false),
            _ => walk_stmt(self, stmt),
        }
    }
    fn visit_expr(&mut self, expr: &'a ast::Expr) {
        if crate::comprehension_ast::Comprehension::from_expr(expr).is_some() {
            // 3.12 内联推导式会影响外层 co_varnames，当前独立函数去糖不能伪装等价。
            self.supported = false;
            return;
        }
        match expr {
            ast::Expr::Name(name) => self.name(name.id.as_str()),
            ast::Expr::Lambda(lambda) => {
                if let Some(params) = &lambda.parameters {
                    self.defaults(params);
                }
            }
            ast::Expr::Named(named) => {
                self.visit_expr(&named.value);
                self.visit_expr(&named.target);
            }
            _ => walk_expr(self, expr),
        }
    }
    fn visit_except_handler(&mut self, handler: &'a ast::ExceptHandler) {
        let ast::ExceptHandler::ExceptHandler(handler) = handler;
        if let Some(type_) = &handler.type_ {
            self.visit_expr(type_);
        }
        if let Some(name) = &handler.name {
            self.name(name.as_str());
        }
        for stmt in &handler.body {
            self.visit_stmt(stmt);
        }
    }
}
