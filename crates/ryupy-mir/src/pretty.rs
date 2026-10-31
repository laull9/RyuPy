//! MIR 文本转储，供 `ryupy dump --emit mir` 与 insta 快照使用。

use crate::mir::Program;

/// TODO(M1.1)：改成类似 rustc `-Zunpretty=mir` 的格式：
///
/// ```text
/// fn fib(_0: i64) -> i64 {
///     let _1: bool;
///   bb0:
///     _1 = Lt(_0, const 2_i64);
///     branch _1 -> [true: bb1, false: bb2];
///   ...
/// }
/// ```
pub fn dump(program: &Program) -> String {
    format!("{program:#?}")
}
