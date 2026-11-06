//! `with` 语句的 HIR 降级（M2.8）。
//!
//! 这里把 `with` **去糖**成已有的 `try` / `except` / `finally` 与普通调用，
//! 好处是 MIR、类型检查与 codegen 都不需要为 `with` 增加新节点；异常区域、
//! `return` / `break` / `continue` 穿越清理逻辑全部复用 `try` 的现有实现。
//!
//! `with ctx as x: body` 展开成（概念上）：
//!
//! ```text
//! mgr = ctx
//! exit = mgr.__exit__          # CPython 同样在 __enter__ 之前取出
//! value = mgr.__enter__()
//! x = value
//! handled = False
//! try:
//!     body
//! except BaseException as exc:
//!     handled = True
//!     if not exit(type(exc), exc, None):
//!         raise
//! finally:
//!     if not handled:
//!         exit(None, None, None)
//! ```
//!
//! 几个关键点：
//! - `handled` 保证 `__exit__` 每次进入只调用一次；被抑制的异常不再进入 finally 的
//!   正常退出分支，而重新抛出的异常由外层 `finally` 通道接管；
//! - `finally` 覆盖正常落下、`return`、`break`、`continue`，这些路径上都要调用
//!   `__exit__(None, None, None)`；
//! - 多上下文按源码顺序嵌套：外层 `try` 包住内层的 `__enter__`，因此内层进入失败时
//!   外层 `__exit__` 仍会收到异常并清理。
//!
//! 所有临时变量都是当前函数的**局部变量**（模块顶层就是 `main` 的局部变量），
//! 名字带前缀避免与用户标识符冲突。

use ryupy_base::{Diagnostic, Ranged, Span};
use ryupy_syntax::ast;

use crate::hir::{
    AssignTarget, Binding, Expr, ExprKind, Handler, Literal, Local, LocalId, Name, Stmt, StmtId,
    StmtKind, UnaryOp,
};
use crate::lower::StmtLowerCtx;

impl StmtLowerCtx<'_> {
    /// 降级一条 `with` 语句，返回包裹展开结果的合成 `if True:` 语句。
    ///
    /// `with` 会展开成多条语句 + 一个 `try`，而 [`Self::lower_stmt`] 的约定是返回
    /// 单条语句，因此用 `if True:` 把多条语句串在一起；条件恒真，MIR 会直接走 then 分支。
    pub(crate) fn lower_with(&mut self, with_stmt: &ast::StmtWith) -> Option<StmtId> {
        if with_stmt.is_async {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, with_stmt.range),
                    "暂不支持 `async with`",
                )
                .with_code("E0100")
                .with_primary_label("目前只支持同步上下文管理器"),
            );
            return None;
        }

        let body = self.lower_suite(&with_stmt.body);
        let expanded = self.lower_with_items(&with_stmt.items, body);
        let cond = self.alloc_with_expr(ExprKind::Literal(Literal::Bool(true)), with_stmt.range);
        Some(self.body.stmts.alloc(Stmt {
            kind: StmtKind::If {
                cond,
                then_: expanded,
                else_: Vec::new(),
            },
            range: with_stmt.range,
            line: self.line_of(with_stmt.range.start().to_usize()),
        }))
    }

    /// 递归展开多个上下文项：第一个包住其余项，形成「先进入、后退出」的嵌套。
    fn lower_with_items(&mut self, items: &[ast::WithItem], body: Vec<StmtId>) -> Vec<StmtId> {
        let Some(first) = items.first() else {
            return body;
        };
        let range = first.context_expr.range();
        let mut out = Vec::new();

        // 1) 先把上下文表达式求值一次，再读取 `__exit__` 与调用 `__enter__`。
        //    求值顺序与 CPython 一致，异常时不会重复求值上下文表达式。
        let mgr = self.alloc_with_temp("mgr", range);
        let context = self.lower_expr(&first.context_expr);
        out.push(self.push_assign_local(mgr, context, range));

        let exit = self.alloc_with_temp("exit", range);
        let mgr_for_exit = self.local_expr(mgr, range);
        let exit_attr = self.alloc_with_expr(
            ExprKind::Attribute {
                base: mgr_for_exit,
                name: Name::from("__exit__"),
            },
            range,
        );
        out.push(self.push_assign_local(exit, exit_attr, range));

        let value = self.alloc_with_temp("value", range);
        let mgr_for_enter = self.local_expr(mgr, range);
        let enter_attr = self.alloc_with_expr(
            ExprKind::Attribute {
                base: mgr_for_enter,
                name: Name::from("__enter__"),
            },
            range,
        );
        let enter_call = self.alloc_with_expr(
            ExprKind::Call {
                callee: enter_attr,
                args: Vec::new(),
            },
            range,
        );
        out.push(self.push_assign_local(value, enter_call, range));

        // 2) `as x`：目标在进入成功后绑定。模块顶层的 x 是模块变量，函数内是局部变量。
        if let Some(target) = &first.optional_vars {
            self.lower_with_target(target, value, &mut out, range);
        }

        // 3) `handled` 标记：区分「异常路径已经调用过 __exit__」与「正常退出」。
        let handled = self.alloc_with_temp("handled", range);
        let false_expr = self.alloc_with_expr(ExprKind::Literal(Literal::Bool(false)), range);
        out.push(self.push_assign_local(handled, false_expr, range));

        // 4) 对应 body：剩余上下文项嵌套在 try 体里。
        let inner = self.lower_with_items(&items[1..], body);

        // 5) 异常分支：handled = True；if not exit(type(exc), exc, None): raise
        let exc = self.alloc_with_temp("exc", range);
        let mut handler_body = Vec::new();
        let true_expr = self.alloc_with_expr(ExprKind::Literal(Literal::Bool(true)), range);
        handler_body.push(self.push_assign_local(handled, true_expr, range));

        let exc_type = self.call_builtin_type(exc, range);
        let exc_value = self.local_expr(exc, range);
        let none_arg = self.none_expr(range);
        let exit_for_handler = self.local_expr(exit, range);
        let exit_call = self.alloc_with_expr(
            ExprKind::Call {
                callee: exit_for_handler,
                args: vec![
                    crate::hir::CallArg::Positional(exc_type),
                    crate::hir::CallArg::Positional(exc_value),
                    crate::hir::CallArg::Positional(none_arg),
                ],
            },
            range,
        );
        let not_exit = self.alloc_with_expr(
            ExprKind::Unary {
                op: UnaryOp::Not,
                operand: exit_call,
            },
            range,
        );
        let reraise = self.body.stmts.alloc(Stmt {
            kind: StmtKind::Raise {
                value: None,
                cause: None,
            },
            range,
            line: self.line_of(range.start().to_usize()),
        });
        let suppress_check = self.body.stmts.alloc(Stmt {
            kind: StmtKind::If {
                cond: not_exit,
                then_: vec![reraise],
                else_: Vec::new(),
            },
            range,
            line: self.line_of(range.start().to_usize()),
        });
        handler_body.push(suppress_check);

        let handler = self.body.handlers.alloc(Handler {
            types: Some(vec!["BaseException".to_owned()]),
            type_expr: None,
            type_line: 0,
            name: Some(Binding::Local(exc)),
            body: handler_body,
            range,
        });

        // 6) finally：没有走过异常分支时调用 exit(None, None, None)。
        let finalbody = vec![self.build_with_normal_exit(handled, exit, range)];

        let try_stmt = self.body.stmts.alloc(Stmt {
            kind: StmtKind::Try {
                body: inner,
                handlers: vec![handler],
                orelse: Vec::new(),
                finalbody,
            },
            range,
            line: self.line_of(range.start().to_usize()),
        });
        out.push(try_stmt);
        out
    }

    /// 构建 `if not handled: exit(None, None, None)` 这条 finally 语句。
    fn build_with_normal_exit(
        &mut self,
        handled: LocalId,
        exit: LocalId,
        range: ryupy_base::TextRange,
    ) -> StmtId {
        let handled_value = self.local_expr(handled, range);
        let not_handled = self.alloc_with_expr(
            ExprKind::Unary {
                op: UnaryOp::Not,
                operand: handled_value,
            },
            range,
        );
        let none_a = self.none_expr(range);
        let none_b = self.none_expr(range);
        let none_c = self.none_expr(range);
        let exit_for_finally = self.local_expr(exit, range);
        let exit_call = self.alloc_with_expr(
            ExprKind::Call {
                callee: exit_for_finally,
                args: vec![
                    crate::hir::CallArg::Positional(none_a),
                    crate::hir::CallArg::Positional(none_b),
                    crate::hir::CallArg::Positional(none_c),
                ],
            },
            range,
        );
        let call_stmt = self.body.stmts.alloc(Stmt {
            kind: StmtKind::Expr(exit_call),
            range,
            line: self.line_of(range.start().to_usize()),
        });
        self.body.stmts.alloc(Stmt {
            kind: StmtKind::If {
                cond: not_handled,
                then_: vec![call_stmt],
                else_: Vec::new(),
            },
            range,
            line: self.line_of(range.start().to_usize()),
        })
    }

    /// 把 `as` 目标绑定到 `__enter__` 的返回值。
    fn lower_with_target(
        &mut self,
        target: &ast::Expr,
        value: LocalId,
        out: &mut Vec<StmtId>,
        range: ryupy_base::TextRange,
    ) {
        match target {
            ast::Expr::Name(name_expr) => {
                let name = Name::from(name_expr.id.as_str());
                match self.resolve_binding(&name) {
                    Some(Binding::Local(local)) => {
                        let target = AssignTarget::Local(local);
                        let value_expr = self.local_expr(value, name_expr.range);
                        out.push(self.push_assign_target(target, value_expr, name_expr.range));
                    }
                    Some(Binding::ModuleVar(var)) => {
                        let target = AssignTarget::ModuleVar(var);
                        let value_expr = self.local_expr(value, name_expr.range);
                        out.push(self.push_assign_target(target, value_expr, name_expr.range));
                    }
                    Some(Binding::Free(idx)) => {
                        let target = AssignTarget::Free(idx);
                        let value_expr = self.local_expr(value, name_expr.range);
                        out.push(self.push_assign_target(target, value_expr, name_expr.range));
                    }
                    None => {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, name_expr.range),
                                format!("未定义的 with 绑定目标 `{name}`"),
                            )
                            .with_code("E0106")
                            .with_primary_label("名字未登记为局部变量或模块变量"),
                        );
                    }
                }
            }
            ast::Expr::Tuple(tuple) => {
                if let Some(items) = self.lower_sequence_target(&tuple.elts, tuple.range) {
                    let value_expr = self.local_expr(value, range);
                    out.push(self.push_assign_target(
                        AssignTarget::Sequence(items),
                        value_expr,
                        tuple.range,
                    ));
                }
            }
            ast::Expr::List(list) => {
                if let Some(items) = self.lower_sequence_target(&list.elts, list.range) {
                    let value_expr = self.local_expr(value, range);
                    out.push(self.push_assign_target(
                        AssignTarget::Sequence(items),
                        value_expr,
                        list.range,
                    ));
                }
            }
            other => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, other.range()),
                        "暂不支持该 `with ... as` 目标",
                    )
                    .with_code("E0100")
                    .with_primary_label("只支持变量或序列解包目标"),
                );
            }
        }
    }

    /// 分配一个 `with` 专用临时局部变量。名字带 `$`，不会与用户标识符冲突。
    fn alloc_with_temp(&mut self, kind: &str, range: ryupy_base::TextRange) -> LocalId {
        let index = self.body.locals.len();
        self.body.locals.alloc(Local {
            name: Name::from(format!("${kind}${index}").as_str()),
            annotation: None,
            is_param: false,
            range,
        })
    }

    /// 分配一个合成表达式。
    fn alloc_with_expr(
        &mut self,
        kind: ExprKind,
        range: ryupy_base::TextRange,
    ) -> crate::hir::ExprId {
        self.body.exprs.alloc(Expr { kind, range })
    }

    /// 读取局部变量的表达式。
    fn local_expr(&mut self, local: LocalId, range: ryupy_base::TextRange) -> crate::hir::ExprId {
        self.alloc_with_expr(ExprKind::Local(local), range)
    }

    /// `None` 字面量表达式。
    fn none_expr(&mut self, range: ryupy_base::TextRange) -> crate::hir::ExprId {
        self.alloc_with_expr(ExprKind::Literal(Literal::None), range)
    }

    /// `type(exc)`：让 `__exit__` 拿到异常的类型对象。
    fn call_builtin_type(
        &mut self,
        exc: LocalId,
        range: ryupy_base::TextRange,
    ) -> crate::hir::ExprId {
        let callee = self.alloc_with_expr(ExprKind::Builtin(crate::hir::Builtin::Type), range);
        let arg = self.local_expr(exc, range);
        self.alloc_with_expr(
            ExprKind::Call {
                callee,
                args: vec![crate::hir::CallArg::Positional(arg)],
            },
            range,
        )
    }

    /// 生成 `let target = value` 形式的赋值语句。
    fn push_assign_target(
        &mut self,
        target: AssignTarget,
        value: crate::hir::ExprId,
        range: ryupy_base::TextRange,
    ) -> StmtId {
        self.body.stmts.alloc(Stmt {
            kind: StmtKind::Assign { target, value },
            range,
            line: self.line_of(range.start().to_usize()),
        })
    }

    /// 生成 `let local = value` 形式的赋值语句。
    fn push_assign_local(
        &mut self,
        local: LocalId,
        value: crate::hir::ExprId,
        range: ryupy_base::TextRange,
    ) -> StmtId {
        self.push_assign_target(AssignTarget::Local(local), value, range)
    }
}
