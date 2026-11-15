//! test-only实际字段降级核查：从工厂/恢复MIR取证，核对原始每次读写与暂停PC，不以集合自洽代替。
use crate::local_access::{rewrite_statement, rewrite_terminator, statement_reads, terminator_reads};
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

type Check<T> = Result<T, &'static str>;

pub(super) struct Snapshot {
    pub(super) function: FnId,
    locals: Vec<(LocalId, LocalDecl)>,
    params: Vec<LocalId>,
    blocks: Vec<(BlockId, BasicBlock)>,
    entry: BlockId,
    residents: FxHashSet<LocalId>,
    origin: Option<Operand>,
}

impl Snapshot {
    pub(super) fn capture(program: &Program, function: FnId, residents: FxHashSet<LocalId>) -> Self {
        let f = &program.functions[function];
        Self {
            function,
            locals: f.body.locals.iter().map(|(id, l)| (id, l.clone())).collect(),
            params: f.params.clone(),
            blocks: f.body.blocks.iter().map(|(id, b)| (id, b.clone())).collect(),
            entry: f.body.entry,
            residents,
            origin: f.dynamic_origin.map(|origin| Operand::Local(origin.owner)),
        }
    }

    pub(super) fn verify(&self, program: &Program) -> Check<()> {
        let factory = &program.functions[self.function];
        if factory.params != self.params { return Err("工厂参数身份改变"); }
        let statements = &factory.body.blocks[factory.body.entry].stmts;
        let [Statement::Assign { dest: data, value: Rvalue::NewObject { class, args } },
            Statement::Assign { dest: result, value: Rvalue::MakeStateGenerator { resume, payload, initial_pc, origin } }] = statements.as_slice()
            else { return Err("工厂必须显式构造同一字段载体和恢复入口"); };
        if *origin != self.origin || *payload != Operand::Local(*data)
            || *initial_pc != i64::from(self.entry.into_raw().into_u32())
            || factory.body.blocks[factory.body.entry].terminator != Terminator::Return(Some(Operand::Local(*result)))
        { return Err("工厂载体/初始PC/返回身份错配"); }
        let state = &program.classes[*class];
        if !state.is_generator_state || state.fields.len() != self.residents.len() || args.len() != state.fields.len() {
            return Err("实际字段布局缺失或含多余字段");
        }
        let f = &program.functions[*resume];
        let [frame] = f.params.as_slice() else { return Err("恢复入口签名错误"); };
        let entry = &f.body.blocks[f.body.entry];
        let [Statement::Assign { dest: raw, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::GeneratorStateData), args: data_args } },
            Statement::Assign { dest: payload, value: Rvalue::Unbox { value: raw_value, to: MirType::Class(payload_class) } },
            Statement::Assign { dest: pc, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::GeneratorStateGetPC), args: pc_args } }] = entry.stmts.as_slice()
            else { return Err("恢复入口未取得同一个字段载体与PC"); };
        if *payload_class != *class || *raw_value != Operand::Local(*raw)
            || data_args.as_slice() != [Operand::Local(*frame)] || pc_args.as_slice() != [Operand::Local(*frame)]
        { return Err("恢复载体来源错配"); }
        // 不读取字段名字：参数关系来自实际构造实参，其他关系来自真实SetField写入的原LocalId。
        // 下方逐语句核查确保该写入紧跟原赋值，随后所有读取都使用同一映射。
        let mut slots = FxHashMap::default();
        let mut reverse = FxHashMap::default();
        let mut register = |local: LocalId, field: u32| -> Check<()> {
            let Some((_, declaration)) = self.locals.iter().find(|(id, _)| *id == local) else { return Err("字段映射引用外来局部"); };
            let Some((_, ty)) = state.fields.get(field as usize) else { return Err("字段号越界"); };
            if !self.residents.contains(&local) || declaration.ty != *ty
                || slots.get(&local).is_some_and(|previous| *previous != field)
                || reverse.get(&field).is_some_and(|previous| *previous != local)
            { return Err("字段映射冲突/类型改变/局部错配"); }
            slots.insert(local, field);
            reverse.insert(field, local);
            Ok(())
        };
        for (field, arg) in args.iter().enumerate() {
            if let Operand::Local(local) = arg {
                if !self.params.contains(local) { return Err("工厂实参不是原参数"); }
                register(*local, field as u32)?;
            }
        }
        for (_, block) in f.body.blocks.iter() {
            for statement in &block.stmts {
                if let Statement::SetField { obj, field, value: Operand::Local(local) } = statement {
                    if *obj != Operand::Local(*payload) { return Err("写回的字段载体不是同一实例"); }
                    register(*local, *field)?;
                }
            }
        }
        // 旧lowering先做暂停活跃性、再删除合成TracePush/Pop，可能留下一只未使用的深度字段。
        // 该死字段没有Python绑定/参数身份，也不得有任何实际读写；不用名字强行为它猜映射。
        let mut erased_types = Vec::new();
        for (local, declaration) in &self.locals {
            if !self.residents.contains(local) || slots.contains_key(local) { continue; }
            let erased_definition = self.blocks.iter().any(|(_, b)| b.stmts.iter().any(|s| {
                matches!(s, Statement::Assign { dest, .. } if dest == local) && trace_removed(s)
            }));
            let observed = self.blocks.iter().any(|(_, b)| {
                b.stmts.iter().filter(|s| !trace_removed(s)).any(|s| {
                    statement_reads(s).contains(local) || matches!(s, Statement::Assign { dest, .. } if dest == local)
                }) || terminator_reads(&b.terminator).contains(local)
            });
            if !erased_definition || observed || self.params.contains(local) || declaration.name.is_some() {
                return Err("真实读写不足以证明全部可观察resident映射");
            }
            erased_types.push(declaration.ty.clone());
        }
        for (field, (_, ty)) in state.fields.iter().enumerate() {
            if reverse.contains_key(&(field as u32)) { continue; }
            let Some(at) = erased_types.iter().position(|expected| expected == ty) else { return Err("未知未使用字段不属于删除的trace局部"); };
            erased_types.remove(at);
            if args[field] != initial_default(ty) { return Err("死trace字段初始化包含额外求值"); }
        }
        if !erased_types.is_empty() { return Err("删除的trace局部与未使用字段不匹配"); }
        for (&local, &field) in &slots {
            let ty = &state.fields[field as usize].1;
            let initial = if self.params.contains(&local) { Operand::Local(local) } else {
                initial_default(ty)
            };
            if args[field as usize] != initial { return Err("工厂未保存原参数引用或初始值"); }
        }
        let mut targets = vec![(self.entry, None)];
        for (_, block) in &self.blocks {
            if let Terminator::Suspend { resume, input, delegated: false, .. } = block.terminator {
                if targets.iter().any(|(target, _)| *target == resume) { return Err("共享恢复目标尚未证明"); }
                targets.push((resume, input));
            }
        }
        for (target, _) in &targets { self.check_dispatch(f, *pc, *target)?; }
        for (id, original) in &self.blocks {
            let lowered = &f.body.blocks[*id];
            let mut at = 0;
            // 原型仅接受genexpr的无send目标恢复；真实Input仍必须在resume首条消费None/注入。
            if targets.iter().skip(1).any(|(target, _)| target == id) {
                let Some(Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::GeneratorInput), args } }) = lowered.stmts.get(at)
                    else { return Err("恢复点缺少GeneratorInput"); };
                if args.as_slice() != [Operand::Local(*frame)] || self.locals.iter().any(|(old, _)| old == dest) {
                    return Err("恢复输入目标或来源错配");
                }
                at += 1;
            }
            for statement in &original.stmts {
                if trace_removed(statement) { continue; }
                let map = self.check_reads(f, lowered, &mut at, statement_reads(statement), &slots, *payload)?;
                let mut expected = statement.clone();
                rewrite_statement(&mut expected, &map);
                if lowered.stmts.get(at) != Some(&expected) { return Err("原语句读取被错误重写"); }
                at += 1;
                if let Statement::Assign { dest, .. } = statement
                    && let Some(field) = slots.get(dest)
                {
                    let expected = Statement::SetField { obj: Operand::Local(*payload), field: *field, value: Operand::Local(*dest) };
                    if lowered.stmts.get(at) != Some(&expected) { return Err("赋值后未写回对应真实字段"); }
                    at += 1;
                }
            }
            let map = self.check_reads(f, lowered, &mut at, terminator_reads(&original.terminator), &slots, *payload)?;
            let mut terminator = original.terminator.clone();
            rewrite_terminator(&mut terminator, &map);
            let expected = match terminator {
                Terminator::Suspend { value, resume, delegated: false, input: None } => {
                    check_pc(lowered, &mut at, *frame, i64::from(resume.into_raw().into_u32()))?;
                    Terminator::Return(Some(value))
                }
                Terminator::Return(value) => {
                    check_pc(lowered, &mut at, *frame, -1)?;
                    Terminator::Return(Some(value.unwrap_or(Operand::Const(Constant::None))))
                }
                Terminator::Suspend { .. } => return Err("复杂暂停未纳入字段证明"),
                other => other,
            };
            if at != lowered.stmts.len() || lowered.terminator != expected { return Err("暂停发布/返回值或额外语句不匹配"); }
        }
        Ok(())
    }

    fn check_reads(&self, f: &Function, block: &BasicBlock, at: &mut usize, reads: FxHashSet<LocalId>,
        slots: &FxHashMap<LocalId, u32>, payload: LocalId) -> Check<FxHashMap<LocalId, LocalId>> {
        let mut reads: Vec<_> = reads.into_iter().filter(|id| slots.contains_key(id)).collect();
        reads.sort_by_key(|id| id.into_raw().into_u32());
        let mut map = FxHashMap::default();
        for local in reads {
            let Some(Statement::Assign { dest, value: Rvalue::GetField { obj, field } }) = block.stmts.get(*at)
                else { return Err("真实读取未经过已证明字段"); };
            if *obj != Operand::Local(payload) || *field != slots[&local]
                || f.body.locals[*dest].ty != f.body.locals[local].ty
                || self.locals.iter().any(|(old, _)| old == dest)
            { return Err("字段读取映射/类型/临时身份错误"); }
            map.insert(local, *dest);
            *at += 1;
        }
        Ok(map)
    }

    fn check_dispatch(&self, f: &Function, pc: LocalId, target: BlockId) -> Check<()> {
        let mut block = f.body.entry;
        let value = i64::from(target.into_raw().into_u32());
        for _ in 0..=f.body.blocks.len() {
            if self.blocks.iter().any(|(id, _)| *id == block) {
                return if block == target { Ok(()) } else { Err("PC派发到错误原块") };
            }
            let b = &f.body.blocks[block];
            block = match &b.terminator {
                Terminator::Goto(next) => *next,
                Terminator::Branch { cond: Operand::Local(condition), then_, else_ } => {
                    let [Statement::Assign { dest, value: Rvalue::Binary { op: BinOp::Eq, lhs: Operand::Local(read_pc), rhs: Operand::Const(Constant::I64(expected)) } }] = b.stmts.as_slice()
                        else { return Err("PC分派不是显式比较"); };
                    if dest != condition || *read_pc != pc { return Err("PC比较值来源错配"); }
                    if value == *expected { *then_ } else { *else_ }
                }
                _ => return Err("PC派发形态错误"),
            };
        }
        Err("PC派发未收敛")
    }
}

fn check_pc(block: &BasicBlock, at: &mut usize, frame: LocalId, pc: i64) -> Check<()> {
    let expected = Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::GeneratorStateSetPC),
        args: vec![Operand::Local(frame), Operand::Const(Constant::I64(pc))] });
    if block.stmts.get(*at) != Some(&expected) { return Err("yield/return未发布精确PC"); }
    *at += 1;
    Ok(())
}

fn trace_removed(statement: &Statement) -> bool {
    matches!(statement,
        Statement::Assign { value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin), .. }, .. }
        | Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::TracePopTo), .. }))
}


fn initial_default(ty: &MirType) -> Operand {
    Operand::Const(match ty {
        MirType::I64 => Constant::I64(0),
        MirType::F64 => Constant::F64(0.0),
        MirType::Bool => Constant::Bool(false),
        MirType::Str => Constant::Str(String::new()),
        _ => Constant::Null,
    })
}
