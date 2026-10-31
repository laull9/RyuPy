//! MIR 合法性检查。codegen 之前必跑，Pass 之后在 debug 构建下也跑。
//!
//! 检查项：
//! - [x] 不存在 `Terminator::Unreachable` 占位
//! - [x] 所有 `LocalId` / `BlockId` 都在各自 arena 范围内
//! - [x] `Binary` 两侧操作数类型一致；`Branch` 条件为 `Bool`
//! - [x] `Return` 的值类型与函数返回类型一致
//! - [x] `Call` 实参个数与被调函数签名一致

use crate::mir::{Callee, Constant, MirType, Operand, Program, Rvalue, Statement, Terminator};

pub fn validate(program: &Program) -> Result<(), Vec<String>> {
    let mut errors = Vec::new();

    if !program.functions.iter().any(|(id, _)| id == program.entry) {
        errors.push(format!(
            "程序入口函数 {:?} 不在 functions arena 中",
            program.entry
        ));
    }

    for (_fn_id, func) in program.functions.iter() {
        if !func.body.blocks.iter().any(|(id, _)| id == func.body.entry) {
            errors.push(format!(
                "函数 `{}` 的入口块 {:?} 不在 blocks arena 中",
                func.name, func.body.entry
            ));
        }

        // 收集从 entry 可达的基本块集合
        let mut reachable = rustc_hash::FxHashSet::default();
        let mut worklist = vec![func.body.entry];
        reachable.insert(func.body.entry);
        while let Some(bb_id) = worklist.pop() {
            if let Some((_, bb)) = func.body.blocks.iter().find(|&(id, _)| id == bb_id) {
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
                    _ => {}
                }
            }
        }

        for &param in &func.params {
            if !func.body.locals.iter().any(|(id, _)| id == param) {
                errors.push(format!(
                    "函数 `{}` 的参数 {:?} 不在 locals arena 中",
                    func.name, param
                ));
            }
        }

        for (block_id, block) in func.body.blocks.iter() {
            let is_reachable = reachable.contains(&block_id);
            for stmt in &block.stmts {
                match stmt {
                    Statement::Assign { dest, value } => {
                        if !func.body.locals.iter().any(|(id, _)| id == *dest) {
                            errors.push(format!(
                                "函数 `{}` 基本块 {:?} 中的赋值目标 {:?} 不在 locals arena 中",
                                func.name, block_id, dest
                            ));
                        }
                        validate_rvalue(func, block_id, value, program, &mut errors);
                    }
                    Statement::Eval(rvalue) => {
                        validate_rvalue(func, block_id, rvalue, program, &mut errors);
                    }
                    Statement::SetField { obj, value, .. } => {
                        validate_operand(func, block_id, obj, &mut errors);
                        validate_operand(func, block_id, value, &mut errors);
                    }
                }
            }

            match &block.terminator {
                Terminator::Unreachable => {
                    if is_reachable {
                        errors.push(format!(
                            "函数 `{}` 基本块 {:?} 残留了 Unreachable 终结符",
                            func.name, block_id
                        ));
                    }
                }
                Terminator::Goto(target) => {
                    if !func.body.blocks.iter().any(|(id, _)| id == *target) {
                        errors.push(format!(
                            "函数 `{}` 基本块 {:?} Goto 目标 {:?} 不在 blocks arena 中",
                            func.name, block_id, target
                        ));
                    }
                }
                Terminator::Branch { cond, then_, else_ } => {
                    validate_operand(func, block_id, cond, &mut errors);
                    if let Some(cond_ty) = operand_type(func, cond) {
                        if cond_ty != MirType::Bool {
                            errors.push(format!(
                                "函数 `{}` 基本块 {:?} Branch 条件类型期望为 Bool，得到 {:?}",
                                func.name, block_id, cond_ty
                            ));
                        }
                    }
                    if !func.body.blocks.iter().any(|(id, _)| id == *then_) {
                        errors.push(format!(
                            "函数 `{}` 基本块 {:?} Branch then 目标 {:?} 不在 blocks arena 中",
                            func.name, block_id, then_
                        ));
                    }
                    if !func.body.blocks.iter().any(|(id, _)| id == *else_) {
                        errors.push(format!(
                            "函数 `{}` 基本块 {:?} Branch else 目标 {:?} 不在 blocks arena 中",
                            func.name, block_id, else_
                        ));
                    }
                }
                Terminator::Return(opt_val) => {
                    if is_reachable {
                        if let Some(val) = opt_val {
                            validate_operand(func, block_id, val, &mut errors);
                            if let Some(val_ty) = operand_type(func, val) {
                                if val_ty != func.ret {
                                    errors.push(format!(
                                        "函数 `{}` 返回值类型不匹配：期望 {:?}，得到 {:?}",
                                        func.name, func.ret, val_ty
                                    ));
                                }
                            }
                        } else if func.ret != MirType::Unit {
                            errors.push(format!(
                                "函数 `{}` 期望返回 {:?}，但终结符为空 Return",
                                func.name, func.ret
                            ));
                        }
                    }
                }
            }
        }
    }

    if errors.is_empty() {
        Ok(())
    } else {
        Err(errors)
    }
}

fn operand_type(func: &crate::mir::Function, op: &Operand) -> Option<MirType> {
    match op {
        Operand::Local(loc) => func
            .body
            .locals
            .iter()
            .find(|(id, _)| id == loc)
            .map(|(_, l)| l.ty.clone()),
        Operand::Const(c) => Some(match c {
            Constant::I64(_) => MirType::I64,
            Constant::F64(_) => MirType::F64,
            Constant::Bool(_) => MirType::Bool,
            Constant::Str(_) => MirType::Str,
        }),
    }
}

fn validate_rvalue(
    func: &crate::mir::Function,
    block_id: crate::mir::BlockId,
    rvalue: &Rvalue,
    program: &Program,
    errors: &mut Vec<String>,
) {
    match rvalue {
        Rvalue::Use(op) => validate_operand(func, block_id, op, errors),
        Rvalue::Binary { op: _, lhs, rhs } => {
            validate_operand(func, block_id, lhs, errors);
            validate_operand(func, block_id, rhs, errors);
            if let (Some(l_ty), Some(r_ty)) = (operand_type(func, lhs), operand_type(func, rhs)) {
                if l_ty != r_ty {
                    errors.push(format!(
                        "函数 `{}` 基本块 {:?} 二元运算两侧操作数类型不一致：{:?} vs {:?}",
                        func.name, block_id, l_ty, r_ty
                    ));
                }
            }
        }
        Rvalue::Unary { operand, .. } => validate_operand(func, block_id, operand, errors),
        Rvalue::Cast { operand, .. } => validate_operand(func, block_id, operand, errors),
        Rvalue::Call { callee, args } => {
            if let Callee::Fn(fn_id) = callee {
                if let Some((_, target_fn)) = program.functions.iter().find(|(id, _)| id == fn_id) {
                    if args.len() != target_fn.params.len() {
                        errors.push(format!(
                            "函数 `{}` 调用 `{}` 实参个数不匹配：期望 {}，得到 {}",
                            func.name,
                            target_fn.name,
                            target_fn.params.len(),
                            args.len()
                        ));
                    }
                } else {
                    errors.push(format!(
                        "函数 `{}` 基本块 {:?} 调用的函数 {:?} 不在 functions arena 中",
                        func.name, block_id, fn_id
                    ));
                }
            }
            for arg in args {
                validate_operand(func, block_id, arg, errors);
            }
        }
        Rvalue::NewObject { args, .. } => {
            for arg in args {
                validate_operand(func, block_id, arg, errors);
            }
        }
        Rvalue::GetField { obj, .. } => validate_operand(func, block_id, obj, errors),
    }
}

fn validate_operand(
    func: &crate::mir::Function,
    block_id: crate::mir::BlockId,
    op: &Operand,
    errors: &mut Vec<String>,
) {
    if let Operand::Local(loc) = op {
        if !func.body.locals.iter().any(|(id, _)| id == *loc) {
            errors.push(format!(
                "函数 `{}` 基本块 {:?} 引用的局部变量 {:?} 不在 locals arena 中",
                func.name, block_id, loc
            ));
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::mir::{BasicBlock, Function, MirType, Terminator};
    use la_arena::Arena;

    #[test]
    fn catches_unreachable_terminator() {
        let mut functions = Arena::default();
        let mut blocks = Arena::default();
        let entry = blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: Terminator::Unreachable,
        });
        let func = Function {
            name: "main".to_string(),
            params: Vec::new(),
            ret: MirType::Unit,
            body: crate::mir::Body {
                locals: Arena::default(),
                blocks,
                entry,
            },
        };
        let entry_fn = functions.alloc(func);
        let prog = Program {
            functions,
            classes: Arena::default(),
            entry: entry_fn,
        };
        assert!(validate(&prog).is_err());
    }
}
