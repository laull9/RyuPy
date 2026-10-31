//! MIR 函数体 → CIL 指令序列。
//!
//! Phase 1 采用最直接的翻译，正确性优先：
//! - 每个 MIR 局部变量 = 一个 CIL local（参数用 `ldarg`/`starg`）；
//! - `dest = lhs op rhs` → `ld lhs; ld rhs; op; stloc dest`；
//! - 基本块按 arena 顺序线性排列，先记下每块起始指令下标，最后回填分支目标
//!   （dotnetdll 的分支指令目标是**指令下标**，不是字节偏移）；
//! - 比较运算：`Lt` → `clt`，`Le` → `cgt` + `ldc.i4.0` + `ceq`（CIL 没有 `cle`）；
//!   浮点比较遇到 NaN 时要用 `cgt.un` / `clt.un` 变体，务必写测试覆盖；
//! - 整数 `+ - *` 使用 `add.ovf` / `sub.ovf` / `mul.ovf`，溢出抛异常而不是静默回绕（ADR-0004）。
//!
//! 进阶（Phase 3+）：把「定义后立即且仅使用一次」的临时变量留在求值栈上，省掉 stloc/ldloc。
//! RyuJIT 本身会做这类优化，所以只有在 IL 体积成为问题时才值得做。

use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;
use ryupy_mir::{
    BinOp, Callee, Constant as MirConstant, FnId, LocalId, Operand, RuntimeFn, Rvalue, Statement,
    Terminator,
};

pub struct CompiledBody {
    pub instructions: Vec<Instruction>,
    pub locals: Vec<LocalVariable>,
}

pub struct RuntimeRefs {
    pub write_i64: MethodRefIndex,
    pub write_newline: MethodRefIndex,
    pub write_sep: MethodRefIndex,
}

pub fn lower_function(
    func: &ryupy_mir::Function,
    runtime_refs: &RuntimeRefs,
    fn_map: &FxHashMap<FnId, MethodIndex>,
) -> CompiledBody {
    // 区分形参（ldarg）与局部变量（ldloc）
    let mut param_slots: FxHashMap<LocalId, u16> = FxHashMap::default();
    for (idx, &p) in func.params.iter().enumerate() {
        param_slots.insert(p, idx as u16);
    }

    let mut local_slots: FxHashMap<LocalId, u16> = FxHashMap::default();
    let mut locals = Vec::new();
    for (loc_id, local_decl) in func.body.locals.iter() {
        if param_slots.contains_key(&loc_id) {
            continue;
        }
        if let Some(var) = crate::types::mir_type_to_local_var(&local_decl.ty) {
            let idx = locals.len() as u16;
            locals.push(var);
            local_slots.insert(loc_id, idx);
        }
    }

    let mut instructions = Vec::new();

    for (_block_id, block) in func.body.blocks.iter() {
        for stmt in &block.stmts {
            match stmt {
                Statement::Assign { dest, value } => {
                    lower_rvalue(
                        value,
                        &mut instructions,
                        runtime_refs,
                        fn_map,
                        &param_slots,
                        &local_slots,
                    );
                    if let Some(&arg_idx) = param_slots.get(dest) {
                        instructions.push(Instruction::StoreArgument(arg_idx));
                    } else if let Some(&local_idx) = local_slots.get(dest) {
                        instructions.push(Instruction::StoreLocal(local_idx));
                    }
                }
                Statement::Eval(rvalue) => {
                    lower_rvalue(
                        rvalue,
                        &mut instructions,
                        runtime_refs,
                        fn_map,
                        &param_slots,
                        &local_slots,
                    );
                }
                Statement::SetField { .. } => {}
            }
        }

        match &block.terminator {
            Terminator::Return(None) => {
                instructions.push(Instruction::Return);
            }
            Terminator::Return(Some(op)) => {
                lower_operand(op, &mut instructions, &param_slots, &local_slots);
                instructions.push(Instruction::Return);
            }
            _ => {}
        }
    }

    CompiledBody {
        instructions,
        locals,
    }
}

fn lower_operand(
    op: &Operand,
    ins: &mut Vec<Instruction>,
    param_slots: &FxHashMap<LocalId, u16>,
    local_slots: &FxHashMap<LocalId, u16>,
) {
    match op {
        Operand::Local(loc) => {
            if let Some(&arg_idx) = param_slots.get(loc) {
                ins.push(Instruction::LoadArgument(arg_idx));
            } else if let Some(&local_idx) = local_slots.get(loc) {
                ins.push(Instruction::LoadLocal(local_idx));
            }
        }
        Operand::Const(c) => match c {
            MirConstant::I64(v) => ins.push(Instruction::LoadConstantInt64(*v)),
            MirConstant::F64(v) => ins.push(Instruction::LoadConstantFloat64(*v)),
            MirConstant::Bool(b) => {
                ins.push(Instruction::LoadConstantInt32(if *b { 1 } else { 0 }))
            }
            MirConstant::Str(s) => ins.push(Instruction::load_string(s)),
        },
    }
}

fn lower_rvalue(
    rval: &Rvalue,
    ins: &mut Vec<Instruction>,
    runtime_refs: &RuntimeRefs,
    fn_map: &FxHashMap<FnId, MethodIndex>,
    param_slots: &FxHashMap<LocalId, u16>,
    local_slots: &FxHashMap<LocalId, u16>,
) {
    match rval {
        Rvalue::Use(op) => lower_operand(op, ins, param_slots, local_slots),
        Rvalue::Binary { op, lhs, rhs } => {
            lower_operand(lhs, ins, param_slots, local_slots);
            lower_operand(rhs, ins, param_slots, local_slots);
            match op {
                BinOp::Add => {
                    ins.push(Instruction::AddOverflow(NumberSign::Signed));
                }
                BinOp::Sub => {
                    ins.push(Instruction::SubtractOverflow(NumberSign::Signed));
                }
                BinOp::Mul => {
                    ins.push(Instruction::MultiplyOverflow(NumberSign::Signed));
                }
                _ => todo!("其他二元运算在后续里程碑实现"),
            }
        }
        Rvalue::Call { callee, args } => {
            for arg in args {
                lower_operand(arg, ins, param_slots, local_slots);
            }
            match callee {
                Callee::Runtime(rt_fn) => match rt_fn {
                    RuntimeFn::WriteI64 => {
                        ins.push(Instruction::call(runtime_refs.write_i64));
                    }
                    RuntimeFn::WriteNewline => {
                        ins.push(Instruction::call(runtime_refs.write_newline));
                    }
                    RuntimeFn::WriteSep => {
                        ins.push(Instruction::call(runtime_refs.write_sep));
                    }
                    _ => todo!("其他 Runtime 方法在后续里程碑实现"),
                },
                Callee::Fn(fn_id) => {
                    let method_idx = fn_map[fn_id];
                    ins.push(Instruction::call(method_idx));
                }
            }
        }
        _ => todo!("其他 Rvalue 在后续里程碑实现"),
    }
}
