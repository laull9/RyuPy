//! MIR（Mid-level IR）：基于基本块的控制流图。
//!
//! 设计取舍（见 ADR-0002）：
//! - Phase 1 采用 **rustc 风格的「局部变量 + 基本块」非 SSA 形式**，而不是严格 SSA。
//!   CIL 本身就是「局部变量槽 + 求值栈」，非 SSA 形式可以几乎一一映射，省掉 φ 节点消解。
//!   Phase 3 做数据流优化时，再按需在 MIR 之上构造 SSA 视图。
//! - MIR 的类型已经「定型」：每个局部变量都有确定的 [`MirType`]，操作已区分整数 / 浮点版本，
//!   Python 特有语义（`//`、`%`、`**`）已改写为运行时调用。codegen 不需要再理解 Python。
//! - MIR 有自己的 ID 体系（[`FnId`]），**codegen 只依赖本 crate，不接触 HIR**。

pub mod build;
mod mir;
pub mod passes;
pub mod pretty;
pub mod validate;

pub use build::build_program;
pub use mir::*;
