//! `try` / `except` / `else` / `finally` 的 MIR 构建。
//!
//! # 为什么单独成文件
//!
//! 异常区域的构建是这一层最容易出错的部分：块的分段必须与 CIL 异常表的区间一一对应，
//! `finally` 又要覆盖「正常落下 / return / break / continue / 异常」五种离开方式。
//! 把它从 `build.rs` 里独立出来，主流程只留一句 `StmtKind::Try => self.build_try(..)`。
//!
//! # 块布局
//!
//! ```text
//!   start_bb:            ← 区域必须从块边界开始，受保护区间不能切进半条语句
//!     [try 体]           受保护（内层 catch 区域，外层 finally 通道）
//!     ↓ 正常落下
//!   handler_i:           except 分支入口（CLR 把异常对象压栈）
//!     ↓
//!   else_bb:             try 体正常结束才走（受 finally 保护）
//!     ↓                （到此 outer_end，外层受保护区结束）
//!   guard_bb:            finally 的异常通道（catch-all）
//!     ↓ 记录异常 + 状态码
//!   finally_entry:       finally 体
//!     ↓
//!   dispatch:            按状态码决定：继续 / 重抛 / 返回 / 跳出
//!   after_bb:
//! ```
//!
//! 只有「块在区域窗口内」是硬约束：codegen 按 arena 顺序发射指令，窗口内的块必须连续
//! 分配，窗口之外才能分配 handler / finally / after 这些块。所以本文件里的分配顺序不能
//! 随意调整——每一段都有注释说明为什么在这个位置分配。
//!
//! # 为什么不用 CIL 的 finally
//!
//! CIL 的 finally 处理器有两个硬限制：不能用 `leave` 离开，也不能抑制正在传播的异常。
//! 而 Python 的 `finally` 必须支持 `return`（甚至用它覆盖返回值）与 `break`，还必须能在
//! `finally` 里吞掉异常。于是这里改成「catch-all + 状态机」：所有离开路径先把状态码写进
//! 局部变量，再跳进 `finally` 体；`finally` 体执行完由分派块决定下一步。代价是多一个状态
//! 变量和一次分派，换来的是与 CPython 一致的语义。
//!
//! # 状态码
//!
//! `0` 正常落下、`1` 异常、`2` 返回、`3+` 跳出到某个块。跳出目标按函数级动作表去重，
//! 同一个 `break` 目标只占一个码。

use ryupy_hir::{Binding, HandlerId, StmtId};

use crate::build::FunctionBuilder;
use crate::build_flow::{FLOW_EXCEPTION, FLOW_FIRST_CUSTOM, FLOW_RETURN, FinallyCtx};
use crate::mir::{
    BinOp, BlockId, Callee, Constant, LocalId as MirLocalId, MirType, Operand, Region, RegionKind,
    RuntimeFn, Rvalue, Statement, Terminator,
};

impl FunctionBuilder<'_> {
    /// 构建一条 `try` 语句。
    pub(crate) fn build_try(
        &mut self,
        body: &[StmtId],
        handlers: &[HandlerId],
        orelse: &[StmtId],
        finalbody: &[StmtId],
    ) {
        self.build_try_cleanup(body, handlers, orelse, finalbody, None);
    }

    /// 捕获的 except 变量在任意退出路径都要清空。复用 finally 的展开通道，
    /// 让 return / break / continue / 再抛异常共用同一次 cell 清理与异常上下文恢复。
    fn build_try_cleanup(
        &mut self,
        body: &[StmtId],
        handlers: &[HandlerId],
        orelse: &[StmtId],
        finalbody: &[StmtId],
        cleanup: Option<(Binding, MirLocalId)>,
    ) {
        let has_finally = !finalbody.is_empty() || cleanup.is_some();

        // 0) 区域从一个干净的块开始：受保护区间必须落在块边界上，
        //    否则前面的语句抛出的异常会被这条 try 误捕获。
        let start_bb = self.new_block();
        self.terminate_current(Terminator::Goto(start_bb));
        self.current_bb = start_bb;
        let start_index = self.block_index(start_bb);

        // finally 的异常槽位（同时充当分派块的重抛源）
        let guard_exc = if has_finally {
            Some(self.alloc_temp(MirType::Dynamic))
        } else {
            None
        };
        let flow_kind = if has_finally {
            Some(self.flow_kind_local())
        } else {
            None
        };

        // 1) try 体
        if has_finally {
            // ctx 要先压栈：try 体里的 break/continue/return 必须穿过 finally。
            self.finally_stack.push(FinallyCtx {
                pending: Vec::new(),
                start: start_index,
            });
            self.protected_stack.push(start_index);
        }
        self.protected_stack.push(start_index);
        for &stmt in body {
            self.build_stmt(stmt);
        }
        let body_fallthrough = self.if_unterminated_take();
        let inner_end = self.block_index_of_next_block();
        self.protected_stack.pop();
        if has_finally {
            self.protected_stack.pop();
        }

        // 2) except 分支（窗口内：它们属于外层 finally 的受保护区）
        let mut handler_normals: Vec<BlockId> = Vec::new();
        let mut handler_regions: Vec<Region> = Vec::new();
        self.build_handlers(
            handlers,
            start_bb,
            inner_end,
            has_finally,
            &mut handler_normals,
            &mut handler_regions,
        );

        // 3) else 子句：只有 try 体正常结束才执行，不能落在内层受保护区间里，
        //    但要落在外层（finally）的受保护区里，所以在这个位置分配。
        let else_entry = if orelse.is_empty() {
            None
        } else {
            let bb = self.new_block();
            self.current_bb = bb;
            for &stmt in orelse {
                self.build_stmt(stmt);
            }
            let fell_through = self.is_current_unterminated();
            if fell_through {
                self.route_normal(&mut handler_normals);
            }
            Some(bb)
        };

        // try 体正常落下：有 else 直接进 else，否则按普通出口处理
        if let Some(bb) = body_fallthrough {
            match else_entry {
                Some(else_bb) => self.blocks[bb].terminator = Terminator::Goto(else_bb),
                None => {
                    self.current_bb = bb;
                    self.route_normal(&mut handler_normals);
                }
            }
        }

        // 4) 外层（finally）受保护区到此结束
        let outer_end = self.block_index_of_next_block();

        // 5) finally：异常通道 → finally 体 → 分派
        let mut after_jumps: Vec<BlockId> = Vec::new();
        if has_finally {
            // 异常通道块在窗口之外（它是 handler，不属于受保护的 try 区）
            let guard_bb = self.new_block();
            self.current_bb = guard_bb;
            self.push_stmt(Statement::Assign {
                dest: guard_exc.expect("已检查"),
                value: Rvalue::CatchException,
            });
            let guard_body = self.new_block();
            self.blocks[guard_bb].terminator = Terminator::Goto(guard_body);
            self.current_bb = guard_body;
            self.emit_frame_restore();
            // 登记当前异常：`finally` 里再抛出的异常要把这条挂成 __context__
            let prev_current = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest: prev_current,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::HandlerEnter),
                    args: vec![Operand::Local(guard_exc.expect("已检查"))],
                },
            });
            self.set_flow(FLOW_EXCEPTION);
            self.regions.push(Region {
                kind: RegionKind::FinallyGuard,
                try_start: start_bb,
                try_len: outer_end - start_index,
                handler: guard_bb,
                exc_local: guard_exc,
            });

            let finally_entry = self.new_block();
            self.terminate_current(Terminator::Goto(finally_entry));

            // 进 finally 体之前把 ctx 摘掉：finally 体里的 return / break 属于外层作用域，
            // 若还留着这条 ctx，它们会又跳回 finally 体自己，形成死循环。
            let ctx = self.finally_stack.pop().expect("finally 上下文缺失");

            self.current_bb = finally_entry;
            for &stmt in finalbody {
                self.build_stmt(stmt);
            }
            if let Some((binding, previous)) = cleanup {
                let unbound = self.expr_builder().runtime_call(
                    RuntimeFn::MakeUnbound,
                    Vec::new(),
                    MirType::Dynamic,
                );
                self.store_binding(binding, unbound, &MirType::Dynamic);
                self.push_stmt(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::HandlerLeave),
                    args: vec![Operand::Local(previous)],
                }));
            }
            let finally_fallthrough = self.if_unterminated_take();
            self.build_dispatch(
                finally_fallthrough,
                flow_kind.expect("已检查"),
                guard_exc.expect("已检查"),
                cleanup
                    .map(|(_, previous)| previous)
                    .unwrap_or(prev_current),
                &mut after_jumps,
            );

            // try 体 / 各 handler / else 的正常落下都跳进 finally
            self.resolve_jumps(ctx.pending, finally_entry);
        }

        // 6) after 块（在所有区域之外）
        let after_bb = self.new_block();
        for region in handler_regions {
            self.regions.push(region);
        }
        self.resolve_jumps(handler_normals, after_bb);
        self.resolve_jumps(after_jumps, after_bb);
        self.current_bb = after_bb;
    }

    /// 构建所有 `except` 分支。
    ///
    /// 每个分支独立一个块：入口先取下异常对象，再登记「当前异常」，执行分支体，
    /// 正常结束时恢复上一层当前异常并把 `as name` 标记为未赋值。
    fn build_handlers(
        &mut self,
        handlers: &[HandlerId],
        start_bb: BlockId,
        inner_end: usize,
        has_finally: bool,
        normals: &mut Vec<BlockId>,
        regions: &mut Vec<Region>,
    ) {
        let inner_start = self.block_index(start_bb);
        for &handler_id in handlers {
            let (types, name, handler_body) = {
                let handler = &self.hir.functions[self.current_hir_fn].body.handlers[handler_id];
                (handler.types.clone(), handler.name, handler.body.clone())
            };

            // 每个分支一个异常槽位；多个类型共用同一个槽位与同一个分支体
            let exc = self.alloc_temp(MirType::Dynamic);
            let body_bb = self.new_block();

            // 一种类型一条 CIL catch 记录，各配一个独立的两指令处理入口。
            // 不能让多条记录共用同一段 handler 指令：CLR 会把「try 与 handler 区间
            // 完全相同的两条记录」当成重复子句，直接判 InvalidProgramException。
            let type_names: Vec<String> = match &types {
                Some(names) if !names.is_empty() => names.iter().map(|n| n.to_string()).collect(),
                _ => Vec::new(),
            };
            let mut trampolines = Vec::new();
            if type_names.is_empty() {
                trampolines.push(self.make_handler_entry(exc, body_bb));
            } else {
                for _ in &type_names {
                    trampolines.push(self.make_handler_entry(exc, body_bb));
                }
            }

            // 分支体
            self.current_bb = body_bb;

            // 0) 被展开的调用栈帧要弹回本函数：异常穿过的那几层不会执行自己的
            //    PopTo，不补这一步，递归守卫的深度会被永久占住。
            self.emit_frame_restore();

            // 1) 登记当前异常（裸 raise 与 __context__ 依赖它）
            let previous = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest: previous,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::HandlerEnter),
                    args: vec![Operand::Local(exc)],
                },
            });

            // 2) `as name` 绑定
            if let Some(binding) = name {
                self.store_binding(binding, Operand::Local(exc), &MirType::Dynamic);
            }

            // 3) 捕获绑定需要在所有退出路径清空。普通未捕获绑定仍走已有轻量路径。
            let captured = match name {
                Some(Binding::Local(local)) => self.locals[self.local_map[&local]].is_cell,
                Some(Binding::Free(_)) => true,
                _ => false,
            };
            if captured {
                self.build_try_cleanup(
                    &handler_body,
                    &[],
                    &[],
                    &[],
                    name.map(|binding| (binding, previous)),
                );
            } else {
                for &stmt in &handler_body {
                    self.build_stmt(stmt);
                }
            }

            // 4) 正常结束：清理绑定、恢复当前异常，再走出口
            if self.is_current_unterminated() {
                if let Some(binding) = name {
                    let unbound = self.expr_builder().runtime_call(
                        RuntimeFn::MakeUnbound,
                        Vec::new(),
                        MirType::Dynamic,
                    );
                    self.store_binding(binding, unbound, &MirType::Dynamic);
                }
                self.push_stmt(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::HandlerLeave),
                    args: vec![Operand::Local(previous)],
                }));
                self.route_normal(normals);
            }

            // 5) 每条 catch 记录各注册一个区域
            if type_names.is_empty() {
                regions.push(Region {
                    kind: RegionKind::CatchAll,
                    try_start: start_bb,
                    try_len: inner_end - inner_start,
                    handler: trampolines[0],
                    exc_local: Some(exc),
                });
            } else {
                for (i, type_name) in type_names.into_iter().enumerate() {
                    regions.push(Region {
                        kind: RegionKind::Catch {
                            type_names: vec![type_name],
                        },
                        try_start: start_bb,
                        try_len: inner_end - inner_start,
                        handler: trampolines[i],
                        exc_local: Some(exc),
                    });
                }
            }
        }

        // has_finally 时 handler 的正常出口已进 finally 的 pending 列表，
        // 这里的 normals 只会收到「没有 finally」的情况。
        let _ = has_finally;
    }

    /// 生成一个「取下异常对象 → 跳到分支体」的两指令处理入口。
    fn make_handler_entry(&mut self, exc: MirLocalId, body_bb: BlockId) -> BlockId {
        let entry = self.new_block();
        self.current_bb = entry;
        self.push_stmt(Statement::Assign {
            dest: exc,
            value: Rvalue::CatchException,
        });
        self.terminate_current(Terminator::Goto(body_bb));
        entry
    }

    /// `finally` 执行完后的分派：按状态码决定继续、重抛、返回还是跳出。
    fn build_dispatch(
        &mut self,
        fallthrough: Option<BlockId>,
        flow_kind: MirLocalId,
        exc_local: MirLocalId,
        prev_current: MirLocalId,
        after_jumps: &mut Vec<BlockId>,
    ) {
        let dispatch_bb = self.new_block();
        if let Some(bb) = fallthrough {
            self.blocks[bb].terminator = Terminator::Goto(dispatch_bb);
        }
        self.current_bb = dispatch_bb;

        // 先固定各动作的目标块，再生成比较链（比较链本身要新建块）
        let rethrow_bb = self.new_block();
        let return_bb = self.new_block();
        let actions = self.actions.clone();
        let action_bbs: Vec<BlockId> = (0..actions.len()).map(|_| self.new_block()).collect();

        let mut checks: Vec<(i64, BlockId)> =
            vec![(FLOW_EXCEPTION, rethrow_bb), (FLOW_RETURN, return_bb)];
        for (i, _) in actions.iter().enumerate() {
            checks.push((FLOW_FIRST_CUSTOM + i as i64, action_bbs[i]));
        }

        let mut check_bb = dispatch_bb;
        for (code, target) in checks {
            self.current_bb = check_bb;
            let cond = self.compare_flow(flow_kind, code);
            let next_bb = self.new_block();
            self.terminate_current(Terminator::Branch {
                cond,
                then_: target,
                else_: next_bb,
            });
            check_bb = next_bb;
        }

        // 比较链落空 = 正常落下：先把「当前异常」恢复成进入 finally 之前的状态
        self.current_bb = check_bb;
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::HandlerLeave),
            args: vec![Operand::Local(prev_current)],
        }));
        after_jumps.push(check_bb);

        // 异常：重抛
        self.current_bb = rethrow_bb;
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::HandlerLeave),
            args: vec![Operand::Local(prev_current)],
        }));
        self.terminate_current(Terminator::Raise {
            value: Some(Operand::Local(exc_local)),
            cause: None,
        });

        // 返回：穿过外层 finally 或直接去统一返回块
        self.current_bb = return_bb;
        if self.finally_stack.is_empty() {
            let bb = self.current_bb;
            self.epilogue_jumps.push(bb);
        } else {
            let bb = self.current_bb;
            self.finally_stack
                .last_mut()
                .expect("刚检查过非空")
                .pending
                .push(bb);
        }

        // 跳出：同样要穿过外层 finally
        for (i, target) in actions.iter().enumerate() {
            self.current_bb = action_bbs[i];
            self.route_jump(*target);
        }
    }

    /// 生成 `flow_kind == code` 的布尔操作数。
    fn compare_flow(&mut self, flow_kind: MirLocalId, code: i64) -> Operand {
        let dest = self.alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Binary {
                op: BinOp::Eq,
                lhs: Operand::Local(flow_kind),
                rhs: Operand::Const(Constant::I64(code)),
            },
        });
        Operand::Local(dest)
    }
}
