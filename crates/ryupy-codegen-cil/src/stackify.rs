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
    BinOp, BlockId, Callee, CastKind, Constant as MirConstant, FnId, LocalId, Operand, RuntimeFn,
    Rvalue, Statement, Terminator, UnaryOp,
};

pub struct CompiledBody {
    pub instructions: Vec<Instruction>,
    pub locals: Vec<LocalVariable>,
}

pub struct RuntimeRefs {
    pub write_i64: MethodRefIndex,
    pub write_f64: MethodRefIndex,
    pub write_bool: MethodRefIndex,
    pub write_str: MethodRefIndex,
    pub write_newline: MethodRefIndex,
    pub write_sep: MethodRefIndex,
    pub str_concat: MethodRefIndex,
    pub floor_div_i64: MethodRefIndex,
    pub mod_i64: MethodRefIndex,
    pub pow_i64: MethodRefIndex,
    pub floor_div_f64: MethodRefIndex,
    pub mod_f64: MethodRefIndex,
    pub pow_f64: MethodRefIndex,
    pub f64_to_i64: MethodRefIndex,
    pub abs_i64: MethodRefIndex,
    pub abs_f64: MethodRefIndex,
}

enum PendingTarget {
    Branch(usize, BlockId),
    BranchTruthy(usize, BlockId),
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
    let mut block_starts: FxHashMap<BlockId, usize> = FxHashMap::default();
    let mut pending_targets: Vec<PendingTarget> = Vec::new();

    // 计算可达基本块集（避免发射死代码导致 JIT 校验失败）
    let mut reachable = rustc_hash::FxHashSet::default();
    let mut worklist = vec![func.body.entry];
    reachable.insert(func.body.entry);
    while let Some(bb_id) = worklist.pop() {
        let bb = &func.body.blocks[bb_id];
        match &bb.terminator {
            Terminator::Goto(target) => {
                if reachable.insert(*target) {
                    worklist.push(*target);
                }
            }
            Terminator::Branch { then_, else_, .. } => {
                if reachable.insert(*then_) {
                    worklist.push(*then_);
                }
                if reachable.insert(*else_) {
                    worklist.push(*else_);
                }
            }
            Terminator::Return(_) | Terminator::Unreachable => {}
        }
    }

    for (block_id, block) in func.body.blocks.iter() {
        if !reachable.contains(&block_id) {
            continue;
        }
        block_starts.insert(block_id, instructions.len());

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
                        &func.body.locals,
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
                        &func.body.locals,
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
            Terminator::Goto(target) => {
                let pos = instructions.len();
                instructions.push(Instruction::Branch(0)); // 占位，稍后回填
                pending_targets.push(PendingTarget::Branch(pos, *target));
            }
            Terminator::Branch { cond, then_, else_ } => {
                lower_operand(cond, &mut instructions, &param_slots, &local_slots);
                let pos_then = instructions.len();
                instructions.push(Instruction::BranchTruthy(0)); // 占位
                pending_targets.push(PendingTarget::BranchTruthy(pos_then, *then_));

                let pos_else = instructions.len();
                instructions.push(Instruction::Branch(0)); // 占位
                pending_targets.push(PendingTarget::Branch(pos_else, *else_));
            }
            Terminator::Unreachable => {}
        }
    }

    // 回填所有分支指令的跳转目标下标
    for pending in pending_targets {
        match pending {
            PendingTarget::Branch(idx, target_block) => {
                let target_ins = block_starts.get(&target_block).copied().unwrap_or(0);
                instructions[idx] = Instruction::Branch(target_ins);
            }
            PendingTarget::BranchTruthy(idx, target_block) => {
                let target_ins = block_starts.get(&target_block).copied().unwrap_or(0);
                instructions[idx] = Instruction::BranchTruthy(target_ins);
            }
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

fn is_f64_operand(op: &Operand, locals: &la_arena::Arena<ryupy_mir::LocalDecl>) -> bool {
    match op {
        Operand::Const(MirConstant::F64(_)) => true,
        Operand::Local(loc) => locals[*loc].ty == ryupy_mir::MirType::F64,
        _ => false,
    }
}

fn lower_rvalue(
    rval: &Rvalue,
    ins: &mut Vec<Instruction>,
    runtime_refs: &RuntimeRefs,
    fn_map: &FxHashMap<FnId, MethodIndex>,
    param_slots: &FxHashMap<LocalId, u16>,
    local_slots: &FxHashMap<LocalId, u16>,
    locals: &la_arena::Arena<ryupy_mir::LocalDecl>,
) {
    match rval {
        Rvalue::Use(op) => lower_operand(op, ins, param_slots, local_slots),
        Rvalue::Binary { op, lhs, rhs } => {
            lower_operand(lhs, ins, param_slots, local_slots);
            lower_operand(rhs, ins, param_slots, local_slots);
            let is_float = is_f64_operand(lhs, locals) || is_f64_operand(rhs, locals);
            match op {
                BinOp::Add => {
                    if is_float {
                        ins.push(Instruction::Add);
                    } else {
                        ins.push(Instruction::AddOverflow(NumberSign::Signed));
                    }
                }
                BinOp::Sub => {
                    if is_float {
                        ins.push(Instruction::Subtract);
                    } else {
                        ins.push(Instruction::SubtractOverflow(NumberSign::Signed));
                    }
                }
                BinOp::Mul => {
                    if is_float {
                        ins.push(Instruction::Multiply);
                    } else {
                        ins.push(Instruction::MultiplyOverflow(NumberSign::Signed));
                    }
                }
                BinOp::Div => ins.push(Instruction::Divide(NumberSign::Signed)),
                BinOp::BitAnd => ins.push(Instruction::And),
                BinOp::BitOr => ins.push(Instruction::Or),
                BinOp::BitXor => ins.push(Instruction::Xor),
                BinOp::Shl => ins.push(Instruction::ShiftLeft),
                BinOp::Shr => ins.push(Instruction::ShiftRight(NumberSign::Signed)),
                BinOp::Eq => ins.push(Instruction::CompareEqual),
                BinOp::Ne => {
                    ins.push(Instruction::CompareEqual);
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
                BinOp::Lt => ins.push(Instruction::CompareLess(NumberSign::Signed)),
                BinOp::Gt => ins.push(Instruction::CompareGreater(NumberSign::Signed)),
                BinOp::Le => {
                    // CIL 无 cle，以 !(a > b) 实现
                    ins.push(Instruction::CompareGreater(NumberSign::Signed));
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
                BinOp::Ge => {
                    // CIL 无 cge，以 !(a < b) 实现
                    ins.push(Instruction::CompareLess(NumberSign::Signed));
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
            }
        }
        Rvalue::Unary { op, operand } => {
            lower_operand(operand, ins, param_slots, local_slots);
            match op {
                UnaryOp::Neg => ins.push(Instruction::Negate),
                UnaryOp::Not => {
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
                UnaryOp::BitNot => ins.push(Instruction::Not),
            }
        }
        Rvalue::Cast { kind, operand } => {
            lower_operand(operand, ins, param_slots, local_slots);
            match kind {
                CastKind::I64ToF64 => ins.push(Instruction::ConvertFloat64),
                CastKind::F64ToI64 => ins.push(Instruction::call(runtime_refs.f64_to_i64)),
                CastKind::BoolToI64 => ins.push(Instruction::Convert(ConversionType::Int64)),
            }
        }
        Rvalue::Call { callee, args } => {
            for arg in args {
                lower_operand(arg, ins, param_slots, local_slots);
            }
            match callee {
                Callee::Runtime(rt_fn) => match rt_fn {
                    RuntimeFn::WriteI64 => ins.push(Instruction::call(runtime_refs.write_i64)),
                    RuntimeFn::WriteF64 => ins.push(Instruction::call(runtime_refs.write_f64)),
                    RuntimeFn::WriteBool => ins.push(Instruction::call(runtime_refs.write_bool)),
                    RuntimeFn::WriteStr => ins.push(Instruction::call(runtime_refs.write_str)),
                    RuntimeFn::WriteNewline => {
                        ins.push(Instruction::call(runtime_refs.write_newline))
                    }
                    RuntimeFn::WriteSep => ins.push(Instruction::call(runtime_refs.write_sep)),
                    RuntimeFn::StrConcat => ins.push(Instruction::call(runtime_refs.str_concat)),
                    RuntimeFn::FloorDivI64 => {
                        ins.push(Instruction::call(runtime_refs.floor_div_i64))
                    }
                    RuntimeFn::ModI64 => ins.push(Instruction::call(runtime_refs.mod_i64)),
                    RuntimeFn::PowI64 => ins.push(Instruction::call(runtime_refs.pow_i64)),
                    RuntimeFn::FloorDivF64 => {
                        ins.push(Instruction::call(runtime_refs.floor_div_f64))
                    }
                    RuntimeFn::ModF64 => ins.push(Instruction::call(runtime_refs.mod_f64)),
                    RuntimeFn::PowF64 => ins.push(Instruction::call(runtime_refs.pow_f64)),
                    RuntimeFn::F64ToI64 => ins.push(Instruction::call(runtime_refs.f64_to_i64)),
                    RuntimeFn::AbsI64 => ins.push(Instruction::call(runtime_refs.abs_i64)),
                    RuntimeFn::AbsF64 => ins.push(Instruction::call(runtime_refs.abs_f64)),
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
