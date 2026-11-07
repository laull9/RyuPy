//! 编译会话的实例语义能力；默认集合保留旧自动试点边界，不依赖模块名称。
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct InstanceCapabilities {
    /// 仅显式普通模块接受运行期相对from-import协议。
    pub relative_from_import: bool,
    /// 仅显式普通模块的非生成器源码Name/bare异常链。
    pub ordinary_name_handlers: bool,
}
