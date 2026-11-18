//! 显式provider模块字典profile的源码图准入；正文与函数继续CIL，不按模块名字注册或回退。
use crate::{HasErrors, Session, modules::SourceModule};
use ryupy_base::{Diagnostic, Span, TextRange};

pub(crate) fn validate(sess: &mut Session, sources: &[SourceModule]) -> anyhow::Result<()> {
    if !sess.experimental_stock_module_namespaces {
        return Ok(());
    }
    let valid = !sess.experimental_stock_thin_functions
        && !sess.fixed_binding_build
        && !sess.cpython_entry
        && sess.cpython_modules.is_empty()
        && sess.instance_modules.is_empty()
        && !sess.instance_direct_globals
        && sess
            .cpython_provider_state
            .plan
            .as_ref()
            .is_some_and(|provider| {
                provider.delegation.entry_script.is_none()
                    && provider.delegation.local_modules.is_empty()
                    && matches!(
                        (
                            provider.version.major,
                            provider.version.minor,
                            provider.version.micro
                        ),
                        (3, 12, 13) | (3, 13, 3)
                    )
            });
    if !valid {
        sess.diagnostics.push(Diagnostic::error(Span::new(sources[0].parsed.file, TextRange::default()),
            "provider模块字典profile要求冻结stock CPython 3.12.13/3.13.3、完整CIL源码图，禁止薄函数profile、fixed binding、本地业务委派或旧显式Instance混选").with_code("E0100"));
        return Err(HasErrors.into());
    }
    // JIT同样先检查完整图，不能在某模块已执行后更换存储或延期已选语义失败。
    for source in sources {
        sess.diagnostics
            .extend(ryupy_hir::validate_provider_namespace_aot(&source.parsed));
    }
    crate::stop_if_errors(sess)
}
