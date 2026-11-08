//! 函数真实 ABI 的逐字段快照；定义、隐藏参数与私有槽都参与比较，不用 Debug 文本充当证据。
use crate::*;

#[derive(Clone, Debug, PartialEq)]
pub(super) struct FunctionIdentity {
    pub module_state: Option<LocalId>,
    pub module_state_source: Option<InstanceStateSource>,
    pub capabilities: ryupy_base::InstanceCapabilities,
    pub definition: Option<DefinitionInfo>,
    pub name: String,
    pub codegen_name: String,
    pub source_file: String,
    pub params: Vec<LocalId>,
    pub ret: MirType,
    pub metadata: Vec<ParamMeta>,
    pub function_global: Option<GlobalId>,
    pub specialization_global: Option<GlobalId>,
    pub defaults_global: Option<GlobalId>,
    pub signature_global: Option<GlobalId>,
    pub environment: Option<LocalId>,
    pub origin: Option<DynamicFunctionOrigin>,
}
impl FunctionIdentity {
    pub fn capture(function: &Function) -> Self {
        Self {
            module_state: function.module_state,
            module_state_source: function.module_state_source,
            capabilities: function.instance_capabilities,
            definition: function.definition.clone(),
            name: function.name.clone(),
            codegen_name: function.codegen_name.clone(),
            source_file: function.source_file.clone(),
            params: function.params.clone(),
            ret: function.ret.clone(),
            metadata: function.param_meta.clone(),
            function_global: function.function_global,
            specialization_global: function.specialization_identity_global,
            defaults_global: function.defaults_global,
            signature_global: function.signature_global,
            environment: function.env,
            origin: function.dynamic_origin,
        }
    }
}

#[derive(Clone, Debug, PartialEq)]
pub(super) struct FunctionImage {
    pub identity: FunctionIdentity,
    pub locals: Vec<LocalDecl>,
    pub blocks: Vec<BasicBlock>,
    pub entry: BlockId,
    pub regions: Vec<Region>,
    pub source_handlers: Vec<BlockId>,
}
impl FunctionImage {
    pub fn capture(function: &Function) -> Self {
        Self {
            identity: FunctionIdentity::capture(function),
            locals: function
                .body
                .locals
                .iter()
                .map(|(_, value)| value.clone())
                .collect(),
            blocks: function
                .body
                .blocks
                .iter()
                .map(|(_, value)| value.clone())
                .collect(),
            entry: function.body.entry,
            regions: function.body.regions.clone(),
            source_handlers: function
                .body
                .source_handler_contracts
                .iter()
                .map(|value| value.try_start)
                .collect(),
        }
    }
}
