//! 薄体lease的封闭使用合同：仅LoadGlobal借用；值运算/调用只消费中性业务对象。
use crate::*;
pub(crate) fn validate(program: &Program, errors: &mut Vec<String>) {
    for (_, function) in program.functions.iter() {
        if (function.core == FunctionCore::StockThinStatic)
            != (function.module_state_source == Some(InstanceStateSource::StockThinLease))
        {
            errors.push("薄函数执行种类和lease来源必须一同冻结".into());
        }
        if function.module_state_source != Some(InstanceStateSource::StockThinLease) {
            continue;
        }
        let Some(lease) = function.module_state else {
            continue;
        };
        if function.definition.is_none()
            || function.function_global.is_some()
            || function.defaults_global.is_some()
            || function.specialization_identity_global.is_some()
        {
            errors.push("薄函数必须保留冷定义且禁止旧函数对象静态槽".into());
        }
        for (_, block) in function.body.blocks.iter() {
            for stmt in &block.stmts {
                match stmt {
                    Statement::Assign { value, .. } | Statement::Eval(value) => {
                        if matches!(stmt, Statement::Assign { dest, .. } if *dest == lease) {
                            errors.push("薄函数lease禁止覆盖".into());
                        }
                        if let Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::StockThinLoadGlobal),
                            args,
                        } = value
                        {
                            if !matches!(args.as_slice(), [Operand::Local(state), Operand::Const(Constant::Str(_))] if *state == lease)
                            {
                                errors.push("薄函数全局读取必须借用原lease和冻结名称".into());
                            }
                            continue;
                        }
                        if !neutral_value(value) {
                            errors.push(format!(
                                "薄函数体禁止旧Context/helper及未准入值操作：{value:?}"
                            ));
                        }
                        let mut reads = rustc_hash::FxHashSet::default();
                        crate::passes::dead_values::read_value(value, &mut reads);
                        if reads.contains(&lease) {
                            errors.push("薄函数lease禁止别名或逃逸".into());
                        }
                    }
                    Statement::Line(_) => {}
                    _ => errors.push("薄函数体禁止全局或字段写入".into()),
                }
            }
            let operand = match &block.terminator {
                Terminator::Return(value) => value.as_ref(),
                Terminator::Branch { cond, .. } => Some(cond),
                Terminator::Goto(_) | Terminator::Unreachable => None,
                _ => {
                    errors.push("薄函数体禁止未准入控制转移".into());
                    None
                }
            };
            let mut reads = rustc_hash::FxHashSet::default();
            if let Some(operand) = operand {
                crate::passes::dead_values::read_operand(operand, &mut reads);
            }
            if reads.contains(&lease) {
                errors.push("薄函数lease禁止进入控制条件或返回值".into());
            }
        }
    }
}

/// 这些既有操作只处理业务值，不能取得Context或活动lease的所有权。
fn neutral_value(value: &Rvalue) -> bool {
    match value {
        Rvalue::Use(_) | Rvalue::Box { .. } | Rvalue::DynTruthy { .. } => true,
        Rvalue::DynBinary {
            op, inplace: false, ..
        } => matches!(
            op,
            DynOp::Add
                | DynOp::Sub
                | DynOp::Mul
                | DynOp::TrueDiv
                | DynOp::FloorDiv
                | DynOp::Mod
                | DynOp::Pow
                | DynOp::BitAnd
                | DynOp::BitOr
                | DynOp::BitXor
                | DynOp::LShift
                | DynOp::RShift
        ),
        Rvalue::DynUnary { op, .. } => {
            matches!(op, DynOp::Neg | DynOp::Pos | DynOp::Not | DynOp::Invert)
        }
        Rvalue::DynCallValue { args, .. } => args.len() <= 3,
        // 既有零参helper只接显式的首参槽和Missing类源，不取得Context、闭包或lease。
        Rvalue::InstanceZeroCall { frame, .. } => frame.class == SuperClassSource::Missing,
        Rvalue::Unary {
            op: UnaryOp::Not, ..
        } => true,
        Rvalue::Call { callee, args } => match callee {
            Callee::Runtime(RuntimeFn::DynNone | RuntimeFn::MakeUnbound) => args.is_empty(),
            Callee::Runtime(RuntimeFn::IsSame) => args.len() == 2,
            Callee::Runtime(RuntimeFn::DynFromDecimal) => {
                matches!(args.as_slice(), [Operand::Const(Constant::Str(_))])
            }
            Callee::Runtime(RuntimeFn::CallValue) => matches!(args.as_slice(),
                [_, Operand::Array(_), Operand::Array(names), Operand::Array(values)] if names.is_empty() && values.is_empty()),
            // 通用条件保持Python真值协议；身份比较装箱的bool也复用此入口。
            Callee::Runtime(RuntimeFn::DynTruthy) => args.len() == 1,
            Callee::Runtime(RuntimeFn::LoadChecked) => {
                matches!(args.as_slice(), [_, Operand::Const(Constant::Str(_))])
            }
            _ => false,
        },
        _ => false,
    }
}

/// 只接真实模块入口的Context；目标角色显式为薄lease体，link前后均核验。
pub(crate) fn validate_factory(
    program: &Program,
    caller: &Function,
    block: BlockId,
    value: &Rvalue,
    errors: &mut Vec<String>,
) {
    let Rvalue::MakeStockThinFunction {
        func: target,
        module_context: context,
    } = value
    else {
        return;
    };
    crate::validate_instance_module::validate_operand(program, caller, block, context, errors);
    if !program.functions.iter().any(|(id, f)| {
        id == *target && f.module_state_source == Some(InstanceStateSource::StockThinLease)
    }) || caller.module_state_source != Some(InstanceStateSource::ModuleEntryParameter)
        || !matches!(context, Operand::Local(state) if Some(*state) == caller.module_state)
    {
        errors.push("薄函数工厂必须从真实模块入口传Context到静态lease体".into());
    }
}
