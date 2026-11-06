//! 显式实例能力的构建边界：不隐式加入模块图，B1只支持JIT来源。
use crate::{HasErrors, Session, modules::SourceModule};
use anyhow::Result;
use ryupy_base::{Diagnostic, Span, TextRange};

pub(crate) fn validate_selection(
    sess: &mut Session,
    sources: &[SourceModule],
    entry: usize,
    aot: bool,
) -> Result<()> {
    // 选择检查只负责自己的诊断，不能提前消费解析阶段已有的错误。
    if sess.instance_modules.is_empty() {
        return Ok(());
    }
    let mut invalid_selection = false;
    let file = sources[entry].parsed.file;
    if aot {
        invalid_selection = true;
        sess.diagnostics.push(
            Diagnostic::error(
                Span::new(file, TextRange::default()),
                "实例模块首切仅支持JIT，不能与AOT、AOT资源或AOT原生绑定组合",
            )
            .with_code("E0100"),
        );
    }
    for name in &sess.instance_modules {
        if !sources.iter().any(|module| &module.name == name) {
            invalid_selection = true;
            sess.diagnostics.push(
                Diagnostic::error(
                    Span::new(file, TextRange::default()),
                    format!(
                        "实例模块 `{name}` 不在实际源码依赖图中；请先导入或显式 --include-module"
                    ),
                )
                .with_code("E0100"),
            );
        }
    }
    if invalid_selection {
        return Err(HasErrors.into());
    }
    Ok(())
}
