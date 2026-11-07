//! RyuPy 的公共底座。
//!
//! 只放「每一层都要用、且与具体 IR 无关」的东西：
//! - [`source`]：源文件登记表与 [`Span`]
//! - [`diagnostic`]：统一的诊断结构与 ariadne 渲染
//!
//! 约束：本 crate 不得依赖任何其它 `ryupy-*` crate。

mod instance_capabilities;
pub use instance_capabilities::InstanceCapabilities;
pub mod diagnostic;
pub mod source;

pub use diagnostic::{Diagnostic, Diagnostics, Label, Severity};
pub use ruff_text_size::{Ranged, TextRange, TextSize};
pub use source::{FileId, SourceDb, SourceFile, Span};
