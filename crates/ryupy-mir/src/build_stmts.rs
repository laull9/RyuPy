//! 语句 HIR → MIR 降级（[`FunctionBuilder`] 的语句侧方法）。
//!
//! `build_stmt` 把每条 HIR 语句落到基本块上；赋值目标的三种形态
//! （局部 / cell / 模块变量 / 外层 cell）、`print` 的参数化展开与
//! import 的运行时调用都在这里。控制流语句的分派也在本文件，
//! 循环与 `try` 的具体构建分别在 `build_loops.rs` / `build_try.rs`。

use ryupy_hir::{AssignTarget, Binding, Builtin, ExprId, ExprKind, StmtId, StmtKind};
use ryupy_ty::{Ty, type_ref_to_ty};

use crate::build::{FunctionBuilder, ty_to_mir};
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement, Terminator};

impl FunctionBuilder<'_> {
    /// 调用运行时导入一个模块，返回动态值。
    fn import_value(&mut self, module: &str) -> Operand {
        let temp = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: temp,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ImportModule),
                args: vec![Operand::Const(Constant::Str(module.to_owned()))],
            },
        });
        Operand::Local(temp)
    }

    /// `import x` / `import a.b` / `import x as y`。
    fn build_import(&mut self, module: &str, bind: Binding, bind_root: bool) {
        let value = if bind_root {
            // `import a.b`：先把 a.b 初始化，再把顶层包 a 绑定到命名空间。
            let _ = self.import_value(module);
            let root = module.split('.').next().unwrap_or(module);
            self.import_value(root)
        } else {
            self.import_value(module)
        };
        self.store_import_binding(bind, value);
    }

    /// `from m import a, b as c`：逐个取属性，缺属性时运行时尝试导入子模块。
    fn build_import_from(&mut self, module: &str, names: &[(String, Binding)]) {
        for (name, bind) in names {
            let temp = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest: temp,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::ImportAttr),
                    args: vec![
                        Operand::Const(Constant::Str(module.to_owned())),
                        Operand::Const(Constant::Str(name.clone())),
                    ],
                },
            });
            self.store_import_binding(*bind, Operand::Local(temp));
        }
    }
    /// 导入调用可在写入前抛错，不能把成功路径的标记传播到 except 路径。
    /// 先前已赋值的局部失败时保留旧值；新导入局部保守保留运行时未绑定检查。
    fn store_import_binding(&mut self, binding: Binding, value: Operand) {
        let fresh = match binding {
            Binding::Local(local) if !self.assigned_locals.contains(&local) => Some(local),
            _ => None,
        };
        self.store_binding(binding, value, &MirType::Dynamic);
        if let Some(local) = fresh {
            self.assigned_locals.remove(&local);
        }
    }

    pub(crate) fn build_stmt(&mut self, stmt_id: StmtId) {
        if !self.is_current_unterminated() {
            // 当前基本块已终结（例如前一条语句是 return/break/continue）。
            // 只有当存在后续不可达语句时，才分配死代码块承载它。
            self.current_bb = self.new_block();
        }
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let stmt = &hir_body.stmts[stmt_id];
        let line = stmt.line;
        self.emit_line(line);
        match &stmt.kind {
            StmtKind::Yield(value) => {
                let value = match value {
                    Some(value) => {
                        let ty = self.expr_builder().ty_of(*value);
                        let value = self.expr_builder().build_expr(*value);
                        self.expr_builder().coerce(value, &ty, &MirType::Dynamic)
                    }
                    None => self.expr_builder().runtime_call(
                        RuntimeFn::DynNone,
                        Vec::new(),
                        MirType::Dynamic,
                    ),
                };
                let resume = self.new_block();
                self.terminate_current(Terminator::Suspend {
                    value,
                    resume,
                    input: None,
                    delegated: false,
                });
                self.current_bb = resume;
            }
            StmtKind::Expr(expr_id) => {
                self.build_expr_stmt(*expr_id);
            }
            StmtKind::Import {
                module,
                bind,
                bind_root,
            } => {
                let module = module.clone();
                let bind = *bind;
                let bind_root = *bind_root;
                self.build_import(&module, bind, bind_root);
            }
            StmtKind::ImportFrom { module, names } => {
                let module = module.clone();
                let names = names.clone();
                self.build_import_from(&module, &names);
            }
            StmtKind::Assign { target, value } => match target {
                AssignTarget::Subscript { base, index } => {
                    self.build_subscript_assign(*base, *index, *value);
                }
                AssignTarget::Sequence(items) => {
                    let items = items.clone();
                    self.build_sequence_assign(&items, *value);
                    self.mark_sequence_targets(&items);
                }
                _ => {
                    let val_ty = self.expr_builder().ty_of(*value);
                    let val_op = self.expr_builder().build_expr(*value);
                    match target {
                        AssignTarget::Local(loc) => {
                            let dest_ty = self.locals[self.local_map[loc]].ty.clone();
                            let coerced = self.expr_builder().coerce(val_op, &val_ty, &dest_ty);
                            self.assign_local(*loc, coerced, &dest_ty);
                        }
                        AssignTarget::Free(idx) => {
                            // nonlocal 写入（M3.0）：值穿过 env 里的外层 cell。
                            let boxed =
                                self.expr_builder()
                                    .coerce(val_op, &val_ty, &MirType::Dynamic);
                            let env = self.expr_builder().env_operand();
                            let cell = self.alloc_temp(MirType::Dynamic);
                            self.push_stmt(Statement::Assign {
                                dest: cell,
                                value: Rvalue::Call {
                                    callee: Callee::Runtime(RuntimeFn::ArrayGet),
                                    args: vec![env, Operand::Const(Constant::I64(*idx as i64))],
                                },
                            });
                            self.push_stmt(Statement::Eval(Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::CellSet),
                                args: vec![Operand::Local(cell), boxed],
                            }));
                        }
                        AssignTarget::ModuleVar(var) => {
                            let gid = self.module_vars[var];
                            let to = self.globals[gid].ty.clone();
                            let value = self.expr_builder().coerce(val_op, &val_ty, &to);
                            self.push_stmt(Statement::SetGlobal { dest: gid, value });
                        }
                        AssignTarget::Attribute { base, name } => {
                            let base_ty = self.expr_builder().ty_of(*base);
                            let base_op = self.expr_builder().build_expr(*base);
                            let base_op = if matches!(base_ty, Ty::Class(_)) {
                                self.expr_builder().as_local(base_op)
                            } else {
                                base_op
                            };
                            if let Ty::Class(cls_id) = base_ty {
                                let cls = &self.hir.classes[cls_id];
                                if let Some((field_idx, field)) =
                                    cls.fields.iter().enumerate().find(|(_, f)| f.name == *name)
                                {
                                    let field_ty = ty_to_mir(&type_ref_to_ty(&field.annotation));
                                    let value =
                                        self.expr_builder().coerce(val_op, &val_ty, &field_ty);
                                    self.push_stmt(Statement::SetField {
                                        obj: base_op,
                                        field: field_idx as u32,
                                        value,
                                    });
                                }
                            } else {
                                // 普通实例 / 类的动态属性写入（M2.5）：统一交给运行时，
                                // 它负责实例字典、property setter 与 AttributeError。
                                let value =
                                    self.expr_builder()
                                        .coerce(val_op, &val_ty, &MirType::Dynamic);
                                self.push_stmt(Statement::Eval(Rvalue::Call {
                                    callee: Callee::Runtime(RuntimeFn::SetAttr),
                                    args: vec![
                                        base_op,
                                        Operand::Const(Constant::Str(name.as_str().to_owned())),
                                        value,
                                    ],
                                }));
                            }
                        }
                        AssignTarget::Subscript { .. } | AssignTarget::Sequence(_) => {}
                    }
                }
            },
            StmtKind::Return(opt_expr) => {
                self.build_return(*opt_expr);
            }
            StmtKind::If { cond, then_, else_ } => {
                let cond_raw = self.expr_builder().build_expr(*cond);
                let cond_ty = self.expr_builder().ty_of(*cond);
                let cond_bool = self.expr_builder().to_bool_operand(cond_raw, &cond_ty);

                let then_bb = self.new_block();
                let else_bb = self.new_block();
                let join_bb = self.new_block();

                self.terminate_current(Terminator::Branch {
                    cond: cond_bool,
                    then_: then_bb,
                    else_: else_bb,
                });

                // 动态函数的分支必须从同一确定赋值状态出发，汇合只保留交集。
                // 否则then里的赋值会误让else/join跳过LoadChecked，未赋值被静默当None。
                // 静态函数的既有编译期诊断策略不在本次窄修范围。
                let assigned_before = self
                    .types
                    .is_dynamic(self.current_hir_fn)
                    .then(|| self.assigned_locals.clone());
                // then 分支
                self.current_bb = then_bb;
                for &s in then_ {
                    self.build_stmt(s);
                }
                if self.is_current_unterminated() {
                    self.terminate_current(Terminator::Goto(join_bb));
                }

                let then_assigned = assigned_before
                    .as_ref()
                    .map(|before| std::mem::replace(&mut self.assigned_locals, before.clone()));
                // else 分支
                self.current_bb = else_bb;
                for &s in else_ {
                    self.build_stmt(s);
                }
                if self.is_current_unterminated() {
                    self.terminate_current(Terminator::Goto(join_bb));
                }

                if let Some(then_assigned) = then_assigned {
                    self.assigned_locals
                        .retain(|local| then_assigned.contains(local));
                }
                self.current_bb = join_bb;
            }
            StmtKind::While { cond, body, orelse } => {
                let cond_bb = self.new_block();
                let body_bb = self.new_block();
                let else_bb = self.new_block();
                let exit_bb = self.new_block();

                self.terminate_current(Terminator::Goto(cond_bb));

                // 循环条件块：条件变假 → else（正常结束）
                self.current_bb = cond_bb;
                let cond_raw = self.expr_builder().build_expr(*cond);
                let cond_ty = self.expr_builder().ty_of(*cond);
                let cond_bool = self.expr_builder().to_bool_operand(cond_raw, &cond_ty);
                self.terminate_current(Terminator::Branch {
                    cond: cond_bool,
                    then_: body_bb,
                    else_: else_bb,
                });

                // 循环体：break 直接跳 exit，跳过 else
                self.current_bb = body_bb;
                self.loop_stack.push((cond_bb, exit_bb));
                for &s in body {
                    self.build_stmt(s);
                }
                self.loop_stack.pop();
                if self.is_current_unterminated() {
                    self.terminate_current(Terminator::Goto(cond_bb));
                }

                // else 体
                self.current_bb = else_bb;
                for &s in orelse {
                    self.build_stmt(s);
                }
                if self.is_current_unterminated() {
                    self.terminate_current(Terminator::Goto(exit_bb));
                }
                self.current_bb = exit_bb;
            }
            StmtKind::Break => {
                let (_, break_bb) = *self
                    .loop_stack
                    .last()
                    .expect("break 语句在 HIR 降级阶段已做循环作用域检查");
                // 跳出可能要穿过 finally：由 route_jump 决定直接跳还是走状态机
                self.route_jump(break_bb);
            }
            StmtKind::Continue => {
                let (continue_bb, _) = *self
                    .loop_stack
                    .last()
                    .expect("continue 语句在 HIR 降级阶段已做循环作用域检查");
                self.route_jump(continue_bb);
            }
            StmtKind::FunctionDefaults(fn_id) => {
                self.emit_function_defaults(*fn_id);
                if let Some(&global) = self.fn_globals.get(fn_id) {
                    self.emit_function_metadata(*fn_id, Operand::Global(global));
                }
            }
            StmtKind::NestedDef { func, target } => {
                self.build_decorated_def(*func, target, &[]);
            }
            StmtKind::DecoratedDef {
                func,
                target,
                decorators,
            } => {
                self.build_decorated_def(*func, target, &decorators.clone());
            }
            StmtKind::ForRange {
                var,
                start,
                stop,
                step,
                body,
                orelse,
            } => {
                self.build_for_range(*var, *start, *stop, *step, body, orelse);
            }
            StmtKind::ForEach {
                var,
                iter,
                body,
                orelse,
            } => {
                self.build_for_each(*var, *iter, body, orelse);
            }
            StmtKind::Raise { value, cause } => {
                // 裸 raise 取运行时「当前异常」；显式 raise 先准备（挂上下文 + 记栈帧）。
                let value_op = match value {
                    Some(expr) => {
                        let ty = self.expr_builder().ty_of(*expr);
                        let raw = self.expr_builder().build_expr(*expr);
                        let boxed = self.expr_builder().coerce(raw, &ty, &MirType::Dynamic);
                        let dest = self.alloc_temp(MirType::Dynamic);
                        self.push_stmt(Statement::Assign {
                            dest,
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::RaisePrepare),
                                args: vec![boxed],
                            },
                        });
                        Operand::Local(dest)
                    }
                    None => {
                        let dest = self.alloc_temp(MirType::Dynamic);
                        self.push_stmt(Statement::Assign {
                            dest,
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::Reraise),
                                args: Vec::new(),
                            },
                        });
                        Operand::Local(dest)
                    }
                };
                let cause_op = cause.map(|e| {
                    let ty = self.expr_builder().ty_of(e);
                    let raw = self.expr_builder().build_expr(e);
                    self.expr_builder().coerce(raw, &ty, &MirType::Dynamic)
                });
                self.terminate_current(Terminator::Raise {
                    value: Some(value_op),
                    cause: cause_op,
                });
            }
            StmtKind::Try {
                body,
                handlers,
                orelse,
                finalbody,
            } => {
                self.build_try(body, handlers, orelse, finalbody);
            }
            StmtKind::Delete(targets) => {
                for &target in targets {
                    self.build_delete(target);
                }
            }
            StmtKind::ClassDef(cls_id) => {
                self.build_class_def(*cls_id);
            }
            StmtKind::Pass => {}
        }
    }

    fn build_expr_stmt(&mut self, expr_id: ExprId) {
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let expr = &hir_body.exprs[expr_id];
        if let ExprKind::Call { callee, args } = &expr.kind {
            let callee_expr = &hir_body.exprs[*callee];
            if matches!(callee_expr.kind, ExprKind::Builtin(Builtin::Print))
                && args
                    .iter()
                    .all(|arg| matches!(arg, ryupy_hir::CallArg::Positional(_)))
            {
                self.build_print_fast(args);
                return;
            }
        }
        let _ = self.expr_builder().build_expr(expr_id);
    }
}
