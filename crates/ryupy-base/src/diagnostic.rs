//! 统一诊断。
//!
//! 各阶段只负责「产出」[`Diagnostic`]，不负责打印；打印由 driver 统一调用 [`render`]。
//! 这样测试里可以直接对诊断文本做快照。

use std::fmt::Write as _;

use ariadne::{Config, IndexType, Report, ReportKind};

use crate::source::{SourceDb, Span};

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Severity {
    Error,
    Warning,
}

#[derive(Clone, Debug)]
pub struct Label {
    pub span: Span,
    pub message: String,
}

/// 仅由产生点标注的可延期失败；错误码/消息不构成软化依据。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum DeferredFailure {
    ParseRejected,
    SupportBoundary,
}

#[derive(Clone, Debug)]
pub struct Diagnostic {
    pub deferred_failure: Option<DeferredFailure>,
    pub severity: Severity,
    /// 稳定的错误码，例如 `E0001`。后续写进 docs/errors.md 便于检索。
    pub code: Option<&'static str>,
    pub message: String,
    pub primary: Label,
    pub secondary: Vec<Label>,
    pub notes: Vec<String>,
}

impl Diagnostic {
    pub fn error(span: Span, message: impl Into<String>) -> Self {
        let message = message.into();
        Self {
            severity: Severity::Error,
            deferred_failure: None,
            code: None,
            primary: Label {
                span,
                message: String::new(),
            },
            message,
            secondary: Vec::new(),
            notes: Vec::new(),
        }
    }

    pub fn with_deferred_failure(mut self, kind: DeferredFailure) -> Self {
        self.deferred_failure = Some(kind);
        self
    }

    pub fn with_code(mut self, code: &'static str) -> Self {
        self.code = Some(code);
        self
    }

    pub fn with_primary_label(mut self, message: impl Into<String>) -> Self {
        self.primary.message = message.into();
        self
    }

    pub fn with_label(mut self, span: Span, message: impl Into<String>) -> Self {
        self.secondary.push(Label {
            span,
            message: message.into(),
        });
        self
    }

    pub fn with_note(mut self, note: impl Into<String>) -> Self {
        self.notes.push(note.into());
        self
    }

    pub fn is_error(&self) -> bool {
        self.severity == Severity::Error
    }
}

/// 诊断收集器：各阶段 push，driver 在阶段边界检查 `has_errors()` 决定是否继续。
#[derive(Debug, Default)]
pub struct Diagnostics {
    items: Vec<Diagnostic>,
}

impl Diagnostics {
    pub fn push(&mut self, d: Diagnostic) {
        self.items.push(d);
    }

    pub fn extend(&mut self, ds: impl IntoIterator<Item = Diagnostic>) {
        self.items.extend(ds);
    }

    pub fn has_errors(&self) -> bool {
        self.items.iter().any(Diagnostic::is_error)
    }

    pub fn iter(&self) -> impl Iterator<Item = &Diagnostic> {
        self.items.iter()
    }

    pub fn is_empty(&self) -> bool {
        self.items.is_empty()
    }
}

/// ariadne 需要的 span 形式：(文件名, 字节区间)。
type AriadneSpan = (String, std::ops::Range<usize>);

fn to_ariadne(db: &SourceDb, span: Span) -> AriadneSpan {
    let name = db.get(span.file).name.clone();
    (
        name,
        span.range.start().to_usize()..span.range.end().to_usize(),
    )
}

/// 把诊断渲染成文本。`color = false` 用于快照测试。
pub fn render(db: &SourceDb, diag: &Diagnostic, color: bool) -> String {
    let kind = match diag.severity {
        Severity::Error => ReportKind::Error,
        Severity::Warning => ReportKind::Warning,
    };
    let config = Config::default()
        .with_color(color)
        .with_index_type(IndexType::Byte);

    let mut builder = Report::build(kind, to_ariadne(db, diag.primary.span))
        .with_config(config)
        .with_message(&diag.message);
    if let Some(code) = diag.code {
        builder = builder.with_code(code);
    }
    builder = builder.with_label(
        ariadne::Label::new(to_ariadne(db, diag.primary.span)).with_message(&diag.primary.message),
    );
    for label in &diag.secondary {
        builder = builder.with_label(
            ariadne::Label::new(to_ariadne(db, label.span)).with_message(&label.message),
        );
    }
    for note in &diag.notes {
        builder = builder.with_note(note);
    }

    let sources = ariadne::sources(db.iter().map(|(_, f)| (f.name.clone(), f.text.clone())));
    let mut out = Vec::new();
    if builder.finish().write(sources, &mut out).is_err() {
        // 渲染失败时退化为纯文本，绝不吞掉诊断。
        let mut s = String::new();
        let _ = write!(s, "error: {}", diag.message);
        return s;
    }
    String::from_utf8_lossy(&out).into_owned()
}
