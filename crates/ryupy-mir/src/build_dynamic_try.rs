//! 仅动态except链的顺序分派：类型求值和正文共用一个上下文清理域。
//!
//! 原try正文由一条Python异常catch进入这里；未知目标不预读，失败不回到同级handler。
//! selected仅在as绑定成功后登记，公共清理只删除实际命中的名字。无匹配走保存对象直抛，
//! 不重走源raise准备。纯静态EH不进入本文件；return/break/continue沿既有flow机制退出。
use ryupy_hir::{Binding, HandlerId};

use crate::build::FunctionBuilder;
use crate::build_flow::{FLOW_EXCEPTION, FLOW_FIRST_CUSTOM, FLOW_RETURN, FinallyCtx};
use crate::mir::*;

impl FunctionBuilder<'_> {
    pub(crate) fn build_dynamic_handlers(
        &mut self,
        handlers: &[HandlerId],
        start_bb: BlockId,
        inner_end: usize,
        has_finally: bool,
        normals: &mut Vec<BlockId>,
        regions: &mut Vec<Region>,
    ) {
        // 只有HIR两遍降级后的整条dynamic链可进入这里，typed目标不能误作bare。
        debug_assert!(handlers.iter().all(|id| {
            self.hir.functions[self.current_hir_fn].body.handlers[*id]
                .types
                .is_none()
        }));
        let original = self.alloc_temp(MirType::Dynamic);
        let entry = self.new_block();
        self.current_bb = entry;
        self.push_stmt(Statement::Assign {
            dest: original,
            value: Rvalue::CatchException,
        });
        let enter = self.new_block();
        self.terminate_current(Terminator::Goto(enter));
        self.current_bb = enter;
        // 尚未弹callee帧，先保原异常现场及ambient context；源raise已准备的对象不替换。
        self.push_stmt(Statement::Assign {
            dest: original,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::RaisePrepare),
                args: vec![Operand::Local(original)],
            },
        });
        self.emit_frame_restore();
        let ambient = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: ambient,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::HandlerEnter),
                args: vec![Operand::Local(original)],
            },
        });
        let selected = self.alloc_temp(MirType::I64);
        self.push_stmt(Statement::Assign {
            dest: selected,
            value: Rvalue::Use(Operand::Const(Constant::I64(-1))),
        });
        let escaped = self.alloc_temp(MirType::Dynamic);
        let parent_flow = self.flow_kind;
        let flow = self.flow_kind_local();
        let dispatch_start = self.new_block();
        self.terminate_current(Terminator::Goto(dispatch_start));
        self.current_bb = dispatch_start;
        let dispatch_index = self.block_index(dispatch_start);
        self.finally_stack.push(FinallyCtx {
            pending: Vec::new(),
            start: dispatch_index,
        });
        self.protected_stack.push(dispatch_index);
        // 异常可发生在try正文任一点，不能把正文末尾事实用于目标或handler正文。
        // 仅已定Dynamic函数撤实际Dynamic槽事实，读时走既有LoadChecked；静态数值不动。
        // 每个互斥handler同起点，链后也保守检查；此新路径及后继会多运行时读取检查。
        let mut incoming_assigned = self.assigned_locals.clone();
        if self.types.is_dynamic(self.current_hir_fn) {
            incoming_assigned
                .retain(|local| self.locals[self.local_map[local]].ty != MirType::Dynamic);
        }
        let mut bindings = Vec::new();
        let mut has_bare = false;
        for (index, &handler_id) in handlers.iter().enumerate() {
            self.assigned_locals = incoming_assigned.clone();
            let (target, target_line, binding, body) = {
                let handler = &self.hir.functions[self.current_hir_fn].body.handlers[handler_id];
                (
                    handler.type_expr,
                    handler.type_line,
                    handler.name,
                    handler.body.clone(),
                )
            };
            let next = if let Some(target) = target {
                // 仅轮到此handler才更新现场，getter失败的调用者行必须是except头。
                self.emit_line(target_line);
                let ty = self.expr_builder().ty_of(target);
                let value = self.expr_builder().build_expr(target);
                let target = self.expr_builder().coerce(value, &ty, &MirType::Dynamic);
                let matches = self.expr_builder().runtime_call(
                    RuntimeFn::ExceptionMatch,
                    vec![Operand::Local(original), target],
                    MirType::Bool,
                );
                let matched = self.new_block();
                let next = self.new_block();
                self.terminate_current(Terminator::Branch {
                    cond: matches,
                    then_: matched,
                    else_: next,
                });
                self.current_bb = matched;
                Some(next)
            } else {
                has_bare = true;
                None
            };
            if let Some(binding) = binding {
                self.store_binding(binding, Operand::Local(original), &MirType::Dynamic);
                // 若绑定本身失败，不清一个尚未成功绑定的名称。
                self.push_stmt(Statement::Assign {
                    dest: selected,
                    value: Rvalue::Use(Operand::Const(Constant::I64(index as i64))),
                });
                bindings.push((index as i64, binding));
            }
            for stmt in body {
                self.build_stmt(stmt);
            }
            if self.is_current_unterminated() {
                self.route_normal(&mut Vec::new());
            }
            if let Some(next) = next {
                self.current_bb = next;
            } else {
                break;
            }
        }
        if !has_bare {
            // 无匹配不是一个新raise：保存原对象和异常动作后直接去清理，不进guard重复准备。
            self.push_stmt(Statement::Assign {
                dest: escaped,
                value: Rvalue::Use(Operand::Local(original)),
            });
            self.set_flow(FLOW_EXCEPTION);
            self.finally_stack
                .last_mut()
                .expect("动态分派清理域")
                .pending
                .push(self.current_bb);
        }
        let dispatch_end = self.block_index_of_next_block();
        self.protected_stack.pop();

        // 真实异常guard在其窗口之外；先准备新错误，再恢复帧/ambient。
        let guard = self.new_block();
        self.current_bb = guard;
        self.push_stmt(Statement::Assign {
            dest: escaped,
            value: Rvalue::CatchException,
        });
        let guard_body = self.new_block();
        self.terminate_current(Terminator::Goto(guard_body));
        self.current_bb = guard_body;
        self.push_stmt(Statement::Assign {
            dest: escaped,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::RaisePrepare),
                args: vec![Operand::Local(escaped)],
            },
        });
        self.emit_frame_restore();
        self.set_flow(FLOW_EXCEPTION);
        self.regions.push(Region {
            kind: RegionKind::FinallyGuard,
            try_start: dispatch_start,
            try_len: dispatch_end - dispatch_index,
            handler: guard,
            exc_local: Some(escaped),
        });
        let cleanup = self.new_block();
        self.terminate_current(Terminator::Goto(cleanup));
        let ctx = self.finally_stack.pop().expect("动态分派清理域");
        self.flow_kind = parent_flow;
        self.resolve_jumps(ctx.pending, cleanup);
        self.current_bb = cleanup;
        self.assigned_locals = incoming_assigned;
        self.clear_selected_exception_name(selected, &bindings);
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::HandlerLeave),
            args: vec![Operand::Local(ambient)],
        }));
        let normal = self.finish_dynamic_handler_flow(flow, escaped);
        self.current_bb = normal;
        if has_finally {
            self.route_normal(normals);
        } else {
            normals.push(normal);
        }
        // CatchAll在codegen中明确绑定PyBaseException，并不吞CLR内部故障。
        regions.push(Region {
            kind: RegionKind::CatchAll,
            try_start: start_bb,
            try_len: inner_end - self.block_index(start_bb),
            handler: entry,
            exc_local: Some(original),
        });
    }

    /// 只有选中的as名字被清理；未匹配/目标报错不碰其它局部、模块或cell。
    fn clear_selected_exception_name(&mut self, selected: LocalId, bindings: &[(i64, Binding)]) {
        for &(index, binding) in bindings {
            let yes = self.new_block();
            let next = self.new_block();
            let cond = self.dynamic_flow_eq(selected, index);
            self.terminate_current(Terminator::Branch {
                cond,
                then_: yes,
                else_: next,
            });
            self.current_bb = yes;
            let unbound = self.expr_builder().runtime_call(
                RuntimeFn::MakeUnbound,
                Vec::new(),
                MirType::Dynamic,
            );
            self.store_binding(binding, unbound, &MirType::Dynamic);
            // store_binding表示普通赋值，会插入确定赋值集合；Unbound是删除，必须撤回。
            // 这里保守撤销所有可能选中的as名称，不改变运行时selected条件。
            if let Binding::Local(local) = binding {
                self.assigned_locals.remove(&local);
            }
            self.terminate_current(Terminator::Goto(next));
            self.current_bb = next;
        }
    }

    /// 本scope的token已恰一次恢复；只路由动作，不复用会再次Leave的静态dispatch。
    fn finish_dynamic_handler_flow(&mut self, flow: LocalId, escaped: LocalId) -> BlockId {
        let rethrow = self.new_block();
        let return_bb = self.new_block();
        let actions = self.actions.clone();
        let action_bbs: Vec<_> = actions.iter().map(|_| self.new_block()).collect();
        let mut checks = vec![(FLOW_EXCEPTION, rethrow), (FLOW_RETURN, return_bb)];
        checks.extend(
            action_bbs
                .iter()
                .enumerate()
                .map(|(index, &block)| (FLOW_FIRST_CUSTOM + index as i64, block)),
        );
        for (code, target) in checks {
            let cond = self.dynamic_flow_eq(flow, code);
            let next = self.new_block();
            self.terminate_current(Terminator::Branch {
                cond,
                then_: target,
                else_: next,
            });
            self.current_bb = next;
        }
        let normal = self.current_bb;
        self.current_bb = rethrow;
        self.terminate_current(Terminator::Raise {
            value: Some(Operand::Local(escaped)),
            cause: None,
        });
        self.current_bb = return_bb;
        self.set_flow(FLOW_RETURN);
        if let Some(ctx) = self.finally_stack.last_mut() {
            ctx.pending.push(return_bb);
        } else {
            self.epilogue_jumps.push(return_bb);
        }
        for (target, block) in actions.into_iter().zip(action_bbs) {
            self.current_bb = block;
            self.route_jump(target);
        }
        normal
    }

    fn dynamic_flow_eq(&mut self, flow: LocalId, value: i64) -> Operand {
        let result = self.alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: result,
            value: Rvalue::Binary {
                op: BinOp::Eq,
                lhs: Operand::Local(flow),
                rhs: Operand::Const(Constant::I64(value)),
            },
        });
        Operand::Local(result)
    }
}
