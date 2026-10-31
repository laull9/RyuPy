//! 控制流语句 HIR 降级。
//!
//! 包含：
//! - `if` / `elif` / `else` 递归降级为嵌套二元 [`StmtKind::If`]；
//! - `for ... in range(...)` 降级为原生计数循环 [`StmtKind::ForRange`]；
//! - 循环深度追踪与跳转合法性校验（循环外 `break` / `continue` 报错）。

use ryupy_base::{Diagnostic, Ranged, Span};
use ryupy_syntax::ast;

use crate::hir::{
    Expr, ExprKind, Literal, Local, LocalId, Name, SequenceItem, Stmt, StmtId, StmtKind,
};
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
            line: self.line_of(if_stmt.range.start().to_usize()),
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
                    line: self.line_of(first.range.start().to_usize()),
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
        // `else` 不属于循环体：`break` 在其中非法，因此按当前 loop_depth 降级
        let orelse = self.lower_suite(&for_stmt.orelse);
        // 循环目标：单个名字直接使用；`a, b` / `a, *rest` 这样的序列目标
        // 先写进一个隐藏局部，再在循环体开头做一次解包赋值
        // （等价于 `for __item in ...: a, b = __item`）。
        let mut unpack_prefix: Option<crate::hir::StmtId> = None;
        let var = match &*for_stmt.target {
            ast::Expr::Name(name_expr) => {
                let name = Name::from(name_expr.id.as_str());
                self.func_scope.locals.get(&name).copied()?
            }
            ast::Expr::Tuple(tuple) => {
                let hidden = self.alloc_hidden_for_local(for_stmt.target.range());
                let items = self.lower_sequence_target(&tuple.elts, tuple.range)?;
                unpack_prefix = Some(self.synth_unpack_stmt(hidden, items, for_stmt.range));
                hidden
            }
            ast::Expr::List(list) => {
                let hidden = self.alloc_hidden_for_local(for_stmt.target.range());
                let items = self.lower_sequence_target(&list.elts, list.range)?;
                unpack_prefix = Some(self.synth_unpack_stmt(hidden, items, for_stmt.range));
                hidden
            }
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, for_stmt.target.range()),
                        "暂不支持该循环目标（只支持单个变量或序列解包）",
                    )
                    .with_code("E0100")
                    .with_primary_label("不支持该循环目标"),
                );
                return None;
            }
        };

        let (start, stop, step) = match &*for_stmt.iter {
            ast::Expr::Call(call_expr) => {
                let is_range = if let ast::Expr::Name(callee_name) = &*call_expr.func {
                    callee_name.id.as_str() == "range"
                } else {
                    false
                };
                if !is_range {
                    // 非 range 的可迭代对象：走通用迭代协议（M2.3 容器图）
                    let iter = self.lower_expr(&for_stmt.iter);
                    self.loop_depth += 1;
                    let mut body = self.lower_suite(&for_stmt.body);
                    if let Some(prefix) = unpack_prefix {
                        body.insert(0, prefix);
                    }
                    self.loop_depth -= 1;
                    return Some(StmtKind::ForEach {
                        var,
                        iter,
                        body,
                        orelse,
                    });
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
            }
            _ => {
                // 非调用表达式也走通用迭代协议
                let iter = self.lower_expr(&for_stmt.iter);
                self.loop_depth += 1;
                let mut body = self.lower_suite(&for_stmt.body);
                if let Some(prefix) = unpack_prefix {
                    body.insert(0, prefix);
                }
                self.loop_depth -= 1;
                return Some(StmtKind::ForEach {
                    var,
                    iter,
                    body,
                    orelse,
                });
            }
        };

        self.loop_depth += 1;
        let mut body = self.lower_suite(&for_stmt.body);
        if let Some(prefix) = unpack_prefix {
            body.insert(0, prefix);
        }
        self.loop_depth -= 1;

        Some(StmtKind::ForRange {
            var,
            start,
            stop,
            step,
            body,
            orelse,
        })
    }

    /// 为序列解包的循环目标分配一个隐藏局部（名字不可能与源码标识符冲突）。
    fn alloc_hidden_for_local(&mut self, range: ryupy_base::TextRange) -> LocalId {
        let index = self.body.locals.len();
        self.body.locals.alloc(Local {
            name: Name::from(format!("__for_item${index}").as_str()),
            annotation: None,
            is_param: false,
            range,
        })
    }

    /// 合成 `目标序列 = 隐藏局部` 的解包赋值语句。
    fn synth_unpack_stmt(
        &mut self,
        hidden: LocalId,
        items: Vec<SequenceItem>,
        range: ryupy_base::TextRange,
    ) -> StmtId {
        let value = self.body.exprs.alloc(crate::hir::Expr {
            kind: ExprKind::Local(hidden),
            range,
        });
        self.body.stmts.alloc(crate::hir::Stmt {
            kind: StmtKind::Assign {
                target: crate::hir::AssignTarget::Sequence(items),
                value,
            },
            range,
            line: self.line_of(range.start().to_usize()),
        })
    }
}
