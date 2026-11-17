//! MIR 自有的函数定义来源；只携带纯数据，不把 HIR 或 AST 暴露给后端。
use rustc_hash::FxHashSet;

#[derive(Debug, Clone, PartialEq, Eq)]
pub struct DefinitionInfo {
    pub lexical_qualname: String,
    pub firstlineno: u32,
    /// 显式profile才携带；每个实际canonical def在构造后冷登记。
    pub introspection_kind: Option<PythonFunctionKind>,
}

/// HIR真实种类经过MIR保持，后端不按方法符号/委托/返回载体猜测。
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
#[repr(i64)]
pub enum PythonFunctionKind {
    OrdinaryModule = 0,
    Generator = 1,
    Coroutine = 2,
    AsyncGenerator = 3,
    OrdinaryNonModule = 4,
}

/// 一个共享签名只对应一个定义；内部恢复函数不允许借用用户函数来源。
pub(crate) fn validate(program: &crate::Program, errors: &mut Vec<String>) {
    let mut signatures = FxHashSet::default();
    for (_, function) in program.functions.iter() {
        if let Some(signature) = function.signature_global
            && !signatures.insert(signature)
        {
            errors.push(format!("函数 `{}` 的定义签名被重复使用", function.name));
        }
        if let Some(definition) = &function.definition {
            if function.signature_global.is_none()
                && function.module_state_source != Some(crate::InstanceStateSource::StockThinLease)
                && !matches!(function.core, crate::FunctionCore::GenericCompanion { .. })
            {
                errors.push(format!("函数 `{}` 的定义来源缺少共享签名", function.name));
            }
            if definition.firstlineno == 0 {
                errors.push(format!("函数 `{}` 的定义来源不是有效源行号", function.name));
            }
        }
    }
}
