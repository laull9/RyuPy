//! 模块级变量与 `import` 语句的 HIR 降级（M2.7）。
//!
//! 两件事放在这里：
//! 1. **收集模块级变量**：顶层赋值、`global` 声明、导入绑定与 `__name__` 等特殊名字；
//! 2. **降级导入语句**：`import a.b` / `from .x import y` 展开成带绝对模块名的
//!    [`StmtKind::Import`] / [`StmtKind::ImportFrom`]，函数体内的导入明确报错。
//!
//! 相对导入的绝对化只依赖 driver 给的 `__package__`，不读文件。

use la_arena::Arena;
use rustc_hash::FxHashMap;
use ryupy_base::{Diagnostic, Ranged, Span};
use ryupy_syntax::ParsedModule;
use ryupy_syntax::ast;

use crate::hir::{
    AssignTarget, Binding, Expr, ExprKind, Literal, ModuleInfo, ModuleVar, ModuleVarId, Name, Stmt,
    StmtId, StmtKind, TypeRef,
};
use crate::imports;
use crate::lower::StmtLowerCtx;
use crate::scope::collect_assigned_names;

/// 收集模块级变量；同名只登记一次，`global` 声明的名字标记为动态。
pub(crate) fn collect_module_vars(
    parsed: &ParsedModule,
    info: &ModuleInfo,
) -> (Arena<ModuleVar>, FxHashMap<Name, ModuleVarId>) {
    // ── 模块级变量（M2.7）────────────────────────────────────────────
    //
    // 顶层赋值、`global` 声明的名字、导入绑定以及 `__name__` / `__package__` /
    // `__file__` 都占一个模块槽位。先统一收集，后续名字解析只需要一张表。
    let mut vars: Arena<ModuleVar> = Arena::default();
    let mut module_var_ids: FxHashMap<Name, ModuleVarId> = FxHashMap::default();
    let mut ensure_var =
        |name: &Name, range: ryupy_base::TextRange, annotation: Option<TypeRef>| {
            *module_var_ids.entry(name.clone()).or_insert_with(|| {
                vars.alloc(ModuleVar {
                    name: name.clone(),
                    range,
                    annotation,
                    is_global: false,
                })
            })
        };
    // 特殊名字：值在模块入口开头写入（见下面的 main 前言）。
    for special in ["__name__", "__package__", "__file__"] {
        ensure_var(
            &Name::from(special),
            parsed.module.range,
            Some(TypeRef::Str),
        );
    }
    // 顶层赋值（含 for 目标、except as 等）。
    for (name, range) in collect_assigned_names(&parsed.module.body) {
        ensure_var(&name, range, None);
    }
    // 模块语句块里的 def 绑定到模块；直接顶层 def 继续使用已有函数槽位。
    for stmt in &parsed.module.body {
        if !matches!(stmt, ast::Stmt::FunctionDef(_) | ast::Stmt::ClassDef(_)) {
            for name in crate::closures_find::collect_def_names(std::slice::from_ref(stmt)) {
                ensure_var(&name, parsed.module.range, None);
            }
        }
    }
    // global 可以出现在任意深度的嵌套函数。这里遍历全部函数体，统一登记模块槽位。
    use ryupy_syntax::ast::visitor::{Visitor, walk_stmt};
    #[derive(Default)]
    struct Globals(Vec<Name>);
    impl<'a> Visitor<'a> for Globals {
        fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
            if let ast::Stmt::Global(global) = stmt {
                self.0
                    .extend(global.names.iter().map(|n| Name::from(n.as_str())));
            } else {
                walk_stmt(self, stmt);
            }
        }
    }
    let mut collector = Globals::default();
    for stmt in &parsed.module.body {
        collector.visit_stmt(stmt);
    }
    let global_names = collector.0;
    for name in &global_names {
        ensure_var(name, parsed.module.range, None);
    }
    // 模块级导入绑定的名字。
    for request in imports::collect_imports(&parsed.module.body, &info.package) {
        let name = Name::from(request.bind.as_str());
        ensure_var(&name, parsed.module.range, None);
    }
    // 标记 `global` 声明的变量：它们的读写走动态路径。
    for name in global_names {
        if let Some(&var) = module_var_ids.get(&name) {
            vars[var].is_global = true;
        }
    }

    (vars, module_var_ids)
}

impl StmtLowerCtx<'_> {
    /// 一条 import 可能绑定多个名字，因此可能展开成多条 HIR 语句。
    pub(crate) fn lower_import_to_root(&mut self, stmt: &ast::Stmt) {
        let mut out = Vec::new();
        self.lower_import_into(stmt, &mut out);
        self.body.root.extend(out);
    }

    pub(crate) fn lower_import_into(&mut self, stmt: &ast::Stmt, out: &mut Vec<StmtId>) {
        if !self.module_scope {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, stmt.range()),
                    "函数体内暂不支持 import",
                )
                .with_code("E0100")
                .with_primary_label("M2.7 只支持模块顶层的导入"),
            );
            return;
        }
        let line = self.line_of(stmt.range().start().to_usize());
        match stmt {
            ast::Stmt::Import(import) => {
                for alias in &import.names {
                    let module = alias.name.as_str().to_owned();
                    let (bind_name, bind_root) = match &alias.asname {
                        Some(name) => (name.as_str().to_owned(), false),
                        None => (
                            module.split('.').next().unwrap_or_default().to_owned(),
                            module.contains('.'),
                        ),
                    };
                    let Some(&bind) = self.module_vars.get(&Name::from(bind_name.as_str())) else {
                        continue;
                    };
                    let id = self.body.stmts.alloc(Stmt {
                        kind: StmtKind::Import {
                            module,
                            bind,
                            bind_root,
                        },
                        range: alias.range,
                        line,
                    });
                    out.push(id);
                }
            }
            ast::Stmt::ImportFrom(from) => {
                // `dataclasses` 由编译期特殊处理（class lowering 识别装饰器），
                // 不是真正的运行时模块，不生成导入语句。
                if from.level == 0 && from.module.as_deref() == Some("dataclasses") {
                    return;
                }
                let level = from.level;
                let Some(module) = imports::absolute_module(
                    from.module.as_ref().map(|m| m.as_str()),
                    level,
                    self.package,
                ) else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, stmt.range()),
                            "无法解析相对导入：超出顶层包",
                        )
                        .with_code("E0100")
                        .with_primary_label("相对导入层级过深"),
                    );
                    return;
                };
                let mut names = Vec::new();
                for alias in &from.names {
                    let name = alias.name.as_str();
                    if name == "*" {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, alias.range),
                                "暂不支持 `from ... import *`",
                            )
                            .with_code("E0100")
                            .with_primary_label("请显式列出要导入的名字"),
                        );
                        continue;
                    }
                    let bind_name = alias
                        .asname
                        .as_ref()
                        .map(|a| a.as_str().to_owned())
                        .unwrap_or_else(|| name.to_owned());
                    let Some(&bind) = self.module_vars.get(&Name::from(bind_name.as_str())) else {
                        continue;
                    };
                    names.push((name.to_owned(), bind));
                }
                if names.is_empty() {
                    return;
                }
                let id = self.body.stmts.alloc(Stmt {
                    kind: StmtKind::ImportFrom { module, names },
                    range: stmt.range(),
                    line,
                });
                out.push(id);
            }
            _ => {}
        }
    }

    /// 写入 `__name__` / `__package__` / `__file__`。
    pub(crate) fn emit_special_module_vars(
        &mut self,
        info: &ModuleInfo,
        range: ryupy_base::TextRange,
    ) {
        let specials = [
            ("__name__", info.name.as_str()),
            ("__package__", info.package.as_str()),
            ("__file__", info.file.as_str()),
        ];
        for (name, value) in specials {
            let name = Name::from(name);
            let Some(&var) = self.module_vars.get(&name) else {
                continue;
            };
            let value = self.body.exprs.alloc(Expr {
                kind: ExprKind::Literal(Literal::Str(value.into())),
                range,
            });
            let id = self.body.stmts.alloc(Stmt {
                kind: StmtKind::Assign {
                    target: AssignTarget::ModuleVar(var),
                    value,
                },
                range,
                line: 1,
            });
            self.body.root.push(id);
        }
    }

    /// 名字在当前位置绑定到哪里：`global` 声明与模块顶层都是模块变量；
    /// `nonlocal` 声明 / 捕获的外层 cell 是 Free（M3.0）。
    pub(crate) fn resolve_binding(&self, name: &Name) -> Option<Binding> {
        if !self.func_scope.is_global(name) {
            if let Some(&idx) = self.func_scope.free.get(name) {
                return Some(Binding::Free(idx));
            }
            if let Some(&local) = self.func_scope.locals.get(name) {
                return Some(Binding::Local(local));
            }
        }
        self.module_vars.get(name).copied().map(Binding::ModuleVar)
    }
}
