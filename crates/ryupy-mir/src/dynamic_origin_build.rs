//! 仅按收敛后的Dynamic模式添加实际self；模块入口冷建一次本次执行的来源。
use crate::build::FunctionBuilder;
use crate::mir::{Constant, LocalDecl, LocalId, MirType, Operand, RuntimeFn};
use rustc_hash::FxHashSet;
use ryupy_hir::{FuncId, FunctionKind, Module, StmtKind};
use ryupy_ty::{FuncMode, Ty};

/// 只收普通定义语句，装饰器结果和嵌套实例不得混进代码的私有直调槽。
pub(crate) fn plain_definitions(hir: &Module) -> FxHashSet<FuncId> {
    hir.functions
        .iter()
        .flat_map(|(_, f)| f.body.stmts.iter())
        .filter_map(|(_, stmt)| match &stmt.kind {
            StmtKind::FunctionDefaults(id) => Some(*id),
            _ => None,
        })
        .collect()
}

impl FunctionBuilder<'_> {
    pub(crate) fn add_dynamic_owner(&mut self, params: &mut Vec<LocalId>) {
        if self.hir.functions[self.current_hir_fn].kind == FunctionKind::ModuleMain
            || self.types.mode(self.current_hir_fn) != FuncMode::Dynamic
        {
            return;
        }
        let owner = self.locals.alloc(LocalDecl {
            ty: MirType::Dynamic,
            name: Some("__function_owner".to_owned()),
            is_cell: false,
        });
        params.push(owner);
        self.dynamic_owner = Some(owner);
    }

    pub(crate) fn frame_origin(&mut self) -> Option<Operand> {
        if let Some(owner) = self.dynamic_owner {
            return Some(Operand::Local(owner));
        }

        if let Some(state) = self.module_state {
            return Some(self.expr_builder().runtime_call(
                RuntimeFn::InstanceExecutionSource,
                vec![Operand::Local(state)],
                MirType::Dynamic,
            ));
        }
        if self.hir.functions[self.current_hir_fn].kind != FunctionKind::ModuleMain {
            return None;
        }
        // 此处在HIR模块前言之前，不读刚重置的__name__槽；与前言使用同一初始名。
        let name = Operand::Const(Constant::Str(self.hir.info.name.clone()));
        let boxed = self
            .expr_builder()
            .coerce(name, &Ty::Str, &MirType::Dynamic);
        Some(self.expr_builder().runtime_call(
            RuntimeFn::ModuleExecutionSource,
            vec![boxed],
            MirType::Dynamic,
        ))
    }
}
