//! 异常链的源码来源与分派方式；合成with不能冒充新实例源码能力。
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum HandlerSemantics {
    SourceStatic,
    SourceDynamic,
    InstanceNameDynamic,
    SynthesizedWith,
}
impl HandlerSemantics {
    pub fn is_dynamic(self) -> bool {
        matches!(self, Self::SourceDynamic | Self::InstanceNameDynamic)
    }
}
