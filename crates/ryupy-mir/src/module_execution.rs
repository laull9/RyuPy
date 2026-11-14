//! 模块执行能力与公开全局存储分类，后端不得推断。
/// 显式构建能力；未选模块严格保留旧ABI。
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq)]
pub enum ModuleExecutionKind {
    #[default]
    LegacySingleton,
    InstanceGlobals,
}

/// 实例环境的可信来源；生成器恢复只能从指定帧槽装载。
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum InstanceStateSource {
    ModuleEntryParameter,
    FunctionParameter,
    GeneratorFrame { frame: crate::LocalId, slot: usize },
}

impl InstanceStateSource {
    pub(crate) fn parameter(is_module: bool) -> Self {
        if is_module {
            Self::ModuleEntryParameter
        } else {
            Self::FunctionParameter
        }
    }
}

/// 裸GlobalId的存储合同；实例公开名称不允许后端生成静态字段。
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub enum GlobalStorage {
    #[default]
    LegacyStatic,
    InstancePublic {
        name: String,
    },
    /// 每真实编译单元一个私有冷来源；函数所属关系只在此登记，link必须重映射内部ID。
    CodeSource {
        entry: crate::FnId,
        functions: Vec<crate::FnId>,
        legacy_entry: Option<LegacySourceEntry>,
    },
}

/// Legacy入口值来自实际HIR身份前言，不在未初始化getter失败后猜初始名字。
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct LegacySourceEntry {
    pub initial_name: String,
    pub name_global: crate::GlobalId,
}
