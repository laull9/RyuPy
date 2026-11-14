//! MIR 合法性检查。codegen 之前必跑，流水线中每个 Pass 之后始终运行。
//!
//! 检查项：
//! - [x] 不存在 `Terminator::Unreachable` 占位
//! - [x] 所有 `LocalId` / `BlockId` 都在各自 arena 范围内
//! - [x] `Binary` 两侧操作数类型一致；`Branch` 条件为 `Bool`
//! - [x] `Return` 的值类型与函数返回类型一致
//! - [x] `Call` 实参个数与被调函数签名一致
//! - [x] 异常区域的块区间连续、处理入口在区间之外、区域之间正确嵌套

pub use crate::validate_exports::validate_linked_exports;

use crate::mir::{
    Callee, Constant, LocalDecl, LocalId, MirType, Operand, Program, RuntimeFn, Rvalue, Statement,
    Terminator,
};

use crate::validate_instance_module::validate_operand;
use crate::validate_regions::validate_regions;

pub fn validate(program: &Program) -> Result<(), Vec<String>> {
    validate_stage(program, None)
}

/// 高层阶段仍验证全部原有义务，只为真实生成器身份开放合法暂停边。
pub fn validate_high(program: &Program, generators: &[crate::FnId]) -> Result<(), Vec<String>> {
    validate_stage(program, Some(generators))
}

fn validate_stage(
    program: &Program,
    generators: Option<&[crate::FnId]>,
) -> Result<(), Vec<String>> {
    let mut errors = Vec::new();
    crate::source_provenance::validate_program(program, &mut errors);
    crate::source_handler_contract::validate_functions(&program.functions, &mut errors);
    crate::direct_globals_contract::validate_functions(&program.functions, &mut errors);
    crate::validate_instance_module::validate(program, &mut errors);
    crate::validate_class_cells::validate(program, &mut errors);
    crate::validate_lexical_super::validate(program, &mut errors);
    crate::validate_identities::validate(program, &mut errors);
    crate::function_definition::validate(program, &mut errors);
    crate::validate_dynamic_origin::validate(program, &mut errors);
    crate::validate_vectors::validate(program, &mut errors);
    crate::validate_module_pairs::validate(program, &mut errors);
    crate::validate_list_reads::validate(program, &mut errors);
    if !program.functions.iter().any(|(id, _)| id == program.entry) {
        errors.push(format!(
            "程序入口函数 {:?} 不在 functions arena 中",
            program.entry
        ));
    }
    for (fn_id, func) in program.functions.iter() {
        let high = generators.is_some_and(|ids| ids.contains(&fn_id));
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
        if high {
            for region in &func.body.regions {
                if reachable.insert(region.handler) {
                    worklist.push(region.handler);
                }
            }
        }
        while let Some(bb_id) = worklist.pop() {
            if let Some((_, bb)) = func.body.blocks.iter().find(|&(id, _)| id == bb_id) {
                match &bb.terminator {
                    Terminator::Goto(target) | Terminator::Suspend { resume: target, .. } => {
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

        validate_regions(func, &mut errors);
        crate::exact_types::validate(program, func, &mut errors);

        for &param in &func.params {
            if local_decl(&func.body.locals, param).is_none() {
                errors.push(format!(
                    "函数 `{}` 的参数 {:?} 不在 locals arena 中",
                    func.name, param
                ));
            }
        }
        // `Unit` 没有 CIL 表示，不能作为局部变量（None 用 Dynamic 表示）。
        // 若放行，codegen 会为赋值发射一个没有目标的求值栈值，生成非法方法体。
        for (loc_id, local) in func.body.locals.iter() {
            if local.ty == MirType::Unit {
                errors.push(format!(
                    "函数 `{}` 的局部变量 {:?} 类型是 Unit；None 应当用 Dynamic 表示",
                    func.name, loc_id
                ));
            }
        }

        for (block_id, block) in func.body.blocks.iter() {
            let is_reachable = reachable.contains(&block_id);
            for stmt in &block.stmts {
                match stmt {
                    Statement::Assign { dest, value } => {
                        if local_decl(&func.body.locals, *dest).is_none() {
                            errors.push(format!(
                                "函数 `{}` 基本块 {:?} 中的赋值目标 {:?} 不在 locals arena 中",
                                func.name, block_id, dest
                            ));
                        }
                        validate_rvalue(func, block_id, value, program, &mut errors);
                        class_namespace::validate_destination(func, *dest, value, &mut errors);
                        runtime_calls::validate_destination(func, *dest, value, &mut errors);
                        if matches!(
                            value,
                            Rvalue::Call {
                                callee: Callee::Runtime(
                                    RuntimeFn::NumericAccumulatorAdd
                                        | RuntimeFn::CheckGlobalBound
                                        | RuntimeFn::TraceCheckDepth
                                ),
                                ..
                            }
                        ) {
                            errors.push(format!(
                                "函数 `{}` 的运行时调用无返回值，不能写入局部",
                                func.name
                            ));
                        }
                    }
                    Statement::Eval(rvalue) => {
                        validate_rvalue(func, block_id, rvalue, program, &mut errors);
                        runtime_calls::validate_eval(func, rvalue, &mut errors);
                    }
                    Statement::SetField { obj, value, .. } => {
                        validate_operand(program, func, block_id, obj, &mut errors);
                        validate_operand(program, func, block_id, value, &mut errors);
                    }
                    Statement::SetGlobal { dest, value } => {
                        validate_operand(
                            program,
                            func,
                            block_id,
                            &Operand::Global(*dest),
                            &mut errors,
                        );
                        if !program.globals.iter().any(|(id, _)| id == *dest) {
                            errors.push(format!(
                                "函数 `{}` 基本块 {:?} 写入了不存在的全局槽位 {:?}",
                                func.name, block_id, dest
                            ));
                        }
                        validate_operand(program, func, block_id, value, &mut errors);
                    }
                    // 只是给 traceback 记录行号，没有操作数
                    Statement::Line(_) => {}
                }
            }

            match &block.terminator {
                Terminator::Raise { value, cause } => {
                    if let Some(v) = value {
                        validate_operand(program, func, block_id, v, &mut errors);
                    }
                    if let Some(c) = cause {
                        validate_operand(program, func, block_id, c, &mut errors);
                    }
                }
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
                    validate_operand(program, func, block_id, cond, &mut errors);
                    if let Some(cond_ty) = operand_type(program, func, cond) {
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
                Terminator::Suspend {
                    value,
                    resume,
                    input,
                    delegated,
                } => {
                    validate_operand(program, func, block_id, value, &mut errors);
                    if !high {
                        errors.push(format!(
                            "函数 `{}` 有尚未降级的暂停边 {:?}",
                            func.name, resume
                        ));
                    } else if *delegated
                        || !func.body.blocks.iter().any(|(id, _)| id == *resume)
                        || operand_type(program, func, value) != Some(MirType::Dynamic)
                        || input.is_some_and(|id| {
                            local_decl(&func.body.locals, id)
                                .is_none_or(|decl| decl.ty != MirType::Dynamic)
                        })
                    {
                        errors.push(format!(
                            "函数 `{}` 的高层暂停目标/值/输入非法或包含未准入委托",
                            func.name
                        ));
                    }
                }
                Terminator::Return(opt_val) => {
                    if is_reachable {
                        if let Some(val) = opt_val {
                            validate_operand(program, func, block_id, val, &mut errors);
                            if let Some(val_ty) = operand_type(program, func, val) {
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

/// la-arena 0.3.1 的 ID 只有稠密下标；先检查范围，保留非法 ID 的 None 语义。
pub(crate) fn local_decl(locals: &la_arena::Arena<LocalDecl>, id: LocalId) -> Option<&LocalDecl> {
    if (id.into_raw().into_u32() as usize) < locals.len() {
        Some(&locals[id])
    } else {
        None
    }
}

pub(crate) fn operand_type(
    program: &Program,
    func: &crate::mir::Function,
    op: &Operand,
) -> Option<MirType> {
    match op {
        Operand::Local(loc) => local_decl(&func.body.locals, *loc).map(|l| l.ty.clone()),
        Operand::Global(global) => program
            .globals
            .iter()
            .find(|(id, _)| id == global)
            .map(|(_, decl)| decl.ty.clone()),
        // 数组的元素类型永远是 object；内联数组本身在求值栈上是 `object[]`，
        // 但在 MIR 里只作为运行时调用的参数使用，统一按 Dynamic 记。
        Operand::Array(_) => Some(MirType::Dynamic),
        Operand::Const(c) => Some(match c {
            Constant::I64(_) => MirType::I64,
            Constant::F64(_) => MirType::F64,
            Constant::Bool(_) => MirType::Bool,
            Constant::Str(_) => MirType::Str,
            // 大整数与 None 只出现在动态路径
            Constant::BigInt(_) | Constant::None | Constant::Null => MirType::Dynamic,
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
        Rvalue::Use(op) => validate_operand(program, func, block_id, op, errors),
        Rvalue::GeneratorDelegateStep { delegate } => {
            validate_operand(program, func, block_id, delegate, errors);
            errors.push(format!("函数 `{}` 在后端前仍有未降级的委托推进", func.name));
        }
        Rvalue::MakeGenerator {
            resume,
            frame,
            origin,
        } => {
            if let Some(origin) = origin {
                validate_operand(program, func, block_id, origin, errors);
                if operand_type(program, func, origin) != Some(MirType::Dynamic) {
                    errors.push(format!(
                        "函数 `{}` 的生成器owner必须是Dynamic引用",
                        func.name
                    ));
                }
            }
            let target = program.functions.iter().find(|(id, _)| id == resume);
            if !target.is_some_and(|(_, target)| {
                target.params.len() == 1
                    && target.ret == MirType::Dynamic
                    && local_decl(&target.body.locals, target.params[0])
                        .is_some_and(|p| p.ty == MirType::Dynamic)
            }) {
                errors.push(format!("函数 `{}` 的生成器恢复入口非法", func.name));
            }
            if frame.len() < 4 {
                errors.push(format!(
                    "函数 `{}` 的生成器帧缺少状态/输入/异常控制槽",
                    func.name
                ));
            }
            for value in frame {
                validate_operand(program, func, block_id, value, errors);
                if operand_type(program, func, value).is_some_and(|ty| ty != MirType::Dynamic) {
                    errors.push(format!("函数 `{}` 的生成器帧必须保存装箱值", func.name));
                }
            }
        }
        Rvalue::MakeFunction {
            instance_context,
            func: target,
            env,
            defaults,
        } => {
            // 默认值必须覆盖所有 Python 参数槽位；环境参数不参与 binder。
            if let Some((_, nested)) = program.functions.iter().find(|(id, _)| id == target) {
                if defaults.len() != nested.param_meta.len()
                    || env.is_empty() == nested.env.is_some()
                    || nested.signature_global.is_none()
                    || instance_context.is_some() != nested.module_state.is_some()
                    || (instance_context.is_some()
                        && nested.module_state_source
                            != Some(crate::InstanceStateSource::FunctionParameter))
                {
                    errors.push(format!(
                        "函数 `{}` 创建 `{}` 时默认值、环境或签名不一致",
                        func.name, nested.name
                    ));
                }
            } else {
                errors.push(format!(
                    "函数 `{}` 创建了不存在的函数 {:?}",
                    func.name, target
                ));
            }
            for op in env
                .iter()
                .chain(defaults.iter())
                .chain(instance_context.iter())
            {
                validate_operand(program, func, block_id, op, errors);
                if let Some(ty) = operand_type(program, func, op)
                    && ty != MirType::Dynamic
                {
                    errors.push(format!(
                        "函数 `{}` 的闭包环境与默认值必须装箱，得到 {:?}",
                        func.name, ty
                    ));
                }
            }
        }
        Rvalue::Binary { op: _, lhs, rhs } => {
            validate_operand(program, func, block_id, lhs, errors);
            validate_operand(program, func, block_id, rhs, errors);
            if let (Some(l_ty), Some(r_ty)) = (
                operand_type(program, func, lhs),
                operand_type(program, func, rhs),
            ) {
                if l_ty != r_ty {
                    errors.push(format!(
                        "函数 `{}` 基本块 {:?} 二元运算两侧操作数类型不一致：{:?} vs {:?}",
                        func.name, block_id, l_ty, r_ty
                    ));
                }
            }
        }
        Rvalue::Unary { operand, .. } => validate_operand(program, func, block_id, operand, errors),
        Rvalue::Cast { operand, .. } => validate_operand(program, func, block_id, operand, errors),
        Rvalue::Call { callee, args } => {
            runtime_calls::validate_call(program, func, callee, args, errors);
            if let Callee::Fn(fn_id) = callee {
                if let Some((_, target_fn)) = program.functions.iter().find(|(id, _)| id == fn_id) {
                    crate::validate_instance_module::validate_direct_call(func, target_fn, errors);
                    if target_fn.dynamic_origin.is_some()
                        && args.last().and_then(|arg| operand_type(program, func, arg))
                            != Some(MirType::Dynamic)
                    {
                        errors.push(format!(
                            "函数 `{}` 调用 `{}` 缺少Dynamic尾owner",
                            func.name, target_fn.name
                        ));
                    }
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
                validate_operand(program, func, block_id, arg, errors);
            }
        }
        Rvalue::IsVector128Supported => {}
        Rvalue::VectorPackF64 { low, high } => {
            validate_operand(program, func, block_id, low, errors);
            validate_operand(program, func, block_id, high, errors);
        }
        Rvalue::VectorBinaryF64 { lhs, rhs, .. } => {
            validate_operand(program, func, block_id, lhs, errors);
            validate_operand(program, func, block_id, rhs, errors);
        }
        Rvalue::NewObject { args, .. } => {
            for arg in args {
                validate_operand(program, func, block_id, arg, errors);
            }
        }
        Rvalue::GetField { obj, .. } => validate_operand(program, func, block_id, obj, errors),
        Rvalue::Box { value, from } => {
            validate_operand(program, func, block_id, value, errors);
            if let Some(v_ty) = operand_type(program, func, value)
                && &v_ty != from
            {
                errors.push(format!(
                    "函数 `{}` 基本块 {:?} 装箱类型不一致：声明 {:?}，实际 {:?}",
                    func.name, block_id, from, v_ty
                ));
            }
        }
        Rvalue::VectorLaneF64 { value, .. } => {
            validate_operand(program, func, block_id, value, errors)
        }
        Rvalue::Unbox { value, .. }
        | Rvalue::IsBoxedF64 { value }
        | Rvalue::UnboxExactF64 { value }
        | Rvalue::IsBoxedI64 { value }
        | Rvalue::UnboxExactI64 { value } => {
            validate_operand(program, func, block_id, value, errors);
            if let Some(v_ty) = operand_type(program, func, value)
                && v_ty != MirType::Dynamic
            {
                errors.push(format!(
                    "函数 `{}` 基本块 {:?} 拆箱的源类型必须是 dyn，得到 {:?}",
                    func.name, block_id, v_ty
                ));
            }
        }
        Rvalue::DynBinary { lhs, rhs, .. } | Rvalue::DynCompare { lhs, rhs, .. } => {
            validate_operand(program, func, block_id, lhs, errors);
            validate_operand(program, func, block_id, rhs, errors);
            for op in [lhs, rhs] {
                if let Some(ty) = operand_type(program, func, op)
                    && ty != MirType::Dynamic
                {
                    errors.push(format!(
                        "函数 `{}` 基本块 {:?} 动态运算的操作数必须是 dyn，得到 {:?}",
                        func.name, block_id, ty
                    ));
                }
            }
        }
        Rvalue::DynUnary { operand, .. } => {
            validate_operand(program, func, block_id, operand, errors);
            if let Some(ty) = operand_type(program, func, operand)
                && ty != MirType::Dynamic
            {
                errors.push(format!(
                    "函数 `{}` 基本块 {:?} 动态一元运算的操作数必须是 dyn，得到 {:?}",
                    func.name, block_id, ty
                ));
            }
        }
        Rvalue::CallWithArgs {
            callee,
            prefix,
            args,
        } => {
            if runtime_calls::fixed_abi(*callee) {
                errors.push(format!(
                    "函数 `{}` 的{:?}只允许固定Call ABI",
                    func.name, callee
                ));
            }
            for op in prefix.iter().chain(args.iter()) {
                validate_operand(program, func, block_id, op, errors);
            }
        }
        Rvalue::InstanceZeroCall { callee, .. } => {
            validate_operand(program, func, block_id, callee, errors);
        }
        Rvalue::DynCallValue { callee, args } => {
            validate_operand(program, func, block_id, callee, errors);
            for arg in args {
                validate_operand(program, func, block_id, arg, errors);
            }
        }
        Rvalue::DynCallMethod {
            captured,
            obj,
            args,
            ..
        } => {
            if let Some(flag) = captured {
                validate_operand(program, func, block_id, flag, errors);
            }
            validate_operand(program, func, block_id, obj, errors);
            for arg in args {
                validate_operand(program, func, block_id, arg, errors);
            }
        }
        Rvalue::CatchException => {}
        Rvalue::DynTruthy { value } => {
            validate_operand(program, func, block_id, value, errors);
            if let Some(ty) = operand_type(program, func, value)
                && ty != MirType::Dynamic
            {
                errors.push(format!(
                    "函数 `{}` 基本块 {:?} 真值判定的操作数必须是 dyn，得到 {:?}",
                    func.name, block_id, ty
                ));
            }
        }
    }
}

#[cfg(test)]
#[path = "validate_tests.rs"]
mod tests;

#[path = "validate_class_namespace.rs"]
mod class_namespace;

#[path = "validate_runtime_calls.rs"]
mod runtime_calls;
