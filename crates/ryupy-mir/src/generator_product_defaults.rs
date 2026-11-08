//! 只认证模块同块真实def的内部Null默认槽冷发布，不授予一般Function标签元数据读取权。
//! 全程序一次建局部/发布/槽角色索引；每条producer→getter→发布链至多128条语句/别名。
use crate::*;
use rustc_hash::{FxHashMap, FxHashSet};

type Site = (FnId, BlockId, usize);
const LIMIT: usize = 128;
fn error(reason: &str) -> String {
    format!("FunctionDefaults合同：{reason}")
}

#[derive(Default)]
pub(super) struct Defaults {
    getters: FxHashMap<(FnId, LocalId), FnId>,
    locals: FxHashMap<(FnId, LocalId), FnId>,
    slots: FxHashSet<GlobalId>,
    allowed: FxHashSet<Site>,
}

struct Index<'a> {
    program: &'a Program,
    assignments: FxHashMap<(FnId, LocalId), Vec<(BlockId, usize)>>,
    writes: FxHashMap<GlobalId, Vec<Site>>,
    roles: FxHashMap<GlobalId, usize>,
    exported: FxHashSet<GlobalId>,
    default_slots: FxHashSet<GlobalId>,
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
    fn unique(&self, function: FnId, local: LocalId) -> Result<(BlockId, usize), String> {
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
    fn producer(&self, site: Site, operand: &Operand) -> Result<(usize, FnId), String> {
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
            } =
                &self.program.functions[function].body.blocks[at].stmts[position]
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
                Rvalue::MakeFunction { .. } => {
                    return Ok((position, make_function(self.program, definition)?));
                }
                _ => return Err(error("receiver没有同块真实MakeFunction来源")),
            }
        }
        Err(error("同块别名链超过128上界"))
    }
}

/// 校验真实构造及Python参数；也用于原有MakeFunction值事实，避免坏FnId索引panic。
pub(super) fn make_function(program: &Program, value: &Rvalue) -> Result<FnId, String> {
    let Rvalue::MakeFunction {
        func,
        instance_context,
        env,
        defaults,
    } = value
    else {
        return Err(error("不是实际MakeFunction构造"));
    };
    if func.into_raw().into_u32() as usize >= program.functions.len() {
        return Err(error("构造FnId不存在"));
    }
    let target = &program.functions[*func];
    let hidden = usize::from(target.dynamic_origin.is_some());
    if instance_context.is_some()
        || !env.is_empty()
        || target.env.is_some()
        || target.module_state.is_some()
        || target.module_state_source.is_some()
        || target.params.len() != target.param_meta.len() + hidden
        || defaults.len() != target.param_meta.len()
        || defaults.iter().any(|v| *v != Operand::Const(Constant::Null))
        || target.param_meta.iter().any(|p| {
            p.has_default
                || !matches!(p.kind, ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword)
        })
    {
        return Err(error("构造不是精确Python参数数的全CLR Null无默认槽"));
    }
    for (local, meta) in target.params.iter().zip(&target.param_meta) {
        if crate::validate::local_decl(&target.body.locals, *local)
            .is_none_or(|decl| decl.ty != meta.ty || decl.is_cell)
        {
            return Err(error("构造的实际参数类型不符"));
        }
    }
    if let Some(origin) = target.dynamic_origin
        && (target.params.last() != Some(&origin.owner)
            || crate::validate::local_decl(&target.body.locals, origin.owner)
                .is_none_or(|decl| decl.ty != MirType::Dynamic || decl.is_cell))
    {
        return Err(error("构造的hidden owner不符"));
    }
    Ok(*func)
}

impl Defaults {
    pub fn inspect(program: &Program, exports: &[ModuleExport]) -> Result<Self, String> {
        let index = Index::new(program, exports);
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
                        result.capture(&index, (function, block_id, position), dest, args)?;
                    }
                }
            }
        }
        result.consumers(program)?;
        Ok(result)
    }
    fn capture(
        &mut self,
        index: &Index<'_>,
        site: Site,
        dest: LocalId,
        args: &[Operand],
    ) -> Result<(), String> {
        let (function, block_id, position) = site;
        let [receiver] = args else {
            return Err(error("getter实参不是单个receiver"));
        };
        if function != index.program.entry {
            return Err(error("getter不在模块def冷发布入口"));
        }
        index.unique(function, dest)?;
        let (producer, target) = index.producer(site, receiver)?;
        if position - producer > LIMIT {
            return Err(error("同块发布跨度超过128上界"));
        }
        let block = &index.program.functions[function].body.blocks[block_id];
        for at in producer + 1..position {
            let valid = match &block.stmts[at] {
                Statement::Assign {
                    dest,
                    value: Rvalue::Use(_) | Rvalue::Box { .. },
                } => {
                    index.producer((function, block_id, at + 1), &Operand::Local(*dest))?
                        == (producer, target)
                }
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::FunctionModule),
                    args,
                }) => {
                    args.len() == 2
                        && index.producer((function, block_id, at), &args[0])? == (producer, target)
                }
                _ => false,
            };
            if !valid {
                return Err(error("producer与getter之间有未认证操作"));
            }
        }
        let slot = index.program.functions[target]
            .defaults_global
            .ok_or_else(|| error("缺少私有defaults槽"))?;
        if !index.slots.contains(&slot)
            || index.roles.get(&slot) != Some(&1)
            || index.exported.contains(&slot)
        {
            return Err(error("私有槽角色冲突、公开导出或无效GlobalId"));
        }
        self.getters.insert((function, dest), target);
        self.locals.insert((function, dest), target);
        self.allowed.insert(site);
        let mut aliases = FxHashSet::default();
        aliases.insert(dest);
        for at in position + 1..block.stmts.len().min(producer + LIMIT + 1) {
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
                    self.locals.insert((function, *dest), target);
                    self.allowed.insert((function, block_id, at));
                }
                Statement::SetGlobal {
                    dest,
                    value: Operand::Local(input),
                } if *dest == slot && aliases.contains(input) => {
                    if index.writes.get(&slot).map(Vec::as_slice)
                        != Some(&[(function, block_id, at)][..])
                    {
                        return Err(error("私有槽重复或错误发布"));
                    }
                    self.allowed.insert((function, block_id, at));
                    return Ok(());
                }
                _ => return Err(error("结果未沿同块私有发布链消费")),
            }
        }
        Err(error("结果缺少有界同块私有发布"))
    }
    pub fn getter(&self, function: FnId, dest: Option<LocalId>) -> Result<FnId, String> {
        dest.and_then(|dest| self.getters.get(&(function, dest)).copied())
            .ok_or_else(|| error("getter没有实际同块发布证明"))
    }
    pub fn private_slot(&self, slot: GlobalId) -> bool {
        self.slots.contains(&slot)
    }

    // 复用既有穷尽Rvalue读操作数走查，不为opaque结果建立第二套数据流。
    fn consumers(&self, program: &Program) -> Result<(), String> {
        use crate::passes::dead_values::{read_operand, read_value};
        for (function, definition) in program.functions.iter() {
            for (block_id, block) in definition.body.blocks.iter() {
                for (position, statement) in block.stmts.iter().enumerate() {
                    if matches!(statement, Statement::SetGlobal { dest, .. } if self.slots.contains(dest))
                        && !self.allowed.contains(&(function, block_id, position))
                    {
                        return Err(error("私有槽存在未认证写入"));
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
                    if reads
                        .iter()
                        .copied()
                        .chain(dest)
                        .any(|local| self.locals.contains_key(&(function, local)))
                        && !self.allowed.contains(&(function, block_id, position))
                    {
                        return Err(error("不透明结果存在其它消费者或覆盖"));
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
                if reads
                    .iter()
                    .any(|local| self.locals.contains_key(&(function, *local)))
                {
                    return Err(error("不透明结果从控制边/return/yield外逃"));
                }
            }
        }
        Ok(())
    }
}
