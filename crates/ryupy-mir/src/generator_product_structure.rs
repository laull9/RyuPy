//! 最后高层输入→实际低层输出的独立结构检验。只走实际块、操作数及相邻局部定义；
//! 不调用 lowering 重建输出，不信 GeneratorLayout，不求 locals×slots 或跨 Retry 固定点。
use crate::generator_product_cursor::{block, local, number, same, Cursor};
use crate::generator_product_snapshot::{Source, SourceStatement};
use crate::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub(super) struct CheckedGenerator { pub factory: FnId, pub resume: FnId }

pub(super) struct Checker<'a> {
    pub source: &'a Source,
    pub body: &'a Body,
    pub frame: LocalId,
    pub seen: FxHashSet<BlockId>,
    pub active: Option<LocalId>,
    pub result: Option<LocalId>,
    pub next_pc: Option<LocalId>,
    pub state: LocalId,
    pub protected: usize,
}

pub(super) fn inspect(program: &Program, source: &Source,
    exceptions: &FxHashMap<String, GlobalId>) -> Result<CheckedGenerator, String> {
    let factory = &program.functions[source.factory];
    if factory.name != source.name || factory.source_file != source.file || factory.params != source.params
        || factory.param_meta != source.parameter_metadata
        || super::generator_product_identity::FunctionIdentity::capture(factory) != source.metadata
        || factory.env != source.environment || factory.module_state != source.module_state
        || !same(&factory.dynamic_origin.map(|s| Operand::Local(s.owner)), &source.origin)
        || factory.body.blocks.len() != 1 || !factory.body.regions.is_empty() {
        return Err("工厂实际参数/env/state/owner/定义身份或控制流改变".into());
    }
    let factory_block = &factory.body.blocks[factory.body.entry];
    let mut cursor = Cursor::new(&factory_block.stmts, source.locals.len());
    let initial = cursor.boxed(Operand::Const(Constant::I64(number(source.entry) as i64)), &MirType::I64)?;
    let mut initial_slots = vec![initial, Operand::Const(Constant::None), Operand::Const(Constant::None), Operand::Const(Constant::None)];
    for (index, ty) in source.locals.iter().enumerate() {
        let slot = local(index);
        let value = if source.params.contains(&slot) { Operand::Local(slot) } else {
            Operand::Const(match ty {
                MirType::I64 => Constant::I64(0), MirType::F64 => Constant::F64(0.0),
                MirType::Bool => Constant::Bool(false), MirType::Str => Constant::Str(String::new()),
                _ => Constant::Null,
            })
        };
        initial_slots.push(if matches!(value, Operand::Const(Constant::Null)) { value }
            else { cursor.boxed(value, ty)? });
    }
    let (result, make) = cursor.assignment()?;
    let Rvalue::MakeGenerator { resume, frame, origin } = make else { return Err("工厂没有唯一实际 MakeGenerator".into()); };
    if !same(frame, &initial_slots) || !same(origin, &source.origin)
        || !matches!(&factory_block.terminator, Terminator::Return(Some(Operand::Local(value))) if *value == result) {
        return Err("工厂首槽、初始参数、来源或返回对象被替换".into());
    }
    cursor.end()?;
    let resume_id = *resume;
    let resume = &program.functions[resume_id];
    let frame = local(source.locals.len());
    let mut expected = source.metadata.clone();
    expected.params = vec![frame];
    expected.ret = MirType::Dynamic;
    expected.metadata.clear();
    expected.codegen_name = format!("{}$resume{}", source.metadata.codegen_name, source.factory.into_raw().into_u32());
    expected.definition = None;
    expected.function_global = None;
    expected.specialization_global = None;
    expected.defaults_global = None;
    expected.signature_global = None;
    expected.environment = None;
    expected.origin = None;
    if super::generator_product_identity::FunctionIdentity::capture(resume) != expected
        || resume.body.locals[frame].ty != MirType::Dynamic || resume.body.locals[frame].is_cell
        || !resume.body.source_handler_contracts.is_empty()
        || resume.params != [frame] || resume.ret != MirType::Dynamic || resume.name != source.name
        || resume.source_file != source.file || resume.env.is_some() || resume.dynamic_origin.is_some()
        || resume.module_state != source.module_state {
        return Err("实际恢复入口不是唯一私有 frame ABI".into());
    }
    for (index, expected) in source.local_declarations.iter().enumerate() {
        if factory.body.locals[local(index)] != *expected
            || resume.body.locals[local(index)] != *expected {
            return Err("原局部类型/身份/cell声明被改写".into());
        }
    }
    let mut checker = Checker { source, body: &resume.body, frame, seen: FxHashSet::default(),
        active: None, result: None, next_pc: None, state: frame, protected: 0 };
    let targets = checker.targets();
    checker.protected = source.blocks.len() + targets.len();
    checker.restore(&targets)?;
    checker.source_blocks()?;
    if source.regions.is_empty() {
        if !checker.body.regions.is_empty() { return Err("无 EH 源生成器增加了异常区域".into()); }
    } else {
        crate::generator_product_routes::inspect(&mut checker, exceptions)?;
    }
    if checker.seen.len() != checker.body.blocks.len() { return Err("低层含未核对的控制块/隐藏调用".into()); }
    checker.freshness(&factory.body, false)?;
    checker.freshness(checker.body, true)?;
    Ok(CheckedGenerator { factory: source.factory, resume: resume_id })
}

impl<'a> Checker<'a> {
    pub fn take(&mut self, id: BlockId) -> Result<&'a BasicBlock, String> {
        if !self.seen.insert(id) { return Err("私有模板控制块被重复引用或出现额外入口".into()); }
        self.body.blocks.iter().find_map(|(actual, value)| (actual == id).then_some(value))
            .ok_or_else(|| "私有模板目标越界".into())
    }
    pub fn mapped(&self, id: BlockId) -> BlockId { block(number(id) + usize::from(!self.source.regions.is_empty())) }
    pub fn private(&self, id: LocalId, ty: MirType) -> Result<(), String> {
        if id.into_raw().into_u32() as usize <= self.source.locals.len()
            || self.body.locals[id].ty != ty { Err("私有模板覆盖frame/原局部或类型错误".into()) } else { Ok(()) }
    }
    fn targets(&self) -> Vec<BlockId> {
        let mut result = vec![self.source.entry];
        for region in &self.source.regions { if !result.contains(&region.handler) { result.push(region.handler); } }
        for source in &self.source.blocks {
            if let Terminator::Suspend { resume, .. } = source.terminator
                && !result.contains(&resume) { result.push(resume); }
        }
        result
    }
    fn restore(&mut self, targets: &[BlockId]) -> Result<(), String> {
        let expected_entry = if self.source.regions.is_empty() { block(self.protected - 1) } else { block(0) };
        if self.body.entry != expected_entry { return Err("恢复入口位置/重排不符".into()); }
        let entry = self.take(expected_entry)?;
        let mut cursor = Cursor::new(&entry.stmts, self.source.locals.len() + 1);
        if !self.source.regions.is_empty() {
            let active = cursor.use_value(&Operand::Const(Constant::I64(-1)))?;
            self.private(active, MirType::I64)?; self.active = Some(active);
        }
        for (index, ty) in self.source.locals.iter().enumerate() {
            let dest = cursor.assign_call(RuntimeFn::ArrayGet, &[Operand::Local(self.frame), Operand::Const(Constant::I64(index as i64 + 4))])?;
            if *ty == MirType::Dynamic {
                if dest != local(index) { return Err("恢复槽没有写回相同 Dynamic 局部".into()); }
            } else {
                self.private(dest, MirType::Dynamic)?;
                let (out, value) = cursor.assignment()?;
                if out != local(index) || !matches!(value, Rvalue::Unbox { value: Operand::Local(input), to } if *input == dest && to == ty) {
                    return Err("恢复槽缺少紧邻同类型 Unbox".into());
                }
            }
        }
        if self.source.depths.len() != 1 { return Err("高层没有唯一真实 TracePush 深度".into()); }
        if cursor.assign_call(RuntimeFn::TraceV1GeneratorDepth, &[])? != self.source.depths[0] {
            return Err("恢复后没有覆盖旧 activation 深度".into());
        }
        let state_object = cursor.assign_call(RuntimeFn::ArrayGet, &[Operand::Local(self.frame), Operand::Const(Constant::I64(0))])?;
        self.private(state_object, MirType::Dynamic)?;
        let (state, value) = cursor.assignment()?;
        self.private(state, MirType::I64)?;
        if !matches!(value, Rvalue::Unbox { value: Operand::Local(input), to: MirType::I64 } if *input == state_object) {
            return Err("PC未从实际首槽拆箱".into());
        }
        self.state = state; cursor.end()?;
        let Terminator::Goto(mut dispatch) = entry.terminator else { return Err("恢复入口未到唯一PC分派".into()); };
        for target in targets.iter().skip(1) {
            let checked = self.take(dispatch)?;
            let (yes, no) = self.comparison(checked, state, BinOp::Eq, number(*target) as i64)?;
            if yes != self.mapped(*target) { return Err("PC整数与重排后的实际目标不对应".into()); }
            dispatch = no;
        }
        if dispatch != self.mapped(self.source.entry) { return Err("PC默认边不是原入口".into()); }
        Ok(())
    }
    pub fn comparison(&self, value: &BasicBlock, lhs: LocalId, op: BinOp, rhs: i64) -> Result<(BlockId, BlockId), String> {
        let [Statement::Assign { dest, value: Rvalue::Binary { op: actual_op, lhs: Operand::Local(actual_lhs), rhs: Operand::Const(Constant::I64(actual_rhs)) } }] = value.stmts.as_slice() else {
            return Err("私有分派不是单一真实比较".into());
        };
        self.private(*dest, MirType::Bool)?;
        if *actual_op != op || *actual_lhs != lhs || *actual_rhs != rhs { return Err("私有分派比较参数错误".into()); }
        if let Terminator::Branch { cond: Operand::Local(cond), then_, else_ } = value.terminator
            && cond == *dest { Ok((then_, else_)) } else { Err("私有比较结果未控制实际边".into()) }
    }
    fn source_blocks(&mut self) -> Result<(), String> {
        let original = self.source;
        for (index, source) in original.blocks.iter().enumerate() {
            let checked = self.take(self.mapped(block(index)))?;
            let mut cursor = Cursor::new(&checked.stmts, self.source.locals.len() + 1);
            if let Some(active) = self.active
                && cursor.use_value(&Operand::Const(Constant::I64(index as i64)))? != active {
                return Err("源块没有先写实际active PC".into());
            }
            for predecessor in &self.source.blocks {
                if let Terminator::Suspend { resume, input, delegated, .. } = &predecessor.terminator
                    && number(*resume) == index {
                    if *delegated { return Err("V1不接受委托恢复".into()); }
                    let dest = cursor.assign_call(RuntimeFn::GeneratorInput, &[Operand::Local(self.frame)])?;
                    if let Some(expected) = input { if dest != *expected { return Err("yield输入接到错误局部".into()); } }
                    else { self.private(dest, MirType::Dynamic)?; }
                }
            }
            for statement in &source.statements {
                match statement {
                    SourceStatement::Operation(expected) => if cursor.next()? != expected {
                        return Err("源操作/操作数/行号被末端lowering改变".into());
                    },
                    SourceStatement::Catch(expected) => {
                        if cursor.assign_call(RuntimeFn::ArrayGet, &[Operand::Local(self.frame), Operand::Const(Constant::I64(3))])? != *expected {
                            return Err("逻辑Catch读取了错误对象或局部".into());
                        }
                        cursor.slot(self.frame, 3, Operand::Const(Constant::None))?;
                    }
                }
            }
            match &source.terminator {
                Terminator::Suspend { value, resume, .. } => {
                    cursor.spill(self.frame, &self.source.locals)?;
                    cursor.pc(self.frame, Operand::Const(Constant::I64(number(*resume) as i64)))?;
                    self.exit(&mut cursor, &checked.terminator, value.clone())?;
                }
                Terminator::Return(value) => {
                    cursor.pc(self.frame, Operand::Const(Constant::I64(-1)))?;
                    self.exit(&mut cursor, &checked.terminator, value.clone().unwrap_or(Operand::Const(Constant::None)))?;
                }
                other => {
                    let mut expected = other.clone();
                    match &mut expected {
                        Terminator::Goto(target) => *target = self.mapped(*target),
                        Terminator::Branch { then_, else_, .. } => { *then_ = self.mapped(*then_); *else_ = self.mapped(*else_); }
                        _ => {}
                    }
                    if !same(&expected, &checked.terminator) { return Err("源控制流或Raise对象改变".into()); }
                }
            }
            cursor.end()?;
        }
        Ok(())
    }
    fn exit(&mut self, cursor: &mut Cursor<'_>, term: &Terminator, value: Operand) -> Result<(), String> {
        if self.source.regions.is_empty() {
            if !same(term, &Terminator::Return(Some(value))) { return Err("普通yield/complete返回值错误".into()); }
        } else {
            let result = cursor.use_value(&value)?; self.private(result, MirType::Dynamic)?;
            if self.result.is_some_and(|old| old != result) { return Err("yield/complete没有统一出口值".into()); }
            self.result = Some(result);
            if !matches!(term, Terminator::Goto(target) if *target == block(self.protected)) { return Err("yield/complete未离开保护区".into()); }
        }
        Ok(())
    }
    fn freshness(&self, body: &Body, resume: bool) -> Result<(), String> {
        let mut counts = FxHashMap::default();
        let mut boxes = FxHashSet::default();
        for (_, block) in body.blocks.iter() {
            for statement in &block.stmts {
                if let Statement::Assign { dest, value } = statement {
                    let result = match value {
                        Rvalue::Box { .. } | Rvalue::MakeGenerator { .. } | Rvalue::CatchException => Some(MirType::Dynamic),
                        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArrayGet | RuntimeFn::GeneratorInput | RuntimeFn::GeneratorRetry | RuntimeFn::TypeObject | RuntimeFn::DynBuiltin), .. } => Some(MirType::Dynamic),
                        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::DynTruthy), .. } => Some(MirType::Bool),
                        Rvalue::Unbox { to, .. } => Some(to.clone()),
                        _ => None,
                    };
                    if result.is_some_and(|ty| body.locals[*dest].ty != ty) { return Err("实际模板局部返回类型错误".into()); }
                    if resume && *dest == self.frame { return Err("私有frame参数被覆盖".into()); }
                    if dest.into_raw().into_u32() as usize >= self.source.locals.len() {
                        *counts.entry(*dest).or_insert(0) += 1;
                        if matches!(value, Rvalue::Box { .. }) { boxes.insert(*dest); }
                    }
                }
            }
        }
        if counts.iter().any(|(id, count)| *count > 1 && (boxes.contains(id)
            || !resume || ![self.active, self.result, self.next_pc].contains(&Some(*id)))) {
            return Err("模板临时重复定义，可能复用过期Box或覆盖实际操作数".into());
        }
        Ok(())
    }
}
