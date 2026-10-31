//! 增量赋值 `target op= value` 的 HIR 降级。
//!
//! 去糖成 `target = target (op=) value`，其中二元运算带 `inplace` 标记：运行时对可变对象
//! （list / bytearray / set / 带 `__iop__` 的实例）原地修改并保留身份，不可变值与普通运算相同。
//!
//! # 只求值一次
//!
//! Python 对 `a[i] += v` / `obj.attr += v` 的目标子表达式（`a`、`i`、`obj`）**只求值一次**，
//! 然后对同一个对象先读后写。直接把目标降级两次（一次读、一次写）会让带副作用的
//! 子表达式执行两遍，例如 `data[next_index()] += 1` 会调用两次 `next_index()`。
//!
//! 因此非「简单」的子表达式（不是名字或字面量）先求值进隐藏局部：
//!
//! ```text
//! data[idx()] += 1   ⇒   if True:
//!                            __aug$3 = idx()
//!                            data[__aug$3] = data[__aug$3] (+=) 1
//! ```
//!
//! 名字与字面量再读一次没有可观察的差别，保持原样，常见写法的 MIR 不变。
//! 多条语句同 `with` 一样用恒真的 `if True:` 串起来，满足 `lower_stmt` 只返回一条语句的约定。

use ryupy_base::{Diagnostic, Ranged, Span};
use ryupy_syntax::ast;

use crate::hir::{
    AssignTarget, Binding, Expr, ExprId, ExprKind, Literal, Local, LocalId, Name, Stmt, StmtId,
    StmtKind,
};
use crate::lower::StmtLowerCtx;
use crate::operator::map_bin_op;

/// 目标中的一个子表达式：可以重复降级的简单表达式，或已求值进隐藏局部。
enum AugOperand<'a> {
    Simple(&'a ast::Expr),
    Hidden(LocalId),
}

impl StmtLowerCtx<'_> {
    /// 降级一条增量赋值；需要隐藏局部时返回包裹它们的 `if True:`。
    pub(crate) fn lower_aug_assign(&mut self, aug_stmt: &ast::StmtAugAssign) -> Option<StmtId> {
        let Some(op) = map_bin_op(aug_stmt.op) else {
            self.diagnostics.push(
                Diagnostic::error(Span::new(self.file, aug_stmt.range), "暂不支持该增量运算符")
                    .with_code("E0100")
                    .with_primary_label("暂不支持的操作符"),
            );
            return None;
        };
        let mut prefix: Vec<StmtId> = Vec::new();
        let (target, lhs) = match &*aug_stmt.target {
            ast::Expr::Name(name_expr) => {
                let name = Name::from(name_expr.id.as_str());
                let target = match self.resolve_binding(&name) {
                    Some(Binding::Local(local)) => AssignTarget::Local(local),
                    Some(Binding::ModuleVar(var)) => AssignTarget::ModuleVar(var),
                    None => return None,
                };
                (target, self.lower_expr(&aug_stmt.target))
            }
            ast::Expr::Attribute(attr) => {
                let base = self.aug_operand(&attr.value, &mut prefix);
                let name = Name::from(attr.attr.as_str());
                let read_base = self.materialize(&base);
                let lhs = self.alloc_aug_expr(
                    ExprKind::Attribute {
                        base: read_base,
                        name: name.clone(),
                    },
                    attr.range,
                );
                let write_base = self.materialize(&base);
                (
                    AssignTarget::Attribute {
                        base: write_base,
                        name,
                    },
                    lhs,
                )
            }
            ast::Expr::Subscript(sub) => {
                // 求值顺序与 CPython 相同：先容器、再下标
                let base = self.aug_operand(&sub.value, &mut prefix);
                let index = self.aug_index(&sub.slice, &mut prefix);
                let read_base = self.materialize(&base);
                let read_index = self.materialize_index(&index);
                let lhs = self.alloc_aug_expr(
                    ExprKind::Subscript {
                        base: read_base,
                        index: read_index,
                    },
                    sub.range,
                );
                let write_base = self.materialize(&base);
                let write_index = self.materialize_index(&index);
                (
                    AssignTarget::Subscript {
                        base: write_base,
                        index: write_index,
                    },
                    lhs,
                )
            }
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, aug_stmt.target.range()),
                        "暂不支持该增量赋值目标",
                    )
                    .with_code("E0100")
                    .with_primary_label("目标必须是变量、属性或下标"),
                );
                return None;
            }
        };
        let rhs = self.lower_expr(&aug_stmt.value);
        let value = self.alloc_aug_expr(
            ExprKind::Binary {
                op,
                lhs,
                rhs,
                inplace: true,
            },
            aug_stmt.range,
        );
        let assign = self.alloc_aug_stmt(StmtKind::Assign { target, value }, aug_stmt.range);
        if prefix.is_empty() {
            return Some(assign);
        }
        prefix.push(assign);
        let cond = self.alloc_aug_expr(ExprKind::Literal(Literal::Bool(true)), aug_stmt.range);
        Some(self.alloc_aug_stmt(
            StmtKind::If {
                cond,
                then_: prefix,
                else_: Vec::new(),
            },
            aug_stmt.range,
        ))
    }

    /// 名字 / 字面量可以重复降级；其它表达式先求值进隐藏局部。
    fn aug_operand<'a>(&mut self, expr: &'a ast::Expr, prefix: &mut Vec<StmtId>) -> AugOperand<'a> {
        if is_simple(expr) {
            return AugOperand::Simple(expr);
        }
        let value = self.lower_expr(expr);
        AugOperand::Hidden(self.hoist(value, expr.range(), prefix))
    }

    /// 下标同理；切片（`a[i:j] += x`）的各分量很少带副作用，整体按简单处理会重复求值，
    /// 所以切片也整体求值成切片对象再复用。
    fn aug_index<'a>(&mut self, slice: &'a ast::Expr, prefix: &mut Vec<StmtId>) -> AugOperand<'a> {
        if is_simple(slice) {
            return AugOperand::Simple(slice);
        }
        let value = self.lower_subscript_index(slice);
        AugOperand::Hidden(self.hoist(value, slice.range(), prefix))
    }

    /// 生成 `__aug$N = value` 并返回隐藏局部。
    fn hoist(
        &mut self,
        value: ExprId,
        range: ryupy_base::TextRange,
        prefix: &mut Vec<StmtId>,
    ) -> LocalId {
        let index = self.body.locals.len();
        let local = self.body.locals.alloc(Local {
            name: Name::from(format!("__aug${index}").as_str()),
            annotation: None,
            is_param: false,
            range,
        });
        let stmt = self.alloc_aug_stmt(
            StmtKind::Assign {
                target: AssignTarget::Local(local),
                value,
            },
            range,
        );
        prefix.push(stmt);
        local
    }

    /// 每次使用都分配新的表达式节点（HIR 表达式不共享 id）。
    fn materialize(&mut self, operand: &AugOperand<'_>) -> ExprId {
        match operand {
            AugOperand::Simple(expr) => self.lower_expr(expr),
            AugOperand::Hidden(local) => {
                let range = self.body.locals[*local].range;
                self.alloc_aug_expr(ExprKind::Local(*local), range)
            }
        }
    }

    fn materialize_index(&mut self, operand: &AugOperand<'_>) -> ExprId {
        match operand {
            AugOperand::Simple(expr) => self.lower_subscript_index(expr),
            AugOperand::Hidden(_) => self.materialize(operand),
        }
    }

    fn alloc_aug_expr(&mut self, kind: ExprKind, range: ryupy_base::TextRange) -> ExprId {
        self.body.exprs.alloc(Expr { kind, range })
    }

    fn alloc_aug_stmt(&mut self, kind: StmtKind, range: ryupy_base::TextRange) -> StmtId {
        let line = self.line_of(range.start().to_usize());
        self.body.stmts.alloc(Stmt { kind, range, line })
    }
}

/// 重复求值没有可观察差别的表达式：名字（读局部 / 模块变量）与常量。
fn is_simple(expr: &ast::Expr) -> bool {
    matches!(
        expr,
        ast::Expr::Name(_)
            | ast::Expr::NumberLiteral(_)
            | ast::Expr::StringLiteral(_)
            | ast::Expr::BooleanLiteral(_)
            | ast::Expr::NoneLiteral(_)
    )
}
