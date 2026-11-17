//! 薄体lease的封闭使用合同：仅LoadGlobal可借用，不准传入旧Context helper或逃逸。
use crate::*;
pub(crate) fn validate(program: &Program, errors: &mut Vec<String>) {
    for (_, function) in program.functions.iter() {
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
                        if let Rvalue::Call { callee, args } = value {
                            if *callee != Callee::Runtime(RuntimeFn::StockThinLoadGlobal)
                                || !matches!(args.as_slice(), [Operand::Local(state), Operand::Const(Constant::Str(_))] if *state == lease)
                            {
                                errors.push(
                                    "薄函数体只能以真实lease读取冻结名称，禁止旧Context/helper"
                                        .into(),
                                );
                            }
                        } else {
                            let mut reads = rustc_hash::FxHashSet::default();
                            crate::passes::dead_values::read_value(value, &mut reads);
                            if reads.contains(&lease) {
                                errors.push("薄函数lease禁止别名或逃逸".into());
                            }
                        }
                    }
                    Statement::Line(_) => {}
                    _ => errors.push("薄函数体禁止全局或字段写入".into()),
                }
            }
            if matches!(&block.terminator, Terminator::Return(Some(Operand::Local(local))) if *local == lease)
            {
                errors.push("薄函数禁止返回lease".into());
            }
        }
    }
}

/// 只接真实模块入口的Context；目标角色显式为薄lease体，link前后均核验。
pub(crate) fn validate_factory(
    program: &Program,
    caller: &Function,
    block: BlockId,
    target: FnId,
    context: &Operand,
    errors: &mut Vec<String>,
) {
    crate::validate_instance_module::validate_operand(program, caller, block, context, errors);
    if !program.functions.iter().any(|(id, f)| {
        id == target && f.module_state_source == Some(InstanceStateSource::StockThinLease)
    }) || caller.module_state_source != Some(InstanceStateSource::ModuleEntryParameter)
        || !matches!(context, Operand::Local(state) if Some(*state) == caller.module_state)
    {
        errors.push("薄函数工厂必须从真实模块入口传Context到静态lease体".into());
    }
}
