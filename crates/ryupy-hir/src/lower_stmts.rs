//! 语句 AST → HIR 降级（[`StmtLowerCtx`] 的语句侧方法）。
//!
//! 这里只做「语法形状 → HIR 节点」的搬运与去糖：
//! - 赋值目标解析（局部 / 模块变量 / 属性 / 下标 / 序列解包）；
//! - `try` 的四段结构原样进入 HIR（区域划分交给 MIR）；
//! - `assert` 去糖为 `if not cond: raise AssertionError(msg)`；
//! - 嵌套 `def` 的降级见 `closures.rs`（M3.0 闭包）。
//!
//! 表达式降级在 `expr.rs`，`with` 在 `with.rs`，控制流在 `control_flow.rs`。

use ryupy_base::{Diagnostic, Ranged, Span};
use ryupy_syntax::ast;

use crate::hir::{AssignTarget, Binding, Builtin, Expr, ExprKind, Name, Stmt, StmtId, StmtKind};
use crate::lower::StmtLowerCtx;

impl StmtLowerCtx<'_> {
    pub(crate) fn lower_stmt_to_root(&mut self, stmt: &ast::Stmt) {
        if matches!(stmt, ast::Stmt::Import(_) | ast::Stmt::ImportFrom(_)) {
            self.lower_import_to_root(stmt);
            return;
        }
        if let Some(stmt_id) = self.lower_stmt(stmt) {
            self.body.root.push(stmt_id);
        }
    }

    pub(crate) fn lower_suite(&mut self, stmts: &[ast::Stmt]) -> Vec<StmtId> {
        let mut out = Vec::new();
        for s in stmts {
            if matches!(s, ast::Stmt::Import(_) | ast::Stmt::ImportFrom(_)) {
                self.lower_import_into(s, &mut out);
                continue;
            }
            if let Some(id) = self.lower_stmt(s) {
                out.push(id);
            }
        }
        out
    }

    /// 序列解包目标：`a, b` / `first, *rest, last`。
    pub(crate) fn lower_sequence_target(
        &mut self,
        elements: &[ast::Expr],
        range: ryupy_base::TextRange,
    ) -> Option<Vec<crate::hir::SequenceItem>> {
        let mut items = Vec::new();
        for element in elements {
            match element {
                ast::Expr::Name(name_expr) => {
                    let name = Name::from(name_expr.id.as_str());
                    let binding = self.resolve_binding(&name)?;
                    items.push(crate::hir::SequenceItem::One(binding));
                }
                ast::Expr::Starred(starred) => {
                    let ast::Expr::Name(name_expr) = starred.value.as_ref() else {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, starred.range),
                                "星号解包的目标必须是简单变量",
                            )
                            .with_code("E0100")
                            .with_primary_label("暂不支持嵌套解包目标"),
                        );
                        return None;
                    };
                    let name = Name::from(name_expr.id.as_str());
                    let binding = self.resolve_binding(&name)?;
                    items.push(crate::hir::SequenceItem::Rest(binding));
                }
                _ => {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, element.range()),
                            "暂不支持嵌套的解包目标",
                        )
                        .with_code("E0100")
                        .with_primary_label("只支持变量与星号变量"),
                    );
                    return None;
                }
            }
        }
        if items
            .iter()
            .filter(|i| matches!(i, crate::hir::SequenceItem::Rest(_)))
            .count()
            > 1
        {
            self.diagnostics.push(
                Diagnostic::error(Span::new(self.file, range), "解包目标中只能有一个星号变量")
                    .with_code("E0100")
                    .with_primary_label("多个星号解包"),
            );
            return None;
        }
        Some(items)
    }

    /// `try` 语句：四段结构原样进入 HIR，交给 MIR 划分受保护区域。
    fn lower_try(&mut self, try_stmt: &ast::StmtTry) -> StmtId {
        if try_stmt.is_star {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, try_stmt.range),
                    "暂不支持 except* 异常组语法",
                )
                .with_code("E0100")
                .with_primary_label("异常组属于暂缓能力"),
            );
        }

        let body = self.lower_suite(&try_stmt.body);

        let mut handlers = Vec::new();
        for handler in &try_stmt.handlers {
            let ast::ExceptHandler::ExceptHandler(except) = handler;
            let types = match &except.type_ {
                None => None,
                Some(type_expr) => {
                    let names = self.exception_type_names(type_expr);
                    if names.is_empty() {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, type_expr.range()),
                                "except 目前只支持内建异常类型名（用户自定义异常在 M2.6 接续）",
                            )
                            .with_code("E0100")
                            .with_primary_label("不支持该异常类型表达式"),
                        );
                    }
                    Some(names)
                }
            };

            let name = except.name.as_ref().and_then(|ident| {
                let ident_name = Name::from(ident.as_str());
                self.resolve_binding(&ident_name).or_else(|| {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, ident.range),
                            format!("`{ident_name}` 未登记为变量"),
                        )
                        .with_code("E0101")
                        .with_primary_label("内部错误：异常变量预扫描遗漏"),
                    );
                    None
                })
            });

            // except 体里允许裸 `raise`
            self.handler_depth += 1;
            let handler_body = self.lower_suite(&except.body);
            self.handler_depth -= 1;

            let id = self.body.handlers.alloc(crate::hir::Handler {
                types,
                name,
                body: handler_body,
                range: except.range,
            });
            handlers.push(id);
        }

        let orelse = self.lower_suite(&try_stmt.orelse);
        let finalbody = self.lower_suite(&try_stmt.finalbody);

        self.body.stmts.alloc(Stmt {
            kind: StmtKind::Try {
                body,
                handlers,
                orelse,
                finalbody,
            },
            range: try_stmt.range,
            line: self.line_of(try_stmt.range.start().to_usize()),
        })
    }

    /// 把 `except` 的类型表达式解析成异常类型名列表。
    ///
    /// 支持 `except E:` 与 `except (E1, E2):`，其中每个名字可以是内建异常类，
    /// 也可以是**继承自 BaseException 的用户类**（M2.6）。返回空列表表示无法解析，
    /// 调用方报诊断。
    fn exception_type_names(&self, expr: &ast::Expr) -> Vec<String> {
        match expr {
            ast::Expr::Name(name) => {
                let ident = Name::from(name.id.as_str());
                match self.global_scope.names.get(&ident) {
                    // 用户类：必须位于 BaseException 家族，否则 `except X` 在 CPython
                    // 里是 TypeError，这里作为暂不支持的能力明确拒绝。
                    Some(crate::scope::Resolution::Class(cls_id)) => {
                        let cls = &self.classes[*cls_id];
                        if cls.is_exception {
                            vec![cls.name.as_str().to_owned()]
                        } else {
                            Vec::new()
                        }
                    }
                    Some(_) => Vec::new(),
                    None => match crate::scope::resolve_builtin(name.id.as_str()) {
                        Some(Builtin::Exception(type_name)) => {
                            vec![type_name.to_owned()]
                        }
                        _ => Vec::new(),
                    },
                }
            }
            ast::Expr::Tuple(tuple) => {
                let mut out = Vec::new();
                for elt in &tuple.elts {
                    out.extend(self.exception_type_names(elt));
                }
                out
            }
            _ => Vec::new(),
        }
    }

    pub(crate) fn lower_stmt(&mut self, stmt: &ast::Stmt) -> Option<StmtId> {
        let kind = match stmt {
            ast::Stmt::Expr(expr_stmt)
                if matches!(expr_stmt.value.as_ref(), ast::Expr::Yield(_)) =>
            {
                let ast::Expr::Yield(value) = expr_stmt.value.as_ref() else {
                    return None;
                };
                if self.module_scope {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, value.range),
                            "yield 只能出现在函数体内",
                        )
                        .with_code("E0100"),
                    );
                    return None;
                }
                StmtKind::Yield(value.value.as_deref().map(|value| self.lower_expr(value)))
            }
            ast::Stmt::Expr(expr_stmt) => {
                let expr_id = self.lower_expr(&expr_stmt.value);
                StmtKind::Expr(expr_id)
            }
            ast::Stmt::Assign(assign_stmt) => {
                let val = self.lower_expr(&assign_stmt.value);
                if assign_stmt.targets.len() == 1 {
                    let target = &assign_stmt.targets[0];
                    match target {
                        ast::Expr::Name(name_expr) => {
                            let name = Name::from(name_expr.id.as_str());
                            let target = match self.resolve_binding(&name) {
                                Some(Binding::Local(local)) => AssignTarget::Local(local),
                                Some(Binding::ModuleVar(var)) => AssignTarget::ModuleVar(var),
                                Some(Binding::Free(idx)) => AssignTarget::Free(idx),
                                None => {
                                    self.diagnostics.push(
                                        Diagnostic::error(
                                            Span::new(self.file, name_expr.range),
                                            format!("未定义的赋值目标 `{name}`"),
                                        )
                                        .with_code("E0106")
                                        .with_primary_label("名字未登记为局部变量或模块变量"),
                                    );
                                    return None;
                                }
                            };
                            StmtKind::Assign { target, value: val }
                        }
                        ast::Expr::Attribute(attr_expr) => {
                            let base = self.lower_expr(&attr_expr.value);
                            let name = Name::from(attr_expr.attr.as_str());
                            StmtKind::Assign {
                                target: AssignTarget::Attribute { base, name },
                                value: val,
                            }
                        }
                        ast::Expr::Subscript(sub) => {
                            let base = self.lower_expr(&sub.value);
                            let index = self.lower_subscript_index(&sub.slice);
                            StmtKind::Assign {
                                target: AssignTarget::Subscript { base, index },
                                value: val,
                            }
                        }
                        ast::Expr::Tuple(tuple) => {
                            let items = self.lower_sequence_target(&tuple.elts, tuple.range)?;
                            StmtKind::Assign {
                                target: AssignTarget::Sequence(items),
                                value: val,
                            }
                        }
                        ast::Expr::List(list) => {
                            let items = self.lower_sequence_target(&list.elts, list.range)?;
                            StmtKind::Assign {
                                target: AssignTarget::Sequence(items),
                                value: val,
                            }
                        }
                        _ => {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, target.range()),
                                    "暂不支持非简单变量或属性访问的赋值目标",
                                )
                                .with_code("E0100")
                                .with_primary_label("仅支持赋值给局部变量或属性"),
                            );
                            return None;
                        }
                    }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, assign_stmt.range()),
                            "暂不支持多元链式赋值",
                        )
                        .with_code("E0100")
                        .with_primary_label("不支持形如 a = b = 1 的链式赋值"),
                    );
                    return None;
                }
            }
            ast::Stmt::AnnAssign(ann_stmt) => {
                if let ast::Expr::Name(name_expr) = &*ann_stmt.target {
                    let name = Name::from(name_expr.id.as_str());
                    let ann = crate::operator::lower_type_ref(
                        &ann_stmt.annotation,
                        self.file,
                        self.global_scope,
                        self.diagnostics,
                    );
                    let target = match self.resolve_binding(&name) {
                        Some(Binding::Local(local)) => {
                            self.body.locals[local].annotation = Some(ann);
                            AssignTarget::Local(local)
                        }
                        Some(Binding::Free(idx)) => {
                            // nonlocal 名字上的标注：写入仍穿过外层 cell（标注本身不落存储）。
                            AssignTarget::Free(idx)
                        }
                        Some(Binding::ModuleVar(var)) => {
                            self.vars[var].annotation = Some(ann);
                            AssignTarget::ModuleVar(var)
                        }
                        None => return None,
                    };
                    let Some(val_expr) = &ann_stmt.value else {
                        return None;
                    };
                    let val = self.lower_expr(val_expr);
                    StmtKind::Assign { target, value: val }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, ann_stmt.target.range()),
                            "暂不支持带标注的非变量赋值",
                        )
                        .with_code("E0100")
                        .with_primary_label("目标必须是变量"),
                    );
                    return None;
                }
            }
            // 增量赋值的去糖与「目标只求值一次」见 aug_assign.rs
            ast::Stmt::AugAssign(aug_stmt) => return self.lower_aug_assign(aug_stmt),
            ast::Stmt::Return(ret_stmt) => {
                let val = ret_stmt.value.as_deref().map(|v| self.lower_expr(v));
                StmtKind::Return(val)
            }
            ast::Stmt::Pass(_) => StmtKind::Pass,
            ast::Stmt::Delete(del_stmt) => {
                let mut targets = Vec::new();
                for target in &del_stmt.targets {
                    match target {
                        // 删除名字需要模块命名空间（M2.7），这里只处理容器下标与属性。
                        ast::Expr::Subscript(sub) => {
                            let base = self.lower_expr(&sub.value);
                            let index = self.lower_subscript_index(&sub.slice);
                            let id = self.body.exprs.alloc(Expr {
                                kind: ExprKind::Subscript { base, index },
                                range: sub.range,
                            });
                            targets.push(id);
                        }
                        ast::Expr::Attribute(attr) => {
                            let base = self.lower_expr(&attr.value);
                            let name = Name::from(attr.attr.as_str());
                            let id = self.body.exprs.alloc(Expr {
                                kind: ExprKind::Attribute { base, name },
                                range: attr.range,
                            });
                            targets.push(id);
                        }
                        _ => {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, target.range()),
                                    "暂不支持该 del 目标",
                                )
                                .with_code("E0100")
                                .with_primary_label("目前只支持 del obj[...] 与 del obj.attr"),
                            );
                        }
                    }
                }
                StmtKind::Delete(targets)
            }
            ast::Stmt::Assert(assert_stmt) => {
                // `assert cond, msg` 去糖为 `if not cond: raise AssertionError(msg)`
                let cond = self.lower_expr(&assert_stmt.test);
                let not_cond = self.body.exprs.alloc(Expr {
                    kind: ExprKind::Unary {
                        op: crate::hir::UnaryOp::Not,
                        operand: cond,
                    },
                    range: assert_stmt.test.range(),
                });
                let raise_args = match &assert_stmt.msg {
                    Some(msg) => vec![self.lower_expr(msg)],
                    None => Vec::new(),
                };
                let callee = self.body.exprs.alloc(Expr {
                    kind: ExprKind::Builtin(crate::hir::Builtin::Exception("AssertionError")),
                    range: assert_stmt.range,
                });
                let call = self.body.exprs.alloc(Expr {
                    kind: ExprKind::Call {
                        callee,
                        args: raise_args
                            .into_iter()
                            .map(crate::hir::CallArg::Positional)
                            .collect(),
                    },
                    range: assert_stmt.range,
                });
                let raise_stmt = self.body.stmts.alloc(Stmt {
                    kind: StmtKind::Raise {
                        value: Some(call),
                        cause: None,
                    },
                    range: assert_stmt.range,
                    line: self.line_of(assert_stmt.range.start().to_usize()),
                });
                StmtKind::If {
                    cond: not_cond,
                    then_: vec![raise_stmt],
                    else_: Vec::new(),
                }
            }
            ast::Stmt::Raise(raise_stmt) => {
                let value = raise_stmt.exc.as_deref().map(|e| self.lower_expr(e));
                let cause = raise_stmt.cause.as_deref().map(|e| self.lower_expr(e));
                // 裸 `raise` 不在 lowering 阶段报错：Python 允许它出现在任何位置，
                // 没有正在处理的异常时在运行时抛 RuntimeError("No active exception to reraise")。
                // 编译期拒绝会误伤「异常处理函数里的裸 raise」这类常见写法。
                StmtKind::Raise { value, cause }
            }
            ast::Stmt::Try(try_stmt) => return Some(self.lower_try(try_stmt)),
            ast::Stmt::With(with_stmt) => return self.lower_with(with_stmt),
            ast::Stmt::If(if_stmt) => return Some(self.lower_if(if_stmt)),
            ast::Stmt::While(while_stmt) => {
                let cond = self.lower_expr(&while_stmt.test);
                self.loop_depth += 1;
                let body = self.lower_suite(&while_stmt.body);
                self.loop_depth -= 1;
                // `else` 不属于循环体：按当前 loop_depth 降级
                let orelse = self.lower_suite(&while_stmt.orelse);
                StmtKind::While { cond, body, orelse }
            }
            ast::Stmt::For(for_stmt) => {
                let kind = self.lower_for(for_stmt)?;
                let line = self.line_of(for_stmt.range.start().to_usize());
                return Some(self.body.stmts.alloc(Stmt {
                    kind,
                    range: for_stmt.range,
                    line,
                }));
            }
            ast::Stmt::Break(b) => {
                if self.loop_depth == 0 {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, b.range),
                            "'break' 只能在循环体内部使用",
                        )
                        .with_code("E0107")
                        .with_primary_label("循环外的 break"),
                    );
                }
                StmtKind::Break
            }
            ast::Stmt::Continue(c) => {
                if self.loop_depth == 0 {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, c.range),
                            "'continue' 只能在循环体内部使用",
                        )
                        .with_code("E0107")
                        .with_primary_label("循环外的 continue"),
                    );
                }
                StmtKind::Continue
            }
            // `global` 在预扫描阶段已经登记为模块变量，这里不需要运行时动作。
            ast::Stmt::Global(_) => return None,
            // `nonlocal` 与嵌套 `def` 由 closures.rs 接管（M3.0）。
            ast::Stmt::Nonlocal(n) => {
                self.lower_nonlocal(n);
                return None;
            }
            ast::Stmt::ClassDef(class) if self.module_scope => {
                let class_id = self
                    .classes
                    .iter()
                    .find_map(|(id, registered)| (registered.range == class.range).then_some(id));
                match class_id {
                    Some(id) => StmtKind::ClassDef(id),
                    None => {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, class.range),
                                "模块类定义未预登记",
                            )
                            .with_code("E0101"),
                        );
                        return None;
                    }
                }
            }
            ast::Stmt::FunctionDef(f) => {
                return self.lower_nested_def(f);
            }
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(Span::new(self.file, stmt.range()), "暂不支持该语句")
                        .with_code("E0100")
                        .with_primary_label("暂不支持此语法结构"),
                );
                return None;
            }
        };

        let range = stmt.range();
        let line = self.line_of(range.start().to_usize());
        Some(self.body.stmts.alloc(Stmt { kind, range, line }))
    }
}
