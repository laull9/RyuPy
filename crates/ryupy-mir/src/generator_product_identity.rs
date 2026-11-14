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

/// 高层已核的模块/普通函数/property在lower期间必须逐字段不变。
/// 生成器由独立Source/模板证明负责；这里只保实际旧FnId，不把新增resume当原函数。
pub(super) struct UnchangedHigh {
    functions: Vec<(FnId, FunctionImage)>,
    globals: Vec<GlobalDecl>,
    classes: Vec<ClassDef>,
    exports: Vec<ModuleExport>,
    entry: FnId,
    source_name: String,
}
impl UnchangedHigh {
    pub fn capture(program: &Program, exports: &[ModuleExport], generators: &[FnId]) -> Self {
        let generators: rustc_hash::FxHashSet<_> = generators.iter().copied().collect();
        Self {
            functions: program
                .functions
                .iter()
                .filter(|(id, _)| !generators.contains(id))
                .map(|(id, function)| (id, FunctionImage::capture(function)))
                .collect(),
            globals: program
                .globals
                .iter()
                .map(|(_, value)| value.clone())
                .collect(),
            classes: program
                .classes
                .iter()
                .map(|(_, value)| value.clone())
                .collect(),
            exports: exports.to_vec(),
            entry: program.entry,
            source_name: program.source_name.clone(),
        }
    }
    pub fn verify(&self, program: &Program, exports: &[ModuleExport]) -> Result<(), String> {
        for (id, image) in &self.functions {
            if id.into_raw().into_u32() as usize >= program.functions.len()
                || FunctionImage::capture(&program.functions[*id]) != *image
            {
                return Err("模块初始化封存：lower改变非生成器实际FnId/image".into());
            }
        }
        if self.entry != program.entry
            || self.source_name != program.source_name
            || self.exports != exports
            || self.globals
                != program
                    .globals
                    .iter()
                    .map(|(_, g)| g.clone())
                    .collect::<Vec<_>>()
            || self.classes
                != program
                    .classes
                    .iter()
                    .map(|(_, c)| c.clone())
                    .collect::<Vec<_>>()
        {
            return Err("模块初始化封存：lower改变根身份/GlobalDecl/exports/class".into());
        }
        Ok(())
    }
}
