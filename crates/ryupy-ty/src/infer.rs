//! 推导上下文：并查集（Union-Find）。
//!
//! 每个尚未确定类型的局部变量 / 表达式分配一个 [`TyVar`]；遇到约束时 `unify`。
//! 推荐自己手写一遍（路径压缩 + 按秩合并），理解后再考虑换 `ena` crate。
//!
//! 设计要点：
//! - 并查集的每个根节点要么是未知（`None`），要么绑定了一个具体的 [`Ty`]；
//! - `unify(Known(a), Known(b))` 时 `a != b` 就是类型错误，但 `Ty::Error` 与任何类型都相容；
//! - `int` → `float` 的隐式提升**不在 unify 里做**，而在二元运算规则里显式处理，
//!   否则 `x = 1; x = 1.5` 这种代码会被悄悄接受，与「单态局部变量」的设计冲突。

#![allow(dead_code)] // 骨架阶段：check.rs 用上之后删除。

use crate::ty::Ty;

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct TyVar(pub u32);

/// 推导过程中的类型：已知或待定。
#[derive(Clone, Debug)]
pub enum InferTy {
    Known(Ty),
    Var(TyVar),
}

#[derive(Debug, Default)]
pub struct InferCtx {
    parent: Vec<u32>,
    rank: Vec<u8>,
    binding: Vec<Option<Ty>>,
}

impl InferCtx {
    pub fn new_var(&mut self) -> TyVar {
        let id = self.parent.len() as u32;
        self.parent.push(id);
        self.rank.push(0);
        self.binding.push(None);
        TyVar(id)
    }

    // TODO(M1.1)：实现 find / unify / resolve。
    // pub fn find(&mut self, v: TyVar) -> TyVar
    // pub fn unify(&mut self, a: &InferTy, b: &InferTy) -> Result<(), (Ty, Ty)>
    // pub fn resolve(&mut self, t: &InferTy) -> Option<Ty>
}
