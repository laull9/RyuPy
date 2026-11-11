//! 显式实例能力的构建边界：不隐式加入模块图，AOT 必须通过图、AST 与部署相对来源合同。
use crate::{
    HasErrors, Session,
    modules::{SourceKind, SourceModule},
};
use anyhow::Result;
use ryupy_base::{Diagnostic, Span, TextRange};

pub(crate) fn validate_selection(
    sess: &mut Session,
    sources: &[SourceModule],
    entry: usize,
    aot: bool,
    has_resources: bool,
    roots: &[std::path::PathBuf],
) -> Result<()> {
    // 选择检查只负责自己的诊断，不能提前消费解析阶段已有的错误。
    if sess.instance_modules.is_empty() {
        return Ok(());
    }
    let mut invalid_selection = false;
    let file = sources[entry].parsed.file;
    if aot && (sess.fixed_binding_build || !has_resources) {
        invalid_selection = true;
        sess.diagnostics.push(
            Diagnostic::error(
                Span::new(file, TextRange::default()),
                "显式实例 AOT 需要资源清单且不支持原生绑定",
            )
            .with_code("E0100"),
        );
    }
    for name in &sess.instance_modules {
        if aot
            && sources
                .iter()
                .any(|m| &m.name == name && m.kind != SourceKind::ModuleFile)
        {
            invalid_selection = true;
            sess.diagnostics.push(
                Diagnostic::error(
                    Span::new(file, TextRange::default()),
                    format!("显式实例 AOT 只允许普通 ModuleFile，不能选择入口或包 `{name}`"),
                )
                .with_code("E0100"),
            );
        }
        if !sources.iter().any(|module| &module.name == name) {
            invalid_selection = true;
            // 缺图仍拒绝；只有合法名字的实际源码证据才检查祖先并细化原因。
            // 不把证据读入图，也不以第二root补齐第一root已经选中的包。
            let missing_parent = (aot && crate::modules::source_evidence(roots, name))
                .then(|| {
                    crate::modules::package_prefixes(name)
                        .into_iter()
                        .find(|prefix| {
                            !matches!(
                                crate::modules::resolve_in(roots, prefix),
                                Some((_, SourceKind::Package))
                            )
                        })
                })
                .flatten();
            let message = if let Some(prefix) = missing_parent {
                format!("显式实例 AOT 模块 `{name}`：包祖先 `{prefix}` 缺少真实 Package 来源")
            } else {
                format!("实例模块 `{name}` 不在实际源码依赖图中；请先导入或显式 --include-module")
            };
            sess.diagnostics.push(
                Diagnostic::error(Span::new(file, TextRange::default()), message)
                    .with_code("E0100"),
            );
        }
    }
    if invalid_selection {
        return Err(HasErrors.into());
    }
    Ok(())
}

/// 只能由本模块在完整前置检查成功后构造；不是手构 IR 的安全边界。
pub(crate) struct ValidatedAot {
    selected: Vec<usize>,
    required: Vec<usize>,
}
impl ValidatedAot {
    pub(crate) fn selects(&self, index: usize) -> bool {
        self.selected.contains(&index)
    }

    pub(crate) fn validate_location(
        &self,
        sess: &mut Session,
        index: usize,
        source: &SourceModule,
        location: &ryupy_mir::ModuleLocation,
    ) -> Result<()> {
        if self.required.contains(&index)
            && !matches!(location, ryupy_mir::ModuleLocation::ExecutableRelative(_))
        {
            reject(sess, source, "最终 UnitMeta 缺少 ExecutableRelative 来源")?;
        }
        Ok(())
    }
}

fn reject(sess: &mut Session, source: &SourceModule, reason: &str) -> Result<()> {
    sess.diagnostics.push(
        Diagnostic::error(
            Span::new(source.parsed.file, TextRange::default()),
            format!("显式实例 AOT 模块 `{}`：{reason}", source.name),
        )
        .with_code("E0100"),
    );
    Err(HasErrors.into())
}

/// 资源计划已解析、但尚未编译任何模块时验证全源码闭包及显式 AST。
pub(crate) fn validate_aot(
    sess: &mut Session,
    sources: &[SourceModule],
    entry: usize,
    aot: bool,
    resources: Option<&crate::resources::ResourcePlan>,
) -> Result<Option<ValidatedAot>> {
    if !aot || sess.instance_modules.is_empty() {
        return Ok(None);
    }
    let mut validated = ValidatedAot {
        selected: Vec::new(),
        required: Vec::new(),
    };
    for (index, source) in sources.iter().enumerate() {
        if index != entry {
            // 图加载允许未解析的名字留给 native；AOT 来源合同不能把 namespace
            // 或普通文件父路径误当已注册包。每个源码祖先必须是真实 Package。
            let mut parent = source.name.as_str();
            while let Some((prefix, _)) = parent.rsplit_once('.') {
                if !sources
                    .iter()
                    .any(|m| m.name == prefix && m.kind == SourceKind::Package)
                {
                    reject(
                        sess,
                        source,
                        &format!("包祖先 `{prefix}` 缺少真实 Package 来源"),
                    )?;
                }
                parent = prefix;
            }
            if resources
                .and_then(|p| p.module_location(index as u32))
                .is_none()
            {
                reject(
                    sess,
                    source,
                    "源码闭包缺少 ExecutableRelative 资源元数据锚点",
                )?;
            }
            validated.required.push(index);
        }
        if sess.instance_modules.contains(&source.name) {
            sess.diagnostics
                .extend(ryupy_hir::validate_instance_aot_direct_globals(
                    &source.parsed,
                    sess.instance_direct_globals,
                ));
            validated.selected.push(index);
        }
    }
    crate::stop_if_errors(sess)?;
    Ok(Some(validated))
}
