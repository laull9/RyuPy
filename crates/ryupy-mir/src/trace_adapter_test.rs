//! S2a隔离准入与真实源/接收发射；不是源文本匹配，也不生成手填scene。
use crate::build::FunctionBuilder;
use crate::{Callee, LocalId, Operand, RuntimeFn, Rvalue, Statement};

pub(crate) fn admission(hir: &ryupy_hir::Module) -> Vec<ryupy_base::Diagnostic> {
    let mut errors = Vec::new();
    for (_, function) in hir.functions.iter() {
        let unsupported = function.is_generator
            || function.body.handlers.iter().any(|(_, handler)| {
                handler.semantics != ryupy_hir::HandlerSemantics::SourceStatic
                    || contains_try(&function.body, &handler.body)
            })
            || function.body.stmts.iter().any(|(_, stmt)| {
                matches!(stmt.kind, ryupy_hir::StmtKind::Raise { value: None, .. })
                    || matches!(
                        stmt.kind,
                        ryupy_hir::StmtKind::Raise { cause: Some(_), .. }
                    )
                    || matches!(&stmt.kind, ryupy_hir::StmtKind::Try { finalbody, .. } if !finalbody.is_empty())
            });
        if unsupported {
            errors.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(hir.file, function.range),
                    "S2a probe不支持bare/from/generator/dynamic except/with/用户finally/handler嵌套try",
                )
                .with_code("E0305"),
            );
        }
    }
    errors
}

impl FunctionBuilder<'_> {
    pub(crate) fn emit_adapter_receive(&mut self, error: LocalId, forward: bool) {
        let Some(depth) = self.frame_depth else {
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(
                        self.hir.file,
                        self.hir.functions[self.current_hir_fn].range,
                    ),
                    "S2a接收方缺少真实activation",
                )
                .with_code("E0305"),
            );
            return;
        };
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(if forward {
                RuntimeFn::TraceAdapterForward
            } else {
                RuntimeFn::TraceAdapterHandle
            }),
            args: vec![Operand::Local(error), Operand::Local(depth)],
        }));
    }
}

// 首切不支持同activation连续自动cleanup转发，按真实HIR语句树保守拒绝。
fn contains_try(body: &ryupy_hir::Body, statements: &[ryupy_hir::StmtId]) -> bool {
    statements.iter().any(|id| match &body.stmts[*id].kind {
        ryupy_hir::StmtKind::Try { .. } => true,
        ryupy_hir::StmtKind::If { then_, else_, .. } => {
            contains_try(body, then_) || contains_try(body, else_)
        }
        ryupy_hir::StmtKind::While {
            body: inner,
            orelse,
            ..
        }
        | ryupy_hir::StmtKind::ForEach {
            body: inner,
            orelse,
            ..
        }
        | ryupy_hir::StmtKind::ForRange {
            body: inner,
            orelse,
            ..
        } => contains_try(body, inner) || contains_try(body, orelse),
        _ => false,
    })
}
