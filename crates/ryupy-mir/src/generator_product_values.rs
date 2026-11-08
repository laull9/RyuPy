//! 最后高层 MIR 的有限局部定义值。值事实来自实际操作数/定义，参数界限须由所有实际调用校验。
//! 不读取已lowered帧，也不跨Retry求解；未知helper/循环定义/外逃一律拒绝整产物。
use crate::*;
use crate::trace_domain::{Atom, Primitive};
use crate::trace_lowering::LoweringOrigins;
use rustc_hash::{FxHashMap, FxHashSet};

#[derive(Clone, Debug, PartialEq, Eq)]
pub(super) enum Value {
    Primitive(Primitive), Text(String), Integer(i64), None, Null,
    Function(FnId), Class(GlobalId), Instance(GlobalId), Generator(FnId),
    ExceptionType(String), Builtin(String), Error, Property(FnId), Array(Vec<Set>),
    Context, Park, Depth, Output, Metadata, ModuleOrigin,
}
pub(super) type Set = Vec<Value>;

pub(super) fn union(target: &mut Set, values: Set) -> Result<(), String> {
    for value in values { if !target.contains(&value) { target.push(value); } }
    if target.len() > 32 { Err("高层能力集合超过有限上界".into()) } else { Ok(()) }
}
pub(super) fn fits(actual: &Set, expected: &Set) -> bool {
    actual.iter().all(|actual| expected.iter().any(|expected| actual == expected || matches!((actual, expected),
        (Value::Text(_), Value::Primitive(Primitive::String))
        | (Value::Integer(_), Value::Primitive(Primitive::Integer))
        | (Value::None, Value::Primitive(Primitive::None)))))
}
pub(super) fn primitive(values: &Set) -> bool {
    values.iter().all(|v| matches!(v, Value::Primitive(_) | Value::Text(_) | Value::Integer(_) | Value::None))
}

pub(super) struct Bounds {
    pub parameters: FxHashMap<(FnId, LocalId), Set>,
    pub returns: FxHashMap<FnId, Set>,
    pub yields: FxHashMap<FnId, Set>,
    pub classes: FxHashSet<GlobalId>,
}
impl Bounds {
    pub fn from_facts(facts: &crate::trace_domain::Facts, origins: &LoweringOrigins) -> Result<Self, String> {
        let convert = |values: &crate::trace_domain::Values| -> Result<Set, String> {
            if values.is_unknown() { return Err("HIR上界含Unknown".into()); }
            values.atoms().iter().map(|atom| Ok(match atom {
                Atom::Primitive(Primitive::None) => Value::None,
                Atom::Primitive(kind) => Value::Primitive(*kind),
                Atom::Function(id) => Value::Function(origins.functions[id]),
                Atom::Class(id) => Value::Class(origins.classes[id]),
                Atom::Instance(id, _) => Value::Instance(origins.classes[id]),
                Atom::Generator(id, _) => Value::Generator(origins.functions[id]),
                Atom::ExceptionClass(name) if matches!(*name, "ValueError" | "TypeError" | "RuntimeError") => Value::ExceptionType((*name).into()),
                Atom::ExceptionInstance(name) if matches!(*name, "ValueError" | "TypeError" | "RuntimeError") => Value::Error,
                _ => return Err("高层定义含未迁入的内建、异常或隐藏能力".into()),
            })).collect()
        };
        let mut result = Self { parameters: FxHashMap::default(), returns: FxHashMap::default(),
            yields: FxHashMap::default(), classes: origins.classes.values().copied().collect() };
        for (hir, summary) in &facts.functions {
            let function = origins.functions[hir];
            for (parameter, value) in &summary.parameters {
                result.parameters.insert((function, origins.bodies[hir].locals[parameter]), convert(value)?);
            }
            result.returns.insert(function, convert(&summary.returns)?);
            result.yields.insert(function, convert(&summary.yields)?);
        }
        Ok(result)
    }
}

pub(super) struct Resolver<'a> {
    pub program: &'a Program,
    pub bounds: &'a Bounds,
    pub generators: &'a [FnId],
    pub properties: FxHashMap<(GlobalId, String), FnId>,
}

impl Resolver<'_> {
    pub fn operand(&self, function: FnId, operand: &Operand) -> Result<Set, String> {
        self.resolve(function, operand, &mut Vec::new())
    }
    fn resolve(&self, function: FnId, operand: &Operand, path: &mut Vec<(FnId, LocalId)>) -> Result<Set, String> {
        if path.len() > 128 { return Err("高层局部定义链超过有界预算".into()); }
        Ok(match operand {
            Operand::Const(value) => vec![match value {
                Constant::None => Value::None, Constant::Null => Value::Null,
                Constant::Str(value) => Value::Text(value.clone()), Constant::I64(value) => Value::Integer(*value),
                Constant::Bool(_) => Value::Primitive(Primitive::Bool), Constant::F64(_) => Value::Primitive(Primitive::Float),
                Constant::BigInt(_) => Value::Primitive(Primitive::Integer),
            }],
            Operand::Array(values) => vec![Value::Array(values.iter().map(|v| self.resolve(function, v, path)).collect::<Result<_, _>>()?)],
            Operand::Local(local) => {
                if path.contains(&(function, *local)) { return Err("高层局部定义存在未闭合环".into()); }
                path.push((function, *local));
                let definition = &self.program.functions[function];
                let mut values = self.bounds.parameters.get(&(function, *local)).cloned().unwrap_or_default();
                if definition.dynamic_origin.is_some_and(|origin| origin.owner == *local) { values.push(Value::Function(function)); }
                let mut assigned = false;
                for (_, block) in definition.body.blocks.iter() {
                    for statement in &block.stmts {
                        if let Statement::Assign { dest, value } = statement && dest == local {
                            assigned = true;
                            union(&mut values, self.value(function, Some(*dest), value, path)?)?;
                        }
                    }
                }
                path.pop();
                if !assigned && values.is_empty() { values.push(Value::Null); }
                values
            }
            Operand::Global(global) => {
                let mut values = Vec::new();
                for (owner, definition) in self.program.functions.iter() {
                    for (_, block) in definition.body.blocks.iter() {
                        for statement in &block.stmts {
                            if let Statement::SetGlobal { dest, value } = statement && dest == global {
                                if owner != self.program.entry { return Err("非模块入口写入全局槽".into()); }
                                union(&mut values, self.resolve(owner, value, path)?)?;
                            }
                        }
                    }
                }
                if values.is_empty() { values.push(Value::Null); }
                values
            }
        })
    }
    pub fn inspect_value(&self, function: FnId, dest: Option<LocalId>, value: &Rvalue) -> Result<Set, String> {
        self.value(function, dest, value, &mut Vec::new())
    }
    fn value(&self, function: FnId, dest: Option<LocalId>, value: &Rvalue,
        path: &mut Vec<(FnId, LocalId)>) -> Result<Set, String> {
        match value {
            Rvalue::Use(value) | Rvalue::Box { value, .. } => self.resolve(function, value, path),
            Rvalue::CatchException => Ok(vec![Value::Error]),
            Rvalue::MakeFunction { instance_context: None, func, env, defaults }
                if env.is_empty() && defaults.iter().all(|v| matches!(v, Operand::Const(Constant::Null)))
                && defaults.len() == self.program.functions[*func].param_meta.len() => Ok(vec![Value::Function(*func)]),
            Rvalue::Call { callee: Callee::Fn(target), args } => {
                let values = args.iter().map(|v| self.resolve(function, v, path)).collect::<Result<Vec<_>, _>>()?;
                self.invoke(function, *target, &values, true)
            }
            Rvalue::Call { callee: Callee::Runtime(runtime), args } => {
                let values = args.iter().map(|v| self.resolve(function, v, path)).collect::<Result<Vec<_>, _>>()?;
                self.runtime(function, dest, *runtime, &values)
            }
            Rvalue::DynCallValue { callee, args } => {
                let callee = self.resolve(function, callee, path)?;
                let args = args.iter().map(|v| self.resolve(function, v, path)).collect::<Result<Vec<_>, _>>()?;
                self.call(function, &callee, &args)
            }
            Rvalue::Binary { op: BinOp::Eq | BinOp::Ne, lhs, rhs } => {
                let lhs = self.resolve(function, lhs, path)?; let rhs = self.resolve(function, rhs, path)?;
                if primitive(&lhs) && primitive(&rhs) { Ok(vec![Value::Primitive(Primitive::Bool)]) }
                else { Err("控制比较不是封闭标量".into()) }
            }
            Rvalue::Binary { op: BinOp::Add, lhs, rhs: Operand::Const(Constant::I64(1)) }
                if self.resolve(function, lhs, path)? == [Value::Depth] => Ok(vec![Value::Depth]),
            Rvalue::Unbox { value, to } => {
                let values = self.resolve(function, value, path)?;
                let expected = match to { MirType::I64 => Primitive::Integer, MirType::Str => Primitive::String,
                    MirType::Bool => Primitive::Bool, MirType::F64 => Primitive::Float,
                    _ => return Err("未闭合的高层拆箱类型".into()) };
                if fits(&values, &vec![Value::Primitive(expected)]) { Ok(values) } else { Err("拆箱输入不是已证精确标量".into()) }
            }
            _ => Err("最后高层MIR包含未认证操作/委托/动态协议".into()),
        }
    }
    pub fn invoke(&self, caller: FnId, target: FnId, args: &[Set], hidden: bool) -> Result<Set, String> {
        if caller != self.program.entry { return Err("首产品域不接受函数内额外用户调用".into()); }
        let function = &self.program.functions[target];
        let count = if hidden { function.params.len() } else { function.param_meta.len() };
        if args.len() != count { return Err("真实函数调用不是固定位置参数形状".into()); }
        for (local, value) in function.params.iter().zip(args) {
            let expected = if function.dynamic_origin.is_some_and(|origin| origin.owner == *local) {
                vec![Value::Function(target)]
            } else { self.bounds.parameters.get(&(target, *local)).cloned().unwrap_or_default() };
            if value.is_empty() || !fits(value, &expected) { return Err("真实参数/env/owner不满足函数定义合同".into()); }
        }
        if self.generators.contains(&target) { Ok(vec![Value::Generator(target)]) }
        else { Ok(self.bounds.returns[&target].clone()) }
    }
    pub fn call(&self, caller: FnId, callee: &Set, args: &[Set]) -> Result<Set, String> {
        if callee.is_empty() { return Err("实际callee为Bottom，不能借不可达猜测准入".into()); }
        let mut result = Vec::new();
        for target in callee {
            let values = match target {
                Value::Function(target) => self.invoke(caller, *target, args, false)?,
                Value::Class(class) if caller == self.program.entry && args.is_empty() => vec![Value::Instance(*class)],
                Value::Builtin(name) => self.builtin(caller, name, args)?,
                _ => return Err("真实callee不是封闭函数或无参数普通类".into()),
            };
            union(&mut result, values)?;
        }
        Ok(result)
    }
}
