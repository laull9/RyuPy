//! 私有生成器帧证明的有限值域。普通对象只是不透明值，不获得任何调用能力。
//! 未初始化、Python None、物理 catch 值和内部 Retry 始终区分；不做删 None 的并集修补。
use crate::*;

#[derive(Clone, Debug, PartialEq, Eq, Hash)]
pub(super) enum Value {
    Uninitialized,
    None,
    Integer(i64),
    Bool(bool),
    Parameter(usize),
    Opaque,
    Frame,
    Exception,
    StoredException,
    Retry,
}

pub(super) fn index(id: LocalId) -> usize {
    id.into_raw().into_u32() as usize
}

pub(super) fn operand(value: &Operand, locals: &[Value]) -> Result<Value, String> {
    Ok(match value {
        Operand::Local(id) => locals.get(index(*id)).cloned().ok_or("局部越界")?,
        Operand::Const(Constant::I64(value)) => Value::Integer(*value),
        Operand::Const(Constant::Bool(value)) => Value::Bool(*value),
        Operand::Const(Constant::None) => Value::None,
        Operand::Const(Constant::Null) => Value::Uninitialized,
        Operand::Array(items) => {
            for item in items {
                if operand(item, locals)? == Value::Frame {
                    return Err("私有 frame 逃逸到数组".into());
                }
            }
            Value::Opaque
        }
        Operand::Const(_) | Operand::Global(_) => Value::Opaque,
    })
}

/// 穷举有限输入形式；新增 MIR 形式必须显式审查，不能由通配分支默许。
pub(super) fn inputs(value: &Rvalue) -> Result<Vec<&Operand>, String> {
    Ok(match value {
        Rvalue::Use(value)
        | Rvalue::Box { value, .. }
        | Rvalue::Unbox { value, .. }
        | Rvalue::DynTruthy { value } => vec![value],
        Rvalue::Binary { lhs, rhs, .. }
        | Rvalue::DynBinary { lhs, rhs, .. }
        | Rvalue::DynCompare { lhs, rhs, .. } => vec![lhs, rhs],
        Rvalue::Unary { operand, .. }
        | Rvalue::Cast { operand, .. }
        | Rvalue::DynUnary { operand, .. } => vec![operand],
        Rvalue::Call { args, .. } | Rvalue::NewObject { args, .. } => args.iter().collect(),
        Rvalue::GetField { obj, .. } => vec![obj],
        Rvalue::CallWithArgs { prefix, args, .. } => prefix.iter().chain(args).collect(),
        Rvalue::DynCallValue { callee, args } => std::iter::once(callee).chain(args).collect(),
        Rvalue::DynCallMethod {
            captured,
            obj,
            args,
            ..
        } => captured
            .iter()
            .chain(std::iter::once(obj))
            .chain(args)
            .collect(),
        Rvalue::CatchException => vec![],
        _ => return Err("frame 子证明未登记的 MIR operation".into()),
    })
}

pub(super) fn compare(op: BinOp, lhs: Value, rhs: Value) -> Value {
    if let (Value::Integer(a), Value::Integer(b)) = (&lhs, &rhs) {
        let result = match op {
            BinOp::Eq => a == b,
            BinOp::Ne => a != b,
            BinOp::Ge => a >= b,
            BinOp::Lt => a < b,
            BinOp::Gt => a > b,
            BinOp::Le => a <= b,
            _ => return Value::Opaque,
        };
        Value::Bool(result)
    } else {
        Value::Opaque
    }
}
