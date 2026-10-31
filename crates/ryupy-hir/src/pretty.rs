//! HIR 文本转储，供 `ryupy dump --emit hir` 与 insta 快照使用。

use crate::hir::Module;

/// 目前直接用 `Debug` 输出，能用就行。
/// TODO(M1.1)：写成接近 Python 源码的缩进格式，快照更易读、diff 更小。
pub fn dump(module: &Module) -> String {
    format!("{module:#?}")
}
