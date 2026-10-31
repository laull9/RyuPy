//! MIR 合法性检查。codegen 之前必跑，Pass 之后在 debug 构建下也跑。
//!
//! 检查项（逐步补全）：
//! - [ ] 不存在 `Terminator::Unreachable` 占位
//! - [ ] 所有 `LocalId` / `BlockId` 都在各自 arena 范围内
//! - [ ] `Binary` 两侧操作数类型一致；`Branch` 条件为 `Bool`
//! - [ ] `Return` 的值类型与函数返回类型一致
//! - [ ] `Call` 实参个数、类型与被调函数签名一致

use crate::mir::Program;

pub fn validate(_program: &Program) -> Result<(), Vec<String>> {
    // TODO(M1.0)：至少实现前两项。在这之前直接放行。
    Ok(())
}
