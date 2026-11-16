//! async-for去糖：同步取迭代器，每轮只保护anext调用和await，目标/正文/else位于耗尽区外。
use crate::lower::StmtLowerCtx;
use crate::{
    AssignTarget, AsyncIterationOp, AwaitContext, Binding, Expr, ExprId, ExprKind, Handler,
    HandlerSemantics, Literal, Local, LocalId, Name, Stmt, StmtId, StmtKind,
};
use ryupy_base::{Diagnostic, Ranged, Span, TextRange};
use ryupy_syntax::ast;

impl StmtLowerCtx<'_> {
    pub(crate) fn lower_async_for(&mut self, source: &ast::StmtFor) -> Option<StmtKind> {
        let range = source.range;
        let iterator = self.async_temp("iterator", range);
        let exhausted = self.async_temp("exhausted", range);
        let item = self.async_temp("item", range);
        let caught = self.async_temp("caught", range);
        let value = self.lower_expr(&source.iter);
        let start = self.async_expr(
            ExprKind::AsyncIteration {
                value,
                op: AsyncIterationOp::Start,
            },
            range,
        );
        let mut outer = vec![self.async_assign(iterator, start, range)];
        let no = self.async_expr(ExprKind::Literal(Literal::Bool(false)), range);
        outer.push(self.async_assign(exhausted, no, range));

        // else的break/continue归属外层；目标与正文才位于本循环的词法作用域。
        let orelse = self.lower_suite(&source.orelse);
        self.loop_depth += 1;
        let target = self.async_target(&source.target);
        let body = self.lower_suite(&source.body);
        self.loop_depth -= 1;
        let target = target?;

        let read = self.async_expr(ExprKind::Local(iterator), range);
        let next = self.async_expr(
            ExprKind::AsyncIteration {
                value: read,
                op: AsyncIterationOp::Next,
            },
            range,
        );
        let awaited = self.async_expr(
            ExprKind::Await {
                value: next,
                context: AwaitContext::AsyncNext,
            },
            range,
        );
        let take = self.async_assign(item, awaited, range);
        self.body.stmts[take].line = self.line_of(range.start().to_usize());
        let yes = self.async_expr(ExprKind::Literal(Literal::Bool(true)), range);
        let mark = self.async_assign(exhausted, yes, range);
        let leave = self.async_stmt(StmtKind::Break, range);
        let reraise = self.async_stmt(
            StmtKind::Raise {
                value: None,
                cause: None,
            },
            range,
        );
        let error = self.async_expr(ExprKind::Local(caught), range);
        let is_stop = self.async_expr(
            ExprKind::AsyncIteration {
                value: error,
                op: AsyncIterationOp::IsStop,
            },
            range,
        );
        let dispatch = self.async_stmt(
            StmtKind::If {
                cond: is_stop,
                then_: vec![mark, leave],
                else_: vec![reraise],
            },
            range,
        );
        // CatchAll + 受信运行时身份判断，避开用户同名StopAsyncIteration类/变量。
        let handler = self.body.handlers.alloc(Handler {
            semantics: HandlerSemantics::SynthesizedAsyncFor,
            types: None,
            type_expr: None,
            type_line: 0,
            name: Some(Binding::Local(caught)),
            body: vec![dispatch],
            range,
        });
        let protected = self.async_stmt(
            StmtKind::Try {
                body: vec![take],
                handlers: vec![handler],
                orelse: vec![],
                finalbody: vec![],
            },
            range,
        );
        let read = self.async_expr(ExprKind::Local(item), range);
        let bind = self.async_stmt(
            StmtKind::Assign {
                target,
                value: read,
            },
            range,
        );
        self.body.stmts[bind].line = self.line_of(range.start().to_usize());
        let mut loop_body = vec![protected, bind];
        loop_body.extend(body);
        let cond = self.async_expr(ExprKind::Literal(Literal::Bool(true)), range);
        outer.push(self.async_stmt(
            StmtKind::While {
                cond,
                body: loop_body,
                orelse: vec![],
            },
            range,
        ));
        let cond = self.async_expr(ExprKind::Local(exhausted), range);
        outer.push(self.async_stmt(
            StmtKind::If {
                cond,
                then_: orelse,
                else_: vec![],
            },
            range,
        ));
        let cond = self.async_expr(ExprKind::Literal(Literal::Bool(true)), range);
        Some(StmtKind::If {
            cond,
            then_: outer,
            else_: vec![],
        })
    }

    fn async_target(&mut self, target: &ast::Expr) -> Option<AssignTarget> {
        Some(match target {
            ast::Expr::Name(name) => match self.resolve_binding(&Name::from(name.id.as_str()))? {
                Binding::Local(id) => AssignTarget::Local(id),
                Binding::ModuleVar(id) => AssignTarget::ModuleVar(id),
                Binding::Free(id) => AssignTarget::Free(id),
            },
            ast::Expr::Tuple(value) => {
                AssignTarget::Sequence(self.lower_sequence_target(&value.elts, value.range)?)
            }
            ast::Expr::List(value) => {
                AssignTarget::Sequence(self.lower_sequence_target(&value.elts, value.range)?)
            }
            ast::Expr::Attribute(value) => AssignTarget::Attribute {
                base: self.lower_expr(&value.value),
                name: Name::from(value.attr.as_str()),
            },
            ast::Expr::Subscript(value) => AssignTarget::Subscript {
                base: self.lower_expr(&value.value),
                index: self.lower_subscript_index(&value.slice),
            },
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, target.range()),
                        "暂不支持该async for赋值目标",
                    )
                    .with_code("E0100"),
                );
                return None;
            }
        })
    }
    fn async_temp(&mut self, name: &str, range: TextRange) -> LocalId {
        let index = self.body.locals.len();
        self.body.locals.alloc(Local {
            name: Name::from(format!("$async_{name}${index}").as_str()),
            annotation: None,
            is_param: false,
            range,
        })
    }
    fn async_expr(&mut self, kind: ExprKind, range: TextRange) -> ExprId {
        self.body.exprs.alloc(Expr { kind, range })
    }
    fn async_stmt(&mut self, kind: StmtKind, range: TextRange) -> StmtId {
        self.body.stmts.alloc(Stmt {
            kind,
            range,
            // 标记/分派不执行新的源码；下一项与目标赋值单独登记for行。
            line: 0,
        })
    }
    fn async_assign(&mut self, local: LocalId, value: ExprId, range: TextRange) -> StmtId {
        self.async_stmt(
            StmtKind::Assign {
                target: AssignTarget::Local(local),
                value,
            },
            range,
        )
    }
}
