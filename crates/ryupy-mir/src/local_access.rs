//! 局部读写与操作数重写的统一入口；穷尽MIR变体，供暂停边界与后续类型化执行区复用。
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub(crate) fn statement_reads(s: &Statement) -> FxHashSet<LocalId> {
    let mut result = FxHashSet::default();
    match s {
        Statement::Assign { value, .. } | Statement::Eval(value) => read_value(value, &mut result),
        Statement::SetField { obj, value, .. } => {
            read_operand(obj, &mut result);
            read_operand(value, &mut result);
        }
        Statement::SetGlobal { value, .. } => read_operand(value, &mut result),
        Statement::Line(_) => {}
    }
    result
}

pub(crate) fn rewrite_operand(op: &mut Operand, map: &FxHashMap<LocalId, LocalId>) {
    match op {
        Operand::Local(id) => {
            if let Some(new) = map.get(id) {
                *id = *new;
            }
        }
        Operand::Array(items) => {
            for item in items {
                rewrite_operand(item, map);
            }
        }
        Operand::Const(_) | Operand::Global(_) => {}
    }
}

pub(crate) fn rewrite_value(v: &mut Rvalue, map: &FxHashMap<LocalId, LocalId>) {
    let rewrite = |op: &mut Operand| rewrite_operand(op, map);
    match v {
        Rvalue::Use(op)
        | Rvalue::GeneratorDelegateStep { delegate: op }
        | Rvalue::GetField { obj: op, .. }
        | Rvalue::Box { value: op, .. }
        | Rvalue::Unbox { value: op, .. }
        | Rvalue::VectorLaneF64 { value: op, .. }
        | Rvalue::IsBoxedF64 { value: op }
        | Rvalue::UnboxExactF64 { value: op }
        | Rvalue::IsBoxedI64 { value: op }
        | Rvalue::UnboxExactI64 { value: op }
        | Rvalue::DynTruthy { value: op }
        | Rvalue::Unary { operand: op, .. }
        | Rvalue::Cast { operand: op, .. }
        | Rvalue::DynUnary { operand: op, .. } => rewrite(op),
        Rvalue::Binary { lhs, rhs, .. }
        | Rvalue::VectorBinaryF64 { lhs, rhs, .. }
        | Rvalue::DynBinary { lhs, rhs, .. }
        | Rvalue::DynCompare { lhs, rhs, .. }
        | Rvalue::VectorPackF64 {
            low: lhs,
            high: rhs,
        } => {
            rewrite(lhs);
            rewrite(rhs);
        }
        Rvalue::Call { args, .. } | Rvalue::NewObject { args, .. } => {
            args.iter_mut().for_each(rewrite)
        }
        Rvalue::CallWithArgs { prefix, args, .. } => {
            prefix.iter_mut().chain(args).for_each(rewrite)
        }
        Rvalue::DynCallValue { callee, args } => {
            rewrite(callee);
            args.iter_mut().for_each(rewrite);
        }
        Rvalue::DynCallMethod {
            captured,
            obj,
            args,
            ..
        } => {
            captured.iter_mut().for_each(rewrite);
            rewrite(obj);
            args.iter_mut().for_each(rewrite);
        }
        Rvalue::MakeGenerator { frame, origin, .. } => {
            frame.iter_mut().chain(origin).for_each(rewrite)
        }
        Rvalue::MakeStateGenerator {
            payload, origin, ..
        } => {
            rewrite(payload);
            origin.iter_mut().for_each(rewrite);
        }
        Rvalue::MakeFunction {
            env,
            defaults,
            instance_context,
            ..
        } => env
            .iter_mut()
            .chain(defaults)
            .chain(instance_context)
            .for_each(rewrite),
        Rvalue::InstanceZeroCall { callee, frame } => {
            rewrite(callee);
            if let Some(id) = &mut frame.first_parameter {
                if let Some(new) = map.get(id) {
                    *id = *new;
                }
            }
            if let crate::SuperClassSource::Lexical { environment, .. } = &mut frame.class {
                if let Some(new) = map.get(environment) {
                    *environment = *new;
                }
            }
        }
        Rvalue::CatchException | Rvalue::IsVector128Supported => {}
    }
}

pub(crate) fn rewrite_statement(s: &mut Statement, map: &FxHashMap<LocalId, LocalId>) {
    match s {
        Statement::Assign { value, .. } | Statement::Eval(value) => rewrite_value(value, map),
        Statement::SetField { obj, value, .. } => {
            rewrite_operand(obj, map);
            rewrite_operand(value, map);
        }
        Statement::SetGlobal { value, .. } => rewrite_operand(value, map),
        Statement::Line(_) => {}
    }
}

pub(crate) fn rewrite_terminator(t: &mut Terminator, map: &FxHashMap<LocalId, LocalId>) {
    match t {
        Terminator::Branch { cond, .. } | Terminator::Suspend { value: cond, .. } => {
            rewrite_operand(cond, map)
        }
        Terminator::Return(value) => {
            if let Some(v) = value {
                rewrite_operand(v, map);
            }
        }
        Terminator::Raise { value, cause } => {
            for v in value.iter_mut().chain(cause) {
                rewrite_operand(v, map);
            }
        }
        Terminator::Goto(_) | Terminator::Unreachable => {}
    }
}

pub(crate) fn terminator_reads(t: &Terminator) -> FxHashSet<LocalId> {
    let mut reads = FxHashSet::default();
    match t {
        Terminator::Branch { cond, .. } | Terminator::Suspend { value: cond, .. } => {
            read_operand(cond, &mut reads)
        }
        Terminator::Return(value) => {
            if let Some(v) = value {
                read_operand(v, &mut reads);
            }
        }
        Terminator::Raise { value, cause } => {
            for v in value.iter().chain(cause) {
                read_operand(v, &mut reads);
            }
        }
        Terminator::Goto(_) | Terminator::Unreachable => {}
    }
    reads
}

pub(crate) fn read_operand(operand: &Operand, reads: &mut FxHashSet<LocalId>) {
    match operand {
        Operand::Local(local) => {
            reads.insert(*local);
        }
        Operand::Array(values) => {
            for value in values {
                read_operand(value, reads);
            }
        }
        Operand::Const(_) | Operand::Global(_) => {}
    }
}

/// exhaustive match保证新增MIR变体必须明确登记读取，不能因漏统计误删活值。
pub(crate) fn read_value(value: &Rvalue, reads: &mut FxHashSet<LocalId>) {
    match value {
        Rvalue::IsVector128Supported => {}
        Rvalue::VectorPackF64 { low, high } => {
            read_operand(low, reads);
            read_operand(high, reads);
        }
        Rvalue::Use(value)
        | Rvalue::GeneratorDelegateStep { delegate: value }
        | Rvalue::GetField { obj: value, .. }
        | Rvalue::Box { value, .. }
        | Rvalue::Unbox { value, .. }
        | Rvalue::VectorLaneF64 { value, .. }
        | Rvalue::IsBoxedF64 { value }
        | Rvalue::UnboxExactF64 { value }
        | Rvalue::IsBoxedI64 { value }
        | Rvalue::UnboxExactI64 { value }
        | Rvalue::DynTruthy { value }
        | Rvalue::Unary { operand: value, .. }
        | Rvalue::Cast { operand: value, .. }
        | Rvalue::DynUnary { operand: value, .. } => read_operand(value, reads),
        Rvalue::Binary { lhs, rhs, .. }
        | Rvalue::VectorBinaryF64 { lhs, rhs, .. }
        | Rvalue::DynBinary { lhs, rhs, .. }
        | Rvalue::DynCompare { lhs, rhs, .. } => {
            read_operand(lhs, reads);
            read_operand(rhs, reads);
        }
        Rvalue::MakeStateGenerator {
            payload, origin, ..
        } => {
            read_operand(payload, reads);
            if let Some(v) = origin {
                read_operand(v, reads);
            }
        }
        Rvalue::MakeGenerator { frame, origin, .. } => {
            for value in frame.iter().chain(origin.iter()) {
                read_operand(value, reads);
            }
        }
        Rvalue::Call { args, .. } | Rvalue::NewObject { args, .. } => {
            for argument in args {
                read_operand(argument, reads);
            }
        }
        Rvalue::CallWithArgs { prefix, args, .. } => {
            for argument in prefix.iter().chain(args) {
                read_operand(argument, reads);
            }
        }
        Rvalue::InstanceZeroCall { callee, frame } => {
            read_operand(callee, reads);
            reads.extend(frame.first_parameter);
            if let crate::SuperClassSource::Lexical { environment, .. } = frame.class {
                reads.insert(environment);
            }
        }
        Rvalue::DynCallValue { callee, args } => {
            read_operand(callee, reads);
            for argument in args {
                read_operand(argument, reads);
            }
        }
        Rvalue::DynCallMethod {
            captured,
            obj,
            args,
            ..
        } => {
            if let Some(value) = captured {
                read_operand(value, reads);
            }
            read_operand(obj, reads);
            for argument in args {
                read_operand(argument, reads);
            }
        }
        Rvalue::MakeFunction {
            env,
            defaults,
            instance_context,
            ..
        } => {
            for argument in env.iter().chain(defaults).chain(instance_context.iter()) {
                read_operand(argument, reads);
            }
        }
        Rvalue::CatchException => {}
    }
}
