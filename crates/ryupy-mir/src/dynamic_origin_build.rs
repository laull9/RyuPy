//! 按真实收敛模式提供帧载体：Dynamic用self，Static用自身signature，模块冷建执行来源。
use crate::build::FunctionBuilder;
use crate::mir::{Constant, LocalDecl, LocalId, MirType, Operand, RuntimeFn};
use rustc_hash::FxHashSet;
use ryupy_hir::{FuncId, FunctionKind, Module, StmtKind};
use ryupy_ty::FuncMode;

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

    pub(crate) fn frame_origin(
        &mut self,
        source: crate::GlobalId,
        signature: Option<crate::GlobalId>,
    ) -> Option<Operand> {
        if let Some(owner) = self.dynamic_owner {
            return Some(Operand::Local(owner));
        }

        if self.hir.functions[self.current_hir_fn].kind != FunctionKind::ModuleMain {
            return signature.map(Operand::Global);
        }
        // source属于实际代码，state只提供目标环境；不能用state.Code推断运行的是谁。
        let context = self
            .module_state
            .map(Operand::Local)
            .unwrap_or(Operand::Const(Constant::Null));
        Some(self.expr_builder().runtime_call(
            RuntimeFn::ModuleExecutionSourceWithCode,
            vec![Operand::Global(source), context],
            MirType::Dynamic,
        ))
    }
}
