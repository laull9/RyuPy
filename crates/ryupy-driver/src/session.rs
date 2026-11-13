//! 编译会话与来源/诊断状态；显式部署请求不改变默认模块解析。
use crate::{ModuleEnvironmentDecision, ModuleEnvironmentPolicy, TraceEntry};
use anyhow::{Context, Result};
use ryupy_base::{Diagnostics, FileId, SourceDb};
use std::path::{Path, PathBuf};

/// 一次编译会话。诊断累积在这里，由调用方（CLI / 测试）决定如何打印。
#[derive(Default)]
pub struct Session {
    /// 显式NativeAOT优化偏好；Default不设置SDK属性。
    pub aot_optimization: crate::AotOptimization,
    /// 项目请求的私有字节守卫；默认None，不授予模块或发布权限。
    #[doc(hidden)]
    pub deployment_inputs: Option<crate::deployment_request::InputGuard>,
    /// 不透明编排状态；公开字段保留既有Session结构更新语法，内部authority不可外部构造。
    #[doc(hidden)]
    pub default_source_state: crate::DefaultSourceState,
    /// 显式封闭分发请求；不隐含-I或宿主site-packages。
    pub distribution_metadata: Option<PathBuf>,
    /// 同次采集载荷的审计摘要，非运行时authority。
    pub distribution_metadata_report: Option<serde_json::Value>,
    /// 显式有限依赖预检的构建期收据；不进入运行时分发载荷或扩展模块发现。
    pub distribution_dependency_report: Option<serde_json::Value>,
    /// 显式强制有限trace V1；合法域外E0310，内部损坏硬停。
    pub trace_product_v1: bool,
    /// 明确正常Main执行的请求；裸API默认Unknown，不从DLL扩展名推断宿主生命周期。
    pub trace_entry: TraceEntry,
    /// 已在实际依赖图中的源码模块显式启用实例全局，不隐式加入依赖。
    pub instance_modules: Vec<String>,
    /// 仅与显式instance_modules共同启用；不改变自动选择。
    pub instance_direct_globals: bool,
    /// 编译会话的隔离候选策略；无公开CLI开关，也不改变运行时模式数。
    pub module_environment_policy: ModuleEnvironmentPolicy,
    /// 当前实际源码图的编译计划；compiled区分已完成编译与首错后未执行者。
    pub module_environment_report: Vec<ModuleEnvironmentDecision>,
    /// 固定绑定编排期间禁止自动迁移，结束后恢复；显式能力仍按原边界。
    #[doc(hidden)]
    pub fixed_binding_build: bool,
    /// 显式输出普通构建的编译阶段标记；默认关闭，不改变运行时 ABI。
    pub diagnostic_stages: bool,
    /// 本次编译显式包含的源码模块；只加入依赖图，仍在import时初始化。
    pub include_modules: Vec<String>,
    pub sources: SourceDb,
    pub diagnostics: Diagnostics,
    /// 显式启用直接 CIL 累加实验；默认保留已验证的 holder 路径。
    /// 仅控制本次会话的 MIR 流水线，不从进程环境隐式读取，也不改变语言语义。
    pub experimental_direct_accumulator: bool,
    /// 显式实验：仅在实际自动选择的普通动态实例模块启用严格ForEach holder。
    /// 默认关闭，不提供公共CLI模式，也不影响显式实例模块与旧ForRange累加器。
    pub experimental_foreach_accumulator: bool,
    /// 实验：精确Int64实参的受守卫纯叶内联；guard失败性能未达默认门槛。
    pub experimental_leaf_inline: bool,
    /// 仅诊断：SLP保相同守卫/物化，但使用直接标量运算，隔离packed本身的收益。
    pub diagnostic_scalar_slp: bool,
    /// 实验：两lane packed SLP已实现，但相对同守卫标量无净收益，默认关闭。
    pub experimental_packed_slp: bool,
}

impl Session {
    pub fn load_file(&mut self, path: &Path) -> Result<FileId> {
        let text = std::fs::read_to_string(path)
            .with_context(|| format!("无法读取源文件 {}", path.display()))?;
        Ok(self.sources.add(path.display().to_string(), text))
    }

    /// 把全部诊断渲染成文本。
    pub fn render_diagnostics(&self, color: bool) -> String {
        self.diagnostics
            .iter()
            .map(|d| ryupy_base::diagnostic::render(&self.sources, d, color))
            .collect()
    }
}
