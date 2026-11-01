//! 操作数 → CIL 取值指令的发射（`ldloc` / `ldarg` / `ldsfld` / 常量 / 数组构造）。

use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;
use ryupy_mir::{Constant as MirConstant, LocalId, Operand};

use crate::stackify::LowerCtx;
use crate::stackify_refs::RuntimeRefs;

pub(crate) fn lower_operand(
    op: &Operand,
    ins: &mut Vec<Instruction>,
    param_slots: &FxHashMap<LocalId, u16>,
    local_slots: &FxHashMap<LocalId, u16>,
    refs: &RuntimeRefs,
) {
    match op {
        Operand::Local(loc) => {
            if let Some(&arg_idx) = param_slots.get(loc) {
                ins.push(Instruction::LoadArgument(arg_idx));
            } else if let Some(&local_idx) = local_slots.get(loc) {
                ins.push(Instruction::LoadLocal(local_idx));
            }
        }
        Operand::Global(global) => {
            if let Some(&field) = refs.global_fields.get(global) {
                ins.push(Instruction::LoadStaticField {
                    volatile: false,
                    param0: FieldSource::Definition(field),
                });
            }
        }
        Operand::Array(items) => {
            if items.is_empty() {
                ins.push(Instruction::call(refs.empty_array));
                return;
            }
            let object_type = refs.object_type.clone();
            ins.push(Instruction::LoadConstantInt32(items.len() as i32));
            ins.push(Instruction::NewArray(object_type.clone()));
            for (index, item) in items.iter().enumerate() {
                ins.push(Instruction::Duplicate);
                ins.push(Instruction::LoadConstantInt32(index as i32));
                lower_operand(item, ins, param_slots, local_slots, refs);
                ins.push(Instruction::StoreElement {
                    skip_type_check: false,
                    skip_range_check: false,
                    skip_null_check: false,
                    param0: object_type.clone(),
                });
            }
        }
        Operand::Const(c) => match c {
            MirConstant::I64(v) => ins.push(Instruction::LoadConstantInt64(*v)),
            MirConstant::F64(v) => ins.push(Instruction::LoadConstantFloat64(*v)),
            MirConstant::Bool(b) => {
                ins.push(Instruction::LoadConstantInt32(if *b { 1 } else { 0 }))
            }
            MirConstant::Str(s) => ins.push(Instruction::load_string(s)),
            MirConstant::BigInt(s) => {
                ins.push(Instruction::load_string(s));
                ins.push(Instruction::call(refs.dyn_from_decimal));
            }
            MirConstant::None => ins.push(Instruction::call(refs.dyn_none)),
            MirConstant::Null => ins.push(Instruction::LoadNull),
        },
    }
}

/// 构造 `object[]`：`newarr` 后逐个 `stelem.ref`。
///
/// 容器构造、方法调用与内建调用都用它，因此只写一份。
pub(crate) fn lower_object_array(args: &[Operand], ins: &mut Vec<Instruction>, ctx: &LowerCtx<'_>) {
    if args.is_empty() {
        ins.push(Instruction::call(ctx.runtime_refs.empty_array));
        return;
    }
    let object_type = ctx.runtime_refs.object_type.clone();
    ins.push(Instruction::LoadConstantInt32(args.len() as i32));
    ins.push(Instruction::NewArray(object_type.clone()));
    for (index, op) in args.iter().enumerate() {
        ins.push(Instruction::Duplicate);
        ins.push(Instruction::LoadConstantInt32(index as i32));
        lower_operand(op, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
        ins.push(Instruction::StoreElement {
            skip_type_check: false,
            skip_range_check: false,
            skip_null_check: false,
            param0: object_type.clone(),
        });
    }
}
