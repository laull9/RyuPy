//! 编译产品冷启动所需的有限原生模块；默认完整，只有driver显式冻结才裁去inspect。
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct NativeModuleRequirements {
    pub inspect: bool,
}
impl Default for NativeModuleRequirements {
    fn default() -> Self {
        Self { inspect: true }
    }
}
