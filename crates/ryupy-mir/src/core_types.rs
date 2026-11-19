//! 通用伴生体的局部类型视图；原ModuleTypes保持不可变，兄弟签名与模块槽仍共享。
use ryupy_hir::FuncId;
use ryupy_ty::{FuncMode, ModuleTypes, TypeckResults};

#[derive(Clone, Copy)]
pub(crate) struct CoreTypes<'a> {
    original: &'a ModuleTypes,
    pub(crate) generic: Option<FuncId>,
}
impl<'a> CoreTypes<'a> {
    pub(crate) fn ordinary(original: &'a ModuleTypes) -> Self {
        Self {
            original,
            generic: None,
        }
    }
    pub(crate) fn mode(&self, id: FuncId) -> FuncMode {
        if self.generic == Some(id) {
            FuncMode::Dynamic
        } else {
            self.original.mode(id)
        }
    }
    pub(crate) fn is_dynamic(&self, id: FuncId) -> bool {
        self.mode(id).is_dynamic()
    }
    pub(crate) fn body(&self, id: FuncId) -> Option<&'a TypeckResults> {
        if self.generic == Some(id) {
            None
        } else {
            self.original.body(id)
        }
    }
}
impl std::ops::Deref for CoreTypes<'_> {
    type Target = ModuleTypes;
    fn deref(&self) -> &Self::Target {
        self.original
    }
}
