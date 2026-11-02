//! 模块运行位置合同；不承载诊断路径，也不执行文件系统操作。
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub enum ModuleLocation {
    /// 旧dump与未选资源的AOT保持诊断basename。
    #[default]
    LegacyDiagnostic,
    /// JIT实际读取源码文件的绝对位置，不进入诊断或manifest。
    SourceAbsolute(String),
    /// 真AOT已选择模块的可执行文件相对虚拟源码锚点。
    ExecutableRelative(String),
}
