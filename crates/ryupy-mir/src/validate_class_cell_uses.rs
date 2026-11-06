//! 限制 class cell 的窄使用形状：同源 Env 捕获、既有固定帧保存与唯一初始化。
//! 不求解任意别名；不符合生成代码协议的 raw cell 逃逸一律在 MIR 阶段拒绝。
use crate::validate_class_cells::{
    Position, assignments, environment, environment_available, extracted_cell,
};
use crate::*;
use rustc_hash::{FxHashMap, FxHashSet};
pub(crate) fn validate_cell_access(
    function: &Function,
    capture: &ClassCellCapture,
    value: &Rvalue,
    position: Position,
    errors: &mut Vec<String>,
) {
    let Some(env) = environment(function, capture) else {
        return;
    };
    let mut raw_reads = FxHashSet::default();
    crate::passes::dead_values::read_value(value, &mut raw_reads);
    if let ClassCellEnvSource::GeneratorFrame { frame, .. } = capture.source
        && raw_reads.contains(&frame)
        && !allowed_frame_use(frame, value)
    {
        errors.push("类 cell 生成器原始frame不得别名或逃逸".into());
    }
    if raw_reads.contains(&env) && !allowed_environment_use(capture, env, value) {
        errors.push("类 cell Env 只允许固定读取/帧保存，不得别名或逃逸".into());
    }
    if matches!(
        value,
        Rvalue::CallWithArgs {
            callee: RuntimeFn::CellSet | RuntimeFn::ArraySet,
            ..
        }
    ) {
        errors.push("类 cell 与生成器帧写入必须使用固定槽协议".into());
    }
    if let Rvalue::Call {
        callee: Callee::Runtime(runtime),
        args,
    } = value
    {
        if *runtime == RuntimeFn::CellGetFree
            && matches!(args.last(), Some(Operand::Const(Constant::Str(name))) if name == "__class__")
            && (!matches!(args.as_slice(), [Operand::Local(source), Operand::Const(Constant::I64(index)), _] if *source == env && *index == capture.index as i64)
                || !environment_available(function, capture, position))
        {
            errors.push(format!(
                "函数 `{}` 读取类 cell 必须使用本函数已恢复的同源 Env 槽",
                function.name
            ));
        }
        if *runtime == RuntimeFn::ArrayGet
            && matches!(args.as_slice(), [Operand::Local(source), Operand::Const(Constant::I64(index))] if *source == env && *index == capture.index as i64)
            && !environment_available(function, capture, position)
        {
            errors.push("生成器类 cell 只能在 Env 恢复之后抽取".into());
        }
        if matches!(runtime, RuntimeFn::CellSet | RuntimeFn::ArraySet)
            && args.first().is_some_and(|operand| {
                extracted_cell(function, capture, operand, position)
                    || matches!(operand, Operand::Local(local) if *local == env)
            })
        {
            errors.push("隐式类 cell 与它的 Env 不允许非局部覆写".into());
        }
        if let ClassCellEnvSource::GeneratorFrame { frame, slot, .. } = capture.source
            && *runtime == RuntimeFn::ArraySet
            && matches!(args.first(), Some(Operand::Local(local)) if *local == frame)
            && !matches!(args.as_slice(), [_, Operand::Const(Constant::I64(index)), value]
                if *index >= 0 && (*index != slot as i64 || (matches!(value, Operand::Local(local) if *local == env) && environment_available(function, capture, position))))
        {
            errors.push("生成器类 cell Env 槽只能在恢复后保存原引用".into());
        }
    }
}

/// 定义点的 raw cell 只允许进入登记的 Env 或唯一 CellSet；禁止先起别名再偷写。
pub(crate) fn validate_owner_uses(
    function: &Function,
    cell: &ClassCellDefinition,
    errors: &mut Vec<String>,
) {
    for (_, block) in function.body.blocks.iter() {
        for statement in &block.stmts {
            let mut reads = FxHashSet::default();
            let allowed = match statement {
                Statement::Assign { value, .. } | Statement::Eval(value) => {
                    crate::passes::dead_values::read_value(value, &mut reads);
                    match value {
                        Rvalue::MakeFunction { func, env, defaults, instance_context } => {
                            cell.captures.iter().find(|capture| capture.function == *func).is_some_and(|capture| {
                                matches!(env.get(capture.index), Some(Operand::Local(local)) if *local == cell.cell)
                                    && env.iter().enumerate().all(|(index, op)| index == capture.index || !uses_local(op, cell.cell))
                                    && !defaults.iter().chain(instance_context.iter()).any(|op| uses_local(op, cell.cell))
                            })
                        }
                        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::CellSet), args } => {
                            matches!(args.as_slice(), [Operand::Local(local), Operand::Local(original)] if *local == cell.cell && *original == cell.original_class)
                        }
                        _ => false,
                    }
                }
                Statement::SetGlobal { value, .. } => {
                    crate::passes::dead_values::read_operand(value, &mut reads);
                    false
                }
                Statement::SetField { obj, value, .. } => {
                    crate::passes::dead_values::read_operand(obj, &mut reads);
                    crate::passes::dead_values::read_operand(value, &mut reads);
                    false
                }
                Statement::Line(_) => true,
            };
            if reads.contains(&cell.cell) && !allowed {
                errors.push("类定义 raw cell 不得别名逃逸或绕过唯一填充".into());
            }
        }
        let values: Vec<_> = match &block.terminator {
            Terminator::Return(value) => value.iter().collect(),
            Terminator::Raise { value, cause } => value.iter().chain(cause.iter()).collect(),
            Terminator::Branch { cond, .. } => vec![cond],
            Terminator::Suspend { value, .. } => vec![value],
            _ => Vec::new(),
        };
        if values.iter().any(|value| uses_local(value, cell.cell)) {
            errors.push("类定义 raw cell 不得通过终结符逃逸".into());
        }
    }
}
fn uses_local(operand: &Operand, local: LocalId) -> bool {
    let mut reads = FxHashSet::default();
    crate::passes::dead_values::read_operand(operand, &mut reads);
    reads.contains(&local)
}

/// 转发临时同样不能产生任意别名；生成器可以原引用保存到该临时的既有固定帧槽。
pub(crate) fn validate_forwarded_uses(
    function: &Function,
    capture: &ClassCellCapture,
    value: &Rvalue,
    captures: &FxHashMap<FnId, (ClassDefId, &ClassCellDefinition, &ClassCellCapture)>,
    class: ClassDefId,
    errors: &mut Vec<String>,
) {
    let mut reads = FxHashSet::default();
    crate::passes::dead_values::read_value(value, &mut reads);
    for local in reads {
        if !derived_cell(function, capture, local) {
            continue;
        }
        let allowed = match value {
            Rvalue::MakeFunction {
                func,
                env,
                defaults,
                instance_context,
            } => captures.get(func).is_some_and(|(target_class, _, target)| {
                *target_class == class
                    && matches!(env.get(target.index), Some(Operand::Local(cell)) if *cell == local)
                    && env
                        .iter()
                        .enumerate()
                        .all(|(index, op)| index == target.index || !uses_local(op, local))
                    && !defaults
                        .iter()
                        .chain(instance_context.iter())
                        .any(|op| uses_local(op, local))
            }),
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ArraySet),
                args,
            } => {
                matches!(capture.source, ClassCellEnvSource::GeneratorFrame { frame, .. }
                    if matches!(args.as_slice(), [Operand::Local(target), Operand::Const(Constant::I64(slot)), Operand::Local(cell)]
                    if *target == frame && *slot == local.into_raw().into_u32() as i64 + 4 && *cell == local))
            }
            _ => false,
        };
        if !allowed {
            errors.push("转发的类 cell 只能进入同源 Env 或保存到自身生成器帧槽".into());
        }
    }
}

/// 由固定 Env 槽抽取的临时具有唯一用途；全函数登记避免跨块别名绕过即时来源检查。
fn derived_cell(function: &Function, capture: &ClassCellCapture, local: LocalId) -> bool {
    let Some(env) = environment(function, capture) else {
        return false;
    };
    assignments(function, local).iter().any(|(_, value)| {
        matches!(value,
        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArrayGet), args }
        if matches!(args.as_slice(), [Operand::Local(source), Operand::Const(Constant::I64(index))]
            if *source == env && *index == capture.index as i64))
    })
}

/// raw 转发临时不得从非 Rvalue 的语句/终结符出口流出；frame 保存使用上面的固定协议。
pub(crate) fn validate_forwarded_exits(
    function: &Function,
    capture: &ClassCellCapture,
    errors: &mut Vec<String>,
) {
    for (_, block) in function.body.blocks.iter() {
        let mut operands = Vec::new();
        for statement in &block.stmts {
            match statement {
                Statement::SetGlobal { value, .. } => operands.push(value),
                Statement::SetField { obj, value, .. } => operands.extend([obj, value]),
                _ => {}
            }
        }
        match &block.terminator {
            Terminator::Return(value) => operands.extend(value.iter()),
            Terminator::Raise { value, cause } => operands.extend(value.iter().chain(cause.iter())),
            Terminator::Branch { cond, .. } => operands.push(cond),
            Terminator::Suspend { value, .. } => operands.push(value),
            _ => {}
        }
        let mut reads = FxHashSet::default();
        for operand in operands {
            crate::passes::dead_values::read_operand(operand, &mut reads);
        }
        if reads.into_iter().any(|local| {
            environment(function, capture) == Some(local) || derived_cell(function, capture, local)
                || matches!(capture.source, ClassCellEnvSource::GeneratorFrame { frame, .. } if frame == local)
        }) {
            errors.push("转发 raw 类 cell 不得通过公开存储或终结符逃逸".into());
        }
    }
}

/// Env 是不可变的 cell 引用数组；既有生成器只允许把它放进自己的固定槽。
fn allowed_environment_use(capture: &ClassCellCapture, env: LocalId, value: &Rvalue) -> bool {
    match value {
        Rvalue::InstanceZeroCall { callee, frame } => {
            matches!(frame.class, SuperClassSource::Lexical { environment, index }
                if environment == env && index == capture.index)
                && matches!(capture.source, ClassCellEnvSource::Parameter)
                && !uses_local(callee, env)
                && frame.first_parameter != Some(env)
        }
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::CellGetFree),
            args,
        } => {
            matches!(args.as_slice(), [Operand::Local(source), Operand::Const(Constant::I64(_)), Operand::Const(Constant::Str(_))] if *source == env)
        }
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ArrayGet),
            args,
        } => {
            matches!(args.as_slice(), [Operand::Local(source), Operand::Const(Constant::I64(_))] if *source == env)
        }
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ArraySet),
            args,
        } => matches!(capture.source, ClassCellEnvSource::GeneratorFrame { frame, slot, .. }
                if matches!(args.as_slice(), [Operand::Local(target), Operand::Const(Constant::I64(index)), Operand::Local(source)]
                    if *target == frame && *index == slot as i64 && *source == env)),
        Rvalue::MakeGenerator { frame, origin, .. } => {
            matches!(capture.source, ClassCellEnvSource::Parameter)
                && matches!(frame.get(env.into_raw().into_u32() as usize + 4), Some(Operand::Local(source)) if *source == env)
                && frame.iter().enumerate().all(|(slot, value)| {
                    slot == env.into_raw().into_u32() as usize + 4 || !uses_local(value, env)
                })
                && !origin.iter().any(|value| uses_local(value, env))
        }
        _ => false,
    }
}

/// 原始frame只交给实际恢复/暂停协议；禁止先起别名后绕过Env槽写保护。
fn allowed_frame_use(frame: LocalId, value: &Rvalue) -> bool {
    let Rvalue::Call {
        callee: Callee::Runtime(runtime),
        args,
    } = value
    else {
        return false;
    };
    match runtime {
        RuntimeFn::ArrayGet => {
            matches!(args.as_slice(), [Operand::Local(source), Operand::Const(Constant::I64(index))] if *source == frame && *index >= 0)
        }
        RuntimeFn::ArraySet => {
            matches!(args.as_slice(), [Operand::Local(target), Operand::Const(Constant::I64(index)), value] if *target == frame && *index >= 0 && !uses_local(value, frame))
        }
        RuntimeFn::GeneratorInput => {
            matches!(args.as_slice(), [Operand::Local(source)] if *source == frame)
        }
        RuntimeFn::GeneratorDelegateStep => {
            matches!(args.as_slice(), [Operand::Local(source), delegate] if *source == frame && !uses_local(delegate, frame))
        }
        _ => false,
    }
}
