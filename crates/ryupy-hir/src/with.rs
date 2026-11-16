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
//! enter = context_method(mgr, "__enter__")
//! exit = context_method(mgr, "__exit__")
//! value = enter()
//! handled = False
//! try:
//!     x = value
//!     body
//! except BaseException as exc:
//!     handled = True
//!     if not exit(type(exc), exc, traceback_of(exc)):
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
        if with_stmt.is_async && !cfg!(feature = "native-coroutines") {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, with_stmt.range),
                    "暂不支持 `async with`",
                )
                .with_code("E0100")
                // 此产生点是合法async with的明确支持域拒绝，不包含未知/type/MIR错误。
                .with_deferred_failure(ryupy_base::diagnostic::DeferredFailure::SupportBoundary)
                .with_primary_label("目前只支持同步上下文管理器"),
            );
            return None;
        }

        let body = self.lower_suite(&with_stmt.body);
        let expanded = self.lower_with_items(&with_stmt.items, body, with_stmt.is_async);
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
    fn lower_with_items(
        &mut self,
        items: &[ast::WithItem],
        body: Vec<StmtId>,
        is_async: bool,
    ) -> Vec<StmtId> {
        let Some(first) = items.first() else {
            return body;
        };
        let range = first.context_expr.range();
        let mut out = Vec::new();

        // 1) 上下文求值一次，依次绑定类型的 enter/exit，再调用 enter。
        //    求值顺序与 CPython 一致，异常时不会重复求值上下文表达式。
        let mgr = self.alloc_with_temp("mgr", range);
        let context = self.lower_expr(&first.context_expr);
        out.push(self.push_assign_local(mgr, context, range));

        let enter = self.alloc_with_temp("enter", range);
        let mgr_for_enter = self.local_expr(mgr, range);
        let enter_attr = self.alloc_with_expr(
            ExprKind::ContextMethod {
                base: mgr_for_enter,
                name: Name::from(if is_async { "__aenter__" } else { "__enter__" }),
            },
            range,
        );
        out.push(self.push_assign_local(enter, enter_attr, range));
        let exit = self.alloc_with_temp("exit", range);
        let mgr_for_exit = self.local_expr(mgr, range);
        let exit_attr = self.alloc_with_expr(
            ExprKind::ContextMethod {
                base: mgr_for_exit,
                name: Name::from(if is_async { "__aexit__" } else { "__exit__" }),
            },
            range,
        );
        out.push(self.push_assign_local(exit, exit_attr, range));
        let value = self.alloc_with_temp("value", range);
        let enter_attr = self.local_expr(enter, range);
        let enter_call = self.alloc_with_expr(
            ExprKind::Call {
                callee: enter_attr,
                args: Vec::new(),
            },
            range,
        );
        let enter_call =
            self.with_await(enter_call, is_async, crate::AwaitContext::AsyncEnter, range);
        out.push(self.push_assign_local(value, enter_call, range));

        // 3) `handled` 标记：区分「异常路径已经调用过 __exit__」与「正常退出」。
        let handled = self.alloc_with_temp("handled", range);
        let false_expr = self.alloc_with_expr(ExprKind::Literal(Literal::Bool(false)), range);
        out.push(self.push_assign_local(handled, false_expr, range));

        // 4) 对应 body：剩余上下文项嵌套在 try 体里。
        // 进入成功后，as 赋值也属于受保护体；失败必须调用已经绑定的 exit。
        let mut inner = Vec::new();
        if let Some(target) = &first.optional_vars {
            self.lower_with_target(target, value, &mut inner, range);
        }
        inner.extend(self.lower_with_items(&items[1..], body, is_async));

        // 5) 异常分支：handled = True；if not exit(type(exc), exc, None): raise
        let exc = self.alloc_with_temp("exc", range);
        let mut handler_body = Vec::new();
        let true_expr = self.alloc_with_expr(ExprKind::Literal(Literal::Bool(true)), range);
        handler_body.push(self.push_assign_local(handled, true_expr, range));

        let exc_type = self.call_builtin_type(exc, range);
        let exc_value = self.local_expr(exc, range);
        let exc_for_tb = self.local_expr(exc, range);
        let none_arg = self.alloc_with_expr(ExprKind::ExceptionTraceback(exc_for_tb), range);
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
        let exit_call = self.with_await(exit_call, is_async, crate::AwaitContext::AsyncExit, range);
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
            semantics: crate::HandlerSemantics::SynthesizedWith,
            types: Some(vec!["BaseException".to_owned()]),
            type_expr: None,
            type_line: 0,
            name: Some(Binding::Local(exc)),
            body: handler_body,
            range,
        });

        // 6) finally：没有走过异常分支时调用 exit(None, None, None)。
        let finalbody = vec![self.build_with_normal_exit(handled, exit, range, is_async)];

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
        is_async: bool,
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
        let exit_call = self.with_await(exit_call, is_async, crate::AwaitContext::AsyncExit, range);
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

    /// 同步清理保持普通调用；异步 enter/exit 保留专用 await 错误上下文。
    fn with_await(
        &mut self,
        value: crate::hir::ExprId,
        is_async: bool,
        context: crate::AwaitContext,
        range: ryupy_base::TextRange,
    ) -> crate::hir::ExprId {
        if is_async {
            self.alloc_with_expr(ExprKind::Await { value, context }, range)
        } else {
            value
        }
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
            ast::Expr::Attribute(attribute) => {
                let target = AssignTarget::Attribute {
                    base: self.lower_expr(&attribute.value),
                    name: Name::from(attribute.attr.as_str()),
                };
                let value = self.local_expr(value, range);
                out.push(self.push_assign_target(target, value, range));
            }
            ast::Expr::Subscript(subscript) => {
                let target = AssignTarget::Subscript {
                    base: self.lower_expr(&subscript.value),
                    index: self.lower_subscript_index(&subscript.slice),
                };
                let value = self.local_expr(value, range);
                out.push(self.push_assign_target(target, value, range));
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

#[cfg(test)]
mod tests {
    use crate::*;

    fn lower(source: &str) -> Module {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("with_protection.py", source);
        let (parsed, errors) = ryupy_syntax::parse(file, source);
        assert!(errors.is_empty(), "{errors:?}");
        let (module, errors) = lower_module(&parsed, &ModuleInfo::default(), source);
        assert!(errors.is_empty(), "{errors:?}");
        module
    }
    fn assert_target_protected(module: &Module) {
        let function = module
            .functions
            .iter()
            .find(|(_, function)| function.name.as_str() == "f")
            .unwrap()
            .1;
        assert!(function.body.stmts.iter().any(|(_, statement)| {
            let StmtKind::Try { body, handlers, .. } = &statement.kind else {
                return false;
            };
            !handlers.is_empty()
                && body.first().is_some_and(|first| {
                    matches!(
                        function.body.stmts[*first].kind,
                        StmtKind::Assign {
                            target: AssignTarget::Sequence(_),
                            ..
                        }
                    )
                })
        }));
    }
    #[test]
    fn synchronous_with_target_binding_is_inside_cleanup_protection() {
        assert_target_protected(&lower(
            "def f(cm):\n    with cm as (left, right):\n        pass\n",
        ));
        lower("def f(cm, obj):\n    with cm as obj.value:\n        pass\n");
    }
    #[cfg(feature = "native-coroutines")]
    #[test]
    fn asynchronous_with_has_contextual_awaits_and_protected_target() {
        let module = lower("async def f(cm):\n    async with cm as (left, right):\n        pass\n");
        assert_target_protected(&module);
        let function = module
            .functions
            .iter()
            .find(|(_, function)| function.is_coroutine)
            .unwrap()
            .1;
        let contexts: Vec<_> = function
            .body
            .exprs
            .iter()
            .filter_map(|(_, expression)| {
                if let ExprKind::Await { context, .. } = expression.kind {
                    Some(context)
                } else {
                    None
                }
            })
            .collect();
        assert_eq!(
            contexts
                .iter()
                .filter(|context| **context == AwaitContext::AsyncEnter)
                .count(),
            1
        );
        assert_eq!(
            contexts
                .iter()
                .filter(|context| **context == AwaitContext::AsyncExit)
                .count(),
            2
        );
        assert!(
            function
                .body
                .exprs
                .iter()
                .any(|(_, expression)| matches!(expression.kind, ExprKind::ExceptionTraceback(_)))
        );
    }
}
