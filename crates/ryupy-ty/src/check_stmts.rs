//! 语句的静态检查与动态降级：赋值合流、控制流、异常和闭包绑定。

use crate::check::FunctionChecker;
use crate::infer::InferTy;
use crate::ty::Ty;
use ryupy_base::{Diagnostic, Span};
use ryupy_hir::{AssignTarget, ExprId, StmtKind};

impl FunctionChecker<'_> {
    pub(crate) fn check_stmt(&mut self, stmt_id: ryupy_hir::StmtId) {
        let stmt = &self.func.body.stmts[stmt_id];
        match &stmt.kind {
            // 嵌套 def：外层函数必为动态模式，正常不会走到这里；防御性降级。
            StmtKind::NestedDef { .. } => self.soft_demote = true,
            StmtKind::Expr(e) => {
                self.check_expr(*e);
            }
            StmtKind::Assign { target, value } => {
                let val_ty = self.check_expr(*value);
                match target {
                    AssignTarget::Free(_) => self.soft_demote = true,
                    AssignTarget::Local(loc_id) => {
                        let loc_infer = self.local_vars[*loc_id].clone();
                        // 异构赋值（`x = 1; x = "a"`）在 Python 里合法。静态推导拿不到
                        // 单一类型，就把变量改绑成 Dynamic 并让整函数降级。
                        if self
                            .infer
                            .unify(&loc_infer, &InferTy::Known(val_ty))
                            .is_err()
                        {
                            self.soft_demote = true;
                            self.infer.force(&loc_infer, Ty::Dynamic);
                        }
                    }
                    AssignTarget::ModuleVar(var) => {
                        // 模块级变量按槽位类型存放。值类型与槽位类型都能静态确定却不同，
                        // 才认为需要降级（Python 允许重绑定，静态槽位放不下另一种类型）。
                        let declared = self.module_vars.get(*var).cloned().unwrap_or(Ty::Dynamic);
                        let mismatch = !declared.is_dynamic()
                            && !val_ty.is_dynamic()
                            && declared != Ty::Error
                            && val_ty != Ty::Error
                            && declared != val_ty;
                        if mismatch {
                            self.soft_demote = true;
                        }
                    }
                    AssignTarget::Subscript { base, index } => {
                        self.check_expr(*base);
                        self.check_expr(*index);
                        self.soft_demote = true;
                    }
                    AssignTarget::Sequence(items) => {
                        for item in items {
                            let binding = match item {
                                ryupy_hir::SequenceItem::One(binding)
                                | ryupy_hir::SequenceItem::Rest(binding) => *binding,
                            };
                            match binding {
                                ryupy_hir::Binding::Free(_) => self.soft_demote = true,
                                ryupy_hir::Binding::Local(local) => {
                                    let loc_infer = self.local_vars[local].clone();
                                    if self
                                        .infer
                                        .unify(&loc_infer, &InferTy::Known(Ty::Dynamic))
                                        .is_err()
                                    {
                                        self.soft_demote = true;
                                        self.infer.force(&loc_infer, Ty::Dynamic);
                                    }
                                }
                                ryupy_hir::Binding::ModuleVar(_) => self.soft_demote = true,
                            }
                        }
                        self.soft_demote = true;
                    }
                    AssignTarget::Attribute { base, name } => {
                        let base_ty = self.check_expr(*base);
                        self.check_attribute_assign(
                            base_ty,
                            *base,
                            name.clone(),
                            val_ty,
                            stmt.range,
                        );
                    }
                }
            }
            StmtKind::Yield(value) => {
                if let Some(value) = value {
                    self.check_expr(*value);
                }
            }
            StmtKind::Return(opt_val) => {
                let ret_ty = match opt_val {
                    Some(e) => self.check_expr(*e),
                    None => Ty::None,
                };
                // 返回类型与标注不一致不再是错误：标注只是优化线索，整函数降级即可
                if self
                    .infer
                    .unify(
                        &InferTy::Known(self.sig.ret.clone()),
                        &InferTy::Known(ret_ty),
                    )
                    .is_err()
                {
                    self.soft_demote = true;
                }
            }
            StmtKind::Pass => {}
            StmtKind::If { cond, then_, else_ } => {
                self.check_cond(*cond);
                for &s in then_ {
                    self.check_stmt(s);
                }
                for &s in else_ {
                    self.check_stmt(s);
                }
            }
            StmtKind::While { cond, body, orelse } => {
                self.check_cond(*cond);
                for &s in body {
                    self.check_stmt(s);
                }
                for &s in orelse {
                    self.check_stmt(s);
                }
            }
            StmtKind::ForEach {
                var,
                iter,
                body,
                orelse,
            } => {
                // 通用迭代依赖容器协议，静态路径不展开
                self.check_expr(*iter);
                match var {
                    ryupy_hir::Binding::Free(_) => self.soft_demote = true,
                    ryupy_hir::Binding::Local(local) => {
                        let var_infer = self.local_vars[*local].clone();
                        let _ = self.infer.unify(&var_infer, &InferTy::Known(Ty::Dynamic));
                    }
                    ryupy_hir::Binding::ModuleVar(_) => {}
                }
                self.soft_demote = true;
                for &s in body {
                    self.check_stmt(s);
                }
                for &s in orelse {
                    self.check_stmt(s);
                }
            }
            StmtKind::ForRange {
                var,
                start,
                stop,
                step,
                body,
                orelse,
            } => {
                let start_ty = self.check_expr(*start);
                let stop_ty = self.check_expr(*stop);
                let step_ty = step.map(|s| self.check_expr(s)).unwrap_or(Ty::Int);

                self.expect_range_int(*start, &start_ty, "start");
                self.expect_range_int(*stop, &stop_ty, "stop");
                if let Some(s) = step {
                    self.expect_range_int(*s, &step_ty, "step");
                }

                match var {
                    ryupy_hir::Binding::Free(_) => self.soft_demote = true,
                    ryupy_hir::Binding::Local(local) => {
                        let var_ty = self.local_vars[*local].clone();
                        let _ = self
                            .infer
                            .unify(&var_ty, &crate::infer::InferTy::Known(Ty::Int));
                    }
                    // 模块级循环变量槽位类型由模块变量推导决定，这里不再约束。
                    ryupy_hir::Binding::ModuleVar(_) => {}
                }

                for &s in body {
                    self.check_stmt(s);
                }
                for &s in orelse {
                    self.check_stmt(s);
                }
            }
            // 默认值表达式的类型在 main 的检查开头统一处理（见 check_function）
            // 导入绑定都是动态值（模块对象 / 函数对象 / 任意属性），
            // 静态路径不展开；读到的类型统一是 Dynamic。
            StmtKind::Import { .. } | StmtKind::ImportFrom { .. } => {
                self.soft_demote = true;
            }
            StmtKind::FunctionDefaults(_) => {}
            StmtKind::Delete(targets) => {
                for &target in targets {
                    self.check_expr(target);
                }
            }
            StmtKind::Raise { value, cause } => {
                if let Some(v) = value {
                    self.check_expr(*v);
                }
                if let Some(c) = cause {
                    self.check_expr(*c);
                }
            }
            StmtKind::Try {
                body,
                handlers,
                orelse,
                finalbody,
            } => {
                for &s in body {
                    self.check_stmt(s);
                }
                for &handler_id in handlers {
                    let (name, handler_body) = {
                        let handler = &self.func.body.handlers[handler_id];
                        (handler.name, handler.body.clone())
                    };
                    // 异常对象是动态值：`as e` 的局部变量按 Dynamic 处理，
                    // 同时让整函数降级——异常对象上的属性（`args` / `__cause__`）
                    // 需要 M2.5 的属性协议，静态槽位放不下。
                    if let Some(binding) = name {
                        match binding {
                            ryupy_hir::Binding::Free(_) => self.soft_demote = true,
                            ryupy_hir::Binding::Local(local) => {
                                let loc_infer = self.local_vars[local].clone();
                                if self
                                    .infer
                                    .unify(&loc_infer, &InferTy::Known(Ty::Dynamic))
                                    .is_err()
                                {
                                    self.soft_demote = true;
                                    self.infer.force(&loc_infer, Ty::Dynamic);
                                }
                            }
                            ryupy_hir::Binding::ModuleVar(_) => self.soft_demote = true,
                        }
                    }
                    for s in handler_body {
                        self.check_stmt(s);
                    }
                }
                for &s in orelse {
                    self.check_stmt(s);
                }
                for &s in finalbody {
                    self.check_stmt(s);
                }
            }
            // 类体属性 / 表达式统一在模块入口的 `check_function` 尾部检查。
            StmtKind::ClassDef(_) => {}
            StmtKind::Break | StmtKind::Continue => {}
        }
    }

    /// `for x in range(...)` 的参数必须是整数；动态值交给运行时判定。
    fn expect_range_int(&mut self, expr: ExprId, ty: &Ty, label: &str) {
        if matches!(ty, Ty::Int | Ty::Bool | Ty::Dynamic | Ty::Error) {
            return;
        }
        self.diagnostics.push(
            Diagnostic::error(
                Span::new(self.file, self.func.body.exprs[expr].range),
                format!("range() 的 {label} 必须为 int，得到 `{ty:?}`"),
            )
            .with_code("E0203")
            .with_primary_label("期望 int 类型"),
        );
    }

    /// 真值判定接受一切类型（Python 里任何对象都有真值），因此不做检查。
    pub(crate) fn check_cond(&mut self, cond: ExprId) {
        let _ = self.check_expr(cond);
    }
}
