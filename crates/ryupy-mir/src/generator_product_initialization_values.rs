//! 初始化见证的私有纯链/消费者核验；不返回泛Global值，不提供通用数据流或heap能力。
use super::*;

impl Initialization {
    pub(super) fn consumers(
        &self,
        context: &Context<'_>,
        published: &FxHashMap<GlobalId, usize>,
        producer_sites: &FxHashSet<Point>,
    ) -> Result<(), String> {
        let name = self.roles.iter().find(|r| r.export.name == "__name__")
            .ok_or_else(|| error("缺name角色"))?.export.value;
        for (function_id, function) in context.program.functions.iter() {
            for (block_id, block) in function.body.blocks.iter() {
                for (position, statement) in block.stmts.iter().enumerate() {
                    let point = (block_id, position);
                    let value = match statement {
                        Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
                        Statement::SetGlobal { value, .. } => {
                            self.read(context, published, function_id, point, value)?;
                            None
                        }
                        Statement::SetField { obj, value, .. } => {
                            self.read(context, published, function_id, point, obj)?;
                            self.read(context, published, function_id, point, value)?;
                            None
                        }
                        Statement::Line(_) => None,
                    };
                    if let Some(value) = value {
                        if matches!(value, Rvalue::Call { callee: Callee::Fn(id), .. }
                            | Rvalue::MakeFunction { func: id, .. } if *id == self.entry)
                        {
                            return Err(error("程序内部调用或包装初始化入口"));
                        }
                        if let Rvalue::Call { callee: Callee::Runtime(runtime), args } = value {
                            match runtime {
                                RuntimeFn::MakeClass => {
                                    context.after(function_id, self.entry, point, published[&name])?;
                                }
                                RuntimeFn::FunctionModule => {
                                    let [_, module] = args.as_slice() else {
                                        return Err(error("FunctionModule冻结实参错误"));
                                    };
                                    if function_id != self.entry
                                        || context.frozen_name(module, point)? != name
                                    {
                                        return Err(error("FunctionModule不是实际已发布name的冻结值"));
                                    }
                                }
                                RuntimeFn::ModuleFile | RuntimeFn::ModuleMetadata
                                    if function_id != self.entry || !producer_sites.contains(&point) =>
                                {
                                    return Err(error("未认证的file/metadata初值消费者"));
                                }
                                RuntimeFn::ModuleExecutionSource
                                    if function_id != self.entry
                                        || block_id != context.body.entry
                                        || position >= *published.values().min().unwrap() =>
                                {
                                    return Err(error("额外模块Source初始化"));
                                }
                                _ => {}
                            }
                        }
                        for operand in operands(value)? {
                            self.read(context, published, function_id, point, operand)?;
                        }
                    }
                }
                let point = (block_id, block.stmts.len());
                let values: Vec<_> = match &block.terminator {
                    Terminator::Branch { cond, .. } => vec![cond],
                    Terminator::Return(value) => value.iter().collect(),
                    Terminator::Raise { value, cause } => value.iter().chain(cause).collect(),
                    Terminator::Suspend { value, .. } => vec![value],
                    _ => vec![],
                };
                for value in values {
                    self.read(context, published, function_id, point, value)?;
                }
            }
        }
        Ok(())
    }

    fn read(
        &self,
        context: &Context<'_>,
        published: &FxHashMap<GlobalId, usize>,
        function: FnId,
        point: Point,
        value: &Operand,
    ) -> Result<(), String> {
        // 只遍历实际操作数；不用递归栈承受手造的深数组，也不赋予Array新能力。
        let mut pending = vec![value];
        while let Some(value) = pending.pop() {
            match value {
                Operand::Array(values) => pending.extend(values),
                Operand::Local(local)
                    if function == self.entry
                        && context.private.contains(local)
                        && !context.transfers.contains(&point) =>
                {
                    return Err(error("前言producer局部向已核store之外外逃"));
                }
                Operand::Global(slot) => {
                    if let Some(role) = self.roles.iter().find(|r| r.export.value == *slot) {
                        context.after(function, self.entry, point, published[slot])?;
                        if !matches!(role.export.name.as_str(), "__name__" | "__package__") {
                            return Err(error("opaque file/metadata初值不能作为用户读取能力"));
                        }
                    }
                }
                _ => {}
            }
        }
        Ok(())
    }
}

pub(super) struct Context<'a> {
    pub(super) program: &'a Program,
    pub(super) body: &'a Body,
    pub(super) order: &'a FxHashMap<BlockId, usize>,
    pub(super) definitions: &'a FxHashMap<LocalId, Vec<Point>>,
    pub(super) private: FxHashSet<LocalId>,
    pub(super) transfers: FxHashSet<Point>,
}
impl<'a> Context<'a> {
    fn before(&self, left: Point, right: Point) -> bool {
        match (self.order.get(&left.0), self.order.get(&right.0)) {
            (Some(a), Some(b)) => (a, left.1) < (b, right.1),
            _ => false,
        }
    }
    fn after(&self, function: FnId, entry: FnId, point: Point, publish: usize) -> Result<(), String> {
        if function != entry || !self.before((self.body.entry, publish), point) {
            Err(error("真实读取/冻结/MakeClass早于发布或绕过entry"))
        } else {
            Ok(())
        }
    }
    fn definition(&self, local: LocalId, before: Point) -> Result<(Point, &'a Rvalue), String> {
        let Some([site]) = self.definitions.get(&local).map(Vec::as_slice) else {
            return Err(error("冻结/producer局部被覆盖或缺定义"));
        };
        if !self.before(*site, before) {
            return Err(error("冻结/producer定义晚于使用或不可达"));
        }
        let Statement::Assign { value, .. } = &self.body.blocks[site.0].stmts[site.1] else {
            return Err(error("缺少真实局部赋值"));
        };
        let declared = crate::validate::local_decl(&self.body.locals, local)
            .ok_or_else(|| error("实际LocalId不存在"))?;
        let module = function(self.program, self.program.entry)?;
        let ty = match value {
            Rvalue::Use(input) => crate::validate::operand_type(self.program, module, input),
            Rvalue::Box { value, from } => {
                if crate::validate::operand_type(self.program, module, value).as_ref() != Some(from)
                    || !matches!(from, MirType::Str | MirType::Dynamic)
                {
                    return Err(error("前言Box不是保全引用/字符串"));
                }
                Some(MirType::Dynamic)
            }
            Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ModuleFile), .. } => Some(MirType::Str),
            Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ModuleMetadata), .. } => Some(MirType::Dynamic),
            _ => None,
        };
        if ty.as_ref() != Some(&declared.ty) {
            return Err(error("前言真实局部类型不符"));
        }
        Ok((*site, value))
    }
    pub(super) fn initial(
        &self,
        mut value: &'a Operand,
        mut before: Point,
        sites: &mut FxHashSet<Point>,
    ) -> Result<Initial, String> {
        for _ in 0..128 {
            match value {
                Operand::Const(Constant::Str(text)) => return Ok(Initial::Text(text.clone())),
                Operand::Local(local) => {
                    let (site, definition) = self.definition(*local, before)?;
                    if site.0 != self.body.entry {
                        return Err(error("发布producer不在entry同块"));
                    }
                    sites.insert(site);
                    before = site;
                    match definition {
                        Rvalue::Use(input) | Rvalue::Box { value: input, .. } => value = input,
                        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ModuleFile), args }
                            if args == &[Operand::Const(Constant::Str("__main__".into()))] =>
                        {
                            return Ok(Initial::File);
                        }
                        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ModuleMetadata), args } => {
                            let [Operand::Const(Constant::Str(module)), Operand::Const(Constant::Str(key))] = args.as_slice() else {
                                return Err(error("metadata实际module/key错误"));
                            };
                            if module != "__main__"
                                || !matches!(key.as_str(), "__spec__" | "__loader__" | "__cached__" | "__path__")
                            {
                                return Err(error("metadata实际module/key错误"));
                            }
                            return Ok(Initial::Metadata(key.clone()));
                        }
                        _ => return Err(error("角色发布producer/helper/key/常量不符")),
                    }
                }
                _ => return Err(error("角色发布producer/helper/key/常量不符")),
            }
        }
        Err(error("初始化局部链超过128上界"))
    }
    fn frozen_name(&self, mut value: &'a Operand, mut before: Point) -> Result<GlobalId, String> {
        for _ in 0..128 {
            match value {
                Operand::Global(slot) => return Ok(*slot),
                Operand::Local(local) => {
                    let (site, definition) = self.definition(*local, before)?;
                    before = site;
                    match definition {
                        Rvalue::Use(input) | Rvalue::Box { value: input, .. } => value = input,
                        _ => return Err(error("FunctionModule冻结不是实际name读取")),
                    }
                }
                _ => return Err(error("FunctionModule冻结不是实际name读取")),
            }
        }
        Err(error("FunctionModule冻结链超过128上界"))
    }
}

fn operands(value: &Rvalue) -> Result<Vec<&Operand>, String> {
    Ok(match value {
        Rvalue::Use(value) | Rvalue::Box { value, .. } | Rvalue::Unbox { value, .. } => vec![value],
        Rvalue::Binary { lhs, rhs, .. } => vec![lhs, rhs],
        Rvalue::Call { args, .. } => args.iter().collect(),
        Rvalue::DynCallValue { callee, args } => std::iter::once(callee).chain(args).collect(),
        Rvalue::MakeFunction { env, defaults, instance_context, .. } => {
            env.iter().chain(defaults).chain(instance_context).collect()
        }
        Rvalue::CatchException => vec![],
        _ => return Err(error("未认证的高层操作数结构")),
    })
}
