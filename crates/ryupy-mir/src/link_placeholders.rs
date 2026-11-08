//! 链接器分配身份时的占位对象；随后由原链接流程完整覆盖，不参与资格判定。
use crate::{Body, ClassDef, Function, MirType};
use la_arena::Arena;

impl ClassDef {
    /// link 阶段先占位，随后整体覆盖。
    pub(super) fn placeholder() -> Self {
        Self {
            class_cell: None,
            name: String::new(),
            codegen_name: String::new(),
            fields: Vec::new(),
            is_value_type: false,
            is_dataclass: false,
            base: None,
            builtin_base: None,
            is_exception: false,
            factory_global: None,
        }
    }
}

impl Function {
    /// 空程序兜底用；正常流程不会使用。
    pub(super) fn placeholder() -> Self {
        Self {
            module_state: None,
            instance_capabilities: Default::default(),
            module_state_source: None,
            name: String::new(),
            codegen_name: String::new(),
            source_file: String::new(),
            definition: None,
            params: Vec::new(),
            ret: MirType::Unit,
            body: Body {
                source_handler_contracts: Vec::new(),
                locals: Arena::default(),
                blocks: Arena::default(),
                entry: la_arena::Idx::from_raw(0.into()),
                regions: Vec::new(),
            },
            param_meta: Vec::new(),
            function_global: None,
            specialization_identity_global: None,
            defaults_global: None,
            signature_global: None,
            env: None,
            dynamic_origin: None,
        }
    }
}
