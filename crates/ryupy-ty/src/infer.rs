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

use crate::ty::Ty;

#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub struct TyVar(pub u32);

/// 推导过程中的类型：已知或待定。
#[derive(Clone, Debug, PartialEq, Eq)]
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

    pub fn find(&mut self, v: TyVar) -> TyVar {
        let mut curr = v.0;
        while self.parent[curr as usize] != curr {
            let next = self.parent[curr as usize];
            self.parent[curr as usize] = self.parent[next as usize];
            curr = next;
        }
        TyVar(curr)
    }

    pub fn unify(&mut self, a: &InferTy, b: &InferTy) -> Result<Ty, (Ty, Ty)> {
        match (a, b) {
            (InferTy::Known(ta), InferTy::Known(tb)) => {
                if *ta == Ty::Error || *tb == Ty::Error {
                    Ok(Ty::Error)
                } else if *ta == Ty::OpaqueStr {
                    // 已确立的object槽只接收，不把任何实参反向窄化为str。
                    Ok(Ty::OpaqueStr)
                } else if ta.is_dynamic() || tb.is_dynamic() {
                    // 动态值可以与任何类型统一：Python 的变量本来就能装任意对象，
                    // 统一成 Dynamic 后整个函数会降级到动态路径（M2.1）。
                    Ok(Ty::Dynamic)
                } else if ta == tb {
                    Ok(ta.clone())
                } else {
                    Err((ta.clone(), tb.clone()))
                }
            }
            (InferTy::Var(v), InferTy::Known(t)) | (InferTy::Known(t), InferTy::Var(v)) => {
                let root = self.find(*v);
                if let Some(existing) = self.binding[root.0 as usize].clone() {
                    let res = self.unify(&InferTy::Known(existing), &InferTy::Known(t.clone()))?;
                    self.binding[root.0 as usize] = Some(res.clone());
                    Ok(res)
                } else {
                    self.binding[root.0 as usize] = Some(t.clone());
                    Ok(t.clone())
                }
            }
            (InferTy::Var(va), InferTy::Var(vb)) => {
                let root_a = self.find(*va);
                let root_b = self.find(*vb);
                if root_a == root_b {
                    return Ok(self.binding[root_a.0 as usize].clone().unwrap_or(Ty::Error));
                }

                let bound = match (
                    self.binding[root_a.0 as usize].clone(),
                    self.binding[root_b.0 as usize].clone(),
                ) {
                    (Some(ta), Some(tb)) => {
                        Some(self.unify(&InferTy::Known(ta), &InferTy::Known(tb))?)
                    }
                    (Some(ta), None) => Some(ta),
                    (None, Some(tb)) => Some(tb),
                    (None, None) => None,
                };

                let (new_root, child) =
                    match self.rank[root_a.0 as usize].cmp(&self.rank[root_b.0 as usize]) {
                        std::cmp::Ordering::Less => (root_b, root_a),
                        std::cmp::Ordering::Greater => (root_a, root_b),
                        std::cmp::Ordering::Equal => {
                            self.rank[root_a.0 as usize] += 1;
                            (root_a, root_b)
                        }
                    };
                self.parent[child.0 as usize] = new_root.0;
                self.binding[new_root.0 as usize] = bound.clone();

                Ok(bound.unwrap_or(Ty::Error))
            }
        }
    }

    /// 强制把一个推导变量绑定成指定类型（异构赋值时用来「忘记」先前结论）。
    ///
    /// 用途：`x = 1; x = "text"` 在 Python 里合法，静态推导无法给出单一类型，
    /// 于是把变量改绑成 `Dynamic`，让整个函数降级到动态路径。
    pub fn force(&mut self, t: &InferTy, ty: Ty) {
        if let InferTy::Var(v) = t {
            let root = self.find(*v);
            self.binding[root.0 as usize] = Some(ty);
        }
    }

    pub fn resolve(&mut self, t: &InferTy) -> Option<Ty> {
        match t {
            InferTy::Known(k) => Some(k.clone()),
            InferTy::Var(v) => {
                let root = self.find(*v);
                self.binding[root.0 as usize].clone()
            }
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn unifies_vars_and_resolves() {
        let mut ctx = InferCtx::default();
        let v1 = ctx.new_var();
        let v2 = ctx.new_var();

        // v1 == v2
        assert!(ctx.unify(&InferTy::Var(v1), &InferTy::Var(v2)).is_ok());

        // v1 == Int
        assert_eq!(
            ctx.unify(&InferTy::Var(v1), &InferTy::Known(Ty::Int)),
            Ok(Ty::Int)
        );

        // v2 should resolve to Int
        assert_eq!(ctx.resolve(&InferTy::Var(v2)), Some(Ty::Int));
    }

    #[test]
    fn reports_mismatch() {
        let mut ctx = InferCtx::default();
        let v = ctx.new_var();
        assert_eq!(
            ctx.unify(&InferTy::Var(v), &InferTy::Known(Ty::Int)),
            Ok(Ty::Int)
        );
        assert_eq!(
            ctx.unify(&InferTy::Var(v), &InferTy::Known(Ty::Float)),
            Err((Ty::Int, Ty::Float))
        );
    }

    #[test]
    fn poison_with_error() {
        let mut ctx = InferCtx::default();
        assert_eq!(
            ctx.unify(&InferTy::Known(Ty::Error), &InferTy::Known(Ty::Int)),
            Ok(Ty::Error)
        );
    }
}
