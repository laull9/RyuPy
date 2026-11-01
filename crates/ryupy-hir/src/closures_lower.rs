//! 语句降级：`nonlocal` 声明与嵌套 `def`（M3.0）。
//!
//! `lower_nested_def` 在 def 位置求值参数默认值并记录 `NestedDef` 语句；
//! `lower_nonlocal` 只处理模块作用域的非法使用（合法声明已在分析阶段解析）。

use ryupy_base::{Diagnostic, Span};
use ryupy_syntax::ast;

use crate::hir::{AssignTarget, Binding, ExprId, Name, StmtId, StmtKind};

// ───────────────────────── 语句降级：nonlocal 与嵌套 def ─────────────────────────

impl crate::lower::StmtLowerCtx<'_> {
    /// `nonlocal` 语句。合法的声明已在分析阶段解析成 free 索引；
    /// 这里只在模块作用域使用时报错（CPython 是 SyntaxError）。
    pub(crate) fn lower_nonlocal(&mut self, n: &ast::StmtNonlocal) {
        if self.module_scope {
            let name = n.names.first().map(|ident| ident.as_str()).unwrap_or("?");
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, n.range),
                    format!("模块作用域不能使用 nonlocal 声明 `{name}`"),
                )
                .with_code("E0103")
                .with_primary_label("nonlocal 只能出现在函数体内"),
            );
        }
    }

    /// 嵌套 `def` 语句（M3.0）：
    /// 1. 在当前位置（外层作用域）求值参数默认值——每次执行 def 都产生新的一份；
    /// 2. 记录 `NestedDef` 语句，MIR 在此位置创建函数对象并写入绑定名。
    ///
    /// 绑定目标通常是外层函数的局部变量（被捕获时是 cell）；模块顶层的
    /// `if` / `for` 里的 def 绑定到模块变量。
    pub(crate) fn lower_nested_def(&mut self, fn_def: &ast::StmtFunctionDef) -> Option<StmtId> {
        if fn_def.is_async {
            self.diagnostics.push(
                Diagnostic::error(Span::new(self.file, fn_def.range), "嵌套函数暂不支持 async")
                    .with_code("E0100")
                    .with_primary_label("异步按后续里程碑接入"),
            );
            return None;
        }
        let name = Name::from(fn_def.name.as_str());
        let Some(&func_id) = self.nested_funcs.get(&fn_def.range) else {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, fn_def.range),
                    format!("内部错误：嵌套函数 `{name}` 未登记"),
                )
                .with_code("E0101")
                .with_primary_label("闭包分析与降级阶段的登记不一致"),
            );
            return None;
        };
        let target = match self.resolve_binding(&name) {
            Some(Binding::Local(local)) => AssignTarget::Local(local),
            Some(Binding::ModuleVar(var)) => AssignTarget::ModuleVar(var),
            Some(Binding::Free(idx)) => AssignTarget::Free(idx),
            None => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, fn_def.name.range),
                        format!("未定义的赋值目标 `{name}`"),
                    )
                    .with_code("E0106")
                    .with_primary_label("名字未登记为局部变量或模块变量"),
                );
                return None;
            }
        };

        let decorators: Vec<_> = fn_def
            .decorator_list
            .iter()
            .map(|decorator| self.lower_expr(&decorator.expression))
            .collect();

        // 参数默认值按最终参数顺序对齐（posonly → args → vararg → kwonly → kwarg），
        // 每次执行 def 都会在当前位置重新求值。
        let mut aligned: Vec<Option<ExprId>> = Vec::new();
        for param_def in fn_def
            .parameters
            .posonlyargs
            .iter()
            .chain(&fn_def.parameters.args)
        {
            aligned.push(
                param_def
                    .default
                    .as_deref()
                    .map(|expr| self.lower_expr(expr)),
            );
        }
        if fn_def.parameters.vararg.is_some() {
            aligned.push(None);
        }
        for param_def in &fn_def.parameters.kwonlyargs {
            aligned.push(
                param_def
                    .default
                    .as_deref()
                    .map(|expr| self.lower_expr(expr)),
            );
        }
        if fn_def.parameters.kwarg.is_some() {
            aligned.push(None);
        }
        self.nested_defaults.insert(func_id, aligned);

        let line = self.line_of(fn_def.range.start().to_usize());
        let stmt_id = self.body.stmts.alloc(crate::hir::Stmt {
            kind: if decorators.is_empty() {
                StmtKind::NestedDef {
                    func: func_id,
                    target,
                }
            } else {
                StmtKind::DecoratedDef {
                    func: func_id,
                    target,
                    decorators,
                }
            },
            range: fn_def.range,
            line,
        });
        Some(stmt_id)
    }
}
