//! 链式赋值的顺序去糖：RHS只求值一次，目标按源码顺序逐个求值和写入。
//!
//! 隐藏局部必须保存同一个Python对象，不能让静态整数在多次动态写入时重新装箱。
//! 首版使用Runtime槽；现有整函数分层会因此选择动态路径。这是保身份的明确成本，
//! 不做静态速度承诺。生成器复用现有局部spill，不另造环境或跨暂停重复求值。
use crate::hir::{
    AssignTarget, Binding, Expr, ExprKind, Literal, Local, Name, Stmt, StmtId, StmtKind, TypeRef,
};
use crate::lower::StmtLowerCtx;
use ryupy_base::{Diagnostic, Ranged, Span, TextRange};
use ryupy_syntax::ast;

impl StmtLowerCtx<'_> {
    /// 用恒真if串联语句，保持lower_stmt一次返回一个StmtId的既有接口。
    pub(crate) fn lower_chain_assign(&mut self, assignment: &ast::StmtAssign) -> Option<StmtId> {
        let value = self.lower_expr(&assignment.value);
        let index = self.body.locals.len();
        let saved = self.body.locals.alloc(Local {
            name: Name::from(format!("__chain${index}").as_str()),
            annotation: Some(TypeRef::Runtime),
            is_param: false,
            range: assignment.range,
        });
        let mut statements = vec![self.chain_statement(
            StmtKind::Assign {
                target: AssignTarget::Local(saved),
                value,
            },
            assignment.range,
        )];
        for target in &assignment.targets {
            // 这里只构造表达式IR，不提前执行对象/下标；每条Assign由MIR按顺序执行。
            let destination = self.chain_target(target)?;
            let value = self.body.exprs.alloc(Expr {
                kind: ExprKind::Local(saved),
                range: assignment.value.range(),
            });
            statements.push(self.chain_statement(
                StmtKind::Assign {
                    target: destination,
                    value,
                },
                target.range(),
            ));
        }
        let cond = self.body.exprs.alloc(Expr {
            kind: ExprKind::Literal(Literal::Bool(true)),
            range: assignment.range,
        });
        Some(self.chain_statement(
            StmtKind::If {
                cond,
                then_: statements,
                else_: vec![],
            },
            assignment.range,
        ))
    }

    fn chain_target(&mut self, target: &ast::Expr) -> Option<AssignTarget> {
        match target {
            ast::Expr::Name(name) => match self.resolve_binding(&Name::from(name.id.as_str())) {
                Some(Binding::Local(local)) => Some(AssignTarget::Local(local)),
                Some(Binding::ModuleVar(var)) => Some(AssignTarget::ModuleVar(var)),
                Some(Binding::Free(free)) => Some(AssignTarget::Free(free)),
                None => {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, target.range()),
                            "链式赋值目标未登记为局部、模块变量或外层cell",
                        )
                        .with_code("E0106"),
                    );
                    None
                }
            },
            ast::Expr::Attribute(attr) => Some(AssignTarget::Attribute {
                base: self.lower_expr(&attr.value),
                name: Name::from(attr.attr.as_str()),
            }),
            ast::Expr::Subscript(subscript) => Some(AssignTarget::Subscript {
                base: self.lower_expr(&subscript.value),
                index: self.lower_subscript_index(&subscript.slice),
            }),
            ast::Expr::Tuple(tuple) => self
                .lower_sequence_target(&tuple.elts, tuple.range)
                .map(AssignTarget::Sequence),
            ast::Expr::List(list) => self
                .lower_sequence_target(&list.elts, list.range)
                .map(AssignTarget::Sequence),
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, target.range()),
                        "暂不支持该链式赋值目标",
                    )
                    .with_code("E0100")
                    .with_primary_label("支持名字、属性、下标及既有平坦序列解包"),
                );
                None
            }
        }
    }
    fn chain_statement(&mut self, kind: StmtKind, range: TextRange) -> StmtId {
        self.body.stmts.alloc(Stmt {
            kind,
            range,
            line: self.line_of(range.start().to_usize()),
        })
    }
}
