//! 模块合并时重映射MIR值中的函数、类与全局身份；覆盖字段状态生成器。
use crate::link::{remap_operand, remap_type};
use crate::mir::*;
use rustc_hash::FxHashMap;

pub(super) fn remap_rvalue(
    value: &Rvalue,
    fn_map: &FxHashMap<FnId, FnId>,
    class_map: &FxHashMap<ClassDefId, ClassDefId>,
    global_map: &FxHashMap<GlobalId, GlobalId>,
) -> Rvalue {
    match value {
        Rvalue::MakeStateGenerator {
            resume,
            sum_resume,
            payload,
            initial_pc,
            origin,
        } => Rvalue::MakeStateGenerator {
            resume: fn_map[resume],
            sum_resume: sum_resume.map(|id| fn_map[&id]),
            payload: remap_operand(payload, global_map),
            initial_pc: *initial_pc,
            origin: origin.as_ref().map(|v| remap_operand(v, global_map)),
        },
        Rvalue::Use(op) => Rvalue::Use(remap_operand(op, global_map)),
        Rvalue::Binary { op, lhs, rhs } => Rvalue::Binary {
            op: *op,
            lhs: remap_operand(lhs, global_map),
            rhs: remap_operand(rhs, global_map),
        },
        Rvalue::Unary { op, operand } => Rvalue::Unary {
            op: *op,
            operand: remap_operand(operand, global_map),
        },
        Rvalue::Cast { kind, operand } => Rvalue::Cast {
            kind: *kind,
            operand: remap_operand(operand, global_map),
        },
        Rvalue::Call { callee, args } => Rvalue::Call {
            callee: match callee {
                Callee::Fn(id) => Callee::Fn(fn_map[id]),
                Callee::Runtime(rt) => Callee::Runtime(*rt),
            },
            args: args.iter().map(|a| remap_operand(a, global_map)).collect(),
        },
        Rvalue::NewObject { class, args } => Rvalue::NewObject {
            class: class_map[class],
            args: args.iter().map(|a| remap_operand(a, global_map)).collect(),
        },
        Rvalue::GetField { obj, field } => Rvalue::GetField {
            obj: remap_operand(obj, global_map),
            field: *field,
        },
        Rvalue::Box { value, from } => Rvalue::Box {
            value: remap_operand(value, global_map),
            from: remap_type(from, class_map),
        },
        Rvalue::IsVector128Supported => Rvalue::IsVector128Supported,
        Rvalue::VectorPackF64 { low, high } => Rvalue::VectorPackF64 {
            low: remap_operand(low, global_map),
            high: remap_operand(high, global_map),
        },
        Rvalue::VectorBinaryF64 { op, lhs, rhs } => Rvalue::VectorBinaryF64 {
            op: *op,
            lhs: remap_operand(lhs, global_map),
            rhs: remap_operand(rhs, global_map),
        },
        Rvalue::VectorLaneF64 { value, lane } => Rvalue::VectorLaneF64 {
            value: remap_operand(value, global_map),
            lane: *lane,
        },
        Rvalue::IsBoxedF64 { value } => Rvalue::IsBoxedF64 {
            value: remap_operand(value, global_map),
        },
        Rvalue::UnboxExactF64 { value } => Rvalue::UnboxExactF64 {
            value: remap_operand(value, global_map),
        },
        Rvalue::IsBoxedI64 { value } => Rvalue::IsBoxedI64 {
            value: remap_operand(value, global_map),
        },
        Rvalue::UnboxExactI64 { value } => Rvalue::UnboxExactI64 {
            value: remap_operand(value, global_map),
        },
        Rvalue::Unbox { value, to } => Rvalue::Unbox {
            value: remap_operand(value, global_map),
            to: remap_type(to, class_map),
        },
        Rvalue::DynBinary {
            op,
            lhs,
            rhs,
            inplace,
        } => Rvalue::DynBinary {
            op: *op,
            lhs: remap_operand(lhs, global_map),
            rhs: remap_operand(rhs, global_map),
            inplace: *inplace,
        },
        Rvalue::DynCompare { op, lhs, rhs } => Rvalue::DynCompare {
            op: *op,
            lhs: remap_operand(lhs, global_map),
            rhs: remap_operand(rhs, global_map),
        },
        Rvalue::DynUnary { op, operand } => Rvalue::DynUnary {
            op: *op,
            operand: remap_operand(operand, global_map),
        },
        Rvalue::DynTruthy { value } => Rvalue::DynTruthy {
            value: remap_operand(value, global_map),
        },
        Rvalue::CallWithArgs {
            callee,
            prefix,
            args,
        } => Rvalue::CallWithArgs {
            callee: *callee,
            prefix: prefix
                .iter()
                .map(|a| remap_operand(a, global_map))
                .collect(),
            args: args.iter().map(|a| remap_operand(a, global_map)).collect(),
        },
        Rvalue::InstanceZeroCall { callee, frame } => Rvalue::InstanceZeroCall {
            callee: remap_operand(callee, global_map),
            frame: *frame,
        },
        Rvalue::DynCallValue { callee, args } => Rvalue::DynCallValue {
            callee: remap_operand(callee, global_map),
            args: args.iter().map(|a| remap_operand(a, global_map)).collect(),
        },
        Rvalue::DynCallMethod {
            captured,
            obj,
            name,
            args,
        } => Rvalue::DynCallMethod {
            captured: captured
                .as_ref()
                .map(|value| remap_operand(value, global_map)),
            obj: remap_operand(obj, global_map),
            name: name.clone(),
            args: args.iter().map(|a| remap_operand(a, global_map)).collect(),
        },
        Rvalue::GeneratorDelegateStep { delegate } => Rvalue::GeneratorDelegateStep {
            delegate: remap_operand(delegate, global_map),
        },
        Rvalue::MakeGenerator {
            resume,
            frame,
            origin,
        } => Rvalue::MakeGenerator {
            resume: fn_map[resume],
            origin: origin.as_ref().map(|op| remap_operand(op, global_map)),
            frame: frame
                .iter()
                .map(|op| remap_operand(op, global_map))
                .collect(),
        },
        Rvalue::MakeStockThinFunction {
            func,
            module_context,
        } => Rvalue::MakeStockThinFunction {
            func: fn_map[func],
            module_context: remap_operand(module_context, global_map),
        },
        Rvalue::MakeFunction {
            instance_context,
            func,
            env,
            defaults,
        } => Rvalue::MakeFunction {
            instance_context: instance_context
                .as_ref()
                .map(|op| remap_operand(op, global_map)),
            func: fn_map[func],
            env: env.iter().map(|a| remap_operand(a, global_map)).collect(),
            defaults: defaults
                .iter()
                .map(|a| remap_operand(a, global_map))
                .collect(),
        },
        Rvalue::CatchException => Rvalue::CatchException,
    }
}
