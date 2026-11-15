//! 语句降级的表达式上下文与定义语句构造；从模块编排分离。
use crate::expr::ExprLowerCtx;
use crate::hir::{ExprId, Stmt, StmtKind};
use crate::lower::StmtLowerCtx;
use ryupy_syntax::ast;

impl StmtLowerCtx<'_> {
    /// 语句降级复用：把下标位置（可能是切片）降级成表达式。
    pub(crate) fn lower_subscript_index(&mut self, slice: &ast::Expr) -> ExprId {
        let mut expr_ctx = ExprLowerCtx {
            class_names: None,
            allow_yield: !self.module_scope,
            file: self.file,
            global_scope: self.global_scope,
            func_scope: self.func_scope,
            module_vars: self.module_vars,
            nested_funcs: self.nested_funcs,
            nested_defaults: self.nested_defaults,
            body: self.body,
            diagnostics: self.diagnostics,
        };
        expr_ctx.lower_subscript_index(slice)
    }

    pub(crate) fn lower_expr(&mut self, expr: &ast::Expr) -> ExprId {
        let mut expr_ctx = ExprLowerCtx {
            class_names: None,
            allow_yield: !self.module_scope,
            file: self.file,
            global_scope: self.global_scope,
            func_scope: self.func_scope,
            module_vars: self.module_vars,
            nested_funcs: self.nested_funcs,
            nested_defaults: self.nested_defaults,
            body: self.body,
            diagnostics: self.diagnostics,
        };
        expr_ctx.lower_expr(expr)
    }

    /// 在当前位置插入一条「求值某函数默认值」的内部语句。
    pub(crate) fn lower_function_defaults(
        &mut self,
        func_id: crate::hir::FuncId,
        range: ryupy_base::TextRange,
    ) {
        let line = self.line_of(range.start().to_usize());
        let stmt_id = self.body.stmts.alloc(Stmt {
            kind: StmtKind::FunctionDefaults(func_id),
            range,
            line,
        });
        self.body.root.push(stmt_id);
    }

    /// 在当前位置插入一条类定义语句（类体属性与方法对象由 MIR 按顺序执行）。
    pub(crate) fn lower_class_def(
        &mut self,
        cls_id: crate::hir::ClassId,
        range: ryupy_base::TextRange,
    ) {
        let line = self.line_of(range.start().to_usize());
        let stmt_id = self.body.stmts.alloc(Stmt {
            kind: StmtKind::ClassDef(cls_id),
            range,
            line,
        });
        self.body.root.push(stmt_id);
    }
}
