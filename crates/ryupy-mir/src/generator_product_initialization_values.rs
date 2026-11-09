//! 初始化见证的私有纯链/消费者核验；不返回泛Global值，不提供通用数据流或heap能力。
use super::*;

impl Initialization {
    pub(super) fn consumers(
        &self,
        context: &Context<'_>,
        published: &FxHashMap<GlobalId, usize>,
        producer_sites: &FxHashSet<Point>,
    ) -> Result<(), Failure> {
        let mut limitations = Limitations::default();
        let name = self
            .roles
            .iter()
            .find(|r| r.export.name == "__name__")
            .ok_or_else(|| error("缺name角色"))?
            .export
            .value;
        for (function_id, function) in context.program.functions.iter() {
            for (block_id, block) in function.body.blocks.iter() {
                for (position, statement) in block.stmts.iter().enumerate() {
                    let point = (block_id, position);
                    let checked = (|| -> Result<(), Failure> {
                        let mut operand_limits = Limitations::default();
                        let value = match statement {
                            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
                            Statement::SetGlobal { value, .. } => {
                                operand_limits.check(self.read(
                                    context,
                                    published,
                                    function_id,
                                    point,
                                    value,
                                ))?;
                                None
                            }
                            Statement::SetField { obj, value, .. } => {
                                operand_limits.check(self.read(
                                    context,
                                    published,
                                    function_id,
                                    point,
                                    obj,
                                ))?;
                                operand_limits.check(self.read(
                                    context,
                                    published,
                                    function_id,
                                    point,
                                    value,
                                ))?;
                                None
                            }
                            Statement::Line(_) => None,
                        };
                        if function_id == self.entry && !context.transfers.contains(&point) {
                            let mut reads = FxHashSet::default();
                            if let Some(value) = value {
                                crate::passes::dead_values::read_value(value, &mut reads);
                            }
                            if reads.iter().any(|local| context.private.contains(local)) {
                                return Err(error("前言producer局部向已核store之外外逃").into());
                            }
                        }
                        if let Some(value) = value {
                            if matches!(value, Rvalue::Call { callee: Callee::Fn(id), .. }
                            | Rvalue::MakeFunction { func: id, .. } if *id == self.entry)
                            {
                                return Err(error("程序内部调用或包装初始化入口").into());
                            }
                            if let Rvalue::Call {
                                callee: Callee::Runtime(runtime),
                                args,
                            } = value
                            {
                                let runtime_check = (|| -> Result<(), Failure> {
                                    match runtime {
                                        RuntimeFn::MakeClass => {
                                            context.after(
                                                function_id,
                                                self.entry,
                                                point,
                                                published[&name],
                                            )?;
                                        }
                                        RuntimeFn::FunctionModule => {
                                            let [_, module] = args.as_slice() else {
                                                return Err(
                                                    error("FunctionModule冻结实参错误").into()
                                                );
                                            };
                                            if function_id != self.entry
                                                || context.frozen_name(module, point)? != name
                                            {
                                                return Err(error(
                                                    "FunctionModule不是实际已发布name的冻结值",
                                                )
                                                .into());
                                            }
                                        }
                                        RuntimeFn::ModuleFile | RuntimeFn::ModuleMetadata
                                            if function_id != self.entry
                                                || !producer_sites.contains(&point) =>
                                        {
                                            return Err(Failure::unsupported(error(
                                                "未认证的file/metadata初值消费者",
                                            )));
                                        }
                                        RuntimeFn::ModuleExecutionSourceWithCode
                                            if function_id != self.entry
                                                || block_id != context.body.entry
                                                || position
                                                    >= *published.values().min().unwrap() =>
                                        {
                                            return Err(error("额外模块Source初始化").into());
                                        }
                                        _ => {}
                                    }
                                    Ok(())
                                })();
                                operand_limits.check(runtime_check)?;
                            }
                            for operand in operands(value) {
                                operand_limits.check(self.read(
                                    context,
                                    published,
                                    function_id,
                                    point,
                                    operand,
                                ))?;
                            }
                        }
                        operand_limits.finish()
                    })();
                    limitations.check(checked)?;
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
                    limitations.check(self.read(context, published, function_id, point, value))?;
                }
            }
        }
        limitations.finish()
    }

    fn read(
        &self,
        context: &Context<'_>,
        published: &FxHashMap<GlobalId, usize>,
        function: FnId,
        point: Point,
        value: &Operand,
    ) -> Result<(), Failure> {
        // 只遍历实际操作数；不用递归栈承受手造的深数组，也不赋予Array新能力。
        let mut limitations = Limitations::default();
        let mut pending = vec![value];
        while let Some(value) = pending.pop() {
            match value {
                Operand::Array(values) => pending.extend(values),
                Operand::Local(local)
                    if function == self.entry
                        && context.private.contains(local)
                        && !context.transfers.contains(&point) =>
                {
                    return Err(error("前言producer局部向已核store之外外逃").into());
                }
                Operand::Global(slot) => {
                    if let Some(role) = self.roles.iter().find(|r| r.export.value == *slot) {
                        limitations.check(context.after(
                            function,
                            self.entry,
                            point,
                            published[slot],
                        ))?;
                        if !matches!(role.export.name.as_str(), "__name__" | "__package__") {
                            limitations.note(error("opaque file/metadata初值不能作为用户读取能力"));
                        }
                    }
                }
                _ => {}
            }
        }
        limitations.finish()
    }
}

pub(super) struct Context<'a> {
    pub(super) program: &'a Program,
    pub(super) body: &'a Body,
    pub(super) order: &'a FxHashMap<BlockId, usize>,
    pub(super) definitions: &'a FxHashMap<LocalId, Vec<Point>>,
    pub(super) private: FxHashSet<LocalId>,
    pub(super) transfers: FxHashSet<Point>,
    pub(super) pending_chains: Vec<(Operand, Point)>,
}
impl<'a> Context<'a> {
    fn before(&self, left: Point, right: Point) -> bool {
        match (self.order.get(&left.0), self.order.get(&right.0)) {
            (Some(a), Some(b)) => (a, left.1) < (b, right.1),
            _ => false,
        }
    }
    fn after(
        &self,
        function: FnId,
        entry: FnId,
        point: Point,
        publish: usize,
    ) -> Result<(), Failure> {
        if function != entry {
            return Err(Failure::unsupported(error("函数内读取时序尚未认证")));
        }
        if point.0 == self.body.entry && point.1 <= publish {
            return Err(error("真实读取/冻结/MakeClass早于发布或绕过entry").into());
        }
        if !self.before((self.body.entry, publish), point) {
            return Err(Failure::unsupported(error("读取点不在已认证模块直链")));
        }
        Ok(())
    }
    fn definition(&self, local: LocalId, before: Point) -> Result<(Point, &'a Rvalue), Failure> {
        let Some([site]) = self.definitions.get(&local).map(Vec::as_slice) else {
            return Err(error("冻结/producer局部被覆盖或缺定义").into());
        };
        if !self.before(*site, before) {
            return Err(error("冻结/producer定义晚于使用或不可达").into());
        }
        let Statement::Assign { value, .. } = &self.body.blocks[site.0].stmts[site.1] else {
            return Err(error("缺少真实局部赋值").into());
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
                    return Err(error("前言Box不是保全引用/字符串").into());
                }
                Some(MirType::Dynamic)
            }
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ModuleFile),
                ..
            } => Some(MirType::Str),
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ModuleMetadata),
                ..
            } => Some(MirType::Dynamic),
            _ => None,
        };
        if ty.as_ref() != Some(&declared.ty) {
            return Err(error("前言真实局部类型不符").into());
        }
        Ok((*site, value))
    }
    pub(super) fn initial(
        &mut self,
        mut value: &'a Operand,
        mut before: Point,
        sites: &mut FxHashSet<Point>,
    ) -> Result<Initial, Failure> {
        for _ in 0..128 {
            match value {
                Operand::Const(Constant::Str(text)) => return Ok(Initial::Text(text.clone())),
                Operand::Local(local) => {
                    let (site, definition) = self.definition(*local, before)?;
                    if site.0 != self.body.entry {
                        return Err(error("发布producer不在entry同块").into());
                    }
                    sites.insert(site);
                    before = site;
                    match definition {
                        Rvalue::Use(input) | Rvalue::Box { value: input, .. } => value = input,
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ModuleFile),
                            args,
                        } if args == &[Operand::Const(Constant::Str("__main__".into()))] => {
                            return Ok(Initial::File);
                        }
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ModuleMetadata),
                            args,
                        } => {
                            let [
                                Operand::Const(Constant::Str(module)),
                                Operand::Const(Constant::Str(key)),
                            ] = args.as_slice()
                            else {
                                return Err(error("metadata实际module/key错误").into());
                            };
                            if module != "__main__"
                                || !matches!(
                                    key.as_str(),
                                    "__spec__" | "__loader__" | "__cached__" | "__path__"
                                )
                            {
                                return Err(error("metadata实际module/key错误").into());
                            }
                            return Ok(Initial::Metadata(key.clone()));
                        }
                        _ => return Err(error("角色发布producer/helper/key/常量不符").into()),
                    }
                }
                _ => return Err(error("角色发布producer/helper/key/常量不符").into()),
            }
        }
        self.pending_chains.push((value.clone(), before));
        Err(Failure::unsupported(error("初始化局部链超过128上界")))
    }
    // 只定位已截断真实初始化链的未核纯producer；不继续求Initial，也不认证发布值。
    // 每个真实frontier至多128个定义，独立reset/effect仍由inspect的同一次前言走查检查。
    pub(super) fn pending_site(&self, site: Point) -> Result<bool, Failure> {
        let mut limitations = Limitations::default();
        for (root, root_before) in &self.pending_chains {
            let mut value = root;
            let mut before = *root_before;
            let mut exhausted = true;
            for _ in 0..128 {
                let Operand::Local(local) = value else {
                    exhausted = false;
                    break;
                };
                let (at, definition) = self.definition(*local, before)?;
                if at == site {
                    return Ok(true);
                }
                before = at;
                match definition {
                    Rvalue::Use(input) | Rvalue::Box { value: input, .. } => value = input,
                    _ => {
                        exhausted = false;
                        break;
                    }
                }
            }
            if exhausted {
                limitations.note(error("未核初始化链身份定位超过128上界"));
            }
        }
        limitations.finish()?;
        Ok(false)
    }
    fn frozen_name(&self, mut value: &'a Operand, mut before: Point) -> Result<GlobalId, Failure> {
        for _ in 0..128 {
            match value {
                Operand::Global(slot) => return Ok(*slot),
                Operand::Local(local) => {
                    let (site, definition) = self.definition(*local, before)?;
                    before = site;
                    match definition {
                        Rvalue::Use(input) | Rvalue::Box { value: input, .. } => value = input,
                        _ => return Err(error("FunctionModule冻结不是实际name读取").into()),
                    }
                }
                _ => return Err(error("FunctionModule冻结不是实际name读取").into()),
            }
        }
        Err(Failure::unsupported(error(
            "FunctionModule冻结链超过128上界",
        )))
    }
}

// 来源校验与本前言见证复用完整操作数枚举，不遗漏数组/调用载体。
use crate::source_provenance::operands;
