//! MIR数值操作及其固定运行时编码；从数据结构主体拆分，保留全部语义说明。

/// 动态运算的操作码。数值必须与 C# 侧 `PyOps` 的常量一致（ADR-0010 的 ABI）。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum DynOp {
    Add,
    Sub,
    Mul,
    TrueDiv,
    FloorDiv,
    Mod,
    Pow,
    BitAnd,
    BitOr,
    BitXor,
    LShift,
    RShift,
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge,
    Neg,
    Pos,
    Not,
    Invert,
}

impl DynOp {
    /// 增量赋值的操作码标记位：`code() | INPLACE_FLAG`。**与 `PyOps.InPlaceFlag` 同步。**
    pub const INPLACE_FLAG: i32 = 0x100;

    /// 运行时操作码编号。**修改必须与 `runtime/RyuPy.Runtime/PyOps.cs` 同步。**
    pub fn code(self) -> i32 {
        match self {
            DynOp::Add => 1,
            DynOp::Sub => 2,
            DynOp::Mul => 3,
            DynOp::TrueDiv => 4,
            DynOp::FloorDiv => 5,
            DynOp::Mod => 6,
            DynOp::Pow => 7,
            DynOp::BitAnd => 8,
            DynOp::BitOr => 9,
            DynOp::BitXor => 10,
            DynOp::LShift => 11,
            DynOp::RShift => 12,
            // 比较与一元运算的编号各自从 1 开始，由调用的运行时方法区分
            DynOp::Eq => 1,
            DynOp::Ne => 2,
            DynOp::Lt => 3,
            DynOp::Le => 4,
            DynOp::Gt => 5,
            DynOp::Ge => 6,
            DynOp::Neg => 1,
            DynOp::Pos => 2,
            DynOp::Not => 3,
            DynOp::Invert => 4,
        }
    }
}

/// 已经是 CIL 层面的语义：`Div` 对整数是向零截断。Python 的 `//` 和 `%` 在构建 MIR 时
/// 就已改写成 [`crate::RuntimeFn`] 调用，不会出现在这里。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum BinOp {
    Add,
    /// i64模2^64加法；溢出判定/动态回退必须由MIR显式构造。
    WrappingAdd,
    Sub,
    Mul,
    Div,
    BitAnd,
    BitOr,
    BitXor,
    Shl,
    Shr,
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum UnaryOp {
    Neg,
    Not,
    BitNot,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum CastKind {
    I64ToF64,
    /// `int(x)`：向零截断，NaN/Inf 需抛 Python 异常（由运行时处理）。
    F64ToI64,
    BoolToI64,
}
