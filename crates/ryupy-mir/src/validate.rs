//! MIR 合法性检查。codegen 之前必跑，Pass 之后在 debug 构建下也跑。
//!
//! 检查项：
//! - [x] 不存在 `Terminator::Unreachable` 占位
//! - [x] 所有 `LocalId` / `BlockId` 都在各自 arena 范围内
//! - [x] `Binary` 两侧操作数类型一致；`Branch` 条件为 `Bool`
//! - [x] `Return` 的值类型与函数返回类型一致
//! - [x] `Call` 实参个数与被调函数签名一致
//! - [x] 异常区域的块区间连续、处理入口在区间之外、区域之间正确嵌套

use crate::mir::{
    Callee, Constant, MirType, Operand, Program, RuntimeFn, Rvalue, Statement, Terminator,
};

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

        validate_regions(func, &mut errors);

        for &param in &func.params {
            if !func.body.locals.iter().any(|(id, _)| id == param) {
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
                        if !func.body.locals.iter().any(|(id, _)| id == *dest) {
                            errors.push(format!(
                                "函数 `{}` 基本块 {:?} 中的赋值目标 {:?} 不在 locals arena 中",
                                func.name, block_id, dest
                            ));
                        }
                        validate_rvalue(func, block_id, value, program, &mut errors);
                        if matches!(
                            value,
                            Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::NumericAccumulatorAdd),
                                ..
                            }
                        ) {
                            errors.push(format!(
                                "函数 `{}` 的累加器Add无返回值，不能写入局部",
                                func.name
                            ));
                        }
                    }
                    Statement::Eval(rvalue) => {
                        validate_rvalue(func, block_id, rvalue, program, &mut errors);
                        if matches!(
                            rvalue,
                            Rvalue::Call {
                                callee: Callee::Runtime(
                                    RuntimeFn::GeneratorInput
                                        | RuntimeFn::GeneratorRetry
                                        | RuntimeFn::PrintTarget
                                        | RuntimeFn::NumericAccumulatorCreate
                                        | RuntimeFn::NumericAccumulatorValue
                                ),
                                ..
                            }
                        ) {
                            errors.push(format!(
                                "函数 `{}` 的运行时控制调用返回值必须写入临时，不能遗留CIL求值栈",
                                func.name
                            ));
                        }
                    }
                    Statement::SetField { obj, value, .. } => {
                        validate_operand(func, block_id, obj, &mut errors);
                        validate_operand(func, block_id, value, &mut errors);
                    }
                    Statement::SetGlobal { dest, value } => {
                        if !program.globals.iter().any(|(id, _)| id == *dest) {
                            errors.push(format!(
                                "函数 `{}` 基本块 {:?} 写入了不存在的全局槽位 {:?}",
                                func.name, block_id, dest
                            ));
                        }
                        validate_operand(func, block_id, value, &mut errors);
                    }
                    // 只是给 traceback 记录行号，没有操作数
                    Statement::Line(_) => {}
                }
            }

            match &block.terminator {
                Terminator::Raise { value, cause } => {
                    if let Some(v) = value {
                        validate_operand(func, block_id, v, &mut errors);
                    }
                    if let Some(c) = cause {
                        validate_operand(func, block_id, c, &mut errors);
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
                    validate_operand(func, block_id, cond, &mut errors);
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
                Terminator::Suspend { value, resume, .. } => {
                    validate_operand(func, block_id, value, &mut errors);
                    errors.push(format!(
                        "函数 `{}` 有尚未降级的暂停边 {:?}",
                        func.name, resume
                    ));
                }
                Terminator::Return(opt_val) => {
                    if is_reachable {
                        if let Some(val) = opt_val {
                            validate_operand(func, block_id, val, &mut errors);
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

/// 校验异常区域：区间连续、处理入口在区间外、嵌套关系正确。
///
/// codegen 直接按「块下标区间」换算 CIL 异常表的指令区间，所以区间必须严格连续；
/// 一旦嵌套顺序错了（例如内层区域越过了外层边界），生成的异常表会被 CLR 判为非法，
/// 而这种错误在 IL 里非常难查，必须在验证阶段拦下来。
fn validate_regions(func: &crate::mir::Function, errors: &mut Vec<String>) {
    let block_count = func.body.blocks.len();
    let mut spans: Vec<(usize, usize)> = Vec::new();
    for region in &func.body.regions {
        let start = region.try_start.into_raw().into_u32() as usize;
        let end = start + region.try_len;
        if end > block_count {
            errors.push(format!(
                "函数 `{}` 的异常区域越界：块区间 [{start}, {end}) 超出 {} 个块",
                func.name, block_count
            ));
            continue;
        }
        if region.try_len == 0 {
            errors.push(format!("函数 `{}` 的异常区域块数为 0", func.name));
        }
        let handler = region.handler.into_raw().into_u32() as usize;
        if (start..end).contains(&handler) {
            errors.push(format!(
                "函数 `{}` 的异常处理入口块 {:?} 落在受保护区间 [{start}, {end}) 内",
                func.name, region.handler
            ));
        }
        if !func.body.blocks.iter().any(|(id, _)| id == region.handler) {
            errors.push(format!(
                "函数 `{}` 的异常处理入口块 {:?} 不在 blocks arena 中",
                func.name, region.handler
            ));
        }
        spans.push((start, end));
    }
    // 区域之间必须嵌套或不相交。
    // 起始块相同是正常情况（try 体的第一条语句既是内层 catch 的起点，也是外层 finally 通道的起点），
    // 所以排序时把较长（外层）的区间排在前面。
    spans.sort_unstable_by(|a, b| a.0.cmp(&b.0).then(b.1.cmp(&a.1)));
    for i in 0..spans.len() {
        for j in (i + 1)..spans.len() {
            let (s1, e1) = spans[i];
            let (s2, e2) = spans[j];
            let nested = s2 >= s1 && e2 <= e1;
            let disjoint = e1 <= s2;
            if !nested && !disjoint {
                errors.push(format!(
                    "函数 `{}` 的异常区域 [{s1}, {e1}) 与 [{s2}, {e2}) 交叉，CIL 不允许这种嵌套",
                    func.name
                ));
            }
        }
    }
}

fn operand_type(program: &Program, func: &crate::mir::Function, op: &Operand) -> Option<MirType> {
    match op {
        Operand::Local(loc) => func
            .body
            .locals
            .iter()
            .find(|(id, _)| id == loc)
            .map(|(_, l)| l.ty.clone()),
        Operand::Global(global) => Some(program.globals[*global].ty.clone()),
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
        Rvalue::Use(op) => validate_operand(func, block_id, op, errors),
        Rvalue::GeneratorDelegateStep { delegate } => {
            validate_operand(func, block_id, delegate, errors);
            errors.push(format!("函数 `{}` 在后端前仍有未降级的委托推进", func.name));
        }
        Rvalue::MakeGenerator { resume, frame } => {
            let target = program.functions.iter().find(|(id, _)| id == resume);
            if !target.is_some_and(|(_, target)| {
                target.params.len() == 1
                    && target.ret == MirType::Dynamic
                    && target
                        .body
                        .locals
                        .iter()
                        .any(|(id, p)| id == target.params[0] && p.ty == MirType::Dynamic)
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
                validate_operand(func, block_id, value, errors);
                if operand_type(program, func, value).is_some_and(|ty| ty != MirType::Dynamic) {
                    errors.push(format!("函数 `{}` 的生成器帧必须保存装箱值", func.name));
                }
            }
        }
        Rvalue::MakeFunction {
            func: target,
            env,
            defaults,
        } => {
            // 默认值必须覆盖所有 Python 参数槽位；环境参数不参与 binder。
            if let Some((_, nested)) = program.functions.iter().find(|(id, _)| id == target) {
                if defaults.len() != nested.param_meta.len()
                    || env.is_empty() == nested.env.is_some()
                    || nested.signature_global.is_none()
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
            for op in env.iter().chain(defaults.iter()) {
                validate_operand(func, block_id, op, errors);
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
            validate_operand(func, block_id, lhs, errors);
            validate_operand(func, block_id, rhs, errors);
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
        Rvalue::Unary { operand, .. } => validate_operand(func, block_id, operand, errors),
        Rvalue::Cast { operand, .. } => validate_operand(func, block_id, operand, errors),
        Rvalue::Call { callee, args } => {
            // 输出与累加器专用ABI校验，防止参数错配生成栈不平衡的CIL。
            if let Callee::Runtime(runtime) = callee {
                let expected: Option<&[MirType]> = match runtime {
                    RuntimeFn::PrintTarget => Some(&[]),
                    RuntimeFn::NumericAccumulatorCreate | RuntimeFn::NumericAccumulatorValue => {
                        Some(&[MirType::Dynamic])
                    }
                    RuntimeFn::NumericAccumulatorAdd => Some(&[MirType::Dynamic, MirType::Dynamic]),
                    RuntimeFn::WriteI64 => Some(&[MirType::Dynamic, MirType::I64]),
                    RuntimeFn::WriteF64 => Some(&[MirType::Dynamic, MirType::F64]),
                    RuntimeFn::WriteBool => Some(&[MirType::Dynamic, MirType::Bool]),
                    RuntimeFn::WriteStr => Some(&[MirType::Dynamic, MirType::Str]),
                    RuntimeFn::WriteObject => Some(&[MirType::Dynamic, MirType::Dynamic]),
                    RuntimeFn::WriteSep | RuntimeFn::WriteNewline => Some(&[MirType::Dynamic]),
                    _ => None,
                };
                if let Some(expected) = expected
                    && (args.len() != expected.len()
                        || args.iter().zip(expected).any(|(arg, ty)| {
                            operand_type(program, func, arg).is_some_and(|actual| &actual != ty)
                        }))
                {
                    errors.push(format!(
                        "函数 {} 的 {:?} 参数不符合{}",
                        func.name,
                        runtime,
                        if matches!(
                            runtime,
                            RuntimeFn::NumericAccumulatorCreate
                                | RuntimeFn::NumericAccumulatorAdd
                                | RuntimeFn::NumericAccumulatorValue
                        ) {
                            "累加器ABI"
                        } else {
                            "输出目标ABI"
                        }
                    ));
                }
            }
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
        Rvalue::Box { value, from } => {
            validate_operand(func, block_id, value, errors);
            if let Some(v_ty) = operand_type(program, func, value)
                && &v_ty != from
            {
                errors.push(format!(
                    "函数 `{}` 基本块 {:?} 装箱类型不一致：声明 {:?}，实际 {:?}",
                    func.name, block_id, from, v_ty
                ));
            }
        }
        Rvalue::Unbox { value, .. } => {
            validate_operand(func, block_id, value, errors);
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
            validate_operand(func, block_id, lhs, errors);
            validate_operand(func, block_id, rhs, errors);
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
            validate_operand(func, block_id, operand, errors);
            if let Some(ty) = operand_type(program, func, operand)
                && ty != MirType::Dynamic
            {
                errors.push(format!(
                    "函数 `{}` 基本块 {:?} 动态一元运算的操作数必须是 dyn，得到 {:?}",
                    func.name, block_id, ty
                ));
            }
        }
        Rvalue::CallWithArgs { prefix, args, .. } => {
            for op in prefix.iter().chain(args.iter()) {
                validate_operand(func, block_id, op, errors);
            }
        }
        Rvalue::DynCallValue { callee, args } => {
            validate_operand(func, block_id, callee, errors);
            for arg in args {
                validate_operand(func, block_id, arg, errors);
            }
        }
        Rvalue::DynCallMethod { obj, args, .. } => {
            validate_operand(func, block_id, obj, errors);
            for arg in args {
                validate_operand(func, block_id, arg, errors);
            }
        }
        Rvalue::CatchException => {}
        Rvalue::DynTruthy { value } => {
            validate_operand(func, block_id, value, errors);
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
            codegen_name: "main".to_string(),
            source_file: "t.py".to_string(),
            params: Vec::new(),
            ret: MirType::Unit,
            body: crate::mir::Body {
                locals: Arena::default(),
                blocks,
                entry,
                regions: Vec::new(),
            },
            param_meta: Vec::new(),
            function_global: None,
            defaults_global: None,
            signature_global: None,
            env: None,
        };
        let entry_fn = functions.alloc(func);
        let prog = Program {
            functions,
            classes: Arena::default(),
            globals: Arena::default(),
            entry: entry_fn,
            source_name: "t.py".to_string(),
        };
        assert!(validate(&prog).is_err());
    }
}
