//! 显式inspect profile的Instance AOT后置证明：注解可保留，Python ABI仍必须完全通用。
//! 该门不修改函数模式；检查优化后的真实MIR、受信context的每个使用和既有完整验证。
use crate::*;
use rustc_hash::FxHashSet;

pub fn validate(program: &Program) -> Result<(), Vec<String>> {
    let mut errors = Vec::new();
    if let Err(existing) = crate::validate::validate(program) {
        errors.extend(existing);
    }
    for (id, function) in program.functions.iter() {
        if id != program.entry
            && function.signature_global.is_some()
            && (function.core != FunctionCore::Ordinary
                || function.ret != MirType::Dynamic
                || function.module_state.is_none()
                || function.dynamic_origin.is_none()
                || function.param_meta.iter().any(|p| p.ty != MirType::Dynamic)
                || function
                    .params
                    .iter()
                    .any(|id| function.body.locals[*id].ty != MirType::Dynamic))
        {
            errors.push(format!(
                "inspect Instance函数 `{}` 必须保持实际Dynamic公开ABI",
                function.name
            ));
        }
        let mut contexts = FxHashSet::default();
        if let Some(state) = function.module_state {
            contexts.insert(state);
        }
        for (_, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                if let Statement::Assign {
                    dest,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::InstanceCaptureDefinitionContext),
                            ..
                        },
                } = statement
                {
                    contexts.insert(*dest);
                }
            }
        }
        for (_, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                let (values, allowed) = match statement {
                    Statement::Assign { value, .. } | Statement::Eval(value) => {
                        if matches!(
                            value,
                            Rvalue::Unbox { .. }
                                | Rvalue::UnboxExactI64 { .. }
                                | Rvalue::UnboxExactF64 { .. }
                        ) {
                            errors.push(format!(
                                "inspect Instance函数 `{}` 不得拆箱Python值",
                                function.name
                            ));
                        }
                        if let Rvalue::Call {
                            callee: Callee::Fn(target),
                            ..
                        } = value
                        {
                            let target = &program.functions[*target];
                            if target.ret != MirType::Dynamic
                                || target.param_meta.iter().any(|p| p.ty != MirType::Dynamic)
                            {
                                errors.push(format!(
                                    "inspect Instance函数 `{}` 不得直调typed核心",
                                    function.name
                                ));
                            }
                        }
                        (
                            crate::source_provenance::operands(value),
                            allowed_context(value, &contexts),
                        )
                    }
                    Statement::SetGlobal { value, .. } => (vec![value], None),
                    Statement::SetField { obj, value, .. } => (vec![obj, value], None),
                    Statement::Line(_) => continue,
                };
                check_contexts(values, allowed, &contexts, &function.name, &mut errors);
            }
            let values = match &block.terminator {
                Terminator::Return(value) => value.iter().collect(),
                Terminator::Raise { value, cause } => value.iter().chain(cause).collect(),
                Terminator::Branch { cond, .. } => vec![cond],
                Terminator::Suspend { value, .. } => vec![value],
                _ => vec![],
            };
            check_contexts(values, None, &contexts, &function.name, &mut errors);
        }
    }
    if errors.is_empty() {
        Ok(())
    } else {
        Err(errors)
    }
}

// 只允许一个直接context输入；默认值/env/普通Runtime参数或数组中的同一local均不得外泄。
fn allowed_context<'a>(value: &'a Rvalue, contexts: &FxHashSet<LocalId>) -> Option<&'a Operand> {
    let allowed = match value {
        Rvalue::Call {
            callee:
                Callee::Runtime(
                    RuntimeFn::InstanceLoadName
                    | RuntimeFn::InstanceStoreName
                    | RuntimeFn::InstanceDeleteName
                    | RuntimeFn::InstanceExecutionSource
                    | RuntimeFn::InstanceCaptureDefinitionContext
                    | RuntimeFn::InstanceDefinitionModule
                    | RuntimeFn::InstanceClassGuard
                    | RuntimeFn::InstanceClassCellGuard
                    | RuntimeFn::InstanceMakeClass
                    | RuntimeFn::InstanceImportGuard
                    | RuntimeFn::InstanceImportName
                    | RuntimeFn::InstanceImportAttribute
                    | RuntimeFn::InstanceRelativeImport
                    | RuntimeFn::InstanceRelativeImportAttr
                    | RuntimeFn::InstanceDirectGlobals,
                ),
            args,
        } => args.first(),
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ModuleExecutionSourceWithCode),
            args,
        } => args.get(1),
        Rvalue::MakeFunction {
            instance_context, ..
        } => instance_context.as_ref(),
        _ => None,
    };
    allowed.filter(|value| matches!(value, Operand::Local(id) if contexts.contains(id)))
}
fn check_contexts<'a>(
    mut values: Vec<&'a Operand>,
    allowed: Option<&'a Operand>,
    contexts: &FxHashSet<LocalId>,
    name: &str,
    errors: &mut Vec<String>,
) {
    let mut consumed = false;
    while let Some(value) = values.pop() {
        match value {
            Operand::Array(items) => values.extend(items),
            Operand::Local(id) if contexts.contains(id) => {
                if !consumed && allowed.is_some_and(|candidate| std::ptr::eq(candidate, value)) {
                    consumed = true;
                } else {
                    errors.push(format!(
                        "inspect Instance函数 `{name}` 的私有源context不得逃逸"
                    ));
                }
            }
            _ => {}
        }
    }
}
