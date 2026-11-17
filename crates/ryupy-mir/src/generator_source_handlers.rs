//! provider生成器源码handler的最终CFG合同；复用既有逐槽游标核实际路由，不以lowering标签代替证明。
//! 所有源块先写active，唯一物理catch按内层优先窗口选handler PC；Retry只保存当前局部/异常，
//! restore按原PC进入真实handler并清待接槽。共享证据只含函数内身份，链接无需猜测外部符号。
use crate::generator_product_cursor::{Cursor, block, local, number};
use crate::*;
use rustc_hash::FxHashSet;
use std::sync::Arc;

#[derive(Clone, Debug)]
pub(crate) struct SourceRoute {
    pub chain: usize,
    pub route: usize,
    pub exception: Option<LocalId>,
}
#[derive(Clone, Debug)]
pub(crate) struct Evidence {
    layout: crate::trace_lowering::GeneratorLayout,
    sources: Vec<SourceRoute>,
    typed_targets: Vec<Vec<Operand>>,
    product: bool,
}

pub(crate) fn install(
    function: &mut Function,
    layout: &crate::trace_lowering::GeneratorLayout,
    sources: Vec<SourceRoute>,
    typed_targets: Vec<Vec<Operand>>,
    product: bool,
) {
    if function.body.source_handler_contracts.is_empty() {
        return;
    }
    let evidence = Arc::new(Evidence {
        layout: layout.clone(),
        sources,
        typed_targets,
        product,
    });
    for contract in &mut function.body.source_handler_contracts {
        contract.generator = Some(evidence.clone());
    }
    // 物理catch保存链0作为路由合同锚；即使整份证据被删，通用孤儿ID检查仍明确拒绝。
    if let Some(region) = function.body.regions.first_mut() {
        region.source_handler_chain = Some(0);
    }
}

pub(crate) fn validate(function: &Function) -> Result<(), String> {
    let contracts = &function.body.source_handler_contracts;
    let evidence = contracts
        .first()
        .and_then(|contract| contract.generator.as_ref())
        .ok_or("缺少生成器路由证据")?;
    let layout = &evidence.layout;
    let InstanceStateSource::GeneratorFrame { frame, .. } =
        function.module_state_source.ok_or("缺少生成器frame")?
    else {
        return Err("路由不属于GeneratorFrame".into());
    };
    if !function.instance_capabilities.provider_namespace
        || !function.instance_capabilities.ordinary_name_handlers
        || function.params != [frame]
        || crate::validate::local_decl(&function.body.locals, frame)
            .is_none_or(|decl| decl.ty != MirType::Dynamic || decl.is_cell)
        || contracts.iter().any(|contract| {
            contract
                .generator
                .as_ref()
                .is_none_or(|other| !Arc::ptr_eq(other, evidence))
        })
        || evidence.sources.len() != contracts.len()
        || layout
            .saved_locals
            .iter()
            .enumerate()
            .any(|(index, id)| *id != local(index))
        || layout.saved_locals.len() != frame.into_raw().into_u32() as usize
    {
        return Err("生成器路由能力、frame或共享来源不一致".into());
    }
    let [region] = function.body.regions.as_slice() else {
        return Err("生成器缺少唯一物理catch".into());
    };
    if region.try_start != block(0)
        || !matches!(region.kind, RegionKind::CatchAll)
        || region.source_handler_chain != Some(0)
        || layout.physical_region != Some((region.try_len, region.handler))
    {
        return Err("物理catch与实际路由窗口不一致".into());
    }
    let mut check = Check {
        function,
        frame,
        active: frame,
        caught: frame,
        pc: frame,
        state: frame,
        result: None,
        frame_ops: FxHashSet::default(),
        original: layout.saved_locals.len(),
        seen: FxHashSet::default(),
        role_writes: FxHashSet::default(),
        reachable: reachable(function)?,
    };
    check.restore(layout)?;
    for (label, id) in layout.original_blocks.iter().enumerate() {
        let source = check.body(*id)?;
        if source.stmts.is_empty()
            && source.terminator == Terminator::Unreachable
            && !check.reachable.contains(id)
        {
            continue;
        }
        if !matches!(source.stmts.first(), Some(Statement::Assign {dest,value:Rvalue::Use(Operand::Const(Constant::I64(actual)))})
            if *dest==check.active && *actual==label as i64)
        {
            return Err("源块没有先写实际active PC".into());
        }
        check.role_writes.insert((*id, 0));
    }
    let handler = check.body(region.handler)?;
    let mut cursor = Cursor::new(&handler.stmts, check.original + 1);
    let (caught, value) = cursor.assignment()?;
    check.private(caught, MirType::Dynamic)?;
    if !matches!(value, Rvalue::CatchException) || region.exc_local != Some(caught) {
        return Err("物理catch对象身份不一致".into());
    }
    check.caught = caught;
    check.role_writes.insert((region.handler, 0));
    if evidence.product {
        cursor.eval_call(RuntimeFn::TraceV1GeneratorCatch, &[Operand::Local(caught)])?;
    } else {
        cursor.eval_call(
            RuntimeFn::GeneratorTracebackReceive,
            &[Operand::Local(frame), Operand::Local(caught)],
        )?;
        let position = cursor.position;
        if cursor.assign_call(RuntimeFn::RaisePrepare, &[Operand::Local(caught)])? != caught {
            return Err("异常准备替换了私有对象槽".into());
        }
        check.role_writes.insert((region.handler, position));
    }
    cursor.end()?;
    let Terminator::Goto(mut dispatch) = handler.terminator else {
        return Err("catch未离开EH到路由".into());
    };
    let mut chosen = FxHashSet::default();
    if evidence.typed_targets.len() != layout.exception_routes.len()
        || layout
            .exception_routes
            .windows(2)
            .any(|pair| pair[0].length > pair[1].length)
    {
        return Err("逻辑route数量或内层优先顺序不一致".into());
    }
    for (route_index, route) in layout.exception_routes.iter().enumerate() {
        let (upper, next) = check.comparison(dispatch, check.active, BinOp::Ge, route.start)?;
        let end = window_end(route, layout.original_blocks.len())?;
        let (mut choose, upper_next) = check.comparison(upper, check.active, BinOp::Lt, end)?;
        if next != upper_next {
            return Err("逻辑区间上下界或不匹配边非法".into());
        }
        choose = check.typed(
            choose,
            next,
            caught,
            &evidence.typed_targets[route_index],
            route.unconditional,
        )?;
        let value = check.body(choose)?;
        let mut cursor = Cursor::new(&value.stmts, check.original + 1);
        let pc = cursor.use_value(&Operand::Const(Constant::I64(route.handler_pc)))?;
        check.private(pc, MirType::I64)?;
        cursor.end()?;
        if check.pc != frame && check.pc != pc {
            return Err("handler未共用实际Retry PC槽".into());
        }
        check.pc = pc;
        check.role_writes.insert((choose, 0));
        if !matches!(value.terminator,Terminator::Goto(target) if target==block(region.try_len+2))
            || !chosen.insert(choose)
        {
            return Err("handler绕过唯一Retry或重复选择".into());
        }
        dispatch = next;
    }
    if dispatch != block(region.try_len + 1) {
        return Err("all-miss未到唯一外逸出口".into());
    }
    let unmatched = check.body(dispatch)?;
    if !unmatched.stmts.is_empty()
        || unmatched.terminator
            != (Terminator::Raise {
                value: Some(Operand::Local(caught)),
                cause: None,
            })
    {
        return Err("all-miss没有直抛原异常".into());
    }
    let retry = check.body(block(region.try_len + 2))?;
    let mut cursor = Cursor::new(&retry.stmts, check.original + 1);
    let types = layout
        .saved_locals
        .iter()
        .map(|id| function.body.locals[*id].ty.clone())
        .collect::<Vec<_>>();
    cursor.spill(frame, &types)?;
    cursor.pc(frame, Operand::Local(check.pc))?;
    cursor.slot(frame, 3, Operand::Local(caught))?;
    let signal = cursor.assign_call(RuntimeFn::GeneratorRetry, &[])?;
    check.private(signal, MirType::Dynamic)?;
    cursor.end()?;
    if retry.terminator != Terminator::Return(Some(Operand::Local(signal))) {
        return Err("Retry没有返回实际哨兵".into());
    }
    check.mark_frame_ops(block(region.try_len + 2), 0, retry.stmts.len());
    check.catch_reads(layout)?;
    check.exits(layout, &types, region.try_len)?;
    let mut source_ids = FxHashSet::default();
    let mut source_routes = FxHashSet::default();
    for source in &evidence.sources {
        let contract = contracts.get(source.chain).ok_or("源码链ID越界")?;
        let route = layout
            .exception_routes
            .get(source.route)
            .ok_or("源码逻辑route越界")?;
        let handler = layout
            .pc_targets
            .iter()
            .find_map(|(pc, id)| (*pc == route.handler_pc).then_some(*id))
            .ok_or("源码handler缺少恢复PC")?;
        if !source_ids.insert(source.chain)
            || !source_routes.insert(source.route)
            || number(contract.try_start) as i64 != route.start
            || !layout
                .catch_reads
                .iter()
                .any(|(id, _, dest)| *id == handler && Some(*dest) == source.exception)
        {
            return Err("源码链与逻辑route/真实异常接收不对应".into());
        }
    }
    check.private_roles()?;
    Ok(())
}

pub(super) struct Check<'a> {
    pub(super) function: &'a Function,
    pub(super) frame: LocalId,
    pub(super) active: LocalId,
    pub(super) caught: LocalId,
    pub(super) pc: LocalId,
    pub(super) state: LocalId,
    pub(super) result: Option<LocalId>,
    pub(super) frame_ops: FxHashSet<(BlockId, usize)>,
    pub(super) original: usize,
    pub(super) seen: FxHashSet<BlockId>,
    pub(super) reachable: FxHashSet<BlockId>,
    pub(super) role_writes: FxHashSet<(BlockId, usize)>,
}
impl<'a> Check<'a> {
    pub(super) fn body(&self, id: BlockId) -> Result<&'a BasicBlock, String> {
        (number(id) < self.function.body.blocks.len())
            .then(|| &self.function.body.blocks[id])
            .ok_or_else(|| "实际CFG块越界".into())
    }
    pub(super) fn mark_frame_ops(&mut self, id: BlockId, start: usize, end: usize) {
        for index in start..end {
            if let Some(Statement::Assign { value, .. } | Statement::Eval(value)) =
                self.function.body.blocks[id].stmts.get(index)
                && matches!(value,Rvalue::Call {callee:Callee::Runtime(RuntimeFn::ArrayGet | RuntimeFn::ArraySet),args}
                    if matches!(args.first(),Some(Operand::Local(frame)) if *frame==self.frame))
            {
                self.frame_ops.insert((id, index));
            }
        }
    }
    pub(super) fn private(&self, id: LocalId, ty: MirType) -> Result<(), String> {
        if id.into_raw().into_u32() as usize <= self.original
            || crate::validate::local_decl(&self.function.body.locals, id)
                .is_none_or(|decl| decl.ty != ty || decl.is_cell)
        {
            Err("路由私有局部身份或类型非法".into())
        } else {
            Ok(())
        }
    }
    fn comparison(
        &mut self,
        id: BlockId,
        lhs: LocalId,
        op: BinOp,
        rhs: i64,
    ) -> Result<(BlockId, BlockId), String> {
        if !self.seen.insert(id) {
            return Err("路由实际比较重复或循环".into());
        }
        let value = self.body(id)?;
        let [
            Statement::Assign {
                dest,
                value:
                    Rvalue::Binary {
                        op: actual,
                        lhs: Operand::Local(left),
                        rhs: Operand::Const(Constant::I64(right)),
                    },
            },
        ] = value.stmts.as_slice()
        else {
            return Err("路由不是单一实际比较".into());
        };
        self.private(*dest, MirType::Bool)?;
        if *actual != op || *left != lhs || *right != rhs {
            return Err("路由实际比较操作数错误".into());
        }
        match value.terminator {
            Terminator::Branch {
                cond: Operand::Local(cond),
                then_,
                else_,
            } if cond == *dest => Ok((then_, else_)),
            _ => Err("比较结果未控制实际边".into()),
        }
    }
    fn restore(&mut self, layout: &crate::trace_lowering::GeneratorLayout) -> Result<(), String> {
        if self.function.body.entry != block(0) {
            return Err("恢复入口未重排到物理try起点".into());
        }
        let entry = self.body(block(0))?;
        let mut cursor = Cursor::new(&entry.stmts, self.original + 1);
        self.active = cursor.use_value(&Operand::Const(Constant::I64(-1)))?;
        self.private(self.active, MirType::I64)?;
        self.role_writes.insert((block(0), 0));
        for id in &layout.saved_locals {
            let ty = &self.function.body.locals[*id].ty;
            let dest = cursor.assign_call(
                RuntimeFn::ArrayGet,
                &[
                    Operand::Local(self.frame),
                    Operand::Const(Constant::I64(i64::from(id.into_raw().into_u32()) + 4)),
                ],
            )?;
            if *ty == MirType::Dynamic {
                if dest != *id {
                    return Err("恢复Dynamic槽身份错误".into());
                }
            } else {
                self.private(dest, MirType::Dynamic)?;
                let (out, value) = cursor.assignment()?;
                if out != *id
                    || !matches!(value,Rvalue::Unbox{value:Operand::Local(input),to}if *input==dest&&to==ty)
                {
                    return Err("恢复槽没有紧邻同类型Unbox".into());
                }
            }
        }
        // 当前普通provider生成器不走TraceV1；product变体仍消费真实activation深度。
        while let Some(Statement::Assign {
            value:
                Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TraceV1GeneratorDepth),
                    ..
                },
            ..
        }) = cursor.statements.get(cursor.position)
        {
            cursor.next()?;
        }
        let object = cursor.assign_call(
            RuntimeFn::ArrayGet,
            &[Operand::Local(self.frame), Operand::Const(Constant::I64(0))],
        )?;
        self.private(object, MirType::Dynamic)?;
        let state_position = cursor.position;
        let (state, value) = cursor.assignment()?;
        self.state = state;
        self.role_writes.insert((block(0), state_position));
        self.private(state, MirType::I64)?;
        if !matches!(value,Rvalue::Unbox{value:Operand::Local(input),to:MirType::I64}if *input==object)
        {
            return Err("恢复PC未读实际frame首槽".into());
        }
        cursor.end()?;
        self.mark_frame_ops(block(0), 0, entry.stmts.len());
        let Terminator::Goto(mut dispatch) = entry.terminator else {
            return Err("恢复入口未到实际PC分派".into());
        };
        for (pc, target) in layout.pc_targets.iter().skip(1) {
            let (yes, no) = self.comparison(dispatch, state, BinOp::Eq, *pc)?;
            if yes != *target {
                return Err("旧PC与重排后的真实handler目标不对应".into());
            }
            dispatch = no;
        }
        if layout.pc_targets.first() != Some(&(layout.initial_pc, dispatch)) {
            return Err("恢复默认边不是原入口".into());
        }
        Ok(())
    }
}

/// 路由证据只含内域块/局部；唯一跨域的typed目标与真实块一起由同一global映射重写。
pub(crate) fn remap(
    contracts: &[SourceHandlerContract],
    globals: &rustc_hash::FxHashMap<GlobalId, GlobalId>,
) -> Vec<SourceHandlerContract> {
    let mut evidence = rustc_hash::FxHashMap::default();
    contracts
        .iter()
        .map(|contract| {
            let mut contract = contract.clone();
            if let Some(old) = &contract.generator {
                let replacement = evidence.entry(Arc::as_ptr(old)).or_insert_with(|| {
                    let mut value = (**old).clone();
                    for target in value.typed_targets.iter_mut().flatten() {
                        if let Operand::Global(id) = target {
                            if let Some(replacement) = globals.get(id) {
                                *id = *replacement;
                            }
                        }
                    }
                    Arc::new(value)
                });
                contract.generator = Some(replacement.clone());
            }
            contract
        })
        .collect()
}

/// CFG清理可以删真正不可达块；实际handler与所有恢复PC必须通过最终真实边保持可达。
fn reachable(function: &Function) -> Result<FxHashSet<BlockId>, String> {
    let mut result = FxHashSet::default();
    let mut queue = vec![function.body.entry];
    queue.extend(function.body.regions.iter().map(|region| region.handler));
    while let Some(id) = queue.pop() {
        if number(id) >= function.body.blocks.len() {
            return Err("最终CFG目标越界".into());
        }
        if !result.insert(id) {
            continue;
        }
        match function.body.blocks[id].terminator {
            Terminator::Goto(target) | Terminator::Suspend { resume: target, .. } => {
                queue.push(target)
            }
            Terminator::Branch { then_, else_, .. } => {
                queue.push(then_);
                queue.push(else_);
            }
            _ => {}
        }
    }
    Ok(result)
}

/// 声明数据和坏IR均走显式拒绝；先证整数域再运算，防usize窗口溢出与i64截断。
fn window_end(
    route: &crate::trace_lowering::FrameCatchRoute,
    blocks: usize,
) -> Result<i64, String> {
    let length = i64::try_from(route.length).map_err(|_| "逻辑窗口长度超出PC整数域")?;
    let start = usize::try_from(route.start).map_err(|_| "逻辑窗口起点不是非负索引")?;
    let end = start.checked_add(route.length).ok_or("逻辑窗口索引溢出")?;
    if route.length == 0 || end > blocks {
        return Err("逻辑窗口越过实际源块".into());
    }
    route
        .start
        .checked_add(length)
        .ok_or_else(|| "逻辑窗口PC溢出".into())
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn oversized_and_out_of_range_windows_remain_diagnostics_after_ir_mutation() {
        for (start, length) in [(0, usize::MAX), (i64::MAX, 2), (-1, 1), (0, 0), (0, 10000)] {
            let mut program=crate::provider_namespace_tests::compile("def source():\n    try:\n        yield 1\n    except ValueError:\n        yield 2\n",true).unwrap();
            let function = program
                .functions
                .iter_mut()
                .find_map(|(_, function)| {
                    matches!(
                        function.module_state_source,
                        Some(InstanceStateSource::GeneratorFrame { .. })
                    )
                    .then_some(function)
                })
                .unwrap();
            let mut evidence = (**function.body.source_handler_contracts[0]
                .generator
                .as_ref()
                .unwrap())
            .clone();
            evidence.layout.exception_routes[0].start = start;
            evidence.layout.exception_routes[0].length = length;
            assert!(
                window_end(
                    &evidence.layout.exception_routes[0],
                    evidence.layout.original_blocks.len()
                )
                .is_err()
            );
            let evidence = Arc::new(evidence);
            for contract in &mut function.body.source_handler_contracts {
                contract.generator = Some(evidence.clone());
            }
            assert!(
                crate::validate::validate(&program).is_err(),
                "{start}/{length}"
            );
        }
    }
}
