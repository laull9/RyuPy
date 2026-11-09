//! 只认证模块同块真实def的内部Null默认槽冷发布，不授予一般Function标签元数据读取权。
//! 全程序一次建局部/发布/槽角色索引；每条producer→getter→发布链至多128条语句/别名。
use crate::generator_product_failure::{Failure, Limitations};
use crate::*;
use rustc_hash::{FxHashMap, FxHashSet};

#[path = "generator_product_defaults_construct.rs"]
mod construct;
pub(super) use construct::make_function;

type Site = (FnId, BlockId, usize);
const LIMIT: usize = 128;
fn error(reason: &str) -> Failure {
    format!("FunctionDefaults合同：{reason}").into()
}
fn unsupported(reason: &str) -> Failure {
    Failure::unsupported(format!("FunctionDefaults合同：{reason}"))
}

#[derive(Default)]
pub(super) struct Defaults {
    getters: FxHashMap<(FnId, LocalId), FnId>,
    locals: FxHashSet<(FnId, LocalId)>,
    slots: FxHashSet<GlobalId>,
    allowed: FxHashSet<Site>,
    // 部分检查结果不是证书；未证明的getter没有Value，成功prepare之前必须清空能力拒绝。
    pending: FxHashSet<(FnId, LocalId)>,
    // 只有输出扫描确实撞到128窗口，才允许该链边界的纯别名保持能力不足。
    frontier: FxHashSet<(FnId, LocalId)>,
}

struct Index<'a> {
    program: &'a Program,
    assignments: FxHashMap<(FnId, LocalId), Vec<(BlockId, usize)>>,
    writes: FxHashMap<GlobalId, Vec<Site>>,
    roles: FxHashMap<GlobalId, usize>,
    exported: FxHashSet<GlobalId>,
    default_slots: FxHashSet<GlobalId>,
    targets: FxHashMap<GlobalId, FnId>,
    slots: FxHashSet<GlobalId>,
}
impl<'a> Index<'a> {
    fn new(program: &'a Program, exports: &[ModuleExport]) -> Self {
        let mut result = Self {
            program,
            assignments: FxHashMap::default(),
            writes: FxHashMap::default(),
            roles: FxHashMap::default(),
            exported: FxHashSet::default(),
            default_slots: FxHashSet::default(),
            targets: FxHashMap::default(),
            slots: program
                .globals
                .iter()
                .filter_map(|(id, slot)| {
                    (slot.ty == MirType::Dynamic && slot.storage == GlobalStorage::LegacyStatic)
                        .then_some(id)
                })
                .collect(),
        };
        for export in exports {
            result.exported.insert(export.value);
            if let ExportGuard::BoundFlag(flag) = export.guard {
                result.exported.insert(flag);
            }
        }
        for (id, function) in program.functions.iter() {
            result.default_slots.extend(function.defaults_global);
            if let Some(slot) = function.defaults_global {
                result.targets.insert(slot, id);
            }
            for slot in [
                function.defaults_global,
                function.function_global,
                function.signature_global,
                function.specialization_identity_global,
                function
                    .dynamic_origin
                    .and_then(|origin| origin.source_global),
            ]
            .into_iter()
            .flatten()
            {
                *result.roles.entry(slot).or_default() += 1;
            }
            for (block_id, block) in function.body.blocks.iter() {
                for (position, statement) in block.stmts.iter().enumerate() {
                    match statement {
                        Statement::Assign { dest, .. } => result
                            .assignments
                            .entry((id, *dest))
                            .or_default()
                            .push((block_id, position)),
                        Statement::SetGlobal { dest, .. } => result
                            .writes
                            .entry(*dest)
                            .or_default()
                            .push((id, block_id, position)),
                        _ => {}
                    }
                }
            }
        }
        for (_, class) in program.classes.iter() {
            if let Some(slot) = class.factory_global {
                *result.roles.entry(slot).or_default() += 1;
            }
        }
        result
    }
    fn private_obligations(&self) -> Result<(), Failure> {
        for slot in &self.default_slots {
            if !self.slots.contains(slot)
                || self.roles.get(slot) != Some(&1)
                || self.exported.contains(slot)
            {
                return Err(error("私有槽角色冲突、公开导出或无效GlobalId"));
            }
            match self.writes.get(slot).map(Vec::as_slice) {
                // 类方法的通用槽可保持CLR null；零写不是获准读取的值能力。
                None | Some([_]) => {}
                _ => return Err(error("私有槽重复或错误发布")),
            }
        }
        Ok(())
    }
    // 必发布来自当前getter的真实receiver，不把所有声明角色误当普通def。
    // 先核全索引的缺/错/重复写，再允许输出窗口返回能力不足。
    fn required_publication(&self, site: Site, target: FnId) -> Result<(), Failure> {
        let slot = self.program.functions[target]
            .defaults_global
            .ok_or_else(|| error("真实getter目标缺少默认槽"))?;
        match self.writes.get(&slot).map(Vec::as_slice) {
            Some([(function, block, at)])
                if (*function, *block) == (site.0, site.1) && *at > site.2 =>
            {
                Ok(())
            }
            None => Err(error("结果未沿同块私有发布链消费：实际索引缺少发布")),
            _ => Err(error("私有槽重复或错误发布")),
        }
    }
    // 能力不足的getter仍不能替另一条私有写背书。复用同一真实定义索引，
    // 仅沿该写的Use/Box追到本次未证明getter；未知尾部只保留能力拒绝，不授信。
    fn pending_publication(
        &self,
        site: Site,
        operand: &Operand,
        pending: &FxHashSet<(FnId, LocalId)>,
    ) -> Result<(), Failure> {
        let (function, block, mut before) = site;
        // 局部游标可缩短输入与索引的借用，不要求调用者的操作数活到整个Index生命周期。
        let mut value = operand;
        for _ in 0..LIMIT {
            let Operand::Local(local) = value else {
                return Err(error("私有槽存在未认证写入"));
            };
            if pending.contains(&(function, *local)) {
                return Ok(());
            }
            let (defined_block, at) = self.unique(function, *local)?;
            if defined_block != block || at >= before {
                return Err(error("私有槽存在未认证写入"));
            }
            match &self.program.functions[function].body.blocks[block].stmts[at] {
                Statement::Assign {
                    value:
                        Rvalue::Use(input)
                        | Rvalue::Box {
                            value: input,
                            from: MirType::Dynamic,
                        },
                    ..
                } => value = input,
                _ => return Err(error("私有槽存在未认证写入")),
            }
            before = at;
        }
        Err(unsupported("私有发布别名链超过128上界"))
    }
    // 复用实际定义索引检查consumer的纯别名来源；只用于否定opaque逃逸，不返回函数能力。
    // 每次最多128步，无固定点/heap；超预算保域外，不能把未证尾部当作已证明安全。
    fn aliases(
        &self,
        function: FnId,
        mut local: LocalId,
        roots: &FxHashSet<(FnId, LocalId)>,
    ) -> Result<bool, Failure> {
        for _ in 0..LIMIT {
            if roots.contains(&(function, local)) {
                return Ok(true);
            }
            let Some([(block, at)]) = self.assignments.get(&(function, local)).map(Vec::as_slice)
            else {
                return Ok(false);
            };
            match &self.program.functions[function].body.blocks[*block].stmts[*at] {
                Statement::Assign {
                    value:
                        Rvalue::Use(Operand::Local(input))
                        | Rvalue::Box {
                            value: Operand::Local(input),
                            from: MirType::Dynamic,
                        },
                    ..
                } => local = *input,
                _ => return Ok(false),
            }
        }
        Err(unsupported("opaque消费者纯别名超过128检查上界"))
    }
    fn unique(&self, function: FnId, local: LocalId) -> Result<(BlockId, usize), Failure> {
        let body = &self.program.functions[function].body;
        if crate::validate::local_decl(&body.locals, local)
            .is_none_or(|decl| decl.ty != MirType::Dynamic)
            || self.program.functions[function].params.contains(&local)
        {
            return Err(error("局部不是独立Dynamic定义"));
        }
        match self.assignments.get(&(function, local)).map(Vec::as_slice) {
            Some([site]) => Ok(*site),
            _ => Err(error("局部缺少唯一实际定义或被覆盖")),
        }
    }
    fn producer(
        &self,
        site: Site,
        operand: &Operand,
        limitations: &mut Limitations,
    ) -> Result<(usize, FnId), Failure> {
        let (function, block, mut limit) = site;
        let mut value = operand;
        for _ in 0..LIMIT {
            let Operand::Local(local) = value else {
                return Err(error("receiver不是同块真实MakeFunction局部"));
            };
            let (at, position) = self.unique(function, *local)?;
            if at != block || position >= limit {
                return Err(error("receiver定义跨块或晚于读取"));
            }
            let Statement::Assign {
                value: definition, ..
            } = &self.program.functions[function].body.blocks[at].stmts[position]
            else {
                return Err(error("receiver缺少实际赋值"));
            };
            match definition {
                Rvalue::Use(input)
                | Rvalue::Box {
                    value: input,
                    from: MirType::Dynamic,
                } => {
                    value = input;
                    limit = position;
                }
                Rvalue::MakeFunction { func, .. } => {
                    // 能力未迁入不抹掉已核真实FnId；capture最终仍须finish才能授信。
                    limitations.check(make_function(self.program, definition))?;
                    return Ok((position, *func));
                }
                _ => return Err(error("receiver没有同块真实MakeFunction来源")),
            }
        }
        Err(unsupported("同块别名链超过128上界"))
    }
}

impl Defaults {
    #[cfg(test)]
    pub fn inspect(program: &Program, exports: &[ModuleExport]) -> Result<Self, Failure> {
        let mut limitations = Limitations::default();
        let result = Self::inspect_partial(program, exports, &mut limitations)?;
        limitations.finish()?;
        Ok(result)
    }
    pub fn inspect_partial(
        program: &Program,
        exports: &[ModuleExport],
        limitations: &mut Limitations,
    ) -> Result<Self, Failure> {
        let index = Index::new(program, exports);
        index.private_obligations()?;
        let mut result = Self {
            slots: index.default_slots.clone(),
            ..Self::default()
        };
        for (function, definition) in program.functions.iter() {
            for (block_id, block) in definition.body.blocks.iter() {
                for (position, statement) in block.stmts.iter().enumerate() {
                    let (dest, value) = match statement {
                        Statement::Assign { dest, value } => (Some(*dest), value),
                        Statement::Eval(value) => (None, value),
                        _ => continue,
                    };
                    if let Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::FunctionDefaults),
                        args,
                    } = value
                    {
                        let dest = dest.ok_or_else(|| error("getter结果未存入私有局部"))?;
                        // 失败链的局部状态不混进已证明链；仍继续检查随后每个实际getter。
                        let mut candidate = Self::default();
                        if limitations
                            .check(candidate.capture(
                                &index,
                                (function, block_id, position),
                                dest,
                                args,
                            ))?
                            .is_some()
                        {
                            result.getters.extend(candidate.getters);
                            result.locals.extend(candidate.locals);
                            result.allowed.extend(candidate.allowed);
                        } else {
                            // 未证producer不抹掉已检查到的opaque输出前缀，独立逃逸仍硬停。
                            result.locals.extend(candidate.locals);
                            result.allowed.extend(candidate.allowed);
                            result.pending.insert((function, dest));
                            result.frontier.extend(candidate.frontier);
                        }
                    }
                }
            }
        }
        result.consumers(&index, limitations)?;
        Ok(result)
    }
    fn capture(
        &mut self,
        index: &Index<'_>,
        site: Site,
        dest: LocalId,
        args: &[Operand],
    ) -> Result<(), Failure> {
        let (function, block_id, position) = site;
        let [receiver] = args else {
            return Err(error("getter实参不是单个receiver"));
        };
        if function != index.program.entry {
            return Err(error("getter不在模块def冷发布入口"));
        }
        index.unique(function, dest)?;
        // 两侧能力不足互不遮硬错；真实receiver先核required写，opaque输出独立保护。
        let mut limitations = Limitations::default();
        let actual = index.producer(site, receiver, &mut limitations);
        let actual = limitations.check(actual)?;
        if let Some((_, target)) = actual {
            index.required_publication(site, target)?;
        }
        let published = limitations.check(self.publication(index, site, dest))?;
        let Some((producer, actual_target)) = actual else {
            return limitations.finish();
        };
        let Some((publish, target)) = published else {
            return limitations.finish();
        };
        if actual_target != target {
            return Err(error("私有槽重复或错误发布"));
        }
        if publish - producer > LIMIT {
            return Err(unsupported("同块发布跨度超过128上界"));
        }
        let block = &index.program.functions[function].body.blocks[block_id];
        for at in producer + 1..position {
            let valid = match &block.stmts[at] {
                Statement::Assign {
                    dest,
                    value: Rvalue::Use(_) | Rvalue::Box { .. },
                } => {
                    index.producer(
                        (function, block_id, at + 1),
                        &Operand::Local(*dest),
                        &mut limitations,
                    )? == (producer, target)
                }
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::FunctionModule),
                    args,
                }) => {
                    args.len() == 2
                        && index.producer((function, block_id, at), &args[0], &mut limitations)?
                            == (producer, target)
                }
                _ => false,
            };
            if !valid {
                return Err(unsupported("producer与getter之间有未认证操作"));
            }
        }
        limitations.finish()?;
        self.getters.insert((function, dest), target);
        Ok(())
    }
    fn publication(
        &mut self,
        index: &Index<'_>,
        site: Site,
        dest: LocalId,
    ) -> Result<(usize, FnId), Failure> {
        let (function, block_id, position) = site;
        let block = &index.program.functions[function].body.blocks[block_id];
        // locals只记录opaque结果身份；实际target必须由唯一私有写取得，不能伪造函数值。
        self.locals.insert((function, dest));
        self.allowed.insert(site);
        let mut aliases = FxHashSet::default();
        aliases.insert(dest);
        for at in position + 1..block.stmts.len().min(position + LIMIT + 1) {
            match &block.stmts[at] {
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Use(Operand::Local(input))
                        | Rvalue::Box {
                            value: Operand::Local(input),
                            from: MirType::Dynamic,
                        },
                } if aliases.contains(input) => {
                    index.unique(function, *dest)?;
                    aliases.insert(*dest);
                    self.locals.insert((function, *dest));
                    self.allowed.insert((function, block_id, at));
                }
                Statement::SetGlobal {
                    dest,
                    value: Operand::Local(input),
                } if aliases.contains(input) => {
                    let target = *index
                        .targets
                        .get(dest)
                        .ok_or_else(|| error("结果未沿同块私有发布链消费"))?;
                    if index.writes.get(dest).map(Vec::as_slice)
                        != Some(&[(function, block_id, at)][..])
                    {
                        return Err(error("私有槽重复或错误发布"));
                    }
                    self.allowed.insert((function, block_id, at));
                    return Ok((at, target));
                }
                _ => return Err(error("结果未沿同块私有发布链消费")),
            }
        }
        if position + LIMIT + 1 < block.stmts.len() {
            self.frontier
                .extend(aliases.into_iter().map(|local| (function, local)));
            Err(unsupported("唯一实际发布在128能力窗口之外"))
        } else {
            Err(error("结果缺少有界同块私有发布"))
        }
    }
    pub fn getter(&self, function: FnId, dest: Option<LocalId>) -> Result<FnId, Failure> {
        dest.and_then(|dest| self.getters.get(&(function, dest)).copied())
            .ok_or_else(|| {
                if dest.is_some_and(|dest| self.pending.contains(&(function, dest))) {
                    unsupported("getter尚无完整同块发布能力")
                } else {
                    error("getter没有实际同块发布证明")
                }
            })
    }
    pub fn private_slot(&self, slot: GlobalId) -> bool {
        self.slots.contains(&slot)
    }

    fn opaque(
        &self,
        index: &Index<'_>,
        function: FnId,
        local: LocalId,
        limitations: &mut Limitations,
    ) -> Result<bool, Failure> {
        if self.locals.contains(&(function, local)) {
            return Ok(true);
        }
        if self.frontier.is_empty() {
            return Ok(false);
        }
        Ok(limitations
            .check(index.aliases(function, local, &self.frontier))?
            .unwrap_or(false))
    }
    // 复用既有穷尽Rvalue读操作数走查，不为opaque结果建立第二套数据流。
    fn consumers(&self, index: &Index<'_>, limitations: &mut Limitations) -> Result<(), Failure> {
        let program = index.program;
        use crate::passes::dead_values::{read_operand, read_value};
        for (function, definition) in program.functions.iter() {
            for (block_id, block) in definition.body.blocks.iter() {
                for (position, statement) in block.stmts.iter().enumerate() {
                    let pending_write = matches!(statement, Statement::SetGlobal { dest, .. }
                        if self.slots.contains(dest) && !self.allowed.contains(&(function, block_id, position)));
                    if pending_write && let Statement::SetGlobal { value, .. } = statement {
                        limitations.check(index.pending_publication(
                            (function, block_id, position),
                            value,
                            &self.pending,
                        ))?;
                    }
                    let mut reads = FxHashSet::default();
                    let dest = match statement {
                        Statement::Assign { dest, value } => {
                            read_value(value, &mut reads);
                            Some(*dest)
                        }
                        Statement::Eval(value) => {
                            read_value(value, &mut reads);
                            None
                        }
                        Statement::SetGlobal { value, .. } => {
                            read_operand(value, &mut reads);
                            None
                        }
                        Statement::SetField { obj, value, .. } => {
                            read_operand(obj, &mut reads);
                            read_operand(value, &mut reads);
                            None
                        }
                        Statement::Line(_) => None,
                    };
                    let mut opaque = false;
                    for local in reads.iter().copied().chain(dest) {
                        opaque |= self.opaque(index, function, local, limitations)?;
                    }
                    if opaque
                        && !self.allowed.contains(&(function, block_id, position))
                        && !pending_write
                    {
                        let deferred_alias = if let Statement::Assign {
                            dest,
                            value:
                                Rvalue::Use(Operand::Local(input))
                                | Rvalue::Box {
                                    value: Operand::Local(input),
                                    from: MirType::Dynamic,
                                },
                        } = statement
                        {
                            let deferred = limitations
                                .check(index.aliases(function, *input, &self.frontier))?
                                .unwrap_or(false);
                            if deferred {
                                index.unique(function, *dest)?;
                            }
                            deferred
                        } else {
                            false
                        };
                        if deferred_alias {
                            limitations.note("FunctionDefaults合同：未证明输出链的后续纯别名");
                        } else {
                            return Err(error("不透明结果存在其它消费者或覆盖"));
                        }
                    }
                }
                let mut reads = FxHashSet::default();
                match &block.terminator {
                    Terminator::Branch { cond, .. } => read_operand(cond, &mut reads),
                    Terminator::Return(Some(value)) => read_operand(value, &mut reads),
                    Terminator::Raise { value, cause } => {
                        for value in value.iter().chain(cause) {
                            read_operand(value, &mut reads);
                        }
                    }
                    Terminator::Suspend { value, input, .. } => {
                        read_operand(value, &mut reads);
                        reads.extend(input);
                    }
                    Terminator::Goto(_) | Terminator::Return(None) | Terminator::Unreachable => {}
                }
                for local in reads {
                    if self.opaque(index, function, local, limitations)? {
                        return Err(error("不透明结果从控制边/return/yield外逃"));
                    }
                }
            }
        }
        Ok(())
    }
}
