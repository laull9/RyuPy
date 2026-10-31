//! `for ... in range(...)` 的 MIR 构建。
//!
//! 单独成文件的原因：计数循环的构建代码量最大（正负步长、动态边界拆箱、
//! 常量步长的正负比较方向），放在 `build.rs` 里会让主构建流程难以阅读。
//!
//! 两条路径：
//! - 静态函数：`range` 的三个操作数是 i64，循环变量也是 i64，直接用 CIL 比较指令；
//! - 动态函数：边界先拆箱成 i64（`PyCast`），循环变量在每轮开头装箱写进用户变量，
//!   这样既保留计数循环的快路径，又让循环体里的 `i` 是动态值。

use ryupy_base::{Diagnostic, Span};
use ryupy_hir::{ExprId, LocalId as HirLocalId, StmtId};
use ryupy_ty::Ty;

use crate::build::FunctionBuilder;
use crate::mir::{
    BinOp, Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement, Terminator, UnaryOp,
};

impl FunctionBuilder<'_> {
    /// `for var in <可迭代对象>`：走运行时迭代协议。
    ///
    /// 结构是「条件块 → 循环体 → 步进块」，与计数循环同形，因此 `break` / `continue`
    /// 可以复用同一套循环目标栈（`continue` 跳到步进块，`break` 跳到退出块）。
    pub(crate) fn build_for_each(&mut self, var: HirLocalId, iter: ExprId, body: &[StmtId]) {
        let iter_op = self.expr_builder().build_expr(iter);
        let iter_ty = self.expr_builder().ty_of(iter);
        let source = self
            .expr_builder()
            .coerce(iter_op, &iter_ty, &MirType::Dynamic);
        let iterator = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: iterator,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::IterStart),
                args: vec![source],
            },
        });

        let cond_bb = self.new_block();
        let body_bb = self.new_block();
        let step_bb = self.new_block();
        let exit_bb = self.new_block();
        self.terminate_current(Terminator::Goto(cond_bb));

        // 条件：还有下一个元素吗
        self.current_bb = cond_bb;
        let ok = self.alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: ok,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::IterNextOk),
                args: vec![Operand::Local(iterator)],
            },
        });
        self.terminate_current(Terminator::Branch {
            cond: Operand::Local(ok),
            then_: body_bb,
            else_: exit_bb,
        });

        // 循环体：先把当前元素写进循环变量
        self.current_bb = body_bb;
        let value = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: value,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::IterValue),
                args: vec![Operand::Local(iterator)],
            },
        });
        let var_mir = self.local_map[&var];
        self.push_stmt(Statement::Assign {
            dest: var_mir,
            value: Rvalue::Use(Operand::Local(value)),
        });
        self.assigned_locals.insert(var);

        self.loop_stack.push((step_bb, exit_bb));
        for &stmt in body {
            self.build_stmt(stmt);
        }
        self.loop_stack.pop();
        if self.is_current_unterminated() {
            self.terminate_current(Terminator::Goto(step_bb));
        }

        // 步进块：迭代器自身在条件块里推进，这里只是跳回去
        self.current_bb = step_bb;
        self.terminate_current(Terminator::Goto(cond_bb));
        self.current_bb = exit_bb;
    }

    /// 动态函数里的 `for x in range(...)`：边界先拆箱成 i64，循环变量仍是动态值。
    ///
    /// 好处是保留计数循环的快路径，不让 `range` 退化成迭代器；代价是边界必须是真整数，
    /// 与 CPython 的 `TypeError: 'float' object cannot be interpreted as an integer` 一致。
    pub(crate) fn build_for_range_dynamic(
        &mut self,
        var: HirLocalId,
        start: ExprId,
        stop: ExprId,
        step: Option<ExprId>,
        body: &[StmtId],
    ) {
        let start_op = self.expr_builder().build_expr(start);
        let stop_op = self.expr_builder().build_expr(stop);
        let step_op = match step {
            Some(s) => self.expr_builder().build_expr(s),
            None => Operand::Const(Constant::I64(1)),
        };
        let start_ty = self.expr_builder().ty_of(start);
        let stop_ty = self.expr_builder().ty_of(stop);
        let step_ty = step
            .map(|s| self.expr_builder().ty_of(s))
            .unwrap_or(Ty::Int);

        // 边界拆箱到 i64；步长额外检查 0（Python 在 range 构造时抛 ValueError）
        let start_i = self
            .expr_builder()
            .coerce(start_op, &start_ty, &MirType::I64);
        let stop_i = self.expr_builder().coerce(stop_op, &stop_ty, &MirType::I64);
        let step_i = if step.is_some() {
            let raw = self
                .expr_builder()
                .coerce(step_op, &step_ty, &MirType::Dynamic);
            let dest = self.expr_builder().alloc_temp(MirType::I64);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::RangeStep),
                    args: vec![raw],
                },
            });
            Operand::Local(dest)
        } else {
            Operand::Const(Constant::I64(1))
        };

        // counter = start；循环变量在每轮开头装箱赋值
        let counter = self.expr_builder().alloc_temp(MirType::I64);
        self.push_stmt(Statement::Assign {
            dest: counter,
            value: Rvalue::Use(start_i),
        });
        let stop_local = self.expr_builder().alloc_temp(MirType::I64);
        self.push_stmt(Statement::Assign {
            dest: stop_local,
            value: Rvalue::Use(stop_i),
        });
        let step_local = self.expr_builder().alloc_temp(MirType::I64);
        self.push_stmt(Statement::Assign {
            dest: step_local,
            value: Rvalue::Use(step_i),
        });

        let var_mir = self.local_map[&var];
        self.assigned_locals.insert(var);

        let cond_bb = self.new_block();
        let body_bb = self.new_block();
        let step_bb = self.new_block();
        let exit_bb = self.new_block();
        self.terminate_current(Terminator::Goto(cond_bb));

        // cond: (step > 0 && counter < stop) || (step < 0 && counter > stop)
        self.current_bb = cond_bb;
        let step_pos = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: step_pos,
            value: Rvalue::Binary {
                op: BinOp::Gt,
                lhs: Operand::Local(step_local),
                rhs: Operand::Const(Constant::I64(0)),
            },
        });
        let lt = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: lt,
            value: Rvalue::Binary {
                op: BinOp::Lt,
                lhs: Operand::Local(counter),
                rhs: Operand::Local(stop_local),
            },
        });
        let gt = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: gt,
            value: Rvalue::Binary {
                op: BinOp::Gt,
                lhs: Operand::Local(counter),
                rhs: Operand::Local(stop_local),
            },
        });
        let positive = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: positive,
            value: Rvalue::Binary {
                op: BinOp::BitAnd,
                lhs: Operand::Local(step_pos),
                rhs: Operand::Local(lt),
            },
        });
        let negative_step = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: negative_step,
            value: Rvalue::Unary {
                op: UnaryOp::Not,
                operand: Operand::Local(step_pos),
            },
        });
        let negative = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: negative,
            value: Rvalue::Binary {
                op: BinOp::BitAnd,
                lhs: Operand::Local(negative_step),
                rhs: Operand::Local(gt),
            },
        });
        let cond = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: cond,
            value: Rvalue::Binary {
                op: BinOp::BitOr,
                lhs: Operand::Local(positive),
                rhs: Operand::Local(negative),
            },
        });
        self.terminate_current(Terminator::Branch {
            cond: Operand::Local(cond),
            then_: body_bb,
            else_: exit_bb,
        });

        // 循环体：先把 counter 装箱写进用户变量
        self.current_bb = body_bb;
        let boxed = self.expr_builder().alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: boxed,
            value: Rvalue::Box {
                value: Operand::Local(counter),
                from: MirType::I64,
            },
        });
        self.push_stmt(Statement::Assign {
            dest: var_mir,
            value: Rvalue::Use(Operand::Local(boxed)),
        });
        self.loop_stack.push((step_bb, exit_bb));
        for &s in body {
            self.build_stmt(s);
        }
        self.loop_stack.pop();
        if self.is_current_unterminated() {
            self.terminate_current(Terminator::Goto(step_bb));
        }

        // 步长块
        self.current_bb = step_bb;
        self.push_stmt(Statement::Assign {
            dest: counter,
            value: Rvalue::Binary {
                op: BinOp::Add,
                lhs: Operand::Local(counter),
                rhs: Operand::Local(step_local),
            },
        });
        self.terminate_current(Terminator::Goto(cond_bb));

        self.current_bb = exit_bb;
    }

    pub(crate) fn build_for_range(
        &mut self,
        var: HirLocalId,
        start: ExprId,
        stop: ExprId,
        step: Option<ExprId>,
        body: &[StmtId],
    ) {
        if self.types.is_dynamic(self.current_hir_fn) {
            self.build_for_range_dynamic(var, start, stop, step, body);
            return;
        }

        // 1. 求值起始、终止和步长操作数
        let start_op = self.expr_builder().build_expr(start);
        let stop_op = self.expr_builder().build_expr(stop);
        let step_op = match step {
            Some(s) => self.expr_builder().build_expr(s),
            None => Operand::Const(Constant::I64(1)),
        };

        // 步长为常量 0 时，编译期报错（语义陷阱 #9）
        if let Operand::Const(Constant::I64(0)) = step_op {
            let range = self.hir.functions[self.current_hir_fn].body.exprs[step.unwrap()].range;
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.hir.file, range),
                    "range() arg 3 must not be zero",
                )
                .with_code("E0302")
                .with_primary_label("步长不能为 0"),
            );
        }

        let var_mir = self.local_map[&var];
        self.assigned_locals.insert(var);

        // var = start
        self.push_stmt(Statement::Assign {
            dest: var_mir,
            value: Rvalue::Use(start_op),
        });

        // 2. 基本块创建
        let cond_bb = self.new_block();
        let body_bb = self.new_block();
        let step_bb = self.new_block();
        let exit_bb = self.new_block();

        self.terminate_current(Terminator::Goto(cond_bb));

        // 3. 条件块 (cond_bb)
        self.current_bb = cond_bb;
        // 条件判断：
        // 若 step 为常量 > 0：var < stop
        // 若 step 为常量 < 0：var > stop
        // 若 step 为非常量：(step > 0 && var < stop) || (step < 0 && var > stop)
        let cond_local = self.expr_builder().alloc_temp(MirType::Bool);
        match step_op {
            Operand::Const(Constant::I64(s)) if s > 0 => {
                self.push_stmt(Statement::Assign {
                    dest: cond_local,
                    value: Rvalue::Binary {
                        op: BinOp::Lt,
                        lhs: Operand::Local(var_mir),
                        rhs: stop_op,
                    },
                });
            }
            Operand::Const(Constant::I64(s)) if s < 0 => {
                self.push_stmt(Statement::Assign {
                    dest: cond_local,
                    value: Rvalue::Binary {
                        op: BinOp::Gt,
                        lhs: Operand::Local(var_mir),
                        rhs: stop_op,
                    },
                });
            }
            _ => {
                // 动态步长：判断 step > 0 ? var < stop : var > stop
                let is_pos_temp = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: is_pos_temp,
                    value: Rvalue::Binary {
                        op: BinOp::Gt,
                        lhs: step_op.clone(),
                        rhs: Operand::Const(Constant::I64(0)),
                    },
                });
                let cond_lt = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: cond_lt,
                    value: Rvalue::Binary {
                        op: BinOp::Lt,
                        lhs: Operand::Local(var_mir),
                        rhs: stop_op.clone(),
                    },
                });
                let cond_gt = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: cond_gt,
                    value: Rvalue::Binary {
                        op: BinOp::Gt,
                        lhs: Operand::Local(var_mir),
                        rhs: stop_op,
                    },
                });
                let pos_cond = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: pos_cond,
                    value: Rvalue::Binary {
                        op: BinOp::BitAnd,
                        lhs: Operand::Local(is_pos_temp),
                        rhs: Operand::Local(cond_lt),
                    },
                });
                let not_pos = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: not_pos,
                    value: Rvalue::Unary {
                        op: UnaryOp::Not,
                        operand: Operand::Local(is_pos_temp),
                    },
                });
                let neg_cond = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: neg_cond,
                    value: Rvalue::Binary {
                        op: BinOp::BitAnd,
                        lhs: Operand::Local(not_pos),
                        rhs: Operand::Local(cond_gt),
                    },
                });
                self.push_stmt(Statement::Assign {
                    dest: cond_local,
                    value: Rvalue::Binary {
                        op: BinOp::BitOr,
                        lhs: Operand::Local(pos_cond),
                        rhs: Operand::Local(neg_cond),
                    },
                });
            }
        }

        self.terminate_current(Terminator::Branch {
            cond: Operand::Local(cond_local),
            then_: body_bb,
            else_: exit_bb,
        });

        // 4. 循环体 (body_bb)
        self.current_bb = body_bb;
        // 关键：continue 跳到 step_bb，break 跳到 exit_bb
        self.loop_stack.push((step_bb, exit_bb));
        for &s in body {
            self.build_stmt(s);
        }
        self.loop_stack.pop();
        if self.is_current_unterminated() {
            self.terminate_current(Terminator::Goto(step_bb));
        }

        // 5. 步长递增块 (step_bb)
        self.current_bb = step_bb;
        self.push_stmt(Statement::Assign {
            dest: var_mir,
            value: Rvalue::Binary {
                op: BinOp::Add,
                lhs: Operand::Local(var_mir),
                rhs: step_op,
            },
        });
        self.terminate_current(Terminator::Goto(cond_bb));

        // 6. 退出块 (exit_bb)
        self.current_bb = exit_bb;
    }
}
