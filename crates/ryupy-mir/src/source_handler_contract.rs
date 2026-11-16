//! 实例源码异常链与实际EH的对应合同；不把合成with或finally误判为新能力。
use crate::*;
#[derive(Debug, Clone)]
pub struct SourceHandlerContract {
    pub try_start: BlockId,
}
pub(crate) fn validate_functions(functions: &la_arena::Arena<Function>, errors: &mut Vec<String>) {
    for (_, function) in functions.iter() {
        let contracts = &function.body.source_handler_contracts;
        if !contracts.is_empty()
            && (function.module_state.is_none()
                || !function.instance_capabilities.ordinary_name_handlers
                || !matches!(
                    function.module_state_source,
                    Some(
                        InstanceStateSource::ModuleEntryParameter
                            | InstanceStateSource::FunctionParameter
                    )
                ))
        {
            errors.push(format!(
                "函数 `{}` 的源码异常链缺少普通实例能力或实际scope非法",
                function.name
            ));
        }
        for (id, contract) in contracts.iter().enumerate() {
            let regions: Vec<_> = function
                .body
                .regions
                .iter()
                .filter(|region| region.source_handler_chain == Some(id))
                .collect();
            if regions.len() != 1
                || regions.first().is_none_or(|region| {
                    !matches!(region.kind, RegionKind::CatchAll)
                        || region.try_start != contract.try_start
                })
            {
                errors.push(format!(
                    "函数 `{}` 的源码异常链{}与实际CatchAll不一致",
                    function.name, id
                ));
            }
        }
        for region in &function.body.regions {
            if region
                .source_handler_chain
                .is_some_and(|id| id >= contracts.len())
            {
                errors.push(format!("函数 `{}` 的源码异常链ID越界", function.name));
            }
        }
    }
}
