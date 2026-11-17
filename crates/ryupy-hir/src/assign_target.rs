//! HIR 赋值目标：局部、模块变量、外层 cell、属性、下标与序列解包。

use crate::hir::{Binding, ExprId, LocalId, ModuleVarId, Name};

#[derive(Debug)]
pub enum AssignTarget {
    Local(LocalId),
    ModuleVar(ModuleVarId),
    /// 通过 `nonlocal` 写入外层函数的 cell（M3.0）：下标对应当前函数
    /// `ClosureInfo::free`。
    Free(usize),
    /// `obj.field = value`（dataclass 字段写入）。
    Attribute {
        base: ExprId,
        name: Name,
    },
    /// `obj[key] = value`。
    Subscript {
        base: ExprId,
        index: ExprId,
    },
    /// `a, b = ...` / `first, *middle, last = ...`。
    Sequence(Vec<SequenceItem>),
}

/// 序列解包的一个位置。
#[derive(Clone, Debug)]
pub enum SequenceItem {
    /// 普通目标：按位置取一个元素。
    One(SequenceTarget),
    /// 星号目标：把中间剩余的元素打包成列表。
    Rest(SequenceTarget),
}

/// 递归解包目标；对象、下标和下一层解包在轮到该目标时才执行。
#[derive(Clone, Debug)]
pub enum SequenceTarget {
    Sequence(Vec<SequenceItem>),
    Binding(Binding),
    Attribute { base: ExprId, name: Name },
    Subscript { base: ExprId, index: ExprId },
}
