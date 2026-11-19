//! 内联类体读取外层隐式cell：临时单元素Env不外泄，仍使用既有free读取错误语义。
use crate::expr_builder::ExprBuilder;
use crate::*;

impl ExprBuilder<'_> {
    pub(crate) fn build_class_free_local(
        &mut self,
        local: ryupy_hir::LocalId,
        range: ryupy_base::TextRange,
    ) -> Operand {
        let target = self.local_map[&local];
        if !self.locals[target].is_cell {
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(self.hir.file, range),
                    "类体free读取缺少定义者的真实局部cell",
                )
                .with_code("E0300"),
            );
            return Operand::Const(Constant::Null);
        }
        let name = self.hir.functions[self.current_hir_fn].body.locals[local]
            .name
            .to_string();
        self.runtime_call(
            RuntimeFn::CellGetFree,
            vec![
                Operand::Array(vec![Operand::Local(target)]),
                Operand::Const(Constant::I64(0)),
                Operand::Const(Constant::Str(name)),
            ],
            MirType::Dynamic,
        )
    }
    pub(crate) fn build_lexical_cell_read(
        &mut self,
        class: ryupy_hir::ClassId,
        range: ryupy_base::TextRange,
    ) -> Operand {
        let cell = self
            .lexical_cell_stack
            .iter()
            .rev()
            .find_map(|(id, cell)| (*id == class).then(|| cell.clone()));
        let Some(cell) = cell else {
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(self.hir.file, range),
                    "内层类体缺少已登记的外层定义cell",
                )
                .with_code("E0300"),
            );
            return Operand::Const(Constant::Null);
        };
        self.runtime_call(
            RuntimeFn::CellGetFree,
            vec![
                Operand::Array(vec![cell]),
                Operand::Const(Constant::I64(0)),
                Operand::Const(Constant::Str("__class__".into())),
            ],
            MirType::Dynamic,
        )
    }
}

/// 只有当前owner登记的raw cell可以进入此不逃逸的单元素临时Env。
pub(crate) fn is_direct_read(value: &Rvalue, cell: &ClassCellDefinition) -> bool {
    matches!(value,
        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::CellGetFree), args }
        if matches!(args.as_slice(), [Operand::Array(values), Operand::Const(Constant::I64(0)), Operand::Const(Constant::Str(name))]
            if name == "__class__" && matches!(values.as_slice(), [Operand::Local(local)] if *local == cell.cell)))
}
