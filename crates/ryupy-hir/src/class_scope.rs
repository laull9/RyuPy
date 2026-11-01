//! 类体源序绑定：条件两侧各自推演，合流区分可能与必定已绑定。
use crate::hir::Name;
use rustc_hash::FxHashSet;

#[derive(Clone, Default)]
pub struct ClassScope {
    possible: FxHashSet<Name>,
    definite: FxHashSet<Name>,
}
impl ClassScope {
    pub(crate) fn contains(&self, name: &Name) -> bool {
        self.possible.contains(name)
    }
    pub(crate) fn definite(&self, name: &Name) -> bool {
        self.definite.contains(name)
    }
    pub(crate) fn insert(&mut self, name: Name) {
        self.possible.insert(name.clone());
        self.definite.insert(name);
    }
    pub(crate) fn merge(&mut self, left: Self, right: Self) {
        self.possible = left.possible.union(&right.possible).cloned().collect();
        self.definite = left
            .definite
            .intersection(&right.definite)
            .cloned()
            .collect();
    }
}
