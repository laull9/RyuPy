//! 控制流语句 HIR 降级。
//!
//! 包含：
//! - `if` / `elif` / `else` 递归降级为嵌套二元 [`StmtKind::If`]；
//! - `for ... in range(...)` 降级为原生计数循环 [`StmtKind::ForRange`]；
//! - 循环深度追踪与跳转合法性校验（循环外 `break` / `continue` 报错）。

use ryupy_base::{Diagnostic, Ranged, Span};
use ryupy_syntax::ast;

use crate::hir::{Expr, ExprKind, Literal, Name, Stmt, StmtId, StmtKind};
use crate::lower::StmtLowerCtx;

impl StmtLowerCtx<'_> {
    /// 将 `if` 语句降级为二元 `If { cond, then_, else_ }`。
    ///
    /// 多个 `elif` 会被递归折叠为 `else` 分支中的嵌套 `If` 语句块。
    pub(crate) fn lower_if(&mut self, if_stmt: &ast::StmtIf) -> StmtId {
        let cond = self.lower_expr(&if_stmt.test);
        let then_ = self.lower_suite(&if_stmt.body);
        let else_ = self.lower_elif_else(&if_stmt.elif_else_clauses);
        self.body.stmts.alloc(Stmt {
            kind: StmtKind::If { cond, then_, else_ },
            range: if_stmt.range,
        })
    }

    /// 递归降级 `elif` 与 `else` 子句列表。
    pub(crate) fn lower_elif_else(&mut self, clauses: &[ast::ElifElseClause]) -> Vec<StmtId> {
        if clauses.is_empty() {
            return Vec::new();
        }
        let first = &clauses[0];
        let rest = &clauses[1..];
        match &first.test {
            Some(test_expr) => {
                let cond = self.lower_expr(test_expr);
                let then_ = self.lower_suite(&first.body);
                let else_ = self.lower_elif_else(rest);
                let nested_if = self.body.stmts.alloc(Stmt {
                    kind: StmtKind::If { cond, then_, else_ },
                    range: first.range,
                });
                vec![nested_if]
            }
            None => self.lower_suite(&first.body),
        }
    }

    /// 将 `for ... in range(...)` 降级为专用计数循环 [`StmtKind::ForRange`]。
    ///
    /// 校验项：
    /// 1. 不支持 `for ... else` 结构；
    /// 2. 循环目标必须是单个局部变量（如 `i` 或 `_`）；
    /// 3. 迭代对象必须是 `range(...)` 调用，参数数量在 1 到 3 之间；
    /// 4. 循环体在 `loop_depth + 1` 上下文中降级，以支持内部 `break` / `continue` 校验。
    pub(crate) fn lower_for(&mut self, for_stmt: &ast::StmtFor) -> Option<StmtKind> {
        if !for_stmt.orelse.is_empty() {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, for_stmt.range),
                    "Phase 1 暂不支持 for ... else 语法",
                )
                .with_code("E0100")
                .with_primary_label("不支持 for else"),
            );
        }
        let var = if let ast::Expr::Name(name_expr) = &*for_stmt.target {
            let name = Name::from(name_expr.id.as_str());
            self.func_scope.locals.get(&name).copied()?
        } else {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, for_stmt.target.range()),
                    "Phase 1 仅支持单个局部变量作为循环变量",
                )
                .with_code("E0100")
                .with_primary_label("不支持非简单变量循环目标"),
            );
            return None;
        };

        let (start, stop, step) = if let ast::Expr::Call(call_expr) = &*for_stmt.iter {
            let is_range = if let ast::Expr::Name(callee_name) = &*call_expr.func {
                callee_name.id.as_str() == "range"
            } else {
                false
            };
            if !is_range {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, for_stmt.iter.range()),
                        "Phase 1 仅支持 `for ... in range(...)` 循环",
                    )
                    .with_code("E0100")
                    .with_primary_label("不支持非 range 的可迭代对象"),
                );
                return None;
            }
            if !call_expr.arguments.keywords.is_empty() {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, call_expr.range()),
                        "range() 暂不支持关键字参数",
                    )
                    .with_code("E0100")
                    .with_primary_label("仅支持位置参数"),
                );
            }
            let args = &call_expr.arguments.args;
            match args.len() {
                1 => {
                    let zero = self.body.exprs.alloc(Expr {
                        kind: ExprKind::Literal(Literal::Int(0)),
                        range: call_expr.range(),
                    });
                    let stop = self.lower_expr(&args[0]);
                    (zero, stop, None)
                }
                2 => {
                    let start = self.lower_expr(&args[0]);
                    let stop = self.lower_expr(&args[1]);
                    (start, stop, None)
                }
                3 => {
                    let start = self.lower_expr(&args[0]);
                    let stop = self.lower_expr(&args[1]);
                    let step = self.lower_expr(&args[2]);
                    (start, stop, Some(step))
                }
                _ => {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, call_expr.range()),
                            format!("range() 期望 1 到 3 个参数，得到 {}", args.len()),
                        )
                        .with_code("E0100")
                        .with_primary_label("参数数量错误"),
                    );
                    return None;
                }
            }
        } else {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, for_stmt.iter.range()),
                    "Phase 1 仅支持 `for ... in range(...)` 循环",
                )
                .with_code("E0100")
                .with_primary_label("不支持非 range 的可迭代对象"),
            );
            return None;
        };

        self.loop_depth += 1;
        let body = self.lower_suite(&for_stmt.body);
        self.loop_depth -= 1;

        Some(StmtKind::ForRange {
            var,
            start,
            stop,
            step,
            body,
        })
    }
}
