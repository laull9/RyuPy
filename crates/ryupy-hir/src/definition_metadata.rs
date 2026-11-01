//! 定义位置的默认值与注解表达式：两者属于外层作用域，不能放进被定义函数体。
use crate::hir::{ExprId, FuncId, Name};
use rustc_hash::FxHashMap;
use std::ops::{Deref, DerefMut};

#[derive(Default)]
pub struct DefinitionMetadata {
    pub defaults: FxHashMap<FuncId, Vec<Option<ExprId>>>,
    pub annotations: FxHashMap<FuncId, Vec<(Name, ExprId)>>,
}
impl Deref for DefinitionMetadata {
    type Target = FxHashMap<FuncId, Vec<Option<ExprId>>>;
    fn deref(&self) -> &Self::Target {
        &self.defaults
    }
}
impl DerefMut for DefinitionMetadata {
    fn deref_mut(&mut self) -> &mut Self::Target {
        &mut self.defaults
    }
}
