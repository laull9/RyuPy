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
    BinOp, BlockId, Callee, CastKind, ClassDefId, Constant as MirConstant, FnId, LocalId, Operand,
    RuntimeFn, Rvalue, Statement, Terminator, UnaryOp,
};

use crate::types::ClassTypeMap;

#[derive(Clone, Debug)]
pub struct ClassCodegenInfo {
    pub ctor: MethodIndex,
    pub fields: Vec<FieldIndex>,
}

pub type ClassInfoMap = FxHashMap<ClassDefId, ClassCodegenInfo>;

struct LowerCtx<'a> {
    runtime_refs: &'a RuntimeRefs,
    fn_map: &'a FxHashMap<FnId, MethodIndex>,
    class_info: &'a ClassInfoMap,
    param_slots: &'a FxHashMap<LocalId, u16>,
    local_slots: &'a FxHashMap<LocalId, u16>,
    locals: &'a la_arena::Arena<ryupy_mir::LocalDecl>,
}

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
    class_types: &ClassTypeMap,
    class_info: &ClassInfoMap,
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
        if let Some(var) = crate::types::mir_type_to_local_var(&local_decl.ty, class_types) {
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

    let ctx = LowerCtx {
        runtime_refs,
        fn_map,
        class_info,
        param_slots: &param_slots,
        local_slots: &local_slots,
        locals: &func.body.locals,
    };

    for (block_id, block) in func.body.blocks.iter() {
        if !reachable.contains(&block_id) {
            continue;
        }
        block_starts.insert(block_id, instructions.len());

        for stmt in &block.stmts {
            match stmt {
                Statement::Assign { dest, value } => {
                    lower_rvalue(value, &mut instructions, &ctx);
                    if let Some(&arg_idx) = param_slots.get(dest) {
                        instructions.push(Instruction::StoreArgument(arg_idx));
                    } else if let Some(&local_idx) = local_slots.get(dest) {
                        instructions.push(Instruction::StoreLocal(local_idx));
                    }
                }
                Statement::Eval(rvalue) => {
                    lower_rvalue(rvalue, &mut instructions, &ctx);
                }
                Statement::SetField { obj, field, value } => {
                    lower_operand(obj, &mut instructions, &param_slots, &local_slots);
                    lower_operand(value, &mut instructions, &param_slots, &local_slots);
                    let class_id = match obj {
                        Operand::Local(loc) => match func.body.locals[*loc].ty {
                            ryupy_mir::MirType::Class(id) => id,
                            _ => panic!("SetField obj 不是 Class 类型"),
                        },
                        _ => panic!("SetField obj 必须是局部变量操作数"),
                    };
                    let field_idx = class_info[&class_id].fields[*field as usize];
                    instructions.push(Instruction::StoreField {
                        unaligned: None,
                        volatile: false,
                        param0: field_idx.into(),
                    });
                }
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

fn lower_rvalue(rval: &Rvalue, ins: &mut Vec<Instruction>, ctx: &LowerCtx<'_>) {
    match rval {
        Rvalue::Use(op) => lower_operand(op, ins, ctx.param_slots, ctx.local_slots),
        Rvalue::Binary { op, lhs, rhs } => {
            lower_operand(lhs, ins, ctx.param_slots, ctx.local_slots);
            lower_operand(rhs, ins, ctx.param_slots, ctx.local_slots);
            let is_float = is_f64_operand(lhs, ctx.locals) || is_f64_operand(rhs, ctx.locals);
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
            lower_operand(operand, ins, ctx.param_slots, ctx.local_slots);
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
            lower_operand(operand, ins, ctx.param_slots, ctx.local_slots);
            match kind {
                CastKind::I64ToF64 => ins.push(Instruction::ConvertFloat64),
                CastKind::F64ToI64 => ins.push(Instruction::call(ctx.runtime_refs.f64_to_i64)),
                CastKind::BoolToI64 => ins.push(Instruction::Convert(ConversionType::Int64)),
            }
        }
        Rvalue::Call { callee, args } => {
            for arg in args {
                lower_operand(arg, ins, ctx.param_slots, ctx.local_slots);
            }
            match callee {
                Callee::Runtime(rt_fn) => match rt_fn {
                    RuntimeFn::WriteI64 => ins.push(Instruction::call(ctx.runtime_refs.write_i64)),
                    RuntimeFn::WriteF64 => ins.push(Instruction::call(ctx.runtime_refs.write_f64)),
                    RuntimeFn::WriteBool => {
                        ins.push(Instruction::call(ctx.runtime_refs.write_bool))
                    }
                    RuntimeFn::WriteStr => ins.push(Instruction::call(ctx.runtime_refs.write_str)),
                    RuntimeFn::WriteNewline => {
                        ins.push(Instruction::call(ctx.runtime_refs.write_newline))
                    }
                    RuntimeFn::WriteSep => ins.push(Instruction::call(ctx.runtime_refs.write_sep)),
                    RuntimeFn::StrConcat => {
                        ins.push(Instruction::call(ctx.runtime_refs.str_concat))
                    }
                    RuntimeFn::FloorDivI64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.floor_div_i64))
                    }
                    RuntimeFn::ModI64 => ins.push(Instruction::call(ctx.runtime_refs.mod_i64)),
                    RuntimeFn::PowI64 => ins.push(Instruction::call(ctx.runtime_refs.pow_i64)),
                    RuntimeFn::FloorDivF64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.floor_div_f64))
                    }
                    RuntimeFn::ModF64 => ins.push(Instruction::call(ctx.runtime_refs.mod_f64)),
                    RuntimeFn::PowF64 => ins.push(Instruction::call(ctx.runtime_refs.pow_f64)),
                    RuntimeFn::F64ToI64 => ins.push(Instruction::call(ctx.runtime_refs.f64_to_i64)),
                    RuntimeFn::AbsI64 => ins.push(Instruction::call(ctx.runtime_refs.abs_i64)),
                    RuntimeFn::AbsF64 => ins.push(Instruction::call(ctx.runtime_refs.abs_f64)),
                },
                Callee::Fn(fn_id) => {
                    let method_idx = ctx.fn_map[fn_id];
                    ins.push(Instruction::call(method_idx));
                }
            }
        }
        Rvalue::NewObject { class, args } => {
            for arg in args {
                lower_operand(arg, ins, ctx.param_slots, ctx.local_slots);
            }
            let ctor = ctx.class_info[class].ctor;
            ins.push(Instruction::NewObject(ctor.into()));
        }
        Rvalue::GetField { obj, field } => {
            lower_operand(obj, ins, ctx.param_slots, ctx.local_slots);
            let class_id = match obj {
                Operand::Local(loc) => match ctx.locals[*loc].ty {
                    ryupy_mir::MirType::Class(id) => id,
                    _ => panic!("GetField obj 不是 Class 类型"),
                },
                _ => panic!("GetField obj 必须是局部变量操作数"),
            };
            let field_idx = ctx.class_info[&class_id].fields[*field as usize];
            ins.push(Instruction::LoadField {
                unaligned: None,
                volatile: false,
                param0: field_idx.into(),
            });
        }
    }
}
