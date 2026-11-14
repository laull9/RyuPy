//! 生成器异常区域降级：原 Region 成为逻辑匹配表，恢复函数只有一个最外层 CLR catch。
//!
//! catch 保存当前局部、待接异常和 handler PC 后返回内部 Retry；运行时再次恢复时从逻辑
//! handler 继续。正常基本块只记录 active_block，不逐语句 spill。yield / return 统一 leave
//! 到保护区外 epilogue，避免跨暂停跳进物理 try 区间或在受保护区域内直接 ret。

use crate::generators::{boxed, call, local, set_slot};
use crate::mir::*;
use la_arena::{Arena, Idx, RawIdx};

pub(super) struct Plan {
    regions: Vec<Region>,
    original_blocks: usize,
}

impl Plan {
    pub(super) fn take(body: &mut Body) -> Self {
        let mut regions = std::mem::take(&mut body.regions);
        // 区间越小越内层；同窗口先 except 再 finally，且保留 except 源码顺序。
        regions.sort_by_key(|region| {
            (
                region.try_len,
                matches!(region.kind, RegionKind::FinallyGuard),
            )
        });
        Self {
            regions,
            original_blocks: body.blocks.len(),
        }
    }

    pub(super) fn handlers(&self) -> impl Iterator<Item = BlockId> + '_ {
        self.regions.iter().map(|region| region.handler)
    }

    pub(super) fn frame_routes(&self) -> Vec<crate::trace_lowering::FrameCatchRoute> {
        self.regions
            .iter()
            .map(|region| crate::trace_lowering::FrameCatchRoute {
                start: number(region.try_start),
                length: region.try_len,
                handler_pc: number(region.handler),
                unconditional: matches!(
                    region.kind,
                    RegionKind::CatchAll | RegionKind::FinallyGuard
                ),
            })
            .collect()
    }

    pub(super) fn finish(
        self,
        body: &mut Body,
        frame: LocalId,
        saved: &[(LocalId, MirType)],
        exception_globals: &rustc_hash::FxHashMap<String, GlobalId>,
        product: bool,
    ) -> (Vec<BlockId>, Vec<(BlockId, usize, LocalId)>) {
        if self.regions.is_empty() {
            return (body.blocks.iter().map(|(id, _)| id).collect(), Vec::new());
        }
        let mut catch_reads = Vec::new();
        let active = local(&mut body.locals, MirType::I64);
        let caught = local(&mut body.locals, MirType::Dynamic);
        let result = local(&mut body.locals, MirType::Dynamic);
        for (id, block) in body.blocks.iter_mut().take(self.original_blocks) {
            // 必须排在 GeneratorInput 之前：throw 注入按原 yield 恢复点的保护范围处理。
            block.stmts.insert(
                0,
                Statement::Assign {
                    dest: active,
                    value: Rvalue::Use(Operand::Const(Constant::I64(number(id)))),
                },
            );
            let mut statements = Vec::new();
            for statement in std::mem::take(&mut block.stmts) {
                if let Statement::Assign {
                    dest,
                    value: Rvalue::CatchException,
                } = statement
                {
                    catch_reads.push((id, statements.len(), dest));
                    statements.push(call(
                        dest,
                        RuntimeFn::ArrayGet,
                        vec![Operand::Local(frame), Operand::Const(Constant::I64(3))],
                    ));
                    statements.push(set_slot(frame, 3, Operand::Const(Constant::None)));
                } else {
                    statements.push(statement);
                }
            }
            block.stmts = statements;
        }
        // restore 自身失败不能误归到某个 Python 语句，故入口先给无保护区的哨兵块号。
        body.blocks[body.entry].stmts.insert(
            0,
            Statement::Assign {
                dest: active,
                value: Rvalue::Use(Operand::Const(Constant::I64(-1))),
            },
        );
        let protected_len = body.blocks.len();
        let epilogue = alloc(
            body,
            vec![],
            Terminator::Return(Some(Operand::Local(result))),
        );
        for (_, block) in body.blocks.iter_mut().take(protected_len) {
            if let Terminator::Return(value) = &block.terminator {
                block.stmts.push(Statement::Assign {
                    dest: result,
                    value: Rvalue::Use(value.clone().unwrap_or(Operand::Const(Constant::None))),
                });
                block.terminator = Terminator::Goto(epilogue);
            }
        }
        let unmatched = alloc(
            body,
            vec![],
            Terminator::Raise {
                value: Some(Operand::Local(caught)),
                cause: None,
            },
        );
        let next_pc = local(&mut body.locals, MirType::I64);
        let retry = make_retry(body, frame, saved, caught, next_pc);
        let mut dispatch = unmatched;
        for region in self.regions.iter().rev() {
            let choose = alloc(
                body,
                vec![Statement::Assign {
                    dest: next_pc,
                    value: Rvalue::Use(Operand::Const(Constant::I64(number(region.handler)))),
                }],
                Terminator::Goto(retry),
            );
            let matched_type = match &region.kind {
                RegionKind::Catch { type_names } => {
                    let mut target = dispatch;
                    for name in type_names.iter().rev() {
                        target = type_test(
                            body,
                            caught,
                            name,
                            exception_globals.get(name).copied(),
                            choose,
                            target,
                        );
                    }
                    target
                }
                RegionKind::CatchAll | RegionKind::FinallyGuard => choose,
            };
            let end = number(region.try_start) + region.try_len as i64;
            let upper = compare(body, active, BinOp::Lt, end, matched_type, dispatch);
            dispatch = compare(
                body,
                active,
                BinOp::Ge,
                number(region.try_start),
                upper,
                dispatch,
            );
        }
        // 真正的 CLR handler 只取异常并 leave 到普通分派块，分派不会跳回物理 try。
        let handler = alloc(
            body,
            vec![
                Statement::Assign {
                    dest: caught,
                    value: Rvalue::CatchException,
                },
                // 普通运行时错误也必须在Retry弹callee帧前记录traceback/当前异常。
                if product {
                    Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::TraceV1GeneratorCatch),
                        args: vec![Operand::Local(caught)],
                    })
                } else {
                    call(
                        caught,
                        RuntimeFn::RaisePrepare,
                        vec![Operand::Local(caught)],
                    )
                },
            ],
            Terminator::Goto(dispatch),
        );
        body.regions.push(Region {
            source_handler_chain: None,
            kind: RegionKind::CatchAll,
            try_start: block_id(0),
            try_len: protected_len,
            handler,
            exc_local: Some(caught),
        });
        let mapping = move_entry_first(body, protected_len);
        for (block, _, _) in &mut catch_reads {
            *block = mapping[number(*block) as usize];
        }
        (mapping, catch_reads)
    }
}

fn number(id: BlockId) -> i64 {
    id.into_raw().into_u32() as i64
}
fn block_id(value: usize) -> BlockId {
    Idx::from_raw(RawIdx::from_u32(value as u32))
}

fn alloc(body: &mut Body, stmts: Vec<Statement>, terminator: Terminator) -> BlockId {
    body.blocks.alloc(BasicBlock { stmts, terminator })
}

fn compare(
    body: &mut Body,
    active: LocalId,
    op: BinOp,
    value: i64,
    yes: BlockId,
    no: BlockId,
) -> BlockId {
    let cond = local(&mut body.locals, MirType::Bool);
    alloc(
        body,
        vec![Statement::Assign {
            dest: cond,
            value: Rvalue::Binary {
                op,
                lhs: Operand::Local(active),
                rhs: Operand::Const(Constant::I64(value)),
            },
        }],
        Terminator::Branch {
            cond: Operand::Local(cond),
            then_: yes,
            else_: no,
        },
    )
}

fn type_test(
    body: &mut Body,
    caught: LocalId,
    name: &str,
    user_class: Option<GlobalId>,
    yes: BlockId,
    no: BlockId,
) -> BlockId {
    let ty = local(&mut body.locals, MirType::Dynamic);
    let answer = local(&mut body.locals, MirType::Dynamic);
    let cond = local(&mut body.locals, MirType::Bool);
    alloc(
        body,
        vec![
            match user_class {
                Some(global) => Statement::Assign {
                    dest: ty,
                    value: Rvalue::Use(Operand::Global(global)),
                },
                None => call(
                    ty,
                    RuntimeFn::TypeObject,
                    vec![Operand::Const(Constant::Str(name.to_owned()))],
                ),
            },
            call(
                answer,
                RuntimeFn::DynBuiltin,
                vec![
                    Operand::Const(Constant::Str("isinstance".into())),
                    Operand::Array(vec![Operand::Local(caught), Operand::Local(ty)]),
                    Operand::Array(vec![]),
                    Operand::Array(vec![]),
                ],
            ),
            call(cond, RuntimeFn::DynTruthy, vec![Operand::Local(answer)]),
        ],
        Terminator::Branch {
            cond: Operand::Local(cond),
            then_: yes,
            else_: no,
        },
    )
}

fn make_retry(
    body: &mut Body,
    frame: LocalId,
    saved: &[(LocalId, MirType)],
    caught: LocalId,
    pc: LocalId,
) -> BlockId {
    let mut stmts = Vec::new();
    for (slot, ty) in saved {
        let value = boxed(&mut body.locals, &mut stmts, Operand::Local(*slot), ty);
        stmts.push(set_slot(
            frame,
            slot.into_raw().into_u32() as usize + 4,
            value,
        ));
    }
    let state = boxed(
        &mut body.locals,
        &mut stmts,
        Operand::Local(pc),
        &MirType::I64,
    );
    stmts.push(set_slot(frame, 0, state));
    stmts.push(set_slot(frame, 3, Operand::Local(caught)));
    let retry = local(&mut body.locals, MirType::Dynamic);
    stmts.push(call(retry, RuntimeFn::GeneratorRetry, vec![]));
    alloc(body, stmts, Terminator::Return(Some(Operand::Local(retry))))
}

/// 入口位于第一个受保护块时，后端无需从 try 外跳进恢复分派中部。
/// PC 的整数标签保持旧编号；只重映射 CFG 边和真实 Region 的块引用。
fn move_entry_first(body: &mut Body, protected_len: usize) -> Vec<BlockId> {
    let old_entry = body.entry;
    let mut old: Vec<_> = std::mem::take(&mut body.blocks)
        .into_iter()
        .map(|(_, block)| Some(block))
        .collect();
    let mut order = vec![old_entry];
    order.extend((0..old.len()).map(block_id).filter(|id| *id != old_entry));
    let mut mapping = vec![block_id(0); old.len()];
    for (new, old) in order.iter().enumerate() {
        mapping[number(*old) as usize] = block_id(new);
    }
    let remap = |id: BlockId| mapping[number(id) as usize];
    let mut blocks = Arena::default();
    for old_id in order {
        // order 恰好是 arena 索引的排列，每个元素只取一次。
        let Some(mut block) = old[number(old_id) as usize].take() else {
            continue;
        };
        match &mut block.terminator {
            Terminator::Goto(target) => *target = remap(*target),
            Terminator::Branch { then_, else_, .. } => {
                *then_ = remap(*then_);
                *else_ = remap(*else_);
            }
            _ => {}
        }
        blocks.alloc(block);
    }
    body.blocks = blocks;
    body.entry = block_id(0);
    for region in &mut body.regions {
        region.try_start = block_id(0);
        region.try_len = protected_len;
        region.handler = remap(region.handler);
    }
    mapping
}

#[cfg(test)]
#[path = "generator_exception_tests.rs"]
mod tests;
