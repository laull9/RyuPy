//! 受控产品实际 helper 实参/effect 合同。未知helper没有按名字默认放行；
//! property与next沿实际值身份闭合，ArrayGet/Set仅在末端模板校验后进入低层，不能用于高层用户heap。
use crate::generator_product_failure::{Failure, Limitations};
use crate::generator_product_values::{
    Bound, Obligation, Resolver, Set, Value, fits, primitive, union,
};
use crate::trace_domain::Primitive;
use crate::*;

fn text(value: &Set) -> Option<&str> {
    match value.as_slice() {
        [Value::Text(text)] => Some(text),
        _ => None,
    }
}
fn array(value: &Set) -> Option<&[Set]> {
    match value.as_slice() {
        [Value::Array(values)] => Some(values),
        _ => None,
    }
}
fn only(values: &Set, allowed: impl Fn(&Value) -> bool) -> bool {
    !values.is_empty() && values.iter().all(allowed)
}

impl Resolver<'_> {
    // CLR参数个数/类型由validate_high全量验证。这里只核已解析的同类型来源，
    // 某个实参未知时仍核其它实参；None表示尚未证明，不是伪造的Value。
    pub fn runtime_obligations(
        &self,
        runtime: RuntimeFn,
        args: &[Option<Set>],
    ) -> Result<(), Failure> {
        for (index, values) in args.iter().enumerate() {
            let Some(values) = values else { continue };
            // 空集合没有负反证；完整Bottom和未知前缀的区别由Bound/Limitations保留。
            if values.is_empty() {
                continue;
            }
            // 新冷载体不是一般Dynamic值，只准指定来源参数，不能借Bottom/未知helper外逃。
            if values
                .iter()
                .any(|v| matches!(v, Value::CodeSource(_) | Value::Signature(_)))
            {
                let valid = match (runtime, index) {
                    (RuntimeFn::ModuleExecutionSourceWithCode, 0) => {
                        only(values, |v| matches!(v, Value::CodeSource(_)))
                    }
                    (RuntimeFn::TracePushWithOrigin, 2) => {
                        only(values, |v| matches!(v, Value::Signature(_)))
                    }
                    _ => false,
                };
                if !valid {
                    return Err("私有来源值进入了未指定的helper参数".into());
                }
            }
            let valid = match (runtime, index) {
                (RuntimeFn::TraceV1Explicit, 0) => only(values, |v| {
                    matches!(v, Value::Error | Value::ExceptionType(_))
                }),
                (
                    RuntimeFn::TraceV1Park | RuntimeFn::TraceV1Handle | RuntimeFn::TraceV1Forward,
                    0,
                ) => only(values, |v| *v == Value::Error),
                (
                    RuntimeFn::TraceV1Park | RuntimeFn::TraceV1Handle | RuntimeFn::TraceV1Forward,
                    1,
                )
                | (RuntimeFn::TracePopTo, 0) => values == &[Value::Depth],
                (
                    RuntimeFn::TraceV1Match | RuntimeFn::TraceV1Abort | RuntimeFn::TraceV1AllMiss,
                    0,
                ) => only(values, |v| matches!(v, Value::Park | Value::None)),
                _ => true,
            };
            if !valid {
                return Err(format!("实际helper {runtime:?} 第{index}实参协议来源错误").into());
            }
        }
        Ok(())
    }
    pub fn runtime(
        &self,
        function: FnId,
        dest: Option<LocalId>,
        runtime: RuntimeFn,
        args: &[Set],
    ) -> Result<Set, Failure> {
        let none = || Ok(vec![Value::None]);
        match (runtime, args) {
            (RuntimeFn::DynNone, []) => none(),
            (RuntimeFn::MakeUnbound, []) => Ok(vec![Value::Null]),
            (RuntimeFn::LoadName | RuntimeFn::LoadChecked, [value, name])
                if text(name).is_some() =>
            {
                // null/unbound 的失败在具体producer建源；只有正常返回分支才取得原值能力。
                Ok(value
                    .iter()
                    .filter(|v| !matches!(v, Value::Null))
                    .cloned()
                    .collect())
            }
            (RuntimeFn::TypeObject, [name])
                if matches!(
                    text(name),
                    Some("ValueError" | "TypeError" | "RuntimeError")
                ) =>
            {
                Ok(vec![Value::ExceptionType(text(name).unwrap().into())])
            }
            (RuntimeFn::BuiltinFunction, [name])
                if matches!(text(name), Some("next" | "print")) =>
            {
                Ok(vec![Value::Builtin(text(name).unwrap().into())])
            }
            (RuntimeFn::MakeException, [name, value])
                if matches!(
                    text(name),
                    Some("ValueError" | "TypeError" | "RuntimeError")
                ) && primitive(value) =>
            {
                Ok(vec![Value::Error])
            }
            (RuntimeFn::TraceV1Explicit, [value])
                if only(value, |v| {
                    matches!(v, Value::Error | Value::ExceptionType(_))
                }) =>
            {
                Ok(vec![Value::Error])
            }
            (RuntimeFn::TraceV1Park, [error, depth])
                if only(error, |v| *v == Value::Error) && depth == &[Value::Depth] =>
            {
                Ok(vec![Value::Park])
            }
            (
                RuntimeFn::TraceV1Match | RuntimeFn::TraceV1Abort | RuntimeFn::TraceV1AllMiss,
                [token],
            ) if only(token, |v| matches!(v, Value::Park | Value::None)) => none(),
            (RuntimeFn::TraceV1Handle | RuntimeFn::TraceV1Forward, [error, depth])
                if only(error, |v| *v == Value::Error) && depth == &[Value::Depth] =>
            {
                none()
            }
            (RuntimeFn::HandlerEnter, [value])
                if only(value, |v| matches!(v, Value::Error | Value::None)) =>
            {
                Ok(vec![Value::Context])
            }
            (RuntimeFn::HandlerLeave, [value])
                if only(value, |v| matches!(v, Value::Context | Value::Null)) =>
            {
                none()
            }
            (RuntimeFn::ExceptionMatch, [error, target])
                if only(error, |v| *v == Value::Error)
                    && target.iter().all(|v| matches!(v, Value::ExceptionType(_))) =>
            {
                Ok(vec![Value::Primitive(Primitive::Bool)])
            }
            (RuntimeFn::TracePushWithOrigin, [file, name, origin])
                if text(file).is_some()
                    && text(name).is_some()
                    && only(origin, |v| {
                        matches!(
                            v,
                            Value::Function(_) | Value::ModuleOrigin | Value::Signature(_)
                        )
                    }) =>
            {
                Ok(vec![Value::Depth])
            }
            (RuntimeFn::TracePush, [file, name])
                if text(file).is_some() && text(name).is_some() =>
            {
                Ok(vec![Value::Depth])
            }
            (RuntimeFn::TracePopTo, [depth]) if depth == &[Value::Depth] => none(),
            (RuntimeFn::TraceCheckDepth, []) => none(),
            (RuntimeFn::ModuleExecutionSourceWithCode, [source, context])
                if only(source, |v| matches!(v, Value::CodeSource(_)))
                    && context == &[Value::Null] =>
            {
                Ok(vec![Value::ModuleOrigin])
            }
            (RuntimeFn::ModuleExecutionSource, [name]) if text(name) == Some("__main__") => {
                Ok(vec![Value::ModuleOrigin])
            }
            (RuntimeFn::ModuleMetadata, [module, key])
                if text(module) == Some("__main__")
                    && matches!(
                        text(key),
                        Some(
                            "__name__"
                                | "__package__"
                                | "__spec__"
                                | "__loader__"
                                | "__cached__"
                                | "__path__"
                        )
                    ) =>
            {
                Ok(vec![
                    if matches!(text(key), Some("__name__" | "__package__")) {
                        Value::Primitive(Primitive::String)
                    } else {
                        Value::Metadata
                    },
                ])
            }
            (RuntimeFn::ModuleFile, [module]) if text(module) == Some("__main__") => {
                Ok(vec![Value::Primitive(Primitive::String)])
            }
            (RuntimeFn::FunctionModule, [callee, module])
                if only(callee, |v| matches!(v, Value::Function(_)))
                    && module.iter().all(|v| {
                        matches!(
                            v,
                            Value::Text(_)
                                | Value::Primitive(Primitive::String)
                                | Value::Null
                                | Value::None
                        )
                    }) =>
            {
                none()
            }
            (RuntimeFn::MakeClass, [name, bases, factory])
                if function == self.program.entry
                    && text(name).is_some()
                    && array(bases).is_some_and(<[Set]>::is_empty)
                    && factory == &[Value::Null] =>
            {
                let Some(dest) = dest else {
                    return Err("类分配没有实际结果局部".into());
                };
                let ids: Vec<_> = self.program.functions[function]
                    .body
                    .blocks
                    .iter()
                    .flat_map(|(_, block)| &block.stmts)
                    .filter_map(|statement| match statement {
                        Statement::SetGlobal {
                            dest: global,
                            value: Operand::Local(value),
                        } if *value == dest && self.bounds.classes.contains(global) => {
                            Some(*global)
                        }
                        _ => None,
                    })
                    .collect();
                if ids.len() == 1 {
                    Ok(vec![Value::Class(ids[0])])
                } else {
                    Err("类分配没有唯一实际ClassId对应槽".into())
                }
            }
            (RuntimeFn::Decorate, [kind, callee]) if kind == &[Value::Integer(1)] => {
                let mut result = Vec::new();
                for value in callee {
                    if let Value::Function(function) = value {
                        result.push(Value::Property(*function));
                    } else {
                        return Err("property不是实际编译getter".into());
                    }
                }
                Ok(result)
            }
            (RuntimeFn::SetAttr, [receiver, name, property])
                if function == self.program.entry
                    && text(name).is_some_and(|name| !name.starts_with("__"))
                    && only(receiver, |v| matches!(v, Value::Class(_)))
                    && only(property, |v| matches!(v, Value::Property(_))) =>
            {
                none()
            }
            (RuntimeFn::FinalizeOrdinaryClass, [receiver])
                if only(receiver, |v| matches!(v, Value::Class(_))) =>
            {
                none()
            }
            (RuntimeFn::DynGetAttr, [receiver, name]) if text(name).is_some() => {
                let mut result = Vec::new();
                for receiver in receiver {
                    let Value::Instance(class) = receiver else {
                        return Err(Failure::unsupported("属性接收者不是固定无字段实例"));
                    };
                    let getter = self
                        .properties
                        .get(&(*class, text(name).unwrap().into()))
                        .ok_or_else(|| Failure::unsupported("实际类没有该固定property"))?;
                    let target = &self.program.functions[*getter];
                    if target.param_meta.len() != 1
                        || self.generators.contains(getter)
                        || self
                            .bounds
                            .parameters
                            .get(&(*getter, target.params[0]))
                            .ok_or("getter缺实际参数上界")?
                            .value()?
                            != &vec![Value::Instance(*class)]
                    {
                        return Err("property→InvokeBound→thunk的真实self合同不成立".into());
                    }
                    union(
                        &mut result,
                        self.bounds
                            .returns
                            .get(getter)
                            .ok_or("getter缺实际返回摘要")?
                            .value()?
                            .clone(),
                    )?;
                }
                Ok(result)
            }
            (RuntimeFn::DynBuiltin, [name, positional, keywords, values])
                if text(name).is_some()
                    && array(keywords).is_some_and(<[Set]>::is_empty)
                    && array(values).is_some_and(<[Set]>::is_empty) =>
            {
                self.builtin(
                    function,
                    text(name).unwrap(),
                    array(positional).ok_or("位置参数不是实际数组")?,
                )
            }
            (RuntimeFn::CallValue, [callee, positional, keywords, values])
                if array(keywords).is_some_and(<[Set]>::is_empty)
                    && array(values).is_some_and(<[Set]>::is_empty) =>
            {
                self.call(
                    function,
                    callee,
                    array(positional).ok_or("call参数非固定数组")?,
                )
            }
            (RuntimeFn::PrintTarget, []) => Ok(vec![Value::Output]),
            (
                RuntimeFn::WriteI64
                | RuntimeFn::WriteF64
                | RuntimeFn::WriteBool
                | RuntimeFn::WriteStr
                | RuntimeFn::WriteObject,
                [output, value],
            ) if output == &[Value::Output] && primitive(value) => none(),
            (RuntimeFn::WriteSep | RuntimeFn::WriteNewline, [output])
                if output == &[Value::Output] =>
            {
                none()
            }
            (RuntimeFn::CheckGlobalBound, [flag, name])
                if primitive(flag) && text(name).is_some() =>
            {
                none()
            }
            _ if crate::trace_product_abi::signature(runtime).is_some()
                || matches!(
                    runtime,
                    RuntimeFn::TracePush
                        | RuntimeFn::TracePushWithOrigin
                        | RuntimeFn::TracePopTo
                        | RuntimeFn::ModuleExecutionSource
                        | RuntimeFn::ModuleExecutionSourceWithCode
                ) =>
            {
                Err(format!("实际helper {runtime:?} 的协议来源合同损坏").into())
            }
            _ => Err(Failure::unsupported(format!(
                "实际helper {runtime:?} 的接收者/实参/effect未闭合"
            ))),
        }
    }
    pub fn builtin(&self, caller: FnId, name: &str, args: &[Set]) -> Result<Set, Failure> {
        match (name, args) {
            ("next", [receiver]) if caller == self.program.entry => {
                let mut result = Vec::new();
                for value in receiver {
                    let Value::Generator(function) = value else {
                        return Err(Failure::unsupported("next的实际对象不是已绑定PyGenerator"));
                    };
                    union(
                        &mut result,
                        self.bounds
                            .yields
                            .get(function)
                            .ok_or("generator缺实际yield摘要")?
                            .value()?
                            .clone(),
                    )?;
                }
                Ok(result)
            }
            ("print", args) if args.iter().all(primitive) => Ok(vec![Value::None]),
            _ => Err(Failure::unsupported("实际builtin callback不在产品闭包")),
        }
    }
    // 每个实际已知定义（包括第33项之后）先经过原硬义务，再丢弃多余集合存储。
    // 不增加Value/摘要，不用未知填充值，也不将这一否定检查当调用成功。
    pub(super) fn check_known(
        &self,
        obligation: Obligation<'_>,
        values: &Set,
    ) -> Result<(), Failure> {
        // 空集合可以是完整Bottom或未完成前缀；是否完成由Bound/Limitations决定。
        // 两者都没有可以否定token/参数合同的实际值，不在此处授予正向能力。
        if values.is_empty() {
            return Ok(());
        }
        match obligation {
            Obligation::Dynamic(callee, index) => {
                for value in callee {
                    if let Value::Function(target) = value {
                        self.check_known(Obligation::Parameter(*target, index, false), values)?;
                    }
                }
                Ok(())
            }
            Obligation::Positionals(callee) => {
                for value in values {
                    if let Value::Array(args) = value {
                        self.call_arguments(
                            callee,
                            &args.iter().cloned().map(Some).collect::<Vec<_>>(),
                        )?;
                    }
                }
                Ok(())
            }
            Obligation::Runtime(runtime, index) => {
                let mut args = vec![None; index + 1];
                args[index] = Some(values.clone());
                self.runtime_obligations(runtime, &args)
            }
            Obligation::Parameter(target, index, hidden) => {
                let function = &self.program.functions[target];
                let count = if hidden {
                    function.params.len()
                } else {
                    function.param_meta.len()
                };
                if index >= count {
                    return Err("真实函数调用不是固定位置参数形状".into());
                }
                let mut args = vec![None; count];
                args[index] = Some(values.clone());
                match self.invoke_arguments(target, &args, hidden) {
                    Err(Failure::NotInDomain(_)) => Ok(()),
                    result => result,
                }
            }
        }
    }
    pub(super) fn call_arguments(&self, callee: &Set, args: &[Option<Set>]) -> Result<(), Failure> {
        for value in callee {
            if let Value::Function(target) = value {
                match self.invoke_arguments(*target, args, false) {
                    Err(Failure::NotInDomain(_)) => {}
                    result => result?,
                }
            }
        }
        Ok(())
    }
    pub fn invoke(
        &self,
        caller: FnId,
        target: FnId,
        args: &[Set],
        hidden: bool,
    ) -> Result<Set, Failure> {
        self.invoke_arguments(
            target,
            &args.iter().cloned().map(Some).collect::<Vec<_>>(),
            hidden,
        )?;
        if caller != self.program.entry {
            return Err(Failure::unsupported("首产品域不接受函数内额外用户调用"));
        }
        if self.generators.contains(&target) {
            Ok(vec![Value::Generator(target)])
        } else {
            Ok(self
                .bounds
                .returns
                .get(&target)
                .ok_or("实际callee缺返回摘要")?
                .value()?
                .clone())
        }
    }
    pub(super) fn invoke_arguments(
        &self,
        target: FnId,
        args: &[Option<Set>],
        hidden: bool,
    ) -> Result<(), Failure> {
        let mut limitations = Limitations::default();
        let function = &self.program.functions[target];
        let count = if hidden {
            function.params.len()
        } else {
            function.param_meta.len()
        };
        if args.len() != count {
            return Err("真实函数调用不是固定位置参数形状".into());
        }
        for (local, value) in function.params.iter().zip(args) {
            let Some(value) = value else {
                limitations.note("调用实参能力尚未闭合");
                continue;
            };
            let expected = if function
                .dynamic_origin
                .is_some_and(|origin| origin.owner == *local)
            {
                vec![Value::Function(target)]
            } else {
                match self.bounds.parameters.get(&(target, *local)) {
                    Some(Bound::Known(values)) => values.clone(),
                    Some(Bound::NotProven) => {
                        limitations.note("实际参数上界尚未证明");
                        continue;
                    }
                    None if function.env == Some(*local)
                        || function.module_state == Some(*local) =>
                    {
                        limitations.note("hidden env/state能力未迁入");
                        continue;
                    }
                    None => return Err("实际参数缺HIR映射上界".into()),
                }
            };
            if expected.is_empty() || value.is_empty() {
                limitations.note("真实参数没有完整输入上界");
            } else if !fits(value, &expected) {
                return Err("真实参数/env/owner不满足函数定义合同".into());
            }
        }
        limitations.finish()
    }

    pub fn call(&self, caller: FnId, callee: &Set, args: &[Set]) -> Result<Set, Failure> {
        if callee.is_empty() {
            return Err(Failure::unsupported(
                "实际callee为Bottom，不能借不可达猜测准入",
            ));
        }
        let mut result = Vec::new();
        for target in callee {
            let values = match target {
                Value::Function(target) => self.invoke(caller, *target, args, false)?,
                Value::Class(class) if caller == self.program.entry && args.is_empty() => {
                    vec![Value::Instance(*class)]
                }
                Value::Builtin(name) => self.builtin(caller, name, args)?,
                _ => return Err(Failure::unsupported("真实callee不是封闭函数或无参数普通类")),
            };
            union(&mut result, values)?;
        }
        Ok(result)
    }
}
