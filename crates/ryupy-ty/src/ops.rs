//! 静态类型系统下的运算规则表（依照 docs/semantics.md §3）。
//!
//! 规则原则：
//! 1. 算术运算：
//!    - 整数与整数运算保持 `Int`，但真除法 `/` 总是产生 `Float`；
//!    - `Int` 与 `Float` 混合运算时自动提升为 `Float`；
//! 2. 位运算：
//!    - 仅允许 `Int` 参与，结果为 `Int`；
//! 3. 比较运算：
//!    - 数值（`Int` / `Float`）可任意混合比较，结果为 `Bool`；
//! 4. 逻辑与一元：
//!    - `not` 运算结果总是 `Bool`；`-` 保留原数值类型；
//! 5. 错误传播：
//!    - 遇到 `Ty::Error` 毒化类型静默传播，抑制级联报错。

use ryupy_hir::{BinOp, CmpOp, UnaryOp};

use crate::ty::Ty;

/// 检查二元运算符两端类型并确定结果类型。
pub fn check_binary_op(op: BinOp, lhs: &Ty, rhs: &Ty) -> Option<Ty> {
    if *lhs == Ty::Error || *rhs == Ty::Error {
        return Some(Ty::Error);
    }

    match (lhs, rhs) {
        // 整数 x 整数
        (Ty::Int, Ty::Int) => match op {
            BinOp::Add | BinOp::Sub | BinOp::Mul | BinOp::FloorDiv | BinOp::Mod | BinOp::Pow => {
                Some(Ty::Int)
            }
            BinOp::TrueDiv => Some(Ty::Float),
            BinOp::BitAnd | BinOp::BitOr | BinOp::BitXor | BinOp::LShift | BinOp::RShift => {
                Some(Ty::Int)
            }
        },
        // 浮点 x 浮点
        (Ty::Float, Ty::Float) => match op {
            BinOp::Add
            | BinOp::Sub
            | BinOp::Mul
            | BinOp::TrueDiv
            | BinOp::FloorDiv
            | BinOp::Mod
            | BinOp::Pow => Some(Ty::Float),
            _ => None,
        },
        // 整数与浮点混合提升为浮点
        (Ty::Int, Ty::Float) | (Ty::Float, Ty::Int) => match op {
            BinOp::Add
            | BinOp::Sub
            | BinOp::Mul
            | BinOp::TrueDiv
            | BinOp::FloorDiv
            | BinOp::Mod
            | BinOp::Pow => Some(Ty::Float),
            _ => None,
        },
        // 字符串拼接
        (Ty::Str, Ty::Str) => match op {
            BinOp::Add => Some(Ty::Str),
            _ => None,
        },
        _ => None,
    }
}

/// 检查一元运算符并确定结果类型。
pub fn check_unary_op(op: UnaryOp, operand: &Ty) -> Option<Ty> {
    if *operand == Ty::Error {
        return Some(Ty::Error);
    }

    match (op, operand) {
        (UnaryOp::Neg | UnaryOp::Pos, Ty::Int) => Some(Ty::Int),
        (UnaryOp::Neg | UnaryOp::Pos, Ty::Float) => Some(Ty::Float),
        (UnaryOp::Not, Ty::Bool | Ty::Int | Ty::Float | Ty::Str) => Some(Ty::Bool),
        (UnaryOp::Invert, Ty::Int) => Some(Ty::Int),
        _ => None,
    }
}

/// 检查比较运算符并确定结果类型（数值之间比较产生 Bool）。
pub fn check_cmp_op(_op: CmpOp, lhs: &Ty, rhs: &Ty) -> Option<Ty> {
    if *lhs == Ty::Error || *rhs == Ty::Error {
        return Some(Ty::Error);
    }

    match (lhs, rhs) {
        (Ty::Int | Ty::Float, Ty::Int | Ty::Float) => Some(Ty::Bool),
        (Ty::Bool, Ty::Bool) => Some(Ty::Bool),
        (Ty::Str, Ty::Str) => Some(Ty::Bool),
        _ => None,
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn table_driven_binary_ops() {
        assert_eq!(
            check_binary_op(BinOp::Add, &Ty::Int, &Ty::Int),
            Some(Ty::Int)
        );
        // 真除法 / 总是产生 float
        assert_eq!(
            check_binary_op(BinOp::TrueDiv, &Ty::Int, &Ty::Int),
            Some(Ty::Float)
        );
        // 整除 // 整数产生 int
        assert_eq!(
            check_binary_op(BinOp::FloorDiv, &Ty::Int, &Ty::Int),
            Some(Ty::Int)
        );
        // 混合提升
        assert_eq!(
            check_binary_op(BinOp::Add, &Ty::Int, &Ty::Float),
            Some(Ty::Float)
        );
        // 位运算只能是 int
        assert_eq!(
            check_binary_op(BinOp::BitAnd, &Ty::Int, &Ty::Int),
            Some(Ty::Int)
        );
        assert!(check_binary_op(BinOp::BitAnd, &Ty::Float, &Ty::Float).is_none());
        // 字符串拼接
        assert_eq!(
            check_binary_op(BinOp::Add, &Ty::Str, &Ty::Str),
            Some(Ty::Str)
        );
    }

    #[test]
    fn table_driven_cmp_and_unary() {
        assert_eq!(
            check_cmp_op(CmpOp::Lt, &Ty::Int, &Ty::Float),
            Some(Ty::Bool)
        );
        assert_eq!(check_unary_op(UnaryOp::Neg, &Ty::Int), Some(Ty::Int));
        assert_eq!(check_unary_op(UnaryOp::Not, &Ty::Int), Some(Ty::Bool));
    }
}
