//! 模块级变量与 `import` 语句的 HIR 降级（M2.7）。
//!
//! 两件事放在这里：
//! 1. **收集模块级变量**：顶层赋值、`global` 声明、导入绑定与 `__name__` 等特殊名字；
//! 2. **降级导入语句**：`import a.b` / `from .x import y` 展开成带绝对模块名的
//!    [`StmtKind::Import`] / [`StmtKind::ImportFrom`]；导入语句本身见 imports_lower。
//!
//! 相对导入的绝对化只依赖 driver 给的 `__package__`，不读文件。

use la_arena::Arena;
use rustc_hash::FxHashMap;
use ryupy_base::Ranged;
use ryupy_syntax::ParsedModule;
use ryupy_syntax::ast;

use crate::hir::{
    AssignTarget, Binding, Expr, ExprKind, Literal, ModuleInfo, ModuleVar, ModuleVarId, Name, Stmt,
    StmtKind, TypeRef,
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
    if crate::future_imports::annotations(&parsed.module.body)
        && crate::annotation_stmts::has_module_annotations(&parsed.module.body)
    {
        ensure_var(&Name::from("__annotations__"), parsed.module.range, None);
    }
    // 顶层赋值（含 for 目标、except as 等）。
    for (name, range) in collect_assigned_names(&parsed.module.body) {
        ensure_var(&name, range, None);
    }
    // 装饰定义的公开结果可以是任意值，独立于原FuncId/ClassId占动态模块槽。
    let mut decorated_names = Vec::new();
    let dataclass_markers = crate::decorators::dataclass_markers(&parsed.module.body);
    for stmt in crate::module_definitions::definitions(&parsed.module.body) {
        let name = match stmt {
            ast::Stmt::FunctionDef(def) if !def.decorator_list.is_empty() => Some(&def.name),
            ast::Stmt::ClassDef(class)
                if crate::decorators::has_runtime_class_decorators(class, &dataclass_markers) =>
            {
                Some(&class.name)
            }
            _ => None,
        };
        if let Some(name) = name {
            let name = Name::from(name.as_str());
            ensure_var(&name, stmt.range(), None);
            decorated_names.push(name);
        }
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
    for name in global_names.into_iter().chain(decorated_names) {
        if let Some(&var) = module_var_ids.get(&name) {
            vars[var].is_global = true;
        }
    }

    if crate::future_imports::annotations(&parsed.module.body)
        && let Some(&var) = module_var_ids.get(&Name::from("__annotations__"))
    {
        // 注解字典可被用户替换为自定义mapping，不能固定为静态dict布局。
        vars[var].is_global = true;
    }
    (vars, module_var_ids)
}

impl StmtLowerCtx<'_> {
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
