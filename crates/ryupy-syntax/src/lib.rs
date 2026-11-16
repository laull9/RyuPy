//! Python 语法前端：对 `ruff_python_parser` 的薄封装。
//!
//! 设计约束：
//! - 整个工作区里**只有本 crate 直接依赖 ruff 解析器**。下游通过 [`ast`] 重导出访问 AST，
//!   这样升级 ruff 版本时影响面可控（见 ADR-0001）。
//! - 解析总是「容错」的：即使有语法错误也返回 AST + 诊断，由 driver 决定是否继续。

use ryupy_base::{Diagnostic, FileId, Span};

/// 下游使用的 AST 类型，统一从这里拿。
pub use ruff_python_ast as ast;

/// 一个解析完成的模块。
#[derive(Debug)]
pub struct ParsedModule {
    pub file: FileId,
    pub module: ast::ModModule,
}

/// 解析一个 Python 源文件。
///
/// 返回值总是包含 AST；`diagnostics` 非空时 AST 中可能含有错误恢复节点。
pub fn parse(file: FileId, source: &str) -> (ParsedModule, Vec<Diagnostic>) {
    let parsed = ruff_python_parser::parse_unchecked_source(source, ast::PySourceType::Python);

    let mut diagnostics: Vec<Diagnostic> = parsed
        .errors()
        .iter()
        .map(|e| {
            Diagnostic::error(Span::new(file, e.location), e.error.to_string())
                .with_code("E0001")
                .with_deferred_failure(ryupy_base::diagnostic::DeferredFailure::ParseRejected)
                .with_primary_label("语法错误")
        })
        .collect();
    diagnostics.extend(parsed.unsupported_syntax_errors().iter().map(|e| {
        Diagnostic::error(Span::new(file, e.range), e.to_string())
            .with_code("E0002")
            .with_deferred_failure(ryupy_base::diagnostic::DeferredFailure::ParseRejected)
            .with_primary_label("目标 Python 版本不支持该语法")
    }));

    let module = parsed.into_syntax();
    (ParsedModule { file, module }, diagnostics)
}

#[cfg(test)]
mod tests {
    use super::*;
    use ryupy_base::SourceDb;

    #[test]
    fn parses_simple_function() {
        let mut db = SourceDb::default();
        let src = "def add(a: int, b: int) -> int:\n    return a + b\n";
        let file = db.add("t.py", src);
        let (parsed, diags) = parse(file, src);
        assert!(diags.is_empty());
        assert_eq!(parsed.module.body.len(), 1);
    }

    #[test]
    fn reports_syntax_error() {
        let mut db = SourceDb::default();
        let src = "def f(:\n    pass\n";
        let file = db.add("t.py", src);
        let (_, diags) = parse(file, src);
        assert!(!diags.is_empty());
    }
}
