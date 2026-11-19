//! 编译会话的实例语义能力；默认集合保留旧自动试点边界，不依赖模块名称。
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub struct InstanceCapabilities {
    /// 固定provider真实模块命名空间：所有编译模块共享实际D与定义/执行环境合同。
    pub provider_namespace: bool,
    /// stock权威字典入口首切：无自定义callable，允许普通类的急切注解。
    pub entry_namespace: bool,
    /// 仅完整来源及AST准入的显式AOT类，在最终发布前检查真实hook。
    pub aot_class_guards: bool,
    /// 仅显式普通实例的零参直接globals冷调用。
    pub direct_globals: bool,
    /// 仅显式普通模块接受运行期相对from-import协议。
    pub relative_from_import: bool,
    /// 仅显式普通模块的非生成器源码Name/bare异常链。
    pub ordinary_name_handlers: bool,
}
