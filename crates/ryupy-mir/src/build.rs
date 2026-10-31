//! HIR + 类型 → MIR。
//!
//! 核心是一个「当前基本块」游标的 builder：
//! - 表达式降级返回 `Operand`，复杂子表达式先存进临时局部变量；
//! - `if` / `while` / `for range` / 短路 `and` `or` 需要新建基本块并连接 terminator；
//! - `break` / `continue` 需要一个「循环目标栈」记录 (continue_bb, break_bb)；
//! - `return` 之后的代码落在一个新的不可达块里，最后由清理 Pass 删除；
//! - 函数末尾没有显式 `return` 时补 `Return(None)`。
//!
//! Python 语义改写在这里完成：
//! - `a // b`、`a % b`、`a ** b` → `Callee::Runtime(..)`；
//! - `int` 与 `float` 混合 → 先插入 `Cast { I64ToF64 }`；
//! - `print(a, b)` → 按各参数类型依次 `WriteI64` / `WriteF64` / ...，中间 `WriteSep`，末尾 `WriteNewline`；
//! - `str + str` → `StrConcat`；
//! - `for i in range(a, b, s)` → 计数循环（注意 `s` 为负数时比较方向相反，`s == 0` 要抛 `ValueError`）。

use ryupy_hir::Module;
use ryupy_ty::ModuleTypes;

use crate::mir::Program;

pub fn build_program(_hir: &Module, _types: &ModuleTypes) -> Program {
    todo!("M1.0：实现 HIR → MIR 构建，见 TODO.md")
}
