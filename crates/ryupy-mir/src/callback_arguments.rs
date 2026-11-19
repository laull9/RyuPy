//! 跨域回调形参的冷 ABI 资格：只有证实为 object 槽的固定位置参数才允许按读物化。
use crate::{Function, FunctionCore, InstanceStateSource, MirType, ParamKind};

impl Function {
    /// 后端对这些真实 Python 槽的每次读取发射解包。隐藏 owner、环境与恢复参数从不接受借值。
    pub fn uses_deferred_callback_arguments(&self) -> bool {
        self.instance_capabilities.provider_namespace
            && self.module_state_source == Some(InstanceStateSource::FunctionParameter)
            && self.core == FunctionCore::Ordinary
            && self.dynamic_origin.is_some()
            && !self.param_meta.is_empty()
            && self
                .params
                .get(..self.param_meta.len())
                .is_some_and(|slots| {
                    self.param_meta.iter().zip(slots).all(|(meta, slot)| {
                        matches!(
                            meta.kind,
                            ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword
                        ) && (slot.into_raw().into_u32() as usize) < self.body.locals.len()
                            && self.body.locals[*slot].ty == MirType::Dynamic
                    })
                })
    }
}
