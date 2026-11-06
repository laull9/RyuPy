//! 实例环境参数及定义捕获的归属验证；禁止静态槽绕过唯一公开字典。
use crate::*;
pub(crate) fn validate(program: &Program, errors: &mut Vec<String>) {
    for (id, function) in program.functions.iter() {
        let contexts = trusted_contexts(function);
        let valid = match (function.module_state, function.module_state_source) {
            (None, None) => true,
            (Some(state), Some(source)) => {
                crate::validate::local_decl(&function.body.locals, state)
                    .is_some_and(|local| local.ty == MirType::Dynamic && !local.is_cell)
                    && match source {
                        InstanceStateSource::ModuleEntryParameter => {
                            id == program.entry
                                && function.params == [state]
                                && function.param_meta.is_empty()
                                && function.env.is_none()
                                && function.dynamic_origin.is_none()
                                && function.ret == MirType::Unit
                        }
                        InstanceStateSource::FunctionParameter => {
                            function.params.get(
                                function.param_meta.len() + usize::from(function.env.is_some()),
                            ) == Some(&state)
                                && function.dynamic_origin.is_some()
                                && function.params.iter().filter(|p| **p == state).count() == 1
                                && function.signature_global.is_some()
                                && function.function_global.is_none()
                                && function.defaults_global.is_none()
                                && function.specialization_identity_global.is_none()
                                && function
                                    .dynamic_origin
                                    .is_none_or(|o| o.source_global.is_none())
                        }
                        InstanceStateSource::GeneratorFrame { frame, slot } => {
                            function.params == [frame]
                                && function.body.blocks.iter().find_map(|(id, block)| (id == function.body.entry).then_some(block)).is_some_and(|block| block.stmts.iter().filter(|s| matches!(s,
                                    Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArrayGet), args } }
                                    if *dest == state && matches!(args.as_slice(), [Operand::Local(f), Operand::Const(Constant::I64(i))] if *f == frame && *i == slot as i64)
                                )).count() == 1)
                                && slot == state.into_raw().into_u32() as usize + 4
                                && function.param_meta.is_empty()
                                && function.env.is_none()
                                && function.dynamic_origin.is_none()
                        }
                    }
            }
            _ => false,
        };
        if !valid {
            errors.push(format!(
                "函数 `{}` 的实例state来源或隐藏参数非法",
                function.name
            ));
        }
        for (block_id, block) in function.body.blocks.iter() {
            for (statement_index, stmt) in block.stmts.iter().enumerate() {
                if let Statement::Assign { dest, .. } = stmt
                    && matches!(function.module_state_source, Some(InstanceStateSource::GeneratorFrame { frame, .. }) if *dest == frame)
                {
                    errors.push(format!("函数 `{}` 不得覆写生成器frame参数", function.name));
                }
                if let Statement::Assign { dest, value } = stmt
                    && Some(*dest) == function.module_state
                {
                    let restore = match function.module_state_source {
                        Some(InstanceStateSource::GeneratorFrame { frame, slot }) => {
                            block_id == function.body.entry
                                && matches!(value,
                                Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArrayGet), args }
                                if matches!(args.as_slice(), [Operand::Local(f), Operand::Const(Constant::I64(i))]
                                    if *f == frame && *i == slot as i64))
                        }
                        _ => false,
                    };
                    if !restore {
                        errors.push(format!("函数 `{}` 不得覆写实例state", function.name));
                    }
                }
                if let Statement::Assign { value, .. } | Statement::Eval(value) = stmt {
                    validate_frame_write(function, value, block_id, statement_index, errors);
                    if let Rvalue::Call {
                        callee: Callee::Runtime(runtime),
                        args,
                    } = value
                        && matches!(
                            runtime,
                            RuntimeFn::InstanceClassGuard
                                | RuntimeFn::InstanceMakeClass
                                | RuntimeFn::InstanceImportGuard
                                | RuntimeFn::InstanceLoadName
                                | RuntimeFn::InstanceStoreName
                                | RuntimeFn::InstanceDeleteName
                                | RuntimeFn::InstanceExecutionSource
                                | RuntimeFn::InstanceCaptureDefinitionContext
                                | RuntimeFn::InstanceDefinitionModule
                        )
                        && !matches!(args.first(), Some(Operand::Local(state)) if contexts.contains(state) && context_dominates(function, *state, block_id, statement_index))
                    {
                        errors.push(format!(
                            "函数 `{}` 的实例helper必须接显式state参数",
                            function.name
                        ));
                    }
                    if let Rvalue::MakeGenerator { resume, frame, .. } = value
                        && let Some(target) = program
                            .functions
                            .iter()
                            .find_map(|(id, f)| (id == *resume).then_some(f))
                        && let Some(InstanceStateSource::GeneratorFrame { slot, .. }) =
                            target.module_state_source
                        && (function.module_state_source
                            != Some(InstanceStateSource::FunctionParameter)
                            || !matches!(frame.get(slot), Some(Operand::Local(state)) if Some(*state) == function.module_state))
                    {
                        errors.push(format!(
                            "函数 `{}` 的生成器frame必须保存实际实例state",
                            function.name
                        ));
                    }
                    if let Rvalue::MakeFunction {
                        instance_context: Some(context),
                        ..
                    } = value
                    {
                        let produced = if let Operand::Local(local) = context {
                            let last =
                                block.stmts[..statement_index]
                                    .iter()
                                    .rev()
                                    .find_map(|s| match s {
                                        Statement::Assign { dest, value } if dest == local => {
                                            Some(value)
                                        }
                                        _ => None,
                                    });
                            matches!(last, Some(Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::InstanceCaptureDefinitionContext), args
                            }) if matches!(args.as_slice(), [Operand::Local(state)] if contexts.contains(state) && context_dominates(function, *state, block_id, statement_index)))
                        } else {
                            false
                        };
                        if !produced {
                            errors.push(format!(
                                "函数 `{}` 的定义context必须来自唯一显式捕获",
                                function.name
                            ));
                        }
                    }
                }
            }
        }
    }
}
pub(crate) fn validate_operand(
    program: &Program,
    func: &Function,
    block: BlockId,
    op: &Operand,
    errors: &mut Vec<String>,
) {
    match op {
        Operand::Local(local)
            if crate::validate::local_decl(&func.body.locals, *local).is_none() =>
        {
            errors.push(format!(
                "函数 `{}` 基本块 {:?} 引用的局部变量 {:?} 不在 locals arena 中",
                func.name, block, local
            ))
        }
        Operand::Global(global)
            if program.globals.iter().any(|(id, declaration)| {
                id == *global && matches!(declaration.storage, GlobalStorage::InstancePublic { .. })
            }) =>
        {
            errors.push(format!(
                "函数 `{}` 不得通过静态GlobalId访问实例公开字典",
                func.name
            ))
        }
        Operand::Array(values) => {
            for value in values {
                validate_operand(program, func, block, value, errors);
            }
        }
        _ => {}
    }
}

/// 只有从已有环境捕获、且没有其它赋值的local可供inline类体使用。
/// 生成器恢复由单独source/frame验证；捕获结果不依赖可变模块名字。
fn trusted_contexts(function: &Function) -> rustc_hash::FxHashSet<LocalId> {
    let mut trusted = rustc_hash::FxHashSet::default();
    if let Some(state) = function.module_state {
        trusted.insert(state);
    } else {
        return trusted;
    }
    let mut definitions = std::collections::HashMap::<LocalId, Vec<&Rvalue>>::new();
    for (_, block) in function.body.blocks.iter() {
        for stmt in &block.stmts {
            if let Statement::Assign { dest, value } = stmt {
                definitions.entry(*dest).or_default().push(value);
            }
        }
    }
    loop {
        let before = trusted.len();
        for (dest, values) in &definitions {
            if matches!(values.as_slice(), [Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceCaptureDefinitionContext), args
            }] if matches!(args.as_slice(), [Operand::Local(parent)] if trusted.contains(parent)))
            {
                trusted.insert(*dest);
            }
        }
        if trusted.len() == before {
            break;
        }
    }
    trusted
}

/// 唯一捕获定义必须支配使用，不能靠另一分支中的同名local伪造环境。
/// 异常边保守加入所有受保护块；root参数不需要CFG证明。
pub(crate) fn context_dominates(
    function: &Function,
    context: LocalId,
    usage: BlockId,
    index: usize,
) -> bool {
    if Some(context) == function.module_state
        && !matches!(
            function.module_state_source,
            Some(InstanceStateSource::GeneratorFrame { .. })
        )
    {
        return true;
    }
    let definition = function.body.blocks.iter().find_map(|(block, body)| {
        body.stmts.iter().enumerate().find_map(|(i, s)| {
            matches!(s, Statement::Assign { dest, .. } if *dest == context).then_some((block, i))
        })
    });
    let Some((definition, position)) = definition else {
        return false;
    };
    if definition == usage {
        return position < index;
    }
    let mut visited = rustc_hash::FxHashSet::default();
    let mut pending = vec![function.body.entry];
    while let Some(id) = pending.pop() {
        if !visited.insert(id) {
            continue;
        }
        if id == definition {
            // 普通capture会抛异常，定义块不能连异常边一起剪掉。
            // GeneratorFrame的ArrayGet是已验证工厂帧恢复协议，非用户回调。
            if Some(context) != function.module_state {
                add_exception_edges(function, id, &mut pending);
            }
            continue;
        }
        if id == usage {
            return false;
        }
        let Some(block) = function
            .body
            .blocks
            .iter()
            .find_map(|(b, block)| (b == id).then_some(block))
        else {
            return false;
        };
        match block.terminator {
            Terminator::Goto(target) => pending.push(target),
            Terminator::Branch { then_, else_, .. } => pending.extend([then_, else_]),
            Terminator::Suspend { resume, .. } => pending.push(resume),
            _ => {}
        }
        add_exception_edges(function, id, &mut pending);
    }
    true
}

/// 实例core必须通过函数对象携带owner；不接受任意直接调用实参伪造来源。
pub(crate) fn validate_direct_call(caller: &Function, target: &Function, errors: &mut Vec<String>) {
    if target.module_state.is_some() {
        errors.push(format!(
            "函数 `{}` 不得直接调用实例core `{}`；必须通过实际函数owner",
            caller.name, target.name
        ));
    }
}

fn add_exception_edges(function: &Function, id: BlockId, pending: &mut Vec<BlockId>) {
    let number = id.into_raw().into_u32() as usize;
    for region in &function.body.regions {
        let start = region.try_start.into_raw().into_u32() as usize;
        if number >= start && number < start + region.try_len {
            pending.push(region.handler);
        }
    }
}

/// 只验证显式本frame写入合同，不声称证明任意MIR别名程序。
/// 固定非owner槽保持现控制协议；owner只能在恢复后原引用spill回同槽。
fn validate_frame_write(
    function: &Function,
    value: &Rvalue,
    block: BlockId,
    index: usize,
    errors: &mut Vec<String>,
) {
    let Some(InstanceStateSource::GeneratorFrame { frame, slot }) = function.module_state_source
    else {
        return;
    };
    let args = match value {
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ArraySet),
            args,
        } => args,
        Rvalue::CallWithArgs {
            callee: RuntimeFn::ArraySet,
            prefix,
            ..
        } if matches!(prefix.first(), Some(Operand::Local(local)) if *local == frame) => {
            errors.push(format!(
                "函数 `{}` 的生成器frame写入必须使用固定槽ABI",
                function.name
            ));
            return;
        }
        _ => return,
    };
    if !matches!(args.first(), Some(Operand::Local(local)) if *local == frame) {
        return;
    }
    let valid = match args.as_slice() {
        [_, Operand::Const(Constant::I64(offset)), value] if *offset >= 0 => {
            *offset != slot as i64
                || matches!(value, Operand::Local(state)
                if Some(*state) == function.module_state && context_dominates(function, *state, block, index))
        }
        _ => false,
    };
    if !valid {
        errors.push(format!(
            "函数 `{}` 的生成器owner槽仅允许恢复后原state写回，禁止未知槽写入",
            function.name
        ));
    }
}
