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

use dotnetdll::prelude::*;
use ryupy_mir::{
    BinOp, Callee, Constant as MirConstant, Operand, RuntimeFn, Rvalue, Statement, Terminator,
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

pub fn lower_function(func: &ryupy_mir::Function, runtime_refs: &RuntimeRefs) -> CompiledBody {
    let mut locals = Vec::new();
    for (_, local_decl) in func.body.locals.iter() {
        if let Some(var) = crate::types::mir_type_to_local_var(&local_decl.ty) {
            locals.push(var);
        }
    }

    let mut instructions = Vec::new();

    for (_block_id, block) in func.body.blocks.iter() {
        for stmt in &block.stmts {
            match stmt {
                Statement::Assign { dest, value } => {
                    lower_rvalue(value, &mut instructions, runtime_refs);
                    let dest_idx = u32::from(dest.into_raw()) as u16;
                    instructions.push(Instruction::StoreLocal(dest_idx));
                }
                Statement::Eval(rvalue) => {
                    lower_rvalue(rvalue, &mut instructions, runtime_refs);
                }
                Statement::SetField { .. } => {}
            }
        }

        match &block.terminator {
            Terminator::Return(None) => {
                instructions.push(Instruction::Return);
            }
            Terminator::Return(Some(op)) => {
                lower_operand(op, &mut instructions);
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

fn lower_operand(op: &Operand, ins: &mut Vec<Instruction>) {
    match op {
        Operand::Local(loc) => {
            let idx = u32::from(loc.into_raw()) as u16;
            ins.push(Instruction::LoadLocal(idx));
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

fn lower_rvalue(rval: &Rvalue, ins: &mut Vec<Instruction>, runtime_refs: &RuntimeRefs) {
    match rval {
        Rvalue::Use(op) => lower_operand(op, ins),
        Rvalue::Binary { op, lhs, rhs } => {
            lower_operand(lhs, ins);
            lower_operand(rhs, ins);
            match op {
                BinOp::Add => {
                    ins.push(Instruction::AddOverflow(NumberSign::Signed));
                }
                _ => todo!("其他二元运算在后续里程碑实现"),
            }
        }
        Rvalue::Call { callee, args } => {
            for arg in args {
                lower_operand(arg, ins);
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
                Callee::Fn(_) => todo!("函数调用在 M1.1 实现"),
            }
        }
        _ => todo!("其他 Rvalue 在后续里程碑实现"),
    }
}
