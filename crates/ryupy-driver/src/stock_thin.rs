//! 薄函数候选的实际图准入；provider必须已冻结，禁止入口委派和额外源码。
use crate::{
    HasErrors, Session,
    modules::{SourceKind, SourceModule},
};
use ryupy_base::{Diagnostic, Span, TextRange};
pub(crate) fn validate(sess: &mut Session, sources: &[SourceModule]) -> anyhow::Result<()> {
    if !sess.experimental_stock_thin_functions {
        return Ok(());
    }
    let valid = !sess.fixed_binding_build
        && !sess.cpython_entry
        && sess
            .cpython_provider_state
            .plan
            .as_ref()
            .is_some_and(|p| p.delegation.entry_script.is_none())
        && sources.len() == 1
        && sources[0].name == "__main__"
        && sources[0].kind == SourceKind::Entry
        && ryupy_hir::has_defined_functions(&sources[0].parsed);
    if !valid {
        sess.diagnostics.push(Diagnostic::error(Span::new(sources[0].parsed.file, TextRange::default()),
            "薄函数候选要求已冻结stock provider、未委派的单源码__main__且至少一个薄函数定义，禁止fixed binding和额外源码模块").with_code("E0100"));
        return Err(HasErrors.into());
    }
    Ok(())
}
