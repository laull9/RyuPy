//! 最后高层 MIR 的有限局部定义值。值事实来自实际操作数/定义，参数界限须由所有实际调用校验。
//! 不读取已lowered帧，也不跨Retry求解；未知helper/循环定义/外逃一律拒绝整产物。
use crate::generator_product_failure::{Failure, Limitations};
use crate::trace_domain::{Atom, Primitive};
use crate::trace_lowering::LoweringOrigins;
use crate::*;
use rustc_hash::{FxHashMap, FxHashSet};

#[derive(Clone, Debug, PartialEq, Eq)]
pub(super) enum Value {
    Primitive(Primitive),
    Text(String),
    Integer(i64),
    None,
    Null,
    Function(FnId),
    FunctionDefaults(FnId),
    Class(GlobalId),
    Instance(GlobalId),
    Generator(FnId),
    ExceptionType(String),
    Builtin(String),
    Error,
    Property(FnId),
    Array(Vec<Set>),
    Context,
    Park,
    Depth,
    Output,
    Metadata,
    ModuleOrigin,
}
pub(super) type Set = Vec<Value>;

// 独立硬义务已核之后才消费能力拒绝。完整Bottom仍是一个实参，不能用flatten删掉位置。
// 未证明的None须由Limitations拦住；成功出口残留缺位是内部合同损坏，不能补值或panic。
pub(super) fn complete_arguments(
    limitations: Limitations,
    values: Vec<Option<Set>>,
) -> Result<Vec<Set>, Failure> {
    limitations.finish()?;
    values
        .into_iter()
        .collect::<Option<Vec<_>>>()
        .ok_or_else(|| "调用实参在能力检查完成后仍缺位".into())
}
#[derive(Clone, Copy)]
pub(super) enum Obligation<'a> {
    Runtime(RuntimeFn, usize),
    Parameter(FnId, usize, bool),
    Dynamic(&'a Set, usize),
    Positionals(&'a Set),
}

pub(super) fn union(target: &mut Set, values: Set) -> Result<(), Failure> {
    for value in values {
        if !target.contains(&value) {
            target.push(value);
        }
    }
    if target.len() > 32 {
        Err(Failure::unsupported("高层能力集合超过有限上界"))
    } else {
        Ok(())
    }
}
// 超预算的已知前缀仅供否定协议来源；绝不越过Limitations::finish取得正向能力。
fn retain_known(target: &mut Set, values: Set, limitations: &mut Limitations) {
    for value in values {
        if !target.contains(&value) {
            if target.len() < 32 {
                target.push(value);
            } else {
                limitations.note("高层能力集合超过有限上界");
            }
        }
    }
}
pub(super) fn fits(actual: &Set, expected: &Set) -> bool {
    actual.iter().all(|actual| {
        expected.iter().any(|expected| {
            actual == expected
                || matches!(
                    (actual, expected),
                    (Value::Text(_), Value::Primitive(Primitive::String))
                        | (Value::Integer(_), Value::Primitive(Primitive::Integer))
                        | (Value::None, Value::Primitive(Primitive::None))
                )
        })
    })
}
pub(super) fn primitive(values: &Set) -> bool {
    values.iter().all(|v| {
        matches!(
            v,
            Value::Primitive(_) | Value::Text(_) | Value::Integer(_) | Value::None
        )
    })
}

// 字段尚未证明与已知Bottom分开；NotProven不能用于fits，也不阻止检查其它独立义务。
pub(super) enum Bound {
    Known(Set),
    NotProven,
}
impl Bound {
    pub fn value(&self) -> Result<&Set, Failure> {
        match self {
            Self::Known(values) => Ok(values),
            Self::NotProven => Err(Failure::unsupported("HIR字段上界尚未证明")),
        }
    }
}
pub(super) struct Bounds {
    pub parameters: FxHashMap<(FnId, LocalId), Bound>,
    pub returns: FxHashMap<FnId, Bound>,
    pub yields: FxHashMap<FnId, Bound>,
    pub classes: FxHashSet<GlobalId>,
}
impl Bounds {
    #[cfg(test)]
    pub fn from_facts(
        facts: &trace_domain::Facts,
        origins: &LoweringOrigins,
    ) -> Result<Self, Failure> {
        let mut limitations = Limitations::default();
        let bounds = Self::inspect(facts, origins, &mut limitations)?;
        limitations.finish()?;
        Ok(bounds)
    }
    pub fn inspect(
        facts: &trace_domain::Facts,
        origins: &LoweringOrigins,
        limitations: &mut Limitations,
    ) -> Result<Self, Failure> {
        let convert = |values: &trace_domain::Values| -> Result<Bound, Failure> {
            let mut complete = !values.is_unknown();
            let mut result = Vec::new();
            for atom in values.atoms() {
                let value = match atom {
                    Atom::Primitive(Primitive::None) => Some(Value::None),
                    Atom::Primitive(kind) => Some(Value::Primitive(*kind)),
                    Atom::Function(id) => Some(Value::Function(
                        *origins.functions.get(id).ok_or("事实缺少实际FnId映射")?,
                    )),
                    Atom::Class(id) => Some(Value::Class(
                        *origins.classes.get(id).ok_or("事实缺少实际ClassId映射")?,
                    )),
                    Atom::Instance(id, _) => Some(Value::Instance(
                        *origins.classes.get(id).ok_or("事实缺少实际ClassId映射")?,
                    )),
                    Atom::Generator(id, _) => Some(Value::Generator(
                        *origins.functions.get(id).ok_or("事实缺少实际FnId映射")?,
                    )),
                    Atom::ExceptionClass(name)
                        if matches!(*name, "ValueError" | "TypeError" | "RuntimeError") =>
                    {
                        Some(Value::ExceptionType((*name).into()))
                    }
                    Atom::ExceptionInstance(name)
                        if matches!(*name, "ValueError" | "TypeError" | "RuntimeError") =>
                    {
                        Some(Value::Error)
                    }
                    _ => None,
                };
                if let Some(value) = value {
                    result.push(value);
                } else {
                    complete = false;
                }
            }
            Ok(if complete {
                Bound::Known(result)
            } else {
                Bound::NotProven
            })
        };
        let mut result = Self {
            parameters: FxHashMap::default(),
            returns: FxHashMap::default(),
            yields: FxHashMap::default(),
            classes: origins.classes.values().copied().collect(),
        };
        for (hir, summary) in &facts.functions {
            let function = *origins.functions.get(hir).ok_or("摘要缺实际函数映射")?;
            for (parameter, value) in &summary.parameters {
                let local = *origins
                    .bodies
                    .get(hir)
                    .and_then(|body| body.locals.get(parameter))
                    .ok_or("参数缺实际LocalId映射")?;
                result.parameters.insert((function, local), convert(value)?);
            }
            result.returns.insert(function, convert(&summary.returns)?);
            result.yields.insert(function, convert(&summary.yields)?);
        }
        if result
            .parameters
            .values()
            .chain(result.returns.values())
            .chain(result.yields.values())
            .any(|bound| matches!(bound, Bound::NotProven))
        {
            limitations.note("HIR字段含未迁入能力，缺完整上界");
        }
        Ok(result)
    }
}

pub(super) struct Resolver<'a> {
    pub program: &'a Program,
    pub bounds: &'a Bounds,
    pub generators: &'a [FnId],
    pub defaults: &'a crate::generator_product_defaults::Defaults,
    pub properties: FxHashMap<(GlobalId, String), FnId>,
}

impl Resolver<'_> {
    pub fn operand(&self, function: FnId, operand: &Operand) -> Result<Set, Failure> {
        self.resolve(function, operand, &mut Vec::new())
    }
    fn resolve(
        &self,
        function: FnId,
        operand: &Operand,
        path: &mut Vec<(FnId, LocalId)>,
    ) -> Result<Set, Failure> {
        let mut limitations = Limitations::default();
        let values = self.resolve_known(function, operand, path, &mut limitations, None)?;
        limitations.finish()?;
        Ok(values)
    }
    // 原Resolver的同一遍定义走查保留已知反例；部分集合只可否定硬义务，不能授予能力。
    // 所有普通operand/value入口先finish；调用检查也在finish之后才读取返回/调用能力。
    fn resolve_known(
        &self,
        function: FnId,
        operand: &Operand,
        path: &mut Vec<(FnId, LocalId)>,
        limitations: &mut Limitations,
        obligation: Option<Obligation<'_>>,
    ) -> Result<Set, Failure> {
        if path.len() > 128 {
            return Err(Failure::unsupported("高层局部定义链超过有界预算"));
        }
        Ok(match operand {
            Operand::Const(value) => vec![match value {
                Constant::None => Value::None,
                Constant::Null => Value::Null,
                Constant::Str(value) => Value::Text(value.clone()),
                Constant::I64(value) => Value::Integer(*value),
                Constant::Bool(_) => Value::Primitive(Primitive::Bool),
                Constant::F64(_) => Value::Primitive(Primitive::Float),
                Constant::BigInt(_) => Value::Primitive(Primitive::Integer),
            }],
            Operand::Array(values) => {
                let mut result = Vec::with_capacity(values.len());
                for (index, value) in values.iter().enumerate() {
                    let nested = match obligation {
                        Some(Obligation::Positionals(callee)) => {
                            Some(Obligation::Dynamic(callee, index))
                        }
                        _ => None,
                    };
                    let checked = self.resolve_known(function, value, path, limitations, nested);
                    result.push(limitations.check(checked)?.unwrap_or_default());
                }
                let values = vec![Value::Array(result)];
                if let Some(obligation) = obligation {
                    self.check_known(obligation, &values)?;
                }
                values
            }
            Operand::Local(local) => {
                if path.contains(&(function, *local)) {
                    return Err(Failure::unsupported("高层局部定义存在未闭合环"));
                }
                path.push((function, *local));
                let checked = (|| -> Result<Set, Failure> {
                    let definition = &self.program.functions[function];
                    let mut values = match self.bounds.parameters.get(&(function, *local)) {
                        Some(Bound::Known(values)) => values.clone(),
                        Some(Bound::NotProven) => {
                            limitations.note("参数字段上界尚未证明");
                            vec![]
                        }
                        None => vec![],
                    };
                    if definition
                        .dynamic_origin
                        .is_some_and(|origin| origin.owner == *local)
                    {
                        values.push(Value::Function(function));
                    }
                    let mut assigned = false;
                    for (_, block) in definition.body.blocks.iter() {
                        for statement in &block.stmts {
                            if let Statement::Assign { dest, value } = statement
                                && dest == local
                            {
                                assigned = true;
                                let result = match value {
                                    Rvalue::Use(input) | Rvalue::Box { value: input, .. } => self
                                        .resolve_known(
                                            function,
                                            input,
                                            path,
                                            limitations,
                                            obligation,
                                        ),
                                    _ => self.value(function, Some(*dest), value, path),
                                };
                                if let Some(known) = limitations.check(result)? {
                                    if let Some(obligation) = obligation {
                                        self.check_known(obligation, &known)?;
                                    }
                                    retain_known(&mut values, known, limitations);
                                }
                            }
                        }
                    }
                    if !assigned && values.is_empty() {
                        // Bottom参数没有任何实际调用输入，不能用CLR Null冒充已证明上界。
                        if definition.params.contains(local) {
                            return Err(Failure::unsupported("函数参数没有实际输入覆盖"));
                        }
                        values.push(Value::Null);
                    }
                    Ok(values)
                })();
                path.pop();
                checked?
            }
            Operand::Global(global) => {
                if self.defaults.private_slot(*global) {
                    return Err("FunctionDefaults合同：私有槽不能作为高层值读取".into());
                }
                let mut values = Vec::new();
                let mut assigned = false;
                for (owner, definition) in self.program.functions.iter() {
                    for (_, block) in definition.body.blocks.iter() {
                        for statement in &block.stmts {
                            if let Statement::SetGlobal { dest, value } = statement
                                && dest == global
                            {
                                if owner != self.program.entry {
                                    return Err("非模块入口写入全局槽".into());
                                }
                                assigned = true;
                                let checked =
                                    self.resolve_known(owner, value, path, limitations, obligation);
                                if let Some(known) = limitations.check(checked)? {
                                    if let Some(obligation) = obligation {
                                        self.check_known(obligation, &known)?;
                                    }
                                    retain_known(&mut values, known, limitations);
                                }
                            }
                        }
                    }
                }
                if !assigned && values.is_empty() {
                    values.push(Value::Null);
                }
                values
            }
        })
    }
    pub fn inspect_value(
        &self,
        function: FnId,
        dest: Option<LocalId>,
        value: &Rvalue,
    ) -> Result<Set, Failure> {
        self.value(function, dest, value, &mut Vec::new())
    }
    fn value(
        &self,
        function: FnId,
        dest: Option<LocalId>,
        value: &Rvalue,
        path: &mut Vec<(FnId, LocalId)>,
    ) -> Result<Set, Failure> {
        match value {
            Rvalue::Use(value) | Rvalue::Box { value, .. } => self.resolve(function, value, path),
            Rvalue::CatchException => Ok(vec![Value::Error]),
            Rvalue::MakeFunction { .. } => Ok(vec![Value::Function(
                crate::generator_product_defaults::make_function(self.program, value)?,
            )]),
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::FunctionDefaults),
                ..
            } => Ok(vec![Value::FunctionDefaults(
                self.defaults.getter(function, dest)?,
            )]),
            Rvalue::Call {
                callee: Callee::Fn(target),
                args,
            } => {
                let mut limitations = Limitations::default();
                let mut values = Vec::with_capacity(args.len());
                for (index, argument) in args.iter().enumerate() {
                    let checked = self.resolve_known(
                        function,
                        argument,
                        path,
                        &mut limitations,
                        Some(Obligation::Parameter(*target, index, true)),
                    );
                    values.push(limitations.check(checked)?);
                }
                limitations.check(self.invoke_arguments(*target, &values, true))?;
                let values = complete_arguments(limitations, values)?;
                self.invoke(function, *target, &values, true)
            }
            Rvalue::Call {
                callee: Callee::Runtime(runtime),
                args,
            } => {
                let mut limitations = Limitations::default();
                let mut values: Vec<Option<Set>> = Vec::with_capacity(args.len());
                for (index, argument) in args.iter().enumerate() {
                    let obligation = if *runtime == RuntimeFn::CallValue && index == 1 {
                        values
                            .first()
                            .and_then(Option::as_ref)
                            .map(Obligation::Positionals)
                    } else {
                        Some(Obligation::Runtime(*runtime, index))
                    };
                    let checked =
                        self.resolve_known(function, argument, path, &mut limitations, obligation);
                    values.push(limitations.check(checked)?);
                }
                self.runtime_obligations(*runtime, &values)?;
                let values = complete_arguments(limitations, values)?;
                self.runtime(function, dest, *runtime, &values)
            }
            Rvalue::DynCallValue { callee, args } => {
                let mut limitations = Limitations::default();
                let checked = self.resolve_known(function, callee, path, &mut limitations, None);
                let callee = limitations.check(checked)?.unwrap_or_default();
                let mut values = Vec::with_capacity(args.len());
                for (index, argument) in args.iter().enumerate() {
                    let checked = self.resolve_known(
                        function,
                        argument,
                        path,
                        &mut limitations,
                        Some(Obligation::Dynamic(&callee, index)),
                    );
                    values.push(limitations.check(checked)?);
                }
                self.call_arguments(&callee, &values)?;
                let values = complete_arguments(limitations, values)?;
                self.call(function, &callee, &values)
            }
            Rvalue::Binary {
                op: BinOp::Eq | BinOp::Ne,
                lhs,
                rhs,
            } => {
                let lhs = self.resolve(function, lhs, path)?;
                let rhs = self.resolve(function, rhs, path)?;
                if primitive(&lhs) && primitive(&rhs) {
                    Ok(vec![Value::Primitive(Primitive::Bool)])
                } else {
                    Err(Failure::unsupported("控制比较不是封闭标量"))
                }
            }
            Rvalue::Binary {
                op: BinOp::Add,
                lhs,
                rhs: Operand::Const(Constant::I64(1)),
            } if self.resolve(function, lhs, path)? == [Value::Depth] => Ok(vec![Value::Depth]),
            Rvalue::Unbox { value, to } => {
                let values = self.resolve(function, value, path)?;
                let expected = match to {
                    MirType::I64 => Primitive::Integer,
                    MirType::Str => Primitive::String,
                    MirType::Bool => Primitive::Bool,
                    MirType::F64 => Primitive::Float,
                    _ => return Err(Failure::unsupported("未闭合的高层拆箱类型")),
                };
                if fits(&values, &vec![Value::Primitive(expected)]) {
                    Ok(values)
                } else {
                    Err(Failure::unsupported("拆箱输入不是已证精确标量"))
                }
            }
            _ => Err(Failure::unsupported(
                "最后高层MIR包含未认证操作/委托/动态协议",
            )),
        }
    }
}
