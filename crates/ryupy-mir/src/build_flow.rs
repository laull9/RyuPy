//! 函数收尾与跨 `finally` 的控制流辅助（[`FunctionBuilder`] 的流侧方法）。
//!
//! - traceback 栈帧的压入 / 恢复（异常展开时由处理入口补弹）；
//! - `finally` 状态机：动作码登记、状态写入、跳出路由；
//! - `return` 的统一出口：所有返回路径汇聚到 epilogue，弹帧逻辑只写一份；
//! - 块下标比较（判断跳转是否离开受保护区域）。

use ryupy_hir::{Binding, ExprId};

use crate::build::{FunctionBuilder, ty_to_mir};
use crate::mir::{
    BinOp, BlockId, Callee, Constant, LocalDecl, LocalId as MirLocalId, MirType, Operand,
    RuntimeFn, Rvalue, Statement, Terminator,
};

/// 一个正在构建的 `finally` 区域。
pub(crate) struct FinallyCtx {
    /// 需要在 finally 体执行完后接续的块。
    ///
    /// 存「待回填的源块」而不是「finally 入口块」，是因为入口块只能在区域窗口关闭之后
    /// 分配（窗口内必须是受保护的代码），而构建期间还不知道它。
    pub(crate) pending: Vec<BlockId>,
    /// 区域起始块下标，用于判断跳出目标是否离开了本区域。
    pub(crate) start: usize,
}

/// `finally` 分派块里的内建动作码。
pub(crate) const FLOW_NORMAL: i64 = 0;
pub(crate) const FLOW_EXCEPTION: i64 = 1;
pub(crate) const FLOW_RETURN: i64 = 2;
/// 自定义动作（break / continue / 跳出）从 3 开始。
pub(crate) const FLOW_FIRST_CUSTOM: i64 = 3;

impl FunctionBuilder<'_> {
    /// 函数入口压入 traceback 栈帧，深度存进局部变量。
    ///
    /// 深度用于两个地方：正常返回时 `PopTo(depth)` 弹掉自己的帧；异常被兜住时
    /// 用depth+1只弹掉被展开的被调用者帧，保留当前帧。
    pub(crate) fn emit_frame_push(
        &mut self,
        source: crate::GlobalId,
        signature: Option<crate::GlobalId>,
    ) {
        // 模块顶层函数在 traceback 里显示为 `<module>`，与 CPython 一致
        let func = &self.hir.functions[self.current_hir_fn];
        let fn_name = if matches!(func.kind, ryupy_hir::FunctionKind::ModuleMain) {
            "<module>".to_string()
        } else {
            func.name.as_str().to_string()
        };
        let origin = self.frame_origin(source, signature);
        let mut args = vec![
            Operand::Const(Constant::Str(self.hir.info.file.clone())),
            Operand::Const(Constant::Str(fn_name)),
        ];
        let runtime = if let Some(origin) = origin {
            args.push(origin);
            RuntimeFn::TracePushWithOrigin
        } else {
            RuntimeFn::TracePush
        };
        let depth = self.alloc_temp(MirType::I64);
        self.push_stmt(Statement::Assign {
            dest: depth,
            value: Rvalue::Call {
                callee: Callee::Runtime(runtime),
                args,
            },
        });
        self.frame_depth = Some(depth);
    }

    /// 把 traceback 栈弹回本函数的深度。
    ///
    /// 异常穿过调用链时，被穿过的函数不会执行自己的返回路径，帧会残留在栈里。
    /// 处理入口（catch / finally 通道）必须补一次弹栈，否则后续的递归深度判断会失真。
    pub(crate) fn emit_frame_restore(&mut self) {
        let Some(depth) = self.frame_depth else {
            return;
        };
        // 生成器每次恢复的帧由运行时Retry管理；原降级本来也会删除这里的PopTo。
        // 提前跳过可避免新增无用的跨暂停局部及读取已被移除的TracePush结果。
        if self.hir.functions[self.current_hir_fn].is_generator {
            return;
        }
        let keep_current = self.alloc_temp(MirType::I64);
        self.push_stmt(Statement::Assign {
            dest: keep_current,
            value: Rvalue::Binary {
                op: BinOp::Add,
                lhs: Operand::Local(depth),
                rhs: Operand::Const(Constant::I64(1)),
            },
        });
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::TracePopTo),
            args: vec![Operand::Local(keep_current)],
        }));
    }

    /// 记录语句行号（只在有帧记录时有效，但总是发射，便于诊断调试产物）。
    pub(crate) fn emit_line(&mut self, line: u32) {
        self.push_stmt(Statement::Line(line));
    }

    /// 每层 finally 独立保存退出原因，不能让内层正常落下覆盖外层待完成return/异常。
    pub(crate) fn flow_kind_local(&mut self) -> MirLocalId {
        let local = self.alloc_temp(MirType::I64);
        self.flow_kind = Some(local);
        local
    }

    /// 写状态码。
    pub(crate) fn set_flow(&mut self, code: i64) {
        let Some(flow) = self.flow_kind else { return };
        self.push_stmt(Statement::Assign {
            dest: flow,
            value: Rvalue::Use(Operand::Const(Constant::I64(code))),
        });
    }

    /// 把跳出目标登记进函数级动作表，返回状态码（相同目标复用同一个码）。
    pub(crate) fn intern_action(&mut self, target: BlockId) -> i64 {
        if let Some(idx) = self.actions.iter().position(|t| *t == target) {
            return FLOW_FIRST_CUSTOM + idx as i64;
        }
        self.actions.push(target);
        FLOW_FIRST_CUSTOM + (self.actions.len() - 1) as i64
    }

    /// 分配临时局部变量。
    pub(crate) fn alloc_temp(&mut self, ty: MirType) -> MirLocalId {
        self.locals.alloc(LocalDecl {
            ty,
            name: None,
            is_cell: false,
        })
    }

    /// 分配 `break` / `continue` / 普通跳出用的目标，并在需要时穿过 finally。
    pub(crate) fn route_jump(&mut self, target: BlockId) {
        if let Some(ctx) = self.finally_stack.last() {
            if self.block_index(target) < ctx.start {
                let code = self.intern_action(target);
                self.set_flow(code);
                let bb = self.current_bb;
                self.finally_stack
                    .last_mut()
                    .expect("刚检查过非空")
                    .pending
                    .push(bb);
                self.start_dead_block();
                return;
            }
        }
        self.terminate_current(Terminator::Goto(target));
    }

    /// 「目标待回填」之后必须换一个块继续构建。
    ///
    /// 为什么不能留原地：`build_stmt` 用「当前块是否已终结」判断上一条语句是否正常落下，
    /// 待回填的块此刻还没有终结符，后续语句会被错误地接到它后面。新块没有前驱，
    /// 会被 `simplify_cfg` 当成死代码清掉。
    fn start_dead_block(&mut self) {
        self.current_bb = self.new_block();
    }

    /// 把一批「目标待定」的块回填成 `Goto(target)`。
    pub(crate) fn resolve_jumps(&mut self, pending: Vec<BlockId>, target: BlockId) {
        for bb in pending {
            if matches!(self.blocks[bb].terminator, Terminator::Unreachable) {
                self.blocks[bb].terminator = Terminator::Goto(target);
            }
        }
    }

    /// 当前块「正常结束」：有 finally 时写 NORMAL 状态并交给它，否则记进 `pending`。
    pub(crate) fn route_normal(&mut self, pending: &mut Vec<BlockId>) {
        if self.finally_stack.is_empty() {
            pending.push(self.current_bb);
        } else {
            self.set_flow(FLOW_NORMAL);
            let bb = self.current_bb;
            self.finally_stack
                .last_mut()
                .expect("刚检查过非空")
                .pending
                .push(bb);
        }
    }

    /// `return`：写返回值到统一槽位，再走 finally 或直接去尾块。
    pub(crate) fn build_return(&mut self, value: Option<ExprId>) {
        let ret_ty = ty_to_mir(&self.types.sigs[self.current_hir_fn].ret);
        let retval = self.retval_local();
        if let Some(dest) = retval {
            let op = match value {
                Some(expr) => {
                    let value_ty = self.expr_builder().ty_of(expr);
                    let raw = self.expr_builder().build_expr(expr);
                    self.expr_builder().coerce(raw, &value_ty, &ret_ty)
                }
                None => Operand::Const(Constant::I64(0)),
            };
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::Use(op),
            });
        }
        if !self.finally_stack.is_empty() {
            self.set_flow(FLOW_RETURN);
            let bb = self.current_bb;
            self.finally_stack
                .last_mut()
                .expect("刚检查过非空")
                .pending
                .push(bb);
        } else {
            let bb = self.current_bb;
            self.epilogue_jumps.push(bb);
        }
        self.start_dead_block();
    }

    /// 返回槽位；返回类型是 `None` 时不需要槽位。
    pub(crate) fn retval_local(&mut self) -> Option<MirLocalId> {
        if matches!(self.ret_ty, MirType::Unit) {
            return None;
        }
        if self.retval.is_none() {
            let local = self.alloc_temp(self.ret_ty.clone());
            self.retval = Some(local);
        }
        self.retval
    }

    /// 函数收尾：隐式返回 + 统一返回块（负责弹帧与 `ret`）。
    ///
    /// 所有 `return` 都汇聚到一个尾块，好处是「弹帧」只有一个出口，
    /// 不用担心某条返回路径漏掉 `PopTo`。
    pub(crate) fn finish_function(&mut self) {
        // 有返回值的函数必须有一个返回槽位，哪怕所有路径都只是 `raise`：
        // 尾块要读它，而尾块可能因为不可达被跳过，所以这里只负责「存在」。
        if !matches!(self.ret_ty, MirType::Unit) && self.retval.is_none() {
            let ty = self.ret_ty.clone();
            self.retval = Some(self.alloc_temp(ty));
        }
        if self.is_current_unterminated() {
            // 隐式 `return None`：非 Unit 函数要把默认值写进返回槽位
            if let Some(dest) = self.retval {
                let value = if matches!(self.ret_ty, MirType::Dynamic) {
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::DynNone),
                        args: Vec::new(),
                    }
                } else {
                    Rvalue::Use(Operand::Const(Constant::I64(0)))
                };
                self.push_stmt(Statement::Assign { dest, value });
            }
            let current = self.current_bb;
            self.epilogue_jumps.push(current);
        }
        let epilogue = self.new_block();
        self.current_bb = epilogue;

        if let Some(depth) = self.frame_depth {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TracePopTo),
                args: vec![Operand::Local(depth)],
            }));
        }
        let ret_op = self.retval.map(Operand::Local);
        self.blocks[epilogue].terminator = Terminator::Return(ret_op);
        self.epilogue = Some(epilogue);

        for bb in std::mem::take(&mut self.epilogue_jumps) {
            if matches!(self.blocks[bb].terminator, Terminator::Unreachable) {
                self.blocks[bb].terminator = Terminator::Goto(epilogue);
            }
        }
    }
    /// 块下标。用于比较「目标块是否在当前区域之前分配」，也就是是否离开了区域。
    pub(crate) fn block_index(&self, bb: BlockId) -> usize {
        bb.into_raw().into_u32() as usize
    }

    /// 下一个将要分配的块下标（等于当前已分配块数）。
    pub(crate) fn block_index_of_next_block(&self) -> usize {
        self.blocks.len()
    }

    /// 当前块若未终结则返回它（表示「正常落下」）。
    pub(crate) fn if_unterminated_take(&mut self) -> Option<BlockId> {
        if self.is_current_unterminated() {
            Some(self.current_bb)
        } else {
            None
        }
    }
    /// 把值写进绑定目标（局部变量 / 模块槽位 / 外层 cell），必要时补装箱/拆箱。
    pub(crate) fn store_binding(&mut self, binding: Binding, value: Operand, from: &MirType) {
        match binding {
            Binding::Local(local) => {
                self.assign_local(local, value, from);
            }
            Binding::ModuleVar(var) => {
                if let Some(state) = self.module_state {
                    let value = self.coerce_mir(value, from, &MirType::Dynamic);
                    self.push_stmt(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::InstanceStoreName),
                        args: vec![
                            Operand::Local(state),
                            Operand::Const(Constant::Str(self.hir.vars[var].name.to_string())),
                            value,
                        ],
                    }));
                    return;
                }
                let gid = self.module_vars[&var];
                let to = self.globals[gid].ty.clone();
                let value = self.coerce_mir(value, from, &to);
                self.push_stmt(Statement::SetGlobal { dest: gid, value });
            }
            Binding::Free(idx) => {
                // nonlocal 写入：从 env 取出外层 cell，再 CellSet（M3.0）。
                let boxed = self.coerce_mir(value, from, &MirType::Dynamic);
                let env = self.expr_builder().env_operand();
                let cell = self.alloc_temp(MirType::Dynamic);
                self.push_stmt(Statement::Assign {
                    dest: cell,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ArrayGet),
                        args: vec![env, Operand::Const(Constant::I64(idx as i64))],
                    },
                });
                self.push_stmt(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::CellSet),
                    args: vec![Operand::Local(cell), boxed],
                }));
            }
        }
    }

    /// 把值写进局部变量：普通变量直接写槽位，cell 变量经过 `CellSet`
    /// （多个闭包共享同一个 `object[1]`，M3.0）。
    pub(crate) fn assign_local(
        &mut self,
        local: ryupy_hir::LocalId,
        value: Operand,
        from: &MirType,
    ) {
        let dest = self.local_map[&local];
        self.assigned_locals.insert(local);
        if self.locals[dest].is_cell {
            let boxed = self.coerce_mir(value, from, &MirType::Dynamic);
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CellSet),
                args: vec![Operand::Local(dest), boxed],
            }));
        } else {
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::Use(value),
            });
        }
    }

    /// 把一个操作数从 `from` 表示转换到 `to` 表示（Dynamic 与静态槽位之间）。
    fn coerce_mir(&mut self, value: Operand, from: &MirType, to: &MirType) -> Operand {
        if from == to {
            return value;
        }
        let dest = self.alloc_temp(to.clone());
        let rvalue = if *to == MirType::Dynamic {
            Rvalue::Box {
                value,
                from: from.clone(),
            }
        } else if *from == MirType::Dynamic {
            Rvalue::Unbox {
                value,
                to: to.clone(),
            }
        } else {
            // 两个不同的静态表示：先装箱再拆箱，约定由运行时转换（如 i64 → f64）。
            let boxed = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest: boxed,
                value: Rvalue::Box {
                    value,
                    from: from.clone(),
                },
            });
            Rvalue::Unbox {
                value: Operand::Local(boxed),
                to: to.clone(),
            }
        };
        self.push_stmt(Statement::Assign {
            dest,
            value: rvalue,
        });
        Operand::Local(dest)
    }
}
