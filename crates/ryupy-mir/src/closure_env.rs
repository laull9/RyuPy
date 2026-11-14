//! 构造闭包环境：普通局部/free 共享 cell 与类定义的特殊 cell 使用同一 Env ABI。
use crate::expr_builder::ExprBuilder;
use crate::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};
use ryupy_base::Diagnostic;
impl ExprBuilder<'_> {
    /// 构造嵌套函数的闭包环境：按被创建函数的 `free` 顺序取 cell。
    ///
    /// 每个名字要么是当前函数的 cell 局部（直接引用槽位），要么是当前函数
    /// 自己的 free 变量（从 env 里取出对应 cell，向上透传）。
    pub(crate) fn build_closure_env(&mut self, nested_fn: ryupy_hir::FuncId) -> Vec<Operand> {
        let free: Vec<ryupy_hir::Name> = self.hir.functions[nested_fn].closure.free.clone();
        let own_cells = self.hir.functions[self.current_hir_fn]
            .closure
            .cells
            .clone();
        let own_free = self.hir.functions[self.current_hir_fn].closure.free.clone();

        let mut ops = Vec::with_capacity(free.len());
        for name in &free {
            if name.as_str() == "__class__"
                && let Some(class) = self.hir.functions[nested_fn].closure.class_cell
                && let Some((active, cell)) = &self.class_cell
                && *active == class
            {
                // 仅定义点的特殊绑定可进入此分支；同名普通属性/局部绝不替代它。
                ops.push(cell.clone());
            } else if own_cells.contains(name) {
                // 当前函数的 cell 局部：直接引用槽位（槽位里就是 object[1]）。
                let hir_local = self.hir.functions[self.current_hir_fn]
                    .body
                    .locals
                    .iter()
                    .find(|(_, l)| l.name == *name)
                    .map(|(id, _)| id);
                match hir_local.and_then(|id| self.local_map.get(&id)) {
                    Some(&mir_local) => ops.push(Operand::Local(mir_local)),
                    None => ops.push(self.missing_cell_operand(name)),
                }
            } else if let Some(idx) = own_free.iter().position(|n| n == name) {
                // 向上透传：从当前函数的 env 里取出外层 cell。
                let env = self.env_operand();
                let cell = self.alloc_temp(MirType::Dynamic);
                self.push_stmt(Statement::Assign {
                    dest: cell,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ArrayGet),
                        args: vec![env, Operand::Const(Constant::I64(idx as i64))],
                    },
                });
                ops.push(Operand::Local(cell));
            } else {
                ops.push(self.missing_cell_operand(name));
            }
        }
        ops
    }

    /// free 名字解析失败（理论上不可达）：报内部诊断并放 None 占位。
    fn missing_cell_operand(&mut self, name: &ryupy_hir::Name) -> Operand {
        let range = self.hir.functions[self.current_hir_fn].range;
        self.diagnostics.push(
            Diagnostic::error(
                ryupy_base::Span::new(self.hir.file, range),
                format!("内部错误：闭包环境缺少 cell `{name}`"),
            )
            .with_code("E0101")
            .with_primary_label("free 变量分析与 MIR 构建不一致"),
        );
        let none = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: none,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::DynNone),
                args: Vec::new(),
            },
        });
        Operand::Local(none)
    }
}
