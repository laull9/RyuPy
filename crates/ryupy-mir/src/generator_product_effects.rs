//! 受控产品实际 helper 实参/effect 合同。未知helper没有按名字默认放行；
//! property与next沿实际值身份闭合，ArrayGet/Set仅在末端模板校验后进入低层，不能用于高层用户heap。
use crate::generator_product_values::{Resolver, Set, Value, primitive, union};
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
    pub fn runtime(
        &self,
        function: FnId,
        dest: Option<LocalId>,
        runtime: RuntimeFn,
        args: &[Set],
    ) -> Result<Set, String> {
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
                        matches!(v, Value::Function(_) | Value::ModuleOrigin)
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
                        return Err("属性接收者不是固定无字段实例".into());
                    };
                    let getter = self
                        .properties
                        .get(&(*class, text(name).unwrap().into()))
                        .ok_or("实际类没有该固定property")?;
                    let target = &self.program.functions[*getter];
                    if target.param_meta.len() != 1
                        || self.generators.contains(getter)
                        || self.bounds.parameters.get(&(*getter, target.params[0]))
                            != Some(&vec![Value::Instance(*class)])
                    {
                        return Err("property→InvokeBound→thunk的真实self合同不成立".into());
                    }
                    union(&mut result, self.bounds.returns[getter].clone())?;
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
            _ => Err(format!("实际helper {runtime:?} 的接收者/实参/effect未闭合")),
        }
    }
    pub fn builtin(&self, caller: FnId, name: &str, args: &[Set]) -> Result<Set, String> {
        match (name, args) {
            ("next", [receiver]) if caller == self.program.entry => {
                let mut result = Vec::new();
                for value in receiver {
                    let Value::Generator(function) = value else {
                        return Err("next的实际对象不是已绑定PyGenerator".into());
                    };
                    union(&mut result, self.bounds.yields[function].clone())?;
                }
                Ok(result)
            }
            ("print", args) if args.iter().all(primitive) => Ok(vec![Value::None]),
            _ => Err("实际builtin callback不在产品闭包".into()),
        }
    }
}
